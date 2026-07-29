using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Server.Custom.AIGM.UMG
{
    public enum AIGMUMGActivationGraphNodeType
    {
        AlwaysOnNode = 0,
        FamilyNode = 1,
        OperationalStackNode = 2,
        NeoBlockReferenceNode = 3,
        TriggerProfileNode = 4,
        CapabilityGateNode = 5,
        GovernanceGateNode = 6,
        TypedIntentNode = 7,
        AdapterMappingNode = 8
    }

    public enum AIGMUMGSituationSnapshotSource
    {
        CurrentWorld = 0,
        SyntheticProof = 1,
        RecordedReplay = 2,
        UnitTest = 3
    }

    public enum AIGMUMGTriggerType
    {
        HostileDetected = 0,
        HostileMageDetected = 1,
        HostileWithinRange = 2,
        ActorHealthBelowThreshold = 3,
        AllyHealthBelowThreshold = 4,
        ProtecteeThreatened = 5,
        ManaBelowThreshold = 6,
        ManaAboveReleaseThreshold = 7,
        AmmunitionLow = 8,
        CurrentTargetLost = 9,
        CommanderHold = 10,
        CommanderStop = 11,
        CommanderFollow = 12,
        PassiveStandDown = 13,
        AbsoluteGMHold = 14,
        MissionCancelled = 15,
        NoThreatPresent = 16,
        CooldownExpired = 17
    }

    public enum AIGMUMGTriggerReentryRule
    {
        AllowAfterCooldown = 0,
        RequireReleaseThenCooldown = 1,
        StableWhileMatched = 2
    }

    public enum AIGMUMGPreviewBranchState
    {
        Dormant = 0,
        Eligible = 1,
        ActivePreview = 2,
        SuspendedBySelector = 3,
        BlockedByCapability = 4,
        BlockedByGovernance = 5,
        BlockedByConfiguration = 6,
        CoolingDown = 7,
        Expired = 8,
        Unconfigured = 9,
        InvalidReference = 10
    }

    public enum AIGMUMGCapabilityGateState
    {
        Ready = 0,
        Degraded = 1,
        Blocked = 2,
        Unproven = 3
    }

    public enum AIGMUMGTypedIntentCategory
    {
        AttackTargetPreview = 0,
        DefendAreaPreview = 1,
        ProtectTargetPreview = 2,
        HealTargetPreview = 3,
        RepositionPreview = 4,
        HoldPositionPreview = 5,
        RegroupPreview = 6,
        ConserveManaPreview = 7,
        ConserveAmmunitionPreview = 8,
        ObserveThreatPreview = 9,
        NoOpPreview = 10
    }

    public enum AIGMUMGRuntimeStateKind
    {
        COLD_RUNTIME_STATE = 0,
        WARM_RUNTIME_STATE = 1,
        CACHED_RESULT_REUSED = 2,
        DIAGNOSTIC_UNSAVED_LAYOUT = 3
    }

    [DataContract]
    public sealed class AIGMUMGKeyValue
    {
        [DataMember(Order = 0)] public string Key { get; set; }
        [DataMember(Order = 1)] public string Value { get; set; }

        public AIGMUMGKeyValue()
        {
            Key = String.Empty;
            Value = String.Empty;
        }

        public AIGMUMGKeyValue(string key, string value)
            : this()
        {
            Key = key ?? String.Empty;
            Value = value ?? String.Empty;
        }
    }

    [DataContract]
    public sealed class AIGMUMGActivationGraphValidationResult
    {
        [DataMember(Order = 0)] public bool IsValid { get; set; }
        [DataMember(Order = 1)] public List<string> Errors { get; set; }
        [DataMember(Order = 2)] public List<string> Warnings { get; set; }

        public AIGMUMGActivationGraphValidationResult()
        {
            IsValid = true;
            Errors = new List<string>();
            Warnings = new List<string>();
        }

        public void Error(string code)
        {
            if (!String.IsNullOrWhiteSpace(code))
                Errors.Add(code);
            IsValid = false;
        }

        public void Warn(string code)
        {
            if (!String.IsNullOrWhiteSpace(code))
                Warnings.Add(code);
        }
    }

    [DataContract]
    public sealed class AIGMUMGActivationGraphNode
    {
        [DataMember(Order = 0)] public string NodeId { get; set; }
        [DataMember(Order = 1)] public AIGMUMGActivationGraphNodeType NodeType { get; set; }
        [DataMember(Order = 2)] public string Label { get; set; }
        [DataMember(Order = 3)] public string FamilyId { get; set; }
        [DataMember(Order = 4)] public string StackId { get; set; }
        [DataMember(Order = 5)] public string ReferenceId { get; set; }
        [DataMember(Order = 6)] public string DefinitionId { get; set; }
        [DataMember(Order = 7)] public bool ConfigurationEnabled { get; set; }
        [DataMember(Order = 8)] public bool EligibleConfiguration { get; set; }
        [DataMember(Order = 9)] public List<AIGMUMGKeyValue> Details { get; set; }

        public AIGMUMGActivationGraphNode()
        {
            NodeId = String.Empty;
            Label = String.Empty;
            FamilyId = String.Empty;
            StackId = String.Empty;
            ReferenceId = String.Empty;
            DefinitionId = String.Empty;
            Details = new List<AIGMUMGKeyValue>();
        }
    }

    [DataContract]
    public sealed class AIGMUMGActivationGraphEdge
    {
        [DataMember(Order = 0)] public string FromNodeId { get; set; }
        [DataMember(Order = 1)] public string ToNodeId { get; set; }
        [DataMember(Order = 2)] public string EdgeKind { get; set; }

        public AIGMUMGActivationGraphEdge()
        {
            FromNodeId = String.Empty;
            ToNodeId = String.Empty;
            EdgeKind = String.Empty;
        }

        public AIGMUMGActivationGraphEdge(string fromNodeId, string toNodeId, string edgeKind)
            : this()
        {
            FromNodeId = fromNodeId ?? String.Empty;
            ToNodeId = toNodeId ?? String.Empty;
            EdgeKind = edgeKind ?? String.Empty;
        }
    }

    [DataContract]
    public sealed class AIGMUMGActivationGraph
    {
        [DataMember(Order = 0)] public string ActorSerial { get; set; }
        [DataMember(Order = 1)] public string ActorKey { get; set; }
        [DataMember(Order = 2)] public string LayoutId { get; set; }
        [DataMember(Order = 3)] public string LayoutVersionId { get; set; }
        [DataMember(Order = 4)] public int LayoutRevision { get; set; }
        [DataMember(Order = 5)] public string GraphVersion { get; set; }
        [DataMember(Order = 6)] public DateTime BuildTimestamp { get; set; }
        [DataMember(Order = 7)] public List<AIGMUMGActivationGraphNode> Nodes { get; set; }
        [DataMember(Order = 8)] public List<AIGMUMGActivationGraphEdge> DirectedEdges { get; set; }
        [DataMember(Order = 9)] public List<string> AlwaysOnNodeIds { get; set; }
        [DataMember(Order = 10)] public List<string> FamilyRootIds { get; set; }
        [DataMember(Order = 11)] public AIGMUMGActivationGraphValidationResult ValidationResult { get; set; }
        [DataMember(Order = 12)] public string GraphFingerprint { get; set; }

        public AIGMUMGActivationGraph()
        {
            ActorSerial = String.Empty;
            ActorKey = String.Empty;
            LayoutId = String.Empty;
            LayoutVersionId = String.Empty;
            GraphVersion = "Phase64D1E.ActivationGraph.v1";
            BuildTimestamp = DateTime.UtcNow;
            Nodes = new List<AIGMUMGActivationGraphNode>();
            DirectedEdges = new List<AIGMUMGActivationGraphEdge>();
            AlwaysOnNodeIds = new List<string>();
            FamilyRootIds = new List<string>();
            ValidationResult = new AIGMUMGActivationGraphValidationResult();
            GraphFingerprint = String.Empty;
        }
    }

    [DataContract]
    public sealed class AIGMUMGTriggerProfile
    {
        [DataMember(Order = 0)] public string TriggerId { get; set; }
        [DataMember(Order = 1)] public AIGMUMGTriggerType TriggerType { get; set; }
        [DataMember(Order = 2)] public double ActivationThreshold { get; set; }
        [DataMember(Order = 3)] public double ReleaseThreshold { get; set; }
        [DataMember(Order = 4)] public TimeSpan MinimumActiveDuration { get; set; }
        [DataMember(Order = 5)] public TimeSpan ReleaseStabilityDuration { get; set; }
        [DataMember(Order = 6)] public TimeSpan CooldownDuration { get; set; }
        [DataMember(Order = 7)] public AIGMUMGTriggerReentryRule ReentryRule { get; set; }
        [DataMember(Order = 8)] public int Severity { get; set; }
        [DataMember(Order = 9)] public string ApplicableFamily { get; set; }
        [DataMember(Order = 10)] public string ApplicableStackId { get; set; }
        [DataMember(Order = 11)] public string DefinitionId { get; set; }
        [DataMember(Order = 12)] public string Provenance { get; set; }

        public AIGMUMGTriggerProfile()
        {
            TriggerId = String.Empty;
            ApplicableFamily = String.Empty;
            ApplicableStackId = String.Empty;
            DefinitionId = String.Empty;
            Provenance = String.Empty;
            ReentryRule = AIGMUMGTriggerReentryRule.AllowAfterCooldown;
            MinimumActiveDuration = TimeSpan.Zero;
            ReleaseStabilityDuration = TimeSpan.Zero;
            CooldownDuration = TimeSpan.Zero;
        }
    }

    [DataContract]
    public sealed class AIGMUMGTriggerEvaluation
    {
        [DataMember(Order = 0)] public string TriggerId { get; set; }
        [DataMember(Order = 1)] public AIGMUMGTriggerType TriggerType { get; set; }
        [DataMember(Order = 2)] public bool Matched { get; set; }
        [DataMember(Order = 3)] public double ObservedValue { get; set; }
        [DataMember(Order = 4)] public double ActivationThreshold { get; set; }
        [DataMember(Order = 5)] public double ReleaseThreshold { get; set; }
        [DataMember(Order = 6)] public int Severity { get; set; }
        [DataMember(Order = 7)] public string Reason { get; set; }
        [DataMember(Order = 8)] public string Provenance { get; set; }

        public AIGMUMGTriggerEvaluation()
        {
            TriggerId = String.Empty;
            Reason = String.Empty;
            Provenance = String.Empty;
        }
    }

    [DataContract]
    public sealed class AIGMUMGCapabilityGateReceipt
    {
        [DataMember(Order = 0)] public AIGMUMGCapabilityGateState State { get; set; }
        [DataMember(Order = 1)] public List<string> Passed { get; set; }
        [DataMember(Order = 2)] public List<string> Failed { get; set; }
        [DataMember(Order = 3)] public List<string> Degraded { get; set; }
        [DataMember(Order = 4)] public List<string> Unproven { get; set; }

        public AIGMUMGCapabilityGateReceipt()
        {
            State = AIGMUMGCapabilityGateState.Unproven;
            Passed = new List<string>();
            Failed = new List<string>();
            Degraded = new List<string>();
            Unproven = new List<string>();
        }
    }

    [DataContract]
    public sealed class AIGMUMGGovernanceGateReceipt
    {
        [DataMember(Order = 0)] public bool Allowed { get; set; }
        [DataMember(Order = 1)] public string Result { get; set; }
        [DataMember(Order = 2)] public List<string> Reasons { get; set; }

        public AIGMUMGGovernanceGateReceipt()
        {
            Allowed = true;
            Result = "allowed";
            Reasons = new List<string>();
        }
    }

    [DataContract]
    public sealed class AIGMUMGDescentCandidateBranch
    {
        [DataMember(Order = 0)] public string FamilyId { get; set; }
        [DataMember(Order = 1)] public string FamilyName { get; set; }
        [DataMember(Order = 2)] public string StackId { get; set; }
        [DataMember(Order = 3)] public string StackName { get; set; }
        [DataMember(Order = 4)] public int StackOrder { get; set; }
        [DataMember(Order = 5)] public AIGMUMGPreviewBranchState State { get; set; }
        [DataMember(Order = 6)] public string SuspendedByStackId { get; set; }
        [DataMember(Order = 7)] public List<string> ReferenceIds { get; set; }
        [DataMember(Order = 8)] public List<string> DefinitionIds { get; set; }
        [DataMember(Order = 9)] public List<AIGMUMGTriggerEvaluation> MatchedTriggers { get; set; }
        [DataMember(Order = 10)] public List<AIGMUMGTriggerEvaluation> UnmatchedTriggers { get; set; }
        [DataMember(Order = 11)] public List<string> UnconfiguredTriggers { get; set; }
        [DataMember(Order = 12)] public AIGMUMGCapabilityGateReceipt Capability { get; set; }
        [DataMember(Order = 13)] public AIGMUMGGovernanceGateReceipt Governance { get; set; }
        [DataMember(Order = 14)] public string HysteresisState { get; set; }
        [DataMember(Order = 15)] public int Severity { get; set; }
        [DataMember(Order = 16)] public int Priority { get; set; }
        [DataMember(Order = 17)] public List<string> ComparisonFactors { get; set; }

        public AIGMUMGDescentCandidateBranch()
        {
            FamilyId = String.Empty;
            FamilyName = String.Empty;
            StackId = String.Empty;
            StackName = String.Empty;
            State = AIGMUMGPreviewBranchState.Dormant;
            SuspendedByStackId = String.Empty;
            ReferenceIds = new List<string>();
            DefinitionIds = new List<string>();
            MatchedTriggers = new List<AIGMUMGTriggerEvaluation>();
            UnmatchedTriggers = new List<AIGMUMGTriggerEvaluation>();
            UnconfiguredTriggers = new List<string>();
            Capability = new AIGMUMGCapabilityGateReceipt();
            Governance = new AIGMUMGGovernanceGateReceipt();
            HysteresisState = String.Empty;
            ComparisonFactors = new List<string>();
        }
    }

    [DataContract]
    public sealed class AIGMUMGFamilySelection
    {
        [DataMember(Order = 0)] public string FamilyId { get; set; }
        [DataMember(Order = 1)] public string SelectedStackId { get; set; }
        [DataMember(Order = 2)] public string SelectedStackName { get; set; }
        [DataMember(Order = 3)] public AIGMUMGPreviewBranchState State { get; set; }
        [DataMember(Order = 4)] public string Reason { get; set; }

        public AIGMUMGFamilySelection()
        {
            FamilyId = String.Empty;
            SelectedStackId = String.Empty;
            SelectedStackName = String.Empty;
            Reason = String.Empty;
        }
    }

    [DataContract]
    public sealed class AIGMUMGTypedIntentProposal
    {
        [DataMember(Order = 0)] public string IntentId { get; set; }
        [DataMember(Order = 1)] public AIGMUMGTypedIntentCategory Category { get; set; }
        [DataMember(Order = 2)] public string PrimaryFamilyId { get; set; }
        [DataMember(Order = 3)] public string SourceStackId { get; set; }
        [DataMember(Order = 4)] public List<string> SourceDefinitionIds { get; set; }
        [DataMember(Order = 5)] public List<AIGMUMGKeyValue> Parameters { get; set; }
        [DataMember(Order = 6)] public List<string> SupportingFamilies { get; set; }
        [DataMember(Order = 7)] public string ConflictResolution { get; set; }

        public AIGMUMGTypedIntentProposal()
        {
            IntentId = Guid.NewGuid().ToString("N");
            Category = AIGMUMGTypedIntentCategory.NoOpPreview;
            PrimaryFamilyId = String.Empty;
            SourceStackId = String.Empty;
            SourceDefinitionIds = new List<string>();
            Parameters = new List<AIGMUMGKeyValue>();
            SupportingFamilies = new List<string>();
            ConflictResolution = String.Empty;
        }
    }

    [DataContract]
    public sealed class AIGMUMGAdapterMappingReceipt
    {
        [DataMember(Order = 0)] public string AdapterId { get; set; }
        [DataMember(Order = 1)] public string AdapterName { get; set; }
        [DataMember(Order = 2)] public string MappingResult { get; set; }
        [DataMember(Order = 3)] public List<string> RequiredCapabilities { get; set; }
        [DataMember(Order = 4)] public bool InvocationAllowed { get; set; }
        [DataMember(Order = 5)] public bool InvocationAttempted { get; set; }

        public AIGMUMGAdapterMappingReceipt()
        {
            AdapterId = String.Empty;
            AdapterName = String.Empty;
            MappingResult = String.Empty;
            RequiredCapabilities = new List<string>();
            InvocationAllowed = false;
            InvocationAttempted = false;
        }
    }

    [DataContract]
    public sealed class AIGMUMGCognitionBudgetReceipt
    {
        [DataMember(Order = 0)] public int InstalledFamilies { get; set; }
        [DataMember(Order = 1)] public int InstalledStacks { get; set; }
        [DataMember(Order = 2)] public int InstalledReferences { get; set; }
        [DataMember(Order = 3)] public int EnabledReferences { get; set; }
        [DataMember(Order = 4)] public int TriggerProfilesEvaluated { get; set; }
        [DataMember(Order = 5)] public int EligibleBranches { get; set; }
        [DataMember(Order = 6)] public int ForegroundedBranches { get; set; }
        [DataMember(Order = 7)] public int CapabilityChecks { get; set; }
        [DataMember(Order = 8)] public int GovernanceChecks { get; set; }
        [DataMember(Order = 9)] public int SelectedBranches { get; set; }
        [DataMember(Order = 10)] public int CompiledReferences { get; set; }
        [DataMember(Order = 11)] public int TypedIntents { get; set; }
        [DataMember(Order = 12)] public int AdapterMappings { get; set; }
        [DataMember(Order = 13)] public int AdapterInvocationAttempts { get; set; }
        [DataMember(Order = 14)] public int AdapterInvocations { get; set; }
        [DataMember(Order = 15)] public int MaxFamilies { get; set; }
        [DataMember(Order = 16)] public int MaxStacks { get; set; }
        [DataMember(Order = 17)] public int MaxReferences { get; set; }
        [DataMember(Order = 18)] public int MaxTriggerProfiles { get; set; }
        [DataMember(Order = 19)] public bool BudgetExceeded { get; set; }

        public AIGMUMGCognitionBudgetReceipt()
        {
            MaxFamilies = 16;
            MaxStacks = 64;
            MaxReferences = 128;
            MaxTriggerProfiles = 128;
        }
    }

    [DataContract]
    public sealed class AIGMUMGDescentReceipt
    {
        [DataMember(Order = 0)] public string ReceiptId { get; set; }
        [DataMember(Order = 1)] public string CorrelationId { get; set; }
        [DataMember(Order = 2)] public DateTime CapturedUtc { get; set; }
        [DataMember(Order = 3)] public string ActorName { get; set; }
        [DataMember(Order = 4)] public string ActorSerial { get; set; }
        [DataMember(Order = 5)] public string ActorKey { get; set; }
        [DataMember(Order = 6)] public string SnapshotId { get; set; }
        [DataMember(Order = 7)] public AIGMUMGSituationSnapshotSource SnapshotSource { get; set; }
        [DataMember(Order = 8)] public string SnapshotScenarioId { get; set; }
        [DataMember(Order = 9)] public string ApprovedLayoutId { get; set; }
        [DataMember(Order = 10)] public string ApprovedLayoutVersionId { get; set; }
        [DataMember(Order = 11)] public int LayoutRevision { get; set; }
        [DataMember(Order = 12)] public string GraphVersion { get; set; }
        [DataMember(Order = 13)] public string GraphFingerprint { get; set; }
        [DataMember(Order = 14)] public string DecisionFingerprint { get; set; }
        [DataMember(Order = 15)] public AIGMUMGRuntimeStateKind RuntimeStateKind { get; set; }
        [DataMember(Order = 16)] public List<string> AlwaysOnNodes { get; set; }
        [DataMember(Order = 17)] public List<AIGMUMGTriggerEvaluation> MatchedTriggers { get; set; }
        [DataMember(Order = 18)] public List<AIGMUMGTriggerEvaluation> UnmatchedTriggers { get; set; }
        [DataMember(Order = 19)] public List<string> UnconfiguredTriggers { get; set; }
        [DataMember(Order = 20)] public List<AIGMUMGDescentCandidateBranch> CandidateBranches { get; set; }
        [DataMember(Order = 21)] public List<AIGMUMGFamilySelection> SelectedBranchByFamily { get; set; }
        [DataMember(Order = 22)] public List<string> SuspendedBranches { get; set; }
        [DataMember(Order = 23)] public List<string> CapabilityBlockedBranches { get; set; }
        [DataMember(Order = 24)] public List<string> GovernanceBlockedBranches { get; set; }
        [DataMember(Order = 25)] public List<string> CoolingDownBranches { get; set; }
        [DataMember(Order = 26)] public List<string> InvalidBranches { get; set; }
        [DataMember(Order = 27)] public List<string> SelectedNeoBlockReferences { get; set; }
        [DataMember(Order = 28)] public AIGMUMGTypedIntentProposal TypedIntent { get; set; }
        [DataMember(Order = 29)] public AIGMUMGAdapterMappingReceipt AdapterMapping { get; set; }
        [DataMember(Order = 30)] public AIGMUMGCognitionBudgetReceipt CognitionBudget { get; set; }
        [DataMember(Order = 31)] public List<string> Warnings { get; set; }
        [DataMember(Order = 32)] public List<string> Errors { get; set; }
        [DataMember(Order = 33)] public string FinalResult { get; set; }

        public AIGMUMGDescentReceipt()
        {
            ReceiptId = Guid.NewGuid().ToString("N");
            CorrelationId = String.Empty;
            CapturedUtc = DateTime.UtcNow;
            ActorName = String.Empty;
            ActorSerial = String.Empty;
            ActorKey = String.Empty;
            SnapshotId = String.Empty;
            SnapshotScenarioId = String.Empty;
            ApprovedLayoutId = String.Empty;
            ApprovedLayoutVersionId = String.Empty;
            GraphVersion = String.Empty;
            GraphFingerprint = String.Empty;
            DecisionFingerprint = String.Empty;
            RuntimeStateKind = AIGMUMGRuntimeStateKind.COLD_RUNTIME_STATE;
            AlwaysOnNodes = new List<string>();
            MatchedTriggers = new List<AIGMUMGTriggerEvaluation>();
            UnmatchedTriggers = new List<AIGMUMGTriggerEvaluation>();
            UnconfiguredTriggers = new List<string>();
            CandidateBranches = new List<AIGMUMGDescentCandidateBranch>();
            SelectedBranchByFamily = new List<AIGMUMGFamilySelection>();
            SuspendedBranches = new List<string>();
            CapabilityBlockedBranches = new List<string>();
            GovernanceBlockedBranches = new List<string>();
            CoolingDownBranches = new List<string>();
            InvalidBranches = new List<string>();
            SelectedNeoBlockReferences = new List<string>();
            TypedIntent = new AIGMUMGTypedIntentProposal();
            AdapterMapping = new AIGMUMGAdapterMappingReceipt();
            CognitionBudget = new AIGMUMGCognitionBudgetReceipt();
            Warnings = new List<string>();
            Errors = new List<string>();
            FinalResult = AIGMUMGPhase64C2Invariant.ExecutionStatus;
        }
    }
}
