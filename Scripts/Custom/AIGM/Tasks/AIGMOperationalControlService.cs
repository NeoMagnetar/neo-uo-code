using System;
using System.Collections.Generic;

using Server;
using Server.Mobiles;

namespace Server.Custom.AIGM.Tasks
{
    public enum AIGMOperationalMode
    {
        Active = 0,
        CancelMission = 1,
        PassiveStandDown = 2,
        AbsoluteGMHold = 3
    }

    public enum AIGMOperationalStopMode
    {
        CancelMission = 0,
        PassiveStandDown = 1,
        AbsoluteGMHold = 2
    }

    public enum AIGMOperationalAction
    {
        ExplicitCommand,
        TaskPulse,
        TargetAcquisition,
        Movement,
        CombatAssignment,
        ThreatAwareness,
        SquadAlert,
        Follow,
        Travel,
        Tracking,
        Locate,
        DirectSelfDefense,
        PersistenceResume,
        ReturnHome,
        Patrol,
        Guard,
        Regroup
    }

    internal sealed class AIGMOperationalRegistryState
    {
        public AIGMOperationalMode OperationalMode = AIGMOperationalMode.Active;
        public int ControlEpoch;
        public string LastOperationalCommand = String.Empty;
        public DateTime LastOperationalCommandTime = DateTime.MinValue;
        public Serial StandDownCommanderSerial = Serial.MinusOne;
        public bool AllowSelfDefenseWhileStandingDown = true;
    }

    public static class AIGMOperationalControlService
    {
        private static readonly Dictionary<Serial, AIGMOperationalRegistryState> Registry = new Dictionary<Serial, AIGMOperationalRegistryState>();
        private static readonly Dictionary<string, DateTime> LastRejectLogUtc = new Dictionary<string, DateTime>();
        private static readonly TimeSpan RejectLogThrottle = TimeSpan.FromSeconds(2.0);

        public static AIGMOperationalMode GetMode(Mobile mobile)
        {
            AIGMRosterTaskState rosterState = GetRosterState(mobile, true);
            if (rosterState != null)
                return rosterState.OperationalMode;

            return GetRegistryState(mobile, false).OperationalMode;
        }

        public static int GetEpoch(Mobile mobile)
        {
            AIGMRosterTaskState rosterState = GetRosterState(mobile, false);
            if (rosterState != null)
                return rosterState.ControlEpoch;

            return GetRegistryState(mobile, false).ControlEpoch;
        }

        public static string DescribeState(Mobile mobile)
        {
            return String.Format(
                "OperationalMode={0}; Epoch={1}; LastCommand={2}",
                GetMode(mobile),
                GetEpoch(mobile),
                Safe(GetLastCommand(mobile)));
        }

        public static bool BeginExplicitCommand(Mobile mobile, Mobile commander, string commandName, out string rejectionReason)
        {
            rejectionReason = null;

            if (mobile == null || mobile.Deleted)
            {
                rejectionReason = "mobile_invalid";
                return false;
            }

            AIGMOperationalMode current = GetMode(mobile);
            if (current == AIGMOperationalMode.AbsoluteGMHold && (commander == null || commander.AccessLevel < AccessLevel.GameMaster))
            {
                rejectionReason = "absolute_hold_requires_gm_release";
                LogCallbackReject(mobile, AIGMOperationalAction.ExplicitCommand, GetEpoch(mobile), GetEpoch(mobile), rejectionReason);
                return false;
            }

            SetMode(mobile, AIGMOperationalMode.Active, commander, commandName, true, true);
            return true;
        }

        public static void Release(Mobile mobile, Mobile commander, string reason)
        {
            if (mobile == null || mobile.Deleted)
                return;

            if (GetMode(mobile) == AIGMOperationalMode.AbsoluteGMHold && (commander == null || commander.AccessLevel < AccessLevel.GameMaster))
            {
                LogCallbackReject(mobile, AIGMOperationalAction.ExplicitCommand, GetEpoch(mobile), GetEpoch(mobile), "absolute_hold_release_requires_gm");
                return;
            }

            SetMode(mobile, AIGMOperationalMode.Active, commander, reason, true, true);
            AIGMExecutionLog.Write(
                "AIGM_OPERATIONAL_RELEASE mobile={0} commander={1} epoch={2} reason={3}",
                Describe(mobile),
                Describe(commander),
                GetEpoch(mobile),
                Safe(reason));
        }

