using System;
using System.Collections.Generic;

namespace Server.Custom.AIGM
{
    public class AIGMConversationContext
    {
        public string ActiveTaskSummary { get; set; }
        public string LastWorldSummary { get; set; }
        public List<AIGMConversationTurn> RecentTurns { get; set; }

        public AIGMConversationContext()
        {
            RecentTurns = new List<AIGMConversationTurn>();
        }
    }

    public class AIGMRequest
    {
        public string RequestId { get; set; }
        public string TimestampUtc { get; set; }
        public string ShardName { get; set; }
        public string RequesterName { get; set; }
        public string AccessLevel { get; set; }
        public string MapName { get; set; }
        public string RegionName { get; set; }
        public string Mode { get; set; }
        public string CompanionName { get; set; }
        public string CompanionTypeName { get; set; }
        public string CompanionProfileKey { get; set; }
        public string CompanionRuntimeIdentity { get; set; }
        public string CompanionMemory { get; set; }
        public string SpeakerName { get; set; }
        public string SpeakerTypeName { get; set; }
        public bool SpeakerIsCompanion { get; set; }
        public string DialogueMode { get; set; }
        public string PartyListenerSet { get; set; }
        public string PartySelectedResponderSet { get; set; }
        public string PartySuppressedResponderSet { get; set; }
        public string TurnCoordinatorDecision { get; set; }
        public string StateContextSummary { get; set; }
        public string Question { get; set; }
        public AIGMTargetInfo Target { get; set; }
        public AIGMSceneContext Scene { get; set; }
        public AIGMExecutionContext Execution { get; set; }
        public AIGMConversationContext Conversation { get; set; }
    }
}
