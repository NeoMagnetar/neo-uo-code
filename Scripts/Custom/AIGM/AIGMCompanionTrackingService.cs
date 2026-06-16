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

            AIGMExecutionLog.Write("TRACKING_AWARENESS_START companion={0} active={1} mode={2} summary=\"{3}\"", companion.Serial.Value, state.IsActive, state.Mode, SafeLog(BuildHiddenTrackingSummary(companion)));
            return BuildStartLine(companion, state);
        }

        public static string StopTracking(BaseHire companion, Mobile speaker)
        {
            if (!IsValidCompanion(companion))
                return "I cannot stop tracking right now.";

            AIGMCompanionTrackingState state = GetOrCreateState(companion);
            state.IsActive = false;
            state.Mode = AIGMCompanionTrackingMode.None;
            AIGMExecutionLog.Write("TRACKING_AWARENESS_STOP companion={0} active={1} summary=\"{2}\"", companion.Serial.Value, state.IsActive, SafeLog(BuildHiddenTrackingSummary(companion)));
            return BuildStopLine(companion);
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
                return BuildInactiveStatusLine(companion, state);

            if (ShouldRefresh(state))
            {
                state.LastReport = BuildTrackingSweepReport(companion, speaker, state.Mode);
                state.LastScanUtc = DateTime.UtcNow;
            }

            AIGMExecutionLog.Write("TRACKING_AWARENESS_STATUS companion={0} active={1} mode={2} summary=\"{3}\"", companion.Serial.Value, state.IsActive, state.Mode, SafeLog(BuildHiddenTrackingSummary(companion)));
            return BuildActiveStatusLine(companion, state);
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
                return BuildAllSignsLine(companion, summary);
            }

            if (selection.SelectedTarget == null)
            {
                ClearLastTarget(state);
                if (oneShotReport)
                {
                    state.IsActive = false;
                    state.Mode = AIGMCompanionTrackingMode.None;
                }
                return BuildNoTrailLine(companion, mode);
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
                return BuildTrailLine(companion, category, selection.SelectedName, selection.SelectedDirection, selection.SelectedDistanceText, selection.SelectedTileText, 1);

            return BuildTrailLine(companion, category, selection.SelectedName, selection.SelectedDirection, selection.SelectedDistanceText, selection.SelectedTileText, count);
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
            if (companion == null || companion.Deleted)
                return null;

            return GetOrCreateState(companion);
        }

        public static string DescribeModeForCognition(AIGMCompanionTrackingMode mode)
        {
            return DescribeMode(mode);
        }

        public static string BuildHiddenTrackingSummary(BaseHire companion)
        {
            AIGMCompanionTrackingState state = GetState(companion);
            if (state == null)
                return "not tracking; no recent trail";

            if (!state.IsActive && String.IsNullOrWhiteSpace(state.LastReport))
                return "not tracking; no recent trail";

            List<string> parts = new List<string>();
            parts.Add(state.IsActive ? "tracking active" : "not tracking");
            parts.Add("focus=" + DescribeMode(state.Mode));
            if (!String.IsNullOrWhiteSpace(state.SkillTier))
                parts.Add("skill=" + state.SkillTier);
            string nearest = BuildNearestTrailSummary(state);
            if (!String.IsNullOrWhiteSpace(nearest))
                parts.Add("nearest=" + nearest);
            string threat = BuildThreatSummary(state);
            if (!String.IsNullOrWhiteSpace(threat))
                parts.Add("threat=" + threat);
            if (!String.IsNullOrWhiteSpace(state.LastCandidateSummary))
                parts.Add("signs=" + Shorten(NaturalizeCandidateSummary(state.LastCandidateSummary), 90));

            return String.Join("; ", parts.ToArray());
        }

        public static string BuildNearestTrailSummary(AIGMCompanionTrackingState state)
        {
            if (state == null || String.IsNullOrWhiteSpace(state.LastKnownTargetDescription))
                return "none";

            List<string> parts = new List<string>();
            parts.Add(state.LastKnownTargetDescription.Trim());
            if (!String.IsNullOrWhiteSpace(state.LastKnownDirectionText))
                parts.Add(state.LastKnownDirectionText.Trim());
            if (!String.IsNullOrWhiteSpace(state.LastKnownDistanceText))
                parts.Add(state.LastKnownDistanceText.Trim());

            return String.Join(", ", parts.ToArray());
        }

        public static string BuildThreatSummary(AIGMCompanionTrackingState state)
        {
            if (state == null)
                return "no tracked threat";

            bool threatMode = state.Mode == AIGMCompanionTrackingMode.Threats || state.Mode == AIGMCompanionTrackingMode.Monsters || state.Mode == AIGMCompanionTrackingMode.All;
            if (!threatMode && String.IsNullOrWhiteSpace(state.LastKnownTargetDescription))
                return "no tracked threat";

            if (String.IsNullOrWhiteSpace(state.LastKnownTargetDescription))
                return "no tracked threat";

            return BuildNearestTrailSummary(state);
        }

        public static string BuildTrackingCapabilitySummary(BaseHire companion, AIGMCompanionTrackingState state)
        {
            if (companion == null || companion.Deleted)
                return "tracking unavailable";

            double tracking = state != null && state.SkillValue > 0.0 ? state.SkillValue : AIGMCompanionSkillReadiness.GetSkillValue(companion, SkillName.Tracking);
            string tier = !String.IsNullOrWhiteSpace(state != null ? state.SkillTier : null) ? state.SkillTier : AIGMCompanionSkillReadiness.BuildTier(tracking);
            return String.Format("Tracking {0:0.0}, {1}; scan-only awareness, no pursuit", tracking, tier);
        }

        public static string BuildTrackingRecommendationSummary(AIGMCompanionTrackingState state)
        {
            if (state == null || !state.IsActive)
                return "not tracking; start a scan before relying on trail signs";

            if (String.IsNullOrWhiteSpace(state.LastKnownTargetDescription))
                return "keep scanning; no clear fresh trail nearby";

            return "keep the trail under watch; nearest sign is " + BuildNearestTrailSummary(state);
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

        private static string BuildStartLine(BaseHire companion, AIGMCompanionTrackingState state)
        {
            string focus = DescribeMode(state != null ? state.Mode : AIGMCompanionTrackingMode.General);
            switch (GetCompanionId(companion))
            {
                case "dakeyras":
                    return String.Format("I will read {0}. If anything crossed near us, I will find the sign.", focus);
                case "danyal":
                    return String.Format("I will keep watch while the trail is read. Nothing moves through {0} for free.", focus);
                case "dardalion":
                    return String.Format("I will hold the line and mark what stirs in {0}.", focus);
                default:
                    return String.Format("I will read {0} and keep it scan-only.", focus);
            }
        }

        private static string BuildStopLine(BaseHire companion)
        {
            switch (GetCompanionId(companion))
            {
                case "dakeyras":
                    return "I will lift my eyes from the trail. No false sign.";
                case "danyal":
                    return "The watch eases. I will keep my eyes open without reading the ground.";
                case "dardalion":
                    return "The tracking watch is ended. I remain ready.";
                default:
                    return "I will stop tracking and keep watch.";
            }
        }

        private static string BuildInactiveStatusLine(BaseHire companion, AIGMCompanionTrackingState state)
        {
            double tracking = state != null && state.SkillValue > 0.0 ? state.SkillValue : AIGMCompanionSkillReadiness.GetSkillValue(companion, SkillName.Tracking);
            string tier = !String.IsNullOrWhiteSpace(state != null ? state.SkillTier : null) ? state.SkillTier : AIGMCompanionSkillReadiness.BuildTier(tracking);
            string capability = String.Format("My Tracking is {0:0.0}, {1}", tracking, tier);
            switch (GetCompanionId(companion))
            {
                case "dakeyras":
                    return "I am not reading a trail right now. " + capability + ".";
                case "danyal":
                    return "No active trail watch. " + capability + ".";
                case "dardalion":
                    return "No tracking watch is set. " + capability + ".";
                default:
                    return "No tracking watch is active. " + capability + ".";
            }
        }

        private static string BuildActiveStatusLine(BaseHire companion, AIGMCompanionTrackingState state)
        {
            string nearest = BuildNearestTrailSummary(state);
            string age = BuildSweepAge(state != null ? state.LastScanUtc : DateTime.MinValue);
            if (String.IsNullOrWhiteSpace(nearest) || nearest.Equals("none", StringComparison.OrdinalIgnoreCase))
                nearest = "no clear trail";

            return String.Format("Tracking {0}; last sweep {1}. {2}.", DescribeMode(state.Mode), age, NaturalizeNearestSentence(nearest));
        }

        private static string BuildAllSignsLine(BaseHire companion, string summary)
        {
            return "I read the nearby signs: " + Shorten(summary, 180) + ".";
        }

        private static string BuildNoTrailLine(BaseHire companion, AIGMCompanionTrackingMode mode)
        {
            string trail = DescribeTrailFocus(mode);
            switch (GetCompanionId(companion))
            {
                case "dakeyras":
                    return String.Format("No clean {0} shows near us. I will keep reading the ground.", trail);
                case "danyal":
                    return String.Format("I do not sense a clear {0} nearby. The watch stays sharp.", trail);
                case "dardalion":
                    return String.Format("No clear {0} nearby. I will mark any change.", trail);
                default:
                    return String.Format("No clear {0} nearby.", trail);
            }
        }

        private static string DescribeTrailFocus(AIGMCompanionTrackingMode mode)
        {
            switch (mode)
            {
                case AIGMCompanionTrackingMode.Animals: return "animal trail";
                case AIGMCompanionTrackingMode.Monsters: return "monster trail";
                case AIGMCompanionTrackingMode.Players: return "player trail";
                case AIGMCompanionTrackingMode.HumanNPCs: return "human trail";
                case AIGMCompanionTrackingMode.NPCs: return "NPC trail";
                case AIGMCompanionTrackingMode.Threats: return "threat sign";
                case AIGMCompanionTrackingMode.All: return "fresh sign";
                default: return "nearby trail";
            }
        }

        private static string BuildTrailLine(BaseHire companion, string category, string name, string direction, string distance, string tile, int count)
        {
            string countText = count <= 1 ? "one " + category + " sign" : count + " " + category + " signs";
            string where = BuildWhereText(direction, distance, tile);
            return String.Format("I find {0}; nearest is {1}{2}.", countText, SafeSpeech(name, "something"), where);
        }

        private static string BuildWhereText(string direction, string distance, string tile)
        {
            List<string> parts = new List<string>();
            if (!String.IsNullOrWhiteSpace(direction))
                parts.Add(direction.Trim());
            if (!String.IsNullOrWhiteSpace(distance))
                parts.Add(distance.Trim());
            if (!String.IsNullOrWhiteSpace(tile))
                parts.Add("near " + tile.Trim());

            return parts.Count == 0 ? String.Empty : ", " + String.Join(", ", parts.ToArray());
        }

        private static string NaturalizeNearestSentence(string nearest)
        {
            if (String.IsNullOrWhiteSpace(nearest) || nearest.Equals("none", StringComparison.OrdinalIgnoreCase))
                return "No clear trail yet";

            return "Nearest sign: " + nearest;
        }

        private static string BuildSweepAge(DateTime lastScanUtc)
        {
            if (lastScanUtc == DateTime.MinValue)
                return "not yet";

            double seconds = Math.Max(0.0, (DateTime.UtcNow - lastScanUtc).TotalSeconds);
            if (seconds < 2.0)
                return "just now";
            if (seconds < 60.0)
                return String.Format("{0:0}s ago", seconds);

            return String.Format("{0:0}m ago", seconds / 60.0);
        }

        private static string GetCompanionId(BaseHire companion)
        {
            IAIGMCompanionActor actor = companion as IAIGMCompanionActor;
            if (actor != null && !String.IsNullOrWhiteSpace(actor.CompanionId))
                return actor.CompanionId.Trim().ToLowerInvariant();

            return companion != null && companion.Name != null ? companion.Name.Trim().ToLowerInvariant() : String.Empty;
        }

        private static string SafeSpeech(string value, string fallback)
        {
            if (String.IsNullOrWhiteSpace(value))
                return fallback;

            return value.Replace("\r", " ").Replace("\n", " ").Trim();
        }

        private static string NaturalizeCandidateSummary(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return "none";

            string text = value.Trim();
            if (text.StartsWith("accepted=", StringComparison.OrdinalIgnoreCase))
                text = text.Substring(9);
            if (String.IsNullOrWhiteSpace(text))
                return "none";

            return text.Replace("@", " at ").Replace(",", "; ");
        }

        private static string Shorten(string value, int max)
        {
            if (String.IsNullOrWhiteSpace(value))
                return String.Empty;

            string text = value.Replace("\r", " ").Replace("\n", " ").Trim();
            if (max > 0 && text.Length > max)
                text = text.Substring(0, max);

            return text;
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
