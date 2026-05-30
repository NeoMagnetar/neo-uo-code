using System;
using System.Collections.Generic;
using Server;
using Server.Commands;
using Server.Items;

namespace Server.Custom
{
    public class LeaguePerimeterShell
    {
        private const string MarkerName = "NeoUO League Perimeter";
        private const int WallNS = 0x0080;
        private const int WallEW = 0x0085;

        public static void Initialize()
        {
            CommandSystem.Register("LeaguePerimeterBuild", AccessLevel.GameMaster, OnBuildCommand);
            CommandSystem.Register("LeaguePerimeterClear", AccessLevel.GameMaster, OnClearCommand);
        }

        [Usage("LeaguePerimeterBuild")]
        [Description("Builds the Dev-only League prototype perimeter shell.")]
        private static void OnBuildCommand(CommandEventArgs e)
        {
            int cleared = ClearExisting();
            int placed = Build();
            e.Mobile.SendMessage("League perimeter rebuilt. Cleared {0} old statics, placed {1} new statics.", cleared, placed);
        }

        [Usage("LeaguePerimeterClear")]
        [Description("Clears previously placed Dev-only League perimeter statics.")]
        private static void OnClearCommand(CommandEventArgs e)
        {
            int cleared = ClearExisting();
            e.Mobile.SendMessage("League perimeter cleared. Removed {0} statics.", cleared);
        }

        private static int Build()
        {
            Map map = Map.Felucca;
            int placed = 0;

            // South border: tighter to the graveyard-facing lower edge.
            placed += AddHorizontal(map, 1120, 1438, 1370, 0, WallEW);
            placed += AddHorizontal(map, 1145, 1442, 1348, 0, WallEW);
            placed += AddHorizontal(map, 1175, 1446, 1328, 0, WallEW);

            // East border: shifted farther east to match the operator's outer boundary anchors.
            placed += AddVertical(map, 1442, 1364, 1442, 0, WallNS);
            placed += AddVertical(map, 1438, 1332, 1438, 0, WallNS);
            placed += AddVertical(map, 1432, 1298, 1434, 0, WallNS);
            placed += AddVertical(map, 1419, 1261, 1425, 0, WallNS);
            placed += AddVertical(map, 1408, 1228, 1412, 0, WallNS);
            placed += AddVertical(map, 1396, 1194, 1398, 0, WallNS);
            placed += AddVertical(map, 1384, 1160, 1382, 0, WallNS);
            placed += AddVertical(map, 1372, 1128, 1368, 0, WallNS);
            placed += AddVertical(map, 1360, 1098, 1352, 0, WallNS);

            return placed;
        }

        private static int AddHorizontal(Map map, int x1, int y, int x2, int z, int itemID)
        {
            int start = Math.Min(x1, x2);
            int end = Math.Max(x1, x2);
            int count = 0;

            for (int x = start; x <= end; x++)
            {
                Place(map, itemID, x, y, z);
                count++;
            }

            return count;
        }

        private static int AddVertical(Map map, int x, int y1, int y2, int z, int itemID)
        {
            int start = Math.Min(y1, y2);
            int end = Math.Max(y1, y2);
            int count = 0;

            for (int y = start; y <= end; y++)
            {
                Place(map, itemID, x, y, z);
                count++;
            }

            return count;
        }

        private static void Place(Map map, int itemID, int x, int y, int z)
        {
            Static tile = new Static(itemID)
            {
                Name = MarkerName,
                Movable = false
            };

            tile.MoveToWorld(new Point3D(x, y, z), map);
        }

        private static int ClearExisting()
        {
            List<Item> toDelete = new List<Item>();

            foreach (Item item in World.Items.Values)
            {
                if (item != null && !item.Deleted && item.Map == Map.Felucca && item.Name == MarkerName)
                {
                    toDelete.Add(item);
                }
            }

            for (int i = 0; i < toDelete.Count; i++)
            {
                toDelete[i].Delete();
            }

            return toDelete.Count;
        }
    }
}
