# Title
Hide spectator chat from fighters

## Domain
Spectator, outsider, and interference

## Command Phrase
Hide spectator chat from fighters

## Intent
Capture the arena rule "Hide spectator chat from fighters" as a stable design and implementation tracking unit within the spectator, outsider, and interference domain.

## Status
legend_only

## Implementation Surface
- Region, Interference handler, Target validation, Gate routing

## Likely Code Touchpoints
- `server/*/Region.cs`
- `custom/controllers/*Interference*`
- `custom/controllers/*Entry*`

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
- [Allow staff invisible observer mode](allow_staff_invisible_observer_mode.md)
- [Deny pet entry from spectator zone](deny_pet_entry_from_spectator_zone.md)

## Progress Log
- 2026-04-11 — OpenClaw — initialized from arena command corpus with status `legend_only`.

## Future Tasks
- Link concrete server/custom code paths when implementation begins.
- Update status when planning, coding, or testing starts.
