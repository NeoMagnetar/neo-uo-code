using System;
using System.Collections.Generic;

using Server;

namespace Server.Custom.AIGM.Tasks
{
    public sealed class AIGMRosterTaskState
    {
        public Serial AgentSerial { get; set; }
        public Serial TrustedCommanderSerial { get; set; }
        public AIGMRosterTaskMode Mode { get; set; }
        public Serial TargetSerial { get; set; }
        public string TargetCanonicalId { get; set; }
        public AIGMRosterTargetSelector Selector { get; set; }
        public AIGMRosterFaction TargetFaction { get; set; }
        public Map AssignedMap { get; set; }
        public string AssignedRegionName { get; set; }
        public Point3D AssignedLocation { get; set; }
        public int AssignedRange { get; set; }
        public Map HomeMap { get; set; }
        public Point3D HomeLocation { get; set; }
        public List<Point3D> PatrolPoints { get; private set; }
        public int PatrolIndex { get; set; }
        public Map LastKnownTargetMap { get; set; }
        public Point3D LastKnownTargetLocation { get; set; }
        public DateTime LastSeenUtc { get; set; }
        public double TrailConfidence { get; set; }
        public Serial CurrentCombatInterruptionSerial { get; set; }
        public DateTime TaskStartUtc { get; set; }
        public string TaskStatus { get; set; }
        public int FailureCount { get; set; }
        public int ReacquireCount { get; set; }
        public string GroupId { get; set; }
        public bool Persists { get; set; }
        public bool PassiveTaskMode { get; set; }
        public bool IsAutonomous { get; set; }
        public AIGMRosterThreatAwarenessLevel Awareness { get; set; }
        public AIGMRosterThreatResponse LastThreatResponse { get; set; }
        public DateTime LastPulseUtc { get; set; }
        public DateTime LastAwarenessUtc { get; set; }
        public AIGMOperationalMode OperationalMode { get; set; }
        public int ControlEpoch { get; set; }
        public string LastOperationalCommand { get; set; }
        public DateTime LastOperationalCommandTime { get; set; }
        public Serial StandDownCommanderSerial { get; set; }
        public bool AllowSelfDefenseWhileStandingDown { get; set; }
        public Serial NativeCombatTargetSerial { get; set; }
        public Serial NativeCombatMissionTargetSerial { get; set; }
        public DateTime NativeCombatStartUtc { get; set; }
        public string NativeCombatSource { get; set; }
        public bool NativeCombatAlreadyActiveLogged { get; set; }

        public AIGMRosterTaskState()
        {
            TargetCanonicalId = String.Empty;
            AssignedRegionName = String.Empty;
            PatrolPoints = new List<Point3D>();
            TaskStatus = "idle";
            GroupId = String.Empty;
            TrailConfidence = 0.0;
            Persists = true;
            IsAutonomous = true;
            Awareness = AIGMRosterThreatAwarenessLevel.Unaware;
            LastThreatResponse = AIGMRosterThreatResponse.None;
            OperationalMode = AIGMOperationalMode.Active;
            LastOperationalCommand = String.Empty;
            StandDownCommanderSerial = Serial.MinusOne;
            AllowSelfDefenseWhileStandingDown = true;
            NativeCombatTargetSerial = Serial.MinusOne;
            NativeCombatMissionTargetSerial = Serial.MinusOne;
            NativeCombatStartUtc = DateTime.MinValue;
            NativeCombatSource = String.Empty;
        }

