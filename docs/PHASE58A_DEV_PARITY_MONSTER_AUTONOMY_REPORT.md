# Phase58A Dev-Parity Monster Autonomy Report

## 1. Branch and HEAD before patch
- Branch: `neo/phase56t-clean-speech-recovery`
- HEAD before patch: `0b8498ceba3b89f743f705f0329625b292a78d76`
- Working tree at audit time was dirty with pre-existing Phase58A/AIGM changes and many unrelated untracked report artifacts.

## 2. Old Dev evidence inspected
Primary planning/evidence sources available in the target repo context and associated notes:
- `NEOUO_FULL_INTEGRATION_PHASE58A_EXECUTION_SPINE_PLAN_REPORT.md`
- `phase58a_runbook.txt`
- `docs/runtime/PHASE58A_MONSTER_HUNT_RUNTIME_PROOF_20260613_0629.md`

Old-Dev behavior expectations captured in those artifacts and aligned against current implementation:
- nearest-valid-monster target acquisition
- monster-only pursuit lane
- bounded re-acquire loop
- conservative door assist during movement
- self-bandage during hunt/combat lane
- self-cure via native potion mechanics when available

## 3. Files changed
Tracked modified files committed for this pass:
- `Scripts/Custom/AIGM/AIGMCompanionCapabilityGate.cs`
- `Scripts/Custom/AIGM/AIGMCompanionCapabilityKind.cs`
- `Scripts/Custom/AIGM/AIGMCompanionIntent.cs`
- `Scripts/Custom/AIGM/AIGMCompanionIntentParser.cs`
- `Scripts/Custom/AIGM/AIGMCompanionTrackingMode.cs`
- `Scripts/Custom/AIGM/AIGMCompanionTrackingService.cs`
- `Scripts/Mobiles/NPCs/AIGMCompanionDakeyras.cs`
- `Scripts/Mobiles/NPCs/AIGMCompanionDanyal.cs`
- `Scripts/Mobiles/NPCs/AIGMCompanionDardalion.cs`

Untracked implementation file included in this pass:
- `Scripts/Commands/AIGMTrackingCommand.cs`

Pre-existing in-tree execution files validated as part of this pass and used by the routed command lane:
- `Scripts/Custom/AIGM/AIGMCompanionExecutionSpine.cs`
- `Scripts/Custom/AIGM/AIGMCompanionExecutionState.cs`
- `Scripts/Custom/AIGM/AIGMCompanionTargetValidator.cs`
- `Scripts/Custom/AIGM/AIGMCompanionCombatController.cs`
- `Scripts/Custom/AIGM/AIGMCompanionDoorService.cs`
- `Scripts/Custom/AIGM/AIGMCompanionSelfSustainService.cs`

This report file added:
- `docs/PHASE58A_DEV_PARITY_MONSTER_AUTONOMY_REPORT.md`

## 4. Commands implemented
Owner/natural-language routing additions verified in companion speech parsing:
- `track monsters`
- `start tracking monsters`
- `hunt monsters`
- `attack monsters`
- `clear monsters`
- `start hunting`
- `hunt closest monster`
- `stop hunting`
- `stop tracking`
- category tracking routes for animals / monsters / NPCs / human NPCs / players / all

GM helper command file added:
- `tm` → track monsters
- `ta` → track animals
- `tn` → track NPCs
- `th` → track human NPCs
- `tp` → track players
- `tall` → track all
- `hm` → start monster hunt
- `ha` → animal hunt placeholder (explicitly denied)
- `stoptrack` → stop tracking and hunt
- `ts` → tracking status
- `td` → tracking state dump

## 5. Valid monster target rules
Validator surface used:
- `Scripts/Custom/AIGM/AIGMCompanionTargetValidator.cs`

Rejects:
- null / deleted / dead targets
- different-map targets
- self
- owner
- companions (`IAIGMCompanionActor`)
- players
- non-creatures
- blessed / invulnerable creatures
- controlled or summoned creatures
- owner-controlled allies
- vendors / escortables / civilians
- human bodies
- animal bodies
- same-team creatures
- out-of-range targets

Accepts only:
- live, same-map, non-human, non-animal, non-player `BaseCreature` monster-body candidates passing bounded validation

## 6. Movement/pathing method used
Movement is bounded single-step pursuit inside the execution spine:
- `AIGMCompanionExecutionSpine.TickMonsterHunt(...)`
- computes direction to the validated target
- turns if needed
- attempts `companion.Move(direction)`
- records before/after location, direction, and distance change in execution state
- returns blocked/turned-only/moved-one-step outcomes explicitly

This is conservative and does not restore freeform travel, coordinate routing, or town travel.

## 7. Door opening method used
Door assist surface:
- `Scripts/Custom/AIGM/AIGMCompanionDoorService.cs`

Method:
- `BaseDoor.TryAutoOpenDoor(companion, false)`

Use posture:
- invoked only during bounded pursuit before declaring closure failure
- no lock bypass logic introduced
- no door-state hacking introduced

## 8. Attack method used
Combat surface:
- `Scripts/Custom/AIGM/AIGMCompanionCombatController.cs`

