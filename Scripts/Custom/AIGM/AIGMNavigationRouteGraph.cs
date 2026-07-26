using System;
using System.Collections.Generic;

using Server;

namespace Server.Custom.AIGM
{
    public enum AIGMNavigationRouteNodeCategory
    {
        Town,
        Shrine,
        DungeonEntrance,
        Road,
        Pass,
        Junction,
        Approach,
        Bank,
        Moongate,
        Canyon,
        Cemetery,
        Inn,
        Cabin,
        DungeonApproach,
        TownApproach,
        RoadSign,
        House,
        GuardHouse,
        TownNode,
        BritainApproach,
        MajorRoad,
        Crossroad,
        Landmark
    }

    public sealed class AIGMNavigationRouteNode
    {
        public AIGMNavigationRouteNode(string id, string displayName, AIGMNavigationRouteNodeCategory category, Map map, Point3D point, string[] neighbors, params string[] aliases)
            : this(id, displayName, category, map, point, neighbors, "existing route graph", "verified", aliases)
        {
        }

        public AIGMNavigationRouteNode(string id, string displayName, AIGMNavigationRouteNodeCategory category, Map map, Point3D point, string[] neighbors, string source, string confidence, params string[] aliases)
        {
            Id = id;
            DisplayName = displayName;
            Category = category;
            Map = map;
            Point = point;
            Neighbors = neighbors ?? new string[0];
            Aliases = aliases ?? new string[0];
            Source = String.IsNullOrWhiteSpace(source) ? "unknown" : source;
            Confidence = String.IsNullOrWhiteSpace(confidence) ? "unverified" : confidence;
        }

        public string Id { get; private set; }
        public string DisplayName { get; private set; }
        public AIGMNavigationRouteNodeCategory Category { get; private set; }
        public Map Map { get; private set; }
        public Point3D Point { get; private set; }
        public string[] Neighbors { get; private set; }
        public string[] Aliases { get; private set; }
        public string Source { get; private set; }
        public string Confidence { get; private set; }
    }

    public sealed class AIGMNavigationRoute
    {
        public AIGMNavigationRoute(string destinationKey, string destinationName, Point3D finalDestination, Map map, List<Point3D> waypoints, List<string> waypointLabels, string sourceNodeId)
            : this(destinationKey, destinationName, finalDestination, map, waypoints, waypointLabels, sourceNodeId, null)
        {
        }

        public AIGMNavigationRoute(string destinationKey, string destinationName, Point3D finalDestination, Map map, List<Point3D> waypoints, List<string> waypointLabels, string sourceNodeId, AIGMNavigationRouteNode destinationAnchor)
        {
            DestinationKey = destinationKey;
            DestinationName = destinationName;
            FinalDestination = finalDestination;
            Map = map;
            Waypoints = waypoints ?? new List<Point3D>();
            WaypointLabels = waypointLabels ?? new List<string>();
            SourceNodeId = sourceNodeId ?? String.Empty;
            DestinationAnchorId = destinationAnchor != null ? destinationAnchor.Id : String.Empty;
            DestinationAnchorName = destinationAnchor != null ? destinationAnchor.DisplayName : String.Empty;
            DestinationAnchorPoint = destinationAnchor != null ? destinationAnchor.Point : Point3D.Zero;
            StopsAtExactDestination = Waypoints.Count > 0 && Waypoints[Waypoints.Count - 1] == finalDestination;
        }

        public string DestinationKey { get; private set; }
        public string DestinationName { get; private set; }
        public Point3D FinalDestination { get; private set; }
        public Map Map { get; private set; }
        public List<Point3D> Waypoints { get; private set; }
        public List<string> WaypointLabels { get; private set; }
        public string SourceNodeId { get; private set; }
        public string DestinationAnchorId { get; private set; }
        public string DestinationAnchorName { get; private set; }
        public Point3D DestinationAnchorPoint { get; private set; }
        public bool StopsAtExactDestination { get; private set; }
    }

    public sealed class AIGMNavigationRoutePreview
    {
        public Point3D Start;
        public Map Map;
        public AIGMNavigationLocation Destination;
        public bool UsesGraph;
        public string Reason;
        public string SourceNodeId;
        public string SourceNodeName;
        public Point3D SourceNodePoint;
        public Point3D FirstWaypoint;
        public string FirstWaypointName;
        public string DestinationAnchorId;
        public string DestinationAnchorName;
        public Point3D DestinationAnchorPoint;
        public Point3D FinalDestination;
        public Point3D CanonicalDestination;
        public Point3D ResolvedStandableTarget;
        public int ArrivalRadius;
        public bool DestinationTargetStandable;
        public string DestinationResolveReason;
        public Point3D FinalApproachPoint;
        public bool StopsAtExactDestination;
        public bool DestinationIsLandmark;
        public AIGMNavigationRoute Route;
    }

    public static class AIGMNavigationRouteGraph
    {
        private const int MaxStartNodeDistance = 500;
        private const int DirectionalStartDistance = 20;

