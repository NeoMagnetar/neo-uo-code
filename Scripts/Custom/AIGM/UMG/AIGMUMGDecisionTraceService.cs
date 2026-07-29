using System;
using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Custom.AIGM.UMG
{
    public static class AIGMUMGDecisionTraceService
    {
        private const int MaxTracesPerActor = 20;
        private static readonly object SyncRoot = new object();
        private static readonly Dictionary<Serial, List<AIGMUMGDecisionTrace>> TracesByActor = new Dictionary<Serial, List<AIGMUMGDecisionTrace>>();

        public static void Record(Mobile actor, AIGMUMGDecisionTrace trace)
        {
            if (actor == null || trace == null)
                return;

            lock (SyncRoot)
            {
                List<AIGMUMGDecisionTrace> list;
                if (!TracesByActor.TryGetValue(actor.Serial, out list))
                {
                    list = new List<AIGMUMGDecisionTrace>();
                    TracesByActor[actor.Serial] = list;
                }

                list.Insert(0, trace);
                while (list.Count > MaxTracesPerActor)
                    list.RemoveAt(list.Count - 1);
            }

            AIGMUMGLog.Write(
                "decision_compiled",
                actor,
                AIGMUMGLog.Fields(
                    "correlationId", trace.CorrelationId,
                    "sleeveId", trace.SleeveId,
                    "selectedIntent", trace.SelectedDecision != null ? trace.SelectedDecision.IntentType.ToString() : "None",
                    "result", trace.Result,
                    "capabilityFailures", String.Join("|", trace.CapabilityFailures.ToArray())));
        }

        public static AIGMUMGDecisionTrace GetLatest(Mobile actor)
        {
            if (actor == null)
                return null;

            lock (SyncRoot)
            {
                List<AIGMUMGDecisionTrace> list;
                if (TracesByActor.TryGetValue(actor.Serial, out list) && list.Count > 0)
                    return list[0];
            }

            return null;
        }

        public static List<AIGMUMGDecisionTrace> GetRecent(Mobile actor, int count)
        {
            List<AIGMUMGDecisionTrace> copy = new List<AIGMUMGDecisionTrace>();
            if (actor == null)
                return copy;

            lock (SyncRoot)
            {
                List<AIGMUMGDecisionTrace> list;
                if (!TracesByActor.TryGetValue(actor.Serial, out list))
                    return copy;

                int max = Math.Max(1, count);
                for (int i = 0; i < list.Count && i < max; i++)
                    copy.Add(list[i]);
            }

            return copy;
        }
    }
}
