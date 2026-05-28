using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Server.Custom.AIGM
{
    public static class AIGMBridgeClient
    {
        public static AIGMResponse Ask(Mobile from, string question, AIGMTargetInfo target)
        {
            AIGMRequest request = BuildRequest(from, question, target);

            try
            {
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create("http://127.0.0.1:4876/aigm/query");
                webRequest.Method = "POST";
                webRequest.ContentType = "application/json";
                webRequest.Timeout = 8000;
                webRequest.ReadWriteTimeout = 8000;

                byte[] bytes = Serialize(request);

                using (Stream requestStream = webRequest.GetRequestStream())
                    requestStream.Write(bytes, 0, bytes.Length);

                using (HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse())
                using (Stream responseStream = response.GetResponseStream())
                {
                    AIGMResponse parsed = Deserialize<AIGMResponse>(responseStream);
                    return Normalize(parsed);
                }
            }
            catch (WebException ex)
            {
                return Failure("Bridge request failed: " + ex.Message);
            }
            catch (Exception ex)
            {
                return Failure("Bridge error: " + ex.Message);
            }
        }

        private static AIGMRequest BuildRequest(Mobile from, string question, AIGMTargetInfo target)
        {
            return new AIGMRequest
            {
                RequestId = Guid.NewGuid().ToString("N"),
                TimestampUtc = DateTime.UtcNow.ToString("o"),
                ShardName = "NeoUO-Staging",
                RequesterName = from != null ? from.Name : null,
                AccessLevel = from != null ? from.AccessLevel.ToString() : null,
                MapName = from != null && from.Map != null ? from.Map.Name : null,
                RegionName = from != null && from.Region != null ? from.Region.Name : null,
                Question = question,
                Target = target,
                Scene = from != null ? AIGMSceneScanner.Capture(from, 8) : null
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

        private static T Deserialize<T>(Stream stream)
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
            return (T)serializer.ReadObject(stream);
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

            return response;
        }

        private static AIGMResponse Failure(string message)
        {
            AIGMResponse response = new AIGMResponse();
            response.Ok = false;
            response.Confidence = "none";
            response.ErrorMessage = message;
            response.ReplyText = "The archives could not reach the local AI bridge.";
            response.Warnings.Add("Falling back from live bridge call.");
            return response;
        }
    }
}
