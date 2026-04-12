# Title
Deny stuck menu in match

## Domain
Login, logout, stuck, and persistence

## Command Phrase
Deny stuck menu in match

## Intent
Capture the arena rule "Deny stuck menu in match" as a stable design and implementation tracking unit within the login, logout, stuck, and persistence domain.

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
- [Extend logout delay in bounds](extend_logout_delay_in_bounds.md)
- [Auto-forfeit disconnects](auto_forfeit_disconnects.md)

## Progress Log
- 2026-04-11 — OpenClaw — initialized from arena command corpus with status `legend_only`.

## Future Tasks
- Link concrete server/custom code paths when implementation begins.
- Update status when planning, coding, or testing starts.
