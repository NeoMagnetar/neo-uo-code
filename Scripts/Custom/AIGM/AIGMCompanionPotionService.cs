using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Spells;

namespace Server.Custom.AIGM
{
    public sealed class AIGMCompanionPotionState
    {
        public bool Enabled;
        public AIGMCompanionPotionProfile Profile = AIGMCompanionPotionProfile.DefaultBattleSupport();
        public DateTime NextPulseUtc = DateTime.MinValue;
        public DateTime NextCureUtc = DateTime.MinValue;
        public DateTime NextRefreshUtc = DateTime.MinValue;
        public DateTime NextBuffUtc = DateTime.MinValue;
        public DateTime NextNoPotionLogUtc = DateTime.MinValue;
        public string LastResult = "idle";
    }

    public static class AIGMCompanionPotionService
    {
        private static readonly Dictionary<Serial, AIGMCompanionPotionState> States = new Dictionary<Serial, AIGMCompanionPotionState>();

        public static bool IsPotionSupportEnabled(Mobile companion)
        {
            AIGMCompanionPotionState state = GetState(companion);
            return state != null && state.Enabled;
        }

        public static string SetPotionSupportEnabled(Mobile companion, bool enabled)
        {
            if (!IsValidCompanion(companion))
                return "Potion support is not available right now.";

            AIGMCompanionPotionState state = GetOrCreateState(companion);
            state.Enabled = enabled;
            state.LastResult = enabled ? "enabled" : "disabled";
            Log("AIGM_POTION_MODE companion={0} enabled={1}", Format(companion), enabled);
            return enabled ? "Potion support enabled." : "Potion support disabled.";
        }

        public static string SetPotionCategory(Mobile companion, string category, bool enabled)
        {
            if (!IsValidCompanion(companion))
                return "Potion support is not available right now.";

            AIGMCompanionPotionState state = GetOrCreateState(companion);
            string normalized = Normalize(category);
            if (normalized == "combat" || normalized == "buff" || normalized == "buffs")
                state.Profile.UseCombatBuffs = enabled;
            else if (normalized == "heal" || normalized == "healing")
                state.Profile.UseHeal = enabled;
            else if (normalized == "cure")
                state.Profile.UseCure = enabled;
            else if (normalized == "refresh" || normalized == "stamina")
                state.Profile.UseRefresh = enabled;

            state.Enabled = true;
            state.LastResult = "category_" + normalized + "_" + enabled;
            return GetPotionStatus(companion);
        }

        public static string GetPotionStatus(Mobile companion)
        {
            if (!IsValidCompanion(companion))
                return "Potion support is not available right now.";

            AIGMCompanionPotionState state = GetState(companion);
            bool enabled = state != null && state.Enabled;
            AIGMCompanionPotionProfile profile = state != null && state.Profile != null ? state.Profile : AIGMCompanionPotionProfile.DefaultBattleSupport();

            return String.Format("potions={0} cure={1} heal={2} refresh={3} buffs={4} counts={5} last={6}",
                enabled ? "on" : "off",
                profile.UseCure ? "on" : "off",
                profile.UseHeal ? "on" : "off",
                profile.UseRefresh ? "on" : "off",
                profile.UseCombatBuffs ? "on" : "off",
                BuildPotionCountSummary(companion),
                state != null ? state.LastResult : "idle");
        }

        public static void Pulse(BaseHire companion)
        {
            if (!IsValidCompanion(companion))
                return;

            AIGMCompanionPotionState state = GetState(companion);
            if (state == null || !state.Enabled)
                return;

            DateTime now = DateTime.UtcNow;
            if (now < state.NextPulseUtc)
                return;

            state.NextPulseUtc = now + AIGMCompanionPotionPolicy.PulseInterval;
            string result;
            TryUseBestPotion(companion, false, out result);
            if (!String.IsNullOrWhiteSpace(result))
                state.LastResult = result;
        }

