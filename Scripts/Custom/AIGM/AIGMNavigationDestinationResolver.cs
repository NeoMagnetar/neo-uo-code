using System;

using Server;

namespace Server.Custom.AIGM
{
    public sealed class AIGMNavigationDestinationResolution
    {
        public AIGMNavigationLocation Location;
        public Point3D Canonical;
        public Point3D Target;
        public int ArrivalRadius;
        public bool IsStandable;
        public bool UsedFallback;
        public string Reason;
    }

    public static class AIGMNavigationDestinationResolver
    {
        private const int DefaultSearchRadius = 6;

        public static AIGMNavigationDestinationResolution Resolve(AIGMNavigationLocation location, Point3D from, Map map)
        {
            AIGMNavigationDestinationResolution result = new AIGMNavigationDestinationResolution();
            result.Location = location;
            result.Canonical = location != null ? location.Point : Point3D.Zero;
            result.Target = result.Canonical;
            result.ArrivalRadius = GetArrivalRadius(location);
            result.IsStandable = false;
            result.UsedFallback = false;
            result.Reason = "invalid";

            if (location == null || map == null)
                return result;

            Point3D normalized = Normalize(map, location.Point);
            if (CanStand(map, normalized))
            {
                result.Target = normalized;
                result.IsStandable = true;
                result.Reason = "canonical_standable";
                return result;
            }

            Point3D best = Point3D.Zero;
            int bestScore = Int32.MaxValue;
            int search = Math.Max(DefaultSearchRadius, result.ArrivalRadius);
            for (int radius = 1; radius <= search; radius++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != radius)
                            continue;

                        Point3D candidate = Normalize(map, new Point3D(location.Point.X + dx, location.Point.Y + dy, location.Point.Z));
                        if (!CanStand(map, candidate))
                            continue;

                        int score = AIGMNavigationLocationRegistry.GetDistance(candidate, location.Point) * 10
                            + AIGMNavigationLocationRegistry.GetDistance(candidate, from);
                        if (score < bestScore)
                        {
                            best = candidate;
                            bestScore = score;
                        }
                    }
                }

                if (best != Point3D.Zero)
                    break;
            }

            if (best != Point3D.Zero)
            {
                result.Target = best;
                result.IsStandable = true;
                result.UsedFallback = true;
                result.Reason = "nearest_standable";
                return result;
            }

            result.Target = normalized;
            result.Reason = "no_standable_tile";
            return result;
        }

        public static int GetArrivalRadius(AIGMNavigationLocation location)
        {
            if (location == null || String.IsNullOrWhiteSpace(location.Category))
                return 3;

            string category = location.Category.Trim().ToLowerInvariant();
            if (category == "town")
                return 6;
            if (category == "road" || category == "roadsign" || category == "landmark" || category == "cemetery" || category == "canyon")
                return 4;
            if (category == "bank" || category == "house" || category == "dungeonentrance" || category == "moongate")
                return 3;

            return 4;
        }

        private static Point3D Normalize(Map map, Point3D p)
        {
            if (map == null)
                return p;

            int z = p.Z;
            if (!map.CanFit(p.X, p.Y, z, 16, false, false))
                z = map.GetAverageZ(p.X, p.Y);

            return new Point3D(p.X, p.Y, z);
        }

        private static bool CanStand(Map map, Point3D p)
        {
            return map != null && map.CanFit(p.X, p.Y, p.Z, 16, false, false);
        }
    }
}
