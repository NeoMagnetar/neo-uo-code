using System;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionAwarenessBus
    {
        public static bool ShareLatestThreatSummary(BaseHire source, out string response)
        {
            response = null;

            if (source == null || source.Deleted || source.Map == null)
            {
                response = "I cannot share what I see just now.";
                return false;
            }

            AIGMCompanionPerceptionState sourceState = AIGMCompanionPerceptionBuffer.Get(source);
            if (sourceState == null || sourceState.LastSightingsBySerial.Count == 0)
            {
                response = "I have nothing new to share just now.";
                return false;
            }

            int delivered = 0;
            IPooledEnumerable eable = source.GetMobilesInRange(12);
            foreach (Mobile mobile in eable)
            {
                BaseHire companion = mobile as BaseHire;
                if (companion == null || companion == source || companion.Deleted || companion.Map != source.Map)
                    continue;

                if (!(companion is AIGMCompanionDakeyras) && !(companion is AIGMCompanionDanyal) && !(companion is AIGMCompanionDardalion))
                    continue;

                AIGMCompanionAwarenessEvent awarenessEvent = new AIGMCompanionAwarenessEvent();
                awarenessEvent.SourceCompanionSerial = source.Serial.Value;
                awarenessEvent.SourceCompanionName = source.Name ?? source.GetType().Name;
                awarenessEvent.TargetCompanionSerial = companion.Serial.Value;
                awarenessEvent.EventKind = "threat_summary";
                awarenessEvent.Summary = BuildThreatSummary(sourceState);
                awarenessEvent.TimestampUtc = DateTime.UtcNow;
                AIGMCompanionPerceptionBuffer.RecordAwarenessEvent(companion, awarenessEvent);
                delivered++;
            }
            eable.Free();

            response = delivered > 0 ? "I have shared what I see with nearby companions." : "No nearby companion was ready to receive my report.";
            return delivered > 0;
        }

        private static string BuildThreatSummary(AIGMCompanionPerceptionState state)
        {
            if (state == null || state.LastSightingsBySerial.Count == 0)
                return "No active threats recorded.";

            int immediate = 0;
            int potential = 0;
            foreach (AIGMCompanionTrackingEntry entry in state.LastSightingsBySerial.Values)
            {
                if (entry == null)
                    continue;

                if (entry.ThreatHint == AIGMCompanionThreatLevel.ImmediateThreat)
                    immediate++;
                else if (entry.ThreatHint == AIGMCompanionThreatLevel.PotentialThreat)
                    potential++;
            }

            return String.Format("Threat summary: immediate={0}, potential={1}.", immediate, potential);
        }
    }
}
