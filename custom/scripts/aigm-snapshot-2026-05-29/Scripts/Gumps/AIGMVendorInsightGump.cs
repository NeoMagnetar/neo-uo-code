using System;

namespace Server.Gumps
{
    public class AIGMVendorInsightGump : Gump
    {
        public AIGMVendorInsightGump(string title, string body)
            : base(100, 100)
        {
            AddPage(0);
            AddBackground(0, 0, 580, 430, 5054);
            AddImageTiled(10, 10, 560, 25, 2624);
            AddAlphaRegion(10, 10, 560, 25);
            AddHtml(20, 12, 540, 20, String.Format("<BASEFONT COLOR=#FFFFFF><CENTER>{0}</CENTER></BASEFONT>", Utility.FixHtml(title ?? "AI GM Vendor Insight")), false, false);

            AddImageTiled(10, 45, 560, 340, 2624);
            AddAlphaRegion(10, 45, 560, 340);
            AddHtml(20, 55, 540, 320, String.Format("<BASEFONT COLOR=#FFFFFF>{0}</BASEFONT>", body ?? "No vendor insight available."), true, true);

            AddButton(20, 395, 4005, 4007, 0, GumpButtonType.Reply, 0);
            AddHtml(55, 395, 80, 20, "<BASEFONT COLOR=#FFFFFF>Close</BASEFONT>", false, false);
        }
    }
}
