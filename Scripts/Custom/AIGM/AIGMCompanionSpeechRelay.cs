using System;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionSpeechRelay
    {
        public static void RelayTrustedSpeech(BaseHire source, Mobile originalSpeaker, string speech)
        {
            if (source == null || source.Deleted || String.IsNullOrWhiteSpace(speech))
                return;

            AIGMCompanionSpeechBus.PublishOwnerSpeech(source, originalSpeaker, speech);
        }

        public static void RelayCompanionUtterance(BaseHire source, string speech)
        {
            if (source == null || source.Deleted || String.IsNullOrWhiteSpace(speech))
                return;

            AIGMCompanionSpeechBus.PublishCompanionSpeech(source, speech);
        }
    }
}
