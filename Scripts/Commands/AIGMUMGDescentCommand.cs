using System;
using System.Collections.Generic;
using Server.Custom.AIGM;
using Server.Custom.AIGM.UMG;

namespace Server.Commands
{
    public static class AIGMUMGDescentCommand
    {
        private static readonly string[] ScenarioIds =
        {
            "quiet",
            "enemy-mage",
            "protectee-injured",
            "low-mana",
            "enemy-mage-low-mana",
            "protectee-injured-low-mana",
            "multi-pressure",
            "commander-hold",
            "commander-stop",
            "stand-down",
            "capability-missing",
            "target-lost",
            "threat-cleared"
        };

        public static void Initialize()
        {
            CommandSystem.Register("umgdescent", AccessLevel.Player, OnCommand);
        }

        private static void OnCommand(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            string verb;
            string rest;
            SplitFirst(Clean(e.ArgString), out verb, out rest);
            if (String.IsNullOrWhiteSpace(verb))
            {
                Usage(e.Mobile);
                return;
            }

            if (String.Equals(verb, "audit", StringComparison.OrdinalIgnoreCase))
            {
                string selector;
                string auditRest;
                SplitFirst(rest, out selector, out auditRest);
                if (!String.Equals(selector, "all", StringComparison.OrdinalIgnoreCase))
                {
                    e.Mobile.SendMessage(68, "Usage: [umgdescent audit all]");
                    return;
                }

                if (!RequireGameMaster(e.Mobile))
                    return;

                e.Mobile.SendMessage(68, BuildAuditAll());
                return;
            }

            if (String.Equals(verb, "receipt", StringComparison.OrdinalIgnoreCase))
            {
                ShowReceipt(e.Mobile, rest);
                return;
            }

            string selectorText;
            string detailText;
            SplitFirst(rest, out selectorText, out detailText);
            if (String.IsNullOrWhiteSpace(selectorText))
            {
                Usage(e.Mobile);
                return;
            }

            Mobile actor;
            string failureCode;
            string failureMessage;
            if (!AIGMUMGSleeveAccessService.TryResolveCompanionSelector(selectorText, out actor, out failureCode, out failureMessage))
            {
                e.Mobile.SendMessage(38, failureMessage);
                return;
            }

            if (!AuthorizePreview(e.Mobile, actor))
                return;

            if (String.Equals(verb, "preview", StringComparison.OrdinalIgnoreCase))
            {
                AIGMUMGDescentReceipt receipt = AIGMUMGPreviewRuntimeService.PreviewCurrentWorld(actor, e.Mobile);
                e.Mobile.SendMessage(receipt.Errors.Count == 0 ? 68 : 38, AIGMUMGPreviewRuntimeService.BuildReceiptSummary(receipt));
                return;
            }

            if (String.Equals(verb, "scenario", StringComparison.OrdinalIgnoreCase))
            {
                RunScenario(e.Mobile, actor, detailText);
                return;
            }

            if (String.Equals(verb, "proof", StringComparison.OrdinalIgnoreCase))
            {
                if (!RequireGameMaster(e.Mobile))
                    return;

                RunProof(e.Mobile, actor, detailText);
                return;
            }

            if (String.Equals(verb, "trace", StringComparison.OrdinalIgnoreCase))
            {
                e.Mobile.SendMessage(68, AIGMUMGDescentTraceService.BuildTraceSummary(AIGMUMGSleeveAccessService.FormatSerial(actor)));
                return;
            }

            if (String.Equals(verb, "state", StringComparison.OrdinalIgnoreCase))
            {
                AIGMUMGOperationalLayoutVersionRecord approved;
                string code;
                if (!AIGMUMGPreviewRuntimeService.TryGetApprovedLayout(actor, out approved, out code))
                {
                    e.Mobile.SendMessage(38, "D1E state unavailable: {0}", code);
                    return;
                }

                e.Mobile.SendMessage(68, AIGMUMGPreviewRuntimeStateStore.BuildSummary(AIGMUMGSleeveAccessService.FormatSerial(actor), approved.VersionId));
                return;
            }

            if (String.Equals(verb, "reset", StringComparison.OrdinalIgnoreCase))
            {
                if (!RequireGameMaster(e.Mobile))
                    return;

                e.Mobile.SendMessage(68, AIGMUMGPreviewRuntimeService.ResetActorState(actor));
                return;
            }

            if (String.Equals(verb, "watch", StringComparison.OrdinalIgnoreCase))
            {
                if (!RequireGameMaster(e.Mobile))
                    return;

                if (String.Equals(detailText, "on", StringComparison.OrdinalIgnoreCase))
                    e.Mobile.SendMessage(68, AIGMUMGPreviewWatchService.SetWatch(actor, true));
                else if (String.Equals(detailText, "off", StringComparison.OrdinalIgnoreCase))
                    e.Mobile.SendMessage(68, AIGMUMGPreviewWatchService.SetWatch(actor, false));
                else
                    e.Mobile.SendMessage(68, "Usage: [umgdescent watch <serial|name> on|off]");
                return;
            }

            Usage(e.Mobile);
        }

