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

            if (TryHandleExplicitGroupTrackingCommand(e))
                return;

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
                if (e.Mobile == owner && (allowTrustedOwnerFallback || decision.RouteKind == AIGMCompanionCommandRouteKind.SharedCompanion || decision.RouteKind == AIGMCompanionCommandRouteKind.NamedCompanion))
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

            if (e.Mobile == owner && decision.RouteKind == AIGMCompanionCommandRouteKind.NamedCompanion)
                AIGMCompanionSpeechBus.PublishOwnerSpeech(this, e.Mobile, e.Speech);

            if (!parsedIntent)
            {
                if (!TryExecuteCompanionCommand(e.Mobile, decision, null, shouldSpeak))
                    return;

                return;
            }

            if (!TryExecuteCompanionCommand(e.Mobile, decision, intent, shouldSpeak))
                return;
        }

        private bool TryHandleExplicitGroupTrackingCommand(SpeechEventArgs e)
        {
            if (e == null || e.Handled || e.Mobile == null)
                return false;

            if (!AIGMCompanionTrackingService.IsExplicitGroupTrackingCommand(e.Speech))
                return false;

            Mobile owner = GetOwner();
            if (owner != e.Mobile)
                return false;

            string speech = e.Speech ?? String.Empty;
            string normalized = speech.Trim().ToLowerInvariant();
            if (normalized == "all tracking status")
            {
                SayTo(e.Mobile, AIGMCompanionTrackingService.GetTrackingStatus(this, e.Mobile));
                e.Handled = true;
                return true;
            }

            AIGMCompanionTrackingMode mode = AIGMCompanionTrackingService.GetModeFromSpeech(speech);
            if (normalized.Contains("start tracking"))
                SayTo(e.Mobile, AIGMCompanionTrackingService.StartTracking(this, e.Mobile, mode));
            else
                SayTo(e.Mobile, AIGMCompanionTrackingService.BuildTrackingSweepReport(this, e.Mobile, mode));

            e.Handled = true;
            return true;
        }

        private bool TryExecuteCompanionCommand(Mobile speaker, AIGMCompanionCommandRouteDecision decision, AIGMCompanionIntent intent, bool shouldSpeak)
        {
            if (speaker == null || decision == null)
                return false;

            if (TryHandleReadOnlyCapability(speaker, decision, intent, shouldSpeak))
                return true;

            string healingResponse;
            if (AIGMCompanionHealingService.TryHandleExplicitHealingCommand(this, speaker, decision != null ? decision.OriginalSpeech : null, out healingResponse))
            {
                if (shouldSpeak && !String.IsNullOrWhiteSpace(healingResponse))
                    SayTo(speaker, healingResponse);
                return true;
            }

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
                text = "I am with you. I will read the road ahead.";
            }
            else if (intentKind == AIGMCompanionIntentKind.Stay)
            {
                ControlTarget = null;
                ControlOrder = OrderType.Stay;
                text = "I will hold here and keep the trail in sight.";
            }
            else if (intentKind == AIGMCompanionIntentKind.GuardOwner)
            {
                ControlTarget = speaker;
                ControlOrder = OrderType.Guard;
                text = "I will watch the edges around you.";
            }
            else if (intentKind == AIGMCompanionIntentKind.GreetCompanion)
            {
                return TryExecuteGreetCompanion(speaker, intent, shouldSpeak);
            }
            else if (IsTrackingIntentKind(intentKind))
            {
                text = BuildTrackingIntentReport(intent, speaker);
            }
            else if (!String.IsNullOrWhiteSpace(intentKind) && IsExplicitDeferredActionIntent(intentKind))
            {
                text = "Not that way, not yet. I can still read what is around us.";
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
                        text = "I am with you. I will read the road ahead.";
                        break;
                    case AIGMCompanionCommandVerbKind.Stop:
                    case AIGMCompanionCommandVerbKind.Stay:
                    case AIGMCompanionCommandVerbKind.Hold:
                    case AIGMCompanionCommandVerbKind.Wait:
                        ControlTarget = null;
                        ControlOrder = OrderType.Stay;
                        text = "I will hold here and keep the trail in sight.";
                        break;
                    case AIGMCompanionCommandVerbKind.Guard:
                        ControlTarget = speaker;
                        ControlOrder = OrderType.Guard;
                        text = "I will watch the edges around you.";
                        break;
                    default:
                        text = "Give me a clearer order, commander. The ground is easier to read than that.";
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
                if (kind == AIGMCompanionIntentKind.GreetCompanion)
                    return !IsGreetToLinkedCompanion(intent);

                if (kind == AIGMCompanionIntentKind.FollowOwner || kind == AIGMCompanionIntentKind.Come || kind == AIGMCompanionIntentKind.Stay || kind == AIGMCompanionIntentKind.GuardOwner)
                    return false;

                if (IsReadOnlyIntentKind(kind))
                    return false;

                if (!String.IsNullOrWhiteSpace(kind))
                    return false;
            }

            return true;
        }

        private bool TryExecuteGreetCompanion(Mobile speaker, AIGMCompanionIntent intent, bool shouldSpeak)
        {
            BaseHire target = FindLinkedCompanionByName(intent != null ? intent.DestinationName : null);
            if (target == null)
                return false;

            string greeting = BuildCompanionGreeting(target);
            Say(greeting);
            IAIGMCompanionActor targetActor = target as IAIGMCompanionActor;
            AIGMCompanionDialogueBus.PublishDialogue(this, greeting, targetActor != null ? targetActor.CompanionId : null);

            return true;
        }

        private string BuildCompanionGreeting(BaseHire target)
        {
            string targetName = target != null && !String.IsNullOrWhiteSpace(target.Name) ? target.Name : "friend";
            string normalized = targetName.ToLowerInvariant();

            if (normalized.Contains("danyal"))
                return "Danyal, keep your fire close. We may need its light before this is done.";

            if (normalized.Contains("dardalion"))
                return "Dardalion, hold the line. I will watch the dark places.";

            return targetName + ", stay sharp. I want the truth before the trail lies to us.";
        }

        private bool IsGreetToLinkedCompanion(AIGMCompanionIntent intent)
        {
            return FindLinkedCompanionByName(intent != null ? intent.DestinationName : null) != null;
        }

        private BaseHire FindLinkedCompanionByName(string companionName)
        {
            if (String.IsNullOrWhiteSpace(companionName) || Map == null)
                return null;

            Mobile owner = GetOwner();
            if (owner == null)
                return null;

            string normalized = companionName.Trim().ToLowerInvariant();
            foreach (Mobile mobile in World.Mobiles.Values)
            {
                BaseHire ally = mobile as BaseHire;
                if (ally == null || ally == this || ally.Deleted || ally.Map != Map || ally.GetOwner() != owner)
                    continue;

                IAIGMCompanionActor actor = ally as IAIGMCompanionActor;
                string allyName = ally.Name != null ? ally.Name.ToLowerInvariant() : String.Empty;
                string actorId = actor != null && actor.CompanionId != null ? actor.CompanionId.ToLowerInvariant() : String.Empty;
                if (allyName == normalized || actorId == normalized)
                    return ally;
            }

            return null;
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
                case AIGMCompanionCapabilityKind.MonsterHunt:
                    text = AIGMCompanionExecutionSpine.StartMonsterHunt(this, speaker);
                    break;
                case AIGMCompanionCapabilityKind.MonsterHuntStop:
                    text = AIGMCompanionExecutionSpine.StopMonsterHunt(this, "stop_command");
                    break;
                case AIGMCompanionCapabilityKind.MonsterHuntStatus:
                    text = AIGMCompanionExecutionSpine.GetStatus(this);
                    break;
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
                case AIGMCompanionCapabilityKind.TrackingAnimals:
                case AIGMCompanionCapabilityKind.TrackingMonsters:
                case AIGMCompanionCapabilityKind.TrackingNPCs:
                case AIGMCompanionCapabilityKind.TrackingHumanNPCs:
                case AIGMCompanionCapabilityKind.TrackingPlayers:
                case AIGMCompanionCapabilityKind.TrackingAll:
                    text = BuildCategoryTrackingReport(intent, speaker);
                    break;
                case AIGMCompanionCapabilityKind.TrackingCycle:
                    text = BuildTrackingCycleReport(intent, speaker);
                    break;
                case AIGMCompanionCapabilityKind.TrackingStop:
                    text = AIGMCompanionTrackingService.StopTracking(this, speaker);
                    break;
                case AIGMCompanionCapabilityKind.TrackingStatus:
                case AIGMCompanionCapabilityKind.ReportTrackingStatus:
                    text = AIGMCompanionTrackingService.GetTrackingStatus(this, speaker);
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
            request.Capability = ResolveCapability(decision, intent);
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

        private string BuildCategoryTrackingReport(AIGMCompanionIntent intent, Mobile speaker)
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

        private string BuildTrackingIntentReport(AIGMCompanionIntent intent, Mobile speaker)
        {
            string kind = intent != null ? intent.Kind : String.Empty;
            switch (kind)
            {
                case AIGMCompanionIntentKind.TrackAnimals:
                case AIGMCompanionIntentKind.TrackMonsters:
                case AIGMCompanionIntentKind.TrackNPCs:
                case AIGMCompanionIntentKind.TrackHumanNPCs:
                case AIGMCompanionIntentKind.TrackPlayers:
                case AIGMCompanionIntentKind.TrackAll:
                    return BuildTrackReadOnlyReport(intent, speaker);
                case AIGMCompanionIntentKind.ReportTrackingStatus:
                    return AIGMCompanionTrackingService.GetTrackingStatus(this, speaker);
                case AIGMCompanionIntentKind.StopTrackingCycle:
                case AIGMCompanionIntentKind.StartTrackingCycle:
                case AIGMCompanionIntentKind.StartTrackingAnimals:
                case AIGMCompanionIntentKind.StartTrackingMonsters:
                case AIGMCompanionIntentKind.StartTrackingNPCs:
                case AIGMCompanionIntentKind.StartTrackingHumanNPCs:
                case AIGMCompanionIntentKind.StartTrackingPlayers:
                case AIGMCompanionIntentKind.StartTrackingAll:
                    return BuildTrackingCycleReport(intent, speaker);
                default:
                    return String.Empty;
            }
        }

        private bool IsTrackingIntentKind(string intentKind)
        {
            switch (intentKind)
            {
                case AIGMCompanionIntentKind.TrackAnimals:
                case AIGMCompanionIntentKind.TrackMonsters:
                case AIGMCompanionIntentKind.TrackNPCs:
                case AIGMCompanionIntentKind.TrackHumanNPCs:
                case AIGMCompanionIntentKind.TrackPlayers:
                case AIGMCompanionIntentKind.TrackAll:
                case AIGMCompanionIntentKind.StartTrackingCycle:
                case AIGMCompanionIntentKind.StartTrackingAnimals:
                case AIGMCompanionIntentKind.StartTrackingMonsters:
                case AIGMCompanionIntentKind.StartTrackingNPCs:
                case AIGMCompanionIntentKind.StartTrackingHumanNPCs:
                case AIGMCompanionIntentKind.StartTrackingPlayers:
                case AIGMCompanionIntentKind.StartTrackingAll:
                case AIGMCompanionIntentKind.StopTrackingCycle:
                case AIGMCompanionIntentKind.ReportTrackingStatus:
                    return true;
                default:
                    return false;
            }
        }

        private AIGMCompanionCapabilityKind ResolveCapability(AIGMCompanionCommandRouteDecision decision, AIGMCompanionIntent intent)
        {
            string kind = intent != null ? intent.Kind : String.Empty;
            switch (kind)
            {
                case AIGMCompanionIntentKind.StartMonsterHunt:
                    return AIGMCompanionCapabilityKind.MonsterHunt;
                case AIGMCompanionIntentKind.StopMonsterHunt:
                    return AIGMCompanionCapabilityKind.MonsterHuntStop;
                case AIGMCompanionIntentKind.ReportMonsterHuntStatus:
                    return AIGMCompanionCapabilityKind.MonsterHuntStatus;
                case AIGMCompanionIntentKind.TrackAnimals:
                case AIGMCompanionIntentKind.TrackMonsters:
                case AIGMCompanionIntentKind.TrackNPCs:
                case AIGMCompanionIntentKind.TrackHumanNPCs:
                case AIGMCompanionIntentKind.TrackPlayers:
                case AIGMCompanionIntentKind.TrackAll:
                    return AIGMCompanionCapabilityKind.TrackReadOnly;
                case AIGMCompanionIntentKind.StartTrackingCycle:
                case AIGMCompanionIntentKind.StartTrackingAnimals:
                case AIGMCompanionIntentKind.StartTrackingMonsters:
                case AIGMCompanionIntentKind.StartTrackingNPCs:
                case AIGMCompanionIntentKind.StartTrackingHumanNPCs:
                case AIGMCompanionIntentKind.StartTrackingPlayers:
                case AIGMCompanionIntentKind.StartTrackingAll:
                    return AIGMCompanionCapabilityKind.TrackingCycle;
                case AIGMCompanionIntentKind.ReportTrackingStatus:
                    return AIGMCompanionCapabilityKind.TrackingStatus;
                case AIGMCompanionIntentKind.StopTrackingCycle:
                    return AIGMCompanionCapabilityKind.TrackingStop;
                case AIGMCompanionIntentKind.HuntAnimals:
                    return AIGMCompanionCapabilityKind.HuntAnimals;
                default:
                    return decision != null ? decision.Capability : AIGMCompanionCapabilityKind.None;
            }
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

            string originCompanionId = sourceCompanion is IAIGMCompanionActor ? ((IAIGMCompanionActor)sourceCompanion).CompanionId : null;
            string dialogueTargetCompanionId = AIGMCompanionTurnCoordinator.ResolveOwnerDirectedDialogueTarget(speech);
            allowRemoteRelay = allowRemoteRelay || AIGMCompanionTurnCoordinator.ShouldRelayOwnerSpeechAsCompanionDialogue(speech);
            string mode = companionOrigin ? "companion_dialogue" : "owner_relay_dialogue";
            bool shouldSpeak = AIGMCompanionTurnCoordinator.ShouldCompanionTakeVisibleTurn(this, effectiveSpeaker, speech, mode, originCompanionId, 0, dialogueTargetCompanionId);
            AIGMCompanionSpeechRequest request = new AIGMCompanionSpeechRequest(this, effectiveSpeaker, speech, mode, null, originCompanionId, 0, allowRemoteRelay, shouldSpeak, !shouldSpeak, dialogueTargetCompanionId);
            string rejection;
            AIGMCompanionSpeechQueue.TryEnqueue(request, shouldSpeak, out rejection);
        }

        public void ReceiveCompanionDialogue(BaseHire sourceCompanion, AIGMCompanionDialogueEvent dialogueEvent)
        {
            if (sourceCompanion == null || sourceCompanion.Deleted || dialogueEvent == null || String.IsNullOrWhiteSpace(dialogueEvent.Text))
                return;

            string originCompanionId = sourceCompanion is IAIGMCompanionActor ? ((IAIGMCompanionActor)sourceCompanion).CompanionId : null;
            bool shouldSpeak = AIGMCompanionTurnCoordinator.ShouldCompanionTakeVisibleTurn(this, sourceCompanion, dialogueEvent.Text, "companion_dialogue", originCompanionId, dialogueEvent.HopCount, dialogueEvent.TargetCompanionId);
            AIGMCompanionSpeechRequest request = new AIGMCompanionSpeechRequest(this, sourceCompanion, dialogueEvent.Text, "companion_dialogue", dialogueEvent.EventId, originCompanionId, dialogueEvent.HopCount, true, shouldSpeak, !shouldSpeak, dialogueEvent.TargetCompanionId);
            string rejection;
            AIGMCompanionSpeechQueue.TryEnqueue(request, shouldSpeak, out rejection);
        }

        public override void OnThink()
        {
            base.OnThink();
            AIGMCompanionTrackingService.Pulse(this);
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
