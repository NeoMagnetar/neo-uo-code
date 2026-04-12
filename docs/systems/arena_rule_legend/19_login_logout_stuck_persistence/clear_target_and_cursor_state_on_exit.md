# Title
Clear target and cursor state on exit

## Domain
Login, logout, stuck, and persistence

## Command Phrase
Clear target and cursor state on exit

## Intent
Capture the arena rule "Clear target and cursor state on exit" as a stable design and implementation tracking unit within the login, logout, stuck, and persistence domain.

## Status
legend_only

## Implementation Surface
- Region logout hooks, DuelContext, Arena controller, Persistence trackers

## Likely Code Touchpoints
- `server/*/Region.cs`
- `server/*/DuelContext*.cs`
- `custom/controllers/*ArenaState*`
- `custom/controllers/*Persistence*`

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
- [Clear aggressions on exit](clear_aggressions_on_exit.md)
- [Close arena gumps on finish](close_arena_gumps_on_finish.md)

## Progress Log
- 2026-04-11 — OpenClaw — initialized from arena command corpus with status `legend_only`.

## Future Tasks
- Link concrete server/custom code paths when implementation begins.
- Update status when planning, coding, or testing starts.
