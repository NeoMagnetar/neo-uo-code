using System;
using System.Collections.Generic;
using Server.Custom.AIGM;
using Server.Custom.AIGM.Tasks;
using Server.Mobiles;

namespace Server.Custom.AIGM.UMG
{
    public static class AIGMUMGPreviewRuntimeService
    {
        public const string FinalPreviewResult = "PREVIEW_ONLY_NOT_DISPATCHED";
        private const int MaxFamilies = 16;
        private const int MaxStacks = 64;
        private const int MaxReferences = 128;
        private const int MaxTriggerProfiles = 128;

        private static readonly object CacheSyncRoot = new object();
        private static readonly Dictionary<string, AIGMUMGDescentReceipt> DecisionCache = new Dictionary<string, AIGMUMGDescentReceipt>(StringComparer.OrdinalIgnoreCase);

        private static readonly string[] ForegroundFamilies =
        {
            AIGMUMGOperationalLayoutService.FamilyCombat,
            AIGMUMGOperationalLayoutService.FamilyPositioning,
            AIGMUMGOperationalLayoutService.FamilyResources,
            AIGMUMGOperationalLayoutService.FamilyProtection
        };

        public static AIGMUMGDescentReceipt PreviewCurrentWorld(Mobile actor, Mobile caller)
        {
            string correlationId = NewCorrelationId();
            AIGMUMGSituationSnapshot snapshot = AIGMUMGSituationSnapshot.CaptureCurrentWorld(actor, correlationId);
            return Evaluate(actor, caller, snapshot, false);
        }

        public static AIGMUMGDescentReceipt PreviewScenario(Mobile actor, Mobile caller, string scenarioId)
        {
            string correlationId = NewCorrelationId();
            AIGMUMGSituationSnapshot snapshot = AIGMUMGSituationSnapshot.BuildSynthetic(actor, scenarioId, AIGMUMGPreviewClock.UtcNow, correlationId);
            return Evaluate(actor, caller, snapshot, false);
        }

        public static string BuildStatus(Mobile actor)
        {
            if (actor == null || actor.Deleted)
                return "D1E status: actor missing.";

            AIGMUMGOperationalLayoutVersionRecord approved;
            string code;
            if (!TryGetApprovedLayout(actor, out approved, out code))
                return "D1E status: " + code + "; actor=" + Describe(actor) + "; final=" + FinalPreviewResult;

            string actorSerial = FormatSerial(actor);
            AIGMUMGActivationGraph graph = AIGMUMGActivationGraphBuilder.Build(actor, approved);
            return String.Format(
                "D1E status actor={0}; layoutVersion={1}; revision={2}; graph={3}; graphValid={4}; runtime={5}; dispatch=false; final={6}",
                Describe(actor),
                approved.VersionId,
                approved.Revision,
                Short(graph.GraphFingerprint),
                graph.ValidationResult.IsValid,
                AIGMUMGPreviewRuntimeStateStore.BuildSummary(actorSerial, approved.VersionId),
                FinalPreviewResult);
        }

        public static string ResetActorState(Mobile actor)
        {
            if (actor == null || actor.Deleted)
                return "D1E reset failed: actor missing.";

            int states = AIGMUMGPreviewRuntimeStateStore.ResetActor(FormatSerial(actor));
            int receipts = AIGMUMGDescentTraceService.ClearActorReceipts(FormatSerial(actor));
            return String.Format("D1E preview state reset for {0}; states={1}; receipts={2}; saved layout/configuration untouched.", Describe(actor), states, receipts);
        }

        public static string BuildReceiptSummary(AIGMUMGDescentReceipt receipt)
        {
            if (receipt == null)
                return "No D1E receipt.";

            List<string> selected = new List<string>();
            for (int i = 0; i < receipt.SelectedBranchByFamily.Count; i++)
            {
                AIGMUMGFamilySelection family = receipt.SelectedBranchByFamily[i];
                selected.Add(family.FamilyId + "=" + (String.IsNullOrWhiteSpace(family.SelectedStackName) ? "none" : family.SelectedStackName + "/" + family.State));
            }

            string intent = receipt.TypedIntent != null ? receipt.TypedIntent.Category.ToString() : "none";
            string adapter = receipt.AdapterMapping != null ? receipt.AdapterMapping.AdapterName : "none";
            return String.Format(
                "D1E receipt={0}; source={1}; scenario={2}; layout={3}; graph={4}; decision={5}; runtime={6}; selected=[{7}]; intent={8}; adapter={9}; attempts={10}; invocations={11}; final={12}",
                Short(receipt.ReceiptId),
                receipt.SnapshotSource,
                String.IsNullOrWhiteSpace(receipt.SnapshotScenarioId) ? "none" : receipt.SnapshotScenarioId,
                receipt.ApprovedLayoutVersionId,
                Short(receipt.GraphFingerprint),
                Short(receipt.DecisionFingerprint),
                receipt.RuntimeStateKind,
                String.Join(", ", selected.ToArray()),
                intent,
                adapter,
                receipt.CognitionBudget.AdapterInvocationAttempts,
                receipt.CognitionBudget.AdapterInvocations,
                receipt.FinalResult);
        }

