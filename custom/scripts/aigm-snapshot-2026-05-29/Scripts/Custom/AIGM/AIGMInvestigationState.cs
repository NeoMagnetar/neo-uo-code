using System.Collections.Generic;

namespace Server.Custom.AIGM
{
    public class AIGMInvestigationSnapshot
    {
        public string LastQuestion { get; set; }
        public string LastTargetSummary { get; set; }
        public string InvestigationKind { get; set; }
        public List<string> LikelyFiles { get; set; }
        public string LastActionDescription { get; set; }

        public AIGMInvestigationSnapshot()
        {
            LikelyFiles = new List<string>();
        }
    }

    public static class AIGMInvestigationState
    {
        private static readonly Dictionary<Serial, AIGMInvestigationSnapshot> States = new Dictionary<Serial, AIGMInvestigationSnapshot>();

        public static void Set(Mobile from, AIGMInvestigationSnapshot snapshot)
        {
            if (from == null || snapshot == null)
                return;

            States[from.Serial] = snapshot;
        }

        public static AIGMInvestigationSnapshot Get(Mobile from)
        {
            if (from == null)
                return null;

            AIGMInvestigationSnapshot snapshot;
            return States.TryGetValue(from.Serial, out snapshot) ? snapshot : null;
        }
    }
}
