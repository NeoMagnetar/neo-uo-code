using System;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionThreatClassifier
    {
        public static AIGMCompanionThreatLevel Classify(BaseHire companion, Mobile target, AIGMTrackingCategory category, int distance)
        {
            if (companion == null || target == null)
                return AIGMCompanionThreatLevel.Neutral;

            if (target == companion.GetOwner())
                return AIGMCompanionThreatLevel.Owner;

            if (target is AIGMCompanionDakeyras || target is AIGMCompanionDanyal || target is AIGMCompanionDardalion)
                return AIGMCompanionThreatLevel.KnownCompanion;

            if (category == AIGMTrackingCategory.Players)
                return distance <= 8 ? AIGMCompanionThreatLevel.Interesting : AIGMCompanionThreatLevel.Neutral;

            if (category == AIGMTrackingCategory.HumanNPCs)
                return AIGMCompanionThreatLevel.Neutral;

            if (category == AIGMTrackingCategory.Animals)
                return AIGMCompanionThreatLevel.Neutral;

            if (category == AIGMTrackingCategory.Monsters)
            {
                if (distance <= 3)
                    return AIGMCompanionThreatLevel.ImmediateThreat;

                if (distance <= 8)
                    return AIGMCompanionThreatLevel.PotentialThreat;

                return AIGMCompanionThreatLevel.Interesting;
            }

            return AIGMCompanionThreatLevel.Neutral;
        }
    }
}
