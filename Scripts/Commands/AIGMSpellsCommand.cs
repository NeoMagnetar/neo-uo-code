using System;
using Server.Custom.AIGM;
using Server.Mobiles;

namespace Server.Commands
{
    public static class AIGMSpellsCommand
    {
        public static void Initialize()
        {
            CommandSystem.Register("AIGMSpells", AccessLevel.GameMaster, OnCommand);
        }

        private static void OnCommand(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            string companionFilter;
            string action;
            SplitArguments(e.ArgString, out companionFilter, out action);
            action = Normalize(action);
            if (String.IsNullOrWhiteSpace(action))
                action = "status";

            int handled = 0;
            ForEachOwnedCompanion(e.Mobile, companionFilter, delegate(BaseHire hire)
            {
                handled++;
                e.Mobile.SendMessage(68, "{0}: {1}", hire.Name, Handle(hire, action));
            });

            if (handled == 0)
                e.Mobile.SendMessage(68, "No owned AIGM companion matched the spell command.");
        }

        private static string Handle(BaseHire hire, string action)
        {
            if (action == "on" || action == "enable")
                return AIGMCompanionSpellService.SetSpellSupportEnabled(hire, true, AIGMCompanionSpellProfile.SupportOnly());
            if (action == "off" || action == "disable" || action == "stop")
                return AIGMCompanionSpellService.SetSpellSupportEnabled(hire, false, null);
            if (action == "status")
                return AIGMCompanionSpellService.GetSpellStatus(hire);
            if (action == "use" || action == "cast")
            {
                string result;
                AIGMCompanionSpellService.TryCastBestSupportSpell(hire, true, out result);
                return result;
            }
            if (action.StartsWith("profile ", StringComparison.Ordinal))
                return AIGMCompanionSpellService.SetSpellProfile(hire, AIGMCompanionSpellProfile.FromText(action.Substring(8).Trim()));

            return AIGMCompanionSpellService.GetSpellStatus(hire);
        }

        private static void ForEachOwnedCompanion(Mobile owner, string companionFilter, Action<BaseHire> action)
        {
            if (owner == null || action == null)
                return;

            string filter = Normalize(companionFilter);
            foreach (Mobile mobile in World.Mobiles.Values)
            {
                BaseHire hire = mobile as BaseHire;
                IAIGMCompanionActor actor = mobile as IAIGMCompanionActor;
                if (hire == null || actor == null || hire.Deleted || !hire.Alive || hire.GetOwner() != owner)
                    continue;
                if (!String.IsNullOrWhiteSpace(filter) && !MatchesCompanion(actor, hire, filter))
                    continue;
                action(hire);
            }
        }

        private static bool MatchesCompanion(IAIGMCompanionActor actor, BaseHire hire, string filter)
        {
            if (actor != null && Normalize(actor.CompanionId) == filter)
                return true;
            if (hire != null && Normalize(hire.Name) == filter)
                return true;
            return filter == "dak" && actor != null && actor.CompanionId == "dakeyras"
                || filter == "waylander" && actor != null && actor.CompanionId == "dakeyras"
                || filter == "dan" && actor != null && actor.CompanionId == "danyal"
                || filter == "dar" && actor != null && actor.CompanionId == "dardalion";
        }

        private static void SplitArguments(string arg, out string companionFilter, out string action)
        {
            companionFilter = null;
            action = arg ?? String.Empty;
            string normalized = Normalize(arg);
            if (String.IsNullOrWhiteSpace(normalized))
            {
                action = "status";
                return;
            }

            string first = normalized;
            int space = normalized.IndexOf(' ');
            if (space >= 0)
                first = normalized.Substring(0, space);

            if (first == "dakeyras" || first == "dak" || first == "waylander" || first == "danyal" || first == "dan" || first == "dardalion" || first == "dar")
            {
                companionFilter = first;
                action = space >= 0 ? normalized.Substring(space + 1).Trim() : "status";
            }
        }

        private static string Normalize(string text)
        {
            if (String.IsNullOrWhiteSpace(text))
                return String.Empty;
            string normalized = text.Trim().ToLowerInvariant();
            normalized = normalized.Replace(",", " ").Replace(".", " ").Replace("!", " ").Replace("?", " ").Replace(";", " ").Replace(":", " ");
            while (normalized.Contains("  "))
                normalized = normalized.Replace("  ", " ");
            return normalized.Trim();
        }
    }
}
