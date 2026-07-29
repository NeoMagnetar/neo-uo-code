using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using Server.Mobiles;

namespace Server.Custom.AIGM.UMG
{
    public static class AIGMUMGPhase64C2Invariant
    {
        public const string ExecutionStatus = "PREVIEW_ONLY_NOT_DISPATCHED";
        public const string DisabledReason = "Live tactical execution is disabled in Phase64C2. Use PreviewOnly.";

        public static bool TacticalDispatchEnabled
        {
            get { return false; }
        }

        public static AIGMUMGExecutionMode ForcePreviewOnly(AIGMUMGExecutionMode requested)
        {
            return AIGMUMGExecutionMode.PreviewOnly;
        }
    }

    public static class AIGMUMGLibraryService
    {
        public static List<AIGMUMGLibraryDefinition> BuildDefaultDefinitions()
        {
            List<AIGMUMGLibraryDefinition> definitions = new List<AIGMUMGLibraryDefinition>();
            Add(definitions, "FRONTLINE_DEFENDER", "Frontline Defender", AIGMUMGNeoStackKind.CombatDoctrine, "Defense", "Protect allies, intercept direct threats, and respect pursuit boundaries.", "protect_group", AIGMUMGCapabilityKind.CanGuard, AIGMUMGCapabilityKind.CanUseAxe);
            Add(definitions, "AGGRESSIVE_BERSERKER", "Aggressive Berserker", AIGMUMGNeoStackKind.CombatDoctrine, "Combat", "High pressure against validated hostile targets while governance blocks reckless action.", "aggression_level", AIGMUMGCapabilityKind.CanGuard);
            Add(definitions, "CAUTIOUS_ARCHER", "Cautious Archer", AIGMUMGNeoStackKind.CombatDoctrine, "Combat", "Prefer ranged pressure and avoid poor melee exposure.", "preferred_range_tiles", AIGMUMGCapabilityKind.CanUseBow);
            Add(definitions, "REAR_LINE_ARCHER", "Rear-Line Archer", AIGMUMGNeoStackKind.MovementPositioning, "Positioning", "Maintain range, protect line of sight, and avoid unsupported melee exposure.", "preferred_range_tiles", AIGMUMGCapabilityKind.CanUseBow, AIGMUMGCapabilityKind.CanFollow);
            Add(definitions, "HEALER_SUPPORT", "Healer Support", AIGMUMGNeoStackKind.SkillsSpellsResources, "Healing", "Prioritize validated ally healing and cure paths.", "heal_ally_below_pct", AIGMUMGCapabilityKind.CanHeal, AIGMUMGCapabilityKind.CanBandage);
            Add(definitions, "WARRIOR_PRIEST", "Warrior Priest", AIGMUMGNeoStackKind.SkillsSpellsResources, "Hybrid", "Preserve mana, cure first, then guard with weapon or bandage fallback.", "mana_reserve_pct", AIGMUMGCapabilityKind.CanGuard, AIGMUMGCapabilityKind.CanBandage);
            Add(definitions, "ANTI_MAGE", "Anti-Mage", AIGMUMGNeoStackKind.CombatDoctrine, "Combat", "Prioritize validated enemy casters without inventing spell disruption.", "target_priority", AIGMUMGCapabilityKind.CanGuard);
            Add(definitions, "ANTI_DEMON", "Anti-Demon", AIGMUMGNeoStackKind.CombatDoctrine, "Combat", "Prioritize demonic enemies only when lawful hostile validation already exists.", "target_priority", AIGMUMGCapabilityKind.CanGuard);
            Add(definitions, "SCOUT_TRACKER", "Scout Tracker", AIGMUMGNeoStackKind.TrackingAwareness, "Tracking", "Track and report target trails through existing tracking services.", "assistance_radius_tiles", AIGMUMGCapabilityKind.CanTrack);
            Add(definitions, "COUNTER_TRACKER", "Counter-Tracker", AIGMUMGNeoStackKind.TrackingAwareness, "Tracking", "Detect pursuit and alert allies when tracking evidence supports it.", "assistance_radius_tiles", AIGMUMGCapabilityKind.CanTrack, AIGMUMGCapabilityKind.CanDetectHidden);
            Add(definitions, "STEALTH_ASSASSIN", "Stealth Assassin", AIGMUMGNeoStackKind.SituationalOverlays, "Stealth", "Use concealment and repositioning only when real hiding and stealth capability exists.", "cooldown_seconds", AIGMUMGCapabilityKind.CanHide, AIGMUMGCapabilityKind.CanStealth);
            Add(definitions, "BROTHERHOOD_SQUAD", "Brotherhood Squad", AIGMUMGNeoStackKind.SquadRelationshipOperations, "Squad", "Coordinate squad intent without personal-name behavior or new dispatch.", "protect_group", AIGMUMGCapabilityKind.CanReceiveSquadOrders);
            Add(definitions, "JOINING_HUNTER", "Joining Hunter", AIGMUMGNeoStackKind.TrackingAwareness, "Tracking", "Track and pressure Joinings within bounded pursuit policy.", "pursuit_tiles", AIGMUMGCapabilityKind.CanTrack, AIGMUMGCapabilityKind.CanGuard);
            Add(definitions, "PROTECT_OWNER", "Protect Owner", AIGMUMGNeoStackKind.SituationalOverlays, "Defense", "Prioritize owner protection through validated guard preview.", "protect_actor", AIGMUMGCapabilityKind.CanGuard);
            Add(definitions, "PROTECT_CIVILIAN", "Protect Civilian", AIGMUMGNeoStackKind.SituationalOverlays, "Defense", "Protect civilian targets and suppress reckless pursuit.", "protect_actor", AIGMUMGCapabilityKind.CanGuard);
            Add(definitions, "HOLD_THE_LINE", "Hold the Line", AIGMUMGNeoStackKind.MovementPositioning, "Positioning", "Hold position unless governance or retreat overlay outranks it.", "regroup_distance_tiles", AIGMUMGCapabilityKind.CanGuard);
            Add(definitions, "RETREAT_AND_REGROUP", "Retreat and Regroup", AIGMUMGNeoStackKind.SituationalOverlays, "Survival", "Retreat to assigned regroup point in preview only.", "retreat_health_pct", AIGMUMGCapabilityKind.CanFollow);
            Add(definitions, "TOWN_PEACE_MODE", "Town Peace Mode", AIGMUMGNeoStackKind.SituationalOverlays, "Governance", "Suppress proactive aggression in guarded or town regions.", "aggression_level", AIGMUMGCapabilityKind.CanGuard);
            Add(definitions, "DUNGEON_FORMATION", "Dungeon Formation", AIGMUMGNeoStackKind.MovementPositioning, "Squad", "Prefer compact formation, line of sight, and regrouping.", "regroup_distance_tiles", AIGMUMGCapabilityKind.CanFollow);
            Add(definitions, "ESCORT", "Escort", AIGMUMGNeoStackKind.MovementPositioning, "Defense", "Escort a protected mobile using existing follow and guard previews.", "protect_actor", AIGMUMGCapabilityKind.CanFollow, AIGMUMGCapabilityKind.CanGuard);
            Add(definitions, "SEARCH_AND_RESCUE", "Search and Rescue", AIGMUMGNeoStackKind.TrackingAwareness, "Tracking", "Search an assigned area and report recovery options.", "assistance_radius_tiles", AIGMUMGCapabilityKind.CanTrack);
            Add(definitions, "RESOURCE_CONSERVATION", "Resource Conservation", AIGMUMGNeoStackKind.SkillsSpellsResources, "Resources", "Conserve mana, reagents, ammunition, and potions unless emergency overlays apply.", "mana_reserve_pct", AIGMUMGCapabilityKind.CanGuard);
            Add(definitions, "LAST_STAND", "Last Stand", AIGMUMGNeoStackKind.SituationalOverlays, "Survival", "Low-retreat emergency protection overlay with governance limits.", "retreat_health_pct", AIGMUMGCapabilityKind.CanGuard);
            Add(definitions, "COUNTER_AMBUSH", "Counter Ambush", AIGMUMGNeoStackKind.SituationalOverlays, "Stealth", "Use tracking, hiding, stealth, and repositioning previews when supported.", "assistance_radius_tiles", AIGMUMGCapabilityKind.CanTrack, AIGMUMGCapabilityKind.CanHide, AIGMUMGCapabilityKind.CanStealth);

            LinkConflict(definitions, "LIB.NEOBLOCK.TOWN_PEACE_MODE.v1", "LIB.NEOBLOCK.AGGRESSIVE_BERSERKER.v1");
            LinkConflict(definitions, "LIB.NEOBLOCK.HOLD_THE_LINE.v1", "LIB.NEOBLOCK.RETREAT_AND_REGROUP.v1");
            LinkConflict(definitions, "LIB.NEOBLOCK.RESOURCE_CONSERVATION.v1", "LIB.NEOBLOCK.AGGRESSIVE_BERSERKER.v1");
            return definitions;
        }

