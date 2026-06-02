using System;
using Server.Custom.AIGM;
using Server.Items;

namespace Server.Mobiles
{
    public class AIGMCompanionDanyal : BaseHire
    {
        public const string RuntimeStamp = "DANYAL_RUNTIME_BINDING_PROOF_20260531_V1";

        private bool _guardOwnerMode;
        private DateTime _nextSupportActionUtc;
        private AIGMExecutionMode _executionMode;

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

        public AIGMExecutionMode ExecutionMode
        {
            get { return _executionMode; }
            set { _executionMode = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string DanyalRuntimeStamp
        {
            get { return RuntimeStamp; }
        }

        [Constructable]
        public AIGMCompanionDanyal()
            : base(AIType.AI_Archer)
        {
            LogRuntimeStamp("CTOR");

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
            _executionMode = AIGMExecutionMode.TrustedCompanionDirect;
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
            LogRuntimeStamp("ONSPEECH");

            if (e == null || e.Mobile == null)
            {
                LogSpeech("DANYAL_SPEECH_NULL");
                base.OnSpeech(e);
                return;
            }

            string speech = e.Speech == null ? String.Empty : e.Speech.Trim();
            LogSpeech("DANYAL_SPEECH_START from=" + SafeName(e.Mobile) + " controlled=" + Controlled + " range=" + e.Mobile.GetDistanceToSqrt(this) + " text=" + speech);

            if (!e.Handled && e.Mobile.InRange(this, 8))
            {
                string normalizedSpeech = speech.ToLowerInvariant();
                Mobile owner = GetOwner();
                bool trusted = e.Mobile == owner || (!Controlled && e.Mobile.AccessLevel >= AccessLevel.GameMaster);
                LogSpeech("DANYAL_TRUST_RESULT trusted=" + trusted + " owner=" + SafeName(owner));

                if (trusted)
                {
                    if (normalizedSpeech == "runtime stamp" || normalizedSpeech == "version")
                    {
                        SayTo(e.Mobile, RuntimeStamp);
                        e.Handled = true;
                        return;
                    }

                    AIGMCompanionIntent intent;
                    LogSpeech("DANYAL_PARSE_START rawSpeech=" + (e.Speech ?? String.Empty));
                    bool parsed = AIGMCompanionIntentParser.TryParse(this, e.Mobile, e.Speech, out intent);
                    LogSpeech("DANYAL_PARSE_RESULT parsed=" + parsed + " kind=" + (intent != null ? intent.Kind : "null") + " rawSpeech=" + (e.Speech ?? String.Empty));

                    if (parsed)
                    {
                        AIGMCompanionActionPolicyResult decision = AIGMCompanionDirectActionPolicy.Decide(this, e.Mobile, intent);
                        LogSpeech("DANYAL_POLICY_RESULT kind=" + (intent != null ? intent.Kind : "null") + " decision=" + (decision != null ? decision.Decision.ToString() : "null") + " reason=" + (decision != null ? decision.Reason ?? String.Empty : String.Empty));

                        if (decision != null && decision.Decision == AIGMCompanionActionDecision.DirectExecute)
                        {
                            string response;
                            LogSpeech("DANYAL_DIRECT_EXECUTE_START kind=" + (intent != null ? intent.Kind : "null"));
                            bool executed = AIGMCompanionActionExecutor.TryExecuteIntent(this, e.Mobile, intent, out response);
                            LogSpeech("DANYAL_DIRECT_EXECUTE_RESULT kind=" + (intent != null ? intent.Kind : "null") + " executed=" + executed + " response=" + (response ?? String.Empty));

                            if (!String.IsNullOrWhiteSpace(response))
                                SayTo(e.Mobile, response);

                            e.Handled = true;
                        }
                        else if (decision != null && decision.Decision == AIGMCompanionActionDecision.Reject)
                        {
                            if (!String.IsNullOrWhiteSpace(decision.Reason))
                                SayTo(e.Mobile, decision.Reason);

                            e.Handled = true;
                        }
                    }

                    if (!e.Handled)
                    {
                        string rejection;
                        bool enqueued = AIGMCompanionSpeechQueue.TryEnqueue(this, e.Mobile, e.Speech, out rejection);
                        LogSpeech("DANYAL_QUEUE_FALLBACK enqueued=" + enqueued + " rejection=" + (rejection ?? String.Empty));
                        if (enqueued)
                        {
                            e.Handled = true;
                        }
                        else if (!String.IsNullOrWhiteSpace(rejection))
                        {
                            Say(rejection);
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
                string path = System.IO.Path.Combine(Core.BaseDirectory, "Logs", "AIGMCompanionDanyal.log");
                System.IO.File.AppendAllText(path, DateTime.UtcNow.ToString("o") + " " + (message ?? String.Empty) + Environment.NewLine);
            }
            catch
            {
            }
        }

        private void LogRuntimeStamp(string phase)
        {
            LogSpeech(
                "DANYAL_RUNTIME_STAMP"
                + " phase=" + (phase ?? String.Empty)
                + " stamp=" + RuntimeStamp
                + " type=" + GetType().FullName
                + " baseType=" + (GetType().BaseType != null ? GetType().BaseType.FullName : String.Empty)
                + " assembly=" + GetType().Assembly.FullName
                + " serial=0x" + Serial.Value.ToString("X8")
                + " utc=" + DateTime.UtcNow.ToString("o"));
        }

        private string SafeName(Mobile mob)
        {
            if (mob == null)
                return "(null)";

            return (mob.Name ?? mob.GetType().Name) + "[0x" + mob.Serial.Value.ToString("X8") + "]";
        }

        public override void OnThink()
        {
            base.OnThink();
            AIGMCompanionActionExecutor.TryReactiveSupport(this);
            AIGMCompanionTrackingController.PulseTracking(this);
            AIGMCompanionTravelController.PulseTravel(this);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1);
            writer.Write(_guardOwnerMode);
            writer.Write(_nextSupportActionUtc);
            writer.Write((int)_executionMode);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            LogRuntimeStamp("DESERIALIZE");
            int version = reader.ReadInt();

            if (version >= 1)
            {
                _guardOwnerMode = reader.ReadBool();
                _nextSupportActionUtc = reader.ReadDateTime();
                _executionMode = (AIGMExecutionMode)reader.ReadInt();
            }
            else
            {
                _executionMode = AIGMExecutionMode.TrustedCompanionDirect;
            }
        }
    }
}
