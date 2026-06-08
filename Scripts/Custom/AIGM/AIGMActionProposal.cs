using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Server.Custom.AIGM
{
    [DataContract]
    public class AIGMActionProposal
    {
        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "actionKind")]
        public string ActionKind { get; set; }

        [DataMember(Name = "description")]
        public string Description { get; set; }

        [DataMember(Name = "category")]
        public string Category { get; set; }

        [DataMember(Name = "previewText")]
        public string PreviewText { get; set; }

        [DataMember(Name = "parameters")]
        public Dictionary<string, string> Parameters { get; set; }

        [DataMember(Name = "requiresConfirmation")]
        public bool RequiresConfirmation { get; set; }

        public AIGMActionProposal()
        {
            Category = "read";
            Parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            RequiresConfirmation = true;
        }

        public void EnsureParameters()
        {
            if (Parameters == null)
                Parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public string GetParameter(string key, string fallback = null)
        {
            if (Parameters == null || String.IsNullOrWhiteSpace(key))
                return fallback;

            string value;
            return Parameters.TryGetValue(key, out value) ? value : fallback;
        }

        public int GetIntParameter(string key, int fallback)
        {
            int value;
            return Int32.TryParse(GetParameter(key), out value) ? value : fallback;
        }
    }
}