        public static AIGMUMGLibraryDefinition Find(IEnumerable<AIGMUMGLibraryDefinition> definitions, string idOrName)
        {
            string key = Normalize(idOrName);
            foreach (AIGMUMGLibraryDefinition definition in definitions)
            {
                if (definition == null)
                    continue;

                if (Normalize(definition.DefinitionId) == key || Normalize(definition.Name) == key)
                    return definition;
            }

            return null;
        }

        public static AIGMUMGCompatibilityResult CheckCompatibility(Mobile actor, AIGMUMGLibraryDefinition definition)
        {
            AIGMUMGCompatibilityResult result = new AIGMUMGCompatibilityResult();
            if (actor == null || definition == null)
            {
                result.State = AIGMUMGCompatibilityState.Unknown;
                result.Explanation = "actor_or_definition_missing";
                return result;
            }

            AIGMCapabilitySnapshot snapshot = AIGMCapabilityRegistry.CreateSnapshot(actor);
            AIGMCapabilityValidationResult validation = AIGMCapabilityRegistry.Validate(snapshot, definition.CapabilityRequirements);
            for (int i = 0; i < validation.Passed.Count; i++)
                result.RequirementsFound.Add(validation.Passed[i]);
            for (int i = 0; i < validation.Failed.Count; i++)
                result.RequirementsMissing.Add(validation.Failed[i]);

            AddIfContains(result.RequirementsDegraded, snapshot.WaypointTravelStatus, "Degraded", "waypoint_travel_degraded");
            AddIfContains(result.RequirementsUnproven, snapshot.AutonomousTaskStatus, "Registered", "autonomous_task_registered_not_proven_live");
            AddIfContains(result.RequirementsUnproven, snapshot.OffensiveSpellcastingStatus, "Unsupported", "offensive_spell_adapter_unsupported");

            result.Fallback = definition.Fallback;
            if (!validation.IsValid)
                result.State = String.IsNullOrWhiteSpace(definition.Fallback) ? AIGMUMGCompatibilityState.Incompatible : AIGMUMGCompatibilityState.CompatibleWithFallback;
            else if (result.RequirementsDegraded.Count > 0)
                result.State = AIGMUMGCompatibilityState.Degraded;
            else
                result.State = AIGMUMGCompatibilityState.Compatible;

            result.Explanation = AIGMCapabilityRegistry.BuildCompactSummary(snapshot);
            return result;
        }

