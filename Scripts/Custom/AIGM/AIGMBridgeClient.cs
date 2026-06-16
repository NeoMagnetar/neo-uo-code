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
            try
            {
                bool preflightOk = IsBridgeReachable();
                if (!preflightOk)
                    Log("Bridge preflight warning: health probe did not confirm readiness; continuing with live request attempt.");

                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(AIGMSettings.BridgeBaseUrl + "/aigm/query");
                webRequest.Method = "POST";
                webRequest.ContentType = "application/json";
                webRequest.Timeout = AIGMSettings.BridgeRequestTimeoutMs;
                webRequest.ReadWriteTimeout = AIGMSettings.BridgeRequestTimeoutMs;

                byte[] bytes = Serialize(request);
                if (AIGMSettings.EnableDebugLogging)
                    Log("RequestId=" + (request != null ? request.RequestId : "null") + " Request JSON: " + Encoding.UTF8.GetString(bytes));

                using (Stream requestStream = webRequest.GetRequestStream())
                    requestStream.Write(bytes, 0, bytes.Length);

                using (HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse())
                using (Stream responseStream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(responseStream, Encoding.UTF8))
                {
                    string json = reader.ReadToEnd();
                    if (AIGMSettings.EnableDebugLogging)
                        Log("RequestId=" + (request != null ? request.RequestId : "null") + " Response JSON: " + json);
                    AIGMResponse parsed = DeserializeJson<AIGMResponse>(json);
                    if (AIGMSettings.EnableDebugLogging)
                        Log("RequestId=" + (request != null ? request.RequestId : "null") + " Parsed response ok=" + (parsed != null ? parsed.Ok.ToString() : "null") + ", replyText=" + (parsed != null ? parsed.ReplyText ?? "null" : "null") + ", error=" + (parsed != null ? parsed.ErrorMessage ?? "null" : "null"));
                    return Normalize(parsed);
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
                return Failure("Bridge request failed: " + details);
            }
            catch (Exception ex)
            {
                Log("Exception: " + ex);
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
                memory.AppendLine(persona.PartyRoster ?? String.Empty);
            }
            memory.AppendLine("World frame: speak as a living Ultima Online companion near roads, woods, graveyards, ruins, monsters, corpses, tracks, wounds, night, weather, and party movement when relevant.");
            memory.AppendLine("Visible speech contract: one or two short in-character sentences only. No debug text, no UMG, no prompt talk, no action-completion report, no generic chatbot phrasing.");

            request.CompanionMemory = memory.ToString().Trim();
            request.CompanionCognition = AIGMCompanionCognitionSnapshot.Build(companion.Shell, speechRequest);
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

            return response;
        }

        private static AIGMResponse Failure(string message)
        {
            AIGMResponse response = new AIGMResponse();
            response.Ok = false;
            response.Confidence = "none";
            response.ErrorMessage = message;
            response.ReplyText = "I need a moment to gather that.";
            response.Warnings.Add("Falling back from live bridge call.");
            if (!String.IsNullOrWhiteSpace(message))
                response.Warnings.Add(message);
            return response;
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
