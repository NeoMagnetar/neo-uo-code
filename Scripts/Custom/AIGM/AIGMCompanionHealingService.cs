using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionHealingService
    {
        public static string BuildHealingStatus(BaseHire healer)
        {
            if (!IsValidHealer(healer))
                return "I cannot assess my healing readiness right now.";

            double healing = AIGMCompanionSkillReadiness.GetSkillValue(healer, SkillName.Healing);
            double anatomy = AIGMCompanionSkillReadiness.GetSkillValue(healer, SkillName.Anatomy);
            int bandages = CountBandages(healer);
            return String.Format("My Healing is {0} at {1:0.0}, Anatomy is {2} at {3:0.0}, and I carry {4} bandages.",
                AIGMCompanionSkillReadiness.BuildTier(healing), healing,
                AIGMCompanionSkillReadiness.BuildTier(anatomy), anatomy,
                bandages);
        }

        public static string BuildSupportStatus(BaseHire healer)
        {
            if (!IsValidHealer(healer))
                return "I cannot assess my support readiness right now.";

            return BuildHealingStatus(healer) + " Spell healing remains deferred in this pass.";
        }

        public static bool TryHandleExplicitHealingCommand(BaseHire healer, Mobile speaker, string rawSpeech, out string response)
        {
            response = null;
            if (!IsValidHealer(healer) || speaker == null || String.IsNullOrWhiteSpace(rawSpeech))
                return false;

            HealingCommand command = ParseHealingCommand(rawSpeech);
            if (!command.IsHealingCommand)
                return false;

            if (command.IsStatusOnly)
            {
                response = command.Kind == HealingCommandKind.SupportStatus ? BuildSupportStatus(healer) : BuildHealingStatus(healer);
                return true;
            }

            if (command.Kind == HealingCommandKind.CastHeal || command.Kind == HealingCommandKind.CastCure || command.Kind == HealingCommandKind.Cure)
            {
                response = "I can assess the wound, but that healing method is not safely wired yet.";
                return true;
            }

            Mobile target;
            BaseHire actingHealer = healer;
            if (command.IsGroupHealing)
            {
                target = ResolveTargetForGroupCommand(healer, speaker, command);
                if (target == null)
                {
                    response = "That target is not allowed for companion healing.";
                    return true;
                }

                actingHealer = SelectBestHealer(healer, target);
                if (actingHealer == null)
                {
                    response = "No companion is ready to bandage that target right now.";
                    return true;
                }
            }
            else
            {
                target = ResolveTargetForDirectCommand(healer, speaker, command);
                if (target == null)
                {
                    response = "That target is not allowed for companion healing.";
                    return true;
                }
            }

            return TryBeginBandage(actingHealer, target, out response);
        }

        private static bool TryBeginBandage(BaseHire healer, Mobile target, out string response)
        {
            response = null;
            if (!IsValidHealer(healer))
            {
                response = "I cannot tend wounds right now.";
                return true;
            }

            if (!IsAllowedTarget(healer, target))
            {
                response = "That target is not allowed for companion healing.";
                return true;
            }

            if (!target.Alive || target.Deleted)
            {
                response = "That target cannot be healed this way.";
                return true;
            }

            if (BandageContext.GetContext(healer) != null)
            {
                response = healer == target ? "I am already bandaging my wounds." : "I am already bandaging someone.";
                return true;
            }

            IAIGMCompanionActor actor = healerActor(healer);
            if (actor != null && DateTime.UtcNow < actor.NextSupportActionUtc)
            {
                response = "I am already committed for the moment.";
                return true;
            }

            if (!healer.InRange(target, Bandage.Range))
            {
                response = healer == target ? "I need a moment to tend to myself." : "You are too far away.";
                return true;
            }

            double healing = AIGMCompanionSkillReadiness.GetSkillValue(healer, SkillName.Healing);
            double anatomy = AIGMCompanionSkillReadiness.GetSkillValue(healer, SkillName.Anatomy);
            if (healing < 30.0 || anatomy < 30.0)
            {
                response = "My Healing and Anatomy are not sufficient for that.";
                return true;
            }

            if (!target.Poisoned && target.Hits >= target.HitsMax)
            {
                response = healer == target ? "I am not wounded." : DescribeTarget(target) + " is not wounded.";
                return true;
            }

            Bandage bandage = FindBandage(healer);
            if (bandage == null || bandage.Amount <= 0)
            {
                response = "I cannot bandage without supplies.";
                return true;
            }

            BandageContext context = BandageContext.BeginHeal(healer, target, bandage is EnhancedBandage);
            if (context == null)
            {
                response = target.Poisoned ? "I cannot begin treating that poison right now." : "I cannot begin bandaging right now.";
                return true;
            }

            bandage.Consume();
            if (actor != null)
                actor.NextSupportActionUtc = DateTime.UtcNow + BandageContext.GetDelay(healer, target);

            if (healer == target)
                response = healer.Name + " begins bandaging their own wounds.";
            else if (target == healer.GetOwner())
                response = healer.Name + " begins bandaging you.";
            else
                response = healer.Name + " begins bandaging " + DescribeTarget(target) + ".";

            return true;
        }

        private static BaseHire SelectBestHealer(BaseHire requesterCompanion, Mobile target)
        {
            Mobile owner = requesterCompanion.GetOwner();
            if (owner == null)
                return null;

            List<BaseHire> candidates = new List<BaseHire>();
            foreach (Mobile mobile in World.Mobiles.Values)
            {
                BaseHire ally = mobile as BaseHire;
                if (!IsValidHealer(ally))
                    continue;
                if (ally.GetOwner() != owner)
                    continue;
                if (!IsAllowedTarget(ally, target))
                    continue;
                candidates.Add(ally);
            }

            candidates.Sort((a, b) => ScoreHealer(b, target).CompareTo(ScoreHealer(a, target)));
            return candidates.Count > 0 && ScoreHealer(candidates[0], target) > 0 ? candidates[0] : null;
        }

        private static int ScoreHealer(BaseHire healer, Mobile target)
        {
            if (!IsValidHealer(healer) || !IsAllowedTarget(healer, target))
                return -1;
            if (!healer.InRange(target, Bandage.Range))
                return 0;
            if (BandageContext.GetContext(healer) != null)
                return 0;
            if (DateTime.UtcNow < healerActor(healer).NextSupportActionUtc)
                return 0;
            if (CountBandages(healer) <= 0)
                return 0;

            int score = (int)Math.Round(AIGMCompanionSkillReadiness.GetSkillValue(healer, SkillName.Healing)
                + AIGMCompanionSkillReadiness.GetSkillValue(healer, SkillName.Anatomy));

            IAIGMCompanionActor actor = healerActor(healer);
            if (actor != null && String.Equals(actor.CompanionId, "danyal", StringComparison.OrdinalIgnoreCase))
                score += 1;

            return score;
        }

        private static Mobile ResolveTargetForGroupCommand(BaseHire healer, Mobile speaker, HealingCommand command)
        {
            if (command.TargetKind == HealingTargetKind.Owner)
                return speaker;
            return ResolveNamedCompanionTarget(healer, command.TargetName);
        }

        private static Mobile ResolveTargetForDirectCommand(BaseHire healer, Mobile speaker, HealingCommand command)
        {
            switch (command.TargetKind)
            {
                case HealingTargetKind.Owner:
                    return speaker;
                case HealingTargetKind.Self:
                    return healer;
                case HealingTargetKind.NamedCompanion:
                    return ResolveNamedCompanionTarget(healer, command.TargetName);
                default:
                    return null;
            }
        }

        private static Mobile ResolveNamedCompanionTarget(BaseHire healer, string targetName)
        {
            if (!IsValidHealer(healer) || String.IsNullOrWhiteSpace(targetName))
                return null;

            Mobile owner = healer.GetOwner();
            if (owner == null)
                return null;

            foreach (Mobile mobile in World.Mobiles.Values)
            {
                BaseHire ally = mobile as BaseHire;
                if (!IsValidHealer(ally))
                    continue;
                if (ally.GetOwner() != owner)
                    continue;
                if (String.Equals(ally.Name, targetName, StringComparison.OrdinalIgnoreCase))
                    return ally;

                IAIGMCompanionActor actor = healerActor(ally);
                if (actor != null)
                {
                    if (String.Equals(actor.CompanionDisplayName, targetName, StringComparison.OrdinalIgnoreCase)
                        || String.Equals(actor.CompanionId, targetName, StringComparison.OrdinalIgnoreCase))
                        return ally;
                }
            }

            return null;
        }

        private static bool IsAllowedTarget(BaseHire healer, Mobile target)
        {
            if (!IsValidHealer(healer) || target == null || target.Deleted)
                return false;

            Mobile owner = healer.GetOwner();
            if (target == owner || target == healer)
                return true;

            BaseHire ally = target as BaseHire;
            return ally != null && ally.GetOwner() == owner && !ally.Deleted;
        }

        private static bool IsValidHealer(BaseHire healer)
        {
            return healer != null && !healer.Deleted && healer.Alive && healer.Map != null;
        }

        private static IAIGMCompanionActor healerActor(BaseHire healer)
        {
            return healer as IAIGMCompanionActor;
        }

        private static Bandage FindBandage(BaseHire healer)
        {
            if (healer == null || healer.Backpack == null)
                return null;

            return healer.Backpack.FindItemByType(typeof(Bandage), true) as Bandage;
        }

        private static int CountBandages(BaseHire healer)
        {
            Bandage bandage = FindBandage(healer);
            return bandage != null ? bandage.Amount : 0;
        }

        private static string DescribeTarget(Mobile target)
        {
            return target != null ? (target.Name ?? target.GetType().Name) : "that target";
        }

        private enum HealingCommandKind
        {
            None,
            HealingStatus,
            SupportStatus,
            Heal,
            Bandage,
            Cure,
            CastHeal,
            CastCure
        }

        private enum HealingTargetKind
        {
            None,
            Owner,
            Self,
            NamedCompanion
        }

        private sealed class HealingCommand
        {
            public bool IsHealingCommand;
            public bool IsStatusOnly;
            public bool IsGroupHealing;
            public HealingCommandKind Kind;
            public HealingTargetKind TargetKind;
            public string TargetName;
        }

        private static HealingCommand ParseHealingCommand(string rawSpeech)
        {
            HealingCommand cmd = new HealingCommand();
            if (String.IsNullOrWhiteSpace(rawSpeech))
                return cmd;

            string s = rawSpeech.Trim().ToLowerInvariant();
            while (s.Contains("  "))
                s = s.Replace("  ", " ");

            if (s.Contains("healing status"))
            {
                cmd.IsHealingCommand = true;
                cmd.IsStatusOnly = true;
                cmd.Kind = HealingCommandKind.HealingStatus;
                return cmd;
            }

            if (s.Contains("support status"))
            {
                cmd.IsHealingCommand = true;
                cmd.IsStatusOnly = true;
                cmd.Kind = HealingCommandKind.SupportStatus;
                return cmd;
            }

            if (s.Contains("all heal me") || s.Contains("companions heal me"))
            {
                cmd.IsHealingCommand = true;
                cmd.IsGroupHealing = true;
                cmd.Kind = HealingCommandKind.Heal;
                cmd.TargetKind = HealingTargetKind.Owner;
                return cmd;
            }

            if (s.Contains("cast heal"))
            {
                cmd.IsHealingCommand = true;
                cmd.Kind = HealingCommandKind.CastHeal;
                return cmd;
            }

            if (s.Contains("cast cure"))
            {
                cmd.IsHealingCommand = true;
                cmd.Kind = HealingCommandKind.CastCure;
                return cmd;
            }

            if (s.Contains("cure "))
            {
                cmd.IsHealingCommand = true;
                cmd.Kind = HealingCommandKind.Cure;
                if (s.Contains("yourself") || s.Contains("self"))
                    cmd.TargetKind = HealingTargetKind.Self;
                else if (s.Contains(" me"))
                    cmd.TargetKind = HealingTargetKind.Owner;
                else
                {
                    cmd.TargetKind = HealingTargetKind.NamedCompanion;
                    cmd.TargetName = ExtractNamedTarget(rawSpeech, "cure");
                }
                return cmd;
            }

            if (s.Contains("bandage"))
            {
                cmd.IsHealingCommand = true;
                cmd.Kind = HealingCommandKind.Bandage;
                if (s.Contains("yourself") || s.Contains("self"))
                    cmd.TargetKind = HealingTargetKind.Self;
                else if (s.Contains(" me") || EndsWithCommandWord(s, "bandage"))
                    cmd.TargetKind = HealingTargetKind.Owner;
                else
                {
                    cmd.TargetKind = HealingTargetKind.NamedCompanion;
                    cmd.TargetName = ExtractNamedTarget(rawSpeech, "bandage");
                    if (String.IsNullOrWhiteSpace(cmd.TargetName))
                        cmd.TargetKind = HealingTargetKind.Owner;
                }
                return cmd;
            }

            if (s.Contains("heal"))
            {
                cmd.IsHealingCommand = true;
                cmd.Kind = HealingCommandKind.Heal;
                if (s.Contains("yourself") || s.Contains("self"))
                    cmd.TargetKind = HealingTargetKind.Self;
                else if (s.Contains(" me") || EndsWithCommandWord(s, "heal"))
                    cmd.TargetKind = HealingTargetKind.Owner;
                else
                {
                    cmd.TargetKind = HealingTargetKind.NamedCompanion;
                    cmd.TargetName = ExtractNamedTarget(rawSpeech, "heal");
                    if (String.IsNullOrWhiteSpace(cmd.TargetName))
                        cmd.TargetKind = HealingTargetKind.Owner;
                }
            }

            return cmd;
        }

        private static string ExtractNamedTarget(string rawSpeech, string verb)
        {
            if (String.IsNullOrWhiteSpace(rawSpeech) || String.IsNullOrWhiteSpace(verb))
                return null;

            string lowered = rawSpeech.ToLowerInvariant();
            int idx = lowered.IndexOf(verb, StringComparison.Ordinal);
            if (idx < 0)
                return null;

            string rest = rawSpeech.Substring(idx + verb.Length).Trim();
            if (String.IsNullOrWhiteSpace(rest))
                return null;

            string[] prefixes = { "dak ", "dakeyras ", "waylander ", "danyal ", "dardalion ", "all ", "companions " };
            string loweredRest = rest.ToLowerInvariant();
            for (int i = 0; i < prefixes.Length; i++)
            {
                if (loweredRest.StartsWith(prefixes[i], StringComparison.Ordinal))
                {
                    rest = rest.Substring(prefixes[i].Length).Trim();
                    loweredRest = rest.ToLowerInvariant();
                }
            }

            if (String.Equals(loweredRest, "me", StringComparison.Ordinal)
                || String.Equals(loweredRest, "yourself", StringComparison.Ordinal)
                || String.Equals(loweredRest, "self", StringComparison.Ordinal))
                return null;

            return rest;
        }

        private static bool EndsWithCommandWord(string speech, string word)
        {
            if (String.IsNullOrWhiteSpace(speech) || String.IsNullOrWhiteSpace(word))
                return false;

            return speech.Equals(word, StringComparison.Ordinal)
                || speech.EndsWith(" " + word, StringComparison.Ordinal);
        }
    }
}
