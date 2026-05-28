using System;
using Server.Custom.AIGM;
using Server.Gumps;
using Server.Items;

namespace Server.Mobiles
{
    public class AIGMCounselor : BaseCreature
    {
        [Constructable]
        public AIGMCounselor()
            : base(AIType.AI_Vendor, FightMode.None, 2, 1, 0.2, 0.4)
        {
            Name = "Archivist Nox";
            Title = "the AI counselor";
            Body = 0x190;
            Hue = Utility.RandomSkinHue();
            SpeechHue = 0x3B2;
            Blessed = true;
            CantWalk = true;

            Utility.AssignRandomHair(this);
            AddItem(new Robe(0x455));
            AddItem(new Sandals(0x455));
        }

        public override bool IsInvulnerable { get { return true; } }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null)
                return;

            if (from.AccessLevel < AIGMSettings.RequiredAccess)
            {
                SayTo(from, "These archives are reserved for staff.");
                return;
            }

            from.CloseGump(typeof(AIGMQuestionGump));
            from.CloseGump(typeof(AIGMResponseGump));
            from.SendGump(new AIGMQuestionGump(from, this));
            SayTo(from, "State your question, counselor.");
        }

        public AIGMCounselor(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}
