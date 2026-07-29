using System;
using System.Collections.Generic;
using Server;

namespace Server.Custom.AIGM
{
    public enum AIGMCompanionDialogueMode
    {
        DirectNamedCommand,
        DirectNamedDialogue,
        GroupCommand,
        GroupConversation,
        CompanionToCompanion,
        OwnerDirectedCompanionDialogue,
        StateCommentary,
        SystemStatus
    }

    public sealed class AIGMCompanionPartySpeechContext
    {
        public string ContextId { get; set; }
        public string OwnerSpeaker { get; set; }
        public string RawMessage { get; set; }
        public string AddressedCompanionId { get; set; }
        public string DialogueTargetCompanionId { get; set; }
        public string AddressingMode { get; set; }
        public Serial PrimaryActorSerial { get; set; }
        public string PrimaryCanonicalId { get; set; }
        public string AddressedGroupId { get; set; }
        public bool AllowSecondaryResponse { get; set; }
        public int SecondaryResponseLimit { get; set; }
        public string CorrelationId { get; set; }
        public bool GroupAddressed { get; set; }
        public List<string> ListenerCompanions { get; private set; }
        public List<string> SelectedResponders { get; private set; }
        public List<string> SuppressedResponders { get; private set; }
        public List<string> SuppressedReasons { get; private set; }
        public AIGMCompanionDialogueMode DialogueMode { get; set; }
        public string ParsedIntent { get; set; }
        public string StateContextSummary { get; set; }
        public string LastPersonaProfileUsed { get; set; }
        public string LastStateSummaryUsed { get; set; }
        public string TurnCoordinatorDecision { get; set; }
        public string CapabilitySafetyPosture { get; set; }
        public string EchoSuppressionReason { get; set; }
        public int CompanionDialogueChainDepth { get; set; }
        public DateTime CreatedUtc { get; set; }

        public AIGMCompanionPartySpeechContext()
        {
            ContextId = Guid.NewGuid().ToString("N");
            OwnerSpeaker = String.Empty;
            RawMessage = String.Empty;
            AddressedCompanionId = String.Empty;
            DialogueTargetCompanionId = String.Empty;
            AddressingMode = "Unaddressed";
            PrimaryActorSerial = Serial.MinusOne;
            PrimaryCanonicalId = String.Empty;
            AddressedGroupId = String.Empty;
            AllowSecondaryResponse = false;
            SecondaryResponseLimit = 0;
            CorrelationId = ContextId;
            ListenerCompanions = new List<string>();
            SelectedResponders = new List<string>();
            SuppressedResponders = new List<string>();
            SuppressedReasons = new List<string>();
            DialogueMode = AIGMCompanionDialogueMode.GroupConversation;
            ParsedIntent = String.Empty;
            StateContextSummary = String.Empty;
            LastPersonaProfileUsed = String.Empty;
            LastStateSummaryUsed = String.Empty;
            TurnCoordinatorDecision = String.Empty;
            CapabilitySafetyPosture = "UMG/capability gates remain authoritative; speech context never executes actions by itself.";
            EchoSuppressionReason = String.Empty;
            CreatedUtc = DateTime.UtcNow;
        }

        public bool IsSelected(string companionId)
        {
            if (String.IsNullOrWhiteSpace(companionId))
                return false;

            for (int i = 0; i < SelectedResponders.Count; i++)
            {
                if (String.Equals(SelectedResponders[i], companionId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public void Suppress(string companionId, string reason)
        {
            if (String.IsNullOrWhiteSpace(companionId))
                return;

            SuppressedResponders.Add(companionId);
            SuppressedReasons.Add(companionId + "=" + (String.IsNullOrWhiteSpace(reason) ? "suppressed" : reason));
        }

        public string FormatListenerSet()
        {
            return ListenerCompanions.Count == 0 ? "none" : String.Join(", ", ListenerCompanions.ToArray());
        }

        public string FormatSelectedResponders()
        {
            return SelectedResponders.Count == 0 ? "none" : String.Join(", ", SelectedResponders.ToArray());
        }

        public string FormatSuppressedResponders()
        {
            return SuppressedResponders.Count == 0 ? "none" : String.Join(", ", SuppressedResponders.ToArray());
        }

        public string FormatSuppressedReasons()
        {
            return SuppressedReasons.Count == 0 ? "none" : String.Join("; ", SuppressedReasons.ToArray());
        }
    }
}
