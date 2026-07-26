using System;

namespace Server.Custom.AIGM
{
    public sealed class UMGNavigationLandmarkQuery
    {
        public string QueryId { get; set; }
        public string Source { get; set; }
        public string Reason { get; set; }
        public string SearchText { get; set; }
        public string FacetName { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public bool HasPosition { get; set; }
        public UMGNavigationLandmarkKind PreferredKind { get; set; }
        public int MaxDistance { get; set; }
        public int MaxResults { get; set; }
        public DateTime CreatedUtc { get; set; }

        public UMGNavigationLandmarkQuery()
        {
            CreatedUtc = DateTime.UtcNow;
            PreferredKind = UMGNavigationLandmarkKind.Unknown;
            MaxDistance = -1;
            MaxResults = 1;
        }

        public static UMGNavigationLandmarkQuery ByText(string searchText, string facetName = null, string source = null, string reason = null)
        {
            return new UMGNavigationLandmarkQuery
            {
                SearchText = searchText,
                FacetName = facetName,
                Source = source,
                Reason = reason
            };
        }

        public static UMGNavigationLandmarkQuery NearPosition(string facetName, int x, int y, int z, int maxDistance = -1, string source = null, string reason = null)
        {
            return new UMGNavigationLandmarkQuery
            {
                FacetName = facetName,
                X = x,
                Y = y,
                Z = z,
                HasPosition = true,
                MaxDistance = maxDistance,
                Source = source,
                Reason = reason
            };
        }
    }
}
