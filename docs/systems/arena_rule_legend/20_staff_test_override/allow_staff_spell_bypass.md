# Title
Allow staff spell bypass

## Domain
Staff, test, and override

## Command Phrase
Allow staff spell bypass

## Intent
Capture the arena rule "Allow staff spell bypass" as a stable design and implementation tracking unit within the staff, test, and override domain.

## Status
legend_only

## Implementation Surface
- Custom controller, Staff access checks, Observer routing, Audit logging

## Likely Code Touchpoints
- `custom/controllers/*Arena*`
- `custom/controllers/*Admin*`
- `server/*/AccessLevel*.cs`

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
- [Allow staff to bypass entry lock](allow_staff_to_bypass_entry_lock.md)
- [Allow staff hidden observer mode](allow_staff_hidden_observer_mode.md)

## Progress Log
- 2026-04-11 — OpenClaw — initialized from arena command corpus with status `legend_only`.

## Future Tasks
- Link concrete server/custom code paths when implementation begins.
- Update status when planning, coding, or testing starts.
