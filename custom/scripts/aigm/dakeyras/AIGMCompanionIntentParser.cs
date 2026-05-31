using System;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionIntentParser
    {
        public static bool TryParse(AIGMCompanionDakeyras companion, Mobile speaker, string rawSpeech, out AIGMCompanionIntent intent)
        {
            intent = null;

            if (companion == null || speaker == null || String.IsNullOrWhiteSpace(rawSpeech))
                return false;

            string speech = rawSpeech.Trim().ToLowerInvariant();

            speech = NormalizeSpeech(speech);

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

        private static Mobile ResolveAttackTarget(AIGMCompanionDakeyras companion, Mobile speaker)
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
