using System;
using System.Collections.Generic;
using Server.ContextMenus;
using Server.Custom.AIGM.Inventory;
using Server.Custom.AIGM.Tasks;
using Server.Items;
using Server.Network;
using Server.Mobiles;

namespace Server.Custom.AIGM.Characters.Waylander
{
    public abstract class WaylanderRosterMobile : BaseCreature, IAIGMRosterTaskAgent, IAIGMCompanionActor
    {
        private string _characterId;
        private bool _adminTestPassive;
        private WaylanderRosterDisposition? _dispositionOverride;
        private bool _boundRosterCompanion;
        private Serial _trustedCommanderSerial;
        private AIGMRosterTaskState _rosterTaskState;
        private bool _dialogueGuardOwnerMode;
        private DateTime _nextSupportActionUtc;

        protected WaylanderRosterMobile(string characterId)
            : this(characterId, null)
        {
        }

        protected WaylanderRosterMobile(string characterId, AIType? overrideAiType)
            : base(
                overrideAiType ?? ResolveAiType(characterId),
                ResolveFightMode(characterId),
                10,
                1,
                0.2,
                0.4)
        {
            _characterId = characterId ?? String.Empty;
            ApplyDefinition(true);
        }

        public WaylanderRosterMobile(Serial serial)
            : base(serial)
        {
        }

        public string CharacterId
        {
            get { return _characterId ?? String.Empty; }
        }

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
            get { return CharacterId; }
        }

        public string CompanionDisplayName
        {
            get
            {
                WaylanderCharacterDefinition definition = Definition;
                if (definition != null && !String.IsNullOrWhiteSpace(definition.VisibleName))
                    return definition.VisibleName;

                return !String.IsNullOrWhiteSpace(Name) ? Name : CharacterId;
            }
        }

        public string CompanionRole
        {
            get
            {
                WaylanderCharacterDefinition definition = Definition;
                return definition != null ? definition.CombatRole : String.Empty;
            }
        }

        public string CompanionProfileKey
        {
            get
            {
                WaylanderCharacterDefinition definition = Definition;
                if (definition != null && !String.IsNullOrWhiteSpace(definition.PersonaAssetKey))
                    return definition.PersonaAssetKey;

                return CharacterId;
            }
        }

        public bool IsAIGMCompanion
        {
            get { return true; }
        }

        public bool CanUseAIGMSkills
        {
            get { return true; }
        }

        public bool GuardOwnerMode
        {
            get { return _dialogueGuardOwnerMode; }
            set { _dialogueGuardOwnerMode = value; }
        }

        public DateTime NextSupportActionUtc
        {
            get { return _nextSupportActionUtc; }
            set { _nextSupportActionUtc = value; }
        }

        public string ExecutionModeKey
        {
            get { return RosterBoundCompanion ? "roster_bound_manual" : "roster_unbound_manual"; }
        }

        public string RosterCharacterId
        {
            get { return CharacterId; }
        }

        public bool RosterPassiveTestMode
        {
            get { return _adminTestPassive; }
        }

        public bool RosterBoundCompanion
        {
            get { return _boundRosterCompanion; }
            set { _boundRosterCompanion = value; }
        }

        public Serial RosterTrustedCommanderSerial
        {
            get { return _trustedCommanderSerial; }
            set { _trustedCommanderSerial = value; }
        }

        public AIGMRosterTaskState RosterTaskState
        {
            get { return _rosterTaskState; }
            set { _rosterTaskState = value; }
        }

        public WaylanderRosterDisposition RosterDisposition
        {
            get { return EffectiveDisposition; }
        }

        public WaylanderCharacterDefinition RosterDefinition
        {
            get { return Definition; }
        }

        public WaylanderCharacterDefinition Definition
        {
            get { return WaylanderRosterCatalog.GetDefinition(CharacterId); }
        }

        public WaylanderRosterDisposition EffectiveDisposition
        {
            get
            {
                if (_dispositionOverride.HasValue)
                    return _dispositionOverride.Value;

                WaylanderCharacterDefinition definition = Definition;
                return definition != null ? definition.Disposition : WaylanderRosterDisposition.NeutralNonAggressive;
            }
        }

        public bool IsAdminTestSpawn
        {
            get { return _adminTestPassive; }
        }

