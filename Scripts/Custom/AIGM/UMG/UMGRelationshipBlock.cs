using System;

namespace Server.Custom.AIGM.UMG
{
    public sealed class UMGRelationshipBlock
    {
        public string Id { get; private set; }
        public string SourceCharacter { get; private set; }
        public string TargetCharacter { get; private set; }
        public string RelationshipType { get; private set; }
        public int TrustLevel { get; private set; }
        public string TensionRule { get; private set; }
        public string SupportRule { get; private set; }
        public string ChallengeRule { get; private set; }
        public string SpeechBias { get; private set; }
        public string[] Tags { get; private set; }

        public UMGRelationshipBlock(string id, string sourceCharacter, string targetCharacter, string relationshipType, int trustLevel, string tensionRule, string supportRule, string challengeRule, string speechBias, string[] tags)
        {
            Id = Clean(id);
            SourceCharacter = Clean(sourceCharacter);
            TargetCharacter = Clean(targetCharacter);
            RelationshipType = Clean(relationshipType);
            TrustLevel = trustLevel;
            TensionRule = Clean(tensionRule);
            SupportRule = Clean(supportRule);
            ChallengeRule = Clean(challengeRule);
            SpeechBias = Clean(speechBias);
            Tags = CleanArray(tags);
        }

        public string FormatCompact()
        {
            return String.Format("Relationship {0}->{1} ({2}, trust {3}): support={4}; challenge={5}; tension={6}", SourceCharacter, TargetCharacter, RelationshipType, TrustLevel, SupportRule, ChallengeRule, TensionRule);
        }

        private static string Clean(string value)
        {
            return String.IsNullOrWhiteSpace(value) ? String.Empty : value.Trim();
        }

        private static string[] CleanArray(string[] values)
        {
            if (values == null || values.Length == 0)
                return new string[0];

            string[] clean = new string[values.Length];
            for (int i = 0; i < values.Length; i++)
                clean[i] = Clean(values[i]);

            return clean;
        }
    }
}
