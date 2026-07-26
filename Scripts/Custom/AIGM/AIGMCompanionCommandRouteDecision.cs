using System;

namespace Server.Custom.AIGM
{
    public sealed class AIGMCompanionCommandRouteDecision
    {
        public string OriginalSpeech { get; set; }
        public string NormalizedSpeech { get; set; }
        public bool IsCompanionCommand { get; set; }
        public bool BlocksCounselorLane { get; set; }
        public bool IsNamedCompanionCommand { get; set; }
        public bool IsSharedCompanionCommand { get; set; }
        public string CompanionKey { get; set; }
        public string CompanionName { get; set; }
        public string CommandVerb { get; set; }
        public AIGMCompanionCommandVerbKind VerbKind { get; set; }
        public AIGMCompanionCapabilityKind Capability { get; set; }
        public bool IsDeferredCapability { get; set; }
        public bool RequiresFutureExecutor { get; set; }
        public AIGMCompanionCommandRouteKind RouteKind { get; set; }
        public string Reason { get; set; }
        public bool IsExecutableNow { get; set; }

        public AIGMCompanionCommandRouteDecision()
        {
            OriginalSpeech = String.Empty;
            NormalizedSpeech = String.Empty;
            CompanionKey = null;
            CompanionName = null;
            CommandVerb = null;
            VerbKind = AIGMCompanionCommandVerbKind.None;
            Capability = AIGMCompanionCapabilityKind.None;
            IsDeferredCapability = false;
            RequiresFutureExecutor = false;
            RouteKind = AIGMCompanionCommandRouteKind.None;
            Reason = "not_companion_command";
            IsExecutableNow = false;
        }
    }
}
