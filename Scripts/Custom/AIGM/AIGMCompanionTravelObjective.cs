using System;
using System.Collections.Generic;
using Server;

namespace Server.Custom.AIGM
{
    public sealed class AIGMCompanionTravelObjective
    {
        public string ObjectiveId;
        public string DestinationName;
        public string DestinationMapName;
        public int DestinationX;
        public int DestinationY;
        public int DestinationZ;
        public int ArrivalRadius;
        public DateTime StartedUtc;
        public DateTime LastProgressUtc;
        public Point3D LastLocation;
        public int LastDistance;
        public int StuckCounter;
        public int RecoveryDirectionX;
        public int RecoveryDirectionY;
        public int RecoveryStepsRemaining;
        public int RecoveryAttempts;
        public AIGMTravelPathStrategy CurrentPathStrategy;
        public int ConsecutiveBlockedSteps;
        public int ConsecutiveNoProgressChecks;
        public int ConsecutiveDirectProgressSteps;
        public DateTime DirectStepLockUntilUtc;
        public int ConsecutivePathFollowerFailures;
        public int ConsecutiveDetourFailures;
        public AIGMTravelMemory Memory;
        public PathFollower ActivePathFollower;
        public Point3D ActiveDetourPoint;
        public Point3D ActiveLocalAlternatePoint;
        public int DetourCommitmentPulsesRemaining;
        public double OriginalActiveSpeed;
        public double OriginalPassiveSpeed;
        public bool TravelSpeedApplied;
        public AIGMTravelRecoveryMode RecoveryMode;
        public Point3D ActiveEscapePoint;
        public int EscapeCommitmentPulsesRemaining;
        public int ConsecutiveEscapeFailures;
        public int ConsecutiveBacktrackFailures;
        public int PathCommitmentPulsesRemaining;
        public Point3D LastCommittedPathGoal;
        public Point3D ActiveWallFollowPoint;
        public int WallFollowDirectionX;
        public int WallFollowDirectionY;
        public int WallFollowCommitmentPulsesRemaining;
        public int ConsecutiveWallFollowFailures;
        public Point3D WallFollowStartLocation;
        public int WallFollowProgressPulses;
        public int OscillationCount;
        public string SuppressedRouteBand;
        public DateTime SuppressedRouteBandUntilUtc;
        public List<AIGMStuckZone> StuckZones;
        public AIGMCompanionTravelStatus Status;
        public bool HasTrackedPursuit;
        public int TrackedTargetSerial;
        public string TrackedTargetName;
        public AIGMTrackingCategory TrackedTargetCategory;
        public DateTime TrackedPursuitStartedUtc;
        public DateTime TrackedPursuitExpiresUtc;

        public Point3D DestinationPoint
        {
            get { return new Point3D(DestinationX, DestinationY, DestinationZ); }
        }

        public AIGMCompanionTravelObjective()
        {
            Memory = new AIGMTravelMemory();
            StuckZones = new List<AIGMStuckZone>();
            CurrentPathStrategy = AIGMTravelPathStrategy.DirectStep;
            RecoveryMode = AIGMTravelRecoveryMode.None;
        }
    }
}
