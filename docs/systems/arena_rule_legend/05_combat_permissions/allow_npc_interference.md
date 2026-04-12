# Title
Allow NPC interference

## Domain
Combat permissions

## Command Phrase
Allow NPC interference

## Intent
Capture the arena rule "Allow NPC interference" as a stable design and implementation tracking unit within the combat permissions domain.

## Status
legend_only

## Implementation Surface
- Region, GuardedRegion, DuelContext, Combat permission hooks

## Likely Code Touchpoints
- `server/*/Region.cs`
- `server/*/GuardedRegion.cs`
- `custom/controllers/*Arena*`
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
- [Deny damage to summoned creatures](deny_damage_to_summoned_creatures.md)
- [Deny NPC interference](deny_npc_interference.md)

## Progress Log
- 2026-04-11 — OpenClaw — initialized from arena command corpus with status `legend_only`.

## Future Tasks
- Link concrete server/custom code paths when implementation begins.
- Update status when planning, coding, or testing starts.
