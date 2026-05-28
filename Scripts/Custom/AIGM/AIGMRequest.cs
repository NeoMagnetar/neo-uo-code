using System;

namespace Server.Custom.AIGM
{
    public class AIGMRequest
    {
        public string RequestId { get; set; }
        public string TimestampUtc { get; set; }
        public string ShardName { get; set; }
        public string RequesterName { get; set; }
        public string AccessLevel { get; set; }
        public string MapName { get; set; }
        public string RegionName { get; set; }
        public string Question { get; set; }
        public AIGMTargetInfo Target { get; set; }
        public AIGMSceneContext Scene { get; set; }
    }
}
