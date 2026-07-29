using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

using Server;
using Server.Custom.AIGM.Navigation;

namespace Server.Custom.AIGM
{
    public sealed class AIGMLegacyWaypointRoute
    {
        public List<Point3D> Waypoints = new List<Point3D>();
        public List<string> WaypointIds = new List<string>();
        public List<string> WaypointLabels = new List<string>();
        public string Source = String.Empty;
        public int CompletionRadius;
        public bool PreserveExactWaypoints;
        public bool StopAtLastWaypoint;
    }

    [DataContract]
    internal sealed class AIGMLegacyRouteFile
    {
        [DataMember(Name = "routes")]
        public AIGMLegacyRouteEntry[] Routes { get; set; }
    }

    [DataContract]
    internal sealed class AIGMLegacyRouteEntry
    {
        [DataMember(Name = "destination")]
        public string Destination { get; set; }

        [DataMember(Name = "aliases")]
        public string[] Aliases { get; set; }

        [DataMember(Name = "map")]
        public string MapName { get; set; }

        [DataMember(Name = "waypoints")]
        public string[] Waypoints { get; set; }
    }

    public static class AIGMLegacyTravelWaypointService
    {
        private static readonly string LegacyRoutesPath = Path.Combine(AIGMNavGraphStore.DataDirectory, "legacy-routes.json");

        public static AIGMLegacyWaypointRoute Resolve(Mobile actor, Point3D finalDestination, Map map, string destinationName)
        {
            Point3D start = actor != null ? actor.Location : Point3D.Zero;
            AIGMNavGraph graph = AIGMNavGraphStore.Graph;
            string source = String.Format("{0};nodes={1};jsonNodes={2};jsonEdges={3}",
                AIGMNavGraphStore.SourceLabel,
                graph != null && graph.Nodes != null ? graph.Nodes.Count : 0,
                AIGMNavGraphStore.JsonNodeCount,
                AIGMNavGraphStore.JsonEdgeCount);

            AIGMExecutionLog.Write(
                "AIGM_LEGACY_WAYPOINT_AUDIT source={0} loaded={1} destination=\"{2}\"",
                SafeLog(source),
                graph != null && graph.Nodes != null && graph.Nodes.Count > 0,
                SafeLog(destinationName));

            if (graph == null || graph.Nodes == null || graph.Nodes.Count == 0)
            {
                LogDecision(destinationName, "disabled", "no_nodes_loaded", null, null);
                return null;
            }

            if (map == null)
            {
                LogDecision(destinationName, "disabled", "no_map", null, null);
                return null;
            }

            if (Distance2D(start, finalDestination) <= 24)
            {
                LogDecision(destinationName, "disabled", "near_destination", null, null);
                return null;
            }

            AIGMLegacyRouteFile routeFile = LoadLegacyRouteFile();
            AIGMLegacyRouteEntry entry = FindExplicitRoute(routeFile, map, destinationName);
            if (entry != null)
            {
                AIGMLegacyWaypointRoute explicitRoute = BuildExplicitRoute(graph, map, destinationName, entry, source);
                if (explicitRoute != null && explicitRoute.Waypoints != null && explicitRoute.Waypoints.Count > 0)
                {
                    string explicitRouteId = NormalizeRouteId(entry.Destination, destinationName);
                    string explicitFirstTarget = explicitRoute.WaypointIds.Count > 0 ? explicitRoute.WaypointIds[0] : "none";
                    LogDecision(destinationName, "explicit", null, explicitRouteId, explicitFirstTarget);
                    LogRoute(destinationName, start, finalDestination, explicitRoute);
                    return explicitRoute;
                }

                LogDecision(destinationName, "disabled", "invalid_explicit_route", null, null);
            }
            else
            {
                LogExplicitMiss(destinationName, "no_alias_match");
            }

            AIGMLegacyWaypointRoute plannerRoute = TryBuildPlannerRoute(actor, finalDestination, map, destinationName, source);
            if (plannerRoute != null && plannerRoute.Waypoints != null && plannerRoute.Waypoints.Count > 0)
            {
                string plannerRouteId = plannerRoute.WaypointIds.Count > 0 ? plannerRoute.WaypointIds[0] : NormalizeRouteId(destinationName, destinationName);
                string plannerFirstTarget = plannerRoute.WaypointIds.Count > 0 ? plannerRoute.WaypointIds[0] : "none";
                LogDecision(destinationName, "planner", null, plannerRouteId, plannerFirstTarget);
                LogRoute(destinationName, start, finalDestination, plannerRoute);
                return plannerRoute;
            }

            AIGMLegacyWaypointRoute legacyGraphRoute = TryBuildLegacyGraphRoute(actor, finalDestination, map, destinationName, source);
            if (legacyGraphRoute != null && legacyGraphRoute.Waypoints != null && legacyGraphRoute.Waypoints.Count > 0)
            {
                string legacyRouteId = legacyGraphRoute.WaypointIds.Count > 0 ? legacyGraphRoute.WaypointIds[0] : NormalizeRouteId(destinationName, destinationName);
                string legacyFirstTarget = legacyGraphRoute.WaypointIds.Count > 0 ? legacyGraphRoute.WaypointIds[0] : "none";
                LogDecision(destinationName, "legacy_graph", null, legacyRouteId, legacyFirstTarget);
                LogRoute(destinationName, start, finalDestination, legacyGraphRoute);
                return legacyGraphRoute;
            }

            LogDecision(destinationName, "disabled", "no_route_source", null, null);
            return null;
        }

