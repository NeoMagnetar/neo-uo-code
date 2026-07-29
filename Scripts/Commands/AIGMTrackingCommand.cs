using System;
using Server.Commands;
using Server.Custom.AIGM;
using Server.Mobiles;

namespace Server.Commands
{
    public static class AIGMTrackingCommand
    {
        public static void Initialize()
        {
            CommandSystem.Register("tm", AccessLevel.GameMaster, e => RunTracking(e, AIGMCompanionTrackingMode.Monsters));
            CommandSystem.Register("ta", AccessLevel.GameMaster, e => RunTracking(e, AIGMCompanionTrackingMode.Animals));
            CommandSystem.Register("tn", AccessLevel.GameMaster, e => RunTracking(e, AIGMCompanionTrackingMode.NPCs));
            CommandSystem.Register("th", AccessLevel.GameMaster, e => RunTracking(e, AIGMCompanionTrackingMode.HumanNPCs));
            CommandSystem.Register("tp", AccessLevel.GameMaster, e => RunTracking(e, AIGMCompanionTrackingMode.Players));
            CommandSystem.Register("tall", AccessLevel.GameMaster, e => RunTracking(e, AIGMCompanionTrackingMode.All));
            CommandSystem.Register("hm", AccessLevel.GameMaster, OnHuntMonsters);
            CommandSystem.Register("ha", AccessLevel.GameMaster, OnHuntAnimals);
            CommandSystem.Register("stoptrack", AccessLevel.GameMaster, OnStopTrack);
            CommandSystem.Register("ts", AccessLevel.GameMaster, OnTrackingStatus);
            CommandSystem.Register("td", AccessLevel.GameMaster, OnTrackingDump);
        }

        private static void RunTracking(CommandEventArgs e, AIGMCompanionTrackingMode mode)
        {
            if (e == null || e.Mobile == null)
                return;

            BaseHire companion = ResolvePreferredCompanion(e.Mobile, 12);
            if (companion == null)
            {
                e.Mobile.SendMessage("No nearby AIGM companion found.");
                return;
            }

            e.Mobile.SendMessage(AIGMCompanionTrackingService.BuildTrackingSweepReport(companion, e.Mobile, mode));
        }

        private static void OnHuntMonsters(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            BaseHire companion = ResolvePreferredCompanion(e.Mobile, 12);
            if (companion == null)
            {
                e.Mobile.SendMessage("No nearby AIGM companion found.");
                return;
            }

            e.Mobile.SendMessage(AIGMCompanionExecutionSpine.StartMonsterHunt(companion, e.Mobile));
        }

        private static void OnHuntAnimals(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            e.Mobile.SendMessage("Animal hunting is not enabled in this lane yet.");
        }

        private static void OnStopTrack(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            BaseHire companion = ResolvePreferredCompanion(e.Mobile, 12);
            if (companion == null)
            {
                e.Mobile.SendMessage("No nearby AIGM companion found.");
                return;
            }

            string trackStop = AIGMCompanionTrackingService.StopTracking(companion, e.Mobile);
            string huntStop = AIGMCompanionExecutionSpine.StopMonsterHunt(companion, "stoptrack_command");
            e.Mobile.SendMessage(trackStop);
            e.Mobile.SendMessage(huntStop);
        }

        private static void OnTrackingStatus(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            BaseHire companion = ResolvePreferredCompanion(e.Mobile, 12);
            if (companion == null)
            {
                e.Mobile.SendMessage("No nearby AIGM companion found.");
                return;
            }

            e.Mobile.SendMessage(AIGMCompanionTrackingService.GetTrackingStatus(companion, e.Mobile));
        }

        private static void OnTrackingDump(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            BaseHire companion = ResolvePreferredCompanion(e.Mobile, 12);
            if (companion == null)
            {
                e.Mobile.SendMessage("No nearby AIGM companion found.");
                return;
            }

            AIGMCompanionTrackingState state = AIGMCompanionTrackingService.GetState(companion);
            if (state == null)
            {
                e.Mobile.SendMessage("No tracking state found.");
                return;
            }

            e.Mobile.SendMessage(String.Format("Companion={0}; Active={1}; Mode={2}; Target={3}; Direction={4}; Distance={5}; Tile={6}; Accepted={7}; Rejected={8}; Last={9}",
                companion.Name,
                state.IsActive,
                state.Mode,
                String.IsNullOrWhiteSpace(state.LastKnownTargetDescription) ? "none" : state.LastKnownTargetDescription,
                String.IsNullOrWhiteSpace(state.LastKnownDirectionText) ? "none" : state.LastKnownDirectionText,
                String.IsNullOrWhiteSpace(state.LastKnownDistanceText) ? "none" : state.LastKnownDistanceText,
                String.IsNullOrWhiteSpace(state.LastKnownTileText) ? "none" : state.LastKnownTileText,
                String.IsNullOrWhiteSpace(state.LastCandidateSummary) ? "none" : state.LastCandidateSummary,
                String.IsNullOrWhiteSpace(state.LastRejectedCandidates) ? "none" : state.LastRejectedCandidates,
                String.IsNullOrWhiteSpace(state.LastReport) ? "none" : state.LastReport));
        }

        private static BaseHire ResolvePreferredCompanion(Mobile from, int range)
        {
            if (from == null || from.Map == null)
                return null;

            BaseHire dak = null;
            BaseHire dard = null;
            BaseHire danyal = null;
            BaseHire best = null;
            int bestDistance = Int32.MaxValue;

            IPooledEnumerable mobiles = from.Map.GetMobilesInRange(from.Location, range);
            foreach (Mobile mobile in mobiles)
            {
                BaseHire hire = mobile as BaseHire;
                IAIGMCompanionActor actor = mobile as IAIGMCompanionActor;
                if (hire == null || actor == null || mobile.Deleted || !mobile.Alive)
                    continue;

                if (String.Equals(hire.Name, "Dakeyras", StringComparison.OrdinalIgnoreCase))
                    dak = hire;
                else if (String.Equals(hire.Name, "Dardalion", StringComparison.OrdinalIgnoreCase))
                    dard = hire;
                else if (String.Equals(hire.Name, "Danyal", StringComparison.OrdinalIgnoreCase))
                    danyal = hire;

                int distance = (int)from.GetDistanceToSqrt(mobile);
                if (distance < bestDistance)
                {
                    best = hire;
                    bestDistance = distance;
                }
            }
            mobiles.Free();

            return dak ?? dard ?? danyal ?? best;
        }
    }
}