Method:
- validate target first with `AIGMCompanionTargetValidator.ValidateMonsterTarget(...)`
- then set:
  - `companion.Combatant = target`
  - `companion.ControlTarget = target`
  - `companion.ControlOrder = OrderType.Attack`
  - `companion.Warmode = true`

Stop method clears combat and returns the companion to follow order.

## 9. Self-bandage method used
Self-bandage surface:
- `Scripts/Custom/AIGM/AIGMCompanionSelfSustainService.cs`
- delegates to `Scripts/Custom/AIGM/AIGMCompanionHealingService.cs`

Native method used:
- `BandageContext.BeginHeal(...)`

No direct HP mutation was introduced in the Phase58A lane.

## 10. Cure potion method used or blocker
Self-cure surface:
- `Scripts/Custom/AIGM/AIGMCompanionSelfSustainService.cs`

Method used:
- find `BaseCurePotion` in backpack
- call native `potion.Drink(companion)`

Validation performed for this pass:
- reviewed `Scripts/Items/Consumables/BaseCurePotion.cs`
- reviewed `Scripts/Items/Consumables/BasePotion.cs`
- confirmed `Drink(Mobile from)` is the native consume/effect path, while `OnDoubleClick` is the player interaction wrapper

Result:
- cure potion use is enabled through native ServUO potion mechanics
- if no potion is present, response remains explicit (`cure_potion_unavailable`)

## 11. Skills/resources used
- Existing current-repo AIGM speech route and capability gate surfaces
- Existing current-repo execution spine / validator / combat / door / sustain surfaces
- Native ServUO `BandageContext`
- Native ServUO `BaseDoor.TryAutoOpenDoor(...)`
- Native ServUO `BaseCurePotion.Drink(...)`
- `dotnet build .\ServUO.sln -v:minimal`

## 12. UMG/capability gate integration
Capability additions/routing added in this pass:
- `TrackingAnimals`
- `TrackingMonsters`
- `TrackingNPCs`
- `TrackingHumanNPCs`
- `TrackingPlayers`
- `TrackingAll`
- `TrackingStop`
- `TrackingStatus`
- `HuntAnimals` (explicit deny gate)

Gate behavior:
- live tracking categories are allowed with reason `phase58b_tracking_live`
- monster hunt remains live through existing Phase58A gate path
- animal hunting remains intentionally closed with visible response

## 13. Safety boundaries
Maintained boundaries:
- monster-only attack lane
- no player pursuit
- no owner/companion targeting
- no vendors/human NPCs/animals in attack lane
- no broad coordinate travel restoration
- no freeform travel reactivation
- self-bandage uses native bandage mechanics
- cure uses native potion mechanics
- combat target assignment only after explicit validation

## 14. Forbidden-reference check
Command run:
- `Select-String -Path .\Scripts\Custom\AIGM\*.cs,.\Scripts\Mobiles\NPCs\AIGMCompanion*.cs -Pattern 'ActionExecutor|DirectActionPolicy|TravelController|AutoPathNavigator|MoveToWorld|Combatant\s*=|ControlTarget\s*=|TargetLocation|Attack\(|BandageContext|CurePotion|CurePoison|BaseDoor|Door|Open|OnDoubleClick|Potion|Poisoned|Hits\s*=|Hits\s+\+='`

Findings:
- old/pre-existing unsafe direct HP mutation still exists in `AIGMCompanionSkillExecutor.cs`
- Phase58A lane audited here does **not** route self-bandage through those direct mutation branches
- newly relied-on references are allowed and bounded:
  - `Combatant =` / `ControlTarget =` in `AIGMCompanionCombatController.cs` for validated monster engagement
  - `BaseDoor.TryAutoOpenDoor(...)` in `AIGMCompanionDoorService.cs`
  - `BandageContext.BeginHeal(...)` through healing service
  - `BaseCurePotion.Drink(...)` through self-sustain service
- no new direct `Hits =` or `Hits +=` writes were introduced by this pass

## 15. Build result
Build command:
- `dotnet build .\ServUO.sln -v:minimal`

Outcome:
- first attempt blocked by live `ServUO Server` file lock on `Ultima.dll`
- after server stop, build succeeded
- final result: `15 Warning(s), 0 Error(s)`

Warnings observed were pre-existing `CS0162 Unreachable code detected` warnings in unrelated AIGM/gump/counselor files.

## 16. Runtime proof checklist
Already available runtime evidence reviewed from prior artifact:
- `docs/runtime/PHASE58A_MONSTER_HUNT_RUNTIME_PROOF_20260613_0629.md`

Confirmed there:
- monster hunt scenario key recognized
- valid nearby monster selected (`a black bear`)
- hunt active
- pursuit phase entered
- movement trace present
- validator rejection evidence for companion candidate present

Still recommended to verify live after this pass:
- `hm` or owner speech `hunt monsters`
- pursue nearest valid monster
- engage when in range
- stop/reacquire behavior after loss/death
- `ts` / `td` status output
- door-open assist in a closed-door pursuit setup
- self-bandage while wounded during active hunt
- self-cure while poisoned and carrying cure potion

## 17. Recommended commit message
- `feat: restore bounded monster autonomy for AIGM companions`
