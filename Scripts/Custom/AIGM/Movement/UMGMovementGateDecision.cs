namespace Server.Custom.AIGM
{
    public sealed class UMGMovementGateDecision
    {
        public UMGMovementGateKind GateKind { get; set; }
        public bool IsAllowed { get; set; }
        public string Reason { get; set; }
        public bool AllowsStateUpdate { get; set; }
        public bool AllowsLiveMovement { get; set; }
        public bool RequiresExplicitIntent { get; set; }
        public bool IsScanOnly { get; set; }
        public bool IsReportOnly { get; set; }
        public bool SuspendsActiveIntent { get; set; }
        public bool ClearsActiveIntent { get; set; }
        public bool PreservesDestination { get; set; }

        public static UMGMovementGateDecision Allow(
            UMGMovementGateKind gateKind,
            string reason = null,
            bool allowsStateUpdate = true,
            bool allowsLiveMovement = false,
            bool requiresExplicitIntent = false,
            bool isScanOnly = false,
            bool isReportOnly = false,
            bool suspendsActiveIntent = false,
            bool clearsActiveIntent = false,
            bool preservesDestination = true)
        {
            return new UMGMovementGateDecision
            {
                GateKind = gateKind,
                IsAllowed = true,
                Reason = reason,
                AllowsStateUpdate = allowsStateUpdate,
                AllowsLiveMovement = allowsLiveMovement,
                RequiresExplicitIntent = requiresExplicitIntent,
                IsScanOnly = isScanOnly,
                IsReportOnly = isReportOnly,
                SuspendsActiveIntent = suspendsActiveIntent,
                ClearsActiveIntent = clearsActiveIntent,
                PreservesDestination = preservesDestination
            };
        }

        public static UMGMovementGateDecision Deny(
            UMGMovementGateKind gateKind,
            string reason,
            bool allowsStateUpdate = false,
            bool allowsLiveMovement = false,
            bool requiresExplicitIntent = false,
            bool isScanOnly = false,
            bool isReportOnly = false,
            bool suspendsActiveIntent = false,
            bool clearsActiveIntent = false,
            bool preservesDestination = true)
        {
            return new UMGMovementGateDecision
            {
                GateKind = gateKind,
                IsAllowed = false,
                Reason = reason,
                AllowsStateUpdate = allowsStateUpdate,
                AllowsLiveMovement = allowsLiveMovement,
                RequiresExplicitIntent = requiresExplicitIntent,
                IsScanOnly = isScanOnly,
                IsReportOnly = isReportOnly,
                SuspendsActiveIntent = suspendsActiveIntent,
                ClearsActiveIntent = clearsActiveIntent,
                PreservesDestination = preservesDestination
            };
        }
    }
}
