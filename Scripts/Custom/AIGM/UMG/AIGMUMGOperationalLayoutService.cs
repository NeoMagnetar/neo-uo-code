using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using Server.Custom.AIGM.Inventory;
using Server.Mobiles;

namespace Server.Custom.AIGM.UMG
{
    public sealed class AIGMUMGOperationalLayoutActionResult
    {
        public bool Accepted { get; set; }
        public string Code { get; set; }
        public string Message { get; set; }
        public AIGMUMGOperationalLayoutEditSession Session { get; set; }
        public AIGMUMGOperationalLayoutValidationResult Validation { get; set; }

        public AIGMUMGOperationalLayoutActionResult()
        {
            Code = String.Empty;
            Message = String.Empty;
        }
    }

    public static class AIGMUMGOperationalLayoutService
    {
        public const string PreviewOnlyLabel = "APPROVED PREVIEWONLY - NOT LIVE EXECUTION";
        public const string PreviewResult = "PREVIEW_ONLY_NOT_DISPATCHED";
        public const int MaxCustomStackNameLength = 48;

        public const string FamilyAlwaysOn = "always_on_spine";
        public const string FamilyCombat = "combat";
        public const string FamilyPositioning = "positioning";
        public const string FamilyResources = "resources";
        public const string FamilyProtection = "protection";
        public const string FamilyTracking = "tracking_awareness";
        public const string FamilySquad = "squad_relationships";
        public const string FamilyOverlays = "situational_overlays";
        public const string FamilyDiagnostics = "reference_diagnostics";

        private const int SessionMinutes = 30;
        private const string PointerFileName = "operational_layouts_v1.json";
        private const string VersionFileName = "operational_layout_versions_v1.json";
        private static readonly object SyncRoot = new object();
        private static readonly Dictionary<string, AIGMUMGOperationalLayoutEditSession> Sessions = new Dictionary<string, AIGMUMGOperationalLayoutEditSession>(StringComparer.OrdinalIgnoreCase);

        private static readonly string[] FamilyIds =
        {
            FamilyAlwaysOn,
            FamilyCombat,
            FamilyPositioning,
            FamilyResources,
            FamilyProtection,
            FamilyTracking,
            FamilySquad,
            FamilyOverlays,
            FamilyDiagnostics
        };

        private static readonly string[] FamilyNames =
        {
            "Always-On Spine",
            "Combat",
            "Positioning",
            "Resources",
            "Protection",
            "Tracking and Awareness",
            "Squad and Relationships",
            "Situational Overlays",
            "Reference and Diagnostics"
        };

        private static readonly string[] MandatoryAlwaysOnRoles =
        {
            "Governance",
            "Identity",
            "Capability Truth",
            "System Invariants",
            "Operational Authority",
            "Owner/Commander Authority",
            "Safety",
            "Logging"
        };

        public static int GetFamilyCount()
        {
            return FamilyIds.Length;
        }

        public static string GetFamilyIdByIndex(int index)
        {
            return index >= 0 && index < FamilyIds.Length ? FamilyIds[index] : String.Empty;
        }

        public static string ResolveOperationalActorKey(Mobile actor)
        {
            if (actor == null)
                return String.Empty;

            string composerKey = AIGMUMGRuntimeService.ResolveActorId(actor);
            if (String.IsNullOrWhiteSpace(composerKey))
                composerKey = actor.GetType().Name;

            return NormalizeId(composerKey) + ".serial." + actor.Serial.Value.ToString("X8");
        }

        private static string ResolveComposerActorKey(Mobile actor)
        {
            return actor != null ? AIGMUMGRuntimeService.ResolveActorId(actor) : String.Empty;
        }

        public static AIGMUMGOperationalLayout OpenWorkingLayout(Mobile actor, string author)
        {
            if (actor == null)
                return null;

            string actorKey = ResolveOperationalActorKey(actor);
            string composerKey = ResolveComposerActorKey(actor);
            AIGMUMGOperationalLayoutPointer pointer = GetPointer(actorKey);
            AIGMUMGOperationalLayoutVersionRecord version = FindCurrentVersion(pointer);
            AIGMUMGOperationalLayout layout = version != null && version.LayoutSnapshot != null
                ? Clone(version.LayoutSnapshot)
                : BuildDefaultLayout(actor, actorKey, composerKey, pointer);

            layout.ActorKey = actorKey;
            layout.ActorSerialAtSave = FormatSerial(actor);
            if (String.IsNullOrWhiteSpace(layout.CreatedBy))
                layout.CreatedBy = SafeAuthor(author);
            NormalizeLayout(layout);
            return layout;
        }

        public static bool CanUseArchitectMode(Mobile caller, Mobile actor)
        {
            return ValidateAccess(caller, actor, true).Accepted;
        }

        public static void MarkSessionDirty(AIGMUMGOperationalLayoutEditSession session, string message)
        {
            MarkDirty(session, message);
        }

        public static bool CheckFreshForSave(AIGMUMGOperationalLayoutEditSession session, out string message)
        {
            message = String.Empty;
            if (session == null)
            {
                message = "Session missing.";
                return false;
            }

            int current = GetLatestRevision(session.ActorKey);
            if (current != session.BaseRevision)
            {
                message = String.Format("Stale layout session: base revision {0}, current revision {1}. Use Reload before saving.", session.BaseRevision, current);
                return false;
            }

            return true;
        }

        public static void NormalizeOrder(AIGMUMGOperationalLayout layout)
        {
            NormalizeLayout(layout);
        }

        public static AIGMUMGOperationalLayoutValidationResult Validate(Mobile actor, AIGMUMGOperationalLayout layout)
        {
            return ValidateLayout(actor, layout);
        }

        public static string BuildVersionSummary(string actorKey)
        {
            return BuildVersionsSummary(actorKey);
        }

        public static string Compare(string leftVersionId, string rightVersionId)
        {
            return CompareVersions(leftVersionId, rightVersionId);
        }

        public static bool CreateCustomStack(AIGMUMGOperationalLayout layout, string familyId, string displayName, string author, out string message)
        {
            message = String.Empty;
            AIGMUMGOperationalFamily family = FindFamily(layout, familyId);
            if (family == null)
            {
                message = "Select a valid family before creating a stack.";
                return false;
            }

            if (family.Locked || String.Equals(family.FamilyId, FamilyAlwaysOn, StringComparison.OrdinalIgnoreCase))
            {
                message = "Stacks cannot be created in the locked Always-On Spine.";
                return false;
            }

            string error = ValidateStackName(family, displayName, null);
            if (!String.IsNullOrWhiteSpace(error))
            {
                message = error;
                return false;
            }

            AIGMUMGOperationalNeoStack stack = new AIGMUMGOperationalNeoStack
            {
                StackId = "opstack." + NormalizeId(family.FamilyId) + ".custom." + Guid.NewGuid().ToString("N").Substring(0, 10),
                FamilyId = family.FamilyId,
                DisplayName = displayName.Trim(),
                Order = NextOrder(family.OperationalNeoStacks),
                Enabled = true,
                Locked = false,
                SystemDefined = false
            };
            family.OperationalNeoStacks.Add(stack);
            NormalizeLayout(layout);
            message = "Created custom Operational NeoStack: " + stack.DisplayName;
            return true;
        }

        public static bool RenameCustomStack(AIGMUMGOperationalLayout layout, string stackId, string displayName, out string message)
        {
            message = String.Empty;
            AIGMUMGOperationalNeoStack stack = FindStack(layout, stackId);
            if (stack == null)
            {
                message = "Select a stack before renaming.";
                return false;
            }

            if (stack.SystemDefined || stack.Locked || String.Equals(stack.FamilyId, FamilyAlwaysOn, StringComparison.OrdinalIgnoreCase))
            {
                message = "Only unlocked custom Operational NeoStacks can be renamed.";
                return false;
            }

            AIGMUMGOperationalFamily family = FindFamily(layout, stack.FamilyId);
            string error = ValidateStackName(family, displayName, stack.StackId);
            if (!String.IsNullOrWhiteSpace(error))
            {
                message = error;
                return false;
            }

            string old = stack.DisplayName;
            stack.DisplayName = displayName.Trim();
            NormalizeLayout(layout);
            message = "Renamed stack '" + old + "' to '" + stack.DisplayName + "'.";
            return true;
        }

        public static bool AddReference(AIGMUMGOperationalLayout layout, string stackId, AIGMUMGLibraryDefinition definition, string assignmentId, string author, out string message)
        {
            message = String.Empty;
            if (layout == null || definition == null)
            {
                message = "Layout or canonical definition is missing.";
                return false;
            }

            AIGMUMGOperationalNeoStack stack = FindStack(layout, stackId);
            if (stack == null)
            {
                message = "Select an Operational NeoStack before adding a block.";
                return false;
            }

            if (stack.Locked || String.Equals(stack.FamilyId, FamilyAlwaysOn, StringComparison.OrdinalIgnoreCase))
            {
                message = "Selected stack is locked.";
                return false;
            }

            if (ContainsDefinitionInStack(stack, definition.DefinitionId))
            {
                message = "That canonical definition already exists in the selected Operational NeoStack.";
                return false;
            }

            AIGMUMGNeoBlockReference reference = NewReference(
                layout.ActorKey,
                stack,
                definition.DefinitionId,
                assignmentId,
                definition.Name,
                definition.IntendedNeoStack.ToString(),
                false,
                "user_added_reference",
                "LibraryDefinition",
                String.Empty,
                author);
            reference.Order = NextOrder(stack.NeoBlockReferences);
            stack.NeoBlockReferences.Add(reference);
            NormalizeLayout(layout);
            message = "Added canonical NeoBlock reference: " + reference.DisplayName;
            return true;
        }

        public static bool RemoveReference(AIGMUMGOperationalLayout layout, string referenceId, out string message)
        {
            message = String.Empty;
            AIGMUMGOperationalNeoStack stack;
            AIGMUMGNeoBlockReference reference;
            if (!FindReference(layout, referenceId, out stack, out reference))
            {
                message = "Select a reference before removing.";
                return false;
            }

            if (IsMandatory(reference) || (stack != null && stack.Locked))
            {
                message = "Mandatory Always-On Spine references cannot be removed.";
                return false;
            }

            if (!String.IsNullOrWhiteSpace(reference.Notes) || (reference.OptionalLocalParameters != null && reference.OptionalLocalParameters.Count > 0))
            {
                message = "Reference has local parameters or notes; remove requires a confirmation page.";
                return false;
            }

            stack.NeoBlockReferences.Remove(reference);
            NormalizeLayout(layout);
            message = "Removed reference: " + reference.DisplayName;
            return true;
        }

        public static bool MoveReference(AIGMUMGOperationalLayout layout, string referenceId, string action, out string message)
        {
            return MoveReferenceInternal(layout, referenceId, action, String.Empty, out message);
        }

        public static bool MoveReferenceToStack(AIGMUMGOperationalLayout layout, string referenceId, string targetStackId, out string message)
        {
            return MoveReferenceInternal(layout, referenceId, "stack", targetStackId, out message);
        }

        public static bool ToggleReferenceEnabled(AIGMUMGOperationalLayout layout, string referenceId, out string message)
        {
            message = String.Empty;
            AIGMUMGOperationalNeoStack stack;
            AIGMUMGNeoBlockReference reference;
            if (!FindReference(layout, referenceId, out stack, out reference))
            {
                message = "Select a reference first.";
                return false;
            }

            if (IsMandatory(reference))
            {
                message = "Mandatory Always-On Spine references cannot be disabled.";
                return false;
            }

            if (reference.Locked)
            {
                message = "Locked references cannot be enabled or disabled until unlocked.";
                return false;
            }

            reference.Enabled = !reference.Enabled;
            NormalizeLayout(layout);
            message = "Configuration " + (reference.Enabled ? "Enabled" : "Disabled") + ": " + reference.DisplayName;
            return true;
        }

        public static bool ToggleReferenceLock(AIGMUMGOperationalLayout layout, string referenceId, out string message)
        {
            message = String.Empty;
            AIGMUMGOperationalNeoStack stack;
            AIGMUMGNeoBlockReference reference;
            if (!FindReference(layout, referenceId, out stack, out reference))
            {
                message = "Select a reference first.";
                return false;
            }

            if (IsMandatory(reference))
            {
                message = "System-protected references remain locked.";
                return false;
            }

            reference.Locked = !reference.Locked;
            NormalizeLayout(layout);
            message = (reference.Locked ? "Locked" : "Unlocked") + " custom reference: " + reference.DisplayName;
            return true;
        }

        public static AIGMUMGOperationalLayoutActionResult BeginSession(Mobile caller, Mobile actor)
        {
            AIGMUMGOperationalLayoutActionResult result = ValidateAccess(caller, actor, false);
            if (!result.Accepted)
                return result;

            ExpireSessions();
            string actorKey = ResolveOperationalActorKey(actor);
            string composerKey = ResolveComposerActorKey(actor);
            AIGMUMGOperationalLayoutPointer pointer = GetPointer(actorKey);
            AIGMUMGOperationalLayoutVersionRecord version = FindCurrentVersion(pointer);
            AIGMUMGOperationalLayout layout = version != null && version.LayoutSnapshot != null
                ? Clone(version.LayoutSnapshot)
                : BuildDefaultLayout(actor, actorKey, composerKey, pointer);

            int baseRevision = pointer != null ? pointer.LatestRevision : 0;
            string baseVersionId = version != null ? version.VersionId : String.Empty;

            AIGMUMGOperationalLayoutEditSession session = new AIGMUMGOperationalLayoutEditSession
            {
                SessionId = "oplayout.session." + Guid.NewGuid().ToString("N").Substring(0, 12),
                CallerSerial = caller.Serial,
                ActorKey = actorKey,
                ActorSerial = actor.Serial,
                BaseRevision = baseRevision,
                BaseVersionId = baseVersionId,
                WorkingLayout = layout,
                Dirty = false,
                CreatedUtc = DateTime.UtcNow,
                LastInteractionUtc = DateTime.UtcNow,
                ArchitectMode = false,
                View = "Main"
            };
            session.ExpandedFamilyIds.Add(FamilyAlwaysOn);

            lock (SyncRoot)
                Sessions[session.SessionId] = session;

            return Accept("session_opened", "Operator Mode opened. No layout data was written.", session);
        }

        public static AIGMUMGOperationalLayoutEditSession GetSession(string sessionId)
        {
            if (String.IsNullOrWhiteSpace(sessionId))
                return null;

            lock (SyncRoot)
            {
                AIGMUMGOperationalLayoutEditSession session;
                return Sessions.TryGetValue(sessionId, out session) ? session : null;
            }
        }

        public static AIGMUMGOperationalLayoutActionResult TouchSession(Mobile caller, Mobile actor, string sessionId, bool requireArchitect, bool rejectStale)
        {
            ExpireSessions();
            AIGMUMGOperationalLayoutEditSession session = GetSession(sessionId);
            if (session == null)
                return Reject("session_expired", "The organizer session expired or was not found. Reload the organizer.");

            if (DateTime.UtcNow - session.LastInteractionUtc > TimeSpan.FromMinutes(SessionMinutes))
            {
                CancelSession(session.SessionId);
                return Reject("session_expired", "The organizer session expired. No layout data was written.");
            }

            AIGMUMGOperationalLayoutActionResult access = ValidateAccess(caller, actor, requireArchitect);
            if (!access.Accepted)
                return access;

            if (caller == null || caller.Serial != session.CallerSerial)
                return Reject("session_owner_mismatch", "This organizer session belongs to another caller.");

            if (actor == null || actor.Serial != session.ActorSerial)
                return Reject("session_actor_mismatch", "This organizer session belongs to another actor.");

            string actorKey = ResolveOperationalActorKey(actor);
            if (!String.Equals(actorKey, session.ActorKey, StringComparison.OrdinalIgnoreCase))
                return Reject("actor_key_changed", "Actor identity changed; reload before editing.");

            int latestRevision = GetLatestRevision(actorKey);
            if (rejectStale && latestRevision != session.BaseRevision)
                return Reject("stale_revision", String.Format("Stale layout session. Base revision {0}; latest revision {1}. Reload before saving.", session.BaseRevision, latestRevision));

            session.LastInteractionUtc = DateTime.UtcNow;
            return Accept("session_valid", latestRevision != session.BaseRevision ? "A newer layout revision exists. Save will be refused until Reload." : "Session valid.", session);
        }

