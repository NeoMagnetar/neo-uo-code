using Server.Mobiles;

namespace Server.Custom.AIGM.Characters.Waylander
{
    public class WaylanderGreyMan : WaylanderRosterMobile
    {
        [Constructable]
        public WaylanderGreyMan() : base("dakeyras.grey_man") { }
        public WaylanderGreyMan(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class WaylanderKysumu : WaylanderRosterMobile
    {
        [Constructable]
        public WaylanderKysumu() : base("kysumu") { }
        public WaylanderKysumu(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class WaylanderYuYuLiang : WaylanderRosterMobile
    {
        [Constructable]
        public WaylanderYuYuLiang() : base("yu_yu_liang") { }
        public WaylanderYuYuLiang(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class WaylanderUstarte : WaylanderRosterMobile
    {
        [Constructable]
        public WaylanderUstarte() : base("ustarte") { }
        public WaylanderUstarte(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class WaylanderKeevaTaliana : WaylanderRosterMobile
    {
        [Constructable]
        public WaylanderKeevaTaliana() : base("keeva_taliana") { }
        public WaylanderKeevaTaliana(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class WaylanderMatzeChai : WaylanderRosterMobile
    {
        [Constructable]
        public WaylanderMatzeChai() : base("matze_chai") { }
        public WaylanderMatzeChai(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class WaylanderAric : WaylanderRosterMobile
    {
        [Constructable]
        public WaylanderAric() : base("aric") { }
        public WaylanderAric(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class WaylanderDukeOfKydor : WaylanderRosterMobile
    {
        [Constructable]
        public WaylanderDukeOfKydor() : base("duke_of_kydor") { }
        public WaylanderDukeOfKydor(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class WaylanderKuanHadorDemonLord : WaylanderRosterMobile
    {
        [Constructable]
        public WaylanderKuanHadorDemonLord() : base("kuan_hador_demon_lord") { }
        public WaylanderKuanHadorDemonLord(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }
}
