using System;

using Server.Custom.AIGM;
using Server.Mobiles;

namespace Server.Commands
{
    public static class AIGMFindCommand
    {
        public static void Initialize()
        {
            CommandSystem.Register("AIGMFind", AccessLevel.GameMaster, OnFind);
            CommandSystem.Register("AIGMLocate", AccessLevel.GameMaster, OnLocate);
            CommandSystem.Register("AIGMTrackMobile", AccessLevel.GameMaster, OnFind);
            CommandSystem.Register("AIGMRegroup", AccessLevel.GameMaster, OnRegroup);
        }

        [Usage("AIGMFind [companionName] <targetName>")]
        [Description("Moves one or all AIGM companions to a named mobile without attacking.")]
        private static void OnFind(CommandEventArgs e)
        {
            HandleFind(e, false);
        }

        [Usage("AIGMLocate <targetName>")]
        [Description("Moves all AIGM companions to a named mobile without attacking.")]
        private static void OnLocate(CommandEventArgs e)
        {
            HandleFind(e, true);
        }

        [Usage("AIGMRegroup [me|targetName]")]
        [Description("Regroups owned AIGM companions on the owner or a named anchor.")]
        private static void OnRegroup(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            string arg = e.ArgString != null ? e.ArgString.Trim() : String.Empty;
            if (String.IsNullOrWhiteSpace(arg))
                arg = "me";

            string response = AIGMCompanionLocateService.StartGroupLocate(e.Mobile, arg, true);
            e.Mobile.SendMessage(68, response);
        }

        private static void HandleFind(CommandEventArgs e, bool forceGroup)
        {
            if (e == null || e.Mobile == null)
                return;

            string arg = e.ArgString != null ? e.ArgString.Trim() : String.Empty;
            if (String.IsNullOrWhiteSpace(arg) || arg.Equals("help", StringComparison.OrdinalIgnoreCase))
            {
                e.Mobile.SendMessage(68, "Usage: [AIGMFind [Dakeyras|Danyal|Dardalion] <targetName> | [AIGMRegroup [me|targetName]");
                return;
            }

            string first;
            string rest;
            SplitFirst(arg, out first, out rest);

            if (!forceGroup && IsCompanionToken(first) && !String.IsNullOrWhiteSpace(rest))
            {
                BaseHire actor = ResolveCompanion(e.Mobile, first);
                if (actor == null)
                {
                    e.Mobile.SendMessage(68, "No owned AIGM companion matched '{0}'.", first);
                    return;
                }

                e.Mobile.SendMessage(68, AIGMCompanionLocateService.StartLocateByName(actor, e.Mobile, rest, AIGMCompanionLocateKind.Locate));
                return;
            }

            e.Mobile.SendMessage(68, AIGMCompanionLocateService.StartGroupLocate(e.Mobile, arg, false));
        }

        private static BaseHire ResolveCompanion(Mobile owner, string token)
        {
            if (owner == null || String.IsNullOrWhiteSpace(token))
                return null;

            string needle = token.Trim().ToLowerInvariant();
            if (needle == "dak" || needle == "waylander")
                needle = "dakeyras";
            else if (needle == "dan")
                needle = "danyal";
            else if (needle == "dar" || needle == "dard")
                needle = "dardalion";

            foreach (BaseHire hire in AIGMCompanionControlStopService.GetOwnedCompanions(owner, 0))
            {
                IAIGMCompanionActor actor = hire as IAIGMCompanionActor;
                string id = actor != null && actor.CompanionId != null ? actor.CompanionId.ToLowerInvariant() : String.Empty;
                string name = hire.Name != null ? hire.Name.ToLowerInvariant() : String.Empty;
                if (id == needle || name == needle)
                    return hire;
            }

            return null;
        }

        private static void SplitFirst(string text, out string first, out string rest)
        {
            first = text;
            rest = String.Empty;
            if (String.IsNullOrWhiteSpace(text))
                return;

            int space = text.Trim().IndexOf(' ');
            if (space < 0)
                return;

            first = text.Substring(0, space).Trim();
            rest = text.Substring(space + 1).Trim();
        }

        private static bool IsCompanionToken(string token)
        {
            if (String.IsNullOrWhiteSpace(token))
                return false;

            string value = token.Trim().ToLowerInvariant();
            return value == "dakeyras" || value == "dak" || value == "waylander" || value == "danyal" || value == "dan" || value == "dardalion" || value == "dard" || value == "dar";
        }
    }
}
