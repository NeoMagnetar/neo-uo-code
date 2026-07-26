using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Server;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionIntentParser
    {
        private static readonly AIGMCompanionProfile[] DefaultProfiles = new[]
        {
            new AIGMCompanionProfile
            {
                Id = "dakeyras",
                DisplayName = "Dakeyras",
                Role = "archer-companion",
                ProfileKey = "dakeyras",
                Aliases = new[] { "dakeyras", "dak", "waylander" }
            },
            new AIGMCompanionProfile
            {
                Id = "danyal",
                DisplayName = "Danyal",
                Role = "wayfarer-companion",
                ProfileKey = "danyal",
                Aliases = new[] { "danyal" }
            },
            new AIGMCompanionProfile
            {
                Id = "dardalion",
                DisplayName = "Dardalion",
                Role = "warrior-priest-companion",
                ProfileKey = "dardalion",
                Aliases = new[] { "dardalion", "dar" }
            }
        };

        public static bool IsClearlyAddressedToDifferentCompanion(BaseHire companion, string speech)
        {
            return IsClearlyAddressedToDifferentCompanion(companion, speech, null);
        }

        public static bool IsClearlyAddressedToDifferentCompanion(BaseHire companion, string speech, IEnumerable<AIGMCompanionProfile> profiles)
        {
            if (companion == null || String.IsNullOrWhiteSpace(speech))
                return false;

            string normalized = NormalizeSpeech(speech.Trim().ToLowerInvariant());
            AIGMCompanionProfile addressed = FindAddressedProfile(normalized, profiles);
            if (addressed == null)
                return false;

            AIGMCompanionProfile self = ResolveCompanionProfile(companion, profiles);
            if (self == null)
                return false;

            return !String.Equals(self.Id ?? String.Empty, addressed.Id ?? String.Empty, StringComparison.OrdinalIgnoreCase);
        }

        public static bool TryParse(BaseHire companion, Mobile speaker, string rawSpeech, out AIGMCompanionIntent intent)
        {
            return TryParse(companion, speaker, rawSpeech, null, out intent);
        }

        public static bool TryParse(BaseHire companion, Mobile speaker, string rawSpeech, IEnumerable<AIGMCompanionProfile> profiles, out AIGMCompanionIntent intent)
        {
            intent = null;

            if (companion == null || speaker == null || String.IsNullOrWhiteSpace(rawSpeech))
                return false;

            string speech = NormalizeSpeech(rawSpeech.Trim().ToLowerInvariant());
            bool explicitlyAddressed = false;

            string addressedSpeech;
            if (TryExtractAddressedCommand(companion, speech, profiles, out addressedSpeech))
            {
                explicitlyAddressed = true;
                speech = NormalizeSpeech(addressedSpeech);
            }
            else
            {
                string strippedOtherNameSpeech;
                if (TryStripAnyKnownCompanionPrefix(speech, profiles, out strippedOtherNameSpeech))
                    speech = NormalizeSpeech(strippedOtherNameSpeech);
            }

            if (speech.Equals("where are you") || speech.Equals("where are we") || speech.Contains("report location") || speech.Contains("what region are you in") || speech.Contains("what are your coordinates"))
            {
                Make(AIGMCompanionIntentKind.ReportLocation, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Equals("scan area") || speech.Equals("scan the area") || speech.Equals("assess area") || speech.Equals("assess the area") || speech.Equals("situation report") || speech.Equals("give me a status report") || speech.Contains("look around"))
            {
                Make(AIGMCompanionIntentKind.ScanArea, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            string lootCommand = CanonicalizeLootCommand(speech);
            if (lootCommand == "auto_loot")
            {
                Make(AIGMCompanionIntentKind.AutoLoot, rawSpeech, out intent);
                intent.DestinationName = speech;
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (lootCommand == "stop_auto_loot")
            {
                Make(AIGMCompanionIntentKind.StopAutoLoot, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (lootCommand == "report_auto_loot_status")
            {
                Make(AIGMCompanionIntentKind.ReportAutoLootStatus, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (lootCommand == "report_burden")
            {
                Make(AIGMCompanionIntentKind.ReportBurden, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (lootCommand == "unload_junk")
            {
                Make(AIGMCompanionIntentKind.UnloadJunk, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            string potionCommand = CanonicalizePotionCommand(speech);
            if (potionCommand == "potion_support")
            {
                Make(AIGMCompanionIntentKind.PotionSupport, rawSpeech, out intent);
                intent.DestinationName = speech;
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (potionCommand == "stop_potion_support")
            {
                Make(AIGMCompanionIntentKind.StopPotionSupport, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (potionCommand == "report_potion_status")
            {
                Make(AIGMCompanionIntentKind.ReportPotionStatus, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (potionCommand == "use_potion")
            {
                Make(AIGMCompanionIntentKind.UsePotion, rawSpeech, out intent);
                intent.DestinationName = speech;
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            string spellCommand = CanonicalizeSpellCommand(speech);
            if (spellCommand == "spell_support")
            {
                Make(AIGMCompanionIntentKind.SpellSupport, rawSpeech, out intent);
                intent.DestinationName = speech;
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (spellCommand == "stop_spell_support")
            {
                Make(AIGMCompanionIntentKind.StopSpellSupport, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (spellCommand == "report_spell_status")
            {
                Make(AIGMCompanionIntentKind.ReportSpellStatus, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (spellCommand == "use_spell")
            {
                Make(AIGMCompanionIntentKind.UseSpell, rawSpeech, out intent);
                intent.DestinationName = speech;
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (lootCommand == "loot_nearby")
            {
                Make(AIGMCompanionIntentKind.LootNearby, rawSpeech, out intent);
                intent.DestinationName = speech;
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (lootCommand == "stop_looting")
            {
                Make(AIGMCompanionIntentKind.StopLooting, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (lootCommand == "report_loot_status")
            {
                Make(AIGMCompanionIntentKind.ReportLootStatus, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Equals("find closest monster") || speech.Equals("find nearest monster") || speech.Equals("track only monster") || speech.Equals("track only monsters"))
            {
                Make(AIGMCompanionIntentKind.TrackMonsters, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Equals("find closest animal") || speech.Equals("find nearest animal") || speech.Equals("track only animal") || speech.Equals("track only animals"))
            {
                Make(AIGMCompanionIntentKind.TrackAnimals, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Equals("find closest npc") || speech.Equals("find nearest npc") || speech.Equals("track only npc") || speech.Equals("track only npcs"))
            {
                Make(AIGMCompanionIntentKind.TrackNPCs, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Equals("find closest player") || speech.Equals("find nearest player") || speech.Equals("track only player") || speech.Equals("track only players"))
            {
                Make(AIGMCompanionIntentKind.TrackPlayers, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Equals("track and move to monster") || speech.Equals("track and move to monsters") || speech.Equals("follow trail to monster") || speech.Equals("follow trail to monsters"))
            {
                Make(AIGMCompanionIntentKind.TrackMonsters, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Equals("track and move to animal") || speech.Equals("track and move to animals") || speech.Equals("follow trail to animal") || speech.Equals("follow trail to animals"))
            {
                Make(AIGMCompanionIntentKind.TrackAnimals, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Equals("track and move to npc") || speech.Equals("track and move to npcs"))
            {
                Make(AIGMCompanionIntentKind.TrackNPCs, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Equals("track and move to player") || speech.Equals("track and move to players"))
            {
                Make(AIGMCompanionIntentKind.TrackPlayers, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Equals("track and hunt player") || speech.Equals("track and hunt players") || speech.Equals("hunt player") || speech.Equals("hunt players") || speech.Equals("attack player") || speech.Equals("attack players"))
            {
                Make(AIGMCompanionIntentKind.RefusePlayerHunt, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Equals("track and hunt npc") || speech.Equals("track and hunt npcs") || speech.Equals("hunt npc") || speech.Equals("hunt npcs") || speech.Equals("attack npc") || speech.Equals("attack npcs"))
            {
                Make(AIGMCompanionIntentKind.TrackNPCs, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Equals("start tracking animals"))
            {
                Make(AIGMCompanionIntentKind.StartTrackingAnimals, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Equals("track animals") || speech.Equals("scan animals") || speech.Equals("track animal") || speech.Equals("scan for animals") || speech.Equals("track for animals") || speech.Equals("scan for animal") || speech.Equals("track for animal"))
            {
                Make(AIGMCompanionIntentKind.TrackAnimals, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Equals("start tracking monsters"))
            {
                Make(AIGMCompanionIntentKind.StartTrackingMonsters, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Equals("track monsters") || speech.Equals("scan monsters") || speech.Equals("track monster") || speech.Equals("track hostile") || speech.Equals("track hostiles") || speech.Equals("track enemies") || speech.Equals("scan for monsters") || speech.Equals("track for monsters") || speech.Equals("scan for monster") || speech.Equals("track for monster"))
            {
                Make(AIGMCompanionIntentKind.TrackMonsters, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Equals("start tracking npcs") || speech.Equals("start tracking npc"))
            {
                Make(AIGMCompanionIntentKind.StartTrackingNPCs, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Equals("track npcs") || speech.Equals("track npc") || speech.Equals("scan npcs") || speech.Equals("scan npc") || speech.Equals("track townsfolk") || speech.Equals("scan for npcs") || speech.Equals("track for npcs") || speech.Equals("scan for npc") || speech.Equals("track for npc"))
            {
                Make(AIGMCompanionIntentKind.TrackNPCs, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Equals("start tracking human npcs") || speech.Equals("start tracking humans") || speech.Equals("start tracking people"))
            {
                Make(AIGMCompanionIntentKind.StartTrackingHumanNPCs, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Equals("track human npcs") || speech.Equals("track human npc") || speech.Equals("track humans") || speech.Equals("track human") || speech.Equals("track people") || speech.Equals("scan humans") || speech.Equals("scan human") || speech.Equals("scan for humans") || speech.Equals("scan for human"))
            {
                Make(AIGMCompanionIntentKind.TrackHumanNPCs, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Equals("start tracking players"))
            {
                Make(AIGMCompanionIntentKind.StartTrackingPlayers, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Equals("track players") || speech.Equals("scan players") || speech.Equals("track traveler") || speech.Equals("track travelers") || speech.Equals("scan for players") || speech.Equals("track for players") || speech.Equals("scan for player") || speech.Equals("track for player"))
            {
                Make(AIGMCompanionIntentKind.TrackPlayers, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Equals("start tracking all"))
            {
                Make(AIGMCompanionIntentKind.StartTrackingAll, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Equals("track all") || speech.Equals("scan all") || speech.Equals("track everything") || speech.Equals("scan everything"))
            {
                Make(AIGMCompanionIntentKind.TrackAll, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Equals("hunt animals") || speech.Equals("hunt animal") || speech.Equals("track and hunt animals") || speech.Equals("track and hunt animal"))
            {
                Make(AIGMCompanionIntentKind.HuntAnimals, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Equals("report threats") || speech.Equals("what threats are nearby") || speech.Equals("what threats are close") || speech.Equals("any threats nearby") || speech.Contains("report danger") || speech.Contains("report hostiles"))
            {
                Make(AIGMCompanionIntentKind.ReportThreats, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Equals("report position") || speech.Equals("current coordinates") || speech.Equals("where am i"))
            {
                Make(AIGMCompanionIntentKind.ReportLocation, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Equals("track people") || speech.Equals("track humans"))
            {
                Make(AIGMCompanionIntentKind.TrackPlayers, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Equals("share what you see") || speech.Equals("share awareness") || speech.Contains("share what you sense"))
            {
                Make(AIGMCompanionIntentKind.ShareAwareness, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            string monsterHuntCommand = CanonicalizeMonsterHuntCommand(speech);
            if (monsterHuntCommand == "start_monster_hunt")
            {
                Make(AIGMCompanionIntentKind.StartMonsterHunt, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (monsterHuntCommand == "stop_monster_hunt")
            {
                Make(AIGMCompanionIntentKind.StopMonsterHunt, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (monsterHuntCommand == "report_monster_hunt_status")
            {
                Make(AIGMCompanionIntentKind.ReportMonsterHuntStatus, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            string trackingCommand = CanonicalizeTrackingCommand(speech);
            if (trackingCommand == "start_tracking_cycle")
            {
                Make(AIGMCompanionIntentKind.StartTrackingCycle, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (trackingCommand == "stop_tracking_cycle")
            {
                Make(AIGMCompanionIntentKind.StopTrackingCycle, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (trackingCommand == "report_tracking_status")
            {
                Make(AIGMCompanionIntentKind.ReportTrackingStatus, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (TryBuildTravelIntent(rawSpeech, speech, explicitlyAddressed, out intent))
                return true;

            if (speech.Equals("stop traveling") || speech.Equals("cancel travel") || speech.Equals("stop travel") || speech.Equals("stop moving") || speech.Equals("cancel traveling"))
            {
                Make(AIGMCompanionIntentKind.StopTravel, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Equals("where are you headed") || speech.Equals("travel status") || speech.Equals("what is your travel status") || speech.Equals("what are you doing travel wise"))
            {
                Make(AIGMCompanionIntentKind.ReportTravelStatus, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Equals("return home"))
            {
                Make(AIGMCompanionIntentKind.ReturnHome, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            string followCompanionId;
            if (TryParseCompanionDirectedPhrase(speech, "follow ", profiles, out followCompanionId))
            {
                intent = new AIGMCompanionIntent();
                intent.Kind = AIGMCompanionIntentKind.FollowCompanion;
                intent.RawText = rawSpeech;
                intent.DestinationName = followCompanionId;
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            string greetCompanionId;
            if (TryParseCompanionDirectedPhrase(speech, "greet ", profiles, out greetCompanionId)
                || TryParseCompanionDirectedPhrase(speech, "hello ", profiles, out greetCompanionId)
                || TryParseCompanionDirectedPhrase(speech, "say hello to ", profiles, out greetCompanionId)
                || TryParseCompanionDirectedPhrase(speech, "speak to ", profiles, out greetCompanionId))
            {
                intent = new AIGMCompanionIntent();
                intent.Kind = AIGMCompanionIntentKind.GreetCompanion;
                intent.RawText = rawSpeech;
                intent.DestinationName = greetCompanionId;
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Equals("follow me") || speech.Equals("follow"))
            {
                Make(AIGMCompanionIntentKind.FollowOwner, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Contains("come here") || speech.Contains("come to me") || speech.Equals("come"))
            {
                Make(AIGMCompanionIntentKind.Come, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Contains("stop fighting") || speech.Contains("disengage") || speech.Contains("stop attack") || speech.Contains("stop attacking"))
            {
                Make(AIGMCompanionIntentKind.StopCombat, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Contains("guard me") || speech.Contains("protect me") || speech.Contains("defend me") || speech.Contains("stand guard"))
            {
                Make(AIGMCompanionIntentKind.GuardOwner, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Contains("bandage yourself") || speech.Contains("bandage self") || speech.Contains("bandage myself") || speech.Contains("bandage your own self"))
            {
                Make(AIGMCompanionIntentKind.BandageSelf, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Contains("bandage me") || speech.Contains("bandage my wounds") || speech.Contains("bandage owner") || speech.Contains("use bandages on me"))
            {
                Make(AIGMCompanionIntentKind.BandageOwner, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Contains("heal yourself") || speech.Contains("heal self") || speech.Contains("heal myself"))
            {
                Make(AIGMCompanionIntentKind.HealSelf, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Contains("heal me") || speech.Contains("heal my wounds") || speech.Contains("heal owner"))
            {
                Make(AIGMCompanionIntentKind.HealOwner, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Contains("cure yourself") || speech.Contains("cure self"))
            {
                Make(AIGMCompanionIntentKind.CureSelf, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Contains("cure me") || speech.Contains("cure my poison"))
            {
                Make(AIGMCompanionIntentKind.CureOwner, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Contains("use healing"))
            {
                Make(AIGMCompanionIntentKind.UseHealingSkill, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Equals("use bandages") || speech.Contains("use a bandage") || speech.Contains("use bandage") || speech.Contains("use bandages on yourself") || speech.Contains("use bandages self"))
            {
                Make(AIGMCompanionIntentKind.UseBandages, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Contains("cast heal"))
            {
                Make(AIGMCompanionIntentKind.CastHeal, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Contains("cast cure"))
            {
                Make(AIGMCompanionIntentKind.CastCure, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Equals("stay") || speech.Equals("stop") || speech.Equals("hold here") || speech.Equals("hold position"))
            {
                Make(AIGMCompanionIntentKind.Stay, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Contains("attack") || speech.Contains("kill") || speech.Contains("fight"))
            {
                intent = new AIGMCompanionIntent();
                intent.Kind = AIGMCompanionIntentKind.AttackTarget;
                intent.RawText = rawSpeech;
                intent.ExplicitlyAddressed = explicitlyAddressed;

                Mobile target = ResolveAttackTarget(companion, speaker);
                if (target != null)
                    intent.TargetSerial = target.Serial.Value;

                return true;
            }

            return false;
        }

        private static AIGMCompanionProfile ResolveCompanionProfile(BaseHire companion, IEnumerable<AIGMCompanionProfile> profiles)
        {
            if (companion == null)
                return null;

            string name = NormalizeSpeech((companion.Name ?? String.Empty).ToLowerInvariant());
            if (String.IsNullOrWhiteSpace(name))
                return null;

            foreach (AIGMCompanionProfile profile in EnumerateProfiles(profiles))
            {
                if (MatchesProfileAlias(profile, name))
                    return profile;
            }

            return null;
        }

        private static AIGMCompanionProfile FindAddressedProfile(string speech, IEnumerable<AIGMCompanionProfile> profiles)
        {
            if (String.IsNullOrWhiteSpace(speech))
                return null;

            foreach (AIGMCompanionProfile profile in EnumerateProfiles(profiles))
            {
                foreach (string alias in GetProfileAliases(profile))
                {
                    string normalizedAlias = NormalizeSpeech(alias);
                    if (String.IsNullOrWhiteSpace(normalizedAlias))
                        continue;

                    if (speech.Equals(normalizedAlias) || speech.StartsWith(normalizedAlias + " "))
                        return profile;
                }
            }

            return null;
        }

        private static bool TryExtractAddressedCommand(BaseHire companion, string speech, IEnumerable<AIGMCompanionProfile> profiles, out string addressedSpeech)
        {
            addressedSpeech = null;
            if (companion == null || String.IsNullOrWhiteSpace(speech))
                return false;

            AIGMCompanionProfile profile = ResolveCompanionProfile(companion, profiles);
            if (profile == null)
            {
                string companionName = NormalizeSpeech((companion.Name ?? String.Empty).ToLowerInvariant());
                if (String.IsNullOrWhiteSpace(companionName))
                    return false;

                return TryStripLeadingAlias(speech, new[] { companionName }, out addressedSpeech);
            }

            return TryStripLeadingAlias(speech, GetProfileAliases(profile), out addressedSpeech);
        }

        private static bool TryStripAnyKnownCompanionPrefix(string speech, IEnumerable<AIGMCompanionProfile> profiles, out string strippedSpeech)
        {
            strippedSpeech = null;
            if (String.IsNullOrWhiteSpace(speech))
                return false;

            List<string> aliases = new List<string>();
            foreach (AIGMCompanionProfile profile in EnumerateProfiles(profiles))
            {
                foreach (string alias in GetProfileAliases(profile))
                    aliases.Add(alias);
            }

            return TryStripLeadingAlias(speech, aliases, out strippedSpeech);
        }

        private static bool TryStripLeadingAlias(string speech, IEnumerable<string> aliases, out string strippedSpeech)
        {
            strippedSpeech = null;
            if (String.IsNullOrWhiteSpace(speech) || aliases == null)
                return false;

            foreach (string alias in aliases)
            {
                string normalizedAlias = NormalizeSpeech(alias);
                if (String.IsNullOrWhiteSpace(normalizedAlias))
                    continue;

                if (speech.Equals(normalizedAlias))
                {
                    strippedSpeech = String.Empty;
                    return true;
                }

                if (speech.StartsWith(normalizedAlias + " "))
                {
                    strippedSpeech = speech.Substring(normalizedAlias.Length).Trim();
                    return true;
                }
            }

            return false;
        }

        private static bool TryParseCompanionDirectedPhrase(string speech, string prefix, IEnumerable<AIGMCompanionProfile> profiles, out string companionId)
        {
            companionId = null;
            if (String.IsNullOrWhiteSpace(speech) || String.IsNullOrWhiteSpace(prefix) || !speech.StartsWith(prefix))
                return false;

            string payload = NormalizeSpeech(speech.Substring(prefix.Length).Trim());
            if (String.IsNullOrWhiteSpace(payload))
                return false;

            foreach (AIGMCompanionProfile profile in EnumerateProfiles(profiles))
            {
                if (MatchesProfileAlias(profile, payload))
                {
                    companionId = !String.IsNullOrWhiteSpace(profile.Id) ? profile.Id : payload;
                    return true;
                }
            }

            return false;
        }

        private static bool MatchesProfileAlias(AIGMCompanionProfile profile, string value)
        {
            if (profile == null || String.IsNullOrWhiteSpace(value))
                return false;

            string normalized = NormalizeSpeech(value);
            foreach (string alias in GetProfileAliases(profile))
            {
                if (NormalizeSpeech(alias).Equals(normalized, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static IEnumerable<string> GetProfileAliases(AIGMCompanionProfile profile)
        {
            if (profile == null)
                yield break;

            if (!String.IsNullOrWhiteSpace(profile.DisplayName))
                yield return profile.DisplayName;

            if (profile.Aliases != null)
            {
                for (int i = 0; i < profile.Aliases.Length; i++)
                {
                    string alias = profile.Aliases[i];
                    if (!String.IsNullOrWhiteSpace(alias))
                        yield return alias;
                }
            }
        }

        private static IEnumerable<AIGMCompanionProfile> EnumerateProfiles(IEnumerable<AIGMCompanionProfile> profiles)
        {
            if (profiles == null)
                profiles = DefaultProfiles;

            foreach (AIGMCompanionProfile profile in profiles)
            {
                if (profile != null)
                    yield return profile;
            }
        }

        private static bool Make(string kind, string rawSpeech, out AIGMCompanionIntent intent)
        {
            intent = new AIGMCompanionIntent();
            intent.Kind = kind;
            intent.RawText = rawSpeech;
            return true;
        }

        private static string NormalizeSpeech(string speech)
        {
            if (String.IsNullOrWhiteSpace(speech))
                return String.Empty;

            speech = speech.Replace(",", " ").Replace(".", " ").Replace("!", " ").Replace("?", " ").Replace(";", " ").Replace(":", " ");
            speech = speech.Replace("yoruself", "yourself");
            speech = speech.Replace("youyrself", "yourself");
            speech = speech.Replace("youself", "yourself");
            speech = speech.Replace("youreself", "yourself");
            speech = speech.Replace("bandages yoruself", "bandage yourself");
            speech = speech.Replace("bandages yourself", "bandage yourself");

            speech = ReplaceWholeSpeech(speech, "track npc", "track npcs");
            speech = ReplaceWholeSpeech(speech, "scan npc", "scan npcs");
            speech = ReplaceWholeSpeech(speech, "scan for npc", "scan npcs");
            speech = ReplaceWholeSpeech(speech, "track for npc", "track npcs");
            speech = ReplaceWholeSpeech(speech, "scan for npcs", "scan npcs");
            speech = ReplaceWholeSpeech(speech, "track for npcs", "track npcs");
            speech = ReplaceWholeSpeech(speech, "scan for animal", "scan animals");
            speech = ReplaceWholeSpeech(speech, "track for animal", "track animals");
            speech = ReplaceWholeSpeech(speech, "scan for animals", "scan animals");
            speech = ReplaceWholeSpeech(speech, "track for animals", "track animals");
            speech = ReplaceWholeSpeech(speech, "scan for monster", "scan monsters");
            speech = ReplaceWholeSpeech(speech, "track for monster", "track monsters");
            speech = ReplaceWholeSpeech(speech, "scan for monsters", "scan monsters");
            speech = ReplaceWholeSpeech(speech, "track for monsters", "track monsters");
            speech = ReplaceWholeSpeech(speech, "scan for player", "scan players");
            speech = ReplaceWholeSpeech(speech, "track for player", "track players");
            speech = ReplaceWholeSpeech(speech, "scan for players", "scan players");
            speech = ReplaceWholeSpeech(speech, "track for players", "track players");
            speech = ReplaceWholeSpeech(speech, "scan for humans", "scan humans");
            speech = ReplaceWholeSpeech(speech, "scan for human", "scan humans");
            speech = ReplaceWholeSpeech(speech, "begin track", "begin tracking");
            speech = ReplaceWholeSpeech(speech, "start track", "start tracking");
            speech = ReplaceWholeSpeech(speech, "start tracking please", "start tracking");
            speech = ReplaceWholeSpeech(speech, "stop tracking please", "stop tracking");
            speech = ReplaceWholeSpeech(speech, "track monsters please", "track monsters");
            speech = ReplaceWholeSpeech(speech, "track animals please", "track animals");
            speech = ReplaceWholeSpeech(speech, "track npcs please", "track npcs");
            speech = ReplaceWholeSpeech(speech, "track players please", "track players");
            speech = ReplaceWholeSpeech(speech, "stop traveling please", "stop traveling");
            speech = ReplaceWholeSpeech(speech, "stop travel please", "stop travel");
            speech = ReplaceWholeSpeech(speech, "travel status please", "travel status");
            speech = ReplaceWholeSpeech(speech, "hunt monsters please", "hunt monsters");
            speech = ReplaceWholeSpeech(speech, "attack monsters please", "attack monsters");
            speech = ReplaceWholeSpeech(speech, "clear monsters please", "clear monsters");
            speech = ReplaceWholeSpeech(speech, "start hunting please", "start hunting");
            speech = ReplaceWholeSpeech(speech, "stop hunting please", "stop hunting");
            speech = ReplaceWholeSpeech(speech, "monster status please", "monster status");
            speech = ReplaceWholeSpeech(speech, "loot please", "loot");
            speech = ReplaceWholeSpeech(speech, "loot corpses please", "loot corpses");
            speech = ReplaceWholeSpeech(speech, "gather loot please", "gather loot");
            speech = ReplaceWholeSpeech(speech, "gather gold please", "gather gold");
            speech = ReplaceWholeSpeech(speech, "stop looting please", "stop looting");

            while (speech.Contains("  "))
                speech = speech.Replace("  ", " ");

            return speech.Trim();
        }

        private static string CanonicalizeMonsterHuntCommand(string speech)
        {
            if (String.IsNullOrWhiteSpace(speech))
                return null;

            string normalized = speech.Trim();
            if (normalized.Equals("hunt monsters") || normalized.Equals("attack monsters") || normalized.Equals("clear monsters") || normalized.Equals("start hunting") || normalized.Equals("start monster tracking") || normalized.Equals("hunt closest monster") || normalized.Equals("track and hunt monsters") || normalized.Equals("track and hunt monster") || normalized.Equals("find and kill monsters") || normalized.Equals("find and kill monster"))
                return "start_monster_hunt";

            if (normalized.Equals("stop hunting") || normalized.Equals("stop tracking monsters") || normalized.Equals("stop monster tracking"))
                return "stop_monster_hunt";

            if (normalized.Equals("monster status") || normalized.Equals("hunt status") || normalized.Equals("hunting status"))
                return "report_monster_hunt_status";

            return null;
        }

        private static string CanonicalizeLootCommand(string speech)
        {
            if (String.IsNullOrWhiteSpace(speech))
                return null;

            string normalized = speech.Trim();

            if (normalized.Equals("auto loot off") || normalized.Equals("disable auto loot") || normalized.Equals("companions auto loot off") || normalized.Equals("everyone auto loot off") || normalized.Equals("all auto loot off") || normalized.Equals("stop auto loot"))
                return "stop_auto_loot";

            if (normalized.Equals("stop looting") || normalized.Equals("stop loot") || normalized.Equals("leave the bodies") || normalized.Equals("leave corpses"))
                return "stop_looting";

            if (normalized.Equals("auto loot status") || normalized.Equals("auto looting status"))
                return "report_auto_loot_status";

            if (normalized.Equals("loot status") || normalized.Equals("looting status"))
                return "report_loot_status";

            if (normalized.Equals("burden") || normalized.Equals("loot burden") || normalized.Equals("pack burden") || normalized.Equals("inventory burden") || normalized.Equals("how heavy are you") || normalized.Equals("how full is your pack"))
                return "report_burden";

            if (normalized.Equals("unload junk") || normalized.Equals("drop junk") || normalized.Equals("lighten pack") || normalized.Equals("companions unload junk") || normalized.Equals("companions drop junk") || normalized.Equals("everyone unload junk") || normalized.Equals("all unload junk"))
                return "unload_junk";

            if (normalized.Equals("auto loot on")
                || normalized.Equals("enable auto loot")
                || normalized.Equals("companions auto loot on")
                || normalized.Equals("everyone auto loot on")
                || normalized.Equals("all auto loot on")
                || normalized.Equals("auto loot gold")
                || normalized.Equals("auto loot supplies")
                || normalized.Equals("auto loot all")
                || normalized.Equals("auto loot default"))
                return "auto_loot";

            if (normalized.StartsWith("auto loot ", StringComparison.Ordinal)
                || normalized.StartsWith("companions auto loot ", StringComparison.Ordinal)
                || normalized.StartsWith("everyone auto loot ", StringComparison.Ordinal)
                || normalized.StartsWith("all auto loot ", StringComparison.Ordinal))
                return "auto_loot";

            if (normalized.Equals("loot")
                || normalized.Equals("loot corpse")
                || normalized.Equals("loot corpses")
                || normalized.Equals("loot body")
                || normalized.Equals("loot bodies")
                || normalized.Equals("loot the bodies")
                || normalized.Equals("check corpse")
                || normalized.Equals("check corpses")
                || normalized.Equals("check the corpses")
                || normalized.Equals("check body")
                || normalized.Equals("check bodies")
                || normalized.Equals("check the bodies")
                || normalized.Equals("gather loot")
                || normalized.Equals("gather gold")
                || normalized.Equals("loot gold")
                || normalized.Equals("loot supplies")
                || normalized.Equals("loot equipment")
                || normalized.Equals("loot all")
                || normalized.Equals("companions loot")
                || normalized.Equals("companions gather loot")
                || normalized.Equals("companions gather gold")
                || normalized.Equals("everyone loot")
                || normalized.Equals("all loot"))
                return "loot_nearby";

            if (normalized.StartsWith("loot ", StringComparison.Ordinal)
                || normalized.StartsWith("gather loot", StringComparison.Ordinal)
                || normalized.StartsWith("gather gold", StringComparison.Ordinal)
                || normalized.StartsWith("check corpse", StringComparison.Ordinal)
                || normalized.StartsWith("check bodies", StringComparison.Ordinal)
                || normalized.StartsWith("check the bodies", StringComparison.Ordinal)
                || normalized.StartsWith("companions loot", StringComparison.Ordinal)
                || normalized.StartsWith("companions gather", StringComparison.Ordinal))
                return "loot_nearby";

            return null;
        }

        private static string CanonicalizePotionCommand(string speech)
        {
            if (String.IsNullOrWhiteSpace(speech))
                return null;

            string normalized = speech.Trim();

            if (normalized.Equals("stop using potions")
                || normalized.Equals("companions stop using potions")
                || normalized.Equals("stop potion support")
                || normalized.Equals("disable potions")
                || normalized.Equals("disable potion support"))
                return "stop_potion_support";

            if (normalized.Equals("potion status")
                || normalized.Equals("potions status")
                || normalized.Equals("companions potion status"))
                return "report_potion_status";

            if (normalized.Equals("drink a cure potion")
                || normalized.Equals("drink cure potion")
                || normalized.Equals("drink a heal potion")
                || normalized.Equals("drink heal potion")
                || normalized.Equals("use refresh if tired")
                || normalized.Equals("use cure potions")
                || normalized.Equals("use healing potions"))
                return "use_potion";

            if (normalized.StartsWith("drink a ", StringComparison.Ordinal)
                || normalized.StartsWith("drink ", StringComparison.Ordinal)
                || normalized.StartsWith("use cure potion", StringComparison.Ordinal)
                || normalized.StartsWith("use cure potions", StringComparison.Ordinal)
                || normalized.StartsWith("use healing potion", StringComparison.Ordinal)
                || normalized.StartsWith("use healing potions", StringComparison.Ordinal)
                || normalized.StartsWith("use refresh", StringComparison.Ordinal))
                return "use_potion";

            if (normalized.Equals("use potions")
                || normalized.Equals("use potion")
                || normalized.Equals("companions use potions")
                || normalized.Equals("everyone use potions")
                || normalized.Equals("all use potions")
                || normalized.Equals("use strength and agility potions in battle"))
                return "potion_support";

            if (normalized.StartsWith("use potions", StringComparison.Ordinal)
                || normalized.StartsWith("use potion", StringComparison.Ordinal)
                || normalized.StartsWith("companions use potions", StringComparison.Ordinal)
                || normalized.StartsWith("everyone use potions", StringComparison.Ordinal)
                || normalized.StartsWith("all use potions", StringComparison.Ordinal))
                return "potion_support";

            return null;
        }

        private static string CanonicalizeSpellCommand(string speech)
        {
            if (String.IsNullOrWhiteSpace(speech))
                return null;

            string normalized = speech.Trim();
            if (normalized.Equals("stop using spells") || normalized.Equals("stop casting") || normalized.Equals("stop support magic") || normalized.Equals("companions stop using spells"))
                return "stop_spell_support";

            if (normalized.Equals("spell status") || normalized.Equals("spells status") || normalized.Equals("companions spell status"))
                return "report_spell_status";

            if (normalized.Equals("use spells") || normalized.Equals("use support magic") || normalized.Equals("companions use support magic") || normalized.Equals("companions use spells"))
                return "spell_support";

            if (normalized.StartsWith("use spells", StringComparison.Ordinal)
                || normalized.StartsWith("use support magic", StringComparison.Ordinal)
                || normalized.StartsWith("companions use support magic", StringComparison.Ordinal)
                || normalized.StartsWith("companions use spells", StringComparison.Ordinal))
                return "spell_support";

            if (normalized.Contains("with magic") || normalized.StartsWith("bless ", StringComparison.Ordinal) || normalized.StartsWith("cast heal", StringComparison.Ordinal) || normalized.StartsWith("cast cure", StringComparison.Ordinal))
                return "use_spell";

            return null;
        }

        private static string CanonicalizeTrackingCommand(string speech)
        {
            if (String.IsNullOrWhiteSpace(speech))
                return null;

            string normalized = speech.Trim();
            if (normalized.Equals("start tracking") || normalized.Equals("begin tracking") || normalized.Equals("track cycle on") || normalized.Equals("track on") || normalized.Equals("track") || normalized.Equals("start tracking all") || normalized.Equals("all start tracking") || normalized.Equals("companions start tracking") || normalized.Equals("everyone start tracking"))
                return "start_tracking_cycle";

            if (normalized.Equals("stop tracking") || normalized.Equals("end tracking") || normalized.Equals("track cycle off") || normalized.Equals("track off"))
                return "stop_tracking_cycle";

            if (normalized.Equals("tracking status")
                || normalized.Equals("track status")
                || normalized.Equals("report tracking")
                || normalized.Equals("report tracking status")
                || normalized.Equals("what is your tracking status")
                || normalized.Equals("what are you tracking")
                || normalized.Equals("what do you sense")
                || normalized.Equals("what do you sense nearby")
                || normalized.Equals("any tracks")
                || normalized.Equals("what tracks")
                || normalized.Equals("what changed")
                || normalized.Equals("all tracking status"))
                return "report_tracking_status";

            return null;
        }

        private static string ReplaceWholeSpeech(string speech, string from, string to)
        {
            if (String.IsNullOrWhiteSpace(speech) || String.IsNullOrWhiteSpace(from) || to == null)
                return speech;

            string trimmed = speech.Trim();
            if (trimmed.Equals(from, StringComparison.Ordinal))
                return to;

            if (trimmed.EndsWith(" please", StringComparison.Ordinal) && trimmed.Substring(0, trimmed.Length - 7).Equals(from, StringComparison.Ordinal))
                return to + " please";

            return speech;
        }

        private static bool TryBuildTravelIntent(string rawSpeech, string speech, bool explicitlyAddressed, out AIGMCompanionIntent intent)
        {
            intent = null;
            if (String.IsNullOrWhiteSpace(speech))
                return false;

            string[] prefixes = new[] { "go to ", "go ", "travel to ", "head to ", "move to ", "walk to ", "navigate to ", "take me to ", "take us to ", "lead me to ", "lead us to " };
            string payload = null;
            for (int i = 0; i < prefixes.Length; i++)
            {
                if (speech.StartsWith(prefixes[i]))
                {
                    payload = speech.Substring(prefixes[i].Length).Trim();
                    break;
                }
            }

            if (String.IsNullOrWhiteSpace(payload))
                return false;

            intent = new AIGMCompanionIntent();
            intent.Kind = AIGMCompanionIntentKind.TravelToDestination;
            intent.RawText = rawSpeech;
            intent.ExplicitlyAddressed = explicitlyAddressed;
            intent.DestinationName = payload;

            Match match = Regex.Match(payload, @"^(\d{1,5})\s+(-?\d{1,5})(?:\s+(-?\d{1,5}))?$", RegexOptions.CultureInvariant);
            if (match.Success)
            {
                int x;
                int y;
                int z = 0;
                if (Int32.TryParse(match.Groups[1].Value, out x) && Int32.TryParse(match.Groups[2].Value, out y))
                {
                    if (match.Groups[3].Success)
                        Int32.TryParse(match.Groups[3].Value, out z);

                    intent.DestinationPoint = new Point3D(x, y, z);
                }
            }

            return true;
        }

        private static Mobile ResolveAttackTarget(BaseHire companion, Mobile speaker)
        {
            if (companion == null)
                return null;

            Mobile speakerCombatant = speaker != null ? speaker.Combatant as Mobile : null;
            if (speakerCombatant != null && !speakerCombatant.Deleted && speakerCombatant.Alive)
                return speakerCombatant;

            Mobile owner = companion.GetOwner();
            Mobile ownerCombatant = owner != null ? owner.Combatant as Mobile : null;
            if (ownerCombatant != null && !ownerCombatant.Deleted && ownerCombatant.Alive)
                return ownerCombatant;

            Mobile companionCombatant = companion.Combatant as Mobile;
            if (companionCombatant != null && !companionCombatant.Deleted && companionCombatant.Alive)
                return companionCombatant;

            if (speaker != null && companion.Map != null)
            {
                Mobile nearestHostile = null;
                double bestDistance = Double.MaxValue;
                IPooledEnumerable mobiles = companion.Map.GetMobilesInRange(companion.Location, 8);
                foreach (Mobile mob in mobiles)
                {
                    if (mob == null || mob.Deleted || !mob.Alive || mob == companion || mob == speaker || mob == owner)
                        continue;

                    if (mob.AccessLevel > AccessLevel.Player)
                        continue;

                    bool clearlyHostile = mob.Criminal || mob.Combatant == companion || mob.Combatant == speaker || mob.Combatant == owner;
                    if (!clearlyHostile)
                        continue;

                    double distance = companion.GetDistanceToSqrt(mob.Location);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        nearestHostile = mob;
                    }
                }
                mobiles.Free();
                if (nearestHostile != null)
                    return nearestHostile;
            }

            return null;
        }
    }
}
