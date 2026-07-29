using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Server.Custom.AIGM.UMG
{
    [DataContract]
    public sealed class AIGMUMGDecision
    {
        [DataMember(Order = 0)] public string CorrelationId { get; set; }
        [DataMember(Order = 1)] public string ActorName { get; set; }
        [DataMember(Order = 2)] public string ActorSerial { get; set; }
        [DataMember(Order = 3)] public AIGMUMGIntentType IntentType { get; set; }
        [DataMember(Order = 4)] public string Target { get; set; }
        [DataMember(Order = 5)] public string Destination { get; set; }
        [DataMember(Order = 6)] public int Priority { get; set; }
        [DataMember(Order = 7)] public List<string> OriginatingBlockIds { get; set; }
        [DataMember(Order = 8)] public List<AIGMCapabilityRequirement> CapabilityRequirements { get; set; }
        [DataMember(Order = 9)] public string SelectedFallback { get; set; }
        [DataMember(Order = 10)] public string Authorization { get; set; }
        [DataMember(Order = 11)] public DateTime? ExpiryUtc { get; set; }
        [DataMember(Order = 12)] public string DeterministicAdapter { get; set; }
        [DataMember(Order = 13)] public string ExecutionReceipt { get; set; }
        [DataMember(Order = 14)] public string Result { get; set; }

        public AIGMUMGDecision()
        {
            CorrelationId = Guid.NewGuid().ToString("N");
            ActorName = String.Empty;
            ActorSerial = String.Empty;
            IntentType = AIGMUMGIntentType.None;
            Target = String.Empty;
            Destination = String.Empty;
            Priority = 0;
            OriginatingBlockIds = new List<string>();
            CapabilityRequirements = new List<AIGMCapabilityRequirement>();
            SelectedFallback = String.Empty;
            Authorization = "dry_run";
            DeterministicAdapter = String.Empty;
            ExecutionReceipt = String.Empty;
            Result = "not_dispatched";
        }
    }

    [DataContract]
    public sealed class AIGMUMGDecisionTrace
    {
        [DataMember(Order = 0)] public string CorrelationId { get; set; }
        [DataMember(Order = 1)] public string ActorName { get; set; }
        [DataMember(Order = 2)] public string ActorSerial { get; set; }
        [DataMember(Order = 3)] public DateTime TimestampUtc { get; set; }
        [DataMember(Order = 4)] public string SleeveId { get; set; }
        [DataMember(Order = 5)] public int SleeveVersion { get; set; }
        [DataMember(Order = 6)] public List<string> SituationFacts { get; set; }
        [DataMember(Order = 7)] public List<string> ActiveNeoStacks { get; set; }
        [DataMember(Order = 8)] public List<string> ActiveOverlays { get; set; }
        [DataMember(Order = 9)] public List<string> CandidateActions { get; set; }
        [DataMember(Order = 10)] public List<string> RejectedActions { get; set; }
        [DataMember(Order = 11)] public List<string> CapabilityFailures { get; set; }
        [DataMember(Order = 12)] public List<string> ConflictingBlocks { get; set; }
        [DataMember(Order = 13)] public AIGMUMGDecision SelectedDecision { get; set; }
        [DataMember(Order = 14)] public string Result { get; set; }

        public AIGMUMGDecisionTrace()
        {
            CorrelationId = Guid.NewGuid().ToString("N");
            ActorName = String.Empty;
            ActorSerial = String.Empty;
            TimestampUtc = DateTime.UtcNow;
            SleeveId = String.Empty;
            SituationFacts = new List<string>();
            ActiveNeoStacks = new List<string>();
            ActiveOverlays = new List<string>();
            CandidateActions = new List<string>();
            RejectedActions = new List<string>();
            CapabilityFailures = new List<string>();
            ConflictingBlocks = new List<string>();
            SelectedDecision = new AIGMUMGDecision();
            Result = "dry_run";
        }

        public string BuildWhySummary()
        {
            AIGMUMGDecision decision = SelectedDecision ?? new AIGMUMGDecision();
            List<string> parts = new List<string>();
            parts.Add("Situation: " + (SituationFacts.Count == 0 ? "none recorded" : String.Join("; ", SituationFacts.ToArray())));
            parts.Add("Active blocks: " + (decision.OriginatingBlockIds.Count == 0 ? "none selected" : String.Join(", ", decision.OriginatingBlockIds.ToArray())));
            parts.Add("Selected intent: " + decision.IntentType);
            parts.Add("Capability validation: " + (CapabilityFailures.Count == 0 ? "valid" : String.Join("; ", CapabilityFailures.ToArray())));
            parts.Add("Adapter: " + (String.IsNullOrWhiteSpace(decision.DeterministicAdapter) ? "none" : decision.DeterministicAdapter));
            parts.Add("Execution: " + (String.IsNullOrWhiteSpace(decision.ExecutionReceipt) ? decision.Result : decision.ExecutionReceipt));
            return String.Join(" | ", parts.ToArray());
        }
    }
}
