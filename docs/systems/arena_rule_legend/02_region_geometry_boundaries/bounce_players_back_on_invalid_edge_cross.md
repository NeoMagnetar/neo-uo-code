# Title
Bounce players back on invalid edge-cross

## Domain
Region geometry and boundaries

## Command Phrase
Bounce players back on invalid edge-cross

## Intent
Capture the arena rule "Bounce players back on invalid edge-cross" as a stable design and implementation tracking unit within the region geometry and boundaries domain.

## Status
legend_only

## Implementation Surface
- Region, Boundary validator, DuelContext, Internal wall controllers

## Likely Code Touchpoints
- `server/*/Region.cs`
- `server/*/GuardedRegion.cs`
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
- [Add internal invisible wall](add_internal_invisible_wall.md)
- [Track entry and exit events](track_entry_and_exit_events.md)

## Progress Log
- 2026-04-11 — OpenClaw — initialized from arena command corpus with status `legend_only`.

## Future Tasks
- Link concrete server/custom code paths when implementation begins.
- Update status when planning, coding, or testing starts.
