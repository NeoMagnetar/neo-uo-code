using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Server.Custom.AIGM.UMG
{
    [DataContract]
    public sealed class AIGMUMGParameterDefinition
    {
        [DataMember(Order = 0)] public string ParameterId { get; set; }
        [DataMember(Order = 1)] public string Label { get; set; }
        [DataMember(Order = 2)] public string Description { get; set; }
        [DataMember(Order = 3)] public AIGMUMGParameterType Type { get; set; }
        [DataMember(Order = 4)] public bool Required { get; set; }
        [DataMember(Order = 5)] public string Default { get; set; }
        [DataMember(Order = 6)] public string Minimum { get; set; }
        [DataMember(Order = 7)] public string Maximum { get; set; }
        [DataMember(Order = 8)] public List<string> AllowedValues { get; set; }
        [DataMember(Order = 9)] public string Unit { get; set; }
        [DataMember(Order = 10)] public AIGMUMGCapabilityKind? CapabilityDependency { get; set; }
        [DataMember(Order = 11)] public string ValidationRule { get; set; }
        [DataMember(Order = 12)] public bool VisibleInOperatorMode { get; set; }
        [DataMember(Order = 13)] public bool VisibleInArchitectMode { get; set; }

        public AIGMUMGParameterDefinition()
        {
            ParameterId = String.Empty;
            Label = String.Empty;
            Description = String.Empty;
            Default = String.Empty;
            Minimum = String.Empty;
            Maximum = String.Empty;
            AllowedValues = new List<string>();
            Unit = String.Empty;
            ValidationRule = String.Empty;
            VisibleInOperatorMode = true;
            VisibleInArchitectMode = true;
        }
    }

    [DataContract]
    public sealed class AIGMUMGParameterValue
    {
        [DataMember(Order = 0)] public string ParameterId { get; set; }
        [DataMember(Order = 1)] public string Value { get; set; }

        public AIGMUMGParameterValue()
        {
            ParameterId = String.Empty;
            Value = String.Empty;
        }
    }

    [DataContract]
    public sealed class AIGMUMGLibraryDefinition
    {
        [DataMember(Order = 0)] public string DefinitionId { get; set; }
        [DataMember(Order = 1)] public int Version { get; set; }
        [DataMember(Order = 2)] public string Name { get; set; }
        [DataMember(Order = 3)] public string Summary { get; set; }
        [DataMember(Order = 4)] public string Category { get; set; }
        [DataMember(Order = 5)] public List<string> Tags { get; set; }
        [DataMember(Order = 6)] public AIGMUMGNeoStackKind IntendedNeoStack { get; set; }
        [DataMember(Order = 7)] public AIGMUMGNeoBlock NeoBlock { get; set; }
        [DataMember(Order = 8)] public List<AIGMUMGParameterDefinition> Parameters { get; set; }
        [DataMember(Order = 9)] public List<AIGMCapabilityRequirement> CapabilityRequirements { get; set; }
        [DataMember(Order = 10)] public List<AIGMUMGTargetScope> SupportedScopes { get; set; }
        [DataMember(Order = 11)] public List<string> Conflicts { get; set; }
        [DataMember(Order = 12)] public List<string> Dependencies { get; set; }
        [DataMember(Order = 13)] public string Fallback { get; set; }
        [DataMember(Order = 14)] public List<string> RecommendedRoles { get; set; }
        [DataMember(Order = 15)] public string KnownLimitations { get; set; }
        [DataMember(Order = 16)] public AIGMUMGExecutionMode DefaultExecutionMode { get; set; }
        [DataMember(Order = 17)] public string TemplateSourceProvenance { get; set; }
        [DataMember(Order = 18)] public DateTime CreatedUtc { get; set; }
        [DataMember(Order = 19)] public DateTime ModifiedUtc { get; set; }

        public AIGMUMGLibraryDefinition()
        {
            DefinitionId = String.Empty;
            Version = 1;
            Name = String.Empty;
            Summary = String.Empty;
            Category = String.Empty;
            Tags = new List<string>();
            NeoBlock = new AIGMUMGNeoBlock();
            Parameters = new List<AIGMUMGParameterDefinition>();
            CapabilityRequirements = new List<AIGMCapabilityRequirement>();
            SupportedScopes = new List<AIGMUMGTargetScope>();
            Conflicts = new List<string>();
            Dependencies = new List<string>();
            Fallback = "Report capability/conflict result and keep current deterministic behavior.";
            RecommendedRoles = new List<string>();
            KnownLimitations = "Preview authoring only. No tactical dispatch in Phase64C2.";
            DefaultExecutionMode = AIGMUMGExecutionMode.PreviewOnly;
            TemplateSourceProvenance = "Phase64C template migration";
            CreatedUtc = DateTime.UtcNow;
            ModifiedUtc = DateTime.UtcNow;
        }
    }

    [DataContract]
    public sealed class AIGMUMGAssignment
    {
        [DataMember(Order = 0)] public string AssignmentId { get; set; }
        [DataMember(Order = 1)] public string DefinitionId { get; set; }
        [DataMember(Order = 2)] public int DefinitionVersion { get; set; }
        [DataMember(Order = 3)] public AIGMUMGTargetScope TargetScope { get; set; }
        [DataMember(Order = 4)] public string TargetCanonicalId { get; set; }
        [DataMember(Order = 5)] public string TargetRuntimeSerial { get; set; }
        [DataMember(Order = 6)] public string NeoStackId { get; set; }
        [DataMember(Order = 7)] public AIGMUMGAssignmentState State { get; set; }
        [DataMember(Order = 8)] public AIGMUMGExecutionMode ExecutionMode { get; set; }
        [DataMember(Order = 9)] public AIGMUMGPreviewParticipation PreviewParticipation { get; set; }
        [DataMember(Order = 10)] public bool Enabled { get; set; }
        [DataMember(Order = 11)] public int PriorityOrder { get; set; }
        [DataMember(Order = 12)] public List<AIGMUMGParameterValue> Parameters { get; set; }
        [DataMember(Order = 13)] public string CreatedBy { get; set; }
        [DataMember(Order = 14)] public string ApprovedBy { get; set; }
        [DataMember(Order = 15)] public DateTime CreatedUtc { get; set; }
        [DataMember(Order = 16)] public DateTime ModifiedUtc { get; set; }
        [DataMember(Order = 17)] public DateTime? Expiry { get; set; }
        [DataMember(Order = 18)] public List<string> Dependencies { get; set; }
        [DataMember(Order = 19)] public List<string> Conflicts { get; set; }
        [DataMember(Order = 20)] public string CapabilitySnapshotVersion { get; set; }
        [DataMember(Order = 21)] public string Notes { get; set; }
        [DataMember(Order = 22)] public string SourceProposalId { get; set; }
        [DataMember(Order = 23)] public string ForkedDefinitionId { get; set; }

        public AIGMUMGAssignment()
        {
            AssignmentId = Guid.NewGuid().ToString("N");
            DefinitionId = String.Empty;
            DefinitionVersion = 1;
            TargetScope = AIGMUMGTargetScope.NPC;
            TargetCanonicalId = String.Empty;
            TargetRuntimeSerial = String.Empty;
            NeoStackId = String.Empty;
            State = AIGMUMGAssignmentState.Draft;
            ExecutionMode = AIGMUMGExecutionMode.PreviewOnly;
            PreviewParticipation = AIGMUMGPreviewParticipation.Inactive;
            Enabled = false;
            PriorityOrder = 640;
            Parameters = new List<AIGMUMGParameterValue>();
            CreatedBy = "unknown";
            ApprovedBy = String.Empty;
            CreatedUtc = DateTime.UtcNow;
            ModifiedUtc = DateTime.UtcNow;
            Dependencies = new List<string>();
            Conflicts = new List<string>();
            CapabilitySnapshotVersion = "runtime";
            Notes = String.Empty;
            SourceProposalId = String.Empty;
            ForkedDefinitionId = String.Empty;
        }
    }

    [DataContract]
    public sealed class AIGMUMGCompatibilityResult
    {
        [DataMember(Order = 0)] public string ResultId { get; set; }
        [DataMember(Order = 1)] public AIGMUMGCompatibilityState State { get; set; }
        [DataMember(Order = 2)] public List<string> RequirementsFound { get; set; }
        [DataMember(Order = 3)] public List<string> RequirementsMissing { get; set; }
        [DataMember(Order = 4)] public List<string> RequirementsDegraded { get; set; }
        [DataMember(Order = 5)] public List<string> RequirementsUnproven { get; set; }
        [DataMember(Order = 6)] public string Fallback { get; set; }
        [DataMember(Order = 7)] public string Explanation { get; set; }

        public AIGMUMGCompatibilityResult()
        {
            ResultId = Guid.NewGuid().ToString("N");
            State = AIGMUMGCompatibilityState.Unknown;
            RequirementsFound = new List<string>();
            RequirementsMissing = new List<string>();
            RequirementsDegraded = new List<string>();
            RequirementsUnproven = new List<string>();
            Fallback = String.Empty;
            Explanation = String.Empty;
        }

        public string BuildSummary()
        {
            return String.Format("{0}; found={1}; missing={2}; degraded={3}; unproven={4}; fallback={5}",
                State,
                RequirementsFound.Count,
                RequirementsMissing.Count,
                RequirementsDegraded.Count,
                RequirementsUnproven.Count,
                String.IsNullOrWhiteSpace(Fallback) ? "none" : Fallback);
        }
    }

    [DataContract]
    public sealed class AIGMUMGChangeSetItem
    {
        [DataMember(Order = 0)] public string ItemId { get; set; }
        [DataMember(Order = 1)] public string ItemType { get; set; }
        [DataMember(Order = 2)] public string Before { get; set; }
        [DataMember(Order = 3)] public string After { get; set; }

        public AIGMUMGChangeSetItem()
        {
            ItemId = String.Empty;
            ItemType = String.Empty;
            Before = String.Empty;
            After = String.Empty;
        }
    }

    [DataContract]
    public sealed class AIGMUMGChangeSet
    {
        [DataMember(Order = 0)] public string ChangeSetId { get; set; }
        [DataMember(Order = 1)] public string Target { get; set; }
        [DataMember(Order = 2)] public string Author { get; set; }
        [DataMember(Order = 3)] public string Source { get; set; }
        [DataMember(Order = 4)] public DateTime TimestampUtc { get; set; }
        [DataMember(Order = 5)] public List<string> AffectedIds { get; set; }
        [DataMember(Order = 6)] public List<AIGMUMGChangeSetItem> Items { get; set; }
        [DataMember(Order = 7)] public string Validation { get; set; }
        [DataMember(Order = 8)] public string Compatibility { get; set; }
        [DataMember(Order = 9)] public string Conflicts { get; set; }
        [DataMember(Order = 10)] public string ApprovalRequirement { get; set; }
        [DataMember(Order = 11)] public string Confirmation { get; set; }
        [DataMember(Order = 12)] public string Version { get; set; }
        [DataMember(Order = 13)] public string RollbackTarget { get; set; }

        public AIGMUMGChangeSet()
        {
            ChangeSetId = Guid.NewGuid().ToString("N");
            Target = String.Empty;
            Author = String.Empty;
            Source = String.Empty;
            TimestampUtc = DateTime.UtcNow;
            AffectedIds = new List<string>();
            Items = new List<AIGMUMGChangeSetItem>();
            Validation = String.Empty;
            Compatibility = String.Empty;
            Conflicts = String.Empty;
            ApprovalRequirement = String.Empty;
            Confirmation = String.Empty;
            Version = String.Empty;
            RollbackTarget = String.Empty;
        }
    }

    [DataContract]
    public sealed class AIGMUMGApprovalRecord
    {
        [DataMember(Order = 0)] public string ApprovalId { get; set; }
        [DataMember(Order = 1)] public string AssignmentId { get; set; }
        [DataMember(Order = 2)] public string ActorId { get; set; }
        [DataMember(Order = 3)] public string Author { get; set; }
        [DataMember(Order = 4)] public string Approver { get; set; }
        [DataMember(Order = 5)] public string Decision { get; set; }
        [DataMember(Order = 6)] public string Reason { get; set; }
        [DataMember(Order = 7)] public DateTime TimestampUtc { get; set; }

        public AIGMUMGApprovalRecord()
        {
            ApprovalId = Guid.NewGuid().ToString("N");
            AssignmentId = String.Empty;
            ActorId = String.Empty;
            Author = String.Empty;
            Approver = String.Empty;
            Decision = String.Empty;
            Reason = String.Empty;
            TimestampUtc = DateTime.UtcNow;
        }
    }

    [DataContract]
    public sealed class AIGMUMGVersionRecord
    {
        [DataMember(Order = 0)] public string VersionId { get; set; }
        [DataMember(Order = 1)] public string TargetId { get; set; }
        [DataMember(Order = 2)] public string AssignmentId { get; set; }
        [DataMember(Order = 3)] public string DefinitionId { get; set; }
        [DataMember(Order = 4)] public int VersionNumber { get; set; }
        [DataMember(Order = 5)] public string ChangeSetId { get; set; }
        [DataMember(Order = 6)] public string Author { get; set; }
        [DataMember(Order = 7)] public string Summary { get; set; }
        [DataMember(Order = 8)] public DateTime TimestampUtc { get; set; }
        [DataMember(Order = 9)] public AIGMUMGAssignment AssignmentSnapshot { get; set; }

        public AIGMUMGVersionRecord()
        {
            VersionId = Guid.NewGuid().ToString("N");
            TargetId = String.Empty;
            AssignmentId = String.Empty;
            DefinitionId = String.Empty;
            VersionNumber = 1;
            ChangeSetId = String.Empty;
            Author = String.Empty;
            Summary = String.Empty;
            TimestampUtc = DateTime.UtcNow;
        }
    }
}
