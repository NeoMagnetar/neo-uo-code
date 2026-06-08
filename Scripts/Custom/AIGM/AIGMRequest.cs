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
        public string Question { get; set; }
        public AIGMTargetInfo Target { get; set; }
        public AIGMSceneContext Scene { get; set; }
        public AIGMExecutionContext Execution { get; set; }
        public AIGMConversationContext Conversation { get; set; }
    }
}
