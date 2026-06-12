using System;
using Server;

namespace Server.Custom.AIGM
{
    public sealed class AIGMCompanionDialogueEvent
    {
        public Guid EventId { get; private set; }
        public Serial SourceCompanionSerial { get; private set; }
        public Serial TargetCompanionSerial { get; private set; }
        public string SourceCompanionName { get; private set; }
        public string TargetCompanionName { get; private set; }
        public string Text { get; private set; }
        public int HopCount { get; private set; }
        public DateTime CreatedUtc { get; private set; }

        public AIGMCompanionDialogueEvent(Serial sourceCompanionSerial, Serial targetCompanionSerial, string sourceCompanionName, string targetCompanionName, string text, int hopCount)
        {
            EventId = Guid.NewGuid();
            SourceCompanionSerial = sourceCompanionSerial;
            TargetCompanionSerial = targetCompanionSerial;
            SourceCompanionName = sourceCompanionName ?? String.Empty;
            TargetCompanionName = targetCompanionName ?? String.Empty;
            Text = text ?? String.Empty;
            HopCount = hopCount;
            CreatedUtc = DateTime.UtcNow;
        }
    }
}
