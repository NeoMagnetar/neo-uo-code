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

        private sealed class RecognizedCommandFamily
        {
            public AIGMCompanionCommandVerbKind VerbKind;
            public AIGMCompanionCapabilityKind Capability;
            public string CanonicalVerb;
            public bool IsExecutableNow;
            public bool IsDeferredCapability;
            public bool RequiresFutureExecutor;
            public string[] ExactMatches;
            public string[] PrefixMatches;
        }

        private static readonly CompanionAliasEntry[] CompanionAliases =
        {
            new CompanionAliasEntry
            {
                Key = "dakeyras",
                Name = "Dakeyras",
                Aliases = new[] { "dak", "dake", "dakeyras", "waylander" }
            },
            new CompanionAliasEntry
            {
                Key = "danyal",
                Name = "Danyal",
                Aliases = new[] { "dan", "danyal" }
            },
            new CompanionAliasEntry
            {
                Key = "dardalion",
                Name = "Dardalion",
                Aliases = new[] { "dar", "dard", "dardalion" }
            }
        };

        private static readonly RecognizedCommandFamily[] CommandFamilies =
        {
            Create(AIGMCompanionCommandVerbKind.Follow, AIGMCompanionCapabilityKind.Follow, "follow", true, false, false,
                exactMatches: new[] { "follow", "follow me" }),
            Create(AIGMCompanionCommandVerbKind.Come, AIGMCompanionCapabilityKind.Follow, "come", true, false, false,
                exactMatches: new[] { "come", "come here", "come to me" }),
            Create(AIGMCompanionCommandVerbKind.Guard, AIGMCompanionCapabilityKind.Guard, "guard", true, false, false,
                exactMatches: new[] { "guard me", "protect me", "defend me", "stand guard" }),
            Create(AIGMCompanionCommandVerbKind.Stay, AIGMCompanionCapabilityKind.Stay, "stay", true, false, false,
                exactMatches: new[] { "stay", "stay here" }),
            Create(AIGMCompanionCommandVerbKind.Hold, AIGMCompanionCapabilityKind.Stay, "hold", true, false, false,
                exactMatches: new[] { "hold", "hold here", "hold position" }),
            Create(AIGMCompanionCommandVerbKind.Stop, AIGMCompanionCapabilityKind.Stay, "stop", true, false, false,
                exactMatches: new[] { "stop" }),
            Create(AIGMCompanionCommandVerbKind.Wait, AIGMCompanionCapabilityKind.Stay, "wait", true, false, false,
                exactMatches: new[] { "wait" }),
            Create(AIGMCompanionCommandVerbKind.ReturnHome, AIGMCompanionCapabilityKind.ReturnHome, "return home", false, true, true,
                exactMatches: new[] { "return home" }),
            Create(AIGMCompanionCommandVerbKind.Travel, AIGMCompanionCapabilityKind.TravelExecute, "travel", false, true, true,
                prefixMatches: new[] { "go to ", "travel to ", "head to " }),
            Create(AIGMCompanionCommandVerbKind.StopTravel, AIGMCompanionCapabilityKind.StopTravel, "stop travel", false, true, true,
                exactMatches: new[] { "stop travel", "stop traveling", "cancel travel", "cancel traveling", "stop moving" }),
            Create(AIGMCompanionCommandVerbKind.TravelStatus, AIGMCompanionCapabilityKind.TravelReadOnly, "travel status", false, true, false,
                exactMatches: new[] { "travel status", "what is your travel status", "where are you headed", "what are you doing travel wise" }),
            Create(AIGMCompanionCommandVerbKind.Scan, AIGMCompanionCapabilityKind.ScanReadOnly, "scan", false, true, false,
                exactMatches: new[] { "scan", "scan area", "scan the area" },
                prefixMatches: new[] { "look around" }),
            Create(AIGMCompanionCommandVerbKind.ReportThreats, AIGMCompanionCapabilityKind.ReportThreatsReadOnly, "report threats", false, true, false,
                exactMatches: new[] { "report threats" },
                prefixMatches: new[] { "report danger", "report hostiles" }),
            Create(AIGMCompanionCommandVerbKind.ShareAwareness, AIGMCompanionCapabilityKind.ShareAwarenessReadOnly, "share awareness", false, true, false,
                exactMatches: new[] { "share awareness", "share what you see" },
                prefixMatches: new[] { "share what you sense" }),
            Create(AIGMCompanionCommandVerbKind.TrackReadOnly, AIGMCompanionCapabilityKind.TrackReadOnly, "track", false, true, false,
                exactMatches: new[] { "track animals", "track monsters", "track players", "track animal", "track monster", "track enemies", "track hostiles", "track npcs", "track npc", "track human npcs", "track humans", "track human", "track people", "scan animals", "scan monsters", "scan players", "find closest monster", "find closest animal", "find closest npc", "find closest player", "track only monster", "track only animal", "track only npc", "track only player", "track and move to monster", "track and move to animal", "track and move to npc", "track and move to player", "follow trail to monster", "follow trail to animal", "track and hunt player", "track and hunt players", "track and hunt npc", "track and hunt npcs", "hunt player", "hunt npc", "attack player", "attack npc" }),
            Create(AIGMCompanionCommandVerbKind.StartTracking, AIGMCompanionCapabilityKind.TrackingCycle, "start tracking", true, false, false,
                exactMatches: new[] { "track", "start tracking", "begin tracking", "track cycle on", "track on", "start tracking all", "all start tracking", "companions start tracking", "everyone start tracking" }),
            Create(AIGMCompanionCommandVerbKind.StopTracking, AIGMCompanionCapabilityKind.TrackingStop, "stop tracking", true, false, false,
                exactMatches: new[] { "stop tracking", "end tracking", "track cycle off", "track off" }),
            Create(AIGMCompanionCommandVerbKind.TrackingStatus, AIGMCompanionCapabilityKind.ReportTrackingStatus, "tracking status", false, true, false,
                exactMatches: new[] { "tracking status", "track status", "report tracking", "report tracking status", "what is your tracking status", "what are you tracking", "what do you sense", "what do you sense nearby", "any tracks", "what tracks", "what changed", "all tracking status" },
                prefixMatches: new[] { "what do you see" }),
            Create(AIGMCompanionCommandVerbKind.Attack, AIGMCompanionCapabilityKind.Attack, "attack", false, true, true,
                prefixMatches: new[] { "attack", "fight", "kill" }),
            Create(AIGMCompanionCommandVerbKind.Disengage, AIGMCompanionCapabilityKind.Disengage, "disengage", false, true, true,
                prefixMatches: new[] { "disengage", "stop fighting", "stop attack", "stop attacking", "stop combat" }),
            Create(AIGMCompanionCommandVerbKind.Heal, AIGMCompanionCapabilityKind.Heal, "heal", false, true, true,
                prefixMatches: new[] { "heal ", "heal me", "heal my wounds", "heal owner", "heal yourself", "heal self", "heal myself", "stop healing", "cancel healing" }),
            Create(AIGMCompanionCommandVerbKind.Bandage, AIGMCompanionCapabilityKind.Bandage, "bandage", false, true, true,
                prefixMatches: new[] { "bandage ", "bandage me", "bandage my wounds", "bandage owner", "use bandages on me", "bandage yourself", "bandage self", "bandage myself", "bandage your own self", "stop bandaging", "cancel bandaging" }),
            Create(AIGMCompanionCommandVerbKind.Cure, AIGMCompanionCapabilityKind.Cure, "cure", false, true, true,
                prefixMatches: new[] { "cure me", "cure my poison", "cure yourself", "cure self" }),
            Create(AIGMCompanionCommandVerbKind.CastHeal, AIGMCompanionCapabilityKind.CastHeal, "cast heal", false, true, true,
                prefixMatches: new[] { "cast heal" }),
            Create(AIGMCompanionCommandVerbKind.CastCure, AIGMCompanionCapabilityKind.CastCure, "cast cure", false, true, true,
                prefixMatches: new[] { "cast cure" }),
            Create(AIGMCompanionCommandVerbKind.FollowCompanion, AIGMCompanionCapabilityKind.FollowCompanion, "follow companion", false, true, true,
                prefixMatches: new[] { "follow danyal", "follow dardalion", "follow dak", "follow dakeyras", "follow waylander" }),
            Create(AIGMCompanionCommandVerbKind.GreetCompanion, AIGMCompanionCapabilityKind.GreetCompanion, "greet companion", false, true, true,
                prefixMatches: new[] { "greet danyal", "greet dardalion", "greet dak", "greet dakeyras", "greet waylander", "hello danyal", "hello dardalion", "hello dak", "hello dakeyras", "hello waylander", "say hello to danyal", "say hello to dardalion", "say hello to dak", "say hello to dakeyras", "say hello to waylander" }),
            Create(AIGMCompanionCommandVerbKind.Report, AIGMCompanionCapabilityKind.None, "report", false, false, false,
                exactMatches: new[] { "report", "report status" })
        };

        public static List<string> GetAddressedCompanionIds(string speech)
        {
            List<string> ids = new List<string>();
            string normalized = NormalizeSpeech(speech);
            if (String.IsNullOrWhiteSpace(normalized))
                return ids;

            string[] words = normalized.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];
                if (String.IsNullOrWhiteSpace(word))
                    continue;

                for (int j = 0; j < CompanionAliases.Length; j++)
                {
                    CompanionAliasEntry entry = CompanionAliases[j];
                    if (entry == null || entry.Aliases == null)
                        continue;

                    bool matched = false;
                    for (int k = 0; k < entry.Aliases.Length; k++)
                    {
                        if (String.Equals(word, entry.Aliases[k], StringComparison.Ordinal))
                        {
                            matched = true;
                            break;
                        }
                    }

                    if (matched && !ids.Contains(entry.Key))
                        ids.Add(entry.Key);
                }
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

                if (IsNaturalNamedDialoguePayload(payload))
                    return ApplyDialogueDecision(decision, AIGMCompanionCommandRouteKind.NamedCompanion, "companion_named_dialogue");

                if (TryRecognizeCommandFamily(payload, out var family))
                    return ApplyFamilyDecision(decision, family, AIGMCompanionCommandRouteKind.NamedCompanion, "companion_named_command");

                return ApplyDialogueDecision(decision, AIGMCompanionCommandRouteKind.NamedCompanion, "companion_named_dialogue");
            }

            string firstWord = GetFirstWord(decision.NormalizedSpeech);
            if (IsUnknownCompanionAlias(firstWord) && StartsWithKnownSharedLeadWord(GetRemainderAfterFirstWord(decision.NormalizedSpeech)))
            {
                decision.RouteKind = AIGMCompanionCommandRouteKind.UnknownCompanionAlias;
                decision.Reason = "companion_alias_unknown";
                decision.BlocksCounselorLane = true;
                return decision;
            }

            if (TryRecognizeCommandFamily(decision.NormalizedSpeech, out var sharedFamily))
                return ApplyFamilyDecision(decision, sharedFamily, AIGMCompanionCommandRouteKind.SharedCompanion, "companion_shared_command");

            if (IsNaturalGroupDialogueSpeech(decision.NormalizedSpeech))
                return ApplyDialogueDecision(decision, AIGMCompanionCommandRouteKind.SharedCompanion, "companion_group_dialogue");

            decision.RouteKind = AIGMCompanionCommandRouteKind.NonCompanion;
            decision.Reason = "not_companion_command";
            return decision;
        }

        private static AIGMCompanionCommandRouteDecision ApplyDialogueDecision(AIGMCompanionCommandRouteDecision decision, AIGMCompanionCommandRouteKind routeKind, string reason)
        {
            decision.IsCompanionCommand = false;
            decision.BlocksCounselorLane = true;
            decision.IsNamedCompanionCommand = false;
            decision.IsSharedCompanionCommand = false;
            decision.CommandVerb = null;
            decision.VerbKind = AIGMCompanionCommandVerbKind.None;
            decision.Capability = AIGMCompanionCapabilityKind.None;
            decision.IsDeferredCapability = false;
            decision.RequiresFutureExecutor = false;
            decision.IsExecutableNow = false;
            decision.RouteKind = routeKind;
            decision.Reason = reason;
            return decision;
        }

        private static AIGMCompanionCommandRouteDecision ApplyFamilyDecision(AIGMCompanionCommandRouteDecision decision, RecognizedCommandFamily family, AIGMCompanionCommandRouteKind routeKind, string reason)
        {
            decision.IsCompanionCommand = true;
            decision.BlocksCounselorLane = true;
            decision.IsNamedCompanionCommand = routeKind == AIGMCompanionCommandRouteKind.NamedCompanion;
            decision.IsSharedCompanionCommand = routeKind == AIGMCompanionCommandRouteKind.SharedCompanion;
            decision.CommandVerb = family.CanonicalVerb;
            decision.VerbKind = family.VerbKind;
            decision.Capability = family.Capability;
            decision.IsDeferredCapability = family.IsDeferredCapability;
            decision.RequiresFutureExecutor = family.RequiresFutureExecutor;
            decision.IsExecutableNow = family.IsExecutableNow;
            decision.RouteKind = routeKind;
            decision.Reason = reason;
            return decision;
        }

        private static bool IsNaturalNamedDialoguePayload(string payload)
        {
            string speech = NormalizeSpeech(payload);
            if (String.IsNullOrWhiteSpace(speech))
                return true;

            return speech.Contains("what do you see")
                || speech.Contains("what do you notice")
                || speech.Contains("what do you think")
                || speech.Contains("how are we looking")
                || speech.Contains("how do we look")
                || speech.Contains("are we safe")
                || speech.Contains("what is ahead")
                || speech.Contains("what's ahead")
                || speech.Contains("discuss")
                || speech.Contains("talk to")
                || speech.Contains("ask ")
                || speech.Contains("keep everyone together")
                || speech.Contains("keep us together")
                || speech.Contains("stay close to")
                || speech.Contains("watch the")
                || speech.Contains("tell me what")
                || speech.Contains("tell us what");
        }

        private static bool IsNaturalGroupDialogueSpeech(string normalizedSpeech)
        {
            if (String.IsNullOrWhiteSpace(normalizedSpeech))
                return false;

            bool groupAddressed = normalizedSpeech.Contains("companions")
                || normalizedSpeech.Contains("all companions")
                || normalizedSpeech.Contains("all of you")
                || normalizedSpeech.Contains("you all")
                || normalizedSpeech.Contains("you three")
                || normalizedSpeech.Contains("three of you")
                || normalizedSpeech.Contains("everyone")
                || normalizedSpeech.Contains("everybody")
                || normalizedSpeech.StartsWith("party ", StringComparison.Ordinal);

            if (!groupAddressed)
                return false;

            return normalizedSpeech.Contains("discuss")
                || normalizedSpeech.Contains("talk")
                || normalizedSpeech.Contains("what do you see")
                || normalizedSpeech.Contains("what do you think")
                || normalizedSpeech.Contains("how are we looking")
                || normalizedSpeech.Contains("situation")
                || normalizedSpeech.Contains("trail")
                || normalizedSpeech.Contains("safe")
                || normalizedSpeech.Contains("notice");
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

        private static bool TryRecognizeCommandFamily(string normalizedSpeech, out RecognizedCommandFamily family)
        {
            family = null;
            if (String.IsNullOrWhiteSpace(normalizedSpeech))
                return false;

            string speech = normalizedSpeech.Trim();
            for (int i = 0; i < CommandFamilies.Length; i++)
            {
                RecognizedCommandFamily candidate = CommandFamilies[i];
                if (MatchesFamily(speech, candidate))
                {
                    family = candidate;
                    return true;
                }
            }

            return false;
        }

        private static bool MatchesFamily(string speech, RecognizedCommandFamily family)
        {
            if (family == null)
                return false;

            if (family.ExactMatches != null)
            {
                for (int i = 0; i < family.ExactMatches.Length; i++)
                {
                    if (speech.Equals(family.ExactMatches[i], StringComparison.Ordinal))
                        return true;
                }
            }

            if (family.PrefixMatches != null)
            {
                for (int i = 0; i < family.PrefixMatches.Length; i++)
                {
                    string prefix = family.PrefixMatches[i];
                    if (String.IsNullOrWhiteSpace(prefix))
                        continue;

                    if (speech.Equals(prefix, StringComparison.Ordinal) || speech.StartsWith(prefix, StringComparison.Ordinal))
                        return true;
                }
            }

            return false;
        }

        private static RecognizedCommandFamily Create(AIGMCompanionCommandVerbKind verbKind, AIGMCompanionCapabilityKind capability, string canonicalVerb, bool isExecutableNow, bool isDeferredCapability, bool requiresFutureExecutor, string[] exactMatches = null, string[] prefixMatches = null)
        {
            RecognizedCommandFamily family = new RecognizedCommandFamily();
            family.VerbKind = verbKind;
            family.Capability = capability;
            family.CanonicalVerb = canonicalVerb;
            family.IsExecutableNow = isExecutableNow;
            family.IsDeferredCapability = isDeferredCapability;
            family.RequiresFutureExecutor = requiresFutureExecutor;
            family.ExactMatches = exactMatches;
            family.PrefixMatches = prefixMatches;
            return family;
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

            if (TryRecognizeCommandFamily(firstWord, out _))
                return true;

            if (speech.Length > firstWord.Length && TryRecognizeCommandFamily(speech, out _))
                return true;

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
