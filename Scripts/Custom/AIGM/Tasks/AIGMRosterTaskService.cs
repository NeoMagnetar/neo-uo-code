using System;
using System.Collections.Generic;

using Server;
using Server.Custom.AIGM;
using Server.Custom.AIGM.Characters.Waylander;
using Server.Mobiles;

namespace Server.Custom.AIGM.Tasks
{
    public static class AIGMRosterTaskService
    {
        private static readonly TimeSpan PulseThrottle = TimeSpan.FromMilliseconds(500.0);

        public static AIGMRosterTaskState GetOrCreateState(IAIGMRosterTaskAgent agent)
        {
            if (agent == null)
                return null;

            BaseCreature creature = agent as BaseCreature;
            if (creature == null)
                return null;

            if (agent.RosterTaskState == null)
            {
                agent.RosterTaskState = new AIGMRosterTaskState
                {
                    AgentSerial = creature.Serial,
                    TrustedCommanderSerial = agent.RosterTrustedCommanderSerial,
                    PassiveTaskMode = agent.RosterPassiveTestMode,
                    IsAutonomous = !agent.RosterBoundCompanion,
                    HomeMap = creature.Map,
                    HomeLocation = creature.Home != Point3D.Zero ? creature.Home : creature.Location
                };
            }

            AIGMRosterTaskState state = agent.RosterTaskState;
            if (state.AgentSerial == Serial.MinusOne)
                state.AgentSerial = creature.Serial;

            if (state.HomeLocation == Point3D.Zero)
                state.HomeLocation = creature.Home != Point3D.Zero ? creature.Home : creature.Location;

            if (state.HomeMap == null || state.HomeMap == Map.Internal)
                state.HomeMap = creature.Map;

            state.TrustedCommanderSerial = agent.RosterTrustedCommanderSerial;
            state.PassiveTaskMode = agent.RosterPassiveTestMode;
            state.IsAutonomous = !agent.RosterBoundCompanion;
            return state;
        }

        public static IEnumerable<IAIGMRosterTaskAgent> EnumerateAgents(Map map, Point3D center, int range)
        {
            foreach (Mobile mobile in World.Mobiles.Values)
            {
                if (!(mobile is IAIGMRosterTaskAgent agent) || !(mobile is BaseCreature creature))
                    continue;

                if (creature.Deleted || !creature.Alive)
                    continue;

                if (map != null && creature.Map != map)
                    continue;

                if (range > 0 && !creature.InRange(center, range))
                    continue;

                yield return agent;
            }
        }

        public static bool AssignTask(
            IAIGMRosterTaskAgent agent,
            Mobile commander,
            AIGMRosterTaskMode mode,
            AIGMRosterTargetSelector selector,
            Serial targetSerial,
            string targetCanonicalId,
            AIGMRosterFaction targetFaction,
            Point3D assignedLocation,
            Map assignedMap,
            int assignedRange,
            string groupId,
            bool persists,
            out string response)
        {
            response = "Task assignment failed.";

            if (agent == null)
            {
                response = "No valid roster agent was found.";
                return false;
            }

            BaseCreature creature = agent as BaseCreature;
            if (creature == null || creature.Deleted)
            {
                response = "The target agent is invalid.";
                return false;
            }

            string rejection;
            if (!AIGMOperationalControlService.BeginExplicitCommand(creature, commander, "task:" + mode, out rejection))
            {
                response = "Operational control rejected the task: " + rejection + ".";
                return false;
            }

            AIGMRosterTaskState state = GetOrCreateState(agent);
            state.Mode = mode;
            state.Selector = selector;
            state.TargetSerial = targetSerial;
            state.TargetCanonicalId = String.IsNullOrWhiteSpace(targetCanonicalId) ? String.Empty : targetCanonicalId.Trim();
            state.TargetFaction = targetFaction;
            state.AssignedLocation = assignedLocation;
            state.AssignedMap = assignedMap ?? creature.Map;
            state.AssignedRange = assignedRange;
            state.TaskStartUtc = DateTime.UtcNow;
            state.TaskStatus = "assigned";
            state.Persists = persists;
            state.GroupId = groupId ?? String.Empty;
            state.LastSeenUtc = DateTime.MinValue;
            state.LastKnownTargetMap = null;
            state.LastKnownTargetLocation = Point3D.Zero;
            state.TrailConfidence = 0.0;
            state.CurrentCombatInterruptionSerial = Serial.MinusOne;
            state.NativeCombatTargetSerial = Serial.MinusOne;
            state.NativeCombatMissionTargetSerial = Serial.MinusOne;
            state.NativeCombatStartUtc = DateTime.MinValue;
            state.NativeCombatSource = String.Empty;
            state.NativeCombatAlreadyActiveLogged = false;
            state.FailureCount = 0;
            state.ReacquireCount = 0;
            state.TrustedCommanderSerial = commander != null ? commander.Serial : agent.RosterTrustedCommanderSerial;
            state.PassiveTaskMode = agent.RosterPassiveTestMode;
            state.IsAutonomous = !agent.RosterBoundCompanion;

            if (state.HomeLocation == Point3D.Zero)
            {
                state.HomeLocation = creature.Home != Point3D.Zero ? creature.Home : creature.Location;
                state.HomeMap = creature.Map;
            }

            if ((mode == AIGMRosterTaskMode.Patrol || mode == AIGMRosterTaskMode.GuardArea) && assignedLocation != Point3D.Zero)
            {
                creature.Home = assignedLocation;
                creature.RangeHome = Math.Max(assignedRange, 6);
            }

            ClearImmediateCombatState(creature, false);

            AIGMExecutionLog.Write(
                "AIGM_TASK_ASSIGNED agent={0} commander={1} mode={2} selector={3} targetId={4} targetSerial={5} targetFaction={6} area={7} range={8} group={9} persistent={10}",
                Describe(creature),
                Describe(commander),
                mode,
                selector,
                Safe(state.TargetCanonicalId),
                targetSerial,
                targetFaction,
                FormatPoint(assignedLocation),
                assignedRange,
                Safe(groupId),
                persists);

            response = BuildAssignmentResponse(creature, mode, selector, state.TargetCanonicalId, targetFaction);
            return true;
        }

