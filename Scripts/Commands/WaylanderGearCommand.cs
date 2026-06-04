using System;
using Server.Commands;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Commands
{
    public static class WaylanderGearCommand
    {
        public static void Initialize()
        {
            CommandSystem.Register("WaylanderGear", AccessLevel.GameMaster, new CommandEventHandler(OnWaylanderGear));
        }

        [Usage("WaylanderGear")]
        [Description("Places a blessed chest containing the full Waylander gear collection at the targeted location.")]
        private static void OnWaylanderGear(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target where you want to place the Waylander gear chest.");
            e.Mobile.Target = new WaylanderGearTarget();
        }

        private sealed class WaylanderGearTarget : Target
        {
            public WaylanderGearTarget() : base(-1, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                IPoint3D p = targeted as IPoint3D;
                if (p == null)
                {
                    from.SendMessage("That is not a valid placement target.");
                    return;
                }

                if (p is Item)
                    p = ((Item)p).GetWorldTop();
                else if (p is Mobile)
                    p = ((Mobile)p).Location;

                Point3D loc = new Point3D(p);
                Map map = from.Map;

                MetalGoldenChest chest = new MetalGoldenChest();
                chest.Name = "Waylander Gear Chest";
                chest.Hue = 1109;
                chest.LootType = LootType.Blessed;
                chest.Movable = true;

                PackFullSet(chest);
                chest.MoveToWorld(loc, map);
                from.SendMessage("The Waylander gear chest has been placed.");
            }
        }

        private static void PackFullSet(Container chest)
        {
            if (chest == null)
                return;

            AddBlessed(chest, new WaylanderCrossbow());
            AddBlessed(chest, new WaylanderFightingKnife());
            AddBlessed(chest, new WaylanderFightingKnife());
            AddBlessed(chest, new WaylanderThrowingKnife());
            AddBlessed(chest, new WaylanderThrowingKnife());
            AddBlessed(chest, new WaylanderThrowingKnife());
            AddBlessed(chest, new WaylanderBootKnife());
            AddBlessed(chest, new WaylanderHuntingKnife());
            AddBlessed(chest, new WaylanderCloak());

            AddBlessed(chest, new ArmourOfBronze_Chest());
            AddBlessed(chest, new ArmourOfBronze_Legs());
            AddBlessed(chest, new ArmourOfBronze_Arms());
            AddBlessed(chest, new ArmourOfBronze_Helm());
            AddBlessed(chest, new ArmourOfBronze_Gloves());

            AddBlessed(chest, new KarnakSilverAxe());
            AddBlessed(chest, new CadorasStalkerBow());
            AddBlessed(chest, new DurmastWarAxe());

            AddBlessed(chest, new DarkBrotherhood_Chest());
            AddBlessed(chest, new DarkBrotherhood_Legs());
            AddBlessed(chest, new DarkBrotherhood_Arms());
            AddBlessed(chest, new DarkBrotherhood_Helm());
            AddBlessed(chest, new DarkBrotherhood_Gloves());

            AddBlessed(chest, new TheThirty_Chest());
            AddBlessed(chest, new TheThirty_Legs());
            AddBlessed(chest, new TheThirty_Arms());
            AddBlessed(chest, new TheThirty_Helm());
            AddBlessed(chest, new TheThirty_Gloves());
            AddBlessed(chest, new TheThirtySilverSword());
            AddBlessed(chest, new TheThirtySilverShield());

            AddBlessed(chest, new KaiHuntingKnife());
            AddBlessed(chest, new KaiHuntingKnife());
            AddBlessed(chest, new OakenwoodSourceRobe());
        }

        private static void AddBlessed(Container chest, Item item)
        {
            if (chest == null || item == null)
                return;

            item.LootType = LootType.Blessed;
            chest.DropItem(item);
        }
    }
}
