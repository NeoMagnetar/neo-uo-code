# Title
Keep town and vendor space protected outside arena

## Domain
Facet and map rules

## Command Phrase
Keep town and vendor space protected outside arena

## Intent
Capture the arena rule "Keep town and vendor space protected outside arena" as a stable design and implementation tracking unit within the facet and map rules domain.

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
- [Mirror the arena on a test facet first](mirror_the_arena_on_a_test_facet_first.md)
- [Decide facet law first, local arena law second](decide_facet_law_first_local_arena_law_second.md)

## Progress Log
- 2026-04-11 — OpenClaw — initialized from arena command corpus with status `legend_only`.

## Future Tasks
- Link concrete server/custom code paths when implementation begins.
- Update status when planning, coding, or testing starts.
