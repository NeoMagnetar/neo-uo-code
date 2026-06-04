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
        private static readonly string[] CounselorPackPhrases = { "in your bag", "in your backpack", "in counselor pack", "in the counselor pack", "in your pack" };
        private static readonly string[] RequesterPackPhrases = { "in my bag", "in my backpack", "in my pack" };

        public static void Augment(AIGMResponse response, string question)
        {
            if (response == null)
                return;

            if (response.ProposedActions == null)
                response.ProposedActions = new List<AIGMActionProposal>();

            TryAddMovementProposal(response, question);
            TryAddPropsReadProposal(response, question);
            TryAddGeneralContainerAddProposal(response, question);
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

            if (live.ProposedActions != null && live.ProposedActions.Count > 0)
            {
                bool hasPropsRead = live.ProposedActions.Any(a => a != null && String.Equals(a.ActionKind, "gm_props_read", StringComparison.OrdinalIgnoreCase));
                if (hasPropsRead && live.NextAction == null)
                {
                    live.NextAction = live.ProposedActions.FirstOrDefault(a => a != null && String.Equals(a.ActionKind, "gm_props_read", StringComparison.OrdinalIgnoreCase));
                }
            }

            return live;
        }

        private static void TryAddMovementProposal(AIGMResponse response, string question)
        {
            if (String.IsNullOrWhiteSpace(question))
                return;

            string lowered = question.Trim().ToLowerInvariant();
            AIGMActionProposal action = null;

            if (lowered.Contains("follow me") || lowered == "follow")
            {
                action = BuildMovementProposal("gm_follow_requester", "Follow me", "Have the counselor follow you.");
            }
            else if (lowered.Contains("stop following") || lowered.Contains("stop moving") || lowered == "stop")
            {
                action = BuildMovementProposal("gm_stop_follow", "Stop following", "Stop the counselor's active movement/follow behavior.");
            }
            else if (lowered.Contains("resume following") || lowered.Contains("resume movement") || lowered == "resume")
            {
                action = BuildMovementProposal("gm_resume_follow", "Resume following", "Resume the counselor's paused movement/follow behavior.");
            }
            else if (lowered.Contains("come here") || lowered.Contains("go to me"))
            {
                action = BuildMovementProposal("gm_go_to_requester", "Come here", "Have the counselor move to your current location.");
            }

            if (action == null)
                return;

            bool alreadyExists = response.ProposedActions.Any(a => a != null && String.Equals(a.ActionKind, action.ActionKind, StringComparison.OrdinalIgnoreCase));
            if (alreadyExists)
                return;

            response.ProposedActions.Insert(0, action);
            response.NextAction = action;
        }

        private static void TryAddGeneralContainerAddProposal(AIGMResponse response, string question)
        {
            if (String.IsNullOrWhiteSpace(question))
                return;

            ParsedContainerAddRequest parsed = ParseContainerAddRequest(question);
            if (parsed == null || String.IsNullOrWhiteSpace(parsed.TypePhrase) || String.IsNullOrWhiteSpace(parsed.TargetContainerKind))
                return;

            AIGMConstructableResolution resolution = AIGMConstructableResolver.Resolve(parsed.TypePhrase, AIGMConstructableKind.Item);
            if (resolution == null || !resolution.Success || !resolution.IsItem)
                return;

            bool alreadyExists = response.ProposedActions.Any(a =>
                a != null &&
                String.Equals(a.ActionKind, "gm_add_container_item", StringComparison.OrdinalIgnoreCase) &&
                String.Equals(a.GetParameter("typeName"), resolution.CanonicalTypeName, StringComparison.OrdinalIgnoreCase) &&
                String.Equals(a.GetParameter("targetContainerKind"), parsed.TargetContainerKind, StringComparison.OrdinalIgnoreCase));

            if (alreadyExists)
                return;

            int amount = parsed.Amount;
            if (amount < 1)
                amount = 1;

            AIGMActionProposal action = new AIGMActionProposal();
            action.ActionKind = "gm_add_container_item";
            action.Title = BuildContainerTitle(amount, resolution.CanonicalTypeName, parsed.TargetContainerKind);
            action.Category = "mutate";
            action.Description = BuildContainerDescription(amount, resolution.CanonicalTypeName, parsed.TargetContainerKind);
            action.RequiresConfirmation = true;
            action.EnsureParameters();
            action.Parameters["commandSurface"] = "Add";
            action.Parameters["typeName"] = resolution.CanonicalTypeName;
            action.Parameters["requestedTypePhrase"] = parsed.TypePhrase;
            action.Parameters["amount"] = amount.ToString();
            action.Parameters["targetContainerKind"] = parsed.TargetContainerKind;
            action.Parameters["resolverKind"] = "item";
            action.Parameters["resolverMatchSource"] = resolution.MatchSource ?? String.Empty;

            response.ProposedActions.Add(action);
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

        private static void TryAddPropsReadProposal(AIGMResponse response, string question)
        {
            if (String.IsNullOrWhiteSpace(question))
                return;

            ParsedPropsReadRequest parsed = ParsePropsReadRequest(question);
            if (parsed == null || String.IsNullOrWhiteSpace(parsed.TargetMode))
                return;

            bool alreadyExists = response.ProposedActions.Any(a =>
                a != null &&
                String.Equals(a.ActionKind, "gm_props_read", StringComparison.OrdinalIgnoreCase) &&
                String.Equals(a.GetParameter("targetMode"), parsed.TargetMode, StringComparison.OrdinalIgnoreCase) &&
                String.Equals(a.GetParameter("targetName"), parsed.TargetName ?? String.Empty, StringComparison.OrdinalIgnoreCase) &&
                String.Equals(a.GetParameter("targetContainerKind"), parsed.TargetContainerKind ?? String.Empty, StringComparison.OrdinalIgnoreCase));

            if (alreadyExists)
                return;

            AIGMActionProposal action = new AIGMActionProposal();
            action.ActionKind = "gm_props_read";
            action.Title = BuildPropsReadTitle(parsed);
            action.Category = "read";
            action.Description = BuildPropsReadDescription(parsed);
            action.RequiresConfirmation = false;
            action.EnsureParameters();
            action.Parameters["commandSurface"] = "Props";
            action.Parameters["targetMode"] = parsed.TargetMode;
            if (!String.IsNullOrWhiteSpace(parsed.TargetName))
                action.Parameters["targetName"] = parsed.TargetName;
            if (!String.IsNullOrWhiteSpace(parsed.TargetContainerKind))
                action.Parameters["targetContainerKind"] = parsed.TargetContainerKind;

            bool exactTarget = !String.IsNullOrWhiteSpace(parsed.TargetName) || !String.IsNullOrWhiteSpace(parsed.TargetContainerKind);
            if (exactTarget)
            {
                for (int i = response.ProposedActions.Count - 1; i >= 0; i--)
                {
                    AIGMActionProposal existing = response.ProposedActions[i];
                    if (existing == null || !String.Equals(existing.ActionKind, "gm_props_read", StringComparison.OrdinalIgnoreCase))
                        continue;

                    string existingName = existing.GetParameter("targetName", String.Empty);
                    string existingContainer = existing.GetParameter("targetContainerKind", String.Empty);
                    bool existingGeneric = String.IsNullOrWhiteSpace(existingName) && String.IsNullOrWhiteSpace(existingContainer);
                    if (existingGeneric)
                        response.ProposedActions.RemoveAt(i);
                }

                response.ProposedActions.Insert(0, action);
                response.NextAction = action;
            }
            else
            {
                response.ProposedActions.Add(action);
                if (response.NextAction == null)
                    response.NextAction = action;
            }
        }

        private static ParsedContainerAddRequest ParseContainerAddRequest(string question)
        {
            string normalized = AIGMConstructableResolver.Normalize(question);
            if (String.IsNullOrWhiteSpace(normalized))
                return null;

            string targetContainerKind = null;
            string matchedPhrase = null;

            for (int i = 0; i < CounselorPackPhrases.Length; i++)
            {
                string phrase = CounselorPackPhrases[i];
                string normalizedPhrase = AIGMConstructableResolver.Normalize(phrase);
                if (normalized.Contains(normalizedPhrase))
                {
                    targetContainerKind = "counselor_pack";
                    matchedPhrase = normalizedPhrase;
                    break;
                }
            }

            if (targetContainerKind == null)
            {
                for (int i = 0; i < RequesterPackPhrases.Length; i++)
                {
                    string phrase = RequesterPackPhrases[i];
                    string normalizedPhrase = AIGMConstructableResolver.Normalize(phrase);
                    if (normalized.Contains(normalizedPhrase))
                    {
                        targetContainerKind = "requester_backpack";
                        matchedPhrase = normalizedPhrase;
                        break;
                    }
                }
            }

            if (targetContainerKind == null || String.IsNullOrWhiteSpace(matchedPhrase))
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

            int endIndex = normalized.IndexOf(matchedPhrase, verbIndex, StringComparison.OrdinalIgnoreCase);
            if (endIndex < 0)
                return null;

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

            ParsedContainerAddRequest result = new ParsedContainerAddRequest();
            result.Amount = amount;
            result.TypePhrase = typePhrase;
            result.TargetContainerKind = targetContainerKind;
            return result;
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

        private static ParsedPropsReadRequest ParsePropsReadRequest(string question)
        {
            string normalized = AIGMConstructableResolver.Normalize(question);
            if (String.IsNullOrWhiteSpace(normalized))
                return null;

            bool looksLikeInspect =
                normalized.Contains("inspect") ||
                normalized.Contains("examine") ||
                normalized.Contains("readproperties") ||
                normalized.Contains("readprops") ||
                normalized.Contains("whatis");

            if (!looksLikeInspect)
                return null;

            ParsedPropsReadRequest result = new ParsedPropsReadRequest();

            if (normalized.Contains("inmybag") || normalized.Contains("inmybackpack") || normalized.Contains("inmypack"))
            {
                result.TargetMode = "container_named_item";
                result.TargetContainerKind = "requester_backpack";
                result.TargetName = ExtractNameBeforeContainerPhrase(question, new[] { "in my backpack", "in my bag", "in my pack" });
                return result;
            }

            if (normalized.Contains("nearestcontainer"))
                result.TargetMode = "nearest_container";
            else if (normalized.Contains("nearestdoor"))
                result.TargetMode = "nearest_door";
            else if (normalized.Contains("nearestitem"))
                result.TargetMode = "nearest_item";
            else if (normalized.Contains("nearestmobile") || normalized.Contains("dragon") || normalized.Contains("vendor") || normalized.Contains("counselor") || normalized.Contains("seaserpent") || normalized.Contains("healer"))
                result.TargetMode = "nearest_mobile";
            else if (normalized.Contains("mytarget") || normalized.Contains("currenttarget") || normalized.Contains("target"))
                result.TargetMode = "nearest_mobile";
            else
                result.TargetMode = "nearest_mobile";

            result.TargetName = ExtractLooseTargetName(question);
            return result;
        }

        private static AIGMActionProposal BuildMovementProposal(string actionKind, string title, string description)
        {
            AIGMActionProposal action = new AIGMActionProposal();
            action.ActionKind = actionKind;
            action.Title = title;
            action.Category = "navigate";
            action.Description = description;
            action.RequiresConfirmation = false;
            action.EnsureParameters();
            action.Parameters["commandSurface"] = "Movement";
            return action;
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

        private static string BuildContainerTitle(int amount, string typeName, string targetContainerKind)
        {
            return "Create " + amount + " " + typeName + " in " + HumanizeContainerKind(targetContainerKind);
        }

        private static string BuildContainerDescription(int amount, string typeName, string targetContainerKind)
        {
            return "Use the native Add-backed GM adapter to create " + amount + " " + typeName + " in " + HumanizeContainerKind(targetContainerKind) + ".";
        }

        private static string HumanizeContainerKind(string targetContainerKind)
        {
            if (String.Equals(targetContainerKind, "counselor_pack", StringComparison.OrdinalIgnoreCase))
                return "the counselor pack";

            if (String.Equals(targetContainerKind, "requester_backpack", StringComparison.OrdinalIgnoreCase))
                return "your backpack";

            return "the target container";
        }

        private static string ExtractNameBeforeContainerPhrase(string question, string[] phrases)
        {
            if (String.IsNullOrWhiteSpace(question) || phrases == null)
                return null;

            string lowered = question.ToLowerInvariant();
            for (int i = 0; i < phrases.Length; i++)
            {
                string phrase = phrases[i];
                int index = lowered.IndexOf(phrase, StringComparison.OrdinalIgnoreCase);
                if (index < 0)
                    continue;

                string left = question.Substring(0, index).Trim();
                left = left.Replace("inspect", String.Empty).Replace("Inspect", String.Empty).Replace("examine", String.Empty).Replace("Examine", String.Empty).Replace("read properties of", String.Empty).Replace("Read properties of", String.Empty).Replace("what is", String.Empty).Replace("What is", String.Empty).Trim();
                string[] parts = left.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                List<string> words = new List<string>();
                for (int p = 0; p < parts.Length; p++)
                {
                    string word = parts[p].ToLowerInvariant();
                    if (word == "the" || word == "a" || word == "an" || word == "my")
                        continue;

                    words.Add(parts[p]);
                }

                return String.Join(" ", words.ToArray()).Trim();
            }

            return null;
        }

        private static string ExtractLooseTargetName(string question)
        {
            if (String.IsNullOrWhiteSpace(question))
                return null;

            string lowered = question.ToLowerInvariant();

            if (lowered.Contains(" in my bag") || lowered.Contains(" in my backpack") || lowered.Contains(" in my pack"))
                return null;
            string[] stopWords = new[] { "nearest", "mobile", "item", "container", "door", "target", "this", "that", "the", "a", "an", "my", "what", "is", "inspect", "examine", "read", "properties", "of", "please" };
            string[] parts = lowered.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> kept = new List<string>();
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i].Trim(',', '.', '?', '!', ':', ';');
                bool skip = false;
                for (int s = 0; s < stopWords.Length; s++)
                {
                    if (part == stopWords[s])
                    {
                        skip = true;
                        break;
                    }
                }

                if (!skip)
                    kept.Add(part);
            }

            return kept.Count > 0 ? String.Join(" ", kept.ToArray()) : null;
        }

        private static string BuildPropsReadTitle(ParsedPropsReadRequest parsed)
        {
            if (parsed != null && !String.IsNullOrWhiteSpace(parsed.TargetName) && !String.IsNullOrWhiteSpace(parsed.TargetContainerKind))
                return "Inspect deeply: " + parsed.TargetName + " in " + HumanizeContainerKind(parsed.TargetContainerKind);

            if (parsed != null && !String.IsNullOrWhiteSpace(parsed.TargetName))
                return "Inspect deeply: " + parsed.TargetName;

            return "Inspect deeply: " + HumanizePropsTargetMode(parsed != null ? parsed.TargetMode : null);
        }

        private static string BuildPropsReadDescription(ParsedPropsReadRequest parsed)
        {
            if (parsed != null && !String.IsNullOrWhiteSpace(parsed.TargetName) && !String.IsNullOrWhiteSpace(parsed.TargetContainerKind))
                return "Read full native properties for " + parsed.TargetName + " in " + HumanizeContainerKind(parsed.TargetContainerKind) + ".";

            if (parsed != null && !String.IsNullOrWhiteSpace(parsed.TargetName))
                return "Read full native properties for " + parsed.TargetName + ".";

            return "Read full native properties for " + HumanizePropsTargetMode(parsed != null ? parsed.TargetMode : null) + ".";
        }

        private static string HumanizePropsTargetMode(string targetMode)
        {
            if (String.Equals(targetMode, "nearest_container", StringComparison.OrdinalIgnoreCase))
                return "the nearest container";

            if (String.Equals(targetMode, "nearest_door", StringComparison.OrdinalIgnoreCase))
                return "the nearest door";

            if (String.Equals(targetMode, "nearest_item", StringComparison.OrdinalIgnoreCase))
                return "the nearest item";

            return "the nearest mobile";
        }

        private sealed class ParsedPropsReadRequest
        {
            public string TargetMode;
            public string TargetName;
            public string TargetContainerKind;
        }

        private sealed class ParsedContainerAddRequest
        {
            public int Amount;
            public string TypePhrase;
            public string TargetContainerKind;
        }

        private sealed class ParsedWorldAddRequest
        {
            public int Amount;
            public string TypePhrase;
        }
    }
}
