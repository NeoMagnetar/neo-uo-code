using System;

namespace Server.Custom.AIGM
{
    [Flags]
    public enum AIGMMovementInterruptionPolicy
    {
        None = 0,
        ExplicitOwnerCommandRequired = 1,
        HardPreempt = 2
    }
}
