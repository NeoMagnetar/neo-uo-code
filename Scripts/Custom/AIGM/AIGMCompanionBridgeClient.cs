using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionBridgeClient
    {
        public static async Task<AIGMCompanionSpeechResult> AskAsync(AIGMCompanionSpeechRequest request, CancellationToken token)
        {
            if (request == null)
                return AIGMCompanionSpeechResult.Fail(null, "Missing request.");

            DateTime started = DateTime.UtcNow;

            try
            {
                string json = BuildJson(request);
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                AIGMExecutionLog.Write("COMPANION_HTTP_START requestId={0} mode={1} speaker={2} companion={3}", request.RequestId, request.DialogueMode, request.SpeakerName, request.CompanionName);

                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create("http://127.0.0.1:4876/aigm/query");
                webRequest.Method = "POST";
                webRequest.ContentType = "application/json";
                webRequest.Timeout = 30000;
                webRequest.ReadWriteTimeout = 30000;
                webRequest.ContentLength = bytes.Length;

                using (token.Register(delegate { try { webRequest.Abort(); } catch { } }))
                {
                    using (Stream requestStream = await Task<Stream>.Factory.FromAsync(webRequest.BeginGetRequestStream, webRequest.EndGetRequestStream, null).ConfigureAwait(false))
                    {
                        await requestStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
                    }

                    using (WebResponse response = await Task<WebResponse>.Factory.FromAsync(webRequest.BeginGetResponse, webRequest.EndGetResponse, null).ConfigureAwait(false))
                    using (Stream responseStream = response.GetResponseStream())
                    using (StreamReader reader = new StreamReader(responseStream))
                    {
                        string body = await reader.ReadToEndAsync().ConfigureAwait(false);
                        HttpWebResponse httpResponse = response as HttpWebResponse;
                        int statusCode = httpResponse != null ? (int)httpResponse.StatusCode : 200;
                        double elapsedMs = (DateTime.UtcNow - started).TotalMilliseconds;
                        AIGMExecutionLog.Write("COMPANION_HTTP_DONE requestId={0} status={1} bytes={2} elapsedMs={3}", request.RequestId, statusCode, body == null ? 0 : body.Length, elapsedMs);

                        if (statusCode < 200 || statusCode >= 300)
                            return AIGMCompanionSpeechResult.Fail(request.RequestId, "Middleware returned HTTP " + statusCode);

                        string reply = AIGMCompanionResponseParser.ExtractReplyText(body);
                        if (String.IsNullOrWhiteSpace(reply))
                            reply = "I am here, but the thought came back empty.";

                        return AIGMCompanionSpeechResult.Success(request.RequestId, reply);
                    }
                }
            }
            catch (WebException ex)
            {
                if (token.IsCancellationRequested)
                {
                    AIGMExecutionLog.Write("COMPANION_HTTP_TIMEOUT requestId={0}", request.RequestId);
                    return AIGMCompanionSpeechResult.Fail(request.RequestId, "Timed out.");
                }

                AIGMExecutionLog.Write("COMPANION_HTTP_FAIL requestId={0} error={1}", request.RequestId, ex);
                return AIGMCompanionSpeechResult.Fail(request.RequestId, ex.Message);
            }
            catch (Exception ex)
            {
                AIGMExecutionLog.Write("COMPANION_HTTP_FAIL requestId={0} error={1}", request.RequestId, ex);
                return AIGMCompanionSpeechResult.Fail(request.RequestId, ex.Message);
            }
        }

        private static string BuildJson(AIGMCompanionSpeechRequest request)
        {
            return "{" +
                "\"requestId\":\"" + EscapeJson(request.RequestId) + "\"," +
                "\"mode\":\"companion_speech\"," +
                "\"Question\":\"" + EscapeJson(request.Text) + "\"," +
                "\"question\":\"" + EscapeJson(request.Text) + "\"," +
                "\"SpeakerName\":\"" + EscapeJson(request.SpeakerName) + "\"," +
                "\"SpeakerTypeName\":\"" + EscapeJson(request.SpeakerTypeName) + "\"," +
                "\"SpeakerIsCompanion\":" + (request.SpeakerIsCompanion ? "true" : "false") + "," +
                "\"DialogueMode\":\"" + EscapeJson(request.DialogueMode) + "\"," +
                "\"CompanionName\":\"" + EscapeJson(request.CompanionName) + "\"," +
                "\"CompanionTypeName\":\"" + EscapeJson(request.CompanionTypeName) + "\"," +
                "\"CompanionProfileKey\":\"" + EscapeJson(request.CompanionProfileKey) + "\"," +
                "\"CompanionRuntimeIdentity\":\"" + EscapeJson(request.CompanionName + ":" + request.CompanionTypeName + ":" + request.CompanionProfileKey) + "\"," +
                "\"MapName\":\"" + EscapeJson(request.MapName) + "\"," +
                "\"RegionName\":\"" + EscapeJson(request.RegionName) + "\"," +
                "\"CompanionMemory\":\"" + EscapeJson(LoadCompanionMemory(request.CompanionSerial)) + "\"," +
                "\"X\":" + request.X + "," +
                "\"Y\":" + request.Y + "," +
                "\"Z\":" + request.Z +
                "}";
        }

        private static string LoadCompanionMemory(Serial companionSerial)
        {
            try
            {
                Mobile companion = World.FindMobile(companionSerial);
                return AIGMCompanionMemoryLoader.LoadMemoryForCompanion(companion);
            }
            catch
            {
                return String.Empty;
            }
        }

        private static string EscapeJson(string value)
        {
            if (value == null)
                return String.Empty;

            return value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
        }
    }
}
