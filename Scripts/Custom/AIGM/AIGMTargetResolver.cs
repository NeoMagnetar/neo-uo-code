using System;
using Server.Items;
using Server.Mobiles;
using System.Collections.Generic;

namespace Server.Custom.AIGM
{
    public static class AIGMTargetResolver
    {
        public static object Resolve(Mobile from, string targetMode)
        {
            return Resolve(from, targetMode, null, null);
        }

        public static object Resolve(Mobile from, string targetMode, string targetName, string targetContainerKind)
        {
            if (from == null || from.Map == null)
                return null;

            string mode = String.IsNullOrWhiteSpace(targetMode) ? "nearest_mobile" : targetMode.Trim().ToLowerInvariant();

            if (!String.IsNullOrWhiteSpace(targetName))
            {
                string normalizedTarget = NormalizeName(targetName);

                if (normalizedTarget == "me" || normalizedTarget == "myself" || normalizedTarget == NormalizeName(from.Name) || normalizedTarget == NormalizeName(from.GetType().Name))
                    return from;

                object named = ResolveNamedTarget(from, mode, targetName, targetContainerKind);
                if (named != null)
                    return named;
            }

            switch (mode)
            {
                case "container_named_item":
                    {
                        Container container = ResolveContainerByKind(from, targetContainerKind);
                        if (container != null && !String.IsNullOrWhiteSpace(targetName))
                        {
                            Item contained = FindItemInContainer(container, NormalizeName(targetName));
                            if (contained != null)
                                return contained;
                        }
                        return null;
                    }
                case "nearest_mobile":
                    return FindNearestMobile(from, 12);
                case "nearest_item":
                    return FindNearestItem(from, 12);
                case "nearest_container":
                    return FindNearestContainer(from, 12);
                case "nearest_door":
                    return FindNearestDoor(from, 12);
                default:
                    return FindNearestMobile(from, 12) ?? (object)FindNearestItem(from, 12);
            }
        }

        private static object ResolveNamedTarget(Mobile from, string mode, string targetName, string targetContainerKind)
        {
            string normalizedTarget = NormalizeName(targetName);
            if (String.IsNullOrWhiteSpace(normalizedTarget))
                return null;

            if (!String.IsNullOrWhiteSpace(targetContainerKind))
            {
                Container container = ResolveContainerByKind(from, targetContainerKind);
                if (container != null)
                {
                    Item contained = FindItemInContainer(container, normalizedTarget);
                    if (contained != null)
                        return contained;
                }
            }

            if (String.Equals(mode, "nearest_mobile", StringComparison.OrdinalIgnoreCase))
                return FindNamedMobile(from, normalizedTarget, 12);

            if (String.Equals(mode, "nearest_item", StringComparison.OrdinalIgnoreCase) || String.Equals(mode, "nearest_container", StringComparison.OrdinalIgnoreCase) || String.Equals(mode, "nearest_door", StringComparison.OrdinalIgnoreCase))
                return FindNamedItem(from, normalizedTarget, 12);

            return FindNamedMobile(from, normalizedTarget, 12) ?? (object)FindNamedItem(from, normalizedTarget, 12);
        }

