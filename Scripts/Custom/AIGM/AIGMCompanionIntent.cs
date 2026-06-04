using Server;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionIntentKind
    {
        public const string FollowOwner = "follow_owner";
        public const string Stay = "stay";
        public const string Come = "come";
        public const string AttackTarget = "attack_target";
        public const string GuardOwner = "guard_owner";
        public const string BandageSelf = "bandage_self";
        public const string BandageOwner = "bandage_owner";
        public const string HealSelf = "heal_self";
        public const string HealOwner = "heal_owner";
        public const string StopCombat = "stop_combat";
        public const string CureSelf = "cure_self";
        public const string CureOwner = "cure_owner";
        public const string UseHealingSkill = "use_healing_skill";
        public const string UseBandages = "use_bandages";
        public const string CastHeal = "cast_heal";
        public const string CastCure = "cast_cure";
        public const string ReportLocation = "report_location";
        public const string ScanArea = "scan_area";
        public const string TrackAnimals = "track_animals";
        public const string TrackMonsters = "track_monsters";
        public const string TrackHumanNPCs = "track_human_npcs";
        public const string TrackPlayers = "track_players";
        public const string ReportThreats = "report_threats";
        public const string ShareAwareness = "share_awareness";
        public const string TravelToDestination = "travel_to_destination";
        public const string StopTravel = "stop_travel";
        public const string ReportTravelStatus = "report_travel_status";
        public const string ReturnHome = "return_home";
        public const string FollowCompanion = "follow_companion";
        public const string GreetCompanion = "greet_companion";
        public const string StartTrackingCycle = "start_tracking_cycle";
        public const string StopTrackingCycle = "stop_tracking_cycle";
        public const string ReportTrackingStatus = "report_tracking_status";
    }

    public sealed class AIGMCompanionIntent
    {
        public string Kind;
        public int TargetSerial;
        public string RawText;
        public string DestinationName;
        public bool AllowRemoteRelay;
        public bool ExplicitlyAddressed;
        public bool AddressedToDifferentCompanion;

        public bool HasTarget
        {
            get { return TargetSerial != 0; }
        }
    }
}
