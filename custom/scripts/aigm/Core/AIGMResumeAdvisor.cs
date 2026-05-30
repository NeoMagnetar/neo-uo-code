using System.Collections.Generic;

namespace Server.Custom.AIGM
{
    public static class AIGMResumeAdvisor
    {
        public static List<string> BuildSuggestions(AIGMInvestigationSnapshot snapshot, List<AIGMActionHistoryEntry> history)
        {
            List<string> suggestions = new List<string>();

            if (snapshot == null)
                return suggestions;

            string kind = snapshot.InvestigationKind ?? "general";
            AIGMActionHistoryEntry last = history != null && history.Count > 0 ? history[history.Count - 1] : null;

            if (kind == "vendor")
            {
                if (snapshot.LikelyFiles != null && snapshot.LikelyFiles.Count > 1)
                    suggestions.Add("Review the next vendor-related file: " + snapshot.LikelyFiles[1]);
                else
                    suggestions.Add("Reopen file insight for the top vendor file.");

                if (last != null && !string.IsNullOrWhiteSpace(last.Description) && last.Description.ToLowerInvariant().Contains("restock"))
                    suggestions.Add("You just restocked the vendor; inspect stock definitions next if behavior still looks wrong.");
                else
                    suggestions.Add("Inspect the stock definition or shared vendor framework next.");
            }
            else if (kind == "spawn")
            {
                suggestions.Add("Inspect the top spawner or region-related file next.");
                suggestions.Add("Retarget a nearby spawner or affected object for a tighter pass.");
            }
            else if (kind == "poison")
            {
                suggestions.Add("Inspect the top poison-related file next.");
                suggestions.Add("Retarget a weapon, item, or mobile that shows the poison behavior.");
            }
            else
            {
                if (snapshot.LikelyFiles != null && snapshot.LikelyFiles.Count > 0)
                    suggestions.Add("Reopen file insight for the top candidate: " + snapshot.LikelyFiles[0]);

                if (last != null && !string.IsNullOrWhiteSpace(last.Description))
                    suggestions.Add("Continue from your last action: " + last.Description);
                else
                    suggestions.Add("Retarget the live object if you want a fresher analysis.");
            }

            if (suggestions.Count < 3)
                suggestions.Add("Retarget the live object if you want a fresh pass.");

            return suggestions;
        }

        public static string BuildResumeSummary(AIGMInvestigationSnapshot snapshot, List<AIGMActionHistoryEntry> history)
        {
            if (snapshot == null)
                return "No recent AI GM investigation is available to continue.";

            string topFile = snapshot.LikelyFiles != null && snapshot.LikelyFiles.Count > 0 ? snapshot.LikelyFiles[0] : "none";
            string lastAction = history != null && history.Count > 0 ? history[history.Count - 1].Description : "none";

            return string.Format("Resuming {0} investigation. Last target: {1}. Top file: {2}. Last action: {3}.",
                snapshot.InvestigationKind ?? "general",
                snapshot.LastTargetSummary ?? "none",
                topFile,
                lastAction ?? "none");
        }
    }
}