        private static void Add(List<AIGMUMGLibraryDefinition> list, string stableId, string name, AIGMUMGNeoStackKind stack, string category, string summary, string primaryParameter, params AIGMUMGCapabilityKind[] requirements)
        {
            AIGMUMGLibraryDefinition definition = new AIGMUMGLibraryDefinition
            {
                DefinitionId = "LIB.NEOBLOCK." + stableId + ".v1",
                Name = name,
                Summary = summary,
                Category = category,
                IntendedNeoStack = stack,
                TemplateSourceProvenance = "Phase64C initial template: " + name
            };
            definition.Tags.Add(Normalize(category));
            definition.Tags.Add(Normalize(name));
            definition.SupportedScopes.Add(AIGMUMGTargetScope.NPC);
            definition.SupportedScopes.Add(AIGMUMGTargetScope.Group);
            definition.RecommendedRoles.Add(category);
            for (int i = 0; i < requirements.Length; i++)
                definition.CapabilityRequirements.Add(new AIGMCapabilityRequirement(requirements[i], name));

            AddCommonParameters(definition);
            PromoteParameter(definition, primaryParameter);
            definition.NeoBlock = BuildSevenMoltNeoBlock(definition, stableId);
            list.Add(definition);
        }

        private static AIGMUMGNeoBlock BuildSevenMoltNeoBlock(AIGMUMGLibraryDefinition definition, string stableId)
        {
            AIGMUMGNeoBlock neoBlock = new AIGMUMGNeoBlock
            {
                NeoBlockId = definition.DefinitionId + ".neoblock",
                Name = definition.Name,
                PriorityOrder = 640,
                Enabled = true
            };
            AIGMUMGBlockStack stack = new AIGMUMGBlockStack
            {
                BlockStackId = definition.DefinitionId + ".blockstack",
                Name = definition.Name + " MOLT",
                PriorityOrder = 640,
                Enabled = true
            };

            AddMolt(stack, stableId, AIGMUMGMoltType.Trigger, definition.Name + " Trigger", "Relevant situation matches the configured target, radius, and resource thresholds.");
            AddMolt(stack, stableId, AIGMUMGMoltType.Directive, definition.Name + " Directive", definition.Summary);
            AddMolt(stack, stableId, AIGMUMGMoltType.Instruction, definition.Name + " Instruction", "Use typed parameters for limits. Do not execute; compile a preview intent only.");
            AddMolt(stack, stableId, AIGMUMGMoltType.Subject, definition.Name + " Subject", "Actor, protected targets, enemies, group, faction, region, and capability snapshot.");
            AddMolt(stack, stableId, AIGMUMGMoltType.Primary, definition.Name + " Primary", "Preserve governance, identity, and validated capability truth before tactical preference.");
            AddMolt(stack, stableId, AIGMUMGMoltType.Philosophy, definition.Name + " Philosophy", "Doctrine describes priorities; deterministic parameters define exact limits.");
            AddMolt(stack, stableId, AIGMUMGMoltType.Blueprint, definition.Name + " Blueprint", "Layer sources -> validate capability -> detect conflicts -> produce typed intent -> " + AIGMUMGPhase64C2Invariant.ExecutionStatus);
            neoBlock.BlockStacks.Add(stack);
            return neoBlock;
        }

        private static void AddMolt(AIGMUMGBlockStack stack, string stableId, AIGMUMGMoltType type, string name, string content)
        {
            stack.MoltBlocks.Add(new AIGMUMGBlock
            {
                BlockId = "LIB.NEOBLOCK." + stableId + ".v1." + type.ToString().ToLowerInvariant(),
                Name = name,
                MoltType = type,
                Content = content,
                Summary = content,
                Category = "LibraryDefinition",
                PriorityOrder = 640,
                BlockState = AIGMUMGBlockState.Draft,
                Source = AIGMUMGBlockSource.Template,
                CreatedBy = "phase64c2_library",
                ApprovedBy = String.Empty
            });
        }

