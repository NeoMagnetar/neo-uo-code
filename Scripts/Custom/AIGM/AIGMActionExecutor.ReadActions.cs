using System;
using System.Text;
using Server.Commands;
using Server.Commands.Generic;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Spells;

namespace Server.Custom.AIGM
{
    public static partial class AIGMActionExecutor
    {
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
