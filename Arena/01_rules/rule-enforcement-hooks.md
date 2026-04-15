# Rule Enforcement Hooks

## Goal
Future implementation should enforce arena law through reusable ServUO-facing law surfaces rather than ad hoc coordinate checks.

## Likely enforcement surfaces
- Region-based entry and exit handling.
- Region-aware travel restriction handling for recall and gate travel.
- Mount restriction handling tied to arena region state.
- Messaging surfaces that explain why a blocked action failed.

## Phase 1 note
These notes point toward enforcement surfaces only. They do not define final code contracts or final class implementations.