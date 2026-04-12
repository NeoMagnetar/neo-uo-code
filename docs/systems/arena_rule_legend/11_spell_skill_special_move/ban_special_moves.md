# Title
Ban special moves

## Domain
Spell, skill, and special move

## Command Phrase
Ban special moves

## Intent
Capture the arena rule "Ban special moves" as a stable design and implementation tracking unit within the spell, skill, and special move domain.

## Status
legend_only

## Implementation Surface
- Region spell hooks, OnSkillUse, Target validation, DuelContext rulesets

## Likely Code Touchpoints
- `server/*/Region.cs`
- `server/*/SpellHelper.cs`
- `server/*/DuelContext*.cs`
- `custom/controllers/*Ruleset*`

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
- [Ban area-of-effect spells](ban_area_of_effect_spells.md)
- [Ban selected skills](ban_selected_skills.md)

## Progress Log
- 2026-04-11 — OpenClaw — initialized from arena command corpus with status `legend_only`.

## Future Tasks
- Link concrete server/custom code paths when implementation begins.
- Update status when planning, coding, or testing starts.
