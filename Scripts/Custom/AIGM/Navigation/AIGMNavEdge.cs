using System;
using System.Runtime.Serialization;

namespace Server.Custom.AIGM.Navigation
{
    public enum AIGMNavEdgeStatus
    {
        Unverified,
        Certified,
        Blocked,
        NeedsIntermediateNode,
        OneWay
    }

    [DataContract]
    public sealed class AIGMNavEdge
    {
        [DataMember(Name = "fromNodeId")]
        public string FromNodeId;

        [DataMember(Name = "toNodeId")]
        public string ToNodeId;

        [DataMember(Name = "cost")]
        public double Cost;

        [DataMember(Name = "bidirectional")]
        public bool Bidirectional;

        [DataMember(Name = "enabled")]
        public bool Enabled;

        [DataMember(Name = "corridorRadius")]
        public int CorridorRadius;

        [DataMember(Name = "tags")]
        public string[] Tags;

        [DataMember(Name = "failureScore")]
        public int FailureScore;

        [DataMember(Name = "tempBlockedUntilUtc")]
        public string TempBlockedUntilUtc;

        [DataMember(Name = "status")]
        public string StatusName;

        public AIGMNavEdge()
        {
            Enabled = true;
            Bidirectional = true;
            CorridorRadius = 4;
            Tags = new string[0];
            Cost = 0.0;
            StatusName = AIGMNavEdgeStatus.Unverified.ToString();
        }

        public DateTime TempBlockedUntil
        {
            get
            {
                DateTime value;
                return DateTime.TryParse(TempBlockedUntilUtc, out value) ? value.ToUniversalTime() : DateTime.MinValue;
            }
            set { TempBlockedUntilUtc = value <= DateTime.MinValue ? null : value.ToUniversalTime().ToString("o"); }
        }

        public bool IsTemporarilyBlocked
        {
            get { return TempBlockedUntil > DateTime.UtcNow; }
        }

        public AIGMNavEdgeStatus Status
        {
            get
            {
                AIGMNavEdgeStatus value;
                return Enum.TryParse(StatusName, true, out value) ? value : AIGMNavEdgeStatus.Unverified;
            }
            set
            {
                StatusName = value.ToString();
            }
        }

        public string EffectiveStatusName
        {
            get
            {
                if (IsTemporarilyBlocked)
                    return "TemporaryBlocked";

                return Status.ToString();
            }
        }

        public bool IsPlannerBlocked(bool allowUnverifiedEdges)
        {
            if (!Enabled || IsTemporarilyBlocked)
                return true;

            AIGMNavEdgeStatus status = Status;
            if (status == AIGMNavEdgeStatus.Blocked || status == AIGMNavEdgeStatus.NeedsIntermediateNode)
                return true;

            if (!allowUnverifiedEdges && status == AIGMNavEdgeStatus.Unverified)
                return true;

            return false;
        }

        public bool Connects(string from, string to)
        {
            if (String.Equals(FromNodeId, from, StringComparison.OrdinalIgnoreCase) && String.Equals(ToNodeId, to, StringComparison.OrdinalIgnoreCase))
                return true;

            return Bidirectional && String.Equals(FromNodeId, to, StringComparison.OrdinalIgnoreCase) && String.Equals(ToNodeId, from, StringComparison.OrdinalIgnoreCase);
        }
    }
}
