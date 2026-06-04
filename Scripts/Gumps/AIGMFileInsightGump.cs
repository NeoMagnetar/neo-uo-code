using System;

namespace Server.Gumps
{
    public class AIGMFileInsightGump : Gump
    {
        public AIGMFileInsightGump(string path, string insight)
            : base(90, 90)
        {
            AddPage(0);
            AddBackground(0, 0, 560, 420, 5054);
            AddImageTiled(10, 10, 540, 25, 2624);
            AddAlphaRegion(10, 10, 540, 25);
            AddHtml(20, 12, 520, 20, String.Format("<BASEFONT COLOR=#FFFFFF><CENTER>{0}</CENTER></BASEFONT>", Utility.FixHtml(path ?? "AI GM File Insight")), false, false);

            AddImageTiled(10, 45, 540, 330, 2624);
            AddAlphaRegion(10, 45, 540, 330);
            AddHtml(20, 55, 520, 310, String.Format("<BASEFONT COLOR=#FFFFFF>{0}</BASEFONT>", insight ?? "No insight available."), true, true);

            AddButton(20, 385, 4005, 4007, 0, GumpButtonType.Reply, 0);
            AddHtml(55, 385, 80, 20, "<BASEFONT COLOR=#FFFFFF>Close</BASEFONT>", false, false);
        }
    }
}
