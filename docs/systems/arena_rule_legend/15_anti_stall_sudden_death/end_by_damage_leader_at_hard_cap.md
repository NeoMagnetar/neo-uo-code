# Title
End by damage leader at hard cap

## Domain
Anti-stall and sudden death

## Command Phrase
End by damage leader at hard cap

## Intent
Capture the arena rule "End by damage leader at hard cap" as a stable design and implementation tracking unit within the anti-stall and sudden death domain.

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
- [End as tie at hard cap](end_as_tie_at_hard_cap.md)
- [Collapse walls inward](collapse_walls_inward.md)

## Progress Log
- 2026-04-11 — OpenClaw — initialized from arena command corpus with status `legend_only`.

## Future Tasks
- Link concrete server/custom code paths when implementation begins.
- Update status when planning, coding, or testing starts.
