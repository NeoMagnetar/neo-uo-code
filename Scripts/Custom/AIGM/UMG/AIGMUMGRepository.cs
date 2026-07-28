using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using Server.Custom.AIGM.Characters.Waylander;

namespace Server.Custom.AIGM.UMG
{
    [DataContract]
    public sealed class AIGMUMGTemplate
    {
        [DataMember(Order = 0)] public string TemplateId { get; set; }
        [DataMember(Order = 1)] public string Name { get; set; }
        [DataMember(Order = 2)] public AIGMUMGNeoStackKind TargetNeoStack { get; set; }
        [DataMember(Order = 3)] public string Description { get; set; }
        [DataMember(Order = 4)] public List<AIGMUMGBlock> Blocks { get; set; }

        public AIGMUMGTemplate()
        {
            TemplateId = String.Empty;
            Name = String.Empty;
            Description = String.Empty;
            Blocks = new List<AIGMUMGBlock>();
        }
    }

    [DataContract]
    public sealed class AIGMUMGBlockProposal
    {
        [DataMember(Order = 0)] public string ProposalId { get; set; }
        [DataMember(Order = 1)] public string TargetActorOrGroup { get; set; }
        [DataMember(Order = 2)] public AIGMUMGNeoStackKind TargetNeoStack { get; set; }
        [DataMember(Order = 3)] public string BlockName { get; set; }
        [DataMember(Order = 4)] public string Reason { get; set; }
        [DataMember(Order = 5)] public string ExpectedBehavior { get; set; }
        [DataMember(Order = 6)] public string Risk { get; set; }
        [DataMember(Order = 7)] public string Status { get; set; }
        [DataMember(Order = 8)] public AIGMUMGBlock Block { get; set; }
        [DataMember(Order = 9)] public DateTime CreatedUtc { get; set; }

        public AIGMUMGBlockProposal()
        {
            ProposalId = Guid.NewGuid().ToString("N");
            TargetActorOrGroup = String.Empty;
            BlockName = String.Empty;
            Reason = String.Empty;
            ExpectedBehavior = String.Empty;
            Risk = String.Empty;
            Status = "DraftAwaitingApproval";
            CreatedUtc = DateTime.UtcNow;
        }
    }

    public static class AIGMUMGRepository
    {
        private static readonly object SyncRoot = new object();
        private static Dictionary<string, AIGMUMGSleeve> _sleeves;
        private static Dictionary<string, AIGMUMGTemplate> _templates;
        private static Dictionary<string, AIGMUMGBlockProposal> _proposals;
        private static Dictionary<string, AIGMUMGLibraryDefinition> _libraryDefinitions;
        private static Dictionary<string, AIGMUMGAssignment> _assignments;
        private static List<AIGMUMGVersionRecord> _versions;

        public static string DataRoot
        {
            get { return Path.Combine(Core.BaseDirectory, "Data", "AIGM", "UMG"); }
        }

        public static AIGMUMGSleeve GetSleeve(string actorId)
        {
            EnsureLoaded();
            if (String.IsNullOrWhiteSpace(actorId))
                return null;

            AIGMUMGSleeve sleeve;
            return _sleeves.TryGetValue(NormalizeId(actorId), out sleeve) ? sleeve : null;
        }

        public static List<AIGMUMGSleeve> GetAllSleeves()
        {
            EnsureLoaded();
            return new List<AIGMUMGSleeve>(_sleeves.Values);
        }

        public static List<AIGMUMGTemplate> GetTemplates()
        {
            EnsureLoaded();
            return new List<AIGMUMGTemplate>(_templates.Values);
        }

        public static AIGMUMGTemplate GetTemplate(string templateNameOrId)
        {
            EnsureLoaded();
            string key = NormalizeId(templateNameOrId);
            AIGMUMGTemplate template;
            if (_templates.TryGetValue(key, out template))
                return template;

            foreach (AIGMUMGTemplate candidate in _templates.Values)
            {
                if (String.Equals(NormalizeId(candidate.Name), key, StringComparison.OrdinalIgnoreCase))
                    return candidate;
            }

            return null;
        }

        public static List<AIGMUMGLibraryDefinition> GetLibraryDefinitions()
        {
            EnsureLoaded();
            return new List<AIGMUMGLibraryDefinition>(_libraryDefinitions.Values);
        }

        public static AIGMUMGLibraryDefinition GetLibraryDefinition(string definitionNameOrId)
        {
            EnsureLoaded();
            return AIGMUMGLibraryService.Find(_libraryDefinitions.Values, definitionNameOrId);
        }

        public static void SaveLibraryDefinition(AIGMUMGLibraryDefinition definition, string author, string summary)
        {
            EnsureLoaded();
            if (definition == null || String.IsNullOrWhiteSpace(definition.DefinitionId))
                return;

            _libraryDefinitions[definition.DefinitionId] = definition;
            PersistLibraryDefinitions();
            AIGMUMGLog.Write("library_definition_saved", null, AIGMUMGLog.Fields("definitionId", definition.DefinitionId, "author", author, "summary", summary));
        }

        public static List<AIGMUMGAssignment> GetAssignments()
        {
            EnsureLoaded();
            return new List<AIGMUMGAssignment>(_assignments.Values);
        }

        public static List<AIGMUMGAssignment> GetAssignmentsForTarget(string targetCanonicalId)
        {
            EnsureLoaded();
            List<AIGMUMGAssignment> results = new List<AIGMUMGAssignment>();
            string key = NormalizeId(targetCanonicalId);
            foreach (AIGMUMGAssignment assignment in _assignments.Values)
            {
                if (assignment != null && String.Equals(NormalizeId(assignment.TargetCanonicalId), key, StringComparison.OrdinalIgnoreCase))
                    results.Add(assignment);
            }

            results.Sort(delegate (AIGMUMGAssignment left, AIGMUMGAssignment right) { return right.ModifiedUtc.CompareTo(left.ModifiedUtc); });
            return results;
        }

