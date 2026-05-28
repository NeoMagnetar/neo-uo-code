using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Custom.AIGM
{
    public class AIGMResponse
    {
        public bool Ok { get; set; }
        public string ReplyText { get; set; }
        public List<string> LikelyFiles { get; set; }
        public List<string> SuggestedChecks { get; set; }
        public List<string> Warnings { get; set; }
        public string Confidence { get; set; }
        public string ErrorMessage { get; set; }
        public string InvestigationKind { get; set; }
        public List<AIGMActionProposal> ProposedActions { get; set; }

        public AIGMResponse()
        {
            LikelyFiles = new List<string>();
            SuggestedChecks = new List<string>();
            Warnings = new List<string>();
            ProposedActions = new List<AIGMActionProposal>();
        }

        private static string FormatCategory(string category)
        {
            if (String.IsNullOrWhiteSpace(category))
                return "Read";

            switch (category.Trim().ToLowerInvariant())
            {
                case "move": return "Move";
                case "mutate": return "Mutate";
                default: return "Read";
            }
        }

        public string ToDisplayHtml()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<BASEFONT COLOR=#101010>");
            sb.Append("<CENTER><BIG>AI GM Counselor</BIG></CENTER><BR>");

            if (!String.IsNullOrWhiteSpace(ReplyText))
            {
                sb.Append("<BASEFONT COLOR=#9C5A00>Answer</BASEFONT><BR>");
                sb.Append(ReplyText);
                sb.Append("<BR><BR>");
            }

            if (LikelyFiles.Count > 0)
            {
                sb.Append("<BASEFONT COLOR=#1E4FA3>Likely Files / Systems</BASEFONT><BR>");
                for (int i = 0; i < LikelyFiles.Count; i++)
                    sb.AppendFormat("- {0}<BR>", Utility.FixHtml(LikelyFiles[i]));
                sb.Append("<BR>");
            }

            if (SuggestedChecks.Count > 0)
            {
                sb.Append("<BASEFONT COLOR=#2E7D32>Suggested Next Checks</BASEFONT><BR>");
                for (int i = 0; i < SuggestedChecks.Count; i++)
                    sb.AppendFormat("- {0}<BR>", Utility.FixHtml(SuggestedChecks[i]));
                sb.Append("<BR>");
            }

            if (Warnings.Count > 0)
            {
                sb.Append("<BASEFONT COLOR=#B33939>Warnings</BASEFONT><BR>");
                for (int i = 0; i < Warnings.Count; i++)
                    sb.AppendFormat("- {0}<BR>", Utility.FixHtml(Warnings[i]));
                sb.Append("<BR>");
            }

            if (ProposedActions.Count > 0)
            {
                sb.Append("<BASEFONT COLOR=#6A3FB5>Proposed Actions</BASEFONT><BR>");
                for (int i = 0; i < ProposedActions.Count; i++)
                    sb.AppendFormat("- [{0}] {1}<BR>", Utility.FixHtml(FormatCategory(ProposedActions[i].Category)), Utility.FixHtml(ProposedActions[i].Description));
                sb.Append("<BR>");
            }

            if (!String.IsNullOrWhiteSpace(Confidence))
                sb.AppendFormat("<BASEFONT COLOR=#555555>Confidence: {0}</BASEFONT><BR>", Utility.FixHtml(Confidence));

            if (!String.IsNullOrWhiteSpace(ErrorMessage))
                sb.AppendFormat("<BR><BASEFONT COLOR=#B00020>Error: {0}</BASEFONT>", Utility.FixHtml(ErrorMessage));

            sb.Append("</BASEFONT>");
            return sb.ToString();
        }
    }
}