        public static bool TryUseBestPotion(Mobile companion, bool manual, out string result)
        {
            result = null;
            if (!IsValidCompanion(companion))
            {
                result = "invalid_companion";
                return false;
            }

            AIGMCompanionPotionState state = GetOrCreateState(companion);
            AIGMCompanionPotionProfile profile = state.Profile ?? AIGMCompanionPotionProfile.DefaultBattleSupport();

            if ((manual || profile.UseCure) && TryUsePotionForPoison(companion, out result))
                return true;

            if ((manual || profile.UseHeal) && IsEmergencyDamaged(companion) && TryUsePotionForDamage(companion, out result))
                return true;

            if ((manual || profile.UseRefresh) && TryUsePotionForStamina(companion, out result))
                return true;

            if ((manual || profile.UseCombatBuffs) && TryUsePotionForCombatBuff(companion, out result))
                return true;

            if ((manual || profile.UseHeal) && TryUsePotionForDamage(companion, out result))
                return true;

            if (String.IsNullOrWhiteSpace(result))
                result = "no_potion_needed";

            state.LastResult = result;
            return false;
        }

        public static bool TryUsePotionForCombatBuff(Mobile companion, out string result)
        {
            result = null;
            if (!IsValidCompanion(companion))
                return false;

            if (!IsCombatRelevant(companion))
            {
                result = "combat_buff_not_needed";
                return false;
            }

            AIGMCompanionPotionState state = GetOrCreateState(companion);
            DateTime now = DateTime.UtcNow;
            if (now < state.NextBuffUtc)
            {
                result = "potion_cooldown_active";
                return false;
            }

            bool usedAny = false;
            string usedNames = String.Empty;

            if (SpellHelper.GetBuffOffset(companion, StatType.Str) <= 0)
            {
                BaseStrengthPotion strength = FindBestStrengthPotion(companion);
                if (strength != null && DrinkPotion(companion, strength, out result))
                {
                    usedAny = true;
                    usedNames = "strength";
                }
            }

            if (SpellHelper.GetBuffOffset(companion, StatType.Dex) <= 0)
            {
                BaseAgilityPotion agility = FindBestAgilityPotion(companion);
                if (agility != null && DrinkPotion(companion, agility, out result))
                {
                    usedAny = true;
                    usedNames = String.IsNullOrWhiteSpace(usedNames) ? "agility" : usedNames + "_agility";
                }
            }

            if (usedAny)
            {
                state.NextBuffUtc = now + AIGMCompanionPotionPolicy.BuffAttemptCooldown;
                state.LastResult = usedNames == "strength_agility" ? "combat_strength_agility_used" : "combat_" + usedNames + "_used";
                result = state.LastResult;
                return true;
            }

            result = "combat_buffs_unavailable_or_active";
            return false;
        }

        public static bool TryUsePotionForPoison(Mobile companion, out string result)
        {
            result = null;
            if (!IsValidCompanion(companion) || !companion.Poisoned)
            {
                result = "not_poisoned";
                return false;
            }

            AIGMCompanionPotionState state = GetOrCreateState(companion);
            DateTime now = DateTime.UtcNow;
            if (now < state.NextCureUtc)
            {
                result = "potion_cooldown_active";
                return false;
            }

            BaseCurePotion potion = FindBestCurePotion(companion);
            if (potion == null)
            {
                LogNoPotion(companion, state, "cure");
                result = "no_cure_potion_available";
                return false;
            }

            bool drank = DrinkPotion(companion, potion, out result);
            state.NextCureUtc = now + AIGMCompanionPotionPolicy.CureCooldown;
            state.LastResult = drank ? "cure_potion_used" : result;
            return drank;
        }

        public static bool TryUsePotionForDamage(Mobile companion, out string result)
        {
            result = null;
            if (!IsValidCompanion(companion) || companion.Hits >= companion.HitsMax)
            {
                result = "heal_not_needed";
                return false;
            }

            if (companion.Poisoned || MortalStrike.IsWounded(companion))
            {
                result = "heal_blocked_by_state";
                return false;
            }

            if (!IsDamagedEnough(companion))
            {
                result = "heal_threshold_not_met";
                return false;
            }

            if (!companion.CanBeginAction(typeof(BaseHealPotion)))
            {
                result = "potion_cooldown_active";
                return false;
            }

            BaseHealPotion potion = FindBestHealPotion(companion);
            if (potion == null)
            {
                LogNoPotion(companion, GetOrCreateState(companion), "heal");
                result = "no_heal_potion_available";
                return false;
            }

            bool drank = DrinkPotion(companion, potion, out result);
            GetOrCreateState(companion).LastResult = drank ? "heal_potion_used" : result;
            return drank;
        }

