using System;
using Server;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public enum AIGMTrackingLoopStage
    {
        Monsters = 0,
        Players = 1,
        HumanNPCs = 2,
        Animals = 3
    }

    public sealed class AIGMCompanionTrackingObjective
    {
        public bool Enabled;
        public DateTime StartedUtc;
        public DateTime NextSweepUtc;
        public DateTime LastSweepUtc;
        public int ActiveTargetSerial;
        public AIGMTrackingLoopStage CurrentStage;
        public string LastStatus;
        public bool ResumeTravelBetweenSweeps;

        public AIGMCompanionTrackingObjective()
        {
            CurrentStage = AIGMTrackingLoopStage.Monsters;
            LastStatus = "idle";
        }
    }

    public static class AIGMCompanionTrackingController
    {
        private static readonly TimeSpan SweepCadence = TimeSpan.FromSeconds(30.0);
        private static readonly TimeSpan RecheckWhileEngaged = TimeSpan.FromSeconds(2.0);

        public static bool StartTracking(BaseHire companion, out string response)
        {
            response = null;
            if (companion == null || companion.Deleted || companion.Map == null)
            {
                response = "I cannot begin tracking just now.";
                return false;
            }

            AIGMCompanionTrackingObjective objective = AIGMCompanionStateAccess.GetTrackingObjective(companion);
            if (objective == null)
                objective = new AIGMCompanionTrackingObjective();

            objective.Enabled = true;
            objective.StartedUtc = DateTime.UtcNow;
            objective.LastSweepUtc = DateTime.MinValue;
            objective.NextSweepUtc = DateTime.UtcNow;
            objective.ActiveTargetSerial = 0;
            objective.CurrentStage = AIGMTrackingLoopStage.Monsters;
            objective.ResumeTravelBetweenSweeps = AIGMCompanionStateAccess.GetTravelObjective(companion) != null;
            objective.LastStatus = objective.ResumeTravelBetweenSweeps ? "tracking between travel pulses" : "tracking patrol active";
            AIGMCompanionStateAccess.SetTrackingObjective(companion, objective);

            response = objective.ResumeTravelBetweenSweeps
                ? "Tracking is active. I will hunt between travel pulses and break off from the route when I engage."
                : "Tracking is active. I will hunt the nearby ground.";
            return true;
        }

        public static bool StopTracking(BaseHire companion, out string response)
        {
            response = null;
            if (companion == null)
                return false;

            AIGMCompanionTrackingObjective objective = AIGMCompanionStateAccess.GetTrackingObjective(companion);
            if (objective == null || !objective.Enabled)
            {
                response = "I was not tracking.";
                return true;
            }

            objective.Enabled = false;
            objective.ActiveTargetSerial = 0;
            objective.LastStatus = "tracking stopped";
            AIGMCompanionStateAccess.SetTrackingObjective(companion, null);
            response = "I have stopped tracking.";
            return true;
        }

        public static bool ReportStatus(BaseHire companion, out string response)
        {
            response = null;
            if (companion == null)
                return false;

            AIGMCompanionTrackingObjective objective = AIGMCompanionStateAccess.GetTrackingObjective(companion);
            if (objective == null || !objective.Enabled)
            {
                response = "I am not tracking right now.";
                return true;
            }

            Mobile target = objective.ActiveTargetSerial != 0 ? World.FindMobile(objective.ActiveTargetSerial) : null;
            if (target != null && !target.Deleted && target.Alive && target.Map == companion.Map)
            {
                response = "I am tracking and engaged on " + SafeName(target) + ".";
                return true;
            }

            response = "Tracking is active. Next sweep: " + FormatStage(objective.CurrentStage) + ".";
            return true;
        }

        public static void PulseTracking(BaseHire companion)
        {
            if (companion == null || companion.Deleted || companion.Map == null || !companion.Alive)
                return;

            AIGMCompanionTrackingObjective objective = AIGMCompanionStateAccess.GetTrackingObjective(companion);
            if (objective == null || !objective.Enabled)
                return;

            Mobile engagedTarget = objective.ActiveTargetSerial != 0 ? World.FindMobile(objective.ActiveTargetSerial) : null;
            if (IsValidTarget(companion, engagedTarget))
            {
                AIGMCompanionActionExecutor.IssueAttackOrder(companion, engagedTarget);
                objective.LastStatus = "engaging " + SafeName(engagedTarget);
                objective.NextSweepUtc = DateTime.UtcNow + RecheckWhileEngaged;
                AIGMCompanionStateAccess.SetTrackingObjective(companion, objective);
                return;
            }

            objective.ActiveTargetSerial = 0;

            Mobile combatant = companion.Combatant as Mobile;
            if (IsValidTarget(companion, combatant))
            {
                objective.LastStatus = "combat already active on " + SafeName(combatant);
                objective.NextSweepUtc = DateTime.UtcNow + RecheckWhileEngaged;
                AIGMCompanionStateAccess.SetTrackingObjective(companion, objective);
                return;
            }

            if (DateTime.UtcNow < objective.NextSweepUtc)
            {
                objective.LastStatus = objective.ResumeTravelBetweenSweeps && AIGMCompanionStateAccess.GetTravelObjective(companion) != null
                    ? "yielding to travel between sweeps"
                    : "waiting for next sweep";
                AIGMCompanionStateAccess.SetTrackingObjective(companion, objective);
                return;
            }

            AIGMTrackingCategory category = GetCategory(objective.CurrentStage);
            AIGMCompanionTrackingSweep sweep = AIGMCompanionTrackingSensor.Sweep(companion, category);
            if (sweep != null)
                AIGMCompanionPerceptionBuffer.RecordSweep(companion, sweep);

            Mobile selected = SelectTarget(companion, sweep, category);
            objective.LastSweepUtc = DateTime.UtcNow;

            if (selected != null)
            {
                objective.ActiveTargetSerial = selected.Serial.Value;
                objective.LastStatus = "engaging " + SafeName(selected) + " from " + FormatStage(objective.CurrentStage) + " sweep";
                objective.CurrentStage = AIGMTrackingLoopStage.Monsters;
                objective.NextSweepUtc = DateTime.UtcNow + RecheckWhileEngaged;
                AIGMCompanionActionExecutor.IssueAttackOrder(companion, selected);
                AIGMCompanionStateAccess.SetTrackingObjective(companion, objective);
                return;
            }

            objective.LastStatus = "no target on " + FormatStage(objective.CurrentStage) + " sweep";
            objective.CurrentStage = GetNextStage(objective.CurrentStage);
            objective.NextSweepUtc = DateTime.UtcNow + SweepCadence;
            AIGMCompanionStateAccess.SetTrackingObjective(companion, objective);
        }

        public static bool ShouldHoldTravelForTracking(BaseHire companion)
        {
            AIGMCompanionTrackingObjective objective = AIGMCompanionStateAccess.GetTrackingObjective(companion);
            if (objective == null || !objective.Enabled)
                return false;

            if (objective.ActiveTargetSerial != 0)
                return true;

            Mobile combatant = companion != null ? companion.Combatant as Mobile : null;
            return IsValidTarget(companion, combatant);
        }

        private static Mobile SelectTarget(BaseHire companion, AIGMCompanionTrackingSweep sweep, AIGMTrackingCategory category)
        {
            if (companion == null || sweep == null || sweep.Entries == null || sweep.Entries.Count == 0)
                return null;

            for (int i = 0; i < sweep.Entries.Count; i++)
            {
                AIGMCompanionTrackingEntry entry = sweep.Entries[i];
                if (entry == null)
                    continue;

                Mobile target = World.FindMobile(entry.TargetSerial);
                if (!IsValidTarget(companion, target))
                    continue;

                if (category == AIGMTrackingCategory.Monsters)
                    return target;

                if (category == AIGMTrackingCategory.Animals)
                    return target;
            }

            return null;
        }

        private static bool IsValidTarget(BaseHire companion, Mobile target)
        {
            if (companion == null || target == null)
                return false;

            if (target.Deleted || !target.Alive || target.Map != companion.Map)
                return false;

            if (target == companion || target == companion.GetOwner())
                return false;

            if (target is AIGMCompanionDakeyras || target is AIGMCompanionDanyal || target is AIGMCompanionDardalion)
                return false;

            return true;
        }

        private static AIGMTrackingCategory GetCategory(AIGMTrackingLoopStage stage)
        {
            switch (stage)
            {
                case AIGMTrackingLoopStage.Players:
                    return AIGMTrackingCategory.Players;
                case AIGMTrackingLoopStage.HumanNPCs:
                    return AIGMTrackingCategory.HumanNPCs;
                case AIGMTrackingLoopStage.Animals:
                    return AIGMTrackingCategory.Animals;
                default:
                    return AIGMTrackingCategory.Monsters;
            }
        }

        private static AIGMTrackingLoopStage GetNextStage(AIGMTrackingLoopStage stage)
        {
            switch (stage)
            {
                case AIGMTrackingLoopStage.Monsters:
                    return AIGMTrackingLoopStage.Players;
                case AIGMTrackingLoopStage.Players:
                    return AIGMTrackingLoopStage.HumanNPCs;
                case AIGMTrackingLoopStage.HumanNPCs:
                    return AIGMTrackingLoopStage.Animals;
                default:
                    return AIGMTrackingLoopStage.Monsters;
            }
        }

        private static string FormatStage(AIGMTrackingLoopStage stage)
        {
            switch (stage)
            {
                case AIGMTrackingLoopStage.Players:
                    return "player";
                case AIGMTrackingLoopStage.HumanNPCs:
                    return "human npc";
                case AIGMTrackingLoopStage.Animals:
                    return "animal";
                default:
                    return "monster";
            }
        }

        private static string SafeName(Mobile target)
        {
            return target != null ? (target.Name ?? target.GetType().Name) : "unknown target";
        }
    }
}
