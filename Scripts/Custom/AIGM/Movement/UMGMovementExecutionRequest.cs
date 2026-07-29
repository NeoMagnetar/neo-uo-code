using System;

namespace Server.Custom.AIGM
{
    public sealed class UMGMovementExecutionRequest
    {
        public string RequestId { get; set; }
        public string Source { get; set; }
        public string Reason { get; set; }
        public bool IsDryRun { get; set; }
        public UMGMovementIntent Intent { get; set; }
        public UMGMovementState State { get; set; }
        public UMGMovementGateDecision GateDecision { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string ActorId { get; set; }
        public string ActorProfileKey { get; set; }

        public UMGMovementExecutionRequest()
        {
            CreatedUtc = DateTime.UtcNow;
        }
    }
}
