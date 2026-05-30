using System;
using System.Collections.Generic;
using System.Linq;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMProposalAugmenter
    {
        public static void Augment(AIGMResponse response, string question)
        {
            if (response == null)
                return;

            if (response.ProposedActions == null)
                response.ProposedActions = new List<AIGMActionProposal>();

            TryAddBandageNativeAddProposal(response, question);
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

        private static void TryAddBandageNativeAddProposal(AIGMResponse response, string question)
        {
            if (String.IsNullOrWhiteSpace(question))
                return;

            string q = question.ToLowerInvariant();

            bool wantsCreate =
                q.Contains("create") ||
                q.Contains("spawn") ||
                q.Contains("make") ||
                q.Contains("add");

            bool wantsBandage = q.Contains("bandage");

            bool wantsWorld =
                q.Contains("at my feet") ||
                q.Contains("near me") ||
                q.Contains("on ground") ||
                q.Contains("on the ground") ||
                q.Contains("here");

            if (!wantsCreate || !wantsBandage || !wantsWorld)
                return;

            bool alreadyExists = response.ProposedActions.Any(a =>
                a != null &&
                String.Equals(a.ActionKind, "gm_add_world_item", StringComparison.OrdinalIgnoreCase) &&
                String.Equals(a.GetParameter("typeName"), "Bandage", StringComparison.OrdinalIgnoreCase));

            if (alreadyExists)
                return;

            AIGMActionProposal action = new AIGMActionProposal();
            action.ActionKind = "gm_add_world_item";
            action.Title = "Create 25 bandages at your feet";
            action.Category = "mutate";
            action.Description = "Use the native Add-backed GM adapter to create 25 Bandages at the requester's feet.";
            action.RequiresConfirmation = true;
            action.EnsureParameters();
            action.Parameters["commandSurface"] = "Add";
            action.Parameters["typeName"] = "Bandage";
            action.Parameters["amount"] = "25";
            action.Parameters["placement"] = "requester_feet";
            action.Parameters["range"] = "1";

            response.ProposedActions.Add(action);
        }

        private static void TryAddGeneralWorldAddProposal(AIGMResponse response, string question)
        {
            if (String.IsNullOrWhiteSpace(question))
                return;

            string q = question.ToLowerInvariant();

            bool wantsCreate =
                q.Contains("create") ||
                q.Contains("spawn") ||
                q.Contains("make") ||
                q.Contains("add");

            bool wantsWorld =
                q.Contains("at my feet") ||
                q.Contains("near me") ||
                q.Contains("on ground") ||
                q.Contains("on the ground") ||
                q.Contains("here");

            if (!wantsCreate || !wantsWorld)
                return;

            string typeName = null;
            string titleName = null;

            if (q.Contains("scissors"))
            {
                typeName = "Scissors";
                titleName = "scissors";
            }
            else if (q.Contains("torch"))
            {
                typeName = "Torch";
                titleName = "torch";
            }
            else if (q.Contains("apple"))
            {
                typeName = "Apple";
                titleName = "apple";
            }
            else if (q.Contains("katana"))
            {
                typeName = "Katana";
                titleName = "katana";
            }
            else
            {
                return;
            }

            bool alreadyExists = response.ProposedActions.Any(a =>
                a != null &&
                String.Equals(a.ActionKind, "gm_add_world_item", StringComparison.OrdinalIgnoreCase) &&
                String.Equals(a.GetParameter("typeName"), typeName, StringComparison.OrdinalIgnoreCase));

            if (alreadyExists)
                return;

            AIGMActionProposal action = new AIGMActionProposal();
            action.ActionKind = "gm_add_world_item";
            action.Title = "Create " + titleName + " at your feet";
            action.Category = "mutate";
            action.Description = "Use the native Add-backed GM adapter to create " + titleName + " at the requester's feet.";
            action.RequiresConfirmation = true;
            action.EnsureParameters();
            action.Parameters["commandSurface"] = "Add";
            action.Parameters["typeName"] = typeName;
            action.Parameters["amount"] = "1";
            action.Parameters["placement"] = "requester_feet";
            action.Parameters["range"] = "1";

            response.ProposedActions.Add(action);
        }
    }
}
