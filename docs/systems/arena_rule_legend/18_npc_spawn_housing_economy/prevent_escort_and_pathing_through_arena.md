# Title
Prevent escort and pathing through arena

## Domain
NPC, spawn, housing, and economy

## Command Phrase
Prevent escort and pathing through arena

## Intent
Capture the arena rule "Prevent escort and pathing through arena" as a stable design and implementation tracking unit within the npc, spawn, housing, and economy domain.

## Status
legend_only

## Implementation Surface
- Region, GuardedRegion, Spawn control, Vendor access checks

## Likely Code Touchpoints
- `server/*/Region.cs`
- `server/*/GuardedRegion.cs`
- `custom/controllers/*Arena*`
- `server/*/Spawner*.cs`

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
- [Despawn wandering NPCs on match start](despawn_wandering_npcs_on_match_start.md)
- [Keep arena economy-neutral](keep_arena_economy_neutral.md)

## Progress Log
- 2026-04-11 — OpenClaw — initialized from arena command corpus with status `legend_only`.

## Future Tasks
- Link concrete server/custom code paths when implementation begins.
- Update status when planning, coding, or testing starts.
