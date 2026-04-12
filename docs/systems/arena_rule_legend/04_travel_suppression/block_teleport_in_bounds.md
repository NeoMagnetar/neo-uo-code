# Title
Block teleport in bounds

## Domain
Travel suppression

## Command Phrase
Block teleport in bounds

## Intent
Capture the arena rule "Block teleport in bounds" as a stable design and implementation tracking unit within the travel suppression domain.

## Status
legend_only

## Implementation Surface
- SpellHelper, Travel validation, Region, Moongate, Teleporter

## Likely Code Touchpoints
- `server/*/SpellHelper.cs`
- `server/*/Region.cs`
- `custom/controllers/*Travel*`

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
- [Block mark in bounds](block_mark_in_bounds.md)
- [Block teleport while flagged in match](block_teleport_while_flagged_in_match.md)

## Progress Log
- 2026-04-11 — OpenClaw — initialized from arena command corpus with status `legend_only`.

## Future Tasks
- Link concrete server/custom code paths when implementation begins.
- Update status when planning, coding, or testing starts.
