using System;
using Server.Custom.AIGM;
using Server.Mobiles;

namespace Server.Commands
{
    public static class AIGMTrackHuntProofCommand
    {
        private const int CompanionRange = 20;
        private const int TargetRange = 20;
        private const int PulseCount = 20;

        public static void Initialize()
        {
            CommandSystem.Register("AIGMTrackHuntProof", AccessLevel.GameMaster, OnCommand);
        }

        private static void OnCommand(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            Mobile caller = e.Mobile;
            string mode;
            string companionArg;
            ParseArgs(e.ArgString, out mode, out companionArg);

            BaseHire companion = ResolveCompanion(caller, companionArg, CompanionRange);
            if (companion == null)
            {
                caller.SendMessage("No nearby AIGM companion found.");
                AIGMExecutionLog.Write("TRACKHUNT_PROOF_RESULT result=fail reason=no_companion caller={0}", caller.Serial.Value);
                return;
            }

            Mobile target = ResolveNearestMonster(companion, TargetRange);
            if (target == null)
            {
                caller.SendMessage("No valid monster in proof range. Spawn/place one and rerun.");
                AIGMExecutionLog.Write("TRACKHUNT_PROOF_RESULT result=fail companion={0} reason=no_valid_monster_in_range", companion.Serial.Value);
                return;
            }

            int beforeHits = target.Hits;
            int distance = (int)Math.Round(companion.GetDistanceToSqrt(target));
            string start;
            AIGMCompanionTrackingState state = null;
            bool nativeMode = EqualsIgnoreCase(mode, "native");
            if (nativeMode)
            {
                string nativeResult;
                bool ordered = AIGMCompanionCombatController.TryEngageMonster(companion, target, out nativeResult);
                start = nativeResult;
                AIGMExecutionLog.Write("TRACKHUNT_PROOF_NATIVE_KILL_START companion={0} companionName=\"{1}\" target={2} targetName=\"{3}\" distance={4} beforeHits={5} ordered={6} result=\"{7}\" combatant={8} warmode={9} controlTarget={10} order={11}",
                    companion.Serial.Value, SafeLog(SafeName(companion)), target.Serial.Value, SafeLog(SafeName(target)), distance, beforeHits, ordered, SafeLog(nativeResult), companion.Combatant != null ? companion.Combatant.Serial.Value : 0, companion.Warmode, companion.ControlTarget != null ? companion.ControlTarget.Serial.Value : 0, companion.ControlOrder);
            }
            else
            {
                start = AIGMCompanionTrackingService.StartTrackingAction(companion, caller, AIGMCompanionTrackingMode.Monsters, AIGMCompanionTrackingActionMode.TrackHunt);
                state = AIGMCompanionTrackingService.GetState(companion);
                AIGMExecutionLog.Write("TRACKHUNT_PROOF_TRACKHUNT_START companion={0} companionName=\"{1}\" target={2} targetName=\"{3}\" distance={4} beforeHits={5}",
                    companion.Serial.Value, SafeLog(SafeName(companion)), target.Serial.Value, SafeLog(SafeName(target)), distance, beforeHits);
            }

            AIGMExecutionLog.Write("TRACKHUNT_PROOF_START companion={0} companionName=\"{1}\" target={2} targetName=\"{3}\" distance={4} beforeHits={5}",
                companion.Serial.Value, SafeLog(SafeName(companion)), target.Serial.Value, SafeLog(SafeName(target)), distance, beforeHits);
            AIGMExecutionLog.Write("TRACKHUNT_PROOF_AFTER_START companion={0} stateActive={1} mode={2} action={3} stateTarget={4} expectedTarget={5} startLine=\"{6}\" targetHits={7}",
                companion.Serial.Value, state != null && state.IsActive, state != null ? state.Mode.ToString() : "none", state != null ? state.ActionMode.ToString() : "none", state != null ? state.CurrentTargetSerial : 0, target.Serial.Value, SafeLog(start), target.Hits);

            caller.SendMessage("AIGM {0} proof started for {1} against {2}. Watch for 20 seconds.", nativeMode ? "native kill" : "TrackHunt", SafeName(companion), SafeName(target));
            new TrackHuntProofTimer(caller, companion, target, beforeHits, nativeMode).Start();
        }

