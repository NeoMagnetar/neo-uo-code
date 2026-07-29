using System;

using Server;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public sealed class AIGMMovementLease
    {
        public Serial CompanionSerial;
        public string LeaseId;
        public AIGMMovementLeaseOwnerType OwnerType;
        public Mobile Requester;
        public Serial RequesterSerial;
        public DateTime AcquiredUtc;
        public DateTime LastHeartbeatUtc;
        public string RouteContextId;
        public AIGMMovementInterruptionPolicy InterruptionPolicy;
        public AIGMMovementLeaseTerminalStatus TerminalStatus;
        public string LastReason;
    }
}
