using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Server.Custom.AIGM;

namespace Server.Custom.AIGM.UMG
{
    public static class UMGPersonaBlockLibrary
    {
        private static readonly ReadOnlyCollection<UMGPersonaBlock> PersonaBlocks = WaylanderPersonaBlocks.BuildPersonaBlocks().AsReadOnly();
        private static readonly ReadOnlyCollection<UMGRelationshipBlock> RelationshipBlocks = WaylanderPersonaBlocks.BuildRelationshipBlocks().AsReadOnly();
        private static readonly ReadOnlyCollection<UMGPartyCognitionBlock> PartyBlocks = WaylanderPersonaBlocks.BuildPartyBlocks().AsReadOnly();
        private static readonly ReadOnlyCollection<UMGPartyCognitionBlock> CognitionHookBlocks = WaylanderPersonaBlocks.BuildCognitionHookBlocks().AsReadOnly();

        public static ReadOnlyCollection<UMGPersonaBlock> GetBlocksForCharacter(string character)
        {
            string normalized = NormalizeCharacter(character);
            List<UMGPersonaBlock> selected = new List<UMGPersonaBlock>();
            for (int i = 0; i < PersonaBlocks.Count; i++)
            {
                if (String.Equals(PersonaBlocks[i].Character, normalized, StringComparison.OrdinalIgnoreCase))
                    selected.Add(PersonaBlocks[i]);
            }

            return selected.AsReadOnly();
        }

        public static ReadOnlyCollection<UMGRelationshipBlock> GetRelationshipBlocks(string sourceCharacter, string targetCharacter)
        {
            string source = NormalizeCharacter(sourceCharacter);
            string target = NormalizeCharacter(targetCharacter);
            List<UMGRelationshipBlock> selected = new List<UMGRelationshipBlock>();
            for (int i = 0; i < RelationshipBlocks.Count; i++)
            {
                UMGRelationshipBlock block = RelationshipBlocks[i];
                if ((String.IsNullOrWhiteSpace(source) || String.Equals(block.SourceCharacter, source, StringComparison.OrdinalIgnoreCase))
                    && (String.IsNullOrWhiteSpace(target) || String.Equals(block.TargetCharacter, target, StringComparison.OrdinalIgnoreCase)))
                    selected.Add(block);
            }

            return selected.AsReadOnly();
        }

        public static ReadOnlyCollection<UMGPartyCognitionBlock> GetPartyBlocks()
        {
            return PartyBlocks;
        }

        public static ReadOnlyCollection<UMGPartyCognitionBlock> GetCognitionHookBlocks()
        {
            return CognitionHookBlocks;
        }

        public static ReadOnlyCollection<UMGPersonaBlock> GetBlocksForDialogueContext(string character, string mode, string targetCharacter)
        {
            return GetRelevantBlocksForDialogueContext(character, mode, targetCharacter, false, null, null, null);
        }

        public static ReadOnlyCollection<UMGPersonaBlock> GetRelevantBlocksForDialogueContext(string character, string mode, string targetCharacter, bool groupAddressed, string stateContext, string rawSpeech, AIGMCompanionCognitionSnapshot snapshot)
        {
            string normalized = NormalizeCharacter(character);
            string target = NormalizeCharacter(targetCharacter);
            if (String.IsNullOrWhiteSpace(target))
                target = InferMentionedCharacter(rawSpeech, normalized);
            string modeText = mode ?? String.Empty;
            string combined = BuildCombinedText(mode, stateContext, rawSpeech, snapshot);
            List<UMGPersonaBlock> selected = new List<UMGPersonaBlock>();

            AddCategory(selected, normalized, "Identity");
            AddCategory(selected, normalized, "Speech");
            AddCategory(selected, normalized, "Action");

            if (groupAddressed || ContainsAny(modeText, "group", "owner_relay", "companion") || ContainsAny(combined, "lead", "who should", "keep moving", "after the fight", "after battle"))
                AddCategory(selected, normalized, "PartyRole");

            if (!String.IsNullOrWhiteSpace(target) || ContainsAny(combined, "think of", "trust", "ask ", "agree", "dakeyras", "waylander", "danyal", "dardalion"))
                AddCategory(selected, normalized, "Relationship");

            if (ContainsAny(combined, "challenge", "wrong", "too cold", "violence", "not needed", "disagree", "lead", "trust", "should lead", "risk", "moral"))
                AddCategory(selected, normalized, "Conflict");

            if (ContainsAny(combined, "wound", "hurt", "grief", "trust", "too cold", "violence", "despair", "after battle", "dead", "corpse"))
                AddCategory(selected, normalized, "Wound");

            if (ContainsAny(combined, "hostile", "possible threats", "corpse", "loot", "burden", "pack", "supplies", "auto loot", "quiet", "blocked", "stuck", "poison", "low health"))
                AddCategory(selected, normalized, "InGameCognition");

            if (selected.Count > 7)
                selected.RemoveRange(7, selected.Count - 7);

            return selected.AsReadOnly();
        }

