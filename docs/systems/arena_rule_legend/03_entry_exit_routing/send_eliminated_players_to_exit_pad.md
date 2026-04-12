# Title
Send eliminated players to exit pad

## Domain
Entry, exit, and routing

## Command Phrase
Send eliminated players to exit pad

## Intent
Capture the arena rule "Send eliminated players to exit pad" as a stable design and implementation tracking unit within the entry, exit, and routing domain.

## Status
legend_only

## Implementation Surface
- Moongate, Teleporter, Entry filter, Exit router, RegionControl

## Likely Code Touchpoints
- `server/*/Moongate*.cs`
- `server/*/Teleporter*.cs`
- `custom/controllers/*Entry*`
- `custom/controllers/*Exit*`

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
- [Deny re-entry after elimination](deny_re_entry_after_elimination.md)
- [Route spectators to stands](route_spectators_to_stands.md)

## Progress Log
- 2026-04-11 — OpenClaw — initialized from arena command corpus with status `legend_only`.

## Future Tasks
- Link concrete server/custom code paths when implementation begins.
- Update status when planning, coding, or testing starts.