        private static readonly AIGMNavigationRouteNode[] Nodes =
        {
            new AIGMNavigationRouteNode("britain", "Britain", AIGMNavigationRouteNodeCategory.Town, Map.Felucca, new Point3D(1546, 1634, 0), Links("britain-bank", "east-britain-bank", "britain-north-cemetery", "britain-west-farmlands", "britain-east-suburbs", "britain-south-docks", "compassion-shrine", "spirituality-shrine"), "brit"),
            new AIGMNavigationRouteNode("britain-bank", "First Bank of Britain", AIGMNavigationRouteNodeCategory.Bank, Map.Felucca, new Point3D(1425, 1690, 0), Links("britain", "britain-moongate", "britain-west-farmlands", "britain-north-cemetery", "to-britain", "the-moat", "brit-guard-house"), "Data/Common.map existing Britain Bank node merged with user sign name", "data-verified", "first bank of britain", "brit bank", "britain bank", "bank of britain", "first bank", "west britain bank"),
            new AIGMNavigationRouteNode("east-britain-bank", "East Britain Bank", AIGMNavigationRouteNodeCategory.Bank, Map.Felucca, new Point3D(1655, 1606, 0), Links("britain", "britain-east-suburbs", "cove"), "ebb"),
            new AIGMNavigationRouteNode("britain-moongate", "Britain Moongate", AIGMNavigationRouteNodeCategory.Moongate, Map.Felucca, new Point3D(1336, 1997, 0), Links("britain-bank", "britain-south-docks"), "brit moongate"),
            new AIGMNavigationRouteNode("britain-north-cemetery", "Britain North Cemetery Road", AIGMNavigationRouteNodeCategory.Road, Map.Felucca, new Point3D(1371, 1482, 0), Links("britain", "britain-bank", "brit-graveyard", "the-great-northern-road", "despise", "justice-shrine"), "britain cemetery", "britain north"),
            new AIGMNavigationRouteNode("britain-west-farmlands", "Britain West Farmlands", AIGMNavigationRouteNodeCategory.Road, Map.Felucca, new Point3D(1228, 1705, 0), Links("britain", "britain-bank", "brit-canyon", "yew-road", "yew-britain-brigand-camp", "orc-cave", "shame"), "britain west", "farmlands"),
            new AIGMNavigationRouteNode("britain-east-suburbs", "Britain East Suburbs", AIGMNavigationRouteNodeCategory.Road, Map.Felucca, new Point3D(1662, 1572, 5), Links("britain", "east-britain-bank", "cove", "compassion-shrine"), "britain east", "suburbs"),
            new AIGMNavigationRouteNode("britain-south-docks", "Britain South Docks Road", AIGMNavigationRouteNodeCategory.Road, Map.Felucca, new Point3D(1470, 1765, 0), Links("britain", "britain-moongate", "spirituality-shrine", "skara-brae"), "britain docks", "britain south"),
            new AIGMNavigationRouteNode("yew-britain-brigand-camp", "Yew-Britain Road Camp", AIGMNavigationRouteNodeCategory.Road, Map.Felucca, new Point3D(885, 1682, 0), Links("britain-west-farmlands", "yew-road", "yew", "shame"), "yew britain road", "brigand camp"),
            new AIGMNavigationRouteNode("xroad-house", "Xroad House", AIGMNavigationRouteNodeCategory.Crossroad, Map.Felucca, new Point3D(1002, 1936, 7), Links("brit-canyon", "britain-moongate", "yew-road"), "Phase60N live field current-position log near west Britain crossroad", "field-verified", "xroad", "crossroad house", "x road house"),
            new AIGMNavigationRouteNode("brit-canyon", "Brit Canyon", AIGMNavigationRouteNodeCategory.Canyon, Map.Felucca, new Point3D(1098, 1928, 7), Links("xroad-house", "yew-road", "britain-west-farmlands"), "user live sign/prop screenshot Location", "field-verified", "britain canyon"),
            new AIGMNavigationRouteNode("yew-road", "Yew Road", AIGMNavigationRouteNodeCategory.Road, Map.Felucca, new Point3D(885, 1682, 0), Links("xroad-house", "brit-canyon", "britain-west-farmlands", "yew-britain-brigand-camp", "shame"), "existing route graph Yew-Britain Road Camp", "data-verified", "yew britain road", "road to yew"),
            new AIGMNavigationRouteNode("brit-graveyard", "Brit Graveyard", AIGMNavigationRouteNodeCategory.Cemetery, Map.Felucca, new Point3D(1371, 1482, 0), Links("britain-north-cemetery", "britain-bank", "the-great-northern-road", "despise-canyon"), "existing route graph Britain North Cemetery Road; canonicalized from user correction", "data-verified", "brit cemetary", "brit cemetery", "brit graveyard", "britain graveyard", "graveyard", "brit cemetery road"),
            new AIGMNavigationRouteNode("despise-canyon", "Despise Canyon", AIGMNavigationRouteNodeCategory.Canyon, Map.Felucca, new Point3D(1371, 1350, 0), Links("brit-graveyard", "despise-canyon-1"), "route-interpolated from verified graveyard and entrance approach", "route-derived", "despise canyon road"),
            new AIGMNavigationRouteNode("despise-canyon-1", "Despise Canyon1", AIGMNavigationRouteNodeCategory.Canyon, Map.Felucca, new Point3D(1374, 1230, 0), Links("despise-canyon", "despise-canyon-2"), "route-interpolated from verified cemetery and entrance approach", "route-derived", "despise canyon 1", "despise canyon one"),
            new AIGMNavigationRouteNode("despise-canyon-2", "Despise Canyon2", AIGMNavigationRouteNodeCategory.Canyon, Map.Felucca, new Point3D(1368, 1145, 0), Links("despise-canyon-1", "despise-entrance"), "route-interpolated from verified cemetery and entrance approach", "route-derived", "despise canyon 2", "despise canyon two"),
            new AIGMNavigationRouteNode("despise-entrance", "Despise Entrance", AIGMNavigationRouteNodeCategory.DungeonEntrance, Map.Felucca, new Point3D(1361, 1071, 4), Links("despise-canyon-2", "despise", "chaos", "rotting-cabin"), "user live sign/prop screenshot Location plus RazorEnhanced region", "field-verified", "despise dungeon entrance"),
            new AIGMNavigationRouteNode("chaos", "Chaos", AIGMNavigationRouteNodeCategory.Shrine, Map.Felucca, new Point3D(1458, 844, 7), Links("despise-entrance", "rotting-cabin", "justice-shrine"), "RazorEnhanced common.def shrine entry", "data-verified", "chaos shrine"),
            new AIGMNavigationRouteNode("rotting-cabin", "Rotting Cabin", AIGMNavigationRouteNodeCategory.Cabin, Map.Felucca, new Point3D(1474, 786, 20), Links("chaos", "despise-entrance", "here-lies-culas"), "user live sign/prop screenshot Location", "field-verified", "rotten cabin"),
            new AIGMNavigationRouteNode("here-lies-culas", "Here Lies Culas", AIGMNavigationRouteNodeCategory.Landmark, Map.Felucca, new Point3D(1474, 786, 20), Links("rotting-cabin"), "ClassicUO journal live sign label near Rotting Cabin", "field-label", "culas", "here lies culas"),
            new AIGMNavigationRouteNode("drunken-inn", "Drunken Inn", AIGMNavigationRouteNodeCategory.Inn, Map.Felucca, new Point3D(1228, 1705, 0), Links("britain-west-farmlands", "brit-canyon"), "anchored to existing Britain West Farmlands road node pending sign prop recheck", "route-derived", "drunken inn"),
            new AIGMNavigationRouteNode("bones-o-scum", "Bones O' Scum", AIGMNavigationRouteNodeCategory.Landmark, Map.Felucca, new Point3D(1098, 1928, 7), Links("brit-canyon"), "anchored to verified Brit Canyon sign cluster pending sign prop recheck", "field-label", "bones scum", "bones o scum"),
            new AIGMNavigationRouteNode("to-britain", "To Britain", AIGMNavigationRouteNodeCategory.RoadSign, Map.Felucca, new Point3D(1392, 1714, 0), Links("britain-bank", "the-moat", "britain-west-farmlands"), "user screenshot sign name; coordinate provisional pending in-client [where]", "provisional", "to britain", "britain road sign", "road to britain"),
            new AIGMNavigationRouteNode("the-moat", "The Moat", AIGMNavigationRouteNodeCategory.BritainApproach, Map.Felucca, new Point3D(1402, 1684, 0), Links("to-britain", "britain-bank", "brit-guard-house", "the-moat-2"), "user screenshot sign name; coordinate provisional pending in-client [where]", "provisional", "the moat", "moat", "brit moat"),
            new AIGMNavigationRouteNode("brit-guard-house", "Brit Guard House", AIGMNavigationRouteNodeCategory.GuardHouse, Map.Felucca, new Point3D(1414, 1716, 0), Links("the-moat", "britain-bank", "to-britain"), "user screenshot house sign name; coordinate provisional pending in-client [where]", "provisional", "brit guard house", "britain guard house", "guard house", "guardhouse"),
            new AIGMNavigationRouteNode("the-moat-2", "The Moat2", AIGMNavigationRouteNodeCategory.BritainApproach, Map.Felucca, new Point3D(1397, 1655, 0), Links("the-moat", "the-great-northern-road", "britain"), "user screenshot sign name; coordinate provisional pending in-client [where]", "provisional", "the moat2", "moat2", "the moat 2", "moat 2", "the moat two", "moat two", "second moat"),
            new AIGMNavigationRouteNode("the-great-northern-road", "The Great Northern Road", AIGMNavigationRouteNodeCategory.MajorRoad, Map.Felucca, new Point3D(1371, 1482, 0), Links("the-moat-2", "brit-graveyard", "britain-north-cemetery"), "existing Britain North Cemetery Road graph anchor plus user screenshot sign name", "data-verified", "great northern road", "the great northern road", "northern road", "britain northern road"),

            new AIGMNavigationRouteNode("cove", "Cove", AIGMNavigationRouteNodeCategory.Town, Map.Felucca, new Point3D(2230, 1159, 0), Links("east-britain-bank", "britain-east-suburbs", "minoc", "vesper")),
            new AIGMNavigationRouteNode("minoc", "Minoc", AIGMNavigationRouteNodeCategory.Town, Map.Felucca, new Point3D(2498, 392, 0), Links("cove", "minoc-bank", "covetous", "wrong")),
            new AIGMNavigationRouteNode("minoc-bank", "Minoc Bank", AIGMNavigationRouteNodeCategory.Bank, Map.Felucca, new Point3D(2503, 552, 0), Links("minoc", "minoc-moongate")),
            new AIGMNavigationRouteNode("minoc-moongate", "Minoc Moongate", AIGMNavigationRouteNodeCategory.Moongate, Map.Felucca, new Point3D(2702, 692, 0), Links("minoc-bank", "vesper-bank")),
            new AIGMNavigationRouteNode("vesper", "Vesper", AIGMNavigationRouteNodeCategory.Town, Map.Felucca, new Point3D(2973, 891, 0), Links("cove", "vesper-bank")),
            new AIGMNavigationRouteNode("vesper-bank", "Vesper Bank", AIGMNavigationRouteNodeCategory.Bank, Map.Felucca, new Point3D(2881, 684, 0), Links("vesper", "minoc-moongate")),

            new AIGMNavigationRouteNode("trinsic", "Trinsic", AIGMNavigationRouteNodeCategory.Town, Map.Felucca, new Point3D(1993, 2827, 0), Links("trinsic-bank", "trinsic-moongate", "destard", "spirituality-shrine", "valor-shrine")),
            new AIGMNavigationRouteNode("trinsic-bank", "Trinsic Bank", AIGMNavigationRouteNodeCategory.Bank, Map.Felucca, new Point3D(1897, 2684, 0), Links("trinsic")),
            new AIGMNavigationRouteNode("trinsic-moongate", "Trinsic Moongate", AIGMNavigationRouteNodeCategory.Moongate, Map.Felucca, new Point3D(1829, 2949, 0), Links("trinsic")),
            new AIGMNavigationRouteNode("skara-brae", "Skara Brae", AIGMNavigationRouteNodeCategory.Town, Map.Felucca, new Point3D(742, 2216, 0), Links("skara-brae-bank", "skara-brae-moongate", "shame"), "skara"),
            new AIGMNavigationRouteNode("skara-brae-bank", "Skara Brae Bank", AIGMNavigationRouteNodeCategory.Bank, Map.Felucca, new Point3D(587, 2146, 0), Links("skara-brae"), "skara bank"),
            new AIGMNavigationRouteNode("skara-brae-moongate", "Skara Brae Moongate", AIGMNavigationRouteNodeCategory.Moongate, Map.Felucca, new Point3D(645, 2068, 0), Links("skara-brae"), "skara moongate"),

            new AIGMNavigationRouteNode("moonglow", "Moonglow", AIGMNavigationRouteNodeCategory.Town, Map.Felucca, new Point3D(4444, 1061, 0), Links("moonglow-bank", "moonglow-moongate", "deceit", "honesty-shrine")),
            new AIGMNavigationRouteNode("moonglow-bank", "Moonglow Bank", AIGMNavigationRouteNodeCategory.Bank, Map.Felucca, new Point3D(4471, 1156, 0), Links("moonglow")),
            new AIGMNavigationRouteNode("moonglow-moongate", "Moonglow Moongate", AIGMNavigationRouteNodeCategory.Moongate, Map.Felucca, new Point3D(4468, 1284, 0), Links("moonglow")),

            new AIGMNavigationRouteNode("compassion-shrine", "Compassion Shrine", AIGMNavigationRouteNodeCategory.Shrine, Map.Felucca, new Point3D(1858, 874, 0), Links("britain", "britain-east-suburbs", "despise"), "compassion"),
            new AIGMNavigationRouteNode("honesty-shrine", "Honesty Shrine", AIGMNavigationRouteNodeCategory.Shrine, Map.Felucca, new Point3D(4212, 563, 0), Links("moonglow", "deceit"), "honesty"),
            new AIGMNavigationRouteNode("honor-shrine", "Honor Shrine", AIGMNavigationRouteNodeCategory.Shrine, Map.Felucca, new Point3D(1723, 3527, 0), Links("trinsic", "destard"), "honor"),
            new AIGMNavigationRouteNode("humility-shrine", "Humility Shrine", AIGMNavigationRouteNodeCategory.Shrine, Map.Felucca, new Point3D(4274, 3697, 0), Links("hythloth"), "humility"),
            new AIGMNavigationRouteNode("justice-shrine", "Justice Shrine", AIGMNavigationRouteNodeCategory.Shrine, Map.Felucca, new Point3D(1300, 633, 0), Links("yew", "despise"), "justice"),
            new AIGMNavigationRouteNode("sacrifice-shrine", "Sacrifice Shrine", AIGMNavigationRouteNodeCategory.Shrine, Map.Felucca, new Point3D(3355, 289, 0), Links("minoc", "deceit"), "sacrifice"),
            new AIGMNavigationRouteNode("spirituality-shrine", "Spirituality Shrine", AIGMNavigationRouteNodeCategory.Shrine, Map.Felucca, new Point3D(1595, 2490, 0), Links("britain", "britain-south-docks", "trinsic", "destard"), "spirituality"),
            new AIGMNavigationRouteNode("valor-shrine", "Valor Shrine", AIGMNavigationRouteNodeCategory.Shrine, Map.Felucca, new Point3D(2491, 3933, 0), Links("trinsic", "fire"), "valor"),

            new AIGMNavigationRouteNode("yew", "Yew", AIGMNavigationRouteNodeCategory.Town, Map.Felucca, new Point3D(504, 942, 0), Links("justice-shrine", "shame", "yew-britain-brigand-camp")),
            new AIGMNavigationRouteNode("covetous", "Covetous", AIGMNavigationRouteNodeCategory.DungeonEntrance, Map.Felucca, new Point3D(2499, 916, 0), Links("minoc")),
            new AIGMNavigationRouteNode("deceit", "Deceit", AIGMNavigationRouteNodeCategory.DungeonEntrance, Map.Felucca, new Point3D(4111, 429, 0), Links("moonglow", "honesty-shrine", "sacrifice-shrine")),
            new AIGMNavigationRouteNode("despise", "Despise", AIGMNavigationRouteNodeCategory.DungeonEntrance, Map.Felucca, new Point3D(1296, 1082, 0), Links("despise-entrance", "britain-north-cemetery", "compassion-shrine", "justice-shrine")),
            new AIGMNavigationRouteNode("destard", "Destard", AIGMNavigationRouteNodeCategory.DungeonEntrance, Map.Felucca, new Point3D(1176, 2635, 0), Links("trinsic", "honor-shrine", "spirituality-shrine")),
            new AIGMNavigationRouteNode("fire", "Fire", AIGMNavigationRouteNodeCategory.DungeonEntrance, Map.Felucca, new Point3D(2922, 3402, 0), Links("valor-shrine", "trinsic")),
            new AIGMNavigationRouteNode("hythloth", "Hythloth", AIGMNavigationRouteNodeCategory.DungeonEntrance, Map.Felucca, new Point3D(4722, 3814, 0), Links("humility-shrine", "moonglow")),
            new AIGMNavigationRouteNode("ice", "Ice", AIGMNavigationRouteNodeCategory.DungeonEntrance, Map.Felucca, new Point3D(1996, 80, 0), Links("wrong", "minoc")),
            new AIGMNavigationRouteNode("orc-cave", "Orc Cave", AIGMNavigationRouteNodeCategory.DungeonEntrance, Map.Felucca, new Point3D(1014, 1434, 0), Links("britain-west-farmlands", "yew"), "orc cave"),
            new AIGMNavigationRouteNode("shame", "Shame", AIGMNavigationRouteNodeCategory.DungeonEntrance, Map.Felucca, new Point3D(512, 1559, 0), Links("skara-brae", "yew")),
            new AIGMNavigationRouteNode("wrong", "Wrong", AIGMNavigationRouteNodeCategory.DungeonEntrance, Map.Felucca, new Point3D(2042, 226, 0), Links("minoc", "ice"))
        };

