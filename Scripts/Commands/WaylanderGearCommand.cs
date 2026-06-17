using Server.Items;

namespace Server.Commands
{
    public class WaylanderGearCommand
    {
        public static void Initialize()
        {
            CommandSystem.Register("WaylanderGear", AccessLevel.GameMaster, OnCommand);
        }

        [Usage("WaylanderGear")]
        [Description("Creates a chest containing the full Waylander gear collection.")]
        private static void OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            if (from == null)
            {
                return;
            }

            MetalGoldenChest chest = new MetalGoldenChest
            {
                Name = "Waylander Gear Chest",
                LootType = LootType.Blessed
            };

            PackWaylanderGear(chest);

            if (from.Backpack != null && from.Backpack.TryDropItem(from, chest, false))
            {
                from.SendMessage("A Waylander Gear Chest has been placed in your backpack.");
                return;
            }

            chest.MoveToWorld(from.Location, from.Map);
            from.SendMessage("A Waylander Gear Chest has been placed at your feet.");
        }

        private static void PackWaylanderGear(Container chest)
        {
            chest.DropItem(new WaylanderCrossbow());
            chest.DropItem(new WaylanderFightingKnife());
            chest.DropItem(new WaylanderFightingKnife());
            chest.DropItem(new WaylanderThrowingKnife());
            chest.DropItem(new WaylanderThrowingKnife());
            chest.DropItem(new WaylanderThrowingKnife());
            chest.DropItem(new WaylanderBootKnife());
            chest.DropItem(new WaylanderHuntingKnife());
            chest.DropItem(new WaylanderCloak());

            chest.DropItem(new ArmourOfBronze_Chest());
            chest.DropItem(new ArmourOfBronze_Legs());
            chest.DropItem(new ArmourOfBronze_Arms());
            chest.DropItem(new ArmourOfBronze_Helm());
            chest.DropItem(new ArmourOfBronze_Gloves());

            chest.DropItem(new KarnakSilverAxe());
            chest.DropItem(new CadorasStalkerBow());
            chest.DropItem(new DurmastWarAxe());

            chest.DropItem(new DarkBrotherhood_Chest());
            chest.DropItem(new DarkBrotherhood_Legs());
            chest.DropItem(new DarkBrotherhood_Arms());
            chest.DropItem(new DarkBrotherhood_Helm());
            chest.DropItem(new DarkBrotherhood_Gloves());

            chest.DropItem(new TheThirty_Chest());
            chest.DropItem(new TheThirty_Legs());
            chest.DropItem(new TheThirty_Arms());
            chest.DropItem(new TheThirty_Helm());
            chest.DropItem(new TheThirty_Gloves());
            chest.DropItem(new TheThirtySilverSword());
            chest.DropItem(new TheThirtySilverShield());

            chest.DropItem(new KaiHuntingKnife());
            chest.DropItem(new KaiHuntingKnife());

            chest.DropItem(new OakenwoodSourceRobe());
        }
    }
}