        public static string BuildCompactDialogueContext(string character, string mode, string targetCharacter, bool groupAddressed, string stateContext, string rawSpeech)
        {
            return BuildCompactDialogueContext(character, mode, targetCharacter, groupAddressed, stateContext, rawSpeech, null);
        }

        public static string BuildCompactDialogueContext(string character, string mode, string targetCharacter, bool groupAddressed, string stateContext, string rawSpeech, AIGMCompanionCognitionSnapshot snapshot)
        {
            string normalized = NormalizeCharacter(character);
            string target = NormalizeCharacter(targetCharacter);
            if (String.IsNullOrWhiteSpace(target))
                target = InferMentionedCharacter(rawSpeech, normalized);

            StringBuilder sb = new StringBuilder();
            AppendDialogueShape(sb, normalized, target, mode, rawSpeech, stateContext, snapshot);
            AppendCognitionHooks(sb, stateContext, rawSpeech, snapshot);
            if (groupAddressed || ContainsAny(mode, "group", "owner_relay") || ContainsAny(rawSpeech, "companions", "discuss", "who should lead"))
                AppendPartyContext(sb, rawSpeech);

            ReadOnlyCollection<UMGPersonaBlock> blocks = GetRelevantBlocksForDialogueContext(normalized, mode, target, groupAddressed, stateContext, rawSpeech, snapshot);
            if (blocks.Count > 0)
            {
                sb.Append("UMG persona context: ");
                for (int i = 0; i < blocks.Count; i++)
                {
                    if (i > 0)
                        sb.Append(" | ");
                    sb.Append(blocks[i].FormatCompact());
                }
                sb.AppendLine();
            }

            ReadOnlyCollection<UMGRelationshipBlock> relationships = GetRelationshipBlocks(normalized, target);
            if (relationships.Count > 0)
            {
                sb.Append("UMG relationship context: ");
                for (int i = 0; i < relationships.Count && i < 2; i++)
                {
                    if (i > 0)
                        sb.Append(" | ");
                    sb.Append(relationships[i].FormatCompact());
                }
                sb.AppendLine();
            }

            string text = sb.ToString().Trim();
            if (text.Length > 2400)
                text = text.Substring(0, 2400);

            return text;
        }

        public static string BuildSelectedContextDebug(string character, string mode, string targetCharacter, bool groupAddressed, string stateContext, string rawSpeech)
        {
            string normalized = NormalizeCharacter(character);
            string target = NormalizeCharacter(targetCharacter);
            if (String.IsNullOrWhiteSpace(target))
                target = InferMentionedCharacter(rawSpeech, normalized);

            StringBuilder sb = new StringBuilder();
            ReadOnlyCollection<UMGPersonaBlock> blocks = GetRelevantBlocksForDialogueContext(normalized, mode, target, groupAddressed, stateContext, rawSpeech, null);
            sb.Append("persona=");
            for (int i = 0; i < blocks.Count; i++)
            {
                if (i > 0)
                    sb.Append(",");
                sb.Append(blocks[i].Id).Append("/").Append(blocks[i].Category);
            }

            ReadOnlyCollection<UMGRelationshipBlock> relationships = GetRelationshipBlocks(normalized, target);
            sb.Append("; relationships=");
            for (int i = 0; i < relationships.Count && i < 2; i++)
            {
                if (i > 0)
                    sb.Append(",");
                sb.Append(relationships[i].Id);
            }

            sb.Append("; target=").Append(String.IsNullOrWhiteSpace(target) ? "none" : target);
            if (groupAddressed || ContainsAny(rawSpeech, "companions", "discuss", "who should lead"))
                sb.Append("; party=PARTY_LEADERSHIP_WAYLANDER_WITH_CHECKS,PARTY_DISAGREEMENT_DISCIPLINE,PARTY_COMPANION_CONVERSATION_ROTATION");
            return sb.ToString();
        }

