using System;
using System.Collections.Generic;
using System.Text;
using Server.Custom.AIGM;

namespace Server.Gumps
{
    public class AIGMActionHistoryGump : Gump
    {
        public AIGMActionHistoryGump(Mobile from)
            : base(120, 120)
        {
            List<AIGMActionHistoryEntry> entries = AIGMActionHistory.Get(from);

            AddPage(0);
            AddBackground(0, 0, 620, 420, 5054);
            AddImageTiled(10, 10, 600, 25, 2624);
            AddAlphaRegion(10, 10, 600, 25);
            AddHtml(20, 12, 580, 20, "<BASEFONT COLOR=#FFFFFF><CENTER>AI GM Recent Actions</CENTER></BASEFONT>", false, false);

            AddImageTiled(10, 45, 600, 330, 2624);
            AddAlphaRegion(10, 45, 600, 330);
            AddHtml(20, 55, 580, 310, BuildHtml(entries), true, true);

            AddButton(20, 385, 4005, 4007, 0, GumpButtonType.Reply, 0);
            AddHtml(55, 385, 70, 20, "<BASEFONT COLOR=#FFFFFF>Close</BASEFONT>", false, false);
        }

        private static string BuildHtml(List<AIGMActionHistoryEntry> entries)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<BASEFONT COLOR=#FFFFFF>");

            if (entries == null || entries.Count == 0)
            {
                sb.Append("No recent AI GM actions recorded.");
                sb.Append("</BASEFONT>");
                return sb.ToString();
            }

            for (int i = entries.Count - 1; i >= 0; i--)
            {
                AIGMActionHistoryEntry e = entries[i];
                sb.AppendFormat("<BASEFONT COLOR=#FFCC99>[{0}]</BASEFONT> {1}<BR>", Utility.FixHtml((e.Category ?? "read").ToUpperInvariant()), Utility.FixHtml(e.Description ?? "Unknown action"));
                sb.AppendFormat("<BASEFONT COLOR=#99CCFF>Target:</BASEFONT> {0}<BR>", Utility.FixHtml(e.TargetSummary ?? "Unknown"));
                sb.AppendFormat("<BASEFONT COLOR=#99FF99>Result:</BASEFONT> {0}<BR><BR>", Utility.FixHtml(e.Result ?? "No result"));
            }

            sb.Append("</BASEFONT>");
            return sb.ToString();
        }
    }
}