        public static void ClearTask(IAIGMRosterTaskAgent agent, string reason)
        {
            BaseCreature creature = agent as BaseCreature;
            AIGMRosterTaskState state = GetOrCreateState(agent);
            if (creature == null || state == null)
                return;

            AIGMOperationalControlService.ClearAllOperationalState(creature, AIGMOperationalStopMode.CancelMission, null, reason);

            AIGMExecutionLog.Write("AIGM_TASK_CLEARED agent={0} reason={1}", Describe(creature), Safe(reason));
        }

        public static void ReturnHome(IAIGMRosterTaskAgent agent, string reason)
        {
            BaseCreature creature = agent as BaseCreature;
            AIGMRosterTaskState state = GetOrCreateState(agent);
            if (creature == null || state == null)
                return;

            string rejection;
            if (!AIGMOperationalControlService.BeginExplicitCommand(creature, null, "return_home", out rejection))
                return;

            state.Mode = AIGMRosterTaskMode.ReturnHome;
            state.TaskStatus = "returning_home";
            state.AssignedLocation = state.HomeLocation;
            state.AssignedMap = state.HomeMap ?? creature.Map;
            state.AssignedRange = 2;
            ClearImmediateCombatState(creature, false);
            AIGMExecutionLog.Write("AIGM_TASK_RETURN_HOME agent={0} reason={1} home={2}", Describe(creature), Safe(reason), FormatPoint(state.HomeLocation));
        }

        public static string BuildStatus(IAIGMRosterTaskAgent agent)
        {
            BaseCreature creature = agent as BaseCreature;
            AIGMRosterTaskState state = GetOrCreateState(agent);
            if (creature == null || state == null)
                return "No valid task state.";

            string target = !String.IsNullOrWhiteSpace(state.TargetCanonicalId)
                ? state.TargetCanonicalId
                : (state.TargetSerial != Serial.MinusOne ? state.TargetSerial.ToString() : "none");

            string lastKnown = state.LastKnownTargetMap == null || state.LastKnownTargetLocation == Point3D.Zero
                ? "none"
                : String.Format("{0} ({1})", state.LastKnownTargetMap.Name, FormatPoint(state.LastKnownTargetLocation));

            string interruption = state.CurrentCombatInterruptionSerial != Serial.MinusOne
                ? Describe(World.FindMobile(state.CurrentCombatInterruptionSerial))
                : "none";

            return String.Format(
                "Name: {0}; {1}; Mode: {2}; Target: {3}; Target status: {4}; Last known: {5}; Trail confidence: {6:0}%; Squad: {7}; Current interruption: {8}; Home: {9}; Persistent: {10}; Operational: {11}",
                creature.Name,
                AIGMRosterCompanionBindingService.BuildAgentControlStatus(agent),
                state.Mode,
                target,
                state.TaskStatus,
                lastKnown,
                state.TrailConfidence,
                String.IsNullOrWhiteSpace(state.GroupId) ? "none" : state.GroupId,
                interruption,
                FormatPoint(state.HomeLocation),
                state.Persists ? "yes" : "no",
                AIGMOperationalControlService.DescribeState(creature));
        }

