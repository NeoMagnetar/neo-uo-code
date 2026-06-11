using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionSpeechQueue
    {
        private static readonly ConcurrentDictionary<int, DateTime> NextAllowedRequestUtc = new ConcurrentDictionary<int, DateTime>();
        private static readonly ConcurrentDictionary<Serial, byte> CompanionInFlight = new ConcurrentDictionary<Serial, byte>();
        private static readonly SemaphoreSlim WorkerGate = new SemaphoreSlim(2, 2);
        private static readonly TimeSpan SpeakerCooldown = TimeSpan.FromSeconds(2.0);
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(25.0);

        public static bool TryEnqueue(IAIGMCompanionActor companion, Mobile speaker, string text, bool shouldSpeak, out string rejection)
        {
            rejection = null;

            if (companion == null || companion.Shell == null || companion.Shell.Deleted)
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

            if (!shouldSpeak)
                return true;

            DateTime now = DateTime.UtcNow;
            DateTime nextAllowed;
            if (NextAllowedRequestUtc.TryGetValue(speaker.Serial.Value, out nextAllowed) && now < nextAllowed)
            {
                rejection = "Please give me a moment.";
                return false;
            }

            if (!CompanionInFlight.TryAdd(companion.Shell.Serial, 1))
            {
                rejection = "I am still thinking.";
                return false;
            }

            NextAllowedRequestUtc[speaker.Serial.Value] = now + SpeakerCooldown;

            SpeechRequest request = new SpeechRequest(companion, speaker, text);
            Task.Run(() => ProcessAsync(request));
            return true;
        }

        private static async Task ProcessAsync(SpeechRequest request)
        {
            await WorkerGate.WaitAsync().ConfigureAwait(false);
            try
            {
                using (CancellationTokenSource cts = new CancellationTokenSource(RequestTimeout))
                {
                    await Task.Run(() =>
                    {
                        AIGMResponse response = AIGMBridgeClient.AskCompanionSpeech(request.Speaker, request.Text, request.Companion);
                        DispatchToShardThread(request, response);
                    }, cts.Token).ConfigureAwait(false);
                }
            }
            catch
            {
                DispatchToShardThread(request, null);
            }
            finally
            {
                CompanionInFlight.TryRemove(request.CompanionSerial, out _);
                WorkerGate.Release();
            }
        }

        private static void DispatchToShardThread(SpeechRequest request, AIGMResponse response)
        {
            Timer.DelayCall(TimeSpan.Zero, delegate
            {
                ApplyResult(request, response);
            });
        }

        private static void ApplyResult(SpeechRequest request, AIGMResponse response)
        {
            if (request == null || request.Companion == null || request.Companion.Shell == null || request.Companion.Shell.Deleted)
                return;

            string reply = response != null ? response.ReplyText : null;
            if (String.IsNullOrWhiteSpace(reply))
                reply = "The archives could not complete the live bridge call.";

            reply = reply.Replace("<BR>", " ").Replace("<br>", " ").Replace("<BR/>", " ").Replace("<br/>", " ");
            if (reply.Length > 220)
                reply = reply.Substring(0, 220);

            request.Companion.Shell.SayTo(request.Speaker, reply);
        }

        private sealed class SpeechRequest
        {
            public IAIGMCompanionActor Companion { get; private set; }
            public Mobile Speaker { get; private set; }
            public string Text { get; private set; }
            public Serial CompanionSerial { get; private set; }

            public SpeechRequest(IAIGMCompanionActor companion, Mobile speaker, string text)
            {
                Companion = companion;
                Speaker = speaker;
                Text = text ?? String.Empty;
                CompanionSerial = companion != null && companion.Shell != null ? companion.Shell.Serial : Serial.MinusOne;
            }
        }
    }
}
