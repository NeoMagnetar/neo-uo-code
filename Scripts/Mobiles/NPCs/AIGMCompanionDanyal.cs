using System;
using System.Collections.Generic;
using Server.Custom.AIGM;
using Server.Custom.AIGM.Characters.Waylander;
using Server.Custom.AIGM.Inventory;
using Server.Custom.AIGM.Tasks;
using Server.Items;

namespace Server.Mobiles
{
    public class AIGMCompanionDanyal : BaseHire, IAIGMCompanionActor, IAIGMRosterTaskAgent
    {
        private bool _guardOwnerMode;
        private DateTime _nextSupportActionUtc;
        private bool _boundRosterCompanion;
        private Serial _trustedCommanderSerial;
        private AIGMRosterTaskState _rosterTaskState;

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

        public string RosterCharacterId
        {
            get { return CompanionId; }
        }

        public bool RosterPassiveTestMode
        {
            get { return false; }
        }

        public bool RosterBoundCompanion
        {
            get { return Controlled || _boundRosterCompanion; }
            set { _boundRosterCompanion = value; }
        }

        public Serial RosterTrustedCommanderSerial
        {
            get { return Controlled && ControlMaster != null ? ControlMaster.Serial : _trustedCommanderSerial; }
            set { _trustedCommanderSerial = value; }
        }

        public AIGMRosterTaskState RosterTaskState
        {
            get { return _rosterTaskState; }
            set { _rosterTaskState = value; }
        }

        public WaylanderRosterDisposition RosterDisposition
        {
            get
            {
                WaylanderCharacterDefinition definition = RosterDefinition;
                return definition != null ? definition.Disposition : WaylanderRosterDisposition.InnocentAllied;
            }
        }

