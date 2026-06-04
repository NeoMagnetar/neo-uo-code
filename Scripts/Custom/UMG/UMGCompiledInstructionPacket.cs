using System;

namespace Server.Custom.UMG
{
    public sealed class UMGCompiledInstructionPacket
    {
        public string ProfileKey { get; set; }
        public string[] ActiveModeTags { get; set; }
        public string[] HardConstraints { get; set; }
        public string[] SoftDirectives { get; set; }
        public string[] Suppressions { get; set; }
        public string ReplyStyle { get; set; }
        public bool AllowAsyncReply { get; set; }
        public bool AllowDialoguePromotion { get; set; }
        public string CommandOwnershipHint { get; set; }
        public string DebugSummary { get; set; }

        public string ToCompactJson()
        {
            return "{" +
                "\"profileKey\":\"" + Escape(ProfileKey) + "\"," +
                "\"replyStyle\":\"" + Escape(ReplyStyle) + "\"," +
                "\"allowAsyncReply\":" + (AllowAsyncReply ? "true" : "false") + "," +
                "\"allowDialoguePromotion\":" + (AllowDialoguePromotion ? "true" : "false") + "," +
                "\"commandOwnershipHint\":\"" + Escape(CommandOwnershipHint) + "\"," +
                "\"debugSummary\":\"" + Escape(DebugSummary) + "\"" +
                "}";
        }

        private static string Escape(string value)
        {
            if (String.IsNullOrEmpty(value))
                return String.Empty;

            return value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", " ").Replace("\n", " ");
        }
    }
}
