using System;

using Server.Custom.AIGM;
using Server.Custom.AIGM.Tasks;

namespace Server.Commands
{
    public static class AIGMUnifiedCommandEntry
    {
        public static void Initialize()
        {
            CommandSystem.Register("aicommands", AccessLevel.GameMaster, OnCommand);
            CommandSystem.Register("aifollow", AccessLevel.GameMaster, OnCommand);
            CommandSystem.Register("stay", AccessLevel.GameMaster, OnCommand);
            CommandSystem.Register("stop", AccessLevel.GameMaster, OnCommand);
            CommandSystem.Register("standdown", AccessLevel.GameMaster, OnCommand);
            CommandSystem.Register("hold", AccessLevel.GameMaster, OnCommand);
            CommandSystem.Register("resume", AccessLevel.GameMaster, OnCommand);
            CommandSystem.Register("track", AccessLevel.GameMaster, OnCommand);
            CommandSystem.Register("hunt", AccessLevel.GameMaster, OnCommand);
            CommandSystem.Register("guard", AccessLevel.GameMaster, OnCommand);
            CommandSystem.Register("travel", AccessLevel.GameMaster, OnCommand);
            CommandSystem.Register("returnhome", AccessLevel.GameMaster, OnCommand);
            CommandSystem.Register("status", AccessLevel.GameMaster, OnCommand);
            CommandSystem.Register("bind", AccessLevel.GameMaster, OnCommand);
            CommandSystem.Register("unbind", AccessLevel.GameMaster, OnCommand);
            CommandSystem.Register("capacity", AccessLevel.GameMaster, OnCommand);
        }

        private static void OnCommand(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            string verb = e.Command;
            AIGMUnifiedCommand command;
            string error;
            if (!AIGMUnifiedCommandParser.TryParse(verb, e.ArgString, out command, out error))
            {
                e.Mobile.SendMessage(error ?? "Invalid AIGM command.");
                return;
            }

            if (command.Help)
            {
                SendHelp(e.Mobile);
                return;
            }

            if (command.Capacity)
            {
                if (command.CapacityValue > 0)
                {
                    AIGMRosterCompanionCapacityService.MaxRosterCompanions = command.CapacityValue;
                    e.Mobile.SendMessage("AIGM roster companion capacity set to {0}.", command.CapacityValue);
                    e.Mobile.SendMessage(AIGMRosterCompanionBindingService.BuildCapacityStatus(e.Mobile));
                }
                else
                {
                    e.Mobile.SendMessage(AIGMRosterCompanionBindingService.BuildCapacityStatus(e.Mobile));
                }

                return;
            }

            string commandText = AIGMUnifiedCommandParser.ToRosterCommandText(command);
            if (String.IsNullOrWhiteSpace(commandText))
            {
                e.Mobile.SendMessage("Unsupported AIGM command.");
                return;
            }

            string response;
            if (AIGMRosterCommandService.TryHandleCommandText(e.Mobile, command.Actor, commandText, out response))
                e.Mobile.SendMessage(response);
            else
                e.Mobile.SendMessage("No AIGM command action was applied.");
        }

        private static void SendHelp(Mobile mobile)
        {
            mobile.SendMessage("CONTROL: [aifollow druss] [stay druss] [stop druss] [standdown druss] [hold druss] [resume druss]");
            mobile.SendMessage("MISSIONS: [track cadoras waylander] [hunt druss monsters] [hunt heroes joinings] [guard druss danyal] [travel heroes yew] [returnhome cadoras]");
            mobile.SendMessage("MANAGEMENT: [status druss] [bind druss] [unbind druss] [capacity] [capacity 20]");
            mobile.SendMessage("SPEECH: Druss, join me | Druss, follow me | Heroes, hunt Joinings | Everyone, go to Yew | Cadoras, return home");
        }
    }
}
