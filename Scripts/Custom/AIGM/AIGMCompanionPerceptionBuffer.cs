using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public sealed class AIGMCompanionPerceptionBuffer
    {
        public sealed class HeardEntrySnapshot
        {
            public DateTime TimestampUtc { get; set; }
            public string Summary { get; set; }
        }

        private sealed class HeardEntry
        {
            public DateTime TimestampUtc { get; set; }
            public string Summary { get; set; }
        }

        private static readonly ConcurrentDictionary<Serial, List<HeardEntry>> BufferByCompanion = new ConcurrentDictionary<Serial, List<HeardEntry>>();
        private static readonly TimeSpan Retention = TimeSpan.FromMinutes(3.0);
        private const int MaxEntries = 8;

        public static void Record(IAIGMCompanionActor companion, string mode, Mobile speaker, string text)
        {
            if (companion == null || companion.Shell == null || speaker == null || String.IsNullOrWhiteSpace(text))
                return;

            string summary = String.Format("{0}:{1}:{2}", mode ?? "speech", speaker.Name ?? "unknown", text);
            List<HeardEntry> list = BufferByCompanion.GetOrAdd(companion.Shell.Serial, _ => new List<HeardEntry>());
            lock (list)
            {
                Prune(list);
                list.Add(new HeardEntry { TimestampUtc = DateTime.UtcNow, Summary = summary });
                while (list.Count > MaxEntries)
                    list.RemoveAt(0);
            }
        }

        public static string BuildRecentContext(IAIGMCompanionActor companion)
        {
            if (companion == null || companion.Shell == null)
                return null;

            List<HeardEntry> list;
            if (!BufferByCompanion.TryGetValue(companion.Shell.Serial, out list))
                return null;

            lock (list)
            {
                Prune(list);
                if (list.Count == 0)
                    return null;

                return String.Join(" | ", list.Select(x => x.Summary).ToArray());
            }
        }

        public static int GetRecentCount(IAIGMCompanionActor companion)
        {
            List<HeardEntrySnapshot> entries = GetRecentEntries(companion);
            return entries != null ? entries.Count : 0;
        }

        public static List<HeardEntrySnapshot> GetRecentEntries(IAIGMCompanionActor companion)
        {
            if (companion == null || companion.Shell == null)
                return null;

            List<HeardEntry> list;
            if (!BufferByCompanion.TryGetValue(companion.Shell.Serial, out list))
                return null;

            lock (list)
            {
                Prune(list);
                if (list.Count == 0)
                    return null;

                return list.Select(x => new HeardEntrySnapshot
                {
                    TimestampUtc = x.TimestampUtc,
                    Summary = x.Summary
                }).ToList();
            }
        }

        private static void Prune(List<HeardEntry> list)
        {
            DateTime cutoff = DateTime.UtcNow - Retention;
            list.RemoveAll(x => x.TimestampUtc < cutoff);
        }
    }
}