        public void ClearTask(bool preserveHome)
        {
            Mode = AIGMRosterTaskMode.None;
            TargetSerial = Serial.MinusOne;
            TargetCanonicalId = String.Empty;
            Selector = AIGMRosterTargetSelector.None;
            TargetFaction = AIGMRosterFaction.None;
            AssignedMap = null;
            AssignedRegionName = String.Empty;
            AssignedLocation = Point3D.Zero;
            AssignedRange = 0;
            PatrolPoints.Clear();
            PatrolIndex = 0;
            LastKnownTargetMap = null;
            LastKnownTargetLocation = Point3D.Zero;
            LastSeenUtc = DateTime.MinValue;
            TrailConfidence = 0.0;
            CurrentCombatInterruptionSerial = Serial.MinusOne;
            TaskStartUtc = DateTime.MinValue;
            TaskStatus = "cleared";
            FailureCount = 0;
            ReacquireCount = 0;
            GroupId = String.Empty;
            Awareness = AIGMRosterThreatAwarenessLevel.Unaware;
            LastThreatResponse = AIGMRosterThreatResponse.None;
            NativeCombatTargetSerial = Serial.MinusOne;
            NativeCombatMissionTargetSerial = Serial.MinusOne;
            NativeCombatStartUtc = DateTime.MinValue;
            NativeCombatSource = String.Empty;
            NativeCombatAlreadyActiveLogged = false;

            if (!preserveHome)
            {
                HomeMap = null;
                HomeLocation = Point3D.Zero;
            }
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write(3);
            writer.Write(AgentSerial);
            writer.Write(TrustedCommanderSerial);
            writer.Write((int)Mode);
            writer.Write(TargetSerial);
            writer.Write(TargetCanonicalId);
            writer.Write((int)Selector);
            writer.Write((int)TargetFaction);
            writer.Write(AssignedMap);
            writer.Write(AssignedRegionName);
            writer.Write(AssignedLocation);
            writer.Write(AssignedRange);
            writer.Write(HomeMap);
            writer.Write(HomeLocation);
            writer.Write(PatrolPoints.Count);
            for (int i = 0; i < PatrolPoints.Count; i++)
                writer.Write(PatrolPoints[i]);
            writer.Write(PatrolIndex);
            writer.Write(LastKnownTargetMap);
            writer.Write(LastKnownTargetLocation);
            writer.Write(LastSeenUtc);
            writer.Write(TrailConfidence);
            writer.Write(CurrentCombatInterruptionSerial);
            writer.Write(TaskStartUtc);
            writer.Write(TaskStatus);
            writer.Write(FailureCount);
            writer.Write(ReacquireCount);
            writer.Write(GroupId);
            writer.Write(Persists);
            writer.Write(PassiveTaskMode);
            writer.Write(IsAutonomous);
            writer.Write((int)Awareness);
            writer.Write((int)LastThreatResponse);
            writer.Write(LastPulseUtc);
            writer.Write(LastAwarenessUtc);
            writer.Write((int)OperationalMode);
            writer.Write(ControlEpoch);
            writer.Write(LastOperationalCommand);
            writer.Write(LastOperationalCommandTime);
            writer.Write(StandDownCommanderSerial);
            writer.Write(AllowSelfDefenseWhileStandingDown);
            writer.Write(NativeCombatTargetSerial);
            writer.Write(NativeCombatMissionTargetSerial);
            writer.Write(NativeCombatStartUtc);
            writer.Write(NativeCombatSource);
            writer.Write(NativeCombatAlreadyActiveLogged);
        }

        public static AIGMRosterTaskState Deserialize(GenericReader reader)
        {
            int version = reader.ReadInt();
            AIGMRosterTaskState state = new AIGMRosterTaskState();
            state.AgentSerial = reader.ReadInt();
            state.TrustedCommanderSerial = reader.ReadInt();
            state.Mode = (AIGMRosterTaskMode)reader.ReadInt();
            state.TargetSerial = reader.ReadInt();
            state.TargetCanonicalId = reader.ReadString();
            state.Selector = (AIGMRosterTargetSelector)reader.ReadInt();
            state.TargetFaction = (AIGMRosterFaction)reader.ReadInt();
            state.AssignedMap = reader.ReadMap();
            state.AssignedRegionName = reader.ReadString();
            state.AssignedLocation = reader.ReadPoint3D();
            state.AssignedRange = reader.ReadInt();
            state.HomeMap = reader.ReadMap();
            state.HomeLocation = reader.ReadPoint3D();
            int patrolCount = reader.ReadInt();
            for (int i = 0; i < patrolCount; i++)
                state.PatrolPoints.Add(reader.ReadPoint3D());
            state.PatrolIndex = reader.ReadInt();
            state.LastKnownTargetMap = reader.ReadMap();
            state.LastKnownTargetLocation = reader.ReadPoint3D();
            state.LastSeenUtc = reader.ReadDateTime();
            state.TrailConfidence = reader.ReadDouble();
            state.CurrentCombatInterruptionSerial = reader.ReadInt();
            state.TaskStartUtc = reader.ReadDateTime();
            state.TaskStatus = reader.ReadString();
            state.FailureCount = reader.ReadInt();
            state.ReacquireCount = reader.ReadInt();
            state.GroupId = reader.ReadString();
            state.Persists = reader.ReadBool();
            state.PassiveTaskMode = reader.ReadBool();
            state.IsAutonomous = reader.ReadBool();
            state.Awareness = (AIGMRosterThreatAwarenessLevel)reader.ReadInt();
            state.LastThreatResponse = (AIGMRosterThreatResponse)reader.ReadInt();
            state.LastPulseUtc = reader.ReadDateTime();
            state.LastAwarenessUtc = reader.ReadDateTime();
            if (version >= 2)
            {
                state.OperationalMode = (AIGMOperationalMode)reader.ReadInt();
                state.ControlEpoch = reader.ReadInt();
                state.LastOperationalCommand = reader.ReadString();
                state.LastOperationalCommandTime = reader.ReadDateTime();
                state.StandDownCommanderSerial = reader.ReadInt();
                state.AllowSelfDefenseWhileStandingDown = reader.ReadBool();
            }
            if (version >= 3)
            {
                state.NativeCombatTargetSerial = reader.ReadInt();
                state.NativeCombatMissionTargetSerial = reader.ReadInt();
                state.NativeCombatStartUtc = reader.ReadDateTime();
                state.NativeCombatSource = reader.ReadString();
                state.NativeCombatAlreadyActiveLogged = reader.ReadBool();
            }
            return state;
        }
    }
}
