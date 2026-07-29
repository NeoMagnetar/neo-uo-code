using System;
using System.Collections.Generic;

namespace Server.Custom.AIGM.UMG
{
    public static class AIGMUMGPreviewWatchService
    {
        private const int CadenceSeconds = 5;
        private const int MaxWatchedActors = 4;

        private static readonly object SyncRoot = new object();
        private static readonly Dictionary<string, WatchRecord> Watches = new Dictionary<string, WatchRecord>(StringComparer.OrdinalIgnoreCase);
        private static Timer _timer;

        public static string SetWatch(Mobile actor, bool enabled)
        {
            if (actor == null || actor.Deleted)
                return "D1E watch failed: actor missing.";

            AIGMUMGOperationalLayoutVersionRecord approved = null;
            string code;
            if (enabled && !AIGMUMGPreviewRuntimeService.TryGetApprovedLayout(actor, out approved, out code))
                return "D1E watch refused: " + code + "; no passive Preview observation started.";

            string actorSerial = AIGMUMGSleeveAccessService.FormatSerial(actor);
            lock (SyncRoot)
            {
                if (!enabled)
                {
                    Watches.Remove(actorSerial);
                    StopTimerIfIdle();
                    return "D1E passive Preview watch off for " + Describe(actor) + ".";
                }

                if (!Watches.ContainsKey(actorSerial) && Watches.Count >= MaxWatchedActors)
                    return "D1E watch refused: bounded watch capacity reached.";

                Watches[actorSerial] = new WatchRecord
                {
                    ActorSerial = actorSerial,
                    ActorName = actor.Name ?? actor.GetType().Name,
                    ApprovedLayoutVersionId = approved.VersionId,
                    EnabledUtc = AIGMUMGPreviewClock.UtcNow
                };

                EnsureTimer();
            }

            return String.Format("D1E passive Preview watch on for {0}; cadence={1}s; PreviewOnly; adapter invocations remain zero.", Describe(actor), CadenceSeconds);
        }

        public static bool IsWatched(Mobile actor)
        {
            if (actor == null)
                return false;

            lock (SyncRoot)
                return Watches.ContainsKey(AIGMUMGSleeveAccessService.FormatSerial(actor));
        }

        public static string BuildAudit()
        {
            lock (SyncRoot)
            {
                if (Watches.Count == 0)
                    return "D1E passive Preview watch: disabled for all actors; cadence=" + CadenceSeconds + "s.";

                List<string> lines = new List<string>();
                foreach (WatchRecord record in Watches.Values)
                {
                    lines.Add(String.Format("{0}[{1}] layoutVersion={2} receipts={3} coalesced={4} last={5}",
                        record.ActorName,
                        record.ActorSerial,
                        record.ApprovedLayoutVersionId,
                        record.GeneratedReceipts,
                        record.CoalescedSnapshots,
                        record.LastCapturedUtc == DateTime.MinValue ? "none" : record.LastCapturedUtc.ToString("O")));
                }

                return "D1E passive Preview watch: " + String.Join(" | ", lines.ToArray());
            }
        }

        private static void EnsureTimer()
        {
            if (_timer != null)
                return;

            _timer = Timer.DelayCall(TimeSpan.FromSeconds(CadenceSeconds), TimeSpan.FromSeconds(CadenceSeconds), Tick);
        }

        private static void StopTimerIfIdle()
        {
            if (Watches.Count != 0 || _timer == null)
                return;

            _timer.Stop();
            _timer = null;
        }

        private static void Tick()
        {
            List<WatchRecord> records;
            lock (SyncRoot)
                records = new List<WatchRecord>(Watches.Values);

            for (int i = 0; i < records.Count; i++)
                Evaluate(records[i]);

            lock (SyncRoot)
                StopTimerIfIdle();
        }

        private static void Evaluate(WatchRecord record)
        {
            if (record == null)
                return;

            Serial serial;
            if (!TryParseSerial(record.ActorSerial, out serial))
            {
                Remove(record.ActorSerial);
                return;
            }

            Mobile actor = World.FindMobile(serial);
            if (actor == null || actor.Deleted)
            {
                Remove(record.ActorSerial);
                return;
            }

            AIGMUMGOperationalLayoutVersionRecord approved;
            string code;
            if (!AIGMUMGPreviewRuntimeService.TryGetApprovedLayout(actor, out approved, out code)
                || !String.Equals(approved.VersionId, record.ApprovedLayoutVersionId, StringComparison.OrdinalIgnoreCase))
            {
                Remove(record.ActorSerial);
                return;
            }

            AIGMUMGDescentReceipt receipt = AIGMUMGPreviewRuntimeService.PreviewCurrentWorld(actor, null);
            lock (SyncRoot)
            {
                WatchRecord live;
                if (!Watches.TryGetValue(record.ActorSerial, out live))
                    return;

                live.LastCapturedUtc = receipt.CapturedUtc;
                if (String.Equals(live.LastSnapshotFingerprint, receipt.SnapshotId, StringComparison.OrdinalIgnoreCase)
                    && receipt.Warnings.Contains("CACHE_REUSED"))
                    live.CoalescedSnapshots++;
                else
                    live.GeneratedReceipts++;

                live.LastSnapshotFingerprint = receipt.SnapshotId;
                live.LastDecisionFingerprint = receipt.DecisionFingerprint;
            }
        }

        private static void Remove(string actorSerial)
        {
            lock (SyncRoot)
                Watches.Remove(actorSerial);
        }

        private static bool TryParseSerial(string value, out Serial serial)
        {
            serial = Serial.MinusOne;
            if (String.IsNullOrWhiteSpace(value))
                return false;

            string text = value.Trim();
            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                text = text.Substring(2);

            int parsed;
            if (!Int32.TryParse(text, System.Globalization.NumberStyles.HexNumber, null, out parsed)
                && !Int32.TryParse(text, out parsed))
                return false;

            serial = parsed;
            return serial.IsValid;
        }

        private static string Describe(Mobile actor)
        {
            return actor == null ? "none" : (String.IsNullOrWhiteSpace(actor.Name) ? actor.GetType().Name : actor.Name) + "[" + AIGMUMGSleeveAccessService.FormatSerial(actor) + "]";
        }

        private sealed class WatchRecord
        {
            public string ActorSerial;
            public string ActorName;
            public string ApprovedLayoutVersionId;
            public DateTime EnabledUtc;
            public DateTime LastCapturedUtc;
            public string LastSnapshotFingerprint;
            public string LastDecisionFingerprint;
            public int GeneratedReceipts;
            public int CoalescedSnapshots;
        }
    }
}
