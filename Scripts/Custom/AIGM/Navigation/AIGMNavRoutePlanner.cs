using System;
using System.Collections.Generic;

using Server;
using Server.Custom.AIGM;

namespace Server.Custom.AIGM.Navigation
{
    public static class AIGMNavRoutePlanner
    {
        public static bool DefaultAllowUnverifiedEdges { get { return false; } }
        private const int AnchorSearchRadius = 160;
        private const int VirtualLinkLimit = 12;
        private const int DestinationApproachRadius = 72;
        private const int PartialRouteProgressTilesMinimum = 20;
        private const double PartialRouteProgressRatioMinimum = 0.20;
        private const int PartialRouteFinalApproachLimit = 25;
        private const double DestinationCollapsePenalty = 325.0;
        private const double WrongDirectionBasePenalty = 40.0;
        private const double BacktrackPenaltyFactor = 1.0;
        private const double GenericBankConnectorPenalty = 80.0;
        private const double LooseApproachPenalty = 24.0;

        public static AIGMNavRoutePlan BuildRoute(Mobile actor, Point3D finalDestination, Map map, string destinationName, string rawDestination = null, bool allowUnverifiedEdges = false)
        {
            AIGMNavRoutePlan plan = new AIGMNavRoutePlan();
            plan.Start = actor != null ? actor.Location : Point3D.Zero;
            plan.FinalDestination = finalDestination;
            plan.Map = map;
            plan.RawDestination = String.IsNullOrWhiteSpace(rawDestination) ? destinationName : rawDestination;

            if (actor == null || map == null)
            {
                plan.Reason = "actor_or_map_missing";
                plan.RouteMode = "NoUsefulGraphRoute";
                LogPlan(actor, destinationName, plan, false);
                return plan;
            }

            AIGMNavGraph graph = AIGMNavGraphStore.Graph;
            AIGMNavigationLocation destinationLocation = ResolveDestinationLocation(destinationName, map, finalDestination);
            AIGMNavNode explicitDestinationNode = ResolveExplicitDestinationNode(graph, destinationName, finalDestination, map);
            DestinationSemanticResolution destinationSemantic = ResolveDestinationSemantic(plan.RawDestination, destinationName, destinationLocation, explicitDestinationNode);
            ApplyDestinationSemantic(plan, destinationSemantic);
            LogDestinationSemantic(plan);
            plan.DestinationNodeLoaded = explicitDestinationNode != null;
            plan.AliasMatched = destinationSemantic != null && !String.IsNullOrWhiteSpace(destinationSemantic.AliasesMatched) && !String.Equals(destinationSemantic.AliasesMatched, "none", StringComparison.OrdinalIgnoreCase);
            plan.DestinationEdgeCount = explicitDestinationNode != null ? graph.GetOutgoing(explicitDestinationNode, true).Count : 0;
            AIGMNavGraphStore.LogStatus(map);
            if (graph.Nodes.Count == 0)
            {
                plan.Reason = "graph_empty";
                plan.RouteMode = "NoUsefulGraphRoute";
                LogPlan(actor, destinationName, plan, false);
                return plan;
            }

            List<AIGMNavNode> startAnchors = BuildStartAnchors(graph, actor.Location, map, allowUnverifiedEdges);
            List<AIGMNavNode> destinationAnchors = BuildDestinationAnchors(graph, finalDestination, map, destinationName, destinationLocation, explicitDestinationNode);
            bool exactDestinationRouteAvailable = HasDestinationRoleRoute(graph, startAnchors, destinationAnchors, destinationLocation, destinationName, explicitDestinationNode, finalDestination, DestinationAnchorRole.Exact, allowUnverifiedEdges);
            bool approachDestinationRouteAvailable = !exactDestinationRouteAvailable && HasDestinationRoleRoute(graph, startAnchors, destinationAnchors, destinationLocation, destinationName, explicitDestinationNode, finalDestination, DestinationAnchorRole.Approach, allowUnverifiedEdges);
            plan.StartCandidateCount = startAnchors.Count;
            plan.DestinationCandidateCount = destinationAnchors.Count;
            if (startAnchors.Count == 0 || destinationAnchors.Count == 0)
            {
                PopulateRefusalDetail(plan, graph, actor.Location, finalDestination, destinationName, destinationLocation, explicitDestinationNode);
                plan.Reason = startAnchors.Count == 0 ? "no_start_anchor" : "no_destination_anchor";
                plan.RouteMode = "NoUsefulGraphRoute";
                LogPlan(actor, destinationName, plan, false);
                return plan;
            }

            AnchorCandidateEvaluation best = null;
            for (int s = 0; s < startAnchors.Count; s++)
            {
                for (int d = 0; d < destinationAnchors.Count; d++)
                {
                    AnchorCandidateEvaluation candidate = EvaluateCandidate(graph, actor.Location, finalDestination, destinationName, destinationLocation, explicitDestinationNode, startAnchors[s], destinationAnchors[d], exactDestinationRouteAvailable, approachDestinationRouteAvailable, allowUnverifiedEdges);
                    plan.CandidatePairCount++;
                    if (candidate.Rejected)
                    {
                        plan.RejectedCandidateCount++;
                        if (String.Equals(candidate.RejectReason, "wrong_direction", StringComparison.OrdinalIgnoreCase))
                            plan.RejectedWrongDirectionCount++;
                        else if (String.Equals(candidate.RejectReason, "semantic_mismatch", StringComparison.OrdinalIgnoreCase))
                            plan.RejectedSemanticCount++;
                        else if (String.Equals(candidate.RejectReason, "no_graph_path", StringComparison.OrdinalIgnoreCase))
                            plan.RejectedNoGraphPathCount++;
                    }

                    LogAnchorCandidate(plan.RouteId, candidate);
                    if (candidate.Rejected)
                    {
                        LogAnchorRejected(plan.RouteId, candidate);
                        continue;
                    }

                    if (best == null || candidate.TotalScore < best.TotalScore)
                        best = candidate;
                }
            }

            if (best == null)
            {
                PartialRouteEvaluation partial = EvaluatePartialRoute(plan.RouteId, graph, actor.Location, finalDestination, destinationName, destinationLocation, explicitDestinationNode, startAnchors, allowUnverifiedEdges);
                if (partial != null && !partial.Rejected)
                {
                    ApplyPartialRoute(plan, actor, destinationName, partial);
                    LogPlan(actor, destinationName, plan, true);
                    return plan;
                }

                if (CanUseDirectLocalRoute(actor.Location, finalDestination, map))
                {
                    BuildDirectLocalRoute(plan, destinationName);
                    LogPlan(actor, destinationName, plan, true);
                    return plan;
                }

                PopulateRefusalDetail(plan, graph, actor.Location, finalDestination, destinationName, destinationLocation, explicitDestinationNode);
                plan.Reason = "no_useful_graph_route";
                plan.RouteMode = "UnsafeFallbackBlocked";
                plan.FinalApproachAllowed = false;
                plan.FinalApproachReason = "unmapped_gap_too_large";
                LogPlan(actor, destinationName, plan, false);
                return plan;
            }

            plan.Success = true;
            plan.Reason = "graph";
            plan.RouteMode = "ExactGraphRoute";
            plan.IsPartial = false;
            plan.TotalCost = best.ApproachCost + best.GraphCost + best.FinalApproachCost;
            plan.SelectedScore = best.TotalScore;
            plan.StartAnchor = best.StartAnchor;
            plan.DestinationAnchor = best.DestinationAnchor;
            plan.SelectedDestinationRole = best.DestinationRole.ToString().ToLowerInvariant();
            plan.RouteEndsAtDestinationNode = best.RouteEndsAtDestinationNode;

            AIGMExecutionLog.Write("AIGM_NAV_ANCHOR_SELECTED routeId={0} selectedStartId={1} selectedDestId={2} selectedScore={3:0.0} selectedRouteIds=\"{4}\" selectedRoutePoints=\"{5}\"",
                SafeLog(plan.RouteId),
                plan.StartAnchor != null ? SafeLog(plan.StartAnchor.Id) : "none",
                plan.DestinationAnchor != null ? SafeLog(plan.DestinationAnchor.Id) : "none",
                plan.SelectedScore,
                SafeLog(FormatNodeIds(best.Route.Nodes)),
                SafeLog(FormatPointPath(best.Route.Nodes)));
            AIGMExecutionLog.Write("AIGM_NAV_DEST_ANCHOR_SELECTED routeId={0} selectedDestId={1} selectedDestRole={2} canonicalDestinationId={3} finalVirtualTarget={4} routeEndsAtDestinationNode={5}",
                SafeLog(plan.RouteId),
                plan.DestinationAnchor != null ? SafeLog(plan.DestinationAnchor.Id) : "none",
                SafeLog(plan.SelectedDestinationRole),
                SafeLog(plan.CanonicalDestinationId),
                AIGMNavRoutePlan.FormatPoint(plan.FinalDestination),
                plan.RouteEndsAtDestinationNode);
            AIGMExecutionLog.Write("AIGM_NAV_GRAPH_ANCHOR actor={0} companionPoint={1} startAnchorId={2} startAnchorPoint={3} startAnchorDistance={4} destAnchorId={5} destAnchorPoint={6} destAnchorDistance={7}",
                Describe(actor),
                AIGMNavRoutePlan.FormatPoint(actor.Location),
                plan.StartAnchor != null ? SafeLog(plan.StartAnchor.Id) : "none",
                plan.StartAnchor != null ? AIGMNavRoutePlan.FormatPoint(plan.StartAnchor.Location) : "none",
                plan.StartAnchor != null ? AIGMNavGraph.Distance(actor.Location, plan.StartAnchor.Location).ToString() : "-1",
                plan.DestinationAnchor != null ? SafeLog(plan.DestinationAnchor.Id) : "none",
                plan.DestinationAnchor != null ? AIGMNavRoutePlan.FormatPoint(plan.DestinationAnchor.Location) : "none",
                plan.DestinationAnchor != null ? AIGMNavGraph.Distance(finalDestination, plan.DestinationAnchor.Location).ToString() : "-1");

            for (int i = 0; i < best.Route.Nodes.Count; i++)
            {
                AIGMNavNode node = best.Route.Nodes[i];
                if (node == null)
                    continue;

                if (i == 0 && AIGMNavGraph.Distance(actor.Location, node.Location) <= node.ArrivalRadius)
                    continue;

                AIGMNavRouteStep step = new AIGMNavRouteStep();
                step.NodeId = node.Id;
                step.Name = node.Name;
                step.Location = node.Location;
                step.ArrivalRadius = Math.Max(1, node.ArrivalRadius);
                plan.Steps.Add(step);
            }

            int finalRadius = 2;
            if (plan.DestinationAnchor != null)
                finalRadius = Math.Max(2, Math.Min(6, plan.DestinationAnchor.ArrivalRadius));

            plan.RouteEndsAtDestinationNode = plan.Steps.Count > 0 && AIGMNavGraph.Distance(plan.Steps[plan.Steps.Count - 1].Location, finalDestination) <= finalRadius;
            if (!plan.RouteEndsAtDestinationNode)
            {
                AIGMNavRouteStep finalStep = new AIGMNavRouteStep();
                finalStep.NodeId = "virtual_destination";
                finalStep.Name = String.IsNullOrWhiteSpace(destinationName) ? "destination" : destinationName;
                finalStep.Location = finalDestination;
                finalStep.ArrivalRadius = finalRadius;
                plan.Steps.Add(finalStep);
            }

            ApplyEdgeSummary(plan);
            LogPlan(actor, destinationName, plan, true);
            return plan;
        }

