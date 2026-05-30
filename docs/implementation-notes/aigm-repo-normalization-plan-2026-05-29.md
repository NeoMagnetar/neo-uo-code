# AIGM Repo Normalization Plan
_Date: 2026-05-29_

## Current posture

`neo-uo-code` now contains a curated dated snapshot of the live AIGM shard code under:
- `custom/scripts/aigm-snapshot-2026-05-29/`

This is intentionally conservative. It preserves the real implementation state without prematurely claiming a final repository layout for the full shard codebase.

## Why this matters

The local NeoUO dev shard is a mixed working tree containing:
- intentional AIGM implementation work
- unrelated shard-specific customizations
- config drift
- runtime clutter outside the repo target model

A direct full-tree import would blur those boundaries.

## Recommended next normalization ladder

### Phase 1 — preserve and document
Keep the dated snapshot intact as a historical import point.

### Phase 2 — promote stable surfaces
Promote stable AIGM surfaces from the snapshot into long-term implementation lanes such as:
- `custom/scripts/aigm/Commands/`
- `custom/scripts/aigm/Core/`
- `custom/scripts/aigm/Gumps/`
- `custom/scripts/aigm/Mobiles/`
- `custom/scripts/aigm/Interfaces/`

### Phase 3 — map to full server import strategy
Once the full shard/server import strategy is decided, reconcile whether AIGM should live:
- as direct mirrored server paths under `server/`
- as tracked custom overlays under `custom/scripts/`
- or through a hybrid import model

## Current recommendation

Do not delete the dated snapshot yet.
Use it as the source of truth for the first normalization pass.
