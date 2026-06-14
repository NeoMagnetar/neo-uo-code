# NeoUO Full Integration - Phase58A Execution Spine Plan Report

## Scope
Produce a concrete implementation plan for the shift from read-only/manual companion smoke testing toward bounded executable companion behavior under UMG governance, with current repo as implementation target and old `NeoUO-Dev` as behavior evidence only.

---

## Executive Summary

The current `NeoUO-FullIntegration-aigm-umg` repo already has a good **speech-routing / command-boundary / read-only tracking / healing-shell** foundation, but it does **not** yet have a safe live execution spine for monster pursuit/combat under UMG governance.

### Current reality
- **Speech routing exists** and is fairly mature.
- **Capability gating exists** but still treats attack/heal/cure/travel execution as deferred/future-executor lanes.
- **Movement router exists** but is intentionally **no-op** for live movement.
- **Tracking service exists** but is **scene-scan/read-only only**.
- **Healing service exists** and uses native `BandageContext` for bandaging, but cure-potion execution is still placeholder-level and `AIGMCompanionSkillExecutor` still contains unsafe direct mutation for non-self healing/magery stubs.
- **Companion shell classes currently advertise `CanUseAIGMSkills = false` and `ExecutionModeKey = "shell-disabled"`**, which is an explicit signal that live execution is not yet enabled.
- **Current GM commands are shell/status-oriented**, not scenario/harness-oriented.

### Key architectural conclusion
Phase58A should **not** be implemented by reviving old freeform command execution wholesale. It should instead add a **new bounded execution lane**:

`intent -> policy/capability gate -> target validator -> action planner -> movement/combat/support executor -> runtime state -> trace/proof log -> optional dialogue summary`

This should use **ServUO-native mechanics as authority**:
- `BaseDoor.TryAutoOpenDoor(Mobile, bool)` for door opening.
- `BandageContext.BeginHeal(...)` for bandaging.
- existing `BaseCreature`/`BaseHire` order surfaces (`Combatant`, `ControlOrder`, `ControlTarget`, `Warmode`) for combat intent.
- `PathFollower` / movement stepping patterns proven in old `NeoUO-Dev`, but **ported conservatively**, not copied blindly.

---

## 1) Current Repo Surface Inventory

## 1.1 Companion speech routing / command intake

### Primary files
- `Scripts/Mobiles/NPCs/AIGMCompanionDakeyras.cs`
- `Scripts/Mobiles/NPCs/AIGMCompanionDanyal.cs`
- `Scripts/Mobiles/NPCs/AIGMCompanionDardalion.cs`
- `Scripts/Custom/AIGM/AIGMCompanionCommandBoundary.cs`
- `Scripts/Custom/AIGM/AIGMCompanionIntentParser.cs`
- `Scripts/Custom/AIGM/AIGMCompanionSpeechBus.cs`
- `Scripts/Custom/AIGM/AIGMCompanionTurnCoordinator.cs`
- `Scripts/Custom/AIGM/AIGMCompanionSpeechQueue.cs`
- `Scripts/Custom/AIGM/AIGMCompanionDialogueBus.cs`
- `Scripts/Custom/AIGM/AIGMCompanionReadOnlyAwareness.cs`

### What is present
Each companion shell overrides `OnSpeech(...)` and does the following:
- validates range / alive speaker
- checks explicit group tracking command handling
- classifies speech via `AIGMCompanionCommandBoundary.Classify(...)`
- resolves addressed companions via `AIGMCompanionCommandBoundary.GetAddressedCompanionIds(...)`
- uses `AIGMCompanionTurnCoordinator.ShouldCompanionTakeVisibleTurn(...)` to pick one visible responder
- parses intent with `AIGMCompanionIntentParser.TryParse(...)`
- uses queue/bus mechanisms to coordinate visible reply vs linked-companion context

### Important implementation observation
The speech lane is already structured enough to support UMG governance. Phase58A should **not** replace this. It should hang the new execution spine **behind** this route layer.

---

## 1.2 Capability gating

