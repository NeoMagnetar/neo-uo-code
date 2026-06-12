using System;
using Server;

namespace Server.Custom.AIGM
{
    public sealed class AIGMCompanionTrackingState
    {
        public int CompanionSerial { get; set; }
        public int OwnerSerial { get; set; }
        public AIGMCompanionTrackingMode Mode { get; set; }
        public bool IsActive { get; set; }
        public DateTime StartedUtc { get; set; }
        public DateTime LastScanUtc { get; set; }
        public string LastReport { get; set; }
        public string LastConfidence { get; set; }
        public string LastKnownTargetDescription { get; set; }
        public string LastKnownDirectionText { get; set; }
        public string LastKnownDistanceText { get; set; }
        public double SkillValue { get; set; }
        public string SkillTier { get; set; }

        public AIGMCompanionTrackingState()
        {
            Mode = AIGMCompanionTrackingMode.None;
            StartedUtc = DateTime.MinValue;
            LastScanUtc = DateTime.MinValue;
            LastReport = String.Empty;
            LastConfidence = String.Empty;
            LastKnownTargetDescription = String.Empty;
            LastKnownDirectionText = String.Empty;
            LastKnownDistanceText = String.Empty;
            SkillTier = String.Empty;
        }
    }
}
