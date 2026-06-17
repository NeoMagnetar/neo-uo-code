using System;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionCombatController
    {
        public static int GetNativeOrderRange()
        {
            return 24;
        }

        public static int GetPreferredEngagementRange(BaseHire companion)
        {
            BaseWeapon weapon = companion != null ? companion.Weapon as BaseWeapon : null;
            if (weapon is BaseRanged)
                return Math.Max(5, Math.Min(8, weapon.MaxRange));

            return 1;
        }

        public static bool TryEngageMonster(BaseHire companion, Mobile target, out string result)
        {
            result = "engage_failed";

            if (companion == null || companion.Deleted || !companion.Alive)
            {
                result = "invalid_companion";
                return false;
            }

            AIGMCompanionTargetValidationResult validation = AIGMCompanionTargetValidator.ValidateMonsterTarget(companion, target, GetNativeOrderRange());
            if (!validation.Allowed)
            {
                result = validation.Reason;
                return false;
            }

            return IssueNativeKillOrder(companion, target, "monster", out result);
        }

        public static bool TryEngageAnimal(BaseHire companion, Mobile target, out string result)
        {
            result = "engage_failed";

            if (companion == null || companion.Deleted || !companion.Alive)
            {
                result = "invalid_companion";
                return false;
            }

            AIGMCompanionTargetValidationResult validation = AIGMCompanionTargetValidator.ValidateAnimalTarget(companion, target, GetNativeOrderRange());
            if (!validation.Allowed)
            {
                result = validation.Reason;
                return false;
            }

            return IssueNativeKillOrder(companion, target, "animal", out result);
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

        private static string SafeLog(string value)
        {
            if (String.IsNullOrEmpty(value))
                return String.Empty;

            return value.Replace("\"", "'");
        }

        private static bool IssueNativeKillOrder(BaseHire companion, Mobile target, string targetKind, out string result)
        {
            result = "engage_failed";

            if (companion == null || companion.Deleted || target == null || target.Deleted || !target.Alive)
            {
                result = "invalid_target";
                return false;
            }

            if (target == companion || target == companion.GetOwner())
            {
                result = "blocked_self_or_owner";
                return false;
            }

            if (target.Map != companion.Map)
            {
                result = "different_map";
                return false;
            }

            Mobile commander = companion.GetOwner();
            if (commander == null || commander.Deleted || !commander.Alive)
            {
                result = "native_kill_no_commander";
                return false;
            }

            if (commander.Map != companion.Map || !commander.InRange(companion, 14))
            {
                result = "native_kill_commander_out_of_range";
                return false;
            }

            if (companion.AIObject == null)
            {
                result = "native_kill_no_ai";
                return false;
            }

            if (!companion.CanBeHarmful(target, false))
            {
                result = "cannot_be_harmful";
                return false;
            }

            int preferredRange = GetPreferredEngagementRange(companion);
            int distance = (int)Math.Round(companion.GetDistanceToSqrt(target));
            bool ranged = companion.Weapon is BaseRanged;

            companion.CantWalk = false;
            companion.AIObject.EndPickTarget(commander, target, OrderType.Attack);
            companion.AIObject.Obey();

            if (ranged)
            {
                if (distance > preferredRange)
                {
                    companion.CurrentSpeed = companion.ActiveSpeed;
                    result = "native_kill_pursuing_" + targetKind + "_at_range";
                    return true;
                }

                if (distance < Math.Max(2, preferredRange - 2))
                {
                    Direction away = target.GetDirectionTo(companion);
                    companion.Direction = away;
                    companion.CurrentSpeed = companion.ActiveSpeed;
                    result = "native_kill_spacing_" + targetKind + "_at_range";
                    return true;
                }
            }
            else if (!companion.InRange(target, 1))
            {
                companion.CurrentSpeed = companion.ActiveSpeed;

                if (companion is AIGMCompanionDardalion)
                {
                    companion.ControlTarget = target;
                }
            }

            result = "engaging_" + targetKind + "_native_kill";
            return true;
        }
    }
}
