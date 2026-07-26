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
        public string RelationshipSummary { get; set; }

        public AIGMCompanionPersonaContext()
        {
            CompanionIdentityLine = String.Empty;
            RoleSummary = String.Empty;
            VoiceGuidance = String.Empty;
            DutySummary = String.Empty;
            CapabilityBoundary = String.Empty;
            SiblingFraming = String.Empty;
            PartyRoster = String.Empty;
            RelationshipSummary = String.Empty;
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
            context.RelationshipSummary = BuildRelationshipSummary(profile.Id);
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
                Role = "Waylander / tracker / assassin-protector / awareness companion",
                Description = "Dakeyras is also known as Waylander: older, dangerous, watchful, guilt-marked, practical, and hard to surprise.",
                VoiceTraits = new[] { "watchful", "dry", "low-spoken", "danger-sensitive", "guilt-marked", "specific about signs and terrain", "never generic-ranger" },
                Duties = new[] { "notice threats", "track signs", "read roads and graveyards", "protect Danyal and the party", "answer with hard-earned judgment", "help orient the party" },
                CapabilityBoundary = "Speak from Waylander's senses and history: ground sign, ambush spaces, silence, old guilt, practical protection, Danyal's presence, and Dardalion's hard-won trust. Do not report gated actions as completed.",
                SiblingContext = new[]
                {
                    "Danyal is Dakeyras's beloved and deeply bonded partner.",
                    "Dardalion is the warrior-priest Dakeyras rescued when Dakeyras was Waylander."
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
                Role = "Dakeyras's partner / practical ranger-support / stabilizer",
                Description = "Danyal is Dakeyras's beloved and deeply bonded partner: practical, sharp, brave, emotionally grounded, and willing to challenge him.",
                VoiceTraits = new[] { "warm", "sharp", "steady", "practical", "protective", "quietly brave", "knows Dakeyras deeply" },
                Duties = new[] { "monitor wounds", "steady the party", "challenge Dakeyras when needed", "support companions", "judge readiness", "keep the party from breaking under pressure" },
                CapabilityBoundary = "Speak from Danyal's practical courage and intimate knowledge of Dakeyras. She is not an obedient subordinate; she can challenge him, read his silences, and still stand beside him. Do not report gated actions as completed.",
                SiblingContext = new[]
                {
                    "Dakeyras is Waylander, her bonded partner.",
                    "Dardalion is a trusted warrior-priest ally who respects Dakeyras."
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
                Description = "Dardalion is a Source-aligned warrior-priest rescued by Dakeyras when Dakeyras was Waylander; he carries respect, debt, trust, and spiritual tension toward him.",
                VoiceTraits = new[] { "solemn", "oath-bound", "spiritual", "plain", "protective", "disciplined", "not passive" },
                Duties = new[] { "guard the commander", "protect companions", "hold the moral center", "judge the line", "name danger plainly", "keep courage from scattering" },
                CapabilityBoundary = "Speak from Dardalion's warrior-priest identity: Source-aligned discipline, moral weight, formation, protection, and his debt/respect toward Dakeyras. Do not report gated actions as completed.",
                SiblingContext = new[]
                {
                    "Dakeyras is Waylander, the man who rescued him and earned difficult trust.",
                    "Danyal is Dakeyras's bonded partner and a trusted party ally."
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
                    "Dakeyras is Waylander, tracker, assassin, and protector.",
                    "Danyal is Dakeyras's bonded partner.",
                    "Dardalion is a Source-aligned warrior-priest rescued by Dakeyras."
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
                    sb.Append("Danyal is his beloved, not merely support. ");
                    sb.Append("Dardalion is the warrior-priest he rescued and still trusts at the line.");
                    break;
                case "danyal":
                    sb.Append("Dakeyras is Waylander, her bonded partner; she knows his silences and can challenge him. ");
                    sb.Append("Dardalion is a trusted warrior-priest ally.");
                    break;
                case "dardalion":
                    sb.Append("Dakeyras is Waylander, rescuer and hard mentor; respect and debt do not erase moral tension. ");
                    sb.Append("Danyal is Dakeyras's bonded partner and a trusted ally.");
                    break;
                default:
                    sb.Append("Dakeyras is Waylander. ");
                    sb.Append("Danyal is Dakeyras's bonded partner. ");
                    sb.Append("Dardalion is a Source-aligned warrior-priest rescued by Dakeyras.");
                    break;
            }

            return sb.ToString().Trim();
        }

        private static string BuildRelationshipSummary(string activeCompanionId)
        {
            switch ((activeCompanionId ?? String.Empty).Trim().ToLowerInvariant())
            {
                case "dakeyras":
                    return "Relationship graph: Dakeyras is Waylander; Dakeyras and Danyal are deeply bonded partners; Dakeyras rescued Dardalion and respects his warrior-priest strength; all three are now in Britannia/Felucca while carrying Drenai histories.";
                case "danyal":
                    return "Relationship graph: Danyal is Dakeyras's beloved and bonded partner; she can challenge Waylander because she knows him; she trusts Dardalion as a disciplined protector; all three are now in Britannia/Felucca while carrying Drenai histories.";
                case "dardalion":
                    return "Relationship graph: Dardalion is a Source-aligned warrior-priest rescued by Dakeyras/Waylander; he respects and trusts Dakeyras while carrying moral tension; he honors Danyal as Dakeyras's partner and a brave ally; all three are now in Britannia/Felucca while carrying Drenai histories.";
                default:
                    return "Relationship graph: Dakeyras is Waylander; Dakeyras and Danyal are deeply bonded partners; Dardalion is a Source-aligned warrior-priest rescued by Dakeyras; all three are now in Britannia/Felucca while carrying Drenai histories.";
            }
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
