using System.Collections.Generic;

namespace Server.Custom.AIGM
{
    public sealed class UMGNavigationNoOpAdapter : IUMGNavigationAdapter
    {
        public UMGNavigationProbeResult ProbePoint(string mapName, int x, int y, int z, string reason = null)
        {
            if (string.IsNullOrWhiteSpace(mapName))
            {
                return UMGNavigationProbeResult.Failure(mapName, x, y, z, "map_name_missing", "No-op navigation adapter requires a map name for point probes.");
            }

            return UMGNavigationProbeResult.Blocked(
                mapName,
                x,
                y,
                z,
                reason ?? "navigation_noop",
                "No-op navigation adapter does not evaluate point passability.");
        }

        public UMGNavigationRegionResult GetRegionAt(string mapName, int x, int y, int z)
        {
            if (string.IsNullOrWhiteSpace(mapName))
            {
                return UMGNavigationRegionResult.Failure(mapName, x, y, z, "map_name_missing", "No-op navigation adapter requires a map name for region lookup.");
            }

            return UMGNavigationRegionResult.Unknown(
                mapName,
                x,
                y,
                z,
                "region_unknown_noop",
                "No-op navigation adapter does not resolve ServUO regions.");
        }

        public UMGNavigationProbeResult CheckRange(string mapName, int fromX, int fromY, int fromZ, int toX, int toY, int toZ, int maxRange)
        {
            if (string.IsNullOrWhiteSpace(mapName))
            {
                return UMGNavigationProbeResult.Failure(mapName, fromX, fromY, fromZ, "map_name_missing", "No-op navigation adapter requires a map name for range checks.");
            }

            if (maxRange < 0)
            {
                return UMGNavigationProbeResult.Failure(mapName, fromX, fromY, fromZ, "range_invalid", "No-op navigation adapter received an invalid range value.");
            }

            return UMGNavigationProbeResult.OutOfRange(
                mapName,
                toX,
                toY,
                toZ,
                "range_unknown_noop",
                "No-op navigation adapter does not evaluate range.");
        }

        public UMGNavigationProbeResult CheckLineOfSight(string mapName, int fromX, int fromY, int fromZ, int toX, int toY, int toZ)
        {
            if (string.IsNullOrWhiteSpace(mapName))
            {
                return UMGNavigationProbeResult.Failure(mapName, fromX, fromY, fromZ, "map_name_missing", "No-op navigation adapter requires a map name for line-of-sight checks.");
            }

            return UMGNavigationProbeResult.NoLineOfSight(
                mapName,
                toX,
                toY,
                toZ,
                "los_unknown_noop",
                "No-op navigation adapter does not evaluate line of sight.");
        }

        public IEnumerable<UMGNavigationEntitySnapshot> GetNearbyEntities(string mapName, int x, int y, int z, int range, int maxResults)
        {
            return System.Array.Empty<UMGNavigationEntitySnapshot>();
        }
    }
}
