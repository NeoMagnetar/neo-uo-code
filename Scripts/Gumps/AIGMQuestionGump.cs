using System;
using System.Collections.Generic;
using Server.Custom.AIGM;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.Gumps
{
    public class AIGMQuestionGump : Gump
    {
        private readonly Mobile m_From;
        private readonly AIGMCounselor m_Npc;
        private readonly AIGMSessionState m_State;

        public AIGMQuestionGump(Mobile from, AIGMCounselor npc)
            : base(50, 50)
        {
            m_From = from;
            m_Npc = npc;
            m_State = AIGMSessionState.Get(from);

            Closable = true;
            Disposable = true;
            Dragable = true;

            AddPage(0);
            AddBackground(0, 0, 520, 385, 5054);
            AddImageTiled(10, 10, 500, 25, 2624);
            AddAlphaRegion(10, 10, 500, 25);
            AddHtml(15, 12, 490, 20, "<BASEFONT COLOR=#FFFFFF><CENTER>AI GM Counselor</CENTER></BASEFONT>", false, false);

            AddImageTiled(10, 45, 500, 90, 2624);
            AddAlphaRegion(10, 45, 500, 90);
            AddHtml(15, 50, 490, 20, "<BASEFONT COLOR=#FFD27F>Current Target</BASEFONT>", false, false);
            AddHtml(15, 72, 490, 58, String.Format("<BASEFONT COLOR=#FFFFFF>{0}</BASEFONT>", Utility.FixHtml(GetTargetSummary())), false, true);

            AddImageTiled(10, 145, 500, 165, 2624);
            AddAlphaRegion(10, 145, 500, 165);
            AddHtml(15, 150, 490, 20, "<BASEFONT COLOR=#99CCFF>Question</BASEFONT>", false, false);
            AddTextEntry(15, 175, 490, 125, 0xF3, 0, m_State.LastQuestion ?? String.Empty);

            AddButton(15, 325, 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddHtml(50, 325, 90, 20, "<BASEFONT COLOR=#FFFFFF>Ask</BASEFONT>", false, false);

            AddButton(145, 325, 4005, 4007, 2, GumpButtonType.Reply, 0);
            AddHtml(180, 325, 110, 20, "<BASEFONT COLOR=#FFFFFF>Set Target</BASEFONT>", false, false);

            AddButton(300, 325, 4005, 4007, 3, GumpButtonType.Reply, 0);
            AddHtml(335, 325, 110, 20, "<BASEFONT COLOR=#FFFFFF>Clear Target</BASEFONT>", false, false);

            AddButton(365, 325, 4005, 4007, 4, GumpButtonType.Reply, 0);
            AddHtml(400, 325, 85, 20, "<BASEFONT COLOR=#FFFFFF>Continue</BASEFONT>", false, false);

            AddButton(430, 325, 4005, 4007, 0, GumpButtonType.Reply, 0);
            AddHtml(465, 325, 45, 20, "<BASEFONT COLOR=#FFFFFF>Close</BASEFONT>", false, false);
        }

        private string GetTargetSummary()
        {
            return m_State.CurrentTarget != null ? m_State.CurrentTarget.ToSummaryString() : "No target selected.";
        }

        public static void BeginTargeting(Mobile from, AIGMCounselor npc)
        {
            from.Target = new AIGMInternalTarget(from, npc);
            from.SendMessage("Target an item or mobile for the AI GM counselor.");
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (m_From == null || m_From.Deleted)
                return;

            if (m_From.AccessLevel < AIGMSettings.RequiredAccess)
                return;

            switch (info.ButtonID)
            {
                case 1:
                    {
                        TextRelay entry = info.GetTextEntry(0);
                        string question = entry == null ? String.Empty : (entry.Text ?? String.Empty).Trim();

                        if (question.Length == 0)
                        {
                            m_From.SendMessage("Enter a question first.");
                            m_From.SendGump(new AIGMQuestionGump(m_From, m_Npc));
                            return;
                        }

                        if (question.Length > AIGMSettings.MaxQuestionLength)
                            question = question.Substring(0, AIGMSettings.MaxQuestionLength);

                        if (DateTime.UtcNow - m_State.LastRequestUtc < AIGMSettings.Cooldown)
                        {
                            m_From.SendMessage("Give the archives a moment before asking again.");
                            m_From.SendGump(new AIGMQuestionGump(m_From, m_Npc));
                            return;
                        }

                        m_State.LastQuestion = question;
                        m_State.LastRequestUtc = DateTime.UtcNow;
                        m_State.LastResponse = AIGMBridgeClient.Ask(m_From, question, m_State.CurrentTarget);

                        if (m_State.LastResponse != null)
                        {
                            AIGMInvestigationSnapshot snapshot = new AIGMInvestigationSnapshot();
                            snapshot.LastQuestion = question;
                            snapshot.LastTargetSummary = m_State.CurrentTarget != null ? m_State.CurrentTarget.ToSummaryString() : "No target selected.";
                            snapshot.InvestigationKind = m_State.LastResponse.InvestigationKind;
                            if (m_State.LastResponse.LikelyFiles != null)
                                snapshot.LikelyFiles.AddRange(m_State.LastResponse.LikelyFiles);
                            m_State.Investigation = snapshot;
                            AIGMInvestigationState.Set(m_From, snapshot);
                        }

                        if (m_State.LastResponse != null && !m_State.LastResponse.Ok)
                        {
                            AIGMResponse fallback = AIGMStubResponder.Ask(m_From, question, m_State.CurrentTarget);
                            fallback.Warnings.Add(m_State.LastResponse.ErrorMessage ?? "Live bridge failed.");
                            m_State.LastResponse = fallback;
                        }

                        m_From.SendGump(new AIGMResponseGump(m_From, m_Npc, m_State.LastResponse));
                        break;
                    }
                case 2:
                    {
                        BeginTargeting(m_From, m_Npc);
                        break;
                    }
                case 3:
                    {
                        m_State.CurrentTarget = null;
                        m_From.SendMessage("AI GM target cleared.");
                        m_From.SendGump(new AIGMQuestionGump(m_From, m_Npc));
                        break;
                    }
                case 4:
                    {
                        AIGMInvestigationSnapshot snapshot = AIGMInvestigationState.Get(m_From);
                        if (snapshot == null)
                        {
                            m_From.SendMessage("No recent AI GM investigation is available to continue.");
                            m_From.SendGump(new AIGMQuestionGump(m_From, m_Npc));
                            return;
                        }

                        List<AIGMActionHistoryEntry> history = AIGMActionHistory.Get(m_From);
                        string summary = AIGMResumeAdvisor.BuildResumeSummary(snapshot, history);

                        AIGMResponse response = new AIGMResponse();
                        response.Ok = true;
                        response.InvestigationKind = snapshot.InvestigationKind;
                        response.ReplyText = summary;
                        response.Confidence = "medium";
                        if (snapshot.LikelyFiles != null)
                            response.LikelyFiles.AddRange(snapshot.LikelyFiles);
                        response.SuggestedChecks.AddRange(AIGMResumeAdvisor.BuildSuggestions(snapshot, history));

                        m_State.LastResponse = response;
                        m_From.SendGump(new AIGMResponseGump(m_From, m_Npc, response));
                        break;
                    }
            }
        }

        private class AIGMInternalTarget : Target
        {
            private readonly Mobile m_From;
            private readonly AIGMCounselor m_Npc;

            public AIGMInternalTarget(Mobile from, AIGMCounselor npc)
                : base(-1, false, TargetFlags.None)
            {
                m_From = from;
                m_Npc = npc;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                AIGMSessionState state = AIGMSessionState.Get(m_From);
                AIGMTargetInfo info = BuildTargetInfo(targeted);

                if (info == null)
                {
                    m_From.SendMessage("That is not a supported AI GM target.");
                }
                else
                {
                    state.CurrentTarget = info;
                    m_From.SendMessage("AI GM target set: {0}", info.ToSummaryString());
                }

                m_From.SendGump(new AIGMQuestionGump(m_From, m_Npc));
            }

            protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
            {
                m_From.SendGump(new AIGMQuestionGump(m_From, m_Npc));
            }

            private static AIGMTargetInfo BuildTargetInfo(object targeted)
            {
                Item item = targeted as Item;
                if (item != null)
                {
                    return new AIGMTargetInfo
                    {
                        Kind = "Item",
                        Serial = item.Serial.Value,
                        Name = item.Name,
                        TypeName = item.GetType().Name,
                        MapName = item.Map != null ? item.Map.Name : null,
                        RegionName = item.Map != null ? Region.Find(item.Location, item.Map).Name : null,
                        X = item.Location.X,
                        Y = item.Location.Y,
                        Z = item.Location.Z
                    };
                }

                Mobile mob = targeted as Mobile;
                if (mob != null)
                {
                    return new AIGMTargetInfo
                    {
                        Kind = "Mobile",
                        Serial = mob.Serial.Value,
                        Name = mob.Name,
                        TypeName = mob.GetType().Name,
                        MapName = mob.Map != null ? mob.Map.Name : null,
                        RegionName = mob.Region != null ? mob.Region.Name : null,
                        X = mob.Location.X,
                        Y = mob.Location.Y,
                        Z = mob.Location.Z
                    };
                }

                return null;
            }
        }
    }
}