        public static void Pulse(IAIGMRosterTaskAgent agent)
        {
            BaseCreature creature = agent as BaseCreature;
            AIGMRosterTaskState state = GetOrCreateState(agent);
            if (creature == null || state == null || creature.Deleted || !creature.Alive || creature.Map == null || creature.Map == Map.Internal)
                return;

            int expectedEpoch = state.ControlEpoch;
            if (!AIGMOperationalControlService.CanOperate(creature, AIGMOperationalAction.TaskPulse, expectedEpoch, false))
            {
                if (!AIGMOperationalControlService.CanOperate(creature, AIGMOperationalAction.DirectSelfDefense, expectedEpoch, AIGMOperationalControlService.IsDirectSelfDefense(creature)))
                    ClearImmediateCombatState(creature, AIGMOperationalControlService.GetMode(creature) == AIGMOperationalMode.AbsoluteGMHold);
                return;
            }

            DateTime now = DateTime.UtcNow;
            if (state.LastPulseUtc != DateTime.MinValue && (now - state.LastPulseUtc) < PulseThrottle)
                return;

            state.LastPulseUtc = now;

            if (state.PassiveTaskMode)
                ClearImmediateCombatState(creature, false);

            if (AIGMRosterFactionService.IsHero(agent.RosterCharacterId))
                AIGMRosterThreatAwarenessService.Pulse(agent, creature, state);

            HandleCombatInterruption(agent, creature, state);

            switch (state.Mode)
            {
                case AIGMRosterTaskMode.None:
                case AIGMRosterTaskMode.Stay:
                    if (state.Mode == AIGMRosterTaskMode.Stay)
                        HoldPosition(agent, creature);
                    break;
                case AIGMRosterTaskMode.FollowOwner:
                    PulseFollow(agent, creature, state);
                    break;
                case AIGMRosterTaskMode.GuardOwner:
                    PulseGuardOwner(agent, creature, state);
                    break;
                case AIGMRosterTaskMode.GuardMobile:
                    PulseGuardMobile(agent, creature, state);
                    break;
                case AIGMRosterTaskMode.GuardArea:
                    PulseGuardArea(agent, creature, state);
                    break;
                case AIGMRosterTaskMode.Patrol:
                    PulsePatrol(agent, creature, state);
                    break;
                case AIGMRosterTaskMode.Track:
                    PulseTrackOrHunt(agent, creature, state, false);
                    break;
                case AIGMRosterTaskMode.Hunt:
                    PulseTrackOrHunt(agent, creature, state, true);
                    break;
                case AIGMRosterTaskMode.ReturnHome:
                    PulseReturnHome(agent, creature, state);
                    break;
                case AIGMRosterTaskMode.Travel:
                    PulseMoveTask(agent, creature, state);
                    break;
                case AIGMRosterTaskMode.Flee:
                case AIGMRosterTaskMode.Hide:
                case AIGMRosterTaskMode.Regroup:
                    PulseMoveTask(agent, creature, state);
                    break;
                case AIGMRosterTaskMode.Escort:
                    PulseGuardMobile(agent, creature, state);
                    break;
            }
        }

        internal static Mobile FindAssignedTarget(IAIGMRosterTaskAgent agent, BaseCreature creature, AIGMRosterTaskState state, bool localOnly)
        {
            if (state == null || creature == null)
                return null;

            if (!AIGMOperationalControlService.CanOperate(creature, AIGMOperationalAction.TargetAcquisition, state.ControlEpoch, false))
                return null;

            Mobile target = ResolveNamedTarget(state);
            if (target != null && IsEligibleTaskTarget(agent, creature, state, target))
                return target;

            int radius = GetSearchRadius(creature, state);
            foreach (Mobile mobile in EnumerateNearbyMobiles(creature, radius))
            {
                if (IsEligibleTaskTarget(agent, creature, state, mobile))
                    return mobile;
            }

            if (localOnly)
                return null;

            foreach (Mobile mobile in World.Mobiles.Values)
            {
                if (IsEligibleTaskTarget(agent, creature, state, mobile))
                    return mobile;
            }

            return null;
        }

        internal static IEnumerable<IAIGMRosterTaskAgent> GetNearbyAllies(IAIGMRosterTaskAgent agent, BaseCreature creature, int range)
        {
            AIGMRosterFaction faction = AIGMRosterFactionService.ResolveFaction(agent.RosterCharacterId);
            foreach (IAIGMRosterTaskAgent candidate in EnumerateAgents(creature.Map, creature.Location, range))
            {
                if (ReferenceEquals(candidate, agent))
                    continue;

                AIGMRosterFaction otherFaction = AIGMRosterFactionService.ResolveFaction(candidate.RosterCharacterId);
                if (otherFaction == faction || AIGMRosterFactionService.AreAllied(faction, otherFaction))
                    yield return candidate;
            }
        }

        internal static Point3D FindRetreatPoint(BaseCreature creature, Mobile threat, int distance)
        {
            if (creature == null || creature.Map == null || threat == null)
                return creature != null ? creature.Location : Point3D.Zero;

            int dx = Math.Sign(creature.X - threat.X);
            int dy = Math.Sign(creature.Y - threat.Y);

            Point3D[] candidates =
            {
                new Point3D(creature.X + (dx * distance), creature.Y + (dy * distance), creature.Z),
                new Point3D(creature.X + (dx * distance), creature.Y, creature.Z),
                new Point3D(creature.X, creature.Y + (dy * distance), creature.Z),
                new Point3D(creature.X + (dx * (distance / 2)), creature.Y + (dy * (distance / 2)), creature.Z)
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                if (creature.Map.CanFit(candidates[i].X, candidates[i].Y, candidates[i].Z, 16, false, false))
                    return candidates[i];
            }

            return creature.Location;
        }

