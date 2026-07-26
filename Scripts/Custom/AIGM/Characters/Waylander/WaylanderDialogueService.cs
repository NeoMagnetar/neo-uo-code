using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Custom.AIGM.Characters.Waylander
{
    public static class WaylanderDialogueService
    {
        private static readonly string[] IdentityPrompts =
        {
            "who are you",
            "your name",
            "name yourself",
            "what are you"
        };

        private static readonly string[] FactionPrompts =
        {
            "who do you serve",
            "what faction",
            "what side",
            "whose side",
            "dark brotherhood",
            "the thirty",
            "wolfshead",
            "drenai",
            "gothir",
            "kuan hador"
        };

        private static readonly string[] PhilosophyPrompts =
        {
            "what do you believe",
            "what matters",
            "why do you fight",
            "what is your duty",
            "what is mercy",
            "what is honor",
            "what is courage",
            "what is loyalty",
            "philosophy"
        };

        public static string BuildReply(WaylanderCharacterDefinition definition, string rawSpeech)
        {
            if (definition == null || !definition.CanTalk || String.IsNullOrWhiteSpace(rawSpeech))
                return null;

            string speech = Normalize(rawSpeech);
            if (speech.Length == 0)
                return null;

            if (ContainsAny(speech, IdentityPrompts))
                return FirstNonEmpty(definition.IdentityLine, definition.DefaultReplyLine);

            string targetId = WaylanderRosterCatalog.ResolveCharacterIdFromSpeech(speech, definition.CharacterId);
            if (!String.IsNullOrWhiteSpace(targetId))
            {
                if (String.Equals(targetId, definition.CharacterId, StringComparison.OrdinalIgnoreCase)
                    || String.Equals(targetId, definition.CanonicalPersonId, StringComparison.OrdinalIgnoreCase))
                    return FirstNonEmpty(definition.IdentityLine, definition.DefaultReplyLine);

                WaylanderRelationshipDefinition relationship = definition.FindRelationship(targetId);
                if (relationship != null)
                {
                    WaylanderCharacterDefinition targetDefinition = WaylanderRosterCatalog.GetDefinition(targetId);
                    string relatedDisplayName = targetDefinition != null ? targetDefinition.VisibleName : targetId;
                    return relationship.BuildDialogueLine(relatedDisplayName);
                }

                string knownFact;
                if (definition.KnownCharacterFacts.TryGetValue(targetId, out knownFact) && !String.IsNullOrWhiteSpace(knownFact))
                    return knownFact;

                if (String.Equals(targetId, "dakeyras", StringComparison.OrdinalIgnoreCase))
                    return FirstNonEmpty(definition.WaylanderLine, definition.UnknownLine);
            }

            if (ContainsAny(speech, FactionPrompts) || ContainsAny(speech, definition.FactionKeywords))
                return FirstNonEmpty(definition.FactionLine, definition.UnknownLine);

            if (ContainsAny(speech, PhilosophyPrompts) || ContainsAny(speech, definition.PhilosophyKeywords))
                return FirstNonEmpty(definition.PhilosophyLine, definition.UnknownLine);

            if (speech.Contains("enemy") || speech.Contains("foe") || speech.Contains("hate"))
                return BuildEnemyReply(definition);

            return definition.DefaultReplyLine;
        }

        private static string BuildEnemyReply(WaylanderCharacterDefinition definition)
        {
            if (definition == null)
                return String.Empty;

            for (int i = 0; i < definition.EnemyIds.Length; i++)
            {
                WaylanderCharacterDefinition enemy = WaylanderRosterCatalog.GetDefinition(definition.EnemyIds[i]);
                if (enemy != null)
                    return String.Format("{0} stands against me, and I have not forgotten why.", enemy.VisibleName);
            }

            return definition.UnknownLine;
        }

        private static bool ContainsAny(string text, string[] probes)
        {
            if (String.IsNullOrWhiteSpace(text) || probes == null)
                return false;

            for (int i = 0; i < probes.Length; i++)
            {
                string probe = probes[i];
                if (!String.IsNullOrWhiteSpace(probe) && text.IndexOf(Normalize(probe), StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        private static string Normalize(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return String.Empty;

            StringBuilder sb = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char ch = Char.ToLowerInvariant(value[i]);
                if (Char.IsLetterOrDigit(ch) || Char.IsWhiteSpace(ch))
                    sb.Append(ch);
            }

            return sb.ToString().Trim();
        }

        private static string FirstNonEmpty(params string[] values)
        {
            if (values == null)
                return String.Empty;

            for (int i = 0; i < values.Length; i++)
            {
                if (!String.IsNullOrWhiteSpace(values[i]))
                    return values[i].Trim();
            }

            return String.Empty;
        }
    }
}
