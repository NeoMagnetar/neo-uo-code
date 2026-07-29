using System;
using System.Collections.Generic;
using Server.Custom.AIGM;
using Server.Mobiles;

namespace Server.Commands
{
    public static class AIGMLootCommand
    {
        public static void Initialize()
        {
            CommandSystem.Register("AIGMLoot", AccessLevel.GameMaster, OnCommand);
        }

        private static void OnCommand(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            string arg = e.ArgString != null ? e.ArgString.Trim() : String.Empty;
            string normalized = Normalize(arg);

            if (normalized.StartsWith("auto ", StringComparison.Ordinal) || normalized == "auto")
            {
                HandleAutoCommand(e.Mobile, arg.Substring(Math.Min(arg.Length, 4)).Trim());
                return;
            }

            if (normalized == "burden")
            {
                ForEachOwnedCompanion(e.Mobile, null, delegate(BaseHire hire)
                {
                    e.Mobile.SendMessage(68, "{0}: {1}", hire.Name, AIGMCompanionInventoryPolicy.BuildStatusLine(hire));
                });
                return;
            }

            if (normalized == "unload")
            {
                ForEachOwnedCompanion(e.Mobile, null, delegate(BaseHire hire)
                {
                    AIGMCompanionInventoryPolicyResult result = AIGMCompanionInventoryPolicy.RunBurdenManagement(hire, true);
                    e.Mobile.SendMessage(68, "{0}: dropped={1} weight={2:0.0} reason={3}", hire.Name, result.DroppedItems, result.DroppedWeight, result.Reason);
                });
                return;
            }

            if (normalized == "status")
            {
                ForEachOwnedCompanion(e.Mobile, null, delegate(BaseHire hire)
                {
                    e.Mobile.SendMessage(68, "{0}: {1}", hire.Name, AIGMCompanionLootService.GetLootStatus(hire));
                });
                return;
            }

            if (normalized == "stop")
            {
                ForEachOwnedCompanion(e.Mobile, null, delegate(BaseHire hire)
                {
                    string stop = AIGMCompanionLootService.StopLooting(hire);
                    string auto = AIGMCompanionAutoLootService.SetAutoLoot(hire, false, null);
                    e.Mobile.SendMessage(68, "{0}: {1} {2}", hire.Name, stop, auto);
                });
                return;
            }

            string companionFilter;
            string profileText;
            SplitArguments(arg, out companionFilter, out profileText);

            AIGMCompanionLootProfile profile = AIGMCompanionLootProfile.FromText(profileText);
            int handled = 0;
            ForEachOwnedCompanion(e.Mobile, companionFilter, delegate(BaseHire hire)
            {
                handled++;
                AIGMCompanionLootResult result = AIGMCompanionLootService.StartLootNearby(hire, e.Mobile, profile);
                e.Mobile.SendMessage(68, "{0}: profile={1} corpses={2}/{3} items={4} gold={5} result={6}",
                    hire.Name,
                    result.Profile != null ? result.Profile.Name : "default",
                    result.CorpsesLooted,
                    result.CorpsesFound,
                    result.ItemsMoved,
                    result.GoldMoved,
                    result.SummaryReason);
            });

            if (handled == 0)
                e.Mobile.SendMessage(68, "No owned AIGM companion matched the loot command.");
        }

        private static void HandleAutoCommand(Mobile owner, string arg)
        {
            if (owner == null)
                return;

            string normalized = Normalize(arg);
            if (String.IsNullOrWhiteSpace(normalized))
                normalized = "status";

            string companionFilter = null;
            string profileText = normalized;
            SplitArguments(normalized, out companionFilter, out profileText);
            string action = Normalize(profileText);

            if (action.StartsWith("profile ", StringComparison.Ordinal))
                action = action.Substring(8).Trim();

            bool turnOn = action == "on" || action.StartsWith("on ", StringComparison.Ordinal) || IsProfileText(action);
            bool turnOff = action == "off" || action == "disable" || action == "stop";
            bool status = action == "status";

            AIGMCompanionLootProfile profile = AIGMCompanionLootProfile.FromText(action);
            int handled = 0;
            ForEachOwnedCompanion(owner, companionFilter, delegate(BaseHire hire)
            {
                handled++;
                if (status)
                {
                    owner.SendMessage(68, "{0}: {1}", hire.Name, AIGMCompanionAutoLootService.GetAutoLootStatus(hire));
                }
                else if (turnOff)
                {
                    owner.SendMessage(68, "{0}: {1}", hire.Name, AIGMCompanionAutoLootService.SetAutoLoot(hire, false, null));
                }
                else if (action.StartsWith("profile ", StringComparison.Ordinal) || IsProfileText(action))
                {
                    if (turnOn)
                        owner.SendMessage(68, "{0}: {1}", hire.Name, AIGMCompanionAutoLootService.SetAutoLoot(hire, true, profile));
                    else
                        owner.SendMessage(68, "{0}: {1}", hire.Name, AIGMCompanionAutoLootService.SetAutoLootProfile(hire, profile));
                }
                else if (turnOn)
                {
                    owner.SendMessage(68, "{0}: {1}", hire.Name, AIGMCompanionAutoLootService.SetAutoLoot(hire, true, profile));
                }
                else
                {
                    owner.SendMessage(68, "{0}: {1}", hire.Name, AIGMCompanionAutoLootService.GetAutoLootStatus(hire));
                }
            });

            if (handled == 0)
                owner.SendMessage(68, "No owned AIGM companion matched the auto-loot command.");
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

        private static void SplitArguments(string arg, out string companionFilter, out string profileText)
        {
            companionFilter = null;
            profileText = arg ?? String.Empty;

            string normalized = Normalize(arg);
            if (String.IsNullOrWhiteSpace(normalized))
            {
                profileText = "default";
                return;
            }

            string first = normalized;
            int space = normalized.IndexOf(' ');
            if (space >= 0)
                first = normalized.Substring(0, space);

            if (first == "dakeyras" || first == "dak" || first == "waylander" || first == "danyal" || first == "dan" || first == "dardalion" || first == "dar")
            {
                companionFilter = first;
                profileText = space >= 0 ? normalized.Substring(space + 1).Trim() : "default";
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

            if (normalized == "nearby")
                normalized = "default";

            return normalized.Trim();
        }

        private static bool IsProfileText(string text)
        {
            string normalized = Normalize(text);
            return normalized == "default"
                || normalized == "gold"
                || normalized == "supplies"
                || normalized == "supply"
                || normalized == "all"
                || normalized == "equipment";
        }
    }
}