        public static AIGMUMGAssignment FindAssignment(string targetCanonicalId, string assignmentOrDefinitionId)
        {
            EnsureLoaded();
            string target = NormalizeId(targetCanonicalId);
            string needle = NormalizeId(assignmentOrDefinitionId);
            foreach (AIGMUMGAssignment assignment in _assignments.Values)
            {
                if (assignment == null || !String.Equals(NormalizeId(assignment.TargetCanonicalId), target, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (String.Equals(NormalizeId(assignment.AssignmentId), needle, StringComparison.OrdinalIgnoreCase)
                    || String.Equals(NormalizeId(assignment.DefinitionId), needle, StringComparison.OrdinalIgnoreCase)
                    || NormalizeId(assignment.AssignmentId).IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                    return assignment;
            }

            return null;
        }

        public static AIGMUMGAssignment FindAssignment(string targetCanonicalId, string definitionId, AIGMUMGAssignmentState state)
        {
            EnsureLoaded();
            string target = NormalizeId(targetCanonicalId);
            string definition = NormalizeId(definitionId);
            foreach (AIGMUMGAssignment assignment in _assignments.Values)
            {
                if (assignment != null
                    && assignment.State == state
                    && String.Equals(NormalizeId(assignment.TargetCanonicalId), target, StringComparison.OrdinalIgnoreCase)
                    && String.Equals(NormalizeId(assignment.DefinitionId), definition, StringComparison.OrdinalIgnoreCase))
                    return assignment;
            }

            return null;
        }

        public static void SaveAssignmentChange(AIGMUMGAssignment assignment, string author, string summary)
        {
            EnsureLoaded();
            if (assignment == null || String.IsNullOrWhiteSpace(assignment.AssignmentId))
                return;

            assignment.ExecutionMode = AIGMUMGExecutionMode.PreviewOnly;
            assignment.ModifiedUtc = DateTime.UtcNow;
            _assignments[assignment.AssignmentId] = assignment;

            int nextVersion = 1;
            for (int i = 0; i < _versions.Count; i++)
            {
                if (String.Equals(_versions[i].AssignmentId, assignment.AssignmentId, StringComparison.OrdinalIgnoreCase))
                    nextVersion = Math.Max(nextVersion, _versions[i].VersionNumber + 1);
            }

            AIGMUMGVersionRecord version = AIGMUMGVersionService.CreateAssignmentVersion(assignment, author, summary, nextVersion);
            _versions.Insert(0, version);
            PersistAssignments();
            PersistVersions();
            AIGMUMGLog.Write("assignment_saved", null, AIGMUMGLog.Fields("assignmentId", assignment.AssignmentId, "target", assignment.TargetCanonicalId, "state", assignment.State.ToString(), "execution", assignment.ExecutionMode.ToString(), "author", author, "summary", summary));
        }

        public static List<AIGMUMGVersionRecord> GetVersionsForTarget(string targetCanonicalId)
        {
            EnsureLoaded();
            List<AIGMUMGVersionRecord> results = new List<AIGMUMGVersionRecord>();
            string key = NormalizeId(targetCanonicalId);
            for (int i = 0; i < _versions.Count; i++)
            {
                if (_versions[i] != null && String.Equals(NormalizeId(_versions[i].TargetId), key, StringComparison.OrdinalIgnoreCase))
                    results.Add(_versions[i]);
            }

            return results;
        }

        public static AIGMUMGVersionRecord GetVersion(string versionId)
        {
            EnsureLoaded();
            string key = NormalizeId(versionId);
            for (int i = 0; i < _versions.Count; i++)
            {
                if (_versions[i] != null && String.Equals(NormalizeId(_versions[i].VersionId), key, StringComparison.OrdinalIgnoreCase))
                    return _versions[i];
            }

            return null;
        }

        public static List<AIGMUMGBlockProposal> GetProposals()
        {
            EnsureLoaded();
            return new List<AIGMUMGBlockProposal>(_proposals.Values);
        }

        public static AIGMUMGBlockProposal ProposeEmergencyManaReserve(string actorId)
        {
            EnsureLoaded();
            string normalized = NormalizeId(actorId);
            AIGMUMGBlockProposal existing = FindExistingProposal(normalized, "PreserveEmergencyMana");
            if (existing != null)
            {
                AIGMUMGLog.Write("agent_proposal_existing", null, AIGMUMGLog.Fields("proposalId", existing.ProposalId, "target", normalized, "block", existing.BlockName));
                return existing;
            }

            AIGMUMGBlock block = NewBlock(
                normalized + ".preserve_emergency_mana.instruction",
                "PreserveEmergencyMana",
                AIGMUMGMoltType.Instruction,
                "Do not use offensive spells while self.mana_pct < 50. Reserve mana for cure and healing. Fallback to sword and bandages.",
                "Reserve half mana for cure/heal before offensive casting.",
                "ManaPolicy",
                780,
                AIGMUMGBlockState.Draft,
                AIGMUMGBlockSource.AgentProposal);
            block.ActivationRule = AIGMUMGActivationRule.Trigger("self.mana_pct < 50", "self.mana_pct < 50");
            block.CapabilityRequirements.Add(new AIGMCapabilityRequirement(AIGMUMGCapabilityKind.CanCastDefensiveSpell, "cure/heal support magic required"));
            block.FallbackBlockOrAction = "Use sword and bandages";
            block.Tags.Add("agent-proposal");
            block.Tags.Add("mana-reserve");

            AIGMUMGBlockProposal proposal = new AIGMUMGBlockProposal
            {
                TargetActorOrGroup = normalized,
                TargetNeoStack = AIGMUMGNeoStackKind.SkillsSpellsResources,
                BlockName = "PreserveEmergencyMana",
                Reason = "Current doctrine can spend mana unless a reserve is explicitly declared.",
                ExpectedBehavior = "Below 50% mana, offensive spell intents are rejected while cure/heal remain valid.",
                Risk = "Low while draft; activation requires capability validation and explicit approval.",
                Block = block
            };

            _proposals[proposal.ProposalId] = proposal;
            PersistProposals();
            AIGMUMGLog.Write("agent_proposal", null, AIGMUMGLog.Fields("proposalId", proposal.ProposalId, "target", normalized, "block", proposal.BlockName));
            return proposal;
        }

        private static AIGMUMGBlockProposal FindExistingProposal(string targetActorOrGroup, string blockName)
        {
            foreach (AIGMUMGBlockProposal proposal in _proposals.Values)
            {
                if (proposal == null)
                    continue;

                if (String.Equals(NormalizeId(proposal.TargetActorOrGroup), NormalizeId(targetActorOrGroup), StringComparison.OrdinalIgnoreCase)
                    && String.Equals(proposal.BlockName, blockName, StringComparison.OrdinalIgnoreCase)
                    && (String.IsNullOrWhiteSpace(proposal.Status) || proposal.Status.IndexOf("Draft", StringComparison.OrdinalIgnoreCase) >= 0))
                    return proposal;
            }

            return null;
        }

        public static void Reload()
        {
            lock (SyncRoot)
            {
                _sleeves = null;
                _templates = null;
                _proposals = null;
                _libraryDefinitions = null;
                _assignments = null;
                _versions = null;
            }

            EnsureLoaded();
        }

        public static string BuildInventorySummary()
        {
            EnsureLoaded();
            int blocks = 0;
            int activeBlocks = 0;
            foreach (AIGMUMGSleeve sleeve in _sleeves.Values)
            {
                blocks += sleeve.CountBlocks(null);
                activeBlocks += sleeve.CountBlocks(AIGMUMGBlockState.Active);
            }

            return String.Format(
                "schema={0}; sleeves={1}; templates={2}; libraryDefinitions={3}; assignments={4}; versions={5}; proposals={6}; blocks={7}; activeBlocks={8}; tacticalDispatch={9}",
                AIGMUMGSleeve.CurrentSchemaName,
                _sleeves.Count,
                _templates.Count,
                _libraryDefinitions.Count,
                _assignments.Count,
                _versions.Count,
                _proposals.Count,
                blocks,
                activeBlocks,
                AIGMUMGPhase64C2Invariant.TacticalDispatchEnabled ? "true" : "false");
        }

        private static void EnsureLoaded()
        {
            if (_sleeves != null && _templates != null && _proposals != null && _libraryDefinitions != null && _assignments != null && _versions != null)
                return;

            lock (SyncRoot)
            {
                if (_sleeves != null && _templates != null && _proposals != null && _libraryDefinitions != null && _assignments != null && _versions != null)
                    return;

                EnsureDirectories();
                BackupV1SidecarsOnce();
                _templates = LoadTemplatesOrDefault();
                _sleeves = LoadSleevesOrDefault();
                _proposals = LoadProposals();
                _libraryDefinitions = LoadLibraryDefinitionsOrDefault();
                _assignments = LoadAssignments();
                _versions = LoadVersions();
                TrySaveSeedFiles();
            }
        }

        private static void EnsureDirectories()
        {
            string[] paths =
            {
                DataRoot,
                Path.Combine(DataRoot, "Blocks"),
                Path.Combine(DataRoot, "BlockStacks"),
                Path.Combine(DataRoot, "NeoBlocks"),
                Path.Combine(DataRoot, "NeoStacks"),
                Path.Combine(DataRoot, "Sleeves"),
                Path.Combine(DataRoot, "Templates"),
                Path.Combine(DataRoot, "Migrations"),
                Path.Combine(DataRoot, "Logs"),
                Path.Combine(DataRoot, "Runtime"),
                Path.Combine(DataRoot, "Proposals"),
                Path.Combine(DataRoot, "Library"),
                Path.Combine(DataRoot, "Assignments"),
                Path.Combine(DataRoot, "Versions"),
                Path.Combine(DataRoot, "ChangeSets"),
                Path.Combine(DataRoot, "MigrationBackups")
            };

            for (int i = 0; i < paths.Length; i++)
            {
                if (!Directory.Exists(paths[i]))
                    Directory.CreateDirectory(paths[i]);
            }
        }

        private static Dictionary<string, AIGMUMGTemplate> LoadTemplatesOrDefault()
        {
            List<AIGMUMGTemplate> loaded;
            if (TryReadJson(Path.Combine(DataRoot, "Templates", "phase64c_initial_templates.json"), out loaded) && loaded != null && loaded.Count > 0)
            {
                Dictionary<string, AIGMUMGTemplate> templates = new Dictionary<string, AIGMUMGTemplate>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < loaded.Count; i++)
                {
                    AIGMUMGTemplate template = loaded[i];
                    if (template != null && !String.IsNullOrWhiteSpace(template.TemplateId))
                        templates[NormalizeId(template.TemplateId)] = template;
                }

                if (templates.Count > 0)
                    return templates;
            }

            return BuildDefaultTemplates();
        }

        private static Dictionary<string, AIGMUMGSleeve> LoadSleevesOrDefault()
        {
            List<AIGMUMGSleeve> loaded;
            if (TryReadJson(Path.Combine(DataRoot, "Migrations", "phase64c_default_sleeves.json"), out loaded) && loaded != null && loaded.Count > 0)
            {
                Dictionary<string, AIGMUMGSleeve> sleeves = new Dictionary<string, AIGMUMGSleeve>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < loaded.Count; i++)
                {
                    AIGMUMGSleeve sleeve = loaded[i];
                    if (sleeve != null && !String.IsNullOrWhiteSpace(sleeve.ActorId))
                    {
                        sleeve.SchemaVersion = AIGMUMGSleeve.CurrentSchemaVersion;
                        sleeve.MigrationVersion = "phase64c2_preview_authoring";
                        sleeves[NormalizeId(sleeve.ActorId)] = sleeve;
                    }
                }

                if (sleeves.Count > 0)
                    return sleeves;
            }

            return BuildDefaultSleeves();
        }

        private static Dictionary<string, AIGMUMGLibraryDefinition> LoadLibraryDefinitionsOrDefault()
        {
            List<AIGMUMGLibraryDefinition> loaded;
            if (!TryReadJson(GetLibraryPath(), out loaded) || loaded == null || loaded.Count == 0)
                loaded = AIGMUMGLibraryService.BuildDefaultDefinitions();

            Dictionary<string, AIGMUMGLibraryDefinition> definitions = new Dictionary<string, AIGMUMGLibraryDefinition>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < loaded.Count; i++)
            {
                AIGMUMGLibraryDefinition definition = loaded[i];
                if (definition == null || String.IsNullOrWhiteSpace(definition.DefinitionId))
                    continue;

                definition.DefaultExecutionMode = AIGMUMGExecutionMode.PreviewOnly;
                definitions[definition.DefinitionId] = definition;
            }

            return definitions;
        }

