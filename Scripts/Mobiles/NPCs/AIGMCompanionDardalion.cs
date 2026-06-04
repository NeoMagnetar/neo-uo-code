using System;
using Server.Custom.AIGM;
using Server.Items;

namespace Server.Mobiles
{
    public class AIGMCompanionDardalion : BaseHire
    {
        public const string RuntimeStamp = "DARDALION_RUNTIME_BINDING_PROOF_20260601_V1";

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
        public string DardalionRuntimeStamp
        {
            get { return RuntimeStamp; }
        }

        public override bool UsesHirelingPayroll
        {
            get { return false; }
        }

        [Constructable]
        public AIGMCompanionDardalion()
            : base(AIType.AI_Melee)
        {
            LogRuntimeStamp("CTOR");

            Name = "Dardalion";
            Title = "the warrior-priest companion";
            Female = false;
            Body = 0x190;
            Hue = Utility.RandomSkinHue();
            SpeechHue = Utility.RandomDyedHue();
            HairItemID = Race.RandomHair(Female);
            HairHue = Race.RandomHairHue();
            Race.RandomFacialHair(this);

            SetStr(85, 95);
            SetDex(60, 70);
            SetInt(55, 65);

            SetHits(120, 135);
            SetDamage(8, 13);

            SetSkill(SkillName.Swords, 50.0, 60.0);
            SetSkill(SkillName.Healing, 50.0, 60.0);
            SetSkill(SkillName.Tactics, 45.0, 55.0);
            SetSkill(SkillName.Anatomy, 40.0, 50.0);
            SetSkill(SkillName.MagicResist, 40.0, 50.0);
            SetSkill(SkillName.Parry, 35.0, 45.0);

            Fame = 500;
            Karma = 1000;

            VirtualArmor = 24;
            ControlSlots = 2;
            Tamable = false;

            AddItem(new Boots(Utility.RandomNeutralHue()));
            AddItem(new Cloak(Utility.RandomBlueHue()));
            AddItem(new Robe(Utility.RandomBlueHue()));
            AddItem(new PlateChest());
            AddItem(new PlateArms());
            AddItem(new PlateGloves());
            AddItem(new PlateGorget());
            AddItem(new PlateLegs());
            AddItem(new Broadsword());
            PackItem(new Bandage(30));
            PackGold(25, 75);
            _executionMode = AIGMExecutionMode.TrustedCompanionDirect;
        }

        public AIGMCompanionDardalion(Serial serial)
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
                LogSpeech("DARDALION_SPEECH_NULL");
                base.OnSpeech(e);
                return;
            }

            string speech = e.Speech == null ? String.Empty : e.Speech.Trim();
            LogSpeech("DARDALION_SPEECH_START from=" + SafeName(e.Mobile) + " controlled=" + Controlled + " range=" + e.Mobile.GetDistanceToSqrt(this) + " text=" + speech);

            Mobile owner = GetOwner();
            bool allowSharedOwnerSpeech = e.Mobile == owner;

