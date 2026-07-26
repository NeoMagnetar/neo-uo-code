using System;
using System.Collections.Generic;

using Server;

namespace Server.Custom.AIGM
{
    public sealed class AIGMNavigationLocation
    {
        public AIGMNavigationLocation(string key, string displayName, string category, Map map, Point3D point, params string[] aliases)
        {
            Key = key;
            DisplayName = displayName;
            Category = category;
            Map = map;
            Point = point;
            Aliases = aliases ?? new string[0];
        }

        public string Key { get; private set; }
        public string DisplayName { get; private set; }
        public string Category { get; private set; }
        public Map Map { get; private set; }
        public Point3D Point { get; private set; }
        public string[] Aliases { get; private set; }
    }

    public static class AIGMNavigationLocationRegistry
    {
        private static readonly AIGMNavigationLocation[] Locations =
        {
            // Town points are from existing storm/map/deco references in this shard.
            new AIGMNavigationLocation("britain", "Britain", "Town", Map.Felucca, new Point3D(1522, 1757, 28), "brit"),
            new AIGMNavigationLocation("minoc", "Minoc", "Town", Map.Felucca, new Point3D(2498, 392, 0)),
            new AIGMNavigationLocation("vesper", "Vesper", "Town", Map.Felucca, new Point3D(2973, 891, 0)),
            new AIGMNavigationLocation("trinsic", "Trinsic", "Town", Map.Felucca, new Point3D(1993, 2827, 0)),
            new AIGMNavigationLocation("jhelom", "Jhelom", "Town", Map.Felucca, new Point3D(490, 1166, 0)),
            new AIGMNavigationLocation("moonglow", "Moonglow", "Town", Map.Felucca, new Point3D(4444, 1061, 0)),
            new AIGMNavigationLocation("yew", "Yew", "Town", Map.Felucca, new Point3D(501, 1005, 0)),
            new AIGMNavigationLocation("magincia", "Magincia", "Town", Map.Felucca, new Point3D(3669, 2099, 20), "new magincia"),
            new AIGMNavigationLocation("cove", "Cove", "Town", Map.Felucca, new Point3D(2230, 1159, 0)),
            new AIGMNavigationLocation("skara-brae", "Skara Brae", "Town", Map.Felucca, new Point3D(742, 2216, 0), "skara", "skara brae"),
            new AIGMNavigationLocation("buccaneers-den", "Buccaneer's Den", "Town", Map.Felucca, new Point3D(2750, 2150, 0), "bucs den", "buccaneers den"),

            // Banks and moongates are from Data/Common.map.
            new AIGMNavigationLocation("britain-bank", "First Bank of Britain", "Bank", Map.Felucca, new Point3D(1425, 1690, 0), "first bank of britain", "brit bank", "britain bank", "bank of britain", "first bank", "west britain bank"),
            new AIGMNavigationLocation("east-britain-bank", "East Britain Bank", "Bank", Map.Felucca, new Point3D(1655, 1606, 0), "ebb"),
            new AIGMNavigationLocation("britain-moongate", "Britain Moongate", "Moongate", Map.Felucca, new Point3D(1336, 1997, 0), "brit moongate"),
            new AIGMNavigationLocation("minoc-bank", "Minoc Bank", "Bank", Map.Felucca, new Point3D(2503, 552, 0)),
            new AIGMNavigationLocation("minoc-moongate", "Minoc Moongate", "Moongate", Map.Felucca, new Point3D(2702, 692, 0)),
            new AIGMNavigationLocation("vesper-bank", "Vesper Bank", "Bank", Map.Felucca, new Point3D(2881, 684, 0)),
            new AIGMNavigationLocation("moonglow-bank", "Moonglow Bank", "Bank", Map.Felucca, new Point3D(4471, 1156, 0)),
            new AIGMNavigationLocation("moonglow-moongate", "Moonglow Moongate", "Moongate", Map.Felucca, new Point3D(4468, 1284, 0)),
            new AIGMNavigationLocation("skara-brae-bank", "Skara Brae Bank", "Bank", Map.Felucca, new Point3D(587, 2146, 0), "skara bank"),
            new AIGMNavigationLocation("skara-brae-moongate", "Skara Brae Moongate", "Moongate", Map.Felucca, new Point3D(645, 2068, 0), "skara moongate"),
            new AIGMNavigationLocation("trinsic-bank", "Trinsic Bank", "Bank", Map.Felucca, new Point3D(1897, 2684, 0)),
            new AIGMNavigationLocation("trinsic-moongate", "Trinsic Moongate", "Moongate", Map.Felucca, new Point3D(1829, 2949, 0)),
            new AIGMNavigationLocation("jhelom-bank", "Jhelom Bank", "Bank", Map.Felucca, new Point3D(1317, 3773, 0)),
            new AIGMNavigationLocation("jhelom-moongate", "Jhelom Moongate", "Moongate", Map.Felucca, new Point3D(1500, 3772, 0)),

            // Phase60O field landmarks from live route screenshots, existing route graph anchors, and local client data.
            new AIGMNavigationLocation("xroad-house", "Xroad House", "Landmark", Map.Felucca, new Point3D(1002, 1936, 7), "xroad", "crossroad house", "x road house"),
            new AIGMNavigationLocation("brit-canyon", "Brit Canyon", "Landmark", Map.Felucca, new Point3D(1098, 1928, 7), "britain canyon"),
            new AIGMNavigationLocation("yew-road", "Yew Road", "Road", Map.Felucca, new Point3D(885, 1682, 0), "yew britain road", "road to yew"),
            new AIGMNavigationLocation("brit-graveyard", "Brit Graveyard", "Cemetery", Map.Felucca, new Point3D(1371, 1482, 10), "brit cemetary", "brit cemetery", "brit graveyard", "britain graveyard", "graveyard", "britain cemetery", "brit cemetery road"),
            new AIGMNavigationLocation("despise-canyon", "Despise Canyon", "Canyon", Map.Felucca, new Point3D(1371, 1350, 0), "despise canyon road"),
            new AIGMNavigationLocation("despise-canyon-1", "Despise Canyon1", "Canyon", Map.Felucca, new Point3D(1374, 1230, 0), "despise canyon 1", "despise canyon one"),
            new AIGMNavigationLocation("despise-canyon-2", "Despise Canyon2", "Canyon", Map.Felucca, new Point3D(1368, 1145, 0), "despise canyon 2", "despise canyon two"),
            new AIGMNavigationLocation("despise-entrance", "Despise Entrance", "DungeonEntrance", Map.Felucca, new Point3D(1361, 1071, 4), "despise dungeon entrance"),
            new AIGMNavigationLocation("chaos", "Chaos", "Shrine", Map.Felucca, new Point3D(1458, 844, 7), "chaos shrine"),
            new AIGMNavigationLocation("rotting-cabin", "Rotting Cabin", "Cabin", Map.Felucca, new Point3D(1474, 786, 20), "rotten cabin"),
            new AIGMNavigationLocation("here-lies-culas", "Here Lies Culas", "Landmark", Map.Felucca, new Point3D(1474, 786, 20), "culas"),
            new AIGMNavigationLocation("drunken-inn", "Drunken Inn", "Inn", Map.Felucca, new Point3D(1228, 1705, 0), "drunken inn"),
            new AIGMNavigationLocation("bones-o-scum", "Bones O' Scum", "Landmark", Map.Felucca, new Point3D(1098, 1928, 7), "bones scum", "bones o scum"),
            new AIGMNavigationLocation("to-britain", "To Britain", "RoadSign", Map.Felucca, new Point3D(1392, 1714, 20), "to britain", "britain road sign", "road to britain"),
            new AIGMNavigationLocation("the-moat", "The Moat", "Landmark", Map.Felucca, new Point3D(1401, 1683, 30), "the moat", "moat", "brit moat"),
            new AIGMNavigationLocation("brit-guard-house", "Brit Guard House", "House", Map.Felucca, new Point3D(1414, 1716, 20), "brit guard house", "britain guard house", "guard house", "guardhouse"),
            new AIGMNavigationLocation("the-moat-2", "The Moat2", "Landmark", Map.Felucca, new Point3D(1397, 1655, 30), "the moat2", "moat2", "the moat 2", "moat 2", "the moat two", "moat two", "second moat"),
            new AIGMNavigationLocation("the-great-northern-road", "The Great Northern Road", "Road", Map.Felucca, new Point3D(1371, 1482, 0), "great northern road", "the great northern road", "northern road", "britain northern road"),

            // Shrine points are from Data/Common.map; earlier decoration entries mark some as unconfirmed.
            new AIGMNavigationLocation("compassion-shrine", "Compassion Shrine", "Shrine", Map.Felucca, new Point3D(1858, 874, 0), "compassion"),
            new AIGMNavigationLocation("honesty-shrine", "Honesty Shrine", "Shrine", Map.Felucca, new Point3D(4212, 563, 0), "honesty"),
            new AIGMNavigationLocation("honor-shrine", "Honor Shrine", "Shrine", Map.Felucca, new Point3D(1723, 3527, 0), "honor"),
            new AIGMNavigationLocation("humility-shrine", "Humility Shrine", "Shrine", Map.Felucca, new Point3D(4274, 3697, 0), "humility"),
            new AIGMNavigationLocation("justice-shrine", "Justice Shrine", "Shrine", Map.Felucca, new Point3D(1300, 633, 0), "justice"),
            new AIGMNavigationLocation("sacrifice-shrine", "Sacrifice Shrine", "Shrine", Map.Felucca, new Point3D(3355, 289, 0), "sacrifice"),
            new AIGMNavigationLocation("spirituality-shrine", "Spirituality Shrine", "Shrine", Map.Felucca, new Point3D(1595, 2490, 0), "spirituality"),
            new AIGMNavigationLocation("valor-shrine", "Valor Shrine", "Shrine", Map.Felucca, new Point3D(2491, 3933, 0), "valor"),

            // Dungeon entrances are from Data/Common.map.
            new AIGMNavigationLocation("covetous", "Covetous", "Dungeon", Map.Felucca, new Point3D(2499, 916, 0), "dungeon covetous", "covetous dungeon"),
            new AIGMNavigationLocation("deceit", "Deceit", "Dungeon", Map.Felucca, new Point3D(4111, 429, 0), "dungeon deceit", "deceit dungeon"),
            new AIGMNavigationLocation("despise", "Despise", "Dungeon", Map.Felucca, new Point3D(1296, 1082, 0), "dungeon despise", "despise dungeon"),
            new AIGMNavigationLocation("destard", "Destard", "Dungeon", Map.Felucca, new Point3D(1176, 2635, 0), "dungeon destard", "destard dungeon"),
            new AIGMNavigationLocation("fire", "Fire", "Dungeon", Map.Felucca, new Point3D(2922, 3402, 0), "fire dungeon", "dungeon fire"),
            new AIGMNavigationLocation("hythloth", "Hythloth", "Dungeon", Map.Felucca, new Point3D(4722, 3814, 0), "dungeon hythloth", "hythloth dungeon"),
            new AIGMNavigationLocation("ice", "Ice", "Dungeon", Map.Felucca, new Point3D(1996, 80, 0), "ice dungeon", "dungeon ice"),
            new AIGMNavigationLocation("orc-cave", "Orc Cave", "Dungeon", Map.Felucca, new Point3D(1014, 1434, 0), "orc cave", "dungeon orc cave"),
            new AIGMNavigationLocation("shame", "Shame", "Dungeon", Map.Felucca, new Point3D(512, 1559, 0), "dungeon shame", "shame dungeon"),
            new AIGMNavigationLocation("wrong", "Wrong", "Dungeon", Map.Felucca, new Point3D(2042, 226, 0), "dungeon wrong", "wrong dungeon")
        };

