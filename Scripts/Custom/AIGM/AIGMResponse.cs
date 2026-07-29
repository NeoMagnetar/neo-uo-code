using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Server.Custom.AIGM
{
    [DataContract]
    public class AIGMResponse
    {
        [DataMember(Name = "ok")]
        public bool Ok { get; set; }

        [DataMember(Name = "replyText")]
        public string ReplyText { get; set; }

        [DataMember(Name = "likelyFiles")]
        public List<string> LikelyFiles { get; set; }

        [DataMember(Name = "suggestedChecks")]
        public List<string> SuggestedChecks { get; set; }

        [DataMember(Name = "warnings")]
        public List<string> Warnings { get; set; }

        [DataMember(Name = "confidence")]
        public string Confidence { get; set; }

        [DataMember(Name = "responseSource")]
        public string ResponseSource { get; set; }

        [DataMember(Name = "source")]
        public string Source { get; set; }

        [DataMember(Name = "fallbackUsed")]
        public bool FallbackUsed { get; set; }

        [DataMember(Name = "degraded")]
        public bool Degraded { get; set; }

        [DataMember(Name = "errorMessage")]
        public string ErrorMessage { get; set; }

        [DataMember(Name = "investigationKind")]
        public string InvestigationKind { get; set; }

        [DataMember(Name = "proposedActions")]
        public List<AIGMActionProposal> ProposedActions { get; set; }

        [DataMember(Name = "plan")]
        public List<string> Plan { get; set; }

        [DataMember(Name = "nextAction")]
        public AIGMActionProposal NextAction { get; set; }

        [DataMember(Name = "stopReason")]
        public string StopReason { get; set; }

        public AIGMResponse()
        {
            LikelyFiles = new List<string>();
            SuggestedChecks = new List<string>();
            Warnings = new List<string>();
            ProposedActions = new List<AIGMActionProposal>();
            Plan = new List<string>();
        }

        private static string FormatCategory(string category)
        {
            if (String.IsNullOrWhiteSpace(category))
                return "Read";

            switch (category.Trim().ToLowerInvariant())
            {
                case "move": return "Move";
                case "navigate": return "Navigate";
                case "mutate": return "Mutate";
                default: return "Read";
            }
        }

        public string ToDisplayHtml()
        {
            try
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

                if (Plan.Count > 0)
                {
                    sb.Append("<BASEFONT COLOR=#8855CC>Plan</BASEFONT><BR>");
                    for (int i = 0; i < Plan.Count; i++)
                        sb.AppendFormat("{0}. {1}<BR>", i + 1, Utility.FixHtml(Plan[i]));
                    sb.Append("<BR>");
                }

                if (NextAction != null && !String.IsNullOrWhiteSpace(NextAction.Description))
                {
                    sb.Append("<BASEFONT COLOR=#7A4B00>Suggested Next Action</BASEFONT><BR>");
                    sb.AppendFormat("- {0}<BR><BR>", Utility.FixHtml(NextAction.Description));
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
                string html = sb.ToString();
                if (html.Length > 12000)
                    html = html.Substring(0, 12000) + "<BR><BR><BASEFONT COLOR=#B33939>Output truncated for gump safety.</BASEFONT>";
                return html;
            }
            catch (Exception ex)
            {
                return "<BASEFONT COLOR=#FF6666>AI GM response rendering failed: " + Utility.FixHtml(ex.Message) + "</BASEFONT>";
            }
        }
    }
}
