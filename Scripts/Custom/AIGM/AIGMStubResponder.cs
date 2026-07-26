using System;

namespace Server.Custom.AIGM
{
    public static class AIGMStubResponder
    {
        public static AIGMResponse Ask(Mobile from, string question, AIGMTargetInfo target)
        {
            AIGMResponse response = new AIGMResponse();
            response.Ok = true;
            response.Confidence = "low";

            string targetSummary = target != null ? target.ToSummaryString() : "No target selected.";

            response.ReplyText = String.Format(
                "Bridge is not connected yet, but the in-game shell is working. I received your question:<BR><BR><I>{0}</I><BR><BR>Current target:<BR>{1}",
                Utility.FixHtml(question ?? String.Empty),
                Utility.FixHtml(targetSummary));

            response.LikelyFiles.Add("Scripts\\Mobiles\\NPCs\\AIGMCounselor.cs");
            response.LikelyFiles.Add("Scripts\\Gumps\\AIGMQuestionGump.cs");
            response.LikelyFiles.Add("Scripts\\Gumps\\AIGMResponseGump.cs");

            response.SuggestedChecks.Add("Verify question entry and gump flow feel good in-game.");
            response.SuggestedChecks.Add("Verify target selection captures the object you care about.");
            response.SuggestedChecks.Add("Next step: replace this stub with a localhost bridge client.");

            response.Warnings.Add("This is a stub response, not live AI reasoning.");
            return response;
        }
    }
}