        public static string BuildProofSummary()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("characters: dakeyras={0}, danyal={1}, dardalion={2}; relationships={3}; party={4}; cognitionHooks={5}",
                GetBlocksForCharacter("dakeyras").Count,
                GetBlocksForCharacter("danyal").Count,
                GetBlocksForCharacter("dardalion").Count,
                RelationshipBlocks.Count,
                PartyBlocks.Count,
                CognitionHookBlocks.Count);
            return sb.ToString();
        }

        private static void AppendPartyContext(StringBuilder sb, string rawSpeech)
        {
            sb.Append("UMG party context: ");
            int appended = 0;
            string speech = rawSpeech == null ? String.Empty : rawSpeech.ToLowerInvariant();
            for (int i = 0; i < PartyBlocks.Count && appended < 3; i++)
            {
                UMGPartyCognitionBlock block = PartyBlocks[i];
                if (block.Id == "PARTY_LEADERSHIP_WAYLANDER_WITH_CHECKS"
                    || block.Id == "PARTY_DISAGREEMENT_DISCIPLINE"
                    || block.Id == "PARTY_COMPANION_CONVERSATION_ROTATION"
                    || (speech.Contains("quiet") && block.Id == "PARTY_QUIET_MODE_RESPECT")
                    || (ContainsAny(speech, "danger", "fight", "combat", "hostile") && block.Id == "PARTY_DANGER_OVERRIDE"))
                {
                    if (appended > 0)
                        sb.Append(" | ");
                    sb.Append(block.Id).Append("/").Append(block.FormatCompact());
                    appended++;
                }
            }
            sb.AppendLine();
        }

        private static void AppendDialogueShape(StringBuilder sb, string character, string target, string mode, string rawSpeech, string stateContext, AIGMCompanionCognitionSnapshot snapshot)
        {
            string combined = BuildCombinedText(mode, stateContext, rawSpeech, snapshot);
            sb.Append("UMG dialogue shape: ");
            if (character == "dakeyras")
                sb.Append("Default to tactical leadership, terse field judgment, and controlled respect. Accept useful checks from Danyal or Dardalion without becoming submissive.");
            else if (character == "danyal")
                sb.Append("Speak practically and directly; protect morale and supplies; challenge Dakeyras when cold tactics lose the human cost.");
            else if (character == "dardalion")
                sb.Append("Speak calmly with moral and spiritual clarity; challenge needless violence without becoming passive or generically pacifist.");
            else
                sb.Append("Stay concise, in-character, and grounded in immediate game context.");

            if (!String.IsNullOrWhiteSpace(target))
                sb.Append(" Address the relationship with ").Append(target).Append(" specifically.");
            if (ContainsAny(combined, "hostile", "possible threats", "combat", "danger"))
                sb.Append(" Compress speech because danger is near.");
            if (ContainsAny(combined, "loot", "corpse", "burden", "pack", "supplies", "auto loot"))
                sb.Append(" Use loot/burden facts only as dialogue context; do not move items or promise item exchange.");
            sb.AppendLine();
        }

