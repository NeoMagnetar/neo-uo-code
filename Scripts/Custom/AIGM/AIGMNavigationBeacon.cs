using System;

using Server;

namespace Server.Custom.AIGM
{
    public sealed class AIGMNavigationBeacon : Mobile
    {
        [Constructable]
        public AIGMNavigationBeacon()
        {
            Name = "AIGM navigation beacon";
            Body = 0x190;
            Blessed = true;
            Hidden = true;
            CantWalk = true;
        }

        public AIGMNavigationBeacon(Serial serial)
            : base(serial)
        {
        }

        public override bool CanBeDamaged()
        {
            return false;
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();

            Timer.DelayCall(TimeSpan.Zero, Delete);
        }
    }
}
