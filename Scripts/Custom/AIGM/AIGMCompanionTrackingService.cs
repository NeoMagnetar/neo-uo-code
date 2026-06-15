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

            return String.Format("{0}: I will keep a tracking watch for {1}. Tracking {2:0.0}, {3}. Pursuit remains gated.", companion.Name, DescribeMode(state.Mode), state.SkillValue, state.SkillTier);
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
                return String.Format("{0}: no tracking watch is active. Tracking {1:0.0}, {2}. I can begin a read-only watch if asked.", companion.Name, state.SkillValue, state.SkillTier);

            if (ShouldRefresh(state))
            {
                state.LastReport = BuildTrackingSweepReport(companion, speaker, state.Mode);
                state.LastScanUtc = DateTime.UtcNow;
            }

            string tile = String.IsNullOrWhiteSpace(state.LastKnownTileText) ? "no tile recorded" : state.LastKnownTileText;
            string last = String.IsNullOrWhiteSpace(state.LastReport) ? "No clear sweep yet." : state.LastReport;
            return String.Format("{0}: tracking watch active for {1}. Tracking {2:0.0}, {3}. Last sweep: {4} Last tile: {5}. Pursuit remains gated.", companion.Name, DescribeMode(state.Mode), state.SkillValue, state.SkillTier, last, tile);
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

            AIGMSceneContext scene = AIGMSceneScanner.Capture(companion, GetTrackingRange(companion));
            if (scene == null || scene.NearbyMobiles == null || scene.NearbyMobiles.Count == 0)
            {
                ClearLastTarget(state);
                state.LastCandidateSummary = "accepted=none";
                state.LastRejectedCandidates = "none";
                return String.Format("{0}: Tracking {1:0.0}, {2}. I do not find a clear {3} trail nearby.", companion.Name, tracking, tier, DescribeMode(mode));
            }

            List<AIGMSceneEntitySummary> matches = new List<AIGMSceneEntitySummary>();
            List<string> rejects = new List<string>();

            for (int i = 0; i < scene.NearbyMobiles.Count; i++)
            {
                AIGMSceneEntitySummary mob = scene.NearbyMobiles[i];
                if (mob == null)
                    continue;

                string rejectReason;
                if (!MatchesCategory(mob, mode, out rejectReason))
                {
                    if (rejects.Count < 12)
                        rejects.Add(DescribeSummary(mob) + "=" + rejectReason);
                    continue;
                }

                if (!CanTrack(companion, mob, out rejectReason))
                {
                    if (rejects.Count < 12)
                        rejects.Add(DescribeSummary(mob) + "=" + rejectReason);
                    continue;
                }

                matches.Add(mob);
            }

            state.LastRejectedCandidates = rejects.Count > 0 ? String.Join(", ", rejects.ToArray()) : "none";

            if (matches.Count == 0)
            {
                ClearLastTarget(state);
                state.LastCandidateSummary = "accepted=none";
                return String.Format("{0}: Tracking {1:0.0}, {2}. I do not find a clear {3} trail nearby.", companion.Name, tracking, tier, DescribeMode(mode));
            }

            matches.Sort((a, b) => GetDistance(companion, a).CompareTo(GetDistance(companion, b)));
            AIGMSceneEntitySummary first = matches[0];
            string direction = DescribeDirection(companion, first);
            string distance = DescribeDistance(companion, first);
            string targetName = String.IsNullOrWhiteSpace(first.Name) ? first.TypeName : first.Name;
            string tile = DescribeTile(first);
            string category = DescribeResultCategory(mode, first);

            state.LastKnownTargetDescription = targetName;
            state.LastKnownDirectionText = direction;
            state.LastKnownDistanceText = distance;
            state.LastKnownTileText = tile;
            state.LastCandidateSummary = BuildCandidateSummary(companion, matches);

            if (matches.Count == 1)
                return String.Format("{0}: Tracking {1:0.0}, {2}. I find 1 {3} sign: {4} at tile {5}, {6}, {7}. Pursuit remains gated.", companion.Name, tracking, tier, category, targetName, tile, direction, distance);

            return String.Format("{0}: Tracking {1:0.0}, {2}. I find {3} {4} signs; nearest is {5} at tile {6}, {7}, {8}. Pursuit remains gated.", companion.Name, tracking, tier, matches.Count, category, targetName, tile, direction, distance);
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
                case AIGMCompanionIntentKind.TrackNPCs:
                    return AIGMCompanionTrackingMode.NPCs;
                case AIGMCompanionIntentKind.TrackAll:
                    return AIGMCompanionTrackingMode.All;
                case AIGMCompanionIntentKind.ReportThreats:
                    return AIGMCompanionTrackingMode.Threats;
                case AIGMCompanionIntentKind.TrackMonsters:
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

            Mobile target = ResolveMobile(mob);
            if (target == null)
                return !IsPlayer(mob) && !IsAnimal(mob) && !IsHumanNPC(mob) && IsMonster(mob);

            return IsHostileMonsterMobile(null, target, out _);
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

            AIGMSceneContext scene = AIGMSceneScanner.Capture(companion, GetTrackingRange(companion));
            if (scene == null || scene.NearbyMobiles == null || scene.NearbyMobiles.Count == 0)
            {
                state.LastCandidateSummary = acceptedSummary;
                state.LastRejectedCandidates = rejectedSummary;
                return null;
            }

            List<AIGMSceneEntitySummary> accepted = new List<AIGMSceneEntitySummary>();
            List<string> rejected = new List<string>();

            for (int i = 0; i < scene.NearbyMobiles.Count; i++)
            {
                AIGMSceneEntitySummary summary = scene.NearbyMobiles[i];
                if (summary == null)
                    continue;

                Mobile target = ResolveMobile(summary);
                string rejectReason;
                if (!IsHostileMonsterMobile(companion, target, out rejectReason))
                {
                    if (rejected.Count < 12)
                        rejected.Add(DescribeSummary(summary) + "=" + rejectReason);
                    continue;
                }

                if (!CanTrack(companion, summary, out rejectReason))
                {
                    if (rejected.Count < 12)
                        rejected.Add(DescribeSummary(summary) + "=" + rejectReason);
                    continue;
                }

                accepted.Add(summary);
            }

            accepted.Sort((a, b) => GetDistance(companion, a).CompareTo(GetDistance(companion, b)));
            acceptedSummary = BuildCandidateSummary(companion, accepted);
            rejectedSummary = rejected.Count > 0 ? String.Join(", ", rejected.ToArray()) : "none";
            state.LastCandidateSummary = acceptedSummary;
            state.LastRejectedCandidates = rejectedSummary;
            state.LastScanUtc = DateTime.UtcNow;

            if (accepted.Count == 0)
                return null;

            AIGMSceneEntitySummary first = accepted[0];
            state.LastKnownTargetDescription = DescribeSummary(first);
            state.LastKnownDirectionText = DescribeDirection(companion, first);
            state.LastKnownDistanceText = DescribeDistance(companion, first);
            state.LastKnownTileText = DescribeTile(first);
            return ResolveMobile(first);
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

        private static string DescribeResultCategory(AIGMCompanionTrackingMode mode, AIGMSceneEntitySummary summary)
        {
            switch (mode)
            {
                case AIGMCompanionTrackingMode.Animals: return "animal";
                case AIGMCompanionTrackingMode.Monsters: return "monster";
                case AIGMCompanionTrackingMode.Players: return "player";
                case AIGMCompanionTrackingMode.HumanNPCs: return "human NPC";
                case AIGMCompanionTrackingMode.NPCs: return IsHumanNPC(summary) ? "human NPC" : "NPC";
                case AIGMCompanionTrackingMode.Threats: return "threat";
                default:
                    return IsHumanNPC(summary) ? "human NPC" : IsPlayer(summary) ? "player" : IsAnimal(summary) ? "animal" : IsMonster(summary) ? "monster" : "sign";
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

        private static bool MatchesCategory(AIGMSceneEntitySummary mob, AIGMCompanionTrackingMode mode, out string rejectReason)
        {
            rejectReason = String.Empty;

            switch (mode)
            {
                case AIGMCompanionTrackingMode.Animals:
                    if (IsAnimal(mob))
                        return true;
                    rejectReason = DescribeClass(mob);
                    return false;
                case AIGMCompanionTrackingMode.Monsters:
                    if (IsHostileMonsterSummary(mob))
                        return true;
                    rejectReason = DescribeClass(mob);
                    return false;
                case AIGMCompanionTrackingMode.Players:
                    if (IsPlayer(mob))
                        return true;
                    rejectReason = DescribeClass(mob);
                    return false;
                case AIGMCompanionTrackingMode.HumanNPCs:
                    if (IsHumanNPC(mob))
                        return true;
                    rejectReason = DescribeClass(mob);
                    return false;
                case AIGMCompanionTrackingMode.NPCs:
                    if (IsHumanNPC(mob) || IsNonHumanNPC(mob))
                        return true;
                    rejectReason = DescribeClass(mob);
                    return false;
                case AIGMCompanionTrackingMode.Threats:
                    if (IsHostileMonsterSummary(mob))
                        return true;
                    rejectReason = DescribeClass(mob);
                    return false;
                case AIGMCompanionTrackingMode.All:
                case AIGMCompanionTrackingMode.General:
                    if (IsAnimal(mob) || IsMonster(mob) || IsPlayer(mob) || IsHumanNPC(mob) || IsNonHumanNPC(mob))
                        return true;
                    rejectReason = "other";
                    return false;
                default:
                    rejectReason = "unsupported_mode";
                    return false;
            }
        }

        private static bool CanTrack(BaseHire companion, AIGMSceneEntitySummary mob, out string rejectReason)
        {
            rejectReason = String.Empty;

            if (companion == null || mob == null)
            {
                rejectReason = "invalid_candidate";
                return false;
            }

            Mobile target = ResolveMobile(mob);
            if (target == null)
                return true;

            if (target == companion)
            {
                rejectReason = "self_target";
                return false;
            }

            if (Core.AOS && !target.Alive)
            {
                rejectReason = "dead_target";
                return false;
            }

            if (target.Hidden && !target.Player && companion.AccessLevel <= target.AccessLevel)
            {
                rejectReason = "hidden_target";
                return false;
            }

            if (!CheckDifficulty(companion, target))
            {
                rejectReason = "tracking_difficulty_failed";
                return false;
            }

            return true;
        }

        private static Mobile ResolveMobile(AIGMSceneEntitySummary summary)
        {
            if (summary == null || summary.Serial == 0)
                return null;

            return World.FindMobile(summary.Serial);
        }

        private static bool CheckDifficulty(Mobile from, Mobile target)
        {
            if (from == null || target == null)
                return false;

            if (!Core.AOS || !target.Player)
                return true;

            int tracking = from.Skills[SkillName.Tracking].Fixed;
            int detectHidden = from.Skills[SkillName.DetectHidden].Fixed;

            if (Core.ML && target.Race == Race.Elf)
                tracking /= 2;

            int hiding = target.Skills[SkillName.Hiding].Fixed;
            int stealth = target.Skills[SkillName.Stealth].Fixed;
            int divisor = hiding + stealth;

            if (TransformationSpellHelper.UnderTransformation(target, typeof(HorrificBeastSpell)))
                divisor -= 200;
            else if (TransformationSpellHelper.UnderTransformation(target, typeof(VampiricEmbraceSpell)) && divisor < 500)
                divisor = 500;
            else if (TransformationSpellHelper.UnderTransformation(target, typeof(WraithFormSpell)) && divisor <= 2000)
                divisor += 200;

            int chance = divisor > 0 ? 50 * (tracking * 2 + detectHidden) / divisor : 100;
            return chance > Utility.Random(100);
        }

        private static bool IsHostileMonsterMobile(BaseHire companion, Mobile target, out string rejectReason)
        {
            rejectReason = String.Empty;

            if (target == null)
            {
                rejectReason = "missing_target";
                return false;
            }

            if (companion != null)
                return AIGMCompanionTargetValidator.IsHostileMonsterCandidate(companion, target, out rejectReason);

            if (target.Player)
            {
                rejectReason = "player";
                return false;
            }

            if (target.Body != null && target.Body.IsAnimal)
            {
                rejectReason = "animal";
                return false;
            }

            if (target.Body != null && target.Body.IsHuman)
            {
                rejectReason = "human_npc";
                return false;
            }

            if (target.Body == null || !target.Body.IsMonster)
            {
                rejectReason = "not_monster_body";
                return false;
            }

            return true;
        }

        private static bool IsAnimal(AIGMSceneEntitySummary mob)
        {
            Mobile target = ResolveMobile(mob);
            if (target != null)
                return !target.Player && target.Body != null && target.Body.IsAnimal;

            string typeName = (mob.TypeName ?? String.Empty).ToLowerInvariant();
            string name = (mob.Name ?? String.Empty).ToLowerInvariant();
            return ContainsAny(typeName, "horse", "ostard", "cat", "dog", "wolf", "bear", "boar", "eagle", "hind", "hart", "cow", "bull")
                || ContainsAny(name, "horse", "ostard", "cat", "dog", "wolf", "bear", "boar", "eagle", "hind", "hart", "cow", "bull");
        }

        private static bool IsMonster(AIGMSceneEntitySummary mob)
        {
            Mobile target = ResolveMobile(mob);
            if (target != null)
                return !target.Player && target.Body != null && target.Body.IsMonster;

            string typeName = (mob.TypeName ?? String.Empty).ToLowerInvariant();
            string name = (mob.Name ?? String.Empty).ToLowerInvariant();
            return ContainsAny(typeName, "dragon", "daemon", "lich", "orc", "troll", "ogre", "ettin", "ratman", "mongbat", "headless", "reaper", "elemental", "skeleton")
                || ContainsAny(name, "orc", "daemon", "lich", "brigand", "pirate", "troll", "ogre", "ratman", "headless", "skeleton");
        }

        private static bool IsPlayer(AIGMSceneEntitySummary mob)
        {
            Mobile target = ResolveMobile(mob);
            if (target != null)
                return target.Player;

            string typeName = (mob.TypeName ?? String.Empty).ToLowerInvariant();
            return typeName.Contains("playermobile") || typeName.Equals("player");
        }

        private static bool IsHumanNPC(AIGMSceneEntitySummary mob)
        {
            Mobile target = ResolveMobile(mob);
            if (target != null)
                return !target.Player && target.Body != null && target.Body.IsHuman;

            string typeName = (mob.TypeName ?? String.Empty).ToLowerInvariant();
            string name = (mob.Name ?? String.Empty).ToLowerInvariant();
            if (IsPlayer(mob))
                return false;

            return ContainsAny(typeName, "vendor", "healer", "seer", "human", "banker", "provisioner", "mage", "warrior")
                || ContainsAny(name, "healer", "banker", "vendor", "mage", "guard", "innkeeper", "provisioner");
        }

        private static bool IsNonHumanNPC(AIGMSceneEntitySummary mob)
        {
            Mobile target = ResolveMobile(mob);
            if (target == null)
                return false;

            if (target.Player)
                return false;

            if (target is BaseHire || target is IAIGMCompanionActor)
                return false;

            if (target is BaseVendor || target is BaseEscortable)
                return true;

            if (target.Body != null && target.Body.IsHuman)
                return false;

            if (target.Body != null && target.Body.IsAnimal)
                return false;

            if (target.Body != null && target.Body.IsMonster)
                return false;

            return true;
        }

        private static string DescribeClass(AIGMSceneEntitySummary mob)
        {
            if (IsPlayer(mob))
                return "player";
            if (IsHumanNPC(mob))
                return "human_npc";
            if (IsNonHumanNPC(mob))
                return "npc";
            if (IsAnimal(mob))
                return "animal";
            if (IsMonster(mob))
                return "monster";
            return "other";
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
                return horizontal;

            if (String.IsNullOrWhiteSpace(horizontal))
                return vertical;

            return vertical + "-" + horizontal;
        }

        private static string DescribeDistance(BaseHire companion, AIGMSceneEntitySummary summary)
        {
            int distance = GetDistance(companion, summary);
            return distance <= 0 ? "close" : distance + " tiles";
        }

        private static int GetDistance(BaseHire companion, AIGMSceneEntitySummary summary)
        {
            if (companion == null || summary == null)
                return 0;

            int dx = summary.X - companion.X;
            int dy = summary.Y - companion.Y;
            return (int)Math.Round(Math.Sqrt(dx * dx + dy * dy));
        }

        private static string DescribeTile(AIGMSceneEntitySummary summary)
        {
            if (summary == null)
                return "unknown";

            return String.Format("{0},{1},{2}", summary.X, summary.Y, summary.Z);
        }

        private static string BuildCandidateSummary(BaseHire companion, List<AIGMSceneEntitySummary> matches)
        {
            if (companion == null || matches == null || matches.Count == 0)
                return "accepted=none";

            List<string> parts = new List<string>();
            for (int i = 0; i < matches.Count && i < 12; i++)
                parts.Add(DescribeSummary(matches[i]) + "@" + GetDistance(companion, matches[i]));

            return "accepted=" + String.Join(", ", parts.ToArray());
        }

        private static string DescribeSummary(AIGMSceneEntitySummary summary)
        {
            if (summary == null)
                return "none";

            return String.IsNullOrWhiteSpace(summary.Name) ? (!String.IsNullOrWhiteSpace(summary.TypeName) ? summary.TypeName : "unknown") : summary.Name;
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