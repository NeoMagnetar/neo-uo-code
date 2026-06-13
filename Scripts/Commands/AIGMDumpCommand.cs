using System;
using System.Text;
using Server.Commands;
using Server.Custom.AIGM;
using Server.Mobiles;

namespace Server.Commands
{
    public static class AIGMDumpCommand
    {
        public static void Initialize()
        {
            CommandSystem.Register("AIGMDump", AccessLevel.GameMaster, OnCommand);
        }

        private static void OnCommand(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            string arg = e.ArgString == null ? String.Empty : e.ArgString.Trim().ToLowerInvariant();
            BaseHire companion = ResolveNearestCompanion(e.Mobile, 12);
            if (companion == null)
            {
                e.Mobile.SendMessage("No nearby AIGM companion found.");
                return;
            }

            if (arg == "companionstate" || arg == "actiontrace" || arg == "capabilitystate" || String.IsNullOrEmpty(arg))
            {
                AIGMCompanionExecutionState state = AIGMCompanionExecutionSpine.GetState(companion);
                if (state == null)
                {
                    e.Mobile.SendMessage("No execution state found.");
                    return;
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("Companion={0}; HuntActive={1}; Phase={2}; Reason={3}; Target={4}; Trace={5}; LastMove={6}; LastCombat={7}; LastReject={8}",
                    companion.Name,
                    state.HuntActive,
                    state.Phase,
                    state.PhaseReason,
                    String.IsNullOrWhiteSpace(state.CurrentTargetName) ? "none" : state.CurrentTargetName,
                    String.IsNullOrWhiteSpace(state.LastTrace) ? "none" : state.LastTrace,
                    String.IsNullOrWhiteSpace(state.LastMovementResult) ? "none" : state.LastMovementResult,
                    String.IsNullOrWhiteSpace(state.LastCombatResult) ? "none" : state.LastCombatResult,
                    String.IsNullOrWhiteSpace(state.LastTargetRejectionReason) ? "none" : state.LastTargetRejectionReason);
                e.Mobile.SendMessage(sb.ToString());
                return;
            }

            e.Mobile.SendMessage("Usage: [AIGMDump CompanionState | CapabilityState | ActionTrace");
        }

        private static BaseHire ResolveNearestCompanion(Mobile from, int range)
        {
            if (from == null || from.Map == null)
                return null;

            BaseHire best = null;
            int bestDistance = Int32.MaxValue;
            IPooledEnumerable mobiles = from.Map.GetMobilesInRange(from.Location, range);
            foreach (Mobile mobile in mobiles)
            {
                BaseHire hire = mobile as BaseHire;
                IAIGMCompanionActor actor = mobile as IAIGMCompanionActor;
                if (hire == null || actor == null || mobile.Deleted || !mobile.Alive)
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
    }
}