        public override bool AlwaysAttackable
        {
            get
            {
                return EffectiveDisposition == WaylanderRosterDisposition.HostileMurderer;
            }
        }

        public override bool AlwaysMurderer
        {
            get
            {
                return EffectiveDisposition == WaylanderRosterDisposition.HostileMurderer;
            }
        }

        public override bool InitialInnocent
        {
            get
            {
                WaylanderRosterDisposition disposition = EffectiveDisposition;
                return disposition == WaylanderRosterDisposition.InnocentAllied
                    || disposition == WaylanderRosterDisposition.NeutralNonAggressive
                    || disposition == WaylanderRosterDisposition.ScenarioControlled;
            }
        }

        public override bool ClickTitle
        {
            get { return false; }
        }

        public override bool CanRummageCorpses
        {
            get { return false; }
        }

        public override bool HandlesOnSpeech(Mobile from)
        {
            if (from == null || !from.Alive || !from.InRange(this, 12))
                return false;

            return true;
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            if (e == null || e.Mobile == null || !e.Mobile.Alive || !e.Mobile.InRange(this, 12))
            {
                base.OnSpeech(e);
                return;
            }

            if (!e.Handled && AIGMRosterCommandService.TryHandleSpeech(this, e.Mobile, e.Speech, out string commandResponse))
            {
                if (!String.IsNullOrWhiteSpace(commandResponse))
                    SayTo(e.Mobile, commandResponse);

                e.Handled = true;
                return;
            }

            base.OnSpeech(e);

            if (e.Handled || !e.Mobile.InRange(this, 8))
                return;

            WaylanderCharacterDefinition definition = Definition;
            if (definition == null || !definition.CanTalk)
                return;

            if (!ShouldRespondToSpeech(definition, e.Speech))
                return;

            bool shouldSpeak = AIGMCompanionTurnCoordinator.ShouldCompanionTakeVisibleTurn(this, e.Mobile, e.Speech);
            string rejection;
            if (AIGMCompanionSpeechQueue.TryEnqueue(this, e.Mobile, e.Speech, shouldSpeak, out rejection))
            {
                AIGMCompanionDialogueThreadService.TryStartActor(this, e.Mobile, e.Speech, AIGMCompanionCommandBoundary.Classify(e.Speech));
                e.Handled = true;
                return;
            }

            string visibleRejection;
            if (shouldSpeak && AIGMCompanionSpeechQueue.TryGetVisibleRejection(rejection, out visibleRejection))
                SayTo(e.Mobile, visibleRejection);
        }

        public override void AddNameProperties(ObjectPropertyList list)
        {
            base.AddNameProperties(list);

            WaylanderCharacterDefinition definition = Definition;
            if (definition == null)
                return;

            if (!String.IsNullOrWhiteSpace(definition.Faction))
                list.Add(String.Format("Faction: {0}", definition.Faction));

            if (!String.IsNullOrWhiteSpace(definition.CombatRole))
                list.Add(String.Format("Role: {0}", definition.CombatRole));

            if (!String.IsNullOrWhiteSpace(definition.EraKey))
                list.Add(String.Format("Era: {0}", definition.EraKey));
        }

        public void ReceiveSpeechBusEvent(IAIGMCompanionActor sourceCompanion, Mobile eventSpeaker, string speech, bool companionOrigin)
        {
            if (sourceCompanion == null || sourceCompanion.Shell == null || sourceCompanion.Shell.Deleted || String.IsNullOrWhiteSpace(speech))
                return;

            Mobile effectiveSpeaker = eventSpeaker ?? sourceCompanion.Shell;
            string originCompanionId = sourceCompanion.CompanionId;
            string dialogueTargetCompanionId = AIGMCompanionTurnCoordinator.ResolveOwnerDirectedDialogueTarget(speech);
            bool allowRemoteRelay = AIGMCompanionTurnCoordinator.ShouldRelayOwnerSpeechAsCompanionDialogue(speech);
            string mode = companionOrigin ? "companion_dialogue" : "owner_relay_dialogue";
            bool shouldSpeak = AIGMCompanionTurnCoordinator.ShouldCompanionTakeVisibleTurn(this, effectiveSpeaker, speech, mode, originCompanionId, 0, dialogueTargetCompanionId);
            AIGMCompanionSpeechRequest request = new AIGMCompanionSpeechRequest(this, effectiveSpeaker, speech, mode, null, originCompanionId, 0, allowRemoteRelay, shouldSpeak, !shouldSpeak, dialogueTargetCompanionId);
            string rejection;
            AIGMCompanionSpeechQueue.TryEnqueue(request, shouldSpeak, out rejection);
        }

