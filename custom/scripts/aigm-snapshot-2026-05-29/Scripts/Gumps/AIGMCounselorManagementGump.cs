using Server.Mobiles;
using Server.Network;

namespace Server.Gumps
{
    public class AIGMCounselorManagementGump : Gump
    {
        private readonly AIGMCounselor _counselor;

        public AIGMCounselorManagementGump(AIGMCounselor counselor)
            : base(50, 200)
        {
            _counselor = counselor;

            AddBackground(25, 10, 530, 220, 0x13BE);
            AddImageTiled(35, 20, 510, 200, 0xA40);
            AddAlphaRegion(35, 20, 510, 200);
            AddImage(10, 0, 0x28DC);
            AddImage(537, 215, 0x28DC);
            AddImage(10, 215, 0x28DC);
            AddImage(537, 0, 0x28DC);

            AddHtml(40, 21, 320, 20, "<BASEFONT COLOR=#FFFFFF><CENTER>AI COUNSELOR MANAGEMENT</CENTER></BASEFONT>", false, false);
            AddLabel(40, 48, 0x480, counselor != null ? counselor.Name : "Counselor");
            AddLabel(40, 72, 0x7FFF, counselor != null ? counselor.Title : "the AI counselor");

            AddButton(390, 24, 0x15E1, 0x15E5, 1, GumpButtonType.Reply, 0);
            AddHtml(408, 21, 120, 20, "<BASEFONT COLOR=#FFFFFF>Open Bag</BASEFONT>", false, false);

            AddButton(390, 44, 0x15E1, 0x15E5, 2, GumpButtonType.Reply, 0);
            AddHtml(408, 41, 120, 20, "<BASEFONT COLOR=#FFFFFF>Customize Outfit</BASEFONT>", false, false);

            AddButton(390, 64, 0x15E1, 0x15E5, 3, GumpButtonType.Reply, 0);
            AddHtml(408, 61, 120, 20, "<BASEFONT COLOR=#FFFFFF>Open Paperdoll</BASEFONT>", false, false);

            AddButton(390, 84, 0x15E1, 0x15E5, 4, GumpButtonType.Reply, 0);
            AddHtml(408, 81, 120, 20, "<BASEFONT COLOR=#FFFFFF>Consult Counselor</BASEFONT>", false, false);

            AddButton(390, 202, 0x15E1, 0x15E5, 0, GumpButtonType.Reply, 0);
            AddHtmlLocalized(408, 201, 120, 20, 1011012, 0x7FFF, false, false);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;

            if (_counselor == null || _counselor.Deleted || from == null)
                return;

            if (!_counselor.IsOwner(from))
                return;

            switch (info.ButtonID)
            {
                case 1:
                    _counselor.OpenManagementBackpack(from);
                    break;
                case 2:
                    if (Server.Multis.BaseHouse.NewVendorSystem)
                        from.SendGump(new NewPlayerVendorCustomizeGump(_counselor));
                    else
                        from.SendGump(new PlayerVendorCustomizeGump(_counselor, from));
                    break;
                case 3:
                    _counselor.OpenManagementPaperdoll(from);
                    break;
                case 4:
                    _counselor.OpenConsultInterface(from);
                    break;
            }
        }
    }
}
