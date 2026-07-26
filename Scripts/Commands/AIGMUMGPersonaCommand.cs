using System;
using System.Collections.ObjectModel;
using Server.Commands;
using Server.Custom.AIGM.UMG;

namespace Server.Commands
{
    public static class AIGMUMGPersonaCommand
    {
        public static void Initialize()
        {
            CommandSystem.Register("AIGMUMGPersona", AccessLevel.GameMaster, OnCommand);
            CommandSystem.Register("AIGMPersonaBlocks", AccessLevel.GameMaster, OnCommand);
            CommandSystem.Register("AIGMUMGContext", AccessLevel.GameMaster, OnContextCommand);
            CommandSystem.Register("AIGMDialogueContext", AccessLevel.GameMaster, OnContextCommand);
        }

        private static void OnCommand(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            string arg = e.ArgString != null ? e.ArgString.Trim() : String.Empty;
            if (String.IsNullOrWhiteSpace(arg))
            {
                e.Mobile.SendMessage(68, "Usage: [AIGMUMGPersona Dakeyras|Danyal|Dardalion");
                e.Mobile.SendMessage(68, UMGPersonaBlockLibrary.BuildProofSummary());
                return;
            }

            ReadOnlyCollection<UMGPersonaBlock> blocks = UMGPersonaBlockLibrary.GetBlocksForCharacter(arg);
            if (blocks.Count == 0)
            {
                e.Mobile.SendMessage(68, "No UMG persona blocks found for '{0}'.", arg);
                return;
            }

            e.Mobile.SendMessage(68, "UMG persona blocks for {0}: {1}", arg, blocks.Count);
            for (int i = 0; i < blocks.Count; i++)
                e.Mobile.SendMessage(68, "{0} [{1}] {2}", blocks[i].Id, blocks[i].Category, Trim(blocks[i].Summary, 96));

            e.Mobile.SendMessage(68, "Relationships from {0}: {1}", arg, UMGPersonaBlockLibrary.GetRelationshipBlocks(arg, null).Count);
        }

        private static void OnContextCommand(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            string arg = e.ArgString != null ? e.ArgString.Trim() : String.Empty;
            if (String.IsNullOrWhiteSpace(arg))
            {
                e.Mobile.SendMessage(68, "Usage: [AIGMUMGContext Dakeyras|Danyal|Dardalion prompt text");
                return;
            }

            string character = arg;
            string prompt = String.Empty;
            int space = arg.IndexOf(' ');
            if (space > 0)
            {
                character = arg.Substring(0, space).Trim();
                prompt = arg.Substring(space + 1).Trim();
            }

            string summary = UMGPersonaBlockLibrary.BuildSelectedContextDebug(character, "owner_or_world_speech", null, ContainsAny(prompt, "companions", "discuss", "all of you"), String.Empty, prompt);
            e.Mobile.SendMessage(68, "UMG selected context: {0}", Trim(summary, 220));
        }

        private static string Trim(string value, int max)
        {
            if (String.IsNullOrWhiteSpace(value))
                return String.Empty;

            value = value.Trim();
            if (max > 0 && value.Length > max)
                value = value.Substring(0, max);

            return value;
        }

        private static bool ContainsAny(string value, params string[] needles)
        {
            if (String.IsNullOrWhiteSpace(value) || needles == null)
                return false;

            string text = value.ToLowerInvariant();
            for (int i = 0; i < needles.Length; i++)
            {
                if (!String.IsNullOrWhiteSpace(needles[i]) && text.Contains(needles[i].ToLowerInvariant()))
                    return true;
            }

            return false;
        }
    }
}
