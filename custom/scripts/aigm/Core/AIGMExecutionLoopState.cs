using System;
using System.Collections.Generic;

namespace Server.Custom.AIGM
{
    public class AIGMExecutionLoopState
    {
        private static readonly Dictionary<Serial, AIGMExecutionLoopState> States = new Dictionary<Serial, AIGMExecutionLoopState>();

        public static AIGMExecutionLoopState Get(Mobile from)
        {
            if (from == null)
                return null;

            AIGMExecutionLoopState state;
            if (!States.TryGetValue(from.Serial, out state))
            {
                state = new AIGMExecutionLoopState();
                States[from.Serial] = state;
            }

            return state;
        }

        public int StepsExecuted { get; set; }
        public DateTime LastStepUtc { get; set; }
        public string LastPlanSummary { get; set; }
        public string LastStopReason { get; set; }
        public bool AutoContinueUsed { get; set; }
    }
}