        public static bool TryUsePotionForStamina(Mobile companion, out string result)
        {
            result = null;
            if (!IsValidCompanion(companion) || companion.StamMax <= 0 || companion.Stam >= companion.StamMax)
            {
                result = "refresh_not_needed";
                return false;
            }

            if (((double)companion.Stam / Math.Max(1, companion.StamMax)) > AIGMCompanionPotionPolicy.RefreshThreshold)
            {
                result = "refresh_threshold_not_met";
                return false;
            }

            AIGMCompanionPotionState state = GetOrCreateState(companion);
            DateTime now = DateTime.UtcNow;
            if (now < state.NextRefreshUtc)
            {
                result = "potion_cooldown_active";
                return false;
            }

            BaseRefreshPotion potion = FindBestRefreshPotion(companion);
            if (potion == null)
            {
                LogNoPotion(companion, state, "refresh");
                result = "no_refresh_potion_available";
                return false;
            }

            bool drank = DrinkPotion(companion, potion, out result);
            state.NextRefreshUtc = now + AIGMCompanionPotionPolicy.RefreshCooldown;
            state.LastResult = drank ? "refresh_potion_used" : result;
            return drank;
        }

        public static bool CanDrinkPotion(Mobile companion, Item potion, out string reason)
        {
            reason = null;
            if (!IsValidCompanion(companion))
            {
                reason = "invalid_companion";
                return false;
            }

            if (potion == null || potion.Deleted || !(potion is BasePotion))
            {
                reason = "invalid_potion";
                return false;
            }

            if (companion.Backpack == null || potion.Parent == null || !potion.IsChildOf(companion.Backpack))
            {
                reason = "not_in_companion_backpack";
                return false;
            }

            if (!potion.Movable)
            {
                reason = "potion_not_movable";
                return false;
            }

            reason = "ok";
            return true;
        }

        public static bool DrinkPotion(Mobile companion, Item potion, out string result)
        {
            string reason;
            if (!CanDrinkPotion(companion, potion, out reason))
            {
                result = reason;
                return false;
            }

            string typeName = potion.GetType().Name;
            int beforeAmount = potion.Amount;
            ((BasePotion)potion).Drink(companion);
            bool consumed = potion.Deleted || potion.Amount < beforeAmount || potion.Parent == null;
            result = consumed ? "used_" + typeName : "not_consumed_" + typeName;
            Log("AIGM_POTION_USE companion={0} potion={1} consumed={2} result={3}", Format(companion), typeName, consumed, result);
            return consumed;
        }

        public static string BuildVisibleResponse(Mobile companion, string result)
        {
            string id = GetCompanionId(companion);
            string normalized = Normalize(result);
            if (normalized.Contains("cure"))
                return id == "dardalion" ? "The poison must be answered quickly." : "Cure first.";
            if (normalized.Contains("heal"))
                return id == "danyal" ? "Drink it now. You're no use on the ground." : "Healing now.";
            if (normalized.Contains("refresh"))
                return "Stamina first.";
            if (normalized.Contains("strength") || normalized.Contains("agility") || normalized.Contains("buff"))
                return id == "dakeyras" ? "Strength first. Then we move." : "Battle draughts ready.";
            if (normalized.Contains("no_") || normalized.Contains("unavailable"))
                return "No useful potion in my pack.";
            return "Potion support ready.";
        }

        public static string BuildPotionCountSummary(Mobile companion)
        {
            if (companion == null || companion.Backpack == null)
                return "none";

            return String.Format("cure={0},heal={1},refresh={2},str={3},agi={4}",
                Count<BaseCurePotion>(companion),
                Count<BaseHealPotion>(companion),
                Count<BaseRefreshPotion>(companion),
                Count<BaseStrengthPotion>(companion),
                Count<BaseAgilityPotion>(companion));
        }

        private static bool IsDamagedEnough(Mobile companion)
        {
            if (companion == null || companion.HitsMax <= 0)
                return false;
            return ((double)companion.Hits / companion.HitsMax) <= AIGMCompanionPotionPolicy.HealThreshold;
        }

        private static bool IsEmergencyDamaged(Mobile companion)
        {
            if (companion == null || companion.HitsMax <= 0)
                return false;
            return ((double)companion.Hits / companion.HitsMax) <= AIGMCompanionPotionPolicy.EmergencyHealThreshold;
        }

