using System;

namespace Server.Custom.AIGM
{
    public sealed class UMGNavigationEntitySnapshot
    {
        public string EntityId { get; set; }
        public string EntityKind { get; set; }
        public string DisplayName { get; set; }
        public string MapName { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public int Distance { get; set; }
        public bool IsHostile { get; set; }
        public bool IsPlayer { get; set; }
        public bool IsCreature { get; set; }
        public bool IsItem { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime SeenUtc { get; set; }
    }
}
