using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Server.Mobiles;

namespace Server.Custom.AIGM.UMG
{
    public sealed class AIGMUMGResolvedReference
    {
        public AIGMUMGNeoBlockReference Reference { get; set; }
        public AIGMUMGLibraryDefinition LibraryDefinition { get; set; }
        public AIGMUMGBlock SleeveBlock { get; set; }
        public string DefinitionId { get; set; }
        public string DisplayName { get; set; }
        public string Source { get; set; }
        public string Summary { get; set; }
        public int PriorityOrder { get; set; }
        public List<AIGMCapabilityRequirement> CapabilityRequirements { get; private set; }

        public AIGMUMGResolvedReference()
        {
            DefinitionId = String.Empty;
            DisplayName = String.Empty;
            Source = String.Empty;
            Summary = String.Empty;
            PriorityOrder = 500;
            CapabilityRequirements = new List<AIGMCapabilityRequirement>();
        }
    }

    public static class AIGMUMGStableHash
    {
        public static string Compute(params string[] parts)
        {
            StringBuilder sb = new StringBuilder();
            if (parts != null)
            {
                for (int i = 0; i < parts.Length; i++)
                    sb.Append(parts[i] ?? String.Empty).Append('\n');
            }

            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(sb.ToString());
                byte[] hash = sha.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", String.Empty);
            }
        }

        public static string Normalize(string value)
        {
            return (value ?? String.Empty).Trim().ToLowerInvariant().Replace(" ", "_").Replace("-", "_");
        }

