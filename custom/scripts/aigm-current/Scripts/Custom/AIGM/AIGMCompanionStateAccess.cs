using System;
using Server.Mobiles;
using Server;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionStateAccess
    {
        private static readonly System.Collections.Generic.Dictionary<int, AIGMCompanionTravelObjective> TravelObjectives = new System.Collections.Generic.Dictionary<int, AIGMCompanionTravelObjective>();
        private static readonly System.Collections.Generic.Dictionary<int, DateTime> NextTravelPulseUtc = new System.Collections.Generic.Dictionary<int, DateTime>();
        private static readonly System.Collections.Generic.Dictionary<int, DateTime> NextTravelThreatScanUtc = new System.Collections.Generic.Dictionary<int, DateTime>();
        public static bool GetGuardOwnerMode(BaseHire companion)
        {
            AIGMCompanionDakeyras dakeyras = companion as AIGMCompanionDakeyras;
            if (dakeyras != null)
                return dakeyras.GuardOwnerMode;

            AIGMCompanionDanyal danyal = companion as AIGMCompanionDanyal;
            if (danyal != null)
                return danyal.GuardOwnerMode;

            return false;
        }

        public static void SetGuardOwnerMode(BaseHire companion, bool value)
        {
            AIGMCompanionDakeyras dakeyras = companion as AIGMCompanionDakeyras;
            if (dakeyras != null)
            {
                dakeyras.GuardOwnerMode = value;
                return;
            }

            AIGMCompanionDanyal danyal = companion as AIGMCompanionDanyal;
            if (danyal != null)
                danyal.GuardOwnerMode = value;
        }

        public static DateTime GetNextSupportActionUtc(BaseHire companion)
        {
            AIGMCompanionDakeyras dakeyras = companion as AIGMCompanionDakeyras;
            if (dakeyras != null)
                return dakeyras.NextSupportActionUtc;

            AIGMCompanionDanyal danyal = companion as AIGMCompanionDanyal;
            if (danyal != null)
                return danyal.NextSupportActionUtc;

            return DateTime.MinValue;
        }

        public static void SetNextSupportActionUtc(BaseHire companion, DateTime value)
        {
            AIGMCompanionDakeyras dakeyras = companion as AIGMCompanionDakeyras;
            if (dakeyras != null)
            {
                dakeyras.NextSupportActionUtc = value;
                return;
            }

            AIGMCompanionDanyal danyal = companion as AIGMCompanionDanyal;
            if (danyal != null)
                danyal.NextSupportActionUtc = value;
        }

        public static AIGMCompanionTravelObjective GetTravelObjective(BaseHire companion)
        {
            if (companion == null)
                return null;

            AIGMCompanionTravelObjective objective;
            TravelObjectives.TryGetValue(companion.Serial.Value, out objective);
            return objective;
        }

        public static void SetTravelObjective(BaseHire companion, AIGMCompanionTravelObjective objective)
        {
            if (companion == null)
                return;

            if (objective == null)
                TravelObjectives.Remove(companion.Serial.Value);
            else
                TravelObjectives[companion.Serial.Value] = objective;
        }

        public static DateTime GetNextTravelPulseUtc(BaseHire companion)
        {
            if (companion == null)
                return DateTime.MinValue;

            DateTime value;
            return NextTravelPulseUtc.TryGetValue(companion.Serial.Value, out value) ? value : DateTime.MinValue;
        }

        public static void SetNextTravelPulseUtc(BaseHire companion, DateTime value)
        {
            if (companion == null)
                return;

            NextTravelPulseUtc[companion.Serial.Value] = value;
        }

        public static DateTime GetNextTravelThreatScanUtc(BaseHire companion)
        {
            if (companion == null)
                return DateTime.MinValue;

            DateTime value;
            return NextTravelThreatScanUtc.TryGetValue(companion.Serial.Value, out value) ? value : DateTime.MinValue;
        }

        public static void SetNextTravelThreatScanUtc(BaseHire companion, DateTime value)
        {
            if (companion == null)
                return;

            NextTravelThreatScanUtc[companion.Serial.Value] = value;
        }

        public static AIGMExecutionMode GetExecutionMode(BaseHire companion)
        {
            AIGMCompanionDakeyras dakeyras = companion as AIGMCompanionDakeyras;
            if (dakeyras != null)
                return dakeyras.ExecutionMode;

            AIGMCompanionDanyal danyal = companion as AIGMCompanionDanyal;
            if (danyal != null)
                return danyal.ExecutionMode;

            return AIGMExecutionMode.SuggestOnly;
        }
    }
}
