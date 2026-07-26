using System;
using System.Collections.Generic;

using Server;
using Server.ContextMenus;
using Server.Custom.AIGM.Characters.Waylander;
using Server.Mobiles;

namespace Server.Custom.AIGM.Tasks
{
    public static class AIGMRosterCompanionBindingService
    {
        public const int OrdinaryBindRange = 12;

        private const int InviteEntryLocalization = 6101;
        private const int FollowEntryLocalization = 6108;
        private const int StandDownEntryLocalization = 6112;
        private const int ReleaseEntryLocalization = 6118;
        private const int StatusEntryLocalization = 6146;

        private enum ContextAction
        {
            Bind,
            Unbind,
            Follow,
            StandDown,
            Status
        }

        public static bool TryBind(IAIGMRosterTaskAgent agent, Mobile owner, out string response)
        {
            response = "Binding failed.";

            BaseCreature creature;
            if (!TryGetCreature(agent, out creature, out response))
                return false;

            if (owner == null || owner.Deleted)
            {
                response = "A valid owner is required.";
                return false;
            }

            WaylanderCharacterDefinition definition = agent.RosterDefinition;
            WaylanderRosterControlPolicy policy = GetControlPolicy(agent);
            string rejection;
            if (!PolicyAllowsBind(definition, policy, owner, out rejection))
            {
                response = String.Format("Binding rejected: {0}.", rejection);
                LogBindRejected(creature, owner, policy, rejection);
                return false;
            }

            if (!IsGameMaster(owner) && (creature.Map != owner.Map || !creature.InRange(owner, OrdinaryBindRange)))
            {
                response = String.Format("Binding rejected: {0} must be within {1} tiles on the same map.", creature.Name, OrdinaryBindRange);
                LogBindRejected(creature, owner, policy, "range_or_map");
                return false;
            }

            if (agent.RosterBoundCompanion)
            {
                if (agent.RosterTrustedCommanderSerial == owner.Serial)
                {
                    response = String.Format("Already bound. {0}", BuildCapacityStatus(owner));
                    return true;
                }

                response = "Binding rejected: this roster agent is already bound to another commander.";
                LogBindRejected(creature, owner, policy, "already_bound_other_commander");
                return false;
            }

            AIGMRosterTaskState state = AIGMRosterTaskService.GetOrCreateState(agent);
            if (state != null
                && state.OperationalMode == AIGMOperationalMode.AbsoluteGMHold
                && state.StandDownCommanderSerial != Serial.MinusOne
                && state.StandDownCommanderSerial != owner.Serial)
            {
                response = "Binding rejected: this roster agent is under an absolute hold owned by another authority.";
                LogBindRejected(creature, owner, policy, "absolute_hold_other_authority");
                return false;
            }

            if (HasDuplicateBoundUnique(agent, creature, owner))
            {
                response = "Binding rejected: another bound instance of this unique canonical character already exists for this owner.";
                LogBindRejected(creature, owner, policy, "duplicate_unique_bound");
                return false;
            }

            int count = AIGMRosterCompanionCapacityService.CountBoundCompanions(owner);
            if (count >= AIGMRosterCompanionCapacityService.MaxRosterCompanions)
            {
                response = String.Format("Roster companion capacity reached ({0}/{1}).", count, AIGMRosterCompanionCapacityService.MaxRosterCompanions);
                LogBindRejected(creature, owner, policy, "capacity_full");
                return false;
            }

            if (state != null)
            {
                state.ClearTask(true);
                if (state.HomeLocation == Point3D.Zero)
                {
                    state.HomeLocation = creature.Home != Point3D.Zero ? creature.Home : creature.Location;
                    state.HomeMap = creature.Map;
                }
            }

            agent.RosterBoundCompanion = true;
            agent.RosterTrustedCommanderSerial = owner.Serial;

            state = AIGMRosterTaskService.GetOrCreateState(agent);
            if (state != null)
            {
                state.TrustedCommanderSerial = owner.Serial;
                state.IsAutonomous = false;
                state.TaskStatus = "bound_idle";
                state.GroupId = BuildOwnerGroupId(owner);
                state.Persists = true;
                state.PassiveTaskMode = agent.RosterPassiveTestMode;
                state.AllowSelfDefenseWhileStandingDown = true;
            }

            creature.ControlTarget = null;
            creature.ControlOrder = OrderType.Stay;
            creature.CantWalk = false;
            AIGMRosterTaskService.ClearImmediateCombatState(creature, false);
            AIGMRosterTaskService.RestoreIdleFightMode(agent, creature);
            AIGMOperationalControlService.Release(creature, owner, "bind_roster_companion");

            if (state != null)
                state.TaskStatus = "bound_idle";

            AIGMExecutionLog.Write(
                "AIGM_ROSTER_BIND actor={0} owner={1} policy={2} aigmCapacity={3}/{4} servuoFollowers={5}/{6}",
                Describe(creature),
                Describe(owner),
                policy,
                AIGMRosterCompanionCapacityService.CountBoundCompanions(owner),
                AIGMRosterCompanionCapacityService.MaxRosterCompanions,
                owner.Followers,
                owner.FollowersMax);

            response = String.Format("Bound {0} as a roster companion. {1}", creature.Name, BuildCapacityStatus(owner));
            return true;
        }

