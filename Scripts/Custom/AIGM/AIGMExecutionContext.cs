using System;

namespace Server.Custom.AIGM
{
    public class AIGMExecutionContext
    {
        public string Mode { get; set; }
        public string LastActionDescription { get; set; }
        public string LastActionResult { get; set; }
        public int StepCount { get; set; }
    }
}
