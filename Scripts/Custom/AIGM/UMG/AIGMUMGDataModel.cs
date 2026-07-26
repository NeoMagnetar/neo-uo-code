using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Server.Custom.AIGM.UMG
{
    [DataContract]
    public sealed class AIGMUMGActivationRule
    {
        [DataMember(Order = 0)] public string RuleId { get; set; }
        [DataMember(Order = 1)] public string HumanSummary { get; set; }
        [DataMember(Order = 2)] public string TriggerText { get; set; }
        [DataMember(Order = 3)] public bool AlwaysActive { get; set; }
        [DataMember(Order = 4)] public List<AIGMUMGTriggerClause> Clauses { get; set; }

        public AIGMUMGActivationRule()
        {
            RuleId = String.Empty;
            HumanSummary = String.Empty;
            TriggerText = String.Empty;
            AlwaysActive = true;
            Clauses = new List<AIGMUMGTriggerClause>();
        }

        public static AIGMUMGActivationRule Always(string summary)
        {
            return new AIGMUMGActivationRule
            {
                RuleId = "always",
                HumanSummary = String.IsNullOrWhiteSpace(summary) ? "Always active while enabled." : summary.Trim(),
                AlwaysActive = true
            };
        }

        public static AIGMUMGActivationRule Trigger(string triggerText, string summary)
        {
            return new AIGMUMGActivationRule
            {
                RuleId = "safe_trigger",
                HumanSummary = String.IsNullOrWhiteSpace(summary) ? triggerText : summary.Trim(),
                TriggerText = triggerText ?? String.Empty,
                AlwaysActive = false
            };
        }
    }

    [DataContract]
    public sealed class AIGMUMGTriggerClause
    {
        [DataMember(Order = 0)] public string Fact { get; set; }
        [DataMember(Order = 1)] public AIGMUMGTriggerOperator Operator { get; set; }
        [DataMember(Order = 2)] public string Value { get; set; }
        [DataMember(Order = 3)] public List<string> ValueSet { get; set; }

        public AIGMUMGTriggerClause()
        {
            Fact = String.Empty;
            Operator = AIGMUMGTriggerOperator.Equals;
            Value = String.Empty;
            ValueSet = new List<string>();
        }
    }

    [DataContract]
    public sealed class AIGMCapabilityRequirement
    {
        [DataMember(Order = 0)] public AIGMUMGCapabilityKind Kind { get; set; }
        [DataMember(Order = 1)] public string SkillName { get; set; }
        [DataMember(Order = 2)] public double MinimumSkillValue { get; set; }
        [DataMember(Order = 3)] public string ItemTypeName { get; set; }
        [DataMember(Order = 4)] public string SpellName { get; set; }
        [DataMember(Order = 5)] public string Reason { get; set; }

        public AIGMCapabilityRequirement()
        {
            SkillName = String.Empty;
            ItemTypeName = String.Empty;
            SpellName = String.Empty;
            Reason = String.Empty;
        }

        public AIGMCapabilityRequirement(AIGMUMGCapabilityKind kind, string reason)
            : this()
        {
            Kind = kind;
            Reason = reason ?? String.Empty;
        }
    }

    [DataContract]
    public sealed class AIGMCapabilityValidationResult
    {
        [DataMember(Order = 0)] public bool IsValid { get; set; }
        [DataMember(Order = 1)] public List<string> Passed { get; set; }
        [DataMember(Order = 2)] public List<string> Failed { get; set; }

        public AIGMCapabilityValidationResult()
        {
            IsValid = true;
            Passed = new List<string>();
            Failed = new List<string>();
        }

        public string BuildSummary()
        {
            if (IsValid)
                return Passed.Count == 0 ? "valid" : "valid: " + String.Join(", ", Passed.ToArray());

            return "invalid: " + String.Join(", ", Failed.ToArray());
        }
    }

    [DataContract]
    public sealed class AIGMUMGConflict
    {
        [DataMember(Order = 0)] public string ConflictId { get; set; }
        [DataMember(Order = 1)] public string LeftBlockId { get; set; }
        [DataMember(Order = 2)] public string RightBlockId { get; set; }
        [DataMember(Order = 3)] public string Reason { get; set; }
        [DataMember(Order = 4)] public string Resolution { get; set; }

        public AIGMUMGConflict()
        {
            ConflictId = String.Empty;
            LeftBlockId = String.Empty;
            RightBlockId = String.Empty;
            Reason = String.Empty;
            Resolution = String.Empty;
        }
    }

    [DataContract]
    public sealed class AIGMUMGBlock
    {
        [DataMember(Order = 0)] public string BlockId { get; set; }
        [DataMember(Order = 1)] public string Name { get; set; }
        [DataMember(Order = 2)] public AIGMUMGMoltType MoltType { get; set; }
        [DataMember(Order = 3)] public string Content { get; set; }
        [DataMember(Order = 4)] public string Summary { get; set; }
        [DataMember(Order = 5)] public List<string> Tags { get; set; }
        [DataMember(Order = 6)] public string Category { get; set; }
        [DataMember(Order = 7)] public int PriorityOrder { get; set; }
        [DataMember(Order = 8)] public bool Enabled { get; set; }
        [DataMember(Order = 9)] public AIGMUMGBlockState BlockState { get; set; }
        [DataMember(Order = 10)] public AIGMUMGBlockScope Scope { get; set; }
        [DataMember(Order = 11)] public AIGMUMGBlockSource Source { get; set; }
        [DataMember(Order = 12)] public int Version { get; set; }
        [DataMember(Order = 13)] public string CreatedBy { get; set; }
        [DataMember(Order = 14)] public string ApprovedBy { get; set; }
        [DataMember(Order = 15)] public DateTime CreatedUtc { get; set; }
        [DataMember(Order = 16)] public DateTime ModifiedUtc { get; set; }
        [DataMember(Order = 17)] public AIGMUMGActivationRule ActivationRule { get; set; }
        [DataMember(Order = 18)] public AIGMUMGActivationRule ExpiryRule { get; set; }
        [DataMember(Order = 19)] public List<string> Dependencies { get; set; }
        [DataMember(Order = 20)] public List<string> Conflicts { get; set; }
        [DataMember(Order = 21)] public List<AIGMCapabilityRequirement> CapabilityRequirements { get; set; }
        [DataMember(Order = 22)] public string FallbackBlockOrAction { get; set; }
        [DataMember(Order = 23)] public string Notes { get; set; }

        public AIGMUMGBlock()
        {
            BlockId = String.Empty;
            Name = String.Empty;
            Content = String.Empty;
            Summary = String.Empty;
            Tags = new List<string>();
            Category = String.Empty;
            PriorityOrder = 500;
            Enabled = true;
            BlockState = AIGMUMGBlockState.Draft;
            Scope = AIGMUMGBlockScope.NPC;
            Source = AIGMUMGBlockSource.UserManual;
            Version = 1;
            CreatedBy = "system";
            ApprovedBy = String.Empty;
            CreatedUtc = DateTime.UtcNow;
            ModifiedUtc = DateTime.UtcNow;
            ActivationRule = AIGMUMGActivationRule.Always("Always active while enabled.");
            ExpiryRule = null;
            Dependencies = new List<string>();
            Conflicts = new List<string>();
            CapabilityRequirements = new List<AIGMCapabilityRequirement>();
            FallbackBlockOrAction = String.Empty;
            Notes = String.Empty;
        }

        public bool IsActiveNow()
        {
            return Enabled && BlockState == AIGMUMGBlockState.Active;
        }

        public AIGMUMGBlock CloneAs(string blockId, AIGMUMGBlockSource source, AIGMUMGBlockState state)
        {
            AIGMUMGBlock clone = (AIGMUMGBlock)MemberwiseClone();
            clone.BlockId = blockId;
            clone.Tags = new List<string>(Tags);
            clone.Dependencies = new List<string>(Dependencies);
            clone.Conflicts = new List<string>(Conflicts);
            clone.CapabilityRequirements = new List<AIGMCapabilityRequirement>(CapabilityRequirements);
            clone.Source = source;
            clone.BlockState = state;
            clone.Version = 1;
            clone.CreatedUtc = DateTime.UtcNow;
            clone.ModifiedUtc = DateTime.UtcNow;
            clone.ApprovedBy = String.Empty;
            return clone;
        }
    }

    [DataContract]
    public sealed class AIGMUMGBlockStack
    {
        [DataMember(Order = 0)] public string BlockStackId { get; set; }
        [DataMember(Order = 1)] public string Name { get; set; }
        [DataMember(Order = 2)] public int PriorityOrder { get; set; }
        [DataMember(Order = 3)] public bool Enabled { get; set; }
        [DataMember(Order = 4)] public List<AIGMUMGBlock> MoltBlocks { get; set; }

        public AIGMUMGBlockStack()
        {
            BlockStackId = String.Empty;
            Name = String.Empty;
            PriorityOrder = 500;
            Enabled = true;
            MoltBlocks = new List<AIGMUMGBlock>();
        }
    }

    [DataContract]
    public sealed class AIGMUMGNeoBlock
    {
        [DataMember(Order = 0)] public string NeoBlockId { get; set; }
        [DataMember(Order = 1)] public string Name { get; set; }
        [DataMember(Order = 2)] public int PriorityOrder { get; set; }
        [DataMember(Order = 3)] public bool Enabled { get; set; }
        [DataMember(Order = 4)] public List<AIGMUMGBlockStack> BlockStacks { get; set; }

        public AIGMUMGNeoBlock()
        {
            NeoBlockId = String.Empty;
            Name = String.Empty;
            PriorityOrder = 500;
            Enabled = true;
            BlockStacks = new List<AIGMUMGBlockStack>();
        }
    }

    [DataContract]
    public sealed class AIGMUMGNeoStack
    {
        [DataMember(Order = 0)] public string NeoStackId { get; set; }
        [DataMember(Order = 1)] public AIGMUMGNeoStackKind StackKind { get; set; }
        [DataMember(Order = 2)] public string Name { get; set; }
        [DataMember(Order = 3)] public int PriorityOrder { get; set; }
        [DataMember(Order = 4)] public bool Enabled { get; set; }
        [DataMember(Order = 5)] public List<AIGMUMGNeoBlock> NeoBlocks { get; set; }

        public AIGMUMGNeoStack()
        {
            NeoStackId = String.Empty;
            Name = String.Empty;
            PriorityOrder = 500;
            Enabled = true;
            NeoBlocks = new List<AIGMUMGNeoBlock>();
        }
    }

    [DataContract]
    public sealed class AIGMUMGSleeve
    {
        public const int CurrentSchemaVersion = 1;
        public const string CurrentSchemaName = "AIGM_UMG_RUNTIME_SCHEMA_V1";

        [DataMember(Order = 0)] public string SleeveId { get; set; }
        [DataMember(Order = 1)] public string ActorId { get; set; }
        [DataMember(Order = 2)] public string DisplayName { get; set; }
        [DataMember(Order = 3)] public int Version { get; set; }
        [DataMember(Order = 4)] public int SchemaVersion { get; set; }
        [DataMember(Order = 5)] public string MigrationVersion { get; set; }
        [DataMember(Order = 6)] public AIGMUMGAutonomyMode AutonomyMode { get; set; }
        [DataMember(Order = 7)] public List<string> ActiveOverlayIds { get; set; }
        [DataMember(Order = 8)] public List<AIGMUMGNeoStack> NeoStacks { get; set; }
        [DataMember(Order = 9)] public DateTime CreatedUtc { get; set; }
        [DataMember(Order = 10)] public DateTime ModifiedUtc { get; set; }

        public AIGMUMGSleeve()
        {
            SleeveId = String.Empty;
            ActorId = String.Empty;
            DisplayName = String.Empty;
            Version = 1;
            SchemaVersion = CurrentSchemaVersion;
            MigrationVersion = "phase64c";
            AutonomyMode = AIGMUMGAutonomyMode.Manual;
            ActiveOverlayIds = new List<string>();
            NeoStacks = new List<AIGMUMGNeoStack>();
            CreatedUtc = DateTime.UtcNow;
            ModifiedUtc = DateTime.UtcNow;
        }

        public int CountBlocks(AIGMUMGBlockState? state)
        {
            int count = 0;
            foreach (AIGMUMGNeoStack stack in NeoStacks)
            {
                foreach (AIGMUMGNeoBlock neoBlock in stack.NeoBlocks)
                {
                    foreach (AIGMUMGBlockStack blockStack in neoBlock.BlockStacks)
                    {
                        foreach (AIGMUMGBlock block in blockStack.MoltBlocks)
                        {
                            if (!state.HasValue || block.BlockState == state.Value)
                                count++;
                        }
                    }
                }
            }

            return count;
        }
    }
}