        private static void AddCommonParameters(AIGMUMGLibraryDefinition definition)
        {
            AddParam(definition, "aggression_level", "Aggression", AIGMUMGParameterType.Percentage, "50", "0", "100", "%");
            AddParam(definition, "defense_level", "Defense", AIGMUMGParameterType.Percentage, "50", "0", "100", "%");
            AddParam(definition, "pursuit_tiles", "Pursuit distance", AIGMUMGParameterType.TileDistance, "8", "0", "24", "tiles");
            AddParam(definition, "preferred_range_tiles", "Preferred range", AIGMUMGParameterType.TileDistance, "6", "0", "18", "tiles");
            AddParam(definition, "retreat_health_pct", "Retreat threshold", AIGMUMGParameterType.Percentage, "25", "0", "100", "%");
            AddParam(definition, "heal_self_below_pct", "Heal self below", AIGMUMGParameterType.Percentage, "35", "0", "100", "%");
            AddParam(definition, "heal_ally_below_pct", "Heal ally below", AIGMUMGParameterType.Percentage, "45", "0", "100", "%");
            AddParam(definition, "mana_reserve_pct", "Mana reserve", AIGMUMGParameterType.Percentage, "50", "0", "100", "%");
            AddParam(definition, "stamina_reserve_pct", "Stamina reserve", AIGMUMGParameterType.Percentage, "20", "0", "100", "%");
            AddParam(definition, "protect_actor", "Protect actor", AIGMUMGParameterType.ActorReference, "", "", "", "");
            AddParam(definition, "protect_group", "Protect group", AIGMUMGParameterType.GroupReference, "Heroes", "", "", "");
            AddParam(definition, "target_priority", "Target priority", AIGMUMGParameterType.OrderedSelectorList, "threatening_protected,nearest_hostile", "", "", "");
            AddParam(definition, "assistance_radius_tiles", "Assistance radius", AIGMUMGParameterType.TileDistance, "12", "0", "32", "tiles");
            AddParam(definition, "regroup_distance_tiles", "Regroup distance", AIGMUMGParameterType.TileDistance, "4", "0", "16", "tiles");
            AddParam(definition, "cooldown_seconds", "Cooldown", AIGMUMGParameterType.DurationSeconds, "10", "0", "3600", "seconds");
            AddParam(definition, "release_delay_seconds", "Release delay", AIGMUMGParameterType.DurationSeconds, "0", "0", "3600", "seconds");
            AddParam(definition, "expiry_condition", "Expiry condition", AIGMUMGParameterType.String, "", "", "", "");
            AddParam(definition, "allow_secondary_response", "Secondary response", AIGMUMGParameterType.Boolean, "false", "", "", "");
            AddParam(definition, "secondary_response_limit", "Secondary limit", AIGMUMGParameterType.Integer, "1", "0", "8", "responses");
        }

        private static void AddParam(AIGMUMGLibraryDefinition definition, string id, string label, AIGMUMGParameterType type, string defaultValue, string min, string max, string unit)
        {
            definition.Parameters.Add(new AIGMUMGParameterDefinition
            {
                ParameterId = id,
                Label = label,
                Description = label + " parameter for " + definition.Name,
                Type = type,
                Required = false,
                Default = defaultValue,
                Minimum = min,
                Maximum = max,
                Unit = unit,
                ValidationRule = type.ToString()
            });
        }

        private static void PromoteParameter(AIGMUMGLibraryDefinition definition, string id)
        {
            for (int i = 0; i < definition.Parameters.Count; i++)
            {
                if (String.Equals(definition.Parameters[i].ParameterId, id, StringComparison.OrdinalIgnoreCase))
                    definition.Parameters[i].Required = true;
            }
        }

        private static void LinkConflict(List<AIGMUMGLibraryDefinition> definitions, string leftId, string rightId)
        {
            AIGMUMGLibraryDefinition left = Find(definitions, leftId);
            AIGMUMGLibraryDefinition right = Find(definitions, rightId);
            if (left != null && !left.Conflicts.Contains(rightId))
                left.Conflicts.Add(rightId);
            if (right != null && !right.Conflicts.Contains(leftId))
                right.Conflicts.Add(leftId);
        }

        private static void AddIfContains(List<string> list, string value, string needle, string output)
        {
            if (!String.IsNullOrWhiteSpace(value) && value.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                list.Add(output);
        }

        public static string Normalize(string value)
        {
            string text = (value ?? String.Empty).Trim().ToLowerInvariant();
            return text.Replace(" ", "_").Replace("-", "_").Replace(".", "_");
        }
    }

    public static class AIGMUMGAssignmentService
    {
        public static AIGMUMGAssignment CreateDraft(string actorId, string runtimeSerial, AIGMUMGLibraryDefinition definition, IDictionary<string, string> parameterValues, string author)
        {
            AIGMUMGAssignment assignment = new AIGMUMGAssignment
            {
                AssignmentId = "assign." + AIGMUMGLibraryService.Normalize(actorId) + "." + AIGMUMGLibraryService.Normalize(definition.Name) + "." + Guid.NewGuid().ToString("N").Substring(0, 8),
                DefinitionId = definition.DefinitionId,
                DefinitionVersion = definition.Version,
                TargetScope = AIGMUMGTargetScope.NPC,
                TargetCanonicalId = actorId,
                TargetRuntimeSerial = runtimeSerial ?? String.Empty,
                NeoStackId = definition.IntendedNeoStack.ToString(),
                State = AIGMUMGAssignmentState.Draft,
                ExecutionMode = AIGMUMGExecutionMode.PreviewOnly,
                PreviewParticipation = AIGMUMGPreviewParticipation.Inactive,
                Enabled = false,
                PriorityOrder = 640,
                CreatedBy = String.IsNullOrWhiteSpace(author) ? "unknown" : author,
                Notes = "Phase64C2 authoring Draft. " + AIGMUMGPhase64C2Invariant.DisabledReason
            };

            for (int i = 0; i < definition.Parameters.Count; i++)
            {
                AIGMUMGParameterDefinition parameter = definition.Parameters[i];
                string value;
                if (parameterValues == null || !parameterValues.TryGetValue(parameter.ParameterId, out value))
                    value = parameter.Default;
                assignment.Parameters.Add(new AIGMUMGParameterValue { ParameterId = parameter.ParameterId, Value = value ?? String.Empty });
            }

            assignment.Conflicts.AddRange(definition.Conflicts);
            assignment.Dependencies.AddRange(definition.Dependencies);
            return assignment;
        }