        public static bool TryUnbind(IAIGMRosterTaskAgent agent, Mobile requester, out string response)
        {
            response = "Unbind failed.";

            BaseCreature creature;
            if (!TryGetCreature(agent, out creature, out response))
                return false;

            Mobile previousOwner = agent.RosterTrustedCommanderSerial != Serial.MinusOne
                ? World.FindMobile(agent.RosterTrustedCommanderSerial)
                : null;

            if (!agent.RosterBoundCompanion)
            {
                response = String.Format("Already autonomous. {0}", BuildCapacityStatus(requester ?? previousOwner));
                return true;
            }

            if (requester != null
                && requester.AccessLevel < AccessLevel.GameMaster
                && agent.RosterTrustedCommanderSerial != requester.Serial)
            {
                response = "Unbind rejected: only the trusted commander or a GM may release this companion.";
                return false;
            }

            AIGMOperationalControlService.ClearAllOperationalState(creature, AIGMOperationalStopMode.CancelMission, requester, "unbind_roster_companion");

            agent.RosterBoundCompanion = false;
            agent.RosterTrustedCommanderSerial = Serial.MinusOne;

            AIGMRosterTaskState state = AIGMRosterTaskService.GetOrCreateState(agent);
            if (state != null)
            {
                state.TrustedCommanderSerial = agent.RosterTrustedCommanderSerial;
                state.IsAutonomous = !agent.RosterBoundCompanion;
                state.GroupId = String.Empty;
                state.TaskStatus = state.IsAutonomous ? "autonomous_idle" : "standard_control_remains";
                state.OperationalMode = AIGMOperationalMode.Active;
                state.LastOperationalCommand = "unbind_roster_companion";
                state.LastOperationalCommandTime = DateTime.UtcNow;
                state.StandDownCommanderSerial = Serial.MinusOne;
                state.AllowSelfDefenseWhileStandingDown = true;
            }

            creature.ControlTarget = null;
            creature.ControlOrder = OrderType.Stay;
            creature.CantWalk = false;
            AIGMRosterTaskService.ClearImmediateCombatState(creature, false);
            AIGMRosterTaskService.RestoreIdleFightMode(agent, creature);

            AIGMExecutionLog.Write(
                "AIGM_ROSTER_UNBIND actor={0} requester={1} previousOwner={2} standardControl={3} aigmCapacity={4}/{5}",
                Describe(creature),
                Describe(requester),
                Describe(previousOwner),
                creature.Controlled && creature.ControlMaster != null,
                previousOwner != null ? AIGMRosterCompanionCapacityService.CountBoundCompanions(previousOwner) : -1,
                AIGMRosterCompanionCapacityService.MaxRosterCompanions);

            if (creature.Controlled && creature.ControlMaster != null)
            {
                response = String.Format(
                    "Custom roster binding cleared where present; standard ServUO control remains with {0}. {1}",
                    Describe(creature.ControlMaster),
                    BuildCapacityStatus(previousOwner ?? requester));
            }
            else
            {
                response = String.Format("Released {0} to autonomous mode. {1}", creature.Name, BuildCapacityStatus(previousOwner ?? requester));
            }

            return true;
        }

        public static void AddContextMenuEntries(IAIGMRosterTaskAgent agent, Mobile from, List<ContextMenuEntry> list)
        {
            if (agent == null || from == null || list == null || !from.Alive)
                return;

            if (ShouldShowBind(agent, from))
                list.Add(new RosterContextEntry(agent, ContextAction.Bind, InviteEntryLocalization, 3));

            if (ShouldShowUnbind(agent, from))
                list.Add(new RosterContextEntry(agent, ContextAction.Unbind, ReleaseEntryLocalization, 3));

            if (AIGMRosterCompanionCapacityService.IsTrustedCommander(agent, from))
            {
                list.Add(new RosterContextEntry(agent, ContextAction.Follow, FollowEntryLocalization, 12));
                list.Add(new RosterContextEntry(agent, ContextAction.StandDown, StandDownEntryLocalization, 12));
                list.Add(new RosterContextEntry(agent, ContextAction.Status, StatusEntryLocalization, 12));
            }
        }

