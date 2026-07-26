using System;
using System.Collections.Generic;
using System.Text;
using Server.Mobiles;

namespace Server.Custom.AIGM.UMG
{
    public static class AIGMUMGDialogueContextService
    {
        public static string BuildBoundedContext(IAIGMCompanionActor actor, Mobile speaker, string rawSpeech, AIGMCompanionCognitionSnapshot cognition)
        {
            if (actor == null || actor.Shell == null)
                return String.Empty;

            string actorId = actor.CompanionId;
            AIGMUMGSleeve sleeve = AIGMUMGRepository.GetSleeve(actorId);
            if (sleeve == null)
                return String.Empty;

            StringBuilder sb = new StringBuilder();
            sb.Append("Phase64C read-only context: ");
            sb.AppendFormat("actorId={0}; displayName={1}; sleeve={2}; autonomy={3}; runtimeSerial={4}. ",
                Safe(sleeve.ActorId),
                Safe(sleeve.DisplayName),
                Safe(sleeve.SleeveId),
                sleeve.AutonomyMode,
                actor.Shell.Serial.Value);

            AppendSelectedBlocks(sb, sleeve, rawSpeech);

            if (ShouldIncludeCapability(rawSpeech))
            {
                AIGMCapabilitySnapshot snapshot = AIGMCapabilityRegistry.CreateSnapshot(actor.Shell);
                sb.Append("Capability truth: ");
                sb.Append(AIGMCapabilityRegistry.BuildCompactSummary(snapshot));
                if (cognition != null)
                {
                    sb.Append("; weapon=").Append(Safe(cognition.WeaponSummary));
                    sb.Append("; skills=").Append(Safe(cognition.StrongestSkillsSummary));
                    sb.Append("; resources=").Append(Safe(cognition.PackSummary));
                }
                sb.Append(". ");
            }

            if (ShouldAllowDraftDiscussion(rawSpeech))
                AppendDraftSummary(sb, sleeve);
            else
                sb.Append("Draft doctrine is excluded from active dialogue authority. ");

            string text = sb.ToString().Trim();
            return text.Length > 1800 ? text.Substring(0, 1800) : text;
        }

        private static void AppendSelectedBlocks(StringBuilder sb, AIGMUMGSleeve sleeve, string rawSpeech)
        {
            List<AIGMUMGBlock> blocks = AIGMUMGCompiler.FlattenOrderedBlocks(sleeve);
            int added = 0;
            for (int i = 0; i < blocks.Count && added < 6; i++)
            {
                AIGMUMGBlock block = blocks[i];
                if (block == null || block.BlockState != AIGMUMGBlockState.Active)
                    continue;

                bool always = block.Category.IndexOf("Identity", StringComparison.OrdinalIgnoreCase) >= 0
                    || block.Category.IndexOf("Governance", StringComparison.OrdinalIgnoreCase) >= 0
                    || block.Name.IndexOf("No False Capability Claims", StringComparison.OrdinalIgnoreCase) >= 0;
                bool relevant = IsQuestionRelevant(rawSpeech, block);
                if (!always && !relevant)
                    continue;

                if (added == 0)
                    sb.Append("Selected active blocks: ");
                else
                    sb.Append(" | ");

                sb.Append(block.MoltType).Append(":").Append(Safe(block.Name)).Append("=").Append(Safe(FirstNonEmpty(block.Summary, block.Content)));
                added++;
            }

            if (added > 0)
                sb.Append(". ");
        }

        private static void AppendDraftSummary(StringBuilder sb, AIGMUMGSleeve sleeve)
        {
            List<string> drafts = new List<string>();
            if (sleeve != null && sleeve.NeoStacks != null)
            {
                for (int s = 0; s < sleeve.NeoStacks.Count; s++)
                {
                    AIGMUMGNeoStack stack = sleeve.NeoStacks[s];
                    if (stack == null || stack.NeoBlocks == null)
                        continue;
                    for (int n = 0; n < stack.NeoBlocks.Count; n++)
                    {
                        AIGMUMGNeoBlock neoBlock = stack.NeoBlocks[n];
                        if (neoBlock == null || neoBlock.BlockStacks == null)
                            continue;
                        for (int b = 0; b < neoBlock.BlockStacks.Count; b++)
                        {
                            AIGMUMGBlockStack blockStack = neoBlock.BlockStacks[b];
                            if (blockStack == null || blockStack.MoltBlocks == null)
                                continue;
                            for (int m = 0; m < blockStack.MoltBlocks.Count; m++)
                            {
                                AIGMUMGBlock block = blockStack.MoltBlocks[m];
                                if (block != null && block.BlockState == AIGMUMGBlockState.Draft)
                                    drafts.Add(block.Name + " (Draft, non-active)");
                            }
                        }
                    }
                }
            }

            if (drafts.Count == 0)
                return;

            sb.Append("Draft proposals visible because explicitly requested: ");
            for (int i = 0; i < drafts.Count && i < 4; i++)
            {
                if (i > 0)
                    sb.Append("; ");
                sb.Append(drafts[i]);
            }
            sb.Append(". ");
        }

        private static bool ShouldIncludeCapability(string rawSpeech)
        {
            return ContainsAny(rawSpeech, "can you", "capability", "able", "heal", "cast", "shoot", "bow", "weapon", "skill", "mana", "bandage", "track", "detect");
        }

        private static bool ShouldAllowDraftDiscussion(string rawSpeech)
        {
            return ContainsAny(rawSpeech, "show proposal", "show proposals", "explain draft", "review doctrine", "draft", "proposal");
        }

        private static bool IsQuestionRelevant(string rawSpeech, AIGMUMGBlock block)
        {
            if (block == null)
                return false;

            string text = (rawSpeech ?? String.Empty).ToLowerInvariant();
            string haystack = ((block.Name ?? String.Empty) + " " + (block.Category ?? String.Empty) + " " + (block.Summary ?? String.Empty)).ToLowerInvariant();
            return ContainsAny(text, "governance", "hold", "stand down", "guard", "protect", "relationship", "identity", "voice")
                && ContainsAny(haystack, "governance", "hold", "guard", "protect", "relationship", "identity", "voice");
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

        private static string FirstNonEmpty(string first, string second)
        {
            return !String.IsNullOrWhiteSpace(first) ? first : second;
        }

        private static string Safe(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return String.Empty;

            value = value.Replace("\r", " ").Replace("\n", " ").Replace("\"", "'");
            return value.Length > 180 ? value.Substring(0, 180) : value;
        }
    }
}
