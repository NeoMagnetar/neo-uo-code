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

            bool started = AIGMMovementController.StartCompanionNamedPath(companion, owner, destinationName, out response);
            if (!started)
                return false;

            AIGMCompanionTravelObjective objective = new AIGMCompanionTravelObjective();
            objective.ObjectiveId = Guid.NewGuid().ToString("N");
            objective.DestinationName = resolved.Name;
            objective.DestinationMapName = resolved.Map != null ? resolved.Map.Name : (companion.Map != null ? companion.Map.Name : String.Empty);
            objective.DestinationX = resolved.X;
            objective.DestinationY = resolved.Y;
            objective.DestinationZ = resolved.Z;
            objective.StartedUtc = DateTime.UtcNow;
            objective.LastProgressUtc = DateTime.UtcNow;
            objective.LastLocation = companion.Location;
            objective.LastDistance = AIGMCompanionMapNavigator.Distance2D(companion.Location, objective.DestinationPoint);
            objective.ArrivalRadius = Math.Max(1, resolved.ArrivalRadius);
            objective.PathCommitmentPulsesRemaining = 12;
            objective.LastCommittedPathGoal = objective.DestinationPoint;
            objective.OriginalActiveSpeed = companion.ActiveSpeed;
            objective.OriginalPassiveSpeed = companion.PassiveSpeed;
            objective.TravelSpeedApplied = true;
            companion.ActiveSpeed = 0.15;
            companion.PassiveSpeed = 0.30;
            AIGMCompanionStateAccess.SetTravelObjective(companion, objective);
            AIGMCompanionStateAccess.SetNextTravelPulseUtc(companion, DateTime.UtcNow + TravelPulseInterval);
            return true;
        }

        public static bool StopTravel(BaseHire companion, string reason, out string response)
        {
            response = null;
            if (companion == null)
                return false;

            Mobile owner = companion.GetOwner();
            if (owner == null)
            {
                response = "I am not traveling anywhere right now.";
                return false;
            }

            AIGMCompanionTravelObjective objective = AIGMCompanionStateAccess.GetTravelObjective(companion);
            if (objective == null)
            {
                response = "I am not traveling anywhere right now.";
                return true;
            }

            if (objective.ActivePathFollower != null)
                objective.ActivePathFollower = null;

            if (objective.TravelSpeedApplied)
            {
                companion.ActiveSpeed = objective.OriginalActiveSpeed;
                companion.PassiveSpeed = objective.OriginalPassiveSpeed;
                objective.TravelSpeedApplied = false;
            }

            objective.ActiveDetourPoint = Point3D.Zero;
            objective.ActiveLocalAlternatePoint = Point3D.Zero;
            objective.ActiveWallFollowPoint = Point3D.Zero;
            objective.DetourCommitmentPulsesRemaining = 0;
            objective.WallFollowCommitmentPulsesRemaining = 0;
            objective.ConsecutiveBlockedSteps = 0;
            objective.ConsecutiveNoProgressChecks = 0;
            objective.ConsecutivePathFollowerFailures = 0;
            objective.ConsecutiveDetourFailures = 0;
            objective.CurrentPathStrategy = AIGMTravelPathStrategy.Stuck;
            companion.ControlTarget = null;
            companion.Combatant = null;
            companion.ControlOrder = OrderType.Stay;
            AIGMCompanionStateAccess.SetTravelObjective(companion, null);
            string ignored;
            AIGMMovementController.Stop(owner, companion, string.IsNullOrWhiteSpace(reason) ? "I have stopped traveling." : reason, out ignored);
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
                case AIGMTravelPathStrategy.LocalAlternateStep:
                    response = "I am making a local sidestep around an obstacle while heading to " + destination + " at " + point + ".";
                    return true;
                case AIGMTravelPathStrategy.WallFollow:
                    response = "I am following the obstacle edge toward " + destination + " at " + point + ".";
                    return true;
                case AIGMTravelPathStrategy.PathFollower:
                    response = "I am pathing toward " + destination + " at " + point + ".";
                    return true;
                case AIGMTravelPathStrategy.CommittedDetour:
                    response = "I am reaching " + destination + " at " + point + " by way of a detour.";
                    return true;
                case AIGMTravelPathStrategy.BreadcrumbBacktrack:
                    response = "I am backing out and trying another route to " + destination + " at " + point + ".";
                    return true;
                default:
                    response = "I am trying to reach " + destination + " at " + point + ".";
                    return true;
            }
        }

        public static void PulseTravel(BaseHire companion)
        {
            if (companion == null || companion.Deleted || companion.Map == null)
                return;

            AIGMCompanionTravelObjective objective = AIGMCompanionStateAccess.GetTravelObjective(companion);
            if (objective == null || DateTime.UtcNow < AIGMCompanionStateAccess.GetNextTravelPulseUtc(companion))
                return;

            if (!objective.TravelSpeedApplied)
            {
                objective.OriginalActiveSpeed = companion.ActiveSpeed;
                objective.OriginalPassiveSpeed = companion.PassiveSpeed;
                companion.ActiveSpeed = 0.15;
                companion.PassiveSpeed = 0.30;
                objective.TravelSpeedApplied = true;
            }

            AIGMCompanionStateAccess.SetNextTravelPulseUtc(companion, DateTime.UtcNow + TravelPulseInterval);
            if (objective.RecoveryMode == AIGMTravelRecoveryMode.EscapeToMinorWaypoint)
            {
                if (objective.ActiveEscapePoint != Point3D.Zero)
                {
                    if (AIGMCompanionMapNavigator.TryDirectStepTowardGoal(companion, objective.ActiveEscapePoint))
                    {
                        objective.EscapeCommitmentPulsesRemaining--;
                        if (Utility.InRange(companion.Location, objective.ActiveEscapePoint, 1) || objective.EscapeCommitmentPulsesRemaining <= 0)
                        {
                            objective.RecoveryMode = AIGMTravelRecoveryMode.RerouteRestart;
                            objective.ActiveEscapePoint = Point3D.Zero;
                        }
                    }
                }
            }
            else if (objective.RecoveryMode == AIGMTravelRecoveryMode.BreadcrumbBacktrack)
            {
                if (objective.ActiveEscapePoint != Point3D.Zero)
                {
                    if (AIGMCompanionMapNavigator.TryDirectStepTowardGoal(companion, objective.ActiveEscapePoint))
                    {
                        if (Utility.InRange(companion.Location, objective.ActiveEscapePoint, 1))
                        {
                            objective.RecoveryMode = AIGMTravelRecoveryMode.RerouteRestart;
                            objective.ActiveEscapePoint = Point3D.Zero;
                        }
                    }
                }
            }
            else
            {
                AIGMCompanionAutoPathNavigator.Step(companion, objective);
                AIGMCompanionAutoPathNavigator.TryExecuteCurrentStrategy(companion, objective);

                if (AIGMCompanionTrapRecovery.DetectStuckCluster(companion, objective))
                {
                    AIGMCompanionTrapRecovery.MarkStuckZone(companion, objective, "oscillation_or_blocked");

                    Point3D escapePoint;
                    if (AIGMCompanionTrapRecovery.TryFindEscapePoint(companion, objective, out escapePoint))
                    {
                        objective.ActiveEscapePoint = escapePoint;
                        objective.EscapeCommitmentPulsesRemaining = 12;
                        objective.RecoveryMode = AIGMTravelRecoveryMode.EscapeToMinorWaypoint;
                    }
                    else if (AIGMCompanionTrapRecovery.TryBreadcrumbBacktrack(companion, objective, out escapePoint))
                    {
                        objective.ActiveEscapePoint = escapePoint;
                        objective.RecoveryMode = AIGMTravelRecoveryMode.BreadcrumbBacktrack;
                    }
                    else
                    {
                        objective.RecoveryMode = AIGMTravelRecoveryMode.HardStuck;
                        objective.Status = AIGMCompanionTravelStatus.Traveling;
                        objective.ConsecutiveEscapeFailures++;
                        objective.ConsecutiveBacktrackFailures++;
                        objective.EscapeCommitmentPulsesRemaining = 16;
                        objective.ConsecutiveBlockedSteps = 0;
                        objective.ConsecutiveNoProgressChecks = 0;

                        if (objective.Memory != null)
                            objective.Memory.RecordFailure(companion.Location, "hard_recovery_retry");

                        Point3D deeperFallback = objective.Memory != null ? objective.Memory.GetBacktrackPoint(companion.Location) : companion.Location;
                        if (deeperFallback != companion.Location)
                        {
                            objective.ActiveEscapePoint = deeperFallback;
                            objective.RecoveryMode = AIGMTravelRecoveryMode.BreadcrumbBacktrack;
                        }
                        else
                        {
                            objective.ActiveEscapePoint = companion.Location;
                        }

                        companion.Say("I am taking a wider route and trying again.");
                    }
                }
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
    }
}
