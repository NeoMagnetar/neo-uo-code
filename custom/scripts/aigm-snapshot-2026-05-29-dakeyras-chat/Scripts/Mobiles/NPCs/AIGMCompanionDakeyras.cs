using System;
using Server.Custom.AIGM;
using Server.Items;

namespace Server.Mobiles
{
    public class AIGMCompanionDakeyras : BaseHire
    {
        [Constructable]
        public AIGMCompanionDakeyras()
            : base(AIType.AI_Archer)
        {
            Name = "Dakeyras";
            Title = "the companion";
            Female = false;
            Body = 0x190;
            Hue = Utility.RandomSkinHue();
            SpeechHue = Utility.RandomDyedHue();
            HairItemID = Race.RandomHair(Female);
            HairHue = Race.RandomHairHue();
            Race.RandomFacialHair(this);

            SetStr(75, 85);
            SetDex(70, 80);
            SetInt(45, 55);

            SetHits(110, 125);
            SetDamage(7, 12);

            SetSkill(SkillName.Archery, 45.0, 55.0);
            SetSkill(SkillName.Healing, 35.0, 45.0);
            SetSkill(SkillName.Fencing, 40.0, 50.0);
            SetSkill(SkillName.MagicResist, 35.0, 45.0);
            SetSkill(SkillName.Tactics, 40.0, 50.0);
            SetSkill(SkillName.Anatomy, 30.0, 40.0);

            Fame = 500;
            Karma = 500;

            VirtualArmor = 20;
            ControlSlots = 2;
            Tamable = false;

            AddItem(new Boots(Utility.RandomNeutralHue()));
            AddItem(new Shirt(Utility.RandomBlueHue()));
            AddItem(new StuddedChest());
            AddItem(new StuddedArms());
            AddItem(new StuddedGloves());
            AddItem(new StuddedGorget());
            AddItem(new StuddedLegs());
            AddItem(new Bow());
            PackItem(new Arrow(50));
            PackItem(new Bandage(25));
            PackGold(25, 75);
        }

        public AIGMCompanionDakeyras(Serial serial)
            : base(serial)
        {
        }

        public override bool ClickTitle
        {
            get { return false; }
        }

        public override bool AddHire(Mobile m)
        {
            if (m == null)
                return false;

            Mobile owner = GetOwner();
            if (owner != null)
            {
                SayTo(m, "I am already bound to {0}.", owner.Name);
                return false;
            }

            if (SetControlMaster(m))
            {
                IsHired = true;
                SayTo(m, "I am with you.");
                return true;
            }

            return false;
        }

        public override bool OnDragDrop(Mobile from, Item item)
        {
            if (!Controlled && from != null && item is Gold)
            {
                if (AddHire(from))
                {
                    if (item != null)
                        item.Delete();

                    return true;
                }
            }

            return base.OnDragDrop(from, item);
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            if (e == null || e.Mobile == null)
            {
                LogSpeech("OnSpeech early-null");
                base.OnSpeech(e);
                return;
            }

            LogSpeech("OnSpeech heard from=" + SafeName(e.Mobile) + " controlled=" + Controlled + " range=" + e.Mobile.GetDistanceToSqrt(this) + " text=" + (e.Speech ?? String.Empty));

            if (!e.Handled && e.Mobile.InRange(this, 8))
            {
                string speech = e.Speech == null ? String.Empty : e.Speech.Trim().ToLowerInvariant();
                Mobile owner = GetOwner();
                bool trusted = e.Mobile == owner || (!Controlled && e.Mobile.AccessLevel >= AccessLevel.GameMaster);
                LogSpeech("OnSpeech trusted=" + trusted + " owner=" + SafeName(owner));

                if (trusted)
                {
                    if (speech.Contains("follow me"))
                    {
                        ControlTarget = e.Mobile;
                        ControlOrder = OrderType.Follow;
                        SayTo(e.Mobile, "I am with you.");
                        LogSpeech("Handled local follow command.");
                        e.Handled = true;
                    }
                    else if (speech.Contains("stop") || speech.Contains("stay"))
                    {
                        ControlOrder = OrderType.Stay;
                        SayTo(e.Mobile, "I will hold here.");
                        LogSpeech("Handled local stop/stay command.");
                        e.Handled = true;
                    }
                    else if (speech.Contains("come here") || speech.Contains("come to me"))
                    {
                        ControlTarget = e.Mobile;
                        ControlOrder = OrderType.Come;
                        SayTo(e.Mobile, "On my way.");
                        LogSpeech("Handled local come-here command.");
                        e.Handled = true;
                    }
                    else
                    {
                        string spokenReply;
                        bool ok = AIGMCompanionChatAdapter.TryRespond(e.Mobile, this, e.Speech, out spokenReply);
                        LogSpeech("Bridge chat attempt ok=" + ok + " reply=" + (spokenReply ?? String.Empty));
                        if (ok)
                        {
                            Say(spokenReply);
                            e.Handled = true;
                        }
                    }
                }
            }

            if (!e.Handled)
                base.OnSpeech(e);
        }

        private void LogSpeech(string message)
        {
            try
            {
                string path = System.IO.Path.Combine(Core.BaseDirectory, "Logs", "AIGMCompanionDakeyras.log");
                System.IO.File.AppendAllText(path, DateTime.UtcNow.ToString("o") + " " + (message ?? String.Empty) + Environment.NewLine);
            }
            catch
            {
            }
        }

        private string SafeName(Mobile mob)
        {
            if (mob == null)
                return "(null)";

            return (mob.Name ?? mob.GetType().Name) + "[0x" + mob.Serial.Value.ToString("X8") + "]";
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
