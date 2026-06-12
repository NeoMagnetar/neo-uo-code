using System;
using System.Collections.Generic;
using Server.Custom.AIGM;
using Server.Items;

namespace Server.Mobiles
{
    public class AIGMCompanionDakeyras : BaseHire, IAIGMCompanionActor
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
            get { return "dakeyras"; }
        }

        public string CompanionDisplayName
        {
            get { return "Dakeyras"; }
        }

        public string CompanionRole
        {
            get { return "archer-companion"; }
        }

        public string CompanionProfileKey
        {
            get { return "dakeyras"; }
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
            ControlSlots = 0;
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
            if (e == null || e.Handled || e.Mobile == null || !e.Mobile.Alive || !e.Mobile.InRange(this, 8))
            {
                base.OnSpeech(e);
                return;
            }

            Mobile owner = GetOwner();
            bool trustedSpeaker = e.Mobile == owner || (!Controlled && e.Mobile.AccessLevel >= AccessLevel.GameMaster);

            AIGMCompanionCommandRouteDecision decision = AIGMCompanionCommandBoundary.Classify(e.Speech);
            List<string> addressedIds = AIGMCompanionCommandBoundary.GetAddressedCompanionIds(e.Speech);
            bool isMultiAddress = addressedIds != null && addressedIds.Count > 1;
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

                shouldSpeak = AIGMCompanionTurnCoordinator.ShouldCompanionTakeVisibleTurn(this, e.Mobile, e.Speech);
            }
            else if (isMultiAddress)
            {
                allowTrustedOwnerFallback = true;
                shouldSpeak = AIGMCompanionTurnCoordinator.ShouldCompanionTakeVisibleTurn(this, e.Mobile, e.Speech);
            }
            else if (decision.RouteKind == AIGMCompanionCommandRouteKind.SharedCompanion)
            {
                if (owner != e.Mobile)
                    return;

                shouldSpeak = AIGMCompanionTurnCoordinator.ShouldCompanionTakeVisibleTurn(this, e.Mobile, e.Speech);
            }
            else if (decision.RouteKind == AIGMCompanionCommandRouteKind.NonCompanion && trustedSpeaker)
            {
                allowTrustedOwnerFallback = true;
                shouldSpeak = AIGMCompanionTurnCoordinator.ShouldCompanionTakeVisibleTurn(this, e.Mobile, e.Speech);
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
                if (e.Mobile == owner && (allowTrustedOwnerFallback || decision.RouteKind == AIGMCompanionCommandRouteKind.SharedCompanion))
                    AIGMCompanionSpeechBus.PublishOwnerSpeech(this, e.Mobile, e.Speech);

                string rejection;
                if (AIGMCompanionSpeechQueue.TryEnqueue(this, e.Mobile, e.Speech, shouldSpeak, out rejection))
                {
                    e.Handled = true;
                    return;
                }

                string visibleRejection;
                if (shouldSpeak && AIGMCompanionSpeechQueue.TryGetVisibleRejection(rejection, out visibleRejection))
                    SayTo(e.Mobile, visibleRejection);

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

            if (TryHandleReadOnlyCapability(speaker, decision, intent, shouldSpeak))
                return true;

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
            else if (!String.IsNullOrWhiteSpace(intentKind) && IsExplicitDeferredActionIntent(intentKind))
            {
                text = String.Format("{0} recognizes that request, but that action lane is deferred in this phase.", CompanionDisplayName);
            }
            else if (!String.IsNullOrWhiteSpace(intentKind))
            {
                return false;
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

                if (IsReadOnlyIntentKind(kind))
                    return false;

                if (!String.IsNullOrWhiteSpace(kind))
                    return false;
            }

            return true;
        }

        private bool TryHandleReadOnlyCapability(Mobile speaker, AIGMCompanionCommandRouteDecision decision, AIGMCompanionIntent intent, bool shouldSpeak)
        {
            AIGMCompanionCapabilityRequest request = BuildCapabilityRequest(speaker, decision, intent);
            AIGMCompanionCapabilityDecision gateDecision = AIGMCompanionCapabilityGate.Decide(request);
            if (gateDecision == null || !gateDecision.Allowed)
                return false;

            string text = null;
            switch (gateDecision.Capability)
            {
                case AIGMCompanionCapabilityKind.ScanReadOnly:
                    text = AIGMCompanionReadOnlyAwareness.BuildScanAreaReport(this, speaker);
                    break;
                case AIGMCompanionCapabilityKind.ReportThreatsReadOnly:
                    text = AIGMCompanionReadOnlyAwareness.BuildThreatReport(this, speaker);
                    break;
                case AIGMCompanionCapabilityKind.ShareAwarenessReadOnly:
                    text = AIGMCompanionReadOnlyAwareness.BuildShareAwarenessReport(this, speaker);
                    break;
                case AIGMCompanionCapabilityKind.TrackReadOnly:
                    text = BuildTrackReadOnlyReport(intent, speaker);
                    break;
                case AIGMCompanionCapabilityKind.TrackingCycle:
                    text = BuildTrackingCycleReport(intent, speaker);
                    break;
                case AIGMCompanionCapabilityKind.ReportTrackingStatus:
                    text = AIGMCompanionReadOnlyAwareness.BuildTrackingStatusReport(this, speaker);
                    break;
                case AIGMCompanionCapabilityKind.TravelReadOnly:
                    text = AIGMCompanionReadOnlyAwareness.BuildTravelStatusReport(this, speaker);
                    break;
            }

            if (String.IsNullOrWhiteSpace(text))
                text = gateDecision.VisibleResponse;

            if (String.IsNullOrWhiteSpace(text))
                return false;

            if (shouldSpeak)
                SayTo(speaker, text);

            return true;
        }

        private AIGMCompanionCapabilityRequest BuildCapabilityRequest(Mobile speaker, AIGMCompanionCommandRouteDecision decision, AIGMCompanionIntent intent)
        {
            AIGMCompanionCapabilityRequest request = new AIGMCompanionCapabilityRequest();
            request.CompanionId = CompanionId;
            request.Speaker = speaker;
            request.RawSpeech = decision != null ? decision.OriginalSpeech : String.Empty;
            request.IntentKind = intent != null ? intent.Kind : String.Empty;
            request.Capability = decision != null ? decision.Capability : AIGMCompanionCapabilityKind.None;
            request.TargetText = intent != null ? intent.DestinationName : String.Empty;
            request.DestinationText = intent != null ? intent.DestinationName : String.Empty;
            request.IsExplicitlyAddressed = intent != null && intent.ExplicitlyAddressed;
            request.DialogueMode = "owner_or_world_speech";
            return request;
        }

        private string BuildTrackReadOnlyReport(AIGMCompanionIntent intent, Mobile speaker)
        {
            AIGMCompanionTrackingMode mode = AIGMCompanionTrackingService.GetModeFromIntentKind(intent != null ? intent.Kind : String.Empty);
            return AIGMCompanionTrackingService.BuildTrackingSweepReport(this, speaker, mode);
        }

        private string BuildTrackingCycleReport(AIGMCompanionIntent intent, Mobile speaker)
        {
            string kind = intent != null ? intent.Kind : String.Empty;
            if (kind == AIGMCompanionIntentKind.StopTrackingCycle)
                return AIGMCompanionTrackingService.StopTracking(this, speaker);

            AIGMCompanionTrackingMode mode = AIGMCompanionTrackingService.GetModeFromIntentKind(kind);
            return AIGMCompanionTrackingService.StartTracking(this, speaker, mode);
        }

        private bool IsReadOnlyIntentKind(string intentKind)
        {
            switch (intentKind)
            {
                case AIGMCompanionIntentKind.ScanArea:
                case AIGMCompanionIntentKind.ReportThreats:
                case AIGMCompanionIntentKind.ShareAwareness:
                case AIGMCompanionIntentKind.ReportTrackingStatus:
                case AIGMCompanionIntentKind.ReportTravelStatus:
                    return true;
                default:
                    return false;
            }
        }

        private bool IsExplicitDeferredActionIntent(string intentKind)
        {
            if (String.IsNullOrWhiteSpace(intentKind))
                return false;

            switch (intentKind)
            {
                case AIGMCompanionIntentKind.ScanArea:
                case AIGMCompanionIntentKind.ReportLocation:
                case AIGMCompanionIntentKind.ReportThreats:
                case AIGMCompanionIntentKind.ShareAwareness:
                case AIGMCompanionIntentKind.TrackAnimals:
                case AIGMCompanionIntentKind.TrackMonsters:
                case AIGMCompanionIntentKind.TrackHumanNPCs:
                case AIGMCompanionIntentKind.TrackPlayers:
                case AIGMCompanionIntentKind.StartTrackingCycle:
                case AIGMCompanionIntentKind.StopTrackingCycle:
                case AIGMCompanionIntentKind.ReportTrackingStatus:
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
                case AIGMCompanionIntentKind.AttackTarget:
                case AIGMCompanionIntentKind.StopCombat:
                case AIGMCompanionIntentKind.TravelToDestination:
                case AIGMCompanionIntentKind.StopTravel:
                case AIGMCompanionIntentKind.ReportTravelStatus:
                case AIGMCompanionIntentKind.ReturnHome:
                case AIGMCompanionIntentKind.FollowCompanion:
                case AIGMCompanionIntentKind.GreetCompanion:
                    return true;
                default:
                    return false;
            }
        }

        public void ReceiveSpeechBusEvent(BaseHire sourceCompanion, Mobile eventSpeaker, string speech, bool companionOrigin)
        {
            if (sourceCompanion == null || sourceCompanion.Deleted || String.IsNullOrWhiteSpace(speech))
                return;

            Mobile effectiveSpeaker = eventSpeaker ?? sourceCompanion;
            AIGMCompanionIntent relayIntent;
            bool parsed = AIGMCompanionIntentParser.TryParse(this, effectiveSpeaker, speech, out relayIntent);
            bool allowRemoteRelay = parsed && relayIntent != null;
            if (allowRemoteRelay)
                relayIntent.AllowRemoteRelay = true;

            string mode = companionOrigin ? "companion_dialogue" : "owner_relay_dialogue";
            AIGMCompanionSpeechRequest request = new AIGMCompanionSpeechRequest(this, effectiveSpeaker, speech, mode, null, sourceCompanion is IAIGMCompanionActor ? ((IAIGMCompanionActor)sourceCompanion).CompanionId : null, 0, allowRemoteRelay);
            string rejection;
            AIGMCompanionSpeechQueue.TryEnqueue(request, true, out rejection);
        }

        public void ReceiveCompanionDialogue(BaseHire sourceCompanion, AIGMCompanionDialogueEvent dialogueEvent)
        {
            if (sourceCompanion == null || sourceCompanion.Deleted || dialogueEvent == null || String.IsNullOrWhiteSpace(dialogueEvent.Text))
                return;

            AIGMCompanionSpeechRequest request = new AIGMCompanionSpeechRequest(this, sourceCompanion, dialogueEvent.Text, "companion_dialogue", dialogueEvent.EventId, sourceCompanion is IAIGMCompanionActor ? ((IAIGMCompanionActor)sourceCompanion).CompanionId : null, dialogueEvent.HopCount, true);
            string rejection;
            AIGMCompanionSpeechQueue.TryEnqueue(request, true, out rejection);
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
