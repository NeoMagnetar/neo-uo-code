using System;
using System.Collections.Generic;

namespace Server.Custom.AIGM
{
    public class AIGMConversationTurn
    {
        public string Role { get; set; }
        public string Text { get; set; }
    }

    public class AIGMSessionState
    {
        private static readonly Dictionary<Serial, AIGMSessionState> States = new Dictionary<Serial, AIGMSessionState>();

        public static AIGMSessionState Get(Mobile from)
        {
            AIGMSessionState state;
            if (!States.TryGetValue(from.Serial, out state))
            {
                state = new AIGMSessionState();
                States[from.Serial] = state;
            }

            return state;
        }

        public AIGMTargetInfo CurrentTarget { get; set; }
        public string LastQuestion { get; set; }
        public AIGMResponse LastResponse { get; set; }
        public DateTime LastRequestUtc { get; set; }
        public bool RequestInProgress { get; set; }
        public AIGMInvestigationSnapshot Investigation { get; set; }
        public string LastActionDescription { get; set; }
        public string LastActionResult { get; set; }
        public int ExecutionStepCount { get; set; }
        public string ActiveTaskSummary { get; set; }
        public string LastWorldSummary { get; set; }
        public List<AIGMConversationTurn> Conversation { get; set; }

        public AIGMSessionState()
        {
            Conversation = new List<AIGMConversationTurn>();
        }

        public void AddTurn(string role, string text)
        {
            if (String.IsNullOrWhiteSpace(role) || String.IsNullOrWhiteSpace(text))
                return;

            Conversation.Add(new AIGMConversationTurn { Role = role, Text = text });
            if (Conversation.Count > 8)
                Conversation.RemoveRange(0, Conversation.Count - 8);
        }
    }
}
