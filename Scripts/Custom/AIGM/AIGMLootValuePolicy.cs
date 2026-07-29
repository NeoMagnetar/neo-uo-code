using System;
using Server.Items;

namespace Server.Custom.AIGM
{
    public static class AIGMLootValuePolicy
    {
        public static bool IsAlwaysKeep(Item item)
        {
            if (item == null || item.Deleted)
                return true;

            return item is Gold
                || item is IGem
                || item is Bandage
                || item is Arrow
                || item is Bolt
                || item is BaseReagent
                || item is BasePotion;
        }

        public static bool IsProtected(Item item)
        {
            if (item == null || item.Deleted)
                return true;

            return !item.Movable
                || item.LootType == LootType.Blessed
                || item.LootType == LootType.Newbied
                || item.Insured
                || item.Nontransferable;
        }

        public static bool IsSpecialOrHighValue(Item item)
        {
            if (item == null || item.Deleted)
                return true;

            if (IsAlwaysKeep(item) || IsProtected(item))
                return true;

            if (!String.IsNullOrWhiteSpace(item.Name))
                return true;

            if (!item.IsStandardLoot())
                return true;

            BaseWeapon weapon = item as BaseWeapon;
            if (weapon != null)
                return weapon.Slayer != SlayerName.None || weapon.Slayer2 != SlayerName.None || !weapon.Attributes.IsEmpty || !weapon.WeaponAttributes.IsEmpty;

            BaseArmor armor = item as BaseArmor;
            if (armor != null)
                return !armor.Attributes.IsEmpty || !armor.ArmorAttributes.IsEmpty || !armor.WeaponAttributes.IsEmpty || !armor.SkillBonuses.IsEmpty;

            BaseJewel jewel = item as BaseJewel;
            if (jewel != null)
                return !jewel.Attributes.IsEmpty || !jewel.Resistances.IsEmpty || !jewel.SkillBonuses.IsEmpty;

            return false;
        }

        public static bool CanDropForBurden(Item item)
        {
            if (item == null || item.Deleted)
                return false;

            if (IsAlwaysKeep(item) || IsProtected(item) || IsSpecialOrHighValue(item))
                return false;

            return item is BaseWeapon
                || item is BaseArmor
                || item is BaseClothing
                || item is Food
                || item.Weight >= 4.0;
        }

        public static int GetDropPriority(Item item)
        {
            if (item == null || item.Deleted)
                return Int32.MaxValue;

            if (item is BaseWeapon)
                return 10;

            if (item is BaseArmor)
                return 20;

            if (item.Weight >= 8.0)
                return 30;

            if (item is BaseClothing)
                return 40;

            if (item is Food)
                return 50;

            return 100;
        }
    }
}
