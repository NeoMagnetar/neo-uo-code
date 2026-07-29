using System;
using System.Collections.Generic;
using Server.Mobiles;
using Server.Spells;
using Server.Spells.First;
using Server.Spells.Fourth;
using Server.Spells.Second;
using Server.Spells.Third;
using Server.Custom.AIGM.UMG;
using Server.Targeting;

namespace Server.Custom.AIGM
{
    public sealed class AIGMCompanionSpellState
    {
        public bool Enabled;
        public AIGMCompanionSpellProfile Profile = AIGMCompanionSpellProfile.SupportOnly();
        public DateTime NextPulseUtc = DateTime.MinValue;
        public DateTime NextCastUtc = DateTime.MinValue;
        public DateTime NextFailureLogUtc = DateTime.MinValue;
        public string LastResult = "idle";
    }

    public static class AIGMCompanionSpellService
    {
        private static readonly Dictionary<Serial, AIGMCompanionSpellState> States = new Dictionary<Serial, AIGMCompanionSpellState>();

        public static bool IsSpellSupportEnabled(Mobile companion)
        {
            AIGMCompanionSpellState state = GetState(companion);
            return state != null && state.Enabled;
        }

        public static string SetSpellSupportEnabled(Mobile companion, bool enabled, AIGMCompanionSpellProfile profile)
        {
            if (!IsValidCompanion(companion))
                return "Spell support is not available right now.";

            AIGMCompanionSpellState state = GetOrCreateState(companion);
            state.Enabled = enabled;
            if (profile != null)
                state.Profile = profile;
            state.LastResult = enabled ? "enabled" : "disabled";
            Log("AIGM_SPELL_MODE companion={0} enabled={1} profile={2}", Format(companion), enabled, state.Profile != null ? state.Profile.Name : "none");
            return enabled ? "Spell support enabled." : "Spell support disabled.";
        }

        public static string SetSpellProfile(Mobile companion, AIGMCompanionSpellProfile profile)
        {
            if (!IsValidCompanion(companion))
                return "Spell support is not available right now.";

            AIGMCompanionSpellState state = GetOrCreateState(companion);
            state.Profile = profile ?? AIGMCompanionSpellProfile.SupportOnly();
            state.Enabled = state.Profile.Name != "none";
            state.LastResult = "profile_" + state.Profile.Name;
            return GetSpellStatus(companion);
        }

        public static string GetSpellStatus(Mobile companion)
        {
            if (!IsValidCompanion(companion))
                return "Spell support is not available right now.";

            AIGMCompanionSpellState state = GetState(companion);
            AIGMCompanionSpellProfile profile = state != null && state.Profile != null ? state.Profile : AIGMCompanionSpellProfile.SupportOnly();
            return String.Format("spells={0} profile={1} magery={2:0.0} mana={3}/{4} cure={5} heal={6} bless={7} last={8}",
                state != null && state.Enabled ? "on" : "off",
                profile.Name,
                companion.Skills[SkillName.Magery].Value,
                companion.Mana,
                companion.ManaMax,
                profile.AllowCure ? "on" : "off",
                profile.AllowHeal ? "on" : "off",
                profile.AllowBless ? "on" : "off",
                state != null ? state.LastResult : "idle");
        }

        public static void PulseSpellSupport(BaseHire companion)
        {
            if (!IsValidCompanion(companion))
                return;

            AIGMCompanionSpellState state = GetState(companion);
            if (state == null || !state.Enabled)
                return;

            DateTime now = DateTime.UtcNow;
            if (now < state.NextPulseUtc)
                return;

            state.NextPulseUtc = now + AIGMCompanionSpellPolicy.PulseInterval;
            string result;
            TryCastBestSupportSpell(companion, false, out result);
        }

        public static bool TryCastBestSupportSpell(Mobile caster, bool manual, out string result)
        {
            result = null;
            if (!IsValidCompanion(caster))
            {
                result = "invalid_caster";
                return false;
            }

            AIGMCompanionSpellState state = GetOrCreateState(caster);
            AIGMCompanionSpellProfile profile = state.Profile ?? AIGMCompanionSpellProfile.SupportOnly();
            if (!manual && (profile.Name == "none" || !state.Enabled))
            {
                result = "spell_support_disabled";
                return false;
            }

            Mobile target = FindPoisonedAlly(caster);
            if ((manual || profile.AllowCure) && target != null && TryCastCure(caster, target, out result))
                return true;

            target = FindEmergencyHealTarget(caster);
            if ((manual || profile.AllowHeal) && target != null && TryCastHeal(caster, target, out result))
                return true;

            target = FindHealTarget(caster);
            if ((manual || profile.AllowHeal) && target != null && TryCastHeal(caster, target, out result))
                return true;

            target = FindBlessTarget(caster);
            if ((manual || profile.AllowBless) && target != null && TryCastBuff(caster, target, out result))
                return true;

            result = "no_support_spell_needed";
            state.LastResult = result;
            return false;
        }

