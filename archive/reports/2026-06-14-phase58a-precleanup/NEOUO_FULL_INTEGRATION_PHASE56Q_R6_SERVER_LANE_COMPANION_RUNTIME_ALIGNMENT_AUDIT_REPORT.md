# NEOUO FULL INTEGRATION PHASE56Q R6 SERVER LANE COMPANION RUNTIME ALIGNMENT AUDIT REPORT

## Scope / Guardrails
Audit phase only.

Confirmed for this phase:
- no files copied
- no files staged
- no commit made
- no reset / clean / overwrite performed
- no server folders bulk-replaced from backup

Note: before the audit instruction arrived, a local companion-lane patch had already been applied to `NeoUO-Dev` companion class files and runtime notes had been written into `neo-uo-code`. This audit itself makes no additional runtime code changes and treats `NeoUO-FullIntegration-aigm-umg` as the authoritative current code server for forward work.

---

## 1. Current Repo State
### Current authoring target
- Repo path: `C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg`
- Branch: `neo/staging-aigm`
- HEAD: `b317ae851fd24035611c2f925a6b052f4f820420`

### Git status
- Current repo contains untracked audit/report files in normal course of this work.
- No files were staged in this audit phase.

### Latest commit context
- Latest visible work in this repo includes the previously committed landmark model / contract / no-op / skeleton / manual verification documentation chain.

### Build result
Command:
- `dotnet build .\ServUO.sln -v:minimal`

Result:
- Build succeeded
- Errors: `0`
- Warnings: `0`

Assessment:
- current integration repo is compile-clean and safe as a forward authoring target

---

## 2. Server Folder Map
### `C:\UO\Server\Neo Ultima Online\NeoUO-Dev`
- Classification: **old working reference** / live prototype shard
- Role: contains the old working companion runtime with Dakeyras / Danyal / Dardalion live logic
- Use: read-only comparison source unless explicitly instructed to patch/test there
- Do not: treat as the authoritative forward code-save target

### `C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg`
- Classification: **current authoring target**
- Role: current clean integration server / full integration target
- Use: add new safe code here, build here, commit here, push from here when authorized
- Do not: bulk overwrite from Dev

### `C:\UO\Server\Neo Ultima Online\NeoUO-Integration-aigm-umg`
- Classification: **partial clone / earlier integration reference**
- Role: earlier partial integration clone
- Use: reference only unless explicitly reauthorized
- Do not: treat as primary compile-validation target

### `C:\UO\Server\Neo Ultima Online\Backups\NeoUO-Dev-preserve-aigm-working-20260607-072757`
- Classification: **backup / emergency preserved working snapshot**
- Role: preserved old working companion snapshot
- Use: read-only comparison / recovery source
- Do not: edit or overwrite

### `C:\UO\Server\Neo Ultima Online\*` other folders
- Classification: production/original or ignore/reference only depending on name
- Use: only with explicit authorization

### `C:\.openclaw\workspace\umg-uo-brain`
- Classification: **UMG cognition reference only**
- Role: read-only cognition/block/ontology/runtime bundle repo
- Use: semantics reference only
- Do not: place ServUO runtime code here

### `C:\.openclaw\workspace`
- Classification: **OpenClaw main workspace**
- Role: main UMG/OpenClaw/Envoy lane
- Use: not for NeoUO server code

### `C:\.openclaw\workspace-ultima-online`
- Classification: **OpenClaw Ultima workspace**
- Role: agent workspace for NeoUO / ServUO lane and AIGM middleware scripts
- Use: only with selected `ultima-online` session

---

## 3. Start Command Map
### NeoUO-FullIntegration-aigm-umg
Path:
- `C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg`

Artifacts:
- `ServUO.sln`: present
- `ServUO.exe`: present
- `_windebug.bat`: present
- `_winrelease.bat`: present

Batch file behavior:
- `_windebug.bat`: `dotnet build -c Debug` then `ServUO.exe -debug`
- `_winrelease.bat`: `dotnet build -c Release` then `ServUO.exe`

Recommended safe start command:
```powershell
cd "C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg"
dotnet build .\ServUO.sln -v:minimal
start .\ServUO.exe
```

