using System;
using System.Collections.Generic;
using Server;
using Server.Custom.AIGM.Characters.Waylander;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionSpeechBus
    {
        public static void PublishOwnerSpeech(BaseHire hearingCompanion, Mobile ownerSpeaker, string speech)
        {
            if (hearingCompanion == null || hearingCompanion.Deleted || ownerSpeaker == null || String.IsNullOrWhiteSpace(speech))
                return;

            if (AIGMCompanionTurnCoordinator.ShouldSuppressOwnerSpeechRelay(speech))
                return;

            RelayToLinkedCompanions(hearingCompanion as IAIGMCompanionActor, ownerSpeaker, speech, false);
        }

        public static void PublishCompanionSpeech(BaseHire sourceCompanion, string speech)
        {
            if (sourceCompanion == null || sourceCompanion.Deleted || String.IsNullOrWhiteSpace(speech))
                return;

            RelayToLinkedCompanions(sourceCompanion as IAIGMCompanionActor, sourceCompanion, speech, true);
        }

        public static void PublishCompanionSpeech(IAIGMCompanionActor sourceCompanion, string speech)
        {
            if (sourceCompanion == null || sourceCompanion.Shell == null || sourceCompanion.Shell.Deleted || String.IsNullOrWhiteSpace(speech))
                return;

            RelayToLinkedCompanions(sourceCompanion, sourceCompanion.Shell, speech, true);
        }

        public static void PublishDelayedCompanionSpeech(BaseHire sourceCompanion, string speech, TimeSpan delay)
        {
            if (sourceCompanion == null || sourceCompanion.Deleted || String.IsNullOrWhiteSpace(speech))
                return;

            Timer.DelayCall(delay, delegate
            {
                if (sourceCompanion == null || sourceCompanion.Deleted || sourceCompanion.Map == null)
                    return;

                RelayToLinkedCompanions(sourceCompanion as IAIGMCompanionActor, sourceCompanion, speech, true);
            });
        }

        public static List<IAIGMCompanionActor> GetLinkedCompanionsIncludingSource(IAIGMCompanionActor source, Mobile owner)
        {
            List<IAIGMCompanionActor> linked = new List<IAIGMCompanionActor>();
            if (source == null || source.Shell == null || source.Shell.Deleted || owner == null || source.Shell.Map == null)
                return linked;

            linked.Add(source);
            foreach (Mobile mobile in World.Mobiles.Values)
            {
                BaseHire hire = mobile as BaseHire;
                IAIGMCompanionActor actor = mobile as IAIGMCompanionActor;
                if (actor == null || actor.Shell == null || actor.Shell.Deleted || actor == source)
                    continue;

                if (actor.Shell.Map != source.Shell.Map)
                    continue;

                if (ResolveOwner(actor) != owner)
                    continue;

                linked.Add(actor);
            }

            return linked;
        }

        public static Mobile ResolveOwner(IAIGMCompanionActor actor)
        {
            if (actor == null || actor.Shell == null)
                return null;

            BaseHire hire = actor.Shell as BaseHire;
            if (hire != null)
                return hire.GetOwner();

            WaylanderRosterMobile roster = actor.Shell as WaylanderRosterMobile;
            if (roster != null && roster.RosterTrustedCommanderSerial != Serial.MinusOne)
                return World.FindMobile(roster.RosterTrustedCommanderSerial);

            return null;
        }

        private static void RelayToLinkedCompanions(IAIGMCompanionActor sourceCompanion, Mobile eventSpeaker, string speech, bool companionOrigin)
        {
            if (sourceCompanion == null || sourceCompanion.Shell == null)
                return;

            Mobile owner = ResolveOwner(sourceCompanion);
            if (owner == null || sourceCompanion.Shell.Map == null)
                return;

            List<IAIGMCompanionActor> linkedCompanions = GetLinkedCompanions(sourceCompanion, owner);
            for (int i = 0; i < linkedCompanions.Count; i++)
            {
                IAIGMCompanionActor ally = linkedCompanions[i];
                BaseHire sourceHire = sourceCompanion.Shell as BaseHire;
                if (sourceHire != null && ally.Shell is AIGMCompanionDakeyras)
                    ((AIGMCompanionDakeyras)ally.Shell).ReceiveSpeechBusEvent(sourceHire, eventSpeaker, speech, companionOrigin);
                else if (sourceHire != null && ally.Shell is AIGMCompanionDanyal)
                    ((AIGMCompanionDanyal)ally.Shell).ReceiveSpeechBusEvent(sourceHire, eventSpeaker, speech, companionOrigin);
                else if (sourceHire != null && ally.Shell is AIGMCompanionDardalion)
                    ((AIGMCompanionDardalion)ally.Shell).ReceiveSpeechBusEvent(sourceHire, eventSpeaker, speech, companionOrigin);
                else if (ally.Shell is WaylanderRosterMobile)
                    ((WaylanderRosterMobile)ally.Shell).ReceiveSpeechBusEvent(sourceCompanion, eventSpeaker, speech, companionOrigin);
            }
        }

        private static List<IAIGMCompanionActor> GetLinkedCompanions(IAIGMCompanionActor sourceCompanion, Mobile owner)
        {
            List<IAIGMCompanionActor> linked = new List<IAIGMCompanionActor>();
            if (sourceCompanion == null || sourceCompanion.Shell == null || owner == null)
                return linked;

            foreach (Mobile mobile in World.Mobiles.Values)
            {
                IAIGMCompanionActor ally = mobile as IAIGMCompanionActor;
                if (ally == null || ally == sourceCompanion || ally.Shell == null || ally.Shell.Deleted)
                    continue;

                if (ally.Shell.Map != sourceCompanion.Shell.Map)
                    continue;

                if (ResolveOwner(ally) != owner)
                    continue;

                linked.Add(ally);
            }

            return linked;
        }
    }
}
