using System;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public sealed class AIGMCompanionCapabilityRequest
    {
        public string CompanionId { get; set; }
        public Mobile Speaker { get; set; }
        public string RawSpeech { get; set; }
        public string IntentKind { get; set; }
        public AIGMCompanionCapabilityKind Capability { get; set; }
        public string TargetText { get; set; }
        public string DestinationText { get; set; }
        public bool IsExplicitlyAddressed { get; set; }
        public string DialogueMode { get; set; }

        public AIGMCompanionCapabilityRequest()
        {
            CompanionId = String.Empty;
            RawSpeech = String.Empty;
            IntentKind = String.Empty;
            Capability = AIGMCompanionCapabilityKind.None;
            TargetText = String.Empty;
            DestinationText = String.Empty;
            DialogueMode = String.Empty;
        }
    }
}
