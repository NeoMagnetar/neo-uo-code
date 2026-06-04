namespace Server.Custom.AIGM
{
    public sealed class AIGMCompanionSpeechResult
    {
        public bool Ok { get; private set; }
        public string RequestId { get; private set; }
        public string ReplyText { get; private set; }
        public string ErrorMessage { get; private set; }

        private AIGMCompanionSpeechResult(bool ok, string requestId, string replyText, string errorMessage)
        {
            Ok = ok;
            RequestId = requestId;
            ReplyText = replyText;
            ErrorMessage = errorMessage;
        }

        public static AIGMCompanionSpeechResult Success(string requestId, string replyText)
        {
            return new AIGMCompanionSpeechResult(true, requestId, replyText, null);
        }

        public static AIGMCompanionSpeechResult Fail(string requestId, string errorMessage)
        {
            return new AIGMCompanionSpeechResult(false, requestId, null, errorMessage);
        }
    }
}
