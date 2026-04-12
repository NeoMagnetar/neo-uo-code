# Title
Ban area-of-effect spells

## Domain
Spell, skill, and special move

## Command Phrase
Ban area-of-effect spells

## Intent
Capture the arena rule "Ban area-of-effect spells" as a stable design and implementation tracking unit within the spell, skill, and special move domain.

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
- [Ban field spells](ban_field_spells.md)
- [Ban special moves](ban_special_moves.md)

## Progress Log
- 2026-04-11 — OpenClaw — initialized from arena command corpus with status `legend_only`.

## Future Tasks
- Link concrete server/custom code paths when implementation begins.
- Update status when planning, coding, or testing starts.