Assessment:
- clean integration server can be started this way, but live companion follow should **not** be expected here until deliberate port/recovery work is done, because most old live companion runtime files are absent

### NeoUO-Dev
Path:
- `C:\UO\Server\Neo Ultima Online\NeoUO-Dev`

Artifacts:
- `ServUO.sln`: present
- `ServUO.exe`: present
- `_windebug.bat`: present
- `_winrelease.bat`: present

Batch file behavior:
- `_windebug.bat`: debug build then `ServUO.exe -debug`
- `_winrelease.bat`: release build then `ServUO.exe`

Recovered historically correct regular startup:
```powershell
Start-Process cmd.exe -ArgumentList '/k', 'cd /d "C:\UO\Server\Neo Ultima Online\NeoUO-Dev" && ServUO.exe'
```

Recovered build-first pattern from shell history:
```powershell
dotnet build "C:\UO\Server\Neo Ultima Online\NeoUO-Dev\Scripts\Scripts.csproj" -c Debug -p:Platform=x64
Start-Process cmd.exe -ArgumentList '/k', 'cd /d "C:\UO\Server\Neo Ultima Online\NeoUO-Dev" && ServUO.exe'
```

Assessment:
- this is the old working/live reference shard and the historically normal ServUO terminal startup path

### NeoUO-Integration-aigm-umg
Artifacts:
- startup artifacts present in typical ServUO shape

Assessment:
- not primary target; reference only unless explicitly reauthorized

### Port/listening note
- Start only one ServUO server at a time unless ports are explicitly verified.
- Record the `Listening:` line and port when using any shard.
- Do not confuse a running `NeoUO-Dev` with `NeoUO-FullIntegration-aigm-umg`.

---

## 4. Middleware Startup / Config Inventory
### Always-on foundation
- `openclaw gateway`
- Node.js runtime host
- foundational middleware / agent runtime
- separate from ServUO shard startup

### Actual AIGM companion middleware
Path:
- `C:\.openclaw\workspace-ultima-online\aigm-middleware-service.js`

Control scripts:
- `start-aigm-middleware.ps1`
- `status-aigm-middleware.ps1`
- `stop-aigm-middleware.ps1`
- `Restart-AIGM-Middleware.ps1`

Supervisor:
- scheduled task `AIGM-Supervisor`

Observed endpoint/port:
- `127.0.0.1:4876`
- route `/aigm/query`
- health `/health`
- version `/version`

Current startup command family:
```powershell
powershell -ExecutionPolicy Bypass -File C:\.openclaw\workspace-ultima-online\start-aigm-middleware.ps1
```

### Current integration repo bridge references
- `Scripts\Custom\AIGM\AIGMBridgeClient.cs` exists in current integration repo
- bridge references point at localhost AIGM middleware endpoint family

### Old Dev bridge references
- `Scripts\Custom\AIGM\AIGMCompanionBridgeClient.cs` exists in old Dev
- companion runtime bridges to middleware for companion-oriented responses/actions

### Ownership classification
- middleware belongs to the broader OpenClaw / Ultima workspace + companion runtime stack
- it is shared infrastructure used by old Dev and relevant future integration work
- it is not a simple ServUO-only local script

---

## 5. Code-Save Protocol
### Authoritative save/commit target going forward
- `C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg`
- branch: `neo/staging-aigm`

### Before edits
```powershell
git branch --show-current
git rev-parse HEAD
git status --short
```

### Build before/after
```powershell
dotnet build .\ServUO.sln -v:minimal
```

### Commit protocol
```powershell
git add <exact file 1>
git add <exact file 2>
git diff --cached --name-status
git commit -m "<clear message>"
```

### Do not
- do not commit from `NeoUO-Dev` unless explicitly instructed
- do not commit from OpenClaw main workspace
- do not commit generated junk
- do not commit `bin/`, `obj/`, `_buildcheck/`
- do not commit unrelated reports unless phase explicitly says so
- do not use `git reset`
- do not use `git clean`
- do not bulk-copy backups into the integration repo

---

