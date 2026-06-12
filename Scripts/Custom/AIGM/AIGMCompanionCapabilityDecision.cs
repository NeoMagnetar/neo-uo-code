using System;

namespace Server.Custom.AIGM
{
    public sealed class AIGMCompanionCapabilityDecision
    {
        public bool Allowed { get; set; }
        public bool Deferred { get; set; }
        public AIGMCompanionCapabilityKind Capability { get; set; }
        public string Reason { get; set; }
        public string VisibleResponse { get; set; }
        public bool RequiresFutureExecutor { get; set; }

        public AIGMCompanionCapabilityDecision()
        {
            Capability = AIGMCompanionCapabilityKind.None;
            Reason = String.Empty;
            VisibleResponse = String.Empty;
        }
    }
}