### Primary files
- `Scripts/Custom/AIGM/AIGMCompanionCapabilityGate.cs`
- `Scripts/Custom/AIGM/AIGMCompanionCapabilityKind.cs`
- `Scripts/Custom/AIGM/AIGMCompanionCapabilityDecision.cs`
- `Scripts/Custom/AIGM/AIGMCompanionCapabilityRequest.cs`

### What is present
`AIGMCompanionCapabilityGate.Decide(...)` currently:
- allows live/local shell basics for `Follow`, `Stay`, `Guard`
- allows read-only `ScanReadOnly`, `ReportThreatsReadOnly`, `ShareAwarenessReadOnly`, `ReportTrackingStatus`, `TravelReadOnly`
- allows read-only tracking cycle categories
- marks most execution lanes as deferred / future-executor-required

### Important gap
The gate vocabulary is still too broad and not yet shaped around the specific bounded Phase58A behavior lane. It has generic `Attack`, `Heal`, `Bandage`, `Cure`, etc., but lacks explicit policy shapes like:
- `MonsterPursuitExecute`
- `MonsterAttackExecute`
- `SelfBandageExecute`
- `SelfCurePotionExecute`
- `DoorAssistDuringPursuit`
- `TargetReacquireExecute`

Phase58A should **add capability granularity**, not just flip generic `Attack = allowed`.

---

## 1.3 Tracking / read-only awareness

### Primary files
- `Scripts/Custom/AIGM/AIGMCompanionTrackingService.cs`
- `Scripts/Custom/AIGM/AIGMCompanionTrackingState.cs`
- `Scripts/Custom/AIGM/AIGMSceneScanner.cs`
- `Scripts/Custom/AIGM/AIGMSceneContext.cs`
- `Scripts/Custom/AIGM/AIGMCompanionReadOnlyAwareness.cs`
- `Scripts/Custom/AIGM/AIGMCompanionPerceptionBuffer.cs`

### What is present
`AIGMCompanionTrackingService` currently:
- starts/stops a tracking watch
- stores last scan / last tile / last target descriptive strings
- builds read-only reports using `AIGMSceneScanner.Capture(...)`
- classifies nearby entities heuristically via name/type matching
- reports nearest/summary results

`AIGMSceneScanner` is a simple range snapshotter over nearby mobiles/items.

### Important gaps
This is **not** an executor-grade tracking stack yet:
- no stable executable target object
- no legality validation for attack eligibility
- no lock/reacquire policy
- no pursuit state machine
- no re-scan cadence for combat continuation
- no hard exclusion for players/humans/tames at execution boundary

The current tracking service is useful as **sensor input**, but not sufficient as the Phase58A action spine.

---

## 1.4 Healing / support

### Primary files
- `Scripts/Custom/AIGM/AIGMCompanionHealingService.cs`
- `Scripts/Custom/AIGM/AIGMCompanionSkillExecutor.cs`
- companion shell files implementing `IAIGMCompanionActor.NextSupportActionUtc`

### What is present
`AIGMCompanionHealingService` is the safer of the two current healing surfaces:
- resolves direct/group healing commands
- validates allowed targets
- checks bandage availability
- uses **native ServUO** `BandageContext.BeginHeal(...)`
- sets support cooldown via `NextSupportActionUtc`

### Important gaps / concerns
`AIGMCompanionSkillExecutor` contains legacy-style placeholder behavior that is not Phase58A-safe:
- non-self bandaging path directly mutates `target.Hits`
- magery heal/cure routines directly mutate target state / act as placeholders
- cure potion path logs success without proving actual inventory/use semantics

For Phase58A, **`AIGMCompanionHealingService` should become the canonical support lane**, and unsafe/direct mutation paths in `AIGMCompanionSkillExecutor` should either be bypassed or retired from the companion execution spine.

---

## 1.5 Movement / runtime movement state

### Primary files
- `Scripts/Custom/AIGM/Movement/UMGMovementRouter.cs`
- `Scripts/Custom/AIGM/Movement/UMGMovementState.cs`
- `Scripts/Custom/AIGM/Movement/UMGMovementNoOpExecutor.cs`
- `Scripts/Custom/AIGM/Navigation/UMGServUONavigationAdapter.cs`
- `Scripts/Custom/AIGM/Navigation/UMGNavigationSnapshotService.cs`

