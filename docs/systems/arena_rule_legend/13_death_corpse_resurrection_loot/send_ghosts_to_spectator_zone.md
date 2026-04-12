# Title
Send ghosts to spectator zone

## Domain
Death, corpse, resurrection, and loot

## Command Phrase
Send ghosts to spectator zone

## Intent
Capture the arena rule "Send ghosts to spectator zone" as a stable design and implementation tracking unit within the death, corpse, resurrection, and loot domain.

## Status
legend_only

## Implementation Surface
- OnDeath, OnResurrect, Corpse handling, DuelContext cleanup

## Likely Code Touchpoints
- `server/*/Region.cs`
- `server/*/Corpse*.cs`
- `server/*/DuelContext*.cs`
- `custom/controllers/*Match*`

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
- [Auto-resurrect after match](auto_resurrect_after_match.md)
- [Lossless death in test arena](lossless_death_in_test_arena.md)

## Progress Log
- 2026-04-11 — OpenClaw — initialized from arena command corpus with status `legend_only`.

## Future Tasks
- Link concrete server/custom code paths when implementation begins.
- Update status when planning, coding, or testing starts.
