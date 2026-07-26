using System;

using Server.Custom.AIGM;
using Server.Mobiles;

namespace Server.Commands
{
    public static class AIGMStopAllCommand
    {
        public static void Initialize()
        {
            CommandSystem.Register("AIGMStopAll", AccessLevel.GameMaster, OnCommand);
            CommandSystem.Register("AIGMStopCompanions", AccessLevel.GameMaster, OnCommand);
            CommandSystem.Register("AIGMStop", AccessLevel.GameMaster, OnCommand);
            CommandSystem.Register("AIGMTrackingHelp", AccessLevel.GameMaster, OnTrackingHelp);
            CommandSystem.Register("AIGMTrackStatus", AccessLevel.GameMaster, OnTrackingStatus);
            CommandSystem.Register("AIGMTrackingStatus", AccessLevel.GameMaster, OnTrackingStatus);
        }

        [Usage("AIGMStopAll [movement|tracking|status]")]
        [Description("Stops AIGM companion movement, tracking, hunting, navigation, and regroup activity without changing support settings.")]
        private static void OnCommand(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            string arg = e.ArgString != null ? e.ArgString.Trim().ToLowerInvariant() : String.Empty;
            if (arg == "status")
            {
                e.Mobile.SendMessage(68, AIGMCompanionControlStopService.FormatStatus(e.Mobile));
                return;
            }

            AIGMCompanionStopScope scope = AIGMCompanionStopScope.AllAIGMActivity;
            if (arg == "movement")
                scope = AIGMCompanionStopScope.Movement | AIGMCompanionStopScope.Navigation | AIGMCompanionStopScope.Regroup;
            else if (arg == "tracking")
                scope = AIGMCompanionStopScope.Tracking | AIGMCompanionStopScope.Hunt | AIGMCompanionStopScope.Regroup | AIGMCompanionStopScope.Movement | AIGMCompanionStopScope.Navigation;

            int count = AIGMCompanionControlStopService.StopAllCompanionActivity(e.Mobile, scope, "command:" + (String.IsNullOrWhiteSpace(arg) ? "all" : arg));
            if ((scope & AIGMCompanionStopScope.Conversation) != 0)
                AIGMCompanionDialogueThreadService.StopForOwner(e.Mobile, "stop_all_command");

            e.Mobile.SendMessage(68, "AIGMStopAll: stopped {0} companion(s), scope={1}. Support settings unchanged.", count, scope);
        }

        private static void OnTrackingHelp(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            e.Mobile.SendMessage(68, AIGMCompanionControlStopService.BuildTrackingHelp());
        }

        private static void OnTrackingStatus(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            BaseHire companion = ResolvePreferredCompanion(e.Mobile, 12);
            if (companion == null)
            {
                e.Mobile.SendMessage(68, "No nearby AIGM companion found.");
                return;
            }

            e.Mobile.SendMessage(68, AIGMCompanionTrackingService.GetTrackingStatus(companion, e.Mobile));
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
