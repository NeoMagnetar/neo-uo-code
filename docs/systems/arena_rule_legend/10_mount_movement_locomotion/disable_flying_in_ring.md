# Title
Disable flying in ring

## Domain
Mount, movement, and locomotion

## Command Phrase
Disable flying in ring

## Intent
Capture the arena rule "Disable flying in ring" as a stable design and implementation tracking unit within the mount, movement, and locomotion domain.

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
- [Disable stealth in ring](disable_stealth_in_ring.md)
- [Slow movement in hazard zones](slow_movement_in_hazard_zones.md)

## Progress Log
- 2026-04-11 — OpenClaw — initialized from arena command corpus with status `legend_only`.

## Future Tasks
- Link concrete server/custom code paths when implementation begins.
- Update status when planning, coding, or testing starts.
