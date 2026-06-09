# NEOUO FULL INTEGRATION PHASE56Q R6 LIVE COMPANION RECOVERY PLAN REPORT

## 1. Current State Confirmation
- branch: `neo/staging-aigm`
- HEAD: `6b49c90b9`
- build result: `dotnet build .\ServUO.sln -v:minimal` succeeded with 0 warnings and 0 errors
- git status: working tree contains pre-existing untracked report files; no files were staged as part of this phase
- audit report reviewed: `C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\NEOUO_FULL_INTEGRATION_PHASE56Q_R6_SERVER_LANE_COMPANION_RUNTIME_ALIGNMENT_AUDIT_REPORT.md`

## 2. Server Lane Map Summary
- authoritative current code server: `C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg`
- old working reference only: `C:\UO\Server\Neo Ultima Online\NeoUO-Dev`
- preserved backup: `C:\UO\Server\Neo Ultima Online\Backups\NeoUO-Dev-preserve-aigm-working-20260607-072757`
- OpenClaw Ultima workspace: `C:\.openclaw\workspace-ultima-online`
- OpenClaw main workspace is not part of this lane and must not receive NeoUO runtime edits

## 3. Middleware Summary
- companion middleware file: `C:\.openclaw\workspace-ultima-online\aigm-middleware-service.js`
- companion middleware endpoint: `127.0.0.1:4876`
- control scripts:
  - `start-aigm-middleware.ps1`
  - `status-aigm-middleware.ps1`
  - `stop-aigm-middleware.ps1`
  - `Restart-AIGM-Middleware.ps1`
- middleware startup/order is already established and must not be changed during the first code-bearing recovery slice
- current bridge compatibility must be reviewed before any live AI-response testing, but that review is later than the first shell phase

## 4. Companion Lane Recovery Objective
“Companion lane restored” for the current integration repo means all of the following are true:
- companion mobiles for Dakeyras, Danyal, and Dardalion exist in the current integration repo
- companion command registration exists in the current integration repo
- named companion commands route into the companion lane and identify the correct addressed companion
- shared owner companion commands route into the companion lane and resolve to linked companions rather than generic counselor handling
- counselor/gump generic AIGM surfaces do not own or propose companion-only follow/guard/stay commands
- movement does not occur until the explicit movement-executor phase is approved and implemented behind current movement contracts and gates

This objective intentionally separates command-lane restoration from movement restoration.

## 5. Recommended First Code-Bearing Phase
### Recommended phase
`PHASE 56Q-R6-COMPANION-LANE-SHELLS — Add Companion Command + Mobile Identity Shells Without Movement`

### Why this is the safest first slice
This slice restores the minimal shape of the companion lane without reintroducing old Dev movement, state, or autonomous behavior. It creates a controlled re-entry point into the current integration repo so command registration and companion identity can be compiled, inspected, and tested independently from runtime movement side effects.

### Scope for the first slice
- selectively add companion command registration
- selectively add Dakeyras, Danyal, and Dardalion identity shells
- keep shells minimal and compile-safe
- no live movement
- no `StateAccess`
- no travel controller
- no auto-path navigator
- no arbitrary action executor
- no counselor/gump mutation in the same slice unless strictly required for compile-safe boundary wiring
- build verification only, with optional safe spawn/construct verification later

### Alternate phase
`PHASE 56Q-R6-COMPANION-COMMAND-ROUTING-SPEC — Specify Companion/Counselor Routing Boundary Before Code`

Use the alternate if the routing boundary is still considered too ambiguous to safely introduce shells.

## 6. Companion Command Routing Plan
The command-routing boundary must be explicit before live movement is enabled.

### Target behavior
- `dak follow me` → enters companion lane as a named companion command and resolves to Dakeyras
- `danyal follow me` → enters companion lane as a named companion command and resolves to Danyal
- `dar follow me` → enters companion lane as a named companion command and resolves to Dardalion
- `follow me` → enters companion lane as a shared owner companion command and resolves to linked companions according to the companion-link model

### Required routing rules
- named companion-addressed commands enter companion lane first
- shared owner commands intended for companions enter companion lane first
- counselor/gump generic AIGM lane must not intercept these companion command forms
- routing must be testable before live movement is enabled
- if movement is not yet implemented, the lane should still confirm correct recognition and targeting without issuing live movement side effects

### Planned technical interpretation
The companion lane should expose a narrow parse-and-resolve boundary:
1. identify whether an utterance is companion-addressed or companion-command-shaped
2. resolve explicit addressee if present (`dak`, `dakeyras`, `danyal`, `dar`, `dardalion`)
3. if no explicit addressee, resolve to the shared linked-companion lane
4. produce a normalized companion action intent record
5. hand off the intent to the current movement/router authority only when the executor phase exists

Until the movement executor exists, the routing path should terminate in a no-op or traceable acknowledgment rather than falling through to counselor/gump handling.

## 7. Counselor Contamination Prevention Plan
The future strategy must block counselor ownership of companion commands as early as possible.

### Required prevention behavior
- detect companion-addressed utterances early in the speech pipeline
- detect shared owner companion command forms early in the speech pipeline
- prevent `AIGMCounselor` and generic gump proposal flows from claiming companion-only commands
- prevent `gm_follow_requester` or equivalent counselor-framed generic follow proposals from surfacing for companion commands
- if a companion command reaches counselor lane anyway, treat that as contamination and emit a traceable error condition rather than silently accepting the route

### Practical boundary strategy
- establish a companion-command classifier ahead of generic counselor/gump proposal logic
- reserve known companion-addressed aliases and shared owner command phrases for companion routing
- route unmatched generic conversation back to counselor lane only when the utterance is not companion-command-shaped
- during early implementation, add diagnostic logging/telemetry points around companion-command capture and contamination fallback so route ownership is observable during testing

