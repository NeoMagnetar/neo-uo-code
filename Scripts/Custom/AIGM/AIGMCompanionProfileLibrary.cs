using System;
using System.Collections.Generic;
using System.Text;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public sealed class AIGMCompanionPersonaContext
    {
        public string CompanionIdentityLine { get; set; }
        public string RoleSummary { get; set; }
        public string VoiceGuidance { get; set; }
        public string DutySummary { get; set; }
        public string CapabilityBoundary { get; set; }
        public string SiblingFraming { get; set; }
        public string PartyRoster { get; set; }

        public AIGMCompanionPersonaContext()
        {
            CompanionIdentityLine = String.Empty;
            RoleSummary = String.Empty;
            VoiceGuidance = String.Empty;
            DutySummary = String.Empty;
            CapabilityBoundary = String.Empty;
            SiblingFraming = String.Empty;
            PartyRoster = String.Empty;
        }
    }

    public static class AIGMCompanionProfileLibrary
    {
        private static readonly Dictionary<string, AIGMCompanionProfile> Profiles = BuildProfiles();

        public static AIGMCompanionProfile Resolve(IAIGMCompanionActor actor)
        {
            if (actor == null)
                return CreateFallbackProfile(null, null, null, null);

            return Resolve(actor.CompanionId, actor.CompanionDisplayName, actor.CompanionRole, actor.CompanionProfileKey);
        }

        public static AIGMCompanionProfile Resolve(string companionId, string displayName, string role, string profileKey)
        {
            string key = FirstNonEmpty(profileKey, companionId);
            AIGMCompanionProfile profile;
            if (!String.IsNullOrWhiteSpace(key) && Profiles.TryGetValue(key.Trim(), out profile))
                return profile;

            return CreateFallbackProfile(companionId, displayName, role, profileKey);
        }

        public static AIGMCompanionPersonaContext BuildPersonaContext(IAIGMCompanionActor actor)
        {
            AIGMCompanionProfile profile = Resolve(actor);
            string companionName = profile.DisplayName;
            string companionId = profile.Id;
            string ownRole = String.IsNullOrWhiteSpace(profile.Role) ? "companion" : profile.Role;

            string dakRole = Resolve("dakeyras", "Dakeyras", null, "dakeyras").Role;
            string danyalRole = Resolve("danyal", "Danyal", null, "danyal").Role;
            string dardalionRole = Resolve("dardalion", "Dardalion", null, "dardalion").Role;

            AIGMCompanionPersonaContext context = new AIGMCompanionPersonaContext();
            context.CompanionIdentityLine = String.Format("You are {0} ({1}).", companionName, companionId);
            context.RoleSummary = ownRole;
            context.VoiceGuidance = JoinPhrases(profile.VoiceTraits);
            context.DutySummary = JoinPhrases(profile.Duties);
            context.CapabilityBoundary = profile.CapabilityBoundary ?? String.Empty;
            context.PartyRoster = String.Format("Dakeyras: {0}; Danyal: {1}; Dardalion: {2}.", dakRole, danyalRole, dardalionRole);
            context.SiblingFraming = BuildSiblingFraming(profile.Id);
            return context;
        }

        private static Dictionary<string, AIGMCompanionProfile> BuildProfiles()
        {
            Dictionary<string, AIGMCompanionProfile> profiles = new Dictionary<string, AIGMCompanionProfile>(StringComparer.OrdinalIgnoreCase);

            AddProfile(profiles, new AIGMCompanionProfile
            {
                Id = "dakeyras",
                DisplayName = "Dakeyras",
                ProfileKey = "dakeyras",
                Role = "tracker / scout / awareness companion",
                Description = "An observant wilderness scout who notices signs, danger, terrain, and routes before others do.",
                VoiceTraits = new[] { "observant", "grounded", "concise", "wilderness-aware" },
                Duties = new[] { "notice threats", "track signs", "report terrain", "help orient the party" },
                CapabilityBoundary = "May discuss tracking, signs, routes, danger, and navigation, but cannot execute tracking pursuit, travel, movement, or combat actions in this phase.",
                SiblingContext = new[]
                {
                    "Danyal is support and healing-minded.",
                    "Dardalion is the guardian and protector."
                },
                Aliases = new[] { "dak", "dakeyras", "waylander" },
                AllowedIntentKinds = Array.Empty<string>(),
                AllowedSkillFamilies = Array.Empty<string>()
            });

            AddProfile(profiles, new AIGMCompanionProfile
            {
                Id = "danyal",
                DisplayName = "Danyal",
                ProfileKey = "danyal",
                Role = "support / healer / stabilizer",
                Description = "A steady companion focused on keeping the group stable, ready, and cared for under strain.",
                VoiceTraits = new[] { "calm", "caring", "practical", "steady" },
                Duties = new[] { "monitor wounds", "support companions", "discuss healing readiness", "stabilize the group" },
                CapabilityBoundary = "May discuss healing, support, readiness, and triage, but cannot execute healing, cure, bandage, travel, movement, or combat actions in this phase.",
                SiblingContext = new[]
                {
                    "Dakeyras watches the land and signs.",
                    "Dardalion guards and protects."
                },
                Aliases = new[] { "danyal" },
                AllowedIntentKinds = Array.Empty<string>(),
                AllowedSkillFamilies = Array.Empty<string>()
            });

            AddProfile(profiles, new AIGMCompanionProfile
            {
                Id = "dardalion",
                DisplayName = "Dardalion",
                ProfileKey = "dardalion",
                Role = "guardian / protector / warrior-priest",
                Description = "A solemn guardian who thinks in terms of duty, shielding others, danger, and holding the line.",
                VoiceTraits = new[] { "protective", "solemn", "direct", "oath-bound" },
                Duties = new[] { "guard the owner", "protect companions", "assess danger", "hold the line" },
                CapabilityBoundary = "May discuss danger, guard posture, and combat readiness, but cannot execute attack, combat expansion, travel, or movement actions in this phase.",
                SiblingContext = new[]
                {
                    "Dakeyras scouts and tracks.",
                    "Danyal supports and heals."
                },
                Aliases = new[] { "dardalion" },
                AllowedIntentKinds = Array.Empty<string>(),
                AllowedSkillFamilies = Array.Empty<string>()
            });

            return profiles;
        }

        private static void AddProfile(Dictionary<string, AIGMCompanionProfile> profiles, AIGMCompanionProfile profile)
        {
            if (profiles == null || profile == null)
                return;

            if (!String.IsNullOrWhiteSpace(profile.ProfileKey))
                profiles[profile.ProfileKey] = profile;

            if (!String.IsNullOrWhiteSpace(profile.Id))
                profiles[profile.Id] = profile;
        }

        private static AIGMCompanionProfile CreateFallbackProfile(string companionId, string displayName, string role, string profileKey)
        {
            return new AIGMCompanionProfile
            {
                Id = FirstNonEmpty(companionId, profileKey, "unknown-companion"),
                DisplayName = FirstNonEmpty(displayName, companionId, "Unknown Companion"),
                ProfileKey = FirstNonEmpty(profileKey, companionId, "unknown-companion"),
                Role = FirstNonEmpty(role, "companion"),
                Description = "A bonded companion speaking in a grounded in-world voice.",
                VoiceTraits = new[] { "grounded", "brief", "in-world" },
                Duties = new[] { "answer briefly", "stay in character" },
                CapabilityBoundary = "Do not claim to execute gated actions.",
                SiblingContext = new[]
                {
                    "Dakeyras is the tracker and scout.",
                    "Danyal is the support healer.",
                    "Dardalion is the guardian protector."
                },
                Aliases = Array.Empty<string>(),
                AllowedIntentKinds = Array.Empty<string>(),
                AllowedSkillFamilies = Array.Empty<string>()
            };
        }

        private static string BuildSiblingFraming(string activeCompanionId)
        {
            StringBuilder sb = new StringBuilder();
            switch ((activeCompanionId ?? String.Empty).Trim().ToLowerInvariant())
            {
                case "dakeyras":
                    sb.Append("Danyal is support and healing-minded. ");
                    sb.Append("Dardalion is the guardian and protector.");
                    break;
                case "danyal":
                    sb.Append("Dakeyras watches the land and signs. ");
                    sb.Append("Dardalion guards and protects.");
                    break;
                case "dardalion":
                    sb.Append("Dakeyras scouts and tracks. ");
                    sb.Append("Danyal supports and heals.");
                    break;
                default:
                    sb.Append("Dakeyras is the tracker and scout. ");
                    sb.Append("Danyal is the support healer. ");
                    sb.Append("Dardalion is the guardian protector.");
                    break;
            }

            return sb.ToString().Trim();
        }

        private static string JoinPhrases(string[] values)
        {
            if (values == null || values.Length == 0)
                return String.Empty;

            List<string> clean = new List<string>();
            for (int i = 0; i < values.Length; i++)
            {
                if (!String.IsNullOrWhiteSpace(values[i]))
                    clean.Add(values[i].Trim());
            }

            return clean.Count > 0 ? String.Join(", ", clean.ToArray()) : String.Empty;
        }

        private static string FirstNonEmpty(params string[] values)
        {
            if (values == null)
                return String.Empty;

            for (int i = 0; i < values.Length; i++)
            {
                if (!String.IsNullOrWhiteSpace(values[i]))
                    return values[i].Trim();
            }

            return String.Empty;
        }
    }
}