### What is present
`UMGMovementRouter` is explicitly structured as a gateable movement authority. It can:
- accept intents
- update bounded movement state
- deny live movement while preserving state
- track active/suspended intent, destination, target serial/name, reason, tracking mode, and last movement decision

### Important gap
It is still intentionally **no-op for live execution**. This is the central missing piece for Phase58A.

### Good news
This router is the correct place to preserve **UMG governance**. Phase58A should not bypass it. Instead, add a **Phase58A-specific live executor** that remains bounded to monster pursuit.

---

## 1.6 Combat surfaces

### Current AIGM surfaces
- `Scripts/Custom/AIGM/AIGMCompanionSkillExecutor.cs` (support only; not good authoritative combat spine)
- companion shells via `BaseHire` / `BaseCreature`

### Core ServUO combat surfaces
- `Scripts/Mobiles/AI/BaseAI.cs`
- `Scripts/Mobiles/NPCs/BaseHire.cs`

Relevant pet/hireling order surfaces visible in current codebase:
- `ControlOrder`
- `ControlTarget`
- `Combatant`
- `Warmode`
- owner-controlled follow/attack order patterns in `BaseAI`

### Important gap
There is no current `NeoUO-FullIntegration`-side **bounded combat execution coordinator** for:
- legal target selection
- attack order issuance
- target loss / kill completion
- reacquire loop
- movement/combat pulse management

---

## 1.7 Door/pathing surfaces

### Core ServUO door surface
- `Scripts/Items/Functional/BaseDoor.cs`

Key method already present and usable:
- `BaseDoor.TryAutoOpenDoor(Mobile m, bool sendMessage)`

This is important because it gives Phase58A a **native, conservative door-open path** instead of requiring custom world mutation.

### Pathing evidence in current full-integration repo
No equivalent live path/pursuit controller currently exists in `Scripts/Custom/AIGM` for companion execution.

### Navigation read-only helper surfaces
- `UMGServUONavigationAdapter.ProbePoint(...)`
- `UMGNavigationSnapshotService.CreateSnapshot(...)`

These are useful for diagnostics/proofing, but not sufficient for live pursuit.

---

## 1.8 Runtime state / proof state

### Primary files
- `Scripts/Custom/AIGM/AIGMExecutionContext.cs`
- `Scripts/Custom/AIGM/AIGMActionHistory.cs`
- `Scripts/Custom/AIGM/AIGMExecutionLog.cs`
- `Scripts/Custom/AIGM/AIGMSessionState.cs`

### Current state quality
- `AIGMExecutionContext` is minimal (`Mode`, `LastActionDescription`, `LastActionResult`, `StepCount`)
- `AIGMActionHistory` can record recent category/description/result/target summary
- `AIGMExecutionLog.Write(...)` appends to `Logs/AIGMExecution.log`

### Important gap
There is not yet a dedicated **companion execution state model** for Phase58A. Missing pieces include:
- current executable target
- last legal target reject reason
- current pursuit phase
- last successful path advance
- last door assist
- last bandage attempt / result
- last cure potion attempt / result
- kill count / scan count / reacquire count
- stop reason when lane terminates

---

## 1.9 Existing GM/admin command patterns

### Current full-integration command surface
- `Scripts/Commands/AIGMCompanionCommand.cs`

Current registered commands:
- `AIGMCompanion`
- `AIGMCompanionStatus`
- `AIGMCompanionRoute`
- `AIGMCompanionSpawnCheck`
- `Dakeyras`
- `Danyal`
- `Dardalion`

Current emphasis:
- shell presence
- route classification
- spawnability checks
- direct companion spawning

### Old-Dev command evidence
- `NeoUO-Dev/Scripts/Commands/AIGMCompanionCommand.cs`
- `NeoUO-Dev/Scripts/Commands/AIGMProofCommand.cs`
- `NeoUO-Dev/Scripts/Commands/AIGMDiscoveryCommand.cs`

