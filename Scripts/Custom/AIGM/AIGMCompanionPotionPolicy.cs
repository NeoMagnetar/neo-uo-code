using System;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionPotionPolicy
    {
        public const double HealThreshold = 0.50;
        public const double EmergencyHealThreshold = 0.25;
        public const double RefreshThreshold = 0.50;

        public static readonly TimeSpan PulseInterval = TimeSpan.FromSeconds(2.0);
        public static readonly TimeSpan CureCooldown = TimeSpan.FromSeconds(10.0);
        public static readonly TimeSpan RefreshCooldown = TimeSpan.FromSeconds(5.0);
        public static readonly TimeSpan BuffAttemptCooldown = TimeSpan.FromSeconds(10.0);
        public static readonly TimeSpan NoPotionLogCooldown = TimeSpan.FromSeconds(20.0);
    }
}
