using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Commands
{
    public static class AIGMCompanionCommand
    {
        public static void Initialize()
        {
            CommandSystem.Register("AIGMCompanion", AccessLevel.GameMaster, new CommandEventHandler(OnDakeyrasCommand));
            CommandSystem.Register("Dakeyras", AccessLevel.GameMaster, new CommandEventHandler(OnDakeyrasCommand));
            CommandSystem.Register("Danyal", AccessLevel.GameMaster, new CommandEventHandler(OnDanyalCommand));
            CommandSystem.Register("Dardalion", AccessLevel.GameMaster, new CommandEventHandler(OnDardalionCommand));
            CommandSystem.Register("WaylanderGear", AccessLevel.GameMaster, new CommandEventHandler(OnWaylanderGearCommand));
            CommandSystem.Register("WaylanderPurgeLegacy", AccessLevel.GameMaster, new CommandEventHandler(OnWaylanderPurgeLegacyCommand));
        }

        [Usage("AIGMCompanion")]
        [Description("Places Dakeyras, the AI companion, at the targeted location.")]
        private static void OnDakeyrasCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target where you want to place Dakeyras.");
            e.Mobile.Target = new DakeyrasTarget();
        }

        [Usage("Danyal")]
        [Description("Places Danyal, the AI companion, at the targeted location.")]
        private static void OnDanyalCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target where you want to place Danyal.");
            e.Mobile.Target = new DanyalTarget();
        }

        [Usage("Dardalion")]
        [Description("Places Dardalion, the AI companion, at the targeted location.")]
        private static void OnDardalionCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target where you want to place Dardalion.");
            e.Mobile.Target = new DardalionTarget();
        }

        [Usage("WaylanderGear")]
        [Description("Places a blessed chest containing the full Waylander gear collection at the targeted location.")]
        private static void OnWaylanderGearCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target where you want to place the Waylander gear chest.");
            e.Mobile.Target = new WaylanderGearTarget();
        }

        [Usage("WaylanderPurgeLegacy")]
        [Description("Deletes legacy Waylander weapon instances that must be purged before swapping their class base types.")]
        private static void OnWaylanderPurgeLegacyCommand(CommandEventArgs e)
        {
            int deleted = 0;
            List<Item> snapshot = new List<Item>(World.Items.Values);

            foreach (Item item in snapshot)
            {
                if (item == null || item.Deleted)
                    continue;

                if (item is WaylanderFightingKnife
                    || item is WaylanderHuntingKnife
                    || item is KaiHuntingKnife
                    || item is KarnakSilverAxe
                    || item is DurmastWarAxe)
                {
                    item.Delete();
                    deleted++;
                }
            }

            e.Mobile.SendMessage("Purged " + deleted + " legacy Waylander weapon items. Save the world before switching class base types.");
        }

        private class DakeyrasTarget : Target
        {
            public DakeyrasTarget()
                : base(-1, true, TargetFlags.None)
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

                AIGMCompanionDakeyras dakeyras = new AIGMCompanionDakeyras();
                dakeyras.MoveToWorld(loc, map);
                from.SendMessage("Dakeyras has been placed.");
            }
        }

        private class DanyalTarget : Target
        {
            public DanyalTarget()
                : base(-1, true, TargetFlags.None)
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

                AIGMCompanionDanyal danyal = new AIGMCompanionDanyal();
                danyal.MoveToWorld(loc, map);
                from.SendMessage("Danyal has been placed.");
            }
        }

        private class DardalionTarget : Target
        {
            public DardalionTarget()
                : base(-1, true, TargetFlags.None)
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

                AIGMCompanionDardalion dardalion = new AIGMCompanionDardalion();
                dardalion.MoveToWorld(loc, map);
                from.SendMessage("Dardalion has been placed.");
            }
        }

        private class WaylanderGearTarget : Target
        {
            public WaylanderGearTarget()
                : base(-1, true, TargetFlags.None)
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

                chest.MoveToWorld(loc, map);
                from.SendMessage("The Waylander gear chest has been placed.");
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
}
