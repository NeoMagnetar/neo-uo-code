using System;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionActionExecutor
    {
        public static bool TryExecuteIntent(AIGMCompanionDakeyras companion, Mobile speaker, AIGMCompanionIntent intent, out string response)
        {
            response = null;

            if (companion == null || speaker == null || intent == null || String.IsNullOrWhiteSpace(intent.Kind))
                return false;

            switch (intent.Kind)
            {
                case AIGMCompanionIntentKind.FollowOwner:
                    IssueFollowOrder(companion, speaker);
                    response = "I am with you.";
                    return true;
                case AIGMCompanionIntentKind.Stay:
                    IssueStayOrder(companion);
                    response = "I will hold here.";
                    return true;
                case AIGMCompanionIntentKind.Come:
                    IssueComeOrder(companion, speaker);
                    response = "On my way.";
                    return true;
                case AIGMCompanionIntentKind.GuardOwner:
                    companion.GuardOwnerMode = true;
                    IssueGuardOrder(companion, speaker);
                    response = "I will guard you.";
                    return true;
                case AIGMCompanionIntentKind.StopCombat:
                    IssueStopCombatOrder(companion);
                    response = "I am disengaging.";
                    return true;
                case AIGMCompanionIntentKind.AttackTarget:
                    return TryAttackTarget(companion, speaker, intent, out response);
                case AIGMCompanionIntentKind.BandageSelf:
                    return AIGMCompanionSkillExecutor.TryUseBandages(companion, companion, out response);
                case AIGMCompanionIntentKind.HealSelf:
                    return AIGMCompanionSkillExecutor.TryUseMageryHeal(companion, companion, out response);
                case AIGMCompanionIntentKind.BandageOwner:
                    if (speaker != companion.GetOwner())
                    {
                        response = "I only tend my bonded companion that way.";
                        return false;
                    }

                    return AIGMCompanionSkillExecutor.TryUseBandages(companion, speaker, out response);
                case AIGMCompanionIntentKind.HealOwner:
                    if (speaker != companion.GetOwner())
                    {
                        response = "I only tend my bonded companion that way.";
                        return false;
                    }

                    return AIGMCompanionSkillExecutor.TryUseMageryHeal(companion, speaker, out response);
                case AIGMCompanionIntentKind.CureSelf:
                    return AIGMCompanionSkillExecutor.TryUseCurePotion(companion, companion, out response);
                case AIGMCompanionIntentKind.CureOwner:
                    if (speaker != companion.GetOwner())
                    {
                        response = "I only tend my bonded companion that way.";
                        return false;
                    }

                    return AIGMCompanionSkillExecutor.TryUseMageryCure(companion, speaker, out response);
                case AIGMCompanionIntentKind.UseHealingSkill:
                    return AIGMCompanionSkillExecutor.TryUseHealingSkill(companion, companion, out response);
                case AIGMCompanionIntentKind.UseBandages:
                    return AIGMCompanionSkillExecutor.TryUseBandages(companion, companion, out response);
                case AIGMCompanionIntentKind.CastHeal:
                    return AIGMCompanionSkillExecutor.TryUseMageryHeal(companion, companion, out response);
                case AIGMCompanionIntentKind.CastCure:
                    return AIGMCompanionSkillExecutor.TryUseMageryCure(companion, companion, out response);
                default:
                    return false;
            }
        }

        public static bool TryReactiveSupport(AIGMCompanionDakeyras companion)
        {
            if (companion == null || companion.Deleted || !companion.Alive)
                return false;

            if (DateTime.UtcNow < companion.NextSupportActionUtc)
                return false;

            if (companion.Hits < Math.Max(25, companion.HitsMax / 2))
            {
                string ignored;
                if (AIGMCompanionSkillExecutor.TryHealTarget(companion, companion, true, out ignored))
                    return true;
            }

            Mobile owner = companion.GetOwner();
            Mobile attacker = owner != null ? owner.Combatant as Mobile : null;
            if (companion.GuardOwnerMode && owner != null && attacker != null && owner.InRange(companion, 10))
            {
                if (!attacker.Deleted && attacker.Alive)
                {
                    IssueAttackOrder(companion, attacker);
                    companion.NextSupportActionUtc = DateTime.UtcNow + TimeSpan.FromSeconds(2.0);
                    return true;
                }
            }

            return false;
        }

        private static bool TryAttackTarget(AIGMCompanionDakeyras companion, Mobile speaker, AIGMCompanionIntent intent, out string response)
        {
            response = null;

            Mobile target = null;
            if (intent != null && intent.HasTarget)
                target = World.FindMobile(intent.TargetSerial);

            if (target == null || target.Deleted || !target.Alive)
            {
                response = "I do not have a valid target.";
                return false;
            }

            companion.GuardOwnerMode = false;
            IssueAttackOrder(companion, target);
            response = "Attacking now.";
            return true;
        }


        private static void IssueFollowOrder(AIGMCompanionDakeyras companion, Mobile target)
        {
            companion.Combatant = null;
            companion.ControlTarget = target;
            companion.ControlOrder = OrderType.Follow;
        }

        private static void IssueComeOrder(AIGMCompanionDakeyras companion, Mobile target)
        {
            companion.Combatant = null;
            companion.ControlTarget = target;
            companion.ControlOrder = OrderType.Come;
        }

        private static void IssueStayOrder(AIGMCompanionDakeyras companion)
        {
            companion.Combatant = null;
            companion.ControlTarget = null;
            companion.ControlOrder = OrderType.Stay;
        }

        private static void IssueGuardOrder(AIGMCompanionDakeyras companion, Mobile target)
        {
            companion.Combatant = null;
            companion.ControlTarget = target;
            companion.ControlOrder = OrderType.Guard;
        }

        private static void IssueAttackOrder(AIGMCompanionDakeyras companion, Mobile target)
        {
            companion.ControlTarget = target;
            companion.Combatant = target;
            companion.ControlOrder = OrderType.Attack;
        }

        private static void IssueStopCombatOrder(AIGMCompanionDakeyras companion)
        {
            companion.Combatant = null;
            companion.ControlTarget = null;
            companion.ControlOrder = OrderType.Stay;
        }
    }
}
