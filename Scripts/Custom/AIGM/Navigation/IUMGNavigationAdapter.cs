using System.Collections.Generic;

namespace Server.Custom.AIGM
{
    public interface IUMGNavigationAdapter
    {
        UMGNavigationProbeResult ProbePoint(string mapName, int x, int y, int z, string reason = null);
        UMGNavigationRegionResult GetRegionAt(string mapName, int x, int y, int z);
        UMGNavigationProbeResult CheckRange(string mapName, int fromX, int fromY, int fromZ, int toX, int toY, int toZ, int maxRange);
        UMGNavigationProbeResult CheckLineOfSight(string mapName, int fromX, int fromY, int fromZ, int toX, int toY, int toZ);
        IEnumerable<UMGNavigationEntitySnapshot> GetNearbyEntities(string mapName, int x, int y, int z, int range, int maxResults);
    }
}
