using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Server.Custom.AIGM
{
    public static class AIGMStubResponder
    {
        public static AIGMResponse Ask(Mobile from, string question, AIGMTargetInfo target)
        {
            AIGMResponse response = new AIGMResponse();
            response.Ok = true;
            response.Confidence = "low";

            string raw = question ?? String.Empty;
            string normalized = raw.Trim();
            string lowered = normalized.ToLowerInvariant();
            string targetSummary = target != null ? target.ToSummaryString() : "No target selected.";

            TryAddMovementActions(response, from, normalized, lowered, target);
            TryAddCounselorInventoryActions(response, normalized, lowered);

            if (response.ProposedActions.Count > 0)
            {
                response.Confidence = "medium";
                response.ReplyText = BuildActionReply(response, normalized, targetSummary);
                response.SuggestedChecks.Add("Click one of the proposed movement actions to execute it in-game.");
                response.SuggestedChecks.Add("Use follow/stop for escort behavior, or named/coordinate travel for navigation.");
                response.Warnings.Add("This is still stub-side intent parsing, not live bridge reasoning.");
                response.LikelyFiles.Add("Scripts\\Custom\\AIGM\\AIGMMovementController.cs");
                response.LikelyFiles.Add("Scripts\\Custom\\AIGM\\AIGMActionExecutor.cs");
                response.LikelyFiles.Add("Scripts\\Mobiles\\NPCs\\AIGMCounselor.cs");
                return response;
            }

            response.ReplyText = String.Format(
                "Bridge is not connected yet, but the in-game shell is working. I received your question:<BR><BR><I>{0}</I><BR><BR>Current target:<BR>{1}",
                Utility.FixHtml(raw),
                Utility.FixHtml(targetSummary));

            response.LikelyFiles.Add("Scripts\\Mobiles\\NPCs\\AIGMCounselor.cs");
            response.LikelyFiles.Add("Scripts\\Gumps\\AIGMQuestionGump.cs");
            response.LikelyFiles.Add("Scripts\\Gumps\\AIGMResponseGump.cs");

            response.SuggestedChecks.Add("Verify question entry and gump flow feel good in-game.");
            response.SuggestedChecks.Add("Verify target selection captures the object you care about.");
            response.SuggestedChecks.Add("Next step: replace this stub with a localhost bridge client.");

            response.Warnings.Add("This is a stub response, not live AI reasoning.");
            return response;
        }

        private static void TryAddMovementActions(AIGMResponse response, Mobile from, string normalized, string lowered, AIGMTargetInfo target)
        {
            if (response == null || String.IsNullOrWhiteSpace(lowered))
                return;

            string arrivalAction;
            if (TryExtractArrivalAction(lowered, out arrivalAction))
            {
                AIGMActionProposal arrival = CreateAction(
                    AIGMCommandAction.SetArrivalAction,
                    "Set arrival action",
                    "Configures what the counselor should do after reaching the destination.",
                    "navigate",
                    false);
                arrival.Parameters["arrivalAction"] = arrivalAction;
                response.ProposedActions.Add(arrival);
                response.NextAction = response.NextAction ?? arrival;
            }

            AddRouteChainingActions(response, normalized, lowered);

            if (ContainsAny(lowered, "pause", "pause movement", "hold on", "wait there for now"))
            {
                AIGMActionProposal pause = CreateAction(
                    AIGMCommandAction.PauseMovement,
                    "Pause movement",
                    "Pauses the counselor's active movement without discarding the route.",
                    "navigate",
                    false);
                response.ProposedActions.Add(pause);
                response.NextAction = response.NextAction ?? pause;
            }

            if (ContainsAny(lowered, "resume", "continue route", "keep going", "go on"))
            {
                AIGMActionProposal resume = CreateAction(
                    AIGMCommandAction.ResumeMovement,
                    "Resume movement",
                    "Resumes the counselor's paused movement.",
                    "navigate",
                    false);
                response.ProposedActions.Add(resume);
                response.NextAction = response.NextAction ?? resume;
            }

            if (ContainsAny(lowered, "cancel route", "cancel movement", "forget that route", "stop and cancel"))
            {
                AIGMActionProposal cancel = CreateAction(
                    AIGMCommandAction.CancelMovement,
                    "Cancel movement",
                    "Cancels the counselor's active or paused movement plan.",
                    "navigate",
                    false);
                response.ProposedActions.Add(cancel);
                response.NextAction = response.NextAction ?? cancel;
            }

            if (ContainsAny(lowered, "where are you", "status", "where are you headed", "what are you doing", "movement status"))
            {
                AIGMActionProposal status = CreateAction(
                    AIGMCommandAction.MovementStatus,
                    "Check movement status",
                    "Reports the counselor's current movement mode and destination.",
                    "read",
                    false);
                response.ProposedActions.Add(status);
                response.NextAction = response.NextAction ?? status;
            }

            if (ContainsAny(lowered, "go to target", "path to target", "move to target", "go near target"))
            {
                AIGMActionProposal pathTarget = CreateAction(
                    AIGMCommandAction.PathToCurrentTarget,
                    "Path to current target",
                    "Moves the counselor toward the currently selected AI GM target.",
                    "navigate",
                    false);
                response.ProposedActions.Add(pathTarget);
                response.NextAction = response.NextAction ?? pathTarget;
            }

            if (ContainsAny(lowered, "follow target", "follow current target", "shadow target"))
            {
                AIGMActionProposal followTarget = CreateAction(
                    AIGMCommandAction.FollowCurrentTarget,
                    "Follow current target",
                    "Puts the counselor into follow mode for the selected target mobile.",
                    "navigate",
                    false);
                response.ProposedActions.Add(followTarget);
                response.NextAction = response.NextAction ?? followTarget;
            }

            if (ContainsAny(lowered, "stop following", "stop follow", "stop moving", "stay here", "halt", "stop"))
            {
                AIGMActionProposal stop = CreateAction(
                    AIGMCommandAction.StopFollowing,
                    "Stop moving",
                    "Stops current follow or pathing behavior.",
                    "navigate",
                    false);
                response.ProposedActions.Add(stop);
                response.NextAction = response.NextAction ?? stop;
            }

            if (ContainsAny(lowered, "follow me", "come with me", "follow us", "follow him", "follow her", "follow them"))
            {
                AIGMActionProposal follow = CreateAction(
                    AIGMCommandAction.FollowMobile,
                    "Follow target mobile",
                    "Puts the counselor into follow mode.",
                    "navigate",
                    false);

                if (target != null && String.Equals(target.Kind, "Mobile", StringComparison.OrdinalIgnoreCase))
                    follow.Parameters["targetSerial"] = target.Serial.ToString();

                response.ProposedActions.Add(follow);
                if (response.NextAction == null)
                    response.NextAction = follow;
            }

            Match coordMatch = Regex.Match(lowered, @"\b(?:go to|walk to|head to|move to|path to|reroute to)\s+(\d{1,5})\s*[, ]\s*(\d{1,5})(?:\s*[, ]\s*(-?\d{1,4}))?\b");
            if (coordMatch.Success)
            {
                AIGMActionProposal path = CreateAction(
                    AIGMCommandAction.PathToCoordinates,
                    "Path to coordinates",
                    "Walks the counselor to the requested coordinates.",
                    "navigate",
                    false);
                path.Parameters["x"] = coordMatch.Groups[1].Value;
                path.Parameters["y"] = coordMatch.Groups[2].Value;
                if (coordMatch.Groups[3].Success)
                    path.Parameters["z"] = coordMatch.Groups[3].Value;

                response.ProposedActions.Add(path);
                if (response.NextAction == null)
                    response.NextAction = path;
            }

            string namedDestination = ExtractNamedDestination(normalized, lowered);
            if (!String.IsNullOrWhiteSpace(namedDestination))
            {
                AIGMActionProposal named = CreateAction(
                    AIGMCommandAction.PathToNamedLocation,
                    "Path to named location",
                    "Walks the counselor to a known named destination.",
                    "navigate",
                    false);
                named.Parameters["destinationName"] = namedDestination;
                response.ProposedActions.Add(named);
                if (response.NextAction == null)
                    response.NextAction = named;
            }

            if (ContainsAny(lowered, "come here", "come to me"))
            {
                AIGMActionProposal here = CreateAction(
                    AIGMCommandAction.PathToCoordinates,
                    "Path to my location",
                    "Walks the counselor to your current position.",
                    "navigate",
                    false);
                if (from != null)
                {
                    here.Parameters["x"] = from.X.ToString();
                    here.Parameters["y"] = from.Y.ToString();
                    here.Parameters["z"] = from.Z.ToString();
                }
                response.ProposedActions.Add(here);
                if (response.NextAction == null)
                    response.NextAction = here;
            }
        }

        private static void AddRouteChainingActions(AIGMResponse response, string normalized, string lowered)
        {
            Match nextCoord = Regex.Match(normalized, @"\b(?:then|after that|next)\s+(?:go to|walk to|head to|move to)\s+(\d{1,5})\s*[, ]\s*(\d{1,5})(?:\s*[, ]\s*(-?\d{1,4}))?\b", RegexOptions.IgnoreCase);
            if (nextCoord.Success)
            {
                AIGMActionProposal queued = CreateAction(
                    AIGMCommandAction.QueueCoordinateRouteStop,
                    "Queue coordinate stop",
                    "Adds another coordinate stop after the current destination.",
                    "navigate",
                    false);
                queued.Parameters["x"] = nextCoord.Groups[1].Value;
                queued.Parameters["y"] = nextCoord.Groups[2].Value;
                if (nextCoord.Groups[3].Success)
                    queued.Parameters["z"] = nextCoord.Groups[3].Value;
                response.ProposedActions.Add(queued);
            }

            Match nextNamed = Regex.Match(normalized, @"\b(?:then|after that|next)\s+(?:go to|walk to|head to|move to)\s+([A-Za-z][A-Za-z '\-]{1,60})", RegexOptions.IgnoreCase);
            if (nextNamed.Success)
            {
                string candidate = nextNamed.Groups[1].Value.Trim();
                candidate = Regex.Replace(candidate, @"\s+(and|then)\s+.*$", String.Empty, RegexOptions.IgnoreCase).Trim();
                if (!Regex.IsMatch(candidate, @"^\d+[ ,]\d+"))
                {
                    AIGMActionProposal queued = CreateAction(
                        AIGMCommandAction.QueueNamedRouteStop,
                        "Queue named stop",
                        "Adds another named destination after the current stop.",
                        "navigate",
                        false);
                    queued.Parameters["destinationName"] = candidate;
                    response.ProposedActions.Add(queued);
                }
            }
        }

        private static bool TryExtractArrivalAction(string lowered, out string arrivalAction)
        {
            arrivalAction = null;
            if (String.IsNullOrWhiteSpace(lowered))
                return false;

            if (ContainsAny(lowered, "and wait", "then wait", "and stay", "then stay"))
            {
                arrivalAction = "wait";
                return true;
            }

            if (ContainsAny(lowered, "then follow me", "and follow me after", "and then follow me"))
            {
                arrivalAction = "follow_me";
                return true;
            }

            if (ContainsAny(lowered, "and scan nearby mobiles", "then scan nearby mobiles", "and inspect nearby mobiles"))
            {
                arrivalAction = "scan_nearby_mobiles";
                return true;
            }

            if (ContainsAny(lowered, "and scan nearby items", "then scan nearby items", "and inspect nearby items"))
            {
                arrivalAction = "scan_nearby_items";
                return true;
            }

            return false;
        }

        private static string BuildActionReply(AIGMResponse response, string normalized, string targetSummary)
        {
            AIGMActionProposal next = response.NextAction;
            string nextDescription = next != null ? next.Description : "an action";
            return String.Format(
                "I parsed that as an actionable request. Current target: {0}<BR><BR>Request: <I>{1}</I><BR><BR>I prepared {2} so you can execute it directly.",
                Utility.FixHtml(targetSummary),
                Utility.FixHtml(normalized ?? String.Empty),
                Utility.FixHtml(nextDescription));
        }

        private static void TryAddCounselorInventoryActions(AIGMResponse response, string normalized, string lowered)
        {
            if (response == null || String.IsNullOrWhiteSpace(lowered))
                return;

            if (ContainsAny(lowered, "open your bag", "open your backpack", "show me your bag", "show me your backpack", "open counselor bag", "open counselor pack"))
            {
                AIGMActionProposal openPack = CreateAction(
                    AIGMCommandAction.OpenCounselorPack,
                    "Open counselor pack",
                    "Opens the counselor's primary pack container.",
                    "read",
                    false);
                response.ProposedActions.Add(openPack);
                response.NextAction = response.NextAction ?? openPack;
            }

            if (ContainsAny(lowered, "place it in your bag", "place it in your pack", "put it in your bag", "put it in your pack", "in your bag", "in your pack"))
            {
                string itemName = ExtractRequestedItemAlias(normalized, lowered);
                if (!String.IsNullOrWhiteSpace(itemName))
                {
                    AIGMActionProposal spawn = CreateAction(
                        AIGMCommandAction.SpawnItemToCounselorPack,
                        "Create item in counselor pack",
                        "Creates an allowed item and places it in the counselor's pack.",
                        "mutate",
                        false);
                    spawn.Parameters["itemName"] = itemName;

                    int amount;
                    if (TryExtractAmount(lowered, out amount) && amount > 0)
                        spawn.Parameters["amount"] = amount.ToString();

                    if (lowered.IndexOf("blessed", StringComparison.OrdinalIgnoreCase) >= 0)
                        spawn.Parameters["blessed"] = "true";

                    response.ProposedActions.Add(spawn);
                    response.NextAction = response.NextAction ?? spawn;
                }
            }
        }

        private static string ExtractRequestedItemAlias(string normalized, string lowered)
        {
            if (String.IsNullOrWhiteSpace(lowered))
                return null;

            string[] knownItems = new string[]
            {
                "katana",
                "longsword",
                "broadsword",
                "dagger",
                "bandage",
                "bandages",
                "blank scroll",
                "blank scrolls",
                "recall rune",
                "rune",
                "gold",
                "black pearl",
                "bloodmoss",
                "garlic",
                "ginseng",
                "mandrake root",
                "nightshade",
                "sulfurous ash",
                "spiders silk",
                "spider silk",
                "scissors"
            };

            for (int i = 0; i < knownItems.Length; i++)
            {
                if (lowered.IndexOf(knownItems[i], StringComparison.OrdinalIgnoreCase) >= 0)
                    return knownItems[i];
            }

            return null;
        }

        private static bool TryExtractAmount(string lowered, out int amount)
        {
            amount = 0;
            Match amountMatch = Regex.Match(lowered ?? String.Empty, @"\b(\d{1,5})\b");
            if (!amountMatch.Success)
                return false;

            return Int32.TryParse(amountMatch.Groups[1].Value, out amount);
        }

        private static AIGMActionProposal CreateAction(string commandName, string description, string preview, string category, bool requiresConfirmation)
        {
            AIGMActionProposal action = new AIGMActionProposal();
            action.ActionKind = "run_gm_command";
            action.Title = description;
            action.Description = description;
            action.Category = category;
            action.PreviewText = preview;
            action.RequiresConfirmation = requiresConfirmation;
            action.Parameters["commandName"] = commandName;
            return action;
        }

        private static bool ContainsAny(string input, params string[] candidates)
        {
            if (String.IsNullOrWhiteSpace(input) || candidates == null)
                return false;

            for (int i = 0; i < candidates.Length; i++)
            {
                if (!String.IsNullOrWhiteSpace(candidates[i]) && input.IndexOf(candidates[i], StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        private static string ExtractNamedDestination(string normalized, string lowered)
        {
            if (String.IsNullOrWhiteSpace(lowered))
                return null;

            Match match = Regex.Match(normalized, @"\b(?:go to|walk to|head to|move to|path to|reroute to)\s+([A-Za-z][A-Za-z '\-]{1,60})", RegexOptions.IgnoreCase);
            if (!match.Success)
                return null;

            string candidate = (match.Groups[1].Value ?? String.Empty).Trim();
            candidate = Regex.Replace(candidate, @"\s+(and|then)\s+.*$", String.Empty, RegexOptions.IgnoreCase).Trim();
            if (Regex.IsMatch(candidate, @"^\d+[ ,]\d+"))
                return null;

            return candidate;
        }
    }
}
