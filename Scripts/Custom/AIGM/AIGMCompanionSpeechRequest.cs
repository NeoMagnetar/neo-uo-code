using System;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public sealed class AIGMCompanionSpeechRequest
    {
        public string RequestId { get; private set; }
        public IAIGMCompanionActor Companion { get; private set; }
        public Mobile Speaker { get; private set; }
        public string RawSpeech { get; private set; }
        public string NormalizedSpeech { get; private set; }
        public string DialogueMode { get; private set; }
        public string CompanionId { get; private set; }
        public string CompanionDisplayName { get; private set; }
        public string CompanionProfileKey { get; private set; }
        public string CompanionRole { get; private set; }
        public string CompanionDescription { get; private set; }
        public string CompanionVoiceGuidance { get; private set; }
        public string CompanionDutySummary { get; private set; }
        public string CompanionCapabilityBoundary { get; private set; }
        public string CompanionSiblingContext { get; private set; }
        public string CompanionPartyRoster { get; private set; }
        public string OwnerGroupContext { get; private set; }
        public string PartyListenerSet { get; private set; }
        public string PartySelectedResponderSet { get; private set; }
        public string PartySuppressedResponderSet { get; private set; }
        public string PartySuppressedResponderReasons { get; private set; }
        public string TurnCoordinatorDecision { get; private set; }
        public string ParsedIntentSummary { get; private set; }
        public string StateContextSummary { get; private set; }
        public string CapabilitySafetyPosture { get; private set; }
        public bool GroupAddressed { get; private set; }
        public string AddressedCompanionId { get; private set; }
        public Serial CompanionSerial { get; private set; }
        public Serial SpeakerSerial { get; private set; }
        public Serial OwnerSerial { get; private set; }
        public string MapName { get; private set; }
        public string RegionName { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }
        public int Z { get; private set; }
        public Guid? EventId { get; private set; }
        public string OriginCompanionId { get; private set; }
        public string DialogueTargetCompanionId { get; private set; }
        public int HopCount { get; private set; }
        public bool AllowRemoteRelay { get; private set; }
        public bool IsPrimaryVisibleTurn { get; private set; }
        public bool IsContextOnly { get; private set; }
        public DateTime CreatedUtc { get; private set; }

        public AIGMCompanionSpeechRequest(IAIGMCompanionActor companion, Mobile speaker, string speech, string dialogueMode, Guid? eventId, string originCompanionId, int hopCount, bool allowRemoteRelay)
            : this(companion, speaker, speech, dialogueMode, eventId, originCompanionId, hopCount, allowRemoteRelay, true, false)
        {
        }

        public AIGMCompanionSpeechRequest(IAIGMCompanionActor companion, Mobile speaker, string speech, string dialogueMode, Guid? eventId, string originCompanionId, int hopCount, bool allowRemoteRelay, bool isPrimaryVisibleTurn, bool isContextOnly)
            : this(companion, speaker, speech, dialogueMode, eventId, originCompanionId, hopCount, allowRemoteRelay, isPrimaryVisibleTurn, isContextOnly, null)
        {
        }

        public AIGMCompanionSpeechRequest(IAIGMCompanionActor companion, Mobile speaker, string speech, string dialogueMode, Guid? eventId, string originCompanionId, int hopCount, bool allowRemoteRelay, bool isPrimaryVisibleTurn, bool isContextOnly, string dialogueTargetCompanionId)
        {
            RequestId = Guid.NewGuid().ToString("N");
            Companion = companion;
            Speaker = speaker;
            RawSpeech = speech ?? String.Empty;
            NormalizedSpeech = (speech ?? String.Empty).Trim();
            DialogueMode = String.IsNullOrWhiteSpace(dialogueMode) ? "owner_or_world_speech" : dialogueMode;
            CompanionId = companion != null ? companion.CompanionId : null;
            CompanionDisplayName = companion != null ? companion.CompanionDisplayName : null;
            CompanionProfileKey = companion != null ? companion.CompanionProfileKey : null;
            CompanionRole = companion != null ? companion.CompanionRole : null;
            AIGMCompanionPersonaContext persona = AIGMCompanionProfileLibrary.BuildPersonaContext(companion);
            CompanionDescription = persona != null ? persona.CompanionIdentityLine : null;
            CompanionVoiceGuidance = persona != null ? persona.VoiceGuidance : null;
            CompanionDutySummary = persona != null ? persona.DutySummary : null;
            CompanionCapabilityBoundary = persona != null ? persona.CapabilityBoundary : null;
            CompanionSiblingContext = persona != null ? persona.SiblingFraming : null;
            CompanionPartyRoster = persona != null ? persona.PartyRoster : null;
            AIGMCompanionPartySpeechContext partyContext = AIGMCompanionTurnCoordinator.BuildContext(companion, speaker, speech, DialogueMode, originCompanionId, hopCount, dialogueTargetCompanionId);
            OwnerGroupContext = partyContext != null ? String.Format("mode={0}; owner={1}; groupAddressed={2}; addressed={3}; dialogueTarget={4}", partyContext.DialogueMode, partyContext.OwnerSpeaker, partyContext.GroupAddressed, partyContext.AddressedCompanionId, partyContext.DialogueTargetCompanionId) : String.Empty;
            PartyListenerSet = partyContext != null ? partyContext.FormatListenerSet() : String.Empty;
            PartySelectedResponderSet = partyContext != null ? partyContext.FormatSelectedResponders() : String.Empty;
            PartySuppressedResponderSet = partyContext != null ? partyContext.FormatSuppressedResponders() : String.Empty;
            PartySuppressedResponderReasons = partyContext != null ? partyContext.FormatSuppressedReasons() : String.Empty;
            TurnCoordinatorDecision = partyContext != null ? partyContext.TurnCoordinatorDecision : String.Empty;
            ParsedIntentSummary = partyContext != null ? partyContext.ParsedIntent : String.Empty;
            StateContextSummary = partyContext != null ? partyContext.StateContextSummary : String.Empty;
            CapabilitySafetyPosture = partyContext != null ? partyContext.CapabilitySafetyPosture : String.Empty;
            GroupAddressed = partyContext != null && partyContext.GroupAddressed;
            AddressedCompanionId = partyContext != null ? partyContext.AddressedCompanionId : String.Empty;
            CompanionSerial = companion != null && companion.Shell != null ? companion.Shell.Serial : Serial.MinusOne;
            SpeakerSerial = speaker != null ? speaker.Serial : Serial.MinusOne;
            BaseHire baseHire = companion != null ? companion.Shell as BaseHire : null;
            Mobile owner = baseHire != null ? baseHire.GetOwner() : null;
            OwnerSerial = owner != null ? owner.Serial : Serial.MinusOne;
            MapName = speaker != null && speaker.Map != null ? speaker.Map.Name : null;
            RegionName = speaker != null && speaker.Region != null ? speaker.Region.Name : null;
            X = speaker != null ? speaker.X : 0;
            Y = speaker != null ? speaker.Y : 0;
            Z = speaker != null ? speaker.Z : 0;
            EventId = eventId;
            OriginCompanionId = originCompanionId;
            DialogueTargetCompanionId = dialogueTargetCompanionId ?? String.Empty;
            HopCount = hopCount;
            AllowRemoteRelay = allowRemoteRelay;
            IsPrimaryVisibleTurn = isPrimaryVisibleTurn;
            IsContextOnly = isContextOnly;
            CreatedUtc = DateTime.UtcNow;
        }
    }
}
