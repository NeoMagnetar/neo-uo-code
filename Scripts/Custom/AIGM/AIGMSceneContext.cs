using System;
using System.Collections.Generic;

namespace Server.Custom.AIGM
{
    public class AIGMSceneEntitySummary
    {
        public string Kind { get; set; }
        public string Name { get; set; }
        public string TypeName { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
    }

    public class AIGMSceneContext
    {
        public string MapName { get; set; }
        public string RegionName { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public List<AIGMSceneEntitySummary> NearbyMobiles { get; set; }
        public List<AIGMSceneEntitySummary> NearbyItems { get; set; }

        public AIGMSceneContext()
        {
            NearbyMobiles = new List<AIGMSceneEntitySummary>();
            NearbyItems = new List<AIGMSceneEntitySummary>();
        }
    }
}
