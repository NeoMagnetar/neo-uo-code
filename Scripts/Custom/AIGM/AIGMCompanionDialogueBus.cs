using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Server;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionDialogueBus
    {
        private static readonly ConcurrentDictionary<string, DateTime> RecentDialogue = new ConcurrentDictionary<string, DateTime>();
        private static readonly TimeSpan RecentDialogueTtl = TimeSpan.FromSeconds(8.0);
        private const int MaxHopCount = 1;

        public static void PublishDialogue(BaseHire sourceCompanion, string speech)
        {
            PublishDialogue(sourceCompanion, speech, null);
        }

        public static void PublishDialogue(BaseHire sourceCompanion, string speech, string preferredTargetCompanionId)
        {
            if (sourceCompanion == null || sourceCompanion.Deleted || sourceCompanion.Map == null || String.IsNullOrWhiteSpace(speech))
                return;

            Mobile owner = sourceCompanion.GetOwner();
            if (owner == null)
                return;

            List<BaseHire> linkedCompanions = GetLinkedCompanions(sourceCompanion, owner);
            RecordDialogueForParty(sourceCompanion, linkedCompanions, speech);

            for (int i = 0; i < linkedCompanions.Count; i++)
            {
                BaseHire target = linkedCompanions[i];
                IAIGMCompanionActor targetActor = target as IAIGMCompanionActor;
                if (!String.IsNullOrWhiteSpace(preferredTargetCompanionId) && (targetActor == null || !String.Equals(targetActor.CompanionId, preferredTargetCompanionId, StringComparison.OrdinalIgnoreCase)))
                    continue;

                AIGMCompanionDialogueEvent dialogueEvent = new AIGMCompanionDialogueEvent(sourceCompanion.Serial, target.Serial, sourceCompanion.Name ?? sourceCompanion.GetType().Name, target.Name ?? target.GetType().Name, preferredTargetCompanionId, speech, 0);
                if (!TryMarkRecent(dialogueEvent))
                    continue;

                Deliver(target, sourceCompanion, dialogueEvent);
            }
        }

        private static void RecordDialogueForParty(BaseHire sourceCompanion, List<BaseHire> linkedCompanions, string speech)
        {
            IAIGMCompanionActor sourceActor = sourceCompanion as IAIGMCompanionActor;
            if (sourceActor != null)
                AIGMCompanionPerceptionBuffer.Record(sourceActor, "companion_dialogue", sourceCompanion, speech);

            if (linkedCompanions == null)
                return;

            for (int i = 0; i < linkedCompanions.Count; i++)
            {
                IAIGMCompanionActor actor = linkedCompanions[i] as IAIGMCompanionActor;
                if (actor != null)
                    AIGMCompanionPerceptionBuffer.Record(actor, "companion_dialogue", sourceCompanion, speech);
            }
        }

        private static void Deliver(BaseHire target, BaseHire sourceCompanion, AIGMCompanionDialogueEvent dialogueEvent)
        {
            if (target == null || target.Deleted || sourceCompanion == null || sourceCompanion.Deleted || dialogueEvent == null)
                return;

            if (dialogueEvent.HopCount > MaxHopCount)
                return;

            if (target is AIGMCompanionDakeyras)
                ((AIGMCompanionDakeyras)target).ReceiveCompanionDialogue(sourceCompanion, dialogueEvent);
            else if (target is AIGMCompanionDanyal)
                ((AIGMCompanionDanyal)target).ReceiveCompanionDialogue(sourceCompanion, dialogueEvent);
            else if (target is AIGMCompanionDardalion)
                ((AIGMCompanionDardalion)target).ReceiveCompanionDialogue(sourceCompanion, dialogueEvent);
        }

        private static bool TryMarkRecent(AIGMCompanionDialogueEvent dialogueEvent)
        {
            if (dialogueEvent == null)
                return false;

            PruneExpired();
            string key = MakeKey(dialogueEvent.TargetCompanionSerial, dialogueEvent.SourceCompanionSerial, dialogueEvent.Text);
            DateTime now = DateTime.UtcNow;
            DateTime existing;
            if (RecentDialogue.TryGetValue(key, out existing) && (now - existing) < RecentDialogueTtl)
                return false;

            RecentDialogue[key] = now;
            return true;
        }

        private static void PruneExpired()
        {
            DateTime now = DateTime.UtcNow;
            foreach (KeyValuePair<string, DateTime> pair in RecentDialogue)
            {
                if ((now - pair.Value) >= RecentDialogueTtl)
                    RecentDialogue.TryRemove(pair.Key, out _);
            }
        }

        private static string MakeKey(Serial targetSerial, Serial sourceSerial, string text)
        {
            return targetSerial.Value + ":" + sourceSerial.Value + ":" + (text ?? String.Empty).Trim().ToLowerInvariant();
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
