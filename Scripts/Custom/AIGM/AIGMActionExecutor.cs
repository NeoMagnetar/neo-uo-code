using System;
using Server.Commands;
using Server.Commands.Generic;
using Server.Gumps;
using Server.Mobiles;
using Server.Spells;

namespace Server.Custom.AIGM
{
    public static class AIGMActionExecutor
    {
        public static string BuildFollowupSuggestion(AIGMActionProposal action)
        {
            if (action == null)
                return null;

            switch (action.ActionKind)
            {
                case "inspect_target":
                    return "Want me to review the likely controlling file next?";
                case "goto_target":
                    return "You are at the target now. Want props or a file analysis next?";
                case "review_file":
                    return "Want me to inspect the next related file or reopen target props?";
                case "inspect_vendor_stock":
                    return "Want me to inspect the concrete vendor class or restock the vendor next?";
                case "run_gm_command":
                    if (action.Parameters != null)
                    {
                        string commandName;
                        if (action.Parameters.TryGetValue("commandName", out commandName))
                        {
                            switch (commandName)
                            {
                                case AIGMCommandAction.RestockVendor:
                                    return "Vendor restocked. Want me to inspect stock-related files next?";
                                case AIGMCommandAction.SpawnTestCopy:
                                    return "Test copy created, marked, and opened in props. Want another duplication or cleanup next?";
                                case AIGMCommandAction.CleanupTestCopies:
                                    return "Nearby marked test copies cleaned up. Want me to create a fresh one now?";
                                case AIGMCommandAction.ListTestCopies:
                                    return "Nearby marked test copies listed. Want me to clean them up or inspect one next?";
                                case AIGMCommandAction.OpenPropsOnTarget:
                                    return "Props opened. Want file analysis for this target next?";
                                case AIGMCommandAction.ViewEquipOnTarget:
                                    return "Equipment shown. Want target props or file analysis next?";
                            }
                        }
                    }
                    break;
            }

            return null;
        }

        public static bool Execute(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            if (from == null || action == null)
            {
                message = "No action was available to execute.";
                return false;
            }

            if (from.AccessLevel < AIGMSettings.RequiredAccess)
            {
                message = "You do not have access to execute AI GM actions.";
                return false;
            }

            switch (action.ActionKind)
            {
                case "inspect_target":
                    return ExecuteInspectTarget(from, action, out message);
                case "goto_target":
                    return ExecuteGotoTarget(from, action, out message);
                case "review_file":
                    return ExecuteReviewFile(from, action, out message);
                case "inspect_vendor_stock":
                    return ExecuteInspectVendorStock(from, action, out message);
                case "run_gm_command":
                    return ExecuteRunGmCommand(from, action, out message);
                default:
                    message = "That AI GM action is not executable yet.";
                    return false;
            }
        }

        private static bool ExecuteInspectTarget(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            IEntity ent = ResolveEntity(action, out message);
            if (ent == null)
                return false;
            if (ent == null)
            {
                message = "Target entity was not found.";
                return false;
            }

            if (!BaseCommand.IsAccessible(from, ent))
            {
                message = "That target is not accessible.";
                return false;
            }

            from.SendGump(new PropertiesGump(from, ent));
            message = "Opened properties for the selected AI GM target.";
            return true;
        }

        private static bool ExecuteGotoTarget(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            IEntity ent = ResolveEntity(action, out message);
            if (ent == null)
                return false;

            IPoint3D p = ent as IPoint3D;
            if (p == null)
            {
                message = "That target does not have a valid world location.";
                return false;
            }

            if (p is Item)
                p = ((Item)p).GetWorldTop();
            else if (p is Mobile)
                p = ((Mobile)p).Location;

            SpellHelper.GetSurfaceTop(ref p);
            from.Location = new Point3D(p);
            from.ProcessDelta();

            message = "Teleported to the selected AI GM target.";
            return true;
        }

