using System;
using System.Collections.Generic;

namespace Server.Custom.AIGM.Characters.Waylander
{
    public enum WaylanderRelationshipTimelineState
    {
        Current,
        Historical,
        Future,
        Deceased
    }

    public sealed class WaylanderRelationshipDefinition
    {
        public string RelatedCharacterId { get; private set; }
        public string RelationshipType { get; private set; }
        public string EmotionalStance { get; private set; }
        public int TrustLevel { get; private set; }
        public string CanonicalContext { get; private set; }
        public WaylanderRelationshipTimelineState TimelineState { get; private set; }
        public string[] DialogueFacts { get; private set; }
        public string[] UnknownFacts { get; private set; }

        public WaylanderRelationshipDefinition(
            string relatedCharacterId,
            string relationshipType,
            string emotionalStance,
            int trustLevel,
            string canonicalContext,
            WaylanderRelationshipTimelineState timelineState,
            string[] dialogueFacts,
            string[] unknownFacts)
        {
            RelatedCharacterId = Clean(relatedCharacterId);
            RelationshipType = Clean(relationshipType);
            EmotionalStance = Clean(emotionalStance);
            TrustLevel = trustLevel;
            CanonicalContext = Clean(canonicalContext);
            TimelineState = timelineState;
            DialogueFacts = CleanArray(dialogueFacts);
            UnknownFacts = CleanArray(unknownFacts);
        }

        public string BuildDialogueLine(string relatedDisplayName)
        {
            string name = String.IsNullOrWhiteSpace(relatedDisplayName) ? RelatedCharacterId : relatedDisplayName;
            string fact = DialogueFacts.Length > 0 ? DialogueFacts[0] : CanonicalContext;

            if (String.IsNullOrWhiteSpace(fact))
                fact = "Our histories cross, though I would choose the words carefully.";

            return String.Format("{0}? {1}", name, fact);
        }

        private static string Clean(string value)
        {
            return String.IsNullOrWhiteSpace(value) ? String.Empty : value.Trim();
        }

        private static string[] CleanArray(string[] values)
        {
            if (values == null || values.Length == 0)
                return Array.Empty<string>();

            List<string> clean = new List<string>();
            for (int i = 0; i < values.Length; i++)
            {
                string value = Clean(values[i]);
                if (!String.IsNullOrWhiteSpace(value))
                    clean.Add(value);
            }

            return clean.ToArray();
        }
    }
}