        internal static void EngageTarget(IAIGMRosterTaskAgent agent, BaseCreature creature, AIGMRosterTaskState state, Mobile target, string status)
        {
            if (creature == null || target == null || target.Deleted || !target.Alive || state == null)
                return;

            if (!AIGMOperationalControlService.CanOperate(creature, AIGMOperationalAction.CombatAssignment, state != null ? state.ControlEpoch : -1, false))
                return;

            state.TaskStatus = status;
            state.TargetSerial = target.Serial;
            state.LastKnownTargetLocation = target.Location;
            state.LastKnownTargetMap = target.Map;
            state.LastSeenUtc = DateTime.UtcNow;
            state.TrailConfidence = 100.0;

            if (TrySustainNativeCombat(agent, creature, state, status))
                return;

            if (state.NativeCombatTargetSerial != Serial.MinusOne && state.NativeCombatTargetSerial != target.Serial)
                ClearNativeCombatState(creature, state, "target_changed", true);

            string detail;
            Mobile commander = ResolveCommander(agent, state);
            AIGMNativeCombatBridgeResult result = AIGMNativeCombatBridge.TryStartNativeCombat(
                creature,
                target,
                commander,
                state.ControlEpoch,
                "roster_task:" + state.Mode,
                out detail);

            if (result != AIGMNativeCombatBridgeResult.Started && result != AIGMNativeCombatBridgeResult.AlreadyEngaged)
            {
                state.TaskStatus = "combat_rejected_" + result.ToString().ToLowerInvariant();
                return;
            }

            MarkNativeCombatState(state, target, "roster_task:" + state.Mode);

            AIGMExecutionLog.Write("AIGM_TASK_TARGET_ACQUIRED agent={0} target={1} mode={2} selector={3}", Describe(creature), Describe(target), state.Mode, state.Selector);
            ShareTargetWithNearbySquad(agent, creature, state, target);
        }

        internal static void MoveToward(BaseCreature creature, IPoint3D point, bool run, int range, AIGMOperationalAction action = AIGMOperationalAction.Movement, int expectedEpoch = -1)
        {
            if (creature == null || point == null || creature.AIObject == null)
                return;

            if (!AIGMOperationalControlService.CanOperate(creature, action, expectedEpoch, false))
                return;

            creature.AIObject.MoveTo(point, run, range);
        }

        internal static void ClearImmediateCombatState(BaseCreature creature, bool clearAggressionLists)
        {
            if (creature == null)
                return;

            IAIGMRosterTaskAgent agent = creature as IAIGMRosterTaskAgent;
            AIGMRosterTaskState state = agent != null ? agent.RosterTaskState : null;
            ClearNativeCombatState(creature, state, "clear_immediate", false);

            creature.Combatant = null;
            creature.FocusMob = null;
            creature.Warmode = false;

            if (creature.ControlOrder == OrderType.Attack)
            {
                creature.ControlTarget = null;
                creature.ControlOrder = OrderType.None;
            }

            if (clearAggressionLists)
            {
                creature.Aggressors.Clear();
                creature.Aggressed.Clear();
            }
        }

        private static bool TrySustainNativeCombat(IAIGMRosterTaskAgent agent, BaseCreature creature, AIGMRosterTaskState state, string status)
        {
            if (creature == null || state == null || state.NativeCombatTargetSerial == Serial.MinusOne)
                return false;

            Mobile nativeTarget = World.FindMobile(state.NativeCombatTargetSerial);
            Mobile currentCombatant = creature.Combatant as Mobile;

            if (nativeTarget == null || nativeTarget.Deleted || !nativeTarget.Alive || nativeTarget.Map != creature.Map)
            {
                ClearNativeCombatState(creature, state, "target_invalid_or_dead", true);
                return false;
            }

            if (currentCombatant != null
                && !currentCombatant.Deleted
                && currentCombatant.Alive
                && currentCombatant.Map == creature.Map
                && currentCombatant.Serial == state.CurrentCombatInterruptionSerial
                && currentCombatant.Serial != state.NativeCombatTargetSerial)
            {
                state.TaskStatus = "native_combat_interrupted";
                return true;
            }

            if (!AIGMNativeCombatBridge.IsNativeCombatActive(creature, nativeTarget))
            {
                ClearNativeCombatState(creature, state, "native_state_lost", false);
                return false;
            }

            state.TaskStatus = status;
            state.LastKnownTargetLocation = nativeTarget.Location;
            state.LastKnownTargetMap = nativeTarget.Map;
            state.LastSeenUtc = DateTime.UtcNow;

            if (!state.NativeCombatAlreadyActiveLogged)
            {
                AIGMNativeCombatBridge.LogAlreadyActive(creature, nativeTarget, state.NativeCombatSource);
                state.NativeCombatAlreadyActiveLogged = true;
            }

            return true;
        }

        private static void MarkNativeCombatState(AIGMRosterTaskState state, Mobile target, string source)
        {
            if (state == null || target == null)
                return;

            state.NativeCombatTargetSerial = target.Serial;
            state.NativeCombatMissionTargetSerial = state.TargetSerial;
            state.NativeCombatStartUtc = DateTime.UtcNow;
            state.NativeCombatSource = source ?? String.Empty;
            state.NativeCombatAlreadyActiveLogged = false;
        }

