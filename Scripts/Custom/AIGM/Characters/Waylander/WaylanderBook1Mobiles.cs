using Server.Mobiles;

namespace Server.Custom.AIGM.Characters.Waylander
{
    public class WaylanderDurmast : WaylanderRosterMobile
    {
        [Constructable]
        public WaylanderDurmast() : base("durmast") { }
        public WaylanderDurmast(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class WaylanderCadoras : WaylanderRosterMobile
    {
        [Constructable]
        public WaylanderCadoras() : base("cadoras") { }
        public WaylanderCadoras(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class WaylanderKarnak : WaylanderRosterMobile
    {
        [Constructable]
        public WaylanderKarnak() : base("karnak") { }
        public WaylanderKarnak(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class WaylanderEgel : WaylanderRosterMobile
    {
        [Constructable]
        public WaylanderEgel() : base("egel") { }
        public WaylanderEgel(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class WaylanderGellan : WaylanderRosterMobile
    {
        [Constructable]
        public WaylanderGellan() : base("gellan") { }
        public WaylanderGellan(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class WaylanderJonat : WaylanderRosterMobile
    {
        [Constructable]
        public WaylanderJonat() : base("jonat") { }
        public WaylanderJonat(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class WaylanderSarvaj : WaylanderRosterMobile
    {
        [Constructable]
        public WaylanderSarvaj() : base("sarvaj") { }
        public WaylanderSarvaj(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class WaylanderKaem : WaylanderRosterMobile
    {
        [Constructable]
        public WaylanderKaem() : base("kaem") { }
        public WaylanderKaem(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class WaylanderOrien : WaylanderRosterMobile
    {
        [Constructable]
        public WaylanderOrien() : base("orien") { }
        public WaylanderOrien(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class WaylanderHewla : WaylanderRosterMobile
    {
        [Constructable]
        public WaylanderHewla() : base("hewla") { }
        public WaylanderHewla(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class WaylanderKai : WaylanderRosterMobile
    {
        [Constructable]
        public WaylanderKai() : base("kai") { }
        public WaylanderKai(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class WaylanderKrylla : WaylanderRosterMobile
    {
        [Constructable]
        public WaylanderKrylla() : base("krylla") { }
        public WaylanderKrylla(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class WaylanderMiriel : WaylanderRosterMobile
    {
        [Constructable]
        public WaylanderMiriel() : base("miriel") { }
        public WaylanderMiriel(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class WaylanderKesaKhan : WaylanderRosterMobile
    {
        [Constructable]
        public WaylanderKesaKhan() : base("kesa_khan") { }
        public WaylanderKesaKhan(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class WaylanderJoining : WaylanderRosterMobile
    {
        [Constructable]
        public WaylanderJoining() : base("joining") { }
        public WaylanderJoining(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }
}
