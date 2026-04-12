# Title
Allow only cosmetic companions

## Domain
Pets, summons, and followers

## Command Phrase
Allow only cosmetic companions

## Intent
Capture the arena rule "Allow only cosmetic companions" as a stable design and implementation tracking unit within the pets, summons, and followers domain.

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
- [Strip escorts and companions at gate](strip_escorts_and_companions_at_gate.md)

## Progress Log
- 2026-04-11 — OpenClaw — initialized from arena command corpus with status `legend_only`.

## Future Tasks
- Link concrete server/custom code paths when implementation begins.
- Update status when planning, coding, or testing starts.
