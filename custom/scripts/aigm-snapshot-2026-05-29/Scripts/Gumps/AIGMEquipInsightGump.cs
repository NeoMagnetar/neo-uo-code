using System;
using System.Text;

namespace Server.Gumps
{
    public class AIGMEquipInsightGump : Gump
    {
        public AIGMEquipInsightGump(Mobile target)
            : base(110, 110)
        {
            AddPage(0);
            AddBackground(0, 0, 580, 430, 5054);
            AddImageTiled(10, 10, 560, 25, 2624);
            AddAlphaRegion(10, 10, 560, 25);
            AddHtml(20, 12, 540, 20, String.Format("<BASEFONT COLOR=#FFFFFF><CENTER>Equipment: {0}</CENTER></BASEFONT>", Utility.FixHtml(target != null ? target.Name : "Unknown")), false, false);

            AddImageTiled(10, 45, 560, 340, 2624);
            AddAlphaRegion(10, 45, 560, 340);
            AddHtml(20, 55, 540, 320, BuildHtml(target), true, true);

            AddButton(20, 395, 4005, 4007, 0, GumpButtonType.Reply, 0);
            AddHtml(55, 395, 80, 20, "<BASEFONT COLOR=#FFFFFF>Close</BASEFONT>", false, false);
        }

        private static string BuildHtml(Mobile target)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<BASEFONT COLOR=#FFFFFF>");

            if (target == null)
            {
                sb.Append("No target available.");
                sb.Append("</BASEFONT>");
                return sb.ToString();
            }

            sb.AppendFormat("<BASEFONT COLOR=#FFD27F>Name:</BASEFONT> {0}<BR>", Utility.FixHtml(target.Name));
            sb.AppendFormat("<BASEFONT COLOR=#FFD27F>Type:</BASEFONT> {0}<BR><BR>", Utility.FixHtml(target.GetType().Name));

            if (target.Items == null || target.Items.Count == 0)
            {
                sb.Append("No equipped items found.");
            }
            else
            {
                sb.Append("<BASEFONT COLOR=#99CCFF>Equipped Items</BASEFONT><BR>");
                for (int i = 0; i < target.Items.Count; i++)
                {
                    Item item = target.Items[i];
                    sb.AppendFormat("- {0}: {1}<BR>", item.Layer, Utility.FixHtml(item.GetType().Name));
                }
            }

            sb.Append("</BASEFONT>");
            return sb.ToString();
        }
    }
}