        private static Dictionary<string, AIGMUMGAssignment> LoadAssignments()
        {
            Dictionary<string, AIGMUMGAssignment> assignments = new Dictionary<string, AIGMUMGAssignment>(StringComparer.OrdinalIgnoreCase);
            List<AIGMUMGAssignment> loaded;
            if (!TryReadJson(GetAssignmentsPath(), out loaded) || loaded == null)
                return assignments;

            for (int i = 0; i < loaded.Count; i++)
            {
                AIGMUMGAssignment assignment = loaded[i];
                if (assignment == null)
                    continue;
                if (String.IsNullOrWhiteSpace(assignment.AssignmentId))
                    assignment.AssignmentId = Guid.NewGuid().ToString("N");
                assignment.ExecutionMode = AIGMUMGExecutionMode.PreviewOnly;
                assignments[assignment.AssignmentId] = assignment;
            }

            return assignments;
        }

        private static List<AIGMUMGVersionRecord> LoadVersions()
        {
            List<AIGMUMGVersionRecord> versions;
            if (!TryReadJson(GetVersionsPath(), out versions) || versions == null)
                versions = new List<AIGMUMGVersionRecord>();
            return versions;
        }

        private static Dictionary<string, AIGMUMGBlockProposal> LoadProposals()
        {
            Dictionary<string, AIGMUMGBlockProposal> proposals = new Dictionary<string, AIGMUMGBlockProposal>(StringComparer.OrdinalIgnoreCase);
            List<AIGMUMGBlockProposal> loaded;
            if (!TryReadJson(GetProposalPath(), out loaded) || loaded == null)
                return proposals;

            for (int i = 0; i < loaded.Count; i++)
            {
                AIGMUMGBlockProposal proposal = loaded[i];
                if (proposal == null)
                    continue;

                if (String.IsNullOrWhiteSpace(proposal.ProposalId))
                    proposal.ProposalId = Guid.NewGuid().ToString("N");

                proposals[proposal.ProposalId] = proposal;
            }

            return proposals;
        }