        public static bool TryBuildRoute(Point3D start, Map map, AIGMNavigationLocation destination, out AIGMNavigationRoute route)
        {
            route = null;

            if (map == null || destination == null || destination.Map != map)
                return false;

            AIGMNavigationDestinationResolution resolution = AIGMNavigationDestinationResolver.Resolve(destination, start, map);
            Point3D finalTarget = resolution.Target;

            AIGMNavigationRouteNode destinationAnchor = FindDestinationAnchor(destination, map);
            if (destinationAnchor == null || destinationAnchor.Map != map)
                return false;

            AIGMNavigationRouteNode startNode = FindNearestNode(start, map, destination.Point, MaxStartNodeDistance);
            if (startNode == null)
                return false;

            List<AIGMNavigationRouteNode> path = FindPath(startNode, destinationAnchor);
            if (path == null || path.Count == 0)
                return false;

            List<Point3D> waypoints = new List<Point3D>();
            List<string> labels = new List<string>();

            for (int i = 0; i < path.Count; i++)
            {
                if (i == 0 && GetDistance(start, path[i].Point) <= 4)
                    continue;

                waypoints.Add(path[i].Point);
                labels.Add(path[i].DisplayName);
            }

            if (waypoints.Count == 0 || waypoints[waypoints.Count - 1] != finalTarget)
            {
                waypoints.Add(finalTarget);
                labels.Add(destination.DisplayName);
            }

            route = new AIGMNavigationRoute(destination.Key, destination.DisplayName, finalTarget, destination.Map, waypoints, labels, startNode.Id, destinationAnchor);
            return waypoints.Count > 1;
        }