        public WaylanderCharacterDefinition RosterDefinition
        {
            get { return WaylanderRosterCatalog.GetDefinition(CompanionId); }
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

        public override DeathMoveResult GetInventoryMoveResultFor(Item item)
        {
            if (AIGMCompanionInventoryService.ShouldRetainBackpackContentOnDeath(this, item))
                return DeathMoveResult.MoveToBackpack;

            return base.GetInventoryMoveResultFor(item);
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
                _boundRosterCompanion = true;
                _trustedCommanderSerial = m.Serial;
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

        public override bool HandlesOnSpeech(Mobile from)
        {
            if (from != null && from.Alive && from.InRange(this, 8))
                return true;

            return base.HandlesOnSpeech(from);
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            if (e == null || e.Mobile == null || !e.Mobile.Alive || !e.Mobile.InRange(this, 8))
            {
                base.OnSpeech(e);
                return;
            }

            AIGMCompanionCommandRouteDecision decision = AIGMCompanionCommandBoundary.Classify(e.Speech);
            if (e.Handled && !ShouldContinueHandledGroupDialogue(decision))
            {
                base.OnSpeech(e);
                return;
            }

            if (!e.Handled && TryHandleExplicitGroupTrackingCommand(e))
                return;

            Mobile owner = GetOwner();
            bool trustedSpeaker = e.Mobile == owner || (!Controlled && e.Mobile.AccessLevel >= AccessLevel.GameMaster);

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

            string spellResponse;
            if (AIGMCompanionSpellService.TryHandleExplicitSpellCommand(this, e.Mobile, e.Speech, out spellResponse))
            {
                if (shouldSpeak && !String.IsNullOrWhiteSpace(spellResponse))
                    SayTo(e.Mobile, spellResponse);

                e.Handled = true;
                return;
            }

            string healingRouteReason;
            if (AIGMCompanionHealingService.ShouldRouteHealingCommand(this, e.Mobile, e.Speech, out healingRouteReason))
            {
                AIGMCompanionHealingService.LogCommandRoute(this, e.Mobile, e.Speech, healingRouteReason);

                string healingResponse;
                if (AIGMCompanionHealingService.TryHandleExplicitHealingCommand(this, e.Mobile, e.Speech, out healingResponse))
                {
                    if (shouldSpeak && !String.IsNullOrWhiteSpace(healingResponse))
                        SayTo(e.Mobile, healingResponse);

                    e.Handled = true;
                    return;
                }
            }

            string stopResponse;
            if (AIGMCompanionControlStopService.TryHandleSpeech(this, e.Mobile, e.Speech, decision, shouldSpeak, out stopResponse))
            {
                if (shouldSpeak && !String.IsNullOrWhiteSpace(stopResponse))
                    SayTo(e.Mobile, stopResponse);

                e.Handled = true;
                return;
            }

            string groupReceiptResponse;
            if (AIGMCompanionGroupReceiptService.TryHandle(this, e.Mobile, e.Speech, out groupReceiptResponse))
            {
                if (shouldSpeak && !String.IsNullOrWhiteSpace(groupReceiptResponse))
                    SayTo(e.Mobile, groupReceiptResponse);

                e.Handled = true;
                return;
            }

            string rosterTaskResponse;
            if (AIGMRosterCommandService.TryHandleSpeech(this, e.Mobile, e.Speech, out rosterTaskResponse))
            {
                if (shouldSpeak && !String.IsNullOrWhiteSpace(rosterTaskResponse))
                    SayTo(e.Mobile, rosterTaskResponse);

                e.Handled = true;
                return;
            }

            string trackingCommandResponse;
            if (AIGMCompanionTrackingCommandService.TryHandleSpeech(this, e.Mobile, e.Speech, decision, shouldSpeak, out trackingCommandResponse))
            {
                if (shouldSpeak && !String.IsNullOrWhiteSpace(trackingCommandResponse))
                    SayTo(e.Mobile, trackingCommandResponse);

                e.Handled = true;
                return;
            }

            string locateResponse;
            if (AIGMCompanionLocateService.TryHandleSpeech(this, e.Mobile, e.Speech, decision, shouldSpeak, out locateResponse))
            {
                AIGMCompanionDialogueThreadService.StopForCommand(owner, decision);

                if (shouldSpeak && !String.IsNullOrWhiteSpace(locateResponse))
                    SayTo(e.Mobile, locateResponse);

                e.Handled = true;
                return;
            }

            string dialogueControlResponse;
            if (AIGMCompanionDialogueControlService.TryHandleSpeechControl(this, e.Mobile, e.Speech, decision, out dialogueControlResponse))
            {
                if (shouldSpeak && !String.IsNullOrWhiteSpace(dialogueControlResponse))
                    SayTo(e.Mobile, dialogueControlResponse);

                e.Handled = true;
                return;
            }

            string navigationResponse;
            if (AIGMNavigationIntentParser.TryHandle(this, e.Mobile, e.Speech, decision, shouldSpeak, out navigationResponse))
            {
                AIGMCompanionDialogueThreadService.StopForCommand(owner, decision);

                if (shouldSpeak && !String.IsNullOrWhiteSpace(navigationResponse))
                    SayTo(e.Mobile, navigationResponse);

                e.Handled = true;
                return;
            }

            if (AIGMCompanionDialogueControlService.ShouldSuppressCasualDialogue(this, e.Mobile, e.Speech, decision))
            {
                e.Handled = true;
                return;
            }

            if (allowTrustedOwnerFallback || ShouldUseCompanionChat(e.Mobile, e.Speech, decision, intent, parsedIntent))
            {
                if (e.Mobile == owner && (allowTrustedOwnerFallback || decision.RouteKind == AIGMCompanionCommandRouteKind.SharedCompanion || decision.RouteKind == AIGMCompanionCommandRouteKind.NamedCompanion))
                    AIGMCompanionSpeechBus.PublishOwnerSpeech(this, e.Mobile, e.Speech);

                string rejection;
                if (AIGMCompanionSpeechQueue.TryEnqueue(this, e.Mobile, e.Speech, shouldSpeak, out rejection))
                {
                    AIGMCompanionDialogueThreadService.TryStart(this, owner, e.Speech, decision);
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
                AIGMCompanionDialogueThreadService.StopForCommand(owner, decision);
                if (!TryExecuteCompanionCommand(e.Mobile, decision, null, shouldSpeak))
                    return;

                return;
            }

            AIGMCompanionDialogueThreadService.StopForCommand(owner, decision);
            if (!TryExecuteCompanionCommand(e.Mobile, decision, intent, shouldSpeak))
                return;
        }

        private bool ShouldContinueHandledGroupDialogue(AIGMCompanionCommandRouteDecision decision)
        {
            return decision != null
                && !decision.IsCompanionCommand
                && decision.RouteKind == AIGMCompanionCommandRouteKind.SharedCompanion;
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
                SayTo(e.Mobile, AIGMCompanionTrackingService.StartTrackingAction(this, e.Mobile, mode, AIGMCompanionTrackingActionMode.TrackHunt));
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

            string spellResponse;
            if (AIGMCompanionSpellService.TryHandleExplicitSpellCommand(this, speaker, decision != null ? decision.OriginalSpeech : null, out spellResponse))
            {
                if (shouldSpeak && !String.IsNullOrWhiteSpace(spellResponse))
                    SayTo(speaker, spellResponse);
                return true;
            }

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
                AIGMCompanionControlStateService.RestoreFollowOwner(this, speaker, "follow_me_command");
                text = "I am with you. Keep your breathing steady.";
            }
            else if (intentKind == AIGMCompanionIntentKind.Stay)
            {
                ControlTarget = null;
                ControlOrder = OrderType.Stay;
                AIGMCompanionModeService.ClearMode(this, "command_stay");
                text = "I will hold here and keep bandages ready.";
            }
            else if (intentKind == AIGMCompanionIntentKind.GuardOwner)
            {
                ControlTarget = speaker;
                ControlOrder = OrderType.Guard;
                AIGMCompanionModeService.SetMode(this, AIGMCompanionMode.GuardOwner, "command_guard");
                text = "I will stay close and keep you covered.";
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
                text = "Not that way, not yet. I can still help keep us steady.";
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
                        AIGMCompanionControlStateService.RestoreFollowOwner(this, speaker, "follow_me_command");
                        text = "I am with you. Keep your breathing steady.";
                        break;
                    case AIGMCompanionCommandVerbKind.Stop:
                    case AIGMCompanionCommandVerbKind.Stay:
                    case AIGMCompanionCommandVerbKind.Hold:
                    case AIGMCompanionCommandVerbKind.Wait:
                        ControlTarget = null;
                        ControlOrder = OrderType.Stay;
                        AIGMCompanionModeService.ClearMode(this, "command_stay");
                        text = "I will hold here and keep bandages ready.";
                        break;
                    case AIGMCompanionCommandVerbKind.Guard:
                        ControlTarget = speaker;
                        ControlOrder = OrderType.Guard;
                        AIGMCompanionModeService.SetMode(this, AIGMCompanionMode.GuardOwner, "command_guard");
                        text = "I will stay close and keep you covered.";
                        break;
                    default:
                        text = "Give me a clearer order, commander. I will not guess with lives in hand.";
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

            if (!decision.IsCompanionCommand)
                return true;

            if (parsedIntent)
            {
                string kind = intent != null ? intent.Kind : null;
                if (kind == AIGMCompanionIntentKind.GreetCompanion)
                    return true;

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

            if (normalized.Contains("dakeyras"))
                return "Dakeyras, keep your eyes sharp. I want to know what you see before the woods know we are here.";

            if (normalized.Contains("dardalion"))
                return "Dardalion, stand close. If danger comes, we meet it together.";

            return targetName + ", stay near. I trust caution more than luck.";
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
                case AIGMCompanionCapabilityKind.LootNearby:
                    text = AIGMCompanionLootService.BuildVisibleResponse(this, AIGMCompanionLootService.StartLootNearby(this, speaker, AIGMCompanionLootProfile.FromText(intent != null ? intent.DestinationName : decision.OriginalSpeech)));
                    break;
                case AIGMCompanionCapabilityKind.LootStop:
                    AIGMCompanionLootService.StopLooting(this);
                    text = AIGMCompanionAutoLootService.SetAutoLoot(this, false, null);
                    break;
                case AIGMCompanionCapabilityKind.LootStatus:
                    text = AIGMCompanionLootService.GetLootStatus(this);
                    break;
                case AIGMCompanionCapabilityKind.AutoLoot:
                    text = AIGMCompanionAutoLootService.SetAutoLoot(this, true, AIGMCompanionLootProfile.FromText(intent != null ? intent.DestinationName : decision.OriginalSpeech));
                    break;
                case AIGMCompanionCapabilityKind.AutoLootStop:
                    AIGMCompanionLootService.StopLooting(this);
                    text = AIGMCompanionAutoLootService.SetAutoLoot(this, false, null);
                    break;
                case AIGMCompanionCapabilityKind.AutoLootStatus:
                    text = AIGMCompanionAutoLootService.GetAutoLootStatus(this);
                    break;
                case AIGMCompanionCapabilityKind.InventoryBurden:
                    text = AIGMCompanionInventoryPolicy.BuildStatusLine(this);
                    break;
                case AIGMCompanionCapabilityKind.UnloadJunk:
                    text = BuildUnloadJunkLine(AIGMCompanionInventoryPolicy.RunBurdenManagement(this, true));
                    break;
                case AIGMCompanionCapabilityKind.PotionSupport:
                    text = AIGMCompanionPotionService.SetPotionSupportEnabled(this, true);
                    break;
                case AIGMCompanionCapabilityKind.PotionSupportStop:
                    text = AIGMCompanionPotionService.SetPotionSupportEnabled(this, false);
                    break;
                case AIGMCompanionCapabilityKind.PotionSupportStatus:
                    text = AIGMCompanionPotionService.GetPotionStatus(this);
                    break;
                case AIGMCompanionCapabilityKind.PotionUse:
                    string potionResult;
                    AIGMCompanionPotionService.TryUseBestPotion(this, true, out potionResult);
                    text = AIGMCompanionPotionService.BuildVisibleResponse(this, potionResult);
                    break;
                case AIGMCompanionCapabilityKind.SpellSupport:
                    text = AIGMCompanionSpellService.SetSpellSupportEnabled(this, true, AIGMCompanionSpellProfile.SupportOnly());
                    break;
                case AIGMCompanionCapabilityKind.SpellSupportStop:
                    text = AIGMCompanionSpellService.SetSpellSupportEnabled(this, false, null);
                    break;
                case AIGMCompanionCapabilityKind.SpellSupportStatus:
                    text = AIGMCompanionSpellService.GetSpellStatus(this);
                    break;
                case AIGMCompanionCapabilityKind.SpellUse:
                    string spellResult;
                    AIGMCompanionSpellService.TryCastBestSupportSpell(this, true, out spellResult);
                    text = AIGMCompanionSpellService.BuildVisibleResponse(this, spellResult);
                    break;
                case AIGMCompanionCapabilityKind.ScanReadOnly:
                    AIGMCompanionModeService.SetMode(this, AIGMCompanionMode.AssessmentExplicit, "scan_read_only");
                    text = AIGMCompanionReadOnlyAwareness.BuildScanAreaReport(this, speaker);
                    break;
                case AIGMCompanionCapabilityKind.ReportThreatsReadOnly:
                    AIGMCompanionModeService.SetMode(this, AIGMCompanionMode.AssessmentExplicit, "report_threats_read_only");
                    text = AIGMCompanionReadOnlyAwareness.BuildThreatReport(this, speaker);
                    break;
                case AIGMCompanionCapabilityKind.ShareAwarenessReadOnly:
                    AIGMCompanionModeService.SetMode(this, AIGMCompanionMode.AssessmentExplicit, "share_awareness_read_only");
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

        private string BuildUnloadJunkLine(AIGMCompanionInventoryPolicyResult result)
        {
            if (result != null && result.DroppedItems > 0)
                return "We are carrying too much. I dropped the dead weight.";

            return "Nothing useless enough to drop.";
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
            if (intent != null && intent.Kind == AIGMCompanionIntentKind.RefusePlayerHunt)
            {
                AIGMCompanionModeService.SetMode(this, AIGMCompanionMode.RefusalPlayerHunt, "refuse_player_hunt");
                return "I can track a player's sign, but I will not make it a hunt.";
            }

            AIGMCompanionTrackingMode mode = AIGMCompanionTrackingService.GetModeFromIntentKind(intent != null ? intent.Kind : String.Empty);
            AIGMCompanionModeService.SetMode(this, AIGMCompanionMode.AssessmentExplicit, "tracking_read_only");
            return AIGMCompanionTrackingService.BuildTrackingSweepReport(this, speaker, mode);
        }

        private string BuildTrackingCycleReport(AIGMCompanionIntent intent, Mobile speaker)
        {
            return AIGMCompanionTrackingService.StartTrackingActionFromIntent(this, speaker, intent);
        }

        private string BuildCategoryTrackingReport(AIGMCompanionIntent intent, Mobile speaker)
        {
            AIGMCompanionTrackingMode mode = AIGMCompanionTrackingService.GetModeFromIntentKind(intent != null ? intent.Kind : String.Empty);
            return AIGMCompanionTrackingService.BuildTrackingSweepReport(this, speaker, mode);
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
                case AIGMCompanionIntentKind.HuntAnimals:
                case AIGMCompanionIntentKind.StartMonsterHunt:
                case AIGMCompanionIntentKind.StopMonsterHunt:
                case AIGMCompanionIntentKind.ReportMonsterHuntStatus:
                    return AIGMCompanionTrackingService.StartTrackingActionFromIntent(this, speaker, intent);
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
                case AIGMCompanionIntentKind.HuntAnimals:
                case AIGMCompanionIntentKind.StartTrackingCycle:
                case AIGMCompanionIntentKind.StartTrackingAnimals:
                case AIGMCompanionIntentKind.StartTrackingMonsters:
                case AIGMCompanionIntentKind.StartTrackingNPCs:
                case AIGMCompanionIntentKind.StartTrackingHumanNPCs:
                case AIGMCompanionIntentKind.StartTrackingPlayers:
                case AIGMCompanionIntentKind.StartTrackingAll:
                case AIGMCompanionIntentKind.StopTrackingCycle:
                case AIGMCompanionIntentKind.ReportTrackingStatus:
                case AIGMCompanionIntentKind.StartMonsterHunt:
                case AIGMCompanionIntentKind.StopMonsterHunt:
                case AIGMCompanionIntentKind.ReportMonsterHuntStatus:
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
                case AIGMCompanionIntentKind.LootNearby:
                    return AIGMCompanionCapabilityKind.LootNearby;
                case AIGMCompanionIntentKind.StopLooting:
                    return AIGMCompanionCapabilityKind.LootStop;
                case AIGMCompanionIntentKind.ReportLootStatus:
                    return AIGMCompanionCapabilityKind.LootStatus;
                case AIGMCompanionIntentKind.AutoLoot:
                    return AIGMCompanionCapabilityKind.AutoLoot;
                case AIGMCompanionIntentKind.StopAutoLoot:
                    return AIGMCompanionCapabilityKind.AutoLootStop;
                case AIGMCompanionIntentKind.ReportAutoLootStatus:
                    return AIGMCompanionCapabilityKind.AutoLootStatus;
                case AIGMCompanionIntentKind.ReportBurden:
                    return AIGMCompanionCapabilityKind.InventoryBurden;
                case AIGMCompanionIntentKind.UnloadJunk:
                    return AIGMCompanionCapabilityKind.UnloadJunk;
                case AIGMCompanionIntentKind.PotionSupport:
                    return AIGMCompanionCapabilityKind.PotionSupport;
                case AIGMCompanionIntentKind.StopPotionSupport:
                    return AIGMCompanionCapabilityKind.PotionSupportStop;
                case AIGMCompanionIntentKind.ReportPotionStatus:
                    return AIGMCompanionCapabilityKind.PotionSupportStatus;
                case AIGMCompanionIntentKind.UsePotion:
                    return AIGMCompanionCapabilityKind.PotionUse;
                case AIGMCompanionIntentKind.SpellSupport:
                    return AIGMCompanionCapabilityKind.SpellSupport;
                case AIGMCompanionIntentKind.StopSpellSupport:
                    return AIGMCompanionCapabilityKind.SpellSupportStop;
                case AIGMCompanionIntentKind.ReportSpellStatus:
                    return AIGMCompanionCapabilityKind.SpellSupportStatus;
                case AIGMCompanionIntentKind.UseSpell:
                    return AIGMCompanionCapabilityKind.SpellUse;
                case AIGMCompanionIntentKind.TrackAnimals:
                case AIGMCompanionIntentKind.TrackMonsters:
                case AIGMCompanionIntentKind.TrackNPCs:
                case AIGMCompanionIntentKind.TrackHumanNPCs:
                case AIGMCompanionIntentKind.TrackPlayers:
                case AIGMCompanionIntentKind.RefusePlayerHunt:
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
                case "healing_status":
                case "support_status":
                case AIGMCompanionIntentKind.ReportLootStatus:
                case AIGMCompanionIntentKind.ReportAutoLootStatus:
                case AIGMCompanionIntentKind.ReportBurden:
                case AIGMCompanionIntentKind.ReportPotionStatus:
                case AIGMCompanionIntentKind.ReportSpellStatus:
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
                case AIGMCompanionIntentKind.RefusePlayerHunt:
                case AIGMCompanionIntentKind.StartTrackingCycle:
                case AIGMCompanionIntentKind.StopTrackingCycle:
                case AIGMCompanionIntentKind.ReportTrackingStatus:
                case AIGMCompanionIntentKind.LootNearby:
                case AIGMCompanionIntentKind.StopLooting:
                case AIGMCompanionIntentKind.ReportLootStatus:
                case AIGMCompanionIntentKind.AutoLoot:
                case AIGMCompanionIntentKind.StopAutoLoot:
                case AIGMCompanionIntentKind.ReportAutoLootStatus:
                case AIGMCompanionIntentKind.ReportBurden:
                case AIGMCompanionIntentKind.UnloadJunk:
                case AIGMCompanionIntentKind.PotionSupport:
                case AIGMCompanionIntentKind.StopPotionSupport:
                case AIGMCompanionIntentKind.ReportPotionStatus:
                case AIGMCompanionIntentKind.UsePotion:
                case AIGMCompanionIntentKind.SpellSupport:
                case AIGMCompanionIntentKind.StopSpellSupport:
                case AIGMCompanionIntentKind.ReportSpellStatus:
                case AIGMCompanionIntentKind.UseSpell:
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
            AIGMCompanionHealingService.Pulse(this);
            AIGMCompanionPotionService.Pulse(this);
            AIGMCompanionSpellService.PulseSpellSupport(this);
            AIGMCompanionTrackingService.Pulse(this);
            AIGMRosterTaskService.Pulse(this);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)2);
            writer.Write(_guardOwnerMode);
            writer.Write(_nextSupportActionUtc);
            writer.Write(_boundRosterCompanion);
            writer.Write(_trustedCommanderSerial);
            writer.Write(_rosterTaskState != null);
            if (_rosterTaskState != null)
                _rosterTaskState.Serialize(writer);
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

            if (version >= 2)
            {
                _boundRosterCompanion = reader.ReadBool();
                _trustedCommanderSerial = reader.ReadInt();
                if (reader.ReadBool())
                    _rosterTaskState = AIGMRosterTaskState.Deserialize(reader);
            }
        }
    }
}