        private static Dictionary<string, AIGMUMGSleeve> BuildDefaultSleeves()
        {
            Dictionary<string, AIGMUMGSleeve> sleeves = new Dictionary<string, AIGMUMGSleeve>(StringComparer.OrdinalIgnoreCase);
            IReadOnlyDictionary<string, WaylanderCharacterDefinition> definitions = WaylanderRosterCatalog.GetAllDefinitions();
            foreach (KeyValuePair<string, WaylanderCharacterDefinition> pair in definitions)
            {
                WaylanderCharacterDefinition definition = pair.Value;
                if (definition == null || definition.IsMount)
                    continue;

                AIGMUMGSleeve sleeve = BuildSleeveFromDefinition(definition);
                sleeves[sleeve.ActorId] = sleeve;
            }

            EnsureSleeve(sleeves, "dakeyras", "Dakeyras", "Shadowed protector, scout, and restrained force.");
            EnsureSleeve(sleeves, "danyal", "Danyal", "Protector and emotionally grounded tactical support.");
            EnsureSleeve(sleeves, "dardalion", "Dardalion", "Warrior-priest, healer, and support defender.");
            return sleeves;
        }

        private static void EnsureSleeve(Dictionary<string, AIGMUMGSleeve> sleeves, string actorId, string displayName, string identity)
        {
            if (!sleeves.ContainsKey(actorId))
                sleeves[actorId] = BuildSleeve(actorId, displayName, identity, "Original Companion", "Heroes", AIGMUMGBlockSource.CanonicalMigration);
        }

        private static AIGMUMGSleeve BuildSleeveFromDefinition(WaylanderCharacterDefinition definition)
        {
            string identity = !String.IsNullOrWhiteSpace(definition.IdentityLine)
                ? definition.IdentityLine
                : definition.VisibleName + ": " + definition.CombatRole;
            return BuildSleeve(
                definition.CharacterId,
                definition.VisibleName,
                identity,
                definition.CombatRole,
                definition.Faction,
                AIGMUMGBlockSource.CanonicalMigration);
        }

        private static AIGMUMGSleeve BuildSleeve(string actorId, string displayName, string identity, string role, string faction, AIGMUMGBlockSource source)
        {
            string id = NormalizeId(actorId);
            AIGMUMGSleeve sleeve = new AIGMUMGSleeve
            {
                SleeveId = "sleeve." + id + ".phase64c.v1",
                ActorId = id,
                DisplayName = String.IsNullOrWhiteSpace(displayName) ? actorId : displayName,
                AutonomyMode = AIGMUMGAutonomyMode.Manual
            };

            AddCanonicalStacks(sleeve);
            AddIdentityBlocks(sleeve, identity, role, faction, source);
            AddCapabilityBlocks(sleeve);
            AddGovernanceBlocks(sleeve);
            AddDefaultDoctrineDrafts(sleeve);
            return sleeve;
        }

