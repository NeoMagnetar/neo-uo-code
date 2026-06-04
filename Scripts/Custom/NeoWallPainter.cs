using System;
using System.Collections.Generic;
using Server;
using Server.Commands;
using Server.Items;

namespace Server.Custom
{
    public static class NeoWallPainter
    {
        private const string MarkerName = "NeoUO Painted Wall";
        private const int WallID = 0x0080;

        private static readonly HashSet<Serial> ActivePainters = new HashSet<Serial>();

        public static void Initialize()
        {
            CommandSystem.Register("NeoWallStart", AccessLevel.GameMaster, OnStart);
            CommandSystem.Register("NeoWallStop", AccessLevel.GameMaster, OnStop);
            CommandSystem.Register("NeoWallClear", AccessLevel.GameMaster, OnClear);

            EventSink.Movement += OnMovement;
        }

        [Usage("NeoWallStart")]
        [Description("Starts painting one wall tile per movement step for the GM.")]
        private static void OnStart(CommandEventArgs e)
        {
            if (e.Mobile == null)
                return;

            ActivePainters.Add(e.Mobile.Serial);
            e.Mobile.SendMessage("NeoWall painter ON.");
        }

        [Usage("NeoWallStop")]
        [Description("Stops painting wall tiles on movement.")]
        private static void OnStop(CommandEventArgs e)
        {
            if (e.Mobile == null)
                return;

            ActivePainters.Remove(e.Mobile.Serial);
            e.Mobile.SendMessage("NeoWall painter OFF.");
        }

        [Usage("NeoWallClear")]
        [Description("Clears all previously painted NeoUO wall tiles.")]
        private static void OnClear(CommandEventArgs e)
        {
            int cleared = ClearExisting();
            e.Mobile.SendMessage("NeoWall cleared {0} painted wall tiles.", cleared);
        }

        private static void OnMovement(MovementEventArgs e)
        {
            Mobile m = e.Mobile;

            if (m == null || m.Deleted || m.Map != Map.Felucca)
                return;

            if (!ActivePainters.Contains(m.Serial))
                return;

            if (m.AccessLevel < AccessLevel.GameMaster)
                return;

            Point3D loc = m.Location;

            foreach (Item item in m.Map.GetItemsInRange(loc, 0))
            {
                if (!item.Deleted && item.Name == MarkerName && item.Location == loc)
                    return;
            }

            Static wall = new Static(WallID)
            {
                Name = MarkerName,
                Movable = false
            };

            wall.MoveToWorld(loc, m.Map);
        }

        private static int ClearExisting()
        {
            List<Item> toDelete = new List<Item>();

            foreach (Item item in World.Items.Values)
            {
                if (item != null && !item.Deleted && item.Name == MarkerName)
                    toDelete.Add(item);
            }

            for (int i = 0; i < toDelete.Count; i++)
                toDelete[i].Delete();

            return toDelete.Count;
        }
    }
}
