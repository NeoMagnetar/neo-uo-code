# Title
Set arena facet to Felucca rules

## Domain
Facet and map rules

## Command Phrase
Set arena facet to Felucca rules

## Intent
Capture the arena rule "Set arena facet to Felucca rules" as a stable design and implementation tracking unit within the facet and map rules domain.

## Status
legend_only

## Implementation Surface
- Map rules, Facet definitions, MapDefinitions, Custom controller

## Likely Code Touchpoints
- `server/*/MapDefinitions.cs`
- `server/*/Map.cs`
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
- [Set arena facet to Trammel rules](set_arena_facet_to_trammel_rules.md)

## Progress Log
- 2026-04-11 — OpenClaw — initialized from arena command corpus with status `legend_only`.

## Future Tasks
- Link concrete server/custom code paths when implementation begins.
- Update status when planning, coding, or testing starts.
