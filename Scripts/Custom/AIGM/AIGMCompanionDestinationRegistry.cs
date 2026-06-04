using System;
using System.Collections.Generic;
using Server;

namespace Server.Custom.AIGM
{
    public sealed class AIGMCompanionDestination
    {
        public string Name;
        public string[] Aliases;
        public Map Map;
        public int X;
        public int Y;
        public int Z;
        public int ArrivalRadius;
        public string Notes;
    }

    public static class AIGMCompanionDestinationRegistry
    {
        private static readonly List<AIGMCompanionDestination> Destinations = new List<AIGMCompanionDestination>
        {
            new AIGMCompanionDestination { Name = "Britain", Aliases = new [] { "britain", "brit" }, Map = Map.Felucca, X = 1496, Y = 1628, Z = 20, ArrivalRadius = 2, Notes = "Britain center/bank area" },
            new AIGMCompanionDestination { Name = "Britain Bank", Aliases = new [] { "britain bank", "brit bank", "bank of britain" }, Map = Map.Felucca, X = 1496, Y = 1628, Z = 20, ArrivalRadius = 2, Notes = "Britain bank" },
            new AIGMCompanionDestination { Name = "Britain Moongate", Aliases = new [] { "britain moongate", "britain gate", "brit moongate", "brit gate" }, Map = Map.Felucca, X = 1336, Y = 1997, Z = 5, ArrivalRadius = 2, Notes = "Britain moongate" },

            new AIGMCompanionDestination { Name = "Minoc", Aliases = new [] { "minoc" }, Map = Map.Felucca, X = 2471, Y = 439, Z = 15, ArrivalRadius = 2, Notes = "Minoc town center" },
            new AIGMCompanionDestination { Name = "Minoc Bank", Aliases = new [] { "minoc bank", "bank of minoc" }, Map = Map.Felucca, X = 2505, Y = 563, Z = 0, ArrivalRadius = 2, Notes = "Minoc bank" },
            new AIGMCompanionDestination { Name = "Minoc Moongate", Aliases = new [] { "minoc moongate", "minoc gate" }, Map = Map.Felucca, X = 2701, Y = 692, Z = 5, ArrivalRadius = 2, Notes = "Minoc moongate" },

            new AIGMCompanionDestination { Name = "Vesper", Aliases = new [] { "vesper" }, Map = Map.Felucca, X = 2899, Y = 676, Z = 0, ArrivalRadius = 2, Notes = "Vesper town center" },
            new AIGMCompanionDestination { Name = "Vesper Bank", Aliases = new [] { "vesper bank", "bank of vesper" }, Map = Map.Felucca, X = 2899, Y = 676, Z = 0, ArrivalRadius = 2, Notes = "Vesper bank" },
            new AIGMCompanionDestination { Name = "Vesper Moongate", Aliases = new [] { "vesper moongate", "vesper gate" }, Map = Map.Felucca, X = 2703, Y = 2153, Z = 0, ArrivalRadius = 2, Notes = "Vesper moongate" },

            new AIGMCompanionDestination { Name = "Moonglow", Aliases = new [] { "moonglow" }, Map = Map.Felucca, X = 4468, Y = 1283, Z = 5, ArrivalRadius = 2, Notes = "Moonglow town center" },
            new AIGMCompanionDestination { Name = "Moonglow Bank", Aliases = new [] { "moonglow bank", "bank of moonglow" }, Map = Map.Felucca, X = 4468, Y = 1283, Z = 5, ArrivalRadius = 2, Notes = "Moonglow bank" },
            new AIGMCompanionDestination { Name = "Moonglow Moongate", Aliases = new [] { "moonglow moongate", "moonglow gate" }, Map = Map.Felucca, X = 4467, Y = 1283, Z = 5, ArrivalRadius = 2, Notes = "Moonglow moongate area" },

            new AIGMCompanionDestination { Name = "Yew", Aliases = new [] { "yew" }, Map = Map.Felucca, X = 633, Y = 858, Z = 0, ArrivalRadius = 2, Notes = "Yew town center" },
            new AIGMCompanionDestination { Name = "Yew Bank", Aliases = new [] { "yew bank", "bank of yew" }, Map = Map.Felucca, X = 634, Y = 858, Z = 0, ArrivalRadius = 2, Notes = "Yew bank" },
            new AIGMCompanionDestination { Name = "Yew Moongate", Aliases = new [] { "yew moongate", "yew gate" }, Map = Map.Felucca, X = 771, Y = 752, Z = 0, ArrivalRadius = 2, Notes = "Yew moongate" },

            new AIGMCompanionDestination { Name = "Trinsic", Aliases = new [] { "trinsic" }, Map = Map.Felucca, X = 1828, Y = 2828, Z = 0, ArrivalRadius = 2, Notes = "Trinsic town center" },
            new AIGMCompanionDestination { Name = "Trinsic Bank", Aliases = new [] { "trinsic bank", "bank of trinsic" }, Map = Map.Felucca, X = 1828, Y = 2828, Z = 0, ArrivalRadius = 2, Notes = "Trinsic bank" },
            new AIGMCompanionDestination { Name = "Trinsic Moongate", Aliases = new [] { "trinsic moongate", "trinsic gate" }, Map = Map.Felucca, X = 1828, Y = 2948, Z = -20, ArrivalRadius = 2, Notes = "Trinsic moongate" },

            new AIGMCompanionDestination { Name = "Skara Brae", Aliases = new [] { "skara brae", "skara" }, Map = Map.Felucca, X = 643, Y = 2067, Z = 5, ArrivalRadius = 2, Notes = "Skara Brae town center" },
            new AIGMCompanionDestination { Name = "Skara Brae Bank", Aliases = new [] { "skara brae bank", "skara bank", "bank of skara brae" }, Map = Map.Felucca, X = 643, Y = 2067, Z = 5, ArrivalRadius = 2, Notes = "Skara Brae bank" },
            new AIGMCompanionDestination { Name = "Skara Brae Moongate", Aliases = new [] { "skara brae moongate", "skara gate" }, Map = Map.Felucca, X = 643, Y = 2067, Z = 5, ArrivalRadius = 2, Notes = "Skara Brae moongate area" },

            new AIGMCompanionDestination { Name = "Jhelom", Aliases = new [] { "jhelom" }, Map = Map.Felucca, X = 1374, Y = 3826, Z = 0, ArrivalRadius = 2, Notes = "Jhelom town center" },
            new AIGMCompanionDestination { Name = "Jhelom Bank", Aliases = new [] { "jhelom bank", "bank of jhelom" }, Map = Map.Felucca, X = 1374, Y = 3826, Z = 0, ArrivalRadius = 2, Notes = "Jhelom bank" },
            new AIGMCompanionDestination { Name = "Jhelom Moongate", Aliases = new [] { "jhelom moongate", "jhelom gate" }, Map = Map.Felucca, X = 1499, Y = 3771, Z = 5, ArrivalRadius = 2, Notes = "Jhelom moongate" },

            new AIGMCompanionDestination { Name = "Magincia", Aliases = new [] { "magincia", "new magincia" }, Map = Map.Felucca, X = 3728, Y = 2165, Z = 20, ArrivalRadius = 2, Notes = "Magincia town center" },
            new AIGMCompanionDestination { Name = "Magincia Bank", Aliases = new [] { "magincia bank", "new magincia bank", "bank of magincia" }, Map = Map.Felucca, X = 3728, Y = 2165, Z = 20, ArrivalRadius = 2, Notes = "Magincia bank" },
            new AIGMCompanionDestination { Name = "Magincia Moongate", Aliases = new [] { "magincia moongate", "magincia gate", "new magincia gate" }, Map = Map.Felucca, X = 3563, Y = 2139, Z = 34, ArrivalRadius = 2, Notes = "Magincia moongate" },

            new AIGMCompanionDestination { Name = "Buccaneer's Den", Aliases = new [] { "buccaneer's den", "buccaneers den", "buccs den", "bucs den", "buccs" }, Map = Map.Felucca, X = 2731, Y = 2162, Z = 0, ArrivalRadius = 2, Notes = "Buccaneer's Den town center" },
            new AIGMCompanionDestination { Name = "Buccaneer's Den Bank", Aliases = new [] { "buccaneer's den bank", "buccaneers den bank", "buccs bank", "bucs bank" }, Map = Map.Felucca, X = 2731, Y = 2162, Z = 0, ArrivalRadius = 2, Notes = "Buccaneer's Den bank" },

            new AIGMCompanionDestination { Name = "Cove", Aliases = new [] { "cove" }, Map = Map.Felucca, X = 2234, Y = 1198, Z = 0, ArrivalRadius = 2, Notes = "Cove town center" },
            new AIGMCompanionDestination { Name = "Cove Bank", Aliases = new [] { "cove bank", "bank of cove" }, Map = Map.Felucca, X = 2234, Y = 1198, Z = 0, ArrivalRadius = 2, Notes = "Cove bank" },

            new AIGMCompanionDestination { Name = "Occlo", Aliases = new [] { "occlo" }, Map = Map.Felucca, X = 3669, Y = 2523, Z = 0, ArrivalRadius = 2, Notes = "Occlo town center" },
            new AIGMCompanionDestination { Name = "Occlo Bank", Aliases = new [] { "occlo bank", "bank of occlo" }, Map = Map.Felucca, X = 3669, Y = 2523, Z = 0, ArrivalRadius = 2, Notes = "Occlo bank" },

            new AIGMCompanionDestination { Name = "Delucia", Aliases = new [] { "delucia", "del" }, Map = Map.Felucca, X = 5228, Y = 3980, Z = 37, ArrivalRadius = 2, Notes = "Delucia town center" },
            new AIGMCompanionDestination { Name = "Delucia Bank", Aliases = new [] { "delucia bank", "bank of delucia" }, Map = Map.Felucca, X = 5228, Y = 3980, Z = 37, ArrivalRadius = 2, Notes = "Delucia bank" },

            new AIGMCompanionDestination { Name = "Papua", Aliases = new [] { "papua" }, Map = Map.Felucca, X = 5723, Y = 3204, Z = -1, ArrivalRadius = 2, Notes = "Papua town center" },
            new AIGMCompanionDestination { Name = "Papua Bank", Aliases = new [] { "papua bank", "bank of papua" }, Map = Map.Felucca, X = 5723, Y = 3204, Z = -1, ArrivalRadius = 2, Notes = "Papua bank" },

            new AIGMCompanionDestination { Name = "Compassion Shrine", Aliases = new [] { "compassion shrine", "shrine of compassion" }, Map = Map.Felucca, X = 1856, Y = 866, Z = -1, ArrivalRadius = 2, Notes = "Compassion shrine" },
            new AIGMCompanionDestination { Name = "Honesty Shrine", Aliases = new [] { "honesty shrine", "shrine of honesty" }, Map = Map.Felucca, X = 4215, Y = 563, Z = 36, ArrivalRadius = 2, Notes = "Honesty shrine" },
            new AIGMCompanionDestination { Name = "Honor Shrine", Aliases = new [] { "honor shrine", "shrine of honor" }, Map = Map.Felucca, X = 1730, Y = 3528, Z = 3, ArrivalRadius = 2, Notes = "Honor shrine" },
            new AIGMCompanionDestination { Name = "Humility Shrine", Aliases = new [] { "humility shrine", "shrine of humility" }, Map = Map.Felucca, X = 4274, Y = 3699, Z = 0, ArrivalRadius = 2, Notes = "Humility shrine" },
            new AIGMCompanionDestination { Name = "Justice Shrine", Aliases = new [] { "justice shrine", "shrine of justice" }, Map = Map.Felucca, X = 1301, Y = 634, Z = 16, ArrivalRadius = 2, Notes = "Justice shrine" },
            new AIGMCompanionDestination { Name = "Sacrifice Shrine", Aliases = new [] { "sacrifice shrine", "shrine of sacrifice" }, Map = Map.Felucca, X = 3355, Y = 298, Z = 9, ArrivalRadius = 2, Notes = "Sacrifice shrine" },
            new AIGMCompanionDestination { Name = "Spirituality Shrine", Aliases = new [] { "spirituality shrine", "shrine of spirituality" }, Map = Map.Felucca, X = 1600, Y = 2489, Z = 5, ArrivalRadius = 2, Notes = "Spirituality shrine" },
            new AIGMCompanionDestination { Name = "Valor Shrine", Aliases = new [] { "valor shrine", "shrine of valor" }, Map = Map.Felucca, X = 2496, Y = 3931, Z = 0, ArrivalRadius = 2, Notes = "Valor shrine" },

            new AIGMCompanionDestination { Name = "Despise Entrance", Aliases = new [] { "despise", "despise entrance" }, Map = Map.Felucca, X = 1296, Y = 1080, Z = 0, ArrivalRadius = 2, Notes = "Despise entrance" },
            new AIGMCompanionDestination { Name = "Deceit Entrance", Aliases = new [] { "deceit", "deceit entrance" }, Map = Map.Felucca, X = 4111, Y = 434, Z = 5, ArrivalRadius = 2, Notes = "Deceit entrance" },
            new AIGMCompanionDestination { Name = "Destard Entrance", Aliases = new [] { "destard", "destard entrance" }, Map = Map.Felucca, X = 1176, Y = 2635, Z = 0, ArrivalRadius = 2, Notes = "Destard entrance" },
            new AIGMCompanionDestination { Name = "Covetous Entrance", Aliases = new [] { "covetous", "covetous entrance" }, Map = Map.Felucca, X = 2499, Y = 919, Z = 0, ArrivalRadius = 2, Notes = "Covetous entrance" },
            new AIGMCompanionDestination { Name = "Wrong Entrance", Aliases = new [] { "wrong", "wrong entrance" }, Map = Map.Felucca, X = 2041, Y = 238, Z = 10, ArrivalRadius = 2, Notes = "Wrong entrance" },
            new AIGMCompanionDestination { Name = "Shame Entrance", Aliases = new [] { "shame", "shame entrance" }, Map = Map.Felucca, X = 514, Y = 1563, Z = 0, ArrivalRadius = 2, Notes = "Shame entrance" },
            new AIGMCompanionDestination { Name = "Hythloth Entrance", Aliases = new [] { "hythloth", "hythloth entrance" }, Map = Map.Felucca, X = 4721, Y = 3814, Z = 0, ArrivalRadius = 2, Notes = "Hythloth entrance" },

            new AIGMCompanionDestination { Name = "Lord British Castle", Aliases = new [] { "lord british castle", "british castle", "castle britain" }, Map = Map.Felucca, X = 1323, Y = 1624, Z = 55, ArrivalRadius = 2, Notes = "Castle Britannia" },
            new AIGMCompanionDestination { Name = "West Britain Bank", Aliases = new [] { "west britain bank", "west brit bank", "wbb" }, Map = Map.Felucca, X = 1175, Y = 1687, Z = 0, ArrivalRadius = 2, Notes = "West Britain bank" }
        };

        public static bool TryResolve(string raw, out AIGMCompanionDestination destination)
        {
            destination = null;
            if (String.IsNullOrWhiteSpace(raw))
                return false;

            string normalized = raw.Trim().ToLowerInvariant();
            for (int i = 0; i < Destinations.Count; i++)
            {
                AIGMCompanionDestination candidate = Destinations[i];
                if (candidate == null)
                    continue;

                if (String.Equals(candidate.Name, raw, StringComparison.OrdinalIgnoreCase))
                {
                    destination = candidate;
                    return true;
                }

                if (candidate.Aliases != null)
                {
                    for (int j = 0; j < candidate.Aliases.Length; j++)
                    {
                        if (String.Equals(candidate.Aliases[j], normalized, StringComparison.OrdinalIgnoreCase))
                        {
                            destination = candidate;
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
