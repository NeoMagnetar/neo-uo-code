# NEOUO FULL INTEGRATION PHASE56Q R6 COMPANION NATURAL SPEECH TEST PACKET

## Purpose
This packet is for operator-facing in-game validation of the natural companion speech UX route-echo behavior.

Product rule:
- Natural speech is the product.
- Bracket commands are admin/debug only.
- Do **not** require `[AIGMCompanionRoute ...]` for normal companion play.
- Normal play must not require `[AIGMCompanionRoute ...]`.

This packet validates natural speech behavior without movement.
- No live movement is expected.
- No middleware response is expected.
- No companion action execution is expected.
- `IsExecutableNow` remains false.

## 1. Server Start Confirmation
Record the following before testing:

- server folder used: `C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg`
- build command used: `dotnet build .\ServUO.sln -v:minimal`
- build result: _______________________________________
- launch command used: `start .\ServUO.exe`
- listening line / port observed: ______________________
- client login success: yes / no
- account / character used: ____________________________
- companions spawned/present: yes / no
- companion locations relative to player: ______________
- notes: ______________________________________________

## 2. Companion Spawn / Setup Section
These are admin setup operations only:
- `[Dakeyras`
- `[Danyal`
- `[Dardalion`

Clarifications:
- these are setup/spawn commands for staff/admin use
- normal companion control is natural speech
- `[AIGMCompanionRoute ...]` is optional debug only, not product UX

## 3. Named Natural Speech Tests
### A. Dakeyras named speech
| Test ID | Spoken Input | Expected Result |
|---|---|---|
| NNS-D-01 | `dak follow me` | only Dakeyras echoes; Danyal silent; Dardalion silent; no counselor gump; no movement; no middleware |
| NNS-D-02 | `dakeyras follow me` | only Dakeyras echoes; Danyal silent; Dardalion silent; no counselor gump; no movement; no middleware |
| NNS-D-03 | `dak scan area` | only Dakeyras echoes; Danyal silent; Dardalion silent; no counselor gump; no movement; no middleware |
| NNS-D-04 | `dak start tracking` | only Dakeyras echoes; Danyal silent; Dardalion silent; no counselor gump; no movement; no middleware |

### B. Danyal named speech
| Test ID | Spoken Input | Expected Result |
|---|---|---|
| NNS-Y-01 | `danyal follow me` | only Danyal echoes; Dakeyras silent; Dardalion silent; no counselor gump; no movement; no middleware |
| NNS-Y-02 | `dan follow me` | only Danyal echoes; Dakeyras silent; Dardalion silent; no counselor gump; no movement; no middleware |

### C. Dardalion named speech
| Test ID | Spoken Input | Expected Result |
|---|---|---|
| NNS-R-01 | `dar follow me` | only Dardalion echoes; Dakeyras silent; Danyal silent; no counselor gump; no movement; no middleware |
| NNS-R-02 | `dardalion follow me` | only Dardalion echoes; Dakeyras silent; Danyal silent; no counselor gump; no movement; no middleware |

## 4. Shared Natural Speech Tests
Shared speech should be recognized by the companion lane, but only one eligible owned companion should echo.

Current anti-spam policy:
- closest eligible nearby owned companion echoes
- if tied, lower serial wins

| Test ID | Spoken Input | Expected Result |
|---|---|---|
| SNS-01 | `follow me` | one eligible owned companion echoes; counselor gump does not open; no `gm_follow_requester`; no movement; no middleware |
| SNS-02 | `come here` | one eligible owned companion echoes; counselor gump does not open; no `gm_follow_requester`; no movement; no middleware |
| SNS-03 | `guard me` | one eligible owned companion echoes; counselor gump does not open; no `gm_follow_requester`; no movement; no middleware |
| SNS-04 | `stay here` | one eligible owned companion echoes; counselor gump does not open; no `gm_follow_requester`; no movement; no middleware |
| SNS-05 | `hold position` | one eligible owned companion echoes; counselor gump does not open; no `gm_follow_requester`; no movement; no middleware |
| SNS-06 | `stop` | one eligible owned companion echoes; counselor gump does not open; no `gm_follow_requester`; no movement; no middleware |
| SNS-07 | `wait` | one eligible owned companion echoes; counselor gump does not open; no `gm_follow_requester`; no movement; no middleware |
| SNS-08 | `scan the area` | one eligible owned companion echoes; counselor gump does not open; no `gm_follow_requester`; no movement; no middleware |
| SNS-09 | `start tracking` | one eligible owned companion echoes; counselor gump does not open; no `gm_follow_requester`; no movement; no middleware |
| SNS-10 | `track around` | one eligible owned companion echoes; counselor gump does not open; no `gm_follow_requester`; no movement; no middleware |
| SNS-11 | `report status` | one eligible owned companion echoes; counselor gump does not open; no `gm_follow_requester`; no movement; no middleware |