        private static void AddCanonicalStacks(AIGMUMGSleeve sleeve)
        {
            AddStack(sleeve, AIGMUMGNeoStackKind.Identity, "Identity", 110);
            AddStack(sleeve, AIGMUMGNeoStackKind.Capability, "Capability", 900);
            AddStack(sleeve, AIGMUMGNeoStackKind.CombatDoctrine, "Combat Doctrine", 640);
            AddStack(sleeve, AIGMUMGNeoStackKind.SkillsSpellsResources, "Skills, Spells, and Resources", 620);
            AddStack(sleeve, AIGMUMGNeoStackKind.MovementPositioning, "Movement and Positioning", 600);
            AddStack(sleeve, AIGMUMGNeoStackKind.TrackingAwareness, "Tracking and Awareness", 580);
            AddStack(sleeve, AIGMUMGNeoStackKind.SquadRelationshipOperations, "Squad and Relationship Operations", 700);
            AddStack(sleeve, AIGMUMGNeoStackKind.SituationalOverlays, "Situational Overlays", 820);
            AddStack(sleeve, AIGMUMGNeoStackKind.Governance, "Governance", 1000);
        }

        private static void AddIdentityBlocks(AIGMUMGSleeve sleeve, string identity, string role, string faction, AIGMUMGBlockSource source)
        {
            AddBlock(sleeve, AIGMUMGNeoStackKind.Identity, NewBlock(sleeve.ActorId + ".identity.subject", "Canonical Identity", AIGMUMGMoltType.Subject, identity, "Migrated identity source.", "Identity", 420, AIGMUMGBlockState.Active, source));
            AddBlock(sleeve, AIGMUMGNeoStackKind.Identity, NewBlock(sleeve.ActorId + ".identity.directive", "Preserve Personality", AIGMUMGMoltType.Directive, "Preserve the existing Profile/journal persona and Neosleeve voice. Tactical overlays must not overwrite canonical identity.", "Persona preservation directive.", "Identity", 410, AIGMUMGBlockState.Active, source));
            AddBlock(sleeve, AIGMUMGNeoStackKind.Identity, NewBlock(sleeve.ActorId + ".identity.instruction", "No False Capability Claims", AIGMUMGMoltType.Instruction, "Do not claim to perform abilities that capability validation rejects.", "No false execution or capability claims.", "Identity", 405, AIGMUMGBlockState.Active, source));
            AddBlock(sleeve, AIGMUMGNeoStackKind.Identity, NewBlock(sleeve.ActorId + ".identity.primary", "Core Role", AIGMUMGMoltType.Primary, String.IsNullOrWhiteSpace(role) ? "Role remains as currently defined." : role, "Role anchor.", "Identity", 400, AIGMUMGBlockState.Active, source));
            AddBlock(sleeve, AIGMUMGNeoStackKind.Identity, NewBlock(sleeve.ActorId + ".identity.philosophy", "Faction Frame", AIGMUMGMoltType.Philosophy, String.IsNullOrWhiteSpace(faction) ? "No separate faction doctrine." : faction, "Faction and interpretive stance.", "Identity", 390, AIGMUMGBlockState.Active, source));
            AddBlock(sleeve, AIGMUMGNeoStackKind.Identity, NewBlock(sleeve.ActorId + ".identity.blueprint", "Voice Structure", AIGMUMGMoltType.Blueprint, "Use existing in-world dialogue surfaces and current prompt persona context; never expose raw UMG data as ordinary speech.", "Voice/prompt bridge blueprint.", "Identity", 380, AIGMUMGBlockState.Active, source));
        }

        private static void AddCapabilityBlocks(AIGMUMGSleeve sleeve)
        {
            AddBlock(sleeve, AIGMUMGNeoStackKind.Capability, NewBlock(sleeve.ActorId + ".capability.generated.primary", "Live Capability Snapshot", AIGMUMGMoltType.Primary, "Capability data is generated from live Mobile stats, skills, equipment, backpack inventory, services, control policy, task state, and region restrictions.", "Machine-maintained capability truth.", "Capability", 980, AIGMUMGBlockState.Active, AIGMUMGBlockSource.RuntimeGenerated));
            AddBlock(sleeve, AIGMUMGNeoStackKind.Capability, NewBlock(sleeve.ActorId + ".capability.no_falsify.instruction", "Manual Notes Cannot Falsify Capability", AIGMUMGMoltType.Instruction, "Manual notes may attach context but must not assert unavailable skills, equipment, spells, services, or resources as real capability.", "Manual capability guard.", "Capability", 970, AIGMUMGBlockState.Active, AIGMUMGBlockSource.SystemInvariant));
        }

        private static void AddGovernanceBlocks(AIGMUMGSleeve sleeve)
        {
            AddBlock(sleeve, AIGMUMGNeoStackKind.Governance, NewBlock(sleeve.ActorId + ".governance.engine_invariants.primary", "Engine and Security Invariants", AIGMUMGMoltType.Primary, "No model-generated prose may directly damage, teleport, spawn, delete, loot, or mutate the world. UMG compiles only typed intents for deterministic services.", "Highest priority safety invariant.", "Governance", 1000, AIGMUMGBlockState.Active, AIGMUMGBlockSource.SystemInvariant));
            AddBlock(sleeve, AIGMUMGNeoStackKind.Governance, NewBlock(sleeve.ActorId + ".governance.operational_hold.instruction", "Operational Hold Authority", AIGMUMGMoltType.Instruction, "Absolute GM Hold and Passive Stand Down outrank all tactical overlays and autonomous modes.", "Phase63A hold/stand-down preservation.", "Governance", 995, AIGMUMGBlockState.Active, AIGMUMGBlockSource.SystemInvariant));
            AddBlock(sleeve, AIGMUMGNeoStackKind.Governance, NewBlock(sleeve.ActorId + ".governance.protected_targets.instruction", "Protected Target Policy", AIGMUMGMoltType.Instruction, "Do not target staff, vendors, protected allies, guards, blessed targets, or control masters unless a deterministic service independently validates harm legality.", "Friendly/protected target prevention.", "Governance", 990, AIGMUMGBlockState.Active, AIGMUMGBlockSource.SystemInvariant));
        }

