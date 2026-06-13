# Phase58A Execution Spine Implementation Report

## summary
Implementation pass 01 added a first bounded executable monster-hunt spine under current AIGM/UMG governance surfaces. This pass is intentionally conservative and partial: it establishes execution state, target validation, door support, self-sustain support, a minimal combat controller, companion shell wiring, and GM dump/scenario scaffolding.

## files added
- Scripts/Custom/AIGM/AIGMCompanionExecutionSpine.cs
- Scripts/Custom/AIGM/AIGMCompanionExecutionState.cs
- Scripts/Custom/AIGM/AIGMCompanionTargetValidator.cs
- Scripts/Custom/AIGM/AIGMCompanionCombatController.cs
- Scripts/Custom/AIGM/AIGMCompanionDoorService.cs
- Scripts/Custom/AIGM/AIGMCompanionSelfSustainService.cs
- Scripts/Custom/AIGM/Movement/UMGMovementPhase58AExecutor.cs
- Scripts/Commands/AIGMScenarioCommand.cs
- Scripts/Commands/AIGMDumpCommand.cs

## files modified
- Scripts/Custom/AIGM/AIGMCompanionCapabilityKind.cs
- Scripts/Custom/AIGM/AIGMCompanionCapabilityGate.cs
- Scripts/Custom/AIGM/AIGMCompanionIntent.cs
- Scripts/Custom/AIGM/AIGMCompanionIntentParser.cs
- Scripts/Custom/AIGM/AIGMCompanionHealingService.cs
- Scripts/Mobiles/NPCs/AIGMCompanionDakeyras.cs
- Scripts/Mobiles/NPCs/AIGMCompanionDanyal.cs
- Scripts/Mobiles/NPCs/AIGMCompanionDardalion.cs

## old Dev evidence inspected
Behavior direction was based on prior repo analysis targeting old Dev parity surfaces including action executor, travel controller, auto path navigator, tracking cycle/sensor, movement controller, and GM proof command patterns.

## native ServUO surfaces used
- BaseDoor.TryAutoOpenDoor(Mobile, bool)
- BaseHire / BaseCreature combat surfaces
- ControlOrder
- ControlTarget
- Combatant
- Warmode
- BandageContext.BeginHeal(...) via AIGMCompanionHealingService
- BaseCurePotion.Drink(...) when safely present in inventory

## capability kinds added/changed
Added:
- MonsterHunt
- MonsterHuntStop
- MonsterHuntStatus

## commands supported
Natural language / speech lane added:
- hunt monsters
- attack monsters
- clear monsters
- start hunting
- start monster tracking
- track monsters
- start tracking monsters
- stop hunting
- stop tracking monsters
- stop monster tracking
- monster status
- hunt status
- hunting status

GM commands added:
- [AIGMScenario MonsterHunt | Healing | Door | Tracking
- [AIGMDump CompanionState | CapabilityState | ActionTrace

## target validation rules
Current validator rejects:
- null/deleted/dead targets
- different map
- self / owner / companions
- players
- non-creatures
- invulnerable/blessed creatures
- controlled/summoned creatures
- owner-controlled allies
- vendors/civilians/base escorts
- human-bodied creatures
- out-of-range candidates
- same-team creatures

## movement limitations
This pass is pursuit-only in architecture intent, but current implementation is still minimal and not runtime-safe enough for full claim of production behavior. The initial executor and spine use direct position movement calls and should be refined in a follow-up patch toward a tighter bounded movement adapter under UMGMovementRouter.

## combat limitations
Combat controller uses native combat/order surfaces, but broader runtime orchestration, reacquire cadence, and periodic tick integration still need refinement.

## self-sustain behavior
- self-bandage path added via AIGMCompanionHealingService.TryBeginSelfBandage
- cure potion path added only when a BaseCurePotion is safely found in backpack
- no spell healing or direct HP mutation added

## GM scenario commands
Scenario command currently writes proof scaffolds to docs/runtime and records current state metadata. It does not yet spawn full controlled monsters/doors/healing scenes.

## compile result
Not yet verified in this pass.
Compiled/not runtime-verified.

## runtime proof path if generated
Scenario command writes paths under:
- docs/runtime/PHASE58A_<SCENARIO>_RUNTIME_PROOF_<timestamp>.md

## known blockers
- UMGMovementRouter and no-op executor were not fully rebound in this pass.
- Companion execution mode flags still need a deliberate enablement review.
- Current movement approach needs replacement/refinement to avoid unsafe or fake success semantics.
- AIGMCompanionSkillExecutor remains a risky surface and was intentionally not promoted as authoritative.
- Scenario command is scaffold-only, not a full automated encounter builder yet.
- Compile/build validation still required.

## next patch recommendation
Phase58A pass 02 should:
1. bind execution to a safe timer/tick cadence instead of one-shot speech-trigger logic
2. rework movement through UMGMovementRouter with a real bounded pursuit adapter
3. add richer action trace storage and dump formatting
4. promote monster-only target selection through tracking service integration
5. complete GM scenario encounter spawning and proof detail output
6. compile and fix all integration issues before runtime claims
