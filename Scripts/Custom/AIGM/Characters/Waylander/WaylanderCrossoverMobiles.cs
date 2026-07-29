using Server.Mobiles;

namespace Server.Custom.AIGM.Characters.Waylander
{
    public class WaylanderDruss : WaylanderRosterMobile
    {
        [Constructable]
        public WaylanderDruss() : base("druss") { }
        public WaylanderDruss(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class WaylanderTenakaKhan : WaylanderRosterMobile
    {
        [Constructable]
        public WaylanderTenakaKhan() : base("tenaka_khan") { }
        public WaylanderTenakaKhan(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class WaylanderNiallad : WaylanderRosterMobile
    {
        [Constructable]
        public WaylanderNiallad() : base("niallad") { }
        public WaylanderNiallad(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class WaylanderSathuliLord : WaylanderRosterMobile
    {
        [Constructable]
        public WaylanderSathuliLord() : base("sathuli_lord") { }
        public WaylanderSathuliLord(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }
}
