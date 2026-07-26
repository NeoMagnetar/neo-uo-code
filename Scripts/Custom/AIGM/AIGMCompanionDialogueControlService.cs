using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Server;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionDialogueControlService
    {
        private static readonly ConcurrentDictionary<int, bool> QuietOwners = new ConcurrentDictionary<int, bool>();

        public static bool IsQuiet(Mobile owner)
        {
            if (owner == null)
                return false;

            bool quiet;
            return QuietOwners.TryGetValue(owner.Serial.Value, out quiet) && quiet;
        }

        public static bool TryHandleSpeechControl(IAIGMCompanionActor companion, Mobile speaker, string speech, AIGMCompanionCommandRouteDecision decision, out string visibleResponse)
        {
            visibleResponse = null;

            BaseHire hire = companion != null ? companion.Shell as BaseHire : null;
            Mobile owner = hire != null ? hire.GetOwner() : null;
            if (owner == null || speaker != owner)
                return false;

            string normalized = Normalize(speech);
            if (String.IsNullOrWhiteSpace(normalized))
                return false;

            if (IsQuietCommand(normalized))
            {
                QuietOwners[owner.Serial.Value] = true;
                SetLinkedCompanionMode(companion, owner, AIGMCompanionMode.DialogueQuietMode, "speech_control_quiet");
                AIGMCompanionDialogueThreadService.MarkQuiet(owner, normalized);
                if (ShouldAcknowledge(companion, decision))
                    visibleResponse = BuildQuietResponse(companion);
                return true;
            }

            if (IsFreeSpeechCommand(normalized))
            {
                bool removed;
                QuietOwners.TryRemove(owner.Serial.Value, out removed);
                ClearLinkedCompanionMode(companion, owner, AIGMCompanionMode.DialogueQuietMode, "speech_control_talk_freely");
                AIGMCompanionDialogueThreadService.ResumeFromQuiet(companion, owner, normalized);
                if (ShouldAcknowledge(companion, decision))
                    visibleResponse = BuildFreeSpeechResponse(companion);
                return true;
            }

            return false;
        }

        private static void SetLinkedCompanionMode(IAIGMCompanionActor companion, Mobile owner, AIGMCompanionMode mode, string reason)
        {
            List<IAIGMCompanionActor> companions = AIGMCompanionSpeechBus.GetLinkedCompanionsIncludingSource(companion, owner);
            if (companions == null || companions.Count == 0)
            {
                if (companion != null)
                    AIGMCompanionModeService.SetMode(companion.Shell, mode, reason);
                return;
            }

            for (int i = 0; i < companions.Count; i++)
            {
                if (companions[i] != null)
                    AIGMCompanionModeService.SetMode(companions[i].Shell, mode, reason);
            }
        }

        private static void ClearLinkedCompanionMode(IAIGMCompanionActor companion, Mobile owner, AIGMCompanionMode mode, string reason)
        {
            List<IAIGMCompanionActor> companions = AIGMCompanionSpeechBus.GetLinkedCompanionsIncludingSource(companion, owner);
            if (companions == null || companions.Count == 0)
            {
                if (companion != null)
                    AIGMCompanionModeService.ClearIfMode(companion.Shell, mode, reason);
                return;
            }

            for (int i = 0; i < companions.Count; i++)
            {
                if (companions[i] != null)
                    AIGMCompanionModeService.ClearIfMode(companions[i].Shell, mode, reason);
            }
        }

        public static bool ShouldSuppressCasualDialogue(IAIGMCompanionActor companion, Mobile speaker, string speech, AIGMCompanionCommandRouteDecision decision)
        {
            BaseHire hire = companion != null ? companion.Shell as BaseHire : null;
            Mobile owner = hire != null ? hire.GetOwner() : null;
            if (owner == null || speaker != owner || !IsQuiet(owner))
                return false;

            if (decision != null && decision.IsCompanionCommand)
                return false;

            return !IsUrgentSpeech(Normalize(speech));
        }

        private static bool ShouldAcknowledge(IAIGMCompanionActor companion, AIGMCompanionCommandRouteDecision decision)
        {
            if (companion == null)
                return false;

            if (decision != null && decision.RouteKind == AIGMCompanionCommandRouteKind.NamedCompanion)
                return String.Equals(decision.CompanionKey, companion.CompanionId, StringComparison.OrdinalIgnoreCase);

            return String.Equals(companion.CompanionId, "dakeyras", StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildQuietResponse(IAIGMCompanionActor companion)
        {
            string id = companion != null ? companion.CompanionId : String.Empty;
            if (String.Equals(id, "danyal", StringComparison.OrdinalIgnoreCase))
                return "Quiet, then. I will speak only when it matters.";
            if (String.Equals(id, "dardalion", StringComparison.OrdinalIgnoreCase))
                return "The line goes quiet. Necessary words only.";
            return "Quiet trail. I will save my breath for danger.";
        }

        private static string BuildFreeSpeechResponse(IAIGMCompanionActor companion)
        {
            string id = companion != null ? companion.CompanionId : String.Empty;
            if (String.Equals(id, "danyal", StringComparison.OrdinalIgnoreCase))
                return "Understood. I will speak when I see something worth saying.";
            if (String.Equals(id, "dardalion", StringComparison.OrdinalIgnoreCase))
                return "Then I will call the line as I see it.";
            return "Good. The trail has a tongue of its own.";
        }

        private static bool IsQuietCommand(string normalized)
        {
            return normalized == "quiet"
                || normalized == "shh"
                || normalized == "shhh"
                || normalized == "stop talking"
                || normalized == "enough talk"
                || normalized == "silence"
                || normalized == "hold your tongues"
                || normalized == "no chatter"
                || normalized == "hold chatter"
                || normalized == "companions quiet"
                || normalized == "companions stop talking"
                || normalized == "everyone quiet";
        }

        private static bool IsFreeSpeechCommand(string normalized)
        {
            return normalized == "talk freely"
                || normalized == "speak freely"
                || normalized == "you can talk"
                || normalized == "resume talking"
                || normalized == "keep talking"
                || normalized == "continue talking"
                || normalized == "you three keep talking"
                || normalized == "companions keep talking"
                || normalized == "companions keep discussing this"
                || normalized == "keep discussing this"
                || normalized == "let them talk"
                || normalized == "continue"
                || normalized == "what were you saying"
                || normalized == "companions talk freely"
                || normalized == "companions speak freely"
                || normalized == "everyone can talk";
        }

        private static bool IsUrgentSpeech(string normalized)
        {
            if (String.IsNullOrWhiteSpace(normalized))
                return false;

            return normalized.Contains("danger")
                || normalized.Contains("help")
                || normalized.Contains("attack")
                || normalized.Contains("hurt")
                || normalized.Contains("injured")
                || normalized.Contains("bleeding")
                || normalized.Contains("poison")
                || normalized.Contains("hostile")
                || normalized.Contains("enemy")
                || normalized.Contains("monster");
        }

        private static string Normalize(string speech)
        {
            if (String.IsNullOrWhiteSpace(speech))
                return String.Empty;

            string normalized = speech.Trim().ToLowerInvariant();
            normalized = normalized.Replace(",", " ").Replace(".", " ").Replace("!", " ").Replace("?", " ").Replace(";", " ").Replace(":", " ");
            while (normalized.Contains("  "))
                normalized = normalized.Replace("  ", " ");
            return normalized.Trim();
        }
    }
}
