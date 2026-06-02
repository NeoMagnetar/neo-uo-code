# AIGM Companion Speech / Travel / Support Notes

Date: 2026-06-01

## Summary

This note captures the current AIGM companion behavior changes implemented in the ServUO codebase for Dakeyras and Danyal.

Primary goals addressed:
- restore broader owner/companion hearing for chatbot-style conversation
- keep direct command authority separate from general dialogue hearing
- reduce travel/tracking state conflicts
- make tracking behave more like bounded route perturbation than mission takeover
- switch reactive self-bandaging to the shard's native bandage timer flow

## Speech and dialogue changes

### Relay / hearing model
- Companion-origin relayed speech is treated as dialogue-first instead of command-first.
- The old listener-name injection hack on companion relay was removed.
- Owner speech remains broadly hearable.
- Addressing influences direct command authority, but should not suppress awareness.

### Dialogue queue behavior
- `companion_dialogue` replies bypass trusted-action reinterpretation.
- Dialogue publishing was made symmetric so both companions relay with a small delay instead of Dakeyras publishing immediately while Danyal delays.
- Linked-companion address detection was broadened to tolerate punctuation/encoding noise and more natural mentions.

## Command / movement / travel changes

### Travel authority cleanup
- Travel startup no longer forces `OrderType.Stop` in `AIGMMovementController.StartPathToPoint(...)`.
- Travel cancellation no longer automatically forces `OrderType.Stay` in `AIGMCompanionTravelController.StopTravel(...)`.

### Follow companion
- `TryFollowCompanion(...)` now uses linked-companion resolution instead of only a narrow nearby scan.

## Tracking changes

### Pursuit bootstrapping
- Tracking pursuit can create a travel objective when none already exists.
- This prevents tracking from failing simply because no active route objective was present.

### Bounded excursion model
Tracking pursuit was adjusted to behave as a temporary excursion in service of travel:
- shorter pursuit lifetimes
- route leash behavior
- short no-contact timeout
- blocked pursuit resumes route instead of stubbornly holding control

Intent:
- tracking should keep companions lively
- allow route readjustment around terrain / mountains / obstruction
- occasionally engage targets
- then resume the primary destination

## Reactive self-bandaging

### Native shard timer path
Reactive self-bandaging now uses the real shard bandage flow:
- `BandageContext.BeginHeal(...)`
- real ServUO bandage timing
- real success/failure timing rules
- avoids duplicate start when already bandaging

### Auto trigger behavior
`TryReactiveSupport(...)` now attempts self-bandaging when:
- the companion is under attack
- the companion is injured at all

This replaced the older behavior that only attempted support healing once the companion dropped to a low-health threshold.

### Scope of change
At present, the native conversion is focused on self-bandaging behavior. Non-self support/bandage paths may still use older helper behavior and can be normalized later if desired.

## Files touched

Likely updated files include:
- `Scripts/Mobiles/NPCs/AIGMCompanionDakeyras.cs`
- `Scripts/Mobiles/NPCs/AIGMCompanionDanyal.cs`
- `Scripts/Custom/AIGM/AIGMCompanionSpeechQueue.cs`
- `Scripts/Custom/AIGM/AIGMCompanionActionExecutor.cs`
- `Scripts/Custom/AIGM/AIGMCompanionTravelController.cs`
- `Scripts/Custom/AIGM/AIGMMovementController.cs`
- `Scripts/Custom/AIGM/AIGMCompanionSkillExecutor.cs`

## Follow-up ideas
- normalize all bandage usage onto native shard mechanics, not only self-bandage
- improve combat lock persistence during tracking excursions
- continue refining command/address parsing aliases and typo tolerance
- further separate route ownership from temporary combat/awareness excursions