        public static void CancelSession(string sessionId)
        {
            if (String.IsNullOrWhiteSpace(sessionId))
                return;

            lock (SyncRoot)
                Sessions.Remove(sessionId);
        }

        public static bool ExpireSessionForDiagnostics(string sessionId)
        {
            if (String.IsNullOrWhiteSpace(sessionId))
                return false;

            lock (SyncRoot)
                return Sessions.Remove(sessionId.Trim());
        }

        public static AIGMUMGOperationalLayoutActionResult ToggleArchitectMode(Mobile caller, Mobile actor, AIGMUMGOperationalLayoutEditSession session)
        {
            if (session == null)
                return Reject("session_missing", "Session missing.");

            bool entering = !session.ArchitectMode;
            AIGMUMGOperationalLayoutActionResult validation = ValidateAccess(caller, actor, entering);
            if (!validation.Accepted)
                return validation;

            session.ArchitectMode = entering;
            session.LastMessage = entering ? "Architect Mode enabled for this edit session." : "Operator Mode enabled.";
            return Accept("mode_changed", session.LastMessage, session);
        }

        public static AIGMUMGOperationalLayoutActionResult ReloadSession(Mobile caller, Mobile actor, AIGMUMGOperationalLayoutEditSession session)
        {
            if (session == null)
                return Reject("session_missing", "Session missing.");

            AIGMUMGOperationalLayoutActionResult access = TouchSession(caller, actor, session.SessionId, false, false);
            if (!access.Accepted)
                return access;

            AIGMUMGOperationalLayoutPointer pointer = GetPointer(session.ActorKey);
            AIGMUMGOperationalLayoutVersionRecord version = FindCurrentVersion(pointer);
            session.WorkingLayout = version != null && version.LayoutSnapshot != null
                ? Clone(version.LayoutSnapshot)
                : BuildDefaultLayout(actor, session.ActorKey, ResolveComposerActorKey(actor), pointer);
            session.BaseRevision = pointer != null ? pointer.LatestRevision : 0;
            session.BaseVersionId = version != null ? version.VersionId : String.Empty;
            session.Dirty = false;
            session.LastMessage = "Reloaded latest persisted layout. No version was created.";
            return Accept("reloaded", session.LastMessage, session);
        }

        public static AIGMUMGOperationalLayoutActionResult CreateStack(AIGMUMGOperationalLayoutEditSession session, string familyId, string displayName, string author)
        {
            if (session == null || session.WorkingLayout == null)
                return Reject("session_missing", "Session missing.");

            AIGMUMGOperationalFamily family = FindFamily(session.WorkingLayout, String.IsNullOrWhiteSpace(familyId) ? session.SelectedFamilyId : familyId);
            if (family == null)
                return Reject("family_missing", "Select a family before creating a stack.");
            if (family.Locked || String.Equals(family.FamilyId, FamilyAlwaysOn, StringComparison.OrdinalIgnoreCase))
                return Reject("family_locked", "Stacks cannot be created in the locked Always-On Spine.");

            string nameError = ValidateStackName(family, displayName, null);
            if (!String.IsNullOrWhiteSpace(nameError))
                return Reject("invalid_stack_name", nameError);

            AIGMUMGOperationalNeoStack stack = new AIGMUMGOperationalNeoStack
            {
                StackId = "opstack." + NormalizeId(family.FamilyId) + ".custom." + Guid.NewGuid().ToString("N").Substring(0, 10),
                FamilyId = family.FamilyId,
                DisplayName = displayName.Trim(),
                Order = NextOrder(family.OperationalNeoStacks),
                Enabled = true,
                Locked = false,
                SystemDefined = false
            };

            family.OperationalNeoStacks.Add(stack);
            session.SelectedFamilyId = family.FamilyId;
            session.SelectedStackId = stack.StackId;
            session.SelectedReferenceId = String.Empty;
            MarkDirty(session, "Created custom Operational NeoStack: " + stack.DisplayName);
            return Accept("stack_created", session.LastMessage, session);
        }

        public static AIGMUMGOperationalLayoutActionResult RenameStack(AIGMUMGOperationalLayoutEditSession session, string displayName)
        {
            if (session == null || session.WorkingLayout == null)
                return Reject("session_missing", "Session missing.");

            AIGMUMGOperationalNeoStack stack = FindStack(session.WorkingLayout, session.SelectedStackId);
            if (stack == null)
                return Reject("stack_missing", "Select a stack before renaming.");
            if (stack.SystemDefined || stack.Locked || String.Equals(stack.FamilyId, FamilyAlwaysOn, StringComparison.OrdinalIgnoreCase))
                return Reject("system_stack_rename_rejected", "Only unlocked custom Operational NeoStacks can be renamed.");

            AIGMUMGOperationalFamily family = FindFamily(session.WorkingLayout, stack.FamilyId);
            string nameError = ValidateStackName(family, displayName, stack.StackId);
            if (!String.IsNullOrWhiteSpace(nameError))
                return Reject("invalid_stack_name", nameError);

            string old = stack.DisplayName;
            stack.DisplayName = displayName.Trim();
            MarkDirty(session, "Renamed stack '" + old + "' to '" + stack.DisplayName + "'.");
            return Accept("stack_renamed", session.LastMessage, session);
        }

        public static AIGMUMGOperationalLayoutActionResult AddReference(Mobile actor, AIGMUMGOperationalLayoutEditSession session, string definitionNameOrId, string author)
        {
            if (session == null || session.WorkingLayout == null)
                return Reject("session_missing", "Session missing.");

            AIGMUMGOperationalNeoStack stack = FindStack(session.WorkingLayout, session.SelectedStackId);
            if (stack == null)
                return Reject("stack_missing", "Select an Operational NeoStack before adding a block.");
            if (stack.Locked)
                return Reject("stack_locked", "Selected stack is locked.");

            ReferenceInfo info = ResolveReference(actor, definitionNameOrId);
            if (info == null || !info.Resolved)
                return Reject("definition_missing", "Canonical definition/reference not found: " + definitionNameOrId);

            if (ContainsDefinitionInStack(stack, info.DefinitionId))
                return Reject("duplicate_reference", "That canonical reference already exists in the selected Operational NeoStack.");

            AIGMUMGNeoBlockReference reference = NewReference(
                session.ActorKey,
                stack,
                info.DefinitionId,
                String.Empty,
                info.DisplayName,
                info.CanonicalStack,
                false,
                "user_added_reference",
                info.ReferenceKind,
                String.Empty,
                author);
            reference.Order = NextOrder(stack.NeoBlockReferences);
            stack.NeoBlockReferences.Add(reference);
            session.SelectedReferenceId = reference.ReferenceId;
            MarkDirty(session, "Added reference: " + reference.DisplayName);
            return Accept("reference_added", session.LastMessage, session);
        }

        public static AIGMUMGOperationalLayoutActionResult RemoveReference(AIGMUMGOperationalLayoutEditSession session, bool confirmed)
        {
            if (session == null || session.WorkingLayout == null)
                return Reject("session_missing", "Session missing.");

            AIGMUMGOperationalNeoStack stack;
            AIGMUMGNeoBlockReference reference;
            if (!FindReference(session.WorkingLayout, session.SelectedReferenceId, out stack, out reference))
                return Reject("reference_missing", "Select a reference before removing.");

            if (IsMandatory(reference) || stack.Locked)
                return Reject("mandatory_reference_remove_rejected", "Mandatory Always-On Spine references cannot be removed.");

            bool hasLocalData = !String.IsNullOrWhiteSpace(reference.Notes) || (reference.OptionalLocalParameters != null && reference.OptionalLocalParameters.Count > 0);
            if (hasLocalData && !confirmed)
            {
                session.View = "ConfirmRemove";
                session.LastMessage = "Reference has local parameters or notes. Confirm removal.";
                return Reject("remove_requires_confirmation", session.LastMessage);
            }

            stack.NeoBlockReferences.Remove(reference);
            session.SelectedReferenceId = String.Empty;
            MarkDirty(session, "Removed reference: " + reference.DisplayName);
            return Accept("reference_removed", session.LastMessage, session);
        }

        public static AIGMUMGOperationalLayoutActionResult MoveReference(AIGMUMGOperationalLayoutEditSession session, string action, string targetStackText)
        {
            if (session == null || session.WorkingLayout == null)
                return Reject("session_missing", "Session missing.");

            action = NormalizeMoveAction(action);

            AIGMUMGOperationalNeoStack stack;
            AIGMUMGNeoBlockReference reference;
            if (!FindReference(session.WorkingLayout, session.SelectedReferenceId, out stack, out reference))
                return Reject("reference_missing", "Select a reference before moving.");

            if (IsMandatory(reference) || (stack != null && stack.Locked))
                return Reject("mandatory_reference_move_rejected", "Mandatory Always-On Spine references may not leave the Always-On Spine.");

            List<AIGMUMGNeoBlockReference> refs = stack.NeoBlockReferences;
            SortReferences(refs);
            int index = refs.IndexOf(reference);
            if (index < 0)
                return Reject("reference_missing", "Reference not found in parent stack.");

            AIGMUMGOperationalNeoStack movedToStack = null;

            if (String.Equals(action, "up", StringComparison.OrdinalIgnoreCase) && index > 0)
            {
                refs.RemoveAt(index);
                refs.Insert(index - 1, reference);
            }
            else if (String.Equals(action, "down", StringComparison.OrdinalIgnoreCase) && index < refs.Count - 1)
            {
                refs.RemoveAt(index);
                refs.Insert(index + 1, reference);
            }
            else if (String.Equals(action, "top", StringComparison.OrdinalIgnoreCase))
            {
                refs.RemoveAt(index);
                refs.Insert(0, reference);
            }
            else if (String.Equals(action, "bottom", StringComparison.OrdinalIgnoreCase))
            {
                refs.RemoveAt(index);
                refs.Add(reference);
            }
            else if (String.Equals(action, "stack", StringComparison.OrdinalIgnoreCase))
            {
                AIGMUMGOperationalNeoStack target = ResolveTargetStack(session.WorkingLayout, stack, targetStackText);
                if (target == null)
                    return Reject("target_stack_missing", "No permitted target stack matched.");
                if (target.Locked || String.Equals(target.FamilyId, FamilyAlwaysOn, StringComparison.OrdinalIgnoreCase))
                    return Reject("target_stack_locked", "Reference cannot be moved to a locked or Always-On stack.");
                if (ContainsDefinitionInStack(target, reference.DefinitionId))
                    return Reject("duplicate_reference", "Target stack already contains that canonical reference.");

                refs.Remove(reference);
                reference.Order = NextOrder(target.NeoBlockReferences);
                target.NeoBlockReferences.Add(reference);
                session.SelectedStackId = target.StackId;
                movedToStack = target;
            }
            else
            {
                return Reject("unknown_move", "Unknown movement operation.");
            }

            AssignReferenceOrders(refs);
            if (movedToStack != null)
                AssignReferenceOrders(movedToStack.NeoBlockReferences);
            NormalizeLayout(session.WorkingLayout);
            MarkDirty(session, "Moved reference: " + reference.DisplayName);
            return Accept("reference_moved", session.LastMessage, session);
        }

        private static string NormalizeMoveAction(string action)
        {
            if (String.IsNullOrWhiteSpace(action))
                return String.Empty;

            string value = action.Trim();
            if (String.Equals(value, "moveup", StringComparison.OrdinalIgnoreCase))
                return "up";
            if (String.Equals(value, "movedown", StringComparison.OrdinalIgnoreCase))
                return "down";
            if (String.Equals(value, "movetop", StringComparison.OrdinalIgnoreCase))
                return "top";
            if (String.Equals(value, "movebottom", StringComparison.OrdinalIgnoreCase))
                return "bottom";
            if (String.Equals(value, "movetostack", StringComparison.OrdinalIgnoreCase) || String.Equals(value, "stackmove", StringComparison.OrdinalIgnoreCase))
                return "stack";

            return value;
        }

        public static AIGMUMGOperationalLayoutActionResult ToggleReferenceEnabled(AIGMUMGOperationalLayoutEditSession session)
        {
            AIGMUMGOperationalNeoStack stack;
            AIGMUMGNeoBlockReference reference;
            if (session == null || session.WorkingLayout == null || !FindReference(session.WorkingLayout, session.SelectedReferenceId, out stack, out reference))
                return Reject("reference_missing", "Select a reference first.");
            if (IsMandatory(reference))
                return Reject("mandatory_reference_disable_rejected", "Mandatory Always-On Spine references cannot be disabled.");
            if (reference.Locked)
                return Reject("reference_locked", "Locked references cannot be enabled or disabled until unlocked.");

            reference.Enabled = !reference.Enabled;
            MarkDirty(session, "Configuration " + (reference.Enabled ? "Enabled" : "Disabled") + ": " + reference.DisplayName);
            return Accept("reference_enabled_toggled", session.LastMessage, session);
        }

        public static AIGMUMGOperationalLayoutActionResult ToggleReferenceLock(AIGMUMGOperationalLayoutEditSession session)
        {
            AIGMUMGOperationalNeoStack stack;
            AIGMUMGNeoBlockReference reference;
            if (session == null || session.WorkingLayout == null || !FindReference(session.WorkingLayout, session.SelectedReferenceId, out stack, out reference))
                return Reject("reference_missing", "Select a reference first.");
            if (IsMandatory(reference))
                return Reject("system_lock_rejected", "System-protected references remain locked.");

            reference.Locked = !reference.Locked;
            MarkDirty(session, (reference.Locked ? "Locked" : "Unlocked") + " custom reference: " + reference.DisplayName);
            return Accept("reference_lock_toggled", session.LastMessage, session);
        }

        public static AIGMUMGOperationalLayoutActionResult SaveDraft(Mobile caller, Mobile actor, AIGMUMGOperationalLayoutEditSession session, string author)
        {
            AIGMUMGOperationalLayoutActionResult touch = TouchSession(caller, actor, session != null ? session.SessionId : String.Empty, true, true);
            if (!touch.Accepted)
                return touch;

            NormalizeLayout(session.WorkingLayout);
            AIGMUMGOperationalLayoutValidationResult validation = ValidateLayout(actor, session.WorkingLayout);
            if (!validation.CanSaveDraft)
                return Reject("draft_save_validation_failed", "Draft save rejected: " + String.Join("; ", validation.StructuralErrors.ToArray()), validation);

            return SaveVersion(actor, session, AIGMUMGOperationalLayoutState.Draft, author, "Draft layout saved", String.Empty);
        }

        public static AIGMUMGOperationalLayoutActionResult ApprovePreview(Mobile caller, Mobile actor, AIGMUMGOperationalLayoutEditSession session, string author)
        {
            AIGMUMGOperationalLayoutActionResult touch = TouchSession(caller, actor, session != null ? session.SessionId : String.Empty, true, true);
            if (!touch.Accepted)
                return touch;
            if (session.Dirty)
                return Reject("approve_requires_saved_draft", "Approve PreviewOnly requires a saved Draft. Save Draft first.");

            AIGMUMGOperationalLayoutValidationResult validation = ValidateLayout(actor, session.WorkingLayout);
            if (!validation.CanApprovePreviewOnly)
                return Reject("approval_validation_failed", "Approve PreviewOnly rejected: " + validation.BuildCompactSummary(), validation);

            return SaveVersion(actor, session, AIGMUMGOperationalLayoutState.ApprovedPreviewOnly, author, "Approved PreviewOnly layout - not live execution", String.Empty);
        }

        public static AIGMUMGOperationalLayoutActionResult Rollback(Mobile caller, Mobile actor, AIGMUMGOperationalLayoutEditSession session, string versionId, string author)
        {
            AIGMUMGOperationalLayoutActionResult touch = TouchSession(caller, actor, session != null ? session.SessionId : String.Empty, true, true);
            if (!touch.Accepted)
                return touch;

            AIGMUMGOperationalLayoutVersionRecord source = GetLayoutVersion(versionId);
            if (source == null || source.LayoutSnapshot == null || !String.Equals(source.ActorKey, session.ActorKey, StringComparison.OrdinalIgnoreCase))
                return Reject("rollback_source_missing", "Rollback source version was not found for this actor.");

            session.WorkingLayout = Clone(source.LayoutSnapshot);
            session.WorkingLayout.State = AIGMUMGOperationalLayoutState.Draft;
            session.WorkingLayout.ExecutionMode = AIGMUMGExecutionMode.PreviewOnly;
            session.Dirty = true;
            session.SelectedReferenceId = String.Empty;
            NormalizeLayout(session.WorkingLayout);
            AIGMUMGOperationalLayoutActionResult saved = SaveVersion(actor, session, AIGMUMGOperationalLayoutState.Draft, author, "Rollback clone of " + source.VersionId, source.VersionId);
            if (saved.Accepted)
                saved.Message = "Rollback created new Draft version from " + source.VersionId + ". Historical version was not mutated.";
            return saved;
        }