        private static Mobile FindNearestMobile(Mobile from, int range)
        {
            Mobile best = null;
            int bestDistance = Int32.MaxValue;
            IPooledEnumerable mobiles = from.Map.GetMobilesInRange(from.Location, range);
            foreach (Mobile mob in mobiles)
            {
                if (mob == null || mob.Deleted || mob == from)
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

        private static Item FindNearestItem(Mobile from, int range)
        {
            Item best = null;
            int bestDistance = Int32.MaxValue;
            IPooledEnumerable items = from.Map.GetItemsInRange(from.Location, range);
            foreach (Item item in items)
            {
                if (item == null || item.Deleted)
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

        private static Container FindNearestContainer(Mobile from, int range)
        {
            Item item = FindNearestItemOfType<Container>(from, range);
            return item as Container;
        }

        private static BaseDoor FindNearestDoor(Mobile from, int range)
        {
            Item item = FindNearestItemOfType<BaseDoor>(from, range);
            return item as BaseDoor;
        }

        private static Item FindNamedItem(Mobile from, string normalizedTarget, int range)
        {
            Item best = null;
            int bestDistance = Int32.MaxValue;
            IPooledEnumerable items = from.Map.GetItemsInRange(from.Location, range);
            foreach (Item item in items)
            {
                if (item == null || item.Deleted)
                    continue;

                if (!MatchesItem(item, normalizedTarget))
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

        private static Mobile FindNamedMobile(Mobile from, string normalizedTarget, int range)
        {
            Mobile exact = null;
            Mobile partial = null;
            int exactDistance = Int32.MaxValue;
            int partialDistance = Int32.MaxValue;

            IPooledEnumerable mobiles = from.Map.GetMobilesInRange(from.Location, range);
            foreach (Mobile mob in mobiles)
            {
                if (mob == null || mob.Deleted)
                    continue;

                string typeName = NormalizeName(mob.GetType().Name);
                string mobName = NormalizeName(mob.Name);
                int distance = (int)Math.Round(from.GetDistanceToSqrt(mob.Location));

                bool exactMatch = typeName == normalizedTarget || mobName == normalizedTarget;
                bool partialMatch = !exactMatch && (typeName.Contains(normalizedTarget) || mobName.Contains(normalizedTarget));

                if (exactMatch && distance < exactDistance)
                {
                    exact = mob;
                    exactDistance = distance;
                }
                else if (partialMatch && distance < partialDistance)
                {
                    partial = mob;
                    partialDistance = distance;
                }
            }
            mobiles.Free();
            return exact ?? partial;
        }

        private static Container ResolveContainerByKind(Mobile from, string targetContainerKind)
        {
            if (String.Equals(targetContainerKind, "requester_backpack", StringComparison.OrdinalIgnoreCase))
                return from.Backpack;

            return null;
        }

        private static Item FindItemInContainer(Container container, string normalizedTarget)
        {
            if (container == null || String.IsNullOrWhiteSpace(normalizedTarget))
                return null;

            Item exact = null;
            Item partial = null;

            for (int i = 0; i < container.Items.Count; i++)
            {
                Item item = container.Items[i];
                if (item == null || item.Deleted)
                    continue;

                string typeName = NormalizeName(item.GetType().Name);
                string itemName = NormalizeName(item.Name);
                bool exactMatch = typeName == normalizedTarget || itemName == normalizedTarget;
                bool partialMatch = !exactMatch && (typeName.Contains(normalizedTarget) || itemName.Contains(normalizedTarget));

                if (exactMatch)
                    return item;

                if (partialMatch && partial == null)
                    partial = item;
            }

            List<Item> queue = new List<Item>();
            for (int i = 0; i < container.Items.Count; i++)
            {
                Container child = container.Items[i] as Container;
                if (child != null)
                    queue.Add(child);
            }

            while (queue.Count > 0)
            {
                Container child = queue[0] as Container;
                queue.RemoveAt(0);
                if (child == null || child.Deleted || child.Items == null)
                    continue;

                for (int i = 0; i < child.Items.Count; i++)
                {
                    Item item = child.Items[i];
                    if (item == null || item.Deleted)
                        continue;

                    string typeName = NormalizeName(item.GetType().Name);
                    string itemName = NormalizeName(item.Name);
                    bool exactMatch = typeName == normalizedTarget || itemName == normalizedTarget;
                    bool partialMatch = !exactMatch && (typeName.Contains(normalizedTarget) || itemName.Contains(normalizedTarget));

                    if (exactMatch)
                        return item;

                    if (partialMatch && partial == null)
                        partial = item;

                    Container nested = item as Container;
                    if (nested != null)
                        queue.Add(nested);
                }
            }

            return partial;
        }

        private static bool MatchesItem(Item item, string normalizedTarget)
        {
            if (item == null || String.IsNullOrWhiteSpace(normalizedTarget))
                return false;

            string typeName = NormalizeName(item.GetType().Name);
            string itemName = NormalizeName(item.Name);
            return typeName == normalizedTarget || itemName == normalizedTarget || typeName.Contains(normalizedTarget) || itemName.Contains(normalizedTarget);
        }

        private static bool MatchesMobile(Mobile mob, string normalizedTarget)
        {
            if (mob == null || String.IsNullOrWhiteSpace(normalizedTarget))
                return false;

            string typeName = NormalizeName(mob.GetType().Name);
            string mobName = NormalizeName(mob.Name);
            return typeName == normalizedTarget || mobName == normalizedTarget || typeName.Contains(normalizedTarget) || mobName.Contains(normalizedTarget);
        }

        private static string NormalizeName(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return String.Empty;

            return value.Trim().ToLowerInvariant().Replace(" ", String.Empty).Replace("-", String.Empty).Replace("_", String.Empty);
        }

        private static Item FindNearestItemOfType<T>(Mobile from, int range) where T : Item
        {
            Item best = null;
            int bestDistance = Int32.MaxValue;
            IPooledEnumerable items = from.Map.GetItemsInRange(from.Location, range);
            foreach (Item item in items)
            {
                if (!(item is T) || item.Deleted)
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