        private static void ClearNativeCombatState(BaseCreature creature, AIGMRosterTaskState state, string reason, bool clearMobileState)
        {
            if (state == null || state.NativeCombatTargetSerial == Serial.MinusOne)
                return;

            Mobile target = World.FindMobile(state.NativeCombatTargetSerial);
            AIGMNativeCombatBridge.EndNativeCombat(creature, target, reason, clearMobileState);
            AIGMExecutionLog.Write("AIGM_NATIVE_COMBAT_TASK_RESUME agent={0} missionTarget={1} endedTarget={2} reason={3}", Describe(creature), state.NativeCombatMissionTargetSerial, state.NativeCombatTargetSerial, Safe(reason));

            state.NativeCombatTargetSerial = Serial.MinusOne;
            state.NativeCombatMissionTargetSerial = Serial.MinusOne;
            state.NativeCombatStartUtc = DateTime.MinValue;
            state.NativeCombatSource = String.Empty;
            state.NativeCombatAlreadyActiveLogged = false;
        }

        internal static void RestoreIdleFightMode(IAIGMRosterTaskAgent agent, BaseCreature creature)
        {
            if (agent == null || creature == null)
                return;

            if (creature is WaylanderRosterMobile rosterMobile && rosterMobile.Definition != null)
            {
                creature.FightMode = rosterMobile.Definition.FightMode;
                return;
            }

            creature.FightMode = FightMode.Aggressor;
        }

        private static void PulseFollow(IAIGMRosterTaskAgent agent, BaseCreature creature, AIGMRosterTaskState state)
        {
            Mobile commander = ResolveCommander(agent, state);
            if (commander == null || commander.Map != creature.Map)
            {
                state.TaskStatus = "commander_missing";
                return;
            }

            state.TaskStatus = "following";
            MoveToward(creature, commander, true, 3);
        }

        private static void PulseGuardOwner(IAIGMRosterTaskAgent agent, BaseCreature creature, AIGMRosterTaskState state)
        {
            if (TrySustainNativeCombat(agent, creature, state, "guarding_owner"))
                return;

            Mobile commander = ResolveCommander(agent, state);
            if (commander == null || commander.Map != creature.Map)
            {
                state.TaskStatus = "commander_missing";
                return;
            }

            if (commander.Combatant is Mobile threat && IsEligibleTaskTarget(agent, creature, state, threat))
            {
                EngageTarget(agent, creature, state, threat, "guarding_owner");
                return;
            }

            state.TaskStatus = "guarding_owner";
            MoveToward(creature, commander, false, 1);
        }

        private static void PulseGuardMobile(IAIGMRosterTaskAgent agent, BaseCreature creature, AIGMRosterTaskState state)
        {
            if (TrySustainNativeCombat(agent, creature, state, "guarding_mobile"))
                return;

            Mobile guarded = ResolveNamedTarget(state);
            if (guarded == null || guarded.Deleted || !guarded.Alive || guarded.Map != creature.Map)
            {
                state.TaskStatus = "guard_target_missing";
                return;
            }

            if (guarded.Combatant is Mobile threat && IsEligibleTaskTarget(agent, creature, state, threat))
            {
                EngageTarget(agent, creature, state, threat, "guarding_mobile");
                return;
            }

            state.TaskStatus = "guarding_mobile";
            MoveToward(creature, guarded, false, 1);
        }

        private static void PulseGuardArea(IAIGMRosterTaskAgent agent, BaseCreature creature, AIGMRosterTaskState state)
        {
            if (TrySustainNativeCombat(agent, creature, state, "guarding_area"))
                return;

            Mobile target = FindAssignedTarget(agent, creature, state, true);
            if (target != null)
            {
                EngageTarget(agent, creature, state, target, "guarding_area");
                return;
            }

            state.TaskStatus = "guarding_area";
            Point3D home = state.AssignedLocation != Point3D.Zero ? state.AssignedLocation : state.HomeLocation;
            if (!creature.InRange(home, Math.Max(state.AssignedRange, 3)))
                MoveToward(creature, home, false, 1);
        }

        private static void PulsePatrol(IAIGMRosterTaskAgent agent, BaseCreature creature, AIGMRosterTaskState state)
        {
            if (TrySustainNativeCombat(agent, creature, state, "patrol_contact"))
                return;

            Mobile target = FindAssignedTarget(agent, creature, state, true);
            if (target != null)
            {
                EngageTarget(agent, creature, state, target, "patrol_contact");
                return;
            }

            if (state.PatrolPoints.Count == 0)
            {
                Point3D center = state.AssignedLocation != Point3D.Zero ? state.AssignedLocation : creature.Location;
                state.PatrolPoints.Add(center);
                state.PatrolPoints.Add(new Point3D(center.X + 3, center.Y, center.Z));
                state.PatrolPoints.Add(new Point3D(center.X, center.Y + 3, center.Z));
            }

            Point3D current = state.PatrolPoints[Math.Max(0, Math.Min(state.PatrolIndex, state.PatrolPoints.Count - 1))];
            state.TaskStatus = "patrolling";
            if (creature.InRange(current, 1))
                state.PatrolIndex = (state.PatrolIndex + 1) % state.PatrolPoints.Count;
            else
                MoveToward(creature, current, false, 1);
        }