        public static AIGMNavigationRoutePreview BuildPreview(Point3D start, Map map, AIGMNavigationLocation destination)
        {
            AIGMNavigationRoutePreview preview = new AIGMNavigationRoutePreview();
            preview.Start = start;
            preview.Map = map;
            preview.Destination = destination;
            preview.FirstWaypoint = destination != null ? destination.Point : Point3D.Zero;
            preview.FirstWaypointName = destination != null ? destination.DisplayName : String.Empty;
            preview.FinalDestination = destination != null ? destination.Point : Point3D.Zero;
            preview.CanonicalDestination = destination != null ? destination.Point : Point3D.Zero;
            preview.ResolvedStandableTarget = destination != null ? destination.Point : Point3D.Zero;
            preview.ArrivalRadius = destination != null ? AIGMNavigationDestinationResolver.GetArrivalRadius(destination) : 0;
            preview.DestinationTargetStandable = false;
            preview.DestinationResolveReason = "invalid";
            preview.FinalApproachPoint = destination != null ? destination.Point : Point3D.Zero;
            preview.StopsAtExactDestination = true;
            preview.Reason = "invalid";

            if (map == null || destination == null || destination.Map != map)
                return preview;

            AIGMNavigationDestinationResolution resolution = AIGMNavigationDestinationResolver.Resolve(destination, start, map);
            preview.CanonicalDestination = resolution.Canonical;
            preview.ResolvedStandableTarget = resolution.Target;
            preview.ArrivalRadius = resolution.ArrivalRadius;
            preview.DestinationTargetStandable = resolution.IsStandable;
            preview.DestinationResolveReason = resolution.Reason;

            AIGMNavigationRoute route;
            if (TryBuildRoute(start, map, destination, out route))
            {
                preview.UsesGraph = true;
                preview.Route = route;
                preview.SourceNodeId = route.SourceNodeId;
                preview.DestinationAnchorId = route.DestinationAnchorId;
                preview.DestinationAnchorName = route.DestinationAnchorName;
                preview.DestinationAnchorPoint = route.DestinationAnchorPoint;
                preview.FinalDestination = route.FinalDestination;
                preview.FinalApproachPoint = route.Waypoints != null && route.Waypoints.Count > 0 ? route.Waypoints[route.Waypoints.Count - 1] : route.FinalDestination;
                preview.StopsAtExactDestination = route.StopsAtExactDestination;
                preview.DestinationIsLandmark = route.DestinationAnchorPoint == route.FinalDestination;

                AIGMNavigationRouteNode source = FindNode(route.SourceNodeId, null);
                if (source != null)
                {
                    preview.SourceNodeName = source.DisplayName;
                    preview.SourceNodePoint = source.Point;
                }

                preview.FirstWaypoint = route.Waypoints[0];
                preview.FirstWaypointName = route.WaypointLabels != null && route.WaypointLabels.Count > 0 ? route.WaypointLabels[0] : "waypoint";
                preview.Reason = "graph";
                return preview;
            }

            AIGMNavigationRouteNode nearest = FindNearestNode(start, map, destination.Point, MaxStartNodeDistance);
            if (nearest != null)
            {
                preview.SourceNodeId = nearest.Id;
                preview.SourceNodeName = nearest.DisplayName;
                preview.SourceNodePoint = nearest.Point;
            }

            preview.UsesGraph = false;
            preview.Reason = "direct";
            preview.FinalDestination = resolution.Target;
            preview.FinalApproachPoint = resolution.Target;
            preview.StopsAtExactDestination = resolution.Target == destination.Point;
            return preview;
        }

