using System;
using Server.Custom.AIGM.Tasks;
using Server.Mobiles;

namespace Server.Custom.AIGM.UMG
{
    public static class AIGMUMGAdapterRegistry
    {
        public static string DescribeConnectedAdapters()
        {
            return "operational=AIGMOperationalControlService; combat=AIGMNativeCombatBridge; movement=AIGMSmartMovementService/AIGMRosterTaskService; tracking=AIGMTrackingHuntService/AIGMRosterTaskService; healing=AIGMCompanionHealingService; spells=AIGMCompanionSpellService; potions=AIGMCompanionPotionService; loot=AIGMCompanionLootService; squad=AIGMRosterTaskService";
        }

        public static bool TryDispatch(Mobile actor, AIGMUMGDecision decision, Mobile commander, bool execute, out string receipt)
        {
            receipt = "not_dispatched";
            if (actor == null || decision == null)
            {
                receipt = "invalid_actor_or_decision";
                return false;
            }

            if (!execute)
            {
                receipt = "dry_run_only:" + decision.DeterministicAdapter;
                return true;
            }

            switch (decision.IntentType)
            {
                case AIGMUMGIntentType.StandDown:
                    AIGMOperationalControlService.ClearAllOperationalState(actor, AIGMOperationalStopMode.PassiveStandDown, commander, "umg_decision:" + decision.CorrelationId);
                    receipt = "stand_down_dispatched";
                    return true;
                case AIGMUMGIntentType.Hold:
                    if (commander == null || commander.AccessLevel < AccessLevel.GameMaster)
                    {
                        receipt = "absolute_hold_requires_gm";
                        return false;
                    }
                    AIGMOperationalControlService.ClearAllOperationalState(actor, AIGMOperationalStopMode.AbsoluteGMHold, commander, "umg_decision:" + decision.CorrelationId);
                    receipt = "hold_dispatched";
                    return true;
                case AIGMUMGIntentType.Resume:
                    AIGMOperationalControlService.Release(actor, commander, "umg_decision:" + decision.CorrelationId);
                    receipt = "resume_dispatched";
                    return true;
                default:
                    receipt = "dispatch_deferred_for_gate4:" + decision.IntentType;
                    return false;
            }
        }
    }
}