        public static string BuildCapacityStatus(Mobile owner)
        {
            if (owner == null)
                return String.Format("AIGM roster: unavailable / {0}; ServUO followers: unavailable. These limits are separate.", AIGMRosterCompanionCapacityService.MaxRosterCompanions);

            return String.Format(
                "{0} companion capacity: AIGM roster {1}/{2} bound; ServUO followers {3}/{4} slots used. These limits are separate.",
                String.IsNullOrWhiteSpace(owner.Name) ? owner.GetType().Name : owner.Name,
                AIGMRosterCompanionCapacityService.CountBoundCompanions(owner),
                AIGMRosterCompanionCapacityService.MaxRosterCompanions,
                owner.Followers,
                owner.FollowersMax);
        }

        public static string BuildAgentControlStatus(IAIGMRosterTaskAgent agent)
        {
            BaseCreature creature = agent as BaseCreature;
            if (agent == null || creature == null)
                return "Control policy: unavailable; Bound: no; Commander: none; Autonomous: unknown";

            AIGMRosterTaskState state = AIGMRosterTaskService.GetOrCreateState(agent);
            Mobile commander = agent.RosterTrustedCommanderSerial != Serial.MinusOne
                ? World.FindMobile(agent.RosterTrustedCommanderSerial)
                : null;

            string capacity = commander != null
                ? String.Format("; AIGM capacity: {0}/{1}; ServUO followers: {2}/{3}",
                    AIGMRosterCompanionCapacityService.CountBoundCompanions(commander),
                    AIGMRosterCompanionCapacityService.MaxRosterCompanions,
                    commander.Followers,
                    commander.FollowersMax)
                : String.Empty;

            return String.Format(
                "Control policy: {0}; Bound: {1}; Commander: {2}; Autonomous: {3}; Standard control: {4}{5}",
                GetControlPolicy(agent),
                agent.RosterBoundCompanion ? "yes" : "no",
                Describe(commander),
                state != null && state.IsAutonomous ? "yes" : "no",
                creature.Controlled && creature.ControlMaster != null ? Describe(creature.ControlMaster) : "none",
                capacity);
        }

        public static WaylanderRosterControlPolicy GetControlPolicy(IAIGMRosterTaskAgent agent)
        {
            WaylanderCharacterDefinition definition = agent != null ? agent.RosterDefinition : null;
            return definition != null ? definition.ControlPolicy : WaylanderRosterControlPolicy.NonCompanion;
        }

        private static bool ShouldShowBind(IAIGMRosterTaskAgent agent, Mobile from)
        {
            if (agent == null || from == null || agent.RosterBoundCompanion)
                return false;

            BaseCreature creature = agent as BaseCreature;
            if (creature == null || creature.Deleted || creature.Map != from.Map || !creature.InRange(from, OrdinaryBindRange))
                return false;

            string rejection;
            return PolicyAllowsBind(agent.RosterDefinition, GetControlPolicy(agent), from, out rejection);
        }

        private static bool ShouldShowUnbind(IAIGMRosterTaskAgent agent, Mobile from)
        {
            if (agent == null || from == null || !agent.RosterBoundCompanion)
                return false;

            return from.AccessLevel >= AccessLevel.GameMaster || agent.RosterTrustedCommanderSerial == from.Serial;
        }

        private static bool PolicyAllowsBind(WaylanderCharacterDefinition definition, WaylanderRosterControlPolicy policy, Mobile owner, out string rejection)
        {
            rejection = null;

            if (definition == null)
            {
                rejection = "missing roster definition";
                return false;
            }

            if (definition.RequiresLoreFlag && !IsGameMaster(owner))
            {
                rejection = "lore flag or GM authorization required";
                return false;
            }

            switch (policy)
            {
                case WaylanderRosterControlPolicy.BindableCompanion:
                    if (IsGameMaster(owner))
                        return true;

                    rejection = "owner authorization registry is not configured; GM authorization required";
                    return false;
                case WaylanderRosterControlPolicy.ScenarioBindable:
                    if (IsGameMaster(owner))
                        return true;

                    rejection = "scenario permission or GM authorization required";
                    return false;
                case WaylanderRosterControlPolicy.AutonomousOnly:
                    rejection = "autonomous-only roster agent";
                    return false;
                case WaylanderRosterControlPolicy.MountOnly:
                    rejection = "mount-only roster agent";
                    return false;
                default:
                    rejection = "not a companion roster agent";
                    return false;
            }
        }