        public static string FormatGraphSummary()
        {
            int edgeCount = 0;
            for (int i = 0; i < Nodes.Length; i++)
                edgeCount += Nodes[i].Neighbors.Length;

            return String.Format("route nodes={0}; directed links={1}", Nodes.Length, edgeCount);
        }

        public static string FormatLandmarks(Point3D start, Map map, bool nearOnly)
        {
            List<AIGMNavigationRouteNode> nodes = new List<AIGMNavigationRouteNode>();
            for (int i = 0; i < Nodes.Length; i++)
            {
                if (!IsLandmarkCategory(Nodes[i].Category) || (map != null && Nodes[i].Map != map))
                    continue;

                if (nearOnly && GetDistance(start, Nodes[i].Point) > 350)
                    continue;

                nodes.Add(Nodes[i]);
            }

            nodes.Sort(delegate(AIGMNavigationRouteNode a, AIGMNavigationRouteNode b)
            {
                return GetDistance(start, a.Point).CompareTo(GetDistance(start, b.Point));
            });

            if (nodes.Count == 0)
                return "No route landmarks found.";

            List<string> parts = new List<string>();
            for (int i = 0; i < nodes.Count; i++)
                parts.Add(String.Format("{0}@{1} {2}/{3}", nodes[i].DisplayName, FormatPoint(nodes[i].Point), nodes[i].Category, nodes[i].Confidence));

            return String.Join(" | ", parts.ToArray());
        }

