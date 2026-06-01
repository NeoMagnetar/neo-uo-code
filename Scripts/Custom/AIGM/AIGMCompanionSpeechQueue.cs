using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionSpeechQueue
    {
        private static readonly ConcurrentDictionary<Serial, DateTime> NextAllowedRequestUtc = new ConcurrentDictionary<Serial, DateTime>();
        private static readonly ConcurrentDictionary<Serial, byte> CompanionInFlight = new ConcurrentDictionary<Serial, byte>();
        private static readonly SemaphoreSlim WorkerGate = new SemaphoreSlim(2, 2);
        private static readonly TimeSpan PlayerCooldown = TimeSpan.FromSeconds(3.0);
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(25.0);

        public static bool TryEnqueue(Mobile companion, Mobile speaker, string text, out string rejection)
        {
            return TryEnqueue(companion, speaker, text, null, out rejection);
        }

        public static bool TryEnqueue(Mobile companion, Mobile speaker, string text, string dialogueMode, out string rejection)
        {
            rejection = null;

            if (companion == null || companion.Deleted)
            {
                rejection = "Companion is missing.";
                return false;
            }

            if (speaker == null || speaker.Deleted)
            {
                rejection = "Speaker is missing.";
                return false;
            }

            if (String.IsNullOrWhiteSpace(text))
            {
                rejection = "Empty speech.";
                return false;
            }

            DateTime now = DateTime.UtcNow;
            DateTime nextAllowed;
            if (NextAllowedRequestUtc.TryGetValue(speaker.Serial, out nextAllowed) && now < nextAllowed)
            {
                rejection = "Please give me a moment.";
                AIGMExecutionLog.Write("DIALOGUE_QUEUE_REJECT reason=cooldown companion={0} speaker={1} mode={2} nextAllowed={3:o} text=\"{4}\"", companion.Serial.Value, speaker.Serial.Value, dialogueMode ?? String.Empty, nextAllowed, SafeLog(text));
                return false;
            }

            NextAllowedRequestUtc[speaker.Serial] = now + PlayerCooldown;

            if (!CompanionInFlight.TryAdd(companion.Serial, 1))
            {
                rejection = "I am still thinking.";
                AIGMExecutionLog.Write("DIALOGUE_QUEUE_REJECT reason=inflight companion={0} speaker={1} mode={2} text=\"{3}\"", companion.Serial.Value, speaker.Serial.Value, dialogueMode ?? String.Empty, SafeLog(text));
                return false;
            }

            AIGMCompanionSpeechRequest request = new AIGMCompanionSpeechRequest(companion, speaker, text, dialogueMode);
            AIGMExecutionLog.Write("COMPANION_QUEUE requestId={0} companion={1} speaker={2} mode={3} text=\"{4}\"", request.RequestId, request.CompanionSerial, request.SpeakerSerial, request.DialogueMode, SafeLog(text));

            Task.Run(() => ProcessAsync(request));
            return true;
        }

        private static async Task ProcessAsync(AIGMCompanionSpeechRequest request)
        {
            await WorkerGate.WaitAsync().ConfigureAwait(false);
            DateTime started = DateTime.UtcNow;

            try
            {
                AIGMExecutionLog.Write("COMPANION_WORKER_START requestId={0}", request.RequestId);
                using (CancellationTokenSource cts = new CancellationTokenSource(RequestTimeout))
                {
                    AIGMCompanionSpeechResult result = await AIGMCompanionBridgeClient.AskAsync(request, cts.Token).ConfigureAwait(false);
                    DispatchResultToShardThread(request, result, started);
                }
            }
            catch (Exception ex)
            {
                AIGMExecutionLog.Write("COMPANION_WORKER_FAIL requestId={0} error={1}", request == null ? "(null)" : request.RequestId, ex);
                if (request != null)
                    DispatchResultToShardThread(request, AIGMCompanionSpeechResult.Fail(request.RequestId, "The companion lost the thought."), started);
            }
            finally
            {
                if (request != null)
                    CompanionInFlight.TryRemove(request.CompanionSerial, out _);

                WorkerGate.Release();
            }
        }

        private static void DispatchResultToShardThread(AIGMCompanionSpeechRequest request, AIGMCompanionSpeechResult result, DateTime started)
        {
            Timer.DelayCall(TimeSpan.Zero, delegate
            {
                try
                {
                    ApplyResultOnShardThread(request, result, started);
                }
                catch (Exception ex)
                {
                    AIGMExecutionLog.Write("COMPANION_APPLY_FAIL requestId={0} error={1}", request == null ? "(null)" : request.RequestId, ex);
                }
            });
        }

        private static void ApplyResultOnShardThread(AIGMCompanionSpeechRequest request, AIGMCompanionSpeechResult result, DateTime started)
        {
            if (request == null)
                return;

            AIGMExecutionLog.Write("COMPANION_APPLY_START requestId={0}", request.RequestId);

            Mobile companion = World.FindMobile(request.CompanionSerial);
            Mobile speaker = World.FindMobile(request.SpeakerSerial);

            if (companion == null || companion.Deleted)
            {
                AIGMExecutionLog.Write("COMPANION_APPLY_SKIP requestId={0} reason=companion_missing", request.RequestId);
                return;
            }

            if (speaker == null || speaker.Deleted)
            {
                AIGMExecutionLog.Write("COMPANION_APPLY_SKIP requestId={0} reason=speaker_missing", request.RequestId);
                return;
            }

            string reply = result != null && result.Ok ? result.ReplyText : "I lost the thread for a moment.";
            reply = AIGMCompanionSpeechSanitizer.ForNpcSpeech(reply);
            double elapsedMs = (DateTime.UtcNow - started).TotalMilliseconds;

            BaseHire trustedCompanion = companion as BaseHire;
            if (trustedCompanion != null)
            {
                AIGMExecutionLog.Write("COMPANION_TRUSTED_ACTION_EXTRACT_START requestId={0} text=\"{1}\" reply=\"{2}\"", request.RequestId, SafeLog(request.Text), SafeLog(reply));
                AIGMCompanionIntent trustedIntent;
                bool extracted = AIGMCompanionResponseActionExtractor.TryExtract(trustedCompanion, speaker, request.Text, reply, out trustedIntent);
                AIGMExecutionLog.Write("COMPANION_TRUSTED_ACTION_EXTRACT_RESULT requestId={0} extracted={1} kind={2}", request.RequestId, extracted, trustedIntent != null ? trustedIntent.Kind : "null");

                if (extracted)
                {
                    AIGMCompanionActionPolicyResult decision = AIGMCompanionDirectActionPolicy.Decide(trustedCompanion, speaker, trustedIntent);
                    AIGMExecutionLog.Write("COMPANION_TRUSTED_ACTION requestId={0} kind={1} decision={2} reason=\"{3}\"", request.RequestId, trustedIntent != null ? trustedIntent.Kind : "null", decision != null ? decision.Decision.ToString() : "null", SafeLog(decision != null ? decision.Reason : String.Empty));

                    if (decision != null && decision.Decision == AIGMCompanionActionDecision.DirectExecute)
                    {
                        string actionResponse;
                        bool executed = AIGMCompanionActionExecutor.TryExecuteIntent(trustedCompanion, speaker, trustedIntent, out actionResponse);
                        AIGMExecutionLog.Write("COMPANION_TRUSTED_ACTION_EXECUTE requestId={0} kind={1} executed={2} response=\"{3}\"", request.RequestId, trustedIntent != null ? trustedIntent.Kind : "null", executed, SafeLog(actionResponse));

                        if (executed)
                        {
                            string spokenAction = !String.IsNullOrWhiteSpace(actionResponse) ? actionResponse : reply;
                            AIGMExecutionLog.Write("COMPANION_SAY requestId={0} ok={1} elapsedMs={2} reply=\"{3}\"", request.RequestId, result != null && result.Ok, elapsedMs, SafeLog(spokenAction));
                            companion.Say(spokenAction);
                            return;
                        }

                        if (!String.IsNullOrWhiteSpace(actionResponse))
                        {
                            AIGMExecutionLog.Write("COMPANION_SAY requestId={0} ok={1} elapsedMs={2} reply=\"{3}\"", request.RequestId, result != null && result.Ok, elapsedMs, SafeLog(actionResponse));
                            companion.Say(actionResponse);
                            return;
                        }
                    }
                }
            }

            AIGMExecutionLog.Write("COMPANION_SAY requestId={0} ok={1} elapsedMs={2} reply=\"{3}\"", request.RequestId, result != null && result.Ok, elapsedMs, SafeLog(reply));
            companion.Say(reply);
            if (trustedCompanion != null && String.Equals(request.DialogueMode, "companion_dialogue", StringComparison.OrdinalIgnoreCase))
            {
                PublishDialogueReply(trustedCompanion, reply);
            }
            else if (trustedCompanion != null && LooksAddressedToLinkedCompanion(trustedCompanion, reply))
            {
                AIGMExecutionLog.Write("DIALOGUE_PROMOTE requestId={0} source={1} text=\"{2}\"", request.RequestId, trustedCompanion.Serial.Value, SafeLog(reply));
                PublishDialogueReply(trustedCompanion, reply);
            }
        }

        private static void PublishDialogueReply(BaseHire trustedCompanion, string reply)
        {
            if (trustedCompanion == null || trustedCompanion.Deleted || String.IsNullOrWhiteSpace(reply))
                return;

            if (trustedCompanion is AIGMCompanionDanyal)
            {
                Timer.DelayCall(TimeSpan.FromSeconds(1.0), delegate
                {
                    if (trustedCompanion == null || trustedCompanion.Deleted)
                        return;

                    AIGMCompanionDialogueBus.PublishDialogue(trustedCompanion, reply);
                });
            }
            else
            {
                AIGMCompanionDialogueBus.PublishDialogue(trustedCompanion, reply);
            }
        }

        private static bool LooksAddressedToLinkedCompanion(BaseHire sourceCompanion, string reply)
        {
            if (sourceCompanion == null || sourceCompanion.Deleted || sourceCompanion.Map == null || String.IsNullOrWhiteSpace(reply))
                return false;

            Mobile owner = sourceCompanion.GetOwner();
            if (owner == null)
                return false;

            string normalized = reply.Trim();
            foreach (Mobile mobile in World.Mobiles.Values)
            {
                BaseHire ally = mobile as BaseHire;
                if (ally == null || ally == sourceCompanion || ally.Deleted)
                    continue;

                if (ally.Map != sourceCompanion.Map)
                    continue;

                if (ally.GetOwner() != owner)
                    continue;

                string allyName = ally.Name ?? String.Empty;
                if (allyName.Length == 0)
                    continue;

                if (normalized.StartsWith(allyName + ",", StringComparison.OrdinalIgnoreCase) || normalized.StartsWith(allyName + ".", StringComparison.OrdinalIgnoreCase) || normalized.StartsWith(allyName + " ", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static string SafeLog(string value)
        {
            if (String.IsNullOrEmpty(value))
                return String.Empty;

            value = value.Replace("\r", " ").Replace("\n", " ");
            if (value.Length > 240)
                value = value.Substring(0, 240) + "...";

            return value;
        }
    }
}
