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

            if (ContainsMechanicalSpeech(text))
                return "I am here. Speak plainly.";

            if (text.Length > MaxSpeechLength)
                text = text.Substring(0, MaxSpeechLength).TrimEnd() + "...";

            return text;
        }

        private static bool ContainsMechanicalSpeech(string text)
        {
            if (String.IsNullOrWhiteSpace(text))
                return false;

            string lower = text.ToLowerInvariant();
            return lower.Contains("i have greeted ")
                || lower.Contains(" has greeted ")
                || lower.Contains("acknowledges stop")
                || lower.Contains("holds position")
                || lower.Contains("action lane is deferred")
                || lower.Contains("deferred in this phase")
                || lower.Contains("advanced action deferred")
                || lower.Contains("command recognized");
        }
    }
}
