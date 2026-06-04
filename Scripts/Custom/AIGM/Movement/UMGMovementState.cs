using System;
using Server;

namespace Server.Custom.AIGM
{
    public sealed class UMGMovementState
    {
        public UMGMovementIntentKind ActiveIntent { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public string DestinationName { get; set; }
        public Point3D DestinationPoint { get; set; }
        public Map DestinationMap { get; set; }
        public int TargetSerial { get; set; }
        public string TargetName { get; set; }
        public string Reason { get; set; }

        public UMGMovementState()
        {
            ActiveIntent = UMGMovementIntentKind.Idle;
            UpdatedUtc = DateTime.UtcNow;
            DestinationPoint = Point3D.Zero;
        }

        public void Apply(UMGMovementIntent intent)
        {
            ActiveIntent = intent != null ? intent.Kind : UMGMovementIntentKind.Idle;
            UpdatedUtc = DateTime.UtcNow;
            DestinationName = intent != null ? intent.DestinationName : null;
            DestinationPoint = intent != null ? intent.DestinationPoint : Point3D.Zero;
            DestinationMap = intent != null ? intent.DestinationMap : null;
            TargetSerial = intent != null ? intent.TargetSerial : 0;
            TargetName = intent != null ? intent.TargetName : null;
            Reason = intent != null ? intent.Reason : null;
        }
    }
}
