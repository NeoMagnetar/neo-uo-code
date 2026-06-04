using System;
using Server.Items;

namespace Server.Mobiles
{
    public class SirMasterCodee : BaseCreature
    {
        [Constructable]
        public SirMasterCodee()
            : base(AIType.AI_Animal, FightMode.None, 10, 1, 0.2, 0.4)
        {
            Name = "Sir Master Codee";
            Title = "the code knight";

            Female = false;
            Race = Race.Human;
            Body = 0x190;
            Hue = Utility.RandomSkinHue();
            Blessed = true;
            CantWalk = true;

            InitStats(90, 90, 25);

            SpeechHue = Utility.RandomDyedHue();

            HairItemID = 0x0; // Bald
            HairHue = 0;
            FacialHairItemID = 0x203F; // Short beard only
            FacialHairHue = 1150;

            AddItem(new Kilt(Utility.RandomPinkHue()));
            AddItem(new BodySash(Utility.RandomGreenHue()));
            AddItem(new FancyShirt(Utility.RandomPinkHue()));
            AddItem(new ThighBoots(Utility.RandomGreenHue()));
            AddItem(new TricorneHat(Utility.RandomPinkHue()));
            AddItem(new Glasses(Utility.RandomGreenHue()));

            Backpack pack = new Backpack();
            pack.Movable = false;
            AddItem(pack);
        }

        public SirMasterCodee(Serial serial)
            : base(serial)
        {
        }

        public override bool ClickTitle
        {
            get
            {
                return true;
            }
        }

        public override bool CanTeach
        {
            get
            {
                return false;
            }
        }

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
}
