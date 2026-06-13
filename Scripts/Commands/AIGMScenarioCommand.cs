using System;
using System.IO;
using Server.Commands;
using Server.Custom.AIGM;
using Server.Mobiles;

namespace Server.Commands
{
    public static class AIGMScenarioCommand
    {
        public static void Initialize()
        {
            CommandSystem.Register("AIGMScenario", AccessLevel.GameMaster, OnCommand);
        }

        private static void OnCommand(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            string arg = e.ArgString == null ? String.Empty : e.ArgString.Trim();
            if (String.IsNullOrWhiteSpace(arg))
            {
                e.Mobile.SendMessage("Usage: [AIGMScenario MonsterHunt | Healing | Door | Tracking");
                return;
            }

            BaseHire companion = ResolveNearestCompanion(e.Mobile, 12);
            string reportDir = Path.Combine(Core.BaseDirectory, "docs", "runtime");
            Directory.CreateDirectory(reportDir);
            string reportPath = Path.Combine(reportDir, String.Format("PHASE58A_{0}_RUNTIME_PROOF_{1:yyyyMMdd_HHmmss}.md", arg.ToUpperInvariant(), DateTime.UtcNow));

            using (StreamWriter writer = new StreamWriter(reportPath, false))
            {
                writer.WriteLine("# Phase58A Runtime Proof");
                writer.WriteLine();
                writer.WriteLine("- Scenario: " + arg);
                writer.WriteLine("- Actor: " + (companion != null ? companion.Name : "none"));
                writer.WriteLine("- GeneratedUtc: " + DateTime.UtcNow.ToString("u"));
                writer.WriteLine("- RuntimeVerified: false");
                writer.WriteLine("- Notes: Scenario command scaffold created in implementation pass 01. Manual/GM runtime execution details not yet automated.");

                if (companion != null)
                {
                    AIGMCompanionExecutionState state = AIGMCompanionExecutionSpine.GetState(companion);
                    writer.WriteLine("- CompanionState: " + (state != null ? state.Phase.ToString() : "none"));
                    writer.WriteLine("- LastTrace: " + (state != null ? state.LastTrace : "none"));
                }
            }

            e.Mobile.SendMessage("Scenario proof scaffold written: " + reportPath);
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
