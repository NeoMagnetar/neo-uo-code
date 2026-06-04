using System;
using Server;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public enum AIGMTrackingCyclePhase
    {
        Monsters = 0,
        Players = 1,
        HumanNPCs = 2,
        Animals = 3
    }

    public static class AIGMCompanionTrackingCycle
    {
        private static readonly TimeSpan DefaultSweepDelay = TimeSpan.FromSeconds(30.0);
        private static readonly TimeSpan MonsterSweepDelay = TimeSpan.FromSeconds(30.0);
        private static readonly TimeSpan AnimalSweepDelay = TimeSpan.FromSeconds(30.0);

        public static bool Start(BaseHire companion, out string response)
        {
            response = null;
            if (companion == null || companion.Deleted || !companion.Alive)
            {
                response = "I cannot begin tracking right now.";
                return false;
            }

            AIGMCompanionStateAccess.SetTrackingEnabled(companion, true);
            AIGMCompanionStateAccess.SetTrackingPhase(companion, AIGMTrackingCyclePhase.Monsters);
            AIGMCompanionStateAccess.SetTrackingMonsterLock(companion, false);
            AIGMCompanionStateAccess.SetTrackingNextSweepUtc(companion, DateTime.UtcNow + TimeSpan.FromSeconds(1.0));
            response = "I will begin tracking.";
            return true;
        }

        public static bool Stop(BaseHire companion, out string response)
        {
            response = null;
            if (companion == null)
            {
                response = "I cannot stop tracking right now.";
                return false;
            }

            AIGMCompanionStateAccess.ResetAllCompanionIntentState(companion);
            UMGMovementRouter.ClearTrackedPursuitState(companion, "tracking_stopped");
            response = "I will stop tracking.";
            return true;
        }

        public static bool ReportStatus(BaseHire companion, out string response)
        {
            response = null;
            if (companion == null || companion.Deleted || !companion.Alive)
            {
                response = "I cannot report tracking right now.";
                return false;
            }

            if (!AIGMCompanionStateAccess.GetTrackingEnabled(companion))
            {
                response = "Tracking is not active.";
                return true;
            }

            AIGMTrackingCyclePhase phase = AIGMCompanionStateAccess.GetTrackingPhase(companion);
            DateTime nextSweepUtc = AIGMCompanionStateAccess.GetTrackingNextSweepUtc(companion);
            bool locked = AIGMCompanionStateAccess.GetTrackingMonsterLock(companion);
            string phaseLabel = phase.ToString().ToLowerInvariant();

            if (nextSweepUtc <= DateTime.UtcNow)
            {
                response = String.Format("Tracking is active. Current phase: {0}. Next sweep is ready now.{1}", phaseLabel, locked ? " Monster lock is engaged." : String.Empty);
                return true;
            }

            TimeSpan remaining = nextSweepUtc - DateTime.UtcNow;
            int seconds = Math.Max(1, (int)Math.Ceiling(remaining.TotalSeconds));
            response = String.Format("Tracking is active. Current phase: {0}. Next sweep in about {1} seconds.{2}", phaseLabel, seconds, locked ? " Monster lock is engaged." : String.Empty);
            return true;
        }

        public static void SuspendPursuitAndTravel(BaseHire companion)
        {
            if (companion == null)
                return;

            AIGMCompanionTravelController.ClearTrackedPursuit(companion);
            AIGMCompanionTravelController.ClearTravelState(companion, false, true, "Travel suspended");
            AIGMCompanionStateAccess.SetTrackingMonsterLock(companion, false);
            UMGMovementRouter.ClearTrackedPursuitState(companion, "pursuit_suspended");
        }

        public static void Pulse(BaseHire companion)
        {
            if (companion == null || companion.Deleted || !companion.Alive || companion.Map == null)
                return;

            if (!AIGMCompanionStateAccess.GetTrackingEnabled(companion))
                return;

            DateTime now = DateTime.UtcNow;
            if (IsActivelyEngaged(companion))
            {
                AIGMCompanionStateAccess.SetTrackingNextSweepUtc(companion, now + TimeSpan.FromSeconds(2.0));
                return;
            }

            if (now < AIGMCompanionStateAccess.GetTrackingNextSweepUtc(companion))
                return;

            AIGMTrackingCyclePhase phase = AIGMCompanionStateAccess.GetTrackingPhase(companion);
            AIGMTrackingCategory category = ToCategory(phase);
            AIGMCompanionTrackingSweep sweep = AIGMCompanionTrackingSensor.Sweep(companion, category);
            if (sweep != null)
                AIGMCompanionPerceptionBuffer.RecordSweep(companion, sweep);

            AIGMCompanionTrackingEntry closest = sweep != null && sweep.Entries != null && sweep.Entries.Count > 0 ? sweep.Entries[0] : null;
            AIGMExecutionLog.Write("TRACKING_CYCLE_SWEEP companion={0} phase={1} entries={2}", companion.Serial.Value, phase, sweep != null && sweep.Entries != null ? sweep.Entries.Count : 0);

            if (phase == AIGMTrackingCyclePhase.Monsters)
            {
                if (TryEngageTrackedTarget(companion, closest, out _))
                {
                    AIGMCompanionStateAccess.SetTrackingMonsterLock(companion, true);
                    AIGMCompanionStateAccess.SetTrackingNextSweepUtc(companion, now + MonsterSweepDelay);
                    return;
                }

                if (AIGMCompanionStateAccess.GetTrackingMonsterLock(companion))
                    AIGMCompanionStateAccess.SetTrackingMonsterLock(companion, false);
            }
            else if (phase == AIGMTrackingCyclePhase.HumanNPCs || phase == AIGMTrackingCyclePhase.Players)
            {
                if (closest != null)
                {
                    AIGMCompanionStateAccess.SetTrackingNextSweepUtc(companion, now + DefaultSweepDelay);
                    AdvancePhase(companion, phase);
                    return;
                }
            }
            else if (phase == AIGMTrackingCyclePhase.Animals)
            {
                if (TryEngageTrackedTarget(companion, closest, out _))
                {
                    AIGMCompanionStateAccess.SetTrackingPhase(companion, AIGMTrackingCyclePhase.Monsters);
                    AIGMCompanionStateAccess.SetTrackingNextSweepUtc(companion, now + AnimalSweepDelay);
                    return;
                }
            }

            AdvancePhase(companion, phase);
            AIGMCompanionStateAccess.SetTrackingNextSweepUtc(companion, now + DefaultSweepDelay);
        }

        private static void AdvancePhase(BaseHire companion, AIGMTrackingCyclePhase phase)
        {
            if (companion == null)
                return;

            if (AIGMCompanionStateAccess.GetTrackingMonsterLock(companion))
            {
                AIGMCompanionStateAccess.SetTrackingPhase(companion, AIGMTrackingCyclePhase.Monsters);
                return;
            }

            switch (phase)
            {
                case AIGMTrackingCyclePhase.Monsters:
                    AIGMCompanionStateAccess.SetTrackingPhase(companion, AIGMTrackingCyclePhase.Players);
                    break;
                case AIGMTrackingCyclePhase.Players:
                    AIGMCompanionStateAccess.SetTrackingPhase(companion, AIGMTrackingCyclePhase.HumanNPCs);
                    break;
                case AIGMTrackingCyclePhase.HumanNPCs:
                    AIGMCompanionStateAccess.SetTrackingPhase(companion, AIGMTrackingCyclePhase.Animals);
                    break;
                default:
                    AIGMCompanionStateAccess.SetTrackingPhase(companion, AIGMTrackingCyclePhase.Monsters);
                    break;
            }
        }

        private static AIGMTrackingCategory ToCategory(AIGMTrackingCyclePhase phase)
        {
            switch (phase)
            {
                case AIGMTrackingCyclePhase.Players:
                    return AIGMTrackingCategory.Players;
                case AIGMTrackingCyclePhase.HumanNPCs:
                    return AIGMTrackingCategory.HumanNPCs;
                case AIGMTrackingCyclePhase.Animals:
                    return AIGMTrackingCategory.Animals;
                default:
                    return AIGMTrackingCategory.Monsters;
            }
        }

        private static bool TryEngageTrackedTarget(BaseHire companion, AIGMCompanionTrackingEntry entry, out string response)
        {
            response = null;
            if (companion == null || entry == null || !entry.IsAlive)
                return false;

            Mobile target = World.FindMobile(entry.TargetSerial);
            if (target == null || target.Deleted || !target.Alive || target.Map != companion.Map)
                return false;

            if (target == companion || target == companion.GetOwner())
                return false;

            bool started = AIGMCompanionTravelController.StartTrackedPursuit(companion, entry);
            if (!started)
                return false;

            UMGMovementRouter.SetTrackedPursuitState(companion, entry);
            response = "I am checking " + (target.Name ?? target.GetType().Name) + " and will resume our route.";
            AIGMExecutionLog.Write("TRACKING_CYCLE_ENGAGE companion={0} target={1} category={2}", companion.Serial.Value, target.Serial.Value, entry.Category);
            return true;
        }

        private static bool IsActivelyEngaged(BaseHire companion)
        {
            if (companion == null)
                return false;

            Mobile combatant = companion.Combatant as Mobile;
            if (combatant != null && !combatant.Deleted && combatant.Alive && combatant.Map == companion.Map)
                return true;

            Mobile owner = companion.GetOwner();
            Mobile ownerCombatant = owner != null ? owner.Combatant as Mobile : null;
            if (ownerCombatant != null && !ownerCombatant.Deleted && ownerCombatant.Alive && owner.InRange(companion, 12))
                return true;

            return false;
        }
    }
}
