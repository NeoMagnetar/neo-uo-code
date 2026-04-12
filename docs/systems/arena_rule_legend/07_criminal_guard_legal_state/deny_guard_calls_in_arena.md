# Title
Deny guard calls in arena

## Domain
Criminal, guard, and legal state

## Command Phrase
Deny guard calls in arena

## Intent
Capture the arena rule "Deny guard calls in arena" as a stable design and implementation tracking unit within the criminal, guard, and legal state domain.

## Status
legend_only

## Implementation Surface
- GuardedRegion, Region, Mobile legal state, DuelContext

## Likely Code Touchpoints
- `server/*/GuardedRegion.cs`
- `server/*/Region.cs`
- `server/*/Mobile.cs`
- `server/*/DuelContext*.cs`

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
- [Enable guards in bounds](enable_guards_in_bounds.md)
- [Auto-flag interference as criminal](auto_flag_interference_as_criminal.md)

## Progress Log
- 2026-04-11 — OpenClaw — initialized from arena command corpus with status `legend_only`.

## Future Tasks
- Link concrete server/custom code paths when implementation begins.
- Update status when planning, coding, or testing starts.
