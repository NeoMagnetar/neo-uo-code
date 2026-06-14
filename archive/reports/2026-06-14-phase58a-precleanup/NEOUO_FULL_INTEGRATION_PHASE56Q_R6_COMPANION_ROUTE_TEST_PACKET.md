# NEOUO FULL INTEGRATION PHASE56Q R6 COMPANION ROUTE TEST PACKET

## Purpose
This packet is for operator-run in-game validation of the passive companion command routing boundary. The goal is to confirm companion-owned commands are classified correctly and do **not** open counselor/gump flows, create proposals, create `gm_follow_requester`, execute movement, or call middleware.

This is a routing and contamination test only.
- No movement is expected.
- No live movement is expected.
- No companion speech execution is expected.
- No middleware response is expected.
- `IsExecutableNow` should remain `false` in all route-diagnostic cases.

## 1. Server Start Confirmation
Record the following before testing:

- server folder used: `C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg`
- branch/head if checked: ______________________________
- build command used: `dotnet build .\ServUO.sln -v:minimal`
- build result: _______________________________________
- server launch command used: `start .\ServUO.exe`
- listening line / port observed: ______________________
- client login success: yes / no
- notes: ______________________________________________

## 2. Primary Diagnostic Command
Confirmed syntax from current code:
- `AIGMCompanionRoute <speech>`

Current diagnostic output fields:
- `Route kind`
- `Companion key`
- `Companion name`
- `Command verb`
- `Blocks counselor lane`
- `Executable now`
- `Reason`
- `Movement deferred`

## 3. Diagnostic Command Test Table
Use the command exactly as written, including the leading command token.

### A. Named Companion Route Tests
| Test ID | Input | Expected RouteKind | Expected BlocksCounselorLane | Expected IsExecutableNow | Expected Notes |
|---|---|---|---|---|---|
| NCR-01 | `[AIGMCompanionRoute dak follow me` | `NamedCompanion` | `true` | `false` | movement deferred; no gump; no middleware; no movement |
| NCR-02 | `[AIGMCompanionRoute dakeyras follow me` | `NamedCompanion` | `true` | `false` | movement deferred; no gump; no middleware; no movement |
| NCR-03 | `[AIGMCompanionRoute dan follow me` | `NamedCompanion` | `true` | `false` | movement deferred; no gump; no middleware; no movement |
| NCR-04 | `[AIGMCompanionRoute danyal follow me` | `NamedCompanion` | `true` | `false` | movement deferred; no gump; no middleware; no movement |
| NCR-05 | `[AIGMCompanionRoute dar follow me` | `NamedCompanion` | `true` | `false` | movement deferred; no gump; no middleware; no movement |
| NCR-06 | `[AIGMCompanionRoute dardalion follow me` | `NamedCompanion` | `true` | `false` | movement deferred; no gump; no middleware; no movement |

### B. Shared Companion Route Tests
| Test ID | Input | Expected RouteKind | Expected BlocksCounselorLane | Expected IsExecutableNow | Expected Notes |
|---|---|---|---|---|---|
| SCR-01 | `[AIGMCompanionRoute follow me` | `SharedCompanion` | `true` | `false` | movement deferred; no gump; no middleware; no movement |
| SCR-02 | `[AIGMCompanionRoute come here` | `SharedCompanion` | `true` | `false` | movement deferred; no gump; no middleware; no movement |
| SCR-03 | `[AIGMCompanionRoute guard me` | `SharedCompanion` | `true` | `false` | movement deferred; no gump; no middleware; no movement |
| SCR-04 | `[AIGMCompanionRoute stay here` | `SharedCompanion` | `true` | `false` | movement deferred; no gump; no middleware; no movement |
| SCR-05 | `[AIGMCompanionRoute hold position` | `SharedCompanion` | `true` | `false` | movement deferred; no gump; no middleware; no movement |
| SCR-06 | `[AIGMCompanionRoute stop` | `SharedCompanion` | `true` | `false` | movement deferred; no gump; no middleware; no movement |
| SCR-07 | `[AIGMCompanionRoute wait` | `SharedCompanion` | `true` | `false` | movement deferred; no gump; no middleware; no movement |
| SCR-08 | `[AIGMCompanionRoute track around` | `SharedCompanion` | `true` | `false` | movement deferred; no gump; no middleware; no movement |
| SCR-09 | `[AIGMCompanionRoute scan the area` | `SharedCompanion` | `true` | `false` | movement deferred; no gump; no middleware; no movement |
| SCR-10 | `[AIGMCompanionRoute report status` | `SharedCompanion` | `true` | `false` | movement deferred; no gump; no middleware; no movement |

