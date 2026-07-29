# Phase64D1E Selective Sleeve Descent Preview

Phase64D1E adds a server-authoritative PreviewOnly runtime that evaluates an approved Phase64D1D Operational Layout and produces a structured descent decision receipt without invoking gameplay adapters.

## Runtime Boundary

- Input is limited to the actor's latest valid Approved + PreviewOnly Operational Layout version.
- Draft layouts, unsaved edit sessions, composer drafts, and other actors' layouts are rejected.
- Runtime activation state is ephemeral and is not persisted into Composer or Operational Layout sidecars.
- The final result for normal D1E decisions is `PREVIEW_ONLY_NOT_DISPATCHED`.
- `TacticalDispatchEnabled` remains false.
- Adapter mappings may be recorded, but invocation attempts and invocations remain zero.

## Runtime Components

- Activation graph model and builder derive an acyclic graph from the approved layout, operational stacks, NeoBlock references, and canonical definitions.
- Immutable situation snapshots describe CurrentWorld, SyntheticProof, RecordedReplay, and UnitTest situations without passing live `Mobile` or `Item` references into selection.
- Typed trigger profiles evaluate deterministic predicates such as hostile presence, mage pressure, protectee injury, low mana, hold/stop/stand-down, target loss, and cooldown expiry.
- Family selection evaluates Combat, Positioning, Resources, and Protection independently while selecting at most one leading stack per family.
- Sibling branches that lose within a family are marked `SuspendedBySelector`; saved layouts are not changed.
- Capability and governance gates reuse existing AIGM services and record structured reasons in the receipt.
- Hysteresis state is held in a bounded in-memory store keyed by actor, approved layout version, family, and stack.
- Trace retention uses bounded recent in-memory history plus private append-only diagnostic JSONL, with size rotation.

## Command Surface

- `[umgdescent preview <serial|name>]`
- `[umgdescent scenario <serial|name> <scenario>]`
- `[umgdescent proof <serial|name> hysteresis]`
- `[umgdescent trace <serial|name>]`
- `[umgdescent receipt <receiptId>]`
- `[umgdescent state <serial|name>]`
- `[umgdescent reset <serial|name>]`
- `[umgdescent watch <serial|name> on|off]`
- `[umgdescent audit all]`

Administrative reset, watch, audit, and controlled-clock proof operations remain Game Master only. Safe preview and trace access follows the existing Sleeve access policy.

## Proof Matrix

Sanitized local proof covered:

- quiet
- enemy-mage
- protectee-injured
- low-mana
- enemy-mage-low-mana
- protectee-injured-low-mana
- multi-pressure
- commander-hold
- commander-stop
- stand-down
- capability-missing
- target-lost
- threat-cleared

The proof also covered mana and protectee threshold hysteresis, minimum active duration, release stability, cooldown entry, cooldown blocking, re-entry after cooldown, deterministic fingerprint replay, duplicate-name Dardalion serial isolation, Druss no-approved-layout behavior, Miriel no-approved-layout behavior, and one minimal companion no-approved-layout behavior.

## Safety Result

All accepted proof receipts ended with `PREVIEW_ONLY_NOT_DISPATCHED`. Adapter mappings were observed, but adapter invocation attempts and adapter invocations were zero. No movement command, combat target assignment, warmode change, healing cast, item consumption, movement lease, sidecar write, account credential change, or ClassicUO change is part of D1E.

