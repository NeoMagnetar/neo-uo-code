using Server;

namespace Server.Custom.AIGM
{
    public sealed class UMGMovementIntent
    {
        public UMGMovementIntentKind Kind { get; set; }
        public Mobile Requester { get; set; }
        public Mobile TargetMobile { get; set; }
        public string DestinationName { get; set; }
        public Point3D DestinationPoint { get; set; }
        public Map DestinationMap { get; set; }
        public int TargetSerial { get; set; }
        public string TargetName { get; set; }
        public string Reason { get; set; }

        public static UMGMovementIntent Create(UMGMovementIntentKind kind)
        {
            return new UMGMovementIntent { Kind = kind };
        }
    }
}
