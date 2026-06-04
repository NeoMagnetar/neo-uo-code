using System;
using System.Text.RegularExpressions;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionResponseParser
    {
        public static string ExtractReplyText(string json)
        {
            if (String.IsNullOrWhiteSpace(json))
                return null;

            string reply =
                ExtractJsonString(json, "replyText") ??
                ExtractJsonString(json, "ReplyText") ??
                ExtractJsonString(json, "message") ??
                ExtractJsonString(json, "Message");

            return DecodeJsonString(reply);
        }

        private static string ExtractJsonString(string json, string key)
        {
            string pattern = "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"(?<v>(?:\\\\.|[^\"])*)\"";
            Match match = Regex.Match(json, pattern);
            if (!match.Success)
                return null;

            return match.Groups["v"].Value;
        }

        private static string DecodeJsonString(string value)
        {
            if (value == null)
                return null;

            return value
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\")
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t");
        }
    }
}
