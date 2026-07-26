using System;

namespace Server.Custom.AIGM
{
    public sealed class UMGNavigationSnapshotRequest
    {
        public string RequestId { get; set; }
        public string Source { get; set; }
        public string Reason { get; set; }
        public string ActorId { get; set; }
        public string ActorProfileKey { get; set; }
        public string FacetName { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public bool IncludeProbe { get; set; }
        public bool IncludeAreaInfo { get; set; }
        public bool IncludeNearbyEntities { get; set; }
        public int NearbyRange { get; set; }
        public int MaxNearbyResults { get; set; }
        public DateTime CreatedUtc { get; set; }

        public UMGNavigationSnapshotRequest()
        {
            CreatedUtc = DateTime.UtcNow;
            IncludeProbe = true;
            IncludeAreaInfo = true;
            IncludeNearbyEntities = false;
            NearbyRange = 0;
            MaxNearbyResults = 0;
        }

        public static UMGNavigationSnapshotRequest FromPosition(string facetName, int x, int y, int z, string actorId = null, string actorProfileKey = null, string source = null, string reason = null)
        {
            return new UMGNavigationSnapshotRequest
            {
                FacetName = facetName,
                X = x,
                Y = y,
                Z = z,
                ActorId = actorId,
                ActorProfileKey = actorProfileKey,
                Source = source,
                Reason = reason
            };
        }
    }
}