        private static void RunScenario(Mobile caller, Mobile actor, string scenarioId)
        {
            if (String.IsNullOrWhiteSpace(scenarioId))
            {
                caller.SendMessage(68, "Usage: [umgdescent scenario <serial|name> <scenario|all>]");
                caller.SendMessage(68, "Scenarios: {0}", String.Join(", ", ScenarioIds));
                return;
            }

            if (String.Equals(scenarioId, "all", StringComparison.OrdinalIgnoreCase))
            {
                for (int i = 0; i < ScenarioIds.Length; i++)
                {
                    AIGMUMGDescentReceipt receipt = AIGMUMGPreviewRuntimeService.PreviewScenario(actor, caller, ScenarioIds[i]);
                    caller.SendMessage(receipt.Errors.Count == 0 ? 68 : 38, "{0}: {1}", ScenarioIds[i], AIGMUMGPreviewRuntimeService.BuildReceiptSummary(receipt));
                }
                return;
            }

            AIGMUMGDescentReceipt single = AIGMUMGPreviewRuntimeService.PreviewScenario(actor, caller, scenarioId);
            caller.SendMessage(single.Errors.Count == 0 ? 68 : 38, AIGMUMGPreviewRuntimeService.BuildReceiptSummary(single));
        }

        private static void RunProof(Mobile caller, Mobile actor, string proofId)
        {
            if (!String.Equals(proofId, "hysteresis", StringComparison.OrdinalIgnoreCase))
            {
                caller.SendMessage(68, "Usage: [umgdescent proof <serial|name> hysteresis]");
                return;
            }

            DateTime baseUtc = new DateTime(2026, 7, 29, 0, 0, 0, DateTimeKind.Utc);
            List<string> lines = new List<string>();
            try
            {
                AIGMUMGPreviewRuntimeService.ResetActorState(actor);
                AIGMUMGPreviewClock.SetProofClock(baseUtc);
                AddProofStep(caller, actor, lines, "mana.activate", "low-mana");
                AIGMUMGPreviewClock.AdvanceProofClock(TimeSpan.FromSeconds(1.0));
                AddProofStep(caller, actor, lines, "mana.above_activation_below_release", "mana-threshold-41");
                AIGMUMGPreviewClock.AdvanceProofClock(TimeSpan.FromSeconds(5.0));
                AddProofStep(caller, actor, lines, "mana.still_below_release", "mana-threshold-41");
                AIGMUMGPreviewClock.AdvanceProofClock(TimeSpan.FromSeconds(1.0));
                AddProofStep(caller, actor, lines, "mana.release_candidate", "mana-threshold-56");
                AIGMUMGPreviewClock.AdvanceProofClock(TimeSpan.FromSeconds(2.0));
                AddProofStep(caller, actor, lines, "mana.release_stability_holds", "mana-threshold-56");
                AIGMUMGPreviewClock.AdvanceProofClock(TimeSpan.FromSeconds(2.0));
                AddProofStep(caller, actor, lines, "mana.cooldown_begins", "mana-threshold-56");
                AIGMUMGPreviewClock.AdvanceProofClock(TimeSpan.FromSeconds(1.0));
                AddProofStep(caller, actor, lines, "mana.cooldown_blocks_reentry", "low-mana");
                AIGMUMGPreviewClock.AdvanceProofClock(TimeSpan.FromSeconds(5.0));
                AddProofStep(caller, actor, lines, "mana.reentry_after_cooldown", "low-mana");

                AIGMUMGPreviewRuntimeService.ResetActorState(actor);
                AIGMUMGPreviewClock.SetProofClock(baseUtc.AddMinutes(10.0));
                AddProofStep(caller, actor, lines, "heal.activate", "protectee-threshold-44");
                AIGMUMGPreviewClock.AdvanceProofClock(TimeSpan.FromSeconds(6.0));
                AddProofStep(caller, actor, lines, "heal.above_activation_below_release", "protectee-threshold-48");
                AIGMUMGPreviewClock.AdvanceProofClock(TimeSpan.FromSeconds(1.0));
                AddProofStep(caller, actor, lines, "heal.release_candidate", "protectee-threshold-56");
                AIGMUMGPreviewClock.AdvanceProofClock(TimeSpan.FromSeconds(4.0));
                AddProofStep(caller, actor, lines, "heal.cooldown_begins", "protectee-threshold-56");

                AIGMUMGPreviewRuntimeService.ResetActorState(actor);
                AIGMUMGPreviewClock.SetProofClock(baseUtc.AddMinutes(20.0));
                AddProofStep(caller, actor, lines, "combat.activate", "enemy-mage");
                AIGMUMGPreviewClock.AdvanceProofClock(TimeSpan.FromSeconds(1.0));
                AddProofStep(caller, actor, lines, "combat.minimum_active_duration", "threat-cleared");
                AIGMUMGPreviewClock.AdvanceProofClock(TimeSpan.FromSeconds(5.0));
                AddProofStep(caller, actor, lines, "combat.release_candidate", "threat-cleared");
                AIGMUMGPreviewClock.AdvanceProofClock(TimeSpan.FromSeconds(4.0));
                AddProofStep(caller, actor, lines, "combat.cooldown_begins", "threat-cleared");

                AIGMUMGPreviewRuntimeService.ResetActorState(actor);
                AIGMUMGPreviewClock.SetProofClock(baseUtc.AddMinutes(30.0));
                AIGMUMGDescentReceipt first = AIGMUMGPreviewRuntimeService.PreviewScenario(actor, caller, "enemy-mage");
                AIGMUMGPreviewRuntimeService.ResetActorState(actor);
                AIGMUMGPreviewClock.SetProofClock(baseUtc.AddMinutes(30.0));
                AIGMUMGDescentReceipt second = AIGMUMGPreviewRuntimeService.PreviewScenario(actor, caller, "enemy-mage");
                lines.Add(String.Format("deterministic_repeat={0}; first={1}; second={2}",
                    String.Equals(first.DecisionFingerprint, second.DecisionFingerprint, StringComparison.OrdinalIgnoreCase) ? "passed" : "failed",
                    first.DecisionFingerprint,
                    second.DecisionFingerprint));
            }
            finally
            {
                AIGMUMGPreviewClock.UseSystemClock();
            }

            caller.SendMessage(68, "D1E hysteresis proof complete; proofClock=restored; dispatch=false; receiptLines={0}", lines.Count);
            for (int i = 0; i < lines.Count; i++)
                caller.SendMessage(68, lines[i]);
        }