        private static void ParseArgs(string argString, out string mode, out string companionArg)
        {
            mode = "trackhunt";
            companionArg = String.Empty;

            string raw = (argString ?? String.Empty).Trim();
            if (String.IsNullOrWhiteSpace(raw))
                return;

            string[] parts = raw.Split(new char[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0 && (EqualsIgnoreCase(parts[0], "native") || EqualsIgnoreCase(parts[0], "trackhunt")))
            {
                mode = parts[0].ToLowerInvariant();
                companionArg = parts.Length > 1 ? parts[1] : String.Empty;
                return;
            }

            companionArg = raw;
        }

        private static BaseHire ResolveCompanion(Mobile from, string argString, int range)
        {
            if (from == null || from.Map == null)
                return null;

            string requested = (argString ?? String.Empty).Trim().ToLowerInvariant();
            BaseHire best = null;
            int bestDistance = Int32.MaxValue;

            IPooledEnumerable mobiles = from.Map.GetMobilesInRange(from.Location, range);
            foreach (Mobile mobile in mobiles)
            {
                BaseHire hire = mobile as BaseHire;
                IAIGMCompanionActor actor = mobile as IAIGMCompanionActor;
                if (hire == null || actor == null || mobile.Deleted || !mobile.Alive)
                    continue;

                if (!String.IsNullOrWhiteSpace(requested) && !MatchesCompanion(actor, hire, requested))
                    continue;

                int distance = (int)from.GetDistanceToSqrt(mobile);
                if (distance < bestDistance)
                {
                    best = hire;
                    bestDistance = distance;
                }
            }
            mobiles.Free();

            return best;
        }

        private static bool MatchesCompanion(IAIGMCompanionActor actor, BaseHire hire, string requested)
        {
            if (String.IsNullOrWhiteSpace(requested))
                return true;

            return EqualsIgnoreCase(actor.CompanionId, requested)
                || EqualsIgnoreCase(actor.CompanionDisplayName, requested)
                || EqualsIgnoreCase(hire.Name, requested);
        }

        private static Mobile ResolveNearestMonster(BaseHire companion, int range)
        {
            if (companion == null || companion.Map == null)
                return null;

            Mobile best = null;
            double bestDistance = Double.MaxValue;
            IPooledEnumerable mobiles = companion.GetMobilesInRange(range);
            foreach (Mobile mobile in mobiles)
            {
                if (mobile == null || mobile == companion || mobile.Deleted || mobile.Map != companion.Map)
                    continue;

                AIGMCompanionTargetValidationResult validation = AIGMCompanionTargetValidator.ValidateMonsterTarget(companion, mobile, range);
                if (!validation.Allowed)
                    continue;

                double distance = companion.GetDistanceToSqrt(mobile);
                if (distance < bestDistance)
                {
                    best = mobile;
                    bestDistance = distance;
                }
            }
            mobiles.Free();

            return best;
        }

        private static string SafeName(Mobile mobile)
        {
            if (mobile == null)
                return "none";

            return String.IsNullOrWhiteSpace(mobile.Name) ? mobile.GetType().Name : mobile.Name;
        }

        private static string SafeLog(string value)
        {
            return String.IsNullOrWhiteSpace(value) ? String.Empty : value.Replace("\"", "'");
        }

        private static bool EqualsIgnoreCase(string left, string right)
        {
            return String.Equals(left ?? String.Empty, right ?? String.Empty, StringComparison.OrdinalIgnoreCase);
        }

        private sealed class TrackHuntProofTimer : Timer
        {
            private readonly Mobile m_Caller;
            private readonly BaseHire m_Companion;
            private readonly Mobile m_Target;
            private readonly int m_BeforeHits;
            private readonly bool m_NativeMode;
            private int m_Pulses;

            public TrackHuntProofTimer(Mobile caller, BaseHire companion, Mobile target, int beforeHits, bool nativeMode)
                : base(TimeSpan.FromSeconds(1.1), TimeSpan.FromSeconds(1.0))
            {
                m_Caller = caller;
                m_Companion = companion;
                m_Target = target;
                m_BeforeHits = beforeHits;
                m_NativeMode = nativeMode;
                Priority = TimerPriority.OneSecond;
            }

            protected override void OnTick()
            {
                m_Pulses++;

                if (!IsStillValid())
                {
                    Finish("fail", "invalid_context");
                    return;
                }

                int beforePulseHits = m_Target.Hits;
                string pulse = m_NativeMode ? String.Empty : AIGMCompanionTrackingService.TickTrackingAction(m_Companion);
                AIGMCompanionTrackingState state = AIGMCompanionTrackingService.GetState(m_Companion);
                int distance = (int)Math.Round(m_Companion.GetDistanceToSqrt(m_Target));
                int combatant = m_Companion.Combatant != null ? m_Companion.Combatant.Serial.Value : 0;
                int stateTarget = state != null ? state.CurrentTargetSerial : 0;

                AIGMExecutionLog.Write("TRACKHUNT_PROOF_AFTER_PULSE mode={0} pulse={1} companion={2} target={3} stateTarget={4} distance={5} combatant={6} warmode={7} controlTarget={8} order={9} targetHits={10} beforePulseHits={11} line=\"{12}\"",
                    m_NativeMode ? "native" : "trackhunt", m_Pulses, m_Companion.Serial.Value, m_Target.Serial.Value, stateTarget, distance, combatant, m_Companion.Warmode, m_Companion.ControlTarget != null ? m_Companion.ControlTarget.Serial.Value : 0, m_Companion.ControlOrder, m_Target.Hits, beforePulseHits, SafeLog(pulse));

                if (m_Target.Hits < m_BeforeHits)
                {
                    AIGMExecutionLog.Write("TRACKHUNT_PROOF_DAMAGE companion={0} target={1} beforeHits={2} afterHits={3} pulse={4}", m_Companion.Serial.Value, m_Target.Serial.Value, m_BeforeHits, m_Target.Hits, m_Pulses);
                    Finish("pass", "target_damaged");
                    return;
                }

                if (m_Pulses >= PulseCount)
                    Finish("fail", "no_damage_after_pulses");
            }

            private bool IsStillValid()
            {
                return m_Companion != null && !m_Companion.Deleted && m_Companion.Alive
                    && m_Target != null && !m_Target.Deleted && m_Target.Alive
                    && m_Companion.Map != null && m_Target.Map == m_Companion.Map;
            }

            private void Finish(string result, string reason)
            {
                Stop();

                int afterHits = m_Target != null && !m_Target.Deleted ? m_Target.Hits : -1;
                int combatant = m_Companion != null && m_Companion.Combatant != null ? m_Companion.Combatant.Serial.Value : 0;

                AIGMExecutionLog.Write("TRACKHUNT_PROOF_RESULT mode={0} result={1} reason={2} companion={3} target={4} beforeHits={5} afterHits={6} pulses={7} combatant={8}",
                    m_NativeMode ? "native" : "trackhunt", result, reason, m_Companion != null ? m_Companion.Serial.Value : 0, m_Target != null ? m_Target.Serial.Value : 0, m_BeforeHits, afterHits, m_Pulses, combatant);

                if (m_Caller != null && !m_Caller.Deleted)
                    m_Caller.SendMessage("AIGM TrackHunt proof {0}: {1}. Target hits {2}->{3}.", result, reason, m_BeforeHits, afterHits);
            }
        }
    }
}