        public static List<string> ValidateParameters(AIGMUMGLibraryDefinition definition, AIGMUMGAssignment assignment)
        {
            List<string> errors = new List<string>();
            if (definition == null || assignment == null)
            {
                errors.Add("definition_or_assignment_missing");
                return errors;
            }

            for (int i = 0; i < definition.Parameters.Count; i++)
            {
                AIGMUMGParameterDefinition parameter = definition.Parameters[i];
                string value = GetParameter(assignment, parameter.ParameterId);
                if (parameter.Required && String.IsNullOrWhiteSpace(value))
                    errors.Add(parameter.ParameterId + ":required");

                double numeric;
                if ((parameter.Type == AIGMUMGParameterType.Integer || parameter.Type == AIGMUMGParameterType.Decimal || parameter.Type == AIGMUMGParameterType.Percentage || parameter.Type == AIGMUMGParameterType.TileDistance || parameter.Type == AIGMUMGParameterType.DurationSeconds)
                    && !String.IsNullOrWhiteSpace(value)
                    && !Double.TryParse(value, out numeric))
                    errors.Add(parameter.ParameterId + ":not_numeric");

                if (Double.TryParse(value, out numeric))
                {
                    double min;
                    double max;
                    if (Double.TryParse(parameter.Minimum, out min) && numeric < min)
                        errors.Add(parameter.ParameterId + ":below_min");
                    if (Double.TryParse(parameter.Maximum, out max) && numeric > max)
                        errors.Add(parameter.ParameterId + ":above_max");
                }
            }

            if (assignment.ExecutionMode != AIGMUMGExecutionMode.PreviewOnly)
                errors.Add("live_execution_mode_rejected_phase64c2");

            return errors;
        }

        public static string GetParameter(AIGMUMGAssignment assignment, string parameterId)
        {
            if (assignment == null || assignment.Parameters == null)
                return String.Empty;

            for (int i = 0; i < assignment.Parameters.Count; i++)
            {
                if (String.Equals(assignment.Parameters[i].ParameterId, parameterId, StringComparison.OrdinalIgnoreCase))
                    return assignment.Parameters[i].Value ?? String.Empty;
            }

            return String.Empty;
        }
    }

    public static class AIGMUMGComposerService
    {
        public static string SchemaName
        {
            get { return AIGMUMGSleeve.CurrentSchemaName; }
        }

        public static string AddDraft(Mobile actor, string definitionName, IDictionary<string, string> parameterValues, string author)
        {
            if (actor == null)
                return "UMG actor not found.";

            AIGMUMGLibraryDefinition definition = AIGMUMGRepository.GetLibraryDefinition(definitionName);
            if (definition == null)
                return "Library definition not found: " + definitionName;

            string actorId = AIGMUMGRuntimeService.ResolveActorId(actor);
            AIGMUMGAssignment existing = AIGMUMGRepository.FindAssignment(actorId, definition.DefinitionId, AIGMUMGAssignmentState.Draft);
            if (existing != null)
                return "Existing Draft assignment returned: " + existing.AssignmentId;

            AIGMUMGAssignment assignment = AIGMUMGAssignmentService.CreateDraft(actorId, String.Format("0x{0:X8}", actor.Serial.Value), definition, parameterValues, author);
            List<string> validation = AIGMUMGAssignmentService.ValidateParameters(definition, assignment);
            if (validation.Count > 0)
                return "Draft rejected by schema validation: " + String.Join(", ", validation.ToArray());

            AIGMUMGRepository.SaveAssignmentChange(assignment, author, "Add Draft " + definition.Name);
            return String.Format("Draft {0} added for {1}: {2}; execution={3}.", assignment.AssignmentId, actor.Name, definition.Name, assignment.ExecutionMode);
        }

        public static string Approve(Mobile actor, string assignmentId, string approver)
        {
            return SetState(actor, assignmentId, AIGMUMGAssignmentState.Approved, approver, "Approved PreviewOnly");
        }

        public static string Reject(Mobile actor, string assignmentId, string approver, string reason)
        {
            string result = SetState(actor, assignmentId, AIGMUMGAssignmentState.Rejected, approver, "Rejected: " + reason);
            return result;
        }