        private static bool ExecuteReviewFile(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            string path;
            if (action.Parameters == null || !action.Parameters.TryGetValue("path", out path) || String.IsNullOrWhiteSpace(path))
            {
                message = "No file path was provided for review.";
                return false;
            }

            string insight = null;
            if (action.Parameters != null)
                action.Parameters.TryGetValue("insight", out insight);

            if (string.IsNullOrWhiteSpace(insight))
                insight = AIGMFileInsightStore.Get(path);

            if (string.IsNullOrWhiteSpace(insight))
                insight = "No deeper file insight was available yet for that file.";

            from.SendGump(new AIGMFileInsightGump(path, insight));
            message = "Opened AI GM file review insight.";
            return true;
        }

        private static bool ExecuteInspectVendorStock(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            string title = "AI GM Vendor Stock Insight";
            string body = null;

            if (action.Parameters != null)
            {
                action.Parameters.TryGetValue("title", out title);
                action.Parameters.TryGetValue("body", out body);
            }

            if (String.IsNullOrWhiteSpace(body))
                body = "No vendor stock insight payload was provided.";

            from.SendGump(new AIGMVendorInsightGump(title, body));
            message = "Opened AI GM vendor stock insight.";
            return true;
        }

        private static bool ExecuteRunGmCommand(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            string commandName;
            if (action.Parameters == null || !action.Parameters.TryGetValue("commandName", out commandName) || String.IsNullOrWhiteSpace(commandName))
            {
                message = "No GM command action name was provided.";
                return false;
            }

            switch (commandName)
            {
                case AIGMCommandAction.TeleportToTarget:
                    return ExecuteGotoTarget(from, action, out message);
                case AIGMCommandAction.OpenPropsOnTarget:
                    return ExecuteInspectTarget(from, action, out message);
                case AIGMCommandAction.RestockVendor:
                    return ExecuteRestockVendor(from, action, out message);
                case AIGMCommandAction.ViewEquipOnTarget:
                    return ExecuteViewEquipOnTarget(from, action, out message);
                case AIGMCommandAction.SpawnTestCopy:
                    return ExecuteSpawnTestCopy(from, action, out message);
                case AIGMCommandAction.CleanupTestCopies:
                    return ExecuteCleanupTestCopies(from, action, out message);
                case AIGMCommandAction.ListTestCopies:
                    return ExecuteListTestCopies(from, action, out message);
                default:
                    message = "That GM command action is not whitelisted.";
                    return false;
            }
        }

        private static bool ExecuteRestockVendor(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            IEntity ent = ResolveEntity(action, out message);
            if (ent == null)
                return false;

            BaseVendor vendor = ent as BaseVendor;
            if (vendor == null)
            {
                message = "That target is not a vendor.";
                return false;
            }

            vendor.Restock();
            message = "Vendor restocked through whitelisted AI GM command action.";
            return true;
        }

        private static bool ExecuteViewEquipOnTarget(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            IEntity ent = ResolveEntity(action, out message);
            if (ent == null)
                return false;

            Mobile mob = ent as Mobile;
            if (mob == null)
            {
                message = "That target is not a mobile.";
                return false;
            }

            from.SendGump(new AIGMEquipInsightGump(mob));
            message = "Opened equipment insight for the selected target.";
            return true;
        }

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

        private static IEntity ResolveEntity(AIGMActionProposal action, out string message)
        {
            message = null;

            string rawSerial;
            if (action.Parameters == null || !action.Parameters.TryGetValue("targetSerial", out rawSerial) || String.IsNullOrWhiteSpace(rawSerial))
            {
                message = "No target serial was provided for this action.";
                return null;
            }

            int serial;
            if (!Int32.TryParse(rawSerial, out serial))
            {
                message = "Target serial was invalid.";
                return null;
            }

            IEntity ent = World.FindEntity(serial);
            if (ent == null)
            {
                message = "Target entity was not found.";
                return null;
            }

            return ent;
        }
    }
}
