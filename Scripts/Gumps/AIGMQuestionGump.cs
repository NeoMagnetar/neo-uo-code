using System;
using System.Collections.Generic;
using System.IO;
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
            AddTextEntry(15, 175, 490, 125, 1153, 0, String.Empty);

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
                        try
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

                            if (AIGMSettings.EnableDebugLogging)
                                Log("Ask start. Question='" + question + "' RequestIdPreview=AIGM-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Utility.RandomMinMax(1000, 9999));
                            m_State.LastQuestion = question;
                            m_State.ActiveTaskSummary = question;
                            m_State.LastRequestUtc = DateTime.UtcNow;
                            m_State.AddTurn("user", question);
                            m_State.LastResponse = AIGMBridgeClient.Ask(m_From, question, m_State.CurrentTarget);
                            int before = m_State.LastResponse != null && m_State.LastResponse.ProposedActions != null ? m_State.LastResponse.ProposedActions.Count : 0;
                            m_State.LastResponse = AIGMProposalAugmenter.EnsureCoreActionProposals(m_From, question, m_State.CurrentTarget, m_State.LastResponse);
                            int after = m_State.LastResponse != null && m_State.LastResponse.ProposedActions != null ? m_State.LastResponse.ProposedActions.Count : 0;
                            bool addedNativeAdd = m_State.LastResponse != null && m_State.LastResponse.ProposedActions != null && m_State.LastResponse.ProposedActions.Exists(a => a != null && a.ActionKind == "gm_add_world_item");
                            AIGMExecutionLog.Write("QUESTION_AUGMENT question=\"{0}\" before={1} after={2} addedNativeAdd={3}", question, before, after, addedNativeAdd);
                            if (AIGMSettings.EnableDebugLogging)
                                Log("Bridge returned. Ok=" + (m_State.LastResponse != null ? m_State.LastResponse.Ok.ToString() : "null") + " ProposedActions=" + (m_State.LastResponse != null && m_State.LastResponse.ProposedActions != null ? m_State.LastResponse.ProposedActions.Count.ToString() : "null"));

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
                                m_State.AddTurn("assistant", m_State.LastResponse.ReplyText);
                                m_State.LastWorldSummary = m_State.LastResponse.ReplyText;
                            }

                            m_From.SendGump(new AIGMResponseGump(m_From, m_Npc, m_State.LastResponse));
                            break;
                        }
                        catch (Exception ex)
                        {
                            Log("Ask exception: " + ex);
                            m_From.SendMessage("AI GM question handling failed: {0}", ex.Message);
                            m_From.SendGump(new AIGMQuestionGump(m_From, m_Npc));
                            break;
                        }
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

        private static void Log(string message)
        {
            if (!AIGMSettings.EnableDebugLogging)
                return;

            try
            {
                string path = Path.Combine(Core.BaseDirectory, "Logs", "AIGMQuestionGump.log");
                File.AppendAllText(path, DateTime.UtcNow.ToString("o") + " " + (message ?? String.Empty) + Environment.NewLine);
            }
            catch
            {
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

            private AIGMTargetInfo BuildTargetInfo(object targeted)
            {
                Item item = targeted as Item;
                if (item != null)
                {
                    AIGMTargetInfo info = new AIGMTargetInfo();
                    info.Kind = "Item";
                    info.Serial = item.Serial.Value;
                    info.Name = item.Name;
                    info.TypeName = item.GetType().Name;
                    info.MapName = item.Map != null ? item.Map.Name : null;
                    info.RegionName = item.Map != null ? Region.Find(item.Location, item.Map).Name : null;
                    info.X = item.Location.X;
                    info.Y = item.Location.Y;
                    info.Z = item.Location.Z;
                    info.Distance = m_From != null ? (int)Math.Round(m_From.GetDistanceToSqrt(item.GetWorldLocation())) : 0;
                    info.IsContainer = item is Container;
                    info.IsDoor = item is BaseDoor;
                    info.IsStatic = item is Static;
                    info.IsMovable = item.Movable;
                    info.IsAlive = false;
                    info.ParentTypeName = item.Parent != null ? item.Parent.GetType().Name : null;
                    AddTag(info, info.IsContainer, "container");
                    AddTag(info, info.IsDoor, "door");
                    AddTag(info, info.IsStatic, "static");
                    AddTag(info, !info.IsMovable, "immovable");
                    AddTag(info, info.ParentTypeName != null, "contained");
                    return info;
                }

                Mobile mob = targeted as Mobile;
                if (mob != null)
                {
                    AIGMTargetInfo info = new AIGMTargetInfo();
                    info.Kind = "Mobile";
                    info.Serial = mob.Serial.Value;
                    info.Name = mob.Name;
                    info.TypeName = mob.GetType().Name;
                    info.MapName = mob.Map != null ? mob.Map.Name : null;
                    info.RegionName = mob.Region != null ? mob.Region.Name : null;
                    info.X = mob.Location.X;
                    info.Y = mob.Location.Y;
                    info.Z = mob.Location.Z;
                    info.Distance = m_From != null ? (int)Math.Round(m_From.GetDistanceToSqrt(mob.Location)) : 0;
                    info.IsPlayer = mob.Player;
                    info.IsNpc = !mob.Player;
                    info.IsVendor = mob is BaseVendor;
                    info.IsAlive = mob.Alive;
                    AddTag(info, info.IsPlayer, "player");
                    AddTag(info, info.IsNpc, "npc");
                    AddTag(info, info.IsVendor, "vendor");
                    AddTag(info, !info.IsAlive, "dead");
                    return info;
                }

                return null;
            }

            private static void AddTag(AIGMTargetInfo info, bool condition, string tag)
            {
                if (info == null || !condition || String.IsNullOrWhiteSpace(tag))
                    return;

                if (!info.Tags.Contains(tag))
                    info.Tags.Add(tag);
            }
        }
    }
}

