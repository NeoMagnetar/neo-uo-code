using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Gumps
{
    public class AIGMTestCopyListGump : Gump
    {
        public AIGMTestCopyListGump(List<Item> items)
            : base(130, 130)
        {
            AddPage(0);
            AddBackground(0, 0, 620, 420, 5054);
            AddImageTiled(10, 10, 600, 25, 2624);
            AddAlphaRegion(10, 10, 600, 25);
            AddHtml(20, 12, 580, 20, "<BASEFONT COLOR=#FFFFFF><CENTER>Nearby AI GM Test Copies</CENTER></BASEFONT>", false, false);

            AddImageTiled(10, 45, 600, 330, 2624);
            AddAlphaRegion(10, 45, 600, 330);
            AddHtml(20, 55, 580, 310, BuildHtml(items), true, true);

            AddButton(20, 385, 4005, 4007, 0, GumpButtonType.Reply, 0);
            AddHtml(55, 385, 70, 20, "<BASEFONT COLOR=#FFFFFF>Close</BASEFONT>", false, false);
        }

        private static string BuildHtml(List<Item> items)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<BASEFONT COLOR=#FFFFFF>");

            if (items == null || items.Count == 0)
            {
                sb.Append("No nearby marked test copies were found.");
                sb.Append("</BASEFONT>");
                return sb.ToString();
            }

            for (int i = 0; i < items.Count; i++)
            {
                Item item = items[i];
                sb.AppendFormat("- {0} [{1}] at {2},{3},{4}<BR>", Utility.FixHtml(item.Name ?? item.GetType().Name), item.GetType().Name, item.Location.X, item.Location.Y, item.Location.Z);
            }

            sb.Append("</BASEFONT>");
            return sb.ToString();
        }
    }
}