        private static void PulseTrackOrHunt(IAIGMRosterTaskAgent agent, BaseCreature creature, AIGMRosterTaskState state, bool engage)
        {
            if (engage && TrySustainNativeCombat(agent, creature, state, "engaging_target"))
                return;

            Mobile target = FindAssignedTarget(agent, creature, state, false);
            if (target != null)
            {
                state.TargetSerial = target.Serial;
                state.LastKnownTargetLocation = target.Location;
                state.LastKnownTargetMap = target.Map;
                state.LastSeenUtc = DateTime.UtcNow;
                state.TrailConfidence = Math.Min(100.0, 40.0 + (creature.Skills[SkillName.Tracking].Value * 0.6));
                state.TaskStatus = engage ? "target_visible" : "trail_acquired";
                AIGMExecutionLog.Write("AIGM_TRACK_TRAIL_UPDATE agent={0} target={1} confidence={2:0}", Describe(creature), Describe(target), state.TrailConfidence);

                if (engage)
                {
                    EngageTarget(agent, creature, state, target, "engaging_target");
                    return;
                }

                MoveToward(creature, target, false, 2);
                return;
            }

            if (state.LastKnownTargetMap == creature.Map && state.LastKnownTargetLocation != Point3D.Zero)
            {
                state.FailureCount++;
                state.TaskStatus = engage ? "searching_last_known" : "tracking_last_known";
                state.TrailConfidence = Math.Max(0.0, state.TrailConfidence - 8.0);
                MoveToward(creature, state.LastKnownTargetLocation, false, 1);
                if (creature.InRange(state.LastKnownTargetLocation, 2))
                {
                    state.ReacquireCount++;
                    AIGMExecutionLog.Write("AIGM_TASK_TARGET_LOST agent={0} mode={1} lastKnown={2}", Describe(creature), state.Mode, FormatPoint(state.LastKnownTargetLocation));
                }
            }
            else if (state.AssignedLocation != Point3D.Zero && state.AssignedMap == creature.Map)
            {
                state.TaskStatus = "moving_to_search_area";
                MoveToward(creature, state.AssignedLocation, false, 1);
            }
            else
            {
                state.TaskStatus = "awaiting_contact";
            }
        }

        private static void PulseReturnHome(IAIGMRosterTaskAgent agent, BaseCreature creature, AIGMRosterTaskState state)
        {
            if (state.HomeMap != null && state.HomeMap != creature.Map)
            {
                state.TaskStatus = "home_map_mismatch";
                return;
            }

            state.TaskStatus = "returning_home";
            if (creature.InRange(state.HomeLocation, 2))
            {
                state.Mode = AIGMRosterTaskMode.Stay;
                state.TaskStatus = "home";
                ClearImmediateCombatState(creature, false);
                return;
            }

            MoveToward(creature, state.HomeLocation, false, 1);
        }

        private static void PulseMoveTask(IAIGMRosterTaskAgent agent, BaseCreature creature, AIGMRosterTaskState state)
        {
            Point3D destination = state.AssignedLocation != Point3D.Zero ? state.AssignedLocation : state.HomeLocation;
            if (destination == Point3D.Zero)
                destination = creature.Location;

            if (creature.InRange(destination, 1))
            {
                state.Mode = AIGMRosterTaskMode.Stay;
                state.TaskStatus = "holding";
                return;
            }

            MoveToward(creature, destination, true, 1);
        }

        private static void HoldPosition(IAIGMRosterTaskAgent agent, BaseCreature creature)
        {
            creature.FightMode = FightMode.Aggressor;
            if (creature.Combatant == null)
                creature.Warmode = false;
        }

        private static void HandleCombatInterruption(IAIGMRosterTaskAgent agent, BaseCreature creature, AIGMRosterTaskState state)
        {
            Mobile combatant = creature.Combatant as Mobile;
            if (!AIGMOperationalControlService.CanOperate(creature, AIGMOperationalAction.CombatAssignment, state.ControlEpoch, AIGMOperationalControlService.IsDirectSelfDefense(creature)))
                return;

            if (combatant == null || combatant.Deleted || !combatant.Alive)
            {
                if (state.CurrentCombatInterruptionSerial != Serial.MinusOne)
                {
                    AIGMExecutionLog.Write("AIGM_HUNT_RESUME agent={0} interruptedBy={1}", Describe(creature), Describe(World.FindMobile(state.CurrentCombatInterruptionSerial)));
                    state.CurrentCombatInterruptionSerial = Serial.MinusOne;
                }
                return;
            }

            if (state.Mode != AIGMRosterTaskMode.Hunt)
                return;

            if (state.TargetSerial != Serial.MinusOne && combatant.Serial == state.TargetSerial)
                return;

            if (state.CurrentCombatInterruptionSerial != combatant.Serial)
            {
                state.CurrentCombatInterruptionSerial = combatant.Serial;
                AIGMExecutionLog.Write("AIGM_HUNT_COMBAT_INTERRUPT agent={0} interruption={1} assignedTarget={2}", Describe(creature), Describe(combatant), state.TargetSerial);
            }

            if (state.LastKnownTargetLocation != Point3D.Zero && combatant.GetDistanceToSqrt(state.LastKnownTargetLocation) > 18)
            {
                creature.Combatant = null;
                creature.FocusMob = null;
                creature.Warmode = false;
            }
        }

