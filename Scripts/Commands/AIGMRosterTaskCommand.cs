using System;

using Server.Custom.AIGM.Tasks;

namespace Server.Commands
{
    public static class AIGMRosterTaskCommand
    {
        public static void Initialize()
        {
            CommandSystem.Register("AIGMTask", AccessLevel.GameMaster, OnTask);
            CommandSystem.Register("AIGMTaskStop", AccessLevel.GameMaster, OnTaskStop);
            CommandSystem.Register("AIGMTaskStatus", AccessLevel.GameMaster, OnTaskStatus);
            CommandSystem.Register("AIGMTaskClear", AccessLevel.GameMaster, OnTaskClear);
            CommandSystem.Register("AIGMTaskReturnHome", AccessLevel.GameMaster, OnTaskReturnHome);
            CommandSystem.Register("AIGMRosterCapacity", AccessLevel.GameMaster, OnCapacity);
            CommandSystem.Register("AIGMBindRoster", AccessLevel.GameMaster, OnBind);
            CommandSystem.Register("AIGMUnbindRoster", AccessLevel.GameMaster, OnUnbind);
            CommandSystem.Register("AIGMStandDown", AccessLevel.GameMaster, OnStandDown);
            CommandSystem.Register("AIGMHold", AccessLevel.GameMaster, OnHold);
            CommandSystem.Register("AIGMResume", AccessLevel.GameMaster, OnResume);
        }

        private static void OnTask(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            string[] tokens = SplitArgs(e.ArgString);
            if (tokens.Length < 2)
            {
                e.Mobile.SendMessage("Usage: [AIGMTask <address> <command...>");
                return;
            }

            string address = NormalizeAddress(tokens[0]);
            string commandText = Join(tokens, 1);
            if (AIGMRosterCommandService.TryHandleCommandText(e.Mobile, address, commandText, out string response))
                e.Mobile.SendMessage(response);
            else
                e.Mobile.SendMessage("No roster task action was applied.");
        }

        private static void OnTaskStop(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            string[] tokens = SplitArgs(e.ArgString);
            if (tokens.Length == 0)
            {
                e.Mobile.SendMessage("Usage: [AIGMTaskStop <address>");
                return;
            }

            if (AIGMRosterCommandService.TryHandleCommandText(e.Mobile, NormalizeAddress(tokens[0]), "stop hunting", out string response))
                e.Mobile.SendMessage(response);
        }

        private static void OnTaskStatus(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            string[] tokens = SplitArgs(e.ArgString);
            if (tokens.Length == 0)
            {
                e.Mobile.SendMessage("Usage: [AIGMTaskStatus <address>");
                return;
            }

            if (AIGMRosterCommandService.TryHandleCommandText(e.Mobile, NormalizeAddress(tokens[0]), "status", out string response))
                e.Mobile.SendMessage(response);
        }

        private static void OnTaskClear(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            string[] tokens = SplitArgs(e.ArgString);
            if (tokens.Length == 0)
            {
                e.Mobile.SendMessage("Usage: [AIGMTaskClear <address>");
                return;
            }

            if (AIGMRosterCommandService.TryHandleCommandText(e.Mobile, NormalizeAddress(tokens[0]), "clear task", out string response))
                e.Mobile.SendMessage(response);
        }

        private static void OnTaskReturnHome(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            string[] tokens = SplitArgs(e.ArgString);
            if (tokens.Length == 0)
            {
                e.Mobile.SendMessage("Usage: [AIGMTaskReturnHome <address>");
                return;
            }

            if (AIGMRosterCommandService.TryHandleCommandText(e.Mobile, NormalizeAddress(tokens[0]), "return home", out string response))
                e.Mobile.SendMessage(response);
        }

        private static void OnCapacity(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            string[] tokens = SplitArgs(e.ArgString);
            if (tokens.Length > 0 && Int32.TryParse(tokens[0], out int value) && value > 0)
            {
                AIGMRosterCompanionCapacityService.MaxRosterCompanions = value;
                e.Mobile.SendMessage("AIGM roster companion capacity set to {0}.", value);
                e.Mobile.SendMessage(AIGMRosterCompanionBindingService.BuildCapacityStatus(e.Mobile));
                return;
            }

            e.Mobile.SendMessage(AIGMRosterCompanionBindingService.BuildCapacityStatus(e.Mobile));
        }

        private static void OnBind(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            string[] tokens = SplitArgs(e.ArgString);
            if (tokens.Length == 0)
            {
                e.Mobile.SendMessage("Usage: [AIGMBindRoster <address>");
                return;
            }

            if (AIGMRosterCommandService.TryHandleCommandText(e.Mobile, NormalizeAddress(tokens[0]), "bind", out string response))
                e.Mobile.SendMessage(response);
        }

        private static void OnUnbind(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            string[] tokens = SplitArgs(e.ArgString);
            if (tokens.Length == 0)
            {
                e.Mobile.SendMessage("Usage: [AIGMUnbindRoster <address>");
                return;
            }

            if (AIGMRosterCommandService.TryHandleCommandText(e.Mobile, NormalizeAddress(tokens[0]), "unbind", out string response))
                e.Mobile.SendMessage(response);
        }

        private static void OnStandDown(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            string[] tokens = SplitArgs(e.ArgString);
            if (tokens.Length == 0)
            {
                e.Mobile.SendMessage("Usage: [AIGMStandDown <address>");
                return;
            }

            if (AIGMRosterCommandService.TryHandleCommandText(e.Mobile, NormalizeAddress(tokens[0]), "stand down", out string response))
                e.Mobile.SendMessage(response);
            else
                e.Mobile.SendMessage("No roster stand-down action was applied.");
        }

        private static void OnHold(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            string[] tokens = SplitArgs(e.ArgString);
            if (tokens.Length == 0)
            {
                e.Mobile.SendMessage("Usage: [AIGMHold <address>");
                return;
            }

            if (AIGMRosterCommandService.TryHandleCommandText(e.Mobile, NormalizeAddress(tokens[0]), "hold", out string response))
                e.Mobile.SendMessage(response);
            else
                e.Mobile.SendMessage("No roster hold action was applied.");
        }

        private static void OnResume(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            string[] tokens = SplitArgs(e.ArgString);
            if (tokens.Length == 0)
            {
                e.Mobile.SendMessage("Usage: [AIGMResume <address>");
                return;
            }

            if (AIGMRosterCommandService.TryHandleCommandText(e.Mobile, NormalizeAddress(tokens[0]), "resume", out string response))
                e.Mobile.SendMessage(response);
            else
                e.Mobile.SendMessage("No roster resume action was applied.");
        }

        private static string[] SplitArgs(string args)
        {
            return String.IsNullOrWhiteSpace(args)
                ? Array.Empty<string>()
                : args.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static string Join(string[] tokens, int startIndex)
        {
            if (tokens == null || startIndex >= tokens.Length)
                return String.Empty;

            return String.Join(" ", tokens, startIndex, tokens.Length - startIndex);
        }

        private static string NormalizeAddress(string value)
        {
            string normalized = (value ?? String.Empty).Trim().ToLowerInvariant();
            switch (normalized)
            {
                case "darkbrotherhood":
                    return "dark brotherhood";
                case "thethirty":
                    return "the thirty";
                case "dukeofkydor":
                    return "duke of kydor";
                case "sathulilord":
                    return "sathuli lord";
                case "yuyuliang":
                    return "yu yu liang";
                case "tenakakhan":
                    return "tenaka khan";
                case "kesakhan":
                    return "kesa khan";
                case "ansichen":
                    return "ansi chen";
                default:
                    return normalized;
            }
        }
    }
}
