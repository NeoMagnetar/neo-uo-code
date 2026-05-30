using System;
using System.Collections.Generic;

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
        public int Distance { get; set; }
        public bool IsPlayer { get; set; }
        public bool IsNpc { get; set; }
        public bool IsVendor { get; set; }
        public bool IsContainer { get; set; }
        public bool IsDoor { get; set; }
        public bool IsStatic { get; set; }
        public bool IsMovable { get; set; }
        public bool IsAlive { get; set; }
        public string ParentTypeName { get; set; }
        public List<string> Tags { get; set; }

        public AIGMTargetInfo()
        {
            Tags = new List<string>();
        }

        public string ToSummaryString()
        {
            if (String.IsNullOrWhiteSpace(TypeName) && String.IsNullOrWhiteSpace(Name))
                return "No target selected.";

            string label = !String.IsNullOrWhiteSpace(Name) ? Name : TypeName;
            string tags = Tags != null && Tags.Count > 0 ? String.Format(" {{{0}}}", String.Join(", ", Tags.ToArray())) : String.Empty;
            return String.Format("{0} [{1}] at {2},{3},{4} ({5}/{6}, d={7}){8}", label, TypeName ?? "Unknown", X, Y, Z, MapName ?? "Unknown", RegionName ?? "Unknown", Distance, tags);
        }
    }
}