        private static Mobile ResolveNamedTarget(AIGMRosterTaskState state)
        {
            if (state == null)
                return null;

            if (state.TargetSerial != Serial.MinusOne)
            {
                Mobile target = World.FindMobile(state.TargetSerial);
                if (target != null && !target.Deleted)
                    return target;
            }

            if (String.IsNullOrWhiteSpace(state.TargetCanonicalId))
                return null;

            foreach (Mobile mobile in World.Mobiles.Values)
            {
                if (mobile is IAIGMRosterTaskAgent targetAgent
                    && String.Equals(targetAgent.RosterCharacterId, state.TargetCanonicalId, StringComparison.OrdinalIgnoreCase)
                    && !mobile.Deleted
                    && mobile.Alive)
                {
                    return mobile;
                }
            }

            return null;
        }

        private static IEnumerable<Mobile> EnumerateNearbyMobiles(BaseCreature creature, int radius)
        {
            IPooledEnumerable eable = creature.GetMobilesInRange(radius);
            try
            {
                foreach (Mobile mobile in eable)
                    yield return mobile;
            }
            finally
            {
                eable.Free();
            }
        }

        private static bool IsEligibleTaskTarget(IAIGMRosterTaskAgent agent, BaseCreature creature, AIGMRosterTaskState state, Mobile target)
        {
            if (agent == null || creature == null || state == null || target == null)
                return false;

            if (target == creature || target.Deleted || !target.Alive || target.IsStaff() || target.Blessed)
                return false;

            if (target is BaseGuard)
                return false;

            if (!creature.CanSee(target) && target.Map == creature.Map && target.GetDistanceToSqrt(creature) <= GetSearchRadius(creature, state) / 2)
            {
                // Tracking can still infer near hidden targets.
            }
            else if (target.Map != creature.Map)
            {
                return false;
            }

            if (target is BaseVendor || target is PlayerVendor)
                return false;

            Mobile commander = ResolveCommander(agent, state);
            if (commander != null && target == commander)
                return false;

            if (target is IAIGMRosterTaskAgent targetAgent)
            {
                if (agent.RosterCharacterId == targetAgent.RosterCharacterId)
                    return false;

                if (AIGMRosterFactionService.AreAllied(
                    AIGMRosterFactionService.ResolveFaction(agent.RosterCharacterId),
                    AIGMRosterFactionService.ResolveFaction(targetAgent.RosterCharacterId)))
                    return false;
            }

            switch (state.Selector)
            {
                case AIGMRosterTargetSelector.NamedCharacter:
                    return target is IAIGMRosterTaskAgent namedAgent
                        && String.Equals(namedAgent.RosterCharacterId, state.TargetCanonicalId, StringComparison.OrdinalIgnoreCase);
                case AIGMRosterTargetSelector.MobileSerial:
                    return target.Serial == state.TargetSerial;
                case AIGMRosterTargetSelector.Faction:
                    return AIGMRosterFactionService.ResolveFaction(target) == state.TargetFaction;
                case AIGMRosterTargetSelector.Heroes:
                    return AIGMRosterFactionService.IsHero(target);
                case AIGMRosterTargetSelector.Enemies:
                    return AIGMRosterFactionService.AreEnemies(
                            AIGMRosterFactionService.ResolveFaction(agent.RosterCharacterId),
                            AIGMRosterFactionService.ResolveFaction(target))
                        || AIGMRosterFactionService.HasExplicitEnemyRelationship(creature, target);
                case AIGMRosterTargetSelector.Monsters:
                    return target is BaseCreature
                        && !target.Player
                        && !(target is IAIGMRosterTaskAgent)
                        && creature.CanBeHarmful(target, false);
                case AIGMRosterTargetSelector.Reds:
                    return target.Kills >= 5 || target.Criminal;
                case AIGMRosterTargetSelector.Blues:
                    return target.Kills < 5 && !target.Criminal && target.AccessLevel == AccessLevel.Player;
                case AIGMRosterTargetSelector.AllHostiles:
                    return IsAllHostilesTarget(agent, creature, target);
                default:
                    return false;
            }
        }

