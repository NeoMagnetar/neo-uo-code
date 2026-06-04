using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMSceneScanner
    {
        public static AIGMSceneContext Capture(Mobile from, int range)
        {
            AIGMSceneContext scene = new AIGMSceneContext();

            if (from == null)
                return scene;

            scene.MapName = from.Map != null ? from.Map.Name : null;
            scene.RegionName = from.Region != null ? from.Region.Name : null;
            scene.X = from.Location.X;
            scene.Y = from.Location.Y;
            scene.Z = from.Location.Z;
            scene.ScanRange = range;

            if (from.Map == null)
                return scene;

            List<AIGMSceneEntitySummary> mobiles = new List<AIGMSceneEntitySummary>();
            IPooledEnumerable mobileEnum = from.Map.GetMobilesInRange(from.Location, range);
            foreach (Mobile mob in mobileEnum)
            {
                if (mob == null || mob == from)
                    continue;

                mobiles.Add(BuildMobileSummary(from, mob));
            }
            mobileEnum.Free();

            mobiles.Sort((a, b) => a.Distance.CompareTo(b.Distance));
            if (mobiles.Count > 8)
                mobiles.RemoveRange(8, mobiles.Count - 8);
            scene.NearbyMobiles.AddRange(mobiles);

            List<AIGMSceneEntitySummary> items = new List<AIGMSceneEntitySummary>();
            IPooledEnumerable itemEnum = from.Map.GetItemsInRange(from.Location, range);
            foreach (Item item in itemEnum)
            {
                if (item == null)
                    continue;

                items.Add(BuildItemSummary(from, item));
            }
            itemEnum.Free();

            items.Sort((a, b) => a.Distance.CompareTo(b.Distance));
            if (items.Count > 12)
                items.RemoveRange(12, items.Count - 12);
            scene.NearbyItems.AddRange(items);

            return scene;
        }

        private static AIGMSceneEntitySummary BuildMobileSummary(Mobile from, Mobile mob)
        {
            AIGMSceneEntitySummary summary = new AIGMSceneEntitySummary();
            summary.Kind = "Mobile";
            summary.Serial = mob.Serial.Value;
            summary.Name = mob.Name;
            summary.TypeName = mob.GetType().Name;
            summary.MapName = mob.Map != null ? mob.Map.Name : null;
            summary.RegionName = mob.Region != null ? mob.Region.Name : null;
            summary.X = mob.Location.X;
            summary.Y = mob.Location.Y;
            summary.Z = mob.Location.Z;
            summary.Distance = (int)Math.Round(from.GetDistanceToSqrt(mob.Location));
            summary.IsPlayer = mob.Player;
            summary.IsNpc = !mob.Player;
            summary.IsVendor = mob is BaseVendor;
            summary.IsAlive = mob.Alive;
            summary.Deleted = mob.Deleted;

            AddTag(summary, summary.IsPlayer, "player");
            AddTag(summary, summary.IsNpc, "npc");
            AddTag(summary, summary.IsVendor, "vendor");
            AddTag(summary, !summary.IsAlive, "dead");
            AddTag(summary, summary.Distance <= 1, "adjacent");
            AddTag(summary, summary.Distance <= 3, "near");
            AddTag(summary, summary.Distance >= 8, "far");

            return summary;
        }

        private static AIGMSceneEntitySummary BuildItemSummary(Mobile from, Item item)
        {
            AIGMSceneEntitySummary summary = new AIGMSceneEntitySummary();
            summary.Kind = "Item";
            summary.Serial = item.Serial.Value;
            summary.Name = item.Name;
            summary.TypeName = item.GetType().Name;
            summary.MapName = item.Map != null ? item.Map.Name : null;
            summary.RegionName = item.Map != null ? Region.Find(item.Location, item.Map).Name : null;
            summary.X = item.Location.X;
            summary.Y = item.Location.Y;
            summary.Z = item.Location.Z;
            summary.Distance = (int)Math.Round(from.GetDistanceToSqrt(item.GetWorldLocation()));
            summary.IsContainer = item is Container;
            summary.IsDoor = item is BaseDoor;
            summary.IsStatic = item is Static;
            summary.IsMovable = item.Movable;
            summary.Deleted = item.Deleted;
            summary.ParentTypeName = item.Parent != null ? item.Parent.GetType().Name : null;

            AddTag(summary, summary.IsContainer, "container");
            AddTag(summary, summary.IsDoor, "door");
            AddTag(summary, summary.IsStatic, "static");
            AddTag(summary, !summary.IsMovable, "immovable");
            AddTag(summary, summary.ParentTypeName != null, "contained");
            AddTag(summary, summary.Distance <= 1, "adjacent");
            AddTag(summary, summary.Distance <= 3, "near");
            AddTag(summary, summary.Distance >= 8, "far");

            return summary;
        }

        private static void AddTag(AIGMSceneEntitySummary summary, bool condition, string tag)
        {
            if (summary == null || !condition || String.IsNullOrWhiteSpace(tag))
                return;

            if (!summary.Tags.Contains(tag))
                summary.Tags.Add(tag);
        }
    }
}
