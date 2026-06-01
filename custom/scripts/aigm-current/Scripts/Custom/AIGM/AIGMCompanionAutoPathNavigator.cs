using System;
using Server;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionAutoPathNavigator
    {
        public static AIGMTravelPathStrategy Step(BaseHire companion, AIGMCompanionTravelObjective objective)
        {
            if (companion == null || objective == null || companion.Map == null)
                return AIGMTravelPathStrategy.Stuck;

            Point3D goal = objective.DestinationPoint;
            int distance = AIGMCompanionMapNavigator.Distance2D(companion.Location, goal);
            objective.Memory.RecordBreadcrumb(companion.Location, distance);
            objective.ActiveLocalAlternatePoint = Point3D.Zero;

            bool longRangeTravel = distance > Math.Max(8, objective.ArrivalRadius + 2);
            bool pathCommitmentActive = objective.PathCommitmentPulsesRemaining > 0;
            bool shouldPreferPathFollower = longRangeTravel || pathCommitmentActive;

            if (shouldPreferPathFollower)
            {
                objective.CurrentPathStrategy = AIGMTravelPathStrategy.PathFollower;
                if (objective.PathCommitmentPulsesRemaining <= 0)
                    objective.PathCommitmentPulsesRemaining = 12;

                objective.LastCommittedPathGoal = goal;
                Log("TRAVEL_STEP strategy=PathFollowerPreferred distance=" + distance + " commitment=" + objective.PathCommitmentPulsesRemaining);
                return objective.CurrentPathStrategy;
            }

            if (AIGMCompanionMapNavigator.CanDirectStepTowardGoal(companion, goal))
            {
                objective.ConsecutiveBlockedSteps = 0;
                objective.CurrentPathStrategy = AIGMTravelPathStrategy.DirectStep;
                Log("TRAVEL_STEP strategy=DirectStep distance=" + distance);
                return objective.CurrentPathStrategy;
            }

            objective.ConsecutiveBlockedSteps++;
            objective.ConsecutiveDirectProgressSteps = 0;
            Log("TRAVEL_BLOCKED count=" + objective.ConsecutiveBlockedSteps);

            if (DateTime.UtcNow < objective.DirectStepLockUntilUtc)
            {
                objective.CurrentPathStrategy = AIGMTravelPathStrategy.DirectStep;
                Log("TRAVEL_STEP strategy=DirectStepLocked distance=" + distance);
                return objective.CurrentPathStrategy;
            }

            if (objective.WallFollowCommitmentPulsesRemaining > 0 && objective.ActiveWallFollowPoint != Point3D.Zero)
            {
                objective.CurrentPathStrategy = AIGMTravelPathStrategy.WallFollow;
                Log("TRAVEL_WALLFOLLOW_RESUME point=" + objective.ActiveWallFollowPoint + " remaining=" + objective.WallFollowCommitmentPulsesRemaining);
                return objective.CurrentPathStrategy;
            }

            Point3D wallFollowPoint;
            if (objective.ConsecutiveBlockedSteps <= 3 && AIGMCompanionMapNavigator.TryChooseWallFollowPoint(companion, goal, objective, out wallFollowPoint))
            {
                objective.ActiveWallFollowPoint = wallFollowPoint;
                objective.WallFollowCommitmentPulsesRemaining = 8;
                objective.ConsecutiveWallFollowFailures = 0;
                objective.CurrentPathStrategy = AIGMTravelPathStrategy.WallFollow;
                Log("TRAVEL_WALLFOLLOW_START point=" + wallFollowPoint + " dir=" + objective.WallFollowDirectionX + "," + objective.WallFollowDirectionY);
                return objective.CurrentPathStrategy;
            }

            Point3D sidestepPoint;
            if (objective.ConsecutiveBlockedSteps <= 2 && AIGMCompanionMapNavigator.TryGetLocalSidestepPoint(companion, goal, objective.Memory, out sidestepPoint))
            {
                objective.ActiveLocalAlternatePoint = sidestepPoint;
                objective.CurrentPathStrategy = AIGMTravelPathStrategy.LocalAlternateStep;
                Log("TRAVEL_LOCAL_SIDESTEP_CHOSEN point=" + sidestepPoint);
                return objective.CurrentPathStrategy;
            }

            if (objective.Memory.IsOscillating(companion.Location))
            {
                Log("TRAVEL_OSCILLATION_DETECTED");
                Point3D backtrack = objective.Memory.GetBacktrackPoint(companion.Location);
                if (backtrack != companion.Location)
                {
                    objective.ActiveDetourPoint = backtrack;
                    objective.DetourCommitmentPulsesRemaining = 10;
                    objective.CurrentPathStrategy = AIGMTravelPathStrategy.BreadcrumbBacktrack;
                    Log("TRAVEL_BACKTRACK_START point=" + backtrack);
                    return objective.CurrentPathStrategy;
                }
            }

            if (objective.ConsecutiveBlockedSteps >= 2 || objective.ConsecutiveNoProgressChecks >= 2)
            {
                objective.CurrentPathStrategy = AIGMTravelPathStrategy.PathFollower;
                if (objective.PathCommitmentPulsesRemaining <= 0)
                    objective.PathCommitmentPulsesRemaining = 8;
                Log("TRAVEL_STEP strategy=PathFollowerFallback distance=" + distance + " commitment=" + objective.PathCommitmentPulsesRemaining);
                return objective.CurrentPathStrategy;
            }

            Point3D detour = AIGMCompanionMapNavigator.GetDetourBandPoint(companion, goal, objective.Memory, objective.ConsecutiveDetourFailures);
            if (detour != companion.Location)
            {
                objective.ActiveDetourPoint = detour;
                objective.DetourCommitmentPulsesRemaining = 10;
                objective.CurrentPathStrategy = AIGMTravelPathStrategy.CommittedDetour;
                Log("TRAVEL_DETOUR_CHOSEN point=" + detour + " band=" + AIGMCompanionMapNavigator.GetRouteBand(companion.Location, detour));
                return objective.CurrentPathStrategy;
            }

            objective.CurrentPathStrategy = AIGMTravelPathStrategy.Stuck;
            Log("TRAVEL_STUCK_FINAL reason=No viable strategy selected.");
            return objective.CurrentPathStrategy;
        }

        public static bool TryExecuteCurrentStrategy(BaseHire companion, AIGMCompanionTravelObjective objective)
        {
            if (companion == null || objective == null)
                return false;

            switch (objective.CurrentPathStrategy)
            {
                case AIGMTravelPathStrategy.DirectStep:
                    return ExecuteDirectStep(companion, objective);
                case AIGMTravelPathStrategy.LocalAlternateStep:
                    return ExecuteLocalAlternateStep(companion, objective);
                case AIGMTravelPathStrategy.WallFollow:
                    return ExecuteWallFollowStep(companion, objective);
                case AIGMTravelPathStrategy.PathFollower:
                    return TryPathFollowerStep(companion, objective, true);
                case AIGMTravelPathStrategy.CommittedDetour:
                case AIGMTravelPathStrategy.BreadcrumbBacktrack:
                    return ExecuteDetourStep(companion, objective);
            }

            return false;
        }

        private static bool ExecuteDirectStep(BaseHire companion, AIGMCompanionTravelObjective objective)
        {
            bool moved = AIGMCompanionMapNavigator.TryDirectStepTowardGoal(companion, objective.DestinationPoint);
            if (moved)
            {
                objective.ConsecutiveBlockedSteps = 0;
                objective.ConsecutiveNoProgressChecks = 0;
                objective.ConsecutiveDirectProgressSteps++;
                if (objective.ConsecutiveDirectProgressSteps >= 3)
                    objective.DirectStepLockUntilUtc = DateTime.UtcNow + TimeSpan.FromSeconds(3.0);
            }
            else
            {
                objective.ConsecutiveDirectProgressSteps = 0;
                objective.ConsecutiveBlockedSteps++;
            }

            return moved;
        }

        private static bool ExecuteLocalAlternateStep(BaseHire companion, AIGMCompanionTravelObjective objective)
        {
            Point3D target = objective.ActiveLocalAlternatePoint;
            if (target == Point3D.Zero || target == companion.Location)
                return false;

            if (AIGMCompanionMapNavigator.TryStepTowardPoint(companion, target))
            {
                objective.ActiveLocalAlternatePoint = Point3D.Zero;
                Log("TRAVEL_LOCAL_SIDESTEP_COMMIT point=" + target);
                return true;
            }

            objective.ActiveLocalAlternatePoint = Point3D.Zero;
            return false;
        }

        private static bool ExecuteWallFollowStep(BaseHire companion, AIGMCompanionTravelObjective objective)
        {
            if (objective.ActiveWallFollowPoint == Point3D.Zero)
                return false;

            if (AIGMCompanionMapNavigator.TryStepTowardPoint(companion, objective.ActiveWallFollowPoint))
            {
                objective.ConsecutiveBlockedSteps = 0;
                objective.ConsecutiveWallFollowFailures = 0;
                if (objective.WallFollowCommitmentPulsesRemaining > 0)
                    objective.WallFollowCommitmentPulsesRemaining--;

                if (objective.WallFollowCommitmentPulsesRemaining <= 0 || Utility.InRange(companion.Location, objective.ActiveWallFollowPoint, 1))
                    objective.ActiveWallFollowPoint = Point3D.Zero;

                Log("TRAVEL_WALLFOLLOW_COMMIT remaining=" + objective.WallFollowCommitmentPulsesRemaining + " point=" + objective.ActiveWallFollowPoint);
                return true;
            }

            objective.ConsecutiveWallFollowFailures++;
            Log("TRAVEL_WALLFOLLOW_FAIL count=" + objective.ConsecutiveWallFollowFailures + " point=" + objective.ActiveWallFollowPoint);
            if (objective.ConsecutiveWallFollowFailures >= 2)
            {
                objective.ActiveWallFollowPoint = Point3D.Zero;
                objective.WallFollowCommitmentPulsesRemaining = 0;
            }

            return false;
        }

        private static bool ExecuteDetourStep(BaseHire companion, AIGMCompanionTravelObjective objective)
        {
            if (objective.ActiveDetourPoint == Point3D.Zero)
                return false;

            if (AIGMCompanionMapNavigator.TryStepTowardPoint(companion, objective.ActiveDetourPoint))
            {
                if (objective.DetourCommitmentPulsesRemaining > 0)
                    objective.DetourCommitmentPulsesRemaining--;

                Log("TRAVEL_DETOUR_COMMIT remaining=" + objective.DetourCommitmentPulsesRemaining + " point=" + objective.ActiveDetourPoint);
                return true;
            }

            return false;
        }

        private static bool TryPathFollowerStep(BaseHire companion, AIGMCompanionTravelObjective objective, bool allowForcedLocalFallback)
        {
            if (companion == null || objective == null || companion.Map == null)
                return false;

            Point3D goal = objective.DestinationPoint;
            bool goalChanged = objective.LastCommittedPathGoal != Point3D.Zero && objective.LastCommittedPathGoal != goal;
            if (goalChanged)
            {
                objective.ActivePathFollower = null;
                objective.LastCommittedPathGoal = goal;
            }

            if (objective.ActivePathFollower == null)
            {
                objective.ActivePathFollower = new PathFollower(companion, goal);
                objective.LastCommittedPathGoal = goal;
                Log("TRAVEL_PATHFOLLOWER_START goal=" + goal);
            }

            bool arrived = objective.ActivePathFollower.Follow(true, objective.ArrivalRadius);
            Log("TRAVEL_PATHFOLLOWER_STEP result=" + arrived + " commitment=" + objective.PathCommitmentPulsesRemaining);

            if (arrived)
            {
                objective.ConsecutivePathFollowerFailures = 0;
                if (objective.PathCommitmentPulsesRemaining > 0)
                    objective.PathCommitmentPulsesRemaining--;
                return true;
            }

            if (objective.LastLocation == companion.Location)
            {
                objective.ConsecutivePathFollowerFailures++;
                Log("TRAVEL_PATHFOLLOWER_FAIL count=" + objective.ConsecutivePathFollowerFailures);

                if (objective.ConsecutivePathFollowerFailures >= 2)
                {
                    objective.ActivePathFollower.ForceRepath();
                    Log("TRAVEL_PATHFOLLOWER_REPATH goal=" + goal);
                }

                if (objective.ConsecutivePathFollowerFailures >= 4)
                {
                    objective.ActivePathFollower = null;
                    objective.PathCommitmentPulsesRemaining = 0;
                    return allowForcedLocalFallback;
                }
            }
            else
            {
                objective.ConsecutivePathFollowerFailures = 0;
                if (objective.PathCommitmentPulsesRemaining > 0)
                    objective.PathCommitmentPulsesRemaining--;
            }

            return true;
        }

        private static void Log(string message)
        {
            try
            {
                string path = System.IO.Path.Combine(Core.BaseDirectory, "Logs", "AIGMCompanionAutoPathNavigator.log");
                System.IO.File.AppendAllText(path, DateTime.UtcNow.ToString("o") + " " + message + Environment.NewLine);
            }
            catch
            {
            }
        }
    }
}
