using System;
using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public sealed class AIGMCompanionPerceptionState
    {
        public readonly Queue<AIGMCompanionTrackingSweep> LastTrackingSweeps = new Queue<AIGMCompanionTrackingSweep>();
        public readonly Dictionary<int, AIGMCompanionTrackingEntry> LastSightingsBySerial = new Dictionary<int, AIGMCompanionTrackingEntry>();
        public readonly Queue<AIGMCompanionAwarenessEvent> LastAwarenessEvents = new Queue<AIGMCompanionAwarenessEvent>();
        public DateTime LastSweepUtc;
    }

    public static class AIGMCompanionPerceptionBuffer
    {
        private static readonly Dictionary<int, AIGMCompanionPerceptionState> States = new Dictionary<int, AIGMCompanionPerceptionState>();
        private const int MaxTrackingSweeps = 20;
        private const int MaxSightings = 100;
        private const int MaxAwarenessEvents = 30;

        public static AIGMCompanionPerceptionState Get(BaseHire companion)
        {
            if (companion == null)
                return null;

            AIGMCompanionPerceptionState state;
            if (!States.TryGetValue(companion.Serial.Value, out state))
            {
                state = new AIGMCompanionPerceptionState();
                States[companion.Serial.Value] = state;
            }

            return state;
        }

        public static void RecordSweep(BaseHire companion, AIGMCompanionTrackingSweep sweep)
        {
            if (companion == null || sweep == null)
                return;

            AIGMCompanionPerceptionState state = Get(companion);
            if (state == null)
                return;

            state.LastTrackingSweeps.Enqueue(sweep);
            while (state.LastTrackingSweeps.Count > MaxTrackingSweeps)
                state.LastTrackingSweeps.Dequeue();

            if (sweep.Entries != null)
            {
                for (int i = 0; i < sweep.Entries.Count; i++)
                {
                    AIGMCompanionTrackingEntry entry = sweep.Entries[i];
                    if (entry == null || entry.TargetSerial == 0)
                        continue;

                    state.LastSightingsBySerial[entry.TargetSerial] = entry;
                }
            }

            while (state.LastSightingsBySerial.Count > MaxSightings)
            {
                int oldestKey = 0;
                DateTime oldest = DateTime.MaxValue;
                foreach (KeyValuePair<int, AIGMCompanionTrackingEntry> pair in state.LastSightingsBySerial)
                {
                    if (pair.Value != null && pair.Value.TimestampUtc < oldest)
                    {
                        oldest = pair.Value.TimestampUtc;
                        oldestKey = pair.Key;
                    }
                }

                if (oldestKey == 0)
                    break;

                state.LastSightingsBySerial.Remove(oldestKey);
            }

            state.LastSweepUtc = DateTime.UtcNow;
        }

        public static void RecordAwarenessEvent(BaseHire companion, AIGMCompanionAwarenessEvent awarenessEvent)
        {
            if (companion == null || awarenessEvent == null)
                return;

            AIGMCompanionPerceptionState state = Get(companion);
            if (state == null)
                return;

            state.LastAwarenessEvents.Enqueue(awarenessEvent);
            while (state.LastAwarenessEvents.Count > MaxAwarenessEvents)
                state.LastAwarenessEvents.Dequeue();
        }
    }
}
