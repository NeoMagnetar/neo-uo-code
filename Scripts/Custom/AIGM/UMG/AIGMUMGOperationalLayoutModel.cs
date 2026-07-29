using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Server.Custom.AIGM.UMG
{
    public enum AIGMUMGOperationalLayoutState
    {
        UnsavedDefaultLayout = 0,
        Draft = 1,
        ApprovedPreviewOnly = 2
    }

    [DataContract]
    public sealed class AIGMUMGOperationalLayout
    {
        public const int CurrentSchemaVersion = 1;

        [DataMember(Order = 0)] public int SchemaVersion { get; set; }
        [DataMember(Order = 1)] public string LayoutId { get; set; }
        [DataMember(Order = 2)] public string ActorKey { get; set; }
        [DataMember(Order = 3)] public string ActorSerialAtSave { get; set; }
        [DataMember(Order = 4)] public string BaseAssignmentVersionId { get; set; }
        [DataMember(Order = 5)] public AIGMUMGOperationalLayoutState State { get; set; }
        [DataMember(Order = 6)] public AIGMUMGExecutionMode ExecutionMode { get; set; }
        [DataMember(Order = 7)] public int Revision { get; set; }
        [DataMember(Order = 8)] public List<AIGMUMGOperationalFamily> Families { get; set; }
        [DataMember(Order = 9)] public DateTime CreatedUtc { get; set; }
        [DataMember(Order = 10)] public string CreatedBy { get; set; }
        [DataMember(Order = 11)] public DateTime UpdatedUtc { get; set; }
        [DataMember(Order = 12)] public string UpdatedBy { get; set; }
        [DataMember(Order = 13)] public string Provenance { get; set; }

        public AIGMUMGOperationalLayout()
        {
            SchemaVersion = CurrentSchemaVersion;
            LayoutId = String.Empty;
            ActorKey = String.Empty;
            ActorSerialAtSave = String.Empty;
            BaseAssignmentVersionId = String.Empty;
            State = AIGMUMGOperationalLayoutState.UnsavedDefaultLayout;
            ExecutionMode = AIGMUMGExecutionMode.PreviewOnly;
            Revision = 0;
            Families = new List<AIGMUMGOperationalFamily>();
            CreatedUtc = DateTime.UtcNow;
            CreatedBy = String.Empty;
            UpdatedUtc = DateTime.UtcNow;
            UpdatedBy = String.Empty;
            Provenance = String.Empty;
        }
    }

    [DataContract]
    public sealed class AIGMUMGOperationalFamily
    {
        [DataMember(Order = 0)] public string FamilyId { get; set; }
        [DataMember(Order = 1)] public string DisplayName { get; set; }
        [DataMember(Order = 2)] public int Order { get; set; }
        [DataMember(Order = 3)] public bool Enabled { get; set; }
        [DataMember(Order = 4)] public bool Locked { get; set; }
        [DataMember(Order = 5)] public bool SystemDefined { get; set; }
        [DataMember(Order = 6)] public List<AIGMUMGOperationalNeoStack> OperationalNeoStacks { get; set; }

        public AIGMUMGOperationalFamily()
        {
            FamilyId = String.Empty;
            DisplayName = String.Empty;
            Order = 500;
            Enabled = true;
            Locked = false;
            SystemDefined = true;
            OperationalNeoStacks = new List<AIGMUMGOperationalNeoStack>();
        }
    }

    [DataContract]
    public sealed class AIGMUMGOperationalNeoStack
    {
        [DataMember(Order = 0)] public string StackId { get; set; }
        [DataMember(Order = 1)] public string DisplayName { get; set; }
        [DataMember(Order = 2)] public string FamilyId { get; set; }
        [DataMember(Order = 3)] public int Order { get; set; }
        [DataMember(Order = 4)] public bool Enabled { get; set; }
        [DataMember(Order = 5)] public bool Locked { get; set; }
        [DataMember(Order = 6)] public bool SystemDefined { get; set; }
        [DataMember(Order = 7)] public List<AIGMUMGNeoBlockReference> NeoBlockReferences { get; set; }

        public AIGMUMGOperationalNeoStack()
        {
            StackId = String.Empty;
            DisplayName = String.Empty;
            FamilyId = String.Empty;
            Order = 500;
            Enabled = true;
            Locked = false;
            SystemDefined = false;
            NeoBlockReferences = new List<AIGMUMGNeoBlockReference>();
        }
    }

    [DataContract]
    public sealed class AIGMUMGNeoBlockReference
    {
        [DataMember(Order = 0)] public string ReferenceId { get; set; }
        [DataMember(Order = 1)] public string DefinitionId { get; set; }
        [DataMember(Order = 2)] public string AssignmentId { get; set; }
        [DataMember(Order = 3)] public int Order { get; set; }
        [DataMember(Order = 4)] public bool Enabled { get; set; }
        [DataMember(Order = 5)] public bool Locked { get; set; }
        [DataMember(Order = 6)] public string Provenance { get; set; }
        [DataMember(Order = 7)] public List<AIGMUMGParameterValue> OptionalLocalParameters { get; set; }
        [DataMember(Order = 8)] public string Notes { get; set; }
        [DataMember(Order = 9)] public DateTime CreatedUtc { get; set; }
        [DataMember(Order = 10)] public string CreatedBy { get; set; }
        [DataMember(Order = 11)] public string ReferenceKind { get; set; }
        [DataMember(Order = 12)] public string MandatoryRole { get; set; }
        [DataMember(Order = 13)] public string DisplayName { get; set; }
        [DataMember(Order = 14)] public string CanonicalStack { get; set; }

        public AIGMUMGNeoBlockReference()
        {
            ReferenceId = String.Empty;
            DefinitionId = String.Empty;
            AssignmentId = String.Empty;
            Order = 500;
            Enabled = true;
            Locked = false;
            Provenance = String.Empty;
            OptionalLocalParameters = new List<AIGMUMGParameterValue>();
            Notes = String.Empty;
            CreatedUtc = DateTime.UtcNow;
            CreatedBy = String.Empty;
            ReferenceKind = "LibraryDefinition";
            MandatoryRole = String.Empty;
            DisplayName = String.Empty;
            CanonicalStack = String.Empty;
        }
    }

    [DataContract]
    public sealed class AIGMUMGOperationalLayoutPointer
    {
        [DataMember(Order = 0)] public string ActorKey { get; set; }
        [DataMember(Order = 1)] public string CurrentDraftVersionId { get; set; }
        [DataMember(Order = 2)] public string CurrentApprovedPreviewVersionId { get; set; }
        [DataMember(Order = 3)] public int LatestRevision { get; set; }
        [DataMember(Order = 4)] public DateTime UpdatedUtc { get; set; }
        [DataMember(Order = 5)] public string UpdatedBy { get; set; }

        public AIGMUMGOperationalLayoutPointer()
        {
            ActorKey = String.Empty;
            CurrentDraftVersionId = String.Empty;
            CurrentApprovedPreviewVersionId = String.Empty;
            LatestRevision = 0;
            UpdatedUtc = DateTime.UtcNow;
            UpdatedBy = String.Empty;
        }
    }

    [DataContract]
    public sealed class AIGMUMGOperationalLayoutPointerFile
    {
        [DataMember(Order = 0)] public int SchemaVersion { get; set; }
        [DataMember(Order = 1)] public List<AIGMUMGOperationalLayoutPointer> Entries { get; set; }

        public AIGMUMGOperationalLayoutPointerFile()
        {
            SchemaVersion = AIGMUMGOperationalLayout.CurrentSchemaVersion;
            Entries = new List<AIGMUMGOperationalLayoutPointer>();
        }
    }

    [DataContract]
    public sealed class AIGMUMGOperationalLayoutVersionRecord
    {
        [DataMember(Order = 0)] public string VersionId { get; set; }
        [DataMember(Order = 1)] public string ActorKey { get; set; }
        [DataMember(Order = 2)] public int Revision { get; set; }
        [DataMember(Order = 3)] public AIGMUMGOperationalLayoutState State { get; set; }
        [DataMember(Order = 4)] public AIGMUMGExecutionMode ExecutionMode { get; set; }
        [DataMember(Order = 5)] public string BaseAssignmentVersionId { get; set; }
        [DataMember(Order = 6)] public string Author { get; set; }
        [DataMember(Order = 7)] public DateTime TimestampUtc { get; set; }
        [DataMember(Order = 8)] public string Summary { get; set; }
        [DataMember(Order = 9)] public string CorrelationId { get; set; }
        [DataMember(Order = 10)] public string SourceVersionId { get; set; }
        [DataMember(Order = 11)] public AIGMUMGOperationalLayout LayoutSnapshot { get; set; }

        public AIGMUMGOperationalLayoutVersionRecord()
        {
            VersionId = String.Empty;
            ActorKey = String.Empty;
            Revision = 0;
            State = AIGMUMGOperationalLayoutState.Draft;
            ExecutionMode = AIGMUMGExecutionMode.PreviewOnly;
            BaseAssignmentVersionId = String.Empty;
            Author = String.Empty;
            TimestampUtc = DateTime.UtcNow;
            Summary = String.Empty;
            CorrelationId = String.Empty;
            SourceVersionId = String.Empty;
        }
    }

    [DataContract]
    public sealed class AIGMUMGOperationalLayoutVersionFile
    {
        [DataMember(Order = 0)] public int SchemaVersion { get; set; }
        [DataMember(Order = 1)] public List<AIGMUMGOperationalLayoutVersionRecord> Entries { get; set; }

        public AIGMUMGOperationalLayoutVersionFile()
        {
            SchemaVersion = AIGMUMGOperationalLayout.CurrentSchemaVersion;
            Entries = new List<AIGMUMGOperationalLayoutVersionRecord>();
        }
    }

    public sealed class AIGMUMGOperationalLayoutValidationResult
    {
        public List<string> StructuralErrors { get; private set; }
        public List<string> Warnings { get; private set; }
        public List<string> HardConflicts { get; private set; }
        public List<string> SoftConflicts { get; private set; }
        public List<string> CapabilityNotes { get; private set; }

        public AIGMUMGOperationalLayoutValidationResult()
        {
            StructuralErrors = new List<string>();
            Warnings = new List<string>();
            HardConflicts = new List<string>();
            SoftConflicts = new List<string>();
            CapabilityNotes = new List<string>();
        }

        public bool CanSaveDraft
        {
            get { return StructuralErrors.Count == 0; }
        }

        public bool CanApprovePreviewOnly
        {
            get { return StructuralErrors.Count == 0 && HardConflicts.Count == 0; }
        }

        public string BuildCompactSummary()
        {
            return String.Format("errors={0}; warnings={1}; hardConflicts={2}; softConflicts={3}; capability={4}",
                StructuralErrors.Count,
                Warnings.Count,
                HardConflicts.Count,
                SoftConflicts.Count,
                CapabilityNotes.Count);
        }
    }

    public sealed class AIGMUMGOperationalLayoutEditSession
    {
        public string SessionId { get; set; }
        public Serial CallerSerial { get; set; }
        public string ActorKey { get; set; }
        public Serial ActorSerial { get; set; }
        public int BaseRevision { get; set; }
        public string BaseVersionId { get; set; }
        public AIGMUMGOperationalLayout WorkingLayout { get; set; }
        public bool Dirty { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime LastInteractionUtc { get; set; }
        public bool ArchitectMode { get; set; }
        public int Page { get; set; }
        public string View { get; set; }
        public string SelectedFamilyId { get; set; }
        public string SelectedStackId { get; set; }
        public string SelectedReferenceId { get; set; }
        public string CompareLeftVersionId { get; set; }
        public string CompareRightVersionId { get; set; }
        public string FilterText { get; set; }
        public List<string> ExpandedFamilyIds { get; private set; }
        public List<string> ExpandedStackIds { get; private set; }
        public string LastMessage { get; set; }
        public List<string> RenderedFamilyIds { get; private set; }
        public List<string> RenderedStackIds { get; private set; }
        public List<string> RenderedReferenceIds { get; private set; }
        public List<string> RenderedDefinitionIds { get; private set; }
        public List<string> RenderedVersionIds { get; private set; }

        public AIGMUMGOperationalLayoutEditSession()
        {
            SessionId = String.Empty;
            CallerSerial = Serial.MinusOne;
            ActorKey = String.Empty;
            ActorSerial = Serial.MinusOne;
            BaseVersionId = String.Empty;
            CreatedUtc = DateTime.UtcNow;
            LastInteractionUtc = DateTime.UtcNow;
            View = "Main";
            SelectedFamilyId = String.Empty;
            SelectedStackId = String.Empty;
            SelectedReferenceId = String.Empty;
            CompareLeftVersionId = String.Empty;
            CompareRightVersionId = String.Empty;
            FilterText = String.Empty;
            ExpandedFamilyIds = new List<string>();
            ExpandedStackIds = new List<string>();
            LastMessage = String.Empty;
            RenderedFamilyIds = new List<string>();
            RenderedStackIds = new List<string>();
            RenderedReferenceIds = new List<string>();
            RenderedDefinitionIds = new List<string>();
            RenderedVersionIds = new List<string>();
        }
    }
}
