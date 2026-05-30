using System;

namespace Server.Custom.AIGM
{
    public static class AIGMSettings
    {
        public const AccessLevel RequiredAccess = AccessLevel.GameMaster;
        public const int MaxQuestionLength = 300;
        public const int MaxReplyLength = 1400;
        public const int BridgeHealthTimeoutMs = 1500;
        public const int BridgeRequestTimeoutMs = 20000;
        public static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(5);
    }
}
