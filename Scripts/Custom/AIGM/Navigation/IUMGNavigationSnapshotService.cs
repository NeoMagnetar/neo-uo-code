namespace Server.Custom.AIGM
{
    public interface IUMGNavigationSnapshotService
    {
        UMGNavigationSnapshotResult CreateSnapshot(UMGNavigationSnapshotRequest request);
    }
}
