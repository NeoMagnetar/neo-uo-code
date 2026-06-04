using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionSpeechBus
    {
        public static void PublishOwnerSpeech(BaseHire hearingCompanion, Mobile ownerSpeaker, string speech)
        {
            if (hearingCompanion == null || hearingCompanion.Deleted || ownerSpeaker == null || String.IsNullOrWhiteSpace(speech))
                return;

            RelayToLinkedCompanions(hearingCompanion, ownerSpeaker, speech, false);
        }

        public static void PublishCompanionSpeech(BaseHire sourceCompanion, string speech)
        {
            if (sourceCompanion == null || sourceCompanion.Deleted || String.IsNullOrWhiteSpace(speech))
                return;

            RelayToLinkedCompanions(sourceCompanion, sourceCompanion, speech, true);
        }

        public static void PublishDelayedCompanionSpeech(BaseHire sourceCompanion, string speech, TimeSpan delay)
        {
            if (sourceCompanion == null || sourceCompanion.Deleted || String.IsNullOrWhiteSpace(speech))
                return;

            Timer.DelayCall(delay, delegate
            {
                if (sourceCompanion == null || sourceCompanion.Deleted || sourceCompanion.Map == null)
                    return;

                RelayToLinkedCompanions(sourceCompanion, sourceCompanion, speech, true);
            });
        }

        private static void RelayToLinkedCompanions(BaseHire sourceCompanion, Mobile eventSpeaker, string speech, bool companionOrigin)
        {
            Mobile owner = sourceCompanion.GetOwner();
            if (owner == null || sourceCompanion.Map == null)
                return;

            List<BaseHire> linkedCompanions = GetLinkedCompanions(sourceCompanion, owner);
            for (int i = 0; i < linkedCompanions.Count; i++)
            {
                BaseHire ally = linkedCompanions[i];
                if (ally is AIGMCompanionDakeyras)
                    ((AIGMCompanionDakeyras)ally).ReceiveSpeechBusEvent(sourceCompanion, eventSpeaker, speech, companionOrigin);
                else if (ally is AIGMCompanionDanyal)
                    ((AIGMCompanionDanyal)ally).ReceiveSpeechBusEvent(sourceCompanion, eventSpeaker, speech, companionOrigin);
                else if (ally is AIGMCompanionDardalion)
                    ((AIGMCompanionDardalion)ally).ReceiveSpeechBusEvent(sourceCompanion, eventSpeaker, speech, companionOrigin);
            }
        }

        private static List<BaseHire> GetLinkedCompanions(BaseHire sourceCompanion, Mobile owner)
        {
            List<BaseHire> linked = new List<BaseHire>();
            if (sourceCompanion == null || owner == null)
                return linked;

            foreach (Mobile mobile in World.Mobiles.Values)
            {
                BaseHire ally = mobile as BaseHire;
                if (ally == null || ally == sourceCompanion || ally.Deleted)
                    continue;

                if (ally.Map != sourceCompanion.Map)
                    continue;

                if (!(ally is AIGMCompanionDakeyras) && !(ally is AIGMCompanionDanyal) && !(ally is AIGMCompanionDardalion))
                    continue;

                if (ally.GetOwner() != owner)
                    continue;

                linked.Add(ally);
            }

            return linked;
        }
    }
}
