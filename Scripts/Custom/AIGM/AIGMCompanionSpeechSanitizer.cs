using System;
using System.Text.RegularExpressions;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionSpeechSanitizer
    {
        private const int MaxSpeechLength = 180;

        public static string ForNpcSpeech(string text)
        {
            if (String.IsNullOrWhiteSpace(text))
                return "I am here.";

            text = text.Replace("<br>", " ").Replace("<br/>", " ").Replace("<br />", " ");
            text = Regex.Replace(text, "<.*?>", String.Empty);
            text = text.Replace("\r", " ").Replace("\n", " ");
            text = Regex.Replace(text, "\\s+", " ").Trim();

            if (text.Length > MaxSpeechLength)
                text = text.Substring(0, MaxSpeechLength).TrimEnd() + "...";

            return text;
        }
    }
}