        private static List<AIGMNavigationRouteNode> FindPath(AIGMNavigationRouteNode start, AIGMNavigationRouteNode destination)
        {
            List<AIGMNavigationRouteNode> open = new List<AIGMNavigationRouteNode>();
            Dictionary<string, string> parent = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, int> costs = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, AIGMNavigationRouteNode> visited = new Dictionary<string, AIGMNavigationRouteNode>(StringComparer.OrdinalIgnoreCase);

            open.Add(start);
            visited[start.Id] = start;
            parent[start.Id] = null;
            costs[start.Id] = 0;

            while (open.Count > 0)
            {
                int openIndex = FindLowestCostIndex(open, costs);
                AIGMNavigationRouteNode node = open[openIndex];
                open.RemoveAt(openIndex);

                if (String.Equals(node.Id, destination.Id, StringComparison.OrdinalIgnoreCase))
                    return ReconstructPath(destination.Id, parent, visited);

                for (int i = 0; i < node.Neighbors.Length; i++)
                {
                    AIGMNavigationRouteNode next = FindNode(node.Neighbors[i], null);
                    if (next == null || next.Map != node.Map)
                        continue;

                    int newCost = costs[node.Id] + GetDistance(node.Point, next.Point);
                    int oldCost;
                    if (costs.TryGetValue(next.Id, out oldCost) && oldCost <= newCost)
                        continue;

                    visited[next.Id] = next;
                    parent[next.Id] = node.Id;
                    costs[next.Id] = newCost;
                    if (!ContainsNode(open, next.Id))
                        open.Add(next);
                }
            }

            return null;
        }

