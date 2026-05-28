using System;

namespace Server.Custom.AIGM
{
    public class AIGMTargetInfo
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

        public string ToSummaryString()
        {
            if (String.IsNullOrWhiteSpace(TypeName) && String.IsNullOrWhiteSpace(Name))
                return "No target selected.";

            string label = !String.IsNullOrWhiteSpace(Name) ? Name : TypeName;
            return String.Format("{0} [{1}] at {2},{3},{4} ({5}/{6})", label, TypeName ?? "Unknown", X, Y, Z, MapName ?? "Unknown", RegionName ?? "Unknown");
        }
    }
}