        public static string Preview(Mobile actor, AIGMUMGOperationalLayoutEditSession session)
        {
            if (actor == null || session == null || session.WorkingLayout == null)
                return "Operational layout preview unavailable. " + PreviewResult;

            AIGMUMGOperationalLayoutValidationResult validation = ValidateLayout(actor, session.WorkingLayout);
            int families = 0;
            int stacks = 0;
            int enabled = 0;
            int disabled = 0;
            int locked = 0;
            int refs = 0;
            int missing = validation.StructuralErrors.Count;

            CountLayout(session.WorkingLayout, out families, out stacks, out refs, out enabled, out disabled, out locked);
            return String.Format(
                "Operational Layout Preview actor={0} serial={1}; layout={2}; session={3}; baseRevision={4}; families={5}; stacks={6}; enabledReferences={7}; disabledReferences={8}; lockedReferences={9}; missingReferences={10}; capabilityResults={11}; conflicts={12}; structuralErrors={13}; warnings={14}; canonicalDefinitions={15}; operationalStacks={16}; referenceCount={17}; finalResult={18}",
                actor.Name,
                FormatSerial(actor),
                session.WorkingLayout.LayoutId,
                session.SessionId,
                session.BaseRevision,
                families,
                stacks,
                enabled,
                disabled,
                locked,
                missing,
                validation.CapabilityNotes.Count,
                validation.HardConflicts.Count + validation.SoftConflicts.Count,
                validation.StructuralErrors.Count,
                validation.Warnings.Count,
                AIGMUMGRepository.GetLibraryDefinitions().Count,
                stacks,
                refs,
                PreviewResult);
        }

        public static string BuildWhy(Mobile actor, AIGMUMGOperationalLayoutEditSession session)
        {
            if (actor == null || session == null || session.WorkingLayout == null)
                return "Why unavailable.";

            AIGMUMGOperationalNeoStack stack;
            AIGMUMGNeoBlockReference reference;
            if (FindReference(session.WorkingLayout, session.SelectedReferenceId, out stack, out reference))
            {
                ReferenceInfo info = ResolveReference(actor, reference.DefinitionId);
                AIGMUMGOperationalLayoutValidationResult validation = ValidateLayout(actor, session.WorkingLayout);
                string capability = CapabilityFor(actor, reference);
                string conflicts = ConflictFor(reference, validation);
                return String.Format(
                    "Why reference={0}; presentBecause={1}; stack={2}; canonical={3}; capability={4}; conflicts={5}; lock={6}; enabled={7}; provenance={8}; origin={9}; execution={10}",
                    reference.DisplayName,
                    String.IsNullOrWhiteSpace(reference.MandatoryRole) ? "operator_layout_reference" : "mandatory_always_on_role:" + reference.MandatoryRole,
                    stack != null ? stack.DisplayName : "missing",
                    info != null ? info.CanonicalStack : reference.CanonicalStack,
                    capability,
                    conflicts,
                    reference.Locked ? "locked" : "unlocked",
                    reference.Enabled ? "Configuration Enabled" : "Configuration Disabled",
                    reference.Provenance,
                    Origin(reference),
                    PreviewResult);
            }

            return "Why layout: default and saved records organize canonical references only; canonical definitions remain immutable; runtime tactical dispatch remains disabled; finalResult=" + PreviewResult;
        }

        public static string BuildVersionsSummary(string actorKey)
        {
            List<AIGMUMGOperationalLayoutVersionRecord> versions = GetLayoutVersionsForActor(actorKey);
            if (versions.Count == 0)
                return "Operational layout versions=0";

            List<string> parts = new List<string>();
            for (int i = 0; i < versions.Count && i < 8; i++)
            {
                AIGMUMGOperationalLayoutVersionRecord version = versions[i];
                int stacks;
                int refs;
                CountVersion(version, out stacks, out refs);
                parts.Add(String.Format("{0}:rev{1}:{2}:{3}:stacks={4}:refs={5}", version.VersionId, version.Revision, version.State, version.Author, stacks, refs));
            }

            return "Operational layout versions=" + versions.Count + ": " + String.Join(" | ", parts.ToArray());
        }

        public static string CompareLatest(string actorKey)
        {
            List<AIGMUMGOperationalLayoutVersionRecord> versions = GetLayoutVersionsForActor(actorKey);
            if (versions.Count < 2)
                return "Need at least two layout versions to compare.";

            return CompareVersions(versions[1].VersionId, versions[0].VersionId);
        }

        public static string CompareVersions(string leftVersionId, string rightVersionId)
        {
            AIGMUMGOperationalLayoutVersionRecord left = GetLayoutVersion(leftVersionId);
            AIGMUMGOperationalLayoutVersionRecord right = GetLayoutVersion(rightVersionId);
            if (left == null || right == null || left.LayoutSnapshot == null || right.LayoutSnapshot == null)
                return "Compare failed: version missing.";

            List<string> changes = new List<string>();
            CompareFamilies(left.LayoutSnapshot, right.LayoutSnapshot, changes);
            CompareStacks(left.LayoutSnapshot, right.LayoutSnapshot, changes);
            CompareReferences(left.LayoutSnapshot, right.LayoutSnapshot, changes);
            if (changes.Count == 0)
                changes.Add("no structural differences");
            return String.Format("Compare {0} -> {1}: {2}", left.VersionId, right.VersionId, String.Join("; ", changes.ToArray()));
        }

        public static string BuildStatus(Mobile actor)
        {
            if (actor == null)
                return "Operational layout status: actor missing.";

            string actorKey = ResolveOperationalActorKey(actor);
            string composerKey = ResolveComposerActorKey(actor);
            AIGMUMGOperationalLayoutPointer pointer = GetPointer(actorKey);
            AIGMUMGOperationalLayoutVersionRecord current = FindCurrentVersion(pointer);
            AIGMUMGOperationalLayout layout = current != null && current.LayoutSnapshot != null ? current.LayoutSnapshot : BuildDefaultLayout(actor, actorKey, composerKey, pointer);
            AIGMUMGOperationalLayoutValidationResult validation = ValidateLayout(actor, layout);
            int families;
            int stacks;
            int refs;
            int enabled;
            int disabled;
            int locked;
            CountLayout(layout, out families, out stacks, out refs, out enabled, out disabled, out locked);
            return String.Format("Operational layout actor={0}; state={1}; mode={2}; revision={3}; baseAssignmentVersion={4}; families={5}; stacks={6}; refs={7}; enabled={8}; disabled={9}; locked={10}; validation={11}; currentVersion={12}; {13}",
                actorKey,
                layout.State,
                layout.ExecutionMode,
                pointer != null ? pointer.LatestRevision : layout.Revision,
                layout.BaseAssignmentVersionId,
                families,
                stacks,
                refs,
                enabled,
                disabled,
                locked,
                validation.BuildCompactSummary(),
                current != null ? current.VersionId : "UNSAVED_DEFAULT_LAYOUT",
                PreviewOnlyLabel);
        }

        public static string BuildAuditAll()
        {
            AIGMUMGOperationalLayoutPointerFile pointers = LoadPointerFile();
            AIGMUMGOperationalLayoutVersionFile versions = LoadVersionFile();
            return String.Format("Operational layout audit: schema=1; pointers={0}; versions={1}; pointerFile={2}; versionFile={3}; tacticalDispatch={4}",
                pointers.Entries.Count,
                versions.Entries.Count,
                GetPointerPath(),
                GetVersionPath(),
                AIGMUMGPhase64C2Invariant.TacticalDispatchEnabled ? "true" : "false");
        }

        public static AIGMUMGOperationalLayoutValidationResult ValidateLayout(Mobile actor, AIGMUMGOperationalLayout layout)
        {
            AIGMUMGOperationalLayoutValidationResult result = new AIGMUMGOperationalLayoutValidationResult();
            if (actor == null || actor.Deleted)
            {
                result.StructuralErrors.Add("actor_missing");
                return result;
            }

            IAIGMCompanionActor companion;
            if (!AIGMUMGSleeveAccessService.IsRegisteredAIGMCompanion(actor, out companion))
                result.StructuralErrors.Add("actor_not_registered_aigm");

            if (layout == null)
            {
                result.StructuralErrors.Add("layout_missing");
                return result;
            }

            string actorKey = ResolveOperationalActorKey(actor);
            if (!String.Equals(NormalizeId(layout.ActorKey), NormalizeId(actorKey), StringComparison.OrdinalIgnoreCase))
                result.StructuralErrors.Add("layout_actor_key_mismatch");

            if (layout.ExecutionMode != AIGMUMGExecutionMode.PreviewOnly)
                result.StructuralErrors.Add("live_execution_mode_rejected");

            HashSet<string> familyIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> stackIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> referenceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, string> referenceStack = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, bool> mandatoryRoles = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < MandatoryAlwaysOnRoles.Length; i++)
                mandatoryRoles[MandatoryAlwaysOnRoles[i]] = false;

            AIGMUMGOperationalFamily always = null;
            for (int i = 0; i < layout.Families.Count; i++)
            {
                AIGMUMGOperationalFamily family = layout.Families[i];
                if (family == null)
                {
                    result.StructuralErrors.Add("family_null");
                    continue;
                }

                if (String.IsNullOrWhiteSpace(family.FamilyId) || !familyIds.Add(family.FamilyId))
                    result.StructuralErrors.Add("family_id_duplicate_or_empty:" + family.FamilyId);
                if (String.IsNullOrWhiteSpace(family.DisplayName))
                    result.StructuralErrors.Add("family_name_empty:" + family.FamilyId);
                if (String.Equals(family.FamilyId, FamilyAlwaysOn, StringComparison.OrdinalIgnoreCase))
                    always = family;

                for (int s = 0; s < family.OperationalNeoStacks.Count; s++)
                {
                    AIGMUMGOperationalNeoStack stack = family.OperationalNeoStacks[s];
                    if (stack == null)
                    {
                        result.StructuralErrors.Add("stack_null:" + family.FamilyId);
                        continue;
                    }

                    if (String.IsNullOrWhiteSpace(stack.StackId) || !stackIds.Add(stack.StackId))
                        result.StructuralErrors.Add("stack_id_duplicate_or_empty:" + stack.StackId);
                    if (!String.Equals(stack.FamilyId, family.FamilyId, StringComparison.OrdinalIgnoreCase))
                        result.StructuralErrors.Add("stack_parent_family_invalid:" + stack.StackId);
                    if (String.IsNullOrWhiteSpace(stack.DisplayName))
                        result.StructuralErrors.Add("stack_name_empty:" + stack.StackId);
                    if (stack.SystemDefined && !IsReservedSystemStackName(stack.StackId, stack.DisplayName))
                        result.StructuralErrors.Add("system_stack_renamed:" + stack.StackId);
                    if (String.Equals(family.FamilyId, FamilyAlwaysOn, StringComparison.OrdinalIgnoreCase) && (!stack.Locked || !stack.Enabled))
                        result.StructuralErrors.Add("always_on_stack_unlocked_or_disabled:" + stack.StackId);
                    if (!stack.SystemDefined)
                    {
                        string error = ValidateStackName(family, stack.DisplayName, stack.StackId);
                        if (!String.IsNullOrWhiteSpace(error))
                            result.StructuralErrors.Add("custom_stack_name_invalid:" + stack.StackId + ":" + error);
                    }

                    HashSet<string> stackDefinitionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    for (int r = 0; r < stack.NeoBlockReferences.Count; r++)
                    {
                        AIGMUMGNeoBlockReference reference = stack.NeoBlockReferences[r];
                        if (reference == null)
                        {
                            result.StructuralErrors.Add("reference_null:" + stack.StackId);
                            continue;
                        }

                        if (String.IsNullOrWhiteSpace(reference.ReferenceId) || !referenceIds.Add(reference.ReferenceId))
                            result.StructuralErrors.Add("reference_id_duplicate_or_empty:" + reference.ReferenceId);
                        referenceStack[reference.ReferenceId] = stack.StackId;
                        if (String.IsNullOrWhiteSpace(reference.DefinitionId))
                            result.StructuralErrors.Add("reference_definition_empty:" + reference.ReferenceId);
                        if (!stackDefinitionIds.Add(reference.DefinitionId))
                            result.StructuralErrors.Add("duplicate_definition_in_stack:" + stack.StackId + ":" + reference.DefinitionId);

                        ReferenceInfo info = ResolveReference(actor, reference.DefinitionId);
                        if (info == null || !info.Resolved)
                            result.StructuralErrors.Add("missing_canonical_reference:" + reference.DefinitionId);

                        if (IsMandatory(reference))
                        {
                            mandatoryRoles[reference.MandatoryRole] = true;
                            if (!String.Equals(family.FamilyId, FamilyAlwaysOn, StringComparison.OrdinalIgnoreCase))
                                result.StructuralErrors.Add("mandatory_reference_outside_always_on:" + reference.ReferenceId);
                            if (!reference.Locked || !reference.Enabled)
                                result.StructuralErrors.Add("mandatory_reference_unlocked_or_disabled:" + reference.ReferenceId);
                        }

                        if (reference.Enabled && info != null && info.LibraryDefinition != null)
                        {
                            AIGMUMGCompatibilityResult compatibility = AIGMUMGLibraryService.CheckCompatibility(actor, info.LibraryDefinition);
                            result.CapabilityNotes.Add(reference.DefinitionId + ":" + ToCapabilityLabel(compatibility.State));
                        }
                    }
                }
            }

            if (always == null)
                result.StructuralErrors.Add("always_on_spine_missing");
            else if (!always.Locked || !always.Enabled || !always.SystemDefined)
                result.StructuralErrors.Add("always_on_spine_not_locked_system_enabled");

            foreach (KeyValuePair<string, bool> role in mandatoryRoles)
            {
                if (!role.Value)
                    result.StructuralErrors.Add("missing_mandatory_always_on_role:" + role.Key);
            }

            AddConflictValidation(layout, result);
            return result;
        }

        public static void PrepareRenderMaps(AIGMUMGOperationalLayoutEditSession session)
        {
            if (session == null)
                return;

            session.RenderedFamilyIds.Clear();
            session.RenderedStackIds.Clear();
            session.RenderedReferenceIds.Clear();
            session.RenderedDefinitionIds.Clear();
            session.RenderedVersionIds.Clear();
        }

        public static string ResolveRenderedFamily(AIGMUMGOperationalLayoutEditSession session, int index)
        {
            return session != null && index >= 0 && index < session.RenderedFamilyIds.Count ? session.RenderedFamilyIds[index] : String.Empty;
        }

        public static string ResolveRenderedStack(AIGMUMGOperationalLayoutEditSession session, int index)
        {
            return session != null && index >= 0 && index < session.RenderedStackIds.Count ? session.RenderedStackIds[index] : String.Empty;
        }

        public static string ResolveRenderedReference(AIGMUMGOperationalLayoutEditSession session, int index)
        {
            return session != null && index >= 0 && index < session.RenderedReferenceIds.Count ? session.RenderedReferenceIds[index] : String.Empty;
        }

        public static string ResolveRenderedDefinition(AIGMUMGOperationalLayoutEditSession session, int index)
        {
            return session != null && index >= 0 && index < session.RenderedDefinitionIds.Count ? session.RenderedDefinitionIds[index] : String.Empty;
        }

        public static string ResolveRenderedVersion(AIGMUMGOperationalLayoutEditSession session, int index)
        {
            return session != null && index >= 0 && index < session.RenderedVersionIds.Count ? session.RenderedVersionIds[index] : String.Empty;
        }

