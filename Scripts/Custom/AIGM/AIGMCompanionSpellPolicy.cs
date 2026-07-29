using System;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionSpellPolicy
    {
        public const double EmergencyHealThreshold = 0.25;
        public const double HealThreshold = 0.50;
        public const double MinimumSupportMagery = 20.0;

        public static readonly TimeSpan PulseInterval = TimeSpan.FromSeconds(2.0);
        public static readonly TimeSpan CastCooldown = TimeSpan.FromSeconds(3.0);
        public static readonly TimeSpan FailureLogCooldown = TimeSpan.FromSeconds(15.0);
        public static readonly TimeSpan TargetInvokePadding = TimeSpan.FromMilliseconds(250);
    }
}
