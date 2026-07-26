using System;

namespace Server.Custom.AIGM
{
    public sealed class AIGMCompanionProfile
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string Role { get; set; }
        public string ProfileKey { get; set; }
        public string Description { get; set; }
        public string[] VoiceTraits { get; set; }
        public string[] Duties { get; set; }
        public string CapabilityBoundary { get; set; }
        public string[] SiblingContext { get; set; }
        public string[] Aliases { get; set; }
        public string[] AllowedIntentKinds { get; set; }
        public string[] AllowedSkillFamilies { get; set; }

        public AIGMCompanionProfile()
        {
            VoiceTraits = Array.Empty<string>();
            Duties = Array.Empty<string>();
            SiblingContext = Array.Empty<string>();
            Aliases = Array.Empty<string>();
            AllowedIntentKinds = Array.Empty<string>();
            AllowedSkillFamilies = Array.Empty<string>();
        }
    }
}
