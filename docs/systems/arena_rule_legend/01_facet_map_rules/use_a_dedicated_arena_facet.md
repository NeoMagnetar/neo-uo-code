# Title
Use a dedicated arena facet

## Domain
Facet and map rules

## Command Phrase
Use a dedicated arena facet

## Intent
Capture the arena rule "Use a dedicated arena facet" as a stable design and implementation tracking unit within the facet and map rules domain.

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
- [Keep the build world safe, make the arena world lethal](keep_the_build_world_safe_make_the_arena_world_lethal.md)
- [Mirror the arena on a test facet first](mirror_the_arena_on_a_test_facet_first.md)

## Progress Log
- 2026-04-11 — OpenClaw — initialized from arena command corpus with status `legend_only`.

## Future Tasks
- Link concrete server/custom code paths when implementation begins.
- Update status when planning, coding, or testing starts.