        private static void AddDefaultDoctrineDrafts(AIGMUMGSleeve sleeve)
        {
            string id = sleeve.ActorId;
            if (id == "druss")
                AddTemplateDraft(sleeve, "frontline_defender");
            else if (id == "dakeyras" || id == "waylander" || id == "dakeyras.grey_man")
                AddTemplateDraft(sleeve, "counter_ambush");
            else if (id == "danyal")
                AddTemplateDraft(sleeve, "protect_owner");
            else if (id == "dardalion")
                AddTemplateDraft(sleeve, "warrior_priest");
            else if (id == "miriel")
                AddTemplateDraft(sleeve, "rear_line_archer");
            else if (id == "cadoras")
                AddTemplateDraft(sleeve, "joining_hunter");
            else if (id == "dark_brotherhood_knight")
                AddTemplateDraft(sleeve, "brotherhood_squad");
            else if (id == "joining")
                AddTemplateDraft(sleeve, "scout_tracker");
            else if (id == "zhu_chao" || id == "ustarte")
                AddTemplateDraft(sleeve, "resource_conservation");
            else
                AddTemplateDraft(sleeve, "town_peace_mode");
        }

        private static void AddTemplateDraft(AIGMUMGSleeve sleeve, string templateId)
        {
            AIGMUMGTemplate template;
            if (!_templates.TryGetValue(templateId, out template))
                return;

            for (int i = 0; i < template.Blocks.Count; i++)
            {
                AIGMUMGBlock clone = template.Blocks[i].CloneAs(sleeve.ActorId + "." + templateId + "." + (i + 1), AIGMUMGBlockSource.Template, AIGMUMGBlockState.Draft);
                clone.Scope = AIGMUMGBlockScope.NPC;
                AddBlock(sleeve, template.TargetNeoStack, clone);
            }
        }

        private static Dictionary<string, AIGMUMGTemplate> BuildDefaultTemplates()
        {
            Dictionary<string, AIGMUMGTemplate> templates = new Dictionary<string, AIGMUMGTemplate>(StringComparer.OrdinalIgnoreCase);
            AddTemplate(templates, "frontline_defender", "Frontline Defender", AIGMUMGNeoStackKind.CombatDoctrine, "Protect allies, engage direct threats, and respect pursuit boundary.", AIGMUMGCapabilityKind.CanGuard, AIGMUMGCapabilityKind.CanUseSword);
            AddTemplate(templates, "aggressive_berserker", "Aggressive Berserker", AIGMUMGNeoStackKind.CombatDoctrine, "High aggression against validated hostile targets.", AIGMUMGCapabilityKind.CanGuard);
            AddTemplate(templates, "cautious_archer", "Cautious Archer", AIGMUMGNeoStackKind.CombatDoctrine, "Prefer ranged combat and avoid bad melee exposure.", AIGMUMGCapabilityKind.CanUseBow);
            AddTemplate(templates, "rear_line_archer", "Rear-Line Archer", AIGMUMGNeoStackKind.MovementPositioning, "Maintain range, prioritize casters and trackers, reposition after exposure.", AIGMUMGCapabilityKind.CanUseBow, AIGMUMGCapabilityKind.CanFollow);
            AddTemplate(templates, "healer_support", "Healer Support", AIGMUMGNeoStackKind.SkillsSpellsResources, "Heal, bandage, and cure allies when validated.", AIGMUMGCapabilityKind.CanHeal, AIGMUMGCapabilityKind.CanBandage);
            AddTemplate(templates, "warrior_priest", "Warrior Priest", AIGMUMGNeoStackKind.SkillsSpellsResources, "Cure poison first, preserve emergency mana, and use sword/bandage fallback.", AIGMUMGCapabilityKind.CanUseSword, AIGMUMGCapabilityKind.CanBandage);
            AddTemplate(templates, "anti_mage", "Anti-Mage", AIGMUMGNeoStackKind.CombatDoctrine, "Prioritize validated enemy casters.", AIGMUMGCapabilityKind.CanGuard);
            AddTemplate(templates, "anti_demon", "Anti-Demon", AIGMUMGNeoStackKind.CombatDoctrine, "Prioritize demonic enemies when lawful and validated.", AIGMUMGCapabilityKind.CanGuard);
            AddTemplate(templates, "scout_tracker", "Scout Tracker", AIGMUMGNeoStackKind.TrackingAwareness, "Track and report target trails.", AIGMUMGCapabilityKind.CanTrack);
            AddTemplate(templates, "counter_tracker", "Counter-Tracker", AIGMUMGNeoStackKind.TrackingAwareness, "Detect pursuit and alert allies.", AIGMUMGCapabilityKind.CanTrack, AIGMUMGCapabilityKind.CanDetectHidden);
            AddTemplate(templates, "stealth_assassin", "Stealth Assassin", AIGMUMGNeoStackKind.SituationalOverlays, "Use concealment and repositioning when supported.", AIGMUMGCapabilityKind.CanHide, AIGMUMGCapabilityKind.CanStealth);
            AddTemplate(templates, "brotherhood_squad", "Brotherhood Squad", AIGMUMGNeoStackKind.SquadRelationshipOperations, "Share squad targets and protect assigned commanders without personal-name behavior.", AIGMUMGCapabilityKind.CanReceiveSquadOrders);
            AddTemplate(templates, "joining_hunter", "Joining Hunter", AIGMUMGNeoStackKind.TrackingAwareness, "Track and pressure Joinings within validated pursuit boundaries.", AIGMUMGCapabilityKind.CanTrack, AIGMUMGCapabilityKind.CanGuard);
            AddTemplate(templates, "protect_owner", "Protect Owner", AIGMUMGNeoStackKind.SituationalOverlays, "Defend the owner through guard and fallback reporting.", AIGMUMGCapabilityKind.CanGuard);
            AddTemplate(templates, "protect_civilian", "Protect Civilian", AIGMUMGNeoStackKind.SituationalOverlays, "Protect civilian targets and suppress reckless pursuit.", AIGMUMGCapabilityKind.CanGuard);
            AddTemplate(templates, "hold_the_line", "Hold the Line", AIGMUMGNeoStackKind.MovementPositioning, "Hold position unless governance or retreat overlay outranks it.", AIGMUMGCapabilityKind.CanGuard);
            AddTemplate(templates, "retreat_and_regroup", "Retreat and Regroup", AIGMUMGNeoStackKind.SituationalOverlays, "Retreat to assigned waypoint or regroup point.", AIGMUMGCapabilityKind.CanFollow);
            AddTemplate(templates, "town_peace_mode", "Town Peace Mode", AIGMUMGNeoStackKind.SituationalOverlays, "Suppress proactive aggression in guarded/town regions while retaining lawful direct defense.", AIGMUMGCapabilityKind.CanGuard);
            AddTemplate(templates, "dungeon_formation", "Dungeon Formation", AIGMUMGNeoStackKind.MovementPositioning, "Prefer compact formation, line of sight, and regrouping.", AIGMUMGCapabilityKind.CanFollow);
            AddTemplate(templates, "escort", "Escort", AIGMUMGNeoStackKind.MovementPositioning, "Escort protected mobile using existing movement and guard services.", AIGMUMGCapabilityKind.CanFollow, AIGMUMGCapabilityKind.CanGuard);
            AddTemplate(templates, "search_and_rescue", "Search and Rescue", AIGMUMGNeoStackKind.TrackingAwareness, "Search assigned area and report/recover allies.", AIGMUMGCapabilityKind.CanTrack);
            AddTemplate(templates, "resource_conservation", "Resource Conservation", AIGMUMGNeoStackKind.SkillsSpellsResources, "Conserve mana, reagents, ammunition, and potions unless emergency overlays apply.", AIGMUMGCapabilityKind.CanGuard);
            AddTemplate(templates, "last_stand", "Last Stand", AIGMUMGNeoStackKind.SituationalOverlays, "Low-retreat emergency protection overlay.", AIGMUMGCapabilityKind.CanGuard);
            AddTemplate(templates, "counter_ambush", "Counter Ambush", AIGMUMGNeoStackKind.SituationalOverlays, "Use tracking, hiding, stealth, and repositioning only when real capabilities validate.", AIGMUMGCapabilityKind.CanTrack, AIGMUMGCapabilityKind.CanHide, AIGMUMGCapabilityKind.CanStealth);
            return templates;
        }