        private static void AppendCognitionHooks(StringBuilder sb, string stateContext, string rawSpeech, AIGMCompanionCognitionSnapshot snapshot)
        {
            string combined = BuildCombinedText(null, stateContext, rawSpeech, snapshot);
            List<string> selected = new List<string>();
            AddHookIf(selected, "COG_AUTO_LOOT_ENABLED", ContainsAny(combined, "autoloot=on", "auto loot enabled", "auto loot"));
            AddHookIf(selected, "COG_CORPSE_LOOTED", ContainsAny(combined, "corpse looted", "looted corpse", "what did we take", "from the corpse"));
            AddHookIf(selected, "COG_BURDEN_HIGH", ContainsAny(combined, "burden=soft", "burden=hard", "too much", "carrying too much", "pack burden"));
            AddHookIf(selected, "COG_PACK_FULL", ContainsAny(combined, "pack full", "overweight"));
            AddHookIf(selected, "COG_JUNK_DROPPED", ContainsAny(combined, "junk dropped", "dropped junk", "dead weight"));
            AddHookIf(selected, "COG_BANKING_NEEDED", ContainsAny(combined, "banking", "deposit", "bank"));
            AddHookIf(selected, "COG_POTION_SUPPORT_ENABLED", ContainsAny(combined, "potions=on", "potion support enabled", "use potions"));
            AddHookIf(selected, "COG_POISON_CURED", ContainsAny(combined, "cure_potion_used", "poison_cured", "used_greatercurepotion", "used_curepotion"));
            AddHookIf(selected, "COG_HEAL_POTION_USED", ContainsAny(combined, "heal_potion_used", "used_greaterhealpotion", "used_healpotion"));
            AddHookIf(selected, "COG_REFRESH_POTION_USED", ContainsAny(combined, "refresh_potion_used", "used_totalrefreshpotion", "used_refreshpotion"));
            AddHookIf(selected, "COG_COMBAT_BUFF_USED", ContainsAny(combined, "combat_strength_used", "combat_agility_used", "combat_strength_agility_used", "used_greaterstrengthpotion", "used_greateragilitypotion", "battle draught"));
            AddHookIf(selected, "COG_NO_POTION_AVAILABLE", ContainsAny(combined, "no_cure_potion_available", "no_heal_potion_available", "no_refresh_potion_available", "no useful potion"));
            AddHookIf(selected, "COG_POTION_COOLDOWN_ACTIVE", ContainsAny(combined, "potion_cooldown_active"));
            AddHookIf(selected, "COG_SPELL_SUPPORT_ENABLED", ContainsAny(combined, "spells=on", "spell support enabled", "support magic", "use spells"));
            AddHookIf(selected, "COG_SUPPORT_SPELL_CAST", ContainsAny(combined, "cure_cast_complete", "heal_cast_complete", "greater_heal_cast_complete", "bless_cast_complete", "support_spell"));
            AddHookIf(selected, "COG_SPELL_NO_MANA", ContainsAny(combined, "no_mana"));
            AddHookIf(selected, "COG_SPELL_SKILL_LOW", ContainsAny(combined, "magery_too_low"));
            AddHookIf(selected, "COG_SPELL_COOLDOWN_ACTIVE", ContainsAny(combined, "spell_cooldown_active", "already_casting"));
            AddHookIf(selected, "COG_NEARBY_HOSTILE", ContainsAny(combined, "possible threats=", "hostile", "enemy", "ambush"));
            AddHookIf(selected, "COG_LOW_HEALTH_PARTY", ContainsAny(combined, "damaged", "low health", "wound"));
            AddHookIf(selected, "COG_POISONED_PARTY_MEMBER", ContainsAny(combined, "poison", "venom"));
            AddHookIf(selected, "COG_TRAVEL_BLOCKED", ContainsAny(combined, "blocked", "path_blocked", "cannot reach"));
            AddHookIf(selected, "COG_OWNER_COMMAND_RECEIVED", ContainsAny(combined, "command", "order"));
            AddHookIf(selected, "COG_GROUP_DISCUSSION_ACTIVE", ContainsAny(combined, "groupaddressed=true", "companions", "discuss"));
            AddHookIf(selected, "COG_QUIET_MODE_ACTIVE", ContainsAny(combined, "quietmode=true", "quiet"));
            AddHookIf(selected, "COG_UNKNOWN_LOCATION", ContainsAny(combined, "unknown region", "unknown location"));
            AddHookIf(selected, "COG_BATTLE_AFTERCARE", ContainsAny(combined, "corpse", "aftercare"));
            AddHookIf(selected, "COG_LOST_OR_STUCK", ContainsAny(combined, "lost", "stuck"));

            if (selected.Count == 0)
                return;

            sb.Append("UMG cognition hooks: ");
            for (int i = 0; i < selected.Count && i < 4; i++)
            {
                UMGPartyCognitionBlock block = FindCognitionHook(selected[i]);
                if (block == null)
                    continue;
                if (i > 0)
                    sb.Append(" | ");
                sb.Append(block.Id).Append("/").Append(block.FormatCompact());
            }
            sb.AppendLine();
        }

