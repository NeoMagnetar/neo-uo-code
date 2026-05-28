using System.Collections.Generic;

namespace Server.Custom.AIGM
{
    public class AIGMActionProposal
    {
        public string ActionKind { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string PreviewText { get; set; }
        public Dictionary<string, string> Parameters { get; set; }
        public bool RequiresConfirmation { get; set; }

        public AIGMActionProposal()
        {
            Category = "read";
            Parameters = new Dictionary<string, string>();
            RequiresConfirmation = true;
        }
    }
}
