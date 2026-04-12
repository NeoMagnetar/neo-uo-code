# Title
Lock team membership at ready-up

## Domain
Team, ally, and party

## Command Phrase
Lock team membership at ready-up

## Intent
Capture the arena rule "Lock team membership at ready-up" as a stable design and implementation tracking unit within the team, ally, and party domain.

## Status
legend_only

## Implementation Surface
- DuelContext, Party checks, Team controllers, Ready-up flow

## Likely Code Touchpoints
- `server/*/DuelContext*.cs`
- `server/*/Party*.cs`
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
- [Free-for-all](free_for_all.md)
- [Randomize starting pads by team](randomize_starting_pads_by_team.md)

## Progress Log
- 2026-04-11 — OpenClaw — initialized from arena command corpus with status `legend_only`.

## Future Tasks
- Link concrete server/custom code paths when implementation begins.
- Update status when planning, coding, or testing starts.