        private static bool IsCombatRelevant(Mobile companion)
        {
            Mobile combatant = companion != null ? companion.Combatant as Mobile : null;
            if (combatant != null && !combatant.Deleted && combatant.Alive && combatant.Map == companion.Map && companion.InRange(combatant, 12))
                return true;

            return companion != null && companion.Warmode;
        }

        private static T FindBestPotion<T>(Mobile companion, Type[] order) where T : Item
        {
            if (companion == null || companion.Backpack == null || order == null)
                return null;

            for (int i = 0; i < order.Length; i++)
            {
                Item[] items = companion.Backpack.FindItemsByType(order[i], true);
                if (items == null)
                    continue;

                for (int j = 0; j < items.Length; j++)
                {
                    T potion = items[j] as T;
                    if (potion != null && !potion.Deleted)
                        return potion;
                }
            }

            return null;
        }

        private static BaseCurePotion FindBestCurePotion(Mobile companion)
        {
            return FindBestPotion<BaseCurePotion>(companion, new[] { typeof(GreaterCurePotion), typeof(CurePotion), typeof(LesserCurePotion) });
        }

        private static BaseHealPotion FindBestHealPotion(Mobile companion)
        {
            return FindBestPotion<BaseHealPotion>(companion, new[] { typeof(GreaterHealPotion), typeof(HealPotion), typeof(LesserHealPotion) });
        }

        private static BaseRefreshPotion FindBestRefreshPotion(Mobile companion)
        {
            return FindBestPotion<BaseRefreshPotion>(companion, new[] { typeof(TotalRefreshPotion), typeof(RefreshPotion) });
        }

        private static BaseStrengthPotion FindBestStrengthPotion(Mobile companion)
        {
            return FindBestPotion<BaseStrengthPotion>(companion, new[] { typeof(GreaterStrengthPotion), typeof(StrengthPotion) });
        }

        private static BaseAgilityPotion FindBestAgilityPotion(Mobile companion)
        {
            return FindBestPotion<BaseAgilityPotion>(companion, new[] { typeof(GreaterAgilityPotion), typeof(AgilityPotion) });
        }

        private static int Count<T>(Mobile companion) where T : Item
        {
            if (companion == null || companion.Backpack == null)
                return 0;

            Item[] items = companion.Backpack.FindItemsByType(typeof(T), true);
            return items != null ? items.Length : 0;
        }

        private static bool IsValidCompanion(Mobile companion)
        {
            return companion != null && !companion.Deleted && companion.Alive && companion is BaseHire && companion is IAIGMCompanionActor;
        }

        private static AIGMCompanionPotionState GetState(Mobile companion)
        {
            if (companion == null)
                return null;

            AIGMCompanionPotionState state;
            States.TryGetValue(companion.Serial, out state);
            return state;
        }

        private static AIGMCompanionPotionState GetOrCreateState(Mobile companion)
        {
            AIGMCompanionPotionState state = GetState(companion);
            if (state == null)
            {
                state = new AIGMCompanionPotionState();
                States[companion.Serial] = state;
            }

            return state;
        }

        private static void LogNoPotion(Mobile companion, AIGMCompanionPotionState state, string kind)
        {
            if (state == null)
                return;

            DateTime now = DateTime.UtcNow;
            if (now < state.NextNoPotionLogUtc)
                return;

            state.NextNoPotionLogUtc = now + AIGMCompanionPotionPolicy.NoPotionLogCooldown;
            Log("AIGM_POTION_NONE companion={0} kind={1}", Format(companion), kind);
        }

        private static string GetCompanionId(Mobile companion)
        {
            IAIGMCompanionActor actor = companion as IAIGMCompanionActor;
            return actor != null && actor.CompanionId != null ? actor.CompanionId.ToLowerInvariant() : String.Empty;
        }

        private static string Format(Mobile mobile)
        {
            return mobile == null ? "null" : String.Format("{0}/{1}", mobile.Name, mobile.Serial.Value);
        }

        private static string Normalize(string text)
        {
            return String.IsNullOrWhiteSpace(text) ? String.Empty : text.Trim().ToLowerInvariant();
        }

        private static void Log(string format, params object[] args)
        {
            AIGMExecutionLog.Write(format, args);
        }
    }
}
