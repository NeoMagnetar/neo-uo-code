namespace Server.Custom.AIGM
{
    public enum AIGMTravelPathStrategy
    {
        DirectStep = 0,
        LocalAlternateStep = 1,
        PathFollower = 2,
        CommittedDetour = 3,
        BreadcrumbBacktrack = 4,
        WallFollow = 5,
        Stuck = 6
    }
}
