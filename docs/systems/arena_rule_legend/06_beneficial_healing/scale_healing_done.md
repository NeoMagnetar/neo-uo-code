# Title
Scale healing done

## Domain
Beneficial and healing

## Command Phrase
Scale healing done

## Intent
Capture the arena rule "Scale healing done" as a stable design and implementation tracking unit within the beneficial and healing domain.

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
- [Scale healing received](scale_healing_received.md)
- [Strip active buffs on entry](strip_active_buffs_on_entry.md)

## Progress Log
- 2026-04-11 — OpenClaw — initialized from arena command corpus with status `legend_only`.

## Future Tasks
- Link concrete server/custom code paths when implementation begins.
- Update status when planning, coding, or testing starts.
