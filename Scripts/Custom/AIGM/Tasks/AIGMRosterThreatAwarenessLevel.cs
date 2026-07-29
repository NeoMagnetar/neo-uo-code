namespace Server.Custom.AIGM.Tasks
{
    public enum AIGMRosterThreatAwarenessLevel
    {
        Unaware = 0,
        Suspicious,
        TrailDetected,
        EnemyLocated,
        UnderPursuit,
        ImmediateThreat
    }

    public enum AIGMRosterThreatResponse
    {
        None = 0,
        Fight,
        HoldGround,
        Flee,
        Hide,
        Reposition,
        SeekAllies,
        Regroup,
        ProtectMobile,
        ReturnHome,
        AlertSquad
    }
}
