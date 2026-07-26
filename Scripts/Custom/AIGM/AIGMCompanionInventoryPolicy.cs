using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public sealed class AIGMCompanionInventoryPolicyResult
    {
        public bool SoftThresholdExceeded;
        public bool HardThresholdExceeded;
        public int CurrentWeight;
        public int MaxWeight;
        public int DroppedItems;
        public int DroppedWeight;
        public readonly List<string> DroppedItemTypes = new List<string>();
        public string Reason;
    }

    public static class AIGMCompanionInventoryPolicy
    {
        public const double SoftThreshold = 0.80;
        public const double HardThreshold = 0.95;
        public const double TargetThreshold = 0.78;

        public static AIGMCompanionInventoryPolicyResult CheckBurden(Mobile companion)
        {
            AIGMCompanionInventoryPolicyResult result = BuildSnapshot(companion);
            if (result.MaxWeight <= 0)
            {
                result.Reason = "no_weight_limit";
                return result;
            }

            result.SoftThresholdExceeded = result.CurrentWeight >= GetThresholdWeight(result.MaxWeight, SoftThreshold);
            result.HardThresholdExceeded = result.CurrentWeight >= GetThresholdWeight(result.MaxWeight, HardThreshold);
            result.Reason = result.HardThresholdExceeded ? "hard_threshold_exceeded" : (result.SoftThresholdExceeded ? "soft_threshold_exceeded" : "burden_ok");

            Console.WriteLine("AIGM_BURDEN_STATUS companion={0} weight={1}/{2} soft={3} hard={4} reason={5}",
                companion != null ? companion.Name : "unknown", result.CurrentWeight, result.MaxWeight, result.SoftThresholdExceeded, result.HardThresholdExceeded, result.Reason);

            return result;
        }

        public static AIGMCompanionInventoryPolicyResult RunBurdenManagement(Mobile companion, bool force)
        {
            AIGMCompanionInventoryPolicyResult result = CheckBurden(companion);
            if (companion == null || companion.Deleted || companion.Backpack == null || companion.Map == null)
                return result;

            if (!force && !result.HardThresholdExceeded)
                return result;

            int targetWeight = result.MaxWeight > 0 ? GetThresholdWeight(result.MaxWeight, TargetThreshold) : Math.Max(0, result.CurrentWeight - 20);
            List<Item> candidates = GetDropCandidates(companion.Backpack);

            for (int i = 0; i < candidates.Count && companion.Backpack.TotalWeight > targetWeight; i++)
            {
                Item item = candidates[i];
                if (item == null || item.Deleted || item.Parent != companion.Backpack)
                    continue;

                int weight = item.TotalWeight + item.PileWeight;
                string type = item.GetType().Name;
                item.MoveToWorld(companion.Location, companion.Map);

                result.DroppedItems++;
                result.DroppedWeight += weight;
                if (!result.DroppedItemTypes.Contains(type))
                    result.DroppedItemTypes.Add(type);

                Console.WriteLine("AIGM_BURDEN_DROP companion={0} item={1} weight={2}", companion.Name, type, weight);
            }

            result.CurrentWeight = companion.Backpack != null ? companion.Backpack.TotalWeight : result.CurrentWeight;
            result.SoftThresholdExceeded = result.MaxWeight > 0 && result.CurrentWeight >= GetThresholdWeight(result.MaxWeight, SoftThreshold);
            result.HardThresholdExceeded = result.MaxWeight > 0 && result.CurrentWeight >= GetThresholdWeight(result.MaxWeight, HardThreshold);
            result.Reason = result.DroppedItems > 0 ? "burden_reduced" : (result.HardThresholdExceeded ? "no_low_priority_items" : result.Reason);

            Console.WriteLine("AIGM_BURDEN_DONE companion={0} weight={1}/{2} dropped={3} droppedWeight={4} reason={5}",
                companion.Name, result.CurrentWeight, result.MaxWeight, result.DroppedItems, result.DroppedWeight, result.Reason);

            return result;
        }

        public static AIGMCompanionInventoryPolicyResult UnloadForLootRetry(Mobile companion)
        {
            return RunBurdenManagement(companion, true);
        }

        public static string BuildStatusLine(Mobile companion)
        {
            AIGMCompanionInventoryPolicyResult result = CheckBurden(companion);
            if (result.MaxWeight <= 0)
                return "Pack burden has no fixed limit.";

            return String.Format("Pack burden: {0}/{1} stones. {2}", result.CurrentWeight, result.MaxWeight, result.Reason);
        }

        private static AIGMCompanionInventoryPolicyResult BuildSnapshot(Mobile companion)
        {
            AIGMCompanionInventoryPolicyResult result = new AIGMCompanionInventoryPolicyResult();
            Container backpack = companion != null ? companion.Backpack : null;
            result.CurrentWeight = backpack != null ? backpack.TotalWeight : 0;
            result.MaxWeight = backpack != null ? backpack.MaxWeight : 0;
            result.Reason = backpack == null ? "missing_backpack" : "burden_ok";
            return result;
        }

        private static List<Item> GetDropCandidates(Container backpack)
        {
            List<Item> candidates = new List<Item>();
            if (backpack == null || backpack.Deleted)
                return candidates;

            List<Item> items = new List<Item>(backpack.Items);
            for (int i = 0; i < items.Count; i++)
            {
                Item item = items[i];
                if (AIGMLootValuePolicy.CanDropForBurden(item))
                    candidates.Add(item);
            }

            candidates.Sort(delegate(Item left, Item right)
            {
                int priority = AIGMLootValuePolicy.GetDropPriority(left).CompareTo(AIGMLootValuePolicy.GetDropPriority(right));
                if (priority != 0)
                    return priority;

                int leftWeight = left != null ? left.TotalWeight + left.PileWeight : 0;
                int rightWeight = right != null ? right.TotalWeight + right.PileWeight : 0;
                return rightWeight.CompareTo(leftWeight);
            });

            return candidates;
        }

        private static int GetThresholdWeight(int maxWeight, double threshold)
        {
            return (int)Math.Ceiling(maxWeight * threshold);
        }
    }
}