        public static bool TryCastCure(Mobile caster, Mobile target, out string result)
        {
            CureSpell spell = new CureSpell(caster, null);
            return TryBeginTargetedSpell(caster, target, spell, "cure", out result);
        }

        public static bool TryCastHeal(Mobile caster, Mobile target, out string result)
        {
            Spell spell = caster.Skills[SkillName.Magery].Value >= 45.0 ? (Spell)new GreaterHealSpell(caster, null) : new HealSpell(caster, null);
            return TryBeginTargetedSpell(caster, target, spell, spell is GreaterHealSpell ? "greater_heal" : "heal", out result);
        }

        public static bool TryCastBuff(Mobile caster, Mobile target, out string result)
        {
            if (HasBlessStatBuff(target))
            {
                result = "bless_already_active";
                return false;
            }

            BlessSpell spell = new BlessSpell(caster, null);
            return TryBeginTargetedSpell(caster, target, spell, "bless", out result);
        }

        public static bool CanCastSpell(Mobile caster, string spellName, out string reason)
        {
            Spell spell = CreateSpell(caster, spellName);
            if (spell == null)
            {
                reason = "spell_not_supported";
                return false;
            }

            return CanCastSpell(caster, spell, out reason);
        }

        public static bool HasRequiredMana(Mobile caster, string spellName)
        {
            Spell spell = CreateSpell(caster, spellName);
            return spell != null && caster != null && caster.Mana >= spell.ScaleMana(spell.GetMana());
        }

        public static bool HasRequiredReagents(Mobile caster, string spellName)
        {
            if (caster == null)
                return false;
            return !caster.Player;
        }

        public static bool IsSpellOnCooldown(Mobile caster, string spellName)
        {
            AIGMCompanionSpellState state = GetState(caster);
            return state != null && DateTime.UtcNow < state.NextCastUtc;
        }

        public static void RecordSpellCooldown(Mobile caster, string spellName)
        {
            AIGMCompanionSpellState state = GetOrCreateState(caster);
            state.NextCastUtc = DateTime.UtcNow + AIGMCompanionSpellPolicy.CastCooldown;
        }

        public static string BuildVisibleResponse(Mobile caster, string result)
        {
            string normalized = Normalize(result);
            if (normalized.Contains("cure"))
                return "The poison must be answered.";
            if (normalized.Contains("heal"))
                return "Hold. I will mend what I can.";
            if (normalized.Contains("bless"))
                return "Stand beneath the Source a moment.";
            if (normalized.Contains("mana"))
                return "I lack the mana for it.";
            if (normalized.Contains("skill"))
                return "That working is beyond me for now.";
            if (normalized.Contains("cooldown") || normalized.Contains("casting"))
                return "A moment. The working has not settled.";
            return "Support magic is ready.";
        }

        public static bool ShouldRouteSpellCommand(BaseHire caster, Mobile speaker, string rawSpeech)
        {
            if (!IsValidCompanion(caster) || speaker == null || String.IsNullOrWhiteSpace(rawSpeech))
                return false;

            string speech = NormalizeCommand(rawSpeech);
            return speech.Contains("with magic")
                || speech.Contains("support magic")
                || speech.Contains("use spells")
                || speech.Contains("spell status")
                || speech.Contains("spells status")
                || speech.Contains("stop casting")
                || speech.Contains("stop using spells")
                || speech.StartsWith("bless ", StringComparison.Ordinal)
                || speech.StartsWith("cast heal", StringComparison.Ordinal)
                || speech.StartsWith("cast cure", StringComparison.Ordinal);
        }

