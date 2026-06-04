using System;
using System.IO;
using Server.Custom.AIGM;
using Server.Mobiles;
using Server.Network;

namespace Server.Gumps
{
    public class AIGMResponseGump : Gump
    {
        private const int ActionButtonBase = 1000;

        private readonly Mobile m_From;
        private readonly AIGMCounselor m_Npc;
        private readonly AIGMResponse m_Response;

        public AIGMResponseGump(Mobile from, AIGMCounselor npc, AIGMResponse response)
            : base(60, 60)
        {
            m_From = from;
            m_Npc = npc;
            m_Response = response;

            Closable = true;
            Disposable = true;
            Dragable = true;

            AddPage(0);
            AddBackground(0, 0, 620, 470, 5054);
            AddImageTiled(10, 10, 600, 360, 2624);
            AddAlphaRegion(10, 10, 600, 360);
            AddHtml(20, 20, 580, 340, m_Response != null ? m_Response.ToDisplayHtml() : "<BASEFONT COLOR=#FF6666>No response available.</BASEFONT>", true, true);

            AddButton(20, 385, 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddHtml(55, 385, 120, 20, "<BASEFONT COLOR=#FFFFFF>Ask Follow-up</BASEFONT>", false, false);

            AddButton(200, 385, 4005, 4007, 2, GumpButtonType.Reply, 0);
            AddHtml(235, 385, 90, 20, "<BASEFONT COLOR=#FFFFFF>Retarget</BASEFONT>", false, false);

            AddButton(420, 385, 4005, 4007, 3, GumpButtonType.Reply, 0);
            AddHtml(455, 385, 70, 20, "<BASEFONT COLOR=#FFFFFF>History</BASEFONT>", false, false);

            AddButton(530, 385, 4005, 4007, 0, GumpButtonType.Reply, 0);
            AddHtml(565, 385, 50, 20, "<BASEFONT COLOR=#FFFFFF>Close</BASEFONT>", false, false);

            RenderActionButtons();
        }

        private void RenderActionButtons()
        {
            if (m_Response == null || m_Response.ProposedActions == null || m_Response.ProposedActions.Count == 0)
                return;

            if (AIGMSettings.EnableDebugLogging)
                Log("RESPONSE_RENDER_ACTIONS count=" + m_Response.ProposedActions.Count);
            int count = Math.Min(3, m_Response.ProposedActions.Count);
            int y = 415;

            for (int i = 0; i < count; i++)
            {
                AIGMActionProposal action = m_Response.ProposedActions[i];
                int buttonId = ActionButtonBase + i;
                if (AIGMSettings.EnableDebugLogging)
                    Log("RESPONSE_RENDER_ACTION index=" + i + " kind=" + (action != null ? action.ActionKind : "null") + " title=" + (action != null ? action.Title : "null") + " buttonId=" + buttonId);
                AddButton(20 + (i * 195), y, 4005, 4007, buttonId, GumpButtonType.Reply, 0);
                AddHtml(55 + (i * 195), y, 170, 20,
                    String.Format("<BASEFONT COLOR=#FFFFFF>{0}</BASEFONT>", Utility.FixHtml(GetActionLabel(action, i))), false, false);
            }
        }

        private static string GetActionLabel(AIGMActionProposal action, int index)
        {
            if (action == null || String.IsNullOrWhiteSpace(action.Description))
                return String.Format("Action {0}", index + 1);

            string prefix = GetCategoryPrefix(action.Category);
            string label = action.Description.Trim();
            if (label.Length > 18)
                label = label.Substring(0, 18) + "...";

            return String.Format("{0} {1}", prefix, label);
        }

        private static string GetCategoryPrefix(string category)
        {
            if (String.IsNullOrWhiteSpace(category))
                return "[R]";

            switch (category.Trim().ToLowerInvariant())
            {
                case "move": return "[M]";
                case "navigate": return "[N]";
                case "mutate": return "[!]";
                default: return "[R]";
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (AIGMSettings.EnableDebugLogging)
                Log("RESPONSE_CLICK buttonId=" + info.ButtonID + " proposedCount=" + (m_Response != null && m_Response.ProposedActions != null ? m_Response.ProposedActions.Count.ToString() : "null"));

            if (m_From == null || m_From.Deleted)
                return;

            switch (info.ButtonID)
            {
                case 1:
                    m_From.SendGump(new AIGMQuestionGump(m_From, m_Npc));
                    break;
                case 2:
                    AIGMQuestionGump.BeginTargeting(m_From, m_Npc);
                    break;
                case 3:
                    m_From.SendGump(new AIGMActionHistoryGump(m_From));
                    break;
                default:
                    if (info.ButtonID >= ActionButtonBase && info.ButtonID < ActionButtonBase + 3)
                    {
                        int index = info.ButtonID - ActionButtonBase;
                        ExecuteAction(index);
                    }
                    break;
            }
        }

        private void ExecuteAction(int index)
        {
            if (m_Response == null || m_Response.ProposedActions == null || index < 0 || index >= m_Response.ProposedActions.Count)
            {
                Log("ExecuteAction invalid index=" + index);
                m_From.SendMessage("No executable AI GM action is available in that slot.");
                return;
            }

            AIGMActionProposal action = m_Response.ProposedActions[index];
            if (AIGMSettings.EnableDebugLogging)
                Log("RESPONSE_SELECTED_ACTION index=" + index + " kind=" + (action != null ? action.ActionKind : "null") + " category=" + (action != null ? action.Category : "null") + " desc=" + (action != null ? action.Description : "null"));
            if (action != null && (action.RequiresConfirmation || (!String.IsNullOrWhiteSpace(action.Category) && action.Category.Equals("mutate", StringComparison.OrdinalIgnoreCase))))
            {
                if (AIGMSettings.EnableDebugLogging)
                    Log("RESPONSE_OPEN_CONFIRM kind=" + (action != null ? action.ActionKind : "null"));
                m_From.SendGump(new AIGMConfirmActionGump(m_From, action));
                return;
            }

            string result;
            if (AIGMActionExecutor.Execute(m_From, action, out result))
            {
                if (AIGMSettings.EnableDebugLogging)
                    Log("ExecuteAction immediate success result=" + (result ?? String.Empty));
                string targetSummary = AIGMActionPreview.BuildTargetSummary(action);
                AIGMActionHistory.Record(m_From, action, result, targetSummary);

                if (!String.IsNullOrWhiteSpace(result))
                    m_From.SendMessage(result);

                string followup = AIGMActionExecutor.BuildFollowupSuggestion(action);
                if (!String.IsNullOrWhiteSpace(followup))
                    m_From.SendMessage(followup);
            }
            else if (!String.IsNullOrWhiteSpace(result))
            {
                if (AIGMSettings.EnableDebugLogging)
                    Log("ExecuteAction immediate failure result=" + result);
                m_From.SendMessage(result);
            }
        }

        private static void Log(string message)
        {
            if (!AIGMSettings.EnableDebugLogging)
                return;

            try
            {
                string path = Path.Combine(Core.BaseDirectory, "Logs", "AIGMResponseGump.log");
                File.AppendAllText(path, DateTime.UtcNow.ToString("o") + " " + (message ?? String.Empty) + Environment.NewLine);
            }
            catch
            {
            }
        }
    }
}
