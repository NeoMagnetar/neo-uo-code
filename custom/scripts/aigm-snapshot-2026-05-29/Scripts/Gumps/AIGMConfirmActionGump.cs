using System;
using System.IO;
using Server.Custom.AIGM;
using Server.Network;

namespace Server.Gumps
{
    public class AIGMConfirmActionGump : Gump
    {
        private readonly Mobile m_From;
        private readonly AIGMActionProposal m_Action;

        public AIGMConfirmActionGump(Mobile from, AIGMActionProposal action)
            : base(120, 120)
        {
            m_From = from;
            m_Action = action;

            AddPage(0);
            AddBackground(0, 0, 560, 340, 5054);
            AddImageTiled(10, 10, 540, 25, 2624);
            AddAlphaRegion(10, 10, 540, 25);
            AddHtml(20, 12, 520, 20, "<BASEFONT COLOR=#FFFFFF><CENTER>Confirm AI GM Action</CENTER></BASEFONT>", false, false);

            AddImageTiled(10, 45, 540, 225, 2624);
            AddAlphaRegion(10, 45, 540, 225);

            string desc = action != null ? Utility.FixHtml(action.Description ?? "Unknown action") : "Unknown action";
            string category = action != null ? Utility.FixHtml((action.Category ?? "read").ToUpperInvariant()) : "UNKNOWN";
            string preview = action != null ? Utility.FixHtml(AIGMActionPreview.BuildPreviewText(action)) : "No preview supplied.";
            string targetSummary = Utility.FixHtml(AIGMActionPreview.BuildTargetSummary(action));
            string warning = Utility.FixHtml(AIGMActionPreview.BuildMutationWarning(action));

            AddHtml(20, 55, 520, 200,
                String.Format("<BASEFONT COLOR=#FFFFFF><BASEFONT COLOR=#FFCC99>Category:</BASEFONT> {0}<BR><BR><BASEFONT COLOR=#FFD27F>Action:</BASEFONT> {1}<BR><BR><BASEFONT COLOR=#99CCFF>Target:</BASEFONT> {2}<BR><BR><BASEFONT COLOR=#99FF99>Preview:</BASEFONT> {3}<BR><BR><BASEFONT COLOR=#FF6666>Warning:</BASEFONT> {4}</BASEFONT>", category, desc, targetSummary, preview, warning),
                true,
                true);

            AddButton(20, 285, 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddHtml(55, 285, 90, 20, "<BASEFONT COLOR=#FFFFFF>Confirm</BASEFONT>", false, false);

            AddButton(140, 285, 4005, 4007, 0, GumpButtonType.Reply, 0);
            AddHtml(175, 285, 90, 20, "<BASEFONT COLOR=#FFFFFF>Cancel</BASEFONT>", false, false);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Log("CONFIRM_OPEN kind=" + (m_Action != null ? m_Action.ActionKind : "null") + " typeName=" + (m_Action != null ? m_Action.GetParameter("typeName") : "null") + " amount=" + (m_Action != null ? m_Action.GetParameter("amount") : "null") + " button=" + info.ButtonID);

            if (m_From == null || m_From.Deleted || m_Action == null)
                return;

            if (info.ButtonID == 1)
            {
                Log("CONFIRM_CLICK kind=" + (m_Action != null ? m_Action.ActionKind : "null"));
                string result;
                if (AIGMActionExecutor.Execute(m_From, m_Action, out result))
                {
                    Log("Confirm execute success result=" + (result ?? String.Empty));
                    string targetSummary = AIGMActionPreview.BuildTargetSummary(m_Action);
                    AIGMActionHistory.Record(m_From, m_Action, result, targetSummary);

                    if (!String.IsNullOrWhiteSpace(result))
                        m_From.SendMessage(result);

                    string followup = AIGMActionExecutor.BuildFollowupSuggestion(m_Action);
                    if (!String.IsNullOrWhiteSpace(followup))
                        m_From.SendMessage(followup);
                }
                else if (!String.IsNullOrWhiteSpace(result))
                {
                    Log("Confirm execute failure result=" + result);
                    m_From.SendMessage(result);
                }
            }
        }

        private static void Log(string message)
        {
            try
            {
                string path = Path.Combine(Core.BaseDirectory, "Logs", "AIGMConfirmActionGump.log");
                File.AppendAllText(path, DateTime.UtcNow.ToString("o") + " " + (message ?? String.Empty) + Environment.NewLine);
            }
            catch
            {
            }
        }
    }
}
