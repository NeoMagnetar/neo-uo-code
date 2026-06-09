namespace Server.Custom.AIGM
{
    public class UMGNavigationNoOpSnapshotService : IUMGNavigationSnapshotService
    {
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
                    "No facet name was provided for the no-op navigation snapshot service.",
                    UMGNavigationSnapshot.Unknown("facet_name_missing", "No facet name was provided for the no-op navigation snapshot service."));
            }

            UMGNavigationSnapshot snapshot = UMGNavigationSnapshot.FromPosition(
                request.FacetName,
                request.X,
                request.Y,
                request.Z,
                request.ActorId,
                request.ActorProfileKey,
                request.Source,
                request.Reason);

            return UMGNavigationSnapshotResult.NoOp(
                "navigation_snapshot_noop",
                "No-op navigation snapshot service does not query navigation adapters or ServUO.",
                snapshot);
        }
    }
}
