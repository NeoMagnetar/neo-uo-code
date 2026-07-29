namespace Server.Custom.AIGM
{
    public enum UMGMovementGateKind
    {
        MovementExecution = 0,
        Pursuit,
        TrackingScan,
        Travel,
        FollowGuard,
        Hold,
        Stop,
        Resume,
        CombatInterruption,
        StateAccessBoundary,
        ParserBoundary,
        SkillExecutorBoundary
    }
}