        private static void AddTemplate(Dictionary<string, AIGMUMGTemplate> templates, string id, string name, AIGMUMGNeoStackKind stack, string description, params AIGMUMGCapabilityKind[] requirements)
        {
            AIGMUMGTemplate template = new AIGMUMGTemplate
            {
                TemplateId = id,
                Name = name,
                TargetNeoStack = stack,
                Description = description
            };

            AIGMUMGBlock block = NewBlock("template." + id + ".instruction", name, AIGMUMGMoltType.Instruction, description, description, "DoctrineTemplate", 650, AIGMUMGBlockState.Draft, AIGMUMGBlockSource.Template);
            block.Scope = AIGMUMGBlockScope.NPC;
            block.FallbackBlockOrAction = "Report capability failure and keep current deterministic behavior.";
            for (int i = 0; i < requirements.Length; i++)
                block.CapabilityRequirements.Add(new AIGMCapabilityRequirement(requirements[i], name));
            template.Blocks.Add(block);
            templates[id] = template;
        }

        private static AIGMUMGNeoStack AddStack(AIGMUMGSleeve sleeve, AIGMUMGNeoStackKind kind, string name, int priority)
        {
            AIGMUMGNeoStack stack = new AIGMUMGNeoStack
            {
                NeoStackId = sleeve.ActorId + "." + kind.ToString().ToLowerInvariant(),
                StackKind = kind,
                Name = name,
                PriorityOrder = priority
            };
            sleeve.NeoStacks.Add(stack);
            return stack;
        }

        private static void AddBlock(AIGMUMGSleeve sleeve, AIGMUMGNeoStackKind stackKind, AIGMUMGBlock block)
        {
            AIGMUMGNeoStack stack = FindStack(sleeve, stackKind);
            if (stack == null)
                stack = AddStack(sleeve, stackKind, stackKind.ToString(), 500);

            AIGMUMGNeoBlock neoBlock;
            if (stack.NeoBlocks.Count == 0)
            {
                neoBlock = new AIGMUMGNeoBlock
                {
                    NeoBlockId = stack.NeoStackId + ".default_neoblock",
                    Name = stack.Name + " NeoBlock",
                    PriorityOrder = stack.PriorityOrder
                };
                stack.NeoBlocks.Add(neoBlock);
            }
            else
            {
                neoBlock = stack.NeoBlocks[0];
            }

            AIGMUMGBlockStack blockStack;
            if (neoBlock.BlockStacks.Count == 0)
            {
                blockStack = new AIGMUMGBlockStack
                {
                    BlockStackId = neoBlock.NeoBlockId + ".default_blockstack",
                    Name = neoBlock.Name + " BlockStack",
                    PriorityOrder = stack.PriorityOrder
                };
                neoBlock.BlockStacks.Add(blockStack);
            }
            else
            {
                blockStack = neoBlock.BlockStacks[0];
            }

            blockStack.MoltBlocks.Add(block);
        }

        private static AIGMUMGNeoStack FindStack(AIGMUMGSleeve sleeve, AIGMUMGNeoStackKind stackKind)
        {
            if (sleeve == null)
                return null;

            for (int i = 0; i < sleeve.NeoStacks.Count; i++)
            {
                if (sleeve.NeoStacks[i].StackKind == stackKind)
                    return sleeve.NeoStacks[i];
            }

            return null;
        }