        public static string Suspend(Mobile actor, string assignmentId, string author)
        {
            AIGMUMGAssignment assignment = ResolveAssignment(actor, assignmentId);
            if (assignment == null)
                return "Assignment not found.";
            assignment.PreviewParticipation = AIGMUMGPreviewParticipation.Suspended;
            assignment.Enabled = false;
            assignment.ModifiedUtc = DateTime.UtcNow;
            AIGMUMGRepository.SaveAssignmentChange(assignment, author, "Suspend Preview");
            return "Preview suspended: " + assignment.AssignmentId;
        }

        public static string Resume(Mobile actor, string assignmentId, string author)
        {
            AIGMUMGAssignment assignment = ResolveAssignment(actor, assignmentId);
            if (assignment == null)
                return "Assignment not found.";
            assignment.ExecutionMode = AIGMUMGExecutionMode.PreviewOnly;
            assignment.PreviewParticipation = AIGMUMGPreviewParticipation.EnabledPreview;
            assignment.Enabled = assignment.State == AIGMUMGAssignmentState.Approved;
            assignment.ModifiedUtc = DateTime.UtcNow;
            AIGMUMGRepository.SaveAssignmentChange(assignment, author, "Resume Preview");
            return "Preview resumed: " + assignment.AssignmentId;
        }

        public static string Rollback(Mobile actor, string versionId, string author)
        {
            if (actor == null)
                return "UMG actor not found.";

            AIGMUMGVersionRecord version = AIGMUMGRepository.GetVersion(versionId);
            if (version == null || version.AssignmentSnapshot == null)
                return "Version not found.";

            AIGMUMGAssignment restored = AIGMUMGRepository.RoundTripClone(version.AssignmentSnapshot);
            restored.ExecutionMode = AIGMUMGExecutionMode.PreviewOnly;
            restored.ModifiedUtc = DateTime.UtcNow;
            AIGMUMGRepository.SaveAssignmentChange(restored, author, "Rollback to " + version.VersionId);
            return "Rollback restored assignment " + restored.AssignmentId + " from version " + version.VersionId + ".";
        }

        public static string ForkDefinition(string definitionName, string newName, string author)
        {
            AIGMUMGLibraryDefinition source = AIGMUMGRepository.GetLibraryDefinition(definitionName);
            if (source == null)
                return "Library definition not found.";

            AIGMUMGLibraryDefinition fork = CloneDefinition(source);
            fork.Name = String.IsNullOrWhiteSpace(newName) ? "Forked " + source.Name : newName.Trim();
            fork.DefinitionId = "LIB.NEOBLOCK." + AIGMUMGLibraryService.Normalize(fork.Name).ToUpperInvariant() + ".v1";
            fork.TemplateSourceProvenance = "Forked from " + source.DefinitionId + " by " + author;
            fork.ModifiedUtc = DateTime.UtcNow;
            if (fork.NeoBlock != null)
            {
                fork.NeoBlock.NeoBlockId = fork.DefinitionId + ".neoblock";
                fork.NeoBlock.Name = fork.Name;
            }

            AIGMUMGRepository.SaveLibraryDefinition(fork, author, "Fork definition");
            return "Forked definition " + fork.DefinitionId + " from " + source.DefinitionId + ".";
        }

        public static string Preview(Mobile actor, string assignmentOrDefinition)
        {
            if (actor == null)
                return "UMG actor not found.";

            string actorId = AIGMUMGRuntimeService.ResolveActorId(actor);
            AIGMUMGAssignment assignment = ResolveAssignment(actor, assignmentOrDefinition);
            AIGMUMGLibraryDefinition definition = assignment != null ? AIGMUMGRepository.GetLibraryDefinition(assignment.DefinitionId) : AIGMUMGRepository.GetLibraryDefinition(assignmentOrDefinition);
            if (definition == null)
                return "No assignment or library definition matched.";

            AIGMUMGCompatibilityResult compatibility = AIGMUMGLibraryService.CheckCompatibility(actor, definition);
            List<string> conflicts = DetectConflicts(actorId, assignment, definition);
            string parameters = assignment != null ? BuildParameterSummary(assignment) : "defaults";
            string intent = InferPreviewIntent(definition);
            string adapter = InferAdapter(definition);
            return String.Format(
                "{0}: {1} on {2}; layer=Assignment Preview; compatibility={3}; conflicts={4}; parameters={5}; typedIntent={6}; adapterMapping={7}; execution={8}",
                actor.Name,
                definition.Name,
                actorId,
                compatibility.BuildSummary(),
                conflicts.Count == 0 ? "none" : String.Join("|", conflicts.ToArray()),
                parameters,
                intent,
                adapter,
                AIGMUMGPhase64C2Invariant.ExecutionStatus);
        }

        public static string Compare(Mobile actor, string assignmentOrDefinition)
        {
            if (actor == null)
                return "UMG actor not found.";

            string actorId = AIGMUMGRuntimeService.ResolveActorId(actor);
            AIGMUMGAssignment assignment = ResolveAssignment(actor, assignmentOrDefinition);
            if (assignment == null)
                return "Assignment not found.";

            AIGMUMGLibraryDefinition definition = AIGMUMGRepository.GetLibraryDefinition(assignment.DefinitionId);
            AIGMUMGSleeve sleeve = AIGMUMGRepository.GetSleeve(actorId);
            int active = sleeve != null ? sleeve.CountBlocks(AIGMUMGBlockState.Active) : 0;
            int draft = sleeve != null ? sleeve.CountBlocks(AIGMUMGBlockState.Draft) : 0;
            return String.Format("Compare {0}: current Sleeve activeMOLT={1} draftMOLT={2}; proposed assignment={3} definition={4} parameters={5}; execution={6}.",
                actor.Name,
                active,
                draft,
                assignment.AssignmentId,
                definition != null ? definition.Name : assignment.DefinitionId,
                BuildParameterSummary(assignment),
                AIGMUMGPhase64C2Invariant.ExecutionStatus);
        }

