using System;
using Server;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionTravelController
    {
        private static readonly TimeSpan TravelPulseInterval = TimeSpan.FromSeconds(1.0);

        public static bool StartTravel(BaseHire companion, string destinationName, out string response)
        {
            response = null;
            if (companion == null || companion.Deleted || companion.Map == null)
            {
                response = "I cannot travel just now.";
                return false;
            }

            Mobile owner = companion.GetOwner();
            if (owner == null)
            {
                response = "I need an owner before I can travel on command.";
                return false;
            }

            AIGMCompanionDestination resolved;
            if (!AIGMCompanionDestinationRegistry.TryResolve(destinationName, out resolved) || resolved == null)
            {
                response = "I do not know that destination.";
                return false;
            }

            if (resolved.Map != null && companion.Map != resolved.Map)
            {
                response = "I can only travel on my current map right now.";
                return false;
            }

            Point3D destination = new Point3D(resolved.X, resolved.Y, resolved.Z);
            if (!AIGMCompanionMapNavigator.CanStandAt(companion.Map, destination.X, destination.Y, destination.Z))
                destination = AIGMCompanionMapNavigator.TryFindNearbyStandablePoint(companion.Map, destination);

            if (!AIGMCompanionMapNavigator.CanStandAt(companion.Map, destination.X, destination.Y, destination.Z))
            {
                response = "That destination is not reachable as a standing point.";
                return false;
            }

            ClearTravelState(companion, false, false, "Travel rerouted");
            AIGMCompanionTrackingCycle.SuspendPursuitAndTravel(companion);
            AIGMCompanionStateAccess.ClearHoldPosition(companion);

            AIGMCompanionTravelObjective objective = new AIGMCompanionTravelObjective();
            objective.ObjectiveId = Guid.NewGuid().ToString("N");
            objective.DestinationName = resolved.Name;
            objective.DestinationMapName = resolved.Map != null ? resolved.Map.Name : (companion.Map != null ? companion.Map.Name : String.Empty);
            objective.DestinationX = destination.X;
            objective.DestinationY = destination.Y;
            objective.DestinationZ = destination.Z;
            objective.StartedUtc = DateTime.UtcNow;
            objective.LastProgressUtc = DateTime.UtcNow;
            objective.LastLocation = companion.Location;
            objective.LastDistance = AIGMCompanionMapNavigator.Distance2D(companion.Location, objective.DestinationPoint);
            objective.ArrivalRadius = Math.Max(1, resolved.ArrivalRadius);
            objective.PathCommitmentPulsesRemaining = 0;
            objective.LastCommittedPathGoal = objective.DestinationPoint;
            objective.OscillationCount = 0;
            objective.SuppressedRouteBand = null;
            objective.SuppressedRouteBandUntilUtc = DateTime.MinValue;
            objective.OriginalActiveSpeed = companion.ActiveSpeed;
            objective.OriginalPassiveSpeed = companion.PassiveSpeed;
            objective.TravelSpeedApplied = false;
            objective.Status = AIGMCompanionTravelStatus.Traveling;

            companion.CantWalk = false;
            companion.Combatant = null;
            companion.ControlTarget = null;
            companion.ControlOrder = OrderType.Come;

            AIGMCompanionStateAccess.SetTravelObjective(companion, objective);
            AIGMCompanionStateAccess.SetNextTravelPulseUtc(companion, DateTime.UtcNow);
            UMGMovementRouter.RecordIntent(companion, new UMGMovementIntent
            {
                Kind = UMGMovementIntentKind.TravelToNamedDestination,
                Requester = owner,
                DestinationName = resolved.Name,
                DestinationPoint = destination,
                DestinationMap = companion.Map,
                Reason = "travel_command"
            });
            response = "I am on my way to " + resolved.Name + ".";
            return true;
        }

        public static bool StopTravel(BaseHire companion, string reason, out string response)
        {
            response = null;
            if (companion == null)
                return false;

            AIGMCompanionTravelObjective objective = AIGMCompanionStateAccess.GetTravelObjective(companion);
            if (objective == null)
            {
                AIGMCompanionStateAccess.ResetAllCompanionIntentState(companion);
                response = "I am not traveling anywhere right now.";
                return true;
            }

            ClearTravelState(companion, true, true, string.IsNullOrWhiteSpace(reason) ? "I have stopped traveling." : reason);
            response = string.IsNullOrWhiteSpace(reason) ? "I have stopped traveling." : reason;
            return true;
        }

        public static bool ReportStatus(BaseHire companion, out string response)
        {
            response = null;
            if (companion == null)
                return false;

            AIGMCompanionTravelObjective objective = AIGMCompanionStateAccess.GetTravelObjective(companion);
            if (objective == null)
            {
                response = "I am not traveling anywhere right now.";
                return true;
            }

            string destination = String.IsNullOrWhiteSpace(objective.DestinationName) ? "an unknown destination" : objective.DestinationName;
            string point = objective.DestinationX + "," + objective.DestinationY + "," + objective.DestinationZ;
            switch (objective.CurrentPathStrategy)
            {
                case AIGMTravelPathStrategy.DirectStep:
                    response = "I am traveling directly toward " + destination + " at " + point + ".";
                    return true;
                case AIGMTravelPathStrategy.PathFollower:
                    response = "I am pathing toward " + destination + " at " + point + ".";
                    return true;
                case AIGMTravelPathStrategy.LocalAlternateStep:
                    response = "I am making a short sidestep while heading to " + destination + " at " + point + ".";
                    return true;
                case AIGMTravelPathStrategy.Stuck:
                    response = "I am stuck before reaching " + destination + " at " + point + ".";
                    return true;
                default:
                    response = "I am trying to reach " + destination + " at " + point + ".";
                    return true;
            }
        }

        public static bool StartTrackedPursuit(BaseHire companion, AIGMCompanionTrackingEntry entry)
        {
            if (companion == null || companion.Deleted || entry == null)
                return false;

            AIGMCompanionStateAccess.ClearHoldPosition(companion);
            AIGMCompanionTravelObjective objective = AIGMCompanionStateAccess.GetTravelObjective(companion);
            if (objective == null)
            {
                objective = new AIGMCompanionTravelObjective();
                objective.ObjectiveId = Guid.NewGuid().ToString("N");
                objective.DestinationName = entry.Name ?? "tracked target";
                objective.DestinationMapName = companion.Map != null ? companion.Map.Name : String.Empty;
                objective.DestinationX = companion.X;
                objective.DestinationY = companion.Y;
                objective.DestinationZ = companion.Z;
                objective.StartedUtc = DateTime.UtcNow;
                objective.LastProgressUtc = DateTime.UtcNow;
                objective.LastLocation = companion.Location;
                objective.LastDistance = 0;
                objective.ArrivalRadius = 1;
                objective.OriginalActiveSpeed = companion.ActiveSpeed;
                objective.OriginalPassiveSpeed = companion.PassiveSpeed;
                objective.TravelSpeedApplied = false;
                AIGMCompanionStateAccess.SetTravelObjective(companion, objective);
            }

            Mobile target = World.FindMobile(entry.TargetSerial);

            objective.HasTrackedPursuit = true;
            objective.TrackedTargetSerial = entry.TargetSerial;
            objective.TrackedTargetName = entry.Name;
            objective.TrackedTargetCategory = entry.Category;
            objective.TrackedPursuitStartedUtc = DateTime.UtcNow;
            objective.TrackedPursuitExpiresUtc = DateTime.UtcNow + TimeSpan.FromSeconds(entry.Category == AIGMTrackingCategory.Monsters ? 18.0 : 10.0);
            if (target != null)
            {
                objective.DestinationName = entry.Name ?? target.Name ?? "tracked target";
                objective.DestinationMapName = target.Map != null ? target.Map.Name : (companion.Map != null ? companion.Map.Name : String.Empty);
                objective.DestinationX = target.X;
                objective.DestinationY = target.Y;
                objective.DestinationZ = target.Z;
                objective.LastDistance = AIGMCompanionMapNavigator.Distance2D(companion.Location, target.Location);
                objective.LastLocation = companion.Location;
                objective.LastProgressUtc = DateTime.UtcNow;
                objective.ArrivalRadius = 1;
            }
            AIGMCompanionStateAccess.SetTravelObjective(companion, objective);
            AIGMCompanionStateAccess.SetNextTravelPulseUtc(companion, DateTime.UtcNow);
            companion.CantWalk = false;
            companion.ControlTarget = target;
            companion.ControlOrder = OrderType.Attack;
            UMGMovementRouter.SetTrackedPursuitState(companion, entry);
            AIGMExecutionLog.Write("TRACKING_PURSUIT_START companion={0} target={1} category={2} name={3}", companion.Serial.Value, entry.TargetSerial, entry.Category, entry.Name ?? String.Empty);
            return true;
        }

        public static void ClearTrackedPursuit(BaseHire companion)
        {
            if (companion == null)
                return;

            AIGMCompanionTravelObjective objective = AIGMCompanionStateAccess.GetTravelObjective(companion);
            if (objective == null)
                return;

            objective.HasTrackedPursuit = false;
            objective.TrackedTargetSerial = 0;
            objective.TrackedTargetName = null;
            objective.TrackedPursuitStartedUtc = DateTime.MinValue;
            objective.TrackedPursuitExpiresUtc = DateTime.MinValue;
            UMGMovementRouter.ClearTrackedPursuitState(companion, "tracked_pursuit_cleared");
        }

        public static void ClearTravelState(BaseHire companion, bool latchHold, bool quiet, string stopReason)
        {
            InternalClearTravelState(companion, latchHold, quiet, stopReason);
        }

        private static bool TryHandleTrackedPursuit(BaseHire companion, AIGMCompanionTravelObjective objective)
        {
            if (companion == null || objective == null || !objective.HasTrackedPursuit || AIGMCompanionStateAccess.IsHoldPositionLatched(companion))
                return false;

            Mobile target = World.FindMobile(objective.TrackedTargetSerial);
            if (target == null || target.Deleted || !target.Alive || target.Map != companion.Map || DateTime.UtcNow >= objective.TrackedPursuitExpiresUtc)
            {
                AIGMExecutionLog.Write("TRACKING_PURSUIT_END companion={0} target={1} reason=invalid_or_expired", companion.Serial.Value, objective.TrackedTargetSerial);
                ClearTrackedPursuit(companion);
                return false;
            }

            bool noRecentContact = !companion.InRange(target, 8);
            if (noRecentContact && DateTime.UtcNow >= objective.TrackedPursuitStartedUtc + TimeSpan.FromSeconds(12.0))
            {
                AIGMExecutionLog.Write("TRACKING_PURSUIT_END companion={0} target={1} reason=short_excursion_no_contact", companion.Serial.Value, objective.TrackedTargetSerial);
                ClearTrackedPursuit(companion);
                return false;
            }

            companion.CantWalk = false;
            companion.ControlTarget = target;
            objective.DestinationName = objective.TrackedTargetName ?? target.Name ?? objective.DestinationName;
            objective.DestinationMapName = target.Map != null ? target.Map.Name : objective.DestinationMapName;
            objective.DestinationX = target.X;
            objective.DestinationY = target.Y;
            objective.DestinationZ = target.Z;
            int destinationDistance = AIGMCompanionMapNavigator.Distance2D(companion.Location, target.Location);

            if (companion is AIGMCompanionDardalion && destinationDistance > 1)
            {
                companion.ControlOrder = OrderType.Follow;
            }

            if (destinationDistance > 24)
            {
                AIGMExecutionLog.Write("TRACKING_PURSUIT_END companion={0} target={1} reason=route_leash distance={2}", companion.Serial.Value, objective.TrackedTargetSerial, destinationDistance);
                ClearTrackedPursuit(companion);
                return false;
            }

            if (companion.InRange(target, 1))
            {
                companion.CantWalk = false;
                companion.ControlTarget = target;
                companion.Combatant = target;
                companion.ControlOrder = OrderType.Attack;
                objective.TrackedPursuitStartedUtc = DateTime.UtcNow;
                AIGMExecutionLog.Write("TRACKING_PURSUIT_ATTACK companion={0} target={1}", companion.Serial.Value, target.Serial.Value);
                return true;
            }

            bool moved = AIGMCompanionMapNavigator.TryStepTowardPoint(companion, target.Location);
            if (!moved)
            {
                AIGMCompanionAutoPathNavigator.Step(companion, objective);
                moved = AIGMCompanionAutoPathNavigator.TryExecuteCurrentStrategy(companion, objective);
            }

            if (moved)
            {
                objective.LastLocation = companion.Location;
                objective.LastProgressUtc = DateTime.UtcNow;
                objective.LastDistance = AIGMCompanionMapNavigator.Distance2D(companion.Location, target.Location);
                objective.TrackedPursuitStartedUtc = DateTime.UtcNow;
            }

            AIGMExecutionLog.Write("TRACKING_PURSUIT_STEP companion={0} target={1} moved={2} targetLoc={3} dist={4}", companion.Serial.Value, target.Serial.Value, moved, target.Location, AIGMCompanionMapNavigator.Distance2D(companion.Location, target.Location));
            if (!moved)
            {
                AIGMExecutionLog.Write("TRACKING_PURSUIT_WAIT companion={0} target={1} reason=blocked_retry", companion.Serial.Value, target.Serial.Value);
                return true;
            }

            return true;
        }

        public static void PulseTravel(BaseHire companion)
        {
            if (companion == null || companion.Deleted || companion.Map == null)
                return;

            AIGMCompanionTravelObjective objective = AIGMCompanionStateAccess.GetTravelObjective(companion);
            if (objective == null)
            {
                AIGMExecutionLog.Write("TRAVEL_PULSE_SKIP companion={0} reason=no_objective", companion.Serial.Value);
                return;
            }

            DateTime nextPulseUtc = AIGMCompanionStateAccess.GetNextTravelPulseUtc(companion);
            if (DateTime.UtcNow < nextPulseUtc)
            {
                AIGMExecutionLog.Write("TRAVEL_PULSE_SKIP companion={0} reason=cooldown nextUtc={1:o}", companion.Serial.Value, nextPulseUtc);
                return;
            }

            if (AIGMCompanionStateAccess.IsHoldPositionLatched(companion))
            {
                AIGMExecutionLog.Write("TRAVEL_PULSE_SKIP companion={0} reason=hold_latched", companion.Serial.Value);
                InternalClearTravelState(companion, true, true, "Travel held");
                return;
            }

            if (!objective.TravelSpeedApplied)
            {
                objective.OriginalActiveSpeed = companion.ActiveSpeed;
                objective.OriginalPassiveSpeed = companion.PassiveSpeed;
                companion.ActiveSpeed = 0.15;
                companion.PassiveSpeed = 0.30;
                objective.TravelSpeedApplied = true;
            }

            companion.CantWalk = false;
            if (companion.ControlOrder == OrderType.Stay || companion.ControlOrder == OrderType.Stop)
                companion.ControlOrder = OrderType.Come;

            AIGMCompanionStateAccess.SetNextTravelPulseUtc(companion, DateTime.UtcNow + TravelPulseInterval);

            AIGMExecutionLog.Write("TRAVEL_PULSE_RUN companion={0} hasTrackedPursuit={1} order={2} combatant={3}", companion.Serial.Value, objective.HasTrackedPursuit, companion.ControlOrder, companion.Combatant != null ? companion.Combatant.Serial.Value : 0);

            if (TryHandleTrackedPursuit(companion, objective))
            {
                AIGMCompanionStateAccess.SetTravelObjective(companion, objective);
                return;
            }

            if (AIGMCompanionMapNavigator.Distance2D(companion.Location, objective.DestinationPoint) <= objective.ArrivalRadius)
            {
                InternalClearTravelState(companion, false, true, "Arrived at destination");
                ApplyStopOrder(companion);
                return;
            }

            AIGMCompanionAutoPathNavigator.Step(companion, objective);
            bool moved = AIGMCompanionAutoPathNavigator.TryExecuteCurrentStrategy(companion, objective);

            if (!moved && objective.CurrentPathStrategy == AIGMTravelPathStrategy.PathFollower)
            {
                Point3D sidestepPoint;
                if (AIGMCompanionMapNavigator.TryGetLocalSidestepPoint(companion, objective.DestinationPoint, objective.Memory, out sidestepPoint))
                {
                    objective.ActiveLocalAlternatePoint = sidestepPoint;
                    objective.CurrentPathStrategy = AIGMTravelPathStrategy.LocalAlternateStep;
                    moved = AIGMCompanionAutoPathNavigator.TryExecuteCurrentStrategy(companion, objective);
                }
            }

            if (!moved)
            {
                objective.CurrentPathStrategy = AIGMTravelPathStrategy.Stuck;
                objective.Status = AIGMCompanionTravelStatus.Stuck;
                AIGMExecutionLog.Write("COMPANION_TRAVEL_STUCK companion={0} destination={1} point={2}", companion.Serial.Value, objective.DestinationName ?? String.Empty, objective.DestinationPoint);
                InternalClearTravelState(companion, true, true, String.Format("I am stuck before reaching {0}.", !String.IsNullOrWhiteSpace(objective.DestinationName) ? objective.DestinationName : objective.DestinationPoint.ToString()));
                return;
            }

            int previousDistance = objective.LastDistance;
            int currentDistance = AIGMCompanionMapNavigator.Distance2D(companion.Location, objective.DestinationPoint);
            if (currentDistance < previousDistance || previousDistance == 0)
            {
                objective.ConsecutiveNoProgressChecks = 0;
                objective.ConsecutiveDirectProgressSteps++;
            }
            else
            {
                objective.ConsecutiveNoProgressChecks++;
                objective.ConsecutiveDirectProgressSteps = 0;
            }

            objective.LastDistance = currentDistance;
            objective.LastLocation = companion.Location;
            objective.LastProgressUtc = DateTime.UtcNow;
            AIGMCompanionStateAccess.SetTravelObjective(companion, objective);
        }

        private static void InternalClearTravelState(BaseHire companion, bool latchHold, bool quiet, string stopReason)
        {
            if (companion == null)
                return;

            AIGMCompanionTravelObjective objective = AIGMCompanionStateAccess.GetTravelObjective(companion);
            if (objective != null)
            {
                objective.ActivePathFollower = null;
                if (objective.TravelSpeedApplied)
                {
                    companion.ActiveSpeed = objective.OriginalActiveSpeed;
                    companion.PassiveSpeed = objective.OriginalPassiveSpeed;
                }
            }

            AIGMCompanionStateAccess.SetTravelObjective(companion, null);
            AIGMCompanionStateAccess.SetNextTravelPulseUtc(companion, DateTime.MinValue);
            AIGMCompanionStateAccess.SetNextTravelThreatScanUtc(companion, DateTime.MinValue);

            if (latchHold)
                AIGMCompanionStateAccess.LatchHoldPosition(companion);

            ApplyStopOrder(companion);

            if (!quiet && !string.IsNullOrWhiteSpace(stopReason))
                companion.Say(stopReason);
        }

        private static void ApplyStopOrder(BaseHire companion)
        {
            if (companion == null)
                return;

            companion.CantWalk = true;
            companion.ControlTarget = null;
            companion.Combatant = null;
            companion.Home = companion.Location;
            companion.RangeHome = 0;
            companion.ControlOrder = OrderType.Stay;
        }
    }
}
