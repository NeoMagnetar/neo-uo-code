using System;
using System.Collections.Generic;

namespace Server.Custom.AIGM
{
    public sealed class UMGNavigationLandmark
    {
        public string LandmarkId { get; set; }
        public string DisplayName { get; set; }
        public UMGNavigationLandmarkKind Kind { get; set; }
        public string FacetName { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public string AreaName { get; set; }
        public string AreaKey { get; set; }
        public bool IsRouteNode { get; set; }
        public bool IsSafePoint { get; set; }
        public bool IsDangerPoint { get; set; }
        public int RiskLevel { get; set; }
        public string Source { get; set; }
        public string Reason { get; set; }
        public string Detail { get; set; }
        public DateTime CreatedUtc { get; set; }
        public List<string> Aliases { get; set; }
        public List<string> Tags { get; set; }

        public UMGNavigationLandmark()
        {
            CreatedUtc = DateTime.UtcNow;
            Kind = UMGNavigationLandmarkKind.Unknown;
            RiskLevel = 0;
            Aliases = new List<string>();
            Tags = new List<string>();
        }

        public static UMGNavigationLandmark Unknown(string reason = null, string detail = null)
        {
            return new UMGNavigationLandmark
            {
                Reason = reason,
                Detail = detail
            };
        }

        public static UMGNavigationLandmark Create(string landmarkId, string displayName, UMGNavigationLandmarkKind kind, string facetName, int x, int y, int z, string areaName = null)
        {
            return new UMGNavigationLandmark
            {
                LandmarkId = landmarkId,
                DisplayName = displayName,
                Kind = kind,
                FacetName = facetName,
                X = x,
                Y = y,
                Z = z,
                AreaName = areaName
            };
        }
    }
}