        public static void ClearAllOperationalState(Mobile mobile, AIGMOperationalStopMode stopMode, Mobile commander, string reason)
        {
            if (mobile == null || mobile.Deleted)
                return;

            AIGMOperationalMode mode = ToOperationalMode(stopMode);
            bool allowSelfDefense = stopMode != AIGMOperationalStopMode.AbsoluteGMHold;
            SetMode(mobile, mode, commander, reason, allowSelfDefense, true);

            BaseCreature creature = mobile as BaseCreature;
            IAIGMRosterTaskAgent rosterAgent = mobile as IAIGMRosterTaskAgent;
            if (rosterAgent != null && creature != null)
                ClearRosterState(rosterAgent, creature, stopMode);

            BaseHire hire = mobile as BaseHire;
            if (hire != null)
                ClearOldCompanionState(hire, commander, stopMode, reason);

            if (creature != null)
                ClearCombatAndMovement(creature, stopMode);

            AIGMExecutionLog.Write(
                "AIGM_OPERATIONAL_CLEAR mobile={0} mode={1} stopMode={2} commander={3} epoch={4} reason={5}",
                Describe(mobile),
                GetMode(mobile),
                stopMode,
                Describe(commander),
                GetEpoch(mobile),
                Safe(reason));
        }

        public static bool CanOperate(Mobile mobile, AIGMOperationalAction action, int expectedEpoch, bool directSelfDefense)
        {
            if (mobile == null || mobile.Deleted)
                return false;

            BaseCreature creature = mobile as BaseCreature;
            if (creature != null && !creature.Alive)
                return false;

            int currentEpoch = GetEpoch(mobile);
            if (expectedEpoch >= 0 && expectedEpoch != currentEpoch)
            {
                LogCallbackReject(mobile, action, expectedEpoch, currentEpoch, "epoch_mismatch");
                return false;
            }

            AIGMOperationalMode mode = GetMode(mobile);
            if (mode == AIGMOperationalMode.Active)
                return true;

            bool allowed = false;
            if (action == AIGMOperationalAction.ExplicitCommand)
            {
                allowed = mode != AIGMOperationalMode.AbsoluteGMHold;
            }
            else if (action == AIGMOperationalAction.DirectSelfDefense && directSelfDefense)
            {
                allowed = mode != AIGMOperationalMode.AbsoluteGMHold && AllowsSelfDefense(mobile);
            }

            if (!allowed)
                LogCallbackReject(mobile, action, expectedEpoch, currentEpoch, "mode_" + mode.ToString().ToLowerInvariant());

            return allowed;
        }

        public static bool IsDirectSelfDefense(Mobile mobile)
        {
            BaseCreature creature = mobile as BaseCreature;
            Mobile combatant = creature != null ? creature.Combatant as Mobile : null;
            return creature != null
                && combatant != null
                && !combatant.Deleted
                && combatant.Alive
                && combatant.Combatant == creature;
        }

        public static void NoteDeleteOrDeath(Mobile mobile, string reason)
        {
            if (mobile == null)
                return;

            SetMode(mobile, AIGMOperationalMode.CancelMission, null, reason, false, true);
        }

