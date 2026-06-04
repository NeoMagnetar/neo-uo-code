using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public enum AIGMTrackingCategory
    {
        Animals,
        Monsters,
        HumanNPCs,
        Players
    }

    public enum AIGMCompanionThreatLevel
    {
        Neutral,
        Interesting,
        PotentialThreat,
        ImmediateThreat,
        KnownAlly,
        KnownCompanion,
        Owner
    }

    public sealed class AIGMCompanionLocationSnapshot
    {
        public int CompanionSerial;
        public string CompanionName;
        public string MapName;
        public string RegionName;
        public int X;
        public int Y;
        public int Z;
        public string SextantText;
        public DateTime TimestampUtc;
    }

    public sealed class AIGMCompanionTrackingEntry
    {
        public int TargetSerial;
        public string Name;
        public string TypeName;
        public AIGMTrackingCategory Category;
        public int Distance;
        public string DirectionApprox;
        public string MapName;
        public int X;
        public int Y;
        public int Z;
        public bool HiddenKnown;
        public bool IsAlive;
        public AIGMCompanionThreatLevel ThreatHint;
        public DateTime TimestampUtc;
    }

    public sealed class AIGMCompanionTrackingSweep
    {
        public int CompanionSerial;
        public string CompanionName;
        public AIGMTrackingCategory Category;
        public int Range;
        public DateTime TimestampUtc;
        public List<AIGMCompanionTrackingEntry> Entries;

        public AIGMCompanionTrackingSweep()
        {
            Entries = new List<AIGMCompanionTrackingEntry>();
        }
    }

    public sealed class AIGMCompanionAwarenessEvent
    {
        public int SourceCompanionSerial;
        public string SourceCompanionName;
        public int TargetCompanionSerial;
        public string EventKind;
        public string Summary;
        public DateTime TimestampUtc;
    }
}

