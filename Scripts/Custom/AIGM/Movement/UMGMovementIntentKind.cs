namespace Server.Custom.AIGM
{
    public enum UMGMovementIntentKind
    {
        Idle = 0,
        FollowPlayer,
        HoldPosition,
        ReturnToPlayer,
        TravelToNamedDestination,
        MoveToPoint,
        PursueTrackedTarget,
        GuardTarget,
        RecoverFromStuck
    }
}
