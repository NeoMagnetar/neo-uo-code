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
                return "Stay close. I am still reading this place.";

            text = text.Replace("<br>", " ").Replace("<br/>", " ").Replace("<br />", " ");
            text = Regex.Replace(text, "<.*?>", String.Empty);
            text = text.Replace("\r", " ").Replace("\n", " ");
            text = Regex.Replace(text, "\\s+", " ").Trim();
            text = StripMechanicalFragments(text);

            if (ContainsMechanicalSpeech(text))
                return "Stay close. I am still reading this place.";

            if (text.Length > MaxSpeechLength)
                text = text.Substring(0, MaxSpeechLength).TrimEnd() + "...";

            return text;
        }

        private static string StripMechanicalFragments(string text)
        {
            if (String.IsNullOrWhiteSpace(text))
                return text;

            text = Regex.Replace(text, "\\b(?:We are |I am |This is |It is )?[^.!?]*\\b(?:Felucca|Trammel|Ilshenar|Malas|Tokuno|Ter Mur)[^.!?]*\\b-?\\d+\\s*,\\s*-?\\d+\\s*,\\s*-?\\d+\\.?\\s*", String.Empty, RegexOptions.IgnoreCase);
            text = Regex.Replace(text, "\\bspeaker distance\\s+\\d+\\b", String.Empty, RegexOptions.IgnoreCase);
            text = Regex.Replace(text, "\\bcompanions close:\\s*[^.!?;]+[.!?;]?", String.Empty, RegexOptions.IgnoreCase);
            text = Regex.Replace(text, "\\s+([,.!?;:])", "$1");
            text = Regex.Replace(text, "^[\\s,.;:!?]+", String.Empty);
            text = Regex.Replace(text, "\\s+", " ").Trim();

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
                || lower.Contains("command recognized")
                || lower.Contains("as an ai")
                || lower.Contains("json object")
                || lower.Contains("json")
                || lower.Contains("prompt")
                || lower.Contains("metadata")
                || lower.Contains("debug")
                || lower.Contains("umg")
                || lower.Contains("molt")
                || lower.Contains("neoblock")
                || lower.Contains("openclaw")
                || lower.Contains("middleware")
                || lower.Contains("capability gate")
                || lower.Contains("hidden context");
        }
    }
}
