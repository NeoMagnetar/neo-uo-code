# Title
Keep arena economy-neutral

## Domain
NPC, spawn, housing, and economy

## Command Phrase
Keep arena economy-neutral

## Intent
Capture the arena rule "Keep arena economy-neutral" as a stable design and implementation tracking unit within the npc, spawn, housing, and economy domain.

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
- [Prevent escort and pathing through arena](prevent_escort_and_pathing_through_arena.md)
- [Keep reward vendors outside ring only](keep_reward_vendors_outside_ring_only.md)

## Progress Log
- 2026-04-11 — OpenClaw — initialized from arena command corpus with status `legend_only`.

## Future Tasks
- Link concrete server/custom code paths when implementation begins.
- Update status when planning, coding, or testing starts.
