// =============================================================================
// WAYLANDER GEAR PACK — ServUO Custom Items
// Based on David Gemmell's "Waylander" (1986) — Drenai Saga
// Drop into: Scripts/Custom/WaylanderGear/
// Compile and use [add ClassName to spawn in-game
// All items are Blessed, Indestructible quality artifacts
// =============================================================================
// ITEM LIST:
//   WaylanderCrossbow        — The double-winged Ventrian crossbow
//   WaylanderFightingKnife   — Hip sheath fighting knife (2x carried)
//   WaylanderThrowingKnife   — Throwing knife (3x carried)
//   WaylanderBootKnife       — Concealed boot knife
//   WaylanderHuntingKnife    — Pack hunting knife (gifted to Kai)
//   WaylanderCloak           — The tactical concealment cloak
//   ArmourOfBronze_Chest     — The legendary Armour of Bronze (chest)
//   ArmourOfBronze_Legs      — The legendary Armour of Bronze (legs)
//   ArmourOfBronze_Arms      — The legendary Armour of Bronze (arms)
//   ArmourOfBronze_Helm      — The legendary Armour of Bronze (helm)
//   ArmourOfBronze_Gloves    — The legendary Armour of Bronze (gloves)
//   KarnakSilverAxe          — Karnak's silver-bladed battle axe
//   CadorasStalkerBow        — Cadoras the Stalker's longbow
//   DurmastWarAxe            — Durmast's axe
//   DarkBrotherhood_Chest    — Dark Brotherhood black armour (chest)
//   DarkBrotherhood_Legs     — Dark Brotherhood black armour (legs)
//   DarkBrotherhood_Arms     — Dark Brotherhood black armour (arms)
//   DarkBrotherhood_Helm     — Dark Brotherhood black armour (helm)
//   DarkBrotherhood_Gloves   — Dark Brotherhood black armour (gloves)
//   TheThirty_Chest          — The Thirty silver armour (chest)
//   TheThirty_Legs           — The Thirty silver armour (legs)
//   TheThirty_Arms           — The Thirty silver armour (arms)
//   TheThirty_Helm           — The Thirty silver armour (helm)
//   TheThirty_Gloves         — The Thirty silver armour (gloves)
//   TheThirtySilverSword     — The Thirty's silver sword
//   TheThirtySilverShield    — The Thirty's silver shield
//   KaiHuntingKnife          — The knives given to Kai by Waylander
//   OakenwoodSourceRobe      — Dardalion's Source blue robes
// =============================================================================

using System;
using Server;
using Server.Items;

namespace Server.Items
{
    public class WaylanderCrossbow : Crossbow
    {
        public override string DefaultName { get { return "Waylander's Crossbow"; } }

        [Constructable]
        public WaylanderCrossbow()
        {
            Hue = 1109;
            Quality = ItemQuality.Exceptional;
            MinDamage = 20;
            MaxDamage = 28;
            Speed = 20;
            MaxHitPoints = 255;
            HitPoints = 255;
            Slayer = SlayerName.None;
            Attributes.WeaponSpeed = 30;
            Attributes.WeaponDamage = 50;
            Attributes.AttackChance = 20;
            Attributes.Luck = 0;
            WeaponAttributes.HitLeechHits = 0;
            WeaponAttributes.HitDispel = 25;
            WeaponAttributes.HitLowerDefend = 20;
            WeaponAttributes.ResistPhysicalBonus = 10;
            AosElementDamages.Physical = 90;
            AosElementDamages.Cold = 10;
            SkillBonuses.SetValues(0, SkillName.Archery, 15.0);
            LootType = LootType.Blessed;
        }

        public override WeaponAbility PrimaryAbility { get { return WeaponAbility.MovingShot; } }
        public override WeaponAbility SecondaryAbility { get { return WeaponAbility.DoubleShot; } }