        private static void AddHookIf(List<string> selected, string id, bool condition)
        {
            if (condition && selected.Count < 6)
                selected.Add(id);
        }

        private static UMGPartyCognitionBlock FindCognitionHook(string id)
        {
            for (int i = 0; i < CognitionHookBlocks.Count; i++)
            {
                if (String.Equals(CognitionHookBlocks[i].Id, id, StringComparison.OrdinalIgnoreCase))
                    return CognitionHookBlocks[i];
            }

            return null;
        }

        private static void AddCategory(List<UMGPersonaBlock> selected, string character, string category)
        {
            for (int i = 0; i < PersonaBlocks.Count; i++)
            {
                UMGPersonaBlock block = PersonaBlocks[i];
                if (!String.Equals(block.Character, character, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!String.Equals(block.Category, category, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!ContainsBlock(selected, block.Id))
                    selected.Add(block);
                return;
            }
        }

        private static bool ContainsBlock(List<UMGPersonaBlock> selected, string id)
        {
            for (int i = 0; i < selected.Count; i++)
            {
                if (String.Equals(selected[i].Id, id, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static string InferMentionedCharacter(string rawSpeech, string self)
        {
            string text = rawSpeech == null ? String.Empty : rawSpeech.ToLowerInvariant();
            if (self != "dakeyras" && (text.Contains("dakeyras") || text.Contains("waylander") || text.Contains(" dak ")))
                return "dakeyras";
            if (self != "danyal" && text.Contains("danyal"))
                return "danyal";
            if (self != "dardalion" && text.Contains("dardalion"))
                return "dardalion";
            return String.Empty;
        }

        private static string BuildCombinedText(string mode, string stateContext, string rawSpeech, AIGMCompanionCognitionSnapshot snapshot)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(mode ?? String.Empty).Append(' ');
            sb.Append(stateContext ?? String.Empty).Append(' ');
            sb.Append(rawSpeech ?? String.Empty).Append(' ');
            if (snapshot != null)
            {
                sb.Append(snapshot.PackSummary ?? String.Empty).Append(' ');
                sb.Append(snapshot.SituationSummary ?? String.Empty).Append(' ');
                sb.Append(snapshot.CurrentHealingState ?? String.Empty).Append(' ');
                sb.Append(snapshot.CurrentThreatState ?? String.Empty).Append(' ');
                sb.Append(snapshot.CompanionMode ?? String.Empty).Append(' ');
                if (snapshot.QuietMode)
                    sb.Append("quietMode=true ");
            }

            return sb.ToString().ToLowerInvariant();
        }

        private static string NormalizeCharacter(string character)
        {
            string value = character == null ? String.Empty : character.Trim().ToLowerInvariant();
            if (value == "waylander" || value == "dak" || value == "grey man")
                return "dakeyras";
            return value;
        }

        private static bool ContainsAny(string haystack, params string[] needles)
        {
            if (String.IsNullOrWhiteSpace(haystack) || needles == null)
                return false;

            string text = haystack.ToLowerInvariant();
            for (int i = 0; i < needles.Length; i++)
            {
                if (!String.IsNullOrWhiteSpace(needles[i]) && text.Contains(needles[i].ToLowerInvariant()))
                    return true;
            }

            return false;
        }
    }
}