        private static bool IsAllHostilesTarget(IAIGMRosterTaskAgent agent, BaseCreature creature, Mobile target)
        {
            if (target == null || target.Deleted || !target.Alive || target == creature)
                return false;

            if (!creature.CanBeHarmful(target, false))
                return false;

            if (target is IAIGMRosterTaskAgent targetAgent)
            {
                AIGMRosterFaction selfFaction = AIGMRosterFactionService.ResolveFaction(agent.RosterCharacterId);
                AIGMRosterFaction otherFaction = AIGMRosterFactionService.ResolveFaction(targetAgent.RosterCharacterId);
                return AIGMRosterFactionService.AreEnemies(selfFaction, otherFaction)
                    || AIGMRosterFactionService.HasExplicitEnemyRelationship(creature, target);
            }

            return target is BaseCreature && !target.Player;
        }

        private static int GetSearchRadius(BaseCreature creature, AIGMRosterTaskState state)
        {
            int trackingBonus = (int)(creature.Skills[SkillName.Tracking].Value / 6.0);
            int radius = 12 + trackingBonus;

            if (state.AssignedRange > 0)
                radius = Math.Max(radius, state.AssignedRange);

            return Math.Min(radius, 40);
        }

        private static Mobile ResolveCommander(IAIGMRosterTaskAgent agent, AIGMRosterTaskState state)
        {
            if (agent == null || state == null)
                return null;

            if (state.TrustedCommanderSerial != Serial.MinusOne)
                return World.FindMobile(state.TrustedCommanderSerial);

            return null;
        }

        private static void ShareTargetWithNearbySquad(IAIGMRosterTaskAgent agent, BaseCreature creature, AIGMRosterTaskState state, Mobile target)
        {
            if (target == null)
                return;

            if (!AIGMOperationalControlService.CanOperate(creature, AIGMOperationalAction.SquadAlert, state != null ? state.ControlEpoch : -1, false))
                return;

            string groupId = !String.IsNullOrWhiteSpace(state.GroupId)
                ? state.GroupId
                : AIGMRosterFactionService.ResolveFaction(agent.RosterCharacterId).ToString();

            foreach (IAIGMRosterTaskAgent ally in GetNearbyAllies(agent, creature, 14))
            {
                BaseCreature allyCreature = ally as BaseCreature;
                AIGMRosterTaskState allyState = GetOrCreateState(ally);
                if (allyCreature == null || allyState == null)
                    continue;

                if (!AIGMOperationalControlService.CanOperate(allyCreature, AIGMOperationalAction.SquadAlert, allyState.ControlEpoch, false))
                    continue;

                if (!String.IsNullOrWhiteSpace(allyState.GroupId) && !String.Equals(allyState.GroupId, groupId, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (allyState.Mode != AIGMRosterTaskMode.Hunt && allyState.Mode != AIGMRosterTaskMode.Track && allyState.Mode != AIGMRosterTaskMode.GuardOwner)
                    continue;

                allyState.TargetSerial = target.Serial;
                allyState.LastKnownTargetLocation = target.Location;
                allyState.LastKnownTargetMap = target.Map;
                allyState.LastSeenUtc = DateTime.UtcNow;
                allyState.TrailConfidence = Math.Max(allyState.TrailConfidence, 70.0);
            }
        }

        private static string BuildAssignmentResponse(BaseCreature creature, AIGMRosterTaskMode mode, AIGMRosterTargetSelector selector, string targetCanonicalId, AIGMRosterFaction targetFaction)
        {
            string targetText = !String.IsNullOrWhiteSpace(targetCanonicalId)
                ? targetCanonicalId
                : (targetFaction != AIGMRosterFaction.None ? targetFaction.ToString() : selector.ToString());

            switch (mode)
            {
                case AIGMRosterTaskMode.FollowOwner:
                    return String.Format("{0} is now following the commander.", creature.Name);
                case AIGMRosterTaskMode.Stay:
                    return String.Format("{0} is staying put.", creature.Name);
                case AIGMRosterTaskMode.Track:
                    return String.Format("{0} is now tracking {1}.", creature.Name, targetText);
                case AIGMRosterTaskMode.Hunt:
                    return String.Format("{0} is now hunting {1}.", creature.Name, targetText);
                case AIGMRosterTaskMode.GuardMobile:
                    return String.Format("{0} is now guarding {1}.", creature.Name, targetText);
                case AIGMRosterTaskMode.GuardOwner:
                    return String.Format("{0} is now guarding the commander.", creature.Name);
                case AIGMRosterTaskMode.ReturnHome:
                    return String.Format("{0} is returning home.", creature.Name);
                case AIGMRosterTaskMode.Regroup:
                    return String.Format("{0} is regrouping.", creature.Name);
                case AIGMRosterTaskMode.Travel:
                    return String.Format("{0} is traveling.", creature.Name);
                default:
                    return String.Format("{0} task set to {1}.", creature.Name, mode);
            }
        }

        private static string Describe(Mobile mobile)
        {
            return mobile == null ? "none" : String.Format("{0}[{1}]", String.IsNullOrWhiteSpace(mobile.Name) ? mobile.GetType().Name : mobile.Name, mobile.Serial);
        }

        private static string FormatPoint(Point3D point)
        {
            return String.Format("{0},{1},{2}", point.X, point.Y, point.Z);
        }

        private static string Safe(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return String.Empty;

            return value.Replace('"', '\'').Replace('\r', ' ').Replace('\n', ' ');
        }
    }
}
