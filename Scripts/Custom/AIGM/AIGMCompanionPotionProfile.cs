namespace Server.Custom.AIGM
{
    public sealed class AIGMCompanionPotionProfile
    {
        public bool UseCombatBuffs;
        public bool UseHeal;
        public bool UseCure;
        public bool UseRefresh;

        public static AIGMCompanionPotionProfile DefaultBattleSupport()
        {
            return new AIGMCompanionPotionProfile
            {
                UseCombatBuffs = true,
                UseHeal = true,
                UseCure = true,
                UseRefresh = true
            };
        }

        public AIGMCompanionPotionProfile Clone()
        {
            return new AIGMCompanionPotionProfile
            {
                UseCombatBuffs = UseCombatBuffs,
                UseHeal = UseHeal,
                UseCure = UseCure,
                UseRefresh = UseRefresh
            };
        }
    }
}
