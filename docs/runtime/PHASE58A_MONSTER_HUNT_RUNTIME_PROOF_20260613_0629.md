# Phase58A Monster Hunt Runtime Proof

- BaselineCommit: 144928dfb
- Branch: neo/phase56t-clean-speech-recovery
- ProofPass: PHASE58A PASS 01R-B3 — Controlled Nearby-Monster Proof
- GeneratedUtc: 2026-06-12 21:29:00Z
- UpdatedUtc: 2026-06-12 22:23:30Z

## baseline
- Baseline commit for this B3 pass: `144928dfb`
- Baseline message: `fix: make Phase58A monster hunt scenario deterministic`

## preflight
- deterministic scenario-command fix already committed before this pass
- server restarted cleanly before controlled nearby-monster proof attempt
- live client used: `NeoMagnetar - ClassicUO [dev] - 1.1.0.301`

## scenario setup result
- A nearby valid monster was present in the live scene.
- Visible target in screenshots: `a black bear`
- Nearby companion party visible in scene; runtime result specifically names `Dakeyras` as the acting companion.

## direct scenario output
Visible client/system output confirms:
- `AIGMScenario key: monsterhunt`
- `AIGMScenario result: Dakeyras: I move toward a black bear.`
- `Scenario proof written: C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\docs\runtime\PHASE58A_MONSTERHUNT_RUNTIME_PROOF_20260613_162201.md`

## companion state / dump evidence
Visible dump output confirms:
- `Companion=Dakeyras`
- `HuntActive=True`
- `Phase=PursuingMonster`
- `Reason=pursuing_target`
- `Target=a black bear`
- `Trace=step_toward_target`
- `LastMove=step_toward_target`
- `LastCombat=none`
- `LastReject=companion_target`

This state packet appeared repeatedly in the screenshots and is sufficient to prove the execution spine left idle and entered live pursuit state.

## validator / target evidence
- A valid nearby monster target was found and accepted for the active pursuit path.
- Target accepted in runtime state:
  - `Target=a black bear`
- Additional `LastReject=companion_target` also appeared in the dump output.
- Most conservative reading: while scanning candidate mobiles, at least one companion candidate was rejected by the validator, while the black bear was accepted as the active monster target.

## movement evidence
Movement evidence is directly present:
- Scenario result text: `Dakeyras: I move toward a black bear.`
- Dump fields:
  - `Phase=PursuingMonster`
  - `Trace=step_toward_target`
  - `LastMove=step_toward_target`

This is sufficient to confirm bounded movement execution entered the live pursuit lane.

## combat evidence
- `LastCombat=none`
- No screenshot in this packet proves combat engagement yet.
- Therefore this B3 pass confirms movement/pursuit trace, but not attack/contact/combat resolution.

## door-open evidence
- Not evidenced in this packet.

## self-sustain evidence
- Not evidenced in this packet.

## capability/action trace evidence
- `Trace=step_toward_target` is non-none and directly visible.
- This satisfies the minimum action-trace requirement for B3.

## proof artifact path
Visible runtime proof path from the live client:
- `C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\docs\runtime\PHASE58A_MONSTERHUNT_RUNTIME_PROOF_20260613_162201.md`

## honest final status label
**B3 — Runtime proof generated; movement/combat trace partially confirmed.**

## conclusion
This pass successfully advances beyond the B2 no-target branch.

What is now directly verified in live runtime:
- scenario key recognized as `monsterhunt`
- nearby valid monster present: `a black bear`
- active companion selected: `Dakeyras`
- hunt left idle state
- `HuntActive=True`
- phase entered live pursuit: `PursuingMonster`
- target field is non-none
- action trace is non-none
- movement trace is non-none (`step_toward_target`)
- validator also rejected at least one companion candidate while scanning (`LastReject=companion_target`), which is consistent with safety filtering still functioning

What remains unproven in this packet:
- actual combat engagement/contact (`LastCombat` still `none`)
- door handling
- self-bandage/cure behavior
- full hunt loop / reacquire / stop completion

This is enough to close the B3 target honestly: the Phase58A harness now proves real movement-pursuit execution against a valid nearby monster under current safety boundaries.
