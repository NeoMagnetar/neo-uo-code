namespace Server.Custom.AIGM
{
    public class AIGMExecutionResult
    {
        public bool Ok { get; private set; }
        public string Message { get; private set; }
        public Serial CreatedSerial { get; private set; }

        private AIGMExecutionResult(bool ok, string message, Serial createdSerial)
        {
            Ok = ok;
            Message = message;
            CreatedSerial = createdSerial;
        }

        public static AIGMExecutionResult Success(string message, Serial createdSerial)
        {
            return new AIGMExecutionResult(true, message, createdSerial);
        }

        public static AIGMExecutionResult Fail(string message)
        {
            return new AIGMExecutionResult(false, message, Serial.Zero);
        }
    }
}
