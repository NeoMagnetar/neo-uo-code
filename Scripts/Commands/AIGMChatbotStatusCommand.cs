using System;
using Server.Custom.AIGM;

namespace Server.Commands
{
    public static class AIGMChatbotStatusCommand
    {
        public static void Initialize()
        {
            CommandSystem.Register("AIGMChatbotStatus", AccessLevel.GameMaster, OnCommand);
            CommandSystem.Register("AIGMDialogueBridgeStatus", AccessLevel.GameMaster, OnCommand);
            CommandSystem.Register("AIGMChatbotHealth", AccessLevel.GameMaster, OnHealthCommand);
            CommandSystem.Register("AIGMChatbotPing", AccessLevel.GameMaster, OnHealthCommand);
        }

        private static void OnCommand(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            e.Mobile.SendMessage(68, "AIGM chatbot bridge: {0}", AIGMBridgeClient.GetChatbotStatusSummary());
        }

        private static void OnHealthCommand(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            e.Mobile.SendMessage(68, "AIGM chatbot health: {0}", AIGMBridgeClient.GetChatbotHealthProbeSummary());
        }
    }
}