            if ((!e.Handled || allowSharedOwnerSpeech) && e.Mobile.InRange(this, 8))
            {
                string normalizedSpeech = speech.ToLowerInvariant();
                bool trustedCompanion = IsTrustedCompanionSpeaker(e.Mobile, owner);
                bool trusted = e.Mobile == owner || trustedCompanion || (!Controlled && e.Mobile.AccessLevel >= AccessLevel.GameMaster);
                LogSpeech("DARDALION_TRUST_RESULT trusted=" + trusted + " trustedCompanion=" + trustedCompanion + " owner=" + SafeName(owner));

                if (trusted)
                {
                    bool clearlyAddressedToDifferentCompanion = AIGMCompanionIntentParser.IsClearlyAddressedToDifferentCompanion(this, e.Speech);
                    if (clearlyAddressedToDifferentCompanion)
                        LogSpeech("DARDALION_AWARE_OTHER_ADDRESSED_EARLY rawSpeech=" + (e.Speech ?? String.Empty));

                    if (normalizedSpeech == "runtime stamp" || normalizedSpeech == "version")
                    {
                        SayTo(e.Mobile, RuntimeStamp);
                        e.Handled = true;
                        return;
                    }

                    AIGMCompanionIntent intent;
                    LogSpeech("DARDALION_PARSE_START rawSpeech=" + (e.Speech ?? String.Empty));
                    bool parsed = AIGMCompanionIntentParser.TryParse(this, e.Mobile, e.Speech, out intent);
                    LogSpeech("DARDALION_PARSE_RESULT parsed=" + parsed + " kind=" + (intent != null ? intent.Kind : "null") + " rawSpeech=" + (e.Speech ?? String.Empty));

                    if (parsed)
                    {
                        if (intent != null && intent.AddressedToDifferentCompanion)
                        {
                            LogSpeech("DARDALION_AWARE_OTHER_ADDRESSED rawSpeech=" + (e.Speech ?? String.Empty));
                        }
                        else
                        {
                            AIGMCompanionActionPolicyResult decision = AIGMCompanionDirectActionPolicy.Decide(this, e.Mobile, intent);
                            LogSpeech("DARDALION_POLICY_RESULT kind=" + (intent != null ? intent.Kind : "null") + " decision=" + (decision != null ? decision.Decision.ToString() : "null") + " reason=" + (decision != null ? decision.Reason ?? String.Empty : String.Empty));

                            if (decision != null && decision.Decision == AIGMCompanionActionDecision.DirectExecute)
                            {
                                string response;
                                LogSpeech("DARDALION_DIRECT_EXECUTE_START kind=" + (intent != null ? intent.Kind : "null"));
                                bool executed = AIGMCompanionActionExecutor.TryExecuteIntent(this, e.Mobile, intent, out response);
                                LogSpeech("DARDALION_DIRECT_EXECUTE_RESULT kind=" + (intent != null ? intent.Kind : "null") + " executed=" + executed + " response=" + (response ?? String.Empty));

                                if (e.Mobile == owner && (intent == null || !intent.ExplicitlyAddressed))
                                    AIGMCompanionSpeechBus.PublishOwnerSpeech(this, e.Mobile, e.Speech);

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
                            else if (IsSupportIntent(intent) || IsStrictDirectCommand(intent))
                            {
                                LogSpeech("DARDALION_DIRECT_EXECUTE_RESULT kind=" + (intent != null ? intent.Kind : "null") + " executed=False response=Recognized direct command blocked from async fallback.");
                                e.Handled = true;
                            }
                        }
                    }

                    if (!e.Handled)
                    {
                        string rejection;
                        bool enqueued = AIGMCompanionSpeechQueue.TryEnqueue(this, e.Mobile, e.Speech, out rejection);
                        LogSpeech("DARDALION_QUEUE_FALLBACK enqueued=" + enqueued + " rejection=" + (rejection ?? String.Empty));
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

        private static bool IsSupportIntent(AIGMCompanionIntent intent)
        {
            if (intent == null || String.IsNullOrWhiteSpace(intent.Kind))
                return false;

            switch (intent.Kind)
            {
                case AIGMCompanionIntentKind.BandageSelf:
                case AIGMCompanionIntentKind.BandageOwner:
                case AIGMCompanionIntentKind.HealSelf:
                case AIGMCompanionIntentKind.HealOwner:
                case AIGMCompanionIntentKind.CureSelf:
                case AIGMCompanionIntentKind.CureOwner:
                case AIGMCompanionIntentKind.UseHealingSkill:
                case AIGMCompanionIntentKind.UseBandages:
                case AIGMCompanionIntentKind.CastHeal:
                case AIGMCompanionIntentKind.CastCure:
                    return true;
                default:
                    return false;
            }
        }

        private bool IsStrictDirectCommand(AIGMCompanionIntent intent)
        {
            if (intent == null)
                return false;

            string kind = intent.Kind;
            return kind == AIGMCompanionIntentKind.FollowOwner
                || kind == AIGMCompanionIntentKind.Stay
                || kind == AIGMCompanionIntentKind.Come
                || kind == AIGMCompanionIntentKind.TravelToDestination
                || kind == AIGMCompanionIntentKind.StopTravel
                || kind == AIGMCompanionIntentKind.ReportTravelStatus
                || kind == AIGMCompanionIntentKind.FollowCompanion
                || kind == AIGMCompanionIntentKind.GreetCompanion
                || kind == AIGMCompanionIntentKind.StartTrackingCycle
                || kind == AIGMCompanionIntentKind.StopTrackingCycle;
        }

        private void LogSpeech(string message)
        {
            try
            {
                string path = System.IO.Path.Combine(Core.BaseDirectory, "Logs", "AIGMCompanionDardalion.log");
                System.IO.File.AppendAllText(path, DateTime.UtcNow.ToString("o") + " " + (message ?? String.Empty) + Environment.NewLine);
            }
            catch
            {
            }
        }

        private void LogRuntimeStamp(string phase)
        {
            LogSpeech(
                "DARDALION_RUNTIME_STAMP"
                + " phase=" + (phase ?? String.Empty)
                + " stamp=" + RuntimeStamp
                + " type=" + GetType().FullName
                + " baseType=" + (GetType().BaseType != null ? GetType().BaseType.FullName : String.Empty)
                + " assembly=" + GetType().Assembly.FullName
                + " serial=0x" + Serial.Value.ToString("X8")
                + " utc=" + DateTime.UtcNow.ToString("o"));
        }

        private bool IsTrustedCompanionSpeaker(Mobile speaker, Mobile owner)
        {
            if (speaker == null || owner == null)
                return false;

            BaseHire ally = speaker as BaseHire;
            if (ally == null || ally == this || ally.Deleted)
                return false;

            return ally.GetOwner() == owner;
        }

        private string SafeName(Mobile mob)
        {
            if (mob == null)
                return "(null)";

            return (mob.Name ?? mob.GetType().Name) + "[0x" + mob.Serial.Value.ToString("X8") + "]";
        }

        public void ReceiveSpeechBusEvent(BaseHire sourceCompanion, Mobile eventSpeaker, string speech, bool companionOrigin)
        {
            if (sourceCompanion == null || sourceCompanion.Deleted || String.IsNullOrWhiteSpace(speech))
                return;

            LogSpeech("DARDALION_SPEECH_BUS source=" + SafeName(sourceCompanion) + " speaker=" + SafeName(eventSpeaker) + " companionOrigin=" + companionOrigin + " text=" + speech);

            Mobile effectiveSpeaker = eventSpeaker;
            if (effectiveSpeaker == null)
                effectiveSpeaker = sourceCompanion;

            bool ownerRelayAwarenessOnly = !companionOrigin && eventSpeaker == GetOwner();

            if (companionOrigin)
            {
                string rejection;
                bool enqueuedCompanionDialogue = AIGMCompanionSpeechQueue.TryEnqueue(this, effectiveSpeaker, speech, "companion_dialogue", out rejection);
                LogSpeech("DARDALION_SPEECH_BUS_DIALOGUE enqueued=" + enqueuedCompanionDialogue + " rejection=" + (rejection ?? String.Empty));
                return;
            }

            if (!ownerRelayAwarenessOnly)
            {
                AIGMCompanionIntent intent;
                bool parsed = AIGMCompanionIntentParser.TryParse(this, effectiveSpeaker, speech, out intent);
                if (parsed && intent != null)
                    intent.AllowRemoteRelay = true;

                if (parsed)
                {
                    if (intent != null && intent.AddressedToDifferentCompanion)
                    {
                        LogSpeech("DARDALION_SPEECH_BUS_IGNORE_OTHER_ADDRESSED rawSpeech=" + (speech ?? String.Empty));
                    }
                    else
                    {
                        AIGMCompanionActionPolicyResult decision = AIGMCompanionDirectActionPolicy.Decide(this, effectiveSpeaker, intent);
                        LogSpeech("DARDALION_SPEECH_BUS_POLICY kind=" + (intent != null ? intent.Kind : "null") + " decision=" + (decision != null ? decision.Decision.ToString() : "null") + " reason=" + (decision != null ? decision.Reason ?? String.Empty : String.Empty));
                        if (decision != null && decision.Decision == AIGMCompanionActionDecision.DirectExecute)
                        {
                            string response;
                            bool executed = AIGMCompanionActionExecutor.TryExecuteIntent(this, effectiveSpeaker, intent, out response);
                            LogSpeech("DARDALION_SPEECH_BUS_EXECUTE kind=" + (intent != null ? intent.Kind : "null") + " executed=" + executed + " response=" + (response ?? String.Empty));
                            if (!String.IsNullOrWhiteSpace(response))
                                Say(response);
                            return;
                        }
                    }
                }
            }
            else
            {
                LogSpeech("DARDALION_SPEECH_BUS_AWARENESS_ONLY rawSpeech=" + (speech ?? String.Empty));
            }

            string rejectionFallback;
            bool enqueued = AIGMCompanionSpeechQueue.TryEnqueue(this, effectiveSpeaker, speech, ownerRelayAwarenessOnly ? "owner_relay_awareness" : "owner_relay_dialogue", out rejectionFallback);
            LogSpeech("DARDALION_SPEECH_BUS_QUEUE enqueued=" + enqueued + " rejection=" + (rejectionFallback ?? String.Empty));
        }

        public void ReceiveCompanionDialogue(BaseHire sourceCompanion, AIGMCompanionDialogueEvent dialogueEvent)
        {
            if (sourceCompanion == null || sourceCompanion.Deleted || dialogueEvent == null || String.IsNullOrWhiteSpace(dialogueEvent.Text))
                return;

            LogSpeech("DARDALION_DIALOGUE source=" + SafeName(sourceCompanion) + " eventId=" + dialogueEvent.EventId + " hop=" + dialogueEvent.HopCount + " text=" + dialogueEvent.Text);

            string rejection;
            bool enqueued = AIGMCompanionSpeechQueue.TryEnqueue(this, sourceCompanion, dialogueEvent.Text, "companion_dialogue", out rejection);
            LogSpeech("DARDALION_DIALOGUE_QUEUE enqueued=" + enqueued + " rejection=" + (rejection ?? String.Empty));
        }

        public override void OnThink()
        {
            base.OnThink();
            AIGMCompanionActionExecutor.TryReactiveSupport(this);
            AIGMCompanionTrackingCycle.Pulse(this);
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
