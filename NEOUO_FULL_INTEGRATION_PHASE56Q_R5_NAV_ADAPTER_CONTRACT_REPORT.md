# NEOUO FULL INTEGRATION PHASE56Q-R5 NAV-ADAPTER-CONTRACT REPORT

## A. Header
- phase: `PHASE 56Q-R5-NAV-ADAPTER-CONTRACT`
- target repo: `C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg`
- branch: `neo/staging-aigm`
- HEAD: `d6444f5813a248f0a75746622db1b46d52ab91b5`
- latest commit: `d6444f58 feat: wire UMG router to no-op movement executor`
- build result:
  - `dotnet build .\ServUO.sln -v:minimal`
  - **Build succeeded**
  - **0 Error(s)**
  - **15 Warning(s)**
- warning count/files:
  - `Scripts\Custom\AIGM\AIGMBridgeClient.cs`
  - `Scripts\Gumps\AIGMResponseGump.cs`
  - `Scripts\Gumps\AIGMQuestionGump.cs`
  - `Scripts\Mobiles\NPCs\AIGMCounselor.cs`

## B. Inputs inspected
### Inventory report path
- `C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\NEOUO_FULL_INTEGRATION_PHASE56Q_R5_NAV_CAPABILITY_INVENTORY_REPORT.md`

### Movement/execution files inspected for style
- `Scripts/Custom/AIGM/Movement/UMGMovementIntent.cs`
- `Scripts/Custom/AIGM/Movement/UMGMovementExecutionResult.cs`
- `Scripts/Custom/AIGM/Movement/IUMGMovementExecutor.cs`
- `Scripts/Custom/AIGM/Movement/UMGMovementNoOpExecutor.cs`

### Relevant findings imported from inventory report
- strong low-level ServUO navigation primitives already exist
- main risk is authority leakage, not primitive absence
- preferred next step is a read-only navigation adapter boundary
- contract should return safe summaries instead of live ServUO objects
- cross-repo follow-through will later be needed in code repo, wiki/NL repo, and UMG repo

## C. Files added
Added exactly these files:
- `Scripts/Custom/AIGM/Navigation/IUMGNavigationAdapter.cs`
- `Scripts/Custom/AIGM/Navigation/UMGNavigationProbeResult.cs`
- `Scripts/Custom/AIGM/Navigation/UMGNavigationRegionResult.cs`
- `Scripts/Custom/AIGM/Navigation/UMGNavigationEntitySnapshot.cs`

## D. Interface added
### `IUMGNavigationAdapter`
Methods added:
- `UMGNavigationProbeResult ProbePoint(string mapName, int x, int y, int z, string reason = null)`
- `UMGNavigationRegionResult GetRegionAt(string mapName, int x, int y, int z)`
- `UMGNavigationProbeResult CheckRange(string mapName, int fromX, int fromY, int fromZ, int toX, int toY, int toZ, int maxRange)`
- `UMGNavigationProbeResult CheckLineOfSight(string mapName, int fromX, int fromY, int fromZ, int toX, int toY, int toZ)`
- `IEnumerable<UMGNavigationEntitySnapshot> GetNearbyEntities(string mapName, int x, int y, int z, int range, int maxResults)`

### Purpose of each method
- `ProbePoint(...)`
  - future read-only standability / blocked-point probe
- `GetRegionAt(...)`
  - future coordinate-to-region awareness
- `CheckRange(...)`
  - future passive range relation check
- `CheckLineOfSight(...)`
  - future passive LOS relation check
- `GetNearbyEntities(...)`
  - future read-only nearby entity summary query

## E. DTOs added
### `UMGNavigationProbeResult`
Properties:
- `bool Succeeded`
- `bool IsPassable`
- `bool IsBlocked`
- `bool IsInRange`
- `bool HasLineOfSight`
- `string Reason`
- `string Detail`
- `string MapName`
- `int X`
- `int Y`
- `int Z`
- `DateTime CheckedUtc`

Helpers:
- `Passable(...)`
- `Blocked(...)`
- `InRange(...)`
- `OutOfRange(...)`
- `LineOfSight(...)`
- `NoLineOfSight(...)`
- `Failure(...)`

### `UMGNavigationRegionResult`
Properties:
- `bool Succeeded`
- `string Reason`
- `string Detail`
- `string MapName`
- `int X`
- `int Y`
- `int Z`
- `string RegionName`
- `string ParentRegionName`
- `string RegionKind`
- `bool IsTown`
- `bool IsDungeon`
- `bool IsGuarded`
- `DateTime CheckedUtc`

Helpers:
- `Found(...)`
- `Unknown(...)`
- `Failure(...)`

### `UMGNavigationEntitySnapshot`
Properties:
- `string EntityId`
- `string EntityKind`
- `string DisplayName`
- `string MapName`
- `int X`
- `int Y`
- `int Z`
- `int Distance`
- `bool IsHostile`
- `bool IsPlayer`
- `bool IsCreature`
- `bool IsItem`
- `bool IsDeleted`
- `DateTime SeenUtc`

## F. Safety confirmations
- read-only contract only: **confirmed**
- no implementation: **confirmed**
- no real navigation: **confirmed**
- no movement: **confirmed**
- no pathfinding: **confirmed**
- no world mutation: **confirmed**
- no `BaseHire` dependency: **confirmed**
- no `Server.Mobile` dependency: **confirmed**
- no `StateAccess` reference: **confirmed**
- router code not modified: **confirmed**
- movement/execution files not modified: **confirmed**
- parser / skill / companion / action / command files not modified: **confirmed**

## G. Forbidden-string scan result
Scanned navigation files for:
- `BaseHire`
- `Server.Mobile`
- `Combatant`
- `ControlTarget`
- `ControlOrder`
- `CantWalk`
- `Home`
- `RangeHome`
- `MoveToWorld`
- `SetLocation`
- `Location =`
- `Direction =`
- `AIObject`
- `DelayCall`
- `Timer`
- `AIGMCompanionStateAccess`
- `AIGMCompanionDakeyras`
- `AIGMCompanionDanyal`
- `AIGMCompanionDardalion`

Result:
- **PASS**
- no forbidden authority strings found

## H. Cross-repo alignment notes
### Later NeoUO wiki / NL repo updates
Should later document:
- read-only navigation adapter purpose and boundaries
- map/region/LOS/range question model in NL
- how navigation snapshots feed UMG cognition
- dry-run vs live navigation execution phases

### Later UO UMG repo updates
Should later model:
- navigation perception snapshot block
- landmark/region interpretation block
- nearby entity perception block
- passive range/LOS check block
- navigation probe / route candidate block
- progress/stuck monitoring blocks

## I. Recommended next phase
Recommended next phase:
- **Phase 56Q-R5-NAV-ADAPTER-CONTRACT-P — Commit Read-Only Navigation Adapter Contract**

Suggested commit message:
- `feat: add read-only UMG navigation adapter contract`

Likely phase after commit:
- **Phase 56Q-R5-NAV-ADAPTER-NOOP — Add no-op navigation adapter + build**

Alternative later phase:
- **Phase 56Q-R5-NAV-SNAPSHOT-MODEL — Add navigation snapshot model**