        public static bool TryGetApprovedLayout(Mobile actor, out AIGMUMGOperationalLayoutVersionRecord approved, out string code)
        {
            approved = null;
            code = String.Empty;
            if (actor == null || actor.Deleted)
            {
                code = "ACTOR_MISSING";
                return false;
            }

            string actorKey = AIGMUMGOperationalLayoutService.ResolveOperationalActorKey(actor);
            List<AIGMUMGOperationalLayoutVersionRecord> versions = AIGMUMGOperationalLayoutService.GetLayoutVersionsForActor(actorKey);
            versions.Sort(delegate (AIGMUMGOperationalLayoutVersionRecord left, AIGMUMGOperationalLayoutVersionRecord right)
            {
                int byRevision = right.Revision.CompareTo(left.Revision);
                if (byRevision != 0)
                    return byRevision;
                return String.Compare(right.VersionId, left.VersionId, StringComparison.OrdinalIgnoreCase);
            });

            string actorSerial = FormatSerial(actor);
            for (int i = 0; i < versions.Count; i++)
            {
                AIGMUMGOperationalLayoutVersionRecord version = versions[i];
                if (version == null || version.LayoutSnapshot == null)
                    continue;

                if (version.State != AIGMUMGOperationalLayoutState.ApprovedPreviewOnly || version.ExecutionMode != AIGMUMGExecutionMode.PreviewOnly)
                    continue;

                AIGMUMGOperationalLayout layout = version.LayoutSnapshot;
                if (layout.State != AIGMUMGOperationalLayoutState.ApprovedPreviewOnly || layout.ExecutionMode != AIGMUMGExecutionMode.PreviewOnly)
                    continue;

                if (!String.Equals(AIGMUMGStableHash.Normalize(layout.ActorKey), AIGMUMGStableHash.Normalize(actorKey), StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!String.Equals(layout.ActorSerialAtSave, actorSerial, StringComparison.OrdinalIgnoreCase))
                    continue;

                approved = version;
                return true;
            }

            code = "NO_APPROVED_OPERATIONAL_LAYOUT";
            return false;
        }

        private static AIGMUMGDescentReceipt Evaluate(Mobile actor, Mobile caller, AIGMUMGSituationSnapshot snapshot, bool diagnosticUnsavedLayout)
        {
            DateTime now = AIGMUMGPreviewClock.UtcNow;
            AIGMUMGDescentReceipt receipt = CreateBaseReceipt(actor, snapshot, now);
            if (diagnosticUnsavedLayout)
                receipt.RuntimeStateKind = AIGMUMGRuntimeStateKind.DIAGNOSTIC_UNSAVED_LAYOUT;

            if (actor == null || actor.Deleted)
            {
                receipt.Errors.Add("ACTOR_MISSING");
                FinalizeAndRecord(receipt, String.Empty, String.Empty);
                return receipt;
            }

            if (caller != null)
            {
                AIGMUMGSleeveAccessResult access = AIGMUMGSleeveAccessService.ValidateForGumpButton(caller, actor);
                if (!access.Accepted)
                {
                    receipt.Errors.Add("ACCESS_REJECTED:" + access.ResultCode);
                    FinalizeAndRecord(receipt, String.Empty, String.Empty);
                    return receipt;
                }
            }

            AIGMUMGOperationalLayoutVersionRecord approved;
            string approvedFailure;
            if (!TryGetApprovedLayout(actor, out approved, out approvedFailure))
            {
                receipt.Errors.Add(approvedFailure);
                receipt.RuntimeStateKind = AIGMUMGRuntimeStateKind.COLD_RUNTIME_STATE;
                FinalizeAndRecord(receipt, String.Empty, String.Empty);
                return receipt;
            }

            AIGMUMGOperationalLayout layout = approved.LayoutSnapshot;
            receipt.ApprovedLayoutId = layout.LayoutId;
            receipt.ApprovedLayoutVersionId = approved.VersionId;
            receipt.LayoutRevision = approved.Revision;
            receipt.RuntimeStateKind = AIGMUMGPreviewRuntimeStateStore.HasState(receipt.ActorSerial, approved.VersionId)
                ? AIGMUMGRuntimeStateKind.WARM_RUNTIME_STATE
                : AIGMUMGRuntimeStateKind.COLD_RUNTIME_STATE;

            AIGMUMGActivationGraph graph = AIGMUMGActivationGraphBuilder.Build(actor, approved);
            receipt.GraphVersion = graph.GraphVersion;
            receipt.GraphFingerprint = graph.GraphFingerprint;
            receipt.AlwaysOnNodes.AddRange(graph.AlwaysOnNodeIds);
            receipt.Warnings.AddRange(graph.ValidationResult.Warnings);
            receipt.Errors.AddRange(graph.ValidationResult.Errors);
            if (!graph.ValidationResult.IsValid)
            {
                receipt.TypedIntent = null;
                receipt.AdapterMapping = null;
                FinalizeAndRecord(receipt, BuildSnapshotFingerprint(snapshot), AIGMUMGPreviewRuntimeStateStore.Fingerprint(receipt.ActorSerial, approved.VersionId));
                return receipt;
            }

            CountInstalled(layout, receipt.CognitionBudget);
            if (receipt.CognitionBudget.InstalledFamilies > MaxFamilies
                || receipt.CognitionBudget.InstalledStacks > MaxStacks
                || receipt.CognitionBudget.InstalledReferences > MaxReferences)
            {
                receipt.CognitionBudget.BudgetExceeded = true;
                receipt.Errors.Add("COGNITION_BUDGET_EXCEEDED");
                receipt.TypedIntent = null;
                receipt.AdapterMapping = null;
                FinalizeAndRecord(receipt, BuildSnapshotFingerprint(snapshot), AIGMUMGPreviewRuntimeStateStore.Fingerprint(receipt.ActorSerial, approved.VersionId));
                return receipt;
            }

            string snapshotFingerprint = BuildSnapshotFingerprint(snapshot);
            string runtimeFingerprint = AIGMUMGPreviewRuntimeStateStore.Fingerprint(receipt.ActorSerial, approved.VersionId);
            string cacheKey = BuildDecisionCacheKey(receipt.ActorSerial, approved.VersionId, graph.GraphFingerprint, snapshotFingerprint, runtimeFingerprint, now);
            AIGMUMGDescentReceipt cached;
            if (TryGetCached(cacheKey, out cached))
            {
                AIGMUMGDescentReceipt reused = AIGMUMGRepository.RoundTripClone(cached);
                reused.ReceiptId = Guid.NewGuid().ToString("N");
                reused.CapturedUtc = now;
                reused.RuntimeStateKind = AIGMUMGRuntimeStateKind.CACHED_RESULT_REUSED;
                reused.Warnings.Add("CACHE_REUSED");
                AIGMUMGDescentTraceService.Record(reused);
                return reused;
            }

            List<AIGMUMGDescentCandidateBranch> candidates = EvaluateBranches(actor, layout, receipt, snapshot, now);
            SelectForegroundBranches(receipt, candidates, now);
            receipt.TypedIntent = CompileTypedIntent(receipt, snapshot);
            receipt.AdapterMapping = MapAdapter(receipt.TypedIntent);
            receipt.CognitionBudget.TypedIntents = receipt.TypedIntent != null ? 1 : 0;
            receipt.CognitionBudget.AdapterMappings = receipt.AdapterMapping != null && !String.IsNullOrWhiteSpace(receipt.AdapterMapping.AdapterName) ? 1 : 0;
            receipt.CognitionBudget.AdapterInvocationAttempts = 0;
            receipt.CognitionBudget.AdapterInvocations = 0;

            FinalizeAndRecord(receipt, snapshotFingerprint, runtimeFingerprint);
            StoreCached(cacheKey, receipt);
            return receipt;
        }

        private static List<AIGMUMGDescentCandidateBranch> EvaluateBranches(Mobile actor, AIGMUMGOperationalLayout layout, AIGMUMGDescentReceipt receipt, AIGMUMGSituationSnapshot snapshot, DateTime now)
        {
            List<AIGMUMGDescentCandidateBranch> candidates = new List<AIGMUMGDescentCandidateBranch>();
            List<AIGMUMGOperationalFamily> families = new List<AIGMUMGOperationalFamily>(layout.Families);
            families.Sort(CompareFamilies);
            for (int f = 0; f < families.Count; f++)
            {
                AIGMUMGOperationalFamily family = families[f];
                if (family == null || String.Equals(family.FamilyId, AIGMUMGOperationalLayoutService.FamilyAlwaysOn, StringComparison.OrdinalIgnoreCase))
                    continue;

                List<AIGMUMGOperationalNeoStack> stacks = family.OperationalNeoStacks != null
                    ? new List<AIGMUMGOperationalNeoStack>(family.OperationalNeoStacks)
                    : new List<AIGMUMGOperationalNeoStack>();
                stacks.Sort(CompareStacks);
                for (int s = 0; s < stacks.Count; s++)
                {
                    AIGMUMGOperationalNeoStack stack = stacks[s];
                    if (stack == null)
                        continue;

                    AIGMUMGDescentCandidateBranch candidate = new AIGMUMGDescentCandidateBranch
                    {
                        FamilyId = family.FamilyId,
                        FamilyName = family.DisplayName,
                        StackId = stack.StackId,
                        StackName = stack.DisplayName,
                        StackOrder = stack.Order,
                        Priority = stack.Order
                    };
                    candidates.Add(candidate);
                    receipt.CandidateBranches.Add(candidate);

                    if (!family.Enabled || !stack.Enabled)
                    {
                        candidate.State = AIGMUMGPreviewBranchState.BlockedByConfiguration;
                        candidate.ComparisonFactors.Add("configuration_enabled=false");
                        continue;
                    }

                    List<string> invalidDefinitions = new List<string>();
                    List<AIGMUMGResolvedReference> resolved = AIGMUMGActivationGraphBuilder.ResolveStackReferences(actor, stack, invalidDefinitions);
                    AddReferenceSummary(candidate, stack, resolved, invalidDefinitions);
                    if (invalidDefinitions.Count > 0)
                    {
                        candidate.State = AIGMUMGPreviewBranchState.InvalidReference;
                        receipt.InvalidBranches.Add(candidate.StackId + ":" + String.Join(",", invalidDefinitions.ToArray()));
                        continue;
                    }

                    List<AIGMUMGTriggerProfile> profiles = BuildProfiles(family, stack, resolved);
                    receipt.CognitionBudget.TriggerProfilesEvaluated += profiles.Count;
                    if (receipt.CognitionBudget.TriggerProfilesEvaluated > MaxTriggerProfiles)
                    {
                        receipt.CognitionBudget.BudgetExceeded = true;
                        receipt.Errors.Add("COGNITION_BUDGET_EXCEEDED");
                        candidate.State = AIGMUMGPreviewBranchState.BlockedByConfiguration;
                        continue;
                    }

                    if (profiles.Count == 0)
                    {
                        candidate.State = AIGMUMGPreviewBranchState.Unconfigured;
                        candidate.UnconfiguredTriggers.Add("UNCONFIGURED_TRIGGER_PROFILE");
                        receipt.UnconfiguredTriggers.Add(candidate.StackId + ":UNCONFIGURED_TRIGGER_PROFILE");
                        continue;
                    }

                    if (HasHardGovernance(snapshot))
                    {
                        candidate.Governance = ValidateGovernance(snapshot, candidate);
                        receipt.CognitionBudget.GovernanceChecks++;
                        if (!candidate.Governance.Allowed)
                        {
                            candidate.State = AIGMUMGPreviewBranchState.BlockedByGovernance;
                            receipt.GovernanceBlockedBranches.Add(candidate.StackId + ":" + String.Join(",", candidate.Governance.Reasons.ToArray()));
                            continue;
                        }
                    }

                    EvaluateTriggers(profiles, snapshot, candidate, receipt);
                    if (candidate.MatchedTriggers.Count == 0)
                    {
                        candidate.State = AIGMUMGPreviewBranchState.Dormant;
                        candidate.HysteresisState = ApplyDormantHysteresis(receipt, candidate, profiles, now);
                        continue;
                    }

                    candidate.Capability = ValidateCapability(actor, snapshot, resolved);
                    receipt.CognitionBudget.CapabilityChecks++;
                    if (candidate.Capability.State == AIGMUMGCapabilityGateState.Blocked)
                    {
                        candidate.State = AIGMUMGPreviewBranchState.BlockedByCapability;
                        receipt.CapabilityBlockedBranches.Add(candidate.StackId + ":" + String.Join(",", candidate.Capability.Failed.ToArray()));
                        continue;
                    }

                    if (!HasHardGovernance(snapshot))
                    {
                        candidate.Governance = ValidateGovernance(snapshot, candidate);
                        receipt.CognitionBudget.GovernanceChecks++;
                        if (!candidate.Governance.Allowed)
                        {
                            candidate.State = AIGMUMGPreviewBranchState.BlockedByGovernance;
                            receipt.GovernanceBlockedBranches.Add(candidate.StackId + ":" + String.Join(",", candidate.Governance.Reasons.ToArray()));
                            continue;
                        }
                    }

                    candidate.HysteresisState = ApplyEligibilityHysteresis(receipt, candidate, profiles, now);
                    if (candidate.State == AIGMUMGPreviewBranchState.CoolingDown)
                        receipt.CoolingDownBranches.Add(candidate.StackId);
                    else if (candidate.State == AIGMUMGPreviewBranchState.Eligible)
                        receipt.CognitionBudget.EligibleBranches++;
                }
            }

            return candidates;
        }

        private static void SelectForegroundBranches(AIGMUMGDescentReceipt receipt, List<AIGMUMGDescentCandidateBranch> candidates, DateTime now)
        {
            for (int i = 0; i < ForegroundFamilies.Length; i++)
            {
                string familyId = ForegroundFamilies[i];
                List<AIGMUMGDescentCandidateBranch> familyCandidates = new List<AIGMUMGDescentCandidateBranch>();
                for (int c = 0; c < candidates.Count; c++)
                {
                    if (String.Equals(candidates[c].FamilyId, familyId, StringComparison.OrdinalIgnoreCase))
                        familyCandidates.Add(candidates[c]);
                }

                receipt.CognitionBudget.ForegroundedBranches += familyCandidates.Count;
                List<AIGMUMGDescentCandidateBranch> eligible = new List<AIGMUMGDescentCandidateBranch>();
                for (int c = 0; c < familyCandidates.Count; c++)
                {
                    if (familyCandidates[c].State == AIGMUMGPreviewBranchState.Eligible)
                        eligible.Add(familyCandidates[c]);
                }

                if (eligible.Count == 0)
                {
                    receipt.SelectedBranchByFamily.Add(new AIGMUMGFamilySelection
                    {
                        FamilyId = familyId,
                        State = AIGMUMGPreviewBranchState.Dormant,
                        Reason = "NO_ELIGIBLE_BRANCH"
                    });
                    continue;
                }

                eligible.Sort(CompareCandidatesForSelection);
                AIGMUMGDescentCandidateBranch winner = eligible[0];
                winner.State = AIGMUMGPreviewBranchState.ActivePreview;
                winner.ComparisonFactors.Add("selected_by_deterministic_order");
                receipt.SelectedBranchByFamily.Add(new AIGMUMGFamilySelection
                {
                    FamilyId = familyId,
                    SelectedStackId = winner.StackId,
                    SelectedStackName = winner.StackName,
                    State = AIGMUMGPreviewBranchState.ActivePreview,
                    Reason = "ACTIVE PREVIEW - NOT DISPATCHED"
                });
                receipt.SelectedNeoBlockReferences.AddRange(winner.ReferenceIds);
                receipt.CognitionBudget.SelectedBranches++;

                for (int c = 1; c < eligible.Count; c++)
                {
                    AIGMUMGDescentCandidateBranch loser = eligible[c];
                    loser.State = AIGMUMGPreviewBranchState.SuspendedBySelector;
                    loser.SuspendedByStackId = winner.StackId;
                    loser.ComparisonFactors.Add("suspended_by=" + winner.StackId);
                    receipt.SuspendedBranches.Add(loser.StackId + "->" + winner.StackId);
                }
            }

            for (int i = 0; i < candidates.Count; i++)
                UpdateRuntimeRecord(receipt, candidates[i], now);
        }

        private static AIGMUMGTypedIntentProposal CompileTypedIntent(AIGMUMGDescentReceipt receipt, AIGMUMGSituationSnapshot snapshot)
        {
            AIGMUMGTypedIntentProposal proposal = new AIGMUMGTypedIntentProposal();
            proposal.Parameters.Add(new AIGMUMGKeyValue("preview_only", "true"));
            proposal.Parameters.Add(new AIGMUMGKeyValue("tactical_dispatch_enabled", AIGMUMGPhase64C2Invariant.TacticalDispatchEnabled ? "true" : "false"));

            if (Contains(snapshot.OperationalState, "AbsoluteGMHold"))
            {
                proposal.Category = AIGMUMGTypedIntentCategory.NoOpPreview;
                proposal.ConflictResolution = "absolute_gm_hold_blocks_tactical_output";
                return proposal;
            }

            if (Contains(snapshot.ExplicitCommanderOrder, "stop") || Contains(snapshot.OperationalState, "CancelMission"))
            {
                proposal.Category = AIGMUMGTypedIntentCategory.NoOpPreview;
                proposal.ConflictResolution = "explicit_stop_or_cancel_priority";
                return proposal;
            }

            if (Contains(snapshot.ExplicitCommanderOrder, "hold"))
            {
                proposal.Category = AIGMUMGTypedIntentCategory.HoldPositionPreview;
                proposal.PrimaryFamilyId = AIGMUMGOperationalLayoutService.FamilyPositioning;
                proposal.ConflictResolution = "hold_priority_reduces_tactical_output";
                return proposal;
            }

            AIGMUMGDescentCandidateBranch protection = FindSelected(receipt, AIGMUMGOperationalLayoutService.FamilyProtection);
            AIGMUMGDescentCandidateBranch combat = FindSelected(receipt, AIGMUMGOperationalLayoutService.FamilyCombat);
            AIGMUMGDescentCandidateBranch positioning = FindSelected(receipt, AIGMUMGOperationalLayoutService.FamilyPositioning);
            AIGMUMGDescentCandidateBranch resources = FindSelected(receipt, AIGMUMGOperationalLayoutService.FamilyResources);

            AIGMUMGDescentCandidateBranch primary = null;
            if (protection != null)
            {
                primary = protection;
                proposal.Category = Contains(protection.StackName, "heal") ? AIGMUMGTypedIntentCategory.HealTargetPreview : AIGMUMGTypedIntentCategory.ProtectTargetPreview;
            }
            else if (combat != null)
            {
                primary = combat;
                proposal.Category = snapshot.NearbyHostiles.Count > 0 ? AIGMUMGTypedIntentCategory.AttackTargetPreview : AIGMUMGTypedIntentCategory.ObserveThreatPreview;
            }
            else if (positioning != null)
            {
                primary = positioning;
                proposal.Category = Contains(positioning.StackName, "retreat") || Contains(positioning.StackName, "reposition")
                    ? AIGMUMGTypedIntentCategory.RepositionPreview
                    : AIGMUMGTypedIntentCategory.HoldPositionPreview;
            }
            else if (resources != null)
            {
                primary = resources;
                proposal.Category = Contains(resources.StackName, "ammunition")
                    ? AIGMUMGTypedIntentCategory.ConserveAmmunitionPreview
                    : AIGMUMGTypedIntentCategory.ConserveManaPreview;
            }
            else
            {
                proposal.Category = AIGMUMGTypedIntentCategory.NoOpPreview;
                proposal.ConflictResolution = "NO_ELIGIBLE_BRANCH";
            }

            if (primary != null)
            {
                proposal.PrimaryFamilyId = primary.FamilyId;
                proposal.SourceStackId = primary.StackId;
                proposal.SourceDefinitionIds.AddRange(primary.DefinitionIds);
                proposal.Parameters.Add(new AIGMUMGKeyValue("source_stack", primary.StackId));
            }

            AddSupportingFamily(proposal, receipt, AIGMUMGOperationalLayoutService.FamilyCombat);
            AddSupportingFamily(proposal, receipt, AIGMUMGOperationalLayoutService.FamilyPositioning);
            AddSupportingFamily(proposal, receipt, AIGMUMGOperationalLayoutService.FamilyResources);
            AddSupportingFamily(proposal, receipt, AIGMUMGOperationalLayoutService.FamilyProtection);
            if (String.IsNullOrWhiteSpace(proposal.ConflictResolution))
                proposal.ConflictResolution = "deterministic_intent_precedence";
            return proposal;
        }

        private static AIGMUMGAdapterMappingReceipt MapAdapter(AIGMUMGTypedIntentProposal intent)
        {
            AIGMUMGAdapterMappingReceipt mapping = new AIGMUMGAdapterMappingReceipt();
            mapping.InvocationAllowed = false;
            mapping.InvocationAttempted = false;
            if (intent == null)
            {
                mapping.MappingResult = "NO_TYPED_INTENT";
                return mapping;
            }

            switch (intent.Category)
            {
                case AIGMUMGTypedIntentCategory.AttackTargetPreview:
                    mapping.AdapterId = "adapter.native_combat_bridge.preview";
                    mapping.AdapterName = "AIGMNativeCombatBridge";
                    mapping.RequiredCapabilities.Add(AIGMUMGCapabilityKind.CanGuard.ToString());
                    break;
                case AIGMUMGTypedIntentCategory.ProtectTargetPreview:
                    mapping.AdapterId = "adapter.operational_guard.preview";
                    mapping.AdapterName = "AIGMOperationalControlService.GuardPreview";
                    mapping.RequiredCapabilities.Add(AIGMUMGCapabilityKind.CanGuard.ToString());
                    break;
                case AIGMUMGTypedIntentCategory.HealTargetPreview:
                    mapping.AdapterId = "adapter.healing.preview";
                    mapping.AdapterName = "AIGMCompanionHealingService";
                    mapping.RequiredCapabilities.Add(AIGMUMGCapabilityKind.CanHeal.ToString());
                    break;
                case AIGMUMGTypedIntentCategory.RepositionPreview:
                case AIGMUMGTypedIntentCategory.RegroupPreview:
                    mapping.AdapterId = "adapter.movement.preview";
                    mapping.AdapterName = "AIGMSmartMovementService/AIGMRosterTaskService";
                    mapping.RequiredCapabilities.Add(AIGMUMGCapabilityKind.CanFollow.ToString());
                    break;
                case AIGMUMGTypedIntentCategory.HoldPositionPreview:
                    mapping.AdapterId = "adapter.hold_position.preview";
                    mapping.AdapterName = "AIGMOperationalControlService.HoldPreview";
                    break;
                case AIGMUMGTypedIntentCategory.ConserveManaPreview:
                    mapping.AdapterId = "adapter.spell_policy.preview";
                    mapping.AdapterName = "AIGMCompanionSpellService.PolicyOnly";
                    break;
                case AIGMUMGTypedIntentCategory.ConserveAmmunitionPreview:
                    mapping.AdapterId = "adapter.inventory_policy.preview";
                    mapping.AdapterName = "AIGMCompanionInventoryPolicy.ReadOnly";
                    break;
                case AIGMUMGTypedIntentCategory.ObserveThreatPreview:
                    mapping.AdapterId = "adapter.tracking.preview";
                    mapping.AdapterName = "AIGMTrackingHuntService/AIGMRosterTaskService";
                    mapping.RequiredCapabilities.Add(AIGMUMGCapabilityKind.CanTrack.ToString());
                    break;
                default:
                    mapping.AdapterId = "adapter.noop.preview";
                    mapping.AdapterName = "NoOpPreviewAdapter";
                    break;
            }

            mapping.MappingResult = AIGMUMGPhase64C2Invariant.TacticalDispatchEnabled
                ? "MAPPED_BUT_D1E_REQUIRES_PREVIEW_ONLY"
                : "MAPPED_PREVIEW_ONLY_NOT_INVOKED";
            return mapping;
        }

        private static List<AIGMUMGTriggerProfile> BuildProfiles(AIGMUMGOperationalFamily family, AIGMUMGOperationalNeoStack stack, List<AIGMUMGResolvedReference> resolved)
        {
            List<AIGMUMGTriggerProfile> profiles = new List<AIGMUMGTriggerProfile>();
            for (int i = 0; i < resolved.Count; i++)
            {
                AIGMUMGResolvedReference item = resolved[i];
                AIGMUMGTriggerProfile explicitProfile = BuildExplicitLocalProfile(family, stack, item);
                if (explicitProfile != null)
                {
                    profiles.Add(explicitProfile);
                    continue;
                }

                AIGMUMGTriggerProfile profile = BuildDefaultProfile(family, stack, item);
                if (profile != null)
                    profiles.Add(profile);
            }

            profiles.Sort(delegate (AIGMUMGTriggerProfile left, AIGMUMGTriggerProfile right)
            {
                int bySeverity = right.Severity.CompareTo(left.Severity);
                if (bySeverity != 0)
                    return bySeverity;
                return String.Compare(left.TriggerId, right.TriggerId, StringComparison.OrdinalIgnoreCase);
            });
            return profiles;
        }

        private static AIGMUMGTriggerProfile BuildExplicitLocalProfile(AIGMUMGOperationalFamily family, AIGMUMGOperationalNeoStack stack, AIGMUMGResolvedReference item)
        {
            AIGMUMGNeoBlockReference reference = item != null ? item.Reference : null;
            if (reference == null || reference.OptionalLocalParameters == null)
                return null;

            string triggerType = GetLocalParameter(reference, "trigger_type");
            if (String.IsNullOrWhiteSpace(triggerType))
                return null;

            AIGMUMGTriggerType parsed;
            try { parsed = (AIGMUMGTriggerType)Enum.Parse(typeof(AIGMUMGTriggerType), triggerType, true); }
            catch { return null; }

            return CreateProfile(family, stack, item, parsed, GetDouble(reference, "activation_threshold", 1.0), GetDouble(reference, "release_threshold", 0.0), GetInt(reference, "severity", 50), "approved_layout_local_parameters");
        }

        private static AIGMUMGTriggerProfile BuildDefaultProfile(AIGMUMGOperationalFamily family, AIGMUMGOperationalNeoStack stack, AIGMUMGResolvedReference item)
        {
            if (item == null)
                return null;

            string id = item.DefinitionId ?? String.Empty;
            string name = item.DisplayName ?? String.Empty;
            string familyId = family != null ? family.FamilyId : String.Empty;

            if (Contains(id, "ANTI_MAGE") || Contains(name, "Anti-Magic") || Contains(name, "Anti-Mage"))
                return CreateProfile(family, stack, item, AIGMUMGTriggerType.HostileMageDetected, 1, 0, 90, "phase64d1e_default_profile_registry");
            if (Contains(id, "LAST_STAND") || Contains(id, "RETREAT_AND_REGROUP") || Contains(name, "Retreat"))
                return CreateProfile(family, stack, item, AIGMUMGTriggerType.ActorHealthBelowThreshold, 25, 35, 85, "phase64d1e_default_profile_registry");
            if (Contains(id, "HEALER_SUPPORT") || Contains(name, "Healing"))
                return CreateProfile(family, stack, item, AIGMUMGTriggerType.AllyHealthBelowThreshold, 45, 55, 85, "phase64d1e_default_profile_registry");
            if (Contains(id, "PROTECT_CIVILIAN") || Contains(id, "PROTECT_OWNER") || Contains(name, "Protect"))
                return CreateProfile(family, stack, item, AIGMUMGTriggerType.ProtecteeThreatened, 1, 0, 88, "phase64d1e_default_profile_registry");
            if (Contains(id, "RESOURCE_CONSERVATION") || Contains(id, "WARRIOR_PRIEST") || Contains(id, "DARDALION_DEFENSIVE_SUPPORT") || Contains(name, "Mana"))
                return CreateProfile(family, stack, item, AIGMUMGTriggerType.ManaBelowThreshold, 40, 55, 80, "phase64d1e_default_profile_registry");
            if (Contains(id, "FRONTLINE_DEFENDER") || Contains(name, "Defensive Combat") || String.Equals(familyId, AIGMUMGOperationalLayoutService.FamilyCombat, StringComparison.OrdinalIgnoreCase))
                return CreateProfile(family, stack, item, AIGMUMGTriggerType.HostileDetected, 1, 0, 60, "phase64d1e_default_profile_registry");
            if (Contains(id, "REAR_LINE_ARCHER") || Contains(id, "HOLD_THE_LINE") || Contains(name, "Rear") || Contains(name, "Position"))
                return CreateProfile(family, stack, item, AIGMUMGTriggerType.HostileWithinRange, 8, 12, 45, "phase64d1e_default_profile_registry");
            if (Contains(id, "SCOUT") || Contains(id, "TRACKER"))
                return CreateProfile(family, stack, item, AIGMUMGTriggerType.HostileDetected, 1, 0, 35, "phase64d1e_default_profile_registry");

            return null;
        }

        private static AIGMUMGTriggerProfile CreateProfile(AIGMUMGOperationalFamily family, AIGMUMGOperationalNeoStack stack, AIGMUMGResolvedReference item, AIGMUMGTriggerType triggerType, double activation, double release, int severity, string provenance)
        {
            AIGMUMGTriggerProfile profile = new AIGMUMGTriggerProfile();
            profile.TriggerType = triggerType;
            profile.ActivationThreshold = activation;
            profile.ReleaseThreshold = release;
            profile.MinimumActiveDuration = TimeSpan.FromSeconds(5.0);
            profile.ReleaseStabilityDuration = TimeSpan.FromSeconds(3.0);
            profile.CooldownDuration = TimeSpan.FromSeconds(4.0);
            profile.ReentryRule = AIGMUMGTriggerReentryRule.RequireReleaseThenCooldown;
            profile.Severity = severity;
            profile.ApplicableFamily = family != null ? family.FamilyId : String.Empty;
            profile.ApplicableStackId = stack != null ? stack.StackId : String.Empty;
            profile.DefinitionId = item != null ? item.DefinitionId : String.Empty;
            profile.Provenance = provenance ?? String.Empty;
            profile.TriggerId = "trigger." + AIGMUMGStableHash.Normalize(profile.ApplicableFamily) + "." + AIGMUMGStableHash.Normalize(profile.ApplicableStackId) + "." + AIGMUMGStableHash.Normalize(profile.DefinitionId) + "." + triggerType;
            return profile;
        }

        private static void EvaluateTriggers(List<AIGMUMGTriggerProfile> profiles, AIGMUMGSituationSnapshot snapshot, AIGMUMGDescentCandidateBranch candidate, AIGMUMGDescentReceipt receipt)
        {
            for (int i = 0; i < profiles.Count; i++)
            {
                AIGMUMGTriggerEvaluation eval = EvaluateTrigger(profiles[i], snapshot);
                if (eval.Matched)
                {
                    candidate.MatchedTriggers.Add(eval);
                    receipt.MatchedTriggers.Add(eval);
                    candidate.Severity = Math.Max(candidate.Severity, eval.Severity);
                }
                else
                {
                    candidate.UnmatchedTriggers.Add(eval);
                    receipt.UnmatchedTriggers.Add(eval);
                }
            }
        }

        private static AIGMUMGTriggerEvaluation EvaluateTrigger(AIGMUMGTriggerProfile profile, AIGMUMGSituationSnapshot snapshot)
        {
            AIGMUMGTriggerEvaluation eval = new AIGMUMGTriggerEvaluation();
            eval.TriggerId = profile.TriggerId;
            eval.TriggerType = profile.TriggerType;
            eval.ActivationThreshold = profile.ActivationThreshold;
            eval.ReleaseThreshold = profile.ReleaseThreshold;
            eval.Severity = profile.Severity;
            eval.Provenance = profile.Provenance;

            switch (profile.TriggerType)
            {
                case AIGMUMGTriggerType.HostileDetected:
                    eval.ObservedValue = snapshot.NearbyHostiles.Count;
                    eval.Matched = eval.ObservedValue >= profile.ActivationThreshold;
                    break;
                case AIGMUMGTriggerType.HostileMageDetected:
                    eval.ObservedValue = CountMageHostiles(snapshot);
                    eval.Matched = eval.ObservedValue >= profile.ActivationThreshold;
                    break;
                case AIGMUMGTriggerType.HostileWithinRange:
                    eval.ObservedValue = NearestHostileDistance(snapshot);
                    eval.Matched = snapshot.NearbyHostiles.Count > 0 && eval.ObservedValue <= profile.ActivationThreshold;
                    break;
                case AIGMUMGTriggerType.ActorHealthBelowThreshold:
                    eval.ObservedValue = snapshot.ActorHealthPct;
                    eval.Matched = eval.ObservedValue <= profile.ActivationThreshold;
                    break;
                case AIGMUMGTriggerType.AllyHealthBelowThreshold:
                    eval.ObservedValue = LowestAllyHealthPct(snapshot);
                    eval.Matched = snapshot.NearbyAllies.Count > 0 && eval.ObservedValue <= profile.ActivationThreshold;
                    break;
                case AIGMUMGTriggerType.ProtecteeThreatened:
                    eval.ObservedValue = (!String.IsNullOrWhiteSpace(snapshot.CurrentProtectee) && (snapshot.NearbyHostiles.Count > 0 || LowestAllyHealthPct(snapshot) <= 55.0)) ? 1 : 0;
                    eval.Matched = eval.ObservedValue >= profile.ActivationThreshold;
                    break;
                case AIGMUMGTriggerType.ManaBelowThreshold:
                    eval.ObservedValue = snapshot.ActorManaPct;
                    eval.Matched = eval.ObservedValue <= profile.ActivationThreshold;
                    break;
                case AIGMUMGTriggerType.ManaAboveReleaseThreshold:
                    eval.ObservedValue = snapshot.ActorManaPct;
                    eval.Matched = eval.ObservedValue >= profile.ReleaseThreshold;
                    break;
                case AIGMUMGTriggerType.AmmunitionLow:
                    eval.ObservedValue = snapshot.AmmunitionCount;
                    eval.Matched = eval.ObservedValue <= profile.ActivationThreshold;
                    break;
                case AIGMUMGTriggerType.CurrentTargetLost:
                    eval.ObservedValue = Contains(snapshot.CurrentTarget, "lost") ? 1 : 0;
                    eval.Matched = eval.ObservedValue >= 1;
                    break;
                case AIGMUMGTriggerType.CommanderHold:
                    eval.ObservedValue = Contains(snapshot.ExplicitCommanderOrder, "hold") ? 1 : 0;
                    eval.Matched = eval.ObservedValue >= 1;
                    break;
                case AIGMUMGTriggerType.CommanderStop:
                    eval.ObservedValue = Contains(snapshot.ExplicitCommanderOrder, "stop") ? 1 : 0;
                    eval.Matched = eval.ObservedValue >= 1;
                    break;
                case AIGMUMGTriggerType.CommanderFollow:
                    eval.ObservedValue = Contains(snapshot.ExplicitCommanderOrder, "follow") ? 1 : 0;
                    eval.Matched = eval.ObservedValue >= 1;
                    break;
                case AIGMUMGTriggerType.PassiveStandDown:
                    eval.ObservedValue = Contains(snapshot.OperationalState, "PassiveStandDown") ? 1 : 0;
                    eval.Matched = eval.ObservedValue >= 1;
                    break;
                case AIGMUMGTriggerType.AbsoluteGMHold:
                    eval.ObservedValue = Contains(snapshot.OperationalState, "AbsoluteGMHold") ? 1 : 0;
                    eval.Matched = eval.ObservedValue >= 1;
                    break;
                case AIGMUMGTriggerType.MissionCancelled:
                    eval.ObservedValue = Contains(snapshot.OperationalState, "CancelMission") || Contains(snapshot.CurrentMission, "cancel") ? 1 : 0;
                    eval.Matched = eval.ObservedValue >= 1;
                    break;
                case AIGMUMGTriggerType.NoThreatPresent:
                    eval.ObservedValue = snapshot.NearbyHostiles.Count;
                    eval.Matched = snapshot.NearbyHostiles.Count == 0;
                    break;
                case AIGMUMGTriggerType.CooldownExpired:
                    eval.ObservedValue = 1;
                    eval.Matched = true;
                    break;
            }

            eval.Reason = eval.Matched ? "trigger matched" : "trigger unmatched";
            return eval;
        }

        private static AIGMUMGCapabilityGateReceipt ValidateCapability(Mobile actor, AIGMUMGSituationSnapshot snapshot, List<AIGMUMGResolvedReference> resolved)
        {
            AIGMUMGCapabilityGateReceipt receipt = new AIGMUMGCapabilityGateReceipt();
            AIGMCapabilitySnapshot capability = AIGMCapabilityRegistry.CreateSnapshot(actor);
            ApplySyntheticCapabilityRemoval(capability, snapshot);

            List<AIGMCapabilityRequirement> requirements = new List<AIGMCapabilityRequirement>();
            for (int i = 0; i < resolved.Count; i++)
                AddRequirements(requirements, resolved[i].CapabilityRequirements);

            if (requirements.Count == 0)
            {
                receipt.State = AIGMUMGCapabilityGateState.Unproven;
                receipt.Unproven.Add("no_explicit_capability_requirements");
                return receipt;
            }

            AIGMCapabilityValidationResult validation = AIGMCapabilityRegistry.Validate(capability, requirements);
            receipt.Passed.AddRange(validation.Passed);
            receipt.Failed.AddRange(validation.Failed);
            if (!validation.IsValid)
            {
                receipt.State = AIGMUMGCapabilityGateState.Blocked;
                return receipt;
            }

            if (Contains(capability.WaypointTravelStatus, "Degraded"))
                receipt.Degraded.Add("waypoint_travel_degraded");
            if (Contains(capability.AutonomousTaskStatus, "Registered"))
                receipt.Unproven.Add("autonomous_task_registered_not_live_dispatch");
            if (Contains(capability.OffensiveSpellcastingStatus, "Unsupported"))
                receipt.Unproven.Add("offensive_spellcasting_unsupported");

            receipt.State = receipt.Degraded.Count > 0 ? AIGMUMGCapabilityGateState.Degraded : AIGMUMGCapabilityGateState.Ready;
            return receipt;
        }

        private static AIGMUMGGovernanceGateReceipt ValidateGovernance(AIGMUMGSituationSnapshot snapshot, AIGMUMGDescentCandidateBranch candidate)
        {
            AIGMUMGGovernanceGateReceipt result = new AIGMUMGGovernanceGateReceipt();
            result.Reasons.Add("priority=stop>follow>hunt>travel");
            result.Reasons.Add("epoch=" + snapshot.CurrentCommandEpoch);
            result.Reasons.Add("movement_owner=" + snapshot.CurrentMovementOwner);

            if (Contains(snapshot.OperationalState, "AbsoluteGMHold"))
                Block(result, "AbsoluteGMHold_blocks_all_tactical_branches");
            else if (Contains(snapshot.ExplicitCommanderOrder, "stop") || Contains(snapshot.OperationalState, "CancelMission"))
                Block(result, "ExplicitStopOrCancelMission_blocks_tactical_output");
            else if (Contains(snapshot.OperationalState, "PassiveStandDown") && IsCombatOrPursuit(candidate))
                Block(result, "PassiveStandDown_blocks_combat_and_pursuit");
            else if (Contains(snapshot.ExplicitCommanderOrder, "hold") && !String.Equals(candidate.FamilyId, AIGMUMGOperationalLayoutService.FamilyPositioning, StringComparison.OrdinalIgnoreCase))
                Block(result, "CommanderHold_reduces_non_positioning_tactical_output");

            return result;
        }

        private static string ApplyEligibilityHysteresis(AIGMUMGDescentReceipt receipt, AIGMUMGDescentCandidateBranch candidate, List<AIGMUMGTriggerProfile> profiles, DateTime now)
        {
            AIGMUMGPreviewRuntimeBranchStateRecord record = AIGMUMGPreviewRuntimeStateStore.GetOrCreate(receipt.ActorSerial, receipt.ApprovedLayoutVersionId, candidate.FamilyId, candidate.StackId);
            AIGMUMGTriggerProfile governing = profiles.Count > 0 ? profiles[0] : new AIGMUMGTriggerProfile();

            if (record.CooldownUntilUtc != DateTime.MinValue && now < record.CooldownUntilUtc && record.CurrentState != AIGMUMGPreviewBranchState.ActivePreview)
            {
                candidate.State = AIGMUMGPreviewBranchState.CoolingDown;
                return "cooling_down_until=" + record.CooldownUntilUtc.ToString("o");
            }

            if (record.BecameEligibleUtc == DateTime.MinValue)
                record.BecameEligibleUtc = now;

            record.ReleaseCandidateUtc = DateTime.MinValue;
            candidate.State = AIGMUMGPreviewBranchState.Eligible;
            bool wasActive = record.CurrentState == AIGMUMGPreviewBranchState.ActivePreview;
            candidate.ComparisonFactors.Add(wasActive ? "hysteresis_existing_active=true" : "hysteresis_existing_active=false");
            candidate.ComparisonFactors.Add("trigger_severity=" + candidate.Severity);
            candidate.ComparisonFactors.Add("branch_priority=" + candidate.Priority);
            candidate.ComparisonFactors.Add("stack_order=" + candidate.StackOrder);
            return "eligible; minimumActive=" + governing.MinimumActiveDuration.TotalSeconds + "s; releaseStability=" + governing.ReleaseStabilityDuration.TotalSeconds + "s; cooldown=" + governing.CooldownDuration.TotalSeconds + "s";
        }

        private static string ApplyDormantHysteresis(AIGMUMGDescentReceipt receipt, AIGMUMGDescentCandidateBranch candidate, List<AIGMUMGTriggerProfile> profiles, DateTime now)
        {
            AIGMUMGPreviewRuntimeBranchStateRecord record = AIGMUMGPreviewRuntimeStateStore.GetOrCreate(receipt.ActorSerial, receipt.ApprovedLayoutVersionId, candidate.FamilyId, candidate.StackId);
            AIGMUMGTriggerProfile governing = profiles.Count > 0 ? profiles[0] : new AIGMUMGTriggerProfile();
            if (record.CurrentState == AIGMUMGPreviewBranchState.ActivePreview)
            {
                if (record.BecameActiveUtc != DateTime.MinValue && now - record.BecameActiveUtc < governing.MinimumActiveDuration)
                {
                    candidate.State = AIGMUMGPreviewBranchState.Eligible;
                    candidate.Severity = Math.Max(candidate.Severity, governing.Severity);
                    candidate.ComparisonFactors.Add("minimum_active_duration_holds");
                    return "minimum_active_duration_holds_until=" + record.BecameActiveUtc.Add(governing.MinimumActiveDuration).ToString("o");
                }

                string releaseThresholdReason;
                if (!ReleaseThresholdCrossed(candidate, profiles, out releaseThresholdReason))
                {
                    candidate.State = AIGMUMGPreviewBranchState.Eligible;
                    candidate.Severity = Math.Max(candidate.Severity, governing.Severity);
                    candidate.ComparisonFactors.Add("release_threshold_not_crossed");
                    return releaseThresholdReason;
                }

                if (record.ReleaseCandidateUtc == DateTime.MinValue)
                {
                    record.ReleaseCandidateUtc = now;
                    candidate.State = AIGMUMGPreviewBranchState.Eligible;
                    candidate.Severity = Math.Max(candidate.Severity, governing.Severity);
                    candidate.ComparisonFactors.Add("release_stability_started");
                    return "release_stability_started=" + now.ToString("o");
                }

                if (now - record.ReleaseCandidateUtc < governing.ReleaseStabilityDuration)
                {
                    candidate.State = AIGMUMGPreviewBranchState.Eligible;
                    candidate.Severity = Math.Max(candidate.Severity, governing.Severity);
                    candidate.ComparisonFactors.Add("release_stability_holds");
                    return "release_stability_holds_until=" + record.ReleaseCandidateUtc.Add(governing.ReleaseStabilityDuration).ToString("o");
                }

                record.CurrentState = AIGMUMGPreviewBranchState.CoolingDown;
                record.CooldownUntilUtc = now.Add(governing.CooldownDuration);
                candidate.State = AIGMUMGPreviewBranchState.CoolingDown;
                return "released_to_cooldown_until=" + record.CooldownUntilUtc.ToString("o");
            }

            if (record.CooldownUntilUtc != DateTime.MinValue && now < record.CooldownUntilUtc)
            {
                candidate.State = AIGMUMGPreviewBranchState.CoolingDown;
                return "cooling_down_until=" + record.CooldownUntilUtc.ToString("o");
            }

            return "dormant";
        }

        private static bool ReleaseThresholdCrossed(AIGMUMGDescentCandidateBranch candidate, List<AIGMUMGTriggerProfile> profiles, out string reason)
        {
            reason = "release_threshold_crossed";
            if (candidate == null || candidate.UnmatchedTriggers == null || candidate.UnmatchedTriggers.Count == 0)
                return true;

            for (int i = 0; i < candidate.UnmatchedTriggers.Count; i++)
            {
                AIGMUMGTriggerEvaluation eval = candidate.UnmatchedTriggers[i];
                AIGMUMGTriggerProfile profile = FindProfile(profiles, eval.TriggerId);
                double release = profile != null ? profile.ReleaseThreshold : eval.ReleaseThreshold;

                switch (eval.TriggerType)
                {
                    case AIGMUMGTriggerType.ActorHealthBelowThreshold:
                    case AIGMUMGTriggerType.AllyHealthBelowThreshold:
                    case AIGMUMGTriggerType.ManaBelowThreshold:
                        if (release > 0.0 && eval.ObservedValue < release)
                        {
                            reason = "release_threshold_not_crossed; observed=" + eval.ObservedValue.ToString("0.###") + "; release=" + release.ToString("0.###");
                            return false;
                        }
                        break;
                    case AIGMUMGTriggerType.HostileWithinRange:
                        if (release > 0.0 && eval.ObservedValue > 0.0 && eval.ObservedValue < release)
                        {
                            reason = "release_threshold_not_crossed; nearestHostileDistance=" + eval.ObservedValue.ToString("0.###") + "; releaseRange=" + release.ToString("0.###");
                            return false;
                        }
                        break;
                    case AIGMUMGTriggerType.HostileDetected:
                    case AIGMUMGTriggerType.HostileMageDetected:
                    case AIGMUMGTriggerType.ProtecteeThreatened:
                        if (eval.ObservedValue > release)
                        {
                            reason = "release_threshold_not_crossed; observed=" + eval.ObservedValue.ToString("0.###") + "; release=" + release.ToString("0.###");
                            return false;
                        }
                        break;
                }
            }

            return true;
        }

        private static AIGMUMGTriggerProfile FindProfile(List<AIGMUMGTriggerProfile> profiles, string triggerId)
        {
            if (profiles == null || String.IsNullOrWhiteSpace(triggerId))
                return null;

            for (int i = 0; i < profiles.Count; i++)
            {
                if (String.Equals(profiles[i].TriggerId, triggerId, StringComparison.OrdinalIgnoreCase))
                    return profiles[i];
            }

            return null;
        }

        private static void UpdateRuntimeRecord(AIGMUMGDescentReceipt receipt, AIGMUMGDescentCandidateBranch candidate, DateTime now)
        {
            AIGMUMGPreviewRuntimeBranchStateRecord record = AIGMUMGPreviewRuntimeStateStore.GetOrCreate(receipt.ActorSerial, receipt.ApprovedLayoutVersionId, candidate.FamilyId, candidate.StackId);
            if (candidate.State == AIGMUMGPreviewBranchState.ActivePreview)
            {
                if (record.CurrentState != AIGMUMGPreviewBranchState.ActivePreview || record.BecameActiveUtc == DateTime.MinValue)
                    record.BecameActiveUtc = now;
                record.BecameEligibleUtc = record.BecameEligibleUtc == DateTime.MinValue ? now : record.BecameEligibleUtc;
                if (!IsReleaseStabilityHysteresis(candidate.HysteresisState))
                    record.ReleaseCandidateUtc = DateTime.MinValue;
            }
            else if (candidate.State == AIGMUMGPreviewBranchState.Dormant)
            {
                record.BecameEligibleUtc = DateTime.MinValue;
                record.BecameActiveUtc = DateTime.MinValue;
                record.ReleaseCandidateUtc = DateTime.MinValue;
            }

            record.CurrentState = candidate.State;
            record.LastDecisionFingerprint = receipt.DecisionFingerprint;
        }

        private static bool IsReleaseStabilityHysteresis(string value)
        {
            return !String.IsNullOrWhiteSpace(value)
                && (value.StartsWith("release_stability_started", StringComparison.OrdinalIgnoreCase)
                    || value.StartsWith("release_stability_holds", StringComparison.OrdinalIgnoreCase));
        }

        private static void FinalizeAndRecord(AIGMUMGDescentReceipt receipt, string snapshotFingerprint, string runtimeFingerprint)
        {
            if (receipt.CognitionBudget == null)
                receipt.CognitionBudget = new AIGMUMGCognitionBudgetReceipt();

            if (receipt.AdapterMapping != null && receipt.AdapterMapping.InvocationAttempted)
                receipt.Errors.Add("ADAPTER_INVOCATION_ATTEMPTED_FORBIDDEN");

            receipt.CognitionBudget.AdapterInvocationAttempts = 0;
            receipt.CognitionBudget.AdapterInvocations = 0;
            receipt.FinalResult = FinalPreviewResult;
            receipt.DecisionFingerprint = BuildDecisionFingerprint(receipt, snapshotFingerprint, runtimeFingerprint);
            for (int i = 0; i < receipt.CandidateBranches.Count; i++)
            {
                AIGMUMGPreviewRuntimeBranchStateRecord record = AIGMUMGPreviewRuntimeStateStore.GetOrCreate(receipt.ActorSerial, receipt.ApprovedLayoutVersionId, receipt.CandidateBranches[i].FamilyId, receipt.CandidateBranches[i].StackId);
                record.LastDecisionFingerprint = receipt.DecisionFingerprint;
                record.LastSnapshotFingerprint = snapshotFingerprint;
            }
            AIGMUMGDescentTraceService.Record(receipt);
        }

        private static AIGMUMGDescentReceipt CreateBaseReceipt(Mobile actor, AIGMUMGSituationSnapshot snapshot, DateTime now)
        {
            AIGMUMGDescentReceipt receipt = new AIGMUMGDescentReceipt();
            receipt.CapturedUtc = now;
            receipt.CorrelationId = snapshot != null ? snapshot.CorrelationId : NewCorrelationId();
            receipt.ActorName = actor != null ? (actor.Name ?? actor.GetType().Name) : String.Empty;
            receipt.ActorSerial = actor != null ? FormatSerial(actor) : String.Empty;
            receipt.ActorKey = actor != null ? AIGMUMGOperationalLayoutService.ResolveOperationalActorKey(actor) : String.Empty;
            receipt.SnapshotId = snapshot != null ? snapshot.SnapshotId : String.Empty;
            receipt.SnapshotSource = snapshot != null ? snapshot.Source : AIGMUMGSituationSnapshotSource.CurrentWorld;
            receipt.SnapshotScenarioId = snapshot != null ? snapshot.ScenarioId : String.Empty;
            receipt.CognitionBudget.MaxFamilies = MaxFamilies;
            receipt.CognitionBudget.MaxStacks = MaxStacks;
            receipt.CognitionBudget.MaxReferences = MaxReferences;
            receipt.CognitionBudget.MaxTriggerProfiles = MaxTriggerProfiles;
            receipt.Warnings.Add("ACTIVE PREVIEW - NOT DISPATCHED");
            receipt.Warnings.Add("TacticalDispatchEnabled=false");
            if (AIGMUMGPreviewClock.IsProofClock)
                receipt.Warnings.Add("controlled_proof_clock");
            return receipt;
        }

        private static void CountInstalled(AIGMUMGOperationalLayout layout, AIGMUMGCognitionBudgetReceipt budget)
        {
            if (layout == null || layout.Families == null || budget == null)
                return;

            budget.InstalledFamilies = layout.Families.Count;
            for (int f = 0; f < layout.Families.Count; f++)
            {
                AIGMUMGOperationalFamily family = layout.Families[f];
                if (family == null || family.OperationalNeoStacks == null)
                    continue;
                budget.InstalledStacks += family.OperationalNeoStacks.Count;
                for (int s = 0; s < family.OperationalNeoStacks.Count; s++)
                {
                    AIGMUMGOperationalNeoStack stack = family.OperationalNeoStacks[s];
                    if (stack == null || stack.NeoBlockReferences == null)
                        continue;
                    budget.InstalledReferences += stack.NeoBlockReferences.Count;
                    for (int r = 0; r < stack.NeoBlockReferences.Count; r++)
                    {
                        AIGMUMGNeoBlockReference reference = stack.NeoBlockReferences[r];
                        if (reference != null && reference.Enabled && stack.Enabled && family.Enabled)
                            budget.EnabledReferences++;
                    }
                }
            }
        }

        private static void AddReferenceSummary(AIGMUMGDescentCandidateBranch candidate, AIGMUMGOperationalNeoStack stack, List<AIGMUMGResolvedReference> resolved, List<string> invalidDefinitions)
        {
            if (stack != null && stack.NeoBlockReferences != null)
            {
                for (int i = 0; i < stack.NeoBlockReferences.Count; i++)
                {
                    AIGMUMGNeoBlockReference reference = stack.NeoBlockReferences[i];
                    if (reference == null)
                        continue;
                    candidate.ReferenceIds.Add(reference.ReferenceId);
                }
            }

            for (int i = 0; i < resolved.Count; i++)
            {
                if (!String.IsNullOrWhiteSpace(resolved[i].DefinitionId))
                    candidate.DefinitionIds.Add(resolved[i].DefinitionId);
                candidate.Priority = Math.Min(candidate.Priority, resolved[i].PriorityOrder);
            }

            if (invalidDefinitions != null && invalidDefinitions.Count > 0)
                candidate.ComparisonFactors.Add("invalidDefinitions=" + String.Join(",", invalidDefinitions.ToArray()));
        }

        private static void AddRequirements(List<AIGMCapabilityRequirement> target, List<AIGMCapabilityRequirement> source)
        {
            if (target == null || source == null)
                return;
            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] != null)
                    target.Add(source[i]);
            }
        }

        private static void ApplySyntheticCapabilityRemoval(AIGMCapabilitySnapshot capability, AIGMUMGSituationSnapshot snapshot)
        {
            if (capability == null || snapshot == null || snapshot.Source != AIGMUMGSituationSnapshotSource.SyntheticProof || snapshot.RemovedCapabilities == null)
                return;

            for (int i = capability.Capabilities.Count - 1; i >= 0; i--)
            {
                for (int r = 0; r < snapshot.RemovedCapabilities.Count; r++)
                {
                    if (String.Equals(capability.Capabilities[i].ToString(), snapshot.RemovedCapabilities[r], StringComparison.OrdinalIgnoreCase))
                    {
                        capability.Capabilities.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        private static string BuildSnapshotFingerprint(AIGMUMGSituationSnapshot snapshot)
        {
            if (snapshot == null)
                return String.Empty;

            List<string> parts = new List<string>();
            parts.Add(snapshot.Source.ToString());
            parts.Add(snapshot.ScenarioId);
            parts.Add(snapshot.ActorSerial);
            parts.Add(snapshot.ActorKey);
            parts.Add(snapshot.Map);
            parts.Add(snapshot.Location);
            parts.Add(snapshot.OperationalState);
            parts.Add(snapshot.CurrentCommandEpoch.ToString());
            parts.Add(snapshot.CurrentMovementOwner);
            parts.Add(snapshot.CurrentMission);
            parts.Add(snapshot.ActorHits + "/" + snapshot.ActorHitsMax);
            parts.Add(snapshot.ActorMana + "/" + snapshot.ActorManaMax);
            parts.Add(snapshot.ActorStamina + "/" + snapshot.ActorStaminaMax);
            parts.Add(snapshot.CurrentProtectee);
            parts.Add(snapshot.CommanderSerial);
            parts.Add(snapshot.ExplicitCommanderOrder);
            parts.Add(snapshot.CurrentTarget);
            parts.Add("inventory=" + snapshot.InventoryCapabilitySummary);
            AddEntities(parts, "hostile", snapshot.NearbyHostiles);
            AddEntities(parts, "ally", snapshot.NearbyAllies);
            AddStrings(parts, "targetCaps", snapshot.TargetCapabilities);
            AddStrings(parts, "cooldowns", snapshot.ActiveCooldowns);
            AddStrings(parts, "removedCaps", snapshot.RemovedCapabilities);
            return AIGMUMGStableHash.Compute(parts.ToArray());
        }

        private static string BuildDecisionFingerprint(AIGMUMGDescentReceipt receipt, string snapshotFingerprint, string runtimeFingerprint)
        {
            List<string> parts = new List<string>();
            parts.Add(receipt.ActorSerial);
            parts.Add(receipt.ActorKey);
            parts.Add(receipt.ApprovedLayoutVersionId);
            parts.Add(receipt.LayoutRevision.ToString());
            parts.Add(receipt.GraphFingerprint);
            parts.Add(receipt.SnapshotScenarioId);
            parts.Add(snapshotFingerprint ?? String.Empty);
            parts.Add(runtimeFingerprint ?? String.Empty);
            parts.Add(receipt.FinalResult);
            for (int i = 0; i < receipt.SelectedBranchByFamily.Count; i++)
            {
                AIGMUMGFamilySelection selected = receipt.SelectedBranchByFamily[i];
                parts.Add("selected|" + selected.FamilyId + "|" + selected.SelectedStackId + "|" + selected.State);
            }
            if (receipt.TypedIntent != null)
                parts.Add("intent|" + receipt.TypedIntent.Category + "|" + receipt.TypedIntent.PrimaryFamilyId + "|" + receipt.TypedIntent.SourceStackId);
            if (receipt.AdapterMapping != null)
                parts.Add("adapter|" + receipt.AdapterMapping.AdapterId + "|" + receipt.AdapterMapping.AdapterName + "|" + receipt.AdapterMapping.InvocationAllowed + "|" + receipt.AdapterMapping.InvocationAttempted);
            return AIGMUMGStableHash.Compute(parts.ToArray());
        }

        private static string BuildDecisionCacheKey(string actorSerial, string layoutVersionId, string graphFingerprint, string snapshotFingerprint, string runtimeFingerprint, DateTime now)
        {
            long clockBucket = now.Ticks / TimeSpan.TicksPerSecond;
            return AIGMUMGStableHash.Compute(actorSerial, layoutVersionId, graphFingerprint, snapshotFingerprint, runtimeFingerprint, clockBucket.ToString());
        }

        private static bool TryGetCached(string cacheKey, out AIGMUMGDescentReceipt receipt)
        {
            lock (CacheSyncRoot)
                return DecisionCache.TryGetValue(cacheKey, out receipt);
        }

        private static void StoreCached(string cacheKey, AIGMUMGDescentReceipt receipt)
        {
            lock (CacheSyncRoot)
            {
                if (DecisionCache.Count > 128)
                    DecisionCache.Clear();
                DecisionCache[cacheKey] = AIGMUMGRepository.RoundTripClone(receipt);
            }
        }

        private static void AddEntities(List<string> parts, string prefix, List<AIGMUMGSituationEntity> entities)
        {
            if (entities == null)
                return;
            List<string> lines = new List<string>();
            for (int i = 0; i < entities.Count; i++)
            {
                AIGMUMGSituationEntity e = entities[i];
                lines.Add(prefix + "|" + e.Serial + "|" + e.Name + "|" + e.DistanceTiles + "|" + e.Hits + "/" + e.HitsMax + "|mage=" + e.IsMage + "|hostile=" + e.IsHostile + "|protectee=" + e.IsProtectee);
            }
            lines.Sort(StringComparer.OrdinalIgnoreCase);
            parts.AddRange(lines);
        }

        private static void AddStrings(List<string> parts, string prefix, List<string> values)
        {
            if (values == null)
                return;
            List<string> copy = new List<string>(values);
            copy.Sort(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < copy.Count; i++)
                parts.Add(prefix + "|" + copy[i]);
        }

        private static int CompareCandidatesForSelection(AIGMUMGDescentCandidateBranch left, AIGMUMGDescentCandidateBranch right)
        {
            int bySeverity = right.Severity.CompareTo(left.Severity);
            if (bySeverity != 0)
                return bySeverity;

            bool leftWasActive = HasFactor(left, "hysteresis_existing_active=true");
            bool rightWasActive = HasFactor(right, "hysteresis_existing_active=true");
            if (leftWasActive != rightWasActive)
                return leftWasActive ? -1 : 1;

            int byPriority = left.Priority.CompareTo(right.Priority);
            if (byPriority != 0)
                return byPriority;

            int byStackOrder = left.StackOrder.CompareTo(right.StackOrder);
            if (byStackOrder != 0)
                return byStackOrder;

            int byStackId = String.Compare(left.StackId, right.StackId, StringComparison.OrdinalIgnoreCase);
            if (byStackId != 0)
                return byStackId;

            string leftDef = left.DefinitionIds.Count > 0 ? left.DefinitionIds[0] : String.Empty;
            string rightDef = right.DefinitionIds.Count > 0 ? right.DefinitionIds[0] : String.Empty;
            return String.Compare(leftDef, rightDef, StringComparison.OrdinalIgnoreCase);
        }

        private static int CompareFamilies(AIGMUMGOperationalFamily left, AIGMUMGOperationalFamily right)
        {
            int byOrder = SafeOrder(left != null ? left.Order : 0).CompareTo(SafeOrder(right != null ? right.Order : 0));
            if (byOrder != 0)
                return byOrder;
            return String.Compare(left != null ? left.FamilyId : String.Empty, right != null ? right.FamilyId : String.Empty, StringComparison.OrdinalIgnoreCase);
        }

        private static int CompareStacks(AIGMUMGOperationalNeoStack left, AIGMUMGOperationalNeoStack right)
        {
            int byOrder = SafeOrder(left != null ? left.Order : 0).CompareTo(SafeOrder(right != null ? right.Order : 0));
            if (byOrder != 0)
                return byOrder;
            return String.Compare(left != null ? left.StackId : String.Empty, right != null ? right.StackId : String.Empty, StringComparison.OrdinalIgnoreCase);
        }

        private static int SafeOrder(int value)
        {
            return value <= 0 ? 500 : value;
        }

        private static bool HasFactor(AIGMUMGDescentCandidateBranch branch, string factor)
        {
            if (branch == null || branch.ComparisonFactors == null)
                return false;
            for (int i = 0; i < branch.ComparisonFactors.Count; i++)
            {
                if (String.Equals(branch.ComparisonFactors[i], factor, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static AIGMUMGDescentCandidateBranch FindSelected(AIGMUMGDescentReceipt receipt, string familyId)
        {
            for (int i = 0; i < receipt.CandidateBranches.Count; i++)
            {
                AIGMUMGDescentCandidateBranch branch = receipt.CandidateBranches[i];
                if (branch.State == AIGMUMGPreviewBranchState.ActivePreview && String.Equals(branch.FamilyId, familyId, StringComparison.OrdinalIgnoreCase))
                    return branch;
            }
            return null;
        }

        private static void AddSupportingFamily(AIGMUMGTypedIntentProposal proposal, AIGMUMGDescentReceipt receipt, string familyId)
        {
            AIGMUMGDescentCandidateBranch selected = FindSelected(receipt, familyId);
            if (selected != null)
                proposal.SupportingFamilies.Add(familyId + ":" + selected.StackId);
        }

        private static int CountMageHostiles(AIGMUMGSituationSnapshot snapshot)
        {
            int count = 0;
            for (int i = 0; i < snapshot.NearbyHostiles.Count; i++)
            {
                if (snapshot.NearbyHostiles[i].IsMage)
                    count++;
            }
            return count;
        }

        private static double NearestHostileDistance(AIGMUMGSituationSnapshot snapshot)
        {
            if (snapshot.NearbyHostiles.Count == 0)
                return 999.0;
            int nearest = snapshot.NearbyHostiles[0].DistanceTiles;
            for (int i = 1; i < snapshot.NearbyHostiles.Count; i++)
                nearest = Math.Min(nearest, snapshot.NearbyHostiles[i].DistanceTiles);
            return nearest;
        }

        private static double LowestAllyHealthPct(AIGMUMGSituationSnapshot snapshot)
        {
            if (snapshot.NearbyAllies.Count == 0)
                return 100.0;
            double lowest = 100.0;
            for (int i = 0; i < snapshot.NearbyAllies.Count; i++)
                lowest = Math.Min(lowest, snapshot.NearbyAllies[i].HealthPct);
            return lowest;
        }

        private static bool IsCombatOrPursuit(AIGMUMGDescentCandidateBranch candidate)
        {
            return candidate != null
                && (String.Equals(candidate.FamilyId, AIGMUMGOperationalLayoutService.FamilyCombat, StringComparison.OrdinalIgnoreCase)
                    || Contains(candidate.StackName, "hunt")
                    || Contains(candidate.StackName, "pursuit")
                    || Contains(candidate.StackName, "attack"));
        }

        private static bool HasHardGovernance(AIGMUMGSituationSnapshot snapshot)
        {
            return snapshot != null
                && (Contains(snapshot.OperationalState, "AbsoluteGMHold")
                    || Contains(snapshot.ExplicitCommanderOrder, "stop")
                    || Contains(snapshot.OperationalState, "CancelMission")
                    || Contains(snapshot.ExplicitCommanderOrder, "hold")
                    || Contains(snapshot.OperationalState, "PassiveStandDown"));
        }

        private static void Block(AIGMUMGGovernanceGateReceipt result, string reason)
        {
            result.Allowed = false;
            result.Result = "blocked";
            result.Reasons.Add(reason);
        }

        private static string GetLocalParameter(AIGMUMGNeoBlockReference reference, string parameterId)
        {
            if (reference == null || reference.OptionalLocalParameters == null)
                return String.Empty;
            for (int i = 0; i < reference.OptionalLocalParameters.Count; i++)
            {
                AIGMUMGParameterValue value = reference.OptionalLocalParameters[i];
                if (value != null && String.Equals(value.ParameterId, parameterId, StringComparison.OrdinalIgnoreCase))
                    return value.Value;
            }
            return String.Empty;
        }

        private static double GetDouble(AIGMUMGNeoBlockReference reference, string parameterId, double fallback)
        {
            double parsed;
            return Double.TryParse(GetLocalParameter(reference, parameterId), out parsed) ? parsed : fallback;
        }

        private static int GetInt(AIGMUMGNeoBlockReference reference, string parameterId, int fallback)
        {
            int parsed;
            return Int32.TryParse(GetLocalParameter(reference, parameterId), out parsed) ? parsed : fallback;
        }

        private static bool Contains(string value, string needle)
        {
            return (value ?? String.Empty).IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string NewCorrelationId()
        {
            return "phase64d1e-" + Guid.NewGuid().ToString("N").Substring(0, 12);
        }

        private static string FormatSerial(Mobile actor)
        {
            return actor != null ? String.Format("0x{0:X8}", actor.Serial.Value) : String.Empty;
        }

        private static string Describe(Mobile actor)
        {
            return actor == null ? "none" : (String.IsNullOrWhiteSpace(actor.Name) ? actor.GetType().Name : actor.Name) + "[" + FormatSerial(actor) + "]";
        }

        private static string Short(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return "none";
            return value.Length <= 12 ? value : value.Substring(0, 12);
        }
    }
}
