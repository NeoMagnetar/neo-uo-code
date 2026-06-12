using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionTrackingService
    {
        private static readonly Dictionary<int, AIGMCompanionTrackingState> States = new Dictionary<int, AIGMCompanionTrackingState>();

        public static string StartTracking(BaseHire companion, Mobile speaker, AIGMCompanionTrackingMode mode)
        {
            if (!IsValidCompanion(companion))
                return "I cannot begin a tracking watch right now.";

            AIGMCompanionTrackingState state = GetOrCreateState(companion);
            state.Mode = mode == AIGMCompanionTrackingMode.None ? AIGMCompanionTrackingMode.General : mode;
            state.IsActive = true;
            state.StartedUtc = DateTime.UtcNow;
            state.SkillValue = AIGMCompanionSkillReadiness.GetSkillValue(companion, SkillName.Tracking);
            state.SkillTier = AIGMCompanionSkillReadiness.BuildTier(state.SkillValue);
            state.LastConfidence = BuildConfidence(state.SkillValue);
            state.OwnerSerial = companion.GetOwner() != null ? companion.GetOwner().Serial.Value : 0;

            string sweep = BuildTrackingSweepReport(companion, speaker, state.Mode);
            state.LastReport = sweep;
            state.LastScanUtc = DateTime.UtcNow;

            return String.Format("I will keep a tracking watch for {0}. Tracking: {1:0.0}, {2}. Pursuit remains gated.", DescribeMode(state.Mode), state.SkillValue, state.SkillTier);
        }

        public static string StopTracking(BaseHire companion, Mobile speaker)
        {
            if (!IsValidCompanion(companion))
                return "I cannot stop tracking right now.";

            AIGMCompanionTrackingState state = GetOrCreateState(companion);
            state.IsActive = false;
            state.Mode = AIGMCompanionTrackingMode.None;
            return "I will stop the tracking watch.";
        }

        public static string GetTrackingStatus(BaseHire companion, Mobile speaker)
        {
            if (!IsValidCompanion(companion))
                return "I cannot report tracking right now.";

            AIGMCompanionTrackingState state = GetOrCreateState(companion);
            state.SkillValue = AIGMCompanionSkillReadiness.GetSkillValue(companion, SkillName.Tracking);
            state.SkillTier = AIGMCompanionSkillReadiness.BuildTier(state.SkillValue);
            state.LastConfidence = BuildConfidence(state.SkillValue);

            if (!state.IsActive)
                return String.Format("No tracking watch is active. Tracking: {0:0.0}, {1}. I can begin a read-only watch if asked.", state.SkillValue, state.SkillTier);

            if (ShouldRefresh(state))
            {
                state.LastReport = BuildTrackingSweepReport(companion, speaker, state.Mode);
                state.LastScanUtc = DateTime.UtcNow;
            }

            string last = String.IsNullOrWhiteSpace(state.LastReport) ? "No clear sweep yet." : state.LastReport;
            return String.Format("My tracking watch is active for {0}. Tracking: {1:0.0}, {2}. Last sweep: {3} Pursuit remains gated.", DescribeMode(state.Mode), state.SkillValue, state.SkillTier, last);
        }

        public static string BuildTrackingSweepReport(BaseHire companion, Mobile speaker, AIGMCompanionTrackingMode mode)
        {
            if (!IsValidCompanion(companion))
                return "I cannot get a clear read just now.";

            double tracking = AIGMCompanionSkillReadiness.GetSkillValue(companion, SkillName.Tracking);
            string tier = AIGMCompanionSkillReadiness.BuildTier(tracking);
            AIGMSceneContext scene = AIGMSceneScanner.Capture(companion, 10);
            if (scene == null || scene.NearbyMobiles == null || scene.NearbyMobiles.Count == 0)
                return String.Format("My Tracking is {0} at {1:0.0}, but I do not see a clear {2} trail nearby.", tier, tracking, DescribeMode(mode));

            List<AIGMSceneEntitySummary> matches = new List<AIGMSceneEntitySummary>();
            for (int i = 0; i < scene.NearbyMobiles.Count; i++)
            {
                AIGMSceneEntitySummary mob = scene.NearbyMobiles[i];
                if (mob == null)
                    continue;

                if (MatchesCategory(mob, mode))
                    matches.Add(mob);
            }

            if (matches.Count == 0)
                return String.Format("My Tracking is {0} at {1:0.0}, but I do not see a clear {2} trail nearby.", tier, tracking, DescribeMode(mode));

            AIGMSceneEntitySummary first = matches[0];
            string direction = DescribeDirection(companion, first);
            string distance = DescribeDistance(companion, first);
            string targetName = String.IsNullOrWhiteSpace(first.Name) ? first.TypeName : first.Name;
            string hint = matches.Count > 1 ? String.Format(" I mark {0} signs in all.", matches.Count) : String.Empty;
            return String.Format("My Tracking is {0} at {1:0.0}. I notice {2} {3}, roughly {4}.{5}", tier, tracking, targetName, direction, distance, hint).Trim();
        }

        public static AIGMCompanionTrackingMode GetModeFromIntentKind(string intentKind)
        {
            switch (intentKind)
            {
                case AIGMCompanionIntentKind.TrackAnimals:
                    return AIGMCompanionTrackingMode.Animals;
                case AIGMCompanionIntentKind.TrackPlayers:
                    return AIGMCompanionTrackingMode.Players;
                case AIGMCompanionIntentKind.TrackHumanNPCs:
                    return AIGMCompanionTrackingMode.HumanNPCs;
                case AIGMCompanionIntentKind.ReportThreats:
                    return AIGMCompanionTrackingMode.Threats;
                case AIGMCompanionIntentKind.TrackMonsters:
                    return AIGMCompanionTrackingMode.Monsters;
                default:
                    return AIGMCompanionTrackingMode.General;
            }
        }

        private static bool IsValidCompanion(BaseHire companion)
        {
            return companion != null && !companion.Deleted && companion.Alive && companion.Map != null;
        }

        private static AIGMCompanionTrackingState GetOrCreateState(BaseHire companion)
        {
            AIGMCompanionTrackingState state;
            if (!States.TryGetValue(companion.Serial.Value, out state))
            {
                state = new AIGMCompanionTrackingState();
                state.CompanionSerial = companion.Serial.Value;
                States[companion.Serial.Value] = state;
            }

            return state;
        }

        private static bool ShouldRefresh(AIGMCompanionTrackingState state)
        {
            return state != null && (state.LastScanUtc == DateTime.MinValue || (DateTime.UtcNow - state.LastScanUtc).TotalSeconds >= 10.0);
        }

        private static string DescribeMode(AIGMCompanionTrackingMode mode)
        {
            switch (mode)
            {
                case AIGMCompanionTrackingMode.Animals: return "animals";
                case AIGMCompanionTrackingMode.Monsters: return "monsters";
                case AIGMCompanionTrackingMode.Players: return "players";
                case AIGMCompanionTrackingMode.HumanNPCs: return "human folk";
                case AIGMCompanionTrackingMode.Threats: return "threats";
                default: return "the nearby ground";
            }
        }

        private static string BuildConfidence(double skill)
        {
            if (skill >= 100.0) return "master";
            if (skill >= 90.0) return "expert";
            if (skill >= 70.0) return "strong";
            if (skill >= 50.0) return "capable";
            if (skill >= 30.0) return "basic";
            return "untrained";
        }

        private static bool MatchesCategory(AIGMSceneEntitySummary mob, AIGMCompanionTrackingMode mode)
        {
            string typeName = (mob.TypeName ?? String.Empty).ToLowerInvariant();
            string name = (mob.Name ?? String.Empty).ToLowerInvariant();
            bool isHuman = typeName.Contains("human") || typeName.Contains("player") || typeName.Contains("vendor") || typeName.Contains("seer") || typeName.Contains("healer");
            bool isMonster = ContainsAny(typeName, "dragon", "daemon", "lich", "orc", "troll", "ogre", "ettin", "ratman", "mongbat", "headless", "reaper", "elemental")
                || ContainsAny(name, "orc", "daemon", "lich", "brigand", "pirate", "troll", "ogre", "ratman", "headless");
            bool isAnimal = ContainsAny(typeName, "wolf", "bear", "horse", "ostard", "packhorse", "cat", "dog", "eagle", "boar", "hind", "hart")
                || ContainsAny(name, "wolf", "bear", "horse", "ostard", "cat", "dog", "boar", "hind", "hart");

            switch (mode)
            {
                case AIGMCompanionTrackingMode.Animals:
                    return isAnimal;
                case AIGMCompanionTrackingMode.Monsters:
                    return isMonster;
                case AIGMCompanionTrackingMode.Players:
                    return typeName.Contains("player") || name.Contains("player");
                case AIGMCompanionTrackingMode.HumanNPCs:
                    return isHuman && !typeName.Contains("player");
                case AIGMCompanionTrackingMode.Threats:
                    return isMonster;
                default:
                    return isAnimal || isMonster || isHuman;
            }
        }

        private static string DescribeDirection(BaseHire companion, AIGMSceneEntitySummary summary)
        {
            if (companion == null || summary == null)
                return "nearby";

            int dx = summary.X - companion.X;
            int dy = summary.Y - companion.Y;

            string vertical = String.Empty;
            string horizontal = String.Empty;

            if (dy < 0)
                vertical = "north";
            else if (dy > 0)
                vertical = "south";

            if (dx > 0)
                horizontal = "east";
            else if (dx < 0)
                horizontal = "west";

            if (String.IsNullOrWhiteSpace(vertical) && String.IsNullOrWhiteSpace(horizontal))
                return "nearby";

            if (String.IsNullOrWhiteSpace(vertical))
                return "to the " + horizontal;

            if (String.IsNullOrWhiteSpace(horizontal))
                return "to the " + vertical;

            return "to the " + vertical + "-" + horizontal;
        }

        private static string DescribeDistance(BaseHire companion, AIGMSceneEntitySummary summary)
        {
            if (companion == null || summary == null)
                return "close";

            int dx = summary.X - companion.X;
            int dy = summary.Y - companion.Y;
            int distance = (int)Math.Round(Math.Sqrt(dx * dx + dy * dy));
            return distance <= 0 ? "close" : distance + " tiles";
        }

        private static bool ContainsAny(string value, params string[] needles)
        {
            if (String.IsNullOrWhiteSpace(value) || needles == null)
                return false;

            for (int i = 0; i < needles.Length; i++)
            {
                if (!String.IsNullOrWhiteSpace(needles[i]) && value.Contains(needles[i]))
                    return true;
            }

            return false;
        }
    }
}