        public static bool TryHandleExplicitSpellCommand(BaseHire caster, Mobile speaker, string rawSpeech, out string response)
        {
            response = null;
            if (!ShouldRouteSpellCommand(caster, speaker, rawSpeech))
                return false;

            string speech = NormalizeCommand(rawSpeech);
            if (speech.Contains("stop casting") || speech.Contains("stop using spells"))
            {
                response = SetSpellSupportEnabled(caster, false, null);
                return true;
            }

            if (speech.Contains("spell status") || speech.Contains("spells status"))
            {
                response = GetSpellStatus(caster);
                return true;
            }

            if (speech.Contains("use spells") || speech.Contains("support magic"))
            {
                response = SetSpellSupportEnabled(caster, true, AIGMCompanionSpellProfile.SupportOnly());
                return true;
            }

            Mobile target = ResolveNamedTarget(caster, speaker, speech);
            if (target == null)
                target = speaker;

            string result;
            bool started;
            if (speech.Contains("cure"))
                started = TryCastCure(caster, target, out result);
            else if (speech.Contains("bless"))
                started = TryCastBuff(caster, target, out result);
            else
                started = TryCastHeal(caster, target, out result);

            response = BuildVisibleResponse(caster, result);
            return true;
        }

        private static bool TryBeginTargetedSpell(Mobile caster, Mobile target, Spell spell, string spellKey, out string result)
        {
            result = null;
            string reason;
            if (!CanCastSpell(caster, spell, out reason))
            {
                result = reason;
                RecordFailure(caster, spellKey, reason);
                return false;
            }

            if (!IsValidTarget(caster, target))
            {
                result = "invalid_target";
                return false;
            }

            if (!caster.InRange(target, 10) || !caster.CanSee(target))
            {
                result = "target_out_of_range_or_sight";
                return false;
            }

            if (!spell.Cast())
            {
                result = "spell_cast_start_failed";
                RecordFailure(caster, spellKey, result);
                return false;
            }

            AIGMCompanionSpellState state = GetOrCreateState(caster);
            state.NextCastUtc = DateTime.UtcNow + AIGMCompanionSpellPolicy.CastCooldown;
            state.LastResult = spellKey + "_cast_started";
            TimeSpan delay = spell.GetCastDelay() + AIGMCompanionSpellPolicy.TargetInvokePadding;
            Timer.DelayCall(delay, delegate { InvokeSpellTarget(caster, target, spellKey); });
            result = state.LastResult;
            Log("AIGM_SPELL_CAST_START caster={0} target={1} spell={2} directive={3} delayMs={4}", Format(caster), Format(target), spellKey, FindAbilityDirectiveId(caster, spellKey), (int)delay.TotalMilliseconds);
            return true;
        }

        private static void InvokeSpellTarget(Mobile caster, Mobile target, string spellKey)
        {
            if (caster == null || caster.Deleted || target == null || target.Deleted)
                return;

            Target activeTarget = caster.Target;
            if (activeTarget == null)
            {
                Log("AIGM_SPELL_TARGET_SKIP caster={0} spell={1} reason=no_active_target", Format(caster), spellKey);
                return;
            }

            activeTarget.Invoke(caster, target);
            AIGMCompanionSpellState state = GetOrCreateState(caster);
            state.LastResult = spellKey + "_cast_complete";
            Log("AIGM_SPELL_CAST_DONE caster={0} target={1} spell={2}", Format(caster), Format(target), spellKey);
        }

        private static bool CanCastSpell(Mobile caster, Spell spell, out string reason)
        {
            reason = null;
            if (!IsValidCompanion(caster))
            {
                reason = "invalid_caster";
                return false;
            }

            if (spell == null)
            {
                reason = "spell_not_supported";
                return false;
            }

            if (caster.Spell != null && caster.Spell.IsCasting)
            {
                reason = "already_casting";
                return false;
            }

            if (caster.Frozen || caster.Paralyzed)
            {
                reason = "caster_blocked";
                return false;
            }

            AIGMCompanionSpellState state = GetOrCreateState(caster);
            if (DateTime.UtcNow < state.NextCastUtc)
            {
                reason = "spell_cooldown_active";
                return false;
            }

            if (caster.Skills[SkillName.Magery].Value < AIGMCompanionSpellPolicy.MinimumSupportMagery)
            {
                reason = "magery_too_low";
                return false;
            }

            int mana = spell.ScaleMana(spell.GetMana());
            if (caster.Mana < mana)
            {
                reason = "no_mana";
                return false;
            }

            reason = "ok";
            return true;
        }

