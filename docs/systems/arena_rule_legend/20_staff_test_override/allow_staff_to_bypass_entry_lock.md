# Title
Allow staff to bypass entry lock

## Domain
Staff, test, and override

## Command Phrase
Allow staff to bypass entry lock

## Intent
Capture the arena rule "Allow staff to bypass entry lock" as a stable design and implementation tracking unit within the staff, test, and override domain.

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
- [Allow staff spell bypass](allow_staff_spell_bypass.md)

## Progress Log
- 2026-04-11 — OpenClaw — initialized from arena command corpus with status `legend_only`.

## Future Tasks
- Link concrete server/custom code paths when implementation begins.
- Update status when planning, coding, or testing starts.
