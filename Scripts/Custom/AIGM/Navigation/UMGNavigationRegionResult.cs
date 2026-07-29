using System;

namespace Server.Custom.AIGM
{
    public sealed class UMGNavigationRegionResult
    {
        public bool Succeeded { get; set; }
        public string Reason { get; set; }
        public string Detail { get; set; }
        public string MapName { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public string RegionName { get; set; }
        public string ParentRegionName { get; set; }
        public string RegionKind { get; set; }
        public bool IsTown { get; set; }
        public bool IsDungeon { get; set; }
        public bool IsGuarded { get; set; }
        public DateTime CheckedUtc { get; set; }

        public static UMGNavigationRegionResult Found(string mapName, int x, int y, int z, string regionName, string parentRegionName = null, string regionKind = null, bool isTown = false, bool isDungeon = false, bool isGuarded = false, string reason = null, string detail = null)
        {
            return new UMGNavigationRegionResult
            {
                Succeeded = true,
                Reason = reason,
                Detail = detail,
                MapName = mapName,
                X = x,
                Y = y,
                Z = z,
                RegionName = regionName,
                ParentRegionName = parentRegionName,
                RegionKind = regionKind,
                IsTown = isTown,
                IsDungeon = isDungeon,
                IsGuarded = isGuarded,
                CheckedUtc = DateTime.UtcNow
            };
        }

        public static UMGNavigationRegionResult Unknown(string mapName, int x, int y, int z, string reason = null, string detail = null)
        {
            return new UMGNavigationRegionResult
            {
                Succeeded = true,
                Reason = reason,
                Detail = detail,
                MapName = mapName,
                X = x,
                Y = y,
                Z = z,
                CheckedUtc = DateTime.UtcNow
            };
        }

        public static UMGNavigationRegionResult Failure(string mapName, int x, int y, int z, string reason, string detail = null)
        {
            return new UMGNavigationRegionResult
            {
                Succeeded = false,
                Reason = reason,
                Detail = detail,
                MapName = mapName,
                X = x,
                Y = y,
                Z = z,
                CheckedUtc = DateTime.UtcNow
            };
        }
    }
}