        private static Spell CreateSpell(Mobile caster, string spellName)
        {
            string normalized = Normalize(spellName);
            if (normalized == "cure")
                return new CureSpell(caster, null);
            if (normalized == "heal")
                return new HealSpell(caster, null);
            if (normalized == "greater heal" || normalized == "greater_heal" || normalized == "gheal")
                return new GreaterHealSpell(caster, null);
            if (normalized == "bless")
                return new BlessSpell(caster, null);
            return null;
        }

        private static Mobile ResolveNamedTarget(BaseHire caster, Mobile speaker, string speech)
        {
            if (caster == null)
                return null;

            if (speech.Contains(" me") || speech.EndsWith(" me", StringComparison.Ordinal))
                return speaker;
            if (speech.Contains(" self") || speech.Contains(" yourself"))
                return caster;

            Mobile owner = caster.GetOwner();
            if (speech.Contains("owner") && owner != null)
                return owner;

            foreach (Mobile mobile in World.Mobiles.Values)
            {
                if (mobile == null || mobile.Deleted || !mobile.Alive || mobile.Map != caster.Map || !caster.InRange(mobile, 12))
                    continue;

                string name = NormalizeCommand(mobile.Name);
                IAIGMCompanionActor actor = mobile as IAIGMCompanionActor;
                string id = actor != null ? NormalizeCommand(actor.CompanionId) : String.Empty;
                if ((!String.IsNullOrWhiteSpace(name) && speech.Contains(name)) || (!String.IsNullOrWhiteSpace(id) && speech.Contains(id)))
                    return mobile;
            }

            return null;
        }

        private static Mobile FindPoisonedAlly(Mobile caster)
        {
            return FindBestAlly(caster, delegate(Mobile m) { return m.Poisoned; }, true);
        }

        private static Mobile FindEmergencyHealTarget(Mobile caster)
        {
            return FindBestAlly(caster, delegate(Mobile m) { return HealthRatio(m) <= AIGMCompanionSpellPolicy.EmergencyHealThreshold && !m.Poisoned; }, false);
        }

        private static Mobile FindHealTarget(Mobile caster)
        {
            return FindBestAlly(caster, delegate(Mobile m) { return HealthRatio(m) <= AIGMCompanionSpellPolicy.HealThreshold && !m.Poisoned; }, false);
        }

        private static Mobile FindBlessTarget(Mobile caster)
        {
            bool combat = caster != null && (caster.Warmode || caster.Combatant != null);
            if (!combat)
                return null;
            return FindBestAlly(caster, delegate(Mobile m) { return !HasBlessStatBuff(m); }, false);
        }

        private static bool HasBlessStatBuff(Mobile mobile)
        {
            return mobile != null
                && (mobile.GetStatMod("[Magic] Str Buff") != null
                    || mobile.GetStatMod("[Magic] Dex Buff") != null
                    || mobile.GetStatMod("[Magic] Int Buff") != null);
        }

        private static Mobile FindBestAlly(Mobile caster, Func<Mobile, bool> predicate, bool poisonPriority)
        {
            if (caster == null || predicate == null)
                return null;

            List<Mobile> allies = BuildAllies(caster);
            Mobile best = null;
            double bestScore = Double.MinValue;
            for (int i = 0; i < allies.Count; i++)
            {
                Mobile ally = allies[i];
                if (!IsValidTarget(caster, ally) || !predicate(ally))
                    continue;

                double score = 100.0 - caster.GetDistanceToSqrt(ally);
                if (!poisonPriority)
                    score += (1.0 - HealthRatio(ally)) * 100.0;
                if (ally == caster)
                    score += poisonPriority ? 20.0 : 5.0;
                if (score > bestScore)
                {
                    best = ally;
                    bestScore = score;
                }
            }

            return best;
        }

        private static List<Mobile> BuildAllies(Mobile caster)
        {
            List<Mobile> allies = new List<Mobile>();
            if (caster == null)
                return allies;

            allies.Add(caster);
            BaseHire hire = caster as BaseHire;
            Mobile owner = hire != null ? hire.GetOwner() : null;
            if (owner != null && !owner.Deleted && owner.Alive && owner.Map == caster.Map && caster.InRange(owner, 10))
                allies.Add(owner);

            if (owner != null)
            {
                foreach (Mobile mobile in World.Mobiles.Values)
                {
                    BaseHire ally = mobile as BaseHire;
                    if (ally == null || ally == caster || ally.Deleted || !ally.Alive || ally.Map != caster.Map || !caster.InRange(ally, 10))
                        continue;
                    if (ally.GetOwner() == owner && ally is IAIGMCompanionActor)
                        allies.Add(ally);
                }
            }

            return allies;
        }

