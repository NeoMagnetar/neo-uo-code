# Title
Ban potions

## Domain
Item, equipment, and consumable

## Command Phrase
Ban potions

## Intent
Capture the arena rule "Ban potions" as a stable design and implementation tracking unit within the item, equipment, and consumable domain.

## Status
legend_only

## Implementation Surface
- DuelContext, Item equip checks, Item use checks, Backpack routing

## Likely Code Touchpoints
- `server/*/DuelContext*.cs`
- `server/*/Mobile.cs`
- `server/*/Item*.cs`
- `custom/controllers/*Loadout*`

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
- [Allow potions](allow_potions.md)

## Progress Log
- 2026-04-11 — OpenClaw — initialized from arena command corpus with status `legend_only`.

## Future Tasks
- Link concrete server/custom code paths when implementation begins.
- Update status when planning, coding, or testing starts.