## 6. Current vs Dev Companion File Presence Table
| File | Current Integration | Old Dev |
|---|---|---|
| `Scripts\Commands\AIGMCompanionCommand.cs` | absent | present |
| `Scripts\Custom\AIGM\AIGMCompanionSpeechQueue.cs` | absent | present |
| `Scripts\Custom\AIGM\AIGMCompanionActionExecutor.cs` | absent | present |
| `Scripts\Custom\AIGM\AIGMCompanionDirectActionPolicy.cs` | absent | present |
| `Scripts\Custom\AIGM\AIGMCompanionTravelController.cs` | absent | present |
| `Scripts\Custom\AIGM\AIGMCompanionAutoPathNavigator.cs` | absent | present |
| `Scripts\Custom\AIGM\AIGMCompanionStateAccess.cs` | absent | present |
| `Scripts\Mobiles\NPCs\AIGMCompanionDakeyras.cs` | absent | present |
| `Scripts\Mobiles\NPCs\AIGMCompanionDanyal.cs` | absent | present |
| `Scripts\Mobiles\NPCs\AIGMCompanionDardalion.cs` | absent | present |

Assessment:
- current integration repo does **not** yet contain most old live companion runtime files
- old Dev remains the source of truth for the live prototype companion lane implementation

---

## 7. Old Dev Companion Follow Path Summary
### Parse / command entry
- `AIGMCompanionIntentParser.cs` recognizes:
  - `follow me`
  - `follow`
  - `come`
  - `come here`
  - `guard me`
  - `stay`
  - and explicit name prefixes (`dak`, `danyal`, `dar`, etc.)

### Speech entry / relay
- companion classes (`Dakeyras`, `Danyal`, `Dardalion`) receive owner speech in `OnSpeech(...)`
- publish linked owner speech via `AIGMCompanionSpeechBus.PublishOwnerSpeech(...)`

### Direct command lane
- `AIGMCompanionDirectActionPolicy.Decide(...)`
- `AIGMCompanionActionExecutor.TryExecuteIntent(...)`
- `UMGMovementRouter.RouteIntent(...)`

### Actual movement primitive used in old Dev
Inside old Dev `UMGMovementRouter.cs`:
- `FollowPlayer` -> `ControlTarget = requester`, `ControlOrder = OrderType.Follow`
- `ReturnToPlayer` -> `ControlOrder = OrderType.Come`
- `GuardTarget` -> `ControlOrder = OrderType.Guard`
- `HoldPosition` -> hold/idle state changes

### Additional old runtime movement layers present
- `AIGMCompanionStateAccess`
- `AIGMCompanionTravelController`
- `AIGMCompanionAutoPathNavigator`

These indicate old Dev had real direct movement/state/travel handling beyond the safe no-op integration architecture.

### Counselor involvement in old Dev
Also present in old Dev:
- `AIGMCounselor`
- `AIGMResponseGump`
- `AIGMQuestionGump`
- generic `gm_follow_requester` proposal path

This is command-lane contamination relative to the intended companion-only lane.

---

## 8. Current Integration Follow Path Summary
### Present in current integration
- movement intent model
- movement state model
- movement router
- movement gate model
- movement execution contract
- no-op movement executor
- safe landmark / navigation DTO infrastructure

### Missing from current integration
- old live companion command registration
- old companion speech queue
- old companion action executor
- old companion travel controller / auto-path navigator / state access
- old companion mobile classes

### Expected behavior in current integration
- parser/router can represent movement intent
- live follow should **not** be assumed to work yet
- current stack is intentionally safe/no-op for movement until a real executor is deliberately implemented

---

## 9. Counselor Lane Contamination Assessment
### Counselor/gump lane elements
- `AIGMCounselor`
- `AIGMResponseGump`
- `AIGMQuestionGump`
- generic proposal lane
- `gm_follow_requester`
- counselor movement failure messages

### Companion lane elements
- Dakeyras
- Danyal
- Dardalion
- `AIGMCompanionCommand`
- companion speech bus / queue / parser / direct action policy / action executor
- `UMGMovementRouter` companion follow/guard/hold path

### Current contamination finding
In old Dev:
- generic follow can still surface counselor-framed proposal text such as:
  - `Have the counselor follow you.`
- companion shared-lane relay also downgrades owner-relayed speech into queue modes (`owner_relay_awareness` / `owner_relay_dialogue`)
- queue performs owner-turn arbitration instead of shared command execution