        private static void LogPlan(Mobile actor, string destinationName, AIGMNavRoutePlan plan, bool routeFound)
        {
            if (plan == null)
                return;

            AIGMExecutionLog.Write("AIGM_NAV_ROUTE_PLAN routeId={0} actor={1} destination=\"{2}\" plannerUsed=True algorithm=AStar routeFound={3} reason={4} routeMode={5} isPartial={6} nodeCount={7} edgeCount={8} routeIds=\"{9}\" routePoints=\"{10}\" start={11} final={12} startAnchorId={13} startAnchorPoint={14} destAnchorId={15} destAnchorPoint={16} totalCost={17:0.0} selectedScore={18:0.0} startCandidates={19} destCandidates={20} candidatePairs={21} rejectedCandidates={22} rejectedWrongDirection={23} fallbackAllowed={24} canonicalDestinationId={25} canonicalDestinationName=\"{26}\" selectedDestRole={27} routeEndsAtDestinationNode={28} frontierNodeId={29} unmappedGapTiles={30} edgeStatuses=\"{31}\" certifiedEdgeCount={32} unverifiedEdgeCount={33} blockedEdgeCount={34}",
                SafeLog(plan.RouteId),
                Describe(actor),
                SafeLog(destinationName),
                routeFound && plan.Success,
                SafeLog(plan.Reason),
                SafeLog(plan.RouteMode),
                plan.IsPartial,
                AIGMNavGraphStore.Graph.Nodes.Count,
                AIGMNavGraphStore.Graph.Edges.Count,
                SafeLog(plan.FormatNodeIds()),
                SafeLog(plan.FormatPointPath()),
                AIGMNavRoutePlan.FormatPoint(plan.Start),
                AIGMNavRoutePlan.FormatPoint(plan.FinalDestination),
                plan.StartAnchor != null ? SafeLog(plan.StartAnchor.Id) : "none",
                plan.StartAnchor != null ? AIGMNavRoutePlan.FormatPoint(plan.StartAnchor.Location) : "none",
                plan.DestinationAnchor != null ? SafeLog(plan.DestinationAnchor.Id) : "none",
                plan.DestinationAnchor != null ? AIGMNavRoutePlan.FormatPoint(plan.DestinationAnchor.Location) : "none",
                plan.TotalCost,
                plan.SelectedScore,
                plan.StartCandidateCount,
                plan.DestinationCandidateCount,
                plan.CandidatePairCount,
                plan.RejectedCandidateCount,
                plan.RejectedWrongDirectionCount,
                plan.SafeDirectFallback,
                SafeLog(plan.CanonicalDestinationId),
                SafeLog(plan.CanonicalDestinationName),
                SafeLog(plan.SelectedDestinationRole),
                plan.RouteEndsAtDestinationNode,
                SafeLog(plan.FrontierNodeId),
                plan.UnmappedGapTiles,
                SafeLog(plan.EdgeStatuses),
                plan.CertifiedEdgeCount,
                plan.UnverifiedEdgeCount,
                plan.BlockedEdgeCount);
            if (!routeFound || !plan.Success)
            {
                AIGMExecutionLog.Write("AIGM_NAV_ROUTE_REFUSAL_DETAIL rawDestination=\"{0}\" canonicalDestination=\"{1}\" nodeLoaded={2} aliasMatched={3} edgeCount={4} pathExistsIgnoringCertification={5} pathExistsWithCertifiedEdges={6} blockedByUnverifiedEdges={7} blockedByNeedsIntermediate={8} routeMode={9} ownerMessage=\"{10}\"",
                    SafeLog(plan.RawDestination),
                    SafeLog(plan.CanonicalDestinationName),
                    plan.DestinationNodeLoaded,
                    plan.AliasMatched,
                    plan.DestinationEdgeCount,
                    plan.PathExistsIgnoringCertification,
                    plan.PathExistsWithCertifiedEdges,
                    plan.BlockedByUnverifiedEdges,
                    plan.BlockedByNeedsIntermediateEdges,
                    SafeLog(plan.RouteMode),
                    SafeLog(plan.OwnerRefusalMessage));
            }
        }

        private static void BuildDirectLocalRoute(AIGMNavRoutePlan plan, string destinationName)
        {
            if (plan == null)
                return;

            plan.Success = true;
            plan.Reason = "direct_local";
            plan.RouteMode = "DirectLocalRoute";
            plan.SafeDirectFallback = true;
            plan.IsPartial = false;
            plan.SelectedDestinationRole = "direct";
            plan.RouteEndsAtDestinationNode = false;
            plan.TotalCost = AIGMNavGraph.Distance(plan.Start, plan.FinalDestination);
            plan.SelectedScore = plan.TotalCost;

            AIGMNavRouteStep step = new AIGMNavRouteStep();
            step.NodeId = "virtual_destination";
            step.Name = String.IsNullOrWhiteSpace(destinationName) ? "destination" : destinationName;
            step.Location = plan.FinalDestination;
            step.ArrivalRadius = 3;
            plan.Steps.Add(step);
        }

        private static bool CanUseDirectLocalRoute(Point3D currentPoint, Point3D finalDestination, Map map)
        {
            return map != null
                && AIGMNavGraph.Distance(currentPoint, finalDestination) <= PartialRouteFinalApproachLimit
                && CanStandAt(map, finalDestination);
        }

        private static SearchResult Search(AIGMNavGraph graph, AIGMNavNode start, AIGMNavNode destination, Point3D finalDestination, bool allowUnverifiedEdges)
        {
            List<AIGMNavNode> open = new List<AIGMNavNode>();
            Dictionary<string, string> parent = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, double> gScore = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, AIGMNavNode> visited = new Dictionary<string, AIGMNavNode>(StringComparer.OrdinalIgnoreCase);

            open.Add(start);
            visited[start.Id] = start;
            parent[start.Id] = null;
            gScore[start.Id] = 0.0;

            while (open.Count > 0)
            {
                int index = FindBestOpen(open, gScore, finalDestination);
                AIGMNavNode current = open[index];
                open.RemoveAt(index);

                if (String.Equals(current.Id, destination.Id, StringComparison.OrdinalIgnoreCase))
                {
                    SearchResult result = new SearchResult();
                    result.Nodes = Reconstruct(destination.Id, parent, visited);
                    result.Cost = gScore[destination.Id];
                    return result;
                }

                List<AIGMNavEdge> edges = graph.GetOutgoing(current, allowUnverifiedEdges);
                for (int i = 0; i < edges.Count; i++)
                {
                    AIGMNavEdge edge = edges[i];
                    AIGMNavNode next = graph.GetOther(edge, current);
                    if (next == null || !next.Enabled || next.Map != current.Map)
                        continue;

                    double cost = EdgeCost(edge, current, next, true);
                    double newScore = gScore[current.Id] + cost;
                    double oldScore;
                    if (gScore.TryGetValue(next.Id, out oldScore) && oldScore <= newScore)
                        continue;

                    visited[next.Id] = next;
                    parent[next.Id] = current.Id;
                    gScore[next.Id] = newScore;
                    if (!Contains(open, next.Id))
                        open.Add(next);
                }
            }

            return null;
        }