        public static IEnumerable<AIGMNavigationLocation> AllLocations
        {
            get { return Locations; }
        }

        public static bool TryResolve(string raw, Map preferredMap, out AIGMNavigationLocation location)
        {
            location = null;
            string normalized = Normalize(raw);

            if (String.IsNullOrWhiteSpace(normalized))
                return false;

            for (int i = 0; i < Locations.Length; i++)
            {
                AIGMNavigationLocation candidate = Locations[i];
                if (candidate == null)
                    continue;

                if (preferredMap != null && candidate.Map != null && preferredMap != candidate.Map)
                    continue;

                if (Matches(candidate, normalized))
                {
                    location = candidate;
                    return true;
                }
            }

            for (int i = 0; i < Locations.Length; i++)
            {
                AIGMNavigationLocation candidate = Locations[i];
                if (candidate != null && Matches(candidate, normalized))
                {
                    location = candidate;
                    return true;
                }
            }

            return false;
        }

        public static string FormatList()
        {
            return FormatList(null);
        }

        public static string FormatList(string category)
        {
            List<string> names = new List<string>();
            string normalizedCategory = NormalizeCategory(category);

            for (int i = 0; i < Locations.Length; i++)
            {
                if (Locations[i] == null)
                    continue;

                if (!String.IsNullOrWhiteSpace(normalizedCategory) && NormalizeCategory(Locations[i].Category) != normalizedCategory)
                    continue;

                names.Add(Locations[i].DisplayName);
            }

            return names.Count == 0 ? "none" : String.Join(", ", names.ToArray());
        }

