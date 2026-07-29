using System;
using System.Collections.Generic;
using Server.Custom.AIGM.Characters.Waylander;

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
                prefixMatches: new[] { "go to ", "travel to ", "head to ", "move to ", "walk to ", "navigate to ", "take me to ", "take us to ", "lead me to ", "lead us to " }),
            Create(AIGMCompanionCommandVerbKind.StopTravel, AIGMCompanionCapabilityKind.StopTravel, "stop travel", false, true, true,
                exactMatches: new[] { "stop travel", "stop traveling", "cancel travel", "cancel traveling", "stop moving" }),
            Create(AIGMCompanionCommandVerbKind.StopAll, AIGMCompanionCapabilityKind.StopAll, "stop all", true, false, false,
                exactMatches: new[] { "stop everything", "everyone stop", "companions stop", "stop all", "halt all", "stop moving", "stop traveling", "stop hunting", "stop navigation", "cancel all movement", "cancel tracking", "regroup stop", "hold position", "everyone hold" }),
            Create(AIGMCompanionCommandVerbKind.LocateMobile, AIGMCompanionCapabilityKind.LocateMobile, "find", true, false, false,
                prefixMatches: new[] { "find ", "locate ", "track ", "start tracking ", "go to " }),
            Create(AIGMCompanionCommandVerbKind.Regroup, AIGMCompanionCapabilityKind.Regroup, "regroup", true, false, false,
                exactMatches: new[] { "regroup", "companions regroup", "everyone regroup", "companions find each other" },
                prefixMatches: new[] { "regroup on ", "companions regroup on ", "everyone regroup on ", "everyone to ", "companions find " }),
            Create(AIGMCompanionCommandVerbKind.TrackingHelp, AIGMCompanionCapabilityKind.TrackingHelp, "tracking help", false, true, false,
                exactMatches: new[] { "tracking help", "track help", "find help", "locate help" }),
            Create(AIGMCompanionCommandVerbKind.TravelStatus, AIGMCompanionCapabilityKind.TravelReadOnly, "travel status", false, true, false,
                exactMatches: new[] { "travel status", "what is your travel status", "where are you headed", "what are you doing travel wise" }),
            Create(AIGMCompanionCommandVerbKind.Scan, AIGMCompanionCapabilityKind.ScanReadOnly, "scan", false, true, false,
                exactMatches: new[] { "scan", "scan area", "scan the area", "assess area", "assess the area", "where are we", "situation report", "give me a status report" },
                prefixMatches: new[] { "look around", "assess this area", "assess our area" }),
            Create(AIGMCompanionCommandVerbKind.ReportThreats, AIGMCompanionCapabilityKind.ReportThreatsReadOnly, "report threats", false, true, false,
                exactMatches: new[] { "report threats", "what threats are nearby", "what threats are close", "any threats nearby" },
                prefixMatches: new[] { "report danger", "report hostiles" }),
            Create(AIGMCompanionCommandVerbKind.ShareAwareness, AIGMCompanionCapabilityKind.ShareAwarenessReadOnly, "share awareness", false, true, false,
                exactMatches: new[] { "share awareness", "share what you see" },
                prefixMatches: new[] { "share what you sense" }),
            Create(AIGMCompanionCommandVerbKind.TrackReadOnly, AIGMCompanionCapabilityKind.TrackReadOnly, "track", false, true, false,
                exactMatches: new[] { "track animals", "track monsters", "track players", "track animal", "track monster", "track enemies", "track hostiles", "track npcs", "track npc", "track human npcs", "track humans", "track human", "track people", "track locations", "track location", "track landmarks", "scan locations", "scan landmarks", "nearest landmarks", "scan animals", "scan monsters", "scan players", "find closest monster", "find closest animal", "find closest npc", "find closest player", "track only monster", "track only animal", "track only npc", "track only player", "track and move to monster", "track and move to animal", "track and move to npc", "track and move to player", "follow trail to monster", "follow trail to animal", "track and hunt player", "track and hunt players", "track and hunt npc", "track and hunt npcs", "hunt player", "hunt npc", "attack player", "attack npc" }),
            Create(AIGMCompanionCommandVerbKind.StartTracking, AIGMCompanionCapabilityKind.TrackingCycle, "start tracking", true, false, false,
                exactMatches: new[] { "track", "start tracking", "begin tracking", "track cycle on", "track on", "start tracking all", "all start tracking", "companions start tracking", "everyone start tracking" }),
            Create(AIGMCompanionCommandVerbKind.StopTracking, AIGMCompanionCapabilityKind.TrackingStop, "stop tracking", true, false, false,
                exactMatches: new[] { "stop tracking", "end tracking", "track cycle off", "track off" }),
            Create(AIGMCompanionCommandVerbKind.TrackingStatus, AIGMCompanionCapabilityKind.ReportTrackingStatus, "tracking status", false, true, false,
                exactMatches: new[] { "tracking status", "track status", "report tracking", "report tracking status", "what is your tracking status", "what are you tracking", "what do you sense", "what do you sense nearby", "any tracks", "what tracks", "what changed", "all tracking status" }),
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
            Create(AIGMCompanionCommandVerbKind.Loot, AIGMCompanionCapabilityKind.LootNearby, "loot", false, true, false,
                exactMatches: new[] { "loot", "loot corpses", "loot corpse", "loot the bodies", "loot bodies", "check corpses", "check the corpses", "check bodies", "check the bodies", "gather loot", "gather gold", "companions loot", "companions gather loot", "companions gather gold", "everyone loot", "all loot" },
                prefixMatches: new[] { "loot ", "gather loot", "gather gold", "check corpse", "check bodies", "check the bodies", "companions loot", "companions gather" }),
            Create(AIGMCompanionCommandVerbKind.StopLoot, AIGMCompanionCapabilityKind.LootStop, "stop looting", false, true, false,
                exactMatches: new[] { "stop looting", "stop loot", "leave the bodies", "leave corpses" }),
            Create(AIGMCompanionCommandVerbKind.LootStatus, AIGMCompanionCapabilityKind.LootStatus, "loot status", false, true, false,
                exactMatches: new[] { "loot status", "looting status" }),
            Create(AIGMCompanionCommandVerbKind.AutoLoot, AIGMCompanionCapabilityKind.AutoLoot, "auto loot", false, true, false,
                exactMatches: new[] { "auto loot on", "enable auto loot", "companions auto loot on", "everyone auto loot on", "all auto loot on", "auto loot gold", "auto loot supplies", "auto loot all", "auto loot default" },
                prefixMatches: new[] { "auto loot ", "companions auto loot ", "everyone auto loot ", "all auto loot " }),
            Create(AIGMCompanionCommandVerbKind.StopAutoLoot, AIGMCompanionCapabilityKind.AutoLootStop, "auto loot off", false, true, false,
                exactMatches: new[] { "auto loot off", "disable auto loot", "companions auto loot off", "everyone auto loot off", "all auto loot off", "stop auto loot" }),
            Create(AIGMCompanionCommandVerbKind.AutoLootStatus, AIGMCompanionCapabilityKind.AutoLootStatus, "auto loot status", false, true, false,
                exactMatches: new[] { "auto loot status", "auto looting status" }),
            Create(AIGMCompanionCommandVerbKind.InventoryBurden, AIGMCompanionCapabilityKind.InventoryBurden, "burden", false, true, false,
                exactMatches: new[] { "burden", "loot burden", "pack burden", "inventory burden", "how heavy are you", "how full is your pack" }),
            Create(AIGMCompanionCommandVerbKind.UnloadJunk, AIGMCompanionCapabilityKind.UnloadJunk, "unload junk", false, true, false,
                exactMatches: new[] { "unload junk", "drop junk", "lighten pack", "companions unload junk", "companions drop junk", "everyone unload junk", "all unload junk" },
                prefixMatches: new[] { "unload junk", "drop junk", "lighten pack", "companions unload", "companions drop" }),
            Create(AIGMCompanionCommandVerbKind.Potions, AIGMCompanionCapabilityKind.PotionSupport, "use potions", false, true, false,
                exactMatches: new[] { "use potions", "use potion", "companions use potions", "everyone use potions", "all use potions", "use strength and agility potions in battle" },
                prefixMatches: new[] { "use potions", "use potion", "companions use potions", "everyone use potions", "all use potions" }),
            Create(AIGMCompanionCommandVerbKind.StopPotions, AIGMCompanionCapabilityKind.PotionSupportStop, "stop using potions", false, true, false,
                exactMatches: new[] { "stop using potions", "companions stop using potions", "stop potion support", "disable potions", "disable potion support" }),
            Create(AIGMCompanionCommandVerbKind.PotionStatus, AIGMCompanionCapabilityKind.PotionSupportStatus, "potion status", false, true, false,
                exactMatches: new[] { "potion status", "potions status", "companions potion status" }),
            Create(AIGMCompanionCommandVerbKind.UsePotion, AIGMCompanionCapabilityKind.PotionUse, "drink potion", false, true, false,
                exactMatches: new[] { "drink a cure potion", "drink cure potion", "drink a heal potion", "drink heal potion", "use refresh if tired", "use cure potions", "use healing potions" },
                prefixMatches: new[] { "drink a ", "drink ", "use cure potion", "use cure potions", "use healing potion", "use healing potions", "use refresh" }),
            Create(AIGMCompanionCommandVerbKind.Spells, AIGMCompanionCapabilityKind.SpellSupport, "use spells", false, true, false,
                exactMatches: new[] { "use spells", "use support magic", "companions use support magic", "companions use spells" },
                prefixMatches: new[] { "use spells", "use support magic", "companions use support magic", "companions use spells" }),
            Create(AIGMCompanionCommandVerbKind.StopSpells, AIGMCompanionCapabilityKind.SpellSupportStop, "stop using spells", false, true, false,
                exactMatches: new[] { "stop using spells", "stop casting", "stop support magic", "companions stop using spells" }),
            Create(AIGMCompanionCommandVerbKind.SpellStatus, AIGMCompanionCapabilityKind.SpellSupportStatus, "spell status", false, true, false,
                exactMatches: new[] { "spell status", "spells status", "companions spell status" }),
            Create(AIGMCompanionCommandVerbKind.UseSpell, AIGMCompanionCapabilityKind.SpellUse, "cast support spell", false, true, false,
                exactMatches: new[] { "heal me with magic", "cure me with magic", "bless me", "cast heal", "cast cure" },
                prefixMatches: new[] { "heal ", "cure ", "bless ", "cast heal", "cast cure" }),
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

            AddRosterAddressedIds(normalized, ids);
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

            CompanionAliasEntry rosterAlias;
            if (TryMatchRosterNamedAlias(decision.NormalizedSpeech, out rosterAlias, out payload))
            {
                decision.CompanionKey = rosterAlias.Key;
                decision.CompanionName = rosterAlias.Name;

                if (IsNaturalNamedDialoguePayload(payload))
                    return ApplyDialogueDecision(decision, AIGMCompanionCommandRouteKind.NamedCompanion, "roster_named_dialogue");

                if (TryRecognizeCommandFamily(payload, out var family))
                    return ApplyFamilyDecision(decision, family, AIGMCompanionCommandRouteKind.NamedCompanion, "roster_named_command");

                return ApplyDialogueDecision(decision, AIGMCompanionCommandRouteKind.NamedCompanion, "roster_named_dialogue");
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
                || speech.Contains("what do you make of")
                || speech.Contains("how are we looking")
                || speech.Contains("how do we look")
                || speech.Contains("are we safe")
                || speech.Contains("what is ahead")
                || speech.Contains("what's ahead")
                || speech.Contains("discuss")
                || speech.Contains("talk to")
                || speech.Contains("tell me about")
                || speech.Contains("tell me the story")
                || speech.Contains("tell me a story")
                || speech.Contains("greet ")
                || speech.Contains("say hello to")
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
                || normalizedSpeech.Contains("what do you make of")
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

        private static bool TryMatchRosterNamedAlias(string normalizedSpeech, out CompanionAliasEntry matchedAlias, out string payload)
        {
            matchedAlias = null;
            payload = String.Empty;

            if (String.IsNullOrWhiteSpace(normalizedSpeech))
                return false;

            List<CompanionAliasEntry> entries = BuildRosterAliasEntries();
            entries.Sort((a, b) => LongestAliasLength(b).CompareTo(LongestAliasLength(a)));

            for (int i = 0; i < entries.Count; i++)
            {
                CompanionAliasEntry entry = entries[i];
                if (entry == null || entry.Aliases == null)
                    continue;

                for (int j = 0; j < entry.Aliases.Length; j++)
                {
                    string alias = NormalizeRosterAlias(entry.Aliases[j]);
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

        private static void AddRosterAddressedIds(string normalizedSpeech, List<string> ids)
        {
            if (String.IsNullOrWhiteSpace(normalizedSpeech) || ids == null)
                return;

            List<CompanionAliasEntry> entries = BuildRosterAliasEntries();
            for (int i = 0; i < entries.Count; i++)
            {
                CompanionAliasEntry entry = entries[i];
                if (entry == null || entry.Aliases == null)
                    continue;

                for (int j = 0; j < entry.Aliases.Length; j++)
                {
                    string alias = NormalizeRosterAlias(entry.Aliases[j]);
                    if (String.IsNullOrWhiteSpace(alias))
                        continue;

                    string haystack = " " + normalizedSpeech + " ";
                    string needle = " " + alias + " ";
                    if (haystack.IndexOf(needle, StringComparison.Ordinal) >= 0 && !ids.Contains(entry.Key))
                    {
                        ids.Add(entry.Key);
                        break;
                    }
                }
            }
        }

        private static List<CompanionAliasEntry> BuildRosterAliasEntries()
        {
            List<CompanionAliasEntry> entries = new List<CompanionAliasEntry>();
            foreach (KeyValuePair<string, WaylanderCharacterDefinition> pair in WaylanderRosterCatalog.GetAllDefinitions())
            {
                WaylanderCharacterDefinition definition = pair.Value;
                if (definition == null)
                    continue;

                List<string> aliases = new List<string>();
                AddAlias(aliases, pair.Key);
                AddAlias(aliases, definition.CanonicalPersonId);
                AddAlias(aliases, definition.VisibleName);

                if (definition.Aliases != null)
                {
                    for (int i = 0; i < definition.Aliases.Length; i++)
                        AddAlias(aliases, definition.Aliases[i]);
                }

                entries.Add(new CompanionAliasEntry
                {
                    Key = String.IsNullOrWhiteSpace(definition.CanonicalPersonId) ? pair.Key : definition.CanonicalPersonId,
                    Name = String.IsNullOrWhiteSpace(definition.VisibleName) ? pair.Key : definition.VisibleName,
                    Aliases = aliases.ToArray()
                });
            }

            return entries;
        }

        private static int LongestAliasLength(CompanionAliasEntry entry)
        {
            int max = 0;
            if (entry == null || entry.Aliases == null)
                return max;

            for (int i = 0; i < entry.Aliases.Length; i++)
            {
                string alias = NormalizeRosterAlias(entry.Aliases[i]);
                if (alias.Length > max)
                    max = alias.Length;
            }

            return max;
        }

        private static void AddAlias(List<string> aliases, string value)
        {
            string normalized = NormalizeRosterAlias(value);
            if (String.IsNullOrWhiteSpace(normalized) || aliases.Contains(normalized))
                return;

            aliases.Add(normalized);
        }

        private static string NormalizeRosterAlias(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return String.Empty;

            return NormalizeSpeech(value.Replace("_", " "));
        }

        private static bool TryRecognizeCommandFamily(string normalizedSpeech, out RecognizedCommandFamily family)
        {
            family = null;
            if (String.IsNullOrWhiteSpace(normalizedSpeech))
                return false;

            string speech = normalizedSpeech.Trim();
            if (IsGoToKnownCompanion(speech))
            {
                family = FindCommandFamily(AIGMCompanionCommandVerbKind.LocateMobile);
                if (family != null)
                    return true;
            }

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

        private static RecognizedCommandFamily FindCommandFamily(AIGMCompanionCommandVerbKind verbKind)
        {
            for (int i = 0; i < CommandFamilies.Length; i++)
            {
                RecognizedCommandFamily family = CommandFamilies[i];
                if (family != null && family.VerbKind == verbKind)
                    return family;
            }

            return null;
        }

        private static bool IsGoToKnownCompanion(string speech)
        {
            if (String.IsNullOrWhiteSpace(speech))
                return false;

            string target = null;
            if (speech.StartsWith("go to ", StringComparison.Ordinal))
                target = speech.Substring(6).Trim();
            else if (speech.StartsWith("travel to ", StringComparison.Ordinal))
                target = speech.Substring(10).Trim();
            else if (speech.StartsWith("walk to ", StringComparison.Ordinal))
                target = speech.Substring(8).Trim();

            if (String.IsNullOrWhiteSpace(target))
                return false;

            string first = GetFirstWord(target);
            return IsKnownAlias(first) || first == "me";
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
