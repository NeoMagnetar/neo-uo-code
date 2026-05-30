namespace Server.Custom.AIGM
{
    public static class AIGMCommandAction
    {
        public const string TeleportToTarget = "teleport_to_target";
        public const string OpenPropsOnTarget = "open_props_on_target";
        public const string RestockVendor = "restock_vendor";
        public const string ViewEquipOnTarget = "view_equip_on_target";
        public const string SpawnTestCopy = "spawn_test_copy";
        public const string CleanupTestCopies = "cleanup_test_copies";
        public const string ListTestCopies = "list_test_copies";
        public const string InspectNearbyMobiles = "inspect_nearby_mobiles";
        public const string InspectNearbyItems = "inspect_nearby_items";
        public const string InspectRegion = "inspect_region";
        public const string ScanAroundTarget = "scan_around_target";
        public const string GoToCoordinates = "goto_coordinates";
        public const string InspectNearestVendor = "inspect_nearest_vendor";
        public const string GoToNearestVendor = "goto_nearest_vendor";
        public const string InspectNearestContainer = "inspect_nearest_container";
        public const string InspectNearestDoor = "inspect_nearest_door";
        public const string InspectNearestMobile = "inspect_nearest_mobile";
        public const string GoToNearestByType = "goto_nearest_by_type";
        public const string InspectDoorState = "inspect_door_state";
        public const string InspectVendorRuntimeStock = "inspect_vendor_runtime_stock";
        public const string ExecuteNextStep = "execute_next_step";
        public const string FollowMobile = "follow_mobile";
        public const string StopFollowing = "stop_following";
        public const string PathToCoordinates = "path_to_coordinates";
        public const string PathToNamedLocation = "path_to_named_location";
        public const string MovementStatus = "movement_status";
        public const string SetArrivalAction = "set_arrival_action";
        public const string PauseMovement = "pause_movement";
        public const string ResumeMovement = "resume_movement";
        public const string CancelMovement = "cancel_movement";
        public const string QueueNamedRouteStop = "queue_named_route_stop";
        public const string QueueCoordinateRouteStop = "queue_coordinate_route_stop";
        public const string PathToCurrentTarget = "path_to_current_target";
        public const string FollowCurrentTarget = "follow_current_target";
        public const string OpenCounselorPack = "open_counselor_pack";
        public const string SpawnItemToCounselorPack = "spawn_item_to_counselor_pack";
    }
}