        private static AIGMLegacyWaypointRoute BuildExplicitRoute(AIGMNavGraph graph, Map map, string destinationName, AIGMLegacyRouteEntry entry, string source)
        {
            if (entry.Waypoints == null || entry.Waypoints.Length == 0)
            {
                LogExplicitMiss(destinationName, "empty_waypoints");
                return null;
            }

            AIGMLegacyWaypointRoute result = new AIGMLegacyWaypointRoute();
            result.Source = source + ";route=explicit";
            result.PreserveExactWaypoints = true;

            for (int i = 0; i < entry.Waypoints.Length; i++)
            {
                string waypointId = entry.Waypoints[i];
                AIGMNavNode node = graph.FindNodeById(waypointId);
                if (node == null)
                {
                    LogExplicitMiss(destinationName, "missing_waypoint:" + waypointId);
                    return null;
                }

                if (!node.Enabled)
                {
                    LogExplicitMiss(destinationName, "disabled_waypoint:" + waypointId);
                    return null;
                }

                if (node.Map != map)
                {
                    LogExplicitMiss(destinationName, "map_mismatch:" + waypointId);
                    return null;
                }

                result.Waypoints.Add(node.Location);
                result.WaypointIds.Add(node.Id);
                result.WaypointLabels.Add(node.Name ?? node.Id);
            }

            AIGMExecutionLog.Write(
                "AIGM_LEGACY_ROUTE_EXPLICIT destination={0} waypoints={1}",
                SafeLog(NormalizeRouteId(entry.Destination, destinationName)),
                SafeLog(String.Join(">", result.WaypointIds.ToArray())));

            return result;
        }

        private static AIGMLegacyWaypointRoute TryBuildPlannerRoute(Mobile actor, Point3D finalDestination, Map map, string destinationName, string source)
        {
            if (actor == null || map == null)
                return null;

            AIGMNavRoutePlan plan = AIGMNavRoutePlanner.BuildRoute(actor, finalDestination, map, destinationName, destinationName);
            if (plan == null || !plan.Success || plan.Steps == null || plan.Steps.Count == 0)
                return null;

            AIGMLegacyWaypointRoute route = new AIGMLegacyWaypointRoute();
            route.Source = source + ";route=planner;" + (plan.RouteMode ?? "unknown");
            route.CompletionRadius = ResolvePlannerCompletionRadius(plan);
            route.PreserveExactWaypoints = true;
            route.StopAtLastWaypoint = plan.IsPartial && !plan.FinalApproachAllowed;

            for (int i = 0; i < plan.Steps.Count; i++)
            {
                AIGMNavRouteStep step = plan.Steps[i];
                if (step == null)
                    continue;

                route.Waypoints.Add(step.Location);
                route.WaypointIds.Add(String.IsNullOrWhiteSpace(step.NodeId) ? ("planner_step_" + (i + 1).ToString()) : step.NodeId);
                route.WaypointLabels.Add(String.IsNullOrWhiteSpace(step.Name) ? ("planner step " + (i + 1).ToString()) : step.Name);
            }

            return route.Waypoints.Count > 0 ? route : null;
        }

