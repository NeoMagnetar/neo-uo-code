using System;
using System.Collections.Generic;
using System.Text;
using Server.Commands;
using Server.Commands.Generic;
using Server.Gumps;
using Server.Items;
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
                                case AIGMCommandAction.InspectNearbyMobiles:
                                    return "Nearby mobiles listed. Want me to inspect one target or scan the area again?";
                                case AIGMCommandAction.InspectNearbyItems:
                                    return "Nearby items listed. Want me to inspect a specific one or compare distances?";
                                case AIGMCommandAction.InspectRegion:
                                    return "Region summary opened. Want me to scan mobiles, items, or move somewhere nearby?";
                                case AIGMCommandAction.ScanAroundTarget:
                                    return "Target-centered world scan opened. Want me to move there or inspect a listed entity?";
                                case AIGMCommandAction.GoToCoordinates:
                                    return "Moved to the requested coordinates. Want a fresh local scan now?";
                                case AIGMCommandAction.FollowMobile:
                                    return "Follow mode is active. Want me to stop following or head somewhere specific next?";
                                case AIGMCommandAction.StopFollowing:
                                    return "Movement stopped. Want me to follow again or walk to a location?";
                                case AIGMCommandAction.PathToCoordinates:
                                    return "Pathing to the requested coordinates. Want a status check or a nearby scan when I arrive?";
                                case AIGMCommandAction.PathToNamedLocation:
                                    return "Pathing to the named location. Want me to stop there or keep following you after arrival?";
                                case AIGMCommandAction.MovementStatus:
                                    return "Movement status checked. Want me to keep going, stop, or redirect somewhere else?";
                                case AIGMCommandAction.SetArrivalAction:
                                    return "Arrival behavior updated. Want me to move somewhere now and use it?";
                                case AIGMCommandAction.PauseMovement:
                                    return "Movement paused. Want me to resume, cancel, or reroute?";
                                case AIGMCommandAction.ResumeMovement:
                                    return "Movement resumed. Want a status check or a different destination?";
                                case AIGMCommandAction.CancelMovement:
                                    return "Movement canceled. Want a new destination or follow order?";
                                case AIGMCommandAction.QueueNamedRouteStop:
                                    return "Queued the next named stop. Want to add another stop or start the route?";
                                case AIGMCommandAction.QueueCoordinateRouteStop:
                                    return "Queued the next coordinate stop. Want another stop or should I start moving?";
                                case AIGMCommandAction.PathToCurrentTarget:
                                    return "Pathing to the current target. Want me to stop there, follow it, or scan on arrival?";
                                case AIGMCommandAction.FollowCurrentTarget:
                                    return "Following the current target now. Want me to keep shadowing it or stop at a destination instead?";
                                case AIGMCommandAction.OpenCounselorPack:
                                    return "Counselor pack opened. Want me to create an item in it now?";
                                case AIGMCommandAction.SpawnItemToCounselorPack:
                                    return "Item created in counselor pack. Want me to open the pack or create another item?";
                                case AIGMCommandAction.InspectNearestVendor:
                                    return "Nearest vendor inspected. Want me to move there or inspect stock next?";
                                case AIGMCommandAction.GoToNearestVendor:
                                    return "Moved to the nearest vendor. Want a fresh local scan or stock inspection next?";
                                case AIGMCommandAction.InspectNearestContainer:
                                    return "Nearest container inspected. Want me to compare its contents or nearby items next?";
                                case AIGMCommandAction.InspectNearestDoor:
                                    return "Nearest door inspected. Want me to move there or inspect surrounding items next?";
                                case AIGMCommandAction.InspectNearestMobile:
                                    return "Nearest mobile inspected. Want me to move there or inspect its equipment/props next?";
                                case AIGMCommandAction.GoToNearestByType:
                                    return "Moved to the nearest matching type. Want a fresh local scan now?";
                                case AIGMCommandAction.InspectDoorState:
                                    return "Door state inspected. Want me to move there or scan around it next?";
                                case AIGMCommandAction.InspectVendorRuntimeStock:
                                    return "Vendor runtime stock inspected. Want me to compare it to likely code files next?";
                                case AIGMCommandAction.ExecuteNextStep:
                                    return "Executed the next suggested AI GM step. Want me to continue or rescan?";
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

            AIGMExecutionLog.Write("EXECUTE_START kind={0}", action == null ? "(null)" : action.ActionKind);

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
                case "gm_add_world_item":
                    return ExecuteNativeAddWorldItem(from, action, out message);
                case "gm_add_container_item":
                    return ExecuteNativeAddContainerItem(from, action, out message);
                case "gm_add_world_mobile":
                    return ExecuteNativeAddWorldMobile(from, action, out message);
                case "run_gm_command":
                    return ExecuteRunGmCommand(from, action, out message);
                default:
                    AIGMExecutionLog.Write("EXECUTE_UNSUPPORTED kind={0}", action.ActionKind ?? String.Empty);
                    message = "That AI GM action is not executable yet.";
                    return false;
            }
        }

        private static bool ExecuteNativeAddWorldItem(Mobile from, AIGMActionProposal action, out string message)
        {
            AIGMExecutionResult result = AIGMNativeAddAdapter.AddWorldItem(from, null, action);
            message = result != null ? result.Message : "Native Add adapter returned no result.";
            return result != null && result.Ok;
        }

        private static bool ExecuteNativeAddContainerItem(Mobile from, AIGMActionProposal action, out string message)
        {
            AIGMExecutionResult result = AIGMNativeAddAdapter.AddContainerItem(from, null, action);
            message = result != null ? result.Message : "Native Add container adapter returned no result.";
            return result != null && result.Ok;
        }

        private static bool ExecuteNativeAddWorldMobile(Mobile from, AIGMActionProposal action, out string message)
        {
            AIGMExecutionResult result = AIGMNativeAddAdapter.AddWorldMobile(from, null, action);
            message = result != null ? result.Message : "Native Add mobile adapter returned no result.";
            return result != null && result.Ok;
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
                case AIGMCommandAction.InspectNearbyMobiles:
                    return ExecuteInspectNearbyMobiles(from, action, out message);
                case AIGMCommandAction.InspectNearbyItems:
                    return ExecuteInspectNearbyItems(from, action, out message);
                case AIGMCommandAction.InspectRegion:
                    return ExecuteInspectRegion(from, action, out message);
                case AIGMCommandAction.ScanAroundTarget:
                    return ExecuteScanAroundTarget(from, action, out message);
                case AIGMCommandAction.GoToCoordinates:
                    return ExecuteGotoCoordinates(from, action, out message);
                case AIGMCommandAction.FollowMobile:
                    return ExecuteFollowMobile(from, action, out message);
                case AIGMCommandAction.StopFollowing:
                    return ExecuteStopFollowing(from, action, out message);
                case AIGMCommandAction.PathToCoordinates:
                    return ExecutePathToCoordinates(from, action, out message);
                case AIGMCommandAction.PathToNamedLocation:
                    return ExecutePathToNamedLocation(from, action, out message);
                case AIGMCommandAction.MovementStatus:
                    return ExecuteMovementStatus(from, action, out message);
                case AIGMCommandAction.SetArrivalAction:
                    return ExecuteSetArrivalAction(from, action, out message);
                case AIGMCommandAction.PauseMovement:
                    return ExecutePauseMovement(from, action, out message);
                case AIGMCommandAction.ResumeMovement:
                    return ExecuteResumeMovement(from, action, out message);
                case AIGMCommandAction.CancelMovement:
                    return ExecuteCancelMovement(from, action, out message);
                case AIGMCommandAction.QueueNamedRouteStop:
                    return ExecuteQueueNamedRouteStop(from, action, out message);
                case AIGMCommandAction.QueueCoordinateRouteStop:
                    return ExecuteQueueCoordinateRouteStop(from, action, out message);
                case AIGMCommandAction.PathToCurrentTarget:
                    return ExecutePathToCurrentTarget(from, action, out message);
                case AIGMCommandAction.FollowCurrentTarget:
                    return ExecuteFollowCurrentTarget(from, action, out message);
                case AIGMCommandAction.InspectNearestVendor:
                    return ExecuteInspectNearestVendor(from, action, out message);
                case AIGMCommandAction.GoToNearestVendor:
                    return ExecuteGoToNearestVendor(from, action, out message);
                case AIGMCommandAction.InspectNearestContainer:
                    return ExecuteInspectNearestContainer(from, action, out message);
                case AIGMCommandAction.InspectNearestDoor:
                    return ExecuteInspectNearestDoor(from, action, out message);
                case AIGMCommandAction.InspectNearestMobile:
                    return ExecuteInspectNearestMobile(from, action, out message);
                case AIGMCommandAction.GoToNearestByType:
                    return ExecuteGoToNearestByType(from, action, out message);
                case AIGMCommandAction.InspectDoorState:
                    return ExecuteInspectDoorState(from, action, out message);
                case AIGMCommandAction.InspectVendorRuntimeStock:
                    return ExecuteInspectVendorRuntimeStock(from, action, out message);
                case AIGMCommandAction.ExecuteNextStep:
                    return ExecuteNextStep(from, action, out message);
                case AIGMCommandAction.OpenCounselorPack:
                    return ExecuteOpenCounselorPack(from, action, out message);
                case AIGMCommandAction.SpawnItemToCounselorPack:
                    return ExecuteSpawnItemToCounselorPack(from, action, out message);
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

        private static bool ExecuteInspectNearbyMobiles(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            if (from == null || from.Map == null)
            {
                message = "No valid GM position was available for nearby mobile inspection.";
                return false;
            }

            AIGMSceneContext scene = AIGMSceneScanner.Capture(from, 10);
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Region: {0}<br>Map: {1}<br>Origin: {2},{3},{4}<br><br>", Utility.FixHtml(scene.RegionName ?? "Unknown"), Utility.FixHtml(scene.MapName ?? "Unknown"), scene.X, scene.Y, scene.Z);

            if (scene.NearbyMobiles.Count == 0)
            {
                sb.Append("No nearby mobiles were found.");
            }
            else
            {
                for (int i = 0; i < scene.NearbyMobiles.Count; i++)
                {
                    AIGMSceneEntitySummary mob = scene.NearbyMobiles[i];
                    sb.AppendFormat("- {0} [{1}] d={2} @ {3},{4},{5}", Utility.FixHtml(mob.Name ?? mob.TypeName ?? "Unknown"), Utility.FixHtml(mob.TypeName ?? "Unknown"), mob.Distance, mob.X, mob.Y, mob.Z);
                    if (mob.Tags != null && mob.Tags.Count > 0)
                        sb.AppendFormat(" {{{0}}}", Utility.FixHtml(String.Join(", ", mob.Tags.ToArray())));
                    sb.Append("<br>");
                }
            }

            from.SendGump(new AIGMWorldInsightGump("AI GM Nearby Mobiles", sb.ToString()));
            message = "Opened nearby mobile world insight.";
            return true;
        }

        private static bool ExecuteInspectNearbyItems(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            if (from == null || from.Map == null)
            {
                message = "No valid GM position was available for nearby item inspection.";
                return false;
            }

            AIGMSceneContext scene = AIGMSceneScanner.Capture(from, 10);
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Region: {0}<br>Map: {1}<br>Origin: {2},{3},{4}<br><br>", Utility.FixHtml(scene.RegionName ?? "Unknown"), Utility.FixHtml(scene.MapName ?? "Unknown"), scene.X, scene.Y, scene.Z);

            if (scene.NearbyItems.Count == 0)
            {
                sb.Append("No nearby items were found.");
            }
            else
            {
                for (int i = 0; i < scene.NearbyItems.Count; i++)
                {
                    AIGMSceneEntitySummary item = scene.NearbyItems[i];
                    sb.AppendFormat("- {0} [{1}] d={2} @ {3},{4},{5}", Utility.FixHtml(item.Name ?? item.TypeName ?? "Unknown"), Utility.FixHtml(item.TypeName ?? "Unknown"), item.Distance, item.X, item.Y, item.Z);
                    if (item.Tags != null && item.Tags.Count > 0)
                        sb.AppendFormat(" {{{0}}}", Utility.FixHtml(String.Join(", ", item.Tags.ToArray())));
                    sb.Append("<br>");
                }
            }

            from.SendGump(new AIGMWorldInsightGump("AI GM Nearby Items", sb.ToString()));
            message = "Opened nearby item world insight.";
            return true;
        }

        private static bool ExecuteInspectRegion(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            if (from == null)
            {
                message = "No GM mobile was available for region inspection.";
                return false;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Map: {0}<br>", Utility.FixHtml(from.Map != null ? from.Map.Name : "Unknown"));
            sb.AppendFormat("Region: {0}<br>", Utility.FixHtml(from.Region != null ? from.Region.Name : "Unknown"));
            sb.AppendFormat("Location: {0},{1},{2}<br>", from.X, from.Y, from.Z);
            sb.AppendFormat("Access: {0}<br>", Utility.FixHtml(from.AccessLevel.ToString()));
            sb.Append("<br>Use nearby mobile/item scans for a denser local picture.");

            from.SendGump(new AIGMWorldInsightGump("AI GM Region Insight", sb.ToString()));
            message = "Opened region world insight.";
            return true;
        }

        private static bool ExecuteScanAroundTarget(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            IEntity ent = ResolveEntity(action, out message);
            if (ent == null)
                return false;

            Point3D loc;
            Map map;

            Item item = ent as Item;
            Mobile mob = ent as Mobile;
            if (item != null)
            {
                loc = item.GetWorldLocation();
                map = item.Map;
            }
            else if (mob != null)
            {
                loc = mob.Location;
                map = mob.Map;
            }
            else
            {
                message = "That target cannot be scanned spatially.";
                return false;
            }

            if (map == null)
            {
                message = "That target has no valid map for scanning.";
                return false;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Target-centered scan at {0},{1},{2} on {3}<br><br>", loc.X, loc.Y, loc.Z, Utility.FixHtml(map.Name));

            int count = 0;
            IPooledEnumerable mobiles = map.GetMobilesInRange(loc, 8);
            foreach (Mobile nearby in mobiles)
            {
                if (nearby == null)
                    continue;

                sb.AppendFormat("- Mobile: {0} [{1}] @ {2},{3},{4}<br>", Utility.FixHtml(nearby.Name ?? nearby.GetType().Name), Utility.FixHtml(nearby.GetType().Name), nearby.X, nearby.Y, nearby.Z);
                count++;
                if (count >= 8)
                    break;
            }
            mobiles.Free();

            if (count == 0)
                sb.Append("No nearby mobiles found around the target.<br>");

            from.SendGump(new AIGMWorldInsightGump("AI GM Target-Centered Scan", sb.ToString()));
            message = "Opened target-centered world scan.";
            return true;
        }

        private static bool ExecuteGotoCoordinates(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            string rawX, rawY, rawZ;
            if (action.Parameters == null || !action.Parameters.TryGetValue("x", out rawX) || !action.Parameters.TryGetValue("y", out rawY))
            {
                message = "No target coordinates were provided.";
                return false;
            }

            int x, y, z;
            if (!Int32.TryParse(rawX, out x) || !Int32.TryParse(rawY, out y))
            {
                message = "Target coordinates were invalid.";
                return false;
            }

            z = 0;
            if (action.Parameters.TryGetValue("z", out rawZ))
                Int32.TryParse(rawZ, out z);

            from.Location = new Point3D(x, y, z);
            from.ProcessDelta();
            message = String.Format("Teleported to coordinates {0},{1},{2}.", x, y, z);
            return true;
        }

        private static bool ExecuteFollowMobile(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            AIGMCounselor counselor = FindCounselor(from, 24);
            if (counselor == null)
            {
                message = "No nearby AI GM counselor was found to follow you.";
                return false;
            }

            Mobile target = from;
            string rawSerial;
            if (action.Parameters != null && action.Parameters.TryGetValue("targetSerial", out rawSerial) && !String.IsNullOrWhiteSpace(rawSerial))
            {
                int serial;
                if (TryParseSerial(rawSerial, out serial))
                {
                    Mobile explicitTarget = World.FindMobile(serial);
                    if (explicitTarget != null && !explicitTarget.Deleted)
                        target = explicitTarget;
                }
            }

            message = "Movement follow is temporarily unavailable while the counselor is being refactored onto vendor-backed actor mechanics.";
            return false;
        }

        private static bool ExecuteStopFollowing(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            AIGMCounselor counselor = FindCounselor(from, 48);
            if (counselor == null)
            {
                message = "No nearby AI GM counselor was found to stop.";
                return false;
            }

            message = "Movement stop is temporarily unavailable while the counselor is being refactored onto vendor-backed actor mechanics.";
            return false;
        }

        private static bool ExecutePathToCoordinates(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            AIGMCounselor counselor = FindCounselor(from, 48);
            if (counselor == null)
            {
                message = "No nearby AI GM counselor was found to move.";
                return false;
            }

            string rawX, rawY, rawZ;
            if (action.Parameters == null || !action.Parameters.TryGetValue("x", out rawX) || !action.Parameters.TryGetValue("y", out rawY))
            {
                message = "No path destination coordinates were provided.";
                return false;
            }

            int x, y, z;
            if (!Int32.TryParse(rawX, out x) || !Int32.TryParse(rawY, out y))
            {
                message = "Path destination coordinates were invalid.";
                return false;
            }

            z = from.Map != null ? from.Map.GetAverageZ(x, y) : 0;
            if (action.Parameters.TryGetValue("z", out rawZ))
                Int32.TryParse(rawZ, out z);

            message = "Pathing is temporarily unavailable while the counselor is being refactored onto vendor-backed actor mechanics.";
            return false;
        }

        private static bool ExecutePathToNamedLocation(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            AIGMCounselor counselor = FindCounselor(from, 48);
            if (counselor == null)
            {
                message = "No nearby AI GM counselor was found to move.";
                return false;
            }

            string rawName;
            if (action.Parameters == null || !action.Parameters.TryGetValue("destinationName", out rawName) || String.IsNullOrWhiteSpace(rawName))
            {
                message = "No named destination was provided.";
                return false;
            }

            AIGMNamedDestination destination;
            if (!AIGMMovementController.TryResolveNamedDestination(rawName, counselor.Map, out destination))
            {
                message = String.Format("I do not know the named destination '{0}' yet.", rawName);
                return false;
            }

            message = "Named pathing is temporarily unavailable while the counselor is being refactored onto vendor-backed actor mechanics.";
            return false;
        }

        private static bool ExecuteInspectNearestVendor(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            BaseVendor vendor = FindNearestVendor(from, 12);
            if (vendor == null)
            {
                message = "No nearby vendor was found.";
                return false;
            }

            from.SendGump(new PropertiesGump(from, vendor));
            message = String.Format("Opened properties for nearest vendor: {0}.", vendor.Name ?? vendor.GetType().Name);
            return true;
        }

        private static bool ExecuteGoToNearestVendor(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            BaseVendor vendor = FindNearestVendor(from, 18);
            if (vendor == null)
            {
                message = "No nearby vendor was found.";
                return false;
            }

            from.Location = vendor.Location;
            from.Map = vendor.Map;
            from.ProcessDelta();
            message = String.Format("Teleported to nearest vendor: {0}.", vendor.Name ?? vendor.GetType().Name);
            return true;
        }

        private static bool ExecuteInspectNearestContainer(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            Container container = FindNearestContainer(from, 12);
            if (container == null)
            {
                message = "No nearby container was found.";
                return false;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Container: {0} [{1}]<br>", Utility.FixHtml(container.Name ?? container.GetType().Name), Utility.FixHtml(container.GetType().Name));
            sb.AppendFormat("Location: {0},{1},{2}<br><br>", container.X, container.Y, container.Z);

            int count = 0;
            foreach (Item item in container.Items)
            {
                if (item == null)
                    continue;

                sb.AppendFormat("- {0} [{1}]<br>", Utility.FixHtml(item.Name ?? item.GetType().Name), Utility.FixHtml(item.GetType().Name));
                count++;
                if (count >= 20)
                    break;
            }

            if (count == 0)
                sb.Append("Container appears empty.<br>");

            from.SendGump(new AIGMWorldInsightGump("AI GM Nearest Container", sb.ToString()));
            message = "Opened nearest container insight.";
            return true;
        }

        private static bool ExecuteInspectNearestDoor(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            BaseDoor door = FindNearestDoor(from, 12);
            if (door == null)
            {
                message = "No nearby door was found.";
                return false;
            }

            from.SendGump(new PropertiesGump(from, door));
            message = String.Format("Opened properties for nearest door: {0}.", door.Name ?? door.GetType().Name);
            return true;
        }

        private static bool ExecuteInspectNearestMobile(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            string typeFilter = null;
            if (action.Parameters != null)
                action.Parameters.TryGetValue("targetType", out typeFilter);

            Mobile mobile = FindNearestMobile(from, 12, typeFilter);
            if (mobile == null)
            {
                message = String.IsNullOrWhiteSpace(typeFilter) ? "No nearby mobile was found." : String.Format("No nearby mobile of type '{0}' was found.", typeFilter);
                return false;
            }

            from.SendGump(new PropertiesGump(from, mobile));
            message = String.Format("Opened properties for nearest mobile: {0}.", mobile.Name ?? mobile.GetType().Name);
            return true;
        }

        private static bool ExecuteGoToNearestByType(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            string typeFilter = null;
            if (action.Parameters == null || !action.Parameters.TryGetValue("targetType", out typeFilter) || String.IsNullOrWhiteSpace(typeFilter))
            {
                message = "No target type was provided.";
                return false;
            }

            IEntity ent = FindNearestByType(from, 18, typeFilter);
            if (ent == null)
            {
                message = String.Format("No nearby entity of type '{0}' was found.", typeFilter);
                return false;
            }

            IPoint3D p = ent as IPoint3D;
            if (p == null)
            {
                message = "The nearest matching entity did not have a valid world location.";
                return false;
            }

            if (p is Item)
                p = ((Item)p).GetWorldTop();
            else if (p is Mobile)
                p = ((Mobile)p).Location;

            SpellHelper.GetSurfaceTop(ref p);
            from.Location = new Point3D(p);
            from.ProcessDelta();
            message = String.Format("Teleported to nearest entity of type '{0}'.", typeFilter);
            return true;
        }

        private static bool ExecuteInspectDoorState(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            BaseDoor door = FindNearestDoor(from, 12);
            if (door == null)
            {
                message = "No nearby door was found.";
                return false;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Door: {0} [{1}]<br>", Utility.FixHtml(door.Name ?? door.GetType().Name), Utility.FixHtml(door.GetType().Name));
            sb.AppendFormat("Location: {0},{1},{2}<br>", door.X, door.Y, door.Z);
            sb.AppendFormat("Open: {0}<br>", door.Open);
            sb.AppendFormat("Locked: {0}<br>", door.Locked);
            sb.AppendFormat("ItemID: {0}<br>", door.ItemID);
            sb.AppendFormat("Hue: {0}<br>", door.Hue);
            from.SendGump(new AIGMWorldInsightGump("AI GM Door State", sb.ToString()));
            message = "Opened nearest door state insight.";
            return true;
        }

        private static bool ExecuteInspectVendorRuntimeStock(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            BaseVendor vendor = FindNearestVendor(from, 12);
            if (vendor == null)
            {
                message = "No nearby vendor was found.";
                return false;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Vendor: {0} [{1}]<br>", Utility.FixHtml(vendor.Name ?? vendor.GetType().Name), Utility.FixHtml(vendor.GetType().Name));
            sb.AppendFormat("Location: {0},{1},{2}<br><br>", vendor.X, vendor.Y, vendor.Z);
            sb.Append("Runtime stock surfaces are partially visible through SBInfo and vendor sell/buy lists.<br><br>");

            IBuyItemInfo[] buyInfo = vendor.GetBuyInfo();
            if (buyInfo == null || buyInfo.Length == 0)
            {
                sb.Append("No vendor buy entries were exposed at runtime.<br>");
            }
            else
            {
                for (int i = 0; i < buyInfo.Length; i++)
                {
                    IBuyItemInfo info = buyInfo[i];
                    sb.AppendFormat("- Buy entry: {0} | Price: {1} | Amount: {2}<br>", Utility.FixHtml(info != null ? info.Name : "null"), info != null ? info.Price.ToString() : "?", info != null ? info.Amount.ToString() : "?");
                    if (i >= 11)
                        break;
                }
            }

            from.SendGump(new AIGMWorldInsightGump("AI GM Vendor Runtime Stock", sb.ToString()));
            message = "Opened vendor runtime stock insight.";
            return true;
        }

        private static bool ExecuteNextStep(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            AIGMSessionState session = AIGMSessionState.Get(from);
            if (session == null || session.LastResponse == null || session.LastResponse.NextAction == null)
            {
                message = "No AI GM next step is available to execute.";
                return false;
            }

            AIGMActionProposal next = session.LastResponse.NextAction;
            if (next.Category != null && next.Category.Equals("mutate", StringComparison.OrdinalIgnoreCase))
            {
                message = "Next step requires explicit confirmation because it is mutating.";
                return false;
            }

            AIGMExecutionLoopState loop = AIGMExecutionLoopState.Get(from);
            if (loop != null && loop.StepsExecuted >= 3)
            {
                message = "AI GM execution loop reached its safety step limit for now.";
                return false;
            }

            string inner;
            bool ok = Execute(from, next, out inner);
            if (loop != null)
            {
                loop.StepsExecuted++;
                loop.LastStepUtc = DateTime.UtcNow;
                loop.LastPlanSummary = session.LastResponse.Plan != null ? String.Join(" -> ", session.LastResponse.Plan.ToArray()) : null;
                loop.LastStopReason = session.LastResponse.StopReason;
            }

            session.LastActionDescription = next.Description;
            session.LastActionResult = inner;
            session.ExecutionStepCount = loop != null ? loop.StepsExecuted : (session.ExecutionStepCount + 1);

            if (ok && !String.IsNullOrWhiteSpace(session.LastQuestion))
            {
                AIGMResponse followup = AIGMBridgeClient.ContinueAfterAction(from, session.LastQuestion, session.CurrentTarget, next.Description, inner, session.ExecutionStepCount);
                if (followup != null)
                {
                    session.LastResponse = followup;

                    bool autoContinued = false;
                    if (loop != null && !loop.AutoContinueUsed && followup.NextAction != null && IsSafeAutoContinue(followup.NextAction) && loop.StepsExecuted < 3)
                    {
                        loop.AutoContinueUsed = true;
                        string chainedMessage;
                        autoContinued = ExecuteNextStep(from, action, out chainedMessage);
                        if (!String.IsNullOrWhiteSpace(chainedMessage))
                            inner = (inner ?? String.Empty) + " | Auto-continue: " + chainedMessage;
                    }

                    if (!autoContinued)
                        from.SendGump(new AIGMResponseGump(from, null, followup));
                }
            }

            message = ok ? (inner ?? "Executed next AI GM step.") : (inner ?? "Failed to execute next AI GM step.");
            return ok;
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
            if (!TryParseSerial(rawSerial, out serial))
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

        private static bool TryParseSerial(string rawSerial, out int serial)
        {
            serial = 0;

            if (String.IsNullOrWhiteSpace(rawSerial))
                return false;

            rawSerial = rawSerial.Trim();

            if (rawSerial.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return Int32.TryParse(rawSerial.Substring(2), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out serial);
            }

            return Int32.TryParse(rawSerial, out serial);
        }

        private static bool IsSafeAutoContinue(AIGMActionProposal action)
        {
            if (action == null)
                return false;

            string category = action.Category ?? "read";
            if (category.Equals("mutate", StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        private static bool ExecuteMovementStatus(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            AIGMMovementState state = AIGMMovementState.Get(from);
            if (state == null)
            {
                message = "The counselor has no recorded movement state.";
                return true;
            }

            BaseCreature counselor = World.FindMobile(state.CounselorSerial) as BaseCreature;
            string who = counselor != null ? (counselor.Name ?? "The counselor") : "The counselor";
            string where = state.DestinationName;
            if (String.IsNullOrWhiteSpace(where))
                where = String.Format("{0},{1},{2}", state.Destination.X, state.Destination.Y, state.Destination.Z);

            string arrival = String.IsNullOrWhiteSpace(state.ArrivalActionKind) ? "wait" : state.ArrivalActionKind;
            message = String.Format("{0} status: {1}. Destination: {2}. Arrival action: {3}.", who, state.LastStatus ?? state.Mode.ToString(), where, arrival);
            return true;
        }

        private static bool ExecuteSetArrivalAction(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            string arrivalAction = null;
            string arrivalArgument = null;
            if (action != null && action.Parameters != null)
            {
                action.Parameters.TryGetValue("arrivalAction", out arrivalAction);
                action.Parameters.TryGetValue("arrivalArgument", out arrivalArgument);
            }

            if (String.IsNullOrWhiteSpace(arrivalAction))
            {
                message = "No arrival action was provided.";
                return false;
            }

            AIGMMovementController.ConfigureArrivalAction(from, arrivalAction, arrivalArgument);
            message = String.Format("Arrival action set to {0}.", arrivalAction);
            return true;
        }

        private static bool ExecutePauseMovement(Mobile from, AIGMActionProposal action, out string message)
        {
            return AIGMMovementController.Pause(from, out message);
        }

        private static bool ExecuteResumeMovement(Mobile from, AIGMActionProposal action, out string message)
        {
            return AIGMMovementController.Resume(from, out message);
        }

        private static bool ExecuteCancelMovement(Mobile from, AIGMActionProposal action, out string message)
        {
            return AIGMMovementController.Cancel(from, out message);
        }

        private static bool ExecuteQueueNamedRouteStop(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            AIGMCounselor counselor = FindCounselor(from, 48);
            if (counselor == null)
            {
                message = "No nearby AI GM counselor was found for route chaining.";
                return false;
            }

            string rawName;
            if (action.Parameters == null || !action.Parameters.TryGetValue("destinationName", out rawName) || String.IsNullOrWhiteSpace(rawName))
            {
                message = "No queued destination name was provided.";
                return false;
            }

            AIGMNamedDestination destination;
            if (!AIGMMovementController.TryResolveNamedDestination(rawName, counselor.Map, out destination))
            {
                message = String.Format("I do not know the queued destination '{0}' yet.", rawName);
                return false;
            }

            AIGMMovementController.QueueNamedStop(from, destination.Name, destination.Location);
            message = String.Format("Queued next stop: {0}.", destination.Name);
            return true;
        }

        private static bool ExecuteQueueCoordinateRouteStop(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            string rawX, rawY, rawZ;
            if (action.Parameters == null || !action.Parameters.TryGetValue("x", out rawX) || !action.Parameters.TryGetValue("y", out rawY))
            {
                message = "No queued coordinates were provided.";
                return false;
            }

            int x, y, z;
            if (!Int32.TryParse(rawX, out x) || !Int32.TryParse(rawY, out y))
            {
                message = "Queued coordinates were invalid.";
                return false;
            }

            z = from.Map != null ? from.Map.GetAverageZ(x, y) : 0;
            if (action.Parameters.TryGetValue("z", out rawZ))
                Int32.TryParse(rawZ, out z);

            AIGMMovementController.QueueCoordinateStop(from, new Point3D(x, y, z));
            message = String.Format("Queued next coordinate stop: {0},{1},{2}.", x, y, z);
            return true;
        }

        private static bool ExecutePathToCurrentTarget(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            AIGMSessionState session = AIGMSessionState.Get(from);
            if (session == null || session.CurrentTarget == null)
            {
                message = "No current AI GM target is selected.";
                return false;
            }

            AIGMCounselor counselor = FindCounselor(from, 48);
            if (counselor == null)
            {
                message = "No nearby AI GM counselor was found to move.";
                return false;
            }

            Point3D destination = new Point3D(session.CurrentTarget.X, session.CurrentTarget.Y, session.CurrentTarget.Z);
            string destinationName = !String.IsNullOrWhiteSpace(session.CurrentTarget.Name) ? session.CurrentTarget.Name : session.CurrentTarget.TypeName;
            message = "Path-to-target is temporarily unavailable while the counselor is being refactored onto vendor-backed actor mechanics.";
            return false;
        }

        private static bool ExecuteFollowCurrentTarget(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            AIGMSessionState session = AIGMSessionState.Get(from);
            if (session == null || session.CurrentTarget == null)
            {
                message = "No current AI GM target is selected.";
                return false;
            }

            if (!String.Equals(session.CurrentTarget.Kind, "Mobile", StringComparison.OrdinalIgnoreCase))
            {
                message = "The current AI GM target is not a mobile and cannot be followed.";
                return false;
            }

            Mobile target = World.FindMobile(session.CurrentTarget.Serial);
            if (target == null || target.Deleted)
            {
                message = "The current target mobile could not be found.";
                return false;
            }

            AIGMCounselor counselor = FindCounselor(from, 48);
            if (counselor == null)
            {
                message = "No nearby AI GM counselor was found to move.";
                return false;
            }

            message = "Follow-current-target is temporarily unavailable while the counselor is being refactored onto vendor-backed actor mechanics.";
            return false;
        }

        public static bool ExecuteSystemAction(Mobile from, string commandName, out string message)
        {
            message = null;
            if (String.IsNullOrWhiteSpace(commandName))
            {
                message = "No system action was provided.";
                return false;
            }

            AIGMActionProposal action = new AIGMActionProposal();
            action.ActionKind = "run_gm_command";
            action.Description = commandName;
            action.Category = "read";
            action.RequiresConfirmation = false;
            action.Parameters["commandName"] = commandName;
            return ExecuteRunGmCommand(from, action, out message);
        }

        private static bool ExecuteOpenCounselorPack(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            AIGMCounselor counselor = FindCounselor(from, 48);
            if (counselor == null)
            {
                message = "No nearby AI GM counselor pack was available.";
                return false;
            }

            IAIGMInventoryCapability inventory = counselor.Inventory;
            if (inventory == null)
            {
                message = "The counselor pack could not be prepared.";
                return false;
            }

            return inventory.TryOpenPrimaryContainer(from, out message);
        }

        private static bool ExecuteSpawnItemToCounselorPack(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;
            LogExecution("ExecuteSpawnItemToCounselorPack start from=" + SafeMobileName(from));

            AIGMCounselor counselor = FindCounselor(from, 48);
            if (counselor == null)
            {
                message = "No nearby AI GM counselor was found for item delivery.";
                return false;
            }

            IAIGMInventoryCapability inventory = counselor.Inventory;
            if (inventory == null)
            {
                message = "The counselor pack could not be prepared.";
                return false;
            }

            string rawItemName = null;
            string rawAmount = null;
            string rawBlessed = null;

            if (action != null && action.Parameters != null)
            {
                action.Parameters.TryGetValue("itemName", out rawItemName);

                if (String.IsNullOrWhiteSpace(rawItemName))
                    action.Parameters.TryGetValue("itemAlias", out rawItemName);

                action.Parameters.TryGetValue("amount", out rawAmount);
                action.Parameters.TryGetValue("blessed", out rawBlessed);
            }

            if (String.IsNullOrWhiteSpace(rawItemName))
            {
                message = "No item name was provided for counselor pack creation.";
                LogExecution("spawn fail no item name");
                return false;
            }

            LogExecution("spawn request item=" + rawItemName + " rawAmount=" + (rawAmount ?? String.Empty) + " rawBlessed=" + (rawBlessed ?? String.Empty));

            int amount = 1;
            if (!String.IsNullOrWhiteSpace(rawAmount))
            {
                int parsedAmount;
                if (Int32.TryParse(rawAmount, out parsedAmount))
                    amount = parsedAmount;
            }

            if (amount < 1)
                amount = 1;

            bool blessed = false;
            if (!String.IsNullOrWhiteSpace(rawBlessed))
                Boolean.TryParse(rawBlessed, out blessed);

            Container pack = inventory.GetPrimaryContainer();
            if (pack == null)
            {
                message = "The counselor pack could not be prepared.";
                return false;
            }

            string[] ctorArgs = BuildAddConstructorArgs(rawItemName, amount);

            Item item;
            string error;
            if (!AIGMAddAdapter.TryCreateItemInContainer(from, pack, rawItemName, ctorArgs, null, out item, out error))
            {
                LogExecution("add adapter failed error=" + (error ?? String.Empty) + " fallingBack=true");
                if (!TryCreateAllowedCounselorPackItem(rawItemName, amount, blessed, out item, out error))
                {
                    message = !String.IsNullOrWhiteSpace(error)
                        ? error
                        : "I do not yet know how to create that item safely.";
                    LogExecution("fallback failed error=" + (message ?? String.Empty));
                    return false;
                }

                if (item == null)
                {
                    message = "Item creation returned no result.";
                    LogExecution("fallback built null item");
                    return false;
                }

                bool droppedFallback = inventory.TryDropCreatedItem(from, item, rawItemName, out message);
                LogExecution("fallback success dropped=" + droppedFallback + " message=" + (message ?? String.Empty));
                return droppedFallback;
            }

            TryApplyBlessed(item, blessed);
            bool dropped = inventory.TryDropCreatedItem(from, item, rawItemName, out message);
            LogExecution("add adapter success dropped=" + dropped + " message=" + (message ?? String.Empty));
            return dropped;
        }

        private static string[] BuildAddConstructorArgs(string itemName, int amount)
        {
            string alias = NormalizeItemAlias(itemName);
            if (String.IsNullOrWhiteSpace(alias))
                return new string[0];

            switch (alias)
            {
                case "bandage":
                case "bandages":
                case "blank scroll":
                case "blank scrolls":
                case "gold":
                case "black pearl":
                case "bloodmoss":
                case "garlic":
                case "ginseng":
                case "mandrake root":
                case "nightshade":
                case "sulfurous ash":
                case "spider silk":
                case "spiders silk":
                    return new string[] { Math.Max(1, amount).ToString() };
                default:
                    return new string[0];
            }
        }

        private static bool TryCreateAllowedCounselorPackItem(string itemName, int amount, bool blessed, out Item item, out string error)
        {
            item = null;
            error = null;

            string alias = NormalizeItemAlias(itemName);
            if (String.IsNullOrWhiteSpace(alias))
            {
                error = "I do not yet know how to create that item safely.";
                return false;
            }

            amount = Math.Max(1, amount);

            switch (alias)
            {
                case "katana":
                    item = new Katana();
                    break;
                case "longsword":
                    item = new Longsword();
                    break;
                case "broadsword":
                    item = new Broadsword();
                    break;
                case "dagger":
                    item = new Dagger();
                    break;
                case "bag":
                    item = new Bag();
                    break;
                case "backpack":
                    item = new Backpack();
                    break;
                case "bandage":
                case "bandages":
                    item = new Bandage(Math.Min(amount, 1000));
                    break;
                case "blank scroll":
                case "blank scrolls":
                    item = new BlankScroll(Math.Min(amount, 1000));
                    break;
                case "recall rune":
                case "rune":
                    item = new RecallRune();
                    break;
                case "gold":
                    item = new Gold(Math.Min(amount, 100000));
                    break;
                case "black pearl":
                    item = new BlackPearl(Math.Min(amount, 1000));
                    break;
                case "bloodmoss":
                    item = new Bloodmoss(Math.Min(amount, 1000));
                    break;
                case "garlic":
                    item = new Garlic(Math.Min(amount, 1000));
                    break;
                case "ginseng":
                    item = new Ginseng(Math.Min(amount, 1000));
                    break;
                case "mandrake root":
                    item = new MandrakeRoot(Math.Min(amount, 1000));
                    break;
                case "nightshade":
                    item = new Nightshade(Math.Min(amount, 1000));
                    break;
                case "sulfurous ash":
                    item = new SulfurousAsh(Math.Min(amount, 1000));
                    break;
                case "spiders silk":
                case "spider silk":
                    item = new SpidersSilk(Math.Min(amount, 1000));
                    break;
                default:
                    error = "I do not yet know how to create that item safely.";
                    return false;
            }

            TryApplyBlessed(item, blessed);
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

        private static string NormalizeItemAlias(string raw)
        {
            if (String.IsNullOrWhiteSpace(raw))
                return null;

            string alias = raw.Trim().ToLowerInvariant();

            while (alias.Contains("  "))
                alias = alias.Replace("  ", " ");

            if (alias == "black pearls")
                alias = "black pearl";
            else if (alias == "mandrake")
                alias = "mandrake root";
            else if (alias == "mandrakes")
                alias = "mandrake root";
            else if (alias == "spider silk")
                alias = "spiders silk";
            else if (alias == "recall runes")
                alias = "recall rune";

            return alias;
        }

        private static void TryApplyBlessed(Item item, bool blessed)
        {
            if (item == null || !blessed)
                return;

            try
            {
                item.LootType = LootType.Blessed;
            }
            catch
            {
            }
        }

        private static AIGMCounselor FindCounselor(Mobile from, int range)
        {
            if (from == null || from.Map == null)
                return null;

            AIGMCounselor best = null;
            int bestDistance = Int32.MaxValue;
            IPooledEnumerable mobiles = from.Map.GetMobilesInRange(from.Location, range);
            foreach (Mobile mob in mobiles)
            {
                AIGMCounselor counselor = mob as AIGMCounselor;
                if (counselor == null || counselor.Deleted)
                    continue;

                int distance = (int)Math.Round(from.GetDistanceToSqrt(counselor.Location));
                if (distance < bestDistance)
                {
                    best = counselor;
                    bestDistance = distance;
                }
            }
            mobiles.Free();
            return best;
        }

        private static BaseVendor FindNearestVendor(Mobile from, int range)
        {
            if (from == null || from.Map == null)
                return null;

            BaseVendor best = null;
            int bestDistance = Int32.MaxValue;
            IPooledEnumerable mobiles = from.Map.GetMobilesInRange(from.Location, range);
            foreach (Mobile mob in mobiles)
            {
                BaseVendor vendor = mob as BaseVendor;
                if (vendor == null || vendor.Deleted)
                    continue;

                int distance = (int)Math.Round(from.GetDistanceToSqrt(vendor.Location));
                if (distance < bestDistance)
                {
                    best = vendor;
                    bestDistance = distance;
                }
            }
            mobiles.Free();
            return best;
        }

        private static Container FindNearestContainer(Mobile from, int range)
        {
            if (from == null || from.Map == null)
                return null;

            Container best = null;
            int bestDistance = Int32.MaxValue;
            IPooledEnumerable items = from.Map.GetItemsInRange(from.Location, range);
            foreach (Item item in items)
            {
                Container container = item as Container;
                if (container == null || container.Deleted)
                    continue;

                int distance = (int)Math.Round(from.GetDistanceToSqrt(container.GetWorldLocation()));
                if (distance < bestDistance)
                {
                    best = container;
                    bestDistance = distance;
                }
            }
            items.Free();
            return best;
        }

        private static BaseDoor FindNearestDoor(Mobile from, int range)
        {
            if (from == null || from.Map == null)
                return null;

            BaseDoor best = null;
            int bestDistance = Int32.MaxValue;
            IPooledEnumerable items = from.Map.GetItemsInRange(from.Location, range);
            foreach (Item item in items)
            {
                BaseDoor door = item as BaseDoor;
                if (door == null || door.Deleted)
                    continue;

                int distance = (int)Math.Round(from.GetDistanceToSqrt(door.GetWorldLocation()));
                if (distance < bestDistance)
                {
                    best = door;
                    bestDistance = distance;
                }
            }
            items.Free();
            return best;
        }

        private static Mobile FindNearestMobile(Mobile from, int range, string typeFilter)
        {
            if (from == null || from.Map == null)
                return null;

            Mobile best = null;
            int bestDistance = Int32.MaxValue;
            IPooledEnumerable mobiles = from.Map.GetMobilesInRange(from.Location, range);
            foreach (Mobile mob in mobiles)
            {
                if (mob == null || mob.Deleted || mob == from)
                    continue;

                if (!String.IsNullOrWhiteSpace(typeFilter) && !mob.GetType().Name.Equals(typeFilter, StringComparison.OrdinalIgnoreCase))
                    continue;

                int distance = (int)Math.Round(from.GetDistanceToSqrt(mob.Location));
                if (distance < bestDistance)
                {
                    best = mob;
                    bestDistance = distance;
                }
            }
            mobiles.Free();
            return best;
        }

        private static IEntity FindNearestByType(Mobile from, int range, string typeFilter)
        {
            if (from == null || from.Map == null || String.IsNullOrWhiteSpace(typeFilter))
                return null;

            IEntity best = null;
            int bestDistance = Int32.MaxValue;

            IPooledEnumerable mobiles = from.Map.GetMobilesInRange(from.Location, range);
            foreach (Mobile mob in mobiles)
            {
                if (mob == null || mob.Deleted || mob == from)
                    continue;

                if (!mob.GetType().Name.Equals(typeFilter, StringComparison.OrdinalIgnoreCase))
                    continue;

                int distance = (int)Math.Round(from.GetDistanceToSqrt(mob.Location));
                if (distance < bestDistance)
                {
                    best = mob;
                    bestDistance = distance;
                }
            }
            mobiles.Free();

            IPooledEnumerable items = from.Map.GetItemsInRange(from.Location, range);
            foreach (Item item in items)
            {
                if (item == null || item.Deleted)
                    continue;

                if (!item.GetType().Name.Equals(typeFilter, StringComparison.OrdinalIgnoreCase))
                    continue;

                int distance = (int)Math.Round(from.GetDistanceToSqrt(item.GetWorldLocation()));
                if (distance < bestDistance)
                {
                    best = item;
                    bestDistance = distance;
                }
            }
            items.Free();

            return best;
        }
    }
}
