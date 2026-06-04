using Server.Items;
using Server.Targeting;

namespace Server.Mobiles
{
    public class AIGMCounselorVendorBackpack : Backpack
    {
        public AIGMCounselorVendorBackpack()
        {
            Layer = Layer.Backpack;
            Weight = 1.0;
            Movable = false;
        }

        public AIGMCounselorVendorBackpack(Serial serial)
            : base(serial)
        {
        }

        public override int DefaultMaxWeight { get { return 0; } }

        public override bool IsAccessibleTo(Mobile m)
        {
            return m != null && m.AccessLevel >= Server.Custom.AIGM.AIGMSettings.RequiredAccess;
        }

        public override bool CheckTarget(Mobile from, Target targ, object targeted)
        {
            if (!base.CheckTarget(from, targ, targeted))
                return false;

            return from != null && from.AccessLevel >= Server.Custom.AIGM.AIGMSettings.RequiredAccess;
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

            Layer = Layer.Backpack;
            Movable = false;
        }
    }
}
