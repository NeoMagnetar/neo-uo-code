# AIGM Props Read First Pass
_Date: 2026-05-29_

## Summary

AIGM now has a live first-pass native props-read lane.

This complements the already-proven Add surfaces and establishes a real inspection capability through the canonical proposal/executor path.

## Confirmed live successes
- inspect nearest mobile / nearby creature
- inspect a nearby Sea Serpent
- inspect the requester / player self
- open structured AI GM props-read gump with exact object/mobile data

## Output richness observed live
The props-read gump is now surfacing exact fields such as:
- type
- serial
- map
- location
- hue
- deleted / movable / alive
- blessed
- hits / mana / stam
- stats
- access level
- item id / weight / layer / amount where relevant

## Important limitation at this milestone
Named item resolution is still incomplete for cases like:
- `inspect the katana in my bag`
- specific ground-item disambiguation in cluttered scenes

So this milestone should be treated as:
- working native props-read first pass
- incomplete named-target refinement

## Significance
AIGM now has live proof of both mutation and inspection capability families, even though inspection targeting still needs refinement.
