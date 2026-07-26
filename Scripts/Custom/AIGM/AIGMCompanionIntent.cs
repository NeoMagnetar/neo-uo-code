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
        public const string TrackNPCs = "track_npcs";
        public const string TrackPlayers = "track_players";
        public const string RefusePlayerHunt = "refuse_player_hunt";
        public const string TrackAll = "track_all";
        public const string StartTrackingAnimals = "start_tracking_animals";
        public const string StartTrackingMonsters = "start_tracking_monsters";
        public const string StartTrackingNPCs = "start_tracking_npcs";
        public const string StartTrackingHumanNPCs = "start_tracking_human_npcs";
        public const string StartTrackingPlayers = "start_tracking_players";
        public const string StartTrackingAll = "start_tracking_all";
        public const string HuntAnimals = "hunt_animals";
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
        public const string StartMonsterHunt = "start_monster_hunt";
        public const string StopMonsterHunt = "stop_monster_hunt";
        public const string ReportMonsterHuntStatus = "report_monster_hunt_status";
        public const string LootNearby = "loot_nearby";
        public const string StopLooting = "stop_looting";
        public const string ReportLootStatus = "report_loot_status";
        public const string AutoLoot = "auto_loot";
        public const string StopAutoLoot = "stop_auto_loot";
        public const string ReportAutoLootStatus = "report_auto_loot_status";
        public const string ReportBurden = "report_burden";
        public const string UnloadJunk = "unload_junk";
        public const string PotionSupport = "potion_support";
        public const string StopPotionSupport = "stop_potion_support";
        public const string ReportPotionStatus = "report_potion_status";
        public const string UsePotion = "use_potion";
        public const string SpellSupport = "spell_support";
        public const string StopSpellSupport = "stop_spell_support";
        public const string ReportSpellStatus = "report_spell_status";
        public const string UseSpell = "use_spell";
    }

    public sealed class AIGMCompanionIntent
    {
        public string Kind;
        public int TargetSerial;
        public string RawText;
        public string DestinationName;
        public Point3D DestinationPoint;
        public Map DestinationMap;
        public bool AllowRemoteRelay;
        public bool ExplicitlyAddressed;
        public bool AddressedToDifferentCompanion;

        public bool HasTarget
        {
            get { return TargetSerial != 0; }
        }

        public bool HasDestinationPoint
        {
            get { return DestinationPoint != Point3D.Zero; }
        }
    }
}