Useful old patterns:
- `CompDebug <state|intent|combat|move|clear>` target-based runtime inspection
- proof/helper command pattern for GM tooling (`AIGMProofBag`)

### Conclusion
Use current full-integration command style as target, but borrow old-Dev’s **targeted debug command ergonomics**.

---

## 2) Old-Dev Parity / Evidence Surfaces

## 2.1 Strong evidence sources

The following old-Dev files are the most useful evidence for intended behavior:
- `NeoUO-Dev/Scripts/Custom/AIGM/AIGMCompanionActionExecutor.cs`
- `NeoUO-Dev/Scripts/Custom/AIGM/AIGMCompanionTravelController.cs`
- `NeoUO-Dev/Scripts/Custom/AIGM/AIGMCompanionAutoPathNavigator.cs`
- `NeoUO-Dev/Scripts/Commands/AIGMCompanionCommand.cs`
- `NeoUO-Dev/Scripts/Commands/AIGMProofCommand.cs`

## 2.2 What old-Dev proves

Old-Dev contains evidence for:
- tracked pursuit start/clear lifecycle
- `ControlOrder` / `Combatant` attack issuance patterns
- ranged/melee role handling
- `PathFollower`-backed pursuit/travel stepping
- movement decision recording
- targeted GM debug output

## 2.3 What should NOT be imported blindly

Do **not** blindly port the following patterns as authoritative:
- any direct HP mutation support paths
- generic freeform attack command handling
- broad travel systems unrelated to monster-only lane
- large old state bags without clear Phase58A necessity
- any executor logic that bypasses capability validation and legality checks

## 2.4 Best old-Dev reuse posture

Treat old-Dev as:
- **behavior evidence**
- **naming inspiration**
- **pulse/state-machine reference**
- **safe minimal extraction source** for pathing/pursuit patterns

Not as a bulk-merge donor.

---

## 3) Concrete Phase58A Architecture Plan

## 3.1 Phase58A policy envelope

Phase58A allowed behaviors:
- track monsters only
- select closest valid monster
- move toward monster
- auto-open door during movement if possible
- attack valid monster only
- re-acquire after kill/lost target
- stop when no monsters remain
- self-bandage when below max hits
- self-cure via cure potion only if safe native inventory/use path exists

Phase58A forbidden behaviors:
- attack players
- attack human/town NPCs
- attack controlled/tamed allies
- broad coordinate travel outside pursuit lane
- spell healing
- direct world/HP mutation from UMG/chat side
- unsafe executor imports

---

## 3.2 Proposed new types/services

## Core orchestration

### Add
- `Scripts/Custom/AIGM/AIGMCompanionExecutionSpine.cs`
- `Scripts/Custom/AIGM/AIGMCompanionExecutionPulse.cs`
- `Scripts/Custom/AIGM/AIGMCompanionExecutionState.cs`
- `Scripts/Custom/AIGM/AIGMCompanionExecutionPhase.cs`

### Responsibility
This becomes the bounded runtime coordinator for Phase58A.

Suggested phases:
- `Idle`
- `ScanForMonster`
- `ValidateCandidate`
- `PursueTarget`
- `OpenDoorAssist`
- `AttackTarget`
- `SelfSupport`
- `Reacquire`
- `Completed`
- `Blocked`
- `Aborted`

---

## Target selection / legality

### Add
- `Scripts/Custom/AIGM/AIGMCompanionTargetPolicy.cs`
- `Scripts/Custom/AIGM/AIGMCompanionTargetCandidate.cs`
- `Scripts/Custom/AIGM/AIGMCompanionTargetValidator.cs`

### Responsibility
Convert scene/tracking sightings into executable candidates and reject anything outside policy.

Hard validation rules should include:
- same map
- alive
- not deleted
- not self
- not owner
- not player
- not `BaseHire` ally of same owner
- not controlled/tamed ally
- not human/town NPC class/category
- must pass `CanBeHarmful(target, false)`
- must be monster-classified by policy, not just name loosely

### Recommendation
Use a **deny-by-default validator** with explicit allow reasons and reject reasons written to trace.

---

