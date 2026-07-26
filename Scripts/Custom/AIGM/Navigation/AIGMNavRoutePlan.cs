using System;
using System.Collections.Generic;

using Server;

namespace Server.Custom.AIGM.Navigation
{
    public sealed class AIGMNavRouteStep
    {
        public string NodeId;
        public string Name;
        public Point3D Location;
        public int ArrivalRadius;
    }

    public sealed class AIGMNavRoutePlan
    {
        public string RouteId;
        public bool Success;
        public string Reason;
        public bool SafeDirectFallback;
        public Point3D Start;
        public Point3D FinalDestination;
        public Map Map;
        public string RawDestination;
        public string CanonicalDestinationId;
        public string CanonicalDestinationName;
        public string DestinationKind;
        public string DestinationAliasesMatched;
        public bool ExplicitDestinationGraphNodeFound;
        public string RouteMode;
        public bool IsPartial;
        public AIGMNavNode StartAnchor;
        public AIGMNavNode DestinationAnchor;
        public string SelectedDestinationRole;
        public bool RouteEndsAtDestinationNode;
        public string FrontierNodeId;
        public Point3D FrontierPoint;
        public int FrontierToFinalDistance;
        public int CurrentToFinalDistance;
        public int ProgressTiles;
        public double ProgressRatio;
        public int UnmappedGapTiles;
        public bool FinalApproachAllowed;
        public string FinalApproachReason;
        public List<AIGMNavRouteStep> Steps;
        public double TotalCost;
        public double SelectedScore;
        public int StartCandidateCount;
        public int DestinationCandidateCount;
        public int CandidatePairCount;
        public int RejectedCandidateCount;
        public int RejectedWrongDirectionCount;
        public int RejectedSemanticCount;
        public int RejectedNoGraphPathCount;
        public string EdgeStatuses;
        public int CertifiedEdgeCount;
        public int UnverifiedEdgeCount;
        public int BlockedEdgeCount;
        public bool DestinationNodeLoaded;
        public bool AliasMatched;
        public int DestinationEdgeCount;
        public bool PathExistsIgnoringCertification;
        public bool PathExistsWithCertifiedEdges;
        public bool BlockedByUnverifiedEdges;
        public bool BlockedByNeedsIntermediateEdges;
        public string OwnerRefusalMessage;

        public AIGMNavRoutePlan()
        {
            RouteId = "route_" + DateTime.UtcNow.Ticks.ToString();
            Steps = new List<AIGMNavRouteStep>();
            Reason = "not_planned";
            RouteMode = "NoUsefulGraphRoute";
            SafeDirectFallback = false;
            FinalApproachReason = String.Empty;
            EdgeStatuses = "none";
            OwnerRefusalMessage = String.Empty;
        }

        public string FormatNodePath()
        {
            if (Steps == null || Steps.Count == 0)
                return "none";

            List<string> parts = new List<string>();
            for (int i = 0; i < Steps.Count; i++)
                parts.Add(Steps[i].Name + "@" + FormatPoint(Steps[i].Location));

            return string.Join(" -> ", parts.ToArray());
        }

        public string FormatNodeIds()
        {
            if (Steps == null || Steps.Count == 0)
                return "none";

            List<string> parts = new List<string>();
            for (int i = 0; i < Steps.Count; i++)
                parts.Add(Steps[i].NodeId);

            return string.Join("->", parts.ToArray());
        }

        public string FormatPointPath()
        {
            if (Steps == null || Steps.Count == 0)
                return "none";

            List<string> parts = new List<string>();
            for (int i = 0; i < Steps.Count; i++)
                parts.Add(FormatPoint(Steps[i].Location));

            return string.Join("->", parts.ToArray());
        }

        public static string FormatPoint(Point3D point)
        {
            return point.X + "," + point.Y + "," + point.Z;
        }
    }
}
