using System;
using System.Collections.Generic;

namespace Server.Custom.AIGM
{
    public sealed class UMGNavigationSnapshot
    {
        public string SnapshotId { get; set; }
        public string Source { get; set; }
        public string Reason { get; set; }
        public string Detail { get; set; }
        public string ActorId { get; set; }
        public string ActorProfileKey { get; set; }
        public string MapName { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public DateTime CreatedUtc { get; set; }
        public UMGNavigationProbeResult PositionProbe { get; set; }
        public UMGNavigationRegionResult Region { get; set; }
        public List<UMGNavigationEntitySnapshot> NearbyEntities { get; set; }
        public bool HasNearestLandmark { get; set; }
        public string NearestLandmarkId { get; set; }
        public string NearestLandmarkName { get; set; }
        public string NearestLandmarkKind { get; set; }
        public int DistanceToNearestLandmark { get; set; }
        public bool HasDestination { get; set; }
        public string DestinationId { get; set; }
        public string DestinationName { get; set; }
        public string DestinationKind { get; set; }
        public string DestinationMapName { get; set; }
        public int DestinationX { get; set; }
        public int DestinationY { get; set; }
        public int DestinationZ { get; set; }
        public int DistanceToDestination { get; set; }
        public bool Succeeded { get; set; }
        public bool IsUnknown { get; set; }

        public UMGNavigationSnapshot()
        {
            CreatedUtc = DateTime.UtcNow;
            NearbyEntities = new List<UMGNavigationEntitySnapshot>();
            DistanceToNearestLandmark = -1;
            DistanceToDestination = -1;
            IsUnknown = true;
        }

        public static UMGNavigationSnapshot Unknown(string reason = null, string detail = null)
        {
            return new UMGNavigationSnapshot
            {
                Succeeded = false,
                IsUnknown = true,
                Reason = reason,
                Detail = detail
            };
        }

        public static UMGNavigationSnapshot FromPosition(string mapName, int x, int y, int z, string actorId = null, string actorProfileKey = null, string source = null, string reason = null)
        {
            return new UMGNavigationSnapshot
            {
                Succeeded = true,
                IsUnknown = false,
                MapName = mapName,
                X = x,
                Y = y,
                Z = z,
                ActorId = actorId,
                ActorProfileKey = actorProfileKey,
                Source = source,
                Reason = reason
            };
        }

        public static UMGNavigationSnapshot WithRegion(UMGNavigationSnapshot snapshot, UMGNavigationRegionResult region)
        {
            if (snapshot == null)
            {
                snapshot = new UMGNavigationSnapshot();
            }

            snapshot.Region = region;
            return snapshot;
        }

        public static UMGNavigationSnapshot WithProbe(UMGNavigationSnapshot snapshot, UMGNavigationProbeResult positionProbe)
        {
            if (snapshot == null)
            {
                snapshot = new UMGNavigationSnapshot();
            }

            snapshot.PositionProbe = positionProbe;
            return snapshot;
        }
    }
}
