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

        public static void PublishDialogue(BaseHire sourceCompanion, string speech)
        {
            if (sourceCompanion == null || sourceCompanion.Deleted || sourceCompanion.Map == null || String.IsNullOrWhiteSpace(speech))
            {
                AIGMExecutionLog.Write("DIALOGUE_PUBLISH_SKIP reason=invalid_source_or_text");
                return;
            }

            Mobile owner = sourceCompanion.GetOwner();
            if (owner == null)
            {
                AIGMExecutionLog.Write("DIALOGUE_PUBLISH_SKIP source={0} reason=no_owner", sourceCompanion.Serial.Value);
                return;
            }

            List<BaseHire> linkedCompanions = GetLinkedCompanions(sourceCompanion, owner);
            AIGMExecutionLog.Write("DIALOGUE_PUBLISH source={0} sourceName={1} linked={2} text=\"{3}\"", sourceCompanion.Serial.Value, sourceCompanion.Name ?? sourceCompanion.GetType().Name, linkedCompanions.Count, SafeLog(speech));
            for (int i = 0; i < linkedCompanions.Count; i++)
            {
                BaseHire target = linkedCompanions[i];
                AIGMCompanionDialogueEvent dialogueEvent = new AIGMCompanionDialogueEvent(sourceCompanion.Serial, target.Serial, sourceCompanion.Name ?? sourceCompanion.GetType().Name, target.Name ?? target.GetType().Name, speech, 0);
                if (!TryMarkRecent(dialogueEvent))
                {
                    AIGMExecutionLog.Write("DIALOGUE_DEDUPE source={0} target={1} text=\"{2}\"", sourceCompanion.Serial.Value, target.Serial.Value, SafeLog(speech));
                    continue;
                }

                Deliver(target, sourceCompanion, dialogueEvent);
            }
        }

        private static void Deliver(BaseHire target, BaseHire sourceCompanion, AIGMCompanionDialogueEvent dialogueEvent)
        {
            if (target == null || target.Deleted || sourceCompanion == null || sourceCompanion.Deleted || dialogueEvent == null)
            {
                AIGMExecutionLog.Write("DIALOGUE_DELIVER_SKIP reason=invalid_delivery_state");
                return;
            }

            AIGMExecutionLog.Write("DIALOGUE_DELIVER eventId={0} source={1} target={2} hop={3} text=\"{4}\"", dialogueEvent.EventId, sourceCompanion.Serial.Value, target.Serial.Value, dialogueEvent.HopCount, SafeLog(dialogueEvent.Text));
            if (target is AIGMCompanionDakeyras)
                ((AIGMCompanionDakeyras)target).ReceiveCompanionDialogue(sourceCompanion, dialogueEvent);
            else if (target is AIGMCompanionDanyal)
                ((AIGMCompanionDanyal)target).ReceiveCompanionDialogue(sourceCompanion, dialogueEvent);
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

        private static string SafeLog(string value)
        {
            if (String.IsNullOrEmpty(value))
                return String.Empty;

            value = value.Replace("\r", " ").Replace("\n", " ");
            if (value.Length > 240)
                value = value.Substring(0, 240) + "...";

            return value;
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

                if (!(ally is AIGMCompanionDakeyras) && !(ally is AIGMCompanionDanyal))
                    continue;

                if (ally.GetOwner() != owner)
                    continue;

                linked.Add(ally);
            }

            return linked;
        }
    }
}