### C. Non-Companion Route Tests
| Test ID | Input | Expected RouteKind | Expected BlocksCounselorLane | Expected IsExecutableNow | Expected Notes |
|---|---|---|---|---|---|
| NNR-01 | `[AIGMCompanionRoute hello there` | `NonCompanion` | `false` | `false` | no movement |
| NNR-02 | `[AIGMCompanionRoute what can you do` | `NonCompanion` | `false` | `false` | no movement |
| NNR-03 | `[AIGMCompanionRoute open counselor` | `NonCompanion` | `false` | `false` | no movement |
| NNR-04 | `[AIGMCompanionRoute I need help` | `NonCompanion` | `false` | `false` | no movement |

### D. Unknown Alias Tests
| Test ID | Input | Expected RouteKind | Expected BlocksCounselorLane | Expected IsExecutableNow | Expected Notes |
|---|---|---|---|---|---|
| UAR-01 | `[AIGMCompanionRoute bob follow me` | `UnknownCompanionAlias` or `NonCompanion` | record exact result | `false` | no movement |
| UAR-02 | `[AIGMCompanionRoute unknown follow me` | `UnknownCompanionAlias` or `NonCompanion` | record exact result | `false` | no movement |

## 4. Live Speech Contamination Tests
These are **not** movement tests.

Stand near the counselor and, if practical, near companion shell presence. Say each phrase in live in-game speech and record observed behavior.

### Inputs
- `dak follow me`
- `danyal follow me`
- `follow me`
- `scan the area`
- `hello counselor`

### Record for each
- Did counselor gump open? yes / no
- Did any `gm_follow_requester` text appear? yes / no
- Did any middleware response appear? yes / no
- Did any companion move? yes / no (expected: no)
- Did server console show error? yes / no
- Notes

### Expected Behavior
For companion-owned commands:
- counselor gump should **not** open
- `gm_follow_requester` should **not** appear
- middleware response should **not** appear
- movement should **not** occur

For non-companion counselor speech such as `hello counselor`:
- counselor behavior may still occur if the current counselor lane normally handles it

## 5. Pass / Fail Criteria
### Pass
- diagnostic command classifies named commands as `NamedCompanion`
- diagnostic command classifies shared commands as `SharedCompanion`
- `IsExecutableNow` remains `false`
- companion-owned live speech does not open counselor/gump path
- no movement occurs
- no middleware call occurs
- no server errors occur during route testing

### Fail
- counselor gump opens for `dak follow me`
- counselor gump opens for `follow me`
- `gm_follow_requester` appears for companion-owned command
- movement occurs
- middleware is called
- `IsExecutableNow` is `true`
- server compile/runtime error occurs

## 6. Result Capture Template
Copy one block per test case.

```markdown
- Test ID:
- Input:
- Expected RouteKind:
- Actual RouteKind:
- Expected BlocksCounselorLane:
- Actual BlocksCounselorLane:
- Expected IsExecutableNow:
- Actual IsExecutableNow:
- Counselor gump opened?:
- gm_follow_requester appeared?:
- Middleware response appeared?:
- Movement occurred?:
- Server console errors?:
- Pass/fail:
- Notes:
```

## 7. Operator Execution Order
1. Build the current integration repo.
2. Start the server from the current integration repo.
3. Confirm client login works.
4. Run all `AIGMCompanionRoute` diagnostic command tests first.
5. Record actual route output line-by-line.
6. Run live speech contamination tests near the counselor.
7. Confirm no movement, no middleware, and no counselor contamination for companion-owned commands.
8. Save all observations before any next phase begins.

## 8. Next-Phase Decision Rules
If all route diagnostics pass:
- recommend `PHASE 56Q-R6-SERVUO-MOVEMENT-EXECUTOR-PLAN`

If counselor contamination remains:
- recommend `PHASE 56Q-R6-COMPANION-COUNSELOR-EARLY-EXIT-REPAIR`

If diagnostic route classification is wrong:
- recommend `PHASE 56Q-R6-COMPANION-ROUTING-BOUNDARY-REPAIR`

If command surface itself fails:
- recommend `PHASE 56Q-R6-COMPANION-COMMAND-DIAGNOSTIC-REPAIR`

## 9. Final Reminder
This packet validates routing doctrine only.
- Do not expect live follow.
- Do not expect live guard/come/stay execution.
- Do not expect middleware participation.
- Any movement during these tests is a failure.
