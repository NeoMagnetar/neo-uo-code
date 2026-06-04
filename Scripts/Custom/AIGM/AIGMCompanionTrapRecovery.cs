using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public enum AIGMTravelRecoveryMode
    {
        None = 0,
        SuspectStuck = 1,
        EscapeRingSearch = 2,
        EscapeToMinorWaypoint = 3,
        BreadcrumbBacktrack = 4,
        RerouteRestart = 5,
        HardStuck = 6
    }

    public sealed class AIGMStuckZone
    {
        public Point3D Center { get; set; }
        public int Radius { get; set; }
        public int FailureCount { get; set; }
        public DateTime LastFailureUtc { get; set; }
        public DateTime AvoidUntilUtc { get; set; }
        public string Reason { get; set; }

        public bool Contains(Point3D p)
        {
            return Utility.InRange(Center, p, Radius);
        }
    }

    public sealed class AIGMEscapeCandidate
    {
        public Point3D Location { get; set; }
        public int Radius { get; set; }
        public int Score { get; set; }
        public string Reason { get; set; }
    }

    public static class AIGMCompanionTrapRecovery
    {
        public static bool DetectStuckCluster(BaseHire companion, AIGMCompanionTravelObjective objective)
        {
            if (companion == null || objective == null || objective.Memory == null)
                return false;

            int nearbyRepeats = 0;
            Point3D current = companion.Location;
            foreach (AIGMTravelBreadcrumb crumb in objective.Memory.Breadcrumbs)
            {
                if (Utility.InRange(current, crumb.Location, 3))
                    nearbyRepeats++;
            }

            bool noProgress = objective.ConsecutiveNoProgressChecks >= 3 || objective.ConsecutiveBlockedSteps >= 3;
            return nearbyRepeats >= 3 && noProgress;
        }

        public static void MarkStuckZone(BaseHire companion, AIGMCompanionTravelObjective objective, string reason)
        {
            if (companion == null || objective == null)
                return;

            int radius = 6;
            if (objective.StuckZones == null)
                objective.StuckZones = new List<AIGMStuckZone>();

            for (int i = 0; i < objective.StuckZones.Count; i++)
            {
                AIGMStuckZone zone = objective.StuckZones[i];
                if (zone.Contains(companion.Location))
                {
                    zone.FailureCount++;
                    zone.Radius = Math.Min(24, zone.Radius + 4);
                    zone.LastFailureUtc = DateTime.UtcNow;
                    zone.AvoidUntilUtc = DateTime.UtcNow + TimeSpan.FromMinutes(5);
                    zone.Reason = reason;
                    Log("TRAP_ZONE_MARKED center=" + zone.Center + " radius=" + zone.Radius + " failures=" + zone.FailureCount);
                    return;
                }
            }

            objective.StuckZones.Add(new AIGMStuckZone
            {
                Center = companion.Location,
                Radius = radius,
                FailureCount = 1,
                LastFailureUtc = DateTime.UtcNow,
                AvoidUntilUtc = DateTime.UtcNow + TimeSpan.FromMinutes(5),
                Reason = reason
            });
            Log("TRAP_ZONE_MARKED center=" + companion.Location + " radius=" + radius + " failures=1");
        }

        public static bool TryFindEscapePoint(BaseHire companion, AIGMCompanionTravelObjective objective, out Point3D escapePoint)
        {
            escapePoint = Point3D.Zero;
            if (companion == null || objective == null)
                return false;

            Log("ESCAPE_RING_SEARCH_START");
            int[] radii = new[] { 6, 10, 14, 20, 28, 36, 48 };
            AIGMEscapeCandidate best = null;

            for (int r = 0; r < radii.Length; r++)
            {
                foreach (Point3D candidate in AIGMCompanionMapNavigator.BuildRingCandidates(companion.Location, radii[r]))
                {
                    if (!AIGMCompanionMapNavigator.CanStandAt(companion.Map, candidate.X, candidate.Y, candidate.Z))
                        continue;

                    if (IsInsideActiveStuckZone(objective, candidate))
                        continue;

                    if (objective.Memory != null && objective.Memory.IsNearFailedPoint(candidate, 3))
                        continue;

                    if (!CanPathLocally(companion, candidate))
                        continue;

                    int score = ScoreEscapeCandidate(companion, objective, candidate, radii[r]);
                    Log("ESCAPE_CANDIDATE point=" + candidate + " score=" + score);
                    if (best == null || score > best.Score)
                    {
                        best = new AIGMEscapeCandidate { Location = candidate, Radius = radii[r], Score = score, Reason = "ring_escape" };
                    }
                }

                if (best != null)
                    break;
            }

            if (best == null)
                return false;

            escapePoint = best.Location;
            Log("ESCAPE_POINT_CHOSEN point=" + best.Location + " score=" + best.Score);
            return true;
        }

        public static bool TryBreadcrumbBacktrack(BaseHire companion, AIGMCompanionTravelObjective objective, out Point3D backtrackPoint)
        {
            backtrackPoint = Point3D.Zero;
            if (companion == null || objective == null || objective.Memory == null)
                return false;

            AIGMTravelBreadcrumb[] crumbs = objective.Memory.Breadcrumbs.ToArray();
            for (int i = crumbs.Length - 1; i >= 0; i--)
            {
                AIGMTravelBreadcrumb b = crumbs[i];
                if (Utility.InRange(companion.Location, b.Location, 4))
                    continue;
                if (IsInsideActiveStuckZone(objective, b.Location))
                    continue;
                if (!AIGMCompanionMapNavigator.CanStandAt(companion.Map, b.Location.X, b.Location.Y, b.Location.Z))
                    continue;
                if (!CanPathLocally(companion, b.Location))
                    continue;

                backtrackPoint = b.Location;
                Log("BREADCRUMB_BACKTRACK_START point=" + backtrackPoint);
                return true;
            }

            return false;
        }

        public static bool IsInsideActiveStuckZone(AIGMCompanionTravelObjective objective, Point3D point)
        {
            if (objective == null || objective.StuckZones == null)
                return false;

            for (int i = 0; i < objective.StuckZones.Count; i++)
            {
                AIGMStuckZone zone = objective.StuckZones[i];
                if (zone.AvoidUntilUtc > DateTime.UtcNow && zone.Contains(point))
                    return true;
            }

            return false;
        }

        public static int ScoreEscapeCandidate(BaseHire companion, AIGMCompanionTravelObjective objective, Point3D candidate, int radius)
        {
            int score = 0;
            int currentDistance = (int)Utility.GetDistanceToSqrt(companion.Location, objective.DestinationPoint);
            int candidateDistance = (int)Utility.GetDistanceToSqrt(candidate, objective.DestinationPoint);

            if (candidateDistance < currentDistance)
                score += 20;
            else
                score -= Math.Min(20, candidateDistance - currentDistance);

            if (objective.StuckZones != null)
            {
                for (int i = 0; i < objective.StuckZones.Count; i++)
                {
                    AIGMStuckZone zone = objective.StuckZones[i];
                    if (zone.AvoidUntilUtc > DateTime.UtcNow)
                    {
                        int distFromZone = (int)Utility.GetDistanceToSqrt(candidate, zone.Center);
                        score += Math.Min(25, distFromZone);
                    }
                }
            }

            score += radius / 2;
            return score;
        }

        public static bool CanPathLocally(BaseHire companion, Point3D point)
        {
            if (companion == null)
                return false;

            return Utility.InRange(companion.Location, point, 38);
        }

        private static void Log(string message)
        {
            try
            {
                string path = System.IO.Path.Combine(Core.BaseDirectory, "Logs", "AIGMCompanionTrapRecovery.log");
                System.IO.File.AppendAllText(path, DateTime.UtcNow.ToString("o") + " " + message + Environment.NewLine);
            }
            catch
            {
            }
        }
    }
}
