namespace Server.Custom.AIGM
{
    public enum AIGMMovementControlMode
    {
        CounselorTemporaryControl = 0,
        CompanionPreserveControl = 1
    }

    public sealed class AIGMMovementStartOptions
    {
        public AIGMMovementControlMode ControlMode { get; set; }
        public bool RequireStaffControl { get; set; }
        public bool PreserveExistingControlMaster { get; set; }
        public bool AllowTeleportFallback { get; set; }
        public string Reason { get; set; }

        public static AIGMMovementStartOptions ForCompanionTravel()
        {
            return new AIGMMovementStartOptions
            {
                ControlMode = AIGMMovementControlMode.CompanionPreserveControl,
                RequireStaffControl = false,
                PreserveExistingControlMaster = true,
                AllowTeleportFallback = false,
                Reason = "companion_travel"
            };
        }

        public static AIGMMovementStartOptions ForCounselorPathing()
        {
            return new AIGMMovementStartOptions
            {
                ControlMode = AIGMMovementControlMode.CounselorTemporaryControl,
                RequireStaffControl = true,
                PreserveExistingControlMaster = false,
                AllowTeleportFallback = false,
                Reason = "counselor_pathing"
            };
        }
    }
}