        public override void AddCustomContextEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.AddCustomContextEntries(from, list);
            AIGMRosterCompanionBindingService.AddContextMenuEntries(this, from, list);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(3);
            writer.Write(_characterId);
            writer.Write(_adminTestPassive);
            writer.Write(_dispositionOverride.HasValue);
            if (_dispositionOverride.HasValue)
                writer.Write((int)_dispositionOverride.Value);
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
            _characterId = version >= 1 ? reader.ReadString() : String.Empty;
            _adminTestPassive = version >= 2 && reader.ReadBool();
            if (version >= 2 && reader.ReadBool())
                _dispositionOverride = (WaylanderRosterDisposition)reader.ReadInt();
            else
                _dispositionOverride = null;
            _boundRosterCompanion = version >= 3 && reader.ReadBool();
            _trustedCommanderSerial = Serial.MinusOne;
            if (version >= 3)
                _trustedCommanderSerial = reader.ReadInt();
            if (version >= 3 && reader.ReadBool())
                _rosterTaskState = AIGMRosterTaskState.Deserialize(reader);
            else
                _rosterTaskState = null;

            Timer.DelayCall(TimeSpan.Zero, delegate
            {
                ApplyDefinition(false);
                ApplyRuntimeDispositionState(true);
                AIGMRosterTaskService.GetOrCreateState(this);
            });
        }

        public void ConfigureSpawnMode(bool passiveTestMode)
        {
            _adminTestPassive = passiveTestMode;
            _dispositionOverride = passiveTestMode
                ? WaylanderRosterDisposition.NeutralNonAggressive
                : (WaylanderRosterDisposition?)null;

            ApplyRuntimeDispositionState(true);
            if (_rosterTaskState != null)
                _rosterTaskState.PassiveTaskMode = passiveTestMode;
        }

        public void ConfigureScenarioDisposition(WaylanderRosterDisposition disposition)
        {
            _dispositionOverride = disposition;
            ApplyRuntimeDispositionState(true);
        }

        protected void ApplyDefinition(bool isFreshConstruct)
        {
            WaylanderCharacterDefinition definition = Definition;
            if (definition == null)
                return;

            Name = definition.VisibleName;
            Title = definition.Title;
            Female = definition.Female;
            Body = definition.Body;
            Hue = definition.Hue > 0 ? definition.Hue : Utility.RandomSkinHue();
            SpeechHue = definition.SpeechHue > 0 ? definition.SpeechHue : 0x3B2;
            BaseSoundID = definition.BaseSoundId > 0 ? definition.BaseSoundId : BaseSoundID;
            CantWalk = false;
            ControlSlots = definition.ControlSlots;

            if (definition.IsHumanoid)
                ApplyHair(definition);

            SetStr(definition.Str);
            SetDex(definition.Dex);
            SetInt(definition.Int);
            SetHits(definition.Hits);
            SetDamage(definition.DamageMin, definition.DamageMax);
            VirtualArmor = definition.VirtualArmor;
            Fame = definition.Fame;
            Karma = definition.Karma;

            SetSkillValues(definition);

            ApplyPersona(definition);

            if (isFreshConstruct && definition.EquipAction != null)
                definition.EquipAction(this);

            if (definition.PostConfigureAction != null)
                definition.PostConfigureAction(this);

            ApplyRuntimeDispositionState(isFreshConstruct);
        }

        protected void AddEquippedItem(Item item)
        {
            if (item == null)
                return;

            item.Movable = false;
            item.LootType = LootType.Blessed;
            AddItem(item);
        }

        protected void AddBackpackItem(Item item)
        {
            if (item == null)
                return;

            item.Movable = false;
            item.LootType = LootType.Blessed;
            PackItem(item);
        }

        public override DeathMoveResult GetInventoryMoveResultFor(Item item)
        {
            if (AIGMCompanionInventoryService.ShouldRetainBackpackContentOnDeath(this, item))
                return DeathMoveResult.MoveToBackpack;

            return base.GetInventoryMoveResultFor(item);
        }

        protected void ApplyHair(WaylanderCharacterDefinition definition)
        {
            HairItemID = definition.HairItemId;
            HairHue = definition.HairHue;

            if (!Female && definition.FacialHairItemId > 0)
            {
                FacialHairItemID = definition.FacialHairItemId;
                FacialHairHue = definition.FacialHairHue > 0 ? definition.FacialHairHue : definition.HairHue;
            }
        }

        protected void SetSkillValue(SkillName name, double value)
        {
            SetSkill(name, value);
        }

        private void SetSkillValues(WaylanderCharacterDefinition definition)
        {
            if (definition == null)
                return;

            foreach (var entry in definition.FixedSkills)
                SetSkill(entry.Key, entry.Value);
        }

        private void ApplyPersona(WaylanderCharacterDefinition definition)
        {
            if (definition == null || String.IsNullOrWhiteSpace(definition.PersonaAssetKey))
                return;

            string personaBlock = WaylanderPersonaRepository.GetPersonaBlock(definition.PersonaAssetKey);
            if (String.IsNullOrWhiteSpace(personaBlock))
                return;

            if (String.IsNullOrWhiteSpace(Profile))
            {
                Profile = personaBlock;
                return;
            }

            if (Profile.IndexOf("[AIGM_CANONICAL_PERSONA_BOOTSTRAP]", StringComparison.OrdinalIgnoreCase) < 0)
                Profile = personaBlock;
        }

        private bool ShouldRespondToSpeech(WaylanderCharacterDefinition definition, string speech)
        {
            if (definition == null || String.IsNullOrWhiteSpace(speech))
                return false;

            string lowered = speech.ToLowerInvariant();
            if (lowered.Contains("who are you") || lowered.Contains("your name") || lowered.Contains("what do you believe") || lowered.Contains("what faction") || lowered.Contains("who is "))
                return true;

            if (!String.IsNullOrWhiteSpace(definition.VisibleName) && lowered.Contains(definition.VisibleName.ToLowerInvariant()))
                return true;

            for (int i = 0; i < definition.Aliases.Length; i++)
            {
                if (!String.IsNullOrWhiteSpace(definition.Aliases[i]) && lowered.Contains(definition.Aliases[i].ToLowerInvariant()))
                    return true;
            }

            return false;
        }

        private static AIType ResolveAiType(string characterId)
        {
            WaylanderCharacterDefinition definition = WaylanderRosterCatalog.GetDefinition(characterId);
            return definition != null ? definition.AiType : AIType.AI_Melee;
        }

        private static FightMode ResolveFightMode(string characterId)
        {
            WaylanderCharacterDefinition definition = WaylanderRosterCatalog.GetDefinition(characterId);
            return definition != null ? definition.FightMode : FightMode.Aggressor;
        }

        private void ApplyRuntimeDispositionState(bool clearAggressionState)
        {
            WaylanderCharacterDefinition definition = Definition;
            if (definition == null)
                return;

            FightMode = ResolveRuntimeFightMode(definition, EffectiveDisposition, _adminTestPassive);
            Combatant = null;
            Warmode = false;

            if (clearAggressionState)
            {
                Aggressors.Clear();
                Aggressed.Clear();
            }

            if (EffectiveDisposition != WaylanderRosterDisposition.HostileMurderer)
            {
                Criminal = false;
                Kills = 0;
            }
        }

        public override void OnThink()
        {
            base.OnThink();
            AIGMRosterTaskService.Pulse(this);
        }

        private static FightMode ResolveRuntimeFightMode(WaylanderCharacterDefinition definition, WaylanderRosterDisposition disposition, bool adminTestPassive)
        {
            if (adminTestPassive)
                return FightMode.Aggressor;

            switch (disposition)
            {
                case WaylanderRosterDisposition.HostileMurderer:
                    return definition != null ? definition.FightMode : FightMode.Closest;
                case WaylanderRosterDisposition.AnimalNonAggressive:
                    return FightMode.None;
                default:
                    return FightMode.Aggressor;
            }
        }
    }
}
