# Title
Disable mounts in sudden death

## Domain
Anti-stall and sudden death

## Command Phrase
Disable mounts in sudden death

## Intent
Capture the arena rule "Disable mounts in sudden death" as a stable design and implementation tracking unit within the anti-stall and sudden death domain.

## Status
legend_only

## Implementation Surface
- DuelContext, Arena state tracker, Hazard controllers, Wall controllers

## Likely Code Touchpoints
- `server/*/DuelContext*.cs`
- `custom/controllers/*SuddenDeath*`
- `custom/controllers/*Hazard*`

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
- [Reveal all hidden fighters](reveal_all_hidden_fighters.md)
- [Disable ranged-only play in sudden death](disable_ranged_only_play_in_sudden_death.md)

## Progress Log
- 2026-04-11 — OpenClaw — initialized from arena command corpus with status `legend_only`.

## Future Tasks
- Link concrete server/custom code paths when implementation begins.
- Update status when planning, coding, or testing starts.
