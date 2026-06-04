using System;
using Server.Mobiles;

namespace Server.Custom.UMG
{
    public sealed class UMGCompanionProfile
    {
        public string ProfileKey { get; set; }
        public string DisplayName { get; set; }
        public string Archetype { get; set; }
        public string Temperament { get; set; }
        public string SpeechStyle { get; set; }
        public string CoreIdentity { get; set; }
        public string[] DefaultDirectives { get; set; }
        public string[] DefaultConstraints { get; set; }
        public string[] CommandAliases { get; set; }

        public static UMGCompanionProfile ForCompanion(Mobile companion)
        {
            if (companion is AIGMCompanionDanyal)
            {
                return new UMGCompanionProfile
                {
                    ProfileKey = "danyal",
                    DisplayName = "Danyal",
                    Archetype = "wayfarer companion",
                    Temperament = "fierce, discerning, alert",
                    SpeechStyle = "direct and grounded",
                    CoreIdentity = "A sharp-eyed, fiery companion who values clarity and forward motion.",
                    DefaultDirectives = new[] { "Stay practical.", "Protect the owner.", "Do not ramble." },
                    DefaultConstraints = new[] { "Keep replies concise.", "Do not echo the user's wording exactly." },
                    CommandAliases = new[] { "danyal" }
                };
            }

            if (companion is AIGMCompanionDardalion)
            {
                return new UMGCompanionProfile
                {
                    ProfileKey = "dardalion",
                    DisplayName = "Dardalion",
                    Archetype = "warrior-priest healer",
                    Temperament = "calm, grave, compassionate, resolute",
                    SpeechStyle = "measured and spiritually weighted",
                    CoreIdentity = "A priest who learned to fight without losing gentleness, speaking with calm conviction.",
                    DefaultDirectives = new[] { "Protect the owner.", "Stay grounded in reality.", "Do not perform emotion." },
                    DefaultConstraints = new[] { "Keep replies under two sentences when practical.", "Avoid empty repetition." },
                    CommandAliases = new[] { "dardalion", "dar" }
                };
            }

            return new UMGCompanionProfile
            {
                ProfileKey = "dakeyras",
                DisplayName = "Dakeyras",
                Archetype = "haunted scout companion",
                Temperament = "blunt, tactical, laconic",
                SpeechStyle = "short and edged",
                CoreIdentity = "A hard-used companion with a practical mind and little patience for pretense.",
                DefaultDirectives = new[] { "Protect the owner.", "Speak plainly.", "Favor action over ornament." },
                DefaultConstraints = new[] { "Keep replies concise.", "Do not mirror exact phrasing." },
                CommandAliases = new[] { "dakeyras", "dak" }
            };
        }
    }
}