## Movement / pursuit execution

### Add
- `Scripts/Custom/AIGM/Movement/UMGMovementPhase58AExecutor.cs`
- `Scripts/Custom/AIGM/Movement/AIGMCompanionPursuitController.cs`
- `Scripts/Custom/AIGM/Movement/AIGMCompanionDoorAssist.cs`

### Modify
- `Scripts/Custom/AIGM/Movement/UMGMovementRouter.cs`
- `Scripts/Custom/AIGM/Movement/UMGMovementState.cs`

### Responsibility
- keep `UMGMovementRouter` as authority
- add a live executor that only accepts `PursueTrackedTarget` and bounded stop/hold/self-support transitions for Phase58A
- call `BaseDoor.TryAutoOpenDoor(companion, false)` before declaring hard movement blockage
- preserve tracking mode / target serial / reason in `UMGMovementState`

### Safe implementation posture
Do **not** enable generic travel execution. Only allow live execution when:
- intent kind == `PursueTrackedTarget`
- target already passed `AIGMCompanionTargetValidator`
- pursuit source reason is Phase58A monster-hunt lane

### Old-Dev extraction candidate
Port only the smallest safe subset of behavior from:
- `AIGMCompanionTravelController`
- `AIGMCompanionAutoPathNavigator`

Prefer wrapping ServUO `PathFollower` instead of copying full old travel state machinery.

---

## Combat execution

### Add
- `Scripts/Custom/AIGM/AIGMCompanionCombatController.cs`
- `Scripts/Custom/AIGM/AIGMCompanionCombatPolicy.cs`

### Responsibility
Own the only legal way to issue Phase58A attack intent.

Suggested behavior:
- set `Warmode = true`
- set `ControlTarget = target`
- set `Combatant = target`
- set `ControlOrder = OrderType.Attack`
- if out of range, let movement executor remain in pursue mode
- if target dead/invalid, clear combat and move to reacquire phase

### Critical rule
No speech-side or UMG-side code should set `Combatant` directly except through this controller.

---

## Support / self-heal lane

### Add
- `Scripts/Custom/AIGM/AIGMCompanionSelfSupportPolicy.cs`

### Modify
- `Scripts/Custom/AIGM/AIGMCompanionHealingService.cs`
- `Scripts/Custom/AIGM/AIGMCompanionSkillExecutor.cs`

### Responsibility
Phase58A support should be strictly:
- self-bandage when wounded and legal
- self-cure potion when poisoned and safe native use path exists

### Strong recommendation
Route self-bandaging through `AIGMCompanionHealingService` / `BandageContext.BeginHeal(...)`, not the direct-mutation branches in `AIGMCompanionSkillExecutor`.

### Cure potion rule
Only enable if repo has a reliable native item/use path that:
- locates actual cure potion in pack
- uses real ServUO consume/effect path
- confirms cooldown / safe use

If not, Phase58A should log:
- `self_cure_potion_deferred_no_safe_native_path`

That is preferable to shipping fake potion behavior.

---

## Trace / proof / dialogue summary

### Add
- `Scripts/Custom/AIGM/AIGMCompanionActionTrace.cs`
- `Scripts/Custom/AIGM/AIGMCompanionActionTraceEntry.cs`
- `Scripts/Custom/AIGM/AIGMCompanionExecutionSummaryBuilder.cs`

### Modify
- `Scripts/Custom/AIGM/AIGMActionHistory.cs`
- `Scripts/Custom/AIGM/AIGMExecutionLog.cs`
- `Scripts/Custom/AIGM/AIGMExecutionContext.cs`

### Responsibility
Capture the full execution spine proof chain:
- requested intent
- gate decision
- candidate scan count
- chosen target / reject reasons
- movement steps / blockage / door-open attempts
- attack order issued
- self-bandage attempt/result
- cure attempt/result
- target lost/killed
- reacquire result
- terminal stop reason

