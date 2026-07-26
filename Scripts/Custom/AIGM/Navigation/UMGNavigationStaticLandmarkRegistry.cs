using System;
using System.Collections.Generic;

namespace Server.Custom.AIGM
{
    public class UMGNavigationStaticLandmarkRegistry : IUMGNavigationLandmarkRegistry
    {
        private static readonly UMGNavigationLandmark[] Landmarks = Array.Empty<UMGNavigationLandmark>();

        public UMGNavigationLandmarkResult Resolve(UMGNavigationLandmarkQuery query)
        {
            if (query == null)
            {
                return UMGNavigationLandmarkResult.Unknown(
                    "landmark_query_null",
                    "No landmark query was provided to the static landmark registry.");
            }

            UMGNavigationLandmark match = FindMatch(query);

            if (match != null)
            {
                return UMGNavigationLandmarkResult.Found(
                    match,
                    "landmark_found",
                    "A matching landmark was found in the static landmark registry.");
            }

            return UMGNavigationLandmarkResult.NoOp(
                "landmark_static_registry_empty",
                "The static landmark registry does not contain seed data yet.");
        }

        public UMGNavigationLandmarkResult FindById(string landmarkId)
        {
            if (string.IsNullOrWhiteSpace(landmarkId))
            {
                return UMGNavigationLandmarkResult.Unknown(
                    "landmark_id_missing",
                    "No landmark id was provided to the static landmark registry.");
            }

            UMGNavigationLandmark match = FindByLandmarkId(landmarkId);

            if (match != null)
            {
                return UMGNavigationLandmarkResult.Found(
                    match,
                    "landmark_found",
                    "A matching landmark id was found in the static landmark registry.");
            }

            return UMGNavigationLandmarkResult.Unknown(
                "landmark_not_found",
                "The requested landmark id was not found in the static landmark registry.");
        }

        public IEnumerable<UMGNavigationLandmark> GetAll()
        {
            return Landmarks;
        }

        public IEnumerable<UMGNavigationLandmark> FindByKind(UMGNavigationLandmarkKind kind)
        {
            if (Landmarks.Length == 0)
            {
                return Landmarks;
            }

            List<UMGNavigationLandmark> matches = new List<UMGNavigationLandmark>();

            for (int i = 0; i < Landmarks.Length; i++)
            {
                UMGNavigationLandmark landmark = Landmarks[i];

                if (landmark != null && landmark.Kind == kind)
                {
                    matches.Add(landmark);
                }
            }

            return matches;
        }

        private static UMGNavigationLandmark FindMatch(UMGNavigationLandmarkQuery query)
        {
            if (query == null || Landmarks.Length == 0)
            {
                return null;
            }

            UMGNavigationLandmark byText = FindBySearchText(query.SearchText, query.FacetName, query.PreferredKind);

            if (byText != null)
            {
                return byText;
            }

            if (query.HasPosition)
            {
                return FindNearest(query.FacetName, query.X, query.Y, query.Z, query.MaxDistance, query.PreferredKind);
            }

            return null;
        }

        private static UMGNavigationLandmark FindByLandmarkId(string landmarkId)
        {
            if (string.IsNullOrWhiteSpace(landmarkId))
            {
                return null;
            }

            for (int i = 0; i < Landmarks.Length; i++)
            {
                UMGNavigationLandmark landmark = Landmarks[i];

                if (landmark != null && StringEquals(landmark.LandmarkId, landmarkId))
                {
                    return landmark;
                }
            }

            return null;
        }

        private static UMGNavigationLandmark FindBySearchText(string searchText, string facetName, UMGNavigationLandmarkKind preferredKind)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return null;
            }

            for (int i = 0; i < Landmarks.Length; i++)
            {
                UMGNavigationLandmark landmark = Landmarks[i];

                if (landmark == null || !MatchesFacet(landmark, facetName) || !MatchesPreferredKind(landmark, preferredKind))
                {
                    continue;
                }

                if (StringEquals(landmark.LandmarkId, searchText)
                    || StringEquals(landmark.DisplayName, searchText)
                    || ContainsAlias(landmark, searchText))
                {
                    return landmark;
                }
            }

            return null;
        }

        private static UMGNavigationLandmark FindNearest(string facetName, int x, int y, int z, int maxDistance, UMGNavigationLandmarkKind preferredKind)
        {
            UMGNavigationLandmark nearest = null;
            int nearestDistance = Int32.MaxValue;

            for (int i = 0; i < Landmarks.Length; i++)
            {
                UMGNavigationLandmark landmark = Landmarks[i];

                if (landmark == null || !MatchesFacet(landmark, facetName) || !MatchesPreferredKind(landmark, preferredKind))
                {
                    continue;
                }

                int distance = GetDistanceSquared(landmark, x, y, z);

                if (maxDistance >= 0 && distance > (maxDistance * maxDistance))
                {
                    continue;
                }

                if (distance < nearestDistance)
                {
                    nearest = landmark;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        private static bool MatchesFacet(UMGNavigationLandmark landmark, string facetName)
        {
            return string.IsNullOrWhiteSpace(facetName)
                || string.IsNullOrWhiteSpace(landmark.FacetName)
                || StringEquals(landmark.FacetName, facetName);
        }

        private static bool MatchesPreferredKind(UMGNavigationLandmark landmark, UMGNavigationLandmarkKind preferredKind)
        {
            return preferredKind == UMGNavigationLandmarkKind.Unknown || landmark.Kind == preferredKind;
        }

        private static bool ContainsAlias(UMGNavigationLandmark landmark, string searchText)
        {
            if (landmark == null || landmark.Aliases == null || string.IsNullOrWhiteSpace(searchText))
            {
                return false;
            }

            for (int i = 0; i < landmark.Aliases.Count; i++)
            {
                if (StringEquals(landmark.Aliases[i], searchText))
                {
                    return true;
                }
            }

            return false;
        }

        private static int GetDistanceSquared(UMGNavigationLandmark landmark, int x, int y, int z)
        {
            int dx = landmark.X - x;
            int dy = landmark.Y - y;
            int dz = landmark.Z - z;

            return (dx * dx) + (dy * dy) + (dz * dz);
        }

        private static bool StringEquals(string left, string right)
        {
            return String.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }
    }
}