        public static string BuildOperatorSummary(Mobile actor)
        {
            if (actor == null)
                return "No actor.";

            string actorId = AIGMUMGRuntimeService.ResolveActorId(actor);
            List<AIGMUMGAssignment> assignments = AIGMUMGRepository.GetAssignmentsForTarget(actorId);
            int draft = Count(assignments, AIGMUMGAssignmentState.Draft);
            int approved = Count(assignments, AIGMUMGAssignmentState.Approved);
            return String.Format("PREVIEW ONLY - NO GAMEPLAY EXECUTION | Actor={0} | Current role={1} | Autonomy=Manual | Preview assignments={2} | Draft proposals={3} | Latest preview={4}",
                actor.Name,
                actor.GetType().Name,
                approved,
                draft,
                assignments.Count > 0 ? assignments[assignments.Count - 1].AssignmentId : "none");
        }

        public static string BuildArchitectSummary(Mobile actor)
        {
            if (actor == null)
                return "No actor.";

            string actorId = AIGMUMGRuntimeService.ResolveActorId(actor);
            AIGMUMGSleeve sleeve = AIGMUMGRepository.GetSleeve(actorId);
            List<AIGMUMGAssignment> assignments = AIGMUMGRepository.GetAssignmentsForTarget(actorId);
            return String.Format("Sleeve={0}; NeoStacks={1}; NeoBlocks={2}; MOLT={3}; Assignments={4}; Proposals={5}; Versions={6}; Raw protected; Live runtime state protected.",
                sleeve != null ? sleeve.SleeveId : "missing",
                sleeve != null ? sleeve.NeoStacks.Count : 0,
                CountNeoBlocks(sleeve),
                sleeve != null ? sleeve.CountBlocks(null) : 0,
                assignments.Count,
                AIGMUMGRepository.GetProposals().Count,
                AIGMUMGRepository.GetVersionsForTarget(actorId).Count);
        }

        public static string BuildLibrarySummary(Mobile actor)
        {
            List<AIGMUMGLibraryDefinition> definitions = AIGMUMGRepository.GetLibraryDefinitions();
            List<string> parts = new List<string>();
            for (int i = 0; i < definitions.Count && i < 10; i++)
            {
                AIGMUMGCompatibilityResult result = actor != null ? AIGMUMGLibraryService.CheckCompatibility(actor, definitions[i]) : null;
                parts.Add(definitions[i].Name + (result != null ? "=" + result.State : String.Empty));
            }
            if (definitions.Count > 10)
                parts.Add("+" + (definitions.Count - 10) + " more");
            return "Library definitions=" + definitions.Count + ": " + String.Join(" | ", parts.ToArray());
        }

        public static string BuildVersionSummary(Mobile actor)
        {
            if (actor == null)
                return "No actor.";
            string actorId = AIGMUMGRuntimeService.ResolveActorId(actor);
            List<AIGMUMGVersionRecord> versions = AIGMUMGRepository.GetVersionsForTarget(actorId);
            List<string> parts = new List<string>();
            for (int i = 0; i < versions.Count && i < 8; i++)
                parts.Add(versions[i].VersionId + ":" + versions[i].Summary);
            return "Versions=" + versions.Count + ": " + String.Join(" | ", parts.ToArray());
        }

        public static Dictionary<string, string> ParseParameters(string text)
        {
            Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (String.IsNullOrWhiteSpace(text))
                return values;

            string[] parts = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                int equals = parts[i].IndexOf('=');
                if (equals <= 0)
                    continue;
                string key = parts[i].Substring(0, equals).Trim();
                string value = parts[i].Substring(equals + 1).Trim();
                if (!String.IsNullOrWhiteSpace(key))
                    values[key] = value;
            }

            return values;
        }

        private static string SetState(Mobile actor, string assignmentId, AIGMUMGAssignmentState state, string approver, string summary)
        {
            AIGMUMGAssignment assignment = ResolveAssignment(actor, assignmentId);
            if (assignment == null)
                return "Assignment not found.";

            if (String.Equals(assignment.CreatedBy, "agent", StringComparison.OrdinalIgnoreCase) && state == AIGMUMGAssignmentState.Approved && String.Equals(assignment.CreatedBy, approver, StringComparison.OrdinalIgnoreCase))
                return "Approval rejected: an author or agent cannot approve its own proposal.";

            assignment.State = state;
            assignment.ExecutionMode = AIGMUMGExecutionMode.PreviewOnly;
            assignment.PreviewParticipation = state == AIGMUMGAssignmentState.Approved ? AIGMUMGPreviewParticipation.EnabledPreview : AIGMUMGPreviewParticipation.Inactive;
            assignment.Enabled = state == AIGMUMGAssignmentState.Approved;
            assignment.ApprovedBy = state == AIGMUMGAssignmentState.Approved ? approver : assignment.ApprovedBy;
            assignment.ModifiedUtc = DateTime.UtcNow;
            AIGMUMGRepository.SaveAssignmentChange(assignment, approver, summary);
            return summary + ": " + assignment.AssignmentId + "; execution=" + assignment.ExecutionMode + "; gameplay unchanged.";
        }