### Recommended log line format
Use structured key-value style in `AIGMExecution.log`, e.g.:
- `PHASE58A_SCAN ...`
- `PHASE58A_TARGET_ACCEPT ...`
- `PHASE58A_TARGET_REJECT ...`
- `PHASE58A_MOVE_STEP ...`
- `PHASE58A_DOOR_ASSIST ...`
- `PHASE58A_ATTACK_ORDER ...`
- `PHASE58A_SELF_BANDAGE_START ...`
- `PHASE58A_SELF_BANDAGE_SKIP ...`
- `PHASE58A_REACQUIRE ...`
- `PHASE58A_STOP ...`

Optional visible summary should be brief, e.g.:
- `Dakeyras: Monster hunt complete. Two monsters dispatched. No further valid monsters remain.`

---

## 3.3 Proposed modifications to existing files

## `AIGMCompanionCapabilityKind.cs`
Add more explicit bounded execution kinds, e.g.:
- `MonsterTrackingExecute`
- `MonsterPursuitExecute`
- `MonsterAttackExecute`
- `DoorAssistExecute`
- `SelfBandageExecute`
- `SelfCurePotionExecute`
- `ReacquireMonsterExecute`

## `AIGMCompanionCapabilityGate.cs`
Change from generic deferred attack/heal/cure gating to explicit Phase58A allowlist.

Example policy direction:
- allow read-only surfaces as before
- allow only the new bounded monster/self-support capabilities
- continue denying generic travel, generic attack, player-facing healing magic, etc.

## Companion shell files (`AIGMCompanionDakeyras.cs`, `...Danyal.cs`, `...Dardalion.cs`)
Potential changes:
- `CanUseAIGMSkills` should become true only when Phase58A lane is active/safe
- `ExecutionModeKey` should reflect actual phase mode (e.g. `phase58a-bounded-execution`)
- speech handlers should route executable monster-hunt intents into the new execution spine instead of treating them as deferred

## `AIGMCompanionTrackingService.cs`
Keep read-only reporting, but add helper methods that produce executable candidate data for the target policy layer.

## `AIGMCompanionHealingService.cs`
Promote to canonical support executor for self-bandage.

## `AIGMCompanionSkillExecutor.cs`
Reduce role to compatibility shim or retire unsafe branches from companion runtime path.

## `UMGMovementRouter.cs`
Add Phase58A live executor wiring for bounded pursuit only.

## `UMGMovementState.cs`
Add fields like:
- `LastTargetValidationReason`
- `LastDoorAssistUtc`
- `LastDoorAssistResult`
- `LastPathAdvanceUtc`
- `LastBlockedReason`
- `ExecutionLane`

## `AIGMExecutionContext.cs`
Expand or wrap with companion-specific execution context data instead of relying on the current 4-property shell.

---

## 3.4 Intent mapping for Phase58A

Do not expose broad new natural-language surfaces yet. Keep Phase58A entry conservative.

Recommended initial executable entry points:
- existing tracking/monster-hunt style command mapped to `StartTrackingCycle` + Phase58A monster hunt mode
- explicit GM scenario commands
- optional explicit owner phrase like `track monsters` only if capability gate and route decision say executable-now

Avoid enabling arbitrary `attack <thing>` natural language in Phase58A.

---

## 4) GM Validation Harness Plan

## 4.1 New command file

### Add
- `Scripts/Commands/AIGMScenarioCommand.cs`
- `Scripts/Commands/AIGMDumpCommand.cs`

This is preferable to overloading `AIGMCompanionCommand.cs` further.

---

## 4.2 Proposed GM scenario commands

## `[AIGMScenario MonsterHunt`
Purpose:
- spawn/select companion
- prepare bounded set of nearby monsters
- start Phase58A hunt loop
- prove reacquire/stop behavior

Suggested modes:
- `AIGMScenario MonsterHunt start`
- `AIGMScenario MonsterHunt reset`
- `AIGMScenario MonsterHunt status`

Proof expectations:
- chose nearest valid monster
- never attacked invalid class
- re-acquired after kill/loss
- stopped when none remained

## `[AIGMScenario Healing`
Purpose:
- validate self-bandage under damage
- optionally validate poison + cure-potion path only if safe native item path exists

Proof expectations:
- `BandageContext` started
- cooldown honored
- no spell-heal path used
- cure potion either safely used or explicitly deferred with reason