        private static bool IsValidTarget(Mobile caster, Mobile target)
        {
            return caster != null && target != null && !target.Deleted && target.Alive && target.Map == caster.Map && caster.CanSee(target);
        }

        private static double HealthRatio(Mobile mobile)
        {
            if (mobile == null || mobile.HitsMax <= 0)
                return 1.0;
            return (double)mobile.Hits / (double)mobile.HitsMax;
        }

        private static bool IsValidCompanion(Mobile companion)
        {
            return companion != null && !companion.Deleted && companion.Alive && companion is BaseHire && companion is IAIGMCompanionActor;
        }

        private static AIGMCompanionSpellState GetState(Mobile companion)
        {
            if (companion == null)
                return null;
            AIGMCompanionSpellState state;
            States.TryGetValue(companion.Serial, out state);
            return state;
        }

        private static AIGMCompanionSpellState GetOrCreateState(Mobile companion)
        {
            AIGMCompanionSpellState state = GetState(companion);
            if (state == null)
            {
                state = new AIGMCompanionSpellState();
                States[companion.Serial] = state;
            }
            return state;
        }

        private static void RecordFailure(Mobile caster, string spellKey, string reason)
        {
            AIGMCompanionSpellState state = GetOrCreateState(caster);
            state.LastResult = reason;
            DateTime now = DateTime.UtcNow;
            if (now >= state.NextFailureLogUtc)
            {
                state.NextFailureLogUtc = now + AIGMCompanionSpellPolicy.FailureLogCooldown;
                Log("AIGM_SPELL_SKIP caster={0} spell={1} reason={2}", Format(caster), spellKey, reason);
            }
        }

        private static string FindAbilityDirectiveId(Mobile caster, string spellKey)
        {
            IAIGMCompanionActor actor = caster as IAIGMCompanionActor;
            string character = actor != null ? Normalize(actor.CompanionId) : String.Empty;
            string ability = Normalize(spellKey);

            List<UMGCompanionAbilityBlock> blocks = WaylanderAbilityBlocks.BuildAbilityBlocks();
            for (int i = 0; i < blocks.Count; i++)
            {
                UMGCompanionAbilityBlock block = blocks[i];
                if (block == null || Normalize(block.Character) != character)
                    continue;

                string blockAbility = Normalize(block.Ability).Replace(" ", "_");
                if ((ability.Contains("cure") && blockAbility.Contains("cure"))
                    || (ability.Contains("heal") && blockAbility.Contains("heal"))
                    || (ability.Contains("bless") && blockAbility.Contains("bless"))
                    || block.Tags != null && ContainsTag(block.Tags, ability))
                    return block.Id;
            }

            return character == "dakeyras" ? "DAKEYRAS_MAGIC_RESTRAINT" : character == "danyal" ? "DANYAL_PRACTICAL_SUPPORT" : "none";
        }

        private static bool ContainsTag(IEnumerable<string> tags, string ability)
        {
            if (tags == null)
                return false;

            foreach (string tag in tags)
            {
                if (Normalize(tag).Contains(ability))
                    return true;
            }

            return false;
        }

        private static string Normalize(string text)
        {
            return String.IsNullOrWhiteSpace(text) ? String.Empty : text.Trim().ToLowerInvariant();
        }

        private static string NormalizeCommand(string text)
        {
            if (String.IsNullOrWhiteSpace(text))
                return String.Empty;
            string normalized = text.Trim().ToLowerInvariant();
            normalized = normalized.Replace(",", " ").Replace(".", " ").Replace("!", " ").Replace("?", " ").Replace(";", " ").Replace(":", " ");
            while (normalized.Contains("  "))
                normalized = normalized.Replace("  ", " ");
            return normalized.Trim();
        }

        private static string Format(Mobile mobile)
        {
            return mobile == null ? "null" : String.Format("{0}/{1}", mobile.Name, mobile.Serial.Value);
        }

        private static void Log(string format, params object[] args)
        {
            AIGMExecutionLog.Write(format, args);
        }
    }
}