        private static void AddProofStep(Mobile caller, Mobile actor, List<string> lines, string label, string scenarioId)
        {
            AIGMUMGDescentReceipt receipt = AIGMUMGPreviewRuntimeService.PreviewScenario(actor, caller, scenarioId);
            string resource = DescribeFamily(receipt, AIGMUMGOperationalLayoutService.FamilyResources);
            string protection = DescribeFamily(receipt, AIGMUMGOperationalLayoutService.FamilyProtection);
            string combat = DescribeFamily(receipt, AIGMUMGOperationalLayoutService.FamilyCombat);
            lines.Add(String.Format("{0}; scenario={1}; combat={2}; resources={3}; protection={4}; final={5}; invocations={6}",
                label,
                scenarioId,
                combat,
                resource,
                protection,
                receipt.FinalResult,
                receipt.CognitionBudget != null ? receipt.CognitionBudget.AdapterInvocations : 0));
        }

        private static string DescribeFamily(AIGMUMGDescentReceipt receipt, string familyId)
        {
            if (receipt == null)
                return "none";

            for (int i = 0; i < receipt.CandidateBranches.Count; i++)
            {
                AIGMUMGDescentCandidateBranch branch = receipt.CandidateBranches[i];
                if (!String.Equals(branch.FamilyId, familyId, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (branch.State == AIGMUMGPreviewBranchState.ActivePreview || branch.State == AIGMUMGPreviewBranchState.CoolingDown)
                    return branch.StackName + "/" + branch.State + "/" + branch.HysteresisState;
            }

            for (int i = 0; i < receipt.SelectedBranchByFamily.Count; i++)
            {
                AIGMUMGFamilySelection selected = receipt.SelectedBranchByFamily[i];
                if (String.Equals(selected.FamilyId, familyId, StringComparison.OrdinalIgnoreCase))
                    return (String.IsNullOrWhiteSpace(selected.SelectedStackName) ? "none" : selected.SelectedStackName) + "/" + selected.State + "/" + selected.Reason;
            }

            return "none";
        }

        private static void ShowReceipt(Mobile caller, string selector)
        {
            if (String.IsNullOrWhiteSpace(selector))
            {
                caller.SendMessage(68, "Usage: [umgdescent receipt <receiptId|correlationId|decisionFingerprint>]");
                return;
            }

            AIGMUMGDescentReceipt receipt = AIGMUMGDescentTraceService.FindByReceiptId(selector);
            if (receipt == null)
                receipt = AIGMUMGDescentTraceService.FindByCorrelationId(selector);
            if (receipt == null)
                receipt = AIGMUMGDescentTraceService.FindByDecisionFingerprint(selector);

            if (receipt == null)
            {
                caller.SendMessage(38, "D1E receipt not found in bounded recent history.");
                return;
            }

            caller.SendMessage(68, AIGMUMGPreviewRuntimeService.BuildReceiptSummary(receipt));
            caller.SendMessage(68, "AlwaysOn={0}; matched={1}; unmatched={2}; unconfigured={3}; suspended={4}; capabilityBlocked={5}; governanceBlocked={6}; final={7}",
                receipt.AlwaysOnNodes.Count,
                receipt.MatchedTriggers.Count,
                receipt.UnmatchedTriggers.Count,
                receipt.UnconfiguredTriggers.Count,
                receipt.SuspendedBranches.Count,
                receipt.CapabilityBlockedBranches.Count,
                receipt.GovernanceBlockedBranches.Count,
                receipt.FinalResult);
            caller.SendMessage(68, "TypedIntent={0}; adapter={1}; mapping={2}; invocationAllowed={3}; invocationAttempted={4}",
                receipt.TypedIntent != null ? receipt.TypedIntent.Category.ToString() : "none",
                receipt.AdapterMapping != null ? receipt.AdapterMapping.AdapterName : "none",
                receipt.AdapterMapping != null ? receipt.AdapterMapping.MappingResult : "none",
                receipt.AdapterMapping != null && receipt.AdapterMapping.InvocationAllowed,
                receipt.AdapterMapping != null && receipt.AdapterMapping.InvocationAttempted);
        }

        private static bool AuthorizePreview(Mobile caller, Mobile actor)
        {
            AIGMUMGSleeveAccessResult access = AIGMUMGSleeveAccessService.ValidateForGumpButton(caller, actor);
            if (!access.Accepted)
            {
                caller.SendMessage(38, access.Message);
                return false;
            }

            return true;
        }

        private static bool RequireGameMaster(Mobile caller)
        {
            if (caller != null && caller.AccessLevel >= AccessLevel.GameMaster)
                return true;

            if (caller != null)
                caller.SendMessage(38, "D1E administrative Preview command requires Game Master access.");
            return false;
        }

        private static string BuildAuditAll()
        {
            int registered = 0;
            int approved = 0;
            int noLayout = 0;
            int watched = 0;
            List<string> examples = new List<string>();

            foreach (Mobile mobile in World.Mobiles.Values)
            {
                IAIGMCompanionActor companion;
                if (!AIGMUMGSleeveAccessService.IsRegisteredAIGMCompanion(mobile, out companion))
                    continue;

                registered++;
                AIGMUMGOperationalLayoutVersionRecord version;
                string code;
                if (AIGMUMGPreviewRuntimeService.TryGetApprovedLayout(mobile, out version, out code))
                {
                    approved++;
                    if (examples.Count < 8)
                        examples.Add(String.Format("{0}[{1}] approved={2}", mobile.Name, AIGMUMGSleeveAccessService.FormatSerial(mobile), version.VersionId));
                }
                else
                {
                    noLayout++;
                }

                if (AIGMUMGPreviewWatchService.IsWatched(mobile))
                    watched++;
            }

            return String.Format("D1E audit all: registered={0}; approvedPreviewOnlyLayouts={1}; noApprovedLayout={2}; watched={3}; dispatch=false; watch={4}; examples={5}",
                registered,
                approved,
                noLayout,
                watched,
                AIGMUMGPreviewWatchService.BuildAudit(),
                examples.Count == 0 ? "none" : String.Join(" | ", examples.ToArray()));
        }

        private static void Usage(Mobile mobile)
        {
            mobile.SendMessage(68, "Usage: [umgdescent preview <serial|name>] | [umgdescent scenario <serial|name> <scenario|all>] | [umgdescent proof <serial|name> hysteresis] | [umgdescent trace <serial|name>] | [umgdescent receipt <id>] | [umgdescent state <serial|name>] | [umgdescent reset <serial|name>] | [umgdescent watch <serial|name> on|off] | [umgdescent audit all]");
        }

        private static void SplitFirst(string text, out string first, out string rest)
        {
            first = String.Empty;
            rest = String.Empty;
            if (String.IsNullOrWhiteSpace(text))
                return;

            string trimmed = text.Trim();
            int space = trimmed.IndexOf(' ');
            if (space < 0)
            {
                first = trimmed;
                return;
            }

            first = trimmed.Substring(0, space).Trim();
            rest = trimmed.Substring(space + 1).Trim();
        }

        private static string Clean(string value)
        {
            return value != null ? value.Trim() : String.Empty;
        }
    }
}
