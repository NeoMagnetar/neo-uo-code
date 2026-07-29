using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using Server.Diagnostics;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMBridgeClient
    {
        private static readonly object DiagnosticsLock = new object();
        private static DateTime LastRequestUtc;
        private static int LastStatusCode;
        private static string LastError;
        private static string LastResponseSource;
        private static int TimeoutCount;
        private static int FallbackCount;
        private static int SuccessCount;

        public static AIGMResponse Ask(Mobile from, string question, AIGMTargetInfo target)
        {
            AIGMRequest request = BuildRequest(from, question, target, null);
            return AskInternal(request);
        }

        public static AIGMResponse AskCompanionSpeech(Mobile from, string question, IAIGMCompanionActor companion)
        {
            AIGMRequest request = BuildCompanionSpeechRequest(from, question, companion);
            return AskInternal(request);
        }

        public static AIGMResponse AskCompanionSpeechRequest(AIGMCompanionSpeechRequest request)
        {
            if (request == null)
                return Failure("Companion speech request was null.");

            AIGMRequest bridgeRequest = BuildCompanionSpeechRequest(request);
            return AskInternal(bridgeRequest);
        }

        private static AIGMResponse AskInternal(AIGMRequest request)
        {
            string requestId = request != null ? request.RequestId : "null";
            bool isCompanionSpeech = request != null && String.Equals(request.Mode, "companion_speech", StringComparison.OrdinalIgnoreCase);
            MarkRequestStarted();
            try
            {
                if (isCompanionSpeech)
                {
                    AIGMExecutionLog.Write("AIGM_CHATBOT_REQUEST_START requestId={0} endpoint={1}/aigm/query companion={2} speaker={3} mode={4}",
                        requestId,
                        AIGMSettings.BridgeBaseUrl,
                        SafeExecutionLog(request.CompanionName),
                        SafeExecutionLog(request.SpeakerName),
                        SafeExecutionLog(request.DialogueMode));
                }

                bool preflightOk = IsBridgeReachable();
                if (!preflightOk)
                    Log("Bridge preflight warning: health probe did not confirm readiness; continuing with live request attempt.");

                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(AIGMSettings.BridgeBaseUrl + "/aigm/query");
                webRequest.Method = "POST";
                webRequest.ContentType = "application/json";
                webRequest.Timeout = AIGMSettings.BridgeRequestTimeoutMs;
                webRequest.ReadWriteTimeout = AIGMSettings.BridgeRequestTimeoutMs;

                byte[] bytes = Serialize(request);
                if (isCompanionSpeech)
                {
                    AIGMExecutionLog.Write("AIGM_CHATBOT_REQUEST_PAYLOAD_SUMMARY requestId={0} bytes={1} questionChars={2} memoryChars={3} cognition={4} umgChars={5}",
                        requestId,
                        bytes.Length,
                        request.Question == null ? 0 : request.Question.Length,
                        request.CompanionMemory == null ? 0 : request.CompanionMemory.Length,
                        request.CompanionCognition == null ? "none" : "present",
                        request.CompanionUMGPersonaContext == null ? 0 : request.CompanionUMGPersonaContext.Length);
                }
                if (AIGMSettings.EnableDebugLogging)
                    Log("RequestId=" + (request != null ? request.RequestId : "null") + " Request JSON: " + Encoding.UTF8.GetString(bytes));

                using (Stream requestStream = webRequest.GetRequestStream())
                    requestStream.Write(bytes, 0, bytes.Length);

                using (HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse())
                using (Stream responseStream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(responseStream, Encoding.UTF8))
                {
                    string json = reader.ReadToEnd();
                    LastStatusCode = (int)response.StatusCode;
                    if (isCompanionSpeech)
                        AIGMExecutionLog.Write("AIGM_CHATBOT_HTTP_STATUS requestId={0} status={1} bytes={2}", requestId, (int)response.StatusCode, json == null ? 0 : json.Length);
                    if (AIGMSettings.EnableDebugLogging)
                        Log("RequestId=" + (request != null ? request.RequestId : "null") + " Response JSON: " + json);
                    AIGMResponse parsed = DeserializeJson<AIGMResponse>(json);
                    if (AIGMSettings.EnableDebugLogging)
                        Log("RequestId=" + (request != null ? request.RequestId : "null") + " Parsed response ok=" + (parsed != null ? parsed.Ok.ToString() : "null") + ", replyText=" + (parsed != null ? parsed.ReplyText ?? "null" : "null") + ", error=" + (parsed != null ? parsed.ErrorMessage ?? "null" : "null"));
                    AIGMResponse normalized = Normalize(parsed);
                    string source = GetResponseSource(normalized);
                    MarkResponse(source, normalized != null && normalized.Ok, normalized != null && normalized.FallbackUsed, normalized != null && normalized.Degraded, null);
                    if (isCompanionSpeech)
                    {
                        AIGMExecutionLog.Write("AIGM_CHATBOT_RESPONSE_OK requestId={0} ok={1} source={2} fallbackUsed={3} degraded={4}",
                            requestId,
                            normalized != null && normalized.Ok,
                            SafeExecutionLog(source),
                            normalized != null && normalized.FallbackUsed,
                            normalized != null && normalized.Degraded);
                        if (normalized == null || String.IsNullOrWhiteSpace(normalized.ReplyText))
                            AIGMExecutionLog.Write("AIGM_CHATBOT_RESPONSE_EMPTY requestId={0} source={1}", requestId, SafeExecutionLog(source));
                        AIGMExecutionLog.Write("AIGM_CHATBOT_SOURCE requestId={0} source={1}", requestId, SafeExecutionLog(source));
                    }
                    return normalized;
                }
            }
            catch (WebException ex)
            {
                string details = ex.Message;
                try
                {
                    if (ex.Response != null)
                    {
                        using (Stream responseStream = ex.Response.GetResponseStream())
                        using (StreamReader reader = new StreamReader(responseStream, Encoding.UTF8))
                            details += " | Body: " + reader.ReadToEnd();
                    }
                }
                catch
                {
                }

                Log("WebException: " + details);
                bool timeout = ex.Status == WebExceptionStatus.Timeout || details.IndexOf("timed out", StringComparison.OrdinalIgnoreCase) >= 0;
                MarkResponse("exception", false, true, true, details);
                if (timeout)
                    MarkTimeout();
                if (isCompanionSpeech)
                    AIGMExecutionLog.Write(timeout ? "AIGM_CHATBOT_TIMEOUT requestId={0} error={1}" : "AIGM_CHATBOT_EXCEPTION requestId={0} error={1}", requestId, SafeExecutionLog(details));
                return Failure("Bridge request failed: " + details);
            }
            catch (Exception ex)
            {
                Log("Exception: " + ex);
                MarkResponse("exception", false, true, true, ex.Message);
                if (isCompanionSpeech)
                    AIGMExecutionLog.Write("AIGM_CHATBOT_EXCEPTION requestId={0} error={1}", requestId, SafeExecutionLog(ex.Message));
                return Failure("Bridge error: " + ex.Message);
            }
        }

        public static AIGMResponse ContinueAfterAction(Mobile from, string originalQuestion, AIGMTargetInfo target, string lastActionDescription, string lastActionResult, int stepCount)
        {
            AIGMExecutionContext execution = new AIGMExecutionContext();
            execution.Mode = "continue";
            execution.LastActionDescription = lastActionDescription;
            execution.LastActionResult = lastActionResult;
            execution.StepCount = stepCount;

            AIGMRequest request = BuildRequest(from, originalQuestion, target, execution);
            return AskInternal(request);
        }

        private static AIGMRequest BuildRequest(Mobile from, string question, AIGMTargetInfo target, AIGMExecutionContext execution)
        {
            AIGMSessionState session = from != null ? AIGMSessionState.Get(from) : null;
            AIGMConversationContext conversation = new AIGMConversationContext();
            if (session != null)
            {
                conversation.ActiveTaskSummary = session.ActiveTaskSummary;
                conversation.LastWorldSummary = session.LastWorldSummary;
                if (session.Conversation != null)
                    conversation.RecentTurns.AddRange(session.Conversation);
            }

            return new AIGMRequest
            {
                RequestId = Guid.NewGuid().ToString("N"),
                TimestampUtc = DateTime.UtcNow.ToString("o"),
                ServerDateUtc = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                ServerDateLocal = DateTime.Now.ToString("yyyy-MM-dd"),
                ServerYear = DateTime.Now.Year,
                ShardName = AIGMSettings.BridgeShardName,
                RequesterName = from != null ? from.Name : null,
                AccessLevel = from != null ? from.AccessLevel.ToString() : null,
                MapName = from != null && from.Map != null ? from.Map.Name : null,
                RegionName = from != null && from.Region != null ? from.Region.Name : null,
                Question = question,
                Target = target,
                Scene = from != null ? AIGMSceneScanner.Capture(from, 8) : null,
                Execution = execution,
                Conversation = conversation
            };
        }

        private static AIGMRequest BuildCompanionSpeechRequest(Mobile from, string question, IAIGMCompanionActor companion)
        {
            AIGMTargetInfo target = new AIGMTargetInfo();
            if (companion != null && companion.Shell != null)
            {
                Mobile shell = companion.Shell;
                target.Kind = "companion_speech";
                target.Serial = shell.Serial.Value;
                target.Name = companion.CompanionDisplayName;
                target.TypeName = shell.GetType().FullName;
                target.MapName = shell.Map != null ? shell.Map.Name : null;
                target.RegionName = shell.Region != null ? shell.Region.Name : null;
                target.X = shell.X;
                target.Y = shell.Y;
                target.Z = shell.Z;
                target.Distance = from != null ? (int)Math.Round(from.GetDistanceToSqrt(shell)) : 0;
                target.IsNpc = true;
                target.IsAlive = shell.Alive;
                target.ParentTypeName = shell.GetType().BaseType != null ? shell.GetType().BaseType.FullName : null;
                target.Tags.Add("companion_speech");
                if (!String.IsNullOrWhiteSpace(companion.CompanionId))
                    target.Tags.Add("companion_id:" + companion.CompanionId);
                if (!String.IsNullOrWhiteSpace(companion.CompanionProfileKey))
                    target.Tags.Add("profile:" + companion.CompanionProfileKey);
                if (!String.IsNullOrWhiteSpace(companion.CompanionRole))
                    target.Tags.Add("role:" + companion.CompanionRole);
                if (!String.IsNullOrWhiteSpace(companion.ExecutionModeKey))
                    target.Tags.Add("execution_mode:" + companion.ExecutionModeKey);
            }

            AIGMRequest request = BuildRequest(from, question ?? String.Empty, target, null);
            ApplyCompanionFields(request, companion, null);
            return request;
        }

        private static AIGMRequest BuildCompanionSpeechRequest(AIGMCompanionSpeechRequest speechRequest)
        {
            Mobile from = speechRequest != null ? speechRequest.Speaker : null;
            IAIGMCompanionActor companion = speechRequest != null ? speechRequest.Companion : null;
            AIGMRequest request = BuildCompanionSpeechRequest(from, speechRequest != null ? speechRequest.RawSpeech : String.Empty, companion);
            ApplyCompanionFields(request, companion, speechRequest);
            return request;
        }

        private static void ApplyCompanionFields(AIGMRequest request, IAIGMCompanionActor companion, AIGMCompanionSpeechRequest speechRequest)
        {
            if (request == null)
                return;

            request.Mode = "companion_speech";
            request.DialogueMode = speechRequest != null ? speechRequest.DialogueMode : "owner_or_world_speech";
            request.SpeakerName = speechRequest != null && speechRequest.Speaker != null ? speechRequest.Speaker.Name ?? speechRequest.Speaker.GetType().Name : request.RequesterName;
            request.SpeakerTypeName = speechRequest != null && speechRequest.Speaker != null ? speechRequest.Speaker.GetType().FullName : null;
            request.SpeakerIsCompanion = speechRequest != null && speechRequest.Speaker is BaseHire;
            request.PartyListenerSet = speechRequest != null ? speechRequest.PartyListenerSet : String.Empty;
            request.PartySelectedResponderSet = speechRequest != null ? speechRequest.PartySelectedResponderSet : String.Empty;
            request.PartySuppressedResponderSet = speechRequest != null ? speechRequest.PartySuppressedResponderSet : String.Empty;
            request.TurnCoordinatorDecision = speechRequest != null ? speechRequest.TurnCoordinatorDecision : String.Empty;
            request.StateContextSummary = speechRequest != null ? speechRequest.StateContextSummary : String.Empty;

            if (companion == null)
                return;

            request.CompanionName = companion.CompanionDisplayName;
            request.CompanionTypeName = companion.Shell != null ? companion.Shell.GetType().FullName : null;
            request.CompanionProfileKey = companion.CompanionProfileKey;
            request.CompanionRuntimeIdentity = (companion.CompanionDisplayName ?? companion.CompanionId ?? "companion")
                + ":"
                + (request.CompanionTypeName ?? "unknown")
                + ":"
                + (companion.CompanionProfileKey ?? "default");

            AIGMCompanionPersonaContext persona = AIGMCompanionProfileLibrary.BuildPersonaContext(companion);
            AIGMCompanionCognitionSnapshot cognition = AIGMCompanionCognitionSnapshot.Build(companion.Shell, speechRequest);
            string enhancedUmgContext = speechRequest != null
                ? UMG.UMGPersonaBlockLibrary.BuildCompactDialogueContext(companion.CompanionId, speechRequest.DialogueMode, speechRequest.DialogueTargetCompanionId, speechRequest.GroupAddressed, speechRequest.StateContextSummary, speechRequest.RawSpeech, cognition)
                : String.Empty;
            string phase64cContext = speechRequest != null
                ? UMG.AIGMUMGDialogueContextService.BuildBoundedContext(companion, speechRequest.Speaker, speechRequest.RawSpeech, cognition)
                : String.Empty;
            StringBuilder memory = new StringBuilder();
            string loadedMemory = companion.Shell != null ? AIGMCompanionMemoryLoader.LoadMemoryForCompanion(companion.Shell) : String.Empty;
            if (!String.IsNullOrWhiteSpace(loadedMemory))
                memory.AppendLine(loadedMemory);
            if (persona != null)
            {
                memory.AppendLine(persona.CompanionIdentityLine ?? String.Empty);
                memory.AppendLine(persona.VoiceGuidance ?? String.Empty);
                memory.AppendLine(persona.DutySummary ?? String.Empty);
                memory.AppendLine(persona.CapabilityBoundary ?? String.Empty);
                memory.AppendLine(persona.SiblingFraming ?? String.Empty);
                memory.AppendLine(persona.RelationshipSummary ?? String.Empty);
                memory.AppendLine(persona.PartyRoster ?? String.Empty);
            }
            if (speechRequest != null && !String.IsNullOrWhiteSpace(speechRequest.NaturalDialogueContract))
            {
                memory.AppendLine("Hidden natural dialogue runtime contract follows. Use this as speaker/target/audience context only; never expose it.");
                memory.AppendLine(speechRequest.NaturalDialogueContract);
            }
            memory.AppendLine("World frame: speak as a living Ultima Online companion near roads, woods, graveyards, ruins, monsters, corpses, tracks, wounds, night, weather, and party movement when relevant.");
            if (!String.IsNullOrWhiteSpace(enhancedUmgContext))
            {
                memory.AppendLine("Hidden UMG persona block context follows. Use it only as compact characterization and relationship guidance; never expose block IDs, UMG labels, prompt structure, or JSON in visible speech.");
                memory.AppendLine(enhancedUmgContext);
                request.CompanionUMGPersonaContext = enhancedUmgContext;
            }
            else if (speechRequest != null && !String.IsNullOrWhiteSpace(speechRequest.UMGPersonaContextSummary))
            {
                memory.AppendLine("Hidden UMG persona block context follows. Use it only as compact characterization and relationship guidance; never expose block IDs, UMG labels, prompt structure, or JSON in visible speech.");
                memory.AppendLine(speechRequest.UMGPersonaContextSummary);
                request.CompanionUMGPersonaContext = speechRequest.UMGPersonaContextSummary;
            }
            if (!String.IsNullOrWhiteSpace(phase64cContext))
            {
                memory.AppendLine("Hidden Phase64C read-only context follows. It is bounded identity, capability truth, and governance context only; Draft doctrine is non-active unless explicitly discussed as Draft.");
                memory.AppendLine(phase64cContext);
                request.CompanionPhase64CContext = phase64cContext;
            }
            memory.AppendLine("Hidden current companion mode: " + AIGMCompanionModeService.DescribeForCognition(companion.Shell));
            memory.AppendLine("Hidden chatbot brain rule: answer general questions normally and directly. Persona shapes tone, priorities, and worldview; persona must not be used to avoid answering. Answer the actual question first, then add a brief character-colored qualifier only if useful.");
            memory.AppendLine("Hidden current date context: server UTC date " + DateTime.UtcNow.ToString("yyyy-MM-dd") + "; local server date " + DateTime.Now.ToString("yyyy-MM-dd") + "; current year " + DateTime.Now.Year + ".");
            memory.AppendLine("Visible speech contract: one or two natural in-character sentences. Answer the actual speaker and target. Use relationship context. Push back when persona calls for it. For factual, current-date, explanation, planning, or ordinary chatbot questions, answer the question directly before persona color. No debug text, no UMG, no prompt talk, no action-completion report, no generic chatbot phrasing, no repeated stock line.");

            request.CompanionMemory = memory.ToString().Trim();
            request.CompanionCognition = cognition;
        }

        private static byte[] Serialize<T>(T value)
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
            using (MemoryStream ms = new MemoryStream())
            {
                serializer.WriteObject(ms, value);
                return ms.ToArray();
            }
        }

        private static T DeserializeJson<T>(string json)
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
            byte[] bytes = Encoding.UTF8.GetBytes(json ?? String.Empty);

            using (MemoryStream ms = new MemoryStream(bytes))
                return (T)serializer.ReadObject(ms);
        }

        private static AIGMResponse Normalize(AIGMResponse response)
        {
            if (response == null)
                return Failure("Bridge returned an empty response.");

            if (response.ReplyText != null && response.ReplyText.Length > AIGMSettings.MaxReplyLength)
                response.ReplyText = response.ReplyText.Substring(0, AIGMSettings.MaxReplyLength);

            if (response.LikelyFiles == null)
                response.LikelyFiles = new System.Collections.Generic.List<string>();

            if (response.SuggestedChecks == null)
                response.SuggestedChecks = new System.Collections.Generic.List<string>();

            if (response.Warnings == null)
                response.Warnings = new System.Collections.Generic.List<string>();

            if (response.ProposedActions == null)
                response.ProposedActions = new System.Collections.Generic.List<AIGMActionProposal>();

            if (response.Plan == null)
                response.Plan = new System.Collections.Generic.List<string>();

            if (String.IsNullOrWhiteSpace(response.ResponseSource))
                response.ResponseSource = !String.IsNullOrWhiteSpace(response.Source) ? response.Source : InferSourceFromWarnings(response);
            if (String.IsNullOrWhiteSpace(response.Source))
                response.Source = response.ResponseSource;
            response.FallbackUsed = response.FallbackUsed || IsFallbackSource(response.ResponseSource) || WarningsMentionFallback(response);
            response.Degraded = response.Degraded || response.FallbackUsed;

            return response;
        }

        private static AIGMResponse Failure(string message)
        {
            AIGMResponse response = new AIGMResponse();
            response.Ok = false;
            response.Confidence = "none";
            response.ResponseSource = "exception";
            response.Source = "exception";
            response.FallbackUsed = true;
            response.Degraded = true;
            response.ErrorMessage = message;
            response.ReplyText = "I need a moment to gather that.";
            response.Warnings.Add("Falling back from live bridge call.");
            if (!String.IsNullOrWhiteSpace(message))
                response.Warnings.Add(message);
            return response;
        }

        public static string GetChatbotStatusSummary()
        {
            lock (DiagnosticsLock)
            {
                bool degraded = FallbackCount > 0 && SuccessCount == 0 || IsFallbackSource(LastResponseSource) || String.IsNullOrWhiteSpace(LastResponseSource) || String.Equals(LastResponseSource, "none", StringComparison.OrdinalIgnoreCase);
                TimeSpan successAge = LastRequestUtc == DateTime.MinValue ? TimeSpan.MaxValue : DateTime.UtcNow - LastRequestUtc;
                string diagnosis = BuildBridgeDiagnosis(degraded);
                return String.Format("middlewareEndpoint={0}/aigm/query healthEndpoint={0}/health lastRequest={1} lastRequestAgeSeconds={2} lastStatus={3} lastResponseSource={4} degraded={5} successCount={6} fallbackCount={7} timeoutCount={8} queueTimeoutCount={9} queueBusyCount={10} activeQueueCount={11} activeConversationThreads={12} lastError={13} diagnosis={14}",
                    AIGMSettings.BridgeBaseUrl,
                    LastRequestUtc == DateTime.MinValue ? "never" : LastRequestUtc.ToString("o"),
                    LastRequestUtc == DateTime.MinValue ? "never" : ((int)successAge.TotalSeconds).ToString(),
                    LastStatusCode,
                    String.IsNullOrWhiteSpace(LastResponseSource) ? "none" : LastResponseSource,
                    degraded,
                    SuccessCount,
                    FallbackCount,
                    TimeoutCount,
                    AIGMCompanionSpeechQueue.TotalQueueTimeoutCount,
                    AIGMCompanionSpeechQueue.TotalQueueBusyCount,
                    AIGMCompanionSpeechQueue.ActiveQueueCount,
                    AIGMCompanionDialogueThreadService.ActiveThreadCount,
                    String.IsNullOrWhiteSpace(LastError) ? "none" : LastError,
                    diagnosis);
            }
        }

        public static string GetChatbotHealthProbeSummary()
        {
            string health = ProbeEndpoint("/health");
            string admin = ProbeEndpoint("/admin/status");
            return "health=" + health + " admin=" + admin;
        }

        public static bool IsFallbackOrDegraded(AIGMResponse response)
        {
            if (response == null)
                return true;

            string source = GetResponseSource(response);
            return response.FallbackUsed || response.Degraded || IsFallbackSource(source) || !IsAcceptedLiveDialogueSource(source);
        }

        public static bool IsAcceptedLiveDialogueResponse(AIGMResponse response)
        {
            if (response == null || !response.Ok)
                return false;

            if (response.FallbackUsed || response.Degraded)
                return false;

            return IsAcceptedLiveDialogueSource(GetResponseSource(response));
        }

        public static string GetResponseSource(AIGMResponse response)
        {
            if (response == null)
                return "none";

            if (!String.IsNullOrWhiteSpace(response.ResponseSource))
                return response.ResponseSource.Trim().ToLowerInvariant();

            if (!String.IsNullOrWhiteSpace(response.Source))
                return response.Source.Trim().ToLowerInvariant();

            return InferSourceFromWarnings(response);
        }

        private static void MarkRequestStarted()
        {
            lock (DiagnosticsLock)
                LastRequestUtc = DateTime.UtcNow;
        }

        private static void MarkResponse(string source, bool ok, bool fallbackUsed, bool degraded, string error)
        {
            lock (DiagnosticsLock)
            {
                LastResponseSource = String.IsNullOrWhiteSpace(source) ? "unknown" : source;
                if (!String.IsNullOrWhiteSpace(error))
                    LastError = error;
                else if (ok)
                    LastError = null;

                if (fallbackUsed || degraded || IsFallbackSource(source) || !IsAcceptedLiveDialogueSource(source))
                    FallbackCount++;
                else if (ok)
                    SuccessCount++;
            }
        }

        private static void MarkTimeout()
        {
            lock (DiagnosticsLock)
                TimeoutCount++;
        }

        private static bool IsFallbackSource(string source)
        {
            if (String.IsNullOrWhiteSpace(source))
                return false;

            string normalized = source.Trim().ToLowerInvariant();
            return normalized.Contains("fallback") || normalized.Contains("heuristic") || normalized == "exception";
        }

        private static bool IsAcceptedLiveDialogueSource(string source)
        {
            if (String.IsNullOrWhiteSpace(source))
                return false;

            string normalized = source.Trim().ToLowerInvariant();
            return normalized == "middleware" || normalized == "local_cognition";
        }

        private static string BuildBridgeDiagnosis(bool degraded)
        {
            if (LastRequestUtc == DateTime.MinValue)
                return "no live dialogue request has completed in this server process";

            if (!String.IsNullOrWhiteSpace(LastError))
                return LastError;

            if (String.IsNullOrWhiteSpace(LastResponseSource) || String.Equals(LastResponseSource, "none", StringComparison.OrdinalIgnoreCase))
                return "last response was null before middleware completed; check speech queue timeout and middleware latency";

            if (IsFallbackSource(LastResponseSource))
                return "last response source was fallback/degraded and was suppressed";

            if (degraded)
                return "bridge has degraded history in this process; latest accepted source may still be healthy";

            if (IsAcceptedLiveDialogueSource(LastResponseSource))
                return "healthy";

            return "last response source was not accepted for live companion dialogue";
        }

        private static string ProbeEndpoint(string path)
        {
            try
            {
                HttpWebRequest probe = (HttpWebRequest)WebRequest.Create(AIGMSettings.BridgeBaseUrl + path);
                probe.Method = "GET";
                probe.Timeout = AIGMSettings.BridgeHealthTimeoutMs;
                probe.ReadWriteTimeout = AIGMSettings.BridgeHealthTimeoutMs;

                using (HttpWebResponse response = (HttpWebResponse)probe.GetResponse())
                using (Stream responseStream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(responseStream, Encoding.UTF8))
                {
                    string body = reader.ReadToEnd();
                    if (body != null && body.Length > 360)
                        body = body.Substring(0, 360);
                    return "ok status=" + (int)response.StatusCode + " body=" + SafeExecutionLog(body);
                }
            }
            catch (Exception ex)
            {
                return "unreachable error=" + SafeExecutionLog(ex.Message);
            }
        }

        private static string InferSourceFromWarnings(AIGMResponse response)
        {
            if (response == null || response.Warnings == null)
                return "unknown";

            for (int i = 0; i < response.Warnings.Count; i++)
            {
                string warning = response.Warnings[i] ?? String.Empty;
                if (warning.IndexOf("OpenClaw-backed", StringComparison.OrdinalIgnoreCase) >= 0)
                    return warning.IndexOf("failed", StringComparison.OrdinalIgnoreCase) >= 0 || warning.IndexOf("fallback", StringComparison.OrdinalIgnoreCase) >= 0 ? "fallback" : "middleware";
                if (warning.IndexOf("Local cognition", StringComparison.OrdinalIgnoreCase) >= 0)
                    return "local_cognition";
                if (warning.IndexOf("heuristic", StringComparison.OrdinalIgnoreCase) >= 0 || warning.IndexOf("fallback", StringComparison.OrdinalIgnoreCase) >= 0)
                    return "fallback";
            }

            return "unknown";
        }

        private static bool WarningsMentionFallback(AIGMResponse response)
        {
            if (response == null || response.Warnings == null)
                return false;

            for (int i = 0; i < response.Warnings.Count; i++)
            {
                string warning = response.Warnings[i] ?? String.Empty;
                if (warning.IndexOf("fallback", StringComparison.OrdinalIgnoreCase) >= 0 || warning.IndexOf("heuristic", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        private static string SafeExecutionLog(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return String.Empty;

            value = value.Replace("\r", " ").Replace("\n", " ").Replace("\"", "'");
            return value.Length > 160 ? value.Substring(0, 160) : value;
        }

        private static bool IsBridgeReachable()
        {
            try
            {
                HttpWebRequest probe = (HttpWebRequest)WebRequest.Create(AIGMSettings.BridgeBaseUrl + "/health");
                probe.Method = "GET";
                probe.Timeout = AIGMSettings.BridgeHealthTimeoutMs;
                probe.ReadWriteTimeout = AIGMSettings.BridgeHealthTimeoutMs;

                using (HttpWebResponse response = (HttpWebResponse)probe.GetResponse())
                    return response != null && (int)response.StatusCode >= 200 && (int)response.StatusCode < 300;
            }
            catch (Exception ex)
            {
                Log("Bridge health probe exception: " + ex.Message);
                return false;
            }
        }

        private static void Log(string message)
        {
            try
            {
                string path = Path.Combine(Core.BaseDirectory, "Logs", "AIGMBridgeClient.log");
                File.AppendAllText(path, DateTime.UtcNow.ToString("o") + " " + (message ?? String.Empty) + Environment.NewLine);
            }
            catch
            {
            }
        }
    }
}
