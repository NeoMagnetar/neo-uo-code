# Title
Require ready-check

## Domain
Match flow, countdown, and victory

## Command Phrase
Require ready-check

## Intent
Capture the arena rule "Require ready-check" as a stable design and implementation tracking unit within the match flow, countdown, and victory domain.

## Status
legend_only

## Implementation Surface
- DuelContext, Match controller, Arena state tracker

## Likely Code Touchpoints
- `server/*/DuelContext*.cs`
- `custom/controllers/*Match*`
- `custom/controllers/*ArenaState*`

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
- [Validate before start](validate_before_start.md)

## Progress Log
- 2026-04-11 — OpenClaw — initialized from arena command corpus with status `legend_only`.

## Future Tasks
- Link concrete server/custom code paths when implementation begins.
- Update status when planning, coding, or testing starts.
