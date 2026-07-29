# Phase64D1D Operational Layout Schema and Controls

Schema version: Operational Layout Schema Version 1.

`AIGMUMGOperationalLayout` fields:
- `SchemaVersion`
- `LayoutId`
- `ActorKey`
- `ActorSerialAtSave`
- `BaseAssignmentVersionId`
- `State`
- `ExecutionMode`
- `Revision`
- `Families`
- `CreatedUtc`
- `CreatedBy`
- `UpdatedUtc`
- `UpdatedBy`
- `Provenance`

`AIGMUMGOperationalFamily` fields:
- `FamilyId`
- `DisplayName`
- `Order`
- `Enabled`
- `Locked`
- `SystemDefined`
- `OperationalNeoStacks`

`AIGMUMGOperationalNeoStack` fields:
- `StackId`
- `DisplayName`
- `FamilyId`
- `Order`
- `Enabled`
- `Locked`
- `SystemDefined`
- `NeoBlockReferences`

`AIGMUMGNeoBlockReference` fields:
- `ReferenceId`
- `DefinitionId`
- `AssignmentId`
- `Order`
- `Enabled`
- `Locked`
- `Provenance`
- `OptionalLocalParameters`
- `Notes`
- `CreatedUtc`
- `CreatedBy`
- `ReferenceKind`
- `MandatoryRole`
- `DisplayName`
- `CanonicalStack`

Validation summary:
- Actor must exist and remain a registered AIGM companion.
- Layout actor key must match the serial-scoped operational actor key.
- Family IDs, stack IDs, and reference IDs must be unique.
- Definitions must resolve and must not be deleted.
- Always-On Spine must remain present, locked, enabled, and system-defined.
- Mandatory Always-On roles must remain reachable.
- System stacks cannot be renamed, deleted, unlocked, or moved out of protection.
- Custom stack names must be nonempty, bounded, unique within family, and free of control characters.
- Duplicate references to the same definition inside the same Operational NeoStack are rejected unless a future canonical model explicitly supports parameter-distinct duplicates.
- Structural validity is separate from capability and conflict warnings.

Button ID map:
- `0`: Close
- `100-199`: Family expand/collapse
- `200-399`: Operational NeoStack expand/collapse
- `400-899`: NeoBlock reference selection
- `900-999`: Mode and primary navigation
- `1000-1099`: Movement controls
- `1100-1199`: Enable, disable, lock, unlock
- `1200-1299`: Create, rename, add, remove, filter
- `1300-1399`: Preview, Why, capability, conflicts, provenance
- `1400-1499`: Versions, rollback, reload, compare reset
- `1500-1599`: Save Draft, Approve Preview, Cancel, confirmation
- `1600-1699`: Pagination
- `1700-1799`: Composer and Backpack
- `1800-1999`: Secondary action rows and version selection

Persistence and migration:
- First open creates no saved layout data.
- First Save Draft creates the initial pointer and first immutable layout version.
- All JSON writes use UTF-8 without BOM.
- Reads tolerate UTF-8 BOM.
- Writes use temp-file replacement and create backups for existing files.
- Interrupted `.tmp` recovery is handled before reads.
- Existing Phase64C2 data remains unchanged and readable.

Rollback:
- A rollback source version is selected by immutable version ID.
- Its snapshot is cloned into a new Draft version.
- The historical source version remains available and unmodified.
