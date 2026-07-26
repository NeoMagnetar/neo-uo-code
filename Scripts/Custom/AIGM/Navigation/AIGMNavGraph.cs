using System;
using System.Collections.Generic;

using Server;

namespace Server.Custom.AIGM.Navigation
{
    public sealed class AIGMNavGraph
    {
        private readonly List<AIGMNavNode> _nodes = new List<AIGMNavNode>();
        private readonly List<AIGMNavEdge> _edges = new List<AIGMNavEdge>();

        public List<AIGMNavNode> Nodes { get { return _nodes; } }
        public List<AIGMNavEdge> Edges { get { return _edges; } }

        public AIGMNavNode FindNode(string text)
        {
            if (String.IsNullOrWhiteSpace(text))
                return null;

            for (int i = 0; i < _nodes.Count; i++)
            {
                if (_nodes[i] != null && _nodes[i].Matches(text))
                    return _nodes[i];
            }

            return null;
        }

        public AIGMNavNode FindNodeById(string id)
        {
            if (String.IsNullOrWhiteSpace(id))
                return null;

            for (int i = 0; i < _nodes.Count; i++)
            {
                if (_nodes[i] != null && String.Equals(_nodes[i].Id, id, StringComparison.OrdinalIgnoreCase))
                    return _nodes[i];
            }

            return null;
        }

        public AIGMNavNode AddOrReplaceNode(AIGMNavNode node)
        {
            if (node == null || String.IsNullOrWhiteSpace(node.Id))
                return null;

            for (int i = 0; i < _nodes.Count; i++)
            {
                if (_nodes[i] != null && String.Equals(_nodes[i].Id, node.Id, StringComparison.OrdinalIgnoreCase))
                {
                    _nodes[i] = node;
                    return node;
                }
            }

            _nodes.Add(node);
            return node;
        }

        public AIGMNavEdge AddOrReplaceEdge(AIGMNavEdge edge)
        {
            if (edge == null || String.IsNullOrWhiteSpace(edge.FromNodeId) || String.IsNullOrWhiteSpace(edge.ToNodeId))
                return null;

            for (int i = 0; i < _edges.Count; i++)
            {
                AIGMNavEdge existing = _edges[i];
                if (existing != null && existing.Connects(edge.FromNodeId, edge.ToNodeId))
                {
                    _edges[i] = edge;
                    return edge;
                }
            }

            _edges.Add(edge);
            return edge;
        }

        public List<AIGMNavEdge> GetOutgoing(AIGMNavNode node)
        {
            return GetOutgoing(node, true);
        }

        public List<AIGMNavEdge> GetOutgoing(AIGMNavNode node, bool allowUnverifiedEdges)
        {
            List<AIGMNavEdge> result = new List<AIGMNavEdge>();
            if (node == null)
                return result;

            for (int i = 0; i < _edges.Count; i++)
            {
                AIGMNavEdge edge = _edges[i];
                if (edge == null || edge.IsPlannerBlocked(allowUnverifiedEdges))
                    continue;

                if (String.Equals(edge.FromNodeId, node.Id, StringComparison.OrdinalIgnoreCase)
                    || (edge.Bidirectional && String.Equals(edge.ToNodeId, node.Id, StringComparison.OrdinalIgnoreCase)))
                    result.Add(edge);
            }

            return result;
        }

        public AIGMNavNode GetOther(AIGMNavEdge edge, AIGMNavNode from)
        {
            if (edge == null || from == null)
                return null;

            string id = String.Equals(edge.FromNodeId, from.Id, StringComparison.OrdinalIgnoreCase) ? edge.ToNodeId : edge.FromNodeId;
            return FindNodeById(id);
        }

        public List<AIGMNavNode> Nearest(Point3D point, Map map, int maxCount, int maxDistance)
        {
            List<AIGMNavNode> result = new List<AIGMNavNode>();
            for (int i = 0; i < _nodes.Count; i++)
            {
                AIGMNavNode node = _nodes[i];
                if (node == null || !node.Enabled || node.Map != map)
                    continue;

                if (maxDistance > 0 && Distance(point, node.Location) > maxDistance)
                    continue;

                result.Add(node);
            }

            result.Sort(delegate(AIGMNavNode a, AIGMNavNode b)
            {
                return Distance(point, a.Location).CompareTo(Distance(point, b.Location));
            });

            if (maxCount > 0 && result.Count > maxCount)
                result.RemoveRange(maxCount, result.Count - maxCount);

            return result;
        }

        public AIGMNavEdge FindEdge(string from, string to)
        {
            for (int i = 0; i < _edges.Count; i++)
            {
                if (_edges[i] != null && _edges[i].Connects(from, to))
                    return _edges[i];
            }

            return null;
        }

        public static int Distance(Point3D a, Point3D b)
        {
            int dx = a.X - b.X;
            int dy = a.Y - b.Y;
            return (int)Math.Round(Math.Sqrt((dx * dx) + (dy * dy)));
        }
    }
}
