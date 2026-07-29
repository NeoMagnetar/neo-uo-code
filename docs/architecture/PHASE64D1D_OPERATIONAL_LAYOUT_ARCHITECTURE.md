# Phase64D1D Operational Layout Architecture

Phase64D1D adds a dedicated Operational Layout subsystem beside the existing Phase64C2 Composer model. The Composer remains Schema V2. The new organizer uses Operational Layout Schema Version 1 and stores only layout references, not canonical definitions.

Persistence decision:
- The implementation uses dedicated sidecars instead of extending existing Phase64C2 version records.
- This avoids migration risk for accepted Composer versions and rollback history.
- Existing `versions_v2.json`, `assignments_v2.json`, and `library_definitions_v2.json` remain the authoritative Composer stores.

Sidecars:
- `Data/AIGM/UMG/OperationalLayouts/operational_layouts_v1.json`
- `Data/AIGM/UMG/OperationalLayouts/operational_layout_versions_v1.json`

Identity model:
- Composer import uses the existing canonical actor key, such as `dardalion`.
- Operational layout persistence uses a serial-scoped key, such as `dardalion.serial.00000193`.
- This prevents duplicate-name or duplicate-role actors from reading or overwriting another actor instance's saved layout.

Core model:
- `AIGMUMGOperationalLayout`
- `AIGMUMGOperationalFamily`
- `AIGMUMGOperationalNeoStack`
- `AIGMUMGNeoBlockReference`
- `AIGMUMGOperationalLayoutEditSession`
- `AIGMUMGOperationalLayoutVersionRecord`
- `AIGMUMGOperationalLayoutPointer`

Required families:
- Always-On Spine
- Combat
- Positioning
- Resources
- Protection
- Tracking and Awareness
- Squad and Relationships
- Situational Overlays
- Reference and Diagnostics

Always-On Spine:
- System-defined, locked, enabled.
- Holds mandatory governance, identity, capability truth, invariants, authority, safety, and logging references.
- Mandatory references cannot be moved, removed, disabled, or unlocked.

Edit-session contract:
- Opening creates only an in-memory working layout.
- Every response revalidates caller, actor, authorization, range, map, session ownership, and revision freshness.
- Stale saves are rejected and must reload.
- Expiration removes the session and writes nothing.

Versioning:
- Save Draft creates a new immutable Draft version and updates only the Draft pointer.
- Approve Preview creates a new immutable Approved PreviewOnly version and preserves Draft history.
- Rollback clones a historical snapshot into a new Draft version.
- Historical layout versions are never mutated or reused.

Preview boundary:
- Preview compiles organization eligibility only.
- It reports structure, counts, capability, conflicts, warnings, and final result.
- Final result is always `PREVIEW_ONLY_NOT_DISPATCHED`.
