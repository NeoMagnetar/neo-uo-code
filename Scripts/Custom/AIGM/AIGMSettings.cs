using System;

namespace Server.Custom.AIGM
{
    public static class AIGMSettings
    {
        public const AccessLevel RequiredAccess = AccessLevel.GameMaster;
        public const int MaxQuestionLength = 300;
        public const int MaxReplyLength = 1400;
        public const int BridgeHealthTimeoutMs = 1500;
        public const int BridgeRequestTimeoutMs = 60000;
        public const string BridgeBaseUrl = "http://127.0.0.1:4876";
        public const string BridgeShardName = "NEO UO DEV";
        public const string CounselorName = "Archivist Nox";
        public const string CounselorTitle = "the AI counselor";
        public static readonly bool EnableDebugLogging = false;
        public static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(5);
    }
}
