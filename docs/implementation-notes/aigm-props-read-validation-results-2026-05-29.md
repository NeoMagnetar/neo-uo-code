# AIGM Props Read Validation Results
_Date: 2026-05-29_

## Live tests confirmed

### Working cases
- inspect nearest mobile
- inspect nearby creature / world target
- inspect self / requester
- open structured native props-read gump

### Structured fields observed live
- Kind
- Type
- Name
- Serial
- Map
- Location
- Hue
- Deleted
- Movable
- Alive
- Blessed
- Hits
- Mana
- Stam
- Stats
- AccessLevel
- item-specific exact fields where applicable

## Current weak cases
- `inspect the katana in my bag`
- named contained-item resolution
- named ground-item resolution in clutter

## Files central to this lane
- `Scripts/Custom/AIGM/AIGMActionExecutor.cs`
- `Scripts/Custom/AIGM/AIGMPropertySnapshot.cs`
- `Scripts/Custom/AIGM/AIGMPropsReadAdapter.cs`
- `Scripts/Custom/AIGM/AIGMProposalAugmenter.cs`
- `Scripts/Custom/AIGM/AIGMTargetResolver.cs`

## Recommendation
Preserve this as a first-pass props milestone, then revisit later for stronger exact named-target resolution before expanding into props-write.
