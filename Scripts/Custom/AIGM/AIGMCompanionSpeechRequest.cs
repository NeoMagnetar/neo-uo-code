using System;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public sealed class AIGMCompanionSpeechRequest
    {
        public string RequestId { get; private set; }
        public Serial CompanionSerial { get; private set; }
        public Serial SpeakerSerial { get; private set; }
        public string CompanionName { get; private set; }
        public string CompanionTypeName { get; private set; }
        public string CompanionProfileKey { get; private set; }
        public string SpeakerName { get; private set; }
        public string SpeakerTypeName { get; private set; }
        public bool SpeakerIsCompanion { get; private set; }
        public string DialogueMode { get; private set; }
        public string Text { get; private set; }
        public string MapName { get; private set; }
        public string RegionName { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }
        public int Z { get; private set; }
        public DateTime CreatedUtc { get; private set; }

        public AIGMCompanionSpeechRequest(Mobile companion, Mobile speaker, string text)
            : this(companion, speaker, text, speaker is BaseHire ? "companion_dialogue" : "owner_or_world_speech")
        {
        }

        public AIGMCompanionSpeechRequest(Mobile companion, Mobile speaker, string text, string dialogueMode)
        {
            RequestId = Guid.NewGuid().ToString("N");
            CompanionSerial = companion == null ? Serial.Zero : companion.Serial;
            SpeakerSerial = speaker == null ? Serial.Zero : speaker.Serial;
            CompanionName = companion == null ? "(null)" : (companion.Name ?? companion.GetType().Name);
            CompanionTypeName = companion == null ? "(null)" : companion.GetType().Name;
            CompanionProfileKey = ResolveProfileKey(companion);
            SpeakerName = speaker == null ? "(null)" : (speaker.Name ?? speaker.GetType().Name);
            SpeakerTypeName = speaker == null ? "(null)" : speaker.GetType().Name;
            SpeakerIsCompanion = speaker is BaseHire;
            DialogueMode = String.IsNullOrWhiteSpace(dialogueMode) ? "owner_or_world_speech" : dialogueMode;
            Text = text ?? String.Empty;
            MapName = speaker != null && speaker.Map != null ? speaker.Map.Name : null;
            RegionName = speaker != null && speaker.Region != null ? speaker.Region.Name : null;
            X = speaker == null ? 0 : speaker.X;
            Y = speaker == null ? 0 : speaker.Y;
            Z = speaker == null ? 0 : speaker.Z;
            CreatedUtc = DateTime.UtcNow;
        }

        private static string ResolveProfileKey(Mobile companion)
        {
            if (companion is Server.Mobiles.AIGMCompanionDanyal)
                return "danyal";

            if (companion is Server.Mobiles.AIGMCompanionDakeyras)
                return "dakeyras";

            return "default";
        }
    }
}
