using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Server.Spells;
using Server.Spells.Necromancy;

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
            state.LastConfidence = BuildConfidence(companion, state.Mode);
            state.OwnerSerial = companion.GetOwner() != null ? companion.GetOwner().Serial.Value : 0;

            string sweep = BuildTrackingSweepReport(companion, speaker, state.Mode);
            state.LastReport = sweep;
            state.LastScanUtc = DateTime.UtcNow;

            return String.Format("{0}: I will begin tracking {1} and report what I sense.", companion.Name, DescribeMode(state.Mode));
        }

        public static string StopTracking(BaseHire companion, Mobile speaker)
        {
            if (!IsValidCompanion(companion))
                return "I cannot stop tracking right now.";

            AIGMCompanionTrackingState state = GetOrCreateState(companion);
            state.IsActive = false;
            state.Mode = AIGMCompanionTrackingMode.None;
            return String.Format("{0}: I will stop the tracking watch.", companion.Name);
        }

        public static string GetTrackingStatus(BaseHire companion, Mobile speaker)
        {
            if (!IsValidCompanion(companion))
                return "I cannot report tracking right now.";

            AIGMCompanionTrackingState state = GetOrCreateState(companion);
            state.SkillValue = AIGMCompanionSkillReadiness.GetSkillValue(companion, SkillName.Tracking);
            state.SkillTier = AIGMCompanionSkillReadiness.BuildTier(state.SkillValue);
            state.LastConfidence = BuildConfidence(companion, state.Mode);

            if (!state.IsActive)
                return String.Format("{0}: no tracking watch is active. Tracking {1:0.0}, {2}. I can begin tracking if asked.", companion.Name, state.SkillValue, state.SkillTier);

            if (ShouldRefresh(state))
            {
                state.LastReport = BuildTrackingSweepReport(companion, speaker, state.Mode);
                state.LastScanUtc = DateTime.UtcNow;
            }

            string tile = String.IsNullOrWhiteSpace(state.LastKnownTileText) ? "no tile recorded" : state.LastKnownTileText;
            string last = String.IsNullOrWhiteSpace(state.LastReport) ? "No clear sweep yet." : state.LastReport;
            return String.Format("{0}: tracking watch active for {1}. Tracking {2:0.0}, {3}. Last sweep: {4} Last tile: {5}.", companion.Name, DescribeMode(state.Mode), state.SkillValue, state.SkillTier, last, tile);
        }

        public static string BuildTrackingSweepReport(BaseHire companion, Mobile speaker, AIGMCompanionTrackingMode mode)
        {
            if (!IsValidCompanion(companion))
                return "I cannot get a clear read just now.";

            double tracking = AIGMCompanionSkillReadiness.GetSkillValue(companion, SkillName.Tracking);
            string tier = AIGMCompanionSkillReadiness.BuildTier(tracking);
            AIGMCompanionTrackingState state = GetOrCreateState(companion);
            state.SkillValue = tracking;
            state.SkillTier = tier;
            state.LastConfidence = BuildConfidence(companion, mode);

            AIGMTrackingSelectionResult selection = AIGMCompanionTrackingSensor.SelectNearest(companion, mode, GetTrackingRange(companion));
            bool oneShotReport = !state.IsActive;
            state.LastCandidateSummary = selection.AcceptedSummary;
            state.LastRejectedCandidates = selection.RejectedSummary;
            state.LastScanUtc = DateTime.UtcNow;

            if (mode == AIGMCompanionTrackingMode.All)
            {
                ClearLastTarget(state);
                state.LastKnownTargetDescription = selection.SelectedName;
                state.LastKnownDirectionText = selection.SelectedDirection;
                state.LastKnownDistanceText = selection.SelectedDistanceText;
                state.LastKnownTileText = selection.SelectedTileText;

                string summary = BuildNaturalAllSummary(selection.CategorySummary);
                if (oneShotReport)
                {
                    state.IsActive = false;
                    state.Mode = AIGMCompanionTrackingMode.None;
                }
                return String.Format("{0}: Tracking {1:0.0}, {2}. I find signs nearby: {3}.", companion.Name, tracking, tier, summary);
            }

            if (selection.SelectedTarget == null)
            {
                ClearLastTarget(state);
                if (oneShotReport)
                {
                    state.IsActive = false;
                    state.Mode = AIGMCompanionTrackingMode.None;
                }
                return String.Format("{0}: Tracking {1:0.0}, {2}. I do not find a clear {3} trail nearby.", companion.Name, tracking, tier, DescribeMode(mode));
            }

            state.LastKnownTargetDescription = selection.SelectedName;
            state.LastKnownDirectionText = selection.SelectedDirection;
            state.LastKnownDistanceText = selection.SelectedDistanceText;
            state.LastKnownTileText = selection.SelectedTileText;

            string category = DescribeResultCategory(mode);
            int count = CountAccepted(selection.AcceptedSummary);

            if (oneShotReport)
            {
                state.IsActive = false;
                state.Mode = AIGMCompanionTrackingMode.None;
            }

            if (count <= 1)
                return String.Format("{0}: Tracking {1:0.0}, {2}. I find 1 {3} sign: {4} at tile {5}, {6}, {7}.", companion.Name, tracking, tier, category, selection.SelectedName, selection.SelectedTileText, selection.SelectedDirection, selection.SelectedDistanceText);

            return String.Format("{0}: Tracking {1:0.0}, {2}. I find {3} {4} signs; nearest is {5} at tile {6}, {7}, {8}.", companion.Name, tracking, tier, count, category, selection.SelectedName, selection.SelectedTileText, selection.SelectedDirection, selection.SelectedDistanceText);
        }

        public static void Pulse(BaseHire companion)
        {
            if (!IsValidCompanion(companion))
                return;

            AIGMCompanionTrackingState state = GetOrCreateState(companion);
            if (state == null || !state.IsActive)
                return;

            if (!ShouldRefresh(state))
                return;

            Mobile owner = companion.GetOwner();
            state.LastReport = BuildTrackingSweepReport(companion, owner, state.Mode);
            state.LastScanUtc = DateTime.UtcNow;
            AIGMExecutionLog.Write("TRACKING_CYCLE_SWEEP companion={0} mode={1} report=\"{2}\"", companion.Serial.Value, state.Mode, SafeLog(state.LastReport));
        }

        public static AIGMCompanionTrackingMode GetModeFromIntentKind(string intentKind)
        {
            switch (intentKind)
            {
                case AIGMCompanionIntentKind.TrackAnimals:
                case AIGMCompanionIntentKind.StartTrackingAnimals:
                    return AIGMCompanionTrackingMode.Animals;
                case AIGMCompanionIntentKind.TrackPlayers:
                case AIGMCompanionIntentKind.StartTrackingPlayers:
                    return AIGMCompanionTrackingMode.Players;
                case AIGMCompanionIntentKind.TrackHumanNPCs:
                case AIGMCompanionIntentKind.StartTrackingHumanNPCs:
                    return AIGMCompanionTrackingMode.HumanNPCs;
                case AIGMCompanionIntentKind.TrackNPCs:
                case AIGMCompanionIntentKind.StartTrackingNPCs:
                    return AIGMCompanionTrackingMode.NPCs;
                case AIGMCompanionIntentKind.TrackAll:
                case AIGMCompanionIntentKind.StartTrackingAll:
                    return AIGMCompanionTrackingMode.All;
                case AIGMCompanionIntentKind.ReportThreats:
                    return AIGMCompanionTrackingMode.Threats;
                case AIGMCompanionIntentKind.TrackMonsters:
                case AIGMCompanionIntentKind.StartTrackingMonsters:
                    return AIGMCompanionTrackingMode.Monsters;
                default:
                    return AIGMCompanionTrackingMode.General;
            }
        }

        public static bool IsExplicitGroupTrackingCommand(string speech)
        {
            if (String.IsNullOrWhiteSpace(speech))
                return false;

            string normalized = NormalizeSpeech(speech);
            return normalized.Equals("start tracking all")
                || normalized.Equals("all start tracking")
                || normalized.Equals("companions start tracking")
                || normalized.Equals("everyone start tracking")
                || normalized.Equals("all tracking status")
                || normalized.Equals("all track animals")
                || normalized.Equals("all track monsters")
                || normalized.Equals("all track players")
                || normalized.Equals("all track npcs")
                || normalized.Equals("all track humans")
                || normalized.Equals("all track human npcs");
        }

        public static AIGMCompanionTrackingMode GetModeFromSpeech(string speech)
        {
            string normalized = NormalizeSpeech(speech);
            if (normalized.Contains("track animals") || normalized.Contains("track animal"))
                return AIGMCompanionTrackingMode.Animals;
            if (normalized.Contains("track monsters") || normalized.Contains("track monster") || normalized.Contains("track enemies") || normalized.Contains("track hostiles"))
                return AIGMCompanionTrackingMode.Monsters;
            if (normalized.Contains("track players") || normalized.Contains("track player"))
                return AIGMCompanionTrackingMode.Players;
            if (normalized.Contains("track npcs") || normalized.Contains("track npc"))
                return AIGMCompanionTrackingMode.NPCs;
            if (normalized.Contains("track humans") || normalized.Contains("track human") || normalized.Contains("track human npcs") || normalized.Contains("track people"))
                return AIGMCompanionTrackingMode.HumanNPCs;
            if (normalized.Contains("track all") || normalized.Contains("start tracking all") || normalized.Contains("all start tracking") || normalized.Contains("companions start tracking") || normalized.Contains("everyone start tracking"))
                return AIGMCompanionTrackingMode.All;
            return AIGMCompanionTrackingMode.General;
        }

        public static AIGMCompanionTrackingState GetState(BaseHire companion)
        {
            return companion == null ? null : GetOrCreateState(companion);
        }

        public static bool IsHostileMonsterSummary(AIGMSceneEntitySummary mob)
        {
            if (mob == null)
                return false;

            Mobile target = World.FindMobile(mob.Serial);
            if (target == null || target.Deleted)
                return false;

            string rejectReason;
            return AIGMCompanionTargetValidator.IsHostileMonsterCandidate(null, target, out rejectReason);
        }

        public static Mobile FindClosestHostileMonster(BaseHire companion, out string acceptedSummary, out string rejectedSummary)
        {
            acceptedSummary = "accepted=none";
            rejectedSummary = "none";

            if (!IsValidCompanion(companion))
                return null;

            AIGMCompanionTrackingState state = GetOrCreateState(companion);
            double tracking = AIGMCompanionSkillReadiness.GetSkillValue(companion, SkillName.Tracking);
            state.SkillValue = tracking;
            state.SkillTier = AIGMCompanionSkillReadiness.BuildTier(tracking);
            state.LastConfidence = BuildConfidence(companion, AIGMCompanionTrackingMode.Monsters);

            AIGMTrackingSelectionResult selection = AIGMCompanionTrackingSensor.SelectClosestHostileMonster(companion, GetTrackingRange(companion));
            acceptedSummary = selection.AcceptedSummary;
            rejectedSummary = selection.RejectedSummary;
            state.LastCandidateSummary = acceptedSummary;
            state.LastRejectedCandidates = rejectedSummary;
            state.LastScanUtc = DateTime.UtcNow;

            if (selection.SelectedTarget == null)
            {
                ClearLastTarget(state);
                return null;
            }

            state.LastKnownTargetDescription = selection.SelectedName;
            state.LastKnownDirectionText = selection.SelectedDirection;
            state.LastKnownDistanceText = selection.SelectedDistanceText;
            state.LastKnownTileText = selection.SelectedTileText;
            return selection.SelectedTarget;
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

        private static int GetTrackingRange(BaseHire companion)
        {
            double tracking = AIGMCompanionSkillReadiness.GetSkillValue(companion, SkillName.Tracking);
            return 10 + (int)(tracking / 10.0);
        }

        private static string DescribeMode(AIGMCompanionTrackingMode mode)
        {
            switch (mode)
            {
                case AIGMCompanionTrackingMode.Animals: return "animals";
                case AIGMCompanionTrackingMode.Monsters: return "monsters";
                case AIGMCompanionTrackingMode.Players: return "players";
                case AIGMCompanionTrackingMode.HumanNPCs: return "human NPCs";
                case AIGMCompanionTrackingMode.NPCs: return "NPCs";
                case AIGMCompanionTrackingMode.Threats: return "threats";
                case AIGMCompanionTrackingMode.All: return "all signs";
                default: return "the nearby ground";
            }
        }

        private static string DescribeResultCategory(AIGMCompanionTrackingMode mode)
        {
            switch (mode)
            {
                case AIGMCompanionTrackingMode.Animals: return "animal";
                case AIGMCompanionTrackingMode.Monsters: return "monster";
                case AIGMCompanionTrackingMode.Players: return "player";
                case AIGMCompanionTrackingMode.HumanNPCs: return "human NPC";
                case AIGMCompanionTrackingMode.NPCs: return "NPC";
                case AIGMCompanionTrackingMode.Threats: return "threat";
                default: return "sign";
            }
        }

        private static string BuildConfidence(BaseHire companion, AIGMCompanionTrackingMode mode)
        {
            double tracking = AIGMCompanionSkillReadiness.GetSkillValue(companion, SkillName.Tracking);
            double detectHidden = AIGMCompanionSkillReadiness.GetSkillValue(companion, SkillName.DetectHidden);
            if (mode == AIGMCompanionTrackingMode.Players)
                tracking = tracking + (detectHidden * 0.5);

            return AIGMCompanionSkillReadiness.BuildTier(tracking);
        }

        private static string BuildNaturalAllSummary(string categorySummary)
        {
            if (String.IsNullOrWhiteSpace(categorySummary))
                return "animals none; monsters none; NPCs none; human NPCs none; players none";

            string animals = ExtractCategoryValue(categorySummary, "Animals");
            string monsters = ExtractCategoryValue(categorySummary, "Monsters");
            string npcs = ExtractCategoryValue(categorySummary, "NPCs");
            string humanNpcs = ExtractCategoryValue(categorySummary, "HumanNPCs");
            string players = ExtractCategoryValue(categorySummary, "Players");

            return String.Format("animal={0}; monster={1}; npc={2}; human npc={3}; player={4}",
                NormalizeCategoryDisplay(animals),
                NormalizeCategoryDisplay(monsters),
                NormalizeCategoryDisplay(npcs),
                NormalizeCategoryDisplay(humanNpcs),
                NormalizeCategoryDisplay(players));
        }

        private static string ExtractCategoryValue(string summary, string key)
        {
            if (String.IsNullOrWhiteSpace(summary) || String.IsNullOrWhiteSpace(key))
                return "none";

            string[] parts = summary.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i].Trim();
                if (part.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))
                    return part.Substring(key.Length + 1).Trim();
            }

            return "none";
        }

        private static string NormalizeCategoryDisplay(string value)
        {
            if (String.IsNullOrWhiteSpace(value) || value.Equals("none", StringComparison.OrdinalIgnoreCase))
                return "none";

            int at = value.LastIndexOf('@');
            if (at > 0 && at < value.Length - 1)
            {
                string name = value.Substring(0, at).Trim();
                string distance = value.Substring(at + 1).Trim();
                return name + " at " + distance + " tiles";
            }

            return value;
        }

        private static int CountAccepted(string acceptedSummary)
        {
            if (String.IsNullOrWhiteSpace(acceptedSummary) || acceptedSummary.Equals("accepted=none", StringComparison.OrdinalIgnoreCase))
                return 0;

            string payload = acceptedSummary.StartsWith("accepted=", StringComparison.OrdinalIgnoreCase)
                ? acceptedSummary.Substring(9)
                : acceptedSummary;

            if (String.IsNullOrWhiteSpace(payload))
                return 0;

            return payload.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Length;
        }

        private static void ClearLastTarget(AIGMCompanionTrackingState state)
        {
            if (state == null)
                return;

            state.LastKnownTargetDescription = String.Empty;
            state.LastKnownDirectionText = String.Empty;
            state.LastKnownDistanceText = String.Empty;
            state.LastKnownTileText = String.Empty;
        }

        private static string NormalizeSpeech(string speech)
        {
            if (String.IsNullOrWhiteSpace(speech))
                return String.Empty;

            string normalized = speech.Trim().ToLowerInvariant();
            while (normalized.Contains("  "))
                normalized = normalized.Replace("  ", " ");
            return normalized;
        }

        private static string SafeLog(string value)
        {
            if (String.IsNullOrEmpty(value))
                return String.Empty;

            value = value.Replace("\r", " ").Replace("\n", " ");
            if (value.Length > 220)
                value = value.Substring(0, 220) + "...";

            return value;
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
