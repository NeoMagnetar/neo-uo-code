using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionCombatController
    {
        public static bool TryEngageMonster(BaseHire companion, Mobile target, out string result)
        {
            result = "engage_failed";

            if (companion == null || companion.Deleted || !companion.Alive)
            {
                result = "invalid_companion";
                return false;
            }

            AIGMCompanionTargetValidationResult validation = AIGMCompanionTargetValidator.ValidateMonsterTarget(companion, target, 24);
            if (!validation.Allowed)
            {
                result = validation.Reason;
                return false;
            }

            companion.Combatant = target;
            companion.ControlTarget = target;
            companion.ControlOrder = OrderType.Attack;
            companion.Warmode = true;
            result = "engaging_monster";
            return true;
        }

        public static void StopCombat(BaseHire companion)
        {
            if (companion == null || companion.Deleted)
                return;

            companion.ControlTarget = null;
            companion.Combatant = null;
            companion.Warmode = false;
            companion.ControlOrder = OrderType.Follow;
        }
    }
}