        public static string Safe(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return String.Empty;

            value = value.Trim().Replace('\r', ' ').Replace('\n', ' ');
            return value.Length > 240 ? value.Substring(0, 240) : value;
        }
    }

    public static class AIGMUMGActivationGraphBuilder
    {
        private static readonly object SyncRoot = new object();
        private static readonly Dictionary<string, AIGMUMGActivationGraph> Cache = new Dictionary<string, AIGMUMGActivationGraph>(StringComparer.OrdinalIgnoreCase);

        private static readonly string[] MandatoryAlwaysOnRoles =
        {
            "Governance",
            "Identity",
            "Capability Truth",
            "System Invariants",
            "Operational Authority",
            "Owner/Commander Authority",
            "Safety",
            "Logging"
        };

        public static AIGMUMGActivationGraph Build(Mobile actor, AIGMUMGOperationalLayoutVersionRecord approvedVersion)
        {
            AIGMUMGActivationGraph graph = BuildUncached(actor, approvedVersion);
            string cacheKey = graph.ActorSerial + "|" + graph.LayoutVersionId + "|" + graph.GraphFingerprint;

            lock (SyncRoot)
            {
                AIGMUMGActivationGraph cached;
                if (Cache.TryGetValue(cacheKey, out cached))
                    return cached;

                RemoveActorVersionEntries(graph.ActorSerial, graph.LayoutVersionId);
                Cache[cacheKey] = graph;
            }

            return graph;
        }

        public static bool TryResolveReference(Mobile actor, AIGMUMGNeoBlockReference reference, out AIGMUMGResolvedReference resolved)
        {
            resolved = new AIGMUMGResolvedReference();
            if (reference == null || String.IsNullOrWhiteSpace(reference.DefinitionId))
                return false;

            resolved.Reference = reference;
            resolved.DefinitionId = reference.DefinitionId;
            resolved.DisplayName = String.IsNullOrWhiteSpace(reference.DisplayName) ? reference.DefinitionId : reference.DisplayName;
            resolved.Source = String.IsNullOrWhiteSpace(reference.ReferenceKind) ? "Unknown" : reference.ReferenceKind;

            AIGMUMGLibraryDefinition definition = AIGMUMGRepository.GetLibraryDefinition(reference.DefinitionId);
            if (definition != null)
            {
                resolved.LibraryDefinition = definition;
                resolved.DisplayName = String.IsNullOrWhiteSpace(definition.Name) ? resolved.DisplayName : definition.Name;
                resolved.Summary = definition.Summary;
                resolved.Source = "LibraryDefinition";
                resolved.PriorityOrder = definition.NeoBlock != null ? definition.NeoBlock.PriorityOrder : 500;
                AddRequirements(resolved.CapabilityRequirements, definition.CapabilityRequirements);
                return true;
            }

            AIGMUMGBlock sleeveBlock = FindSleeveBlock(actor, reference.DefinitionId);
            if (sleeveBlock != null)
            {
                resolved.SleeveBlock = sleeveBlock;
                resolved.DisplayName = String.IsNullOrWhiteSpace(sleeveBlock.Name) ? resolved.DisplayName : sleeveBlock.Name;
                resolved.Summary = sleeveBlock.Summary;
                resolved.Source = "CanonicalSleeveBlock";
                resolved.PriorityOrder = sleeveBlock.PriorityOrder;
                AddRequirements(resolved.CapabilityRequirements, sleeveBlock.CapabilityRequirements);
                return true;
            }

            return false;
        }

        public static List<AIGMUMGResolvedReference> ResolveStackReferences(Mobile actor, AIGMUMGOperationalNeoStack stack, List<string> invalidDefinitions)
        {
            List<AIGMUMGResolvedReference> resolved = new List<AIGMUMGResolvedReference>();
            if (stack == null || stack.NeoBlockReferences == null)
                return resolved;

            List<AIGMUMGNeoBlockReference> refs = new List<AIGMUMGNeoBlockReference>(stack.NeoBlockReferences);
            refs.Sort(CompareReferences);
            for (int i = 0; i < refs.Count; i++)
            {
                AIGMUMGResolvedReference item;
                if (TryResolveReference(actor, refs[i], out item))
                    resolved.Add(item);
                else if (invalidDefinitions != null && refs[i] != null)
                    invalidDefinitions.Add(refs[i].DefinitionId);
            }

            return resolved;
        }

        private static AIGMUMGActivationGraph BuildUncached(Mobile actor, AIGMUMGOperationalLayoutVersionRecord approvedVersion)
        {
            AIGMUMGActivationGraph graph = new AIGMUMGActivationGraph();
            graph.BuildTimestamp = DateTime.UtcNow;
            graph.ActorSerial = actor != null ? String.Format("0x{0:X8}", actor.Serial.Value) : String.Empty;

            AIGMUMGOperationalLayout layout = approvedVersion != null ? approvedVersion.LayoutSnapshot : null;
            if (approvedVersion == null || layout == null)
            {
                graph.ValidationResult.Error("NO_APPROVED_OPERATIONAL_LAYOUT");
                graph.GraphFingerprint = Fingerprint(graph);
                return graph;
            }

            graph.ActorKey = approvedVersion.ActorKey;
            graph.LayoutVersionId = approvedVersion.VersionId;
            graph.LayoutRevision = approvedVersion.Revision;
            graph.LayoutId = layout.LayoutId;

            if (approvedVersion.State != AIGMUMGOperationalLayoutState.ApprovedPreviewOnly || approvedVersion.ExecutionMode != AIGMUMGExecutionMode.PreviewOnly)
                graph.ValidationResult.Error("approved_preview_only_required");
            if (layout.State != AIGMUMGOperationalLayoutState.ApprovedPreviewOnly || layout.ExecutionMode != AIGMUMGExecutionMode.PreviewOnly)
                graph.ValidationResult.Error("layout_snapshot_not_approved_preview_only");

            AIGMUMGOperationalLayoutValidationResult layoutValidation = AIGMUMGOperationalLayoutService.ValidateLayout(actor, layout);
            for (int i = 0; i < layoutValidation.StructuralErrors.Count; i++)
                graph.ValidationResult.Error("layout:" + layoutValidation.StructuralErrors[i]);
            for (int i = 0; i < layoutValidation.Warnings.Count; i++)
                graph.ValidationResult.Warn("layout:" + layoutValidation.Warnings[i]);

            Dictionary<string, AIGMUMGActivationGraphNode> nodeIds = new Dictionary<string, AIGMUMGActivationGraphNode>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, bool> alwaysRoles = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < MandatoryAlwaysOnRoles.Length; i++)
                alwaysRoles[MandatoryAlwaysOnRoles[i]] = false;

            List<AIGMUMGOperationalFamily> families = new List<AIGMUMGOperationalFamily>(layout.Families);
            families.Sort(CompareFamilies);
            for (int f = 0; f < families.Count; f++)
            {
                AIGMUMGOperationalFamily family = families[f];
                if (family == null)
                    continue;

                string familyNodeId = "family:" + family.FamilyId;
                AddNode(graph, nodeIds, new AIGMUMGActivationGraphNode
                {
                    NodeId = familyNodeId,
                    NodeType = AIGMUMGActivationGraphNodeType.FamilyNode,
                    Label = family.DisplayName,
                    FamilyId = family.FamilyId,
                    ConfigurationEnabled = family.Enabled,
                    EligibleConfiguration = family.Enabled
                });
                graph.FamilyRootIds.Add(familyNodeId);

                List<AIGMUMGOperationalNeoStack> stacks = family.OperationalNeoStacks != null
                    ? new List<AIGMUMGOperationalNeoStack>(family.OperationalNeoStacks)
                    : new List<AIGMUMGOperationalNeoStack>();
                stacks.Sort(CompareStacks);
                for (int s = 0; s < stacks.Count; s++)
                {
                    AIGMUMGOperationalNeoStack stack = stacks[s];
                    if (stack == null)
                        continue;

                    bool always = String.Equals(family.FamilyId, AIGMUMGOperationalLayoutService.FamilyAlwaysOn, StringComparison.OrdinalIgnoreCase);
                    string stackNodeId = "stack:" + stack.StackId;
                    AIGMUMGActivationGraphNode stackNode = new AIGMUMGActivationGraphNode
                    {
                        NodeId = stackNodeId,
                        NodeType = always ? AIGMUMGActivationGraphNodeType.AlwaysOnNode : AIGMUMGActivationGraphNodeType.OperationalStackNode,
                        Label = stack.DisplayName,
                        FamilyId = family.FamilyId,
                        StackId = stack.StackId,
                        ConfigurationEnabled = family.Enabled && stack.Enabled,
                        EligibleConfiguration = family.Enabled && stack.Enabled
                    };
                    stackNode.Details.Add(new AIGMUMGKeyValue("order", stack.Order.ToString()));
                    AddNode(graph, nodeIds, stackNode);
                    graph.DirectedEdges.Add(new AIGMUMGActivationGraphEdge(familyNodeId, stackNodeId, "family_contains_stack"));
                    if (always)
                        graph.AlwaysOnNodeIds.Add(stackNodeId);

                    List<AIGMUMGNeoBlockReference> refs = stack.NeoBlockReferences != null
                        ? new List<AIGMUMGNeoBlockReference>(stack.NeoBlockReferences)
                        : new List<AIGMUMGNeoBlockReference>();
                    refs.Sort(CompareReferences);
                    for (int r = 0; r < refs.Count; r++)
                    {
                        AIGMUMGNeoBlockReference reference = refs[r];
                        if (reference == null)
                            continue;

                        string refNodeId = "ref:" + reference.ReferenceId;
                        AIGMUMGActivationGraphNode refNode = new AIGMUMGActivationGraphNode
                        {
                            NodeId = refNodeId,
                            NodeType = AIGMUMGActivationGraphNodeType.NeoBlockReferenceNode,
                            Label = String.IsNullOrWhiteSpace(reference.DisplayName) ? reference.DefinitionId : reference.DisplayName,
                            FamilyId = family.FamilyId,
                            StackId = stack.StackId,
                            ReferenceId = reference.ReferenceId,
                            DefinitionId = reference.DefinitionId,
                            ConfigurationEnabled = family.Enabled && stack.Enabled && reference.Enabled,
                            EligibleConfiguration = family.Enabled && stack.Enabled && reference.Enabled
                        };
                        refNode.Details.Add(new AIGMUMGKeyValue("referenceKind", reference.ReferenceKind));
                        refNode.Details.Add(new AIGMUMGKeyValue("mandatoryRole", reference.MandatoryRole));
                        AddNode(graph, nodeIds, refNode);
                        graph.DirectedEdges.Add(new AIGMUMGActivationGraphEdge(stackNodeId, refNodeId, "stack_contains_reference"));

                        AIGMUMGResolvedReference resolved;
                        if (!TryResolveReference(actor, reference, out resolved))
                            graph.ValidationResult.Error("missing_reference_definition:" + reference.DefinitionId);

                        if (!String.IsNullOrWhiteSpace(reference.MandatoryRole) && alwaysRoles.ContainsKey(reference.MandatoryRole))
                            alwaysRoles[reference.MandatoryRole] = true;
                    }

                    if (!always)
                    {
                        string triggerNodeId = "trigger:" + stack.StackId;
                        string capNodeId = "capability:" + stack.StackId;
                        string govNodeId = "governance:" + stack.StackId;
                        string intentNodeId = "intent:" + stack.StackId;
                        string adapterNodeId = "adapter:" + stack.StackId;
                        AddNode(graph, nodeIds, RuntimeNode(triggerNodeId, AIGMUMGActivationGraphNodeType.TriggerProfileNode, "Trigger Profile", family.FamilyId, stack.StackId));
                        AddNode(graph, nodeIds, RuntimeNode(capNodeId, AIGMUMGActivationGraphNodeType.CapabilityGateNode, "Capability Gate", family.FamilyId, stack.StackId));
                        AddNode(graph, nodeIds, RuntimeNode(govNodeId, AIGMUMGActivationGraphNodeType.GovernanceGateNode, "Governance Gate", family.FamilyId, stack.StackId));
                        AddNode(graph, nodeIds, RuntimeNode(intentNodeId, AIGMUMGActivationGraphNodeType.TypedIntentNode, "Typed Intent Proposal", family.FamilyId, stack.StackId));
                        AddNode(graph, nodeIds, RuntimeNode(adapterNodeId, AIGMUMGActivationGraphNodeType.AdapterMappingNode, "Adapter Mapping", family.FamilyId, stack.StackId));
                        graph.DirectedEdges.Add(new AIGMUMGActivationGraphEdge(stackNodeId, triggerNodeId, "stack_evaluates_triggers"));
                        graph.DirectedEdges.Add(new AIGMUMGActivationGraphEdge(triggerNodeId, capNodeId, "trigger_gates_capability"));
                        graph.DirectedEdges.Add(new AIGMUMGActivationGraphEdge(capNodeId, govNodeId, "capability_gates_governance"));
                        graph.DirectedEdges.Add(new AIGMUMGActivationGraphEdge(govNodeId, intentNodeId, "governance_gates_intent"));
                        graph.DirectedEdges.Add(new AIGMUMGActivationGraphEdge(intentNodeId, adapterNodeId, "intent_maps_adapter_without_invocation"));
                    }
                }
            }

            for (int i = 0; i < MandatoryAlwaysOnRoles.Length; i++)
            {
                if (!alwaysRoles[MandatoryAlwaysOnRoles[i]])
                    graph.ValidationResult.Error("MISSING_ALWAYS_ON_INVARIANT:" + MandatoryAlwaysOnRoles[i]);
            }

            ValidateReachabilityAndCycles(graph, nodeIds);
            graph.GraphFingerprint = Fingerprint(graph);
            return graph;
        }

        private static AIGMUMGActivationGraphNode RuntimeNode(string nodeId, AIGMUMGActivationGraphNodeType type, string label, string familyId, string stackId)
        {
            return new AIGMUMGActivationGraphNode
            {
                NodeId = nodeId,
                NodeType = type,
                Label = label,
                FamilyId = familyId,
                StackId = stackId,
                ConfigurationEnabled = true,
                EligibleConfiguration = true
            };
        }

        private static void AddNode(AIGMUMGActivationGraph graph, Dictionary<string, AIGMUMGActivationGraphNode> nodeIds, AIGMUMGActivationGraphNode node)
        {
            if (graph == null || node == null)
                return;

            if (String.IsNullOrWhiteSpace(node.NodeId))
            {
                graph.ValidationResult.Error("node_id_missing");
                return;
            }

            if (nodeIds.ContainsKey(node.NodeId))
            {
                graph.ValidationResult.Error("duplicate_node_id:" + node.NodeId);
                return;
            }

            nodeIds[node.NodeId] = node;
            graph.Nodes.Add(node);
        }

        private static void ValidateReachabilityAndCycles(AIGMUMGActivationGraph graph, Dictionary<string, AIGMUMGActivationGraphNode> nodeIds)
        {
            Dictionary<string, List<string>> edges = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < graph.DirectedEdges.Count; i++)
            {
                AIGMUMGActivationGraphEdge edge = graph.DirectedEdges[i];
                if (edge == null || String.IsNullOrWhiteSpace(edge.FromNodeId) || String.IsNullOrWhiteSpace(edge.ToNodeId))
                {
                    graph.ValidationResult.Error("edge_missing_endpoint");
                    continue;
                }

                if (!nodeIds.ContainsKey(edge.FromNodeId))
                    graph.ValidationResult.Error("edge_missing_from:" + edge.FromNodeId);
                if (!nodeIds.ContainsKey(edge.ToNodeId))
                    graph.ValidationResult.Error("edge_missing_to:" + edge.ToNodeId);

                List<string> list;
                if (!edges.TryGetValue(edge.FromNodeId, out list))
                {
                    list = new List<string>();
                    edges[edge.FromNodeId] = list;
                }
                list.Add(edge.ToNodeId);
            }

            HashSet<string> visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string root in graph.FamilyRootIds)
                Visit(root, edges, visited, visiting, graph.ValidationResult);

            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                AIGMUMGActivationGraphNode node = graph.Nodes[i];
                if (node.NodeType == AIGMUMGActivationGraphNodeType.FamilyNode && !visited.Contains(node.NodeId))
                    graph.ValidationResult.Error("family_not_reachable:" + node.NodeId);
                if (node.NodeType == AIGMUMGActivationGraphNodeType.OperationalStackNode && !visited.Contains(node.NodeId))
                    graph.ValidationResult.Error("orphan_operational_stack:" + node.NodeId);
                if (node.NodeType == AIGMUMGActivationGraphNodeType.AlwaysOnNode && !visited.Contains(node.NodeId))
                    graph.ValidationResult.Error("always_on_not_reachable:" + node.NodeId);
            }
        }

        private static void Visit(string nodeId, Dictionary<string, List<string>> edges, HashSet<string> visited, HashSet<string> visiting, AIGMUMGActivationGraphValidationResult validation)
        {
            if (String.IsNullOrWhiteSpace(nodeId) || visited.Contains(nodeId))
                return;

            if (visiting.Contains(nodeId))
            {
                validation.Error("activation_graph_cycle:" + nodeId);
                return;
            }

            visiting.Add(nodeId);
            List<string> children;
            if (edges.TryGetValue(nodeId, out children))
            {
                children.Sort(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < children.Count; i++)
                    Visit(children[i], edges, visited, visiting, validation);
            }
            visiting.Remove(nodeId);
            visited.Add(nodeId);
        }

        private static string Fingerprint(AIGMUMGActivationGraph graph)
        {
            List<string> parts = new List<string>();
            parts.Add(graph.ActorSerial);
            parts.Add(graph.ActorKey);
            parts.Add(graph.LayoutId);
            parts.Add(graph.LayoutVersionId);
            parts.Add(graph.LayoutRevision.ToString());
            parts.Add(graph.GraphVersion);

            List<AIGMUMGActivationGraphNode> nodes = new List<AIGMUMGActivationGraphNode>(graph.Nodes);
            nodes.Sort(delegate (AIGMUMGActivationGraphNode left, AIGMUMGActivationGraphNode right) { return String.Compare(left.NodeId, right.NodeId, StringComparison.OrdinalIgnoreCase); });
            for (int i = 0; i < nodes.Count; i++)
            {
                AIGMUMGActivationGraphNode node = nodes[i];
                parts.Add("N|" + node.NodeId + "|" + node.NodeType + "|" + node.Label + "|" + node.FamilyId + "|" + node.StackId + "|" + node.ReferenceId + "|" + node.DefinitionId + "|" + node.ConfigurationEnabled + "|" + node.EligibleConfiguration);
            }

            List<AIGMUMGActivationGraphEdge> edges = new List<AIGMUMGActivationGraphEdge>(graph.DirectedEdges);
            edges.Sort(delegate (AIGMUMGActivationGraphEdge left, AIGMUMGActivationGraphEdge right)
            {
                int byFrom = String.Compare(left.FromNodeId, right.FromNodeId, StringComparison.OrdinalIgnoreCase);
                if (byFrom != 0)
                    return byFrom;
                int byTo = String.Compare(left.ToNodeId, right.ToNodeId, StringComparison.OrdinalIgnoreCase);
                if (byTo != 0)
                    return byTo;
                return String.Compare(left.EdgeKind, right.EdgeKind, StringComparison.OrdinalIgnoreCase);
            });
            for (int i = 0; i < edges.Count; i++)
                parts.Add("E|" + edges[i].FromNodeId + "|" + edges[i].ToNodeId + "|" + edges[i].EdgeKind);

            return AIGMUMGStableHash.Compute(parts.ToArray());
        }

        private static AIGMUMGBlock FindSleeveBlock(Mobile actor, string blockId)
        {
            if (actor == null || String.IsNullOrWhiteSpace(blockId))
                return null;

            string actorId = AIGMUMGRuntimeService.ResolveActorId(actor);
            AIGMUMGSleeve sleeve = AIGMUMGRepository.GetSleeve(actorId);
            if (sleeve == null || sleeve.NeoStacks == null)
                return null;

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
                            if (block != null && String.Equals(block.BlockId, blockId, StringComparison.OrdinalIgnoreCase))
                                return block;
                        }
                    }
                }
            }

            return null;
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

        private static void RemoveActorVersionEntries(string actorSerial, string versionId)
        {
            List<string> remove = new List<string>();
            foreach (string key in Cache.Keys)
            {
                if (key.StartsWith(actorSerial + "|" + versionId + "|", StringComparison.OrdinalIgnoreCase))
                    remove.Add(key);
            }
            for (int i = 0; i < remove.Count; i++)
                Cache.Remove(remove[i]);
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

        private static int CompareReferences(AIGMUMGNeoBlockReference left, AIGMUMGNeoBlockReference right)
        {
            int byOrder = SafeOrder(left != null ? left.Order : 0).CompareTo(SafeOrder(right != null ? right.Order : 0));
            if (byOrder != 0)
                return byOrder;
            return String.Compare(left != null ? left.ReferenceId : String.Empty, right != null ? right.ReferenceId : String.Empty, StringComparison.OrdinalIgnoreCase);
        }

        private static int SafeOrder(int value)
        {
            return value <= 0 ? 500 : value;
        }
    }
}
