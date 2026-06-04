using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionMapNavigator
    {
        public static int GetCorrectedZ(Map map, int x, int y, int z)
        {
            if (map == null)
                return z;

            if (map.CanFit(x, y, z, 16, false, false))
                return z;

            return map.GetAverageZ(x, y);
        }

        public static bool CanStandAt(Map map, int x, int y, int z)
        {
            return map != null && map.CanFit(x, y, z, 16, false, false);
        }

        public static int Distance2D(Point3D a, Point3D b)
        {
            int dx = a.X - b.X;
            int dy = a.Y - b.Y;
            return (int)Math.Round(Math.Sqrt((dx * dx) + (dy * dy)));
        }

        public static Point3D GetNextStepToward(BaseHire companion, Point3D destination)
        {
            if (companion == null || companion.Map == null)
                return Point3D.Zero;

            int dx = Math.Sign(destination.X - companion.X);
            int dy = Math.Sign(destination.Y - companion.Y);
            return GetBestCandidate(companion, dx, dy, true);
        }

        public static bool CanDirectStepTowardGoal(BaseHire companion, Point3D destination)
        {
            if (companion == null || companion.Map == null)
                return false;

            Point3D next = GetNextStepToward(companion, destination);
            return next != companion.Location && next != Point3D.Zero;
        }

        public static bool TryStepTowardPoint(BaseHire companion, Point3D target)
        {
            if (companion == null || companion.Map == null)
                return false;

            Point3D next = GetNextStepToward(companion, target);
            if (next == companion.Location || next == Point3D.Zero)
                return false;

            Direction direction = companion.GetDirectionTo(next) | Direction.Running;
            companion.Direction = direction;
            return companion.Move(direction);
        }

        public static Point3D GetRecoveryStep(BaseHire companion, Point3D destination, AIGMCompanionTravelObjective objective)
        {
            if (companion == null || companion.Map == null || objective == null)
                return Point3D.Zero;

            int dx = Math.Sign(destination.X - companion.X);
            int dy = Math.Sign(destination.Y - companion.Y);

            if (objective.RecoveryStepsRemaining <= 0)
            {
                int lateralX = 0;
                int lateralY = 0;

                if (Math.Abs(destination.X - companion.X) >= Math.Abs(destination.Y - companion.Y))
                {
                    lateralY = dy == 0 ? 1 : dy;
                }
                else
                {
                    lateralX = dx == 0 ? 1 : dx;
                }

                if (objective.RecoveryAttempts % 2 == 1)
                {
                    lateralX *= -1;
                    lateralY *= -1;
                }

                objective.RecoveryDirectionX = lateralX;
                objective.RecoveryDirectionY = lateralY;
                objective.RecoveryStepsRemaining = 3 + Math.Min(2, objective.RecoveryAttempts);
            }

            if (objective.StuckCounter >= 6)
            {
                Point3D retreat = GetBestCandidate(companion, -dx, -dy, true);
                if (retreat != companion.Location)
                    return retreat;

                Point3D deeperRetreat = GetBestCandidateFromPoint(companion.Map, companion.Location, -dx * 2, -dy * 2, companion.Z);
                if (deeperRetreat != companion.Location)
                    return deeperRetreat;
            }

            Point3D lateral = GetBestCandidate(companion, objective.RecoveryDirectionX, objective.RecoveryDirectionY, true);
            if (lateral != companion.Location)
            {
                objective.RecoveryStepsRemaining--;
                return lateral;
            }

            Point3D alternate = GetBestCandidate(companion, objective.RecoveryDirectionY, objective.RecoveryDirectionX, true);
            if (alternate != companion.Location)
            {
                objective.RecoveryStepsRemaining--;
                return alternate;
            }

            return companion.Location;
        }

        private static Point3D GetBestCandidate(BaseHire companion, int dx, int dy, bool allowSides)
        {
            return GetBestCandidateFromPoint(companion.Map, companion.Location, dx, dy, companion.Z, allowSides);
        }

        public static bool TryDirectStepTowardGoal(BaseHire companion, Point3D destination)
        {
            return TryStepTowardPoint(companion, destination);
        }

        public static bool TryLocalSidestep(BaseHire companion, Point3D goal, AIGMTravelMemory memory)
        {
            Point3D sidestepPoint;
            if (!TryGetLocalSidestepPoint(companion, goal, memory, out sidestepPoint))
                return false;

            return TryStepTowardPoint(companion, sidestepPoint);
        }

        public static bool TryGetLocalSidestepPoint(BaseHire companion, Point3D goal, AIGMTravelMemory memory, out Point3D sidestepPoint)
        {
            sidestepPoint = Point3D.Zero;
            if (companion == null || companion.Map == null)
                return false;

            int dx = Math.Sign(goal.X - companion.X);
            int dy = Math.Sign(goal.Y - companion.Y);
            Point3D[] candidates = new Point3D[]
            {
                MakePoint(companion.Map, companion.X + dx, companion.Y - dy, companion.Z),
                MakePoint(companion.Map, companion.X - dx, companion.Y + dy, companion.Z),
                MakePoint(companion.Map, companion.X - dx, companion.Y, companion.Z),
                MakePoint(companion.Map, companion.X, companion.Y - dy, companion.Z)
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                Point3D candidate = candidates[i];
                if (candidate == companion.Location)
                    continue;

                if (memory != null && memory.IsNearFailedPoint(candidate, 2))
                    continue;

                if (CanStandAt(companion.Map, candidate.X, candidate.Y, candidate.Z))
                {
                    sidestepPoint = candidate;
                    return true;
                }
            }

            return false;
        }

        public static bool TryChooseWallFollowPoint(BaseHire companion, Point3D goal, AIGMCompanionTravelObjective objective, out Point3D wallPoint)
        {
            wallPoint = Point3D.Zero;
            if (companion == null || companion.Map == null || objective == null)
                return false;

            Point3D candidate;
            if (TryGetWallFollowContinuationPoint(companion, goal, objective, out candidate))
            {
                wallPoint = candidate;
                return true;
            }

            int goalDx = Math.Sign(goal.X - companion.X);
            int goalDy = Math.Sign(goal.Y - companion.Y);

            int[] lateralSigns = new int[] { -1, 1 };
            for (int s = 0; s < lateralSigns.Length; s++)
            {
                int sign = lateralSigns[s];
                int lateralX = 0;
                int lateralY = 0;
                int forwardX = 0;
                int forwardY = 0;

                if (goalDx != 0 && goalDy != 0)
                {
                    lateralX = -goalDx;
                    forwardY = goalDy;
                }
                else if (goalDx != 0)
                {
                    lateralY = sign;
                    forwardX = goalDx;
                }
                else if (goalDy != 0)
                {
                    lateralX = sign;
                    forwardY = goalDy;
                }
                else
                {
                    continue;
                }

                Point3D[] candidates = new Point3D[]
                {
                    MakePoint(companion.Map, companion.X + lateralX, companion.Y + lateralY, companion.Z),
                    MakePoint(companion.Map, companion.X + lateralX + forwardX, companion.Y + lateralY + forwardY, companion.Z),
                    MakePoint(companion.Map, companion.X + lateralX + lateralX, companion.Y + lateralY + lateralY, companion.Z),
                    MakePoint(companion.Map, companion.X + forwardX, companion.Y + forwardY, companion.Z)
                };

                for (int i = 0; i < candidates.Length; i++)
                {
                    Point3D next = candidates[i];
                    if (next == companion.Location)
                        continue;

                    if (objective.Memory != null && objective.Memory.IsNearFailedPoint(next, 2))
                        continue;

                    if (CanStandAt(companion.Map, next.X, next.Y, next.Z))
                    {
                        objective.WallFollowDirectionX = lateralX != 0 ? lateralX : forwardX;
                        objective.WallFollowDirectionY = lateralY != 0 ? lateralY : forwardY;
                        wallPoint = next;
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool TryGetWallFollowContinuationPoint(BaseHire companion, Point3D goal, AIGMCompanionTravelObjective objective, out Point3D wallPoint)
        {
            wallPoint = Point3D.Zero;
            if (companion == null || companion.Map == null || objective == null)
                return false;

            int forwardX = objective.WallFollowDirectionX;
            int forwardY = objective.WallFollowDirectionY;
            if (forwardX == 0 && forwardY == 0)
                return false;

            int goalDx = Math.Sign(goal.X - companion.X);
            int goalDy = Math.Sign(goal.Y - companion.Y);
            bool closerOnGoalAxis = (goalDx != 0 && goalDx == forwardX) || (goalDy != 0 && goalDy == forwardY);

            Point3D[] candidates = closerOnGoalAxis
                ? new Point3D[]
                {
                    MakePoint(companion.Map, companion.X + forwardX, companion.Y + forwardY, companion.Z),
                    MakePoint(companion.Map, companion.X + forwardX + goalDx, companion.Y + forwardY + goalDy, companion.Z),
                    MakePoint(companion.Map, companion.X + goalDx, companion.Y + goalDy, companion.Z)
                }
                : new Point3D[]
                {
                    MakePoint(companion.Map, companion.X + forwardX, companion.Y + forwardY, companion.Z),
                    MakePoint(companion.Map, companion.X + forwardX + goalDx, companion.Y + forwardY + goalDy, companion.Z),
                    MakePoint(companion.Map, companion.X + goalDx, companion.Y + goalDy, companion.Z)
                };

            for (int i = 0; i < candidates.Length; i++)
            {
                Point3D candidate = candidates[i];
                if (candidate == companion.Location)
                    continue;

                if (objective.Memory != null && objective.Memory.IsNearFailedPoint(candidate, 2))
                    continue;

                if (CanStandAt(companion.Map, candidate.X, candidate.Y, candidate.Z))
                {
                    wallPoint = candidate;
                    return true;
                }
            }

            return false;
        }

        public static Point3D GetDetourBandPoint(BaseHire companion, Point3D goal, AIGMTravelMemory memory, int attempt)
        {
            if (companion == null || companion.Map == null)
                return companion != null ? companion.Location : Point3D.Zero;

            int dx = Math.Sign(goal.X - companion.X);
            int dy = Math.Sign(goal.Y - companion.Y);
            int radius = 8 + Math.Min(8, attempt * 2);

            Point3D[] candidates = new Point3D[]
            {
                MakePoint(companion.Map, companion.X + (dx * radius), companion.Y, companion.Z),
                MakePoint(companion.Map, companion.X, companion.Y + (dy * radius), companion.Z),
                MakePoint(companion.Map, companion.X + (dx * radius), companion.Y + (dy * radius), companion.Z),
                MakePoint(companion.Map, companion.X + (dx * radius), companion.Y - (dy * radius), companion.Z),
                MakePoint(companion.Map, companion.X - (dx * radius), companion.Y + (dy * radius), companion.Z)
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                if (memory != null && memory.IsNearFailedPoint(candidates[i], 3))
                    continue;

                if (CanStandAt(companion.Map, candidates[i].X, candidates[i].Y, candidates[i].Z))
                    return candidates[i];
            }

            return companion.Location;
        }

        public static string GetRouteBand(Point3D from, Point3D to)
        {
            int dx = to.X - from.X;
            int dy = to.Y - from.Y;

            if (Math.Abs(dx) >= Math.Abs(dy))
                return dy >= 0 ? "SouthBypass" : "NorthBypass";

            return dx >= 0 ? "EastBypass" : "WestBypass";
        }

        public static IEnumerable<Point3D> BuildRingCandidates(Point3D center, int radius)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                yield return new Point3D(center.X + dx, center.Y - radius, center.Z);
                yield return new Point3D(center.X + dx, center.Y + radius, center.Z);
            }

            for (int dy = -radius + 1; dy <= radius - 1; dy++)
            {
                yield return new Point3D(center.X - radius, center.Y + dy, center.Z);
                yield return new Point3D(center.X + radius, center.Y + dy, center.Z);
            }
        }

        private static Point3D GetBestCandidateFromPoint(Map map, Point3D origin, int dx, int dy, int z, bool allowSides = false)
        {
            Point3D[] candidates = allowSides
                ? new Point3D[]
                {
                    MakePoint(map, origin.X + dx, origin.Y + dy, z),
                    MakePoint(map, origin.X + dx, origin.Y, z),
                    MakePoint(map, origin.X, origin.Y + dy, z),
                    MakePoint(map, origin.X + dx, origin.Y - dy, z),
                    MakePoint(map, origin.X - dx, origin.Y + dy, z),
                    MakePoint(map, origin.X - dx, origin.Y, z),
                    MakePoint(map, origin.X, origin.Y - dy, z)
                }
                : new Point3D[]
                {
                    MakePoint(map, origin.X + dx, origin.Y + dy, z)
                };

            for (int i = 0; i < candidates.Length; i++)
            {
                Point3D candidate = candidates[i];
                if (candidate != origin && CanStandAt(map, candidate.X, candidate.Y, candidate.Z))
                    return candidate;
            }

            return origin;
        }

        public static Point3D TryFindNearbyStandablePoint(Map map, Point3D origin)
        {
            if (map == null)
                return origin;

            for (int radius = 1; radius <= 2; radius++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        Point3D candidate = MakePoint(map, origin.X + dx, origin.Y + dy, origin.Z);
                        if (CanStandAt(map, candidate.X, candidate.Y, candidate.Z))
                            return candidate;
                    }
                }
            }

            return origin;
        }

        private static Point3D MakePoint(Map map, int x, int y, int z)
        {
            return new Point3D(x, y, GetCorrectedZ(map, x, y, z));
        }
    }
}