        private static AIGMUMGAssignment ResolveAssignment(Mobile actor, string id)
        {
            if (actor == null)
                return null;

            string actorId = AIGMUMGRuntimeService.ResolveActorId(actor);
            return AIGMUMGRepository.FindAssignment(actorId, id);
        }

        private static List<string> DetectConflicts(string actorId, AIGMUMGAssignment assignment, AIGMUMGLibraryDefinition definition)
        {
            List<string> conflicts = new List<string>();
            List<AIGMUMGAssignment> assignments = AIGMUMGRepository.GetAssignmentsForTarget(actorId);
            for (int i = 0; i < assignments.Count; i++)
            {
                AIGMUMGAssignment other = assignments[i];
                if (other == null || other.State == AIGMUMGAssignmentState.Rejected || other.State == AIGMUMGAssignmentState.Archived)
                    continue;
                if (assignment != null && String.Equals(other.AssignmentId, assignment.AssignmentId, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (definition.Conflicts.Contains(other.DefinitionId))
                    conflicts.Add("hard_incompatibility:" + definition.DefinitionId + "<->" + other.DefinitionId);
                if (String.Equals(other.DefinitionId, definition.DefinitionId, StringComparison.OrdinalIgnoreCase))
                    conflicts.Add("duplicate_assignment:" + other.AssignmentId);
            }
            return conflicts;
        }

        private static string BuildParameterSummary(AIGMUMGAssignment assignment)
        {
            if (assignment == null || assignment.Parameters == null || assignment.Parameters.Count == 0)
                return "none";

            List<string> parts = new List<string>();
            for (int i = 0; i < assignment.Parameters.Count && i < 8; i++)
                parts.Add(assignment.Parameters[i].ParameterId + "=" + assignment.Parameters[i].Value);
            if (assignment.Parameters.Count > 8)
                parts.Add("+" + (assignment.Parameters.Count - 8) + " more");
            return String.Join(",", parts.ToArray());
        }

        private static string InferPreviewIntent(AIGMUMGLibraryDefinition definition)
        {
            string text = (definition.Name + " " + definition.Summary + " " + definition.Category).ToLowerInvariant();
            if (text.Contains("heal") || text.Contains("priest"))
                return "Heal/Cure/ReserveMana preview";
            if (text.Contains("archer"))
                return "RangedPosition/Guard preview";
            if (text.Contains("track") || text.Contains("hunter") || text.Contains("rescue"))
                return "Track/Report preview";
            if (text.Contains("retreat") || text.Contains("formation") || text.Contains("escort"))
                return "MovementPolicy preview";
            if (text.Contains("peace"))
                return "SuppressAggression preview";
            return "Guard/Engage preview";
        }

        private static string InferAdapter(AIGMUMGLibraryDefinition definition)
        {
            string text = (definition.Name + " " + definition.Summary + " " + definition.Category).ToLowerInvariant();
            if (text.Contains("heal") || text.Contains("priest"))
                return "AIGMCompanionHealingService/AIGMCompanionSpellService";
            if (text.Contains("track") || text.Contains("hunter"))
                return "AIGMTrackingHuntService";
            if (text.Contains("movement") || text.Contains("formation") || text.Contains("escort") || text.Contains("retreat"))
                return "AIGMSmartMovementService/AIGMRosterTaskService";
            return "AIGMNativeCombatBridge";
        }

        private static int Count(List<AIGMUMGAssignment> assignments, AIGMUMGAssignmentState state)
        {
            int count = 0;
            for (int i = 0; i < assignments.Count; i++)
            {
                if (assignments[i].State == state)
                    count++;
            }
            return count;
        }

        private static int CountNeoBlocks(AIGMUMGSleeve sleeve)
        {
            if (sleeve == null)
                return 0;
            int count = 0;
            for (int i = 0; i < sleeve.NeoStacks.Count; i++)
                count += sleeve.NeoStacks[i].NeoBlocks.Count;
            return count;
        }

        private static AIGMUMGLibraryDefinition CloneDefinition(AIGMUMGLibraryDefinition source)
        {
            return AIGMUMGRepository.RoundTripClone(source);
        }
    }

    public static class AIGMUMGVersionService
    {
        public static AIGMUMGVersionRecord CreateAssignmentVersion(AIGMUMGAssignment assignment, string author, string summary, int versionNumber)
        {
            return new AIGMUMGVersionRecord
            {
                VersionId = "ver." + AIGMUMGLibraryService.Normalize(assignment.TargetCanonicalId) + "." + versionNumber + "." + Guid.NewGuid().ToString("N").Substring(0, 8),
                TargetId = assignment.TargetCanonicalId,
                AssignmentId = assignment.AssignmentId,
                DefinitionId = assignment.DefinitionId,
                VersionNumber = versionNumber,
                Author = author ?? String.Empty,
                Summary = summary ?? String.Empty,
                AssignmentSnapshot = AIGMUMGRepository.RoundTripClone(assignment)
            };
        }
    }
}
