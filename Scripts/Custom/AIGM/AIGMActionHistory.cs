using System;
using System.Collections.Generic;

namespace Server.Custom.AIGM
{
    public class AIGMActionHistoryEntry
    {
        public DateTime WhenUtc { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public string Result { get; set; }
        public string TargetSummary { get; set; }
    }

    public static class AIGMActionHistory
    {
        private static readonly Dictionary<Serial, List<AIGMActionHistoryEntry>> Entries = new Dictionary<Serial, List<AIGMActionHistoryEntry>>();

        public static void Record(Mobile from, AIGMActionProposal action, string result, string targetSummary)
        {
            if (from == null || action == null)
                return;

            List<AIGMActionHistoryEntry> list;
            if (!Entries.TryGetValue(from.Serial, out list))
            {
                list = new List<AIGMActionHistoryEntry>();
                Entries[from.Serial] = list;
            }

            list.Add(new AIGMActionHistoryEntry
            {
                WhenUtc = DateTime.UtcNow,
                Category = action.Category,
                Description = action.Description,
                Result = result,
                TargetSummary = targetSummary
            });

            if (list.Count > 8)
                list.RemoveAt(0);
        }

        public static List<AIGMActionHistoryEntry> Get(Mobile from)
        {
            if (from == null)
                return new List<AIGMActionHistoryEntry>();

            List<AIGMActionHistoryEntry> list;
            return Entries.TryGetValue(from.Serial, out list) ? new List<AIGMActionHistoryEntry>(list) : new List<AIGMActionHistoryEntry>();
        }
    }
}
