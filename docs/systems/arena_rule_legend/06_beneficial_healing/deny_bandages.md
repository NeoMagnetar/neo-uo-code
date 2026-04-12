# Title
Deny bandages

## Domain
Beneficial and healing

## Command Phrase
Deny bandages

## Intent
Capture the arena rule "Deny bandages" as a stable design and implementation tracking unit within the beneficial and healing domain.

## Status
legend_only

## Implementation Surface
- Region, GuardedRegion, OnHeal hooks, Buff controllers

## Likely Code Touchpoints
- `server/*/Region.cs`
- `server/*/GuardedRegion.cs`
- `server/*/SpellHelper.cs`
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
- [Allow bandages](allow_bandages.md)
- [Allow cure potions](allow_cure_potions.md)

## Progress Log
- 2026-04-11 — OpenClaw — initialized from arena command corpus with status `legend_only`.

## Future Tasks
- Link concrete server/custom code paths when implementation begins.
- Update status when planning, coding, or testing starts.
