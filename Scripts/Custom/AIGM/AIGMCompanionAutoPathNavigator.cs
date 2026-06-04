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

            if (objective.Memory != null)
                objective.Memory.RecordBreadcrumb(companion.Location, distance);

            objective.ActiveLocalAlternatePoint = Point3D.Zero;
            objective.ActiveDetourPoint = Point3D.Zero;
            objective.ActiveWallFollowPoint = Point3D.Zero;
            objective.DetourCommitmentPulsesRemaining = 0;
            objective.WallFollowCommitmentPulsesRemaining = 0;
            objective.RecoveryMode = AIGMTravelRecoveryMode.None;

            if (AIGMCompanionMapNavigator.CanDirectStepTowardGoal(companion, goal))
            {
                objective.CurrentPathStrategy = AIGMTravelPathStrategy.DirectStep;
                return objective.CurrentPathStrategy;
            }

            bool preferPathFollower = distance > Math.Max(6, objective.ArrivalRadius + 1)
                || objective.ConsecutiveBlockedSteps > 0
                || objective.ConsecutiveNoProgressChecks > 0;

            if (preferPathFollower)
            {
                objective.CurrentPathStrategy = AIGMTravelPathStrategy.PathFollower;
                return objective.CurrentPathStrategy;
            }

            Point3D sidestepPoint;
            if (AIGMCompanionMapNavigator.TryGetLocalSidestepPoint(companion, goal, objective.Memory, out sidestepPoint))
            {
                objective.ActiveLocalAlternatePoint = sidestepPoint;
                objective.CurrentPathStrategy = AIGMTravelPathStrategy.LocalAlternateStep;
                return objective.CurrentPathStrategy;
            }

            objective.CurrentPathStrategy = AIGMTravelPathStrategy.Stuck;
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
                case AIGMTravelPathStrategy.PathFollower:
                    return ExecutePathFollowerStep(companion, objective);
                case AIGMTravelPathStrategy.LocalAlternateStep:
                    return ExecuteLocalAlternateStep(companion, objective);
                default:
                    return false;
            }
        }

        private static bool ExecuteDirectStep(BaseHire companion, AIGMCompanionTravelObjective objective)
        {
            bool moved = AIGMCompanionMapNavigator.TryDirectStepTowardGoal(companion, objective.DestinationPoint);
            if (moved)
            {
                objective.ConsecutiveBlockedSteps = 0;
                objective.ConsecutiveNoProgressChecks = 0;
                objective.ConsecutiveDirectProgressSteps++;
                objective.ConsecutivePathFollowerFailures = 0;
            }
            else
            {
                objective.ConsecutiveDirectProgressSteps = 0;
                objective.ConsecutiveBlockedSteps++;
            }

            return moved;
        }

        private static bool ExecutePathFollowerStep(BaseHire companion, AIGMCompanionTravelObjective objective)
        {
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
            }

            bool movedOrAdvanced = objective.ActivePathFollower.Follow(true, objective.ArrivalRadius);
            if (companion.Location != objective.LastLocation)
            {
                objective.ConsecutiveBlockedSteps = 0;
                objective.ConsecutiveNoProgressChecks = 0;
                objective.ConsecutivePathFollowerFailures = 0;
                return true;
            }

            if (movedOrAdvanced)
                return true;

            objective.ConsecutivePathFollowerFailures++;
            objective.ConsecutiveBlockedSteps++;

            if (objective.ConsecutivePathFollowerFailures >= 2)
                objective.ActivePathFollower.ForceRepath();

            if (objective.ConsecutivePathFollowerFailures >= 4)
            {
                objective.ActivePathFollower = null;
                return false;
            }

            return true;
        }

        private static bool ExecuteLocalAlternateStep(BaseHire companion, AIGMCompanionTravelObjective objective)
        {
            Point3D target = objective.ActiveLocalAlternatePoint;
            objective.ActiveLocalAlternatePoint = Point3D.Zero;
            if (target == Point3D.Zero || target == companion.Location)
                return false;

            bool moved = AIGMCompanionMapNavigator.TryStepTowardPoint(companion, target);
            if (moved)
            {
                objective.ConsecutiveBlockedSteps = 0;
                objective.ConsecutiveNoProgressChecks = 0;
                objective.ConsecutivePathFollowerFailures = 0;
            }

            return moved;
        }
    }
}
