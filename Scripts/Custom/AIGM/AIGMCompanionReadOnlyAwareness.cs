using System;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionReadOnlyAwareness
    {
        public static string BuildScanAreaReport(BaseHire companion, Mobile speaker)
        {
            if (companion == null || companion.Deleted || !companion.Alive || companion.Map == null)
                return "I can look over the immediate area, but I have no clear ground to read just now.";

            AIGMSceneContext scene = AIGMSceneScanner.Capture(companion, 8);
            int nearbyFigures = scene != null && scene.NearbyMobiles != null ? scene.NearbyMobiles.Count : 0;
            int possibleThreats = CountPossibleThreats(companion, speaker, scene);
            string posture = possibleThreats > 0 ? "uncertain" : "clear";
            string companionId = GetCompanionId(companion);

            if (String.Equals(companionId, "dakeyras", StringComparison.OrdinalIgnoreCase))
                return String.Format("I scan the area. I see {0} nearby figures, {1} possible threats, and the path around us is {2}. {3} Pursuit remains gated.", nearbyFigures, possibleThreats, posture, AIGMCompanionSkillReadiness.BuildTrackingReadiness(companion));

            if (String.Equals(companionId, "dardalion", StringComparison.OrdinalIgnoreCase))
                return String.Format("I have the ground in sight. There are {0} nearby figures, {1} possible threats, and our immediate footing is {2}. {3}", nearbyFigures, possibleThreats, posture, TrimSentence(AIGMCompanionSkillReadiness.BuildCombatReadiness(companion)));

            return String.Format("I look over the immediate area. I see {0} nearby figures, {1} possible threats, and nothing suggests a clear move yet. {2}", nearbyFigures, possibleThreats, TrimSentence(AIGMCompanionSkillReadiness.BuildSupportReadiness(companion)));
        }

        public static string BuildThreatReport(BaseHire companion, Mobile speaker)
        {
            if (companion == null || companion.Deleted || !companion.Alive || companion.Map == null)
                return "I cannot judge the danger clearly from here.";

            AIGMSceneContext scene = AIGMSceneScanner.Capture(companion, 8);
            int possibleThreats = CountPossibleThreats(companion, speaker, scene);
            string companionId = GetCompanionId(companion);

            if (possibleThreats <= 0)
            {
                if (String.Equals(companionId, "dardalion", StringComparison.OrdinalIgnoreCase))
                    return String.Format("I see no immediate threat pressing on us, though I remain watchful. {0} Attack execution remains gated.", TrimSentence(AIGMCompanionSkillReadiness.BuildCombatReadiness(companion)));

                return String.Format("I see no obvious threat at hand, though I am still watching the ground around us. {0}", TrimSentence(AIGMCompanionSkillReadiness.BuildTrackingReadiness(companion)));
            }

            if (String.Equals(companionId, "dardalion", StringComparison.OrdinalIgnoreCase))
                return String.Format("I count {0} possible threats in sight. {1} I will hold watch, but attack execution remains closed.", possibleThreats, TrimSentence(AIGMCompanionSkillReadiness.BuildCombatReadiness(companion)));

            return String.Format("I can mark {0} possible threats in sight. {1} I can report them, but pursuit remains gated.", possibleThreats, TrimSentence(AIGMCompanionSkillReadiness.BuildTrackingReadiness(companion)));
        }

        public static string BuildShareAwarenessReport(BaseHire companion, Mobile speaker)
        {
            if (companion == null || companion.Deleted || !companion.Alive)
                return "I have little to share just now.";

            Mobile owner = companion.GetOwner();
            string ownerRange = owner != null && owner.Map == companion.Map ? DescribeOwnerProximity(companion, owner) : "our owner is not close at hand";
            AIGMSceneContext scene = companion.Map != null ? AIGMSceneScanner.Capture(companion, 8) : null;
            int figures = scene != null && scene.NearbyMobiles != null ? scene.NearbyMobiles.Count : 0;
            int threats = CountPossibleThreats(companion, speaker, scene);
            string danger = threats > 0 ? "danger is present" : "no immediate threat stands out";
            string readiness = SelectPrimaryReadiness(companion);

            return String.Format("My awareness is this: {0}, I count {1} nearby figures, {2}, and {3}. The action lanes beyond reporting remain gated.", ownerRange, figures, danger, readiness);
        }

        public static string BuildTrackingStatusReport(BaseHire companion, Mobile speaker)
        {
            string companionId = GetCompanionId(companion);
            if (String.Equals(companionId, "dakeyras", StringComparison.OrdinalIgnoreCase))
                return AIGMCompanionTrackingService.GetTrackingStatus(companion, speaker);

            string readiness = AIGMCompanionSkillReadiness.BuildTrackingReadiness(companion);
            return readiness + " Tracking execution is still gated.";
        }

        public static string BuildTravelStatusReport(BaseHire companion, Mobile speaker)
        {
            string readiness = AIGMCompanionSkillReadiness.BuildTravelReadiness(companion);
            string companionId = GetCompanionId(companion);
            if (String.Equals(companionId, "dakeyras", StringComparison.OrdinalIgnoreCase))
                return readiness + " No travel objective is active, and travel execution remains gated.";

            return readiness + " Travel execution remains gated.";
        }

        public static string BuildTrackCategoryReport(BaseHire companion, Mobile speaker, string categoryLabel)
        {
            string readiness = AIGMCompanionSkillReadiness.BuildTrackingReadiness(companion);
            if (String.IsNullOrWhiteSpace(categoryLabel))
                categoryLabel = "signs";

            return String.Format("{0} I can read nearby {1}, but the pursuit cycle is still gated.", readiness, categoryLabel);
        }

        public static string BuildTrackingDeferredReport(BaseHire companion, Mobile speaker)
        {
            string companionId = GetCompanionId(companion);
            if (String.Equals(companionId, "danyal", StringComparison.OrdinalIgnoreCase))
                return AIGMCompanionSkillReadiness.BuildTrackingReadiness(companion) + " I can help observe, but Dakeyras remains the sharper tracker.";

            if (String.Equals(companionId, "dardalion", StringComparison.OrdinalIgnoreCase))
                return AIGMCompanionSkillReadiness.BuildTrackingReadiness(companion) + " I can hold watch, but Dakeyras is better suited to trail-reading.";

            return AIGMCompanionSkillReadiness.BuildTrackingReadiness(companion) + " Pursuit remains gated.";
        }

        public static string BuildSupportStatusReport(BaseHire companion, Mobile speaker)
        {
            return AIGMCompanionHealingService.BuildSupportStatus(companion);
        }

        public static string BuildHealingStatusReport(BaseHire companion, Mobile speaker)
        {
            return AIGMCompanionHealingService.BuildHealingStatus(companion);
        }

        public static string BuildCombatStatusReport(BaseHire companion, Mobile speaker)
        {
            return AIGMCompanionSkillReadiness.BuildCombatReadiness(companion) + " Attack execution remains gated.";
        }

        public static string BuildGuardStatusReport(BaseHire companion, Mobile speaker)
        {
            return AIGMCompanionSkillReadiness.BuildCombatReadiness(companion) + " I am ready to guard, but combat execution remains gated.";
        }

        private static int CountPossibleThreats(BaseHire companion, Mobile speaker, AIGMSceneContext scene)
        {
            if (companion == null || scene == null || scene.NearbyMobiles == null)
                return 0;

            int threats = 0;
            for (int i = 0; i < scene.NearbyMobiles.Count; i++)
            {
                AIGMSceneEntitySummary mob = scene.NearbyMobiles[i];
                if (mob == null)
                    continue;

                string name = mob.Name ?? String.Empty;
                string typeName = mob.TypeName ?? String.Empty;

                if (Matches(companion.Name, name) || Matches(speaker != null ? speaker.Name : null, name))
                    continue;

                if (ContainsAny(typeName, "dragon", "daemon", "lich", "orc", "troll", "ogre", "ettin", "ratman", "mongbat", "headless", "reaper", "elemental"))
                {
                    threats++;
                    continue;
                }

                if (ContainsAny(name, "orc", "daemon", "lich", "brigand", "pirate", "troll", "ogre", "ratman", "headless"))
                    threats++;
            }

            return threats;
        }

        private static string DescribeOwnerProximity(BaseHire companion, Mobile owner)
        {
            int distance = (int)Math.Round(companion.GetDistanceToSqrt(owner.Location));
            if (distance <= 1)
                return "our owner is close beside us";
            if (distance <= 4)
                return "our owner is nearby";
            if (distance <= 8)
                return "our owner is within calling distance";
            return "our owner is farther off than I would like";
        }

        private static string GetCompanionId(BaseHire companion)
        {
            IAIGMCompanionActor actor = companion as IAIGMCompanionActor;
            return actor != null ? actor.CompanionId ?? String.Empty : String.Empty;
        }

        private static string SelectPrimaryReadiness(BaseHire companion)
        {
            string companionId = GetCompanionId(companion);
            if (String.Equals(companionId, "dakeyras", StringComparison.OrdinalIgnoreCase))
                return TrimSentence(AIGMCompanionSkillReadiness.BuildTrackingReadiness(companion));
            if (String.Equals(companionId, "danyal", StringComparison.OrdinalIgnoreCase))
                return TrimSentence(AIGMCompanionSkillReadiness.BuildSupportReadiness(companion));
            if (String.Equals(companionId, "dardalion", StringComparison.OrdinalIgnoreCase))
                return TrimSentence(AIGMCompanionSkillReadiness.BuildCombatReadiness(companion));
            return "my readiness remains measured";
        }

        private static string TrimSentence(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return String.Empty;

            string text = value.Trim();
            while (text.EndsWith(".", StringComparison.Ordinal))
                text = text.Substring(0, text.Length - 1).TrimEnd();
            return text;
        }

        private static bool Matches(string left, string right)
        {
            return !String.IsNullOrWhiteSpace(left)
                && !String.IsNullOrWhiteSpace(right)
                && String.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static bool ContainsAny(string value, params string[] needles)
        {
            if (String.IsNullOrWhiteSpace(value) || needles == null)
                return false;

            string haystack = value.ToLowerInvariant();
            for (int i = 0; i < needles.Length; i++)
            {
                if (!String.IsNullOrWhiteSpace(needles[i]) && haystack.Contains(needles[i].ToLowerInvariant()))
                    return true;
            }

            return false;
        }
    }
}
