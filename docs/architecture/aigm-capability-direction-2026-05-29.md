# AIGM Capability Direction
_Date: 2026-05-29_

## Core direction

AIGM should evolve as an orchestration layer over native NeoUO / ServUO GM systems rather than as a fake parallel GM engine.

## Current architectural finding

The visible AI counselor has split mechanic needs:
- `PlayerVendor`-like embodiment for paperdoll, backpack, clothing, and owner-style interaction
- `BaseCreature`-like control semantics for current movement/follow/pathing systems

These responsibilities should not remain permanently fused into one inheritance assumption.

## Recommended model

Refactor toward explicit capabilities:
- actor / embodiment shell
- inventory capability
- spawn capability
- movement capability
- inspection capability
- targeting capability
- conversation capability

## Live-system guardrails

- keep `AIGMActionProposal` as the canonical execution model
- prefer explicit action kinds for native GM capabilities
- use native backends where available
- avoid broad arbitrary GM string parsing as the core actuation layer
- avoid detouring back into UI affordance work as the main blocker while native capability work is actively progressing