Conclusion:
- counselor lane can still catch or coexist with companion follow semantics in old Dev
- this is contamination and should be blocked/rerouted in future recovery work

---

## 10. Movement / No-Op Assessment
### Current integration repo
- safe movement architecture exists
- movement executor is effectively no-op / safe gate aligned
- companion follow should not be expected to perform real NPC movement until executor work is done

### Old Dev
- live direct movement path exists through `ControlOrder` writes and travel/state support
- but this path is mixed with legacy/counselor contamination and bypasses the new safe gate architecture

Risk:
- old Dev movement may work but is not automatically safe to port directly

---

## 11. File Bucket Classification
### Bucket A — safe candidates for selective port
- `Scripts\Commands\AIGMCompanionCommand.cs`
- companion mobile identity shells
- minimal speech routing / parser DTOs / harmless models

### Bucket B — port only behind new gates/contracts
- `AIGMCompanionActionExecutor.cs`
- `AIGMCompanionSpeechQueue.cs`
- `AIGMCompanionDirectActionPolicy.cs`
- `AIGMCompanionTravelController.cs`
- `AIGMCompanionAutoPathNavigator.cs`
- direct movement / follow action paths

### Bucket C — dangerous / do not directly port
- `AIGMCompanionStateAccess.cs`
- direct location mutation patterns
- arbitrary autonomous pathing without gate control
- any code that lets counselor lane command companions

### Bucket D — already replaced in current integration
- current intent model
- current parser / router surface
- current `UMGMovementRouter` abstraction layer
- current movement gate / contract model
- current no-op executor
- navigation snapshot / landmark scaffolding

---

## 12. Recommended Recovery Strategy
### Primary recommendation: Option C
**Port old speech/command queue carefully, but route actions through current parser/router/gates.**

Refined phased path:
1. restore command registration + companion mobile shells in current integration
2. restore companion speech/command lane only, without old unsafe autonomous movement internals
3. implement a narrow real `UMGServUOMovementExecutor` behind `IUMGMovementExecutor` for explicit follow/come/guard primitives only
4. block counselor/gump ownership of companion commands
5. test named commands first:
   - `dak follow me`
   - `danyal follow me`
6. then test shared `follow me`

### Alternate path: Option B
Implement only a narrow real `UMGServUOMovementExecutor` for explicit `FollowPlayer` first, after command lane restoration.

### Not recommended as primary
- full bulk dump from old Dev into current integration
- direct StateAccess / auto-path / travel-controller import without current gate alignment
- keeping counselor/gump lane active for companion commands

---

## 13. Explicit No-Copy List
Do not bulk-copy from old Dev into current integration:
- entire `Scripts\Custom\AIGM` tree
- entire `Scripts\Mobiles\NPCs` tree
- backup snapshots
- `bin/`, `obj/`, `_buildcheck/`
- old runtime travel/path/state internals without selective review

---

## 14. Explicit Selective-Port Candidates
Safest first selective-port candidates into current integration:
- `AIGMCompanionCommand.cs`
- companion mobile identity shells for Dakeyras / Danyal / Dardalion
- minimal direct speech routing hooks
- parser/intent DTO compatibility surfaces
- direct-action policy adapted to current router/gates

Port only after wrapper design/gate review:
- action executor
- speech queue
- travel controller
- auto-path navigator
- any movement primitive beyond explicit follow/guard/hold

---

## 15. Final Alignment Conclusions
- Authoritative current code server: `NeoUO-FullIntegration-aigm-umg`
- Old working reference only: `NeoUO-Dev`
- Preserved working backup: `Backups\NeoUO-Dev-preserve-aigm-working-20260607-072757`
- Current integration is compile-clean and safe, but intentionally lacks most live companion runtime files.
- Old Dev contains the old working companion runtime, but also contains counselor contamination and queue/arbitration behavior that is not aligned with the intended clean companion-only lane.
- Companion follow should not be expected to work fully in current integration until the live command lane is deliberately restored and a real movement executor is safely implemented.
- All new forward work should be authored, built, and committed in `NeoUO-FullIntegration-aigm-umg` on `neo/staging-aigm`.

## 16. Audit Integrity
Confirmed for this audit phase:
- no files copied from backup
- no files staged in current integration repo
- no commit made
- no reset / clean / destructive sync performed
