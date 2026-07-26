using System;
using System.Collections.Generic;

namespace Server.Custom.AIGM
{
    public class UMGServUONavigationAdapter : IUMGNavigationAdapter
    {
        public UMGNavigationProbeResult ProbePoint(string mapName, int x, int y, int z, string reason = null)
        {
            if (string.IsNullOrWhiteSpace(mapName))
            {
                return UMGNavigationProbeResult.Failure(mapName, x, y, z, "map_name_missing", "ServUO navigation adapter requires a map name for point probes.");
            }

            Map map;
            if (!TryResolveMap(mapName, out map))
            {
                return UMGNavigationProbeResult.Failure(mapName, x, y, z, "map_unknown", "ServUO navigation adapter could not resolve the requested map.");
            }

            try
            {
                if (!IsPointWithinBounds(map, x, y))
                {
                    return UMGNavigationProbeResult.Failure(mapName, x, y, z, "point_probe_failed", "Point is outside map bounds.");
                }

                bool canFit = map.CanFit(x, y, z, 16, false, true, true);

                if (canFit)
                {
                    return UMGNavigationProbeResult.Passable(map.Name, x, y, z, reason ?? "point_probe_passable", "ServUO Map.CanFit returned passable for the requested point.");
                }

                return UMGNavigationProbeResult.Blocked(map.Name, x, y, z, reason ?? "point_probe_blocked", "ServUO Map.CanFit returned blocked for the requested point.");
            }
            catch (Exception ex)
            {
                return UMGNavigationProbeResult.Failure(mapName, x, y, z, "point_probe_failed", ex.Message);
            }
        }

        public UMGNavigationRegionResult GetRegionAt(string mapName, int x, int y, int z)
        {
            if (string.IsNullOrWhiteSpace(mapName))
            {
                return UMGNavigationRegionResult.Failure(mapName, x, y, z, "map_name_missing", "ServUO navigation adapter requires a map name for region lookup.");
            }

            Map map;
            if (!TryResolveMap(mapName, out map))
            {
                return UMGNavigationRegionResult.Failure(mapName, x, y, z, "map_unknown", "ServUO navigation adapter could not resolve the requested map.");
            }

            try
            {
                if (!IsPointWithinBounds(map, x, y))
                {
                    return UMGNavigationRegionResult.Failure(mapName, x, y, z, "region_lookup_failed", "Point is outside map bounds.");
                }

                Point3D point = new Point3D(x, y, z);
                Region region = Region.Find(point, map);

                if (region == null)
                {
                    return UMGNavigationRegionResult.Unknown(map.Name, x, y, z, "region_unknown", "ServUO Region.Find returned no region.");
                }

                string regionName = region.Name;
                string parentRegionName = region.Parent != null ? region.Parent.Name : null;
                string regionKind = region.GetType().Name;

                if (string.IsNullOrWhiteSpace(regionName) && string.IsNullOrWhiteSpace(regionKind))
                {
                    return UMGNavigationRegionResult.Unknown(map.Name, x, y, z, "region_unknown", "Resolved region did not expose a stable name or kind.");
                }

                return UMGNavigationRegionResult.Found(
                    map.Name,
                    x,
                    y,
                    z,
                    regionName,
                    parentRegionName,
                    regionKind,
                    false,
                    false,
                    false,
                    "region_found",
                    "ServUO Region.Find resolved a region for the requested point.");
            }
            catch (Exception ex)
            {
                return UMGNavigationRegionResult.Failure(mapName, x, y, z, "region_lookup_failed", ex.Message);
            }
        }

        public UMGNavigationProbeResult CheckRange(string mapName, int fromX, int fromY, int fromZ, int toX, int toY, int toZ, int maxRange)
        {
            if (string.IsNullOrWhiteSpace(mapName))
            {
                return UMGNavigationProbeResult.Failure(mapName, fromX, fromY, fromZ, "map_name_missing", "ServUO navigation adapter requires a map name for range checks.");
            }

            if (maxRange < 0)
            {
                return UMGNavigationProbeResult.Failure(mapName, fromX, fromY, fromZ, "range_invalid", "ServUO navigation adapter received an invalid range value.");
            }

            return UMGNavigationProbeResult.OutOfRange(mapName, toX, toY, toZ, "range_deferred", "Range evaluation is deferred in this adapter slice.");
        }

        public UMGNavigationProbeResult CheckLineOfSight(string mapName, int fromX, int fromY, int fromZ, int toX, int toY, int toZ)
        {
            if (string.IsNullOrWhiteSpace(mapName))
            {
                return UMGNavigationProbeResult.Failure(mapName, fromX, fromY, fromZ, "map_name_missing", "ServUO navigation adapter requires a map name for line-of-sight checks.");
            }

            return UMGNavigationProbeResult.NoLineOfSight(mapName, toX, toY, toZ, "los_deferred", "Line-of-sight evaluation is deferred in this adapter slice.");
        }

        public IEnumerable<UMGNavigationEntitySnapshot> GetNearbyEntities(string mapName, int x, int y, int z, int range, int maxResults)
        {
            return Array.Empty<UMGNavigationEntitySnapshot>();
        }

        private static bool TryResolveMap(string mapName, out Map map)
        {
            map = null;

            if (string.IsNullOrWhiteSpace(mapName))
            {
                return false;
            }

            string normalized = mapName.Trim();

            if (string.Equals(normalized, "Felucca", StringComparison.OrdinalIgnoreCase))
            {
                map = Map.Felucca;
            }
            else if (string.Equals(normalized, "Trammel", StringComparison.OrdinalIgnoreCase))
            {
                map = Map.Trammel;
            }
            else if (string.Equals(normalized, "Ilshenar", StringComparison.OrdinalIgnoreCase))
            {
                map = Map.Ilshenar;
            }
            else if (string.Equals(normalized, "Malas", StringComparison.OrdinalIgnoreCase))
            {
                map = Map.Malas;
            }
            else if (string.Equals(normalized, "Tokuno", StringComparison.OrdinalIgnoreCase))
            {
                map = Map.Tokuno;
            }
            else if (string.Equals(normalized, "TerMur", StringComparison.OrdinalIgnoreCase) || string.Equals(normalized, "Ter Mur", StringComparison.OrdinalIgnoreCase))
            {
                map = Map.TerMur;
            }
            else if (string.Equals(normalized, "Internal", StringComparison.OrdinalIgnoreCase))
            {
                map = Map.Internal;
            }

            return map != null;
        }

        private static bool IsPointWithinBounds(Map map, int x, int y)
        {
            if (map == null || map == Map.Internal)
            {
                return false;
            }

            return x >= 0 && y >= 0 && x < map.Width && y < map.Height;
        }
    }
}
