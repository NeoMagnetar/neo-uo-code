using System;
using Server.Custom.AIGM;
using Server.Items;

namespace Server.Mobiles
{
    public class AIGMCompanionDanyal : BaseHire, IAIGMCompanionActor
    {
        private bool _guardOwnerMode;
        private DateTime _nextSupportActionUtc;

        public Mobile Shell
        {
            get { return this; }
        }

        public string ActorId
        {
            get { return CompanionId; }
        }

        public string DisplayName
        {
            get { return CompanionDisplayName; }
        }

        public IAIGMInventoryCapability Inventory
        {
            get { return null; }
        }

        public string CompanionId
        {
            get { return "danyal"; }
        }

        public string CompanionDisplayName
        {
            get { return "Danyal"; }
        }

        public string CompanionRole
        {
            get { return "wayfarer-companion"; }
        }

        public string CompanionProfileKey
        {
            get { return "danyal"; }
        }

        public bool IsAIGMCompanion
        {
            get { return true; }
        }

        public bool CanUseAIGMSkills
        {
            get { return false; }
        }

        public bool GuardOwnerMode
        {
            get { return _guardOwnerMode; }
            set { _guardOwnerMode = value; }
        }

        public DateTime NextSupportActionUtc
        {
            get { return _nextSupportActionUtc; }
            set { _nextSupportActionUtc = value; }
        }

        public string ExecutionModeKey
        {
            get { return "shell-disabled"; }
        }

        [Constructable]
        public AIGMCompanionDanyal()
            : base(AIType.AI_Archer)
        {
            Name = "Danyal";
            Title = "the wayfarer companion";
            Female = true;
            Body = 0x191;
            Hue = Utility.RandomSkinHue();
            SpeechHue = Utility.RandomDyedHue();
            HairItemID = Race.RandomHair(Female);
            HairHue = Race.RandomHairHue();

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
            AddItem(new FancyShirt(Utility.RandomBlueHue()));
            AddItem(new LeatherChest());
            AddItem(new LeatherArms());
            AddItem(new LeatherGloves());
            AddItem(new LeatherGorget());
            AddItem(new LeatherLegs());
            AddItem(new Bow());
            PackItem(new Arrow(50));
            PackItem(new Bandage(25));
            PackGold(25, 75);
        }

        public AIGMCompanionDanyal(Serial serial)
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
                SayTo(m, "I am with you. My active command lane is not enabled yet.");
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
            base.OnSpeech(e);

            if (e == null || e.Handled || e.Mobile == null || !e.Mobile.Alive || !e.Mobile.InRange(this, 8))
                return;

            AIGMCompanionCommandRouteDecision decision = AIGMCompanionCommandBoundary.Classify(e.Speech);
            if (decision == null || decision.RouteKind == AIGMCompanionCommandRouteKind.EmptySpeech || decision.RouteKind == AIGMCompanionCommandRouteKind.NonCompanion || decision.RouteKind == AIGMCompanionCommandRouteKind.UnknownCompanionAlias)
                return;

            if (decision.RouteKind == AIGMCompanionCommandRouteKind.NamedCompanion)
            {
                if (!String.Equals(decision.CompanionKey, CompanionId, StringComparison.OrdinalIgnoreCase))
                    return;
            }
            else if (decision.RouteKind == AIGMCompanionCommandRouteKind.SharedCompanion)
            {
                if (GetOwner() != e.Mobile)
                    return;

                double selfDistance = e.Mobile.GetDistanceToSqrt(this);
                IPooledEnumerable mobiles = e.Mobile.Map != null ? e.Mobile.Map.GetMobilesInRange(e.Mobile.Location, 8) : null;
                if (mobiles != null)
                {
                    foreach (Mobile mobile in mobiles)
                    {
                        BaseHire other = mobile as BaseHire;
                        IAIGMCompanionActor actor = other as IAIGMCompanionActor;
                        if (other == null || actor == null || other == this || other.Deleted || other.GetOwner() != e.Mobile)
                            continue;

                        double otherDistance = e.Mobile.GetDistanceToSqrt(other);
                        if (otherDistance < selfDistance || (Math.Abs(otherDistance - selfDistance) < 0.01 && other.Serial.Value < Serial.Value))
                        {
                            mobiles.Free();
                            return;
                        }
                    }

                    mobiles.Free();
                }
            }
            else
            {
                return;
            }

            string verb = String.IsNullOrWhiteSpace(decision.CommandVerb) ? "command" : decision.CommandVerb;
            string text = decision.RouteKind == AIGMCompanionCommandRouteKind.NamedCompanion
                ? String.Format("{0} recognizes {1}; movement deferred.", CompanionDisplayName, verb)
                : String.Format("Shared companion command recognized: {0}. Movement deferred.", verb);

            SayTo(e.Mobile, text);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1);
            writer.Write(_guardOwnerMode);
            writer.Write(_nextSupportActionUtc);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            if (version >= 1)
            {
                _guardOwnerMode = reader.ReadBool();
                _nextSupportActionUtc = reader.ReadDateTime();
            }
        }
    }
}
