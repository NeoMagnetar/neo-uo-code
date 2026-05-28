using System;
using System.Collections.Generic;

namespace Server.Custom.AIGM
{
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
    }
}
