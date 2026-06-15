using System;
using Server;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public enum AIGMCompanionExecutionPhase
    {
        Idle = 0,
        TrackingMonsters,
        SelectingMonsterTarget,
        PursuingMonster,
        OpeningDoor,
        EngagingMonster,
        SelfBandaging,
        SelfCuring,
        Reacquiring,
        Stopped,
        Blocked
    }

    public sealed class AIGMCompanionExecutionState
    {
        public int CompanionSerial { get; set; }
        public int OwnerSerial { get; set; }
        public bool HuntActive { get; set; }
        public AIGMCompanionExecutionPhase Phase { get; set; }
        public string PhaseReason { get; set; }
        public DateTime StartedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public DateTime LastTickUtc { get; set; }
        public int CurrentTargetSerial { get; set; }
        public string CurrentTargetName { get; set; }
        public Point3D CurrentTargetPoint { get; set; }
        public string LastTrace { get; set; }
        public string LastError { get; set; }
        public bool LastDoorOpenAttempted { get; set; }
        public bool LastDoorOpenSucceeded { get; set; }
        public string LastDoor { get; set; }
        public string LastDoorTarget { get; set; }
        public bool LastBandageAttempted { get; set; }
        public bool LastBandageStarted { get; set; }
        public bool LastCureAttempted { get; set; }
        public bool LastCureSucceeded { get; set; }
        public string LastMovementResult { get; set; }
        public string LastCombatResult { get; set; }
        public string LastTargetRejectionReason { get; set; }
        public string LastCandidateSummary { get; set; }
        public string LastRejectedCandidates { get; set; }
        public Point3D LastMoveFrom { get; set; }
        public Point3D LastMoveTo { get; set; }
        public string LastMoveDirection { get; set; }
        public int LastMoveDistanceBefore { get; set; }
        public int LastMoveDistanceAfter { get; set; }
        public int ScanRange { get; set; }
        public int ReacquireCount { get; set; }

        public AIGMCompanionExecutionState()
        {
            Phase = AIGMCompanionExecutionPhase.Idle;
            PhaseReason = "idle";
            StartedUtc = DateTime.MinValue;
            UpdatedUtc = DateTime.MinValue;
            LastTickUtc = DateTime.MinValue;
            CurrentTargetName = String.Empty;
            CurrentTargetPoint = Point3D.Zero;
            LastTrace = String.Empty;
            LastError = String.Empty;
            LastDoor = String.Empty;
            LastDoorTarget = String.Empty;
            LastMovementResult = String.Empty;
            LastCombatResult = String.Empty;
            LastTargetRejectionReason = String.Empty;
            LastCandidateSummary = String.Empty;
            LastRejectedCandidates = String.Empty;
            LastMoveFrom = Point3D.Zero;
            LastMoveTo = Point3D.Zero;
            LastMoveDirection = String.Empty;
            LastMoveDistanceBefore = -1;
            LastMoveDistanceAfter = -1;
            ScanRange = 12;
        }
    }
}