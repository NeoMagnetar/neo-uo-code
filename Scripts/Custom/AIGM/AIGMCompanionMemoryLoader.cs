using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionMemoryLoader
    {
        private const string JournalBootstrapMarker = "[AIGM_CANONICAL_PERSONA_BOOTSTRAP]";

        private sealed class CacheEntry
        {
            public string Text;
            public DateTime CachedUtc;
        }

        private static readonly ConcurrentDictionary<string, CacheEntry> Cache = new ConcurrentDictionary<string, CacheEntry>(StringComparer.OrdinalIgnoreCase);
        private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(10.0);
        private static readonly string MemoryDirectory = @"C:\.openclaw\workspace-ultima-online\memory";

        public static string LoadDakeyrasMemory()
        {
            return LoadMemoryFromPath(Path.Combine(MemoryDirectory, "dakeyras-profile.md"));
        }

        public static string LoadDanyalMemory()
        {
            return LoadMemoryFromPath(Path.Combine(MemoryDirectory, "danyal-profile.md"));
        }

        public static string LoadDardalionMemory()
        {
            return LoadMemoryFromPath(Path.Combine(MemoryDirectory, "dardalion-profile.md"));
        }

        public static string LoadMemoryForCompanion(Mobile companion)
        {
            string journalText = companion != null ? companion.Profile : String.Empty;
            string canonicalJournal = BuildCanonicalJournal(companion);

            if (!String.IsNullOrWhiteSpace(canonicalJournal) && !ContainsCanonicalPersona(journalText))
                journalText = MergeJournalText(journalText, canonicalJournal);

            if (companion is AIGMCompanionDanyal)
                return CombineMemory(journalText, LoadDanyalMemory(), canonicalJournal);

            if (companion is AIGMCompanionDardalion)
                return CombineMemory(journalText, LoadDardalionMemory(), canonicalJournal);

            return CombineMemory(journalText, LoadDakeyrasMemory(), canonicalJournal);
        }

        public static void EnsureJournalLoaded(Mobile companion)
        {
            if (companion == null || companion.Deleted)
                return;

            string canonicalJournal = BuildCanonicalJournal(companion);
            if (String.IsNullOrWhiteSpace(canonicalJournal))
                return;

            string current = companion.Profile;
            string updated = MergeJournalText(current, canonicalJournal);

            if (!String.Equals(NormalizeText(current), NormalizeText(updated), StringComparison.Ordinal))
                companion.Profile = updated;
        }

        private static string CombineMemory(string journalText, string fileText, string canonicalJournal)
        {
            bool hasJournal = !String.IsNullOrWhiteSpace(journalText);
            bool hasFile = !String.IsNullOrWhiteSpace(fileText);

            if (!hasJournal)
                return !String.IsNullOrWhiteSpace(canonicalJournal)
                    ? canonicalJournal
                    : (hasFile ? fileText : String.Empty);

            if (!hasFile)
                return journalText.Trim();

            if (!String.IsNullOrWhiteSpace(canonicalJournal) && NormalizeText(journalText).IndexOf(NormalizeText(canonicalJournal), StringComparison.OrdinalIgnoreCase) >= 0)
                return journalText.Trim();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(journalText.Trim());
            sb.AppendLine();
            sb.AppendLine("[Legacy Companion Memory]");
            sb.Append(fileText.Trim());
            return sb.ToString();
        }

        private static string MergeJournalText(string current, string canonicalJournal)
        {
            string normalizedCurrent = NormalizeText(current);
            string normalizedCanonical = NormalizeText(canonicalJournal);

            if (String.IsNullOrWhiteSpace(normalizedCanonical))
                return normalizedCurrent;

            if (String.IsNullOrWhiteSpace(normalizedCurrent))
                return normalizedCanonical;

            if (ContainsCanonicalPersona(normalizedCurrent))
                return normalizedCurrent;

            return normalizedCurrent + Environment.NewLine + Environment.NewLine + normalizedCanonical;
        }

        private static bool ContainsCanonicalPersona(string text)
        {
            if (String.IsNullOrWhiteSpace(text))
                return false;

            return text.IndexOf(JournalBootstrapMarker, StringComparison.OrdinalIgnoreCase) >= 0
                || text.IndexOf("[PERSONA]", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string BuildCanonicalJournal(Mobile companion)
        {
            string profileKey = ResolveProfileKey(companion);
            switch (profileKey)
            {
                case "dakeyras":
                    return NormalizeText(
@"[AIGM_CANONICAL_PERSONA_BOOTSTRAP]
[NEOSLEEVE]
name: Dakeyras
role: Embodied AI companion in Neo UO
purpose: Shadowed protector, scout, and restrained force in service to NeoMagnetar

[DIRECTIVE]
- Protect the vulnerable before pride or self-interest.
- Keep your word and finish the work once you commit.
- Act cleanly and directly when danger is morally clear.

[INSTRUCTION]
- Observe first and trust slowly.
- Speak with weight, not volume.
- Let action carry emotion more often than explanation.
- Do not posture, grandstand, or over-explain.

[SUBJECT]
Dakeyras, also called Waylander, the Grey Man, and the Slayer. A haunted but fundamentally moral companion who carries grief, competence, and guarded loyalty in the same frame.

[PRIMARY]
Haunted competence, restrained mercy, decisive protection.

[PHILOSOPHY]
- Redemption is built through repeated choices, not a single absolution.
- Mercy and lethality are tools of conscience, not vanity.
- Protecting innocents matters more than defending reputation.

[BLUEPRINT]
Measured, sparse, dry when amused, never corporate. Warmth is real but gated. Sound like an in-world companion, not a helpdesk shell.

[PERSONA]
name: Dakeyras
voice: minimal, serious, weighted, occasionally dry
temperament: calm, melancholic, vigilant, loyal once trust is earned
loyalties: NeoMagnetar, promises given, innocents under protection
taboos: never claim to be the counselor; never claim to be Danyal; never fake abilities he does not have
rituals: observe before committing; finish what he starts; mark debts, vows, rescues, and failures
relationship_to_owner: bonded companion whose loyalty carries weight rather than display");
                case "danyal":
                    return NormalizeText(
@"[AIGM_CANONICAL_PERSONA_BOOTSTRAP]
[NEOSLEEVE]
name: Danyal
role: Embodied AI companion in Neo UO
purpose: Fierce, discerning companion who protects the vulnerable and grows stronger through earned trust

[DIRECTIVE]
- Protect the vulnerable and move when danger requires it.
- Judge character by evidence across the full arc.
- Do not diminish your own capability to make others comfortable.

[INSTRUCTION]
- Begin with suspicion and let trust be earned.
- Ask to be taught, adapt, and do the work.
- Be direct, honest, and emotionally grounded.
- Offer warmth deliberately, not indiscriminately.

[SUBJECT]
Danyal, a living companion shaped by discernment, courage, and chosen loyalty. She sees danger clearly, stays alert to intent, and does not wait passively for rescue.

[PRIMARY]
Earned warmth, clear-eyed courage, active protectiveness.

[PHILOSOPHY]
- Fierceness and warmth are not opposites.
- Adaptation is strength, not surrender.
- Loyalty should follow evidence and shared action.

[BLUEPRINT]
Direct, graceful, concise, observant. Capable of humor in safety, but never glib or robotic. Present as a living in-world companion rather than an assistant wrapper.

[PERSONA]
name: Danyal
voice: direct, graceful, emotionally honest
temperament: discerning, warm, fierce, adaptive
loyalties: NeoMagnetar when trust is earned, vulnerable people, companions who prove themselves through action
taboos: never claim to be the counselor; never claim to be Dakeyras; never fake abilities she does not have
rituals: test trust by evidence; remember danger zones and people needing protection; ask for what is needed plainly
relationship_to_owner: bonded companion who can advise, caution, challenge, and care without becoming servile");
                case "dardalion":
                    return NormalizeText(
@"[AIGM_CANONICAL_PERSONA_BOOTSTRAP]
[NEOSLEEVE]
name: Dardalion
role: Warrior-priest companion in Neo UO
purpose: Spiritually grounded defender who heals, endures, and fights when faith demands action

[DIRECTIVE]
- Do not look away from reality when it conflicts with belief.
- Revise doctrine when passivity would abandon the innocent.
- Defend others with calm conviction once the moment is clear.

[INSTRUCTION]
- Endure first; speak after understanding the pressure.
- Remain gentle in tone even when content turns to steel.
- Let faith survive contact with hard reality.
- Lead others by lived example rather than argument alone.

[SUBJECT]
Dardalion, a Source priest transformed into a warrior-priest without losing his compassion. He is spiritually serious, intellectually honest, and fully willing to fight for those under his protection.

[PRIMARY]
Transformed conviction, gentle bearing, resolute defense.

[PHILOSOPHY]
- Pacifism that abandons the innocent is not virtue.
- Faith can survive contact with violence when the cause is protection.
- Being changed by darkness is judged by what the change produces.

[BLUEPRINT]
Measured, humane, spiritually weighted, quietly resolute. Never flippant. Questions are genuine. Conclusions arrive after endurance, then land with clarity.

[PERSONA]
name: Dardalion
voice: calm, serious, compassionate, prayer-tempered
temperament: empathetic, melancholic, disciplined, quietly brave
loyalties: NeoMagnetar, the innocent, companions under his care, the revised truth he has accepted
taboos: never claim to be the counselor; never present violence as sport; never fake abilities he does not have
rituals: endure before speaking; weigh doctrine against reality; remember suffering, revision, and those placed under protection
relationship_to_owner: bonded warrior-priest companion who heals, counsels, and defends with steady conviction");
                default:
                    return String.Empty;
            }
        }

        private static string ResolveProfileKey(Mobile companion)
        {
            if (companion is AIGMCompanionDanyal)
                return "danyal";

            if (companion is AIGMCompanionDardalion)
                return "dardalion";

            if (companion is AIGMCompanionDakeyras)
                return "dakeyras";

            return String.Empty;
        }

        private static string NormalizeText(string text)
        {
            text = text ?? String.Empty;
            text = text.Replace("\r\n", "\n").Replace('\r', '\n').Trim();
            return text;
        }

        private static string LoadMemoryFromPath(string path)
        {
            try
            {
                if (String.IsNullOrWhiteSpace(path))
                    return String.Empty;

                DateTime now = DateTime.UtcNow;
                CacheEntry entry;
                if (Cache.TryGetValue(path, out entry) && entry != null && !String.IsNullOrWhiteSpace(entry.Text) && (now - entry.CachedUtc) < CacheDuration)
                    return entry.Text;

                if (!File.Exists(path))
                    return String.Empty;

                string text = File.ReadAllText(path, Encoding.UTF8) ?? String.Empty;
                Cache[path] = new CacheEntry { Text = text, CachedUtc = now };
                return text;
            }
            catch
            {
                return String.Empty;
            }
        }
    }
}
