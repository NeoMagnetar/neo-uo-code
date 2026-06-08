using System;

namespace Server.Custom.AIGM
{
    public interface IAIGMCompanionActor : IAIGMActor
    {
        string CompanionId { get; }
        string CompanionDisplayName { get; }
        string CompanionRole { get; }
        string CompanionProfileKey { get; }
        bool IsAIGMCompanion { get; }
        bool CanUseAIGMSkills { get; }
        bool GuardOwnerMode { get; set; }
        DateTime NextSupportActionUtc { get; set; }
        string ExecutionModeKey { get; }
    }
}
