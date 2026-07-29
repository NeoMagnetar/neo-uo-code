namespace Server.Custom.AIGM
{
    public sealed class AIGMCompanionSpellProfile
    {
        public string Name;
        public bool AllowCure;
        public bool AllowHeal;
        public bool AllowBless;

        public static AIGMCompanionSpellProfile SupportOnly()
        {
            return new AIGMCompanionSpellProfile
            {
                Name = "support",
                AllowCure = true,
                AllowHeal = true,
                AllowBless = true
            };
        }

        public static AIGMCompanionSpellProfile None()
        {
            return new AIGMCompanionSpellProfile { Name = "none" };
        }

        public static AIGMCompanionSpellProfile FromText(string text)
        {
            string normalized = string.IsNullOrWhiteSpace(text) ? "support" : text.Trim().ToLowerInvariant();
            if (normalized == "none" || normalized == "off")
                return None();
            return SupportOnly();
        }
    }
}