## `[AIGMScenario Door`
Purpose:
- validate monster pursuit through an intervening closed door

Proof expectations:
- door candidate detected
- `BaseDoor.TryAutoOpenDoor(...)` attempted
- movement resumed or blocked with reason

## `[AIGMScenario Tracking`
Purpose:
- validate sensor selection and legality filtering without necessarily running full combat

Proof expectations:
- nearby entities listed
- accepted vs rejected candidates logged
- nearest valid monster chosen deterministically

---

## 4.3 Proposed dump/debug commands

## `[AIGMDump CompanionState`
Target a companion and dump:
- companion serial/name
- owner serial/name
- execution mode key
- `CanUseAIGMSkills`
- current execution phase
- active target serial/name
- active movement intent
- last target validation reason
- last movement decision
- last support action UTC
- current hits / poisoned state
- stop reason
- counters: scans, reacquires, kills, door assists

## `[AIGMDump CapabilityState`
Dump effective capability decisions for Phase58A-relevant capabilities:
- monster tracking execute
- monster pursuit execute
- monster attack execute
- self-bandage execute
- self-cure-potion execute
- door assist execute

This should include:
- allowed/denied
- deferred flag
- reason
- visible response if any

---

## 4.4 Proof output format

Use both:
1. **GM chat summary output**
2. **persistent execution log lines** in `Logs/AIGMExecution.log`

Recommended summary example:
- `Phase58A MonsterHunt: Dakeyras selected orc[0x...] at 5 tiles, opened 1 door, dispatched 2 monsters, stopped: no_valid_monsters_remaining.`

Recommended trace tags:
- `PHASE58A_SCENARIO_START`
- `PHASE58A_SCENARIO_STEP`
- `PHASE58A_SCENARIO_ASSERT`
- `PHASE58A_SCENARIO_END`

---

## 5) Risks / Blockers

## 5.1 Highest risk: unsafe legacy execution paths
`AIGMCompanionSkillExecutor` still includes direct state mutation patterns. If those remain in the execution lane, they will undermine the ServUO-authoritative model.

**Mitigation:** bypass them for Phase58A except where they can be proven native-safe.

## 5.2 Target classification false positives
Current read-only monster classification in `AIGMCompanionTrackingService` is heuristic/string-based. That is not strong enough alone for attack legality.

**Mitigation:** add explicit validator rules on actual `Mobile` instances, not just scene summaries.

## 5.3 Door/pathing edge cases
Door assist is safe if limited to `BaseDoor.TryAutoOpenDoor(...)`, but pathing around blocked geometry can still fail/stall.

**Mitigation:** bounded pulse budget, blockage counters, deterministic stop reasons.

## 5.4 Cure potion path may not be safely available
Current repo review did not establish a safe native cure-potion execution lane from companion inventory.

**Mitigation:** ship Phase58A with self-bandage first; gate cure potion behind proof of native inventory/use path.

## 5.5 Speech route scope creep
If broad natural language `attack/kill/fight` is made executable too early, policy bypass risk increases.

**Mitigation:** use explicit bounded hunt-mode entry and GM scenarios first.

## 5.6 Current shell flags still indicate disabled execution
All three current companion shells expose:
- `CanUseAIGMSkills = false`
- `ExecutionModeKey = "shell-disabled"`

**Mitigation:** change only when the bounded spine is actually in place.

---

## 6) Recommended Sequencing

## Phase58A-1: Policy and state spine
- add new capability kinds
- add `AIGMCompanionExecutionState`
- add target policy / validator
- add trace entry model
- add dump commands

## Phase58A-2: Sensor to candidate bridge
- extend tracking service to produce executable candidates
- wire legality rejection reasons
- prove nearest-valid-monster selection via GM tracking scenario

## Phase58A-3: Bounded movement execution
- add Phase58A movement executor under `UMGMovementRouter`
- implement pursuit-only movement
- add door assist via `BaseDoor.TryAutoOpenDoor(...)`
- prove with GM door scenario

