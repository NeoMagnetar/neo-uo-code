using System;
using Server;

namespace Server.Custom.AIGM
{
    public enum AIGMCompanionTrackingActionMode
    {
        TrackOnly,
        TrackMove,
        TrackHunt
    }

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
        public string LastKnownTileText { get; set; }
        public string LastCandidateSummary { get; set; }
        public string LastRejectedCandidates { get; set; }
        public double SkillValue { get; set; }
        public string SkillTier { get; set; }
        public AIGMCompanionTrackingActionMode ActionMode { get; set; }
        public int CurrentTargetSerial { get; set; }
        public string CurrentTargetName { get; set; }
        public Point3D LastKnownTargetLocation { get; set; }
        public DateTime LastActionUtc { get; set; }
        public string LastActionResult { get; set; }
        public string StopReason { get; set; }
        public bool EngagementAllowed { get; set; }

        public AIGMCompanionTrackingState()
        {
            Mode = AIGMCompanionTrackingMode.None;
            ActionMode = AIGMCompanionTrackingActionMode.TrackOnly;
            StartedUtc = DateTime.MinValue;
            LastScanUtc = DateTime.MinValue;
            LastReport = String.Empty;
            LastConfidence = String.Empty;
            LastKnownTargetDescription = String.Empty;
            LastKnownDirectionText = String.Empty;
            LastKnownDistanceText = String.Empty;
            LastKnownTileText = String.Empty;
            LastCandidateSummary = String.Empty;
            LastRejectedCandidates = String.Empty;
            SkillTier = String.Empty;
            CurrentTargetName = String.Empty;
            LastKnownTargetLocation = Point3D.Zero;
            LastActionUtc = DateTime.MinValue;
            LastActionResult = String.Empty;
            StopReason = String.Empty;
        }
    }
}