        private static int FindLowestCostIndex(List<AIGMNavigationRouteNode> open, Dictionary<string, int> costs)
        {
            int bestIndex = 0;
            int bestCost = Int32.MaxValue;

            for (int i = 0; i < open.Count; i++)
            {
                int cost;
                if (!costs.TryGetValue(open[i].Id, out cost))
                    cost = Int32.MaxValue;

                if (cost < bestCost)
                {
                    bestCost = cost;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        private static bool ContainsNode(List<AIGMNavigationRouteNode> nodes, string id)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                if (String.Equals(nodes[i].Id, id, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static List<AIGMNavigationRouteNode> ReconstructPath(string destinationId, Dictionary<string, string> parent, Dictionary<string, AIGMNavigationRouteNode> visited)
        {
            List<AIGMNavigationRouteNode> reversed = new List<AIGMNavigationRouteNode>();
            string current = destinationId;

            while (!String.IsNullOrWhiteSpace(current))
            {
                AIGMNavigationRouteNode node;
                if (!visited.TryGetValue(current, out node))
                    break;

                reversed.Add(node);
                parent.TryGetValue(current, out current);
            }

            reversed.Reverse();
            return reversed;
        }

        private static AIGMNavigationRouteNode FindNearestNode(Point3D start, Map map, Point3D destination, int maxDistance)
        {
            AIGMNavigationRouteNode best = null;
            int bestScore = Int32.MaxValue;

            for (int i = 0; i < Nodes.Length; i++)
            {
                if (Nodes[i].Map != map)
                    continue;

                int distance = GetDistance(start, Nodes[i].Point);
                if (distance > maxDistance)
                    continue;

                int score = distance + DirectionPenalty(start, Nodes[i].Point, destination);
                if (score < bestScore)
                {
                    best = Nodes[i];
                    bestScore = score;
                }
            }

            return best;
        }

        private static AIGMNavigationRouteNode FindDestinationAnchor(AIGMNavigationLocation destination, Map map)
        {
            if (destination == null || map == null)
                return null;

            AIGMNavigationRouteNode exact = FindNode(destination.Key, destination.DisplayName);
            if (exact != null && exact.Map == map)
                return exact;

            AIGMNavigationRouteNode best = null;
            int bestScore = Int32.MaxValue;

            for (int i = 0; i < Nodes.Length; i++)
            {
                AIGMNavigationRouteNode node = Nodes[i];
                if (node == null || node.Map != map || !IsUsefulDestinationAnchor(node))
                    continue;

                int distance = GetDistance(node.Point, destination.Point);
                int score = distance + DestinationAnchorPenalty(node);
                if (score < bestScore)
                {
                    best = node;
                    bestScore = score;
                }
            }

            return best;
        }

        private static bool IsUsefulDestinationAnchor(AIGMNavigationRouteNode node)
        {
            if (node == null)
                return false;

            return node.Category == AIGMNavigationRouteNodeCategory.Road
                || node.Category == AIGMNavigationRouteNodeCategory.Canyon
                || node.Category == AIGMNavigationRouteNodeCategory.Cemetery
                || node.Category == AIGMNavigationRouteNodeCategory.DungeonApproach
                || node.Category == AIGMNavigationRouteNodeCategory.DungeonEntrance
                || node.Category == AIGMNavigationRouteNodeCategory.TownApproach
                || node.Category == AIGMNavigationRouteNodeCategory.RoadSign
                || node.Category == AIGMNavigationRouteNodeCategory.BritainApproach
                || node.Category == AIGMNavigationRouteNodeCategory.MajorRoad
                || node.Category == AIGMNavigationRouteNodeCategory.Crossroad
                || node.Category == AIGMNavigationRouteNodeCategory.Landmark
                || node.Category == AIGMNavigationRouteNodeCategory.Bank
                || node.Category == AIGMNavigationRouteNodeCategory.GuardHouse
                || node.Category == AIGMNavigationRouteNodeCategory.TownNode
                || node.Category == AIGMNavigationRouteNodeCategory.Town;
        }

        private static int DestinationAnchorPenalty(AIGMNavigationRouteNode node)
        {
            if (node == null)
                return 1000;

            if (node.Category == AIGMNavigationRouteNodeCategory.Road
                || node.Category == AIGMNavigationRouteNodeCategory.Canyon
                || node.Category == AIGMNavigationRouteNodeCategory.DungeonApproach
                || node.Category == AIGMNavigationRouteNodeCategory.DungeonEntrance
                || node.Category == AIGMNavigationRouteNodeCategory.TownApproach
                || node.Category == AIGMNavigationRouteNodeCategory.RoadSign
                || node.Category == AIGMNavigationRouteNodeCategory.BritainApproach
                || node.Category == AIGMNavigationRouteNodeCategory.MajorRoad)
                return 0;

            if (node.Category == AIGMNavigationRouteNodeCategory.Bank
                || node.Category == AIGMNavigationRouteNodeCategory.GuardHouse
                || node.Category == AIGMNavigationRouteNodeCategory.TownNode
                || node.Category == AIGMNavigationRouteNodeCategory.Cemetery
                || node.Category == AIGMNavigationRouteNodeCategory.Crossroad)
                return 25;

            if (node.Category == AIGMNavigationRouteNodeCategory.Town)
                return 75;

            return 50;
        }

        private static int DirectionPenalty(Point3D start, Point3D node, Point3D destination)
        {
            int toNodeX = node.X - start.X;
            int toNodeY = node.Y - start.Y;
            int toDestinationX = destination.X - start.X;
            int toDestinationY = destination.Y - start.Y;
            int nodeDistance = GetDistance(start, node);

            if (nodeDistance <= DirectionalStartDistance)
                return 0;

            int dot = (toNodeX * toDestinationX) + (toNodeY * toDestinationY);
            if (dot <= 0)
                return 1200;

            int penalty = 0;
            if (Math.Abs(toDestinationX) >= 100 && Math.Abs(toNodeX) > DirectionalStartDistance && Math.Sign(toDestinationX) != Math.Sign(toNodeX))
                penalty += 1200;

            if (Math.Abs(toDestinationY) >= 100 && Math.Abs(toNodeY) > 60 && Math.Sign(toDestinationY) != Math.Sign(toNodeY))
                penalty += 400;

            int directDistance = GetDistance(start, destination);
            int nodeToDestination = GetDistance(node, destination);
            if (nodeToDestination > directDistance + 50)
                penalty += 250;

            return penalty;
        }

        private static AIGMNavigationRouteNode FindNode(string idOrAlias, string displayName)
        {
            string id = AIGMNavigationLocationRegistry.NormalizeForLookup(idOrAlias);
            string display = AIGMNavigationLocationRegistry.NormalizeForLookup(displayName);

            for (int i = 0; i < Nodes.Length; i++)
            {
                if (Matches(Nodes[i], id) || (!String.IsNullOrWhiteSpace(display) && Matches(Nodes[i], display)))
                    return Nodes[i];
            }

            return null;
        }

        private static bool Matches(AIGMNavigationRouteNode node, string normalized)
        {
            if (node == null || String.IsNullOrWhiteSpace(normalized))
                return false;

            if (AIGMNavigationLocationRegistry.NormalizeForLookup(node.Id) == normalized || AIGMNavigationLocationRegistry.NormalizeForLookup(node.DisplayName) == normalized)
                return true;

            for (int i = 0; node.Aliases != null && i < node.Aliases.Length; i++)
            {
                if (AIGMNavigationLocationRegistry.NormalizeForLookup(node.Aliases[i]) == normalized)
                    return true;
            }

            return false;
        }

        private static bool IsLandmarkCategory(AIGMNavigationRouteNodeCategory category)
        {
            return category == AIGMNavigationRouteNodeCategory.Road
                || category == AIGMNavigationRouteNodeCategory.Canyon
                || category == AIGMNavigationRouteNodeCategory.Cemetery
                || category == AIGMNavigationRouteNodeCategory.Inn
                || category == AIGMNavigationRouteNodeCategory.Cabin
                || category == AIGMNavigationRouteNodeCategory.DungeonApproach
                || category == AIGMNavigationRouteNodeCategory.TownApproach
                || category == AIGMNavigationRouteNodeCategory.RoadSign
                || category == AIGMNavigationRouteNodeCategory.House
                || category == AIGMNavigationRouteNodeCategory.GuardHouse
                || category == AIGMNavigationRouteNodeCategory.TownNode
                || category == AIGMNavigationRouteNodeCategory.BritainApproach
                || category == AIGMNavigationRouteNodeCategory.MajorRoad
                || category == AIGMNavigationRouteNodeCategory.Crossroad
                || category == AIGMNavigationRouteNodeCategory.Landmark;
        }

        private static string[] Links(params string[] ids)
        {
            return ids ?? new string[0];
        }

        private static int GetDistance(Point3D a, Point3D b)
        {
            int dx = a.X - b.X;
            int dy = a.Y - b.Y;
            return (int)Math.Round(Math.Sqrt((dx * dx) + (dy * dy)));
        }

        private static string FormatPoint(Point3D point)
        {
            return point.X + "," + point.Y + "," + point.Z;
        }
    }
}