        private static AIGMLegacyWaypointRoute TryBuildLegacyGraphRoute(Mobile actor, Point3D finalDestination, Map map, string destinationName, string source)
        {
            if (actor == null || map == null)
                return null;

            AIGMNavigationLocation destinationLocation;
            if (!TryResolveLocation(destinationName, finalDestination, map, out destinationLocation))
                return null;

            AIGMNavigationRoute legacyRoute;
            if (!AIGMNavigationRouteGraph.TryBuildRoute(actor.Location, map, destinationLocation, out legacyRoute) || legacyRoute == null || legacyRoute.Waypoints == null || legacyRoute.Waypoints.Count == 0)
                return null;

            AIGMLegacyWaypointRoute route = new AIGMLegacyWaypointRoute();
            route.Source = source + ";route=legacy_graph";
            route.CompletionRadius = AIGMNavigationDestinationResolver.GetArrivalRadius(destinationLocation);
            route.PreserveExactWaypoints = true;
            route.StopAtLastWaypoint = !legacyRoute.StopsAtExactDestination;

            for (int i = 0; i < legacyRoute.Waypoints.Count; i++)
            {
                Point3D waypoint = legacyRoute.Waypoints[i];
                string label = legacyRoute.WaypointLabels != null && i < legacyRoute.WaypointLabels.Count
                    ? legacyRoute.WaypointLabels[i]
                    : ("legacy waypoint " + (i + 1).ToString());

                route.Waypoints.Add(waypoint);
                route.WaypointIds.Add(BuildSyntheticWaypointId(label, waypoint, i));
                route.WaypointLabels.Add(label);
            }

            return route;
        }

        private static AIGMLegacyRouteFile LoadLegacyRouteFile()
        {
            AIGMLegacyRouteFile result = new AIGMLegacyRouteFile();
            result.Routes = new AIGMLegacyRouteEntry[0];

            try
            {
                if (!File.Exists(LegacyRoutesPath))
                {
                    AIGMExecutionLog.Write("AIGM_LEGACY_ROUTE_FILE_LOAD source=\"{0}\" routes=0", SafeLog(LegacyRoutesPath));
                    return result;
                }

                using (FileStream stream = File.Open(LegacyRoutesPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(AIGMLegacyRouteFile));
                    result = serializer.ReadObject(stream) as AIGMLegacyRouteFile ?? result;
                }
            }
            catch (Exception ex)
            {
                AIGMExecutionLog.Write(
                    "AIGM_LEGACY_ROUTE_FILE_LOAD source=\"{0}\" routes=0 error=\"{1}\"",
                    SafeLog(LegacyRoutesPath),
                    SafeLog(ex.Message));
                return result;
            }

            int routeCount = result.Routes == null ? 0 : result.Routes.Length;
            AIGMExecutionLog.Write("AIGM_LEGACY_ROUTE_FILE_LOAD source=\"{0}\" routes={1}", SafeLog(LegacyRoutesPath), routeCount);
            return result;
        }

        private static AIGMLegacyRouteEntry FindExplicitRoute(AIGMLegacyRouteFile routeFile, Map map, string destinationName)
        {
            if (routeFile == null || routeFile.Routes == null)
                return null;

            string destination = AIGMNavNode.Normalize(destinationName);
            for (int i = 0; i < routeFile.Routes.Length; i++)
            {
                AIGMLegacyRouteEntry entry = routeFile.Routes[i];
                if (entry == null)
                    continue;

                if (!RouteMapMatches(entry.MapName, map))
                    continue;

                if (MatchesExplicitDestination(entry, destination))
                    return entry;
            }

            return null;
        }

        private static bool MatchesExplicitDestination(AIGMLegacyRouteEntry entry, string destination)
        {
            if (entry == null || String.IsNullOrWhiteSpace(destination))
                return false;

            if (AIGMNavNode.Normalize(entry.Destination) == destination)
                return true;

            for (int i = 0; entry.Aliases != null && i < entry.Aliases.Length; i++)
            {
                if (AIGMNavNode.Normalize(entry.Aliases[i]) == destination)
                    return true;
            }

            return false;
        }

        private static bool RouteMapMatches(string routeMapName, Map map)
        {
            if (map == null || String.IsNullOrWhiteSpace(routeMapName))
                return true;

            return AIGMNavNode.ResolveMap(routeMapName) == map;
        }

        private static string NormalizeRouteId(string preferred, string fallback)
        {
            string normalized = AIGMNavNode.BuildId(preferred);
            if (!String.IsNullOrWhiteSpace(normalized) && normalized != "node")
                return normalized;

            return AIGMNavNode.BuildId(fallback);
        }

