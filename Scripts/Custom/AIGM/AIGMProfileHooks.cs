using System;
using System.IO;
using Server;
using Server.Gumps;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMProfileHooks
    {
        public static void Initialize()
        {
            EventSink.ProfileRequest += OnProfileRequest;
            EventSink.ChangeProfileRequest += OnChangeProfileRequest;
            EventSink.PaperdollRequest += OnPaperdollRequest;
        }

        private static void OnPaperdollRequest(PaperdollRequestEventArgs e)
        {
            if (e == null || e.Beholder == null || e.Beheld == null)
                return;

            AIGMCounselor counselor = e.Beheld as AIGMCounselor;
            if (counselor == null)
                return;

            Log("PaperdollRequest beholder=" + SafeName(e.Beholder) + " beheld=" + SafeName(e.Beheld));
        }

        private static void OnProfileRequest(ProfileRequestEventArgs e)
        {
            if (e == null || e.Beholder == null || e.Beheld == null)
                return;

            AIGMCounselor counselor = e.Beheld as AIGMCounselor;
            if (counselor == null)
                return;

            Mobile from = e.Beholder;
            Log("ProfileRequest beholder=" + SafeName(from) + " beheld=" + SafeName(e.Beheld) + " access=" + from.AccessLevel);

            if (from.Deleted)
                return;

            if (from.AccessLevel < AIGMSettings.RequiredAccess)
            {
                counselor.SayTo(from, "These archives are reserved for staff.");
            }

            // Intentionally do not hijack profile display requests.
        }

        private static void OnChangeProfileRequest(ChangeProfileRequestEventArgs e)
        {
            if (e == null || e.Beholder == null || e.Beheld == null)
                return;

            AIGMCounselor counselor = e.Beheld as AIGMCounselor;
            if (counselor == null)
                return;

            Mobile from = e.Beholder;
            Log("ChangeProfileRequest beholder=" + SafeName(from) + " beheld=" + SafeName(e.Beheld) + " access=" + from.AccessLevel + " text='" + (e.Text ?? String.Empty) + "'");

            if (from.Deleted)
                return;

            if (from.AccessLevel < AIGMSettings.RequiredAccess)
            {
                counselor.SayTo(from, "These archives are reserved for staff.");
                return;
            }

            string question = e.Text == null ? String.Empty : e.Text.Trim();
            if (question.Length == 0)
            {
                from.CloseGump(typeof(AIGMQuestionGump));
                from.CloseGump(typeof(AIGMResponseGump));
                from.SendGump(new AIGMQuestionGump(from, counselor));
                counselor.SayTo(from, "State your question, counselor.");
                return;
            }

            if (question.Length > AIGMSettings.MaxQuestionLength)
                question = question.Substring(0, AIGMSettings.MaxQuestionLength);

            AIGMSessionState state = AIGMSessionState.Get(from);
            if (DateTime.UtcNow - state.LastRequestUtc < AIGMSettings.Cooldown)
            {
                from.SendMessage("Give the archives a moment before asking again.");
                from.SendGump(new AIGMQuestionGump(from, counselor));
                return;
            }

            state.LastQuestion = question;
            state.ActiveTaskSummary = question;
            state.LastRequestUtc = DateTime.UtcNow;
            state.AddTurn("user", question);
            state.LastResponse = AIGMBridgeClient.Ask(from, question, state.CurrentTarget);

            if (state.LastResponse != null)
            {
                AIGMInvestigationSnapshot snapshot = new AIGMInvestigationSnapshot();
                snapshot.LastQuestion = question;
                snapshot.LastTargetSummary = state.CurrentTarget != null ? state.CurrentTarget.ToSummaryString() : "No target selected.";
                snapshot.InvestigationKind = state.LastResponse.InvestigationKind;
                if (state.LastResponse.LikelyFiles != null)
                    snapshot.LikelyFiles.AddRange(state.LastResponse.LikelyFiles);
                state.Investigation = snapshot;
                AIGMInvestigationState.Set(from, snapshot);
                state.AddTurn("assistant", state.LastResponse.ReplyText);
                state.LastWorldSummary = state.LastResponse.ReplyText;
            }

            from.CloseGump(typeof(AIGMQuestionGump));
            from.CloseGump(typeof(AIGMResponseGump));
            from.SendGump(new AIGMResponseGump(from, counselor, state.LastResponse));
            counselor.SayTo(from, "Very well. I am consulting the archives now.");
        }

        private static string SafeName(Mobile mob)
        {
            if (mob == null)
                return "null";

            return String.Format("{0}({1})", mob.Name ?? mob.GetType().Name, mob.Serial.Value);
        }

        private static void Log(string message)
        {
            try
            {
                string path = Path.Combine(Core.BaseDirectory, "Logs", "AIGMProfileHooks.log");
                File.AppendAllText(path, DateTime.UtcNow.ToString("o") + " " + (message ?? String.Empty) + Environment.NewLine);
            }
            catch
            {
            }
        }
    }
}
