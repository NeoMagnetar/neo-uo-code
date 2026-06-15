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

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("[companion_speech_request]");
            sb.AppendLine("mode: " + (request.DialogueMode ?? "owner_or_world_speech"));
            sb.AppendLine("visible_turn: " + (request.IsPrimaryVisibleTurn ? "true" : "false"));
            sb.AppendLine("active_companion: " + (request.CompanionDisplayName ?? request.CompanionId ?? "unknown"));
            sb.AppendLine("active_profile: " + (request.CompanionProfileKey ?? "unknown"));
            sb.AppendLine("active_role: " + (request.CompanionRole ?? "unknown"));
            sb.AppendLine("speaker: " + (request.Speaker != null ? request.Speaker.Name ?? request.Speaker.GetType().Name : "unknown"));
            sb.AppendLine("speaker_kind: " + (request.Speaker is BaseHire ? "companion" : "owner"));
            sb.AppendLine("allow_remote_relay: " + (request.AllowRemoteRelay ? "true" : "false"));
            sb.AppendLine("event_id: " + (request.EventId.HasValue ? request.EventId.Value.ToString() : String.Empty));
            sb.AppendLine("origin_companion_id: " + (request.OriginCompanionId ?? String.Empty));
            sb.AppendLine("hop_count: " + request.HopCount);
            sb.AppendLine("speech: " + (request.RawSpeech ?? String.Empty));
            sb.AppendLine("owner_group_context: " + (request.OwnerGroupContext ?? String.Empty));
            sb.AppendLine("listener_set: " + (request.PartyListenerSet ?? String.Empty));
            sb.AppendLine("selected_responders: " + (request.PartySelectedResponderSet ?? String.Empty));
            sb.AppendLine("suppressed_responders: " + (request.PartySuppressedResponderSet ?? String.Empty));
            sb.AppendLine("suppressed_reasons: " + (request.PartySuppressedResponderReasons ?? String.Empty));
            sb.AppendLine("turn_coordinator_decision: " + (request.TurnCoordinatorDecision ?? String.Empty));
            sb.AppendLine("parsed_intent: " + (request.ParsedIntentSummary ?? String.Empty));
            sb.AppendLine("state_context: " + (request.StateContextSummary ?? String.Empty));
            sb.AppendLine("capability_safety_posture: " + (request.CapabilitySafetyPosture ?? String.Empty));
            sb.AppendLine();
            sb.AppendLine("[active_companion_identity]");
            sb.AppendLine(request.CompanionDescription ?? String.Empty);
            sb.AppendLine("role_profile: " + (request.CompanionRole ?? String.Empty));
            sb.AppendLine("voice_guidance: " + (request.CompanionVoiceGuidance ?? String.Empty));
            sb.AppendLine("duties: " + (request.CompanionDutySummary ?? String.Empty));
            sb.AppendLine("capability_boundary: " + (request.CompanionCapabilityBoundary ?? String.Empty));
            sb.AppendLine("sibling_context: " + (request.CompanionSiblingContext ?? String.Empty));
            sb.AppendLine("companion_party: " + (request.CompanionPartyRoster ?? String.Empty));
            sb.AppendLine("[/active_companion_identity]");
            sb.AppendLine();
            sb.AppendLine("[behavior_rules]");
            sb.AppendLine("Speak as the active companion only.");
            sb.AppendLine("Do not speak as another companion.");
            sb.AppendLine("Use listener_set, selected_responders, suppressed_reasons, and state_context as live party context.");
            sb.AppendLine("If this companion is selected, answer in character using its role and current state.");
            sb.AppendLine("Treat sibling companions as real companions, not scenery.");
            sb.AppendLine("If asked about another companion, use dialogue context if present.");
            sb.AppendLine("If no relevant linked dialogue context exists, say so naturally in character.");
            sb.AppendLine("Do not summarize the relay, queue, bus, bridge, archives, or technical system behavior.");
            sb.AppendLine("Do not claim to execute gated actions.");
            sb.AppendLine("Do not fake healing, cure, bandage, travel, tracking pursuit, movement, attack, or combat execution.");
            sb.AppendLine("For gated actions, give a brief in-character deferral matching this companion's role.");
            sb.AppendLine("For direct named commands, only the addressed companion may visibly answer or execute; others are context-only.");
            sb.AppendLine("For group conversation, keep the answer short and distinct from sibling companions.");
            sb.AppendLine("For companion_dialogue, allow one natural follow-up only and do not continue the chain.");
            sb.AppendLine("Keep the reply short, clear, and suitable for Ultima Online journal readability.");
            sb.AppendLine("If visible_turn is false, this request should not produce visible speech.");
            sb.AppendLine("[/behavior_rules]");
            sb.AppendLine("[/companion_speech_request]");

            return AskCompanionSpeech(request.Speaker, sb.ToString(), request.Companion);
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

            string companionQuestion = question ?? String.Empty;
            if (companion != null)
            {
                AIGMCompanionPersonaContext persona = AIGMCompanionProfileLibrary.BuildPersonaContext(companion);
                companionQuestion = "[mode:companion_speech]\n"
                    + "[companion_id:" + (companion.CompanionId ?? String.Empty) + "]\n"
                    + "[companion_name:" + (companion.CompanionDisplayName ?? String.Empty) + "]\n"
                    + "[companion_role:" + (companion.CompanionRole ?? String.Empty) + "]\n"
                    + "[companion_profile:" + (companion.CompanionProfileKey ?? String.Empty) + "]\n"
                    + "[persona_identity:" + (persona != null ? persona.CompanionIdentityLine ?? String.Empty : String.Empty) + "]\n"
                    + "[persona_voice:" + (persona != null ? persona.VoiceGuidance ?? String.Empty : String.Empty) + "]\n"
                    + "[persona_duties:" + (persona != null ? persona.DutySummary ?? String.Empty : String.Empty) + "]\n"
                    + "[persona_boundary:" + (persona != null ? persona.CapabilityBoundary ?? String.Empty : String.Empty) + "]\n"
                    + "[persona_siblings:" + (persona != null ? persona.SiblingFraming ?? String.Empty : String.Empty) + "]\n"
                    + "[party_roster:" + (persona != null ? persona.PartyRoster ?? String.Empty : String.Empty) + "]\n"
                    + question;
            }

            return BuildRequest(from, companionQuestion, target, null);
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
