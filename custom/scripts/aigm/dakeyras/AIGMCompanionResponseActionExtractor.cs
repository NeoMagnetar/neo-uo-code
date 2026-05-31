using System;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionResponseActionExtractor
    {
        public static bool TryExtract(AIGMCompanionDakeyras companion, Mobile speaker, string originalSpeech, string replyText, out AIGMCompanionIntent intent)
        {
            intent = null;

            if (companion == null || speaker == null)
                return false;

            // Prefer the owner's original speech for intent extraction; the AI reply is only supporting evidence.
            if (!String.IsNullOrWhiteSpace(originalSpeech) && AIGMCompanionIntentParser.TryParse(companion, speaker, originalSpeech, out intent))
                return true;

            if (String.IsNullOrWhiteSpace(replyText))
                return false;

            string text = replyText.Trim().ToLowerInvariant();

            if (text.Contains("guard you") || text.Contains("protect you"))
                return Make(AIGMCompanionIntentKind.GuardOwner, originalSpeech, out intent);

            if (text.Contains("follow") || text.Contains("with you"))
                return Make(AIGMCompanionIntentKind.FollowOwner, originalSpeech, out intent);

            if (text.Contains("bandage") && (text.Contains("my own") || text.Contains("upon yourself") || text.Contains("yourself")))
                return Make(AIGMCompanionIntentKind.BandageSelf, originalSpeech, out intent);

            if (text.Contains("bandage") && (text.Contains("you") || text.Contains("your wounds")))
                return Make(AIGMCompanionIntentKind.BandageOwner, originalSpeech, out intent);

            if (text.Contains("heal myself") || text.Contains("heal yourself") || text.Contains("tend to my own wounds"))
                return Make(AIGMCompanionIntentKind.HealSelf, originalSpeech, out intent);

            if (text.Contains("heal you") || text.Contains("heal your wounds"))
                return Make(AIGMCompanionIntentKind.HealOwner, originalSpeech, out intent);

            if (text.Contains("cast cure") || text.Contains("curative magic") || text.Contains("cleanse affliction"))
                return Make(AIGMCompanionIntentKind.CastCure, originalSpeech, out intent);

            if (text.Contains("cast heal") || text.Contains("healing magic"))
                return Make(AIGMCompanionIntentKind.CastHeal, originalSpeech, out intent);

            if (text.Contains("attack") || text.Contains("strike it down") || text.Contains("bring it down") || text.Contains("ready to defend"))
            {
                intent = new AIGMCompanionIntent();
                intent.Kind = AIGMCompanionIntentKind.AttackTarget;
                intent.RawText = originalSpeech;
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
    }
}
