using System;
using System.Collections.Generic;

namespace Server.Custom.AIGM
{
    public class UMGNavigationNoOpLandmarkRegistry : IUMGNavigationLandmarkRegistry
    {
        public UMGNavigationLandmarkResult Resolve(UMGNavigationLandmarkQuery query)
        {
            if (query == null)
            {
                return UMGNavigationLandmarkResult.Unknown(
                    "landmark_query_null",
                    "No landmark query was provided to the no-op landmark registry.");
            }

            return UMGNavigationLandmarkResult.NoOp(
                "landmark_registry_noop",
                "Landmark resolution is not available in the no-op landmark registry.");
        }

        public UMGNavigationLandmarkResult FindById(string landmarkId)
        {
            if (string.IsNullOrWhiteSpace(landmarkId))
            {
                return UMGNavigationLandmarkResult.Unknown(
                    "landmark_id_missing",
                    "No landmark id was provided to the no-op landmark registry.");
            }

            return UMGNavigationLandmarkResult.NoOp(
                "landmark_registry_noop",
                "Landmark lookup is not available in the no-op landmark registry.");
        }

        public IEnumerable<UMGNavigationLandmark> GetAll()
        {
            return Array.Empty<UMGNavigationLandmark>();
        }

        public IEnumerable<UMGNavigationLandmark> FindByKind(UMGNavigationLandmarkKind kind)
        {
            return Array.Empty<UMGNavigationLandmark>();
        }
    }
}
