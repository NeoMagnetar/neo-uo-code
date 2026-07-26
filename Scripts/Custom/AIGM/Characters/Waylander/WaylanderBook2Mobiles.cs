using Server.Mobiles;

namespace Server.Custom.AIGM.Characters.Waylander
{
    public class WaylanderAngel : WaylanderRosterMobile
    {
        [Constructable]
        public WaylanderAngel() : base("angel") { }
        public WaylanderAngel(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class WaylanderSenta : WaylanderRosterMobile
    {
        [Constructable]
        public WaylanderSenta() : base("senta") { }
        public WaylanderSenta(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class WaylanderBelash : WaylanderRosterMobile
    {
        [Constructable]
        public WaylanderBelash() : base("belash") { }
        public WaylanderBelash(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class WaylanderMorak : WaylanderRosterMobile
    {
        [Constructable]
        public WaylanderMorak() : base("morak") { }
        public WaylanderMorak(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class WaylanderZhuChao : WaylanderRosterMobile
    {
        [Constructable]
        public WaylanderZhuChao() : base("zhu_chao") { }
        public WaylanderZhuChao(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class WaylanderBodalen : WaylanderRosterMobile
    {
        [Constructable]
        public WaylanderBodalen() : base("bodalen") { }
        public WaylanderBodalen(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class WaylanderAnsiChen : WaylanderRosterMobile
    {
        [Constructable]
        public WaylanderAnsiChen() : base("ansi_chen") { }
        public WaylanderAnsiChen(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class WaylanderInnicas : WaylanderRosterMobile
    {
        [Constructable]
        public WaylanderInnicas() : base("innicas") { }
        public WaylanderInnicas(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class WaylanderRegnak : WaylanderRosterMobile
    {
        [Constructable]
        public WaylanderRegnak() : base("regnak") { }
        public WaylanderRegnak(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class WaylanderDarkBrotherhoodKnight : WaylanderRosterMobile
    {
        [Constructable]
        public WaylanderDarkBrotherhoodKnight() : base("dark_brotherhood_knight") { }
        public WaylanderDarkBrotherhoodKnight(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }

    public class WaylanderScar : WaylanderRosterMount
    {
        [Constructable]
        public WaylanderScar() : base("scar") { }
        public WaylanderScar(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }
}