        private static bool HasDuplicateBoundUnique(IAIGMRosterTaskAgent agent, BaseCreature creature, Mobile owner)
        {
            WaylanderCharacterDefinition definition = agent != null ? agent.RosterDefinition : null;
            if (agent == null || creature == null || owner == null || definition == null || !definition.IsUnique || definition.AllowMultiples)
                return false;

            string canonical = String.IsNullOrWhiteSpace(definition.CanonicalPersonId) ? agent.RosterCharacterId : definition.CanonicalPersonId;
            foreach (Mobile mobile in World.Mobiles.Values)
            {
                if (mobile == null || mobile.Deleted || mobile == creature)
                    continue;

                IAIGMRosterTaskAgent other = mobile as IAIGMRosterTaskAgent;
                if (other == null || !other.RosterBoundCompanion || other.RosterTrustedCommanderSerial != owner.Serial)
                    continue;

                WaylanderCharacterDefinition otherDefinition = other.RosterDefinition;
                string otherCanonical = otherDefinition != null && !String.IsNullOrWhiteSpace(otherDefinition.CanonicalPersonId)
                    ? otherDefinition.CanonicalPersonId
                    : other.RosterCharacterId;

                if (String.Equals(canonical, otherCanonical, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static bool TryGetCreature(IAIGMRosterTaskAgent agent, out BaseCreature creature, out string response)
        {
            creature = agent as BaseCreature;
            if (agent == null || creature == null || creature.Deleted)
            {
                response = "A live roster task agent is required.";
                return false;
            }

            response = null;
            return true;
        }

        private static string BuildOwnerGroupId(Mobile owner)
        {
            return owner == null ? String.Empty : String.Format("bound:{0}", owner.Serial.Value);
        }

        private static bool IsGameMaster(Mobile mobile)
        {
            return mobile != null && mobile.AccessLevel >= AccessLevel.GameMaster;
        }

        private static void LogBindRejected(BaseCreature creature, Mobile owner, WaylanderRosterControlPolicy policy, string reason)
        {
            AIGMExecutionLog.Write(
                "AIGM_ROSTER_BIND_REJECT actor={0} owner={1} policy={2} reason={3}",
                Describe(creature),
                Describe(owner),
                policy,
                Safe(reason));
        }

        private static string Describe(Mobile mobile)
        {
            return mobile == null ? "none" : String.Format("{0}[0x{1:X8}]", String.IsNullOrWhiteSpace(mobile.Name) ? mobile.GetType().Name : mobile.Name, mobile.Serial.Value);
        }

        private static string Safe(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return String.Empty;

            value = value.Replace('"', '\'').Replace('\r', ' ').Replace('\n', ' ');
            return value.Length > 220 ? value.Substring(0, 220) : value;
        }

        private sealed class RosterContextEntry : ContextMenuEntry
        {
            private readonly IAIGMRosterTaskAgent _agent;
            private readonly ContextAction _action;

            public RosterContextEntry(IAIGMRosterTaskAgent agent, ContextAction action, int localization, int range)
                : base(localization, range)
            {
                _agent = agent;
                _action = action;
            }

            public override void OnClick()
            {
                Mobile from = Owner != null ? Owner.From : null;
                if (from == null || _agent == null)
                    return;

                string response;
                switch (_action)
                {
                    case ContextAction.Bind:
                        TryBind(_agent, from, out response);
                        break;
                    case ContextAction.Unbind:
                        TryUnbind(_agent, from, out response);
                        break;
                    case ContextAction.Follow:
                        AIGMRosterCommandService.TryHandleCommandText(from, _agent.RosterCharacterId, "follow me", out response);
                        break;
                    case ContextAction.StandDown:
                        AIGMRosterCommandService.TryHandleCommandText(from, _agent.RosterCharacterId, "stand down", out response);
                        break;
                    default:
                        response = AIGMRosterTaskService.BuildStatus(_agent);
                        break;
                }

                if (!String.IsNullOrWhiteSpace(response))
                    from.SendMessage(response);
            }
        }
    }
}
