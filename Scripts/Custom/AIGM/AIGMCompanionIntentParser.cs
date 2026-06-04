using System;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionIntentParser
    {
        public static bool IsClearlyAddressedToDifferentCompanion(BaseHire companion, string speech)
        {
            if (companion == null || String.IsNullOrWhiteSpace(speech))
                return false;

            string normalized = NormalizeSpeech(speech.Trim().ToLowerInvariant());
            bool addressedToDak = normalized.StartsWith("dak ") || normalized.StartsWith("dakeyras ") || normalized.StartsWith("waylander ");
            bool addressedToDanyal = normalized.StartsWith("danyal ");
            bool addressedToDardalion = normalized.StartsWith("dardalion ") || normalized.StartsWith("dar ");

            if (companion is AIGMCompanionDakeyras)
                return addressedToDanyal || addressedToDardalion;

            if (companion is AIGMCompanionDanyal)
                return addressedToDak || addressedToDardalion;

            if (companion is AIGMCompanionDardalion)
                return addressedToDak || addressedToDanyal;

            return false;
        }

        public static bool TryParse(BaseHire companion, Mobile speaker, string rawSpeech, out AIGMCompanionIntent intent)
        {
            intent = null;

            if (companion == null || speaker == null || String.IsNullOrWhiteSpace(rawSpeech))
                return false;

            string speech = rawSpeech.Trim().ToLowerInvariant();
            speech = NormalizeSpeech(speech);

            bool explicitlyAddressed = false;
            string addressedSpeech;
            if (TryExtractAddressedCommand(companion, speech, out addressedSpeech))
            {
                explicitlyAddressed = true;
                speech = NormalizeSpeech(addressedSpeech);
            }
            else
            {
                string strippedOtherNameSpeech;
                if (TryStripAnyKnownCompanionPrefix(speech, out strippedOtherNameSpeech))
                    speech = NormalizeSpeech(strippedOtherNameSpeech);
            }

            if (speech.Equals("where are you") || speech.Contains("report location") || speech.Contains("what region are you in") || speech.Contains("what are your coordinates"))
            {
                Make(AIGMCompanionIntentKind.ReportLocation, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Equals("scan area") || speech.Equals("scan the area") || speech.Contains("look around"))
            {
                Make(AIGMCompanionIntentKind.ScanArea, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Equals("track animals") || speech.Equals("scan animals") || speech.Equals("track animal") || speech.Equals("scan for animals") || speech.Equals("track for animals") || speech.Equals("scan for animal") || speech.Equals("track for animal"))
            {
                Make(AIGMCompanionIntentKind.TrackAnimals, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Equals("track monsters") || speech.Equals("scan monsters") || speech.Equals("track monster") || speech.Equals("track hostile") || speech.Equals("track hostiles") || speech.Equals("scan for monsters") || speech.Equals("track for monsters") || speech.Equals("scan for monster") || speech.Equals("track for monster"))
            {
                Make(AIGMCompanionIntentKind.TrackMonsters, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Equals("track npcs") || speech.Equals("track npc") || speech.Equals("track human npcs") || speech.Equals("track human npc") || speech.Equals("scan npcs") || speech.Equals("scan npc") || speech.Equals("track townsfolk") || speech.Equals("scan for npcs") || speech.Equals("track for npcs") || speech.Equals("scan for npc") || speech.Equals("track for npc") || speech.Equals("scan humans") || speech.Equals("scan human") || speech.Equals("scan for humans") || speech.Equals("scan for human"))
            {
                Make(AIGMCompanionIntentKind.TrackHumanNPCs, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Equals("track players") || speech.Equals("scan players") || speech.Equals("track traveler") || speech.Equals("track travelers") || speech.Equals("scan for players") || speech.Equals("track for players") || speech.Equals("scan for player") || speech.Equals("track for player"))
            {
                Make(AIGMCompanionIntentKind.TrackPlayers, rawSpeech, out intent);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Equals("report threats") || speech.Contains("report danger") || speech.Contains("report hostiles") || speech.Contains("what do you sense"))
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

            if (speech.StartsWith("go to ") || speech.StartsWith("travel to ") || speech.StartsWith("head to "))
            {
                intent = new AIGMCompanionIntent();
                intent.Kind = AIGMCompanionIntentKind.Come;
                intent.RawText = rawSpeech;
                intent.DestinationName = ExtractDestinationName(speech);
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

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

            if (speech.Equals("follow danyal") || speech.Equals("follow dakeyras") || speech.Equals("follow dardalion") || speech.Equals("follow dar"))
            {
                intent = new AIGMCompanionIntent();
                intent.Kind = AIGMCompanionIntentKind.FollowCompanion;
                intent.RawText = rawSpeech;
                intent.DestinationName = speech.Replace("follow ", String.Empty).Trim();
                intent.ExplicitlyAddressed = explicitlyAddressed;
                return true;
            }

            if (speech.Equals("greet danyal") || speech.Equals("greet dakeyras") || speech.Equals("greet dardalion") || speech.Equals("hello danyal") || speech.Equals("hello dakeyras") || speech.Equals("hello dardalion") || speech.Equals("say hello to danyal") || speech.Equals("say hello to dakeyras") || speech.Equals("say hello to dardalion"))
            {
                intent = new AIGMCompanionIntent();
                intent.Kind = AIGMCompanionIntentKind.GreetCompanion;
                intent.RawText = rawSpeech;
                intent.DestinationName = speech.Replace("greet ", String.Empty).Replace("hello ", String.Empty).Replace("say hello to ", String.Empty).Trim();
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

            if (speech.Contains("guard me") || speech.Contains("protect me") || speech.Contains("defend me"))
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

        private static bool TryExtractAddressedCommand(BaseHire companion, string speech, out string addressedSpeech)
        {
            addressedSpeech = null;
            if (companion == null || String.IsNullOrWhiteSpace(speech))
                return false;

            string companionName = NormalizeSpeech((companion.Name ?? String.Empty).ToLowerInvariant());
            string[] aliases = companion is AIGMCompanionDakeyras
                ? new[] { companionName, "dak", "waylander" }
                : companion is AIGMCompanionDardalion
                    ? new[] { companionName, "dar" }
                    : new[] { companionName };

            for (int i = 0; i < aliases.Length; i++)
            {
                string alias = NormalizeSpeech(aliases[i]);
                if (String.IsNullOrWhiteSpace(alias))
                    continue;

                if (speech == alias)
                {
                    addressedSpeech = String.Empty;
                    return true;
                }

                if (speech.StartsWith(alias + " "))
                {
                    addressedSpeech = speech.Substring(alias.Length).Trim();
                    return true;
                }
            }

            return false;
        }

        private static bool TryStripAnyKnownCompanionPrefix(string speech, out string strippedSpeech)
        {
            strippedSpeech = null;
            if (String.IsNullOrWhiteSpace(speech))
                return false;

            string[] aliases = new[] { "dakeyras", "dak", "waylander", "danyal", "dardalion", "dar" };
            for (int i = 0; i < aliases.Length; i++)
            {
                string alias = aliases[i];
                if (speech.Equals(alias))
                {
                    strippedSpeech = String.Empty;
                    return true;
                }

                if (speech.StartsWith(alias + " "))
                {
                    strippedSpeech = speech.Substring(alias.Length).Trim();
                    return true;
                }
            }

            return false;
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
            speech = ReplaceWholeSpeech(speech, "go to britain moongate", "go to britain");

            while (speech.Contains("  "))
                speech = speech.Replace("  ", " ");

            return speech.Trim();
        }

        private static string CanonicalizeTrackingCommand(string speech)
        {
            if (String.IsNullOrWhiteSpace(speech))
                return null;

            string normalized = speech.Trim();
            if (normalized.Equals("start tracking") || normalized.Equals("begin tracking") || normalized.Equals("track cycle on") || normalized.Equals("track on") || normalized.Equals("track"))
                return "start_tracking_cycle";

            if (normalized.Equals("stop tracking") || normalized.Equals("end tracking") || normalized.Equals("track cycle off") || normalized.Equals("track off"))
                return "stop_tracking_cycle";

            if (normalized.Equals("tracking status") || normalized.Equals("track status") || normalized.Equals("report tracking") || normalized.Equals("report tracking status") || normalized.Equals("what is your tracking status"))
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

        private static string ExtractDestinationName(string speech)
        {
            if (String.IsNullOrWhiteSpace(speech))
                return null;

            string normalized = speech;
            string[] prefixes = new[] { "go to ", "travel to ", "head to " };
            for (int i = 0; i < prefixes.Length; i++)
            {
                if (normalized.StartsWith(prefixes[i]))
                    return normalized.Substring(prefixes[i].Length).Trim();
            }

            return null;
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



