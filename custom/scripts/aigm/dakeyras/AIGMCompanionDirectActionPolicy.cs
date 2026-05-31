using System;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public enum AIGMCompanionActionDecision
    {
        Reject = 0,
        DirectExecute = 1,
        NeedsConfirmation = 2,
        SendToAsyncAI = 3
    }

    public sealed class AIGMCompanionActionPolicyResult
    {
        public AIGMCompanionActionDecision Decision { get; private set; }
        public string Reason { get; private set; }

        private AIGMCompanionActionPolicyResult(AIGMCompanionActionDecision decision, string reason)
        {
            Decision = decision;
            Reason = reason;
        }

        public static AIGMCompanionActionPolicyResult Direct(string reason)
        {
            return new AIGMCompanionActionPolicyResult(AIGMCompanionActionDecision.DirectExecute, reason);
        }

        public static AIGMCompanionActionPolicyResult Reject(string reason)
        {
            return new AIGMCompanionActionPolicyResult(AIGMCompanionActionDecision.Reject, reason);
        }

        public static AIGMCompanionActionPolicyResult Async(string reason)
        {
            return new AIGMCompanionActionPolicyResult(AIGMCompanionActionDecision.SendToAsyncAI, reason);
        }

        public static AIGMCompanionActionPolicyResult Confirm(string reason)
        {
            return new AIGMCompanionActionPolicyResult(AIGMCompanionActionDecision.NeedsConfirmation, reason);
        }
    }

    public static class AIGMCompanionDirectActionPolicy
    {
        public static AIGMCompanionActionPolicyResult Decide(AIGMCompanionDakeyras companion, Mobile speaker, AIGMCompanionIntent intent)
        {
            if (companion == null || companion.Deleted)
                return AIGMCompanionActionPolicyResult.Reject("Companion is missing.");

            if (speaker == null || speaker.Deleted)
                return AIGMCompanionActionPolicyResult.Reject("Speaker is missing.");

            if (intent == null || String.IsNullOrWhiteSpace(intent.Kind))
                return AIGMCompanionActionPolicyResult.Async("No local companion intent.");

            if (!IsTrustedSpeaker(companion, speaker))
                return AIGMCompanionActionPolicyResult.Async("Speaker is not trusted for direct action.");

            if (!companion.Alive)
                return AIGMCompanionActionPolicyResult.Reject("I cannot act while dead.");

            if (!speaker.InRange(companion, 12))
                return AIGMCompanionActionPolicyResult.Reject("You are too far away.");

            switch (companion.ExecutionMode)
            {
                case AIGMExecutionMode.SuggestOnly:
                    return AIGMCompanionActionPolicyResult.Async("Suggest-only mode.");
                case AIGMExecutionMode.TrustedCompanionDirect:
                case AIGMExecutionMode.TrustedOwnerLowRiskDirect:
                case AIGMExecutionMode.DevOwnerFullAuto:
                    if (IsDirectCompanionAbility(intent.Kind))
                        return AIGMCompanionActionPolicyResult.Direct("Trusted companion ability.");

                    return AIGMCompanionActionPolicyResult.Async("Intent is not direct-executable companion ability.");
                default:
                    return AIGMCompanionActionPolicyResult.Async("Unknown execution mode.");
            }
        }

        private static bool IsTrustedSpeaker(AIGMCompanionDakeyras companion, Mobile speaker)
        {
            if (companion == null || speaker == null)
                return false;

            if (speaker.AccessLevel >= AccessLevel.GameMaster)
                return true;

            Mobile owner = companion.GetOwner();
            return owner != null && owner == speaker;
        }

        private static bool IsDirectCompanionAbility(string kind)
        {
            switch (kind)
            {
                case AIGMCompanionIntentKind.FollowOwner:
                case AIGMCompanionIntentKind.Stay:
                case AIGMCompanionIntentKind.Come:
                case AIGMCompanionIntentKind.AttackTarget:
                case AIGMCompanionIntentKind.GuardOwner:
                case AIGMCompanionIntentKind.BandageSelf:
                case AIGMCompanionIntentKind.BandageOwner:
                case AIGMCompanionIntentKind.HealSelf:
                case AIGMCompanionIntentKind.HealOwner:
                case AIGMCompanionIntentKind.StopCombat:
                case AIGMCompanionIntentKind.CureSelf:
                case AIGMCompanionIntentKind.CureOwner:
                case AIGMCompanionIntentKind.UseHealingSkill:
                case AIGMCompanionIntentKind.UseBandages:
                case AIGMCompanionIntentKind.CastHeal:
                case AIGMCompanionIntentKind.CastCure:
                    return true;
                default:
                    return false;
            }
        }
    }
}
