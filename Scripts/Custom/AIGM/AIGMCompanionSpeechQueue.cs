using System;
using System.Collections.Concurrent;
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
        private static readonly ConcurrentDictionary<Serial, DateTime> NextBridgeUnavailableNoticeUtc = new ConcurrentDictionary<Serial, DateTime>();
        private static readonly SemaphoreSlim WorkerGate = new SemaphoreSlim(2, 2);
        private static int QueueTimeoutCount;
        private static int QueueBusyCount;
        private static readonly TimeSpan PlayerCooldown = TimeSpan.FromSeconds(3.0);
        private static readonly TimeSpan SharedOwnerDialogueCooldown = TimeSpan.FromSeconds(0.75);
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(75.0);
        private static readonly TimeSpan BridgeUnavailableNoticeCooldown = TimeSpan.FromSeconds(15.0);

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

            ApplyDialogueMode(request);
            AIGMCompanionPerceptionBuffer.Record(request.Companion, request.DialogueMode, request.Speaker, request.RawSpeech);

            if (!shouldSpeak || request.IsContextOnly)
                return true;

            DateTime now = DateTime.UtcNow;
            DateTime nextAllowed;
            bool sharedOwnerDialogue = String.Equals(request.DialogueMode, "owner_relay_awareness", StringComparison.OrdinalIgnoreCase)
                || String.Equals(request.DialogueMode, "owner_relay_dialogue", StringComparison.OrdinalIgnoreCase)
                || String.Equals(request.DialogueMode, "owner_direct_dialogue", StringComparison.OrdinalIgnoreCase);
            bool companionDialogue = String.Equals(request.DialogueMode, "companion_dialogue", StringComparison.OrdinalIgnoreCase);
            bool partyBroadcast = IsPlayerPartyBroadcast(request);
            TimeSpan cooldown = sharedOwnerDialogue || partyBroadcast ? SharedOwnerDialogueCooldown : PlayerCooldown;
            int cooldownKey = sharedOwnerDialogue || partyBroadcast
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
                Interlocked.Increment(ref QueueBusyCount);
                if (request.IsPrimaryVisibleTurn && String.Equals(request.AddressingMode, "ExactActor", StringComparison.OrdinalIgnoreCase))
                    rejection = String.Format("{0}: dialogue turn rejected - actor already has a pending reply.", request.CompanionDisplayName ?? request.CompanionId ?? "Companion");
                else
                    rejection = request.IsPrimaryVisibleTurn ? "busy_primary" : "yield_turn";
                return false;
            }

            Task.Run(() => ProcessAsync(request));
            return true;
        }

        private static void ApplyDialogueMode(AIGMCompanionSpeechRequest request)
        {
            if (request == null || request.Companion == null || request.Companion.Shell == null)
                return;

            if (String.Equals(request.DialogueMode, "companion_dialogue", StringComparison.OrdinalIgnoreCase))
            {
                AIGMCompanionModeService.SetMode(request.Companion.Shell, AIGMCompanionMode.DialogueCompanionReply, "companion_dialogue");
                return;
            }

            if (request.GroupAddressed ||
                String.Equals(request.DialogueMode, "owner_relay_dialogue", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(request.DialogueMode, "owner_relay_awareness", StringComparison.OrdinalIgnoreCase))
            {
                AIGMCompanionModeService.SetMode(request.Companion.Shell, AIGMCompanionMode.DialogueGroupBanter, "group_dialogue");
                return;
            }

            if (!request.IsContextOnly)
                AIGMCompanionModeService.SetMode(request.Companion.Shell, AIGMCompanionMode.DialogueNatural, "natural_dialogue");
        }

        private static bool IsPlayerPartyBroadcast(AIGMCompanionSpeechRequest request)
        {
            if (request == null || request.Speaker is BaseHire)
                return false;

            if (!String.IsNullOrWhiteSpace(request.AddressedCompanionId))
                return false;

            return request.GroupAddressed
                || String.Equals(request.DialogueMode, "owner_or_world_speech", StringComparison.OrdinalIgnoreCase);
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
                        AIGMResponse response = AIGMBridgeClient.AskCompanionSpeechRequest(request);
                        DispatchToShardThread(request, response);
                    }, cts.Token).ConfigureAwait(false);
                }
            }
            catch
            {
                Interlocked.Increment(ref QueueTimeoutCount);
                AIGMExecutionLog.Write("AIGM_CHATBOT_QUEUE_TIMEOUT requestId={0} timeoutSeconds={1} companion={2} mode={3}",
                    request != null ? request.RequestId : String.Empty,
                    (int)RequestTimeout.TotalSeconds,
                    request != null ? SafeLog(request.CompanionDisplayName) : String.Empty,
                    request != null ? SafeLog(request.DialogueMode) : String.Empty);
                DispatchToShardThread(request, null);
            }
            finally
            {
                CompanionInFlight.TryRemove(request.CompanionSerial, out _);
                WorkerGate.Release();
            }
        }

        public static int ActiveQueueCount
        {
            get { return CompanionInFlight.Count; }
        }

        public static int TotalQueueTimeoutCount
        {
            get { return QueueTimeoutCount; }
        }

        public static int TotalQueueBusyCount
        {
            get { return QueueBusyCount; }
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

            LogDialogueContext(request);

            bool bridgeOk = response != null && response.Ok;
            string responseSource = AIGMBridgeClient.GetResponseSource(response);
            bool liveDialogueResponse = AIGMBridgeClient.IsAcceptedLiveDialogueResponse(response);
            string rawReply = liveDialogueResponse ? response.ReplyText : null;

            if (!liveDialogueResponse)
            {
                AIGMExecutionLog.Write("AIGM_CHATBOT_SUPPRESSED_DEGRADED requestId={0} source={1} ok={2} fallbackUsed={3} degraded={4} mode={5} primary={6} reason={7}",
                    request.RequestId,
                    SafeLog(responseSource),
                    bridgeOk,
                    response != null && response.FallbackUsed,
                    response != null && response.Degraded,
                    SafeLog(request.DialogueMode),
                    request.IsPrimaryVisibleTurn,
                    response != null ? SafeLog(response.ErrorMessage) : "null_response");
                AIGMExecutionLog.Write("AIGM_CHATBOT_SOURCE requestId={0} source={1} visible=False reason=not_live_dialogue", request.RequestId, SafeLog(responseSource));
                AIGMCompanionDialogueThreadService.RecordSuppressedBridge(request, responseSource, response != null ? response.ErrorMessage : "null_response");
                SendBridgeUnavailableNotice(request);
                return;
            }

            string reply = SanitizeVisibleReply(rawReply);
            if (String.IsNullOrWhiteSpace(reply))
            {
                AIGMExecutionLog.Write("AIGM_CHATBOT_SANITIZER_REJECT requestId={0} source={1}", request.RequestId, SafeLog(responseSource));
                AIGMExecutionLog.Write("AIGM_CHATBOT_SUPPRESSED_DEGRADED requestId={0} source={1} ok={2} fallbackUsed={3} degraded={4} mode={5} primary={6} reason=sanitizer_empty",
                    request.RequestId,
                    SafeLog(responseSource),
                    bridgeOk,
                    response != null && response.FallbackUsed,
                    response != null && response.Degraded,
                    SafeLog(request.DialogueMode),
                    request.IsPrimaryVisibleTurn);
                AIGMExecutionLog.Write("AIGM_CHATBOT_SOURCE requestId={0} source={1} visible=False reason=sanitizer_empty", request.RequestId, SafeLog(responseSource));
                AIGMCompanionDialogueThreadService.RecordSuppressedBridge(request, responseSource, "sanitizer_empty");
                SendBridgeUnavailableNotice(request);
                return;
            }

            string suppressReason;
            if (AIGMCompanionDialogueThreadService.ShouldSuppressVisibleTurn(request, out suppressReason))
            {
                AIGMExecutionLog.Write("AIGM_CONVERSATION_SUPPRESS_BRIDGE_UNHEALTHY owner={0} requestId={1} source=queue reason={2} mode={3}",
                    request.OwnerSerial.Value,
                    request.RequestId,
                    SafeLog(suppressReason),
                    SafeLog(request.DialogueMode));
                AIGMExecutionLog.Write("AIGM_CHATBOT_SOURCE requestId={0} source={1} visible=False reason={2}", request.RequestId, SafeLog(responseSource), SafeLog(suppressReason));
                return;
            }

            AIGMExecutionLog.Write("AIGM_DIALOGUE_PATH requestId={0} path=live source={1} companion={2} speaker={3}", request.RequestId, SafeLog(responseSource), SafeLog(request.CompanionDisplayName), SafeLog(request.Speaker != null ? request.Speaker.Name : null));

            if (reply.Length > 220)
                reply = reply.Substring(0, 220);

            if (String.Equals(request.DialogueMode, "companion_dialogue", StringComparison.OrdinalIgnoreCase))
                request.Companion.Shell.Say(reply);
            else
                request.Companion.Shell.SayTo(request.Speaker, reply);

            AIGMCompanionPerceptionBuffer.Record(request.Companion, "reply", request.Companion.Shell, reply);
            AIGMCompanionDialogueThreadService.RecordVisibleTurn(request, reply);

            if (String.Equals(request.DialogueMode, "companion_dialogue", StringComparison.OrdinalIgnoreCase))
                return;

            if (request.AllowRemoteRelay && request.HopCount == 0)
                PublishDialogueReply(request.Companion, reply, request.DialogueTargetCompanionId);
        }

        private static string SanitizeVisibleReply(string reply)
        {
            if (String.IsNullOrWhiteSpace(reply))
                return reply;

            reply = AIGMCompanionSpeechSanitizer.ForNpcSpeech(reply);

            string lower = reply.ToLowerInvariant();
            if (ContainsInternalMarker(lower))
                return null;

            return reply.Trim();
        }

        private static void SendBridgeUnavailableNotice(AIGMCompanionSpeechRequest request)
        {
            if (request == null || !request.IsPrimaryVisibleTurn || request.Speaker == null || request.Speaker.Deleted)
                return;

            DateTime now = DateTime.UtcNow;
            DateTime nextAllowed;
            if (NextBridgeUnavailableNoticeUtc.TryGetValue(request.Speaker.Serial, out nextAllowed) && now < nextAllowed)
                return;

            NextBridgeUnavailableNoticeUtc[request.Speaker.Serial] = now + BridgeUnavailableNoticeCooldown;
            request.Speaker.SendMessage(38, "AIGM dialogue bridge unavailable; check [AIGMChatbotStatus].");
        }

        private static bool ContainsInternalMarker(string lower)
        {
            if (String.IsNullOrWhiteSpace(lower))
                return false;

            return lower.Contains("[mode:")
                || lower.Contains("[companion_id:")
                || lower.Contains("[companion_name:")
                || lower.Contains("[companion_role:")
                || lower.Contains("[companion_profile:")
                || lower.Contains("[persona_identity:")
                || lower.Contains("[listener")
                || lower.Contains("[state")
                || lower.Contains("i understand your request as:");
        }

        private static void LogDialogueContext(AIGMCompanionSpeechRequest request)
        {
            if (request == null)
                return;

            int selectedBlockCount = CountSelectedBlocks(request.UMGPersonaContextSummary);
            AIGMExecutionLog.Write("AIGM_DIALOGUE_CONTEXT requestId={0} speaker=\"{1}\" speakerRole=\"{2}\" companion=\"{3}\" target=\"{4}\" targetType=\"{5}\" mode={6} selectedBlocks={7} nearby=\"{8}\"",
                request.RequestId,
                SafeLog(request.Speaker != null ? request.Speaker.Name : null),
                SafeLog(request.SpeakerRole),
                SafeLog(request.CompanionDisplayName),
                SafeLog(request.AddressedTargetName),
                SafeLog(request.TargetType),
                SafeLog(request.DialogueMode),
                selectedBlockCount,
                SafeLog(request.NearbyCompanions));
        }

        private static int CountSelectedBlocks(string context)
        {
            if (String.IsNullOrWhiteSpace(context))
                return 0;

            int count = 0;
            string[] markers = new[] { "UMG persona:", "UMG relationship:", "UMG party:", "UMG cognition hooks:" };
            for (int i = 0; i < markers.Length; i++)
            {
                int index = 0;
                while ((index = context.IndexOf(markers[i], index, StringComparison.OrdinalIgnoreCase)) >= 0)
                {
                    count++;
                    index += markers[i].Length;
                }
            }

            return count;
        }

        private static string SafeLog(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return String.Empty;

            string text = value.Replace("\r", " ").Replace("\n", " ").Replace("\"", "'");
            return text.Length > 180 ? text.Substring(0, 180) : text;
        }

        private static void PublishDialogueReply(IAIGMCompanionActor trustedCompanion, string reply, string dialogueTargetCompanionId)
        {
            if (trustedCompanion == null || trustedCompanion.Shell == null || trustedCompanion.Shell.Deleted || String.IsNullOrWhiteSpace(reply))
                return;

            Timer.DelayCall(TimeSpan.FromSeconds(1.0), delegate
            {
                if (trustedCompanion == null || trustedCompanion.Shell == null || trustedCompanion.Shell.Deleted)
                    return;

                BaseHire hire = trustedCompanion.Shell as BaseHire;
                if (hire != null)
                    AIGMCompanionDialogueBus.PublishDialogue(hire, reply, dialogueTargetCompanionId);
                else
                    AIGMCompanionSpeechBus.PublishCompanionSpeech(trustedCompanion, reply);
            });
        }

    }
}
