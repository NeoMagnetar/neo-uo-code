using System;
using Server.Custom.AIGM;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Commands
{
    public static class AIGMProofCommand
    {
        public static void Initialize()
        {
            CommandSystem.Register("AIGMProofBag", AccessLevel.GameMaster, new CommandEventHandler(AIGMProofBag_OnCommand));
        }

        [Usage("AIGMProofBag <itemName> [amount]")]
        [Description("Creates an item through the AIGM Add adapter directly into your own backpack.")]
        private static void AIGMProofBag_OnCommand(CommandEventArgs e)
        {
            if (e.Mobile == null)
                return;

            if (e.Mobile.Backpack == null)
            {
                e.Mobile.SendMessage("You do not have a backpack available.");
                return;
            }

            if (e.Arguments == null || e.Arguments.Length <= 0)
            {
                e.Mobile.SendMessage("Usage: AIGMProofBag <itemName> [amount]");
                return;
            }

            string itemName = e.GetString(0);
            int amount = 1;

            if (e.Length > 1)
            {
                int parsed;
                if (Int32.TryParse(e.GetString(1), out parsed) && parsed > 0)
                    amount = parsed;
            }

            string[] ctorArgs = BuildArgs(itemName, amount);
            Item item;
            string message;

            if (AIGMAddAdapter.TryCreateItemInContainer(e.Mobile, e.Mobile.Backpack, itemName, ctorArgs, null, out item, out message))
            {
                e.Mobile.SendMessage("AIGM proof success: {0}", message ?? "item created");
                return;
            }

            e.Mobile.SendMessage("AIGM proof failed: {0}", message ?? "unknown failure");
        }

        private static string[] BuildArgs(string itemName, int amount)
        {
            string alias = Normalize(itemName);
            switch (alias)
            {
                case "bandage":
                case "bandages":
                case "blank scroll":
                case "blank scrolls":
                case "gold":
                case "black pearl":
                case "bloodmoss":
                case "garlic":
                case "ginseng":
                case "mandrake root":
                case "nightshade":
                case "sulfurous ash":
                case "spider silk":
                case "spiders silk":
                    return new string[] { Math.Max(1, amount).ToString() };
                default:
                    return new string[0];
            }
        }

        private static string Normalize(string raw)
        {
            if (String.IsNullOrWhiteSpace(raw))
                return String.Empty;

            string alias = raw.Trim().ToLowerInvariant();
            while (alias.Contains("  "))
                alias = alias.Replace("  ", " ");
            return alias;
        }
    }
}
