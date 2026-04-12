# Title
Scale melee damage up

## Domain
Environment, hazard, and atmosphere

## Command Phrase
Scale melee damage up

## Intent
Capture the arena rule "Scale melee damage up" as a stable design and implementation tracking unit within the environment, hazard, and atmosphere domain.

## Status
legend_only

## Implementation Surface
- Region, SpellDamageScalar, Hazard controller, Announcer systems

## Likely Code Touchpoints
- `server/*/Region.cs`
- `custom/controllers/*Hazard*`
- `custom/controllers/*Announcer*`

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
- [Scale spell damage down](scale_spell_damage_down.md)
- [Scale healing down](scale_healing_down.md)

## Progress Log
- 2026-04-11 — OpenClaw — initialized from arena command corpus with status `legend_only`.

## Future Tasks
- Link concrete server/custom code paths when implementation begins.
- Update status when planning, coding, or testing starts.
