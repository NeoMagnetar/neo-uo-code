using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Server.Custom.AIGM.Navigation
{
    [DataContract]
    internal sealed class AIGMNavNodeFile
    {
        [DataMember(Name = "nodes")]
        public AIGMNavNode[] Nodes;
    }

    [DataContract]
    internal sealed class AIGMNavEdgeFile
    {
        [DataMember(Name = "edges")]
        public AIGMNavEdge[] Edges;
    }

    public static class AIGMNavGraphStore
    {
        private static readonly object SyncRoot = new object();
        private static AIGMNavGraph _graph;
        private static DateTime _loadedUtc;
        private static int _jsonNodeCount;
        private static int _jsonEdgeCount;
        private static int _staticNodeCount;
        private static int _staticEdgeCount;
        private static string _sourceLabel = "empty";

        public static string DataDirectory
        {
            get { return Path.Combine(Core.BaseDirectory, "Data", "AIGM", "Navigation"); }
        }

        public static string NodesPath
        {
            get { return Path.Combine(DataDirectory, "nav-nodes.json"); }
        }

        public static string EdgesPath
        {
            get { return Path.Combine(DataDirectory, "nav-edges.json"); }
        }

        public static string ExportPath
        {
            get { return Path.Combine(DataDirectory, "nav-routes-export.json"); }
        }

        public static AIGMNavGraph Graph
        {
            get
            {
                EnsureLoaded();
                return _graph;
            }
        }

        public static int JsonNodeCount { get { EnsureLoaded(); return _jsonNodeCount; } }
        public static int JsonEdgeCount { get { EnsureLoaded(); return _jsonEdgeCount; } }
        public static int StaticNodeCount { get { EnsureLoaded(); return _staticNodeCount; } }
        public static int StaticEdgeCount { get { EnsureLoaded(); return _staticEdgeCount; } }
        public static string SourceLabel { get { EnsureLoaded(); return _sourceLabel; } }

        public static void EnsureLoaded()
        {
            lock (SyncRoot)
            {
                if (_graph != null)
                    return;

                Reload();
            }
        }

        public static void Reload()
        {
            lock (SyncRoot)
            {
                Directory.CreateDirectory(DataDirectory);
                EnsureFile(NodesPath, "{\"nodes\":[]}");
                EnsureFile(EdgesPath, "{\"edges\":[]}");

                AIGMNavGraph graph = new AIGMNavGraph();
                AIGMNavNodeFile nodeFile = ReadJson<AIGMNavNodeFile>(NodesPath) ?? new AIGMNavNodeFile();
                AIGMNavEdgeFile edgeFile = ReadJson<AIGMNavEdgeFile>(EdgesPath) ?? new AIGMNavEdgeFile();
                _jsonNodeCount = 0;
                _jsonEdgeCount = 0;
                _staticNodeCount = 0;
                _staticEdgeCount = 0;
                _sourceLabel = "empty";

                for (int i = 0; nodeFile.Nodes != null && i < nodeFile.Nodes.Length; i++)
                {
                    AIGMNavNode node = NormalizeNode(nodeFile.Nodes[i]);
                    if (node != null)
                    {
                        graph.AddOrReplaceNode(node);
                        _jsonNodeCount++;
                    }
                }

                for (int i = 0; edgeFile.Edges != null && i < edgeFile.Edges.Length; i++)
                {
                    AIGMNavEdge edge = NormalizeEdge(edgeFile.Edges[i]);
                    if (edge != null)
                    {
                        graph.AddOrReplaceEdge(edge);
                        _jsonEdgeCount++;
                    }
                }

                AddStaticSeedGraph(graph);
                _sourceLabel = BuildSourceLabel();

                _graph = graph;
                _loadedUtc = DateTime.UtcNow;
                AIGMExecutionLog.Write("AIGM_NAV_GRAPH_LOAD nodes={0} edges={1} jsonNodes={2} jsonEdges={3} staticNodes={4} staticEdges={5} source={6} nodesPath=\"{7}\" edgesPath=\"{8}\"", graph.Nodes.Count, graph.Edges.Count, _jsonNodeCount, _jsonEdgeCount, _staticNodeCount, _staticEdgeCount, _sourceLabel, NodesPath, EdgesPath);
            }
        }

        public static string FormatStatus(Map map)
        {
            EnsureLoaded();
            int enabledNodes = 0;
            int enabledEdges = 0;
            for (int i = 0; i < _graph.Nodes.Count; i++)
            {
                AIGMNavNode node = _graph.Nodes[i];
                if (node != null && node.Enabled && (map == null || node.Map == map))
                    enabledNodes++;
            }

            for (int i = 0; i < _graph.Edges.Count; i++)
            {
                AIGMNavEdge edge = _graph.Edges[i];
                if (edge != null && edge.Enabled)
                    enabledEdges++;
            }

            return String.Format("nodesLoaded={0}; edgesLoaded={1}; enabledNodes={2}; enabledEdges={3}; map={4}; graphSource={5}",
                _graph.Nodes.Count,
                _graph.Edges.Count,
                enabledNodes,
                enabledEdges,
                map == null ? "null" : (map.Name ?? map.ToString()),
                _sourceLabel);
        }

        public static void LogStatus(Map map)
        {
            EnsureLoaded();
            int enabledNodes = 0;
            int enabledEdges = 0;
            for (int i = 0; i < _graph.Nodes.Count; i++)
            {
                AIGMNavNode node = _graph.Nodes[i];
                if (node != null && node.Enabled && (map == null || node.Map == map))
                    enabledNodes++;
            }

            for (int i = 0; i < _graph.Edges.Count; i++)
            {
                AIGMNavEdge edge = _graph.Edges[i];
                if (edge != null && edge.Enabled)
                    enabledEdges++;
            }

            AIGMExecutionLog.Write("AIGM_NAV_GRAPH_STATUS nodesLoaded={0} edgesLoaded={1} enabledNodes={2} enabledEdges={3} map={4} graphSource={5} jsonNodes={6} jsonEdges={7} staticNodes={8} staticEdges={9}",
                _graph.Nodes.Count,
                _graph.Edges.Count,
                enabledNodes,
                enabledEdges,
                map == null ? "null" : (map.Name ?? map.ToString()),
                _sourceLabel,
                _jsonNodeCount,
                _jsonEdgeCount,
                _staticNodeCount,
                _staticEdgeCount);
        }

        public static void Save()
        {
            lock (SyncRoot)
            {
                EnsureLoaded();
                Directory.CreateDirectory(DataDirectory);

                AIGMNavNodeFile nodeFile = new AIGMNavNodeFile { Nodes = _graph.Nodes.ToArray() };
                AIGMNavEdgeFile edgeFile = new AIGMNavEdgeFile { Edges = _graph.Edges.ToArray() };
                WriteJson(NodesPath, nodeFile);
                WriteJson(EdgesPath, edgeFile);
                WriteExport();
            }
        }

        public static void WriteExport()
        {
            lock (SyncRoot)
            {
                EnsureLoaded();
                Directory.CreateDirectory(DataDirectory);
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine("  \"nodesPath\": \"" + Escape(NodesPath) + "\",");
                sb.AppendLine("  \"edgesPath\": \"" + Escape(EdgesPath) + "\",");
                sb.AppendLine("  \"loadedUtc\": \"" + _loadedUtc.ToString("o") + "\",");
                sb.AppendLine("  \"graphSource\": \"" + Escape(_sourceLabel) + "\",");
                sb.AppendLine("  \"nodeCount\": " + _graph.Nodes.Count + ",");
                sb.AppendLine("  \"edgeCount\": " + _graph.Edges.Count);
                sb.AppendLine("}");
                File.WriteAllText(ExportPath, sb.ToString());
            }
        }

        private static void AddStaticSeedGraph(AIGMNavGraph graph)
        {
            _staticNodeCount = 0;
            _staticEdgeCount = 0;

            AddStaticNode(graph, "britain_bank", "First Bank of Britain", AIGMNavNodeType.Bank, 1425, 1690, 0, 4, "Britain", "Central Britain", new[] { "first bank of britain", "brit bank", "britain bank", "bank of britain", "first bank" }, new[] { "britain", "bank", "town" });
            AddStaticNode(graph, "to_britain", "To Britain", AIGMNavNodeType.Connector, 1392, 1714, 0, 4, "Britain", "Britain Approach", new[] { "to britain", "britain road sign", "road to britain" }, new[] { "britain", "road", "approach" });
            AddStaticNode(graph, "the_moat", "The Moat", AIGMNavNodeType.Landmark, 1402, 1684, 0, 4, "Britain", "Britain Approach", new[] { "the moat", "moat", "brit moat" }, new[] { "britain", "road", "landmark" });
            AddStaticNode(graph, "brit_guard_house", "Brit Guard House", AIGMNavNodeType.BuildingEntrance, 1414, 1716, 0, 3, "Britain", "Central Britain", new[] { "brit guard house", "britain guard house", "guard house", "guardhouse" }, new[] { "britain", "guard", "building" });
            AddStaticNode(graph, "the_moat2", "The Moat2", AIGMNavNodeType.Landmark, 1397, 1655, 30, 5, "Britain", "Britain Approach", new[] { "the moat2", "moat2", "the moat 2", "moat 2", "the moat two", "moat two", "second moat" }, new[] { "britain", "road", "landmark" });
            AddStaticNode(graph, "the_great_northern_road", "The Great Northern Road", AIGMNavNodeType.Road, 1371, 1482, 0, 4, "Britain", "North Britain", new[] { "great northern road", "the great northern road", "northern road", "britain northern road" }, new[] { "britain", "road", "north" });
            AddStaticNode(graph, "brit_graveyard", "Brit Graveyard", AIGMNavNodeType.Landmark, 1371, 1482, 10, 5, "Britain", "North Britain", new[] { "brit cemetary", "brit cemetery", "brit graveyard", "britain graveyard", "graveyard" }, new[] { "britain", "graveyard", "road" });
            AddStaticNode(graph, "despise_canyon", "Despise Canyon", AIGMNavNodeType.Road, 1371, 1350, 0, 4, "Britain", "Despise Road", new[] { "despise canyon" }, new[] { "despise", "road" });
            AddStaticNode(graph, "despise_canyon1", "Despise Canyon1", AIGMNavNodeType.Road, 1374, 1230, 0, 4, "Britain", "Despise Road", new[] { "despise canyon1", "despise canyon 1" }, new[] { "despise", "road" });
            AddStaticNode(graph, "despise_canyon2", "Despise Canyon2", AIGMNavNodeType.Road, 1368, 1145, 0, 4, "Britain", "Despise Road", new[] { "despise canyon2", "despise canyon 2" }, new[] { "despise", "road" });
            AddStaticNode(graph, "despise_entrance", "Despise Entrance", AIGMNavNodeType.DungeonEntrance, 1361, 1071, 0, 3, "Britain", "Despise", new[] { "despise", "despise entrance", "entrance to despise" }, new[] { "despise", "dungeon", "entrance" });
            AddStaticNode(graph, "xroad_house", "Xroad House", AIGMNavNodeType.Landmark, 1194, 1689, 0, 4, "Britain", "West Britain Road", new[] { "xroad house", "crossroad house", "crossroads house" }, new[] { "britain", "road", "west" });
            AddStaticNode(graph, "brit_canyon", "Brit Canyon", AIGMNavNodeType.Road, 1165, 1608, 0, 4, "Britain", "West Britain Road", new[] { "brit canyon", "britain canyon" }, new[] { "britain", "road", "west" });
            AddStaticNode(graph, "yew_road", "Yew Road", AIGMNavNodeType.Road, 1034, 1524, 0, 4, "Yew", "Yew Road", new[] { "yew road", "road to yew" }, new[] { "yew", "road" });

            AddStaticEdge(graph, "britain_bank", "to_britain");
            AddStaticEdge(graph, "to_britain", "the_moat");
            AddStaticEdge(graph, "the_moat", "brit_guard_house");
            AddStaticEdge(graph, "the_moat", "the_moat2");
            AddStaticEdge(graph, "the_moat2", "the_great_northern_road");
            AddStaticEdge(graph, "the_great_northern_road", "brit_graveyard");
            AddStaticEdge(graph, "brit_graveyard", "despise_canyon");
            AddStaticEdge(graph, "despise_canyon", "despise_canyon1");
            AddStaticEdge(graph, "despise_canyon1", "despise_canyon2");
            AddStaticEdge(graph, "despise_canyon2", "despise_entrance");
            AddStaticEdge(graph, "xroad_house", "brit_canyon");
            AddStaticEdge(graph, "brit_canyon", "yew_road");
        }

        private static void AddStaticNode(AIGMNavGraph graph, string id, string name, AIGMNavNodeType type, int x, int y, int z, int radius, string region, string zone, string[] aliases, string[] tags)
        {
            if (graph.FindNodeById(id) != null)
                return;

            AIGMNavNode node = new AIGMNavNode();
            node.Id = id;
            node.Name = name;
            node.Type = type;
            node.MapName = "Felucca";
            node.Location = new Point3D(x, y, z);
            node.ArrivalRadius = radius;
            node.LinkRadius = 14;
            node.Region = region;
            node.Zone = zone;
            node.Aliases = aliases ?? new string[0];
            node.Tags = tags ?? new string[0];
            node.Enabled = true;
            graph.AddOrReplaceNode(node);
            _staticNodeCount++;
        }

        private static void AddStaticEdge(AIGMNavGraph graph, string from, string to)
        {
            if (graph.FindEdge(from, to) != null)
                return;

            AIGMNavNode a = graph.FindNodeById(from);
            AIGMNavNode b = graph.FindNodeById(to);
            if (a == null || b == null)
                return;

            AIGMNavEdge edge = new AIGMNavEdge();
            edge.FromNodeId = from;
            edge.ToNodeId = to;
            edge.Bidirectional = true;
            edge.Enabled = true;
            edge.Cost = AIGMNavGraph.Distance(a.Location, b.Location);
            edge.CorridorRadius = Math.Max(3, Math.Min(a.LinkRadius, b.LinkRadius));
            edge.Tags = new[] { "static", "road" };
            edge.Status = AIGMNavEdgeStatus.Unverified;
            graph.AddOrReplaceEdge(edge);
            _staticEdgeCount++;
        }

        private static string BuildSourceLabel()
        {
            if (_jsonNodeCount > 0 || _jsonEdgeCount > 0)
                return (_staticNodeCount > 0 || _staticEdgeCount > 0) ? "mixed" : "json";
            return (_staticNodeCount > 0 || _staticEdgeCount > 0) ? "static" : "empty";
        }

        private static AIGMNavNode NormalizeNode(AIGMNavNode node)
        {
            if (node == null)
                return null;

            if (String.IsNullOrWhiteSpace(node.Name))
                node.Name = node.Id;
            if (String.IsNullOrWhiteSpace(node.Id))
                node.Id = AIGMNavNode.BuildId(node.Name);
            if (String.IsNullOrWhiteSpace(node.MapName))
                node.MapName = "Felucca";
            if (String.IsNullOrWhiteSpace(node.TypeName))
                node.TypeName = AIGMNavNodeType.Road.ToString();
            if (node.ArrivalRadius <= 0)
                node.ArrivalRadius = DefaultRadius(node.Type);
            if (node.LinkRadius <= 0)
                node.LinkRadius = 14;
            if (node.Aliases == null)
                node.Aliases = new string[0];
            if (node.Tags == null)
                node.Tags = new string[0];
            return node;
        }

        private static AIGMNavEdge NormalizeEdge(AIGMNavEdge edge)
        {
            if (edge == null || String.IsNullOrWhiteSpace(edge.FromNodeId) || String.IsNullOrWhiteSpace(edge.ToNodeId))
                return null;
            if (edge.CorridorRadius <= 0)
                edge.CorridorRadius = 4;
            if (edge.Tags == null)
                edge.Tags = new string[0];
            if (String.IsNullOrWhiteSpace(edge.StatusName))
                edge.Status = AIGMNavEdgeStatus.Unverified;
            return edge;
        }

        public static int DefaultRadius(AIGMNavNodeType type)
        {
            switch (type)
            {
                case AIGMNavNodeType.Road:
                case AIGMNavNodeType.Connector:
                case AIGMNavNodeType.Bridge:
                    return 3;
                case AIGMNavNodeType.Gate:
                case AIGMNavNodeType.Landmark:
                    return 4;
                case AIGMNavNodeType.TownHub:
                    return 6;
                case AIGMNavNodeType.BuildingEntrance:
                    return 2;
                case AIGMNavNodeType.DungeonEntrance:
                case AIGMNavNodeType.Bank:
                case AIGMNavNodeType.Moongate:
                    return 3;
                default:
                    return 4;
            }
        }

        private static void EnsureFile(string path, string content)
        {
            if (!File.Exists(path))
                File.WriteAllText(path, content);
        }

        private static T ReadJson<T>(string path) where T : class
        {
            try
            {
                using (FileStream stream = File.OpenRead(path))
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
                    return serializer.ReadObject(stream) as T;
                }
            }
            catch (Exception ex)
            {
                AIGMExecutionLog.Write("AIGM_NAV_GRAPH_LOAD_ERROR path=\"{0}\" error=\"{1}\"", path, SafeLog(ex.Message));
                return null;
            }
        }

        private static void WriteJson<T>(string path, T value)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
                serializer.WriteObject(stream, value);
                string json = Encoding.UTF8.GetString(stream.ToArray());
                File.WriteAllText(path, json);
            }
        }

        private static string Escape(string value)
        {
            return String.IsNullOrEmpty(value) ? String.Empty : value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static string SafeLog(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return String.Empty;
            value = value.Replace('"', '\'').Replace('\r', ' ').Replace('\n', ' ');
            return value.Length > 220 ? value.Substring(0, 220) : value;
        }
    }
}
