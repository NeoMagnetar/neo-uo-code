using System;
using System.Collections.Generic;

namespace Server.Custom.AIGM
{
    public class AIGMSceneEntitySummary
    {
        public string Kind { get; set; }
        public int Serial { get; set; }
        public string Name { get; set; }
        public string TypeName { get; set; }
        public string MapName { get; set; }
        public string RegionName { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public int Distance { get; set; }
        public bool IsPlayer { get; set; }
        public bool IsNpc { get; set; }
        public bool IsVendor { get; set; }
        public bool IsContainer { get; set; }
        public bool IsDoor { get; set; }
        public bool IsStatic { get; set; }
        public bool IsMovable { get; set; }
        public bool IsAlive { get; set; }
        public bool Deleted { get; set; }
        public string ParentTypeName { get; set; }
        public List<string> Tags { get; set; }

        public AIGMSceneEntitySummary()
        {
            Tags = new List<string>();
        }
    }

    public class AIGMSceneContext
    {
        public string MapName { get; set; }
        public string RegionName { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public int ScanRange { get; set; }
        public List<AIGMSceneEntitySummary> NearbyMobiles { get; set; }
        public List<AIGMSceneEntitySummary> NearbyItems { get; set; }

        public AIGMSceneContext()
        {
            NearbyMobiles = new List<AIGMSceneEntitySummary>();
            NearbyItems = new List<AIGMSceneEntitySummary>();
        }
    }
}
