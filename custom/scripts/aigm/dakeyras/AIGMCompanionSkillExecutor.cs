using System;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionSkillExecutor
    {
        public static bool TryHealTarget(AIGMCompanionDakeyras companion, Mobile target, bool preferBandages, out string response)
        {
            Log("SKILL_EXEC_START method=TryHealTarget preferBandages=" + preferBandages);
            Log("SKILL_TARGET target=" + SafeName(target));
            response = null;

            if (companion == null || target == null || target.Deleted || !target.Alive)
            {
                response = "That healing target is invalid.";
                Log("HEAL_FAIL reason=invalid_target");
                return false;
            }

            if (preferBandages || CanUseBandages(companion))
                return TryUseBandages(companion, target, out response);

            response = "I do not have a working healing method right now.";
            Log("HEAL_FAIL reason=no_healing_method");
            return false;
        }

        public static bool TryCureTarget(AIGMCompanionDakeyras companion, Mobile target, out string response)
        {
            Log("SKILL_EXEC_START method=TryCureTarget");
            Log("SKILL_TARGET target=" + SafeName(target));
            response = null;

            if (companion == null || target == null || target.Deleted || !target.Alive)
            {
                response = "That cure target is invalid.";
                Log("CURE_FAIL reason=invalid_target");
                return false;
            }

            response = "I am not yet trained to cast curative magic.";
            Log("CURE_FAIL reason=not_trained");
            return false;
        }

        public static bool TryUseHealingSkill(AIGMCompanionDakeyras companion, Mobile target, out string response)
        {
            Log("SKILL_EXEC_START method=TryUseHealingSkill");
            return TryHealTarget(companion, target, true, out response);
        }

        public static bool TryUseBandages(AIGMCompanionDakeyras companion, Mobile target, out string response)
        {
            Log("SKILL_EXEC_START method=TryUseBandages");
            Log("SKILL_TARGET target=" + SafeName(target));
            response = null;

            if (companion == null || target == null || target.Deleted || !target.Alive)
            {
                response = "That healing target is invalid.";
                Log("BANDAGE_FAIL reason=invalid_target");
                return false;
            }

            if (DateTime.UtcNow < companion.NextSupportActionUtc)
            {
                response = "I need a moment before I can tend wounds again.";
                Log("COOLDOWN_FAIL nextUtc=" + companion.NextSupportActionUtc.ToString("o"));
                return false;
            }
            Log("COOLDOWN_OK");

            Bandage bandage = FindBandages(companion);
            if (bandage == null || bandage.Amount <= 0)
            {
                response = "I do not have any bandages.";
                Log("BANDAGE_MISSING");
                return false;
            }
            Log("BANDAGE_FOUND serial=0x" + bandage.Serial.Value.ToString("X8") + " amount=" + bandage.Amount);

            if (!companion.InRange(target, 2))
            {
                response = target == companion ? "I need a moment to tend to myself." : "You are too far away for bandaging.";
                Log("RANGE_FAIL distance=" + companion.GetDistanceToSqrt(target));
                return false;
            }
            Log("RANGE_OK");

            if (target.Hits >= target.HitsMax)
            {
                response = target == companion ? "I do not need bandaging." : "You do not need bandaging.";
                Log("HEAL_NOT_NEEDED hits=" + target.Hits + " max=" + target.HitsMax);
                return false;
            }

            int healAmount = Math.Max(8, (int)(companion.Skills[SkillName.Healing].Value / 10.0));
            target.Hits = Math.Min(target.HitsMax, target.Hits + healAmount);
            bandage.Consume();
            companion.NextSupportActionUtc = DateTime.UtcNow + TimeSpan.FromSeconds(8.0);

            response = target == companion ? "I am tending to my wounds." : "Hold still while I bandage you.";
            Log("HEAL_APPLIED amount=" + healAmount + " targetHits=" + target.Hits + " targetMax=" + target.HitsMax);
            return true;
        }

        public static bool TryUseMageryHeal(AIGMCompanionDakeyras companion, Mobile target, out string response)
        {
            Log("SKILL_EXEC_START method=TryUseMageryHeal");
            Log("SKILL_TARGET target=" + SafeName(target));
            response = null;

            if (companion == null || target == null || target.Deleted || !target.Alive)
            {
                response = "That healing target is invalid.";
                Log("MAGERY_HEAL_FAIL reason=invalid_target");
                return false;
            }

            if (DateTime.UtcNow < companion.NextSupportActionUtc)
            {
                response = "I need a moment before I can work healing magic again.";
                Log("MAGERY_HEAL_FAIL reason=cooldown nextUtc=" + companion.NextSupportActionUtc.ToString("o"));
                return false;
            }

            if (target == companion)
            {
                if (target.Hits >= target.HitsMax)
                {
                    response = "I do not need healing magic.";
                    Log("MAGERY_HEAL_FAIL reason=not_needed_self hits=" + target.Hits + " max=" + target.HitsMax);
                    return false;
                }
            }
            else
            {
                if (!companion.InRange(target, 8))
                {
                    response = "You are too far away for healing magic.";
                    Log("MAGERY_HEAL_FAIL reason=range distance=" + companion.GetDistanceToSqrt(target));
                    return false;
                }

                if (target.Hits >= target.HitsMax)
                {
                    response = "You do not need healing magic.";
                    Log("MAGERY_HEAL_FAIL reason=not_needed_target hits=" + target.Hits + " max=" + target.HitsMax);
                    return false;
                }
            }

            int healAmount = Math.Max(10, (int)(companion.Skills[SkillName.Healing].Value / 8.0));
            target.Hits = Math.Min(target.HitsMax, target.Hits + healAmount);
            companion.NextSupportActionUtc = DateTime.UtcNow + TimeSpan.FromSeconds(6.0);

            response = target == companion ? "I call healing magic upon myself." : "Stand fast while I cast a healing spell upon you.";
            Log("MAGERY_HEAL_APPLIED amount=" + healAmount + " targetHits=" + target.Hits + " targetMax=" + target.HitsMax);
            return true;
        }

        public static bool TryUseCurePotion(AIGMCompanionDakeyras companion, Mobile target, out string response)
        {
            Log("SKILL_EXEC_START method=TryUseCurePotion");
            Log("SKILL_TARGET target=" + SafeName(target));
            response = null;

            if (companion == null || target == null || target.Deleted || !target.Alive)
            {
                response = "That cure target is invalid.";
                Log("CURE_POTION_FAIL reason=invalid_target");
                return false;
            }

            if (target != companion)
            {
                response = "I only quaff cure draughts for myself.";
                Log("CURE_POTION_FAIL reason=target_not_self");
                return false;
            }

            if (DateTime.UtcNow < companion.NextSupportActionUtc)
            {
                response = "I need a moment before I can use another draught.";
                Log("CURE_POTION_FAIL reason=cooldown nextUtc=" + companion.NextSupportActionUtc.ToString("o"));
                return false;
            }

            companion.NextSupportActionUtc = DateTime.UtcNow + TimeSpan.FromSeconds(6.0);
            response = "I drink a cure draught to cleanse myself.";
            Log("CURE_POTION_APPLIED");
            return true;
        }

        public static bool TryUseMageryCure(AIGMCompanionDakeyras companion, Mobile target, out string response)
        {
            Log("SKILL_EXEC_START method=TryUseMageryCure");
            Log("SKILL_TARGET target=" + SafeName(target));
            response = null;

            if (companion == null || target == null || target.Deleted || !target.Alive)
            {
                response = "That cure target is invalid.";
                Log("MAGERY_CURE_FAIL reason=invalid_target");
                return false;
            }

            if (!companion.InRange(target, 8))
            {
                response = "You are too far away for curative magic.";
                Log("MAGERY_CURE_FAIL reason=range distance=" + companion.GetDistanceToSqrt(target));
                return false;
            }

            if (DateTime.UtcNow < companion.NextSupportActionUtc)
            {
                response = "I need a moment before I can work curative magic again.";
                Log("MAGERY_CURE_FAIL reason=cooldown nextUtc=" + companion.NextSupportActionUtc.ToString("o"));
                return false;
            }

            companion.NextSupportActionUtc = DateTime.UtcNow + TimeSpan.FromSeconds(6.0);
            response = target == companion ? "I call curative magic upon myself." : "Stand fast while I cast a cure spell upon you.";
            Log("MAGERY_CURE_APPLIED target=" + SafeName(target));
            return true;
        }

        public static bool CanUseBandages(AIGMCompanionDakeyras companion)
        {
            return FindBandages(companion) != null;
        }

        private static Bandage FindBandages(AIGMCompanionDakeyras companion)
        {
            if (companion == null || companion.Backpack == null)
                return null;

            Item[] items = companion.Backpack.FindItemsByType(typeof(Bandage), true);
            if (items == null || items.Length == 0)
                return null;

            return items[0] as Bandage;
        }

        private static void Log(string message)
        {
            try
            {
                string path = System.IO.Path.Combine(Core.BaseDirectory, "Logs", "AIGMExecution.log");
                System.IO.File.AppendAllText(path, DateTime.UtcNow.ToString("o") + " " + (message ?? String.Empty) + Environment.NewLine);
            }
            catch
            {
            }
        }

        private static string SafeName(Mobile mob)
        {
            if (mob == null)
                return "(null)";

            return (mob.Name ?? mob.GetType().Name) + "[0x" + mob.Serial.Value.ToString("X8") + "]";
        }
    }
}
