using System;
using System.Collections.Generic;
using System.Linq;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMProposalAugmenter
    {
        private static readonly string[] CreateVerbs = { "create", "spawn", "make", "add" };
        private static readonly string[] WorldPhrases = { "at my feet", "near me", "on ground", "on the ground", "here" };

        public static void Augment(AIGMResponse response, string question)
        {
            if (response == null)
                return;

            if (response.ProposedActions == null)
                response.ProposedActions = new List<AIGMActionProposal>();

            TryAddGeneralWorldAddProposal(response, question);
        }

        public static AIGMResponse EnsureCoreActionProposals(Mobile from, string question, AIGMTargetInfo target, AIGMResponse live)
        {
            if (live == null)
                return live;

            if (live.ProposedActions != null && live.ProposedActions.Count == 0)
            {
                AIGMResponse stub = AIGMStubResponder.Ask(from, question, target);
                if (stub != null && stub.ProposedActions != null && stub.ProposedActions.Count > 0)
                {
                    if (live.ProposedActions == null)
                        live.ProposedActions = new List<AIGMActionProposal>();

                    for (int i = 0; i < stub.ProposedActions.Count; i++)
                        live.ProposedActions.Add(stub.ProposedActions[i]);

                    if (live.NextAction == null)
                        live.NextAction = stub.NextAction;

                    if (live.SuggestedChecks == null)
                        live.SuggestedChecks = new List<string>();

                    live.SuggestedChecks.Add("Action proposals were supplemented locally because the live bridge returned no executable actions for this request.");

                    if (live.Warnings == null)
                        live.Warnings = new List<string>();

                    live.Warnings.Add("Proposal augmentation is active for known core counselor commands while the live bridge proposal layer is incomplete.");

                    if (String.IsNullOrWhiteSpace(live.Confidence))
                        live.Confidence = "low";
                }
            }

            Augment(live, question);
            return live;
        }

        private static void TryAddGeneralWorldAddProposal(AIGMResponse response, string question)
        {
            if (String.IsNullOrWhiteSpace(question))
                return;

            ParsedWorldAddRequest parsed = ParseWorldAddRequest(question);
            if (parsed == null || String.IsNullOrWhiteSpace(parsed.TypePhrase))
                return;

            AIGMConstructableResolution resolution = AIGMConstructableResolver.Resolve(parsed.TypePhrase, AIGMConstructableKind.Any);
            if (resolution == null || !resolution.Success)
                return;

            string actionKind = resolution.IsItem ? "gm_add_world_item" : (resolution.IsMobile ? "gm_add_world_mobile" : null);
            if (String.IsNullOrWhiteSpace(actionKind))
                return;

            bool alreadyExists = response.ProposedActions.Any(a =>
                a != null &&
                String.Equals(a.ActionKind, actionKind, StringComparison.OrdinalIgnoreCase) &&
                String.Equals(a.GetParameter("typeName"), resolution.CanonicalTypeName, StringComparison.OrdinalIgnoreCase));

            if (alreadyExists)
                return;

            int amount = parsed.Amount;
            if (amount < 1)
                amount = 1;

            AIGMActionProposal action = new AIGMActionProposal();
            action.ActionKind = actionKind;
            action.Title = BuildTitle(actionKind, amount, resolution.CanonicalTypeName);
            action.Category = "mutate";
            action.Description = BuildDescription(actionKind, amount, resolution.CanonicalTypeName);
            action.RequiresConfirmation = true;
            action.EnsureParameters();
            action.Parameters["commandSurface"] = "Add";
            action.Parameters["typeName"] = resolution.CanonicalTypeName;
            action.Parameters["requestedTypePhrase"] = parsed.TypePhrase;
            action.Parameters["amount"] = amount.ToString();
            action.Parameters["placement"] = "requester_feet";
            action.Parameters["range"] = "1";
            action.Parameters["resolverKind"] = resolution.IsItem ? "item" : "mobile";
            action.Parameters["resolverMatchSource"] = resolution.MatchSource ?? String.Empty;

            if (resolution.IsMobile)
                action.Parameters["executionSupport"] = "proposal_only_until_mobile_add_is_wired";

            response.ProposedActions.Add(action);
        }

        private static ParsedWorldAddRequest ParseWorldAddRequest(string question)
        {
            string normalized = AIGMConstructableResolver.Normalize(question);
            if (String.IsNullOrWhiteSpace(normalized))
                return null;

            bool wantsWorld = WorldPhrases.Any(p => normalized.Contains(AIGMConstructableResolver.Normalize(p)));
            if (!wantsWorld)
                return null;

            string verb = null;
            int verbIndex = Int32.MaxValue;
            for (int i = 0; i < CreateVerbs.Length; i++)
            {
                int index = normalized.IndexOf(CreateVerbs[i], StringComparison.OrdinalIgnoreCase);
                if (index >= 0 && index < verbIndex)
                {
                    verb = CreateVerbs[i];
                    verbIndex = index;
                }
            }

            if (String.IsNullOrWhiteSpace(verb))
                return null;

            int endIndex = normalized.Length;
            for (int i = 0; i < WorldPhrases.Length; i++)
            {
                int index = normalized.IndexOf(AIGMConstructableResolver.Normalize(WorldPhrases[i]), verbIndex, StringComparison.OrdinalIgnoreCase);
                if (index >= 0 && index < endIndex)
                    endIndex = index;
            }

            string core = normalized.Substring(verbIndex + verb.Length, endIndex - (verbIndex + verb.Length)).Trim();
            if (String.IsNullOrWhiteSpace(core))
                return null;

            int amount = 1;
            string[] parts = core.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            int start = 0;

            if (parts.Length > 0)
            {
                int parsedAmount;
                if (Int32.TryParse(parts[0], out parsedAmount))
                {
                    amount = parsedAmount;
                    start = 1;
                }
            }

            List<string> typeWords = new List<string>();
            for (int i = start; i < parts.Length; i++)
            {
                string word = parts[i];
                if (word == "a" || word == "an" || word == "the" || word == "some")
                    continue;

                typeWords.Add(word);
            }

            string typePhrase = String.Join(" ", typeWords.ToArray()).Trim();
            if (String.IsNullOrWhiteSpace(typePhrase))
                return null;

            ParsedWorldAddRequest result = new ParsedWorldAddRequest();
            result.Amount = amount;
            result.TypePhrase = typePhrase;
            return result;
        }

        private static string BuildTitle(string actionKind, int amount, string typeName)
        {
            if (String.Equals(actionKind, "gm_add_world_mobile", StringComparison.OrdinalIgnoreCase))
                return "Spawn " + amount + " " + typeName + " here";

            return "Create " + amount + " " + typeName + " at your feet";
        }

        private static string BuildDescription(string actionKind, int amount, string typeName)
        {
            if (String.Equals(actionKind, "gm_add_world_mobile", StringComparison.OrdinalIgnoreCase))
                return "Propose spawning " + amount + " " + typeName + " near the requester. Execution support for mobile add may still be pending.";

            return "Use the native Add-backed GM adapter to create " + amount + " " + typeName + " at the requester's feet.";
        }

        private sealed class ParsedWorldAddRequest
        {
            public int Amount;
            public string TypePhrase;
        }
    }
}
