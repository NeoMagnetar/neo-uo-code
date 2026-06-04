using System;
using System.Collections.Generic;
using Server;
using Server.Commands;
using Server.Items;

namespace Server.Custom
{
    public class DevMoongates
    {
        private const string MarkerName = "Dev Moongate";

        private static readonly Point3D Outside = new Point3D(1385, 1497, 10);
        private static readonly Point3D Inside = new Point3D(1385, 1489, 10);
        private static readonly Map GateMap = Map.Felucca;

        public static void Initialize()
        {
            CommandSystem.Register("DevMoongatesGen", AccessLevel.GameMaster, OnGenerate);
            CommandSystem.Register("DevMoongatesDelete", AccessLevel.GameMaster, OnDelete);
        }

        [Usage("DevMoongatesGen")]
        [Description("Creates the paired dev moongates for the walled test area.")]
        private static void OnGenerate(CommandEventArgs e)
        {
            int deleted = ClearExisting();
            int placed = Build();
            e.Mobile.SendMessage("Dev moongates generated. Cleared {0} old gates, placed {1} new gates.", deleted, placed);
        }

        [Usage("DevMoongatesDelete")]
        [Description("Deletes the paired dev moongates for the walled test area.")]
        private static void OnDelete(CommandEventArgs e)
        {
            int deleted = ClearExisting();
            e.Mobile.SendMessage("Dev moongates deleted. Removed {0} gates.", deleted);
        }

        private static int Build()
        {
            CreateGate(Outside, Inside);
            CreateGate(Inside, Outside);
            return 2;
        }

        private static void CreateGate(Point3D source, Point3D dest)
        {
            Moongate gate = new Moongate(dest, GateMap)
            {
                Name = MarkerName,
                Hue = 0x482,
                Dispellable = false,
                Movable = false
            };

            gate.MoveToWorld(source, GateMap);
        }

        private static int ClearExisting()
        {
            List<Item> toDelete = new List<Item>();

            foreach (Item item in World.Items.Values)
            {
                if (item != null && !item.Deleted && item.Map == GateMap && item.Name == MarkerName)
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
