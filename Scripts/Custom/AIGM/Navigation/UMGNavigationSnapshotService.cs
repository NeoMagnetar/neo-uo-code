using System;

namespace Server.Custom.AIGM
{
    public class UMGNavigationSnapshotService : IUMGNavigationSnapshotService
    {
        private readonly IUMGNavigationAdapter _adapter;

        public UMGNavigationSnapshotService()
            : this(null)
        {
        }

        public UMGNavigationSnapshotService(IUMGNavigationAdapter adapter)
        {
            _adapter = adapter ?? new UMGNavigationNoOpAdapter();
        }

        public UMGNavigationSnapshotResult CreateSnapshot(UMGNavigationSnapshotRequest request)
        {
            if (request == null)
            {
                return UMGNavigationSnapshotResult.Failure(
                    "snapshot_request_null",
                    "No navigation snapshot request was provided.");
            }

            if (string.IsNullOrWhiteSpace(request.FacetName))
            {
                return UMGNavigationSnapshotResult.Failure(
                    "facet_name_missing",
                    "No facet name was provided for navigation snapshot composition.",
                    UMGNavigationSnapshot.Unknown("facet_name_missing", "No facet name was provided for navigation snapshot composition."));
            }

            UMGNavigationSnapshot snapshot = null;

            try
            {
                snapshot = UMGNavigationSnapshot.FromPosition(
                    request.FacetName,
                    request.X,
                    request.Y,
                    request.Z,
                    request.ActorId,
                    request.ActorProfileKey,
                    request.Source,
                    request.Reason);

                if (request.IncludeProbe)
                {
                    snapshot = UMGNavigationSnapshot.WithProbe(
                        snapshot,
                        _adapter.ProbePoint(request.FacetName, request.X, request.Y, request.Z, request.Reason));
                }

                if (request.IncludeAreaInfo)
                {
                    snapshot = UMGNavigationSnapshot.WithRegion(
                        snapshot,
                        _adapter.GetRegionAt(request.FacetName, request.X, request.Y, request.Z));
                }

                if (request.IncludeNearbyEntities)
                {
                    snapshot.Detail = AppendDetail(snapshot.Detail, "Nearby entity composition is deferred in this service slice.");
                }

                return UMGNavigationSnapshotResult.Success(
                    snapshot,
                    "navigation_snapshot_region_probe_composed",
                    snapshot.Detail);
            }
            catch (Exception ex)
            {
                return UMGNavigationSnapshotResult.Failure(
                    "navigation_snapshot_composition_failed",
                    ex.Message,
                    snapshot ?? UMGNavigationSnapshot.Unknown("navigation_snapshot_composition_failed", ex.Message));
            }
        }

        private static string AppendDetail(string currentDetail, string additionalDetail)
        {
            if (string.IsNullOrWhiteSpace(additionalDetail))
            {
                return currentDetail;
            }

            if (string.IsNullOrWhiteSpace(currentDetail))
            {
                return additionalDetail;
            }

            return currentDetail + " " + additionalDetail;
        }
    }
}
