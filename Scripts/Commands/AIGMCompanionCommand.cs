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
    }
}