        private static AIGMUMGBlock NewBlock(string id, string name, AIGMUMGMoltType type, string content, string summary, string category, int priority, AIGMUMGBlockState state, AIGMUMGBlockSource source)
        {
            AIGMUMGBlock block = new AIGMUMGBlock
            {
                BlockId = id,
                Name = name,
                MoltType = type,
                Content = content ?? String.Empty,
                Summary = summary ?? String.Empty,
                Category = category ?? String.Empty,
                PriorityOrder = priority,
                BlockState = state,
                Source = source,
                CreatedBy = source.ToString(),
                ApprovedBy = state == AIGMUMGBlockState.Active ? "phase64c_migration" : String.Empty
            };
            block.Tags.Add(type.ToString().ToLowerInvariant());
            block.Tags.Add((category ?? String.Empty).ToLowerInvariant());
            return block;
        }

        private static void TrySaveSeedFiles()
        {
            try
            {
                WriteJsonAtomicIfMissing(Path.Combine(DataRoot, "Templates", "phase64c_initial_templates.json"), new List<AIGMUMGTemplate>(_templates.Values));
                WriteJsonAtomicIfMissing(Path.Combine(DataRoot, "Migrations", "phase64c_default_sleeves.json"), new List<AIGMUMGSleeve>(_sleeves.Values));
                WriteJsonAtomicIfMissing(GetProposalPath(), new List<AIGMUMGBlockProposal>(_proposals.Values));
                WriteJsonAtomicIfMissing(GetLibraryPath(), new List<AIGMUMGLibraryDefinition>(_libraryDefinitions.Values));
                WriteJsonAtomicIfMissing(GetAssignmentsPath(), new List<AIGMUMGAssignment>(_assignments.Values));
                WriteJsonAtomicIfMissing(GetVersionsPath(), _versions);
            }
            catch
            {
            }
        }

        private static void PersistProposals()
        {
            WriteJsonAtomic(GetProposalPath(), new List<AIGMUMGBlockProposal>(_proposals.Values));
        }

        private static void PersistLibraryDefinitions()
        {
            WriteJsonAtomic(GetLibraryPath(), new List<AIGMUMGLibraryDefinition>(_libraryDefinitions.Values));
        }

        private static void PersistAssignments()
        {
            WriteJsonAtomic(GetAssignmentsPath(), new List<AIGMUMGAssignment>(_assignments.Values));
        }

        private static void PersistVersions()
        {
            WriteJsonAtomic(GetVersionsPath(), _versions);
        }

        private static string GetProposalPath()
        {
            return Path.Combine(DataRoot, "Proposals", "agent_proposals.json");
        }

        private static string GetLibraryPath()
        {
            return Path.Combine(DataRoot, "Library", "library_definitions_v2.json");
        }

        private static string GetAssignmentsPath()
        {
            return Path.Combine(DataRoot, "Assignments", "assignments_v2.json");
        }

        private static string GetVersionsPath()
        {
            return Path.Combine(DataRoot, "Versions", "versions_v2.json");
        }

        private static void BackupV1SidecarsOnce()
        {
            try
            {
                string marker = Path.Combine(DataRoot, "MigrationBackups", "phase64c2_v1_backup.marker");
                if (File.Exists(marker))
                    return;

                string dir = Path.Combine(DataRoot, "MigrationBackups", "phase64c2_" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
                Directory.CreateDirectory(dir);
                CopyIfExists(Path.Combine(DataRoot, "Templates", "phase64c_initial_templates.json"), Path.Combine(dir, "phase64c_initial_templates.json"));
                CopyIfExists(Path.Combine(DataRoot, "Migrations", "phase64c_default_sleeves.json"), Path.Combine(dir, "phase64c_default_sleeves.json"));
                CopyIfExists(GetProposalPath(), Path.Combine(dir, "agent_proposals.json"));
                File.WriteAllText(marker, dir);
            }
            catch (Exception ex)
            {
                AIGMUMGLog.Write("migration_backup_warning", null, AIGMUMGLog.Fields("error", ex.Message));
            }
        }

        private static void CopyIfExists(string source, string destination)
        {
            if (File.Exists(source))
                File.Copy(source, destination, true);
        }

        public static T RoundTripClone<T>(T value)
        {
            if (value == null)
                return default(T);

            using (MemoryStream stream = new MemoryStream())
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
                serializer.WriteObject(stream, value);
                stream.Position = 0;
                return (T)serializer.ReadObject(stream);
            }
        }

        private static bool TryReadJson<T>(string path, out T value)
        {
            value = default(T);
            try
            {
                if (String.IsNullOrWhiteSpace(path) || !File.Exists(path) || new FileInfo(path).Length == 0)
                    return false;

                byte[] bytes = File.ReadAllBytes(path);
                int offset = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? 3 : 0;
                using (MemoryStream stream = new MemoryStream(bytes, offset, bytes.Length - offset))
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
                    value = (T)serializer.ReadObject(stream);
                    return true;
                }
            }
            catch (Exception ex)
            {
                AIGMUMGLog.Write("repository_load_warning", null, AIGMUMGLog.Fields("path", path, "error", ex.Message));
                return false;
            }
        }

        private static void WriteJsonAtomicIfMissing<T>(string path, T value)
        {
            if (!File.Exists(path))
                WriteJsonAtomic(path, value);
        }

        private static void WriteJsonAtomic<T>(string path, T value)
        {
            if (String.IsNullOrWhiteSpace(path))
                return;

            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string temp = path + ".tmp";
            using (FileStream stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
                serializer.WriteObject(stream, value);
            }

            if (File.Exists(path))
            {
                string backup = path + "." + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".bak";
                File.Copy(path, backup, true);
            }

            File.Copy(temp, path, true);
            File.Delete(temp);
        }

        private static string NormalizeId(string value)
        {
            string text = (value ?? String.Empty).Trim().ToLowerInvariant();
            text = text.Replace(" ", "_").Replace("-", "_");
            return text;
        }
    }
}