## 8. Movement Restoration Architecture Plan
Planning only. No implementation in this phase.

### Preferred architecture
Add `UMGServUOMovementExecutor` as the first narrow live movement executor behind existing movement gates/contracts.

### First explicit primitive set
- `FollowPlayer`
- `ReturnToPlayer` / `Come`
- `GuardTarget`
- `HoldPosition`

### Required movement constraints
- use `ControlOrder` / `ControlTarget` only behind `IUMGMovementExecutor`
- `UMGMovementRouter` and movement gates remain the authority for routing and permission decisions
- no direct `Location` mutation
- no `StateAccess` dependency in the initial executor phase
- no auto-path navigator in the initial executor phase
- no travel controller in the initial executor phase
- no autonomous background movement expansion until the narrow executor is validated

### Recovery sequencing principle
Movement must be introduced only after command entry, identity shells, and counselor-boundary behavior are verified. This keeps the first recovery step compile-safe and reduces the blast radius of old Dev movement assumptions.

## 9. Old Dev Selective-Port Classification Table
| Old Dev file | Classification | Rationale |
|---|---|---|
| `Scripts\Commands\AIGMCompanionCommand.cs` | port now | This is the likely narrow command entry surface needed to reintroduce companion command registration in a controlled way. It should be selectively reauthored/ported into current integration rather than blindly copied. |
| `Scripts\Mobiles\NPCs\AIGMCompanionDakeyras.cs` | port now | Identity shell candidate. Keep only minimal mobile identity/registration behavior in the first slice. Exclude live movement/runtime coupling. |
| `Scripts\Mobiles\NPCs\AIGMCompanionDanyal.cs` | port now | Same reasoning as Dakeyras: identity shell only in first slice. |
| `Scripts\Mobiles\NPCs\AIGMCompanionDardalion.cs` | port now | Same reasoning as Dakeyras/Danyal: identity shell only in first slice. |
| `AIGMCompanionSpeechQueue.cs` | port later behind gate | Useful for shared-lane handling, but old arbitration behavior contributes to the one-companion-owner-turn problem. Revisit only after routing boundary is specified. |
| `AIGMCompanionActionExecutor.cs` | port later behind gate | Old action execution is too broad for the first recovery slice. Reintroduce only after the current integration routing and executor abstractions are ready. |
| `AIGMCompanionDirectActionPolicy.cs` | port later behind gate | Valuable as reference for command interpretation, but should be selectively distilled into current routing contracts instead of copied wholesale. |
| `AIGMCompanionTravelController.cs` | do not port | This is exactly the kind of travel/autonomy coupling that should stay out until the narrow movement executor proves safe. |
| `AIGMCompanionAutoPathNavigator.cs` | do not port | Too invasive for the recovery start. Keep out of the first recovery path and likely out of the narrow movement phase entirely. |
| `AIGMCompanionStateAccess.cs` | reference only | Explicitly excluded from the first recovery path. High coupling surface. Use only as a reference when later phases need to understand old state assumptions. |
| `AIGMCompanionBridgeClient.cs` | reference only | Middleware/bridge compatibility needs planned review first. Do not port into the first shell slice. Treat as a later bridge-alignment reference. |

## 10. Test Plan
### Stage A — build-only
- add the first shell-phase code only
- build the current integration repo
- expected outcome: compile success
- no server runtime behavior required yet

### Stage B — spawn/construct test
- confirm companion mobiles can be added/constructed/spawned safely
- expected outcome: identity shells exist and are constructible
- no follow movement expected
- no travel/path behavior expected

### Stage C — command-route test
- named command recognized: `dak follow me`
- named command recognized: `danyal follow me`
- named command recognized: `dar follow me`
- shared command recognized: `follow me`
- companion lane claims the command
- counselor lane does not trigger
- no `gm_follow_requester` proposal surfaces for companion commands
- still no live movement unless executor exists

### Stage D — movement-executor test
Only after `UMGServUOMovementExecutor` exists and is approved.
- test `dak follow me`
- test `danyal follow me`
- test shared `follow me`
- validate router/gates remain authority
- validate movement uses explicit primitives only
- validate no direct location mutation and no old autonomous travel coupling

## 11. No-Copy List
Do not do any of the following in the recovery path:
- bulk-copy old Dev into current integration
- copy entire `Scripts\Custom\AIGM`
- copy entire `Scripts\Mobiles\NPCs`
- copy `AIGMCompanionStateAccess` directly
- copy old travel/path/autonomous movement directly
- keep counselor/gump lane active for companion follow commands
- add direct movement outside current gates/contracts
- bypass current movement contracts

## 12. Recommendation Summary
- current integration repo remains the only forward code-save/build/commit authority
- old Dev remains reference only
- the safest first code-bearing slice is `PHASE 56Q-R6-COMPANION-LANE-SHELLS`
- movement must remain disabled until a narrow `UMGServUOMovementExecutor` is added behind current contracts
- counselor/gump contamination prevention must be designed as an explicit routing boundary, not left to incidental behavior

## 13. No-Mutation Confirmation
This planning phase made no runtime mutations.
- no files changed: no runtime code files changed during this phase
- no files staged: confirmed
- no commit made: confirmed
- no backup copied: confirmed
- no reset/clean/overwrite: confirmed
- no middleware changed: confirmed
- no runtime code changed: confirmed

## 14. Expected Next Phase
`PHASE 56Q-R6-COMPANION-LANE-SHELLS — Add Companion Command + Mobile Identity Shells Without Movement`

## 15. Alternate Next Phase
`PHASE 56Q-R6-COMPANION-COMMAND-ROUTING-SPEC — Specify Companion/Counselor Routing Boundary Before Code`