        private static List<AIGMNavNode> BuildStartAnchors(AIGMNavGraph graph, Point3D actorPoint, Map map, bool allowUnverifiedEdges)
        {
            List<AIGMNavNode> raw = graph.Nearest(actorPoint, map, VirtualLinkLimit, AnchorSearchRadius);
            List<AIGMNavNode> filtered = new List<AIGMNavNode>();
            for (int i = 0; i < raw.Count; i++)
            {
                AIGMNavNode node = raw[i];
                if (node == null || !NodeHasUsableOutgoingEdges(graph, node, allowUnverifiedEdges))
                    continue;

                if (!CanStandAt(map, node.Location))
                    continue;

                AddUniqueNode(filtered, node);
            }

            return filtered.Count > 0 ? filtered : raw;
        }

        private static List<AIGMNavNode> BuildDestinationAnchors(AIGMNavGraph graph, Point3D finalDestination, Map map, string destinationName, AIGMNavigationLocation location, AIGMNavNode explicitDestinationNode)
        {
            List<AIGMNavNode> candidates = new List<AIGMNavNode>();
            if (explicitDestinationNode != null)
                AddUniqueNode(candidates, explicitDestinationNode);

            List<AIGMNavNode> nearby = graph.Nearest(finalDestination, map, VirtualLinkLimit, AnchorSearchRadius);
            for (int i = 0; i < nearby.Count; i++)
            {
                if (nearby[i] == null || !CanStandAt(map, nearby[i].Location))
                    continue;

                AddUniqueNode(candidates, nearby[i]);
            }

            if (location != null)
            {
                for (int i = 0; i < graph.Nodes.Count; i++)
                {
                    AIGMNavNode node = graph.Nodes[i];
                    if (node == null || !node.Enabled || node.Map != map)
                        continue;

                    if (!CanStandAt(map, node.Location))
                        continue;

                    if (MatchesDestinationNode(node, location, destinationName))
                        AddUniqueNode(candidates, node);
                    else if (AIGMNavGraph.Distance(node.Location, finalDestination) <= DestinationApproachRadius && HasUsefulApproachTag(node))
                        AddUniqueNode(candidates, node);
                }
            }

            return candidates;
        }

        private static DestinationSemanticResolution ResolveDestinationSemantic(string rawDestination, string destinationName, AIGMNavigationLocation location, AIGMNavNode explicitDestinationNode)
        {
            DestinationSemanticResolution semantic = new DestinationSemanticResolution();
            semantic.RawDestination = String.IsNullOrWhiteSpace(rawDestination) ? destinationName : rawDestination;
            semantic.CanonicalDestinationId = location != null && !String.IsNullOrWhiteSpace(location.Key)
                ? location.Key
                : (explicitDestinationNode != null ? explicitDestinationNode.Id : AIGMNavNode.BuildId(destinationName));
            semantic.CanonicalName = location != null && !String.IsNullOrWhiteSpace(location.DisplayName)
                ? location.DisplayName
                : (explicitDestinationNode != null ? explicitDestinationNode.Name : destinationName);
            semantic.DestinationKind = location != null && !String.IsNullOrWhiteSpace(location.Category)
                ? AIGMNavigationLocationRegistry.NormalizeForLookup(location.Category)
                : (explicitDestinationNode != null ? explicitDestinationNode.Type.ToString().ToLowerInvariant() : "virtual");
            semantic.ExplicitGraphNodeFound = explicitDestinationNode != null;
            semantic.AliasesMatched = ResolveAliasesMatched(semantic.RawDestination, location, explicitDestinationNode);
            return semantic;
        }

        private static void ApplyDestinationSemantic(AIGMNavRoutePlan plan, DestinationSemanticResolution semantic)
        {
            if (plan == null || semantic == null)
                return;

            plan.CanonicalDestinationId = semantic.CanonicalDestinationId;
            plan.CanonicalDestinationName = semantic.CanonicalName;
            plan.DestinationKind = semantic.DestinationKind;
            plan.DestinationAliasesMatched = semantic.AliasesMatched;
            plan.ExplicitDestinationGraphNodeFound = semantic.ExplicitGraphNodeFound;
        }

        private static void LogDestinationSemantic(AIGMNavRoutePlan plan)
        {
            if (plan == null)
                return;

            AIGMExecutionLog.Write("AIGM_NAV_DEST_SEMANTIC_RESOLVE routeId={0} rawDestination=\"{1}\" canonicalDestinationId={2} canonicalName=\"{3}\" aliasesMatched=\"{4}\" destinationKind={5} explicitGraphNodeFound={6}",
                SafeLog(plan.RouteId),
                SafeLog(plan.RawDestination),
                SafeLog(plan.CanonicalDestinationId),
                SafeLog(plan.CanonicalDestinationName),
                SafeLog(plan.DestinationAliasesMatched),
                SafeLog(plan.DestinationKind),
                plan.ExplicitDestinationGraphNodeFound);
        }

        private static string ResolveAliasesMatched(string rawDestination, AIGMNavigationLocation location, AIGMNavNode explicitDestinationNode)
        {
            string normalizedRaw = AIGMNavNode.Normalize(rawDestination);
            List<string> matches = new List<string>();

            AddAliasMatch(matches, location != null ? location.DisplayName : null, normalizedRaw);
            AddAliasMatch(matches, location != null ? location.Key : null, normalizedRaw);
            for (int i = 0; location != null && location.Aliases != null && i < location.Aliases.Length; i++)
                AddAliasMatch(matches, location.Aliases[i], normalizedRaw);

            AddAliasMatch(matches, explicitDestinationNode != null ? explicitDestinationNode.Name : null, normalizedRaw);
            AddAliasMatch(matches, explicitDestinationNode != null ? explicitDestinationNode.Id : null, normalizedRaw);
            for (int i = 0; explicitDestinationNode != null && explicitDestinationNode.Aliases != null && i < explicitDestinationNode.Aliases.Length; i++)
                AddAliasMatch(matches, explicitDestinationNode.Aliases[i], normalizedRaw);

            return matches.Count == 0 ? "none" : String.Join("|", matches.ToArray());
        }

