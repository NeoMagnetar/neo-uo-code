using System;

namespace Server.Custom.AIGM
{
    public sealed class UMGNavigationSnapshotResult
    {
        public bool Succeeded { get; set; }
        public bool WasNoOp { get; set; }
        public bool WasDenied { get; set; }
        public string Reason { get; set; }
        public string Detail { get; set; }
        public UMGNavigationSnapshot Snapshot { get; set; }
        public DateTime CompletedUtc { get; set; }

        public UMGNavigationSnapshotResult()
        {
            CompletedUtc = DateTime.UtcNow;
        }

        public static UMGNavigationSnapshotResult Success(UMGNavigationSnapshot snapshot, string reason = null, string detail = null)
        {
            return new UMGNavigationSnapshotResult
            {
                Succeeded = true,
                Snapshot = snapshot,
                Reason = reason,
                Detail = detail
            };
        }

        public static UMGNavigationSnapshotResult Failure(string reason, string detail = null, UMGNavigationSnapshot snapshot = null)
        {
            return new UMGNavigationSnapshotResult
            {
                Succeeded = false,
                Snapshot = snapshot,
                Reason = reason,
                Detail = detail
            };
        }

        public static UMGNavigationSnapshotResult Denied(string reason, string detail = null, UMGNavigationSnapshot snapshot = null)
        {
            return new UMGNavigationSnapshotResult
            {
                Succeeded = false,
                WasDenied = true,
                Snapshot = snapshot,
                Reason = reason,
                Detail = detail
            };
        }

        public static UMGNavigationSnapshotResult NoOp(string reason = null, string detail = null, UMGNavigationSnapshot snapshot = null)
        {
            return new UMGNavigationSnapshotResult
            {
                Succeeded = false,
                WasNoOp = true,
                Snapshot = snapshot,
                Reason = reason,
                Detail = detail
            };
        }
    }
}