        public static string FormatNearest(Point3D from, Map map, int maxCount)
        {
            if (map == null)
                return "No landmark map is available.";

            List<AIGMNavigationLocation> matches = new List<AIGMNavigationLocation>();
            for (int i = 0; i < Locations.Length; i++)
            {
                AIGMNavigationLocation location = Locations[i];
                if (location == null || location.Map != map)
                    continue;

                matches.Add(location);
            }

            matches.Sort(delegate(AIGMNavigationLocation a, AIGMNavigationLocation b)
            {
                return GetDistance(from, a.Point).CompareTo(GetDistance(from, b.Point));
            });

            int count = Math.Max(1, Math.Min(maxCount <= 0 ? 6 : maxCount, matches.Count));
            if (count == 0)
                return "No known landmarks are on this map.";

            List<string> parts = new List<string>();
            for (int i = 0; i < count; i++)
            {
                AIGMNavigationLocation location = matches[i];
                parts.Add(String.Format("{0}, {1} tiles {2}, {3}", location.DisplayName, GetDistance(from, location.Point), FormatDirection(from, location.Point), FormatPoint(location.Point)));
            }

            return String.Join(" | ", parts.ToArray());
        }

        public static string FormatLocationStats(AIGMNavigationLocation location, Point3D from, Map map)
        {
            if (location == null)
                return "That landmark is not known.";

            if (map != null && location.Map != null && map != location.Map)
                return String.Format("{0} is known on {1}, not this map.", location.DisplayName, location.Map.Name ?? location.Map.ToString());

            AIGMNavigationDestinationResolution resolution = AIGMNavigationDestinationResolver.Resolve(location, from, map);
            string standable = resolution.IsStandable ? "standable" : "unreachable";
            return String.Format("{0}: canonical={1}; standTarget={2}; arrivalRadius={3}; distance={4} tiles {5}; target={6}.",
                location.DisplayName,
                FormatPoint(location.Point),
                FormatPoint(resolution.Target),
                resolution.ArrivalRadius,
                GetDistance(from, location.Point),
                FormatDirection(from, location.Point),
                standable);
        }

