# Title
Delete corpse on round end

## Domain
Death, corpse, resurrection, and loot

## Command Phrase
Delete corpse on round end

## Intent
Capture the arena rule "Delete corpse on round end" as a stable design and implementation tracking unit within the death, corpse, resurrection, and loot domain.

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
- [Bounce corpse contents out](bounce_corpse_contents_out.md)
- [Deny in-match resurrection](deny_in_match_resurrection.md)

## Progress Log
- 2026-04-11 — OpenClaw — initialized from arena command corpus with status `legend_only`.

## Future Tasks
- Link concrete server/custom code paths when implementation begins.
- Update status when planning, coding, or testing starts.
