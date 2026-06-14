using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionExecutionSpine
    {
        private static readonly Dictionary<int, AIGMCompanionExecutionState> States = new Dictionary<int, AIGMCompanionExecutionState>();

        public static string StartMonsterHunt(BaseHire companion, Mobile speaker)
        {
            if (!IsValidCompanion(companion))
                return "I cannot begin the hunt right now.";

            AIGMCompanionExecutionState state = GetOrCreateState(companion);
            state.OwnerSerial = speaker != null ? speaker.Serial.Value : 0;
            state.HuntActive = true;
            state.StartedUtc = DateTime.UtcNow;
            state.UpdatedUtc = DateTime.UtcNow;
            state.ReacquireCount = 0;
            SetPhase(state, AIGMCompanionExecutionPhase.TrackingMonsters, "hunt_started");
            Trace(state, "hunt_started");
            return TickMonsterHunt(companion);
        }

        public static string StopMonsterHunt(BaseHire companion, string reason)
        {
            if (!IsValidCompanion(companion))
                return "I cannot stop the hunt right now.";

            AIGMCompanionExecutionState state = GetOrCreateState(companion);
            state.HuntActive = false;
            state.CurrentTargetSerial = 0;
            state.CurrentTargetName = String.Empty;
            state.CurrentTargetPoint = Point3D.Zero;
            SetPhase(state, AIGMCompanionExecutionPhase.Stopped, String.IsNullOrWhiteSpace(reason) ? "hunt_stopped" : reason);
            AIGMCompanionCombatController.StopCombat(companion);
            Trace(state, "hunt_stopped");
            return companion.Name + ": I stop the monster hunt.";
        }

        public static string GetStatus(BaseHire companion)
        {
            if (!IsValidCompanion(companion))
                return "I cannot report hunt status right now.";

            AIGMCompanionExecutionState state = GetOrCreateState(companion);
            string target = String.IsNullOrWhiteSpace(state.CurrentTargetName) ? "none" : state.CurrentTargetName;
            return String.Format("{0}: huntActive={1}, phase={2}, target={3}, trace={4}", companion.Name, state.HuntActive, state.Phase, target, String.IsNullOrWhiteSpace(state.LastTrace) ? "none" : state.LastTrace);
        }

        public static AIGMCompanionExecutionState GetState(BaseHire companion)
        {
            return companion == null ? null : GetOrCreateState(companion);
        }

        public static string TickMonsterHunt(BaseHire companion)
        {
            if (!IsValidCompanion(companion))
                return "I cannot continue the hunt right now.";

            AIGMCompanionExecutionState state = GetOrCreateState(companion);
            state.LastTickUtc = DateTime.UtcNow;

            if (!state.HuntActive)
            {
                SetPhase(state, AIGMCompanionExecutionPhase.Stopped, "hunt_inactive");
                return companion.Name + ": no monster hunt is active.";
            }

            string sustainResponse;
            state.LastBandageAttempted = false;
            state.LastBandageStarted = false;
            if (AIGMCompanionSelfSustainService.TryBandageSelf(companion, out sustainResponse))
            {
                state.LastBandageAttempted = true;
                state.LastBandageStarted = true;
                SetPhase(state, AIGMCompanionExecutionPhase.SelfBandaging, sustainResponse);
                Trace(state, sustainResponse);
                return companion.Name + ": I begin bandaging my wounds.";
            }

            state.LastCureAttempted = false;
            state.LastCureSucceeded = false;
            if (companion.Poisoned && AIGMCompanionSelfSustainService.TryUseCurePotion(companion, out sustainResponse))
            {
                state.LastCureAttempted = true;
                state.LastCureSucceeded = true;
                SetPhase(state, AIGMCompanionExecutionPhase.SelfCuring, sustainResponse);
                Trace(state, sustainResponse);
                return companion.Name + ": I use a cure potion on myself.";
            }

            Mobile target = ResolveCurrentTarget(companion, state);
            if (target == null)
            {
                SetPhase(state, AIGMCompanionExecutionPhase.SelectingMonsterTarget, "selecting_target");
                target = AcquireNearestMonsterTarget(companion, state);
                if (target == null)
                {
                    state.HuntActive = false;
                    SetPhase(state, AIGMCompanionExecutionPhase.Stopped, "no_valid_monsters");
                    Trace(state, "no_valid_monsters");
                    return companion.Name + ": no valid monsters remain nearby.";
                }
            }

            state.CurrentTargetSerial = target.Serial.Value;
            state.CurrentTargetName = target.Name ?? target.GetType().Name;
            state.CurrentTargetPoint = target.Location;

            state.LastDoorOpenAttempted = true;
            state.LastDoorOpenSucceeded = false;
            if (!companion.InRange(target, 1))
            {
                AIGMCompanionTargetValidationResult preDoorValidation = AIGMCompanionTargetValidator.ValidateMonsterTarget(companion, target, state.ScanRange);
                if (!preDoorValidation.Allowed)
                {
                    state.LastTargetRejectionReason = preDoorValidation.Reason;
                    state.CurrentTargetSerial = 0;
                    state.CurrentTargetName = String.Empty;
                    state.CurrentTargetPoint = Point3D.Zero;
                    SetPhase(state, AIGMCompanionExecutionPhase.Reacquiring, preDoorValidation.Reason);
                    Trace(state, preDoorValidation.Reason);
                    return companion.Name + ": I will not pursue an invalid target.";
                }

                state.LastDoorOpenSucceeded = AIGMCompanionDoorService.TryOpenNearbyDoor(companion);
                if (state.LastDoorOpenSucceeded)
                {
                    SetPhase(state, AIGMCompanionExecutionPhase.OpeningDoor, "door_opened_for_pursuit");
                    Trace(state, "door_opened_for_pursuit");
                }

                SetPhase(state, AIGMCompanionExecutionPhase.PursuingMonster, "pursuing_target");
                Point3D before = companion.Location;
                int distanceBefore = (int)companion.GetDistanceToSqrt(target);
                Direction direction = companion.GetDirectionTo(target);
                state.LastMoveFrom = before;
                state.LastMoveDirection = (direction & Direction.Mask).ToString();
                state.LastMoveDistanceBefore = distanceBefore;

                bool moved = false;
                bool turnedFirst = false;
                if ((companion.Direction & Direction.Mask) != (direction & Direction.Mask))
                {
                    companion.Direction = direction;
                    turnedFirst = true;
                }

                moved = companion.Move(direction);
                Point3D after = companion.Location;
                int distanceAfter = (int)companion.GetDistanceToSqrt(target);
                state.LastMoveTo = after;
                state.LastMoveDistanceAfter = distanceAfter;

                if (after != before)
                {
                    state.LastMovementResult = "moved_one_step from=" + FormatPoint(before) + " to=" + FormatPoint(after) + " dir=" + state.LastMoveDirection + " distBefore=" + distanceBefore + " distAfter=" + distanceAfter + (turnedFirst ? " turnedFirst=true" : String.Empty);
                    Trace(state, "step_toward_target_moved");

                    if (companion.InRange(target, 1))
                    {
                        string postMoveCombatResult;
                        if (AIGMCompanionCombatController.TryEngageMonster(companion, target, out postMoveCombatResult))
                        {
                            state.LastCombatResult = postMoveCombatResult;
                            SetPhase(state, AIGMCompanionExecutionPhase.EngagingMonster, postMoveCombatResult);
                            Trace(state, "post_move_combat_engaged");
                            return companion.Name + ": I move into range and engage " + state.CurrentTargetName + ".";
                        }

                        state.LastCombatResult = postMoveCombatResult;
                    }

                    return companion.Name + ": I move toward " + state.CurrentTargetName + ".";
                }

                state.LastMovementResult = moved
                    ? "step_toward_target_no_location_change from=" + FormatPoint(before) + " to=" + FormatPoint(after) + " dir=" + state.LastMoveDirection + " distBefore=" + distanceBefore + " distAfter=" + distanceAfter + (turnedFirst ? " turnedFirst=true" : String.Empty)
                    : (turnedFirst
                        ? "turned_toward_target_only from=" + FormatPoint(before) + " to=" + FormatPoint(after) + " dir=" + state.LastMoveDirection + " distBefore=" + distanceBefore + " distAfter=" + distanceAfter
                        : "movement_blocked from=" + FormatPoint(before) + " to=" + FormatPoint(after) + " dir=" + state.LastMoveDirection + " distBefore=" + distanceBefore + " distAfter=" + distanceAfter);
                Trace(state, moved ? "step_toward_target_no_location_change" : (turnedFirst ? "turned_toward_target_only" : "movement_blocked"));
                SetPhase(state, AIGMCompanionExecutionPhase.Blocked, moved ? "no_location_change" : (turnedFirst ? "turned_only" : "movement_blocked"));
                return companion.Name + ": I could not close the distance to " + state.CurrentTargetName + ".";
            }

            string combatResult;
            if (AIGMCompanionCombatController.TryEngageMonster(companion, target, out combatResult))
            {
                state.LastCombatResult = combatResult;
                SetPhase(state, AIGMCompanionExecutionPhase.EngagingMonster, combatResult);
                Trace(state, combatResult);
                return companion.Name + ": I engage " + state.CurrentTargetName + ".";
            }

            state.LastCombatResult = combatResult;
            state.ReacquireCount++;
            state.CurrentTargetSerial = 0;
            state.CurrentTargetName = String.Empty;
            state.CurrentTargetPoint = Point3D.Zero;
            SetPhase(state, AIGMCompanionExecutionPhase.Reacquiring, combatResult);
            Trace(state, combatResult);
            return companion.Name + ": I lost my mark and will reacquire.";
        }

        private static Mobile ResolveCurrentTarget(BaseHire companion, AIGMCompanionExecutionState state)
        {
            if (companion == null || state == null || state.CurrentTargetSerial == 0)
                return null;

            Mobile target = World.FindMobile(state.CurrentTargetSerial);
            if (target == null)
                return null;

            AIGMCompanionTargetValidationResult validation = AIGMCompanionTargetValidator.ValidateMonsterTarget(companion, target, state.ScanRange);
            if (!validation.Allowed)
            {
                state.LastTargetRejectionReason = validation.Reason;
                return null;
            }

            return target;
        }

        private static Mobile AcquireNearestMonsterTarget(BaseHire companion, AIGMCompanionExecutionState state)
        {
            Mobile best = null;
            int bestDistance = Int32.MaxValue;
            List<string> notes = new List<string>();

            foreach (Mobile mobile in World.Mobiles.Values)
            {
                string categoryReason;
                if (!AIGMCompanionTargetValidator.IsHostileMonsterCandidate(companion, mobile, out categoryReason))
                {
                    if (notes.Count < 8)
                        notes.Add(DescribeCandidate(mobile) + "=" + categoryReason);
                    state.LastTargetRejectionReason = categoryReason;
                    continue;
                }

                AIGMCompanionTargetValidationResult validation = AIGMCompanionTargetValidator.ValidateMonsterTarget(companion, mobile, state.ScanRange);
                if (!validation.Allowed)
                {
                    if (notes.Count < 8)
                        notes.Add(DescribeCandidate(mobile) + "=" + validation.Reason);
                    state.LastTargetRejectionReason = validation.Reason;
                    continue;
                }

                int distance = (int)companion.GetDistanceToSqrt(mobile);
                if (distance < bestDistance)
                {
                    best = mobile;
                    bestDistance = distance;
                }
            }

            state.LastCandidateSummary = best != null
                ? "accepted=" + DescribeCandidate(best) + " dist=" + bestDistance + (notes.Count > 0 ? " rejected=" + String.Join(",", notes.ToArray()) : String.Empty)
                : (notes.Count > 0 ? "accepted=none rejected=" + String.Join(",", notes.ToArray()) : "accepted=none rejected=none");

            return best;
        }

        private static bool IsValidCompanion(BaseHire companion)
        {
            return companion != null && !companion.Deleted && companion.Alive && companion.Map != null;
        }

        private static AIGMCompanionExecutionState GetOrCreateState(BaseHire companion)
        {
            AIGMCompanionExecutionState state;
            if (!States.TryGetValue(companion.Serial.Value, out state))
            {
                state = new AIGMCompanionExecutionState();
                state.CompanionSerial = companion.Serial.Value;
                States[companion.Serial.Value] = state;
            }

            return state;
        }

        private static void SetPhase(AIGMCompanionExecutionState state, AIGMCompanionExecutionPhase phase, string reason)
        {
            if (state == null)
                return;

            state.Phase = phase;
            state.PhaseReason = reason ?? String.Empty;
            state.UpdatedUtc = DateTime.UtcNow;
        }

        private static void Trace(AIGMCompanionExecutionState state, string trace)
        {
            if (state == null)
                return;

            state.LastTrace = trace ?? String.Empty;
        }

        private static string FormatPoint(Point3D point)
        {
            return String.Format("({0},{1},{2})", point.X, point.Y, point.Z);
        }

        private static string DescribeCandidate(Mobile mobile)
        {
            if (mobile == null)
                return "null";

            string name = String.IsNullOrWhiteSpace(mobile.Name) ? mobile.GetType().Name : mobile.Name;
            return name.Replace(",", String.Empty);
        }
    }
}