        public WaylanderCrossbow(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class WaylanderFightingKnife : Leafblade
    {
        public override string DefaultName { get { return "Waylander's Fighting Knife"; } }

        [Constructable]
        public WaylanderFightingKnife()
        {
            Hue = 1109;
            Quality = ItemQuality.Exceptional;
            MinDamage = 13;
            MaxDamage = 17;
            Speed = 25;
            MaxHitPoints = 255;
            HitPoints = 255;
            Attributes.WeaponSpeed = 25;
            Attributes.WeaponDamage = 40;
            Attributes.AttackChance = 15;
            WeaponAttributes.HitLeechHits = 20;
            WeaponAttributes.HitLowerDefend = 15;
            AosElementDamages.Physical = 100;
            SkillBonuses.SetValues(0, SkillName.Fencing, 10.0);
            SkillBonuses.SetValues(1, SkillName.Stealth, 5.0);
            LootType = LootType.Blessed;
        }

        public override WeaponAbility PrimaryAbility { get { return WeaponAbility.ArmorIgnore; } }
        public override WeaponAbility SecondaryAbility { get { return WeaponAbility.InfectiousStrike; } }

        public WaylanderFightingKnife(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class WaylanderThrowingKnife : WarCleaver
    {
        public override string DefaultName { get { return "Waylander's Throwing Knife"; } }

        [Constructable]
        public WaylanderThrowingKnife()
        {
            Hue = 1109;
            Quality = ItemQuality.Exceptional;
            MinDamage = 10;
            MaxDamage = 14;
            Speed = 22;
            MaxHitPoints = 200;
            HitPoints = 200;
            Attributes.WeaponSpeed = 20;
            Attributes.WeaponDamage = 30;
            Attributes.AttackChance = 20;
            WeaponAttributes.HitLowerAttack = 20;
            AosElementDamages.Physical = 100;
            SkillBonuses.SetValues(0, SkillName.Throwing, 10.0);
            LootType = LootType.Blessed;
        }

        public override WeaponAbility PrimaryAbility { get { return WeaponAbility.ShadowStrike; } }
        public override WeaponAbility SecondaryAbility { get { return WeaponAbility.ArmorPierce; } }

        public WaylanderThrowingKnife(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class WaylanderBootKnife : Dagger
    {
        public override string DefaultName { get { return "Waylander's Boot Knife"; } }

        [Constructable]
        public WaylanderBootKnife()
        {
            Hue = 1109;
            Quality = ItemQuality.Exceptional;
            MinDamage = 8;
            MaxDamage = 11;
            Speed = 28;
            MaxHitPoints = 200;
            HitPoints = 200;
            Attributes.WeaponSpeed = 30;
            Attributes.WeaponDamage = 20;
            Attributes.AttackChance = 10;
            WeaponAttributes.HitLeechHits = 10;
            AosElementDamages.Physical = 100;
            SkillBonuses.SetValues(0, SkillName.Fencing, 5.0);
            LootType = LootType.Blessed;
        }

        public override WeaponAbility PrimaryAbility { get { return WeaponAbility.InfectiousStrike; } }
        public override WeaponAbility SecondaryAbility { get { return WeaponAbility.ShadowStrike; } }

        public WaylanderBootKnife(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class WaylanderHuntingKnife : Dagger
    {
        public override string DefaultName { get { return "Waylander's Hunting Knife"; } }

        [Constructable]
        public WaylanderHuntingKnife()
        {
            Hue = 1175;
            Quality = ItemQuality.Normal;
            MinDamage = 9;
            MaxDamage = 13;
            Speed = 30;
            MaxHitPoints = 150;
            HitPoints = 150;
            Attributes.WeaponDamage = 15;
            AosElementDamages.Physical = 100;
            LootType = LootType.Blessed;
        }

        public override WeaponAbility PrimaryAbility { get { return WeaponAbility.BleedAttack; } }
        public override WeaponAbility SecondaryAbility { get { return WeaponAbility.Disarm; } }

        public WaylanderHuntingKnife(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class WaylanderCloak : Cloak
    {
        public override string DefaultName { get { return "Waylander's Cloak"; } }

        [Constructable]
        public WaylanderCloak()
        {
            Hue = 1175;
            LootType = LootType.Blessed;
            Attributes.BonusDex = 5;
            Attributes.DefendChance = 10;
            Attributes.RegenStam = 2;
            SkillBonuses.SetValues(0, SkillName.Stealth, 15.0);
            SkillBonuses.SetValues(1, SkillName.Hiding, 10.0);
        }

        public WaylanderCloak(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class ArmourOfBronze_Chest : PlateChest
    {
        public override string DefaultName { get { return "Armour of Bronze"; } }

        [Constructable]
        public ArmourOfBronze_Chest()
        {
            Hue = 2213;
            Quality = ItemQuality.Exceptional;
            LootType = LootType.Blessed;
            MaxHitPoints = 255;
            HitPoints = 255;
            ArmorAttributes.SelfRepair = 5;
            ArmorAttributes.DurabilityBonus = 100;
            ArmorAttributes.MageArmor = 0;
            Attributes.BonusStr = 15;
            Attributes.BonusHits = 25;
            Attributes.DefendChance = 15;
            Attributes.Luck = 150;
            Attributes.RegenHits = 3;
            SkillBonuses.SetValues(0, SkillName.Tactics, 10.0);
            SkillBonuses.SetValues(1, SkillName.Parry, 10.0);
        }

        public ArmourOfBronze_Chest(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class ArmourOfBronze_Legs : PlateLegs
    {
        public override string DefaultName { get { return "Armour of Bronze Greaves"; } }

        [Constructable]
        public ArmourOfBronze_Legs()
        {
            Hue = 2213;
            Quality = ItemQuality.Exceptional;
            LootType = LootType.Blessed;
            MaxHitPoints = 255;
            HitPoints = 255;
            ArmorAttributes.SelfRepair = 5;
            ArmorAttributes.DurabilityBonus = 100;
            Attributes.BonusStr = 10;
            Attributes.BonusDex = 5;
            Attributes.DefendChance = 10;
            Attributes.Luck = 100;
        }

        public ArmourOfBronze_Legs(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class ArmourOfBronze_Arms : PlateArms
    {
        public override string DefaultName { get { return "Armour of Bronze Vambraces"; } }

        [Constructable]
        public ArmourOfBronze_Arms()
        {
            Hue = 2213;
            Quality = ItemQuality.Exceptional;
            LootType = LootType.Blessed;
            MaxHitPoints = 255;
            HitPoints = 255;
            ArmorAttributes.SelfRepair = 5;
            ArmorAttributes.DurabilityBonus = 100;
            Attributes.BonusStr = 8;
            Attributes.DefendChance = 8;
            Attributes.Luck = 100;
        }

        public ArmourOfBronze_Arms(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class ArmourOfBronze_Helm : PlateHelm
    {
        public override string DefaultName { get { return "Armour of Bronze Helm"; } }

        [Constructable]
        public ArmourOfBronze_Helm()
        {
            Hue = 2213;
            Quality = ItemQuality.Exceptional;
            LootType = LootType.Blessed;
            MaxHitPoints = 255;
            HitPoints = 255;
            ArmorAttributes.SelfRepair = 5;
            ArmorAttributes.DurabilityBonus = 100;
            Attributes.BonusHits = 15;
            Attributes.DefendChance = 12;
            Attributes.Luck = 100;
            Attributes.RegenHits = 2;
            SkillBonuses.SetValues(0, SkillName.Tactics, 5.0);
        }

        public ArmourOfBronze_Helm(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class ArmourOfBronze_Gloves : PlateGloves
    {
        public override string DefaultName { get { return "Armour of Bronze Gauntlets"; } }

        [Constructable]
        public ArmourOfBronze_Gloves()
        {
            Hue = 2213;
            Quality = ItemQuality.Exceptional;
            LootType = LootType.Blessed;
            MaxHitPoints = 255;
            HitPoints = 255;
            ArmorAttributes.SelfRepair = 5;
            ArmorAttributes.DurabilityBonus = 100;
            Attributes.BonusStr = 5;
            Attributes.AttackChance = 5;
            Attributes.Luck = 100;
        }

        public ArmourOfBronze_Gloves(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class KarnakSilverAxe : BattleAxe
    {
        public override string DefaultName { get { return "Karnak's Battle Axe"; } }

        [Constructable]
        public KarnakSilverAxe()
        {
            Hue = 1153;
            Quality = ItemQuality.Exceptional;
            LootType = LootType.Blessed;
            MinDamage = 18;
            MaxDamage = 26;
            Speed = 32;
            MaxHitPoints = 255;
            HitPoints = 255;
            Attributes.WeaponDamage = 45;
            Attributes.WeaponSpeed = 15;
            Attributes.AttackChance = 15;
            Attributes.BonusStr = 10;
            Attributes.Luck = 75;
            WeaponAttributes.HitLeechHits = 30;
            WeaponAttributes.HitLowerDefend = 25;
            AosElementDamages.Physical = 90;
            AosElementDamages.Energy = 10;
            SkillBonuses.SetValues(0, SkillName.Swords, 10.0);
            SkillBonuses.SetValues(1, SkillName.Tactics, 15.0);
        }

        public override WeaponAbility PrimaryAbility { get { return WeaponAbility.WhirlwindAttack; } }
        public override WeaponAbility SecondaryAbility { get { return WeaponAbility.CrushingBlow; } }

        public KarnakSilverAxe(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class CadorasStalkerBow : CompositeBow
    {
        public override string DefaultName { get { return "Cadoras the Stalker's Bow"; } }

        [Constructable]
        public CadorasStalkerBow()
        {
            Hue = 1175;
            Quality = ItemQuality.Exceptional;
            LootType = LootType.Blessed;
            MinDamage = 16;
            MaxDamage = 24;
            Speed = 22;
            MaxHitPoints = 255;
            HitPoints = 255;
            Attributes.WeaponDamage = 40;
            Attributes.WeaponSpeed = 20;
            Attributes.AttackChance = 25;
            WeaponAttributes.HitLowerAttack = 25;
            WeaponAttributes.HitLowerDefend = 20;
            AosElementDamages.Physical = 100;
            SkillBonuses.SetValues(0, SkillName.Archery, 15.0);
            SkillBonuses.SetValues(1, SkillName.Tracking, 10.0);
        }

        public override WeaponAbility PrimaryAbility { get { return WeaponAbility.MovingShot; } }
        public override WeaponAbility SecondaryAbility { get { return WeaponAbility.ParalyzingBlow; } }

        public CadorasStalkerBow(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class DurmastWarAxe : WarAxe
    {
        public override string DefaultName { get { return "Durmast's Axe"; } }

        [Constructable]
        public DurmastWarAxe()
        {
            Hue = 1175;
            Quality = ItemQuality.Exceptional;
            LootType = LootType.Blessed;
            MinDamage = 15;
            MaxDamage = 22;
            Speed = 30;
            MaxHitPoints = 255;
            HitPoints = 255;
            Attributes.WeaponDamage = 35;
            Attributes.WeaponSpeed = 10;
            Attributes.Luck = 200;
            Attributes.AttackChance = 10;
            WeaponAttributes.HitLeechHits = 25;
            WeaponAttributes.HitLowerDefend = 15;
            AosElementDamages.Physical = 100;
            SkillBonuses.SetValues(0, SkillName.Swords, 10.0);
        }

        public override WeaponAbility PrimaryAbility { get { return WeaponAbility.FrenziedWhirlwind; } }
        public override WeaponAbility SecondaryAbility { get { return WeaponAbility.BleedAttack; } }

        public DurmastWarAxe(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class DarkBrotherhood_Chest : PlateChest
    {
        public override string DefaultName { get { return "Dark Brotherhood Plate"; } }

        [Constructable]
        public DarkBrotherhood_Chest()
        {
            Hue = 1175;
            Quality = ItemQuality.Exceptional;
            LootType = LootType.Blessed;
            MaxHitPoints = 255;
            HitPoints = 255;
            ArmorAttributes.SelfRepair = 3;
            ArmorAttributes.DurabilityBonus = 80;
            Attributes.BonusHits = 15;
            Attributes.DefendChance = 10;
            Attributes.BonusMana = 20;
            Attributes.RegenMana = 3;
            SkillBonuses.SetValues(0, SkillName.Necromancy, 10.0);
            SkillBonuses.SetValues(1, SkillName.SpiritSpeak, 10.0);
        }

        public DarkBrotherhood_Chest(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class DarkBrotherhood_Legs : PlateLegs
    {
        public override string DefaultName { get { return "Dark Brotherhood Greaves"; } }

        [Constructable]
        public DarkBrotherhood_Legs()
        {
            Hue = 1175;
            Quality = ItemQuality.Exceptional;
            LootType = LootType.Blessed;
            MaxHitPoints = 255;
            HitPoints = 255;
            ArmorAttributes.SelfRepair = 3;
            Attributes.BonusHits = 10;
            Attributes.BonusMana = 10;
        }

        public DarkBrotherhood_Legs(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class DarkBrotherhood_Arms : PlateArms
    {
        public override string DefaultName { get { return "Dark Brotherhood Vambraces"; } }

        [Constructable]
        public DarkBrotherhood_Arms()
        {
            Hue = 1175;
            Quality = ItemQuality.Exceptional;
            LootType = LootType.Blessed;
            MaxHitPoints = 255;
            HitPoints = 255;
            ArmorAttributes.SelfRepair = 3;
            Attributes.BonusMana = 10;
        }

        public DarkBrotherhood_Arms(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class DarkBrotherhood_Helm : PlateHelm
    {
        public override string DefaultName { get { return "Dark Brotherhood Helm"; } }

        [Constructable]
        public DarkBrotherhood_Helm()
        {
            Hue = 1175;
            Quality = ItemQuality.Exceptional;
            LootType = LootType.Blessed;
            MaxHitPoints = 255;
            HitPoints = 255;
            ArmorAttributes.SelfRepair = 3;
            Attributes.BonusMana = 15;
            Attributes.RegenMana = 2;
            SkillBonuses.SetValues(0, SkillName.SpiritSpeak, 5.0);
        }

        public DarkBrotherhood_Helm(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class DarkBrotherhood_Gloves : PlateGloves
    {
        public override string DefaultName { get { return "Dark Brotherhood Gauntlets"; } }

        [Constructable]
        public DarkBrotherhood_Gloves()
        {
            Hue = 1175;
            Quality = ItemQuality.Exceptional;
            LootType = LootType.Blessed;
            MaxHitPoints = 255;
            HitPoints = 255;
            ArmorAttributes.SelfRepair = 3;
            Attributes.BonusMana = 8;
        }

        public DarkBrotherhood_Gloves(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class TheThirty_Chest : PlateChest
    {
        public override string DefaultName { get { return "Armour of the Thirty"; } }

        [Constructable]
        public TheThirty_Chest()
        {
            Hue = 1153;
            Quality = ItemQuality.Exceptional;
            LootType = LootType.Blessed;
            MaxHitPoints = 255;
            HitPoints = 255;
            ArmorAttributes.SelfRepair = 5;
            ArmorAttributes.DurabilityBonus = 100;
            Attributes.BonusHits = 20;
            Attributes.BonusMana = 20;
            Attributes.RegenHits = 3;
            Attributes.RegenMana = 3;
            Attributes.DefendChance = 15;
            Attributes.Luck = 100;
            SkillBonuses.SetValues(0, SkillName.Meditation, 10.0);
            SkillBonuses.SetValues(1, SkillName.Parry, 10.0);
        }

        public TheThirty_Chest(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class TheThirty_Legs : PlateLegs
    {
        public override string DefaultName { get { return "Greaves of the Thirty"; } }

        [Constructable]
        public TheThirty_Legs()
        {
            Hue = 1153;
            Quality = ItemQuality.Exceptional;
            LootType = LootType.Blessed;
            MaxHitPoints = 255;
            HitPoints = 255;
            ArmorAttributes.SelfRepair = 5;
            ArmorAttributes.DurabilityBonus = 100;
            Attributes.BonusHits = 15;
            Attributes.BonusMana = 10;
            Attributes.RegenHits = 2;
        }

        public TheThirty_Legs(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class TheThirty_Arms : PlateArms
    {
        public override string DefaultName { get { return "Vambraces of the Thirty"; } }

        [Constructable]
        public TheThirty_Arms()
        {
            Hue = 1153;
            Quality = ItemQuality.Exceptional;
            LootType = LootType.Blessed;
            MaxHitPoints = 255;
            HitPoints = 255;
            ArmorAttributes.SelfRepair = 5;
            Attributes.BonusMana = 10;
            Attributes.RegenMana = 2;
        }

        public TheThirty_Arms(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class TheThirty_Helm : PlateHelm
    {
        public override string DefaultName { get { return "Helm of the Thirty"; } }

        [Constructable]
        public TheThirty_Helm()
        {
            Hue = 1153;
            Quality = ItemQuality.Exceptional;
            LootType = LootType.Blessed;
            MaxHitPoints = 255;
            HitPoints = 255;
            ArmorAttributes.SelfRepair = 5;
            Attributes.BonusMana = 15;
            Attributes.RegenMana = 3;
            Attributes.DefendChance = 10;
            SkillBonuses.SetValues(0, SkillName.Meditation, 10.0);
            SkillBonuses.SetValues(1, SkillName.SpiritSpeak, 10.0);
        }

        public TheThirty_Helm(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class TheThirty_Gloves : PlateGloves
    {
        public override string DefaultName { get { return "Gauntlets of the Thirty"; } }

        [Constructable]
        public TheThirty_Gloves()
        {
            Hue = 1153;
            Quality = ItemQuality.Exceptional;
            LootType = LootType.Blessed;
            MaxHitPoints = 255;
            HitPoints = 255;
            ArmorAttributes.SelfRepair = 5;
            Attributes.BonusMana = 8;
            Attributes.AttackChance = 5;
        }

        public TheThirty_Gloves(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class TheThirtySilverSword : Longsword
    {
        public override string DefaultName { get { return "Silver Sword of the Thirty"; } }

        [Constructable]
        public TheThirtySilverSword()
        {
            Hue = 1153;
            Quality = ItemQuality.Exceptional;
            LootType = LootType.Blessed;
            MinDamage = 15;
            MaxDamage = 22;
            Speed = 28;
            MaxHitPoints = 255;
            HitPoints = 255;
            Slayer = SlayerName.Repond;
            Slayer2 = SlayerName.Exorcism;
            Attributes.WeaponDamage = 40;
            Attributes.WeaponSpeed = 20;
            Attributes.AttackChance = 15;
            Attributes.BonusMana = 15;
            Attributes.RegenMana = 2;
            WeaponAttributes.HitDispel = 50;
            WeaponAttributes.HitLeechMana = 20;
            WeaponAttributes.HitHarm = 25;
            AosElementDamages.Physical = 50;
            AosElementDamages.Energy = 50;
            SkillBonuses.SetValues(0, SkillName.Swords, 10.0);
            SkillBonuses.SetValues(1, SkillName.Meditation, 10.0);
        }

        public override WeaponAbility PrimaryAbility { get { return WeaponAbility.ArmorIgnore; } }
        public override WeaponAbility SecondaryAbility { get { return WeaponAbility.NerveStrike; } }

        public TheThirtySilverSword(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class TheThirtySilverShield : MetalShield
    {
        public override string DefaultName { get { return "Silver Shield of the Thirty"; } }

        [Constructable]
        public TheThirtySilverShield()
        {
            Hue = 1153;
            Quality = ItemQuality.Exceptional;
            LootType = LootType.Blessed;
            MaxHitPoints = 255;
            HitPoints = 255;
            ArmorAttributes.SelfRepair = 5;
            ArmorAttributes.DurabilityBonus = 100;
            Attributes.DefendChance = 20;
            Attributes.ReflectPhysical = 10;
            Attributes.BonusMana = 10;
            Attributes.RegenMana = 2;
            SkillBonuses.SetValues(0, SkillName.Parry, 15.0);
        }

        public TheThirtySilverShield(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class KaiHuntingKnife : Cleaver
    {
        public override string DefaultName { get { return "Kai's Knife"; } }

        [Constructable]
        public KaiHuntingKnife()
        {
            Hue = 1175;
            Quality = ItemQuality.Normal;
            LootType = LootType.Blessed;
            MinDamage = 10;
            MaxDamage = 15;
            Speed = 28;
            MaxHitPoints = 200;
            HitPoints = 200;
            Attributes.WeaponDamage = 15;
            Attributes.Luck = 50;
            WeaponAttributes.HitLeechHits = 15;
            AosElementDamages.Physical = 100;
        }

        public override WeaponAbility PrimaryAbility { get { return WeaponAbility.BleedAttack; } }
        public override WeaponAbility SecondaryAbility { get { return WeaponAbility.Disarm; } }

        public KaiHuntingKnife(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class OakenwoodSourceRobe : Robe
    {
        public override string DefaultName { get { return "Robes of the Source"; } }

        [Constructable]
        public OakenwoodSourceRobe()
        {
            Hue = 1266;
            LootType = LootType.Blessed;
            Attributes.BonusMana = 30;
            Attributes.RegenMana = 5;
            Attributes.RegenHits = 2;
            Attributes.Luck = 50;
            SkillBonuses.SetValues(0, SkillName.Meditation, 20.0);
            SkillBonuses.SetValues(1, SkillName.SpiritSpeak, 20.0);
            SkillBonuses.SetValues(2, SkillName.Healing, 10.0);
        }

        public OakenwoodSourceRobe(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }
}


