using System;
using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Custom.AIGM.UMG
{
    public static class AIGMUMGCompiler
    {
        public static AIGMUMGDecisionTrace Compile(Mobile actor, AIGMUMGSleeve sleeve, bool dryRun)
        {
            AIGMCapabilitySnapshot snapshot = AIGMCapabilityRegistry.CreateSnapshot(actor);
            Dictionary<string, string> facts = AIGMUMGTriggerLanguage.BuildFacts(snapshot);
            AIGMUMGDecisionTrace trace = new AIGMUMGDecisionTrace
            {
                ActorName = snapshot.ActorName,
                ActorSerial = snapshot.Serial,
                SleeveId = sleeve != null ? sleeve.SleeveId : String.Empty,
                SleeveVersion = sleeve != null ? sleeve.Version : 0,
                Result = dryRun ? "dry_run" : "compiled"
            };

            AppendFacts(trace, snapshot, facts);

            if (sleeve == null)
            {
                trace.Result = "missing_sleeve";
                return trace;
            }

            List<AIGMUMGBlock> orderedBlocks = FlattenOrderedBlocks(sleeve, trace);
            List<AIGMUMGConflict> conflicts = DetectConflicts(orderedBlocks);
            for (int i = 0; i < conflicts.Count; i++)
                trace.ConflictingBlocks.Add(conflicts[i].LeftBlockId + "<->" + conflicts[i].RightBlockId + ":" + conflicts[i].Reason);

            AIGMUMGDecision selected = null;
            for (int i = 0; i < orderedBlocks.Count; i++)
            {
                AIGMUMGBlock block = orderedBlocks[i];
                if (IsPolicyOnlyBlock(block))
                {
                    trace.RejectedActions.Add(block.BlockId + ":policy_guard_not_action");
                    continue;
                }

                string triggerReason;
                if (!AIGMUMGTriggerLanguage.Evaluate(block.ActivationRule, facts, out triggerReason))
                {
                    trace.RejectedActions.Add(block.BlockId + ":trigger_false:" + triggerReason);
                    continue;
                }

                AIGMUMGDecision candidate = BuildIntent(actor, block, snapshot);
                candidate.Authorization = dryRun ? "dry_run" : "compiled_authorized";
                trace.CandidateActions.Add(block.BlockId + ":" + candidate.IntentType + ":priority=" + candidate.Priority);

                AIGMCapabilityValidationResult validation = AIGMCapabilityRegistry.Validate(snapshot, block.CapabilityRequirements);
                if (!validation.IsValid)
                {
                    string failure = block.BlockId + ":" + validation.BuildSummary();
                    trace.CapabilityFailures.Add(failure);
                    trace.RejectedActions.Add(block.BlockId + ":invalid_capability");
                    continue;
                }

                if (HasConflict(block, conflicts))
                {
                    trace.RejectedActions.Add(block.BlockId + ":conflicted");
                    continue;
                }

                selected = candidate;
                break;
            }

            if (selected == null)
                selected = BuildDefaultReport(actor, snapshot);

            selected.CorrelationId = trace.CorrelationId;
            trace.SelectedDecision = selected;
            return trace;
        }

        public static List<AIGMUMGBlock> FlattenOrderedBlocks(AIGMUMGSleeve sleeve)
        {
            return FlattenOrderedBlocks(sleeve, null);
        }

        private static List<AIGMUMGBlock> FlattenOrderedBlocks(AIGMUMGSleeve sleeve, AIGMUMGDecisionTrace trace)
        {
            List<AIGMUMGBlock> blocks = new List<AIGMUMGBlock>();
            if (sleeve == null || sleeve.NeoStacks == null)
                return blocks;

            List<AIGMUMGNeoStack> stacks = new List<AIGMUMGNeoStack>(sleeve.NeoStacks);
            stacks.Sort(CompareNeoStacks);

            for (int i = 0; i < stacks.Count; i++)
            {
                AIGMUMGNeoStack stack = stacks[i];
                if (stack == null || !stack.Enabled)
                    continue;

                if (trace != null)
                    trace.ActiveNeoStacks.Add(stack.StackKind + ":" + stack.Name);

                List<AIGMUMGNeoBlock> neoBlocks = new List<AIGMUMGNeoBlock>(stack.NeoBlocks);
                neoBlocks.Sort(delegate (AIGMUMGNeoBlock left, AIGMUMGNeoBlock right) { return right.PriorityOrder.CompareTo(left.PriorityOrder); });
                for (int n = 0; n < neoBlocks.Count; n++)
                {
                    if (neoBlocks[n] == null || !neoBlocks[n].Enabled)
                        continue;

                    List<AIGMUMGBlockStack> blockStacks = new List<AIGMUMGBlockStack>(neoBlocks[n].BlockStacks);
                    blockStacks.Sort(delegate (AIGMUMGBlockStack left, AIGMUMGBlockStack right) { return right.PriorityOrder.CompareTo(left.PriorityOrder); });
                    for (int b = 0; b < blockStacks.Count; b++)
                    {
                        if (blockStacks[b] == null || !blockStacks[b].Enabled)
                            continue;

                        for (int m = 0; m < blockStacks[b].MoltBlocks.Count; m++)
                        {
                            AIGMUMGBlock block = blockStacks[b].MoltBlocks[m];
                            if (block != null && block.IsActiveNow())
                                blocks.Add(block);
                        }
                    }
                }
            }

            blocks.Sort(CompareBlocks);
            return blocks;
        }

        public static List<AIGMUMGConflict> DetectConflicts(List<AIGMUMGBlock> orderedBlocks)
        {
            List<AIGMUMGConflict> conflicts = new List<AIGMUMGConflict>();
            if (orderedBlocks == null)
                return conflicts;

            for (int i = 0; i < orderedBlocks.Count; i++)
            {
                AIGMUMGBlock left = orderedBlocks[i];
                if (left == null || left.Conflicts == null)
                    continue;

                for (int j = i + 1; j < orderedBlocks.Count; j++)
                {
                    AIGMUMGBlock right = orderedBlocks[j];
                    if (right == null)
                        continue;

                    if (left.Conflicts.Contains(right.BlockId) || right.Conflicts.Contains(left.BlockId))
                    {
                        conflicts.Add(new AIGMUMGConflict
                        {
                            ConflictId = "conflict:" + left.BlockId + ":" + right.BlockId,
                            LeftBlockId = left.BlockId,
                            RightBlockId = right.BlockId,
                            Reason = "declared_block_conflict",
                            Resolution = left.PriorityOrder >= right.PriorityOrder ? left.BlockId + " outranks" : right.BlockId + " outranks"
                        });
                    }
                }
            }

            return conflicts;
        }

        private static AIGMUMGDecision BuildIntent(Mobile actor, AIGMUMGBlock block, AIGMCapabilitySnapshot snapshot)
        {
            AIGMUMGDecision decision = new AIGMUMGDecision
            {
                ActorName = snapshot.ActorName,
                ActorSerial = snapshot.Serial,
                Priority = block.PriorityOrder,
                SelectedFallback = block.FallbackBlockOrAction,
                DeterministicAdapter = ResolveAdapter(block)
            };
            decision.OriginatingBlockIds.Add(block.BlockId);
            decision.CapabilityRequirements.AddRange(block.CapabilityRequirements);
            decision.IntentType = InferIntent(block);
            decision.Target = InferTarget(block);
            decision.Destination = InferDestination(block);
            decision.Result = AIGMUMGPhase64C2Invariant.ExecutionStatus;
            decision.ExecutionReceipt = "typed_intent_compiled_only:" + AIGMUMGPhase64C2Invariant.ExecutionStatus;
            return decision;
        }

        private static AIGMUMGDecision BuildDefaultReport(Mobile actor, AIGMCapabilitySnapshot snapshot)
        {
            AIGMUMGDecision decision = new AIGMUMGDecision
            {
                ActorName = snapshot.ActorName,
                ActorSerial = snapshot.Serial,
                IntentType = AIGMUMGIntentType.Report,
                Priority = 0,
                DeterministicAdapter = "AIGMUMGReportAdapter",
                ExecutionReceipt = "no_active_valid_behavior_block:" + AIGMUMGPhase64C2Invariant.ExecutionStatus,
                Result = AIGMUMGPhase64C2Invariant.ExecutionStatus
            };
            return decision;
        }

        private static AIGMUMGIntentType InferIntent(AIGMUMGBlock block)
        {
            string text = ((block.Name ?? String.Empty) + " " + (block.Category ?? String.Empty) + " " + (block.Summary ?? String.Empty) + " " + (block.Content ?? String.Empty)).ToLowerInvariant();

            if (ContainsAny(text, "absolute hold", "hold ground", "hold"))
                return AIGMUMGIntentType.Hold;
            if (ContainsAny(text, "stand down", "passive"))
                return AIGMUMGIntentType.StandDown;
            if (ContainsAny(text, "resume"))
                return AIGMUMGIntentType.Resume;
            if (ContainsAny(text, "retreat", "regroup"))
                return text.Contains("retreat") ? AIGMUMGIntentType.Retreat : AIGMUMGIntentType.Regroup;
            if (ContainsAny(text, "heal", "greater heal"))
                return text.Contains("cast") || text.Contains("spell") ? AIGMUMGIntentType.CastSpell : AIGMUMGIntentType.Heal;
            if (ContainsAny(text, "cure", "poison"))
                return text.Contains("spell") ? AIGMUMGIntentType.CastSpell : AIGMUMGIntentType.Cure;
            if (ContainsAny(text, "bandage"))
                return AIGMUMGIntentType.Bandage;
            if (ContainsAny(text, "potion"))
                return AIGMUMGIntentType.UsePotion;
            if (ContainsAny(text, "track"))
                return AIGMUMGIntentType.Track;
            if (ContainsAny(text, "hunt"))
                return AIGMUMGIntentType.Hunt;
            if (ContainsAny(text, "guard", "protect", "defend"))
                return AIGMUMGIntentType.Guard;
            if (ContainsAny(text, "engage", "attack", "combat", "frontline", "berserker"))
                return AIGMUMGIntentType.Engage;
            if (ContainsAny(text, "follow"))
                return AIGMUMGIntentType.Follow;
            if (ContainsAny(text, "travel"))
                return AIGMUMGIntentType.Travel;
            if (ContainsAny(text, "patrol"))
                return AIGMUMGIntentType.Patrol;
            if (ContainsAny(text, "return home"))
                return AIGMUMGIntentType.ReturnHome;
            if (ContainsAny(text, "loot"))
                return AIGMUMGIntentType.Loot;
            if (ContainsAny(text, "alert"))
                return AIGMUMGIntentType.AlertSquad;
            if (ContainsAny(text, "hide", "stealth"))
                return AIGMUMGIntentType.Hide;
            if (ContainsAny(text, "reveal", "detect hidden"))
                return AIGMUMGIntentType.Reveal;
            if (ContainsAny(text, "stay"))
                return AIGMUMGIntentType.Stay;

            return AIGMUMGIntentType.Report;
        }

        private static string ResolveAdapter(AIGMUMGBlock block)
        {
            AIGMUMGIntentType intent = InferIntent(block);
            switch (intent)
            {
                case AIGMUMGIntentType.StandDown:
                case AIGMUMGIntentType.Hold:
                case AIGMUMGIntentType.Resume:
                    return "AIGMUMGOperationalControlAdapter";
                case AIGMUMGIntentType.Engage:
                case AIGMUMGIntentType.Guard:
                    return "AIGMUMGCombatAdapter/AIGMNativeCombatBridge";
                case AIGMUMGIntentType.Follow:
                case AIGMUMGIntentType.Stay:
                case AIGMUMGIntentType.Regroup:
                case AIGMUMGIntentType.Retreat:
                case AIGMUMGIntentType.Travel:
                case AIGMUMGIntentType.Patrol:
                case AIGMUMGIntentType.ReturnHome:
                    return "AIGMUMGMovementAdapter/AIGMSmartMovementService/AIGMRosterTaskService";
                case AIGMUMGIntentType.Track:
                case AIGMUMGIntentType.Hunt:
                    return "AIGMUMGTrackingAdapter/AIGMTrackingHuntService/AIGMRosterTaskService";
                case AIGMUMGIntentType.Heal:
                case AIGMUMGIntentType.Cure:
                case AIGMUMGIntentType.Bandage:
                    return "AIGMUMGHealingAdapter/AIGMCompanionHealingService";
                case AIGMUMGIntentType.CastSpell:
                    return "AIGMUMGSpellAdapter/AIGMCompanionSpellService";
                case AIGMUMGIntentType.UsePotion:
                    return "AIGMUMGPotionAdapter/AIGMCompanionPotionService";
                case AIGMUMGIntentType.Loot:
                    return "AIGMUMGLootAdapter/AIGMCompanionLootService";
                case AIGMUMGIntentType.AlertSquad:
                    return "AIGMUMGSquadAdapter/AIGMRosterTaskService";
                default:
                    return "AIGMUMGReportAdapter";
            }
        }

        private static string InferTarget(AIGMUMGBlock block)
        {
            string text = ((block.Name ?? String.Empty) + " " + (block.Summary ?? String.Empty) + " " + (block.Content ?? String.Empty)).ToLowerInvariant();
            if (text.Contains("protected ally"))
                return "protected_ally";
            if (text.Contains("owner"))
                return "owner";
            if (text.Contains("joining"))
                return "joining";
            if (text.Contains("dark brotherhood"))
                return "dark_brotherhood";
            if (text.Contains("caster"))
                return "enemy_caster";
            if (text.Contains("danyal"))
                return "danyal";
            if (text.Contains("miriel"))
                return "miriel";
            return String.Empty;
        }

        private static string InferDestination(AIGMUMGBlock block)
        {
            string text = ((block.Name ?? String.Empty) + " " + (block.Summary ?? String.Empty) + " " + (block.Content ?? String.Empty)).ToLowerInvariant();
            if (text.Contains("home"))
                return "home";
            if (text.Contains("waypoint"))
                return "assigned_waypoint";
            if (text.Contains("town"))
                return "current_town_region";
            return String.Empty;
        }

        private static bool HasConflict(AIGMUMGBlock block, List<AIGMUMGConflict> conflicts)
        {
            if (block == null || conflicts == null)
                return false;

            for (int i = 0; i < conflicts.Count; i++)
            {
                if (String.Equals(conflicts[i].LeftBlockId, block.BlockId, StringComparison.OrdinalIgnoreCase)
                    || String.Equals(conflicts[i].RightBlockId, block.BlockId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static int CompareNeoStacks(AIGMUMGNeoStack left, AIGMUMGNeoStack right)
        {
            int leftPriority = RuntimePriority(left != null ? left.StackKind : AIGMUMGNeoStackKind.Identity);
            int rightPriority = RuntimePriority(right != null ? right.StackKind : AIGMUMGNeoStackKind.Identity);
            int stackCompare = leftPriority.CompareTo(rightPriority);
            if (stackCompare != 0)
                return stackCompare;

            int leftOrder = left != null ? left.PriorityOrder : 0;
            int rightOrder = right != null ? right.PriorityOrder : 0;
            return rightOrder.CompareTo(leftOrder);
        }

        private static int CompareBlocks(AIGMUMGBlock left, AIGMUMGBlock right)
        {
            int sourceCompare = RuntimeBlockPriority(left).CompareTo(RuntimeBlockPriority(right));
            if (sourceCompare != 0)
                return sourceCompare;

            int leftOrder = left != null ? left.PriorityOrder : 0;
            int rightOrder = right != null ? right.PriorityOrder : 0;
            return rightOrder.CompareTo(leftOrder);
        }

        private static bool IsPolicyOnlyBlock(AIGMUMGBlock block)
        {
            if (block == null)
                return true;

            if (block.Source == AIGMUMGBlockSource.SystemInvariant)
                return true;

            return block.Category.IndexOf("Identity", StringComparison.OrdinalIgnoreCase) >= 0
                && block.MoltType != AIGMUMGMoltType.Blueprint;
        }

        private static int RuntimeBlockPriority(AIGMUMGBlock block)
        {
            if (block == null)
                return 99;

            if (block.Source == AIGMUMGBlockSource.SystemInvariant)
                return 1;
            if (block.Name.IndexOf("Absolute GM Hold", StringComparison.OrdinalIgnoreCase) >= 0)
                return 2;
            if (block.Name.IndexOf("Passive Stand Down", StringComparison.OrdinalIgnoreCase) >= 0)
                return 3;
            if (block.Scope == AIGMUMGBlockScope.Owner)
                return 4;
            if (block.MoltType == AIGMUMGMoltType.Primary && block.Category.IndexOf("Governance", StringComparison.OrdinalIgnoreCase) >= 0)
                return 5;
            if (block.Category.IndexOf("Emergency", StringComparison.OrdinalIgnoreCase) >= 0 || block.Category.IndexOf("Survival", StringComparison.OrdinalIgnoreCase) >= 0)
                return 6;
            if (block.Scope == AIGMUMGBlockScope.Scenario)
                return 7;
            if (block.Category.IndexOf("Mission", StringComparison.OrdinalIgnoreCase) >= 0)
                return 8;
            if (block.Scope == AIGMUMGBlockScope.Squad)
                return 9;
            if (block.Category.IndexOf("Doctrine", StringComparison.OrdinalIgnoreCase) >= 0 || block.Category.IndexOf("Policy", StringComparison.OrdinalIgnoreCase) >= 0)
                return 10;
            if (block.Category.IndexOf("Identity", StringComparison.OrdinalIgnoreCase) >= 0)
                return 11;
            return 12;
        }

        private static int RuntimePriority(AIGMUMGNeoStackKind kind)
        {
            switch (kind)
            {
                case AIGMUMGNeoStackKind.Governance:
                    return 1;
                case AIGMUMGNeoStackKind.SituationalOverlays:
                    return 2;
                case AIGMUMGNeoStackKind.SquadRelationshipOperations:
                    return 3;
                case AIGMUMGNeoStackKind.CombatDoctrine:
                    return 4;
                case AIGMUMGNeoStackKind.SkillsSpellsResources:
                    return 5;
                case AIGMUMGNeoStackKind.MovementPositioning:
                    return 6;
                case AIGMUMGNeoStackKind.TrackingAwareness:
                    return 7;
                case AIGMUMGNeoStackKind.Capability:
                    return 8;
                case AIGMUMGNeoStackKind.Identity:
                default:
                    return 9;
            }
        }

        private static void AppendFacts(AIGMUMGDecisionTrace trace, AIGMCapabilitySnapshot snapshot, Dictionary<string, string> facts)
        {
            trace.SituationFacts.Add("capability=" + AIGMCapabilityRegistry.BuildCompactSummary(snapshot));
            foreach (KeyValuePair<string, string> pair in facts)
            {
                if (trace.SituationFacts.Count >= 10)
                    break;

                trace.SituationFacts.Add(pair.Key + "=" + pair.Value);
            }
        }

        private static bool ContainsAny(string text, params string[] needles)
        {
            if (String.IsNullOrWhiteSpace(text) || needles == null)
                return false;

            for (int i = 0; i < needles.Length; i++)
            {
                if (!String.IsNullOrWhiteSpace(needles[i]) && text.IndexOf(needles[i], StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }
    }
}