        private static void ClearRosterState(IAIGMRosterTaskAgent agent, BaseCreature creature, AIGMOperationalStopMode stopMode)
        {
            AIGMRosterTaskState state = AIGMRosterTaskService.GetOrCreateState(agent);
            if (state == null)
                return;

            state.ClearTask(true);
            state.TaskStatus = stopMode == AIGMOperationalStopMode.CancelMission
                ? "cancelled"
                : (stopMode == AIGMOperationalStopMode.AbsoluteGMHold ? "absolute_hold" : "standing_down");
            state.Awareness = AIGMRosterThreatAwarenessLevel.Unaware;
            state.LastThreatResponse = AIGMRosterThreatResponse.None;
            state.LastKnownTargetLocation = Point3D.Zero;
            state.LastKnownTargetMap = null;
            state.TargetSerial = Serial.MinusOne;
            state.TargetCanonicalId = String.Empty;
            state.TrailConfidence = 0.0;
            AIGMRosterTaskService.ClearImmediateCombatState(creature, stopMode == AIGMOperationalStopMode.AbsoluteGMHold);
            if (stopMode != AIGMOperationalStopMode.CancelMission)
                creature.FightMode = FightMode.None;
            else
                AIGMRosterTaskService.RestoreIdleFightMode(agent, creature);
        }

        private static void ClearOldCompanionState(BaseHire hire, Mobile commander, AIGMOperationalStopMode stopMode, string reason)
        {
            string ignored;
            try { AIGMLegacyTravelService.Stop(hire, reason, out ignored); } catch { }
            try { AIGMNativeNavigationService.Stop(hire, reason, out ignored); } catch { }
            try { AIGMSmartMovementService.Stop(hire, reason, out ignored); } catch { }
            try { AIGMCompanionLocateService.StopLocate(hire, reason); } catch { }
            try { AIGMCompanionTrackingService.StopTracking(hire, commander ?? hire.GetOwner(), false); } catch { }
            try { AIGMCompanionExecutionSpine.StopMonsterHunt(hire, reason); } catch { }

            Mobile requester = commander ?? hire.GetOwner();
            ReleaseLease(hire, AIGMMovementLeaseOwnerType.Travel, requester, reason);
            ReleaseLease(hire, AIGMMovementLeaseOwnerType.Hunt, requester, reason);
            ReleaseLease(hire, AIGMMovementLeaseOwnerType.Follow, requester, reason);
            ReleaseLease(hire, AIGMMovementLeaseOwnerType.Stop, requester, reason);

            hire.ControlTarget = null;
            hire.ControlOrder = OrderType.Stay;
            hire.CantWalk = false;
            hire.Home = hire.Location;
            hire.RangeHome = stopMode == AIGMOperationalStopMode.CancelMission ? 2 : 0;
            hire.FightMode = stopMode == AIGMOperationalStopMode.CancelMission ? FightMode.Aggressor : FightMode.None;
            AIGMCompanionModeService.SetMode(hire, AIGMCompanionMode.Stopped, reason);
        }

        private static void ClearCombatAndMovement(BaseCreature creature, AIGMOperationalStopMode stopMode)
        {
            creature.Combatant = null;
            creature.FocusMob = null;
            creature.Warmode = false;

            if (stopMode == AIGMOperationalStopMode.AbsoluteGMHold)
            {
                creature.Aggressors.Clear();
                creature.Aggressed.Clear();
            }
        }

        private static void ReleaseLease(BaseHire hire, AIGMMovementLeaseOwnerType ownerType, Mobile requester, string reason)
        {
            AIGMMovementOwnershipService.ReleaseIfOwned(
                hire,
                ownerType,
                requester,
                null,
                reason,
                AIGMMovementLeaseTerminalStatus.InterruptedStop);
        }

