# NEOUO FULL INTEGRATION PHASE56Q-R5 NAV-SNAPSHOT-MODEL REPORT

## A. Header
- phase: `PHASE 56Q-R5-NAV-SNAPSHOT-MODEL`
- target repo: `C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg`
- branch: `neo/staging-aigm`
- HEAD: `d9c02d53d07c2e7ba49136baec29dfa061cca9c2`
- latest commit: `d9c02d5 feat: add no-op UMG navigation adapter`
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
Inspected:
- `Scripts/Custom/AIGM/Navigation/IUMGNavigationAdapter.cs`
- `Scripts/Custom/AIGM/Navigation/UMGNavigationNoOpAdapter.cs`
- `Scripts/Custom/AIGM/Navigation/UMGNavigationProbeResult.cs`
- `Scripts/Custom/AIGM/Navigation/UMGNavigationRegionResult.cs`
- `Scripts/Custom/AIGM/Navigation/UMGNavigationEntitySnapshot.cs`

Style findings used:
- namespace is `Server.Custom.AIGM`
- DTOs use public auto-properties
- DTO helpers use static factory methods returning new instances
- `DateTime.UtcNow` is used for timestamp fields
- navigation adapter uses `IEnumerable<UMGNavigationEntitySnapshot>` for query boundary
- passive DTO style favors null-tolerant strings and simple constructor-free object initialization
- collection-bearing snapshot model can safely initialize `List<UMGNavigationEntitySnapshot>` in constructor

## C. File added
Added:
- `Scripts/Custom/AIGM/Navigation/UMGNavigationSnapshot.cs`

## D. Snapshot model summary
Added properties:
- `string SnapshotId`
- `string Source`
- `string Reason`
- `string Detail`
- `string ActorId`
- `string ActorProfileKey`
- `string MapName`
- `int X`
- `int Y`
- `int Z`
- `DateTime CreatedUtc`
- `UMGNavigationProbeResult PositionProbe`
- `UMGNavigationRegionResult Region`
- `List<UMGNavigationEntitySnapshot> NearbyEntities`
- `bool HasNearestLandmark`
- `string NearestLandmarkId`
- `string NearestLandmarkName`
- `string NearestLandmarkKind`
- `int DistanceToNearestLandmark`
- `bool HasDestination`
- `string DestinationId`
- `string DestinationName`
- `string DestinationKind`
- `string DestinationMapName`
- `int DestinationX`
- `int DestinationY`
- `int DestinationZ`
- `int DistanceToDestination`
- `bool Succeeded`
- `bool IsUnknown`

Constructor defaults:
- `CreatedUtc = DateTime.UtcNow`
- `NearbyEntities = new List<UMGNavigationEntitySnapshot>()`
- `DistanceToNearestLandmark = -1`
- `DistanceToDestination = -1`
- `IsUnknown = true`

Helper methods added:
- `Unknown(string reason = null, string detail = null)`
- `FromPosition(string mapName, int x, int y, int z, string actorId = null, string actorProfileKey = null, string source = null, string reason = null)`
- `WithRegion(UMGNavigationSnapshot snapshot, UMGNavigationRegionResult region)`
- `WithProbe(UMGNavigationSnapshot snapshot, UMGNavigationProbeResult positionProbe)`

## E. Semantics
Actor identity fields:
- `ActorId`
- `ActorProfileKey`

Map/coordinate fields:
- `MapName`
- `X`
- `Y`
- `Z`

Region/probe fields:
- `Region`
- `PositionProbe`

Nearby entity summary field:
- `NearbyEntities`

Landmark placeholder fields:
- `HasNearestLandmark`
- `NearestLandmarkId`
- `NearestLandmarkName`
- `NearestLandmarkKind`
- `DistanceToNearestLandmark`

Destination placeholder fields:
- `HasDestination`
- `DestinationId`
- `DestinationName`
- `DestinationKind`
- `DestinationMapName`
- `DestinationX`
- `DestinationY`
- `DestinationZ`
- `DistanceToDestination`

Timestamp/status/reason fields:
- `SnapshotId`
- `Source`
- `Reason`
- `Detail`
- `CreatedUtc`
- `Succeeded`
- `IsUnknown`

## F. Safety confirmations
- passive model only: **confirmed**
- no real navigation implemented: **confirmed**
- no ServUO map/region reads: **confirmed**
- no pathfinding: **confirmed**
- no movement: **confirmed**
- no world/control mutation: **confirmed**
- no BaseHire dependency: **confirmed**
- no Server.Mobile dependency: **confirmed**
- no StateAccess reference: **confirmed**
- router code not modified: **confirmed**
- movement/execution files not modified: **confirmed**
- parser/skill/companion/action/command files not modified: **confirmed**
- wiki/NL repo not modified: **confirmed**
- UO UMG repo not modified: **confirmed**
- IR glyph work not modified: **confirmed**

## G. Forbidden-string scan result
- result: **PASS**
- findings: none

## H. Build result
- errors: **0**
- warnings: **15**
- warning files:
  - `Scripts\Custom\AIGM\AIGMBridgeClient.cs`
  - `Scripts\Gumps\AIGMResponseGump.cs`
  - `Scripts\Gumps\AIGMQuestionGump.cs`
  - `Scripts\Mobiles\NPCs\AIGMCounselor.cs`

Important warning note:
- warnings do **not** touch `Scripts/Custom/AIGM/Navigation/*`
- warnings do **not** touch `UMGMovementRouter.cs`
- warnings do **not** touch movement/execution files

## I. Cross-repo alignment note
Later NeoUO wiki/NL documentation should explain:
- navigation snapshot semantics
- actor/location/region/probe snapshot meaning
- landmark and destination placeholders
- unknown vs succeeded snapshot interpretation

Later UO UMG block/sleeve/IR semantics may need surfaces for:
- navigation snapshot
- region
- landmark
- route
- stuck
- gate state
- probe state

IR glyph formalization may later need:
- a navigation snapshot glyph or equivalent symbol grouping
- symbols for region, landmark, route, probe, blocked, stuck, dry-run, live execution, tracking scan/report, and pursuit

## J. Recommended next phase
Recommended next phase:
- **Phase 56Q-R5-NAV-SNAPSHOT-MODEL-P — Commit Navigation Snapshot Model**

Suggested commit message:
- `feat: add UMG navigation snapshot model`

Likely phase after commit:
- **Phase 56Q-R5-SERVUO-NAV-READ-ADAPTER-PLAN — Plan first real ServUO navigation read adapter**

Alternative:
- **Phase 56Q-R5-LANDMARK-MODEL — Add landmark model only**
