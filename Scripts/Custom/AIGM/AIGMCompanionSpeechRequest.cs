using System;
using System.Collections.Generic;
using Server.Custom.AIGM.UMG;
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
        public string CompanionRelationshipSummary { get; private set; }
        public string CompanionMode { get; private set; }
        public string CompanionModeReason { get; private set; }
        public string OwnerGroupContext { get; private set; }
        public string PartyListenerSet { get; private set; }
        public string PartySelectedResponderSet { get; private set; }
        public string PartySuppressedResponderSet { get; private set; }
        public string PartySuppressedResponderReasons { get; private set; }
        public string TurnCoordinatorDecision { get; private set; }
        public string ParsedIntentSummary { get; private set; }
        public string StateContextSummary { get; private set; }
        public string CapabilitySafetyPosture { get; private set; }
        public string UMGPersonaContextSummary { get; private set; }
        public string SpeakerRole { get; private set; }
        public string AddressedTargetName { get; private set; }
        public string TargetType { get; private set; }
        public string NearbyCompanions { get; private set; }
        public string RecentDialogueTurns { get; private set; }
        public string NaturalDialogueContract { get; private set; }
        public bool GroupAddressed { get; private set; }
        public string AddressedCompanionId { get; private set; }
        public string AddressingMode { get; private set; }
        public Serial PrimaryActorSerial { get; private set; }
        public string PrimaryCanonicalId { get; private set; }
        public string AddressedGroupId { get; private set; }
        public bool AllowSecondaryResponse { get; private set; }
        public int SecondaryResponseLimit { get; private set; }
        public string CorrelationId { get; private set; }
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
            CompanionRelationshipSummary = persona != null ? persona.RelationshipSummary : null;
            AIGMCompanionModeState modeState = AIGMCompanionModeService.GetSnapshot(companion != null ? companion.Shell : null);
            CompanionMode = AIGMCompanionModeService.FormatMode(modeState.Mode);
            CompanionModeReason = modeState.Reason;
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
            AddressingMode = partyContext != null ? partyContext.AddressingMode : "Unaddressed";
            PrimaryActorSerial = partyContext != null ? partyContext.PrimaryActorSerial : Serial.MinusOne;
            PrimaryCanonicalId = partyContext != null ? partyContext.PrimaryCanonicalId : String.Empty;
            AddressedGroupId = partyContext != null ? partyContext.AddressedGroupId : String.Empty;
            AllowSecondaryResponse = partyContext != null && partyContext.AllowSecondaryResponse;
            SecondaryResponseLimit = partyContext != null ? partyContext.SecondaryResponseLimit : 0;
            CorrelationId = partyContext != null && !String.IsNullOrWhiteSpace(partyContext.CorrelationId) ? partyContext.CorrelationId : RequestId;
            CompanionSerial = companion != null && companion.Shell != null ? companion.Shell.Serial : Serial.MinusOne;
            SpeakerSerial = speaker != null ? speaker.Serial : Serial.MinusOne;
            BaseHire baseHire = companion != null ? companion.Shell as BaseHire : null;
            Mobile owner = ResolveOwner(companion, speaker);
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
            UMGPersonaContextSummary = UMGPersonaBlockLibrary.BuildCompactDialogueContext(CompanionId, DialogueMode, DialogueTargetCompanionId, GroupAddressed, StateContextSummary, RawSpeech);
            SpeakerRole = ResolveSpeakerRole(speaker, owner);
            TargetType = ResolveTargetType(speaker, owner);
            NearbyCompanions = BuildNearbyCompanionSummary(companion, owner);
            AddressedTargetName = ResolveTargetName(companion, owner);
            RecentDialogueTurns = AIGMCompanionPerceptionBuffer.BuildRecentContext(companion);
            NaturalDialogueContract = BuildNaturalDialogueContract();
        }

        private string ResolveTargetType(Mobile speaker, Mobile owner)
        {
            if (!String.IsNullOrWhiteSpace(DialogueTargetCompanionId) || !String.IsNullOrWhiteSpace(AddressedCompanionId))
                return "named companion";

            if (GroupAddressed)
                return "group";

            if (speaker != null && speaker is BaseHire)
                return "companion";

            if (owner != null && speaker == owner)
                return "owner/player";

            return "unknown";
        }

        private static string ResolveSpeakerRole(Mobile speaker, Mobile owner)
        {
            if (speaker == null)
                return "unknown";

            IAIGMCompanionActor actor = speaker as IAIGMCompanionActor;
            if (actor != null)
                return "companion:" + (actor.CompanionId ?? actor.CompanionDisplayName ?? speaker.Name ?? "unknown");

            if (owner != null && speaker == owner)
                return "owner/player";

            return speaker.Player ? "player" : "world";
        }

        private string ResolveTargetName(IAIGMCompanionActor self, Mobile owner)
        {
            string targetId = !String.IsNullOrWhiteSpace(DialogueTargetCompanionId) ? DialogueTargetCompanionId : AddressedCompanionId;
            if (!String.IsNullOrWhiteSpace(targetId))
            {
                string name = FindCompanionNameById(self, owner, targetId);
                if (!String.IsNullOrWhiteSpace(name))
                    return name;
                return targetId;
            }

            if (GroupAddressed)
                return "party";

            return owner != null ? owner.Name : String.Empty;
        }

        private static string FindCompanionNameById(IAIGMCompanionActor self, Mobile owner, string targetId)
        {
            if (self == null || self.Shell == null || owner == null || String.IsNullOrWhiteSpace(targetId))
                return null;

            string normalized = targetId.Trim().ToLowerInvariant();
            foreach (Mobile mobile in World.Mobiles.Values)
            {
                IAIGMCompanionActor actor = mobile as IAIGMCompanionActor;
                if (actor == null || actor.Shell == null || actor.Shell.Deleted || AIGMCompanionSpeechBus.ResolveOwner(actor) != owner || actor.Shell.Map != self.Shell.Map)
                    continue;

                if (String.Equals(actor.CompanionId, normalized, StringComparison.OrdinalIgnoreCase)
                    || String.Equals(actor.CompanionDisplayName, targetId, StringComparison.OrdinalIgnoreCase)
                    || String.Equals(actor.Shell.Name, targetId, StringComparison.OrdinalIgnoreCase))
                    return actor.CompanionDisplayName ?? actor.Shell.Name;
            }

            return null;
        }

        private static string BuildNearbyCompanionSummary(IAIGMCompanionActor self, Mobile owner)
        {
            if (self == null || self.Shell == null || owner == null)
                return String.Empty;

            List<string> names = new List<string>();
            foreach (Mobile mobile in World.Mobiles.Values)
            {
                IAIGMCompanionActor actor = mobile as IAIGMCompanionActor;
                if (actor == null || actor.Shell == null || actor.Shell.Deleted || actor.Shell.Map != self.Shell.Map || AIGMCompanionSpeechBus.ResolveOwner(actor) != owner || !self.Shell.InRange(actor.Shell, 16))
                    continue;

                names.Add(String.Format("{0}/{1}", actor.CompanionDisplayName ?? actor.Shell.Name, actor.CompanionRole ?? actor.CompanionId));
            }

            return names.Count == 0 ? String.Empty : String.Join(", ", names.ToArray());
        }

        private static Mobile ResolveOwner(IAIGMCompanionActor companion, Mobile speaker)
        {
            Mobile linkedOwner = AIGMCompanionSpeechBus.ResolveOwner(companion);
            if (linkedOwner != null)
                return linkedOwner;

            BaseHire speakerCompanion = speaker as BaseHire;
            if (speakerCompanion != null && speakerCompanion.GetOwner() != null)
                return speakerCompanion.GetOwner();

            return speaker;
        }

        private string BuildNaturalDialogueContract()
        {
            return String.Format(
                "Natural dialogue contract: speaker={0}; addressedTarget={1}; targetType={2}; addressingMode={3}; primary={4}/{5}; secondaryAllowed={6}; secondaryLimit={7}; nearbyCompanions={8}; recentTurns={9}; responseLength={10}; directQuestion={11}; groupThread={12}; rules=speak as {13} only, answer the current speaker, use UMG persona as style/filter, no JSON, no debug, no raw block IDs, no canned stock phrase, no copyrighted quotes.",
                SpeakerRole ?? "unknown",
                AddressedTargetName ?? String.Empty,
                TargetType ?? "unknown",
                AddressingMode ?? "Unaddressed",
                PrimaryCanonicalId ?? String.Empty,
                PrimaryActorSerial != Serial.MinusOne ? String.Format("0x{0:X8}", PrimaryActorSerial.Value) : "none",
                AllowSecondaryResponse,
                SecondaryResponseLimit,
                NearbyCompanions ?? String.Empty,
                RecentDialogueTurns ?? String.Empty,
                GroupAddressed ? "discussion" : "normal",
                IsDirectQuestion(RawSpeech),
                GroupAddressed || String.Equals(DialogueMode, "owner_relay_dialogue", StringComparison.OrdinalIgnoreCase),
                CompanionDisplayName ?? CompanionId ?? "this companion");
        }

        private static bool IsDirectQuestion(string speech)
        {
            if (String.IsNullOrWhiteSpace(speech))
                return false;

            string normalized = speech.Trim().ToLowerInvariant();
            return normalized.Contains("?")
                || normalized.StartsWith("what ")
                || normalized.StartsWith("who ")
                || normalized.StartsWith("why ")
                || normalized.StartsWith("where ")
                || normalized.StartsWith("do ")
                || normalized.StartsWith("does ")
                || normalized.StartsWith("are ")
                || normalized.StartsWith("should ");
        }
    }
}
