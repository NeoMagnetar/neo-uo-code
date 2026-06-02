using System;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionIntentParser
    {
        public static bool TryParse(BaseHire companion, Mobile speaker, string rawSpeech, out AIGMCompanionIntent intent)
        {
            intent = null;

            if (companion == null || speaker == null || String.IsNullOrWhiteSpace(rawSpeech))
                return false;

            string speech = rawSpeech.Trim().ToLowerInvariant();

            speech = NormalizeSpeech(speech);

            if (speech.Equals("where are you") || speech.Contains("report location") || speech.Contains("what region are you in") || speech.Contains("what are your coordinates"))
                return Make(AIGMCompanionIntentKind.ReportLocation, rawSpeech, out intent);

            if (speech.Equals("scan area") || speech.Equals("scan the area") || speech.Contains("look around"))
                return Make(AIGMCompanionIntentKind.ScanArea, rawSpeech, out intent);

            if (speech.Equals("track animals") || speech.Contains("scan animals"))
                return Make(AIGMCompanionIntentKind.TrackAnimals, rawSpeech, out intent);

            if (speech.Equals("track monsters") || speech.Contains("scan monsters"))
                return Make(AIGMCompanionIntentKind.TrackMonsters, rawSpeech, out intent);

            if (speech.Equals("track npcs") || speech.Equals("track human npcs") || speech.Equals("track human npc") || speech.Contains("scan npcs"))
                return Make(AIGMCompanionIntentKind.TrackHumanNPCs, rawSpeech, out intent);

            if (speech.Equals("track players") || speech.Contains("scan players"))
                return Make(AIGMCompanionIntentKind.TrackPlayers, rawSpeech, out intent);

            if (speech.Equals("start tracking") || speech.Equals("begin tracking") || speech.Equals("track on") || speech.Equals("enable tracking"))
                return Make(AIGMCompanionIntentKind.StartTracking, rawSpeech, out intent);

            if (speech.Equals("stop tracking") || speech.Equals("end tracking") || speech.Equals("track off") || speech.Equals("disable tracking"))
                return Make(AIGMCompanionIntentKind.StopTracking, rawSpeech, out intent);

            if (speech.Equals("tracking status") || speech.Equals("report tracking") || speech.Equals("what is your tracking status"))
                return Make(AIGMCompanionIntentKind.ReportTrackingStatus, rawSpeech, out intent);

            if (speech.Equals("report threats") || speech.Contains("report danger") || speech.Contains("report hostiles") || speech.Contains("what do you sense"))
                return Make(AIGMCompanionIntentKind.ReportThreats, rawSpeech, out intent);

            if (speech.Equals("report position") || speech.Equals("current coordinates") || speech.Equals("where am i"))
                return Make(AIGMCompanionIntentKind.ReportLocation, rawSpeech, out intent);

            if (speech.Equals("track people") || speech.Equals("track humans"))
                return Make(AIGMCompanionIntentKind.TrackPlayers, rawSpeech, out intent);

            if (speech.Equals("share what you see") || speech.Equals("share awareness") || speech.Contains("share what you sense"))
                return Make(AIGMCompanionIntentKind.ShareAwareness, rawSpeech, out intent);

            if (speech.StartsWith("go to ") || speech.StartsWith("travel to ") || speech.StartsWith("head to "))
            {
                intent = new AIGMCompanionIntent();
                intent.Kind = AIGMCompanionIntentKind.TravelToDestination;
                intent.RawText = rawSpeech;
                intent.DestinationName = ExtractDestinationName(speech);
                return !String.IsNullOrWhiteSpace(intent.DestinationName);
            }

            if (speech.Equals("stop traveling") || speech.Equals("cancel travel") || speech.Equals("stop travel") || speech.Equals("stop moving") || speech.Equals("cancel traveling"))
                return Make(AIGMCompanionIntentKind.StopTravel, rawSpeech, out intent);

            if (speech.Equals("where are you headed") || speech.Equals("travel status") || speech.Equals("what is your travel status") || speech.Equals("what are you doing travel wise"))
                return Make(AIGMCompanionIntentKind.ReportTravelStatus, rawSpeech, out intent);

            if (speech.Equals("return home"))
                return Make(AIGMCompanionIntentKind.ReturnHome, rawSpeech, out intent);

            if (speech.Equals("follow danyal") || speech.Equals("follow dakeyras"))
            {
                intent = new AIGMCompanionIntent();
                intent.Kind = AIGMCompanionIntentKind.FollowCompanion;
                intent.RawText = rawSpeech;
                intent.DestinationName = speech.Replace("follow ", String.Empty).Trim();
                return true;
            }

            if (speech.Contains("follow me") || speech.Contains("follow"))
                return Make(AIGMCompanionIntentKind.FollowOwner, rawSpeech, out intent);

            if (speech.Contains("come here") || speech.Contains("come to me") || speech.Equals("come"))
                return Make(AIGMCompanionIntentKind.Come, rawSpeech, out intent);

            if (speech.Contains("stop fighting") || speech.Contains("disengage") || speech.Contains("stop attack") || speech.Contains("stop attacking"))
                return Make(AIGMCompanionIntentKind.StopCombat, rawSpeech, out intent);

            if (speech.Contains("guard me") || speech.Contains("protect me") || speech.Contains("defend me"))
                return Make(AIGMCompanionIntentKind.GuardOwner, rawSpeech, out intent);

            if (speech.Contains("bandage yourself") || speech.Contains("bandage self") || speech.Contains("bandage myself") || speech.Contains("bandage your own self"))
                return Make(AIGMCompanionIntentKind.BandageSelf, rawSpeech, out intent);

            if (speech.Contains("bandage me") || speech.Contains("bandage my wounds") || speech.Contains("bandage owner") || speech.Contains("use bandages on me"))
                return Make(AIGMCompanionIntentKind.BandageOwner, rawSpeech, out intent);

            if (speech.Contains("heal yourself") || speech.Contains("heal self") || speech.Contains("heal myself"))
                return Make(AIGMCompanionIntentKind.HealSelf, rawSpeech, out intent);

            if (speech.Contains("heal me") || speech.Contains("heal my wounds") || speech.Contains("heal owner"))
                return Make(AIGMCompanionIntentKind.HealOwner, rawSpeech, out intent);

            if (speech.Contains("cure yourself") || speech.Contains("cure self"))
                return Make(AIGMCompanionIntentKind.CureSelf, rawSpeech, out intent);

            if (speech.Contains("cure me") || speech.Contains("cure my poison"))
                return Make(AIGMCompanionIntentKind.CureOwner, rawSpeech, out intent);

            if (speech.Contains("use healing"))
                return Make(AIGMCompanionIntentKind.UseHealingSkill, rawSpeech, out intent);

            if (speech.Equals("use bandages") || speech.Contains("use a bandage") || speech.Contains("use bandage") || speech.Contains("use bandages on yourself") || speech.Contains("use bandages self"))
                return Make(AIGMCompanionIntentKind.UseBandages, rawSpeech, out intent);

            if (speech.Contains("cast heal"))
                return Make(AIGMCompanionIntentKind.CastHeal, rawSpeech, out intent);

            if (speech.Contains("cast cure"))
                return Make(AIGMCompanionIntentKind.CastCure, rawSpeech, out intent);

            if (speech.Contains("stay") || speech.Equals("stop") || speech.Contains("hold here") || speech.Contains("hold position"))
                return Make(AIGMCompanionIntentKind.Stay, rawSpeech, out intent);

            if (speech.Contains("attack") || speech.Contains("kill") || speech.Contains("fight"))
            {
                intent = new AIGMCompanionIntent();
                intent.Kind = AIGMCompanionIntentKind.AttackTarget;
                intent.RawText = rawSpeech;

                Mobile target = ResolveAttackTarget(companion, speaker);
                if (target != null)
                    intent.TargetSerial = target.Serial.Value;

                return true;
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

            while (speech.Contains("  "))
                speech = speech.Replace("  ", " ");

            return speech.Trim();
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
