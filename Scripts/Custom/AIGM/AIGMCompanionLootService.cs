using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Custom.AIGM
{
    public sealed class AIGMCompanionLootResult
    {
        public AIGMCompanionLootProfile Profile;
        public int CorpsesFound;
        public int CorpsesLooted;
        public int ItemsMoved;
        public int GoldMoved;
        public readonly List<string> MovedItemTypes = new List<string>();
        public readonly List<string> SkipReasons = new List<string>();
        public string SummaryReason;
    }

    public static class AIGMCompanionLootService
    {
        private const int DefaultRange = 4;
        private static readonly Dictionary<Serial, string> ActiveLooting = new Dictionary<Serial, string>();

        public static List<Corpse> FindNearbyCorpses(Mobile companion, int range)
        {
            List<Corpse> corpses = new List<Corpse>();
            if (companion == null || companion.Deleted || companion.Map == null || range < 0)
                return corpses;

            IPooledEnumerable items = companion.Map.GetItemsInRange(companion.Location, range);
            foreach (Item item in items)
            {
                Corpse corpse = item as Corpse;
                if (corpse == null || corpse.Deleted)
                    continue;

                corpses.Add(corpse);
            }
            items.Free();

            corpses.Sort(delegate(Corpse left, Corpse right)
            {
                double leftDistance = companion.GetDistanceToSqrt(left.Location);
                double rightDistance = companion.GetDistanceToSqrt(right.Location);
                return leftDistance.CompareTo(rightDistance);
            });

            return corpses;
        }

        public static bool CanLootCorpse(Mobile companion, Corpse corpse, Mobile owner, out string reason)
        {
            reason = null;

            if (companion == null || companion.Deleted || corpse == null || corpse.Deleted)
            {
                reason = "missing_companion_or_corpse";
                return false;
            }

            if (companion.Map == null || corpse.Map == null || companion.Map != corpse.Map || !companion.InRange(corpse.Location, DefaultRange))
            {
                reason = "corpse_out_of_range";
                return false;
            }

            Mobile corpseOwner = corpse.Owner;
            if (corpseOwner == null)
            {
                reason = "corpse_owner_unknown";
                return false;
            }

            if (corpseOwner.Player || corpseOwner is PlayerMobile)
            {
                reason = "player_corpse_protected";
                return false;
            }

            if (corpseOwner == companion || corpseOwner is IAIGMCompanionActor || corpseOwner is BaseHire)
            {
                reason = "companion_or_hire_corpse_protected";
                return false;
            }

            if (corpseOwner.AccessLevel > AccessLevel.Player)
            {
                reason = "staff_corpse_protected";
                return false;
            }

            BaseCreature creature = corpseOwner as BaseCreature;
            if (creature == null)
            {
                reason = "not_monster_corpse";
                return false;
            }

            if (creature.Controlled || creature.Summoned || creature.GetMaster() != null || creature.IsBonded)
            {
                reason = "controlled_or_summoned_corpse_protected";
                return false;
            }

            bool clearlyHostile = creature.AlwaysMurderer
                || creature.AlwaysAttackable
                || creature.Murderer
                || creature.Criminal
                || creature.Karma < 0
                || HasRelevantAggression(corpse, companion, owner);

            if (!clearlyHostile)
            {
                reason = "friendly_corpse_protected";
                return false;
            }

            if (!corpse.CanLoot(companion, null))
            {
                reason = "server_loot_access_denied";
                return false;
            }

            return true;
        }

        public static List<Item> GetLootableItems(Corpse corpse, AIGMCompanionLootProfile profile)
        {
            List<Item> items = new List<Item>();
            if (corpse == null || corpse.Deleted)
                return items;

            AIGMCompanionLootProfile effectiveProfile = profile ?? AIGMCompanionLootProfile.Default;
            List<Item> corpseItems = new List<Item>(corpse.Items);
            for (int i = 0; i < corpseItems.Count; i++)
            {
                Item item = corpseItems[i];
                if (item == null || item.Deleted)
                    continue;

                if (IsItemInProfile(item, effectiveProfile))
                    items.Add(item);
            }

            return items;
        }

        public static AIGMCompanionLootResult StartLootNearby(Mobile companion, Mobile owner, AIGMCompanionLootProfile profile)
        {
            AIGMCompanionLootResult result = new AIGMCompanionLootResult();
            result.Profile = profile ?? AIGMCompanionLootProfile.Default;

            if (companion == null || companion.Deleted)
            {
                result.SummaryReason = "missing_companion";
                return result;
            }

            ActiveLooting[companion.Serial] = result.Profile.Name;
            Console.WriteLine("AIGM_LOOT_START companion={0} profile={1} range={2}", companion.Name, result.Profile.Name, DefaultRange);

            List<Corpse> corpses = FindNearbyCorpses(companion, DefaultRange);
            result.CorpsesFound = corpses.Count;

            for (int i = 0; i < corpses.Count; i++)
            {
                Corpse corpse = corpses[i];
                string reason;
                if (!CanLootCorpse(companion, corpse, owner, out reason))
                {
                    AddSkip(result, reason);
                    Console.WriteLine("AIGM_LOOT_SKIP_CORPSE companion={0} corpse={1} reason={2}", companion.Name, corpse.Serial, reason);
                    continue;
                }

                Console.WriteLine("AIGM_LOOT_CORPSE_FOUND companion={0} corpse={1} owner={2}", companion.Name, corpse.Serial, corpse.Owner != null ? corpse.Owner.GetType().Name : "unknown");
                AIGMCompanionLootResult corpseResult;
                if (TryLootCorpse(companion, corpse, result.Profile, out corpseResult))
                    result.CorpsesLooted++;

                Merge(result, corpseResult);
            }

            ActiveLooting.Remove(companion.Serial);

            if (result.CorpsesFound == 0)
                result.SummaryReason = "no_reachable_corpse";
            else if (result.ItemsMoved == 0 && String.IsNullOrWhiteSpace(result.SummaryReason))
                result.SummaryReason = "nothing_safe_to_take";
            else
                result.SummaryReason = "loot_complete";

            Console.WriteLine("AIGM_LOOT_DONE companion={0} profile={1} corpses={2} looted={3} items={4} gold={5} reason={6}",
                companion.Name, result.Profile.Name, result.CorpsesFound, result.CorpsesLooted, result.ItemsMoved, result.GoldMoved, result.SummaryReason);

            if (result.ItemsMoved > 0)
                AIGMCompanionInventoryPolicy.RunBurdenManagement(companion, false);

            return result;
        }

        public static AIGMCompanionLootResult StartLootCorpse(Mobile companion, Mobile owner, Corpse corpse, AIGMCompanionLootProfile profile)
        {
            AIGMCompanionLootResult result = new AIGMCompanionLootResult();
            result.Profile = profile ?? AIGMCompanionLootProfile.Default;

            if (companion == null || companion.Deleted || corpse == null || corpse.Deleted)
            {
                result.SummaryReason = "missing_companion_or_corpse";
                return result;
            }

            ActiveLooting[companion.Serial] = result.Profile.Name;
            result.CorpsesFound = 1;
            Console.WriteLine("AIGM_LOOT_START companion={0} profile={1} range=single corpse={2}", companion.Name, result.Profile.Name, corpse.Serial);

            string reason;
            if (!CanLootCorpse(companion, corpse, owner, out reason))
            {
                AddSkip(result, reason);
                result.SummaryReason = reason;
                ActiveLooting.Remove(companion.Serial);
                Console.WriteLine("AIGM_LOOT_SKIP_CORPSE companion={0} corpse={1} reason={2}", companion.Name, corpse.Serial, reason);
                return result;
            }

            Console.WriteLine("AIGM_LOOT_CORPSE_FOUND companion={0} corpse={1} owner={2}", companion.Name, corpse.Serial, corpse.Owner != null ? corpse.Owner.GetType().Name : "unknown");
            AIGMCompanionLootResult corpseResult;
            if (TryLootCorpse(companion, corpse, result.Profile, out corpseResult))
                result.CorpsesLooted++;

            Merge(result, corpseResult);
            ActiveLooting.Remove(companion.Serial);

            if (result.ItemsMoved == 0 && String.IsNullOrWhiteSpace(result.SummaryReason))
                result.SummaryReason = "nothing_safe_to_take";
            else if (String.IsNullOrWhiteSpace(result.SummaryReason))
                result.SummaryReason = "loot_complete";

            Console.WriteLine("AIGM_LOOT_DONE companion={0} profile={1} corpses={2} looted={3} items={4} gold={5} reason={6}",
                companion.Name, result.Profile.Name, result.CorpsesFound, result.CorpsesLooted, result.ItemsMoved, result.GoldMoved, result.SummaryReason);

            if (result.ItemsMoved > 0)
                AIGMCompanionInventoryPolicy.RunBurdenManagement(companion, false);

            return result;
        }

        public static bool TryLootCorpse(Mobile companion, Corpse corpse, AIGMCompanionLootProfile profile, out AIGMCompanionLootResult result)
        {
            result = new AIGMCompanionLootResult();
            result.Profile = profile ?? AIGMCompanionLootProfile.Default;

            if (companion == null || corpse == null)
            {
                result.SummaryReason = "missing_companion_or_corpse";
                return false;
            }

            List<Item> items = GetLootableItems(corpse, result.Profile);
            for (int i = 0; i < items.Count; i++)
            {
                Item item = items[i];
                string reason;
                int goldAmount = item is Gold ? item.Amount : 0;
                if (TryMoveItemToCompanionPack(companion, corpse, item, out reason))
                {
                    result.ItemsMoved++;
                    result.GoldMoved += goldAmount;
                    string itemType = item.GetType().Name;
                    if (!result.MovedItemTypes.Contains(itemType))
                        result.MovedItemTypes.Add(itemType);

                    Console.WriteLine("AIGM_LOOT_ITEM companion={0} corpse={1} item={2} amount={3}", companion.Name, corpse.Serial, itemType, item.Amount);
                }
                else if (String.Equals(reason, "backpack_full_or_overweight", StringComparison.Ordinal))
                {
                    Console.WriteLine("AIGM_LOOT_RETRY_UNLOAD companion={0} corpse={1} item={2}", companion.Name, corpse.Serial, item.GetType().Name);
                    AIGMCompanionInventoryPolicy.UnloadForLootRetry(companion);
                    if (TryMoveItemToCompanionPack(companion, corpse, item, out reason))
                    {
                        result.ItemsMoved++;
                        result.GoldMoved += goldAmount;
                        string itemType = item.GetType().Name;
                        if (!result.MovedItemTypes.Contains(itemType))
                            result.MovedItemTypes.Add(itemType);

                        Console.WriteLine("AIGM_LOOT_ITEM companion={0} corpse={1} item={2} amount={3} retry=true", companion.Name, corpse.Serial, itemType, item.Amount);
                    }
                    else
                    {
                        AddSkip(result, reason);
                        Console.WriteLine("AIGM_LOOT_SKIP_ITEM companion={0} corpse={1} item={2} reason={3} retry=true", companion.Name, corpse.Serial, item.GetType().Name, reason);
                    }
                }
                else
                {
                    AddSkip(result, reason);
                    Console.WriteLine("AIGM_LOOT_SKIP_ITEM companion={0} corpse={1} item={2} reason={3}", companion.Name, corpse.Serial, item.GetType().Name, reason);
                }
            }

            result.SummaryReason = result.ItemsMoved > 0 ? "corpse_looted" : "no_matching_items";
            return result.ItemsMoved > 0;
        }

        public static bool TryMoveItemToCompanionPack(Mobile companion, Item item, out string reason)
        {
            Corpse corpse = item != null ? item.Parent as Corpse : null;
            return TryMoveItemToCompanionPack(companion, corpse, item, out reason);
        }

        public static bool TryMoveItemToCompanionPack(Mobile companion, Corpse corpse, Item item, out string reason)
        {
            reason = null;

            if (companion == null || companion.Deleted || item == null || item.Deleted)
            {
                reason = "missing_companion_or_item";
                return false;
            }

            Container backpack = companion.Backpack;
            if (backpack == null || backpack.Deleted)
            {
                reason = "missing_companion_backpack";
                return false;
            }

            if (item is Container)
            {
                reason = "nested_container_skipped";
                return false;
            }

            if (!item.Movable)
            {
                reason = "item_not_movable";
                return false;
            }

            if (item.LootType == LootType.Blessed || item.LootType == LootType.Newbied || item.Insured || item.Nontransferable)
            {
                reason = "item_protected";
                return false;
            }

            LRReason reject = LRReason.Inspecific;
            if (corpse != null && !corpse.CheckLift(companion, item, ref reject))
            {
                reason = "corpse_lift_denied_" + reject;
                return false;
            }

            if (!item.CheckLift(companion, item, ref reject))
            {
                reason = "item_lift_denied_" + reject;
                return false;
            }

            if (!backpack.CheckHold(companion, item, false, true))
            {
                reason = "backpack_full_or_overweight";
                return false;
            }

            if (!backpack.TryDropItem(companion, item, false))
            {
                reason = "backpack_drop_failed";
                return false;
            }

            return true;
        }

        public static string StopLooting(Mobile companion)
        {
            if (companion == null)
                return "No looting task is active.";

            bool removed = ActiveLooting.Remove(companion.Serial);
            Console.WriteLine("AIGM_LOOT_STOP companion={0} active={1}", companion.Name, removed);
            return removed ? BuildCharacterLine(companion, "stop") : "No looting task is active.";
        }

        public static string GetLootStatus(Mobile companion)
        {
            if (companion == null)
                return "No looting task is active.";

            string profile;
            if (ActiveLooting.TryGetValue(companion.Serial, out profile))
            {
                Console.WriteLine("AIGM_LOOT_STATUS companion={0} active=true profile={1}", companion.Name, profile);
                return "I am checking the bodies.";
            }

            Console.WriteLine("AIGM_LOOT_STATUS companion={0} active=false", companion.Name);
            return "No looting task is active.";
        }

        public static string BuildVisibleResponse(Mobile companion, AIGMCompanionLootResult result)
        {
            if (result == null)
                return "I found nothing safe to take.";

            if (result.ItemsMoved <= 0)
            {
                if (String.Equals(result.SummaryReason, "no_reachable_corpse", StringComparison.Ordinal))
                    return "No corpse close enough.";

                return "Nothing safe to take.";
            }

            return BuildCharacterLine(companion, "done");
        }

        private static bool IsItemInProfile(Item item, AIGMCompanionLootProfile profile)
        {
            if (item == null)
                return false;

            switch ((profile ?? AIGMCompanionLootProfile.Default).Kind)
            {
                case AIGMCompanionLootProfileKind.GoldOnly:
                    return item is Gold;
                case AIGMCompanionLootProfileKind.Supplies:
                    return IsSupply(item);
                case AIGMCompanionLootProfileKind.Equipment:
                    return IsEquipment(item);
                case AIGMCompanionLootProfileKind.AllMonsterLoot:
                    return true;
                case AIGMCompanionLootProfileKind.Default:
                default:
                    return item is Gold || IsSupply(item);
            }
        }

        private static bool IsSupply(Item item)
        {
            return item is Gold
                || item is IGem
                || item is Bandage
                || item is Arrow
                || item is Bolt
                || item is BaseReagent
                || item is BasePotion;
        }

        private static bool IsEquipment(Item item)
        {
            return item is BaseWeapon
                || item is BaseArmor
                || item is BaseJewel;
        }

        private static bool HasRelevantAggression(Corpse corpse, Mobile companion, Mobile owner)
        {
            if (corpse == null || corpse.Aggressors == null)
                return false;

            for (int i = 0; i < corpse.Aggressors.Count; i++)
            {
                Mobile aggressor = corpse.Aggressors[i];
                if (aggressor == null || aggressor.Deleted)
                    continue;

                if (aggressor == companion || aggressor == owner)
                    return true;
            }

            return false;
        }

        private static string BuildCharacterLine(Mobile companion, string mode)
        {
            string id = GetCompanionId(companion);
            if (String.Equals(mode, "stop", StringComparison.Ordinal))
                return "I will leave the bodies.";

            if (String.Equals(id, "danyal", StringComparison.Ordinal))
                return "I gathered what we can use.";

            if (String.Equals(id, "dardalion", StringComparison.Ordinal))
                return "Only what serves the living.";

            return "I checked the bodies.";
        }

        private static string GetCompanionId(Mobile companion)
        {
            IAIGMCompanionActor actor = companion as IAIGMCompanionActor;
            if (actor != null && !String.IsNullOrWhiteSpace(actor.CompanionId))
                return actor.CompanionId.ToLowerInvariant();

            return companion != null && companion.Name != null ? companion.Name.ToLowerInvariant() : String.Empty;
        }

        private static void Merge(AIGMCompanionLootResult target, AIGMCompanionLootResult source)
        {
            if (target == null || source == null)
                return;

            target.ItemsMoved += source.ItemsMoved;
            target.GoldMoved += source.GoldMoved;
            for (int i = 0; i < source.MovedItemTypes.Count; i++)
            {
                if (!target.MovedItemTypes.Contains(source.MovedItemTypes[i]))
                    target.MovedItemTypes.Add(source.MovedItemTypes[i]);
            }

            for (int i = 0; i < source.SkipReasons.Count; i++)
                AddSkip(target, source.SkipReasons[i]);
        }

        private static void AddSkip(AIGMCompanionLootResult result, string reason)
        {
            if (result == null || String.IsNullOrWhiteSpace(reason))
                return;

            if (!result.SkipReasons.Contains(reason))
                result.SkipReasons.Add(reason);
        }
    }
}