        private static int ResolvePlannerCompletionRadius(AIGMNavRoutePlan plan)
        {
            if (plan == null || plan.Steps == null || plan.Steps.Count == 0)
                return 0;

            AIGMNavRouteStep last = plan.Steps[plan.Steps.Count - 1];
            return last != null ? Math.Max(1, last.ArrivalRadius) : 0;
        }

        private static bool TryResolveLocation(string destinationName, Point3D finalDestination, Map map, out AIGMNavigationLocation location)
        {
            location = null;

            if (AIGMNavigationLocationRegistry.TryResolve(destinationName, map, out location))
                return true;

            foreach (AIGMNavigationLocation candidate in AIGMNavigationLocationRegistry.AllLocations)
            {
                if (candidate == null || candidate.Map != map)
                    continue;

                int arrivalRadius = AIGMNavigationDestinationResolver.GetArrivalRadius(candidate);
                if (Distance2D(candidate.Point, finalDestination) <= arrivalRadius)
                {
                    location = candidate;
                    return true;
                }
            }

            return false;
        }

        private static string BuildSyntheticWaypointId(string label, Point3D waypoint, int index)
        {
            return String.Format(
                "{0}_{1}_{2}_{3}_{4}",
                NormalizeRouteId(label, "legacy_waypoint"),
                index + 1,
                waypoint.X,
                waypoint.Y,
                waypoint.Z);
        }

        private static void LogDecision(string destinationName, string mode, string reason, string route, string firstTarget)
        {
            if (String.Equals(mode, "explicit", StringComparison.OrdinalIgnoreCase)
                || String.Equals(mode, "planner", StringComparison.OrdinalIgnoreCase)
                || String.Equals(mode, "legacy_graph", StringComparison.OrdinalIgnoreCase))
            {
                AIGMExecutionLog.Write(
                    "AIGM_LEGACY_WAYPOINT_DECISION destination=\"{0}\" mode={1} route={2} firstTarget={3}",
                    SafeLog(destinationName),
                    SafeLog(mode),
                    SafeLog(route),
                    SafeLog(firstTarget));
                return;
            }

            AIGMExecutionLog.Write(
                "AIGM_LEGACY_WAYPOINT_DECISION destination=\"{0}\" mode=disabled reason={1}",
                SafeLog(destinationName),
                SafeLog(reason));
        }

        private static void LogExplicitMiss(string destinationName, string reason)
        {
            AIGMExecutionLog.Write(
                "AIGM_LEGACY_ROUTE_EXPLICIT_MISS destination={0} reason={1}",
                SafeLog(AIGMNavNode.BuildId(destinationName)),
                SafeLog(reason));
        }

        private static void LogRoute(string destinationName, Point3D start, Point3D finalDestination, AIGMLegacyWaypointRoute route)
        {
            AIGMExecutionLog.Write(
                "AIGM_LEGACY_WAYPOINT_ROUTE destination=\"{0}\" start={1} final={2} waypointCount={3} waypoints=\"{4}\"",
                SafeLog(destinationName),
                FormatPoint(start),
                FormatPoint(finalDestination),
                route != null && route.Waypoints != null ? route.Waypoints.Count : 0,
                SafeLog(FormatRoute(route)));
        }

        private static int Distance2D(Point3D a, Point3D b)
        {
            int dx = a.X - b.X;
            int dy = a.Y - b.Y;
            return (int)Math.Round(Math.Sqrt((dx * dx) + (dy * dy)));
        }

        private static string FormatRoute(AIGMLegacyWaypointRoute route)
        {
            if (route == null || route.Waypoints == null || route.Waypoints.Count == 0)
                return "none";

            string[] parts = new string[route.Waypoints.Count];
            for (int i = 0; i < route.Waypoints.Count; i++)
            {
                string id = route.WaypointIds != null && i < route.WaypointIds.Count ? route.WaypointIds[i] : ("waypoint_" + (i + 1).ToString());
                parts[i] = id + "@" + FormatPoint(route.Waypoints[i]);
            }

            return String.Join(">", parts);
        }

        private static string FormatPoint(Point3D point)
        {
            return String.Format("{0},{1},{2}", point.X, point.Y, point.Z);
        }

        private static string SafeLog(string value)
        {
            return value == null ? String.Empty : value.Replace("\r", " ").Replace("\n", " ").Replace("\"", "'");
        }
    }
}
