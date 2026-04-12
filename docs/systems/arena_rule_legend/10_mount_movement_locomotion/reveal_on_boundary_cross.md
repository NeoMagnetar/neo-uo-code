# Title
Reveal on boundary cross

## Domain
Mount, movement, and locomotion

## Command Phrase
Reveal on boundary cross

## Intent
Capture the arena rule "Reveal on boundary cross" as a stable design and implementation tracking unit within the mount, movement, and locomotion domain.

## Status
legend_only

## Implementation Surface
- Region movement hooks, Mount checks, Boundary validator, Custom locomotion controllers

## Likely Code Touchpoints
- `server/*/Region.cs`
- `server/*/Mobile.cs`
- `custom/controllers/*Boundary*`
- `custom/controllers/*Arena*`

## Dependencies
- Chapter index for domain context.
- implementation_status.md for current status rollup.
- change_log.md for structural updates over time.

## Edge Cases
- Confirm interaction with neighboring region, travel, and duel-state rules before implementation.
- Verify whether staff or test overrides should bypass this rule.

## Notes
- Initialized from the imported arena command corpus.
- Keep the command phrase stable even if implementation details evolve.

## Related Rules
- [Bounce from spectator barrier](bounce_from_spectator_barrier.md)
- [Disable stealth in ring](disable_stealth_in_ring.md)

## Progress Log
- 2026-04-11 — OpenClaw — initialized from arena command corpus with status `legend_only`.

## Future Tasks
- Link concrete server/custom code paths when implementation begins.
- Update status when planning, coding, or testing starts.
