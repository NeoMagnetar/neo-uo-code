using System;
using Server.Custom.AIGM;
using Server.Mobiles;

namespace Server.Commands
{
    public static class AIGMCompanionCommand
    {
        private static readonly string[] CompanionNames =
        {
            "Dakeyras",
            "Danyal",
            "Dardalion"
        };

        public static void Initialize()
        {
            CommandSystem.Register("AIGMCompanion", AccessLevel.GameMaster, new CommandEventHandler(OnAIGMCompanionCommand));
            CommandSystem.Register("AIGMCompanionStatus", AccessLevel.GameMaster, new CommandEventHandler(OnAIGMCompanionCommand));
            CommandSystem.Register("AIGMCompanionRoute", AccessLevel.GameMaster, new CommandEventHandler(OnAIGMCompanionRouteCommand));
        }

        [Usage("AIGMCompanion")]
        [Description("Reports the current companion-lane shell status. Live movement is deferred.")]
        private static void OnAIGMCompanionCommand(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            e.Mobile.SendMessage("Companion lane shell is present.");
            e.Mobile.SendMessage("Companions: {0}.", String.Join(", ", CompanionNames));
            e.Mobile.SendMessage("Movement is disabled in this phase.");
            e.Mobile.SendMessage("Deferred commands: follow, come, guard, stay.");
        }

        [Usage("AIGMCompanionRoute <speech>")]
        [Description("Classifies speech for companion-lane routing without executing commands.")]
        private static void OnAIGMCompanionRouteCommand(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            string speech = e.ArgString ?? String.Empty;
            AIGMCompanionCommandRouteDecision decision = AIGMCompanionCommandBoundary.Classify(speech);

            e.Mobile.SendMessage("Route kind: {0}", decision.RouteKind);
            e.Mobile.SendMessage("Companion key: {0}", String.IsNullOrWhiteSpace(decision.CompanionKey) ? "(none)" : decision.CompanionKey);
            e.Mobile.SendMessage("Companion name: {0}", String.IsNullOrWhiteSpace(decision.CompanionName) ? "(none)" : decision.CompanionName);
            e.Mobile.SendMessage("Command verb: {0}", String.IsNullOrWhiteSpace(decision.CommandVerb) ? "(none)" : decision.CommandVerb);
            e.Mobile.SendMessage("Blocks counselor lane: {0}", decision.BlocksCounselorLane ? "yes" : "no");
            e.Mobile.SendMessage("Executable now: {0}", decision.IsExecutableNow ? "yes" : "no");
            e.Mobile.SendMessage("Reason: {0}", decision.Reason ?? "(none)");
            e.Mobile.SendMessage("Movement deferred: yes");
        }
    }
}