        private static void AddAliasMatch(List<string> matches, string value, string normalizedRaw)
        {
            if (matches == null || String.IsNullOrWhiteSpace(value))
                return;

            string normalizedValue = AIGMNavNode.Normalize(value);
            if (!String.IsNullOrWhiteSpace(normalizedRaw) && normalizedValue != normalizedRaw)
                return;

            for (int i = 0; i < matches.Count; i++)
            {
                if (String.Equals(matches[i], value, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            matches.Add(value);
        }

        private static bool HasDestinationRoleRoute(AIGMNavGraph graph, List<AIGMNavNode> startAnchors, List<AIGMNavNode> destinationAnchors, AIGMNavigationLocation destinationLocation, string destinationName, AIGMNavNode explicitDestinationNode, Point3D finalDestination, DestinationAnchorRole role, bool allowUnverifiedEdges)
        {
            if (graph == null || startAnchors == null || destinationAnchors == null)
                return false;

            for (int s = 0; s < startAnchors.Count; s++)
            {
                AIGMNavNode startAnchor = startAnchors[s];
                if (startAnchor == null || !CanStandAt(startAnchor.Map, startAnchor.Location))
                    continue;

                for (int d = 0; d < destinationAnchors.Count; d++)
                {
                    AIGMNavNode destinationAnchor = destinationAnchors[d];
                    if (destinationAnchor == null || !CanStandAt(destinationAnchor.Map, destinationAnchor.Location))
                        continue;

                    if (GetDestinationAnchorRole(destinationAnchor, destinationLocation, destinationName, explicitDestinationNode, finalDestination) != role)
                        continue;

                    SearchResult result = Search(graph, startAnchor, destinationAnchor, finalDestination, allowUnverifiedEdges);
                    if (result != null && result.Nodes != null && result.Nodes.Count > 0)
                        return true;
                }
            }

            return false;
        }

        private static void PopulateRefusalDetail(AIGMNavRoutePlan plan, AIGMNavGraph graph, Point3D currentPoint, Point3D finalDestination, string destinationName, AIGMNavigationLocation destinationLocation, AIGMNavNode explicitDestinationNode)
        {
            if (plan == null || graph == null)
                return;

            plan.DestinationNodeLoaded = explicitDestinationNode != null;
            plan.AliasMatched = !String.IsNullOrWhiteSpace(plan.DestinationAliasesMatched) && !String.Equals(plan.DestinationAliasesMatched, "none", StringComparison.OrdinalIgnoreCase);
            plan.DestinationEdgeCount = explicitDestinationNode != null ? graph.GetOutgoing(explicitDestinationNode, true).Count : 0;

            List<AIGMNavNode> startAny = BuildStartAnchors(graph, currentPoint, plan.Map, true);
            List<AIGMNavNode> destAny = BuildDestinationAnchors(graph, finalDestination, plan.Map, destinationName, destinationLocation, explicitDestinationNode);
            List<AIGMNavNode> startCertified = BuildStartAnchors(graph, currentPoint, plan.Map, false);
            List<AIGMNavNode> destCertified = destAny;

            plan.PathExistsIgnoringCertification = HasAnyPath(graph, startAny, destAny, finalDestination, true);
            plan.PathExistsWithCertifiedEdges = HasAnyPath(graph, startCertified, destCertified, finalDestination, false);
            plan.BlockedByUnverifiedEdges = plan.PathExistsIgnoringCertification && !plan.PathExistsWithCertifiedEdges;
            plan.BlockedByNeedsIntermediateEdges = HasNeedsIntermediateEdge(graph, startAny, destAny, finalDestination);
            plan.OwnerRefusalMessage = BuildOwnerRefusalMessage(plan, destinationName, destinationLocation, explicitDestinationNode);
        }

        private static bool HasAnyPath(AIGMNavGraph graph, List<AIGMNavNode> starts, List<AIGMNavNode> destinations, Point3D finalDestination, bool allowUnverifiedEdges)
        {
            if (graph == null || starts == null || destinations == null)
                return false;

            for (int s = 0; s < starts.Count; s++)
            {
                AIGMNavNode start = starts[s];
                if (start == null)
                    continue;

                for (int d = 0; d < destinations.Count; d++)
                {
                    AIGMNavNode dest = destinations[d];
                    if (dest == null)
                        continue;

                    SearchResult result = Search(graph, start, dest, finalDestination, allowUnverifiedEdges);
                    if (result != null && result.Nodes != null && result.Nodes.Count > 0)
                        return true;
                }
            }

            return false;
        }

        private static bool HasNeedsIntermediateEdge(AIGMNavGraph graph, List<AIGMNavNode> starts, List<AIGMNavNode> destinations, Point3D finalDestination)
        {
            if (graph == null || starts == null || destinations == null)
                return false;

            for (int s = 0; s < starts.Count; s++)
            {
                AIGMNavNode start = starts[s];
                if (start == null)
                    continue;

                for (int d = 0; d < destinations.Count; d++)
                {
                    AIGMNavNode dest = destinations[d];
                    if (dest == null)
                        continue;

                    if (PathTouchesBlockedStatus(graph, start, dest, finalDestination, AIGMNavEdgeStatus.NeedsIntermediateNode))
                        return true;
                }
            }

            return false;
        }

        private static bool PathTouchesBlockedStatus(AIGMNavGraph graph, AIGMNavNode start, AIGMNavNode destination, Point3D finalDestination, AIGMNavEdgeStatus blockedStatus)
        {
            if (graph == null || start == null || destination == null)
                return false;

            List<AIGMNavEdge> edges = graph.GetOutgoing(start, true);
            for (int i = 0; i < edges.Count; i++)
            {
                AIGMNavEdge edge = edges[i];
                AIGMNavNode next = graph.GetOther(edge, start);
                if (edge == null || next == null)
                    continue;

                if (edge.Status == blockedStatus)
                    return true;
            }

            return false;
        }

        private static string BuildOwnerRefusalMessage(AIGMNavRoutePlan plan, string destinationName, AIGMNavigationLocation destinationLocation, AIGMNavNode explicitDestinationNode)
        {
            string display = !String.IsNullOrWhiteSpace(plan.CanonicalDestinationName) ? plan.CanonicalDestinationName : destinationName;
            if (!plan.AliasMatched)
                return display + " was not matched to a known graph destination.";
            if (!plan.DestinationNodeLoaded)
            {
                if (destinationLocation != null)
                    return display + " is loaded as a location, but not connected to the active waypoint graph.";
                return display + " is not loaded in the active graph.";
            }
            if (explicitDestinationNode != null && !explicitDestinationNode.Enabled)
                return display + " is loaded, but its graph node is disabled.";
            if (plan.DestinationEdgeCount <= 0)
                return display + " is loaded, but its node is disconnected in the active graph.";
            if (plan.BlockedByNeedsIntermediateEdges)
                return display + " is loaded, but the route is blocked by edges marked NeedsIntermediateNode.";
            if (plan.BlockedByUnverifiedEdges)
                return display + " is loaded, but the route from here uses unverified edges. Run edge probes or allow test routing.";
            if (plan.PathExistsIgnoringCertification)
                return display + " is loaded, but the current route is filtered out before certified travel can start.";
            return display + " is loaded, but no connected graph path is available from here.";
        }

        private static DestinationAnchorRole GetDestinationAnchorRole(AIGMNavNode destinationAnchor, AIGMNavigationLocation destinationLocation, string destinationName, AIGMNavNode explicitDestinationNode, Point3D finalDestination)
        {
            if (destinationAnchor == null)
                return DestinationAnchorRole.Generic;

            bool exactDestinationNode = MatchesDestinationNode(destinationAnchor, destinationLocation, destinationName)
                || (explicitDestinationNode != null && String.Equals(explicitDestinationNode.Id, destinationAnchor.Id, StringComparison.OrdinalIgnoreCase));
            if (exactDestinationNode)
                return DestinationAnchorRole.Exact;

            if (IsBankDestination(destinationLocation, destinationName))
            {
                if (NodeMatchesAnyTag(destinationAnchor, new[] { "bank_approach", "bank-approach", "approach" }))
                    return DestinationAnchorRole.Approach;

                if (destinationAnchor.Type == AIGMNavNodeType.BuildingEntrance || destinationAnchor.Type == AIGMNavNodeType.Connector)
                {
                    if (destinationAnchor.Matches("First Bank of Britain") || destinationAnchor.Matches("Brit Bank"))
                        return DestinationAnchorRole.Approach;

                    if (AIGMNavGraph.Distance(destinationAnchor.Location, finalDestination) <= DestinationApproachRadius
                        && (NodeMatchesAnyTag(destinationAnchor, new[] { "bank" }) || HasUsefulApproachTag(destinationAnchor)))
                        return DestinationAnchorRole.Approach;
                }
            }

            return DestinationAnchorRole.Generic;
        }

        private static AnchorCandidateEvaluation EvaluateCandidate(AIGMNavGraph graph, Point3D currentPoint, Point3D finalDestination, string destinationName, AIGMNavigationLocation destinationLocation, AIGMNavNode explicitDestinationNode, AIGMNavNode startAnchor, AIGMNavNode destinationAnchor, bool exactDestinationRouteAvailable, bool approachDestinationRouteAvailable, bool allowUnverifiedEdges)
        {
            AnchorCandidateEvaluation candidate = new AnchorCandidateEvaluation();
            candidate.StartAnchor = startAnchor;
            candidate.DestinationAnchor = destinationAnchor;
            candidate.DestinationRole = GetDestinationAnchorRole(destinationAnchor, destinationLocation, destinationName, explicitDestinationNode, finalDestination);
            candidate.CurrentToFinal = AIGMNavGraph.Distance(currentPoint, finalDestination);
            candidate.StartToFinal = startAnchor != null ? AIGMNavGraph.Distance(startAnchor.Location, finalDestination) : Int32.MaxValue;
            candidate.ApproachCost = startAnchor != null ? AIGMNavGraph.Distance(currentPoint, startAnchor.Location) : 0.0;
            candidate.FinalApproachCost = destinationAnchor != null ? AIGMNavGraph.Distance(destinationAnchor.Location, finalDestination) : 0.0;
            candidate.HeadingAngle = startAnchor != null ? ComputeHeadingAngle(currentPoint, startAnchor.Location, finalDestination) : 0.0;
            candidate.RejectReason = String.Empty;

            if (startAnchor == null || destinationAnchor == null)
                return RejectCandidate(candidate, "invalid_stand_target");

            if (!CanStandAt(startAnchor.Map, startAnchor.Location) || !CanStandAt(destinationAnchor.Map, destinationAnchor.Location))
                return RejectCandidate(candidate, "invalid_stand_target");

            if (IsBankDestination(destinationLocation, destinationName) && NodeMatchesAnyTag(startAnchor, new[] { "graveyard", "cemetery" }) && candidate.StartToFinal > candidate.CurrentToFinal + 8.0)
                return RejectCandidate(candidate, "wrong_direction");

            SearchResult result = Search(graph, startAnchor, destinationAnchor, finalDestination, allowUnverifiedEdges);
            if (result == null || result.Nodes == null || result.Nodes.Count == 0)
                return RejectCandidate(candidate, "no_graph_path");

            if (IsBankDestination(destinationLocation, destinationName))
            {
                if (exactDestinationRouteAvailable && candidate.DestinationRole != DestinationAnchorRole.Exact)
                    return RejectCandidate(candidate, "semantic_mismatch");

                if (!exactDestinationRouteAvailable && approachDestinationRouteAvailable && candidate.DestinationRole == DestinationAnchorRole.Generic)
                    return RejectCandidate(candidate, "semantic_mismatch");
            }

            ComputeRouteCosts(graph, result.Nodes, out candidate.GraphCost, out candidate.FailedEdgePenalty);
            candidate.BacktrackPenalty = Math.Max(0.0, candidate.StartToFinal - candidate.CurrentToFinal) * BacktrackPenaltyFactor;
            candidate.WrongDirectionPenalty = ComputeWrongDirectionPenalty(currentPoint, startAnchor.Location, finalDestination, candidate.StartToFinal, candidate.CurrentToFinal, candidate.HeadingAngle);
            candidate.SemanticPenalty = ComputeSemanticPenalty(destinationLocation, destinationName, explicitDestinationNode, destinationAnchor, finalDestination, candidate.FinalApproachCost);
            candidate.DestinationCollapsePenalty = ComputeDestinationCollapsePenalty(startAnchor, destinationAnchor, destinationLocation, destinationName, explicitDestinationNode, result.Nodes, candidate.ApproachCost);
            candidate.RouteEndsAtDestinationNode = candidate.DestinationRole == DestinationAnchorRole.Exact && candidate.FinalApproachCost <= Math.Max(2, destinationAnchor.ArrivalRadius);

            if (candidate.SemanticPenalty >= 500.0)
                return RejectCandidate(candidate, "semantic_mismatch");

            candidate.Route = result;
            candidate.TotalScore = candidate.ApproachCost
                + candidate.GraphCost
                + candidate.FinalApproachCost
                + candidate.WrongDirectionPenalty
                + candidate.BacktrackPenalty
                + candidate.SemanticPenalty
                + candidate.FailedEdgePenalty
                + candidate.DestinationCollapsePenalty;
            return candidate;
        }

        private static PartialRouteEvaluation EvaluatePartialRoute(string routeId, AIGMNavGraph graph, Point3D currentPoint, Point3D finalDestination, string destinationName, AIGMNavigationLocation destinationLocation, AIGMNavNode explicitDestinationNode, List<AIGMNavNode> startAnchors, bool allowUnverifiedEdges)
        {
            if (graph == null || startAnchors == null || startAnchors.Count == 0)
                return null;

            PartialRouteEvaluation best = null;
            for (int s = 0; s < startAnchors.Count; s++)
            {
                AIGMNavNode startAnchor = startAnchors[s];
                if (startAnchor == null || !CanStandAt(startAnchor.Map, startAnchor.Location))
                    continue;

                for (int i = 0; i < graph.Nodes.Count; i++)
                {
                    PartialRouteEvaluation candidate = EvaluatePartialCandidate(routeId, graph, currentPoint, finalDestination, destinationName, destinationLocation, explicitDestinationNode, startAnchor, graph.Nodes[i], allowUnverifiedEdges);
                    LogPartialCandidate(candidate);
                    if (candidate == null || candidate.Rejected)
                        continue;

                    if (best == null || candidate.TotalScore < best.TotalScore)
                        best = candidate;
                }
            }

            return best;
        }

        private static PartialRouteEvaluation EvaluatePartialCandidate(string routeId, AIGMNavGraph graph, Point3D currentPoint, Point3D finalDestination, string destinationName, AIGMNavigationLocation destinationLocation, AIGMNavNode explicitDestinationNode, AIGMNavNode startAnchor, AIGMNavNode frontier, bool allowUnverifiedEdges)
        {
            PartialRouteEvaluation candidate = new PartialRouteEvaluation();
            candidate.RouteId = routeId;
            candidate.DestinationName = destinationName;
            candidate.StartAnchor = startAnchor;
            candidate.FrontierNode = frontier;
            candidate.CurrentToFinal = AIGMNavGraph.Distance(currentPoint, finalDestination);
            candidate.StartToFinal = startAnchor != null ? AIGMNavGraph.Distance(startAnchor.Location, finalDestination) : Int32.MaxValue;
            candidate.ApproachCost = startAnchor != null ? AIGMNavGraph.Distance(currentPoint, startAnchor.Location) : 0.0;
            candidate.HeadingAngle = startAnchor != null ? ComputeHeadingAngle(currentPoint, startAnchor.Location, finalDestination) : 0.0;
            candidate.FinalApproachReason = String.Empty;
            candidate.RejectReason = String.Empty;

            if (startAnchor == null || frontier == null)
                return RejectPartialCandidate(candidate, "invalid_stand_target");

            if (!frontier.Enabled || frontier.Map != startAnchor.Map || !CanStandAt(frontier.Map, frontier.Location))
                return RejectPartialCandidate(candidate, "invalid_stand_target");

            if (String.Equals(startAnchor.Id, frontier.Id, StringComparison.OrdinalIgnoreCase) && candidate.ApproachCost > Math.Max(8.0, startAnchor.ArrivalRadius))
                return RejectPartialCandidate(candidate, "no_meaningful_progress");

            if (IsBankDestination(destinationLocation, destinationName) && NodeMatchesAnyTag(frontier, new[] { "graveyard", "cemetery" }))
                return RejectPartialCandidate(candidate, "semantic_mismatch");

            SearchResult result = Search(graph, startAnchor, frontier, finalDestination, allowUnverifiedEdges);
            if (result == null || result.Nodes == null || result.Nodes.Count == 0)
                return RejectPartialCandidate(candidate, "no_graph_path");

            ComputeRouteCosts(graph, result.Nodes, out candidate.GraphCost, out candidate.FailedEdgePenalty);
            candidate.FrontierToFinal = AIGMNavGraph.Distance(frontier.Location, finalDestination);
            candidate.ProgressTiles = candidate.CurrentToFinal - candidate.FrontierToFinal;
            candidate.ProgressRatio = candidate.CurrentToFinal > 0 ? (double)candidate.ProgressTiles / candidate.CurrentToFinal : 0.0;
            candidate.WrongDirectionPenalty = ComputeWrongDirectionPenalty(currentPoint, startAnchor.Location, finalDestination, candidate.StartToFinal, candidate.CurrentToFinal, candidate.HeadingAngle);
            candidate.SemanticScore = ComputeFrontierSemanticScore(destinationLocation, destinationName, explicitDestinationNode, frontier, finalDestination);
            candidate.UnmappedGapTiles = candidate.FrontierToFinal;
            candidate.FinalApproachAllowed = candidate.UnmappedGapTiles <= PartialRouteFinalApproachLimit && CanStandAt(startAnchor.Map, finalDestination);
            candidate.FinalApproachReason = candidate.FinalApproachAllowed ? "final_gap_within_limit" : "unmapped_gap_too_large";

            if (candidate.ProgressTiles <= 0)
                return RejectPartialCandidate(candidate, "wrong_direction");

            if (candidate.WrongDirectionPenalty >= (WrongDirectionBasePenalty * 2.0))
                return RejectPartialCandidate(candidate, "wrong_direction");

            if (candidate.ProgressTiles < PartialRouteProgressTilesMinimum && candidate.ProgressRatio < PartialRouteProgressRatioMinimum)
                return RejectPartialCandidate(candidate, "no_meaningful_progress");

            if (candidate.SemanticScore < -5.0)
                return RejectPartialCandidate(candidate, "semantic_mismatch");

            candidate.Route = result;
            candidate.TotalScore = candidate.ApproachCost
                + candidate.GraphCost
                + candidate.FrontierToFinal
                + candidate.WrongDirectionPenalty
                + candidate.FailedEdgePenalty
                - (candidate.ProgressTiles * 4.0)
                - (candidate.ProgressRatio * 120.0)
                - (candidate.SemanticScore * 15.0);
            return candidate;
        }

        private static void ApplyPartialRoute(AIGMNavRoutePlan plan, Mobile actor, string destinationName, PartialRouteEvaluation partial)
        {
            if (plan == null || partial == null || partial.Route == null || partial.Route.Nodes == null)
                return;

            plan.Success = true;
            plan.Reason = "partial_graph_frontier";
            plan.RouteMode = "PartialGraphRoute";
            plan.IsPartial = true;
            plan.StartAnchor = partial.StartAnchor;
            plan.DestinationAnchor = partial.FrontierNode;
            plan.SelectedDestinationRole = "frontier";
            plan.RouteEndsAtDestinationNode = false;
            plan.TotalCost = partial.ApproachCost + partial.GraphCost + partial.FrontierToFinal;
            plan.SelectedScore = partial.TotalScore;
            plan.FrontierNodeId = partial.FrontierNode != null ? partial.FrontierNode.Id : String.Empty;
            plan.FrontierPoint = partial.FrontierNode != null ? partial.FrontierNode.Location : Point3D.Zero;
            plan.FrontierToFinalDistance = partial.FrontierToFinal;
            plan.CurrentToFinalDistance = partial.CurrentToFinal;
            plan.ProgressTiles = partial.ProgressTiles;
            plan.ProgressRatio = partial.ProgressRatio;
            plan.UnmappedGapTiles = partial.UnmappedGapTiles;
            plan.FinalApproachAllowed = partial.FinalApproachAllowed;
            plan.FinalApproachReason = partial.FinalApproachReason;

            AIGMExecutionLog.Write("AIGM_NAV_ANCHOR_SELECTED routeId={0} selectedStartId={1} selectedDestId={2} selectedScore={3:0.0} selectedRouteIds=\"{4}\" selectedRoutePoints=\"{5}\"",
                SafeLog(plan.RouteId),
                plan.StartAnchor != null ? SafeLog(plan.StartAnchor.Id) : "none",
                plan.DestinationAnchor != null ? SafeLog(plan.DestinationAnchor.Id) : "none",
                plan.SelectedScore,
                SafeLog(FormatNodeIds(partial.Route.Nodes)),
                SafeLog(FormatPointPath(partial.Route.Nodes)));
            AIGMExecutionLog.Write("AIGM_NAV_DEST_ANCHOR_SELECTED routeId={0} selectedDestId={1} selectedDestRole={2} canonicalDestinationId={3} finalVirtualTarget={4} routeEndsAtDestinationNode={5}",
                SafeLog(plan.RouteId),
                plan.DestinationAnchor != null ? SafeLog(plan.DestinationAnchor.Id) : "none",
                SafeLog(plan.SelectedDestinationRole),
                SafeLog(plan.CanonicalDestinationId),
                AIGMNavRoutePlan.FormatPoint(plan.FinalDestination),
                plan.RouteEndsAtDestinationNode);
            AIGMExecutionLog.Write("AIGM_NAV_PARTIAL_ROUTE_SELECTED routeId={0} destination=\"{1}\" frontierNodeId={2} frontierPoint={3} routeIds=\"{4}\" routePoints=\"{5}\" progressTiles={6} progressRatio={7:0.00} unmappedGapTiles={8} finalApproachAllowed={9} finalApproachReason={10}",
                SafeLog(plan.RouteId),
                SafeLog(destinationName),
                SafeLog(plan.FrontierNodeId),
                AIGMNavRoutePlan.FormatPoint(plan.FrontierPoint),
                SafeLog(FormatNodeIds(partial.Route.Nodes)),
                SafeLog(FormatPointPath(partial.Route.Nodes)),
                plan.ProgressTiles,
                plan.ProgressRatio,
                plan.UnmappedGapTiles,
                plan.FinalApproachAllowed,
                SafeLog(plan.FinalApproachReason));
            AIGMExecutionLog.Write("AIGM_NAV_GRAPH_ANCHOR actor={0} companionPoint={1} startAnchorId={2} startAnchorPoint={3} startAnchorDistance={4} destAnchorId={5} destAnchorPoint={6} destAnchorDistance={7}",
                Describe(actor),
                AIGMNavRoutePlan.FormatPoint(actor.Location),
                plan.StartAnchor != null ? SafeLog(plan.StartAnchor.Id) : "none",
                plan.StartAnchor != null ? AIGMNavRoutePlan.FormatPoint(plan.StartAnchor.Location) : "none",
                plan.StartAnchor != null ? AIGMNavGraph.Distance(actor.Location, plan.StartAnchor.Location).ToString() : "-1",
                plan.DestinationAnchor != null ? SafeLog(plan.DestinationAnchor.Id) : "none",
                plan.DestinationAnchor != null ? AIGMNavRoutePlan.FormatPoint(plan.DestinationAnchor.Location) : "none",
                plan.DestinationAnchor != null ? AIGMNavGraph.Distance(plan.FinalDestination, plan.DestinationAnchor.Location).ToString() : "-1");

            for (int i = 0; i < partial.Route.Nodes.Count; i++)
            {
                AIGMNavNode node = partial.Route.Nodes[i];
                if (node == null)
                    continue;

                if (i == 0 && AIGMNavGraph.Distance(actor.Location, node.Location) <= node.ArrivalRadius)
                    continue;

                AIGMNavRouteStep step = new AIGMNavRouteStep();
                step.NodeId = node.Id;
                step.Name = node.Name;
                step.Location = node.Location;
                step.ArrivalRadius = Math.Max(1, node.ArrivalRadius);
                plan.Steps.Add(step);
            }

            if (plan.FinalApproachAllowed)
            {
                AIGMNavRouteStep finalStep = new AIGMNavRouteStep();
                finalStep.NodeId = "virtual_destination";
                finalStep.Name = String.IsNullOrWhiteSpace(destinationName) ? "destination" : destinationName;
                finalStep.Location = plan.FinalDestination;
                finalStep.ArrivalRadius = 3;
                plan.Steps.Add(finalStep);
            }
            else
            {
                AIGMExecutionLog.Write("AIGM_NAV_UNMAPPED_GAP routeId={0} destination=\"{1}\" frontierNodeId={2} frontierToFinalDistance={3} messageSentToOwner=False",
                    SafeLog(plan.RouteId),
                    SafeLog(destinationName),
                    SafeLog(plan.FrontierNodeId),
                    plan.FrontierToFinalDistance);
            }

            ApplyEdgeSummary(plan);
        }

        private static double ComputeFrontierSemanticScore(AIGMNavigationLocation destinationLocation, string destinationName, AIGMNavNode explicitDestinationNode, AIGMNavNode frontier, Point3D finalDestination)
        {
            if (frontier == null)
                return -10.0;

            double score = 0.0;
            if (!String.IsNullOrWhiteSpace(destinationName) && frontier.Matches(destinationName))
                score += 6.0;

            if (destinationLocation != null)
            {
                string normalizedName = AIGMNavNode.Normalize(destinationLocation.DisplayName);
                if (!String.IsNullOrWhiteSpace(frontier.Zone) && AIGMNavNode.Normalize(frontier.Zone).Contains(normalizedName))
                    score += 4.0;
                if (!String.IsNullOrWhiteSpace(frontier.Region) && AIGMNavNode.Normalize(frontier.Region).Contains(normalizedName))
                    score += 3.0;

                string normalizedCategory = AIGMNavigationLocationRegistry.NormalizeForLookup(destinationLocation.Category);
                if (!String.IsNullOrWhiteSpace(normalizedCategory))
                {
                    if (HasTag(frontier.Tags, normalizedCategory))
                        score += 2.0;
                    if (normalizedCategory == "landmark" || normalizedCategory == "road")
                    {
                        if (HasUsefulApproachTag(frontier))
                            score += 2.0;
                    }
                }
            }

            if (explicitDestinationNode != null && frontier.Map == explicitDestinationNode.Map)
            {
                if (!String.IsNullOrWhiteSpace(frontier.Zone) && !String.IsNullOrWhiteSpace(explicitDestinationNode.Zone)
                    && String.Equals(AIGMNavNode.Normalize(frontier.Zone), AIGMNavNode.Normalize(explicitDestinationNode.Zone), StringComparison.OrdinalIgnoreCase))
                    score += 3.0;
                if (!String.IsNullOrWhiteSpace(frontier.Region) && !String.IsNullOrWhiteSpace(explicitDestinationNode.Region)
                    && String.Equals(AIGMNavNode.Normalize(frontier.Region), AIGMNavNode.Normalize(explicitDestinationNode.Region), StringComparison.OrdinalIgnoreCase))
                    score += 2.0;
            }

            if (NodeMatchesAnyTag(frontier, new[] { "graveyard", "cemetery", "hazard" }))
                score -= 8.0;

            if (AIGMNavGraph.Distance(frontier.Location, finalDestination) <= 32)
                score += 3.0;

            return score;
        }

        private static void LogPartialCandidate(PartialRouteEvaluation candidate)
        {
            if (candidate == null)
                return;

            AIGMExecutionLog.Write("AIGM_NAV_PARTIAL_ROUTE_CANDIDATE routeId={0} destination=\"{1}\" candidateFrontierId={2} candidateFrontierPoint={3} currentToFinal={4} frontierToFinal={5} progressTiles={6} progressRatio={7:0.00} graphCost={8:0.0} headingAngle={9:0.0} semanticScore={10:0.0} totalScore={11:0.0} rejected={12} rejectReason={13}",
                SafeLog(candidate.RouteId),
                SafeLog(candidate.DestinationName),
                candidate.FrontierNode != null ? SafeLog(candidate.FrontierNode.Id) : "none",
                candidate.FrontierNode != null ? AIGMNavRoutePlan.FormatPoint(candidate.FrontierNode.Location) : "none",
                candidate.CurrentToFinal,
                candidate.FrontierToFinal,
                candidate.ProgressTiles,
                candidate.ProgressRatio,
                candidate.GraphCost,
                candidate.HeadingAngle,
                candidate.SemanticScore,
                candidate.TotalScore,
                candidate.Rejected,
                SafeLog(candidate.RejectReason));
        }

        private static PartialRouteEvaluation RejectPartialCandidate(PartialRouteEvaluation candidate, string reason)
        {
            candidate.Rejected = true;
            candidate.RejectReason = reason;
            candidate.TotalScore = Double.MaxValue;
            return candidate;
        }

        private static void ComputeRouteCosts(AIGMNavGraph graph, List<AIGMNavNode> nodes, out double graphCost, out double failedEdgePenalty)
        {
            graphCost = 0.0;
            failedEdgePenalty = 0.0;
            if (graph == null || nodes == null || nodes.Count < 2)
                return;

            for (int i = 1; i < nodes.Count; i++)
            {
                AIGMNavNode from = nodes[i - 1];
                AIGMNavNode to = nodes[i];
                AIGMNavEdge edge = graph.FindEdge(from != null ? from.Id : null, to != null ? to.Id : null);
                if (edge == null || from == null || to == null)
                    continue;

                graphCost += EdgeCost(edge, from, to, false);
                failedEdgePenalty += Math.Max(0, edge.FailureScore) * 75.0;
            }
        }

        private static double EdgeCost(AIGMNavEdge edge, AIGMNavNode from, AIGMNavNode to, bool includeFailurePenalty)
        {
            double cost = edge.Cost > 0.0 ? edge.Cost : AIGMNavGraph.Distance(from.Location, to.Location);
            if (includeFailurePenalty)
                cost += Math.Max(0, edge.FailureScore) * 75.0;
            if (HasTag(edge.Tags, "hazard") || HasTag(to.Tags, "hazard"))
                cost += 250.0;
            if (HasTag(edge.Tags, "road") || HasTag(to.Tags, "road"))
                cost -= Math.Min(15.0, cost * 0.05);
            return Math.Max(1.0, cost);
        }

        private static double ComputeWrongDirectionPenalty(Point3D currentPoint, Point3D startPoint, Point3D finalDestination, double startToFinal, double currentToFinal, double headingAngle)
        {
            double penalty = 0.0;
            if (startToFinal > currentToFinal + 5.0)
                penalty += WrongDirectionBasePenalty;
            if (IsBehind(currentPoint, startPoint, finalDestination))
                penalty += WrongDirectionBasePenalty;
            if (headingAngle > 90.0)
                penalty += WrongDirectionBasePenalty + Math.Min(45.0, headingAngle - 90.0);
            return penalty;
        }

        private static double ComputeDestinationCollapsePenalty(AIGMNavNode startAnchor, AIGMNavNode destinationAnchor, AIGMNavigationLocation destinationLocation, string destinationName, AIGMNavNode explicitDestinationNode, List<AIGMNavNode> routeNodes, double approachCost)
        {
            if (startAnchor == null || destinationAnchor == null || routeNodes == null || routeNodes.Count == 0)
                return 0.0;

            bool exactDestinationNode = MatchesDestinationNode(destinationAnchor, destinationLocation, destinationName)
                || (explicitDestinationNode != null && String.Equals(explicitDestinationNode.Id, destinationAnchor.Id, StringComparison.OrdinalIgnoreCase));
            if (!exactDestinationNode || routeNodes.Count > 1)
                return 0.0;

            int threshold = Math.Max(destinationAnchor.ArrivalRadius + 8, 24);
            if (approachCost <= threshold)
                return 0.0;

            return DestinationCollapsePenalty;
        }

        private static double ComputeSemanticPenalty(AIGMNavigationLocation destinationLocation, string destinationName, AIGMNavNode explicitDestinationNode, AIGMNavNode destinationAnchor, Point3D finalDestination, double finalApproachCost)
        {
            if (destinationAnchor == null)
                return 500.0;

            bool exactDestinationNode = MatchesDestinationNode(destinationAnchor, destinationLocation, destinationName)
                || (explicitDestinationNode != null && String.Equals(explicitDestinationNode.Id, destinationAnchor.Id, StringComparison.OrdinalIgnoreCase));
            if (exactDestinationNode)
                return 0.0;

            if (IsBankDestination(destinationLocation, destinationName))
            {
                if (NodeMatchesAnyTag(destinationAnchor, new[] { "graveyard", "cemetery" }))
                    return 500.0;

                if (destinationAnchor.Type == AIGMNavNodeType.Bank || NodeMatchesAnyTag(destinationAnchor, new[] { "bank" }))
                    return 0.0;

                if (GetDestinationAnchorRole(destinationAnchor, destinationLocation, destinationName, explicitDestinationNode, finalDestination) == DestinationAnchorRole.Approach)
                    return LooseApproachPenalty + (Math.Max(0.0, finalApproachCost - 18.0) * 2.0);

                return 180.0 + GenericBankConnectorPenalty + Math.Max(0.0, finalApproachCost - DestinationApproachRadius) * 3.0;
            }

            int maxApproach = AllowedApproachRadius(destinationLocation);
            if (finalApproachCost <= maxApproach)
                return 0.0;

            return (finalApproachCost - maxApproach) * 2.0;
        }

        private static int AllowedApproachRadius(AIGMNavigationLocation destinationLocation)
        {
            if (destinationLocation == null || String.IsNullOrWhiteSpace(destinationLocation.Category))
                return 40;

            string category = AIGMNavigationLocationRegistry.NormalizeForLookup(destinationLocation.Category);
            if (category == "bank" || category == "house" || category == "roadsign")
                return 48;
            if (category == "road" || category == "landmark")
                return 32;
            return 40;
        }

        private static AIGMNavigationLocation ResolveDestinationLocation(string destinationName, Map map, Point3D finalDestination)
        {
            AIGMNavigationLocation location;
            if (AIGMNavigationLocationRegistry.TryResolve(destinationName, map, out location))
                return location;

            foreach (AIGMNavigationLocation candidate in AIGMNavigationLocationRegistry.AllLocations)
            {
                if (candidate == null || candidate.Map != map)
                    continue;

                if (AIGMNavigationLocationRegistry.GetDistance(candidate.Point, finalDestination) <= AIGMNavigationDestinationResolver.GetArrivalRadius(candidate))
                    return candidate;
            }

            return null;
        }

        private static AIGMNavNode ResolveExplicitDestinationNode(AIGMNavGraph graph, string destinationName, Point3D finalDestination, Map map)
        {
            if (graph == null)
                return null;

            AIGMNavNode node = graph.FindNode(destinationName);
            if (node != null && node.Map == map)
                return node;

            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                node = graph.Nodes[i];
                if (node == null || !node.Enabled || node.Map != map)
                    continue;

                if (AIGMNavGraph.Distance(node.Location, finalDestination) <= Math.Max(2, node.ArrivalRadius))
                    return node;
            }

            return null;
        }

        private static bool MatchesDestinationNode(AIGMNavNode node, AIGMNavigationLocation location, string destinationName)
        {
            if (node == null)
                return false;

            if (!String.IsNullOrWhiteSpace(destinationName) && node.Matches(destinationName))
                return true;

            if (location == null)
                return false;

            if (node.Matches(location.DisplayName) || node.Matches(location.Key))
                return true;

            for (int i = 0; location.Aliases != null && i < location.Aliases.Length; i++)
            {
                if (node.Matches(location.Aliases[i]))
                    return true;
            }

            return false;
        }

        private static bool NodeHasUsableOutgoingEdges(AIGMNavGraph graph, AIGMNavNode node, bool allowUnverifiedEdges)
        {
            return graph != null && node != null && graph.GetOutgoing(node, allowUnverifiedEdges).Count > 0;
        }

        private static void ApplyEdgeSummary(AIGMNavRoutePlan plan)
        {
            if (plan == null)
                return;

            plan.EdgeStatuses = "none";
            plan.CertifiedEdgeCount = 0;
            plan.UnverifiedEdgeCount = 0;
            plan.BlockedEdgeCount = 0;

            List<string> statuses = new List<string>();
            AIGMNavGraph graph = AIGMNavGraphStore.Graph;
            if (graph == null || plan.Steps == null || plan.Steps.Count < 2)
                return;

            for (int i = 1; i < plan.Steps.Count; i++)
            {
                AIGMNavRouteStep fromStep = plan.Steps[i - 1];
                AIGMNavRouteStep toStep = plan.Steps[i];
                if (fromStep == null || toStep == null)
                    continue;

                if (String.Equals(fromStep.NodeId, "virtual_destination", StringComparison.OrdinalIgnoreCase)
                    || String.Equals(toStep.NodeId, "virtual_destination", StringComparison.OrdinalIgnoreCase))
                    continue;

                AIGMNavEdge edge = graph.FindEdge(fromStep.NodeId, toStep.NodeId);
                string status = edge != null ? edge.EffectiveStatusName : "Missing";
                if (String.Equals(status, "Certified", StringComparison.OrdinalIgnoreCase) || String.Equals(status, "OneWay", StringComparison.OrdinalIgnoreCase))
                    plan.CertifiedEdgeCount++;
                else if (String.Equals(status, "Unverified", StringComparison.OrdinalIgnoreCase))
                    plan.UnverifiedEdgeCount++;
                else
                    plan.BlockedEdgeCount++;

                statuses.Add(fromStep.NodeId + "->" + toStep.NodeId + "=" + status);
            }

            if (statuses.Count > 0)
                plan.EdgeStatuses = String.Join("|", statuses.ToArray());
        }

        private static bool CanStandAt(Map map, Point3D point)
        {
            return map != null && map.CanFit(point.X, point.Y, point.Z, 16, false, false);
        }

        private static void AddUniqueNode(List<AIGMNavNode> nodes, AIGMNavNode node)
        {
            if (nodes == null || node == null)
                return;

            for (int i = 0; i < nodes.Count; i++)
            {
                if (String.Equals(nodes[i].Id, node.Id, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            nodes.Add(node);
        }

        private static bool IsBankDestination(AIGMNavigationLocation location, string destinationName)
        {
            if (location != null && AIGMNavigationLocationRegistry.NormalizeForLookup(location.Category) == "bank")
                return true;

            string normalized = AIGMNavNode.Normalize(destinationName);
            return normalized.Contains("bank");
        }

        private static bool HasUsefulApproachTag(AIGMNavNode node)
        {
            if (node == null)
                return false;

            return NodeMatchesAnyTag(node, new[] { "road", "approach", "connector", "town", "britain", "landmark", "guard" })
                || node.Type == AIGMNavNodeType.Road
                || node.Type == AIGMNavNodeType.Connector
                || node.Type == AIGMNavNodeType.Landmark
                || node.Type == AIGMNavNodeType.BuildingEntrance;
        }

        private static bool NodeMatchesAnyTag(AIGMNavNode node, string[] tags)
        {
            if (node == null)
                return false;

            for (int i = 0; tags != null && i < tags.Length; i++)
            {
                string wanted = tags[i];
                if (HasTag(node.Tags, wanted))
                    return true;

                string normalizedWanted = AIGMNavNode.Normalize(wanted);
                if (!String.IsNullOrWhiteSpace(node.Region) && AIGMNavNode.Normalize(node.Region).Contains(normalizedWanted))
                    return true;
                if (!String.IsNullOrWhiteSpace(node.Zone) && AIGMNavNode.Normalize(node.Zone).Contains(normalizedWanted))
                    return true;
                if (!String.IsNullOrWhiteSpace(node.Name) && AIGMNavNode.Normalize(node.Name).Contains(normalizedWanted))
                    return true;
            }

            return false;
        }

        private static bool IsBehind(Point3D currentPoint, Point3D startPoint, Point3D finalDestination)
        {
            int towardX = finalDestination.X - currentPoint.X;
            int towardY = finalDestination.Y - currentPoint.Y;
            int startX = startPoint.X - currentPoint.X;
            int startY = startPoint.Y - currentPoint.Y;
            return (towardX * startX) + (towardY * startY) < 0;
        }

        private static double ComputeHeadingAngle(Point3D currentPoint, Point3D startPoint, Point3D finalDestination)
        {
            double ax = startPoint.X - currentPoint.X;
            double ay = startPoint.Y - currentPoint.Y;
            double bx = finalDestination.X - currentPoint.X;
            double by = finalDestination.Y - currentPoint.Y;
            double magA = Math.Sqrt((ax * ax) + (ay * ay));
            double magB = Math.Sqrt((bx * bx) + (by * by));
            if (magA <= 0.001 || magB <= 0.001)
                return 0.0;

            double cosine = ((ax * bx) + (ay * by)) / (magA * magB);
            cosine = Math.Max(-1.0, Math.Min(1.0, cosine));
            return Math.Acos(cosine) * (180.0 / Math.PI);
        }

        private static int FindBestOpen(List<AIGMNavNode> open, Dictionary<string, double> gScore, Point3D finalDestination)
        {
            int bestIndex = 0;
            double bestScore = Double.MaxValue;
            for (int i = 0; i < open.Count; i++)
            {
                double g;
                if (!gScore.TryGetValue(open[i].Id, out g))
                    g = Double.MaxValue / 2.0;

                double f = g + AIGMNavGraph.Distance(open[i].Location, finalDestination);
                if (f < bestScore)
                {
                    bestIndex = i;
                    bestScore = f;
                }
            }

            return bestIndex;
        }

        private static List<AIGMNavNode> Reconstruct(string id, Dictionary<string, string> parent, Dictionary<string, AIGMNavNode> visited)
        {
            List<AIGMNavNode> reversed = new List<AIGMNavNode>();
            string current = id;
            while (!String.IsNullOrWhiteSpace(current))
            {
                AIGMNavNode node;
                if (!visited.TryGetValue(current, out node))
                    break;
                reversed.Add(node);
                parent.TryGetValue(current, out current);
            }

            reversed.Reverse();
            return reversed;
        }

        private static bool Contains(List<AIGMNavNode> nodes, string id)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                if (String.Equals(nodes[i].Id, id, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static void LogAnchorCandidate(string routeId, AnchorCandidateEvaluation candidate)
        {
            if (candidate == null)
                return;

            AIGMExecutionLog.Write("AIGM_NAV_ANCHOR_CANDIDATE routeId={0} candidateStartId={1} candidateStartPoint={2} candidateDestId={3} candidateDestPoint={4} approachCost={5:0.0} graphCost={6:0.0} finalApproachCost={7:0.0} currentToFinal={8} startToFinal={9} headingAngle={10:0.0} wrongDirectionPenalty={11:0.0} backtrackPenalty={12:0.0} semanticPenalty={13:0.0} totalScore={14:0.0} rejected={15} rejectReason={16}",
                SafeLog(routeId),
                candidate.StartAnchor != null ? SafeLog(candidate.StartAnchor.Id) : "none",
                candidate.StartAnchor != null ? AIGMNavRoutePlan.FormatPoint(candidate.StartAnchor.Location) : "none",
                candidate.DestinationAnchor != null ? SafeLog(candidate.DestinationAnchor.Id) : "none",
                candidate.DestinationAnchor != null ? AIGMNavRoutePlan.FormatPoint(candidate.DestinationAnchor.Location) : "none",
                candidate.ApproachCost,
                candidate.GraphCost,
                candidate.FinalApproachCost,
                candidate.CurrentToFinal,
                candidate.StartToFinal,
                candidate.HeadingAngle,
                candidate.WrongDirectionPenalty,
                candidate.BacktrackPenalty,
                candidate.SemanticPenalty + candidate.DestinationCollapsePenalty + candidate.FailedEdgePenalty,
                candidate.TotalScore,
                candidate.Rejected,
                SafeLog(candidate.RejectReason));
            AIGMExecutionLog.Write("AIGM_NAV_DEST_ANCHOR_CANDIDATE routeId={0} candidateDestId={1} candidateDestRole={2} semanticMatchScore={3:0.0} finalApproachCost={4:0.0} rejected={5} rejectReason={6}",
                SafeLog(routeId),
                candidate.DestinationAnchor != null ? SafeLog(candidate.DestinationAnchor.Id) : "none",
                SafeLog(candidate.DestinationRole.ToString().ToLowerInvariant()),
                Math.Max(0.0, 500.0 - candidate.SemanticPenalty),
                candidate.FinalApproachCost,
                candidate.Rejected,
                SafeLog(candidate.RejectReason));
        }

        private static void LogAnchorRejected(string routeId, AnchorCandidateEvaluation candidate)
        {
            if (candidate == null)
                return;

            AIGMExecutionLog.Write("AIGM_NAV_ANCHOR_REJECTED routeId={0} candidateStartId={1} candidateDestId={2} reason={3}",
                SafeLog(routeId),
                candidate.StartAnchor != null ? SafeLog(candidate.StartAnchor.Id) : "none",
                candidate.DestinationAnchor != null ? SafeLog(candidate.DestinationAnchor.Id) : "none",
                SafeLog(candidate.RejectReason));
        }

        private static AnchorCandidateEvaluation RejectCandidate(AnchorCandidateEvaluation candidate, string reason)
        {
            candidate.Rejected = true;
            candidate.RejectReason = reason;
            candidate.TotalScore = Double.MaxValue;
            return candidate;
        }

        private static string FormatNodeIds(List<AIGMNavNode> nodes)
        {
            if (nodes == null || nodes.Count == 0)
                return "none";

            List<string> ids = new List<string>();
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] != null)
                    ids.Add(nodes[i].Id);
            }

            return ids.Count == 0 ? "none" : String.Join("->", ids.ToArray());
        }

        private static string FormatPointPath(List<AIGMNavNode> nodes)
        {
            if (nodes == null || nodes.Count == 0)
                return "none";

            List<string> points = new List<string>();
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] != null)
                    points.Add(AIGMNavRoutePlan.FormatPoint(nodes[i].Location));
            }

            return points.Count == 0 ? "none" : String.Join("->", points.ToArray());
        }

        private static bool HasTag(string[] tags, string wanted)
        {
            for (int i = 0; tags != null && i < tags.Length; i++)
            {
                if (String.Equals(tags[i], wanted, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static string Describe(Mobile mobile)
        {
            return mobile == null ? "missing" : ((mobile.Name ?? mobile.GetType().Name) + "[" + mobile.Serial + "]");
        }

        private static string SafeLog(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return String.Empty;
            value = value.Replace('"', '\'').Replace('\r', ' ').Replace('\n', ' ');
            return value.Length > 220 ? value.Substring(0, 220) : value;
        }

        private sealed class SearchResult
        {
            public List<AIGMNavNode> Nodes;
            public double Cost;
        }

        private sealed class AnchorCandidateEvaluation
        {
            public AIGMNavNode StartAnchor;
            public AIGMNavNode DestinationAnchor;
            public DestinationAnchorRole DestinationRole;
            public SearchResult Route;
            public double ApproachCost;
            public double GraphCost;
            public double FinalApproachCost;
            public double CurrentToFinal;
            public double StartToFinal;
            public double HeadingAngle;
            public double WrongDirectionPenalty;
            public double BacktrackPenalty;
            public double SemanticPenalty;
            public double FailedEdgePenalty;
            public double DestinationCollapsePenalty;
            public double TotalScore;
            public bool RouteEndsAtDestinationNode;
            public bool Rejected;
            public string RejectReason;
        }

        private sealed class DestinationSemanticResolution
        {
            public string RawDestination;
            public string CanonicalDestinationId;
            public string CanonicalName;
            public string DestinationKind;
            public string AliasesMatched;
            public bool ExplicitGraphNodeFound;
        }

        private sealed class PartialRouteEvaluation
        {
            public string RouteId;
            public string DestinationName;
            public AIGMNavNode StartAnchor;
            public AIGMNavNode FrontierNode;
            public SearchResult Route;
            public double ApproachCost;
            public double GraphCost;
            public double HeadingAngle;
            public double WrongDirectionPenalty;
            public double FailedEdgePenalty;
            public double SemanticScore;
            public double TotalScore;
            public double StartToFinal;
            public int CurrentToFinal;
            public int FrontierToFinal;
            public int ProgressTiles;
            public double ProgressRatio;
            public int UnmappedGapTiles;
            public bool FinalApproachAllowed;
            public string FinalApproachReason;
            public bool Rejected;
            public string RejectReason;
        }

        private enum DestinationAnchorRole
        {
            Exact,
            Approach,
            Generic
        }
    }
}
