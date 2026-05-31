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
                return false;
            }

            NextAllowedRequestUtc[speaker.Serial] = now + PlayerCooldown;

            if (!CompanionInFlight.TryAdd(companion.Serial, 1))
            {
                rejection = "I am still thinking.";
                return false;
            }

            AIGMCompanionSpeechRequest request = new AIGMCompanionSpeechRequest(companion, speaker, text);
            AIGMExecutionLog.Write("COMPANION_QUEUE requestId={0} companion={1} speaker={2} text=\"{3}\"", request.RequestId, request.CompanionSerial, request.SpeakerSerial, SafeLog(text));

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

            AIGMCompanionDakeyras dakeyras = companion as AIGMCompanionDakeyras;
            if (dakeyras != null)
            {
                AIGMExecutionLog.Write("COMPANION_TRUSTED_ACTION_EXTRACT_START requestId={0} text=\"{1}\" reply=\"{2}\"", request.RequestId, SafeLog(request.Text), SafeLog(reply));
                AIGMCompanionIntent trustedIntent;
                bool extracted = AIGMCompanionResponseActionExtractor.TryExtract(dakeyras, speaker, request.Text, reply, out trustedIntent);
                AIGMExecutionLog.Write("COMPANION_TRUSTED_ACTION_EXTRACT_RESULT requestId={0} extracted={1} kind={2}", request.RequestId, extracted, trustedIntent != null ? trustedIntent.Kind : "null");

                if (extracted)
                {
                    AIGMCompanionActionPolicyResult decision = AIGMCompanionDirectActionPolicy.Decide(dakeyras, speaker, trustedIntent);
                    AIGMExecutionLog.Write("COMPANION_TRUSTED_ACTION requestId={0} kind={1} decision={2} reason=\"{3}\"", request.RequestId, trustedIntent != null ? trustedIntent.Kind : "null", decision != null ? decision.Decision.ToString() : "null", SafeLog(decision != null ? decision.Reason : String.Empty));

                    if (decision != null && decision.Decision == AIGMCompanionActionDecision.DirectExecute)
                    {
                        string actionResponse;
                        bool executed = AIGMCompanionActionExecutor.TryExecuteIntent(dakeyras, speaker, trustedIntent, out actionResponse);
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
