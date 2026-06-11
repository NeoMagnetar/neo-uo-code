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

        public override bool UsesHirelingPayroll
        {
            get { return false; }
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
            ControlSlots = 0;
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
            if (e == null || e.Handled || e.Mobile == null || !e.Mobile.Alive || !e.Mobile.InRange(this, 8))
            {
                base.OnSpeech(e);
                return;
            }

            Mobile owner = GetOwner();
            bool trustedSpeaker = e.Mobile == owner || (!Controlled && e.Mobile.AccessLevel >= AccessLevel.GameMaster);

            AIGMCompanionCommandRouteDecision decision = AIGMCompanionCommandBoundary.Classify(e.Speech);
            if (decision == null || decision.RouteKind == AIGMCompanionCommandRouteKind.EmptySpeech)
            {
                base.OnSpeech(e);
                return;
            }

            bool shouldSpeak = false;
            bool allowTrustedOwnerFallback = false;

            if (decision.RouteKind == AIGMCompanionCommandRouteKind.NamedCompanion)
            {
                if (!String.Equals(decision.CompanionKey, CompanionId, StringComparison.OrdinalIgnoreCase))
                    return;

                shouldSpeak = true;
            }
            else if (decision.RouteKind == AIGMCompanionCommandRouteKind.SharedCompanion)
            {
                if (owner != e.Mobile)
                    return;

                shouldSpeak = IsSharedCommandSpokesperson(e.Mobile);
            }
            else if (decision.RouteKind == AIGMCompanionCommandRouteKind.NonCompanion && trustedSpeaker)
            {
                allowTrustedOwnerFallback = true;
                shouldSpeak = IsSharedCommandSpokesperson(e.Mobile);
            }
            else
            {
                base.OnSpeech(e);
                return;
            }

            AIGMCompanionIntent intent;
            bool parsedIntent = AIGMCompanionIntentParser.TryParse(this, e.Mobile, e.Speech, out intent);

            if (allowTrustedOwnerFallback || ShouldUseCompanionChat(e.Mobile, e.Speech, decision, intent, parsedIntent))
            {
                string rejection;
                if (AIGMCompanionSpeechQueue.TryEnqueue(this, e.Mobile, e.Speech, shouldSpeak, out rejection))
                {
                    e.Handled = true;
                    return;
                }

                if (!String.IsNullOrWhiteSpace(rejection) && shouldSpeak)
                    SayTo(e.Mobile, rejection);

                e.Handled = true;
                return;
            }

            if (!parsedIntent)
            {
                if (!TryExecuteCompanionCommand(e.Mobile, decision, null, shouldSpeak))
                    return;

                return;
            }

            if (!TryExecuteCompanionCommand(e.Mobile, decision, intent, shouldSpeak))
                return;
        }

        private bool TryExecuteCompanionCommand(Mobile speaker, AIGMCompanionCommandRouteDecision decision, AIGMCompanionIntent intent, bool shouldSpeak)
        {
            if (speaker == null || decision == null)
                return false;

            Mobile owner = GetOwner();
            if (owner == null)
            {
                if (!SetControlMaster(speaker))
                {
                    SayTo(speaker, "I could not bind to you.");
                    return false;
                }

                IsHired = true;
                owner = speaker;
            }
            else if (owner != speaker)
            {
                return false;
            }

            string text;
            string intentKind = intent != null ? intent.Kind : null;

            if (intentKind == AIGMCompanionIntentKind.FollowOwner || intentKind == AIGMCompanionIntentKind.Come)
            {
                ControlTarget = speaker;
                ControlOrder = OrderType.Follow;
                text = String.Format("{0} acknowledges {1} and follows.", CompanionDisplayName, decision.CommandVerb ?? "follow");
            }
            else if (intentKind == AIGMCompanionIntentKind.Stay)
            {
                ControlTarget = null;
                ControlOrder = OrderType.Stay;
                text = String.Format("{0} acknowledges {1} and holds position.", CompanionDisplayName, decision.CommandVerb ?? "stay");
            }
            else if (intentKind == AIGMCompanionIntentKind.GuardOwner)
            {
                ControlTarget = speaker;
                ControlOrder = OrderType.Guard;
                text = String.Format("{0} acknowledges {1} and guards you.", CompanionDisplayName, decision.CommandVerb ?? "guard");
            }
            else if (!String.IsNullOrWhiteSpace(intentKind))
            {
                text = String.Format("{0} recognizes that command, but that action lane is not enabled yet.", CompanionDisplayName);
            }
            else
            {
                switch (decision.VerbKind)
                {
                    case AIGMCompanionCommandVerbKind.Follow:
                    case AIGMCompanionCommandVerbKind.Come:
                        ControlTarget = speaker;
                        ControlOrder = OrderType.Follow;
                        text = String.Format("{0} acknowledges {1} and follows.", CompanionDisplayName, decision.CommandVerb);
                        break;
                    case AIGMCompanionCommandVerbKind.Stop:
                    case AIGMCompanionCommandVerbKind.Stay:
                    case AIGMCompanionCommandVerbKind.Hold:
                    case AIGMCompanionCommandVerbKind.Wait:
                        ControlTarget = null;
                        ControlOrder = OrderType.Stay;
                        text = String.Format("{0} acknowledges {1} and holds position.", CompanionDisplayName, decision.CommandVerb);
                        break;
                    case AIGMCompanionCommandVerbKind.Guard:
                        ControlTarget = speaker;
                        ControlOrder = OrderType.Guard;
                        text = String.Format("{0} acknowledges {1} and guards you.", CompanionDisplayName, decision.CommandVerb);
                        break;
                    default:
                        text = decision.RouteKind == AIGMCompanionCommandRouteKind.NamedCompanion
                            ? String.Format("{0} recognizes {1}; advanced action deferred.", CompanionDisplayName, decision.CommandVerb ?? "command")
                            : String.Format("Shared companion command recognized: {0}. Advanced action deferred.", decision.CommandVerb ?? "command");
                        break;
                }
            }

            if (shouldSpeak)
                SayTo(speaker, text);

            return true;
        }

        private bool ShouldUseCompanionChat(Mobile speaker, string speech, AIGMCompanionCommandRouteDecision decision, AIGMCompanionIntent intent, bool parsedIntent)
        {
            if (speaker == null || String.IsNullOrWhiteSpace(speech) || decision == null)
                return false;

            if (decision.RouteKind != AIGMCompanionCommandRouteKind.NamedCompanion && decision.RouteKind != AIGMCompanionCommandRouteKind.SharedCompanion)
                return false;

            if (parsedIntent)
            {
                string kind = intent != null ? intent.Kind : null;
                if (kind == AIGMCompanionIntentKind.FollowOwner || kind == AIGMCompanionIntentKind.Come || kind == AIGMCompanionIntentKind.Stay || kind == AIGMCompanionIntentKind.GuardOwner)
                    return false;

                if (!String.IsNullOrWhiteSpace(kind))
                    return false;
            }

            return true;
        }


        private bool IsSharedCommandSpokesperson(Mobile speaker)
        {
            if (speaker == null || speaker.Map == null)
                return false;

            double selfDistance = speaker.GetDistanceToSqrt(this);
            IPooledEnumerable mobiles = speaker.Map.GetMobilesInRange(speaker.Location, 8);
            foreach (Mobile mobile in mobiles)
            {
                BaseHire other = mobile as BaseHire;
                IAIGMCompanionActor actor = other as IAIGMCompanionActor;
                if (other == null || actor == null || other == this || other.Deleted || other.GetOwner() != speaker)
                    continue;

                double otherDistance = speaker.GetDistanceToSqrt(other);
                if (otherDistance < selfDistance || (Math.Abs(otherDistance - selfDistance) < 0.01 && other.Serial.Value < Serial.Value))
                {
                    mobiles.Free();
                    return false;
                }
            }

            mobiles.Free();
            return true;
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
