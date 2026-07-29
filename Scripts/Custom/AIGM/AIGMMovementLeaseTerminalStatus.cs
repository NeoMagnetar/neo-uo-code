using System;

namespace Server.Custom.AIGM
{
    public enum AIGMMovementLeaseTerminalStatus
    {
        Active,
        Released,
        Completed,
        Blocked,
        Invalid,
        InterruptedStop,
        InterruptedFollow,
        InterruptedHunt,
        Rejected
    }
}