        private static void SetMode(Mobile mobile, AIGMOperationalMode mode, Mobile commander, string commandName, bool allowSelfDefense, bool incrementEpoch)
        {
            AIGMRosterTaskState rosterState = GetRosterState(mobile, false);
            if (rosterState != null)
            {
                if (incrementEpoch)
                    rosterState.ControlEpoch++;
                rosterState.OperationalMode = mode;
                rosterState.LastOperationalCommand = String.IsNullOrWhiteSpace(commandName) ? mode.ToString() : commandName.Trim();
                rosterState.LastOperationalCommandTime = DateTime.UtcNow;
                rosterState.StandDownCommanderSerial = commander != null ? commander.Serial : Serial.MinusOne;
                rosterState.AllowSelfDefenseWhileStandingDown = allowSelfDefense;
            }
            else
            {
                AIGMOperationalRegistryState state = GetRegistryState(mobile, true);
                if (incrementEpoch)
                    state.ControlEpoch++;
                state.OperationalMode = mode;
                state.LastOperationalCommand = String.IsNullOrWhiteSpace(commandName) ? mode.ToString() : commandName.Trim();
                state.LastOperationalCommandTime = DateTime.UtcNow;
                state.StandDownCommanderSerial = commander != null ? commander.Serial : Serial.MinusOne;
                state.AllowSelfDefenseWhileStandingDown = allowSelfDefense;
            }

            AIGMExecutionLog.Write(
                "AIGM_OPERATIONAL_MODE mobile={0} mode={1} commander={2} epoch={3} allowSelfDefense={4} command={5}",
                Describe(mobile),
                mode,
                Describe(commander),
                GetEpoch(mobile),
                allowSelfDefense,
                Safe(commandName));
            AIGMExecutionLog.Write(
                "AIGM_OPERATIONAL_EPOCH mobile={0} epoch={1} command={2}",
                Describe(mobile),
                GetEpoch(mobile),
                Safe(commandName));
        }

        private static AIGMOperationalMode ToOperationalMode(AIGMOperationalStopMode stopMode)
        {
            switch (stopMode)
            {
                case AIGMOperationalStopMode.AbsoluteGMHold:
                    return AIGMOperationalMode.AbsoluteGMHold;
                case AIGMOperationalStopMode.PassiveStandDown:
                    return AIGMOperationalMode.PassiveStandDown;
                default:
                    return AIGMOperationalMode.CancelMission;
            }
        }

        private static AIGMRosterTaskState GetRosterState(Mobile mobile, bool create)
        {
            IAIGMRosterTaskAgent agent = mobile as IAIGMRosterTaskAgent;
            if (agent == null)
                return null;

            return create ? AIGMRosterTaskService.GetOrCreateState(agent) : agent.RosterTaskState;
        }

        private static AIGMOperationalRegistryState GetRegistryState(Mobile mobile, bool create)
        {
            if (mobile == null)
                return new AIGMOperationalRegistryState();

            AIGMOperationalRegistryState state;
            if (!Registry.TryGetValue(mobile.Serial, out state) && create)
            {
                state = new AIGMOperationalRegistryState();
                Registry[mobile.Serial] = state;
            }

            return state ?? new AIGMOperationalRegistryState();
        }

        private static bool AllowsSelfDefense(Mobile mobile)
        {
            AIGMRosterTaskState rosterState = GetRosterState(mobile, false);
            if (rosterState != null)
                return rosterState.AllowSelfDefenseWhileStandingDown;

            return GetRegistryState(mobile, false).AllowSelfDefenseWhileStandingDown;
        }

        private static string GetLastCommand(Mobile mobile)
        {
            AIGMRosterTaskState rosterState = GetRosterState(mobile, false);
            if (rosterState != null)
                return rosterState.LastOperationalCommand;

            return GetRegistryState(mobile, false).LastOperationalCommand;
        }

        private static void LogCallbackReject(Mobile mobile, AIGMOperationalAction action, int expectedEpoch, int currentEpoch, string reason)
        {
            string key = mobile != null ? mobile.Serial.Value + ":" + action + ":" + reason : "none:" + action + ":" + reason;
            DateTime now = DateTime.UtcNow;
            DateTime last;
            if (LastRejectLogUtc.TryGetValue(key, out last) && (now - last) < RejectLogThrottle && reason != "epoch_mismatch")
                return;

            LastRejectLogUtc[key] = now;
            AIGMExecutionLog.Write(
                "AIGM_OPERATIONAL_CALLBACK_REJECT mobile={0} action={1} expectedEpoch={2} currentEpoch={3} mode={4} reason={5}",
                Describe(mobile),
                action,
                expectedEpoch,
                currentEpoch,
                GetMode(mobile),
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
    }
}
