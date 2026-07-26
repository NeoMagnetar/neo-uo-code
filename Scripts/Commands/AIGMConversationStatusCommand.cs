using Server.Custom.AIGM;

namespace Server.Commands
{
    public static class AIGMConversationStatusCommand
    {
        public static void Initialize()
        {
            CommandSystem.Register("AIGMConversationStatus", AccessLevel.GameMaster, OnCommand);
            CommandSystem.Register("AIGMDialogueThreadStatus", AccessLevel.GameMaster, OnCommand);
        }

        private static void OnCommand(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            e.Mobile.SendMessage(68, "AIGM conversation: {0}", AIGMCompanionDialogueThreadService.BuildStatusSummary(e.Mobile));
        }
    }
}
