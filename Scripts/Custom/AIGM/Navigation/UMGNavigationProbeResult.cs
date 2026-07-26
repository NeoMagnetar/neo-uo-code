using System;

namespace Server.Custom.AIGM
{
    public sealed class UMGNavigationProbeResult
    {
        public bool Succeeded { get; set; }
        public bool IsPassable { get; set; }
        public bool IsBlocked { get; set; }
        public bool IsInRange { get; set; }
        public bool HasLineOfSight { get; set; }
        public string Reason { get; set; }
        public string Detail { get; set; }
        public string MapName { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public DateTime CheckedUtc { get; set; }

        public static UMGNavigationProbeResult Passable(string mapName, int x, int y, int z, string reason = null, string detail = null)
        {
            return new UMGNavigationProbeResult
            {
                Succeeded = true,
                IsPassable = true,
                IsBlocked = false,
                Reason = reason,
                Detail = detail,
                MapName = mapName,
                X = x,
                Y = y,
                Z = z,
                CheckedUtc = DateTime.UtcNow
            };
        }

        public static UMGNavigationProbeResult Blocked(string mapName, int x, int y, int z, string reason = null, string detail = null)
        {
            return new UMGNavigationProbeResult
            {
                Succeeded = true,
                IsPassable = false,
                IsBlocked = true,
                Reason = reason,
                Detail = detail,
                MapName = mapName,
                X = x,
                Y = y,
                Z = z,
                CheckedUtc = DateTime.UtcNow
            };
        }

        public static UMGNavigationProbeResult InRange(string mapName, int x, int y, int z, string reason = null, string detail = null)
        {
            return new UMGNavigationProbeResult
            {
                Succeeded = true,
                IsInRange = true,
                Reason = reason,
                Detail = detail,
                MapName = mapName,
                X = x,
                Y = y,
                Z = z,
                CheckedUtc = DateTime.UtcNow
            };
        }

        public static UMGNavigationProbeResult OutOfRange(string mapName, int x, int y, int z, string reason = null, string detail = null)
        {
            return new UMGNavigationProbeResult
            {
                Succeeded = true,
                IsInRange = false,
                Reason = reason,
                Detail = detail,
                MapName = mapName,
                X = x,
                Y = y,
                Z = z,
                CheckedUtc = DateTime.UtcNow
            };
        }

        public static UMGNavigationProbeResult LineOfSight(string mapName, int x, int y, int z, string reason = null, string detail = null)
        {
            return new UMGNavigationProbeResult
            {
                Succeeded = true,
                HasLineOfSight = true,
                Reason = reason,
                Detail = detail,
                MapName = mapName,
                X = x,
                Y = y,
                Z = z,
                CheckedUtc = DateTime.UtcNow
            };
        }

        public static UMGNavigationProbeResult NoLineOfSight(string mapName, int x, int y, int z, string reason = null, string detail = null)
        {
            return new UMGNavigationProbeResult
            {
                Succeeded = true,
                HasLineOfSight = false,
                Reason = reason,
                Detail = detail,
                MapName = mapName,
                X = x,
                Y = y,
                Z = z,
                CheckedUtc = DateTime.UtcNow
            };
        }

        public static UMGNavigationProbeResult Failure(string mapName, int x, int y, int z, string reason, string detail = null)
        {
            return new UMGNavigationProbeResult
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
