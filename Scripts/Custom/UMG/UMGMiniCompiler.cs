using System;
using Server.Custom.AIGM;
using Server.Mobiles;

namespace Server.Custom.UMG
{
    public static class UMGMiniCompiler
    {
        public static UMGCompiledInstructionPacket Compile(Mobile companion, Mobile speaker, string rawText)
        {
            UMGCompanionProfile profile = UMGCompanionProfile.ForCompanion(companion);
            string speech = rawText == null ? String.Empty : rawText.Trim();

            bool addressedToOtherCommand = false;
            BaseHire hire = companion as BaseHire;
            if (hire != null && !String.IsNullOrWhiteSpace(speech))
            {
                bool otherAddressed = AIGMCompanionIntentParser.IsClearlyAddressedToDifferentCompanion(hire, speech);
                addressedToOtherCommand = otherAddressed && LooksCommandLike(speech);
            }

            if (addressedToOtherCommand)
            {
                return new UMGCompiledInstructionPacket
                {
                    ProfileKey = profile.ProfileKey,
                    ActiveModeTags = new[] { "addressed_to_other_command", "aware_silent" },
                    HardConstraints = new[] { "do_not_answer_other_companion_commands" },
                    SoftDirectives = new[] { "Remain aware without answering." },
                    Suppressions = new[] { "no_async_reply", "no_dialogue_promotion" },
                    ReplyStyle = profile.SpeechStyle,
                    AllowAsyncReply = false,
                    AllowDialoguePromotion = false,
                    CommandOwnershipHint = "other",
                    DebugSummary = "Suppressed async reply because the speech appears to be a command addressed to another companion."
                };
            }

            return new UMGCompiledInstructionPacket
            {
                ProfileKey = profile.ProfileKey,
                ActiveModeTags = new[] { "default_companion_mode" },
                HardConstraints = profile.DefaultConstraints ?? new string[0],
                SoftDirectives = profile.DefaultDirectives ?? new string[0],
                Suppressions = new[] { "avoid_exact_echo" },
                ReplyStyle = profile.SpeechStyle,
                AllowAsyncReply = true,
                AllowDialoguePromotion = true,
                CommandOwnershipHint = "self_or_general",
                DebugSummary = "Default companion async behavior packet."
            };
        }

        private static bool LooksCommandLike(string speech)
        {
            if (String.IsNullOrWhiteSpace(speech))
                return false;

            string normalized = speech.Trim().ToLowerInvariant();
            return normalized.Contains(" go to ")
                || normalized.Contains(" travel ")
                || normalized.Contains(" follow ")
                || normalized.Contains(" stay")
                || normalized.Contains(" stop")
                || normalized.Contains(" track")
                || normalized.Contains(" guard")
                || normalized.Contains(" attack")
                || normalized.Contains(" heal")
                || normalized.Contains(" bandage");
        }
    }
}
