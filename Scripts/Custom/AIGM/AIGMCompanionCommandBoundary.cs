using System;
using System.Collections.Generic;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionCommandBoundary
    {
        private sealed class CompanionAliasEntry
        {
            public string Key;
            public string Name;
            public string[] Aliases;
        }

        private static readonly CompanionAliasEntry[] CompanionAliases =
        {
            new CompanionAliasEntry
            {
                Key = "dakeyras",
                Name = "Dakeyras",
                Aliases = new[] { "dak", "dakeyras" }
            },
            new CompanionAliasEntry
            {
                Key = "danyal",
                Name = "Danyal",
                Aliases = new[] { "danyal", "dan" }
            },
            new CompanionAliasEntry
            {
                Key = "dardalion",
                Name = "Dardalion",
                Aliases = new[] { "dar", "dardalion" }
            }
        };

        private static readonly string[] KnownSharedLeadWords =
        {
            "follow",
            "come",
            "guard",
            "stay",
            "hold",
            "stop",
            "wait",
            "track",
            "scan",
            "report"
        };

        public static List<string> GetAddressedCompanionIds(string speech)
        {
            List<string> ids = new List<string>();
            string normalized = NormalizeSpeech(speech);
            if (String.IsNullOrWhiteSpace(normalized))
                return ids;

            string[] words = normalized.Split(' ');
            foreach (CompanionAliasEntry entry in CompanionAliases)
            {
                if (entry == null || entry.Aliases == null)
                    continue;

                bool matched = false;
                foreach (string alias in entry.Aliases)
                {
                    for (int i = 0; i < words.Length; i++)
                    {
                        if (String.Equals(words[i], alias, StringComparison.Ordinal))
                        {
                            matched = true;
                            break;
                        }
                    }

                    if (matched)
                        break;
                }

                if (matched)
                    ids.Add(entry.Key);
            }

            return ids;
        }

        public static AIGMCompanionCommandRouteDecision Classify(string speech)
        {
            AIGMCompanionCommandRouteDecision decision = new AIGMCompanionCommandRouteDecision();
            decision.OriginalSpeech = speech ?? String.Empty;
            decision.NormalizedSpeech = NormalizeSpeech(speech);
            decision.IsExecutableNow = false;

            if (String.IsNullOrWhiteSpace(decision.NormalizedSpeech))
            {
                decision.RouteKind = AIGMCompanionCommandRouteKind.EmptySpeech;
                decision.Reason = "speech_empty";
                return decision;
            }

            string payload;
            CompanionAliasEntry namedAlias;
            if (TryMatchNamedAlias(decision.NormalizedSpeech, out namedAlias, out payload))
            {
                decision.CompanionKey = namedAlias.Key;
                decision.CompanionName = namedAlias.Name;

                if (TryRecognizeVerb(payload, out var verbKind, out var verbText))
                {
                    decision.IsCompanionCommand = true;
                    decision.BlocksCounselorLane = true;
                    decision.IsNamedCompanionCommand = true;
                    decision.IsSharedCompanionCommand = false;
                    decision.CommandVerb = verbText;
                    decision.VerbKind = verbKind;
                    decision.RouteKind = AIGMCompanionCommandRouteKind.NamedCompanion;
                    decision.Reason = "companion_named_command";
                    return decision;
                }

                decision.RouteKind = AIGMCompanionCommandRouteKind.NonCompanion;
                decision.Reason = "not_companion_command";
                return decision;
            }

            string firstWord = GetFirstWord(decision.NormalizedSpeech);
            if (IsUnknownCompanionAlias(firstWord) && StartsWithKnownSharedLeadWord(GetRemainderAfterFirstWord(decision.NormalizedSpeech)))
            {
                decision.RouteKind = AIGMCompanionCommandRouteKind.UnknownCompanionAlias;
                decision.Reason = "companion_alias_unknown";
                decision.BlocksCounselorLane = true;
                return decision;
            }

            if (TryRecognizeVerb(decision.NormalizedSpeech, out var sharedVerbKind, out var sharedVerbText))
            {
                decision.IsCompanionCommand = true;
                decision.BlocksCounselorLane = true;
                decision.IsNamedCompanionCommand = false;
                decision.IsSharedCompanionCommand = true;
                decision.CommandVerb = sharedVerbText;
                decision.VerbKind = sharedVerbKind;
                decision.RouteKind = AIGMCompanionCommandRouteKind.SharedCompanion;
                decision.Reason = "companion_shared_command";
                return decision;
            }

            decision.RouteKind = AIGMCompanionCommandRouteKind.NonCompanion;
            decision.Reason = "not_companion_command";
            return decision;
        }

        private static bool TryMatchNamedAlias(string normalizedSpeech, out CompanionAliasEntry matchedAlias, out string payload)
        {
            matchedAlias = null;
            payload = String.Empty;

            if (String.IsNullOrWhiteSpace(normalizedSpeech))
                return false;

            foreach (CompanionAliasEntry entry in CompanionAliases)
            {
                if (entry == null || entry.Aliases == null)
                    continue;

                foreach (string alias in entry.Aliases)
                {
                    if (String.IsNullOrWhiteSpace(alias))
                        continue;

                    if (normalizedSpeech.Equals(alias, StringComparison.Ordinal))
                    {
                        matchedAlias = entry;
                        payload = String.Empty;
                        return true;
                    }

                    if (normalizedSpeech.StartsWith(alias + " ", StringComparison.Ordinal))
                    {
                        matchedAlias = entry;
                        payload = normalizedSpeech.Substring(alias.Length).Trim();
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TryRecognizeVerb(string normalizedSpeech, out AIGMCompanionCommandVerbKind verbKind, out string verbText)
        {
            verbKind = AIGMCompanionCommandVerbKind.None;
            verbText = null;

            if (String.IsNullOrWhiteSpace(normalizedSpeech))
                return false;

            string speech = normalizedSpeech.Trim();

            if (speech.Equals("follow") || speech.Equals("follow me"))
                return Match(AIGMCompanionCommandVerbKind.Follow, "follow", out verbKind, out verbText);

            if (speech.Equals("come") || speech.Equals("come here") || speech.Equals("come to me"))
                return Match(AIGMCompanionCommandVerbKind.Come, "come", out verbKind, out verbText);

            if (speech.Equals("guard me") || speech.Equals("protect me") || speech.Equals("defend me"))
                return Match(AIGMCompanionCommandVerbKind.Guard, "guard", out verbKind, out verbText);

            if (speech.Equals("stay") || speech.Equals("stay here"))
                return Match(AIGMCompanionCommandVerbKind.Stay, "stay", out verbKind, out verbText);

            if (speech.Equals("hold") || speech.Equals("hold position") || speech.Equals("hold here"))
                return Match(AIGMCompanionCommandVerbKind.Hold, "hold", out verbKind, out verbText);

            if (speech.Equals("stop"))
                return Match(AIGMCompanionCommandVerbKind.Stop, "stop", out verbKind, out verbText);

            if (speech.Equals("wait"))
                return Match(AIGMCompanionCommandVerbKind.Wait, "wait", out verbKind, out verbText);

            if (speech.Equals("track") || speech.Equals("track around"))
                return Match(AIGMCompanionCommandVerbKind.Track, "track", out verbKind, out verbText);

            if (speech.Equals("scan") || speech.Equals("scan the area") || speech.Equals("scan area"))
                return Match(AIGMCompanionCommandVerbKind.Scan, "scan", out verbKind, out verbText);

            if (speech.Equals("report") || speech.Equals("report status"))
                return Match(AIGMCompanionCommandVerbKind.Report, "report", out verbKind, out verbText);

            return false;
        }

        private static bool Match(AIGMCompanionCommandVerbKind kind, string text, out AIGMCompanionCommandVerbKind verbKind, out string verbText)
        {
            verbKind = kind;
            verbText = text;
            return true;
        }

        private static bool IsUnknownCompanionAlias(string firstWord)
        {
            if (String.IsNullOrWhiteSpace(firstWord))
                return false;

            if (IsKnownAlias(firstWord))
                return false;

            return firstWord.Length <= 12 && EndsLikeCompanionAlias(firstWord);
        }

        private static bool IsKnownAlias(string word)
        {
            foreach (CompanionAliasEntry entry in CompanionAliases)
            {
                if (entry == null || entry.Aliases == null)
                    continue;

                foreach (string alias in entry.Aliases)
                {
                    if (String.Equals(alias, word, StringComparison.Ordinal))
                        return true;
                }
            }

            return false;
        }

        private static bool EndsLikeCompanionAlias(string word)
        {
            return word.EndsWith("ion", StringComparison.Ordinal)
                || word.EndsWith("ras", StringComparison.Ordinal)
                || word.EndsWith("yal", StringComparison.Ordinal)
                || word.Equals("waylander", StringComparison.Ordinal);
        }

        private static bool StartsWithKnownSharedLeadWord(string speech)
        {
            string firstWord = GetFirstWord(speech);
            if (String.IsNullOrWhiteSpace(firstWord))
                return false;

            for (int i = 0; i < KnownSharedLeadWords.Length; i++)
            {
                if (String.Equals(KnownSharedLeadWords[i], firstWord, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static string GetFirstWord(string speech)
        {
            if (String.IsNullOrWhiteSpace(speech))
                return String.Empty;

            int space = speech.IndexOf(' ');
            return space < 0 ? speech : speech.Substring(0, space);
        }

        private static string GetRemainderAfterFirstWord(string speech)
        {
            if (String.IsNullOrWhiteSpace(speech))
                return String.Empty;

            int space = speech.IndexOf(' ');
            if (space < 0 || space >= speech.Length - 1)
                return String.Empty;

            return speech.Substring(space + 1).Trim();
        }

        private static string NormalizeSpeech(string speech)
        {
            if (String.IsNullOrWhiteSpace(speech))
                return String.Empty;

            string normalized = speech.Trim().ToLowerInvariant();
            normalized = normalized.Replace(",", " ").Replace(".", " ").Replace("!", " ").Replace("?", " ").Replace(";", " ").Replace(":", " ");

            while (normalized.Contains("  "))
                normalized = normalized.Replace("  ", " ");

            return normalized.Trim();
        }
    }
}
