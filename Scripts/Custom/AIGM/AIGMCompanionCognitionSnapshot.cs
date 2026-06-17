using System;
using System.Collections.Generic;
using System.Text;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public sealed class AIGMCompanionCognitionSnapshot
    {
        public string CompanionId { get; set; }
        public string CompanionName { get; set; }
        public string Role { get; set; }
        public int Hits { get; set; }
        public int HitsMax { get; set; }
        public int Stam { get; set; }
        public int StamMax { get; set; }
        public int Mana { get; set; }
        public int ManaMax { get; set; }
        public int Str { get; set; }
        public int Dex { get; set; }
        public int Int { get; set; }
        public string WeaponSummary { get; set; }
        public string ArmorSummary { get; set; }
        public string PackSummary { get; set; }
        public string StrongestSkillsSummary { get; set; }
        public string LocationSummary { get; set; }
        public string PartySummary { get; set; }
        public string SituationSummary { get; set; }
        public bool TrackingActive { get; set; }
        public string TrackingFocus { get; set; }
        public string LastTrackingReport { get; set; }
        public string LastSweepAge { get; set; }
        public string LastNearestCreature { get; set; }
        public string LastKnownThreatSummary { get; set; }
        public string TrackingCapabilitySummary { get; set; }
        public string TrackingRecommendationSummary { get; set; }
        public string TrackingActionMode { get; set; }
        public string TrackingTargetCategory { get; set; }
        public string CurrentTrackingTarget { get; set; }
        public string CurrentTrackingTargetDistance { get; set; }
        public bool TrackingHuntActive { get; set; }
        public bool TrackingEngagementAllowed { get; set; }
        public string TrackingStopReason { get; set; }
        public string RecentDialogueSummary { get; set; }
        public string IntentSummary { get; set; }
        public string CapabilitySummary { get; set; }
        public string ResponseStyleSummary { get; set; }
        public string DialogueMode { get; set; }
        public string RecentSpeaker { get; set; }
        public string RecentCompanionLine { get; set; }
        public string CurrentTrackingState { get; set; }
        public string CurrentHealingState { get; set; }
        public string CurrentThreatState { get; set; }
        public bool QuietMode { get; set; }

        public AIGMCompanionCognitionSnapshot()
        {
            CompanionId = "unknown";
            CompanionName = "unknown";
            Role = "unknown";
            WeaponSummary = "unknown";
            ArmorSummary = "unknown";
            PackSummary = "unknown";
            StrongestSkillsSummary = "unknown";
            LocationSummary = "unknown";
            PartySummary = "unknown";
            SituationSummary = "unknown";
            TrackingActive = false;
            TrackingFocus = "not tracking";
            LastTrackingReport = "no recent trail";
            LastSweepAge = "no recent sweep";
            LastNearestCreature = "none";
            LastKnownThreatSummary = "no tracked threat";
            TrackingCapabilitySummary = "tracking not assessed";
            TrackingRecommendationSummary = "no tracking recommendation";
            TrackingActionMode = "track only";
            TrackingTargetCategory = "none";
            CurrentTrackingTarget = "none";
            CurrentTrackingTargetDistance = "unknown";
            TrackingHuntActive = false;
            TrackingEngagementAllowed = false;
            TrackingStopReason = "none";
            RecentDialogueSummary = "none";
            IntentSummary = "unknown";
            CapabilitySummary = "unknown";
            ResponseStyleSummary = "Hidden cognition only. Visible replyText must be short in-character speech with no UMG, block, JSON, prompt, metadata, or debug wording.";
            DialogueMode = "DIALOGUE_NATURAL";
            RecentSpeaker = "unknown";
            RecentCompanionLine = "none";
            CurrentTrackingState = "not tracking";
            CurrentHealingState = "stable";
            CurrentThreatState = "unknown";
            QuietMode = false;
        }

        public static AIGMCompanionCognitionSnapshot Build(Mobile companion, AIGMCompanionSpeechRequest request)
        {
            AIGMCompanionCognitionSnapshot snapshot = new AIGMCompanionCognitionSnapshot();
            IAIGMCompanionActor actor = companion as IAIGMCompanionActor;

            snapshot.CompanionId = Safe(actor != null ? actor.CompanionId : null);
            snapshot.CompanionName = Safe(actor != null ? actor.CompanionDisplayName : companion != null ? companion.Name : null);
            snapshot.Role = Safe(actor != null ? actor.CompanionRole : null);

            if (companion == null || companion.Deleted)
            {
                snapshot.SituationSummary = "companion shell unavailable";
                return snapshot;
            }

            snapshot.Hits = companion.Hits;
            snapshot.HitsMax = SafeInt(companion.HitsMax);
            snapshot.Stam = companion.Stam;
            snapshot.StamMax = SafeInt(companion.StamMax);
            snapshot.Mana = companion.Mana;
            snapshot.ManaMax = SafeInt(companion.ManaMax);
            snapshot.Str = companion.Str;
            snapshot.Dex = companion.Dex;
            snapshot.Int = companion.Int;
            snapshot.WeaponSummary = BuildWeaponSummary(companion);
            snapshot.ArmorSummary = BuildArmorSummary(companion);
            snapshot.PackSummary = BuildPackSummary(companion);
            snapshot.StrongestSkillsSummary = BuildStrongestSkillsSummary(companion);
            snapshot.LocationSummary = BuildLocationSummary(companion, request);
            snapshot.PartySummary = BuildPartySummary(companion, request);
            snapshot.SituationSummary = BuildSituationSummary(companion, request);
            ApplyTrackingSummary(snapshot, companion as BaseHire);
            snapshot.RecentDialogueSummary = BuildRecentDialogueSummary(actor);
            snapshot.DialogueMode = BuildDialogueMode(request);
            snapshot.RecentSpeaker = request != null && request.Speaker != null ? Safe(request.Speaker.Name) : "unknown";
            snapshot.RecentCompanionLine = snapshot.RecentDialogueSummary;
            snapshot.CurrentTrackingState = BuildCurrentTrackingState(snapshot);
            snapshot.CurrentHealingState = BuildCurrentHealingState(companion);
            snapshot.CurrentThreatState = snapshot.SituationSummary;
            snapshot.QuietMode = BuildQuietMode(companion as BaseHire);
            snapshot.IntentSummary = BuildIntentSummary(request);
            snapshot.CapabilitySummary = BuildCapabilitySummary(request);
            return snapshot;
        }

        private static string BuildWeaponSummary(Mobile companion)
        {
            Item weapon = companion.FindItemOnLayer(Layer.OneHanded);
            if (weapon == null)
                weapon = companion.FindItemOnLayer(Layer.TwoHanded);

            if (weapon == null)
                return "no equipped weapon found";

            return DescribeItem(weapon);
        }

        private static string BuildArmorSummary(Mobile companion)
        {
            Layer[] layers = new[]
            {
                Layer.Helm,
                Layer.Neck,
                Layer.InnerTorso,
                Layer.MiddleTorso,
                Layer.OuterTorso,
                Layer.Arms,
                Layer.Gloves,
                Layer.Pants,
                Layer.InnerLegs,
                Layer.OuterLegs,
                Layer.Shoes,
                Layer.Cloak,
                Layer.TwoHanded
            };

            List<string> armor = new List<string>();
            for (int i = 0; i < layers.Length; i++)
            {
                Item item = companion.FindItemOnLayer(layers[i]);
                if (item is BaseArmor)
                    armor.Add(DescribeItem(item));
            }

            if (armor.Count == 0)
                return "no worn armor found";

            return TrimJoin(armor, 6);
        }

        private static string BuildPackSummary(Mobile companion)
        {
            Container pack = companion.Backpack;
            if (pack == null || pack.Items == null || pack.Items.Count == 0)
                return "no pack items noted";

            int arrows = CountItems(pack, "Arrow");
            int bolts = CountItems(pack, "Bolt");
            int bandages = CountItems(pack, "Bandage");
            List<string> parts = new List<string>();
            if (arrows > 0)
                parts.Add(arrows + " arrows");
            if (bolts > 0)
                parts.Add(bolts + " bolts");
            if (bandages > 0)
                parts.Add(bandages + " bandages");

            return parts.Count > 0 ? String.Join(", ", parts.ToArray()) : "no relevant pack items noted";
        }

        private static string BuildStrongestSkillsSummary(Mobile companion)
        {
            SkillName[] relevant =
            {
                SkillName.Archery,
                SkillName.Swords,
                SkillName.Fencing,
                SkillName.Macing,
                SkillName.Tactics,
                SkillName.Anatomy,
                SkillName.Healing,
                SkillName.MagicResist,
                SkillName.Parry,
                SkillName.Tracking,
                SkillName.Magery,
                SkillName.Meditation
            };

            List<SkillScore> scores = new List<SkillScore>();
            for (int i = 0; i < relevant.Length; i++)
            {
                double value = AIGMCompanionSkillReadiness.GetSkillValue(companion, relevant[i]);
                if (value > 0.0)
                    scores.Add(new SkillScore(relevant[i].ToString(), value));
            }

            scores.Sort(delegate(SkillScore left, SkillScore right)
            {
                return right.Value.CompareTo(left.Value);
            });

            if (scores.Count == 0)
                return "no measured skills";

            List<string> parts = new List<string>();
            for (int i = 0; i < scores.Count && i < 4; i++)
                parts.Add(String.Format("{0} {1:0.0} ({2})", scores[i].Name, scores[i].Value, AIGMCompanionSkillReadiness.BuildTier(scores[i].Value)));

            return String.Join(", ", parts.ToArray());
        }

        private static string BuildLocationSummary(Mobile companion, AIGMCompanionSpeechRequest request)
        {
            string map = companion.Map != null ? companion.Map.Name : "unknown map";
            string region = companion.Region != null ? companion.Region.Name : "unknown region";
            string distance = String.Empty;
            if (request != null && request.Speaker != null && request.Speaker.Map == companion.Map)
                distance = String.Format(", speaker distance {0:0}", companion.GetDistanceToSqrt(request.Speaker));

            return String.Format("{0}/{1} at {2},{3},{4}{5}", map, region, companion.X, companion.Y, companion.Z, distance);
        }

        private static string BuildPartySummary(Mobile companion, AIGMCompanionSpeechRequest request)
        {
            BaseHire hire = companion as BaseHire;
            Mobile owner = hire != null ? hire.GetOwner() : null;
            List<string> parts = new List<string>();

            if (owner != null)
                parts.Add("owner=" + Safe(owner.Name));

            if (request != null)
            {
                if (!String.IsNullOrWhiteSpace(request.PartyListenerSet))
                    parts.Add("listeners=" + request.PartyListenerSet);
                if (!String.IsNullOrWhiteSpace(request.PartySelectedResponderSet))
                    parts.Add("selected=" + request.PartySelectedResponderSet);
                if (!String.IsNullOrWhiteSpace(request.PartySuppressedResponderSet))
                    parts.Add("suppressed=" + request.PartySuppressedResponderSet);
            }

            List<string> nearby = new List<string>();
            if (owner != null && companion.Map != null)
            {
                foreach (Mobile mobile in World.Mobiles.Values)
                {
                    BaseHire ally = mobile as BaseHire;
                    IAIGMCompanionActor allyActor = ally as IAIGMCompanionActor;
                    if (ally == null || ally == companion || ally.Deleted || allyActor == null)
                        continue;
                    if (ally.Map != companion.Map || ally.GetOwner() != owner)
                        continue;
                    nearby.Add(String.Format("{0}@{1:0}", allyActor.CompanionId, companion.GetDistanceToSqrt(ally)));
                }
            }

            if (nearby.Count > 0)
                parts.Add("nearby=" + String.Join(", ", nearby.ToArray()));

            return parts.Count > 0 ? String.Join("; ", parts.ToArray()) : "no party context";
        }

        private static string BuildSituationSummary(Mobile companion, AIGMCompanionSpeechRequest request)
        {
            if (companion.Map == null)
                return "no map context";

            AIGMSceneContext scene = AIGMSceneScanner.Capture(companion, 8);
            int mobiles = scene != null && scene.NearbyMobiles != null ? scene.NearbyMobiles.Count : 0;
            int items = scene != null && scene.NearbyItems != null ? scene.NearbyItems.Count : 0;
            int threats = CountPossibleThreats(companion, request != null ? request.Speaker : null, scene);
            return String.Format("nearby mobiles={0}, nearby items={1}, possible threats={2}", mobiles, items, threats);
        }

        private static string BuildRecentDialogueSummary(IAIGMCompanionActor actor)
        {
            string context = AIGMCompanionPerceptionBuffer.BuildRecentContext(actor);
            if (String.IsNullOrWhiteSpace(context))
                return "none";
            return context.Length > 360 ? context.Substring(0, 360) : context;
        }

        private static void ApplyTrackingSummary(AIGMCompanionCognitionSnapshot snapshot, BaseHire companion)
        {
            if (snapshot == null)
                return;

            AIGMCompanionTrackingState state = AIGMCompanionTrackingService.GetState(companion);
            if (state == null)
            {
                snapshot.TrackingActive = false;
                snapshot.TrackingFocus = "not tracking";
                snapshot.LastTrackingReport = "no recent trail";
                snapshot.LastSweepAge = "no recent sweep";
                snapshot.LastNearestCreature = "none";
                snapshot.LastKnownThreatSummary = "no tracked threat";
                snapshot.TrackingCapabilitySummary = "no tracking state available";
                snapshot.TrackingRecommendationSummary = "start tracking before relying on trail signs";
                snapshot.TrackingActionMode = "track only";
                snapshot.TrackingTargetCategory = "none";
                snapshot.CurrentTrackingTarget = "none";
                snapshot.CurrentTrackingTargetDistance = "unknown";
                snapshot.TrackingHuntActive = false;
                snapshot.TrackingEngagementAllowed = false;
                snapshot.TrackingStopReason = "none";
                return;
            }

            snapshot.TrackingActive = state.IsActive;
            snapshot.TrackingFocus = state.IsActive ? AIGMCompanionTrackingService.DescribeModeForCognition(state.Mode) : "not tracking";
            snapshot.LastTrackingReport = ShortOrDefault(AIGMCompanionTrackingService.BuildHiddenTrackingSummary(companion), "no recent trail", 240);
            snapshot.LastSweepAge = BuildSweepAge(state.LastScanUtc);
            snapshot.LastNearestCreature = ShortOrDefault(AIGMCompanionTrackingService.BuildNearestTrailSummary(state), "none", 160);
            snapshot.LastKnownThreatSummary = ShortOrDefault(AIGMCompanionTrackingService.BuildThreatSummary(state), "no tracked threat", 160);
            snapshot.TrackingCapabilitySummary = ShortOrDefault(AIGMCompanionTrackingService.BuildTrackingCapabilitySummary(companion, state), "tracking not assessed", 160);
            snapshot.TrackingRecommendationSummary = ShortOrDefault(AIGMCompanionTrackingService.BuildTrackingRecommendationSummary(state), "no tracking recommendation", 180);
            snapshot.TrackingActionMode = AIGMCompanionTrackingService.DescribeActionModeForCognition(state.ActionMode);
            snapshot.TrackingTargetCategory = AIGMCompanionTrackingService.DescribeModeForCognition(state.Mode);
            snapshot.CurrentTrackingTarget = ShortOrDefault(state.CurrentTargetName, "none", 120);
            snapshot.CurrentTrackingTargetDistance = ShortOrDefault(state.LastKnownDistanceText, "unknown", 80);
            snapshot.TrackingHuntActive = state.IsActive && state.ActionMode == AIGMCompanionTrackingActionMode.TrackHunt;
            snapshot.TrackingEngagementAllowed = state.EngagementAllowed;
            snapshot.TrackingStopReason = ShortOrDefault(state.StopReason, "none", 120);
            AIGMExecutionLog.Write("COMPANION_COGNITION_TRACKING companion={0} active={1} focus=\"{2}\" action=\"{3}\" target=\"{4}\" summary=\"{5}\"", companion != null ? companion.Serial.Value : 0, snapshot.TrackingActive, SafeLog(snapshot.TrackingFocus), SafeLog(snapshot.TrackingActionMode), SafeLog(snapshot.CurrentTrackingTarget), SafeLog(snapshot.LastTrackingReport));
        }

        private static string BuildIntentSummary(AIGMCompanionSpeechRequest request)
        {
            if (request == null)
                return "unknown";

            List<string> parts = new List<string>();
            parts.Add("dialogueMode=" + BuildDialogueMode(request));
            if (!String.IsNullOrWhiteSpace(request.ParsedIntentSummary))
                parts.Add("parsed=" + request.ParsedIntentSummary);
            if (!String.IsNullOrWhiteSpace(request.AddressedCompanionId))
                parts.Add("addressed=" + request.AddressedCompanionId);
            if (!String.IsNullOrWhiteSpace(request.DialogueTargetCompanionId))
                parts.Add("dialogueTarget=" + request.DialogueTargetCompanionId);
            parts.Add("groupAddressed=" + request.GroupAddressed);
            BaseHire hire = request.Companion != null ? request.Companion.Shell as BaseHire : null;
            parts.Add("quietMode=" + BuildQuietMode(hire));
            return String.Join("; ", parts.ToArray());
        }

        private static string BuildDialogueMode(AIGMCompanionSpeechRequest request)
        {
            if (request == null)
                return "DIALOGUE_NATURAL";

            if (String.Equals(request.DialogueMode, "companion_dialogue", StringComparison.OrdinalIgnoreCase))
                return "DIALOGUE_COMPANION_REPLY";

            if (request.GroupAddressed)
                return "DIALOGUE_GROUP_BANTER";

            if (!String.IsNullOrWhiteSpace(request.AddressedCompanionId))
                return "DIALOGUE_NATURAL";

            return "DIALOGUE_NATURAL";
        }

        private static string BuildCurrentTrackingState(AIGMCompanionCognitionSnapshot snapshot)
        {
            if (snapshot == null || !snapshot.TrackingActive)
                return "inactive";

            return String.Format("{0}; action={1}; target={2}; distance={3}", snapshot.TrackingFocus, snapshot.TrackingActionMode, snapshot.CurrentTrackingTarget, snapshot.CurrentTrackingTargetDistance);
        }

        private static string BuildCurrentHealingState(Mobile companion)
        {
            if (companion == null)
                return "unknown";

            int hitsMax = SafeInt(companion.HitsMax);
            if (hitsMax <= 0)
                return "unknown";

            if (companion.Hits < hitsMax)
                return String.Format("damaged {0}/{1}", companion.Hits, hitsMax);

            return String.Format("full {0}/{1}", companion.Hits, hitsMax);
        }

        private static bool BuildQuietMode(BaseHire companion)
        {
            Mobile owner = companion != null ? companion.GetOwner() : null;
            return AIGMCompanionDialogueControlService.IsQuiet(owner);
        }

        private static string BuildCapabilitySummary(AIGMCompanionSpeechRequest request)
        {
            if (request == null)
                return "speech-only hidden cognition; execution remains gated";

            if (!String.IsNullOrWhiteSpace(request.CapabilitySafetyPosture))
                return request.CapabilitySafetyPosture;

            return "speech-only hidden cognition; execution remains gated";
        }

        private static int CountPossibleThreats(Mobile companion, Mobile speaker, AIGMSceneContext scene)
        {
            if (scene == null || scene.NearbyMobiles == null)
                return 0;

            int threats = 0;
            for (int i = 0; i < scene.NearbyMobiles.Count; i++)
            {
                AIGMSceneEntitySummary mob = scene.NearbyMobiles[i];
                if (mob == null)
                    continue;

                string name = mob.Name ?? String.Empty;
                string typeName = mob.TypeName ?? String.Empty;
                if (Matches(companion != null ? companion.Name : null, name) || Matches(speaker != null ? speaker.Name : null, name))
                    continue;

                if (ContainsAny(typeName, "dragon", "daemon", "lich", "orc", "troll", "ogre", "ettin", "ratman", "mongbat", "headless", "reaper", "elemental")
                    || ContainsAny(name, "orc", "daemon", "lich", "brigand", "pirate", "troll", "ogre", "ratman", "headless"))
                    threats++;
            }

            return threats;
        }

        private static int CountItems(Container pack, string typeName)
        {
            if (pack == null || pack.Items == null || String.IsNullOrWhiteSpace(typeName))
                return 0;

            int count = 0;
            for (int i = 0; i < pack.Items.Count; i++)
            {
                Item item = pack.Items[i];
                if (item == null || item.Deleted)
                    continue;
                if (String.Equals(item.GetType().Name, typeName, StringComparison.OrdinalIgnoreCase))
                    count += Math.Max(1, item.Amount);
            }

            return count;
        }

        private static string DescribeItem(Item item)
        {
            if (item == null)
                return "unknown";

            string label = !String.IsNullOrWhiteSpace(item.Name) ? item.Name : item.GetType().Name;
            return label + " (" + item.GetType().Name + ")";
        }

        private static string TrimJoin(List<string> values, int max)
        {
            if (values == null || values.Count == 0)
                return "unknown";

            List<string> selected = new List<string>();
            for (int i = 0; i < values.Count && i < max; i++)
                selected.Add(values[i]);

            if (values.Count > max)
                selected.Add("+" + (values.Count - max) + " more");

            return String.Join(", ", selected.ToArray());
        }

        private static string Safe(string value)
        {
            return String.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim();
        }

        private static string ShortOrDefault(string value, string fallback, int max)
        {
            string text = String.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
            if (max > 0 && text.Length > max)
                text = text.Substring(0, max);
            return text;
        }

        private static string BuildSweepAge(DateTime lastScanUtc)
        {
            if (lastScanUtc == DateTime.MinValue)
                return "no recent sweep";

            double seconds = Math.Max(0.0, (DateTime.UtcNow - lastScanUtc).TotalSeconds);
            if (seconds < 2.0)
                return "just now";
            if (seconds < 60.0)
                return String.Format("{0:0}s ago", seconds);

            return String.Format("{0:0}m ago", seconds / 60.0);
        }

        private static int SafeInt(int value)
        {
            return value < 0 ? 0 : value;
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

        private static string SafeLog(string value)
        {
            if (String.IsNullOrEmpty(value))
                return String.Empty;

            string text = value.Replace("\r", " ").Replace("\n", " ");
            if (text.Length > 220)
                text = text.Substring(0, 220);

            return text;
        }

        private sealed class SkillScore
        {
            public string Name { get; private set; }
            public double Value { get; private set; }

            public SkillScore(string name, double value)
            {
                Name = name;
                Value = value;
            }
        }
    }
}
