using System;
using System.Runtime.Serialization;

using Server;

namespace Server.Custom.AIGM.Navigation
{
    public enum AIGMNavNodeType
    {
        Road,
        Bridge,
        Gate,
        Door,
        BuildingEntrance,
        Corner,
        Landmark,
        DungeonEntrance,
        TownHub,
        Shrine,
        Bank,
        Moongate,
        Connector,
        SafeStop,
        HazardAvoidance
    }

    [DataContract]
    public sealed class AIGMNavNode
    {
        [DataMember(Name = "id")]
        public string Id;

        [DataMember(Name = "name")]
        public string Name;

        [DataMember(Name = "aliases")]
        public string[] Aliases;

        [DataMember(Name = "map")]
        public string MapName;

        [DataMember(Name = "x")]
        public int X;

        [DataMember(Name = "y")]
        public int Y;

        [DataMember(Name = "z")]
        public int Z;

        [DataMember(Name = "type")]
        public string TypeName;

        [DataMember(Name = "arrivalRadius")]
        public int ArrivalRadius;

        [DataMember(Name = "linkRadius")]
        public int LinkRadius;

        [DataMember(Name = "enabled")]
        public bool Enabled;

        [DataMember(Name = "region")]
        public string Region;

        [DataMember(Name = "zone")]
        public string Zone;

        [DataMember(Name = "tags")]
        public string[] Tags;

        public AIGMNavNode()
        {
            Aliases = new string[0];
            Tags = new string[0];
            Enabled = true;
            ArrivalRadius = 4;
            LinkRadius = 14;
            TypeName = AIGMNavNodeType.Road.ToString();
            MapName = "Felucca";
        }

        public AIGMNavNodeType Type
        {
            get
            {
                AIGMNavNodeType type;
                return Enum.TryParse(TypeName, true, out type) ? type : AIGMNavNodeType.Road;
            }
            set { TypeName = value.ToString(); }
        }

        public Map Map
        {
            get { return ResolveMap(MapName); }
        }

        public Point3D Location
        {
            get { return new Point3D(X, Y, Z); }
            set
            {
                X = value.X;
                Y = value.Y;
                Z = value.Z;
            }
        }

        public bool Matches(string text)
        {
            string normalized = Normalize(text);
            if (String.IsNullOrWhiteSpace(normalized))
                return false;

            if (Normalize(Id) == normalized || Normalize(Name) == normalized)
                return true;

            for (int i = 0; Aliases != null && i < Aliases.Length; i++)
            {
                if (Normalize(Aliases[i]) == normalized)
                    return true;
            }

            return false;
        }

        public void AddAlias(string alias)
        {
            if (String.IsNullOrWhiteSpace(alias) || Matches(alias))
                return;

            string[] old = Aliases ?? new string[0];
            string[] next = new string[old.Length + 1];
            for (int i = 0; i < old.Length; i++)
                next[i] = old[i];
            next[next.Length - 1] = alias.Trim();
            Aliases = next;
        }

        public static string Normalize(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return String.Empty;

            string normalized = value.Trim().ToLowerInvariant();
            normalized = normalized.Replace("'", String.Empty).Replace("-", " ").Replace("_", " ");
            while (normalized.Contains("  "))
                normalized = normalized.Replace("  ", " ");
            return normalized;
        }

        public static string BuildId(string value)
        {
            string normalized = Normalize(value);
            if (String.IsNullOrWhiteSpace(normalized))
                normalized = "node";
            return normalized.Replace(" ", "_");
        }

        public static Map ResolveMap(string mapName)
        {
            if (String.IsNullOrWhiteSpace(mapName))
                return Map.Felucca;

            string normalized = mapName.Trim().ToLowerInvariant();
            if (normalized == "felucca")
                return Map.Felucca;
            if (normalized == "trammel")
                return Map.Trammel;
            if (normalized == "ilshenar")
                return Map.Ilshenar;
            if (normalized == "malas")
                return Map.Malas;
            if (normalized == "tokuno")
                return Map.Tokuno;
            if (normalized == "termur")
                return Map.TerMur;

            return Map.Felucca;
        }
    }
}