## Phase58A-4: Bounded combat execution
- add combat controller
- issue legal monster-only attack orders
- add target loss/kill detection
- implement reacquire loop
- prove with GM monster-hunt scenario

## Phase58A-5: Self-support lane
- route self-bandage through `AIGMCompanionHealingService`
- add poison/cure only if native-safe inventory use is confirmed
- prove with GM healing scenario

## Phase58A-6: Dialogue/proof polish
- build concise completion summaries
- improve `AIGMExecution.log` trace quality
- keep owner-visible narration short and factual

---

## 7) Key Target Files for Next Coding Pass

### Existing files to modify
- `Scripts/Custom/AIGM/AIGMCompanionCapabilityKind.cs`
- `Scripts/Custom/AIGM/AIGMCompanionCapabilityGate.cs`
- `Scripts/Custom/AIGM/AIGMCompanionTrackingService.cs`
- `Scripts/Custom/AIGM/AIGMCompanionHealingService.cs`
- `Scripts/Custom/AIGM/AIGMCompanionSkillExecutor.cs`
- `Scripts/Custom/AIGM/AIGMExecutionContext.cs`
- `Scripts/Custom/AIGM/AIGMActionHistory.cs`
- `Scripts/Custom/AIGM/AIGMExecutionLog.cs`
- `Scripts/Custom/AIGM/Movement/UMGMovementRouter.cs`
- `Scripts/Custom/AIGM/Movement/UMGMovementState.cs`
- `Scripts/Mobiles/NPCs/AIGMCompanionDakeyras.cs`
- `Scripts/Mobiles/NPCs/AIGMCompanionDanyal.cs`
- `Scripts/Mobiles/NPCs/AIGMCompanionDardalion.cs`

### New files to add
- `Scripts/Custom/AIGM/AIGMCompanionExecutionSpine.cs`
- `Scripts/Custom/AIGM/AIGMCompanionExecutionState.cs`
- `Scripts/Custom/AIGM/AIGMCompanionExecutionPhase.cs`
- `Scripts/Custom/AIGM/AIGMCompanionTargetPolicy.cs`
- `Scripts/Custom/AIGM/AIGMCompanionTargetValidator.cs`
- `Scripts/Custom/AIGM/AIGMCompanionTargetCandidate.cs`
- `Scripts/Custom/AIGM/AIGMCompanionCombatController.cs`
- `Scripts/Custom/AIGM/AIGMCompanionSelfSupportPolicy.cs`
- `Scripts/Custom/AIGM/AIGMCompanionActionTrace.cs`
- `Scripts/Custom/AIGM/AIGMCompanionExecutionSummaryBuilder.cs`
- `Scripts/Custom/AIGM/Movement/UMGMovementPhase58AExecutor.cs`
- `Scripts/Custom/AIGM/Movement/AIGMCompanionPursuitController.cs`
- `Scripts/Custom/AIGM/Movement/AIGMCompanionDoorAssist.cs`
- `Scripts/Commands/AIGMScenarioCommand.cs`
- `Scripts/Commands/AIGMDumpCommand.cs`

---

## 8) Immediate Next Coding Steps

1. **Create the Phase58A state and capability vocabulary first.**
2. **Add target validator before enabling any attack execution.**
3. **Wire a pursuit-only live movement executor under `UMGMovementRouter`.**
4. **Use `BaseDoor.TryAutoOpenDoor(...)` as the only initial door interaction path.**
5. **Route self-bandage through `AIGMCompanionHealingService` / `BandageContext`, not direct mutation.**
6. **Leave cure potion disabled unless a safe native path is confirmed in code.**
7. **Add GM scenario/dump commands before field-testing**, so the user is no longer the manual harness.

---

## Bottom Line

The repo is ready for Phase58A planning but not yet for safe live execution. The strongest path is:
- preserve the current speech/UMG route layer,
- add a new bounded execution spine,
- use ServUO-native mechanics for doors/bandages/combat authority,
- borrow old-Dev pursuit/pathing ideas conservatively,
- and ship with a GM scenario harness from day one.

That gives the project executable monster autonomy without regressing into unsafe freeform companion execution.
