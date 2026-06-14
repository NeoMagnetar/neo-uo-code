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
            CommandSystem.Register("md", AccessLevel.GameMaster, OnCompanionStateAlias);
            CommandSystem.Register("ma", AccessLevel.GameMaster, OnActionTraceAlias);
            CommandSystem.Register("mc", AccessLevel.GameMaster, OnCapabilityStateAlias);
        }

        private static void OnCompanionStateAlias(CommandEventArgs e)
        {
            OnCommandWithArg(e, "companionstate");
        }

        private static void OnActionTraceAlias(CommandEventArgs e)
        {
            OnCommandWithArg(e, "actiontrace");
        }

        private static void OnCapabilityStateAlias(CommandEventArgs e)
        {
            OnCommandWithArg(e, "capabilitystate");
        }

        private static void OnCommand(CommandEventArgs e)
        {
            string arg = e != null && e.ArgString != null ? e.ArgString.Trim().ToLowerInvariant() : String.Empty;
            OnCommandWithArg(e, arg);
        }

        private static void OnCommandWithArg(CommandEventArgs e, string arg)
        {
            if (e == null || e.Mobile == null)
                return;

            arg = arg == null ? String.Empty : arg.Trim().ToLowerInvariant();
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
                sb.AppendFormat("Companion={0}; HuntActive={1}; Phase={2}; Reason={3}; Target={4}; Trace={5}; LastMove={6}; LastMoveFrom={7}; LastMoveTo={8}; LastMoveDirection={9}; LastMoveDistanceBefore={10}; LastMoveDistanceAfter={11}; LastCombat={12}; LastReject={13}; Candidates={14}",
                    companion.Name,
                    state.HuntActive,
                    state.Phase,
                    state.PhaseReason,
                    String.IsNullOrWhiteSpace(state.CurrentTargetName) ? "none" : state.CurrentTargetName,
                    String.IsNullOrWhiteSpace(state.LastTrace) ? "none" : state.LastTrace,
                    String.IsNullOrWhiteSpace(state.LastMovementResult) ? "none" : state.LastMovementResult,
                    FormatPoint(state.LastMoveFrom),
                    FormatPoint(state.LastMoveTo),
                    String.IsNullOrWhiteSpace(state.LastMoveDirection) ? "none" : state.LastMoveDirection,
                    state.LastMoveDistanceBefore,
                    state.LastMoveDistanceAfter,
                    String.IsNullOrWhiteSpace(state.LastCombatResult) ? "none" : state.LastCombatResult,
                    String.IsNullOrWhiteSpace(state.LastTargetRejectionReason) ? "none" : state.LastTargetRejectionReason,
                    String.IsNullOrWhiteSpace(state.LastCandidateSummary) ? "none" : state.LastCandidateSummary);
                e.Mobile.SendMessage(sb.ToString());
                return;
            }

            e.Mobile.SendMessage("Usage: [AIGMDump CompanionState | CapabilityState | ActionTrace");
        }

        private static string FormatPoint(Point3D point)
        {
            return String.Format("({0},{1},{2})", point.X, point.Y, point.Z);
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