        public static List<AIGMUMGOperationalLayoutVersionRecord> GetLayoutVersionsForActor(string actorKey)
        {
            List<AIGMUMGOperationalLayoutVersionRecord> results = new List<AIGMUMGOperationalLayoutVersionRecord>();
            string key = NormalizeId(actorKey);
            AIGMUMGOperationalLayoutVersionFile file = LoadVersionFile();
            for (int i = 0; i < file.Entries.Count; i++)
            {
                AIGMUMGOperationalLayoutVersionRecord version = file.Entries[i];
                if (version != null && String.Equals(NormalizeId(version.ActorKey), key, StringComparison.OrdinalIgnoreCase))
                    results.Add(version);
            }

            results.Sort(delegate (AIGMUMGOperationalLayoutVersionRecord left, AIGMUMGOperationalLayoutVersionRecord right) { return right.Revision.CompareTo(left.Revision); });
            return results;
        }

        private static AIGMUMGOperationalLayoutActionResult SaveVersion(Mobile actor, AIGMUMGOperationalLayoutEditSession session, AIGMUMGOperationalLayoutState state, string author, string summary, string sourceVersionId)
        {
            string actorKey = session.ActorKey;
            AIGMUMGOperationalLayoutPointerFile pointerFile = LoadPointerFile();
            AIGMUMGOperationalLayoutVersionFile versionFile = LoadVersionFile();
            AIGMUMGOperationalLayoutPointer pointer = FindPointer(pointerFile, actorKey);
            if (pointer == null)
            {
                pointer = new AIGMUMGOperationalLayoutPointer { ActorKey = actorKey };
                pointerFile.Entries.Add(pointer);
            }

            if (pointer.LatestRevision != session.BaseRevision)
                return Reject("stale_revision", String.Format("Stale layout session. Base revision {0}; latest revision {1}. Reload before saving.", session.BaseRevision, pointer.LatestRevision));

            int revision = pointer.LatestRevision + 1;
            AIGMUMGOperationalLayout snapshot = Clone(session.WorkingLayout);
            snapshot.SchemaVersion = AIGMUMGOperationalLayout.CurrentSchemaVersion;
            snapshot.LayoutId = "layout." + NormalizeId(actorKey) + ".r" + revision + "." + Guid.NewGuid().ToString("N").Substring(0, 8);
            snapshot.ActorKey = actorKey;
            snapshot.ActorSerialAtSave = FormatSerial(actor);
            snapshot.BaseAssignmentVersionId = GetLatestAssignmentVersionId(ResolveComposerActorKey(actor));
            snapshot.State = state;
            snapshot.ExecutionMode = AIGMUMGExecutionMode.PreviewOnly;
            snapshot.Revision = revision;
            if (String.IsNullOrWhiteSpace(snapshot.CreatedBy))
                snapshot.CreatedBy = SafeAuthor(author);
            if (snapshot.CreatedUtc == DateTime.MinValue)
                snapshot.CreatedUtc = DateTime.UtcNow;
            snapshot.UpdatedUtc = DateTime.UtcNow;
            snapshot.UpdatedBy = SafeAuthor(author);
            snapshot.Provenance = String.IsNullOrWhiteSpace(sourceVersionId) ? "organizer_save" : "rollback_clone:" + sourceVersionId;
            NormalizeLayout(snapshot);

            AIGMUMGOperationalLayoutVersionRecord version = new AIGMUMGOperationalLayoutVersionRecord
            {
                VersionId = "opver." + NormalizeId(actorKey) + "." + revision + "." + Guid.NewGuid().ToString("N").Substring(0, 8),
                ActorKey = actorKey,
                Revision = revision,
                State = state,
                ExecutionMode = AIGMUMGExecutionMode.PreviewOnly,
                BaseAssignmentVersionId = snapshot.BaseAssignmentVersionId,
                Author = SafeAuthor(author),
                TimestampUtc = DateTime.UtcNow,
                Summary = summary,
                CorrelationId = "phase64d1d-" + Guid.NewGuid().ToString("N").Substring(0, 12),
                SourceVersionId = sourceVersionId ?? String.Empty,
                LayoutSnapshot = snapshot
            };

            versionFile.Entries.Insert(0, version);
            pointer.LatestRevision = revision;
            pointer.UpdatedUtc = DateTime.UtcNow;
            pointer.UpdatedBy = SafeAuthor(author);
            if (state == AIGMUMGOperationalLayoutState.Draft)
                pointer.CurrentDraftVersionId = version.VersionId;
            else if (state == AIGMUMGOperationalLayoutState.ApprovedPreviewOnly)
                pointer.CurrentApprovedPreviewVersionId = version.VersionId;

            SaveVersionFile(versionFile);
            SavePointerFile(pointerFile);

            session.WorkingLayout = Clone(snapshot);
            session.BaseRevision = revision;
            session.BaseVersionId = version.VersionId;
            session.Dirty = false;
            session.LastMessage = summary + ": " + version.VersionId + "; " + PreviewOnlyLabel;
            AIGMUMGLog.Write("operational_layout_saved", actor, AIGMUMGLog.Fields("versionId", version.VersionId, "actor", actorKey, "revision", revision.ToString(), "state", state.ToString(), "author", SafeAuthor(author), "execution", AIGMUMGExecutionMode.PreviewOnly.ToString()));
            return Accept("layout_version_saved", session.LastMessage, session);
        }

        private static AIGMUMGOperationalLayoutActionResult ValidateAccess(Mobile caller, Mobile actor, bool requireArchitect)
        {
            if (caller == null || caller.Deleted || caller.NetState == null)
                return Reject("caller_not_connected", "Organizer access rejected: caller is not connected.");
            if (actor == null || actor.Deleted)
                return Reject("actor_missing", "Organizer access rejected: actor no longer exists.");

            AIGMUMGSleeveAccessResult access = AIGMUMGSleeveAccessService.ValidateForGumpButton(caller, actor);
            if (!access.Accepted)
                return Reject(access.ResultCode, access.Message);

            if (requireArchitect)
            {
                string authorization;
                if (!AIGMUMGSleeveAccessService.IsAuthorizedCaller(caller, actor, out authorization))
                    return Reject("architect_unauthorized", "Architect Mode rejected: caller is not authorized.");
            }

            return Accept("access_ok", "Access accepted.", null);
        }

        private static AIGMUMGOperationalLayout BuildDefaultLayout(Mobile actor, string actorKey, string composerActorKey, AIGMUMGOperationalLayoutPointer pointer)
        {
            DateTime now = DateTime.UtcNow;
            AIGMUMGOperationalLayout layout = new AIGMUMGOperationalLayout
            {
                SchemaVersion = AIGMUMGOperationalLayout.CurrentSchemaVersion,
                LayoutId = "UNSAVED_DEFAULT_LAYOUT." + NormalizeId(actorKey),
                ActorKey = actorKey,
                ActorSerialAtSave = FormatSerial(actor),
                BaseAssignmentVersionId = GetLatestAssignmentVersionId(composerActorKey),
                State = AIGMUMGOperationalLayoutState.UnsavedDefaultLayout,
                ExecutionMode = AIGMUMGExecutionMode.PreviewOnly,
                Revision = pointer != null ? pointer.LatestRevision : 0,
                CreatedUtc = now,
                CreatedBy = "deterministic_default",
                UpdatedUtc = now,
                UpdatedBy = "deterministic_default",
                Provenance = "UNSAVED_DEFAULT_LAYOUT"
            };

            AddDefaultFamilies(layout);
            AIGMUMGSleeve sleeve = AIGMUMGRepository.GetSleeve(composerActorKey);
            AddCanonicalSleeveReferences(layout, sleeve, actorKey);
            AddAssignmentReferences(layout, actorKey, composerActorKey);
            NormalizeLayout(layout);
            return layout;
        }

        private static void AddDefaultFamilies(AIGMUMGOperationalLayout layout)
        {
            for (int i = 0; i < FamilyIds.Length; i++)
            {
                AIGMUMGOperationalFamily family = new AIGMUMGOperationalFamily
                {
                    FamilyId = FamilyIds[i],
                    DisplayName = FamilyNames[i],
                    Order = (i + 1) * 100,
                    Enabled = true,
                    Locked = String.Equals(FamilyIds[i], FamilyAlwaysOn, StringComparison.OrdinalIgnoreCase),
                    SystemDefined = true
                };
                layout.Families.Add(family);
            }

            AddSystemStack(layout, FamilyAlwaysOn, "always.governance", "Governance", true);
            AddSystemStack(layout, FamilyAlwaysOn, "always.identity", "Identity", true);
            AddSystemStack(layout, FamilyAlwaysOn, "always.capability_truth", "Capability Truth", true);
            AddSystemStack(layout, FamilyAlwaysOn, "always.system_invariants", "System Invariants", true);
            AddSystemStack(layout, FamilyAlwaysOn, "always.operational_authority", "Operational Authority", true);
            AddSystemStack(layout, FamilyAlwaysOn, "always.owner_commander_authority", "Owner/Commander Authority", true);
            AddSystemStack(layout, FamilyAlwaysOn, "always.safety", "Safety", true);
            AddSystemStack(layout, FamilyAlwaysOn, "always.logging", "Logging", true);
            AddSystemStack(layout, FamilyCombat, "combat.canonical", "Combat Doctrine", false);
            AddSystemStack(layout, FamilyPositioning, "positioning.canonical", "Movement and Positioning", false);
            AddSystemStack(layout, FamilyResources, "resources.canonical", "Skills, Spells, and Resources", false);
            AddSystemStack(layout, FamilyProtection, "protection.canonical", "Protection", false);
            AddSystemStack(layout, FamilyTracking, "tracking.canonical", "Tracking and Awareness", false);
            AddSystemStack(layout, FamilySquad, "squad.canonical", "Squad and Relationships", false);
            AddSystemStack(layout, FamilyOverlays, "overlays.canonical", "Situational Overlays", false);
            AddSystemStack(layout, FamilyDiagnostics, "diagnostics.unclassified", "Unclassified", false);
        }

        private static void AddCanonicalSleeveReferences(AIGMUMGOperationalLayout layout, AIGMUMGSleeve sleeve, string actorKey)
        {
            if (sleeve == null || sleeve.NeoStacks == null)
                return;

            AIGMUMGBlock governance = null;
            AIGMUMGBlock engine = null;
            AIGMUMGBlock operational = null;
            AIGMUMGBlock protectedTargets = null;

            for (int s = 0; s < sleeve.NeoStacks.Count; s++)
            {
                AIGMUMGNeoStack stack = sleeve.NeoStacks[s];
                if (stack == null)
                    continue;

                List<AIGMUMGBlock> blocks = FlattenStackBlocks(stack);
                for (int b = 0; b < blocks.Count; b++)
                {
                    AIGMUMGBlock block = blocks[b];
                    if (block == null)
                        continue;

                    string stackId = MapSleeveBlockToOperationalStack(stack, block);
                    string role = MandatoryRoleFor(stack, block);
                    AIGMUMGOperationalNeoStack target = FindStack(layout, stackId);
                    if (target == null)
                        target = FindStack(layout, "diagnostics.unclassified");

                    AddReferenceIfAbsent(layout, target, actorKey, block.BlockId, String.Empty, block.Name, stack.Name, true, "canonical_sleeve_import", "CanonicalSleeveBlock", role, "System");

                    if (stack.StackKind == AIGMUMGNeoStackKind.Governance && governance == null)
                        governance = block;
                    if (Contains(block.Name, "Engine") || Contains(block.Name, "Invariant"))
                        engine = block;
                    if (Contains(block.Name, "Operational") || Contains(block.Name, "Hold"))
                        operational = block;
                    if (Contains(block.Name, "Protected") || Contains(block.Name, "Target") || Contains(block.Name, "Safety"))
                        protectedTargets = block;
                }
            }

            AddRoleAlias(layout, actorKey, "Governance", governance, "always.governance");
            AddRoleAlias(layout, actorKey, "System Invariants", engine, "always.system_invariants");
            AddRoleAlias(layout, actorKey, "Operational Authority", operational, "always.operational_authority");
            AddRoleAlias(layout, actorKey, "Owner/Commander Authority", operational, "always.owner_commander_authority");
            AddRoleAlias(layout, actorKey, "Safety", protectedTargets, "always.safety");
            AddRoleAlias(layout, actorKey, "Logging", engine, "always.logging");
        }

        private static void AddRoleAlias(AIGMUMGOperationalLayout layout, string actorKey, string role, AIGMUMGBlock block, string stackId)
        {
            if (block == null)
                return;

            AIGMUMGOperationalNeoStack stack = FindStack(layout, stackId);
            if (stack == null)
                return;

            AddReferenceIfAbsent(layout, stack, actorKey, block.BlockId, String.Empty, block.Name, stack.DisplayName, true, "canonical_sleeve_role_alias", "CanonicalSleeveBlock", role, "System");
        }

        private static void AddAssignmentReferences(AIGMUMGOperationalLayout layout, string actorKey, string composerActorKey)
        {
            List<AIGMUMGAssignment> assignments = AIGMUMGRepository.GetAssignmentsForTarget(composerActorKey);
            assignments.Sort(CompareAssignmentsForDefault);
            for (int i = 0; i < assignments.Count; i++)
            {
                AIGMUMGAssignment assignment = assignments[i];
                if (assignment == null)
                    continue;

                AIGMUMGLibraryDefinition definition = AIGMUMGRepository.GetLibraryDefinition(assignment.DefinitionId);
                string stackId = definition != null ? MapDefinitionToOperationalStack(definition) : "diagnostics.unclassified";
                AIGMUMGOperationalNeoStack stack = FindStack(layout, stackId);
                if (stack == null)
                    stack = FindStack(layout, "diagnostics.unclassified");

                AddReferenceIfAbsent(layout, stack, actorKey, assignment.DefinitionId, assignment.AssignmentId, definition != null ? definition.Name : assignment.DefinitionId, definition != null ? definition.IntendedNeoStack.ToString() : assignment.NeoStackId, false, "assignment_import", "LibraryDefinition", String.Empty, assignment.CreatedBy);
            }
        }