## 5. Anti-Spam / Ownership Behavior Tests
### Multiple companions nearby
1. Place Dakeyras, Danyal, and Dardalion near the player.
2. Say: `follow me`
3. Record which companion echoes.
4. Confirm only one companion echoes.

### Distance test
If practical:
1. Move one companion closer to the player than the others.
2. Say: `scan the area`
3. Record whether the closest companion echoes.

### Ownership / authorization observation
If ownership checks exist and it is safe to test:
- test owner speech
- test non-owner speech
- record whether non-owner speech is ignored or echoed

Do not force complex ownership scenarios if current shell behavior is intentionally conservative/diagnostic-only.

## 6. Counselor Separation Tests
### Counselor speech inputs
Say:
- `hello counselor`
- `consult counselor`
- `question counselor`
- `archives`

Expected:
- companion shells stay quiet
- counselor lane may handle normally
- counselor gump may open for counselor commands
- this is not contamination

### Companion speech near counselor
Say:
- `dak follow me`
- `follow me`
- `scan the area`

Expected:
- counselor gump does not open
- no `gm_follow_requester` appears
- no counselor response takes ownership
- companion echo behavior occurs as above

## 7. Non-Companion Speech Tests
Say:
- `hello there`
- `what can you do`
- `I need help`
- random player chatter

Expected:
- companion shells do not echo
- counselor behavior depends on normal counselor proximity/logic
- no movement
- no middleware from companion lane

## 8. Optional Admin / Debug Comparison
This section is optional and staff-only.

Use only if natural speech behavior is ambiguous.
- `[AIGMCompanionRoute dak follow me`
- `[AIGMCompanionRoute follow me`
- `[AIGMCompanionRoute hello there`

Purpose:
- compare route classification vs live speech behavior

Clarifications:
- not normal gameplay
- not required for average play

## 9. Pass / Fail Criteria
### Pass
- named natural speech echoes only from the addressed shell
- shared natural speech echoes only from one eligible shell
- shared anti-spam policy works
- `hello counselor` does not trigger companion echo
- companion commands do not open counselor gump
- no `gm_follow_requester` appears for companion commands
- no movement occurs
- no middleware response occurs
- no server errors occur

### Fail
- all companions echo shared commands at once
- named command echoes from the wrong shell
- companion command opens counselor gump
- `gm_follow_requester` appears
- movement occurs
- middleware response appears
- server error appears
- normal play requires `[AIGMCompanionRoute ...]`

## 10. Result Capture Template
Copy one block per test case.

```markdown
- Test ID:
- Spoken input:
- Companions present:
- Companion positions / distance notes:
- Expected echo shell:
- Actual echo shell:
- Did any non-target shell echo?:
- Did counselor gump open?:
- Did gm_follow_requester appear?:
- Did middleware response appear?:
- Did movement occur?:
- Server console errors?:
- Pass/fail:
- Notes:
```

## 11. Next-Phase Decision Rules
If natural speech UX passes:
- recommend `PHASE 56Q-R6-SERVUO-MOVEMENT-EXECUTOR-PLAN`

If shared speech is noisy:
- recommend `PHASE 56Q-R6-COMPANION-SHARED-ECHO-ANTI-SPAM-REPAIR`

If named speech routes wrong:
- recommend `PHASE 56Q-R6-COMPANION-NAMED-SPEECH-REPAIR`

If counselor still hijacks:
- recommend `PHASE 56Q-R6-COMPANION-COUNSELOR-EARLY-EXIT-REPAIR`

If companion shells fail to spawn/load:
- recommend `PHASE 56Q-R6-COMPANION-SHELL-RUNTIME-REPAIR`

## 12. Final Reminder
This packet is for validating natural in-game UX without movement.
- Normal play should use speech, not bracket commands.
- No movement should occur.
- No middleware should be called.
- No live action execution should occur.
