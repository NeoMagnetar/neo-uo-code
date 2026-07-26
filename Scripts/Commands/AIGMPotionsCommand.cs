using System;
using Server.Custom.AIGM;
using Server.Mobiles;

namespace Server.Commands
{
    public static class AIGMPotionsCommand
    {
        public static void Initialize()
        {
            CommandSystem.Register("AIGMPotions", AccessLevel.GameMaster, OnCommand);
        }

        private static void OnCommand(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            string arg = e.ArgString != null ? e.ArgString.Trim() : String.Empty;
            string companionFilter;
            string actionText;
            SplitArguments(arg, out companionFilter, out actionText);

            string action = Normalize(actionText);
            if (String.IsNullOrWhiteSpace(action))
                action = "status";

            int handled = 0;
            ForEachOwnedCompanion(e.Mobile, companionFilter, delegate(BaseHire hire)
            {
                handled++;
                string response = HandleForCompanion(hire, action);
                e.Mobile.SendMessage(68, "{0}: {1}", hire.Name, response);
            });

            if (handled == 0)
                e.Mobile.SendMessage(68, "No owned AIGM companion matched the potion command.");
        }

        private static string HandleForCompanion(BaseHire hire, string action)
        {
            if (hire == null)
                return "Potion support is not available right now.";

            if (action == "on" || action == "enable")
                return AIGMCompanionPotionService.SetPotionSupportEnabled(hire, true);
            if (action == "off" || action == "disable" || action == "stop")
                return AIGMCompanionPotionService.SetPotionSupportEnabled(hire, false);
            if (action == "status")
                return AIGMCompanionPotionService.GetPotionStatus(hire);
            if (action == "use" || action == "drink")
            {
                string result;
                AIGMCompanionPotionService.TryUseBestPotion(hire, true, out result);
                return result;
            }

            if (action.StartsWith("combat ", StringComparison.Ordinal))
                return AIGMCompanionPotionService.SetPotionCategory(hire, "combat", action.EndsWith(" on", StringComparison.Ordinal));
            if (action.StartsWith("buff ", StringComparison.Ordinal))
                return AIGMCompanionPotionService.SetPotionCategory(hire, "buff", action.EndsWith(" on", StringComparison.Ordinal));
            if (action.StartsWith("heal ", StringComparison.Ordinal))
                return AIGMCompanionPotionService.SetPotionCategory(hire, "heal", action.EndsWith(" on", StringComparison.Ordinal));
            if (action.StartsWith("cure ", StringComparison.Ordinal))
                return AIGMCompanionPotionService.SetPotionCategory(hire, "cure", action.EndsWith(" on", StringComparison.Ordinal));
            if (action.StartsWith("refresh ", StringComparison.Ordinal))
                return AIGMCompanionPotionService.SetPotionCategory(hire, "refresh", action.EndsWith(" on", StringComparison.Ordinal));

            return AIGMCompanionPotionService.GetPotionStatus(hire);
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
                if (hire == null || actor == null || hire.Deleted || !hire.Alive)
                    continue;

                if (hire.GetOwner() != owner)
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

        private static void SplitArguments(string arg, out string companionFilter, out string actionText)
        {
            companionFilter = null;
            actionText = arg ?? String.Empty;

            string normalized = Normalize(arg);
            if (String.IsNullOrWhiteSpace(normalized))
            {
                actionText = "status";
                return;
            }

            string first = normalized;
            int space = normalized.IndexOf(' ');
            if (space >= 0)
                first = normalized.Substring(0, space);

            if (first == "dakeyras" || first == "dak" || first == "waylander" || first == "danyal" || first == "dan" || first == "dardalion" || first == "dar")
            {
                companionFilter = first;
                actionText = space >= 0 ? normalized.Substring(space + 1).Trim() : "status";
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