        private static void AddReferenceIfAbsent(AIGMUMGOperationalLayout layout, AIGMUMGOperationalNeoStack stack, string actorKey, string definitionId, string assignmentId, string displayName, string canonicalStack, bool locked, string provenance, string kind, string mandatoryRole, string author)
        {
            if (stack == null || String.IsNullOrWhiteSpace(definitionId))
                return;

            for (int i = 0; i < stack.NeoBlockReferences.Count; i++)
            {
                AIGMUMGNeoBlockReference existing = stack.NeoBlockReferences[i];
                if (existing != null
                    && String.Equals(existing.DefinitionId, definitionId, StringComparison.OrdinalIgnoreCase)
                    && String.Equals(existing.AssignmentId, assignmentId ?? String.Empty, StringComparison.OrdinalIgnoreCase)
                    && String.Equals(existing.MandatoryRole, mandatoryRole ?? String.Empty, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            AIGMUMGNeoBlockReference reference = NewReference(actorKey, stack, definitionId, assignmentId, displayName, canonicalStack, locked, provenance, kind, mandatoryRole, author);
            reference.Order = NextOrder(stack.NeoBlockReferences);
            stack.NeoBlockReferences.Add(reference);
        }

        private static AIGMUMGNeoBlockReference NewReference(string actorKey, AIGMUMGOperationalNeoStack stack, string definitionId, string assignmentId, string displayName, string canonicalStack, bool locked, string provenance, string kind, string mandatoryRole, string author)
        {
            string sourceStable = String.IsNullOrWhiteSpace(assignmentId) ? definitionId : assignmentId;
            string stable = String.IsNullOrWhiteSpace(mandatoryRole) ? sourceStable : sourceStable + "." + mandatoryRole;
            stable = NormalizeId(stable);
            return new AIGMUMGNeoBlockReference
            {
                ReferenceId = "opref." + NormalizeId(actorKey) + "." + NormalizeId(stack.StackId) + "." + stable,
                DefinitionId = definitionId ?? String.Empty,
                AssignmentId = assignmentId ?? String.Empty,
                Enabled = true,
                Locked = locked,
                Provenance = provenance ?? String.Empty,
                Notes = String.Empty,
                CreatedUtc = DateTime.UtcNow,
                CreatedBy = SafeAuthor(author),
                ReferenceKind = kind ?? "LibraryDefinition",
                MandatoryRole = mandatoryRole ?? String.Empty,
                DisplayName = displayName ?? definitionId ?? String.Empty,
                CanonicalStack = canonicalStack ?? String.Empty
            };
        }

        private static ReferenceInfo ResolveReference(Mobile actor, string definitionId)
        {
            if (String.IsNullOrWhiteSpace(definitionId))
                return null;

            AIGMUMGLibraryDefinition definition = AIGMUMGRepository.GetLibraryDefinition(definitionId);
            if (definition != null)
                return new ReferenceInfo(true, definition.DefinitionId, definition.Name, definition.IntendedNeoStack.ToString(), "LibraryDefinition", definition);

            AIGMUMGSleeve sleeve = actor != null ? AIGMUMGRepository.GetSleeve(AIGMUMGRuntimeService.ResolveActorId(actor)) : null;
            if (sleeve != null && sleeve.NeoStacks != null)
            {
                for (int s = 0; s < sleeve.NeoStacks.Count; s++)
                {
                    AIGMUMGNeoStack stack = sleeve.NeoStacks[s];
                    if (stack == null || stack.NeoBlocks == null)
                        continue;

                    for (int n = 0; n < stack.NeoBlocks.Count; n++)
                    {
                        AIGMUMGNeoBlock neoBlock = stack.NeoBlocks[n];
                        if (neoBlock == null)
                            continue;
                        if (String.Equals(neoBlock.NeoBlockId, definitionId, StringComparison.OrdinalIgnoreCase))
                            return new ReferenceInfo(true, neoBlock.NeoBlockId, neoBlock.Name, stack.Name, "CanonicalNeoBlock", null);

                        List<AIGMUMGBlock> blocks = FlattenNeoBlockBlocks(neoBlock);
                        for (int b = 0; b < blocks.Count; b++)
                        {
                            AIGMUMGBlock block = blocks[b];
                            if (block != null && String.Equals(block.BlockId, definitionId, StringComparison.OrdinalIgnoreCase))
                                return new ReferenceInfo(true, block.BlockId, block.Name, stack.Name, "CanonicalSleeveBlock", null);
                        }
                    }
                }
            }

            return new ReferenceInfo(false, definitionId, definitionId, String.Empty, "Missing", null);
        }

        private static string MapSleeveBlockToOperationalStack(AIGMUMGNeoStack stack, AIGMUMGBlock block)
        {
            if (stack == null)
                return "diagnostics.unclassified";

            if (stack.StackKind == AIGMUMGNeoStackKind.Identity)
                return "always.identity";
            if (stack.StackKind == AIGMUMGNeoStackKind.Capability)
                return "always.capability_truth";
            if (stack.StackKind == AIGMUMGNeoStackKind.Governance)
            {
                if (block != null && (Contains(block.Name, "Engine") || Contains(block.Name, "Invariant")))
                    return "always.system_invariants";
                if (block != null && (Contains(block.Name, "Operational") || Contains(block.Name, "Hold")))
                    return "always.operational_authority";
                if (block != null && (Contains(block.Name, "Protected") || Contains(block.Name, "Target")))
                    return "always.safety";
                return "always.governance";
            }

            if (stack.StackKind == AIGMUMGNeoStackKind.CombatDoctrine)
                return "combat.canonical";
            if (stack.StackKind == AIGMUMGNeoStackKind.MovementPositioning)
                return "positioning.canonical";
            if (stack.StackKind == AIGMUMGNeoStackKind.SkillsSpellsResources)
                return "resources.canonical";
            if (stack.StackKind == AIGMUMGNeoStackKind.TrackingAwareness)
                return "tracking.canonical";
            if (stack.StackKind == AIGMUMGNeoStackKind.SquadRelationshipOperations)
                return "squad.canonical";
            if (stack.StackKind == AIGMUMGNeoStackKind.SituationalOverlays)
                return "overlays.canonical";

            return "diagnostics.unclassified";
        }

        private static string MandatoryRoleFor(AIGMUMGNeoStack stack, AIGMUMGBlock block)
        {
            if (stack == null || block == null)
                return String.Empty;
            if (stack.StackKind == AIGMUMGNeoStackKind.Identity && Contains(block.Name, "Canonical Identity"))
                return "Identity";
            if (stack.StackKind == AIGMUMGNeoStackKind.Capability && Contains(block.Name, "Capability"))
                return "Capability Truth";
            if (stack.StackKind == AIGMUMGNeoStackKind.Governance)
            {
                if (Contains(block.Name, "Engine") || Contains(block.Name, "Invariant"))
                    return "System Invariants";
                if (Contains(block.Name, "Operational") || Contains(block.Name, "Hold"))
                    return "Operational Authority";
                if (Contains(block.Name, "Protected") || Contains(block.Name, "Target"))
                    return "Safety";
                return "Governance";
            }

            return String.Empty;
        }

        private static string MapDefinitionToOperationalStack(AIGMUMGLibraryDefinition definition)
        {
            if (definition == null)
                return "diagnostics.unclassified";

            if (definition.IntendedNeoStack == AIGMUMGNeoStackKind.CombatDoctrine)
            {
                if (Contains(definition.Category, "Defense") || Contains(definition.Name, "Protect") || Contains(definition.Name, "Defender"))
                    return "protection.canonical";
                return "combat.canonical";
            }
            if (definition.IntendedNeoStack == AIGMUMGNeoStackKind.MovementPositioning)
                return "positioning.canonical";
            if (definition.IntendedNeoStack == AIGMUMGNeoStackKind.SkillsSpellsResources)
                return "resources.canonical";
            if (definition.IntendedNeoStack == AIGMUMGNeoStackKind.TrackingAwareness)
                return "tracking.canonical";
            if (definition.IntendedNeoStack == AIGMUMGNeoStackKind.SquadRelationshipOperations)
                return "squad.canonical";
            if (definition.IntendedNeoStack == AIGMUMGNeoStackKind.SituationalOverlays)
            {
                if (Contains(definition.Category, "Defense") || Contains(definition.Name, "Protect"))
                    return "protection.canonical";
                return "overlays.canonical";
            }

            return "diagnostics.unclassified";
        }

        private static void AddSystemStack(AIGMUMGOperationalLayout layout, string familyId, string stackId, string name, bool locked)
        {
            AIGMUMGOperationalFamily family = FindFamily(layout, familyId);
            if (family == null)
                return;

            family.OperationalNeoStacks.Add(new AIGMUMGOperationalNeoStack
            {
                StackId = stackId,
                DisplayName = name,
                FamilyId = familyId,
                Order = NextOrder(family.OperationalNeoStacks),
                Enabled = true,
                Locked = locked,
                SystemDefined = true
            });
        }

        private static void AddConflictValidation(AIGMUMGOperationalLayout layout, AIGMUMGOperationalLayoutValidationResult result)
        {
            List<AIGMUMGNeoBlockReference> references = FlattenReferences(layout, true);
            for (int i = 0; i < references.Count; i++)
            {
                AIGMUMGLibraryDefinition left = AIGMUMGRepository.GetLibraryDefinition(references[i].DefinitionId);
                if (left == null || left.Conflicts == null)
                    continue;

                for (int j = i + 1; j < references.Count; j++)
                {
                    if (left.Conflicts.Contains(references[j].DefinitionId))
                        result.HardConflicts.Add(left.DefinitionId + "<->" + references[j].DefinitionId);
                }
            }
        }

        private static string CapabilityFor(Mobile actor, AIGMUMGNeoBlockReference reference)
        {
            if (reference == null)
                return "Unproven";
            AIGMUMGLibraryDefinition definition = AIGMUMGRepository.GetLibraryDefinition(reference.DefinitionId);
            if (definition == null)
                return "Ready";
            AIGMUMGCompatibilityResult result = AIGMUMGLibraryService.CheckCompatibility(actor, definition);
            return ToCapabilityLabel(result.State) + " " + result.BuildSummary();
        }

        private static string ConflictFor(AIGMUMGNeoBlockReference reference, AIGMUMGOperationalLayoutValidationResult validation)
        {
            if (reference == null || validation == null)
                return "none";
            List<string> matches = new List<string>();
            for (int i = 0; i < validation.HardConflicts.Count; i++)
            {
                if (validation.HardConflicts[i].IndexOf(reference.DefinitionId, StringComparison.OrdinalIgnoreCase) >= 0)
                    matches.Add(validation.HardConflicts[i]);
            }

            return matches.Count == 0 ? "none" : String.Join("|", matches.ToArray());
        }

        private static string ToCapabilityLabel(AIGMUMGCompatibilityState state)
        {
            if (state == AIGMUMGCompatibilityState.Compatible)
                return "Ready";
            if (state == AIGMUMGCompatibilityState.Degraded)
                return "Degraded";
            if (state == AIGMUMGCompatibilityState.Incompatible)
                return "Blocked";
            if (state == AIGMUMGCompatibilityState.CompatibleWithFallback)
                return "Degraded";
            return "Unproven";
        }

        private static string Origin(AIGMUMGNeoBlockReference reference)
        {
            if (reference == null)
                return "unknown";
            if (Contains(reference.Provenance, "rollback"))
                return "rollback";
            if (Contains(reference.Provenance, "approval"))
                return "approval";
            if (Contains(reference.Provenance, "assignment"))
                return "assignment_import";
            if (Contains(reference.Provenance, "canonical"))
                return "canonical_default";
            if (Contains(reference.Provenance, "user"))
                return "user_creation";
            return reference.Provenance;
        }

        private static AIGMUMGOperationalLayoutPointer GetPointer(string actorKey)
        {
            return FindPointer(LoadPointerFile(), actorKey);
        }

        private static AIGMUMGOperationalLayoutPointer FindPointer(AIGMUMGOperationalLayoutPointerFile file, string actorKey)
        {
            if (file == null || file.Entries == null)
                return null;
            string key = NormalizeId(actorKey);
            for (int i = 0; i < file.Entries.Count; i++)
            {
                AIGMUMGOperationalLayoutPointer pointer = file.Entries[i];
                if (pointer != null && String.Equals(NormalizeId(pointer.ActorKey), key, StringComparison.OrdinalIgnoreCase))
                    return pointer;
            }

            return null;
        }

        private static int GetLatestRevision(string actorKey)
        {
            AIGMUMGOperationalLayoutPointer pointer = GetPointer(actorKey);
            return pointer != null ? pointer.LatestRevision : 0;
        }

        private static AIGMUMGOperationalLayoutVersionRecord FindCurrentVersion(AIGMUMGOperationalLayoutPointer pointer)
        {
            if (pointer == null)
                return null;

            AIGMUMGOperationalLayoutVersionRecord draft = GetLayoutVersion(pointer.CurrentDraftVersionId);
            if (draft != null)
                return draft;
            return GetLayoutVersion(pointer.CurrentApprovedPreviewVersionId);
        }

        private static AIGMUMGOperationalLayoutVersionRecord GetLayoutVersion(string versionId)
        {
            if (String.IsNullOrWhiteSpace(versionId))
                return null;

            AIGMUMGOperationalLayoutVersionFile file = LoadVersionFile();
            for (int i = 0; i < file.Entries.Count; i++)
            {
                AIGMUMGOperationalLayoutVersionRecord version = file.Entries[i];
                if (version != null && String.Equals(version.VersionId, versionId, StringComparison.OrdinalIgnoreCase))
                    return version;
            }

            return null;
        }

        private static AIGMUMGOperationalLayoutPointerFile LoadPointerFile()
        {
            RecoverInterruptedWrite(GetPointerPath());
            AIGMUMGOperationalLayoutPointerFile file;
            if (!TryReadJson(GetPointerPath(), out file) || file == null)
                file = new AIGMUMGOperationalLayoutPointerFile();
            if (file.Entries == null)
                file.Entries = new List<AIGMUMGOperationalLayoutPointer>();
            return file;
        }

        private static AIGMUMGOperationalLayoutVersionFile LoadVersionFile()
        {
            RecoverInterruptedWrite(GetVersionPath());
            AIGMUMGOperationalLayoutVersionFile file;
            if (!TryReadJson(GetVersionPath(), out file) || file == null)
                file = new AIGMUMGOperationalLayoutVersionFile();
            if (file.Entries == null)
                file.Entries = new List<AIGMUMGOperationalLayoutVersionRecord>();
            return file;
        }

        private static void SavePointerFile(AIGMUMGOperationalLayoutPointerFile file)
        {
            WriteJsonAtomic(GetPointerPath(), file);
        }

        private static void SaveVersionFile(AIGMUMGOperationalLayoutVersionFile file)
        {
            WriteJsonAtomic(GetVersionPath(), file);
        }

        private static bool TryReadJson<T>(string path, out T value)
        {
            value = default(T);
            try
            {
                RecoverInterruptedWrite(path);

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
                AIGMUMGLog.Write("operational_layout_load_warning", null, AIGMUMGLog.Fields("path", path, "error", ex.Message));
                return false;
            }
        }

        private static void WriteJsonAtomic<T>(string path, T value)
        {
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            byte[] bytes;
            using (MemoryStream stream = new MemoryStream())
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
                serializer.WriteObject(stream, value);
                bytes = stream.ToArray();
            }

            string temp = path + ".tmp";
            File.WriteAllBytes(temp, bytes);
            if (File.Exists(path))
            {
                string backup = path + "." + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".bak";
                File.Replace(temp, path, backup, true);
            }
            else
            {
                File.Move(temp, path);
            }
        }

        private static void RecoverInterruptedWrite(string path)
        {
            try
            {
                if (String.IsNullOrWhiteSpace(path))
                    return;

                string temp = path + ".tmp";
                if (!File.Exists(temp))
                    return;

                if (!File.Exists(path))
                {
                    File.Move(temp, path);
                    return;
                }
                else
                {
                    string interrupted = temp + "." + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".interrupted";
                    File.Move(temp, interrupted);
                    AIGMUMGLog.Write("operational_layout_recovered_interrupted_write", null, AIGMUMGLog.Fields("path", path, "temp", interrupted));
                }
            }
            catch (Exception ex)
            {
                AIGMUMGLog.Write("operational_layout_recovery_warning", null, AIGMUMGLog.Fields("path", path, "error", ex.Message));
            }
        }

        private static string GetLayoutRoot()
        {
            return Path.Combine(AIGMUMGRepository.DataRoot, "OperationalLayouts");
        }

        private static string GetPointerPath()
        {
            return Path.Combine(GetLayoutRoot(), PointerFileName);
        }

        private static string GetVersionPath()
        {
            return Path.Combine(GetLayoutRoot(), VersionFileName);
        }

        private static string GetLatestAssignmentVersionId(string actorKey)
        {
            List<AIGMUMGVersionRecord> versions = AIGMUMGRepository.GetVersionsForTarget(actorKey);
            return versions.Count > 0 ? versions[0].VersionId : String.Empty;
        }

        private static AIGMUMGOperationalLayout Clone(AIGMUMGOperationalLayout layout)
        {
            return AIGMUMGRepository.RoundTripClone(layout);
        }

        private static void NormalizeLayout(AIGMUMGOperationalLayout layout)
        {
            if (layout == null || layout.Families == null)
                return;

            layout.Families.Sort(CompareFamilies);
            for (int i = 0; i < layout.Families.Count; i++)
            {
                AIGMUMGOperationalFamily family = layout.Families[i];
                if (family == null)
                    continue;

                family.Order = (i + 1) * 100;
                if (family.OperationalNeoStacks == null)
                    family.OperationalNeoStacks = new List<AIGMUMGOperationalNeoStack>();
                family.OperationalNeoStacks.Sort(CompareStacks);
                for (int s = 0; s < family.OperationalNeoStacks.Count; s++)
                {
                    AIGMUMGOperationalNeoStack stack = family.OperationalNeoStacks[s];
                    if (stack == null)
                        continue;
                    stack.FamilyId = family.FamilyId;
                    stack.Order = (s + 1) * 100;
                    if (stack.NeoBlockReferences == null)
                        stack.NeoBlockReferences = new List<AIGMUMGNeoBlockReference>();
                    stack.NeoBlockReferences.Sort(CompareReferences);
                    for (int r = 0; r < stack.NeoBlockReferences.Count; r++)
                        stack.NeoBlockReferences[r].Order = (r + 1) * 100;
                }
            }
        }

        private static int CompareFamilies(AIGMUMGOperationalFamily left, AIGMUMGOperationalFamily right)
        {
            int byOrder = SafeOrder(left).CompareTo(SafeOrder(right));
            if (byOrder != 0)
                return byOrder;
            return String.Compare(left != null ? left.FamilyId : String.Empty, right != null ? right.FamilyId : String.Empty, StringComparison.OrdinalIgnoreCase);
        }

        private static int CompareStacks(AIGMUMGOperationalNeoStack left, AIGMUMGOperationalNeoStack right)
        {
            int byOrder = SafeOrder(left).CompareTo(SafeOrder(right));
            if (byOrder != 0)
                return byOrder;
            return String.Compare(left != null ? left.StackId : String.Empty, right != null ? right.StackId : String.Empty, StringComparison.OrdinalIgnoreCase);
        }

        private static int CompareReferences(AIGMUMGNeoBlockReference left, AIGMUMGNeoBlockReference right)
        {
            int byOrder = SafeOrder(left).CompareTo(SafeOrder(right));
            if (byOrder != 0)
                return byOrder;
            return String.Compare(left != null ? left.DefinitionId : String.Empty, right != null ? right.DefinitionId : String.Empty, StringComparison.OrdinalIgnoreCase);
        }

        private static int CompareAssignmentsForDefault(AIGMUMGAssignment left, AIGMUMGAssignment right)
        {
            int byOrder = (left != null ? left.PriorityOrder : 0).CompareTo(right != null ? right.PriorityOrder : 0);
            if (byOrder != 0)
                return byOrder;
            return String.Compare(left != null ? left.DefinitionId : String.Empty, right != null ? right.DefinitionId : String.Empty, StringComparison.OrdinalIgnoreCase);
        }

        private static int SafeOrder(AIGMUMGOperationalFamily value)
        {
            return value != null ? value.Order : 0;
        }

        private static int SafeOrder(AIGMUMGOperationalNeoStack value)
        {
            return value != null ? value.Order : 0;
        }

        private static int SafeOrder(AIGMUMGNeoBlockReference value)
        {
            return value != null ? value.Order : 0;
        }

        public static AIGMUMGOperationalFamily FindFamily(AIGMUMGOperationalLayout layout, string familyId)
        {
            if (layout == null || layout.Families == null)
                return null;
            for (int i = 0; i < layout.Families.Count; i++)
            {
                AIGMUMGOperationalFamily family = layout.Families[i];
                if (family != null && String.Equals(family.FamilyId, familyId, StringComparison.OrdinalIgnoreCase))
                    return family;
            }

            return null;
        }

        public static AIGMUMGOperationalNeoStack FindStack(AIGMUMGOperationalLayout layout, string stackId, out AIGMUMGOperationalFamily parentFamily)
        {
            parentFamily = null;
            if (layout == null || layout.Families == null || String.IsNullOrWhiteSpace(stackId))
                return null;

            for (int i = 0; i < layout.Families.Count; i++)
            {
                AIGMUMGOperationalFamily family = layout.Families[i];
                if (family == null || family.OperationalNeoStacks == null)
                    continue;
                for (int s = 0; s < family.OperationalNeoStacks.Count; s++)
                {
                    AIGMUMGOperationalNeoStack stack = family.OperationalNeoStacks[s];
                    if (stack != null && (String.Equals(stack.StackId, stackId, StringComparison.OrdinalIgnoreCase) || String.Equals(stack.DisplayName, stackId, StringComparison.OrdinalIgnoreCase)))
                    {
                        parentFamily = family;
                        return stack;
                    }
                }
            }

            return null;
        }

        public static AIGMUMGOperationalNeoStack FindStack(AIGMUMGOperationalLayout layout, string stackId)
        {
            if (layout == null || layout.Families == null || String.IsNullOrWhiteSpace(stackId))
                return null;
            for (int i = 0; i < layout.Families.Count; i++)
            {
                AIGMUMGOperationalFamily family = layout.Families[i];
                if (family == null || family.OperationalNeoStacks == null)
                    continue;
                for (int s = 0; s < family.OperationalNeoStacks.Count; s++)
                {
                    AIGMUMGOperationalNeoStack stack = family.OperationalNeoStacks[s];
                    if (stack != null && (String.Equals(stack.StackId, stackId, StringComparison.OrdinalIgnoreCase) || String.Equals(stack.DisplayName, stackId, StringComparison.OrdinalIgnoreCase)))
                        return stack;
                }
            }

            return null;
        }

        public static AIGMUMGNeoBlockReference FindReference(AIGMUMGOperationalLayout layout, string referenceId)
        {
            AIGMUMGOperationalNeoStack stack;
            AIGMUMGNeoBlockReference reference;
            return FindReference(layout, referenceId, out stack, out reference) ? reference : null;
        }

        public static bool FindReference(AIGMUMGOperationalLayout layout, string referenceId, out AIGMUMGOperationalFamily parentFamily, out AIGMUMGOperationalNeoStack parentStack)
        {
            parentFamily = null;
            parentStack = null;
            if (layout == null || layout.Families == null || String.IsNullOrWhiteSpace(referenceId))
                return false;

            for (int i = 0; i < layout.Families.Count; i++)
            {
                AIGMUMGOperationalFamily family = layout.Families[i];
                if (family == null || family.OperationalNeoStacks == null)
                    continue;
                for (int s = 0; s < family.OperationalNeoStacks.Count; s++)
                {
                    AIGMUMGOperationalNeoStack stack = family.OperationalNeoStacks[s];
                    if (stack == null || stack.NeoBlockReferences == null)
                        continue;
                    for (int r = 0; r < stack.NeoBlockReferences.Count; r++)
                    {
                        AIGMUMGNeoBlockReference candidate = stack.NeoBlockReferences[r];
                        if (candidate != null && String.Equals(candidate.ReferenceId, referenceId, StringComparison.OrdinalIgnoreCase))
                        {
                            parentFamily = family;
                            parentStack = stack;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public static bool FindReference(AIGMUMGOperationalLayout layout, string referenceId, out AIGMUMGOperationalNeoStack parentStack, out AIGMUMGNeoBlockReference reference)
        {
            parentStack = null;
            reference = null;
            if (layout == null || layout.Families == null || String.IsNullOrWhiteSpace(referenceId))
                return false;

            for (int i = 0; i < layout.Families.Count; i++)
            {
                AIGMUMGOperationalFamily family = layout.Families[i];
                if (family == null)
                    continue;
                for (int s = 0; s < family.OperationalNeoStacks.Count; s++)
                {
                    AIGMUMGOperationalNeoStack stack = family.OperationalNeoStacks[s];
                    if (stack == null)
                        continue;
                    for (int r = 0; r < stack.NeoBlockReferences.Count; r++)
                    {
                        AIGMUMGNeoBlockReference candidate = stack.NeoBlockReferences[r];
                        if (candidate != null && String.Equals(candidate.ReferenceId, referenceId, StringComparison.OrdinalIgnoreCase))
                        {
                            parentStack = stack;
                            reference = candidate;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static AIGMUMGOperationalNeoStack ResolveTargetStack(AIGMUMGOperationalLayout layout, AIGMUMGOperationalNeoStack current, string targetStackText)
        {
            if (!String.IsNullOrWhiteSpace(targetStackText))
                return FindStack(layout, targetStackText.Trim());

            AIGMUMGOperationalFamily family = current != null ? FindFamily(layout, current.FamilyId) : null;
            if (family == null)
                return null;

            for (int i = 0; i < family.OperationalNeoStacks.Count; i++)
            {
                AIGMUMGOperationalNeoStack stack = family.OperationalNeoStacks[i];
                if (stack != null && !stack.SystemDefined && !stack.Locked && !String.Equals(stack.StackId, current.StackId, StringComparison.OrdinalIgnoreCase))
                    return stack;
            }

            return null;
        }

        private static bool MoveReferenceInternal(AIGMUMGOperationalLayout layout, string referenceId, string action, string targetStackText, out string message)
        {
            message = String.Empty;
            AIGMUMGOperationalNeoStack stack;
            AIGMUMGNeoBlockReference reference;
            if (!FindReference(layout, referenceId, out stack, out reference))
            {
                message = "Select a reference before moving.";
                return false;
            }

            if (IsMandatory(reference) || (stack != null && stack.Locked))
            {
                message = "Mandatory Always-On Spine references may not leave the Always-On Spine.";
                return false;
            }

            if (stack == null || stack.NeoBlockReferences == null)
            {
                message = "Reference parent stack is invalid.";
                return false;
            }

            List<AIGMUMGNeoBlockReference> refs = stack.NeoBlockReferences;
            SortReferences(refs);
            int index = refs.IndexOf(reference);
            if (index < 0)
            {
                message = "Reference not found in parent stack.";
                return false;
            }

            if (String.Equals(action, "up", StringComparison.OrdinalIgnoreCase) && index > 0)
            {
                refs.RemoveAt(index);
                refs.Insert(index - 1, reference);
            }
            else if (String.Equals(action, "down", StringComparison.OrdinalIgnoreCase) && index < refs.Count - 1)
            {
                refs.RemoveAt(index);
                refs.Insert(index + 1, reference);
            }
            else if (String.Equals(action, "top", StringComparison.OrdinalIgnoreCase))
            {
                refs.RemoveAt(index);
                refs.Insert(0, reference);
            }
            else if (String.Equals(action, "bottom", StringComparison.OrdinalIgnoreCase))
            {
                refs.RemoveAt(index);
                refs.Add(reference);
            }
            else if (String.Equals(action, "stack", StringComparison.OrdinalIgnoreCase))
            {
                AIGMUMGOperationalNeoStack target = ResolveTargetStack(layout, stack, targetStackText);
                if (target == null)
                {
                    message = "No permitted target stack matched.";
                    return false;
                }

                if (target.Locked || String.Equals(target.FamilyId, FamilyAlwaysOn, StringComparison.OrdinalIgnoreCase))
                {
                    message = "Reference cannot be moved to a locked or Always-On stack.";
                    return false;
                }

                if (ContainsDefinitionInStack(target, reference.DefinitionId))
                {
                    message = "Target stack already contains that canonical reference.";
                    return false;
                }

                refs.Remove(reference);
                reference.Order = NextOrder(target.NeoBlockReferences);
                target.NeoBlockReferences.Add(reference);
            }
            else
            {
                message = "Unknown movement operation.";
                return false;
            }

            NormalizeLayout(layout);
            message = "Moved reference: " + reference.DisplayName;
            return true;
        }

        private static void SortReferences(List<AIGMUMGNeoBlockReference> references)
        {
            if (references != null)
                references.Sort(CompareReferences);
        }

        private static void AssignReferenceOrders(List<AIGMUMGNeoBlockReference> references)
        {
            if (references == null)
                return;
            for (int i = 0; i < references.Count; i++)
            {
                if (references[i] != null)
                    references[i].Order = (i + 1) * 100;
            }
        }

        private static bool ContainsDefinitionInStack(AIGMUMGOperationalNeoStack stack, string definitionId)
        {
            if (stack == null || stack.NeoBlockReferences == null)
                return false;
            for (int i = 0; i < stack.NeoBlockReferences.Count; i++)
            {
                if (stack.NeoBlockReferences[i] != null && String.Equals(stack.NeoBlockReferences[i].DefinitionId, definitionId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static bool IsMandatory(AIGMUMGNeoBlockReference reference)
        {
            return reference != null && !String.IsNullOrWhiteSpace(reference.MandatoryRole);
        }

        private static string ValidateStackName(AIGMUMGOperationalFamily family, string name, string existingStackId)
        {
            if (String.IsNullOrWhiteSpace(name))
                return "Stack name cannot be empty.";
            string trimmed = name.Trim();
            if (trimmed.Length > MaxCustomStackNameLength)
                return "Stack name is too long.";
            for (int i = 0; i < trimmed.Length; i++)
            {
                if (Char.IsControl(trimmed[i]))
                    return "Stack name contains control characters.";
            }

            if (IsReservedDisplayName(trimmed))
                return "Stack name is reserved.";
            if (family != null && family.OperationalNeoStacks != null)
            {
                for (int i = 0; i < family.OperationalNeoStacks.Count; i++)
                {
                    AIGMUMGOperationalNeoStack stack = family.OperationalNeoStacks[i];
                    if (stack == null)
                        continue;
                    if (!String.IsNullOrWhiteSpace(existingStackId) && String.Equals(stack.StackId, existingStackId, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (String.Equals(stack.DisplayName, trimmed, StringComparison.OrdinalIgnoreCase))
                        return "Stack name must be unique within the family.";
                }
            }

            return String.Empty;
        }

        private static bool IsReservedSystemStackName(string stackId, string displayName)
        {
            return IsReservedDisplayName(displayName) || (stackId != null && stackId.IndexOf(".canonical", StringComparison.OrdinalIgnoreCase) >= 0) || (stackId != null && stackId.StartsWith("always.", StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsReservedDisplayName(string name)
        {
            if (String.IsNullOrWhiteSpace(name))
                return false;
            for (int i = 0; i < FamilyNames.Length; i++)
            {
                if (String.Equals(FamilyNames[i], name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            string[] reserved =
            {
                "Governance",
                "Identity",
                "Capability Truth",
                "System Invariants",
                "Operational Authority",
                "Owner/Commander Authority",
                "Safety",
                "Logging",
                "Combat Doctrine",
                "Movement and Positioning",
                "Skills, Spells, and Resources",
                "Protection",
                "Tracking and Awareness",
                "Squad and Relationships",
                "Situational Overlays",
                "Unclassified"
            };
            for (int i = 0; i < reserved.Length; i++)
            {
                if (String.Equals(reserved[i], name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static int NextOrder<T>(List<T> list)
        {
            return list == null ? 100 : (list.Count + 1) * 100;
        }

        private static void MarkDirty(AIGMUMGOperationalLayoutEditSession session, string message)
        {
            if (session == null)
                return;
            NormalizeLayout(session.WorkingLayout);
            session.Dirty = true;
            session.LastInteractionUtc = DateTime.UtcNow;
            session.LastMessage = message ?? String.Empty;
        }

        private static List<AIGMUMGBlock> FlattenStackBlocks(AIGMUMGNeoStack stack)
        {
            List<AIGMUMGBlock> blocks = new List<AIGMUMGBlock>();
            if (stack == null || stack.NeoBlocks == null)
                return blocks;
            for (int i = 0; i < stack.NeoBlocks.Count; i++)
                blocks.AddRange(FlattenNeoBlockBlocks(stack.NeoBlocks[i]));
            blocks.Sort(delegate (AIGMUMGBlock left, AIGMUMGBlock right)
            {
                int byOrder = (left != null ? left.PriorityOrder : 0).CompareTo(right != null ? right.PriorityOrder : 0);
                if (byOrder != 0)
                    return byOrder;
                return String.Compare(left != null ? left.BlockId : String.Empty, right != null ? right.BlockId : String.Empty, StringComparison.OrdinalIgnoreCase);
            });
            return blocks;
        }

        private static List<AIGMUMGBlock> FlattenNeoBlockBlocks(AIGMUMGNeoBlock neoBlock)
        {
            List<AIGMUMGBlock> blocks = new List<AIGMUMGBlock>();
            if (neoBlock == null || neoBlock.BlockStacks == null)
                return blocks;
            for (int i = 0; i < neoBlock.BlockStacks.Count; i++)
            {
                AIGMUMGBlockStack stack = neoBlock.BlockStacks[i];
                if (stack == null || stack.MoltBlocks == null)
                    continue;
                for (int b = 0; b < stack.MoltBlocks.Count; b++)
                {
                    if (stack.MoltBlocks[b] != null)
                        blocks.Add(stack.MoltBlocks[b]);
                }
            }

            return blocks;
        }

        private static List<AIGMUMGNeoBlockReference> FlattenReferences(AIGMUMGOperationalLayout layout, bool enabledOnly)
        {
            List<AIGMUMGNeoBlockReference> refs = new List<AIGMUMGNeoBlockReference>();
            if (layout == null || layout.Families == null)
                return refs;
            for (int f = 0; f < layout.Families.Count; f++)
            {
                AIGMUMGOperationalFamily family = layout.Families[f];
                if (family == null)
                    continue;
                for (int s = 0; s < family.OperationalNeoStacks.Count; s++)
                {
                    AIGMUMGOperationalNeoStack stack = family.OperationalNeoStacks[s];
                    if (stack == null)
                        continue;
                    for (int r = 0; r < stack.NeoBlockReferences.Count; r++)
                    {
                        AIGMUMGNeoBlockReference reference = stack.NeoBlockReferences[r];
                        if (reference != null && (!enabledOnly || reference.Enabled))
                            refs.Add(reference);
                    }
                }
            }

            return refs;
        }

        private static void CountLayout(AIGMUMGOperationalLayout layout, out int families, out int stacks, out int references, out int enabled, out int disabled, out int locked)
        {
            families = 0;
            stacks = 0;
            references = 0;
            enabled = 0;
            disabled = 0;
            locked = 0;
            if (layout == null || layout.Families == null)
                return;
            families = layout.Families.Count;
            for (int f = 0; f < layout.Families.Count; f++)
            {
                AIGMUMGOperationalFamily family = layout.Families[f];
                if (family == null || family.OperationalNeoStacks == null)
                    continue;
                stacks += family.OperationalNeoStacks.Count;
                for (int s = 0; s < family.OperationalNeoStacks.Count; s++)
                {
                    AIGMUMGOperationalNeoStack stack = family.OperationalNeoStacks[s];
                    if (stack == null || stack.NeoBlockReferences == null)
                        continue;
                    for (int r = 0; r < stack.NeoBlockReferences.Count; r++)
                    {
                        AIGMUMGNeoBlockReference reference = stack.NeoBlockReferences[r];
                        if (reference == null)
                            continue;
                        references++;
                        if (reference.Enabled)
                            enabled++;
                        else
                            disabled++;
                        if (reference.Locked)
                            locked++;
                    }
                }
            }
        }

        private static void CountVersion(AIGMUMGOperationalLayoutVersionRecord version, out int stacks, out int references)
        {
            int families;
            int enabled;
            int disabled;
            int locked;
            CountLayout(version != null ? version.LayoutSnapshot : null, out families, out stacks, out references, out enabled, out disabled, out locked);
        }

        private static void CompareFamilies(AIGMUMGOperationalLayout left, AIGMUMGOperationalLayout right, List<string> changes)
        {
            HashSet<string> leftIds = FamilySet(left);
            HashSet<string> rightIds = FamilySet(right);
            foreach (string id in leftIds)
            {
                if (!rightIds.Contains(id))
                    changes.Add("family removed:" + id);
            }
            foreach (string id in rightIds)
            {
                if (!leftIds.Contains(id))
                    changes.Add("family added:" + id);
            }
        }

        private static void CompareStacks(AIGMUMGOperationalLayout left, AIGMUMGOperationalLayout right, List<string> changes)
        {
            Dictionary<string, AIGMUMGOperationalNeoStack> leftStacks = StackMap(left);
            Dictionary<string, AIGMUMGOperationalNeoStack> rightStacks = StackMap(right);
            foreach (KeyValuePair<string, AIGMUMGOperationalNeoStack> pair in leftStacks)
            {
                AIGMUMGOperationalNeoStack rightStack;
                if (!rightStacks.TryGetValue(pair.Key, out rightStack))
                {
                    changes.Add("stack removed:" + pair.Key);
                    continue;
                }
                if (!String.Equals(pair.Value.DisplayName, rightStack.DisplayName, StringComparison.Ordinal))
                    changes.Add("stack renamed:" + pair.Key + ":" + pair.Value.DisplayName + "->" + rightStack.DisplayName);
            }
            foreach (KeyValuePair<string, AIGMUMGOperationalNeoStack> pair in rightStacks)
            {
                if (!leftStacks.ContainsKey(pair.Key))
                    changes.Add("stack added:" + pair.Key);
            }
        }

        private static void CompareReferences(AIGMUMGOperationalLayout left, AIGMUMGOperationalLayout right, List<string> changes)
        {
            Dictionary<string, AIGMUMGNeoBlockReference> leftRefs = ReferenceMap(left);
            Dictionary<string, AIGMUMGNeoBlockReference> rightRefs = ReferenceMap(right);
            Dictionary<string, string> leftParents = ReferenceParentMap(left);
            Dictionary<string, string> rightParents = ReferenceParentMap(right);
            foreach (KeyValuePair<string, AIGMUMGNeoBlockReference> pair in leftRefs)
            {
                AIGMUMGNeoBlockReference rightRef;
                if (!rightRefs.TryGetValue(pair.Key, out rightRef))
                {
                    changes.Add("reference removed:" + pair.Key);
                    continue;
                }
                if (!String.Equals(leftParents[pair.Key], rightParents[pair.Key], StringComparison.OrdinalIgnoreCase))
                    changes.Add("reference moved:" + pair.Key);
                if (pair.Value.Order != rightRef.Order)
                    changes.Add("reference order changed:" + pair.Key);
                if (pair.Value.Enabled != rightRef.Enabled)
                    changes.Add("enabled changed:" + pair.Key);
                if (pair.Value.Locked != rightRef.Locked)
                    changes.Add("lock changed:" + pair.Key);
                if (!SameParameters(pair.Value.OptionalLocalParameters, rightRef.OptionalLocalParameters))
                    changes.Add("parameters changed:" + pair.Key);
            }
            foreach (KeyValuePair<string, AIGMUMGNeoBlockReference> pair in rightRefs)
            {
                if (!leftRefs.ContainsKey(pair.Key))
                    changes.Add("reference added:" + pair.Key);
            }
        }

        private static HashSet<string> FamilySet(AIGMUMGOperationalLayout layout)
        {
            HashSet<string> set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (layout == null || layout.Families == null)
                return set;
            for (int i = 0; i < layout.Families.Count; i++)
            {
                if (layout.Families[i] != null)
                    set.Add(layout.Families[i].FamilyId);
            }
            return set;
        }

        private static Dictionary<string, AIGMUMGOperationalNeoStack> StackMap(AIGMUMGOperationalLayout layout)
        {
            Dictionary<string, AIGMUMGOperationalNeoStack> map = new Dictionary<string, AIGMUMGOperationalNeoStack>(StringComparer.OrdinalIgnoreCase);
            if (layout == null || layout.Families == null)
                return map;
            for (int f = 0; f < layout.Families.Count; f++)
            {
                AIGMUMGOperationalFamily family = layout.Families[f];
                if (family == null)
                    continue;
                for (int s = 0; s < family.OperationalNeoStacks.Count; s++)
                {
                    AIGMUMGOperationalNeoStack stack = family.OperationalNeoStacks[s];
                    if (stack != null)
                        map[stack.StackId] = stack;
                }
            }
            return map;
        }

        private static Dictionary<string, AIGMUMGNeoBlockReference> ReferenceMap(AIGMUMGOperationalLayout layout)
        {
            Dictionary<string, AIGMUMGNeoBlockReference> map = new Dictionary<string, AIGMUMGNeoBlockReference>(StringComparer.OrdinalIgnoreCase);
            List<AIGMUMGNeoBlockReference> refs = FlattenReferences(layout, false);
            for (int i = 0; i < refs.Count; i++)
                map[refs[i].ReferenceId] = refs[i];
            return map;
        }

        private static Dictionary<string, string> ReferenceParentMap(AIGMUMGOperationalLayout layout)
        {
            Dictionary<string, string> map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (layout == null || layout.Families == null)
                return map;
            for (int f = 0; f < layout.Families.Count; f++)
            {
                AIGMUMGOperationalFamily family = layout.Families[f];
                if (family == null)
                    continue;
                for (int s = 0; s < family.OperationalNeoStacks.Count; s++)
                {
                    AIGMUMGOperationalNeoStack stack = family.OperationalNeoStacks[s];
                    if (stack == null)
                        continue;
                    for (int r = 0; r < stack.NeoBlockReferences.Count; r++)
                    {
                        AIGMUMGNeoBlockReference reference = stack.NeoBlockReferences[r];
                        if (reference != null)
                            map[reference.ReferenceId] = stack.StackId;
                    }
                }
            }
            return map;
        }

        private static bool SameParameters(List<AIGMUMGParameterValue> left, List<AIGMUMGParameterValue> right)
        {
            int leftCount = left != null ? left.Count : 0;
            int rightCount = right != null ? right.Count : 0;
            if (leftCount != rightCount)
                return false;
            for (int i = 0; i < leftCount; i++)
            {
                if (!String.Equals(left[i].ParameterId, right[i].ParameterId, StringComparison.OrdinalIgnoreCase) || !String.Equals(left[i].Value, right[i].Value, StringComparison.Ordinal))
                    return false;
            }
            return true;
        }

        public static bool RunQueuedPhase64D1DProofIfRequested()
        {
            string requestPath = GetProofRequestPath();
            string claimedPath = ClaimProofRequest(requestPath);
            if (String.IsNullOrWhiteSpace(claimedPath))
                return false;

            string reportPath = String.Empty;
            try
            {
                reportPath = RunPhase64D1DProof("queued_request");
                ConsumeProofRequest(claimedPath, "consumed");
                AIGMUMGLog.Write("phase64d1d_proof_queued_complete", null, AIGMUMGLog.Fields("report", reportPath, "final", PreviewResult));
            }
            catch (Exception ex)
            {
                reportPath = WriteProofReport("error", "PHASE64D1D proof failed: " + ex);
                ConsumeProofRequest(claimedPath, "failed");
                AIGMUMGLog.Write("phase64d1d_proof_queued_failed", null, AIGMUMGLog.Fields("report", reportPath, "error", ex.Message));
            }

            return true;
        }

        public static string RunPhase64D1DProof(string reason)
        {
            const string author = "phase64d1d_proof";
            List<string> lines = new List<string>();
            lines.Add("# Phase64D1D Full Skills-Gump Sleeve Organizer Live Proof");
            lines.Add("");
            lines.Add("- GeneratedUtc: " + DateTime.UtcNow.ToString("o"));
            lines.Add("- Reason: " + (String.IsNullOrWhiteSpace(reason) ? "manual" : reason));
            lines.Add("- TacticalDispatchEnabled: " + AIGMUMGPhase64C2Invariant.TacticalDispatchEnabled);
            lines.Add("- PreviewFinalResult: " + PreviewResult);
            lines.Add("- PointerPath: " + GetPointerPath());
            lines.Add("- VersionPath: " + GetVersionPath());

            int versionCountAtStart = LoadVersionFile().Entries.Count;
            bool pointerFileAtStart = File.Exists(GetPointerPath());
            bool versionFileAtStart = File.Exists(GetVersionPath());
            lines.Add("- SidecarBeforeOpen: pointer=" + pointerFileAtStart + " version=" + versionFileAtStart + " layoutVersions=" + versionCountAtStart);

            Mobile dardalion = FindProofMobile(0x00000193);
            RequireProof(dardalion != null, "Dardalion primary actor 0x00000193 missing");
            string dardalionKey = ResolveOperationalActorKey(dardalion);
            lines.Add("- PrimaryActor: " + DescribeProofMobile(dardalion) + " actorKey=" + dardalionKey + " composerKey=" + ResolveComposerActorKey(dardalion));
            lines.Add("- BackpackBefore: " + BuildBackpackProofSummary());

            AIGMUMGOperationalLayoutEditSession session = CreateProofSession(dardalion, author);
            lines.Add("- OperatorModeOpen: session=" + session.SessionId + " architect=" + session.ArchitectMode + " layoutState=" + session.WorkingLayout.State);
            int versionCountAfterOpen = LoadVersionFile().Entries.Count;
            lines.Add("- OpenWriteProof: versionsBefore=" + versionCountAtStart + " versionsAfterOpen=" + versionCountAfterOpen + " pointerExists=" + File.Exists(GetPointerPath()));
            RequireProof(versionCountAfterOpen == versionCountAtStart, "opening organizer wrote a layout version");

            session.ArchitectMode = true;
            lines.Add("- ArchitectMode: enabled=true");

            string defensiveCombat = AddProofStack(session, FamilyCombat, "Defensive Combat", author, lines);
            string antiMagic = AddProofStack(session, FamilyCombat, "Anti-Magic Response", author, lines);
            string protectedRear = AddProofStack(session, FamilyPositioning, "Protected Rear Support", author, lines);
            string emergencyReposition = AddProofStack(session, FamilyPositioning, "Emergency Reposition", author, lines);
            string manaConservation = AddProofStack(session, FamilyResources, "Mana Conservation", author, lines);
            string emergencyHealing = AddProofStack(session, FamilyResources, "Emergency Healing Reserve", author, lines);
            string protectDanyal = AddProofStack(session, FamilyProtection, "Protect Danyal", author, lines);
            string protectCommander = AddProofStack(session, FamilyProtection, "Protect Commander", author, lines);

            string frontline = AddProofReference(dardalion, session, defensiveCombat, "LIB.NEOBLOCK.FRONTLINE_DEFENDER.v1", author, lines);
            string lastStand = AddProofReference(dardalion, session, defensiveCombat, "LIB.NEOBLOCK.LAST_STAND.v1", author, lines);
            string antiMage = AddProofReference(dardalion, session, antiMagic, "LIB.NEOBLOCK.ANTI_MAGE.v1", author, lines);
            AddProofReference(dardalion, session, protectedRear, "LIB.NEOBLOCK.REAR_LINE_ARCHER.v1", author, lines);
            AddProofReference(dardalion, session, emergencyReposition, "LIB.NEOBLOCK.RETREAT_AND_REGROUP.v1", author, lines);
            AddProofReference(dardalion, session, manaConservation, "LIB.NEOBLOCK.RESOURCE_CONSERVATION.v1", author, lines);
            AddProofReference(dardalion, session, emergencyHealing, "LIB.NEOBLOCK.HEALER_SUPPORT.v1", author, lines);
            AddProofReference(dardalion, session, protectDanyal, "LIB.NEOBLOCK.PROTECT_CIVILIAN.v1", author, lines);
            AddProofReference(dardalion, session, protectCommander, "LIB.NEOBLOCK.PROTECT_OWNER.v1", author, lines);

            session.SelectedReferenceId = lastStand;
            RequireProof(MoveReference(session, "up", String.Empty).Accepted, "move up failed");
            RequireProof(MoveReference(session, "down", String.Empty).Accepted, "move down failed");
            RequireProof(MoveReference(session, "top", String.Empty).Accepted, "move top failed");
            RequireProof(MoveReference(session, "bottom", String.Empty).Accepted, "move bottom failed");
            RequireProof(MoveReference(session, "stack", antiMagic).Accepted, "move to another custom stack failed");
            lines.Add("- MoveProof: up/down/top/bottom/stack accepted for " + lastStand);

            session.SelectedReferenceId = antiMage;
            RequireProof(ToggleReferenceEnabled(session).Accepted, "disable custom reference failed");
            RequireProof(ToggleReferenceEnabled(session).Accepted, "re-enable custom reference failed");
            RequireProof(ToggleReferenceLock(session).Accepted, "lock custom reference failed");
            lines.Add("- EnableLockProof: disabledThenEnabled=true locked=true reference=" + antiMage);

            string mandatoryReference = FindFirstMandatoryReferenceId(session.WorkingLayout);
            RequireProof(!String.IsNullOrWhiteSpace(mandatoryReference), "mandatory Always-On reference missing");
            session.SelectedReferenceId = mandatoryReference;
            AIGMUMGOperationalLayoutActionResult illegalMove = MoveReference(session, "stack", defensiveCombat);
            AIGMUMGOperationalLayoutActionResult illegalRemove = RemoveReference(session, false);
            RequireProof(!illegalMove.Accepted, "mandatory Always-On move was accepted");
            RequireProof(!illegalRemove.Accepted, "mandatory Always-On removal was accepted");
            lines.Add("- AlwaysOnProtectionProof: move=" + illegalMove.Code + " remove=" + illegalRemove.Code);

            int versionCountBeforePreview = LoadVersionFile().Entries.Count;
            string preview = Preview(dardalion, session);
            int versionCountAfterPreview = LoadVersionFile().Entries.Count;
            RequireProof(versionCountBeforePreview == versionCountAfterPreview, "preview wrote a layout version");
            lines.Add("- PreviewWithoutWrite: before=" + versionCountBeforePreview + " after=" + versionCountAfterPreview);
            lines.Add("- PreviewSummary: " + preview);

            AIGMUMGOperationalLayoutValidationResult validation = ValidateLayout(dardalion, session.WorkingLayout);
            RequireProof(validation.CanSaveDraft, "draft validation failed: " + String.Join("|", validation.StructuralErrors.ToArray()));
            AIGMUMGOperationalLayoutActionResult draft = SaveVersion(dardalion, session, AIGMUMGOperationalLayoutState.Draft, author, "Dardalion proof Draft layout", String.Empty);
            RequireProof(draft.Accepted, "draft save failed: " + draft.Message);
            string draftVersionId = session.BaseVersionId;
            AIGMUMGOperationalLayoutVersionRecord draftVersion = GetLayoutVersion(draftVersionId);
            lines.Add("- DraftSaveProof: " + draft.Message);

            validation = ValidateLayout(dardalion, session.WorkingLayout);
            RequireProof(validation.CanApprovePreviewOnly, "approval validation failed: " + validation.BuildCompactSummary());
            AIGMUMGOperationalLayoutActionResult approved = SaveVersion(dardalion, session, AIGMUMGOperationalLayoutState.ApprovedPreviewOnly, author, "Dardalion proof Approved PreviewOnly - NOT LIVE EXECUTION", String.Empty);
            RequireProof(approved.Accepted, "approve preview failed: " + approved.Message);
            string approvedVersionId = session.BaseVersionId;
            lines.Add("- ApprovedPreviewProof: " + approved.Message);
            lines.Add("- DraftApprovedCompare: " + CompareVersions(draftVersionId, approvedVersionId));

            string diagnosticStack = AddProofStack(session, FamilyDiagnostics, "Proof Diagnostic Stack", author, lines);
            AddProofReference(dardalion, session, diagnosticStack, "LIB.NEOBLOCK.SEARCH_AND_RESCUE.v1", author, lines);
            AIGMUMGOperationalLayoutActionResult secondDraft = SaveVersion(dardalion, session, AIGMUMGOperationalLayoutState.Draft, author, "Dardalion proof second Draft after approved preview", String.Empty);
            RequireProof(secondDraft.Accepted, "second draft save failed: " + secondDraft.Message);
            lines.Add("- SecondDraftProof: " + secondDraft.Message);

            RequireProof(draftVersion != null && draftVersion.LayoutSnapshot != null, "source draft version missing before rollback");
            session.WorkingLayout = Clone(draftVersion.LayoutSnapshot);
            session.WorkingLayout.State = AIGMUMGOperationalLayoutState.Draft;
            session.WorkingLayout.ExecutionMode = AIGMUMGExecutionMode.PreviewOnly;
            session.Dirty = true;
            AIGMUMGOperationalLayoutActionResult rollback = SaveVersion(dardalion, session, AIGMUMGOperationalLayoutState.Draft, author, "Rollback clone of " + draftVersionId, draftVersionId);
            RequireProof(rollback.Accepted, "rollback clone failed: " + rollback.Message);
            lines.Add("- RollbackProof: " + rollback.Message + " sourceStillPresent=" + (GetLayoutVersion(draftVersionId) != null));

            int beforeCancelVersions = LoadVersionFile().Entries.Count;
            AIGMUMGOperationalLayoutEditSession cancelSession = CreateProofSession(dardalion, author);
            cancelSession.ArchitectMode = true;
            AddProofStack(cancelSession, FamilyDiagnostics, "Cancel Proof Unsaved Stack", author, lines);
            CancelSession(cancelSession.SessionId);
            int afterCancelVersions = LoadVersionFile().Entries.Count;
            RequireProof(beforeCancelVersions == afterCancelVersions, "cancel created a version");
            lines.Add("- CancelProof: before=" + beforeCancelVersions + " after=" + afterCancelVersions + " noWrite=true");

            AIGMUMGOperationalLayoutEditSession staleA = CreateProofSession(dardalion, author);
            AIGMUMGOperationalLayoutEditSession staleB = CreateProofSession(dardalion, author);
            staleA.ArchitectMode = true;
            staleB.ArchitectMode = true;
            AddProofStack(staleA, FamilyDiagnostics, "Concurrency Winner Stack", author, lines);
            AIGMUMGOperationalLayoutActionResult staleWinner = SaveVersion(dardalion, staleA, AIGMUMGOperationalLayoutState.Draft, author, "Concurrency winner Draft", String.Empty);
            RequireProof(staleWinner.Accepted, "concurrency winner save failed");
            AddProofStack(staleB, FamilyDiagnostics, "Concurrency Stale Stack", author, lines);
            AIGMUMGOperationalLayoutActionResult staleReject = SaveVersion(dardalion, staleB, AIGMUMGOperationalLayoutState.Draft, author, "Stale Draft should reject", String.Empty);
            RequireProof(!staleReject.Accepted && String.Equals(staleReject.Code, "stale_revision", StringComparison.OrdinalIgnoreCase), "stale save was not rejected");
            lines.Add("- StaleSessionProof: winner=" + staleWinner.Message + " stale=" + staleReject.Message);

            AIGMUMGOperationalLayoutEditSession expired = CreateProofSession(dardalion, author);
            expired.LastInteractionUtc = DateTime.UtcNow - TimeSpan.FromMinutes(SessionMinutes + 5);
            AIGMUMGOperationalLayoutActionResult expiredResult = TouchSession(null, dardalion, expired.SessionId, false, false);
            RequireProof(!expiredResult.Accepted && String.Equals(expiredResult.Code, "session_expired", StringComparison.OrdinalIgnoreCase), "expired session was not rejected");
            lines.Add("- SessionExpirationProof: " + expiredResult.Code + " " + expiredResult.Message);

            AddActorRegressionLine(lines, "Druss", 0x00000304);
            AddActorRegressionLine(lines, "Miriel", 0x00002AA5);
            AddActorRegressionLine(lines, "DuplicateNameDardalion", 0x00003B7A);
            AddActorRegressionLine(lines, "MinimalJoining", 0x00005A14);
            Mobile duplicateDardalion = FindProofMobile(0x00003B7A);
            if (duplicateDardalion != null)
            {
                string duplicateKey = ResolveOperationalActorKey(duplicateDardalion);
                RequireProof(!String.Equals(dardalionKey, duplicateKey, StringComparison.OrdinalIgnoreCase), "duplicate-name actor shared the primary Dardalion operational layout key");
                RequireProof(GetLayoutVersionsForActor(duplicateKey).Count == 0, "duplicate-name actor inherited saved layout versions from primary Dardalion");
                lines.Add("- DuplicateNameIsolationProof: primaryKey=" + dardalionKey + " duplicateKey=" + duplicateKey + " duplicateSavedVersions=0 isolated=true");
            }

            lines.Add("- BackpackAfter: " + BuildBackpackProofSummary());
            lines.Add("- FinalLayoutVersions: " + LoadVersionFile().Entries.Count);
            lines.Add("- FinalDardalionVersions: " + GetLayoutVersionsForActor(dardalionKey).Count);
            lines.Add("- FinalPreviewResult: " + PreviewResult);
            lines.Add("- DispatchProof: tactical=false adapterInvoked=false selectiveSleeveDescent=false autonomousItemUse=false");
            lines.Add("- Result: PHASE64D1D live organizer proof passed");

            return WriteProofReport("live_proof", String.Join(Environment.NewLine, lines.ToArray()));
        }

        private static AIGMUMGOperationalLayoutEditSession CreateProofSession(Mobile actor, string author)
        {
            string actorKey = ResolveOperationalActorKey(actor);
            string composerKey = ResolveComposerActorKey(actor);
            AIGMUMGOperationalLayoutPointer pointer = GetPointer(actorKey);
            AIGMUMGOperationalLayoutVersionRecord version = FindCurrentVersion(pointer);
            AIGMUMGOperationalLayout layout = version != null && version.LayoutSnapshot != null
                ? Clone(version.LayoutSnapshot)
                : BuildDefaultLayout(actor, actorKey, composerKey, pointer);

            AIGMUMGOperationalLayoutEditSession session = new AIGMUMGOperationalLayoutEditSession
            {
                SessionId = "oplayout.proof." + Guid.NewGuid().ToString("N").Substring(0, 12),
                CallerSerial = Serial.Zero,
                ActorKey = actorKey,
                ActorSerial = actor.Serial,
                BaseRevision = pointer != null ? pointer.LatestRevision : 0,
                BaseVersionId = version != null ? version.VersionId : String.Empty,
                WorkingLayout = layout,
                Dirty = false,
                CreatedUtc = DateTime.UtcNow,
                LastInteractionUtc = DateTime.UtcNow,
                ArchitectMode = false,
                View = "Main"
            };
            session.ExpandedFamilyIds.Add(FamilyAlwaysOn);
            lock (SyncRoot)
                Sessions[session.SessionId] = session;
            return session;
        }

        private static string AddProofStack(AIGMUMGOperationalLayoutEditSession session, string familyId, string displayName, string author, List<string> lines)
        {
            AIGMUMGOperationalLayoutActionResult result = CreateStack(session, familyId, displayName, author);
            RequireProof(result.Accepted, "create stack failed: " + displayName + " " + result.Message);
            lines.Add("- CreateStack: family=" + familyId + " name=\"" + displayName + "\" stackId=" + session.SelectedStackId);
            return session.SelectedStackId;
        }

        private static string AddProofReference(Mobile actor, AIGMUMGOperationalLayoutEditSession session, string stackId, string definitionId, string author, List<string> lines)
        {
            session.SelectedStackId = stackId;
            AIGMUMGOperationalLayoutActionResult result = AddReference(actor, session, definitionId, author);
            RequireProof(result.Accepted, "add reference failed: " + definitionId + " " + result.Message);
            lines.Add("- AddReference: stackId=" + stackId + " definitionId=" + definitionId + " referenceId=" + session.SelectedReferenceId);
            return session.SelectedReferenceId;
        }

        private static string FindFirstMandatoryReferenceId(AIGMUMGOperationalLayout layout)
        {
            List<AIGMUMGNeoBlockReference> references = FlattenReferences(layout, false);
            for (int i = 0; i < references.Count; i++)
            {
                if (IsMandatory(references[i]))
                    return references[i].ReferenceId;
            }
            return String.Empty;
        }

        private static void AddActorRegressionLine(List<string> lines, string label, int serial)
        {
            Mobile actor = FindProofMobile(serial);
            if (actor == null)
            {
                lines.Add("- " + label + "Regression: missing serial=0x" + serial.ToString("X8"));
                return;
            }

            AIGMUMGOperationalLayout layout = OpenWorkingLayout(actor, "phase64d1d_regression");
            AIGMUMGOperationalLayoutValidationResult validation = ValidateLayout(actor, layout);
            int families;
            int stacks;
            int references;
            int enabled;
            int disabled;
            int locked;
            CountLayout(layout, out families, out stacks, out references, out enabled, out disabled, out locked);
            string operationalKey = ResolveOperationalActorKey(actor);
            lines.Add("- " + label + "Regression: actor=" + DescribeProofMobile(actor) + " operationalKey=" + operationalKey + " composerKey=" + ResolveComposerActorKey(actor) + " savedVersions=" + GetLayoutVersionsForActor(operationalKey).Count + " families=" + families + " stacks=" + stacks + " refs=" + references + " validation=" + validation.BuildCompactSummary() + " state=" + layout.State);
        }

        private static string BuildBackpackProofSummary()
        {
            List<Mobile> actors = AIGMCompanionInventoryService.EnumerateRegisteredLiveCompanions();
            int normalized = 0;
            int duplicates = 0;
            for (int i = 0; i < actors.Count; i++)
            {
                AIGMCompanionBackpackAudit audit = AIGMCompanionInventoryService.AuditBackpack(actors[i]);
                if (audit != null && audit.IsNormalized)
                    normalized++;
                if (audit != null && String.Equals(audit.ValidationStatus, "duplicate_backpacks", StringComparison.OrdinalIgnoreCase))
                    duplicates++;
            }

            return "registered=" + actors.Count + " normalized=" + normalized + " duplicateBackpackActors=" + duplicates;
        }

        private static Mobile FindProofMobile(int serial)
        {
            return World.FindMobile((Serial)serial);
        }

        private static string DescribeProofMobile(Mobile mobile)
        {
            if (mobile == null)
                return "missing";
            return (String.IsNullOrWhiteSpace(mobile.Name) ? mobile.GetType().Name : mobile.Name) + "[" + FormatSerial(mobile) + "]";
        }

        private static void RequireProof(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private static string WriteProofReport(string stage, string content)
        {
            string dir = GetProofDirectory();
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "PHASE64D1D_" + stage + "_" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".md");
            File.WriteAllText(path, content ?? String.Empty, new UTF8Encoding(false));
            return path;
        }

        private static string GetProofDirectory()
        {
            string auditPointer = Path.Combine(Core.BaseDirectory, "PHASE64D1D_AUDIT_PATH.txt");
            if (File.Exists(auditPointer))
            {
                string auditDir = File.ReadAllText(auditPointer).Trim();
                if (!String.IsNullOrWhiteSpace(auditDir))
                    return Path.Combine(auditDir, "live_proof");
            }

            return Path.Combine(Core.BaseDirectory, "docs", "runtime", "phase64d1d_operational_layout");
        }

        private static string GetProofRequestPath()
        {
            return Path.Combine(GetProofDirectory(), "run_phase64d1d_live_proof.request");
        }

        private static string ClaimProofRequest(string requestPath)
        {
            try
            {
                if (!File.Exists(requestPath))
                    return String.Empty;

                string claimedPath = requestPath + ".running." + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
                File.Move(requestPath, claimedPath);
                return claimedPath;
            }
            catch
            {
                return String.Empty;
            }
        }

        private static void ConsumeProofRequest(string requestPath, string suffix)
        {
            try
            {
                if (!File.Exists(requestPath))
                    return;
                string destination = requestPath + "." + suffix + "." + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
                File.Move(requestPath, destination);
            }
            catch
            {
            }
        }

        private static AIGMUMGOperationalLayoutActionResult Accept(string code, string message, AIGMUMGOperationalLayoutEditSession session)
        {
            return new AIGMUMGOperationalLayoutActionResult { Accepted = true, Code = code, Message = message, Session = session };
        }

        private static AIGMUMGOperationalLayoutActionResult Reject(string code, string message)
        {
            return new AIGMUMGOperationalLayoutActionResult { Accepted = false, Code = code, Message = message };
        }

        private static AIGMUMGOperationalLayoutActionResult Reject(string code, string message, AIGMUMGOperationalLayoutValidationResult validation)
        {
            return new AIGMUMGOperationalLayoutActionResult { Accepted = false, Code = code, Message = message, Validation = validation };
        }

        private static void ExpireSessions()
        {
            lock (SyncRoot)
            {
                List<string> expired = new List<string>();
                foreach (KeyValuePair<string, AIGMUMGOperationalLayoutEditSession> pair in Sessions)
                {
                    if (pair.Value == null || DateTime.UtcNow - pair.Value.LastInteractionUtc > TimeSpan.FromMinutes(SessionMinutes))
                        expired.Add(pair.Key);
                }
                for (int i = 0; i < expired.Count; i++)
                    Sessions.Remove(expired[i]);
            }
        }

        private static string FormatSerial(Mobile mobile)
        {
            return mobile != null ? String.Format("0x{0:X8}", mobile.Serial.Value) : String.Empty;
        }

        private static string SafeAuthor(string author)
        {
            string value = String.IsNullOrWhiteSpace(author) ? "unknown" : author.Trim();
            return value.Replace('\r', ' ').Replace('\n', ' ');
        }

        private static string NormalizeId(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return String.Empty;
            StringBuilder sb = new StringBuilder();
            string text = value.Trim().ToLowerInvariant();
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
                    sb.Append(c);
                else if (c == '_' || c == '-' || c == '.' || c == '/')
                    sb.Append('_');
            }
            return sb.ToString().Trim('_');
        }

        private static bool Contains(string value, string needle)
        {
            return value != null && needle != null && value.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private sealed class ReferenceInfo
        {
            public bool Resolved { get; private set; }
            public string DefinitionId { get; private set; }
            public string DisplayName { get; private set; }
            public string CanonicalStack { get; private set; }
            public string ReferenceKind { get; private set; }
            public AIGMUMGLibraryDefinition LibraryDefinition { get; private set; }

            public ReferenceInfo(bool resolved, string definitionId, string displayName, string canonicalStack, string referenceKind, AIGMUMGLibraryDefinition libraryDefinition)
            {
                Resolved = resolved;
                DefinitionId = definitionId ?? String.Empty;
                DisplayName = displayName ?? String.Empty;
                CanonicalStack = canonicalStack ?? String.Empty;
                ReferenceKind = referenceKind ?? String.Empty;
                LibraryDefinition = libraryDefinition;
            }
        }
    }
}
