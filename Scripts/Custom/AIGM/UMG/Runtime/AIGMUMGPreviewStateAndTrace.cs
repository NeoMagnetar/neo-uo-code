using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Server.Custom.AIGM.UMG
{
    public static class AIGMUMGPreviewClock
    {
        private static readonly object SyncRoot = new object();
        private static DateTime? _proofUtc;

        public static DateTime UtcNow
        {
            get
            {
                lock (SyncRoot)
                    return _proofUtc.HasValue ? _proofUtc.Value : DateTime.UtcNow;
            }
        }

        public static bool IsProofClock
        {
            get
            {
                lock (SyncRoot)
                    return _proofUtc.HasValue;
            }
        }

        public static void UseSystemClock()
        {
            lock (SyncRoot)
                _proofUtc = null;
        }

        public static void SetProofClock(DateTime utc)
        {
            lock (SyncRoot)
                _proofUtc = utc.Kind == DateTimeKind.Utc ? utc : utc.ToUniversalTime();
        }

        public static void AdvanceProofClock(TimeSpan delta)
        {
            lock (SyncRoot)
            {
                if (!_proofUtc.HasValue)
                    _proofUtc = DateTime.UtcNow;
                _proofUtc = _proofUtc.Value.Add(delta);
            }
        }
    }

    [DataContract]
    public sealed class AIGMUMGPreviewRuntimeBranchStateRecord
    {
        [DataMember(Order = 0)] public string ActorSerial { get; set; }
        [DataMember(Order = 1)] public string ApprovedLayoutVersionId { get; set; }
        [DataMember(Order = 2)] public string FamilyId { get; set; }
        [DataMember(Order = 3)] public string StackId { get; set; }
        [DataMember(Order = 4)] public AIGMUMGPreviewBranchState CurrentState { get; set; }
        [DataMember(Order = 5)] public DateTime BecameEligibleUtc { get; set; }
        [DataMember(Order = 6)] public DateTime BecameActiveUtc { get; set; }
        [DataMember(Order = 7)] public DateTime ReleaseCandidateUtc { get; set; }
        [DataMember(Order = 8)] public DateTime CooldownUntilUtc { get; set; }
        [DataMember(Order = 9)] public string LastSnapshotFingerprint { get; set; }
        [DataMember(Order = 10)] public string LastDecisionFingerprint { get; set; }

        public AIGMUMGPreviewRuntimeBranchStateRecord()
        {
            ActorSerial = String.Empty;
            ApprovedLayoutVersionId = String.Empty;
            FamilyId = String.Empty;
            StackId = String.Empty;
            CurrentState = AIGMUMGPreviewBranchState.Dormant;
            BecameEligibleUtc = DateTime.MinValue;
            BecameActiveUtc = DateTime.MinValue;
            ReleaseCandidateUtc = DateTime.MinValue;
            CooldownUntilUtc = DateTime.MinValue;
            LastSnapshotFingerprint = String.Empty;
            LastDecisionFingerprint = String.Empty;
        }
    }

    public static class AIGMUMGPreviewRuntimeStateStore
    {
        private static readonly object SyncRoot = new object();
        private static readonly Dictionary<string, AIGMUMGPreviewRuntimeBranchStateRecord> States = new Dictionary<string, AIGMUMGPreviewRuntimeBranchStateRecord>(StringComparer.OrdinalIgnoreCase);

        public static bool HasState(string actorSerial, string approvedLayoutVersionId)
        {
            string prefix = KeyPrefix(actorSerial, approvedLayoutVersionId);
            lock (SyncRoot)
            {
                foreach (string key in States.Keys)
                {
                    if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            return false;
        }

        public static AIGMUMGPreviewRuntimeBranchStateRecord GetOrCreate(string actorSerial, string approvedLayoutVersionId, string familyId, string stackId)
        {
            string key = Key(actorSerial, approvedLayoutVersionId, familyId, stackId);
            lock (SyncRoot)
            {
                AIGMUMGPreviewRuntimeBranchStateRecord record;
                if (!States.TryGetValue(key, out record))
                {
                    record = new AIGMUMGPreviewRuntimeBranchStateRecord
                    {
                        ActorSerial = actorSerial ?? String.Empty,
                        ApprovedLayoutVersionId = approvedLayoutVersionId ?? String.Empty,
                        FamilyId = familyId ?? String.Empty,
                        StackId = stackId ?? String.Empty
                    };
                    States[key] = record;
                }

                return record;
            }
        }

        public static List<AIGMUMGPreviewRuntimeBranchStateRecord> GetActorRecords(string actorSerial, string approvedLayoutVersionId)
        {
            List<AIGMUMGPreviewRuntimeBranchStateRecord> copy = new List<AIGMUMGPreviewRuntimeBranchStateRecord>();
            string prefix = KeyPrefix(actorSerial, approvedLayoutVersionId);
            lock (SyncRoot)
            {
                foreach (KeyValuePair<string, AIGMUMGPreviewRuntimeBranchStateRecord> pair in States)
                {
                    if (pair.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        copy.Add(Clone(pair.Value));
                }
            }

            copy.Sort(delegate (AIGMUMGPreviewRuntimeBranchStateRecord left, AIGMUMGPreviewRuntimeBranchStateRecord right)
            {
                int byFamily = String.Compare(left.FamilyId, right.FamilyId, StringComparison.OrdinalIgnoreCase);
                if (byFamily != 0)
                    return byFamily;
                return String.Compare(left.StackId, right.StackId, StringComparison.OrdinalIgnoreCase);
            });
            return copy;
        }

        public static int ResetActor(string actorSerial)
        {
            List<string> remove = new List<string>();
            string prefix = (actorSerial ?? String.Empty) + "|";
            lock (SyncRoot)
            {
                foreach (string key in States.Keys)
                {
                    if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        remove.Add(key);
                }

                for (int i = 0; i < remove.Count; i++)
                    States.Remove(remove[i]);
            }

            return remove.Count;
        }

        public static string BuildSummary(string actorSerial, string approvedLayoutVersionId)
        {
            List<AIGMUMGPreviewRuntimeBranchStateRecord> records = GetActorRecords(actorSerial, approvedLayoutVersionId);
            if (records.Count == 0)
                return "COLD_RUNTIME_STATE; records=0";

            List<string> lines = new List<string>();
            for (int i = 0; i < records.Count && i < 24; i++)
            {
                AIGMUMGPreviewRuntimeBranchStateRecord r = records[i];
                lines.Add(r.FamilyId + "/" + r.StackId + "=" + r.CurrentState + "; cooldownUntil=" + FormatUtc(r.CooldownUntilUtc));
            }
            return "WARM_RUNTIME_STATE; records=" + records.Count + "; " + String.Join(" | ", lines.ToArray());
        }

        public static string Fingerprint(string actorSerial, string approvedLayoutVersionId)
        {
            List<AIGMUMGPreviewRuntimeBranchStateRecord> records = GetActorRecords(actorSerial, approvedLayoutVersionId);
            List<string> parts = new List<string>();
            for (int i = 0; i < records.Count; i++)
            {
                AIGMUMGPreviewRuntimeBranchStateRecord r = records[i];
                parts.Add(r.FamilyId + "|" + r.StackId + "|" + r.CurrentState + "|" + FormatUtc(r.CooldownUntilUtc) + "|" + FormatUtc(r.BecameActiveUtc) + "|" + FormatUtc(r.ReleaseCandidateUtc));
            }
            return AIGMUMGStableHash.Compute(parts.ToArray());
        }

        private static AIGMUMGPreviewRuntimeBranchStateRecord Clone(AIGMUMGPreviewRuntimeBranchStateRecord record)
        {
            if (record == null)
                return null;

            return new AIGMUMGPreviewRuntimeBranchStateRecord
            {
                ActorSerial = record.ActorSerial,
                ApprovedLayoutVersionId = record.ApprovedLayoutVersionId,
                FamilyId = record.FamilyId,
                StackId = record.StackId,
                CurrentState = record.CurrentState,
                BecameEligibleUtc = record.BecameEligibleUtc,
                BecameActiveUtc = record.BecameActiveUtc,
                ReleaseCandidateUtc = record.ReleaseCandidateUtc,
                CooldownUntilUtc = record.CooldownUntilUtc,
                LastSnapshotFingerprint = record.LastSnapshotFingerprint,
                LastDecisionFingerprint = record.LastDecisionFingerprint
            };
        }

        private static string Key(string actorSerial, string approvedLayoutVersionId, string familyId, string stackId)
        {
            return KeyPrefix(actorSerial, approvedLayoutVersionId) + AIGMUMGStableHash.Normalize(familyId) + "|" + AIGMUMGStableHash.Normalize(stackId);
        }

        private static string KeyPrefix(string actorSerial, string approvedLayoutVersionId)
        {
            return (actorSerial ?? String.Empty) + "|" + (approvedLayoutVersionId ?? String.Empty) + "|";
        }

        private static string FormatUtc(DateTime value)
        {
            return value == DateTime.MinValue ? "none" : value.ToString("o");
        }
    }

    public static class AIGMUMGDescentTraceService
    {
        private const int MaxReceiptsPerActor = 32;
        private const int MaxGlobalReceipts = 256;
        private const long MaxLogBytes = 4L * 1024L * 1024L;
        private static readonly object SyncRoot = new object();
        private static readonly Dictionary<string, List<AIGMUMGDescentReceipt>> ReceiptsByActor = new Dictionary<string, List<AIGMUMGDescentReceipt>>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<AIGMUMGDescentReceipt> GlobalReceipts = new List<AIGMUMGDescentReceipt>();

        public static void Record(AIGMUMGDescentReceipt receipt)
        {
            if (receipt == null)
                return;

            lock (SyncRoot)
            {
                List<AIGMUMGDescentReceipt> list;
                if (!ReceiptsByActor.TryGetValue(receipt.ActorSerial, out list))
                {
                    list = new List<AIGMUMGDescentReceipt>();
                    ReceiptsByActor[receipt.ActorSerial] = list;
                }

                list.Insert(0, receipt);
                while (list.Count > MaxReceiptsPerActor)
                    list.RemoveAt(list.Count - 1);

                GlobalReceipts.Insert(0, receipt);
                while (GlobalReceipts.Count > MaxGlobalReceipts)
                    GlobalReceipts.RemoveAt(GlobalReceipts.Count - 1);
            }

            AppendPrivateJsonLine(receipt);
            AIGMUMGLog.Write("d1e_descent_receipt", null, AIGMUMGLog.Fields(
                "receiptId", receipt.ReceiptId,
                "actor", receipt.ActorSerial,
                "layoutVersion", receipt.ApprovedLayoutVersionId,
                "decisionFingerprint", receipt.DecisionFingerprint,
                "final", receipt.FinalResult,
                "adapterInvocationAttempts", receipt.CognitionBudget.AdapterInvocationAttempts.ToString(),
                "adapterInvocations", receipt.CognitionBudget.AdapterInvocations.ToString()));
        }

        public static List<AIGMUMGDescentReceipt> GetRecent(string actorSerial, int count)
        {
            List<AIGMUMGDescentReceipt> copy = new List<AIGMUMGDescentReceipt>();
            lock (SyncRoot)
            {
                List<AIGMUMGDescentReceipt> list;
                if (!ReceiptsByActor.TryGetValue(actorSerial ?? String.Empty, out list))
                    return copy;

                int max = Math.Max(1, count);
                for (int i = 0; i < list.Count && i < max; i++)
                    copy.Add(list[i]);
            }

            return copy;
        }

        public static AIGMUMGDescentReceipt FindByReceiptId(string receiptId)
        {
            return Find(delegate (AIGMUMGDescentReceipt receipt) { return String.Equals(receipt.ReceiptId, receiptId, StringComparison.OrdinalIgnoreCase); });
        }

        public static AIGMUMGDescentReceipt FindByCorrelationId(string correlationId)
        {
            return Find(delegate (AIGMUMGDescentReceipt receipt) { return String.Equals(receipt.CorrelationId, correlationId, StringComparison.OrdinalIgnoreCase); });
        }

        public static AIGMUMGDescentReceipt FindByDecisionFingerprint(string decisionFingerprint)
        {
            return Find(delegate (AIGMUMGDescentReceipt receipt) { return String.Equals(receipt.DecisionFingerprint, decisionFingerprint, StringComparison.OrdinalIgnoreCase); });
        }

        public static int ClearActorReceipts(string actorSerial)
        {
            lock (SyncRoot)
            {
                List<AIGMUMGDescentReceipt> list;
                if (!ReceiptsByActor.TryGetValue(actorSerial ?? String.Empty, out list))
                    return 0;

                int count = list.Count;
                ReceiptsByActor.Remove(actorSerial ?? String.Empty);
                for (int i = GlobalReceipts.Count - 1; i >= 0; i--)
                {
                    if (String.Equals(GlobalReceipts[i].ActorSerial, actorSerial, StringComparison.OrdinalIgnoreCase))
                        GlobalReceipts.RemoveAt(i);
                }

                return count;
            }
        }

        public static string BuildTraceSummary(string actorSerial)
        {
            List<AIGMUMGDescentReceipt> receipts = GetRecent(actorSerial, 8);
            if (receipts.Count == 0)
                return "No D1E descent receipts recorded for actor.";

            List<string> lines = new List<string>();
            for (int i = 0; i < receipts.Count; i++)
            {
                AIGMUMGDescentReceipt r = receipts[i];
                lines.Add(String.Format("#{0} receipt={1} source={2} scenario={3} intent={4} selected={5} final={6} dfp={7}",
                    i + 1,
                    Short(r.ReceiptId),
                    r.SnapshotSource,
                    String.IsNullOrWhiteSpace(r.SnapshotScenarioId) ? "none" : r.SnapshotScenarioId,
                    r.TypedIntent != null ? r.TypedIntent.Category.ToString() : "none",
                    r.SelectedBranchByFamily.Count,
                    r.FinalResult,
                    Short(r.DecisionFingerprint)));
            }

            return String.Join(" | ", lines.ToArray());
        }

        public static string ToJson<T>(T value)
        {
            if (value == null)
                return "null";

            using (MemoryStream stream = new MemoryStream())
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
                serializer.WriteObject(stream, value);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        private static AIGMUMGDescentReceipt Find(Predicate<AIGMUMGDescentReceipt> predicate)
        {
            if (predicate == null)
                return null;

            lock (SyncRoot)
            {
                for (int i = 0; i < GlobalReceipts.Count; i++)
                {
                    if (predicate(GlobalReceipts[i]))
                        return GlobalReceipts[i];
                }
            }

            return null;
        }

        private static void AppendPrivateJsonLine(AIGMUMGDescentReceipt receipt)
        {
            try
            {
                string path = GetLogPath();
                string dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                if (File.Exists(path) && new FileInfo(path).Length > MaxLogBytes)
                {
                    string rollover = path + "." + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".rollover";
                    File.Move(path, rollover);
                }

                File.AppendAllText(path, ToJson(receipt) + Environment.NewLine, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                AIGMUMGLog.Write("d1e_descent_receipt_log_warning", null, AIGMUMGLog.Fields("error", ex.Message));
            }
        }

        public static string GetLogPath()
        {
            return Path.Combine(Core.BaseDirectory, "Logs", "AIGM", "UMG", "D1E", "descent_receipts.jsonl");
        }

        private static string Short(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return "none";
            return value.Length <= 12 ? value : value.Substring(0, 12);
        }
    }
}
