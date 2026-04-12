# Title
Count followers against eligibility

## Domain
Pets, summons, and followers

## Command Phrase
Count followers against eligibility

## Intent
Capture the arena rule "Count followers against eligibility" as a stable design and implementation tracking unit within the pets, summons, and followers domain.

## Status
legend_only

## Implementation Surface
- Mobile follower counts, Summon handling, Region, Entry filter

## Likely Code Touchpoints
- `server/*/Mobile.cs`
- `server/*/SpellHelper.cs`
- `server/*/BaseCreature*.cs`
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
- [Allow pets only in beast arena mode](allow_pets_only_in_beast_arena_mode.md)
- [Deny outsider pet healing](deny_outsider_pet_healing.md)

## Progress Log
- 2026-04-11 — OpenClaw — initialized from arena command corpus with status `legend_only`.

## Future Tasks
- Link concrete server/custom code paths when implementation begins.
- Update status when planning, coding, or testing starts.
