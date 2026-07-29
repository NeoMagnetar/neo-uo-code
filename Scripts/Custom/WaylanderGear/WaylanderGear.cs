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
    // =========================================================================
    // WAYLANDER'S DOUBLE-WINGED CROSSBOW
    // Custom-made in Ventria. Small, pistol-gripped, fires two bolts in quick
    // succession. Effective to 20 feet. The signature weapon of the Slayer.
    // Base: Crossbow | Primary: Moving Shot | Secondary: Double Shot
    // Hue: 1109 (dark gunmetal/steel with Ventrian craftsmanship)
    // =========================================================================
    public class WaylanderCrossbow : Crossbow
    {
        public override int ArtifactRarity { get { return 11; } }

        public override string DefaultName { get { return "Waylander's Crossbow"; } }

        [Constructable]
        public WaylanderCrossbow()
        {
            // Appearance
            Hue = 1109;                         // Dark gunmetal — Ventrian steel
            Quality = ItemQuality.Exceptional;

            // Damage — precise, high-velocity bolts; two per cycle
            MinDamage = 20;
            MaxDamage = 28;

            // Speed — fast; the double-wing mechanism makes it the fastest
            // ranged weapon in the Drenai world
            Speed = 20;                         // Fast (lower = faster in ServUO)

            // Durability — masterwork Ventrian construction; indestructible
            MaxHitPoints = 255;
            HitPoints = 255;

            // Slayer — Waylander was effective against the Dark Brotherhood
            // and Vagrian forces equally; no single slayer group
            Slayer = SlayerName.None;

            // Weapon Abilities
            // Primary: Moving Shot — Waylander fires while repositioning (his
            //   signature tactic of sweeping the cloak and firing in motion)
            // Secondary: Double Shot — The defining mechanical feature of the
            //   double-winged crossbow; two bolts per trigger cycle

            // Attributes — reflects assassin-tier craftsmanship and lethality
            Attributes.WeaponSpeed = 30;        // Fast attack speed bonus
            Attributes.WeaponDamage = 50;       // High damage bonus (masterwork)
            Attributes.AttackChance = 20;       // Hit chance increase (accuracy)
            Attributes.Luck = 0;

            // WeaponAttributes
            WeaponAttributes.HitLeechHits = 0;  // Not a life-drain weapon
            WeaponAttributes.HitDispel = 25;    // Effective vs Dark Brotherhood
                                                 // spirit-walkers; disrupts them
            WeaponAttributes.HitLowerDefend = 20; // Waylander's bolts find gaps;
                                                    // lowers target defense rating
            WeaponAttributes.ResistPhysicalBonus = 10;

            // Element damage — physical primary; slight cold (Nadir night terrain)
            // 90% physical / 10% cold
            AosElementDamages.Physical = 90;
            AosElementDamages.Cold = 10;

            // Skill bonus — Archery mastery (Waylander is beyond legendary)
            SkillBonuses.SetValues(0, SkillName.Archery, 15.0);

            // Lore / artifact flags
            LootType = LootType.Blessed;
        }

        public override WeaponAbility PrimaryAbility   { get { return WeaponAbility.MovingShot; } }
        public override WeaponAbility SecondaryAbility { get { return WeaponAbility.DoubleShot; } }

        public WaylanderCrossbow(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    // =========================================================================
    // WAYLANDER'S FIGHTING KNIFE
    // Carried two at the hip. Primary close-combat blades.
    // Base: Leafblade | Hue: 1109 (matching crossbow — unified Ventrian kit)
    // Primary: Armor Ignore | Secondary: Infectious Strike
    // =========================================================================
    public class WaylanderFightingKnife : Leafblade
    {
        public override int ArtifactRarity { get { return 9; } }

        public override string DefaultName { get { return "Waylander's Fighting Knife"; } }

        [Constructable]
        public WaylanderFightingKnife()
        {
            Hue = 1109;
            Quality = ItemQuality.Exceptional;

            MinDamage = 13;
            MaxDamage = 17;
            Speed = 25;                         // Very fast — close-quarters
            MaxHitPoints = 255;
            HitPoints = 255;

            Attributes.WeaponSpeed = 25;
            Attributes.WeaponDamage = 40;
            Attributes.AttackChance = 15;

            WeaponAttributes.HitLeechHits = 20; // Blade strikes vital points
            WeaponAttributes.HitLowerDefend = 15;

            AosElementDamages.Physical = 100;

            SkillBonuses.SetValues(0, SkillName.Fencing, 10.0);
            SkillBonuses.SetValues(1, SkillName.Stealth, 5.0);

            LootType = LootType.Blessed;
        }

        public override WeaponAbility PrimaryAbility   { get { return WeaponAbility.ArmorIgnore; } }
        public override WeaponAbility SecondaryAbility { get { return WeaponAbility.InfectiousStrike; } }

        public WaylanderFightingKnife(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    // =========================================================================
    // WAYLANDER'S THROWING KNIFE
    // Three carried on person at all times. Precision throwing blades.
    // Base: WarCleaver (throwing weapon proxy) | Hue: 1109
    // Primary: Shadow Strike | Secondary: Armor Pierce
    // =========================================================================
    public class WaylanderThrowingKnife : WarCleaver
    {
        public override int ArtifactRarity { get { return 7; } }

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
            Attributes.AttackChance = 20;       // Throwing accuracy

            WeaponAttributes.HitLowerAttack = 20; // Disrupts attacker rhythm

            AosElementDamages.Physical = 100;

            SkillBonuses.SetValues(0, SkillName.Throwing, 10.0);

            LootType = LootType.Blessed;
        }

        public override WeaponAbility PrimaryAbility   { get { return WeaponAbility.ShadowStrike; } }
        public override WeaponAbility SecondaryAbility { get { return WeaponAbility.ArmorPierce; } }

        public WaylanderThrowingKnife(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    // =========================================================================
    // WAYLANDER'S BOOT KNIFE
    // Concealed in boot at all times. Last resort / close-quarters.
    // Base: Dagger | Hue: 1109
    // Primary: Infectious Strike | Secondary: Shadow Strike
    // =========================================================================
    public class WaylanderBootKnife : Dagger
    {
        public override int ArtifactRarity { get { return 6; } }

        public override string DefaultName { get { return "Waylander's Boot Knife"; } }

        [Constructable]
        public WaylanderBootKnife()
        {
            Hue = 1109;
            Quality = ItemQuality.Exceptional;

            MinDamage = 8;
            MaxDamage = 11;
            Speed = 28;                         // Fastest — desperate close range
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

        public override WeaponAbility PrimaryAbility   { get { return WeaponAbility.InfectiousStrike; } }
        public override WeaponAbility SecondaryAbility { get { return WeaponAbility.ShadowStrike; } }

        public WaylanderBootKnife(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    // =========================================================================
    // WAYLANDER'S HUNTING KNIFE
    // Kept in pack. Given to Kai in the foothills after the wolf encounter.
    // When dropped, this is a lesser-quality version of the fighting knife —
    // utility blade, not optimised for assassination.
    // Base: Dagger | Hue: 1175 (worn iron — pack knife)
    // =========================================================================
    public class WaylanderHuntingKnife : Dagger
    {
        public override int ArtifactRarity { get { return 4; } }

        public override string DefaultName { get { return "Waylander's Hunting Knife"; } }

        [Constructable]
        public WaylanderHuntingKnife()
        {
            Hue = 1175;                         // Worn iron hue — utility blade
            Quality = ItemQuality.Normal;

            MinDamage = 9;
            MaxDamage = 13;
            Speed = 30;
            MaxHitPoints = 150;
            HitPoints = 150;

            Attributes.WeaponDamage = 15;
            AosElementDamages.Physical = 100;

            // Note: The second knife given to Kai — use [add WaylanderHuntingKnife
            // twice and rename the second "Kai's Knife" via [props if desired.
            LootType = LootType.Blessed;
        }

        public override WeaponAbility PrimaryAbility   { get { return WeaponAbility.BleedAttack; } }
        public override WeaponAbility SecondaryAbility { get { return WeaponAbility.Disarm; } }

        public WaylanderHuntingKnife(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    // =========================================================================
    // WAYLANDER'S CLOAK
    // The tactical cloak used to conceal the crossbow.
    // Swept aside to allow the first shots to be a complete surprise.
    // Dark, heavy, functional. Base: Cloak | Hue: 1175 (near-black)
    // Provides stealth and evasion bonuses.
    // =========================================================================
    public class WaylanderCloak : Cloak
    {
        public override int ArtifactRarity { get { return 8; } }
        public override int BasePhysicalResistance { get { return 5; } }
        public override int BaseColdResistance { get { return 15; } }

        public override string DefaultName { get { return "Waylander's Cloak"; } }

        [Constructable]
        public WaylanderCloak()
        {
            Hue = 1175;                         // Near-black
            LootType = LootType.Blessed;

            // Stealth / evasion bonuses — the cloak's tactical function
            Attributes.BonusDex = 5;            // Agility in motion
            Attributes.DefendChance = 10;       // Hard to see in the cloak
            Attributes.RegenStam = 2;           // Recovery during movement

            SkillBonuses.SetValues(0, SkillName.Stealth, 15.0);
            SkillBonuses.SetValues(1, SkillName.Hiding, 10.0);

            // Cold resistance — Nadir terrain / mountain cold
        }

        public WaylanderCloak(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    // =========================================================================
    // THE ARMOUR OF BRONZE — CHEST
    // Orien's legendary armour. National symbol of the Drenai.
    // Not magically powerful in itself — its power is what it represents.
    // Effect: massive morale/aura bonus. Inspires those around the wearer.
    // Hue: 2213 (rich bronze/copper)
    // The full set must be worn for the ArtifactRarity and set bonus to apply.
    // =========================================================================
    public class ArmourOfBronze_Chest : PlateChest
    {
        public override int ArtifactRarity { get { return 12; } }
        public override int BasePhysicalResistance { get { return 20; } }
        public override int BaseFireResistance { get { return 10; } }
        public override int BaseColdResistance { get { return 10; } }
        public override int BasePoisonResistance { get { return 5; } }
        public override int BaseEnergyResistance { get { return 5; } }

        public override string DefaultName { get { return "Armour of Bronze"; } }

        [Constructable]
        public ArmourOfBronze_Chest()
        {
            Hue = 2213;                         // Rich bronze — the national colour
            Quality = ItemQuality.Exceptional;
            LootType = LootType.Blessed;

            // Durability — ancient but indestructible; Orien's personal armour
            MaxHitPoints = 255;
            HitPoints = 255;

            // Physical protection — solid bronze plate
            ArmorAttributes.SelfRepair = 5;
            ArmorAttributes.DurabilityBonus = 100;
            ArmorAttributes.MageArmor = 0;

            // Attributes — the Armour's effect on the battlefield
            // Its true power is psychological; translates to massive stat bonuses
            Attributes.BonusStr = 15;           // Bearing of a king
            Attributes.BonusHits = 25;          // Endurance of legend
            Attributes.DefendChance = 15;       // Deflects as if guided
            Attributes.Luck = 150;              // The Chosen One's armour
            Attributes.RegenHits = 3;           // Rallying effect — heals the will

            // Resistances — Bronze is not the best material, but this armour
            // transcends material — the faith of the Drenai people is the shield

            // Skill bonus — the wearer commands presence
            SkillBonuses.SetValues(0, SkillName.Tactics, 10.0);
            SkillBonuses.SetValues(1, SkillName.Parry, 10.0);
        }

        public ArmourOfBronze_Chest(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class ArmourOfBronze_Legs : PlateLegs
    {
        public override int ArtifactRarity { get { return 12; } }
        public override int BasePhysicalResistance { get { return 18; } }
        public override int BaseFireResistance { get { return 8; } }
        public override int BaseColdResistance { get { return 10; } }
        public override int BasePoisonResistance { get { return 5; } }
        public override int BaseEnergyResistance { get { return 5; } }

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
        public override int ArtifactRarity { get { return 12; } }
        public override int BasePhysicalResistance { get { return 16; } }
        public override int BaseFireResistance { get { return 8; } }
        public override int BaseColdResistance { get { return 10; } }
        public override int BasePoisonResistance { get { return 5; } }
        public override int BaseEnergyResistance { get { return 5; } }

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
        public override int ArtifactRarity { get { return 12; } }
        public override int BasePhysicalResistance { get { return 18; } }
        public override int BaseFireResistance { get { return 10; } }
        public override int BaseColdResistance { get { return 10; } }
        public override int BasePoisonResistance { get { return 5; } }
        public override int BaseEnergyResistance { get { return 8; } }

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
        public override int ArtifactRarity { get { return 12; } }
        public override int BasePhysicalResistance { get { return 14; } }
        public override int BaseFireResistance { get { return 8; } }
        public override int BaseColdResistance { get { return 8; } }
        public override int BasePoisonResistance { get { return 5; } }
        public override int BaseEnergyResistance { get { return 5; } }

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

    // =========================================================================
    // KARNAK'S SILVER-BLADED BATTLE AXE
    // The silver-bladed axe of General Karnak. Flamboyant and lethal.
    // Believed by Karnak to be Snaga — the legendary Nadir axe of legend.
    // Base: LargeBattleAxe | Hue: 1153 (bright silver)
    // Primary: Whirlwind Attack | Secondary: Crushing Blow
    // The whirlwind reflects his siege-rally style — sweeping morale attacks.
    // The crushing blow reflects the physical weight of his command presence.
    // =========================================================================
    public class KarnakSilverAxe : LargeBattleAxe
    {
        public override int ArtifactRarity { get { return 10; } }

        public override string DefaultName { get { return "Karnak's Battle Axe"; } }

        [Constructable]
        public KarnakSilverAxe()
        {
            Hue = 1153;                         // Bright silver — his visual identity
            Quality = ItemQuality.Exceptional;
            LootType = LootType.Blessed;

            MinDamage = 18;
            MaxDamage = 26;
            Speed = 32;                         // Heavy but powerful
            MaxHitPoints = 255;
            HitPoints = 255;

            Attributes.WeaponDamage = 45;
            Attributes.WeaponSpeed = 15;
            Attributes.AttackChance = 15;
            Attributes.BonusStr = 10;           // Requires commanding strength
            Attributes.Luck = 75;               // The general's battlefield luck

            WeaponAttributes.HitLeechHits = 30; // Rally — each kill feeds the fight
            WeaponAttributes.HitLowerDefend = 25; // Devastating strikes open armor

            AosElementDamages.Physical = 90;
            AosElementDamages.Energy = 10;      // The silver carries a charge

            SkillBonuses.SetValues(0, SkillName.Swords, 10.0);
            SkillBonuses.SetValues(1, SkillName.Tactics, 15.0);
        }

        public override WeaponAbility PrimaryAbility   { get { return WeaponAbility.WhirlwindAttack; } }
        public override WeaponAbility SecondaryAbility { get { return WeaponAbility.CrushingBlow; } }

        public KarnakSilverAxe(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    // =========================================================================
    // CADORAS THE STALKER'S BOW
    // The bow of Cadoras — a master assassin who operates with a professional
    // code of honor. Patient, precise, will not take an unsporting shot.
    // Base: CompositeBow | Hue: 1175 (dark wood — professional, not showy)
    // Primary: Moving Shot | Secondary: Paralyzing Blow (pinning shot)
    // =========================================================================
    public class CadorasStalkerBow : CompositeBow
    {
        public override int ArtifactRarity { get { return 9; } }

        public override string DefaultName { get { return "Cadoras the Stalker's Bow"; } }

        [Constructable]
        public CadorasStalkerBow()
        {
            Hue = 1175;                         // Dark wood — professional, not showy
            Quality = ItemQuality.Exceptional;
            LootType = LootType.Blessed;

            MinDamage = 16;
            MaxDamage = 24;
            Speed = 22;
            MaxHitPoints = 255;
            HitPoints = 255;

            Attributes.WeaponDamage = 40;
            Attributes.WeaponSpeed = 20;
            Attributes.AttackChance = 25;       // His accuracy is legendary

            WeaponAttributes.HitLowerAttack = 25; // Disrupts target's offense
            WeaponAttributes.HitLowerDefend = 20;

            AosElementDamages.Physical = 100;

            SkillBonuses.SetValues(0, SkillName.Archery, 15.0);
            SkillBonuses.SetValues(1, SkillName.Tracking, 10.0); // The Stalker
        }

        public override WeaponAbility PrimaryAbility   { get { return WeaponAbility.MovingShot; } }
        public override WeaponAbility SecondaryAbility { get { return WeaponAbility.ParalyzingBlow; } }

        public CadorasStalkerBow(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    // =========================================================================
    // DURMAST'S DOUBLE AXE
    // The double axe of Durmast — the rogue who turned out to be the Chosen One.
    // Heavy, brutal, self-interested — until it wasn't.
    // Base: TwoHandedAxe | Hue: 1175 (worn iron — a working weapon, not ornamental)
    // Primary: Frenzied Whirlwind | Secondary: Bleed Attack
    // The Chosen One's weapon — has Luck reflecting the cosmic choice.
    // =========================================================================
    public class DurmastWarAxe : TwoHandedAxe
    {
        public override int ArtifactRarity { get { return 8; } }

        public override string DefaultName { get { return "Durmast's Axe"; } }

        [Constructable]
        public DurmastWarAxe()
        {
            Hue = 1175;                         // Worn iron — a rogue's weapon
            Quality = ItemQuality.Exceptional;
            LootType = LootType.Blessed;

            MinDamage = 15;
            MaxDamage = 22;
            Speed = 30;
            MaxHitPoints = 255;
            HitPoints = 255;

            Attributes.WeaponDamage = 35;
            Attributes.WeaponSpeed = 10;
            Attributes.Luck = 200;              // He was the Chosen One.
                                                // The universe chose him. Luck = canon.
            Attributes.AttackChance = 10;

            WeaponAttributes.HitLeechHits = 25; // A survivor's weapon
            WeaponAttributes.HitLowerDefend = 15;

            AosElementDamages.Physical = 100;

            SkillBonuses.SetValues(0, SkillName.Swords, 10.0);
        }

        public override WeaponAbility PrimaryAbility   { get { return WeaponAbility.FrenziedWhirlwind; } }
        public override WeaponAbility SecondaryAbility { get { return WeaponAbility.BleedAttack; } }

        public DurmastWarAxe(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    // =========================================================================
    // DARK BROTHERHOOD ARMOUR SET
    // The black armour of the Dark Brotherhood — Vagrian mystical enforcers.
    // Spirit-realm fighters. Torturers. Hunters of priests.
    // Hue: 1175 (near-black iron) — visually distinct from the Thirty's silver
    // Enhanced with dark/cold/poison resistance. Chaos-aligned.
    // =========================================================================
    public class DarkBrotherhood_Chest : PlateChest
    {
        public override int ArtifactRarity { get { return 9; } }
        public override int BasePhysicalResistance { get { return 15; } }
        public override int BaseFireResistance { get { return 5; } }
        public override int BaseColdResistance { get { return 20; } }
        public override int BasePoisonResistance { get { return 20; } }
        public override int BaseEnergyResistance { get { return 10; } }

        public override string DefaultName { get { return "Dark Brotherhood Plate"; } }

        [Constructable]
        public DarkBrotherhood_Chest()
        {
            Hue = 1175;                         // Near-black
            Quality = ItemQuality.Exceptional;
            LootType = LootType.Blessed;
            MaxHitPoints = 255;
            HitPoints = 255;
            ArmorAttributes.SelfRepair = 3;
            ArmorAttributes.DurabilityBonus = 80;

            // Dark Brotherhood — spirit realm focus, cold/poison affinity
            Attributes.BonusHits = 15;
            Attributes.DefendChance = 10;
            Attributes.BonusMana = 20;          // Spirit-realm operations require mana
            Attributes.RegenMana = 3;


            SkillBonuses.SetValues(0, SkillName.Necromancy, 10.0); // Chaos alignment
            SkillBonuses.SetValues(1, SkillName.SpiritSpeak, 10.0);
        }

        public DarkBrotherhood_Chest(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class DarkBrotherhood_Legs : PlateLegs
    {
        public override int ArtifactRarity { get { return 9; } }
        public override int BasePhysicalResistance { get { return 12; } }
        public override int BaseFireResistance { get { return 5; } }
        public override int BaseColdResistance { get { return 18; } }
        public override int BasePoisonResistance { get { return 15; } }
        public override int BaseEnergyResistance { get { return 8; } }

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
        public override int ArtifactRarity { get { return 9; } }
        public override int BasePhysicalResistance { get { return 10; } }
        public override int BaseFireResistance { get { return 5; } }
        public override int BaseColdResistance { get { return 15; } }
        public override int BasePoisonResistance { get { return 15; } }
        public override int BaseEnergyResistance { get { return 8; } }

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
        public override int ArtifactRarity { get { return 9; } }
        public override int BasePhysicalResistance { get { return 12; } }
        public override int BaseFireResistance { get { return 5; } }
        public override int BaseColdResistance { get { return 18; } }
        public override int BasePoisonResistance { get { return 18; } }
        public override int BaseEnergyResistance { get { return 10; } }

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
        public override int ArtifactRarity { get { return 9; } }
        public override int BasePhysicalResistance { get { return 10; } }
        public override int BaseFireResistance { get { return 5; } }
        public override int BaseColdResistance { get { return 15; } }
        public override int BasePoisonResistance { get { return 15; } }
        public override int BaseEnergyResistance { get { return 8; } }

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

    // =========================================================================
    // THE THIRTY — SILVER ARMOUR SET
    // Warrior-priests of the Source. Silver armor, white cloak.
    // Fight in both physical and astral realms.
    // Hue: 1153 (bright silver) — the moral inversion of the Dark Brotherhood
    // Enhanced with holy/energy/fire resistance. Source-aligned.
    // =========================================================================
    public class TheThirty_Chest : PlateChest
    {
        public override int ArtifactRarity { get { return 10; } }
        public override int BasePhysicalResistance { get { return 18; } }
        public override int BaseFireResistance { get { return 15; } }
        public override int BaseColdResistance { get { return 10; } }
        public override int BasePoisonResistance { get { return 15; } }
        public override int BaseEnergyResistance { get { return 20; } }

        public override string DefaultName { get { return "Armour of the Thirty"; } }

        [Constructable]
        public TheThirty_Chest()
        {
            Hue = 1153;                         // Bright silver
            Quality = ItemQuality.Exceptional;
            LootType = LootType.Blessed;
            MaxHitPoints = 255;
            HitPoints = 255;
            ArmorAttributes.SelfRepair = 5;
            ArmorAttributes.DurabilityBonus = 100;

            Attributes.BonusHits = 20;
            Attributes.BonusMana = 20;          // Source power flows through them
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
        public override int ArtifactRarity { get { return 10; } }
        public override int BasePhysicalResistance { get { return 16; } }
        public override int BaseFireResistance { get { return 12; } }
        public override int BaseColdResistance { get { return 8; } }
        public override int BasePoisonResistance { get { return 12; } }
        public override int BaseEnergyResistance { get { return 16; } }

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
        public override int ArtifactRarity { get { return 10; } }
        public override int BasePhysicalResistance { get { return 14; } }
        public override int BaseFireResistance { get { return 10; } }
        public override int BaseColdResistance { get { return 8; } }
        public override int BasePoisonResistance { get { return 10; } }
        public override int BaseEnergyResistance { get { return 14; } }

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
        public override int ArtifactRarity { get { return 10; } }
        public override int BasePhysicalResistance { get { return 15; } }
        public override int BaseFireResistance { get { return 15; } }
        public override int BaseColdResistance { get { return 10; } }
        public override int BasePoisonResistance { get { return 12; } }
        public override int BaseEnergyResistance { get { return 18; } }

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
        public override int ArtifactRarity { get { return 10; } }
        public override int BasePhysicalResistance { get { return 12; } }
        public override int BaseFireResistance { get { return 10; } }
        public override int BaseColdResistance { get { return 8; } }
        public override int BasePoisonResistance { get { return 10; } }
        public override int BaseEnergyResistance { get { return 14; } }

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

    // =========================================================================
    // THE THIRTY'S SILVER SWORD
    // The silver sword of the Thirty — used in physical and astral combat.
    // Dispels spirits, disrupts dark magic, serves the Source.
    // Base: Longsword | Hue: 1153 (bright silver)
    // Primary: Armor Ignore | Secondary: Nerve Strike
    // Slayer: Undead (spirit/chaos entities; Dark Brotherhood alignment)
    // =========================================================================
    public class TheThirtySilverSword : Longsword
    {
        public override int ArtifactRarity { get { return 10; } }

        public override string DefaultName { get { return "Silver Sword of the Thirty"; } }

        [Constructable]
        public TheThirtySilverSword()
        {
            Hue = 1153;                         // Silver
            Quality = ItemQuality.Exceptional;
            LootType = LootType.Blessed;

            MinDamage = 15;
            MaxDamage = 22;
            Speed = 28;
            MaxHitPoints = 255;
            HitPoints = 255;

            // Slayer — effective against the Dark Brotherhood
            // (spirit-realm entities, chaos-aligned)
            Slayer = SlayerName.Repond;         // Spirit/undead slayer class
            Slayer2 = SlayerName.Exorcism;      // Chaos/demon slayer

            Attributes.WeaponDamage = 40;
            Attributes.WeaponSpeed = 20;
            Attributes.AttackChance = 15;
            Attributes.BonusMana = 15;
            Attributes.RegenMana = 2;

            WeaponAttributes.HitDispel = 50;    // Primary function vs Dark Brotherhood —
                                                 // dispelling their spirit forms
            WeaponAttributes.HitLeechMana = 20; // Recovers mana from enemy spirits
            WeaponAttributes.HitHarm = 25;      // The Harm spell — Source counterattack

            AosElementDamages.Physical = 50;
            AosElementDamages.Energy = 50;      // Holy energy; the Source's light

            SkillBonuses.SetValues(0, SkillName.Swords, 10.0);
            SkillBonuses.SetValues(1, SkillName.Meditation, 10.0);
        }

        public override WeaponAbility PrimaryAbility   { get { return WeaponAbility.ArmorIgnore; } }
        public override WeaponAbility SecondaryAbility { get { return WeaponAbility.NerveStrike; } }

        public TheThirtySilverSword(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    // =========================================================================
    // THE THIRTY'S SILVER SHIELD
    // The silver shield of the Thirty — physical and astral deflection.
    // Base: MetalShield | Hue: 1153 (bright silver)
    // =========================================================================
    public class TheThirtySilverShield : MetalShield
    {
        public override int ArtifactRarity { get { return 10; } }
        public override int BasePhysicalResistance { get { return 15; } }
        public override int BaseFireResistance { get { return 15; } }
        public override int BaseColdResistance { get { return 12; } }
        public override int BasePoisonResistance { get { return 15; } }
        public override int BaseEnergyResistance { get { return 20; } }

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

            Attributes.DefendChance = 20;       // Shield blocks in spirit realm too
            Attributes.ReflectPhysical = 10;    // Turns blows back
            Attributes.BonusMana = 10;
            Attributes.RegenMana = 2;


            SkillBonuses.SetValues(0, SkillName.Parry, 15.0);
        }

        public TheThirtySilverShield(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    // =========================================================================
    // KAI'S HUNTING KNIFE
    // The knives given to Kai by Waylander after the wolf encounter.
    // Same base as WaylanderHuntingKnife but renamed as Kai's item.
    // Kai uses them to ensure Danyal completes the journey south safely.
    // Hue: 1175 — the same worn iron Waylander carried in his pack.
    // =========================================================================
    public class KaiHuntingKnife : Dagger
    {
        public override int ArtifactRarity { get { return 5; } }

        public override string DefaultName { get { return "Kai's Knife"; } }

        [Constructable]
        public KaiHuntingKnife()
        {
            Hue = 1175;
            Quality = ItemQuality.Normal;    // Utility blade, not masterwork
            LootType = LootType.Blessed;

            MinDamage = 10;
            MaxDamage = 15;
            Speed = 28;
            MaxHitPoints = 200;
            HitPoints = 200;

            Attributes.WeaponDamage = 15;
            Attributes.Luck = 50;               // Waylander's luck passed with the gift

            WeaponAttributes.HitLeechHits = 15; // Kai's healing nature —
                                                  // the blade that heals as it protects
            AosElementDamages.Physical = 100;
        }

        public override WeaponAbility PrimaryAbility   { get { return WeaponAbility.BleedAttack; } }
        public override WeaponAbility SecondaryAbility { get { return WeaponAbility.Disarm; } }

        public KaiHuntingKnife(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    // =========================================================================
    // DARDALION'S SOURCE BLUE ROBES
    // The blue robes of the Source Priesthood — Dardalion's pre-transformation
    // identifier. Marks the wearer for Vagrian execution. Pacifist garment.
    // Hue: 1266 (deep cerulean blue)
    // Bonuses: Meditation, Spirit Speak, Mana regen — zero combat ability.
    // =========================================================================
    public class OakenwoodSourceRobe : Robe
    {
        public override int ArtifactRarity { get { return 6; } }
        public override int BasePhysicalResistance { get { return 5; } }
        public override int BaseFireResistance { get { return 5; } }
        public override int BaseColdResistance { get { return 10; } }
        public override int BasePoisonResistance { get { return 10; } }
        public override int BaseEnergyResistance { get { return 15; } }

        public override string DefaultName { get { return "Robes of the Source"; } }

        [Constructable]
        public OakenwoodSourceRobe()
        {
            Hue = 1266;                         // Deep cerulean — Source blue
            LootType = LootType.Blessed;

            // Purely spiritual — zero combat enhancement
            Attributes.BonusMana = 30;
            Attributes.RegenMana = 5;
            Attributes.RegenHits = 2;
            Attributes.Luck = 50;               // The Source provides


            SkillBonuses.SetValues(0, SkillName.Meditation, 20.0);
            SkillBonuses.SetValues(1, SkillName.SpiritSpeak, 20.0);
            SkillBonuses.SetValues(2, SkillName.Healing, 10.0);
        }

        public OakenwoodSourceRobe(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

} // end namespace Server.Items

// =============================================================================
// SPAWN GUIDE — use [add ClassName in-game or server console
// =============================================================================
// WAYLANDER KIT:
//   [add WaylanderCrossbow
//   [add WaylanderFightingKnife   (add twice for both hip knives)
//   [add WaylanderThrowingKnife   (add 3x)
//   [add WaylanderBootKnife
//   [add WaylanderHuntingKnife
//   [add WaylanderCloak
//
// ARMOUR OF BRONZE (full set):
//   [add ArmourOfBronze_Chest
//   [add ArmourOfBronze_Legs
//   [add ArmourOfBronze_Arms
//   [add ArmourOfBronze_Helm
//   [add ArmourOfBronze_Gloves
//
// KARNAK:
//   [add KarnakSilverAxe
//
// CADORAS:
//   [add CadorasStalkerBow
//
// DURMAST:
//   [add DurmastWarAxe
//
// DARK BROTHERHOOD SET:
//   [add DarkBrotherhood_Chest
//   [add DarkBrotherhood_Legs
//   [add DarkBrotherhood_Arms
//   [add DarkBrotherhood_Helm
//   [add DarkBrotherhood_Gloves
//
// THE THIRTY SET:
//   [add TheThirty_Chest
//   [add TheThirty_Legs
//   [add TheThirty_Arms
//   [add TheThirty_Helm
//   [add TheThirty_Gloves
//   [add TheThirtySilverSword
//   [add TheThirtySilverShield
//
// KAI:
//   [add KaiHuntingKnife         (add 2x — Waylander gave both)
//
// DARDALION:
//   [add OakenwoodSourceRobe
// =============================================================================
