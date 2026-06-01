using System;
using Server;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionSpeechRelay
    {
        public static void RelayTrustedSpeech(BaseHire source, Mobile originalSpeaker, string speech)
        {
            RelayToLinkedCompanions(source, originalSpeaker, speech);
        }

        public static void RelayCompanionUtterance(BaseHire source, string speech)
        {
            RelayToLinkedCompanions(source, source, speech);
        }

        private static void RelayToLinkedCompanions(BaseHire source, Mobile originalSpeaker, string speech)
        {
            if (source == null || source.Deleted || String.IsNullOrWhiteSpace(speech))
                return;

            Mobile owner = source.GetOwner();
            if (owner == null || source.Map == null)
                return;

            IPooledEnumerable eable = source.GetMobilesInRange(12);
            foreach (Mobile mobile in eable)
            {
                BaseHire ally = mobile as BaseHire;
                if (ally == null || ally == source || ally.Deleted)
                    continue;

                if (ally.GetOwner() != owner)
                    continue;

                if (ally is AIGMCompanionDakeyras)
                    ((AIGMCompanionDakeyras)ally).ReceiveSpeechBusEvent(source, originalSpeaker, speech, true);
                else if (ally is AIGMCompanionDanyal)
                    ((AIGMCompanionDanyal)ally).ReceiveSpeechBusEvent(source, originalSpeaker, speech, true);
            }
            eable.Free();
        }
    }
}
