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
        public string[] Aliases { get; set; }
        public string[] AllowedIntentKinds { get; set; }
        public string[] AllowedSkillFamilies { get; set; }

        public AIGMCompanionProfile()
        {
            Aliases = Array.Empty<string>();
            AllowedIntentKinds = Array.Empty<string>();
            AllowedSkillFamilies = Array.Empty<string>();
        }
    }
}