        internal static string NormalizeForLookup(string value)
        {
            return Normalize(value);
        }

        internal static int GetDistance(Point3D a, Point3D b)
        {
            int dx = a.X - b.X;
            int dy = a.Y - b.Y;
            return (int)Math.Round(Math.Sqrt((dx * dx) + (dy * dy)));
        }

        internal static string FormatDirection(Point3D from, Point3D to)
        {
            int dx = to.X - from.X;
            int dy = to.Y - from.Y;
            if (Math.Abs(dx) <= 1 && Math.Abs(dy) <= 1)
                return "here";

            string ns = dy < -1 ? "north" : (dy > 1 ? "south" : String.Empty);
            string ew = dx < -1 ? "west" : (dx > 1 ? "east" : String.Empty);
            if (!String.IsNullOrWhiteSpace(ns) && !String.IsNullOrWhiteSpace(ew))
                return ns + ew;
            return !String.IsNullOrWhiteSpace(ns) ? ns : ew;
        }

        internal static string FormatPoint(Point3D point)
        {
            return point.X + "," + point.Y + "," + point.Z;
        }

        private static bool Matches(AIGMNavigationLocation location, string normalized)
        {
            if (location == null)
                return false;

            if (Normalize(location.Key) == normalized || Normalize(location.DisplayName) == normalized)
                return true;

            for (int i = 0; location.Aliases != null && i < location.Aliases.Length; i++)
            {
                if (Normalize(location.Aliases[i]) == normalized)
                    return true;
            }

            return false;
        }

        private static string NormalizeCategory(string value)
        {
            string normalized = Normalize(value);
            if (normalized == "towns")
                return "town";
            if (normalized == "shrines")
                return "shrine";
            if (normalized == "dungeons")
                return "dungeon";
            if (normalized == "banks")
                return "bank";
            if (normalized == "moongates")
                return "moongate";
            if (normalized == "landmarks")
                return "landmark";
            if (normalized == "roads")
                return "road";
            if (normalized == "canyons")
                return "canyon";
            if (normalized == "cemeteries")
                return "cemetery";
            if (normalized == "inns")
                return "inn";
            if (normalized == "cabins")
                return "cabin";
            if (normalized == "cemetery")
                return "cemetery";
            if (normalized == "cemetaries" || normalized == "cemetarys")
                return "cemetery";
            if (normalized == "road signs")
                return "roadsign";
            if (normalized == "houses")
                return "house";
            return normalized;
        }

        private static string Normalize(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return String.Empty;

            string normalized = value.Trim().ToLowerInvariant();
            normalized = normalized.Replace("'", String.Empty).Replace("-", " ");
            while (normalized.Contains("  "))
                normalized = normalized.Replace("  ", " ");
            return normalized;
        }
    }
}
