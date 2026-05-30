using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using Server.Diagnostics;

namespace Server.Custom.AIGM
{
    public static class AIGMBridgeClient
    {
        public static AIGMResponse Ask(Mobile from, string question, AIGMTargetInfo target)
        {
            AIGMRequest request = BuildRequest(from, question, target, null);
            return AskInternal(request);
        }

        private static AIGMResponse AskInternal(AIGMRequest request)
        {
            try
            {
                Log("AIGMBridgeClient PATCH MARKER v2 hit RequestId=" + (request != null ? request.RequestId : "null"));
                bool preflightOk = IsBridgeReachable();
                if (!preflightOk)
                    Log("Bridge preflight warning: localhost:4876 health probe did not confirm readiness; continuing with live request attempt.");

                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create("http://127.0.0.1:4876/aigm/query");
                webRequest.Method = "POST";
                webRequest.ContentType = "application/json";
                webRequest.Timeout = 8000;
                webRequest.ReadWriteTimeout = 8000;

                byte[] bytes = Serialize(request);
                Log("RequestId=" + (request != null ? request.RequestId : "null") + " Request JSON: " + Encoding.UTF8.GetString(bytes));

                using (Stream requestStream = webRequest.GetRequestStream())
                    requestStream.Write(bytes, 0, bytes.Length);

                using (HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse())
                using (Stream responseStream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(responseStream, Encoding.UTF8))
                {
                    string json = reader.ReadToEnd();
                    Log("RequestId=" + (request != null ? request.RequestId : "null") + " Response JSON: " + json);
                    AIGMResponse parsed = DeserializeJson<AIGMResponse>(json);
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
                ShardName = "NeoUO-Dev",
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
            response.ReplyText = "The archives could not complete the live bridge call.";
            response.Warnings.Add("Falling back from live bridge call.");
            if (!String.IsNullOrWhiteSpace(message))
                response.Warnings.Add(message);
            return response;
        }

        private static bool IsBridgeReachable()
        {
            try
            {
                HttpWebRequest probe = (HttpWebRequest)WebRequest.Create("http://127.0.0.1:4876/health");
                probe.Method = "GET";
                probe.Timeout = 1200;
                probe.ReadWriteTimeout = 1200;

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
