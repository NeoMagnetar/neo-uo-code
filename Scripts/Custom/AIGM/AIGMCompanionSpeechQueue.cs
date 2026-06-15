using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionSpeechQueue
    {
        public static bool IsInternalRejectionToken(string rejection)
        {
            if (String.IsNullOrWhiteSpace(rejection))
                return true;

            switch (rejection.Trim().ToLowerInvariant())
            {
                case "busy_primary":
                case "busy_primary?":
                case "busy_relay":
                case "yield_turn":
                case "context_only":
                case "relay_silent":
                case "not_selected":
                case "already_in_flight":
                case "in_flight":
                case "primary_busy":
                case "context":
                case "silent":
                    return true;
                default:
                    return false;
            }
        }

        public static bool TryGetVisibleRejection(string rejection, out string visible)
        {
            visible = null;
            if (String.IsNullOrWhiteSpace(rejection) || IsInternalRejectionToken(rejection))
                return false;

            visible = rejection;
            return true;
        }
        private static readonly ConcurrentDictionary<int, DateTime> NextAllowedRequestUtc = new ConcurrentDictionary<int, DateTime>();
        private static readonly ConcurrentDictionary<Serial, byte> CompanionInFlight = new ConcurrentDictionary<Serial, byte>();
        private static readonly ConcurrentDictionary<Serial, DateTime> NextVisibleTimeoutFallbackUtc = new ConcurrentDictionary<Serial, DateTime>();
        private static readonly SemaphoreSlim WorkerGate = new SemaphoreSlim(2, 2);
        private static readonly TimeSpan PlayerCooldown = TimeSpan.FromSeconds(3.0);
        private static readonly TimeSpan SharedOwnerDialogueCooldown = TimeSpan.FromSeconds(0.75);
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(25.0);
        private static readonly TimeSpan VisibleTimeoutFallbackCooldown = TimeSpan.FromSeconds(12.0);

        public static bool TryEnqueue(IAIGMCompanionActor companion, Mobile speaker, string text, bool shouldSpeak, out string rejection)
        {
            string dialogueTargetCompanionId = AIGMCompanionTurnCoordinator.ResolveOwnerDirectedDialogueTarget(text);
            bool allowRemoteRelay = AIGMCompanionTurnCoordinator.ShouldRelayOwnerSpeechAsCompanionDialogue(text);
            AIGMCompanionSpeechRequest request = new AIGMCompanionSpeechRequest(companion, speaker, text, "owner_or_world_speech", null, null, 0, allowRemoteRelay, shouldSpeak, !shouldSpeak, dialogueTargetCompanionId);
            return TryEnqueue(request, shouldSpeak, out rejection);
        }

        public static bool TryEnqueue(IAIGMCompanionActor companion, Mobile speaker, string text, string dialogueMode, out string rejection)
        {
            bool isPrimaryVisibleTurn = !String.Equals(dialogueMode, "companion_dialogue", StringComparison.OrdinalIgnoreCase)
                && !String.Equals(dialogueMode, "owner_relay_dialogue", StringComparison.OrdinalIgnoreCase);
            bool isContextOnly = !isPrimaryVisibleTurn;
            string dialogueTargetCompanionId = AIGMCompanionTurnCoordinator.ResolveOwnerDirectedDialogueTarget(text);
            bool allowRemoteRelay = String.Equals(dialogueMode, "owner_relay_dialogue", StringComparison.OrdinalIgnoreCase)
                || AIGMCompanionTurnCoordinator.ShouldRelayOwnerSpeechAsCompanionDialogue(text);
            AIGMCompanionSpeechRequest request = new AIGMCompanionSpeechRequest(companion, speaker, text, dialogueMode, null, null, 0, allowRemoteRelay, isPrimaryVisibleTurn, isContextOnly, dialogueTargetCompanionId);
            return TryEnqueue(request, isPrimaryVisibleTurn, out rejection);
        }

        public static bool TryEnqueue(AIGMCompanionSpeechRequest request, bool shouldSpeak, out string rejection)
        {
            rejection = null;

            if (request == null || request.Companion == null || request.Companion.Shell == null || request.Companion.Shell.Deleted)
            {
                rejection = "Companion is missing.";
                return false;
            }

            if (request.Speaker == null || request.Speaker.Deleted)
            {
                rejection = "Speaker is missing.";
                return false;
            }

            if (String.IsNullOrWhiteSpace(request.RawSpeech))
            {
                rejection = "Empty speech.";
                return false;
            }

            AIGMCompanionPerceptionBuffer.Record(request.Companion, request.DialogueMode, request.Speaker, request.RawSpeech);

            if (!shouldSpeak || request.IsContextOnly)
                return true;

            DateTime now = DateTime.UtcNow;
            DateTime nextAllowed;
            bool sharedOwnerDialogue = String.Equals(request.DialogueMode, "owner_relay_awareness", StringComparison.OrdinalIgnoreCase)
                || String.Equals(request.DialogueMode, "owner_relay_dialogue", StringComparison.OrdinalIgnoreCase)
                || String.Equals(request.DialogueMode, "owner_direct_dialogue", StringComparison.OrdinalIgnoreCase);
            bool companionDialogue = String.Equals(request.DialogueMode, "companion_dialogue", StringComparison.OrdinalIgnoreCase);
            TimeSpan cooldown = sharedOwnerDialogue ? SharedOwnerDialogueCooldown : PlayerCooldown;
            int cooldownKey = sharedOwnerDialogue
                ? ((request.Speaker.Serial.Value * 397) ^ request.CompanionSerial.Value)
                : ((request.Speaker.Serial.Value * 397) ^ 0);
            if (!companionDialogue && NextAllowedRequestUtc.TryGetValue(cooldownKey, out nextAllowed) && now < nextAllowed)
            {
                rejection = request.IsPrimaryVisibleTurn ? "Please give me a moment." : "yield_turn";
                return false;
            }

            if (!companionDialogue)
                NextAllowedRequestUtc[cooldownKey] = now + cooldown;

            if (!CompanionInFlight.TryAdd(request.CompanionSerial, 1))
            {
                rejection = request.IsPrimaryVisibleTurn ? "busy_primary" : "yield_turn";
                return false;
            }

            Task.Run(() => ProcessAsync(request));
            return true;
        }

        private static async Task ProcessAsync(AIGMCompanionSpeechRequest request)
        {
            await WorkerGate.WaitAsync().ConfigureAwait(false);
            try
            {
                using (CancellationTokenSource cts = new CancellationTokenSource(RequestTimeout))
                {
                    await Task.Run(() =>
                    {
                        string recentContext = AIGMCompanionPerceptionBuffer.BuildRecentContext(request.Companion);
                        if (!String.IsNullOrWhiteSpace(recentContext))
                            request = new AIGMCompanionSpeechRequest(request.Companion, request.Speaker, request.RawSpeech + "\n[dialogue_history]\n" + recentContext, request.DialogueMode, request.EventId, request.OriginCompanionId, request.HopCount, request.AllowRemoteRelay, request.IsPrimaryVisibleTurn, request.IsContextOnly, request.DialogueTargetCompanionId);

                        AIGMResponse response = AIGMBridgeClient.AskCompanionSpeechRequest(request);
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

        private static void DispatchToShardThread(AIGMCompanionSpeechRequest request, AIGMResponse response)
        {
            Timer.DelayCall(TimeSpan.Zero, delegate
            {
                ApplyResult(request, response);
            });
        }

        private static void ApplyResult(AIGMCompanionSpeechRequest request, AIGMResponse response)
        {
            if (request == null || request.Companion == null || request.Companion.Shell == null || request.Companion.Shell.Deleted)
                return;

            string reply = response != null ? response.ReplyText : null;
            if (String.IsNullOrWhiteSpace(reply))
            {
                if (!request.IsPrimaryVisibleTurn)
                    return;

                reply = BuildVisibleTimeoutFallback(request.Companion);
                if (String.IsNullOrWhiteSpace(reply))
                    return;
            }

            reply = reply.Replace("<BR>", " ").Replace("<br>", " ").Replace("<BR/>", " ").Replace("<br/>", " ");
            if (reply.Length > 220)
                reply = reply.Substring(0, 220);

            request.Companion.Shell.SayTo(request.Speaker, reply);
            AIGMCompanionPerceptionBuffer.Record(request.Companion, "reply", request.Companion.Shell, reply);

            if (String.Equals(request.DialogueMode, "companion_dialogue", StringComparison.OrdinalIgnoreCase))
                return;

            if (request.AllowRemoteRelay && request.HopCount == 0)
                PublishDialogueReply(request.Companion.Shell as BaseHire, reply, request.DialogueTargetCompanionId);
        }

        private static string BuildVisibleTimeoutFallback(IAIGMCompanionActor companion)
        {
            if (companion == null || companion.Shell == null)
                return null;

            DateTime now = DateTime.UtcNow;
            DateTime nextAllowed;
            if (NextVisibleTimeoutFallbackUtc.TryGetValue(companion.Shell.Serial, out nextAllowed) && now < nextAllowed)
                return null;

            NextVisibleTimeoutFallbackUtc[companion.Shell.Serial] = now + VisibleTimeoutFallbackCooldown;
            return String.Format("{0} seems distracted for a moment.", companion.CompanionDisplayName);
        }

        private static void PublishDialogueReply(BaseHire trustedCompanion, string reply, string dialogueTargetCompanionId)
        {
            if (trustedCompanion == null || trustedCompanion.Deleted || String.IsNullOrWhiteSpace(reply))
                return;

            Timer.DelayCall(TimeSpan.FromSeconds(1.0), delegate
            {
                if (trustedCompanion == null || trustedCompanion.Deleted)
                    return;

                AIGMCompanionDialogueBus.PublishDialogue(trustedCompanion, reply, dialogueTargetCompanionId);
            });
        }

    }
}
