# AIGM Companion Reactive Self-Bandaging Notes

Date: 2026-06-01

## Summary

This note captures the ServUO implementation change from fake instant self-healing to native shard-timed self-bandaging for Dakeyras and Danyal.

Primary goals addressed:
- use the real shard bandage timer flow for self-healing
- auto-trigger self-bandaging when companions are under attack and injured
- avoid duplicate bandage starts while a bandage is already active

## Native bandage path

Reactive self-bandaging now uses the shard's native bandage flow:
- `BandageContext.BeginHeal(...)`
- real ServUO bandage timing
- real success/failure timing rules
- real bandage consumption

## Auto trigger behavior

Reactive support now attempts self-bandaging when:
- the companion is under attack
- the companion is injured at all

This replaced the older behavior that only attempted support healing after a lower-health threshold was reached.

## Current scope

At present, the native conversion is focused on self-bandaging behavior.

Non-self support/bandage paths may still use older helper behavior and can be normalized later if desired.

## Files touched

Likely updated files include:
- `Scripts/Custom/AIGM/AIGMCompanionSkillExecutor.cs`
- `Scripts/Custom/AIGM/AIGMCompanionActionExecutor.cs`

## Follow-up ideas
- normalize all bandage usage onto native shard mechanics, not only self-bandage
- refine combat-pressure heuristics that trigger support actions
