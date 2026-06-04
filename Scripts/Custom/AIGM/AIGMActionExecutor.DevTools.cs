using System;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static partial class AIGMActionExecutor
    {
        private static bool ExecuteSpawnTestCopy(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            IEntity ent = ResolveEntity(action, out message);
            if (ent == null)
                return false;

            Item item = ent as Item;
            if (item == null)
            {
                message = "Spawn test copy currently supports items only. Mobile test-copy support is a later upgrade.";
                return false;
            }

            Item copy = Server.Commands.Dupe.DupeItem(from, item);
            if (copy == null)
            {
                message = "Unable to create a test copy of that item.";
                return false;
            }

            copy.Name = (copy.Name ?? item.Name ?? item.GetType().Name) + " [TEST COPY]";
            copy.Hue = 1150;
            copy.MoveToWorld(from.Location, from.Map);
            from.SendGump(new PropertiesGump(from, copy));
            message = "Spawned a marked test copy of the selected item near you and opened its properties.";
            return true;
        }

        private static bool ExecuteCleanupTestCopies(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            if (from == null || from.Map == null)
            {
                message = "No valid GM position was available for cleanup.";
                return false;
            }

            int removed = 0;
            IPooledEnumerable items = from.Map.GetItemsInRange(from.Location, 8);
            foreach (Item item in items)
            {
                if (item == null || item.Deleted)
                    continue;

                string name = item.Name;
                if (!string.IsNullOrWhiteSpace(name) && name.IndexOf("[TEST COPY]", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    item.Delete();
                    removed++;
                }
            }
            items.Free();

            message = removed > 0
                ? string.Format("Removed {0} nearby marked test cop{1}.", removed, removed == 1 ? "y" : "ies")
                : "No nearby marked test copies were found.";
            return true;
        }

        private static bool ExecuteListTestCopies(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            if (from == null || from.Map == null)
            {
                message = "No valid GM position was available for listing test copies.";
                return false;
            }

            System.Collections.Generic.List<Item> found = new System.Collections.Generic.List<Item>();
            IPooledEnumerable items = from.Map.GetItemsInRange(from.Location, 8);
            foreach (Item item in items)
            {
                if (item == null || item.Deleted)
                    continue;

                string name = item.Name;
                if (!string.IsNullOrWhiteSpace(name) && name.IndexOf("[TEST COPY]", StringComparison.OrdinalIgnoreCase) >= 0)
                    found.Add(item);
            }
            items.Free();

            from.SendGump(new AIGMTestCopyListGump(found));
            message = found.Count > 0
                ? string.Format("Listed {0} nearby marked test cop{1}.", found.Count, found.Count == 1 ? "y" : "ies")
                : "No nearby marked test copies were found.";
            return true;
        }

        private static string SafeMobileName(Mobile mob)
        {
            if (mob == null)
                return "null";

            return String.Format("{0}({1})", mob.Name ?? mob.GetType().Name, mob.Serial.Value);
        }

        private static void LogExecution(string message)
        {
            try
            {
                string path = System.IO.Path.Combine(Core.BaseDirectory, "Logs", "AIGMExecution.log");
                System.IO.File.AppendAllText(path, DateTime.UtcNow.ToString("o") + " " + (message ?? String.Empty) + Environment.NewLine);
            }
            catch
            {
            }
        }
    }
}
