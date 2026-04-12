# Title
Bounce outsiders who cross line

## Domain
Spectator, outsider, and interference

## Command Phrase
Bounce outsiders who cross line

## Intent
Capture the arena rule "Bounce outsiders who cross line" as a stable design and implementation tracking unit within the spectator, outsider, and interference domain.

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
- [Deny corpse looting by outsiders](deny_corpse_looting_by_outsiders.md)
- [Separate spectator and participant gates](separate_spectator_and_participant_gates.md)

## Progress Log
- 2026-04-11 — OpenClaw — initialized from arena command corpus with status `legend_only`.

## Future Tasks
- Link concrete server/custom code paths when implementation begins.
- Update status when planning, coding, or testing starts.
