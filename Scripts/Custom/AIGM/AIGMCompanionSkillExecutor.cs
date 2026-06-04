using System;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionSkillExecutor
    {
        public static bool TryHealTarget(BaseHire companion, Mobile target, bool preferBandages, out string response)
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

        public static bool TryCureTarget(BaseHire companion, Mobile target, out string response)
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

        public static bool TryUseHealingSkill(BaseHire companion, Mobile target, out string response)
        {
            Log("SKILL_EXEC_START method=TryUseHealingSkill");
            return TryHealTarget(companion, target, true, out response);
        }

        public static bool TryUseBandages(BaseHire companion, Mobile target, out string response)
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

            if (target != companion)
            {
                DateTime nextSupportActionUtc = GetNextSupportActionUtc(companion);
                if (DateTime.UtcNow < nextSupportActionUtc)
                {
                    response = "I need a moment before I can tend wounds again.";
                    Log("COOLDOWN_FAIL nextUtc=" + nextSupportActionUtc.ToString("o"));
                    return false;
                }
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

            if (target == companion)
            {
                if (DateTime.UtcNow < GetNextSupportActionUtc(companion))
                {
                    response = "I am not ready to bind another bandage yet.";
                    Log("BANDAGE_NATIVE_SKIP reason=self_cooldown nextUtc=" + GetNextSupportActionUtc(companion).ToString("o") + " healer=" + SafeName(companion));
                    return false;
                }

                if (BandageContext.GetContext(companion) != null)
                {
                    response = "I am already bandaging my wounds.";
                    Log("BANDAGE_NATIVE_SKIP reason=already_bandaging healer=" + SafeName(companion));
                    return false;
                }

                BandageContext context = BandageContext.BeginHeal(companion, companion, bandage is EnhancedBandage);
                if (context == null)
                {
                    response = "I cannot begin bandaging just now.";
                    Log("BANDAGE_FAIL reason=begin_heal_failed");
                    return false;
                }

                bandage.Consume();
                SetNextSupportActionUtc(companion, DateTime.UtcNow + TimeSpan.FromSeconds(10.0));
                response = "I begin tending to my wounds.";
                Log("BANDAGE_NATIVE_START healer=" + SafeName(companion) + " target=" + SafeName(target) + " amountRemaining=" + bandage.Amount + " nextUtc=" + GetNextSupportActionUtc(companion).ToString("o"));
                return true;
            }

            int healAmount = Math.Max(8, (int)(companion.Skills[SkillName.Healing].Value / 10.0));
            target.Hits = Math.Min(target.HitsMax, target.Hits + healAmount);
            bandage.Consume();
            SetNextSupportActionUtc(companion, DateTime.UtcNow + TimeSpan.FromSeconds(8.0));

            response = target == companion ? "I am tending to my wounds." : "Hold still while I bandage you.";
            Log("HEAL_APPLIED amount=" + healAmount + " targetHits=" + target.Hits + " targetMax=" + target.HitsMax);
            return true;
        }

        public static bool TryUseMageryHeal(BaseHire companion, Mobile target, out string response)
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

            DateTime nextSupportActionUtc = GetNextSupportActionUtc(companion);
            if (DateTime.UtcNow < nextSupportActionUtc)
            {
                response = "I need a moment before I can work healing magic again.";
                Log("MAGERY_HEAL_FAIL reason=cooldown nextUtc=" + nextSupportActionUtc.ToString("o"));
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
            SetNextSupportActionUtc(companion, DateTime.UtcNow + TimeSpan.FromSeconds(6.0));

            response = target == companion ? "I call healing magic upon myself." : "Stand fast while I cast a healing spell upon you.";
            Log("MAGERY_HEAL_APPLIED amount=" + healAmount + " targetHits=" + target.Hits + " targetMax=" + target.HitsMax);
            return true;
        }

        public static bool TryUseCurePotion(BaseHire companion, Mobile target, out string response)
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

            DateTime nextSupportActionUtc = GetNextSupportActionUtc(companion);
            if (DateTime.UtcNow < nextSupportActionUtc)
            {
                response = "I need a moment before I can use another draught.";
                Log("CURE_POTION_FAIL reason=cooldown nextUtc=" + nextSupportActionUtc.ToString("o"));
                return false;
            }

            SetNextSupportActionUtc(companion, DateTime.UtcNow + TimeSpan.FromSeconds(6.0));
            response = "I drink a cure draught to cleanse myself.";
            Log("CURE_POTION_APPLIED");
            return true;
        }

        public static bool TryUseMageryCure(BaseHire companion, Mobile target, out string response)
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

            DateTime nextSupportActionUtc = GetNextSupportActionUtc(companion);
            if (DateTime.UtcNow < nextSupportActionUtc)
            {
                response = "I need a moment before I can work curative magic again.";
                Log("MAGERY_CURE_FAIL reason=cooldown nextUtc=" + nextSupportActionUtc.ToString("o"));
                return false;
            }

            SetNextSupportActionUtc(companion, DateTime.UtcNow + TimeSpan.FromSeconds(6.0));
            response = target == companion ? "I call curative magic upon myself." : "Stand fast while I cast a cure spell upon you.";
            Log("MAGERY_CURE_APPLIED target=" + SafeName(target));
            return true;
        }

        public static bool CanUseBandages(BaseHire companion)
        {
            return FindBandages(companion) != null;
        }

        private static Bandage FindBandages(BaseHire companion)
        {
            if (companion == null || companion.Backpack == null)
                return null;

            Item[] items = companion.Backpack.FindItemsByType(typeof(Bandage), true);
            if (items == null || items.Length == 0)
                return null;

            return items[0] as Bandage;
        }

        private static bool TryStartNativeSelfBandage(BaseHire companion)
        {
            if (companion == null || companion.Deleted || !companion.Alive)
                return false;

            if (BandageContext.GetContext(companion) != null)
            {
                Log("BANDAGE_NATIVE_SKIP reason=already_bandaging healer=" + SafeName(companion));
                return false;
            }

            Bandage bandage = FindBandages(companion);
            if (bandage == null || bandage.Deleted || bandage.Amount <= 0)
            {
                Log("BANDAGE_NATIVE_SKIP reason=no_bandage healer=" + SafeName(companion));
                return false;
            }

            BandageContext context = BandageContext.BeginHeal(companion, companion, bandage is EnhancedBandage);
            if (context == null)
            {
                Log("BANDAGE_NATIVE_SKIP reason=begin_heal_failed healer=" + SafeName(companion));
                return false;
            }

            bandage.Consume();
            Log("BANDAGE_NATIVE_START healer=" + SafeName(companion) + " amountRemaining=" + bandage.Amount);
            return true;
        }

        private static bool IsUnderAttack(BaseHire companion)
        {
            if (companion == null || companion.Deleted)
                return false;

            Mobile combatant = companion.Combatant as Mobile;
            if (combatant != null && !combatant.Deleted && combatant.Alive)
                return true;

            Mobile owner = companion.GetOwner();
            Mobile ownerCombatant = owner != null ? owner.Combatant as Mobile : null;
            if (ownerCombatant != null && !ownerCombatant.Deleted && ownerCombatant.Alive && owner.InRange(companion, 10))
                return true;

            return false;
        }

        private static DateTime GetNextSupportActionUtc(BaseHire companion)
        {
            AIGMCompanionDakeyras dakeyras = companion as AIGMCompanionDakeyras;
            if (dakeyras != null)
                return dakeyras.NextSupportActionUtc;

            AIGMCompanionDanyal danyal = companion as AIGMCompanionDanyal;
            if (danyal != null)
                return danyal.NextSupportActionUtc;

            AIGMCompanionDardalion dardalion = companion as AIGMCompanionDardalion;
            if (dardalion != null)
                return dardalion.NextSupportActionUtc;

            return DateTime.MinValue;
        }

        private static void SetNextSupportActionUtc(BaseHire companion, DateTime value)
        {
            AIGMCompanionDakeyras dakeyras = companion as AIGMCompanionDakeyras;
            if (dakeyras != null)
            {
                dakeyras.NextSupportActionUtc = value;
                return;
            }

            AIGMCompanionDanyal danyal = companion as AIGMCompanionDanyal;
            if (danyal != null)
            {
                danyal.NextSupportActionUtc = value;
                return;
            }

            AIGMCompanionDardalion dardalion = companion as AIGMCompanionDardalion;
            if (dardalion != null)
                dardalion.NextSupportActionUtc = value;
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
