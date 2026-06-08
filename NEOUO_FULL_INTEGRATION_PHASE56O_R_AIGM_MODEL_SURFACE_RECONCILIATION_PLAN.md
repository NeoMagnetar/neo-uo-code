# NEOUO FULL INTEGRATION PHASE56O-R AIGM MODEL SURFACE RECONCILIATION PLAN

## Section 1 — Current State

- target path: `C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg`
- branch: `neo/staging-aigm`
- HEAD: `feefa82bde7bb5cb9987eed29ea090e9a123b3a2`
- current changed files summary:
  - `Scripts/Custom/AIGM/IAIGMActor.cs`
  - `Scripts/Custom/AIGM/IAIGMInventoryCapability.cs`
  - `Scripts/Custom/AIGM/AIGMCounselorInventoryCapability.cs`
  - `Scripts/Custom/AIGM/AIGMExecutionContext.cs`
  - report artifacts in repo root
- build command used:
  - `dotnet build .\ServUO.sln -v:minimal`
- build error count:
  - `42`
- full build output saved to:
  - `C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg\NEOUO_FULL_INTEGRATION_PHASE56O_R_BUILD_ERRORS.txt`

## Section 2 — Error Classification Table

| Missing symbol/member | Referenced by | Error group | Likely preserved Dev source | Correction class | Recommended patch strategy |
|---|---|---|---|---|---|
| `AIGMConversationContext` | `AIGMBridgeClient`, `AIGMSessionState` consumers | Request / Conversation / Execution | `Scripts/Custom/AIGM/AIGMConversationContext.cs` | A | include in minimal core model patch |
| `AIGMSessionState.ActiveTaskSummary` | bridge/session consumers | Session State | `Scripts/Custom/AIGM/AIGMSessionState.cs` | B/C | reconcile target file with preserved Dev members |
| `AIGMSessionState.LastWorldSummary` | bridge/session consumers | Session State | `Scripts/Custom/AIGM/AIGMSessionState.cs` | B/C | reconcile target file with preserved Dev members |
| `AIGMSessionState.Conversation` | bridge/session consumers | Session State | `Scripts/Custom/AIGM/AIGMSessionState.cs` | B/C | reconcile target file with preserved Dev members |
| `AIGMSessionState.AddTurn` | bridge/session consumers | Session State | `Scripts/Custom/AIGM/AIGMSessionState.cs` | B/C | reconcile target file with preserved Dev members |
| `AIGMRequest.Execution` | `AIGMBridgeClient` | Request / Conversation / Execution | `Scripts/Custom/AIGM/AIGMRequest.cs` | C | narrow AIGMRequest patch or extraction phase |
| `AIGMRequest.Conversation` | `AIGMBridgeClient` | Request / Conversation / Execution | `Scripts/Custom/AIGM/AIGMRequest.cs` | C | narrow AIGMRequest patch or extraction phase |
| `AIGMResponse.Plan` | bridge/action consumers | Response / Proposal | `Scripts/Custom/AIGM/AIGMResponse.cs` | B/C | reconcile target response model |
| `AIGMActionProposal.Title` | bridge/action consumers | Response / Proposal | `Scripts/Custom/AIGM/AIGMActionProposal.cs` | B/C | reconcile target proposal model |
| `AIGMCounselorManagementGump` | counselor surfaces | Counselor / Gump Support | `Scripts/Gumps/AIGMCounselorManagementGump.cs` (or equivalent preserved Dev location) | A/D | defer to support/UI patch unless hard dependency remains |
| `AIGMProposalAugmenter` | counselor / proposal support | Counselor / Gump Support | preserved Dev AIGM support surface | A/D | defer if possible |
| `AIGMExecutionLog` | counselor / execution support | Execution Logging | preserved Dev execution logging surface | A/D | defer if possible |
| `AIGMTargetInfo.Distance` | target enrichment consumers | Target Info Enrichment | `Scripts/Custom/AIGM/AIGMTargetInfo.cs` | B/C | reconcile target info model |
| `AIGMTargetInfo.IsContainer` | target enrichment consumers | Target Info Enrichment | `Scripts/Custom/AIGM/AIGMTargetInfo.cs` | B/C | reconcile target info model |
| `AIGMTargetInfo.IsDoor` | target enrichment consumers | Target Info Enrichment | `Scripts/Custom/AIGM/AIGMTargetInfo.cs` | B/C | reconcile target info model |
| `AIGMTargetInfo.IsStatic` | target enrichment consumers | Target Info Enrichment | `Scripts/Custom/AIGM/AIGMTargetInfo.cs` | B/C | reconcile target info model |
| `AIGMTargetInfo.IsMovable` | target enrichment consumers | Target Info Enrichment | `Scripts/Custom/AIGM/AIGMTargetInfo.cs` | B/C | reconcile target info model |
| `AIGMTargetInfo.IsAlive` | target enrichment consumers | Target Info Enrichment | `Scripts/Custom/AIGM/AIGMTargetInfo.cs` | B/C | reconcile target info model |
| `AIGMTargetInfo.ParentTypeName` | target enrichment consumers | Target Info Enrichment | `Scripts/Custom/AIGM/AIGMTargetInfo.cs` | B/C | reconcile target info model |
| `AIGMTargetInfo.IsPlayer` | target enrichment consumers | Target Info Enrichment | `Scripts/Custom/AIGM/AIGMTargetInfo.cs` | B/C | reconcile target info model |
| `AIGMTargetInfo.IsNpc` | target enrichment consumers | Target Info Enrichment | `Scripts/Custom/AIGM/AIGMTargetInfo.cs` | B/C | reconcile target info model |
| `AIGMTargetInfo.IsVendor` | target enrichment consumers | Target Info Enrichment | `Scripts/Custom/AIGM/AIGMTargetInfo.cs` | B/C | reconcile target info model |
| `AIGMTargetInfo.Tags` | target enrichment consumers | Target Info Enrichment | `Scripts/Custom/AIGM/AIGMTargetInfo.cs` | B/C | reconcile target info model |

### Correction class key
- **A** = missing whole file from preserved Dev
- **B** = existing target file is stale / missing members
- **C** = existing target file has divergent API shape
- **D** = error belongs to a support/UI subsystem that may be deferred

## Section 3 — Model Surface Dependency Map

### Request / Conversation / Execution
- `AIGMRequest`
- `AIGMConversationContext`
- `AIGMExecutionContext`

### Session State
- `AIGMSessionState`
- member enrichment:
  - `ActiveTaskSummary`
  - `LastWorldSummary`
  - `Conversation`
  - `AddTurn`

### Response / Proposal
- `AIGMResponse`
- `AIGMActionProposal`
- member enrichment:
  - `Plan`
  - `Title`

### Counselor / Gump Support
- `AIGMCounselorManagementGump`
- `AIGMProposalAugmenter`

### Execution Logging
- `AIGMExecutionLog`

### Target Info Enrichment
- `AIGMTargetInfo`
- member enrichment:
  - `Distance`
  - `IsContainer`
  - `IsDoor`
  - `IsStatic`
  - `IsMovable`
  - `IsAlive`
  - `ParentTypeName`
  - `IsPlayer`
  - `IsNpc`
  - `IsVendor`
  - `Tags`

## Section 4 — Minimal Patch Strategy

Recommended option:
- **Option 2: split into two patches**

### Phase 56O-S1 — Core model contract only
Include only the smallest coherent non-UI model layer necessary to satisfy the bridge/runtime compile path:
- `AIGMConversationContext`
- targeted `AIGMRequest` reconciliation
  - `Execution`
  - `Conversation`
- targeted `AIGMSessionState` reconciliation
  - `ActiveTaskSummary`
  - `LastWorldSummary`
  - `Conversation`
  - `AddTurn`
- targeted `AIGMResponse` / `AIGMActionProposal` member reconciliation
  - `Plan`
  - `Title`
- targeted `AIGMTargetInfo` enrichment
  - add only missing members required by current compile

### Phase 56O-S2 — Counselor / gump support
Defer these until after core model contract is reconciled, unless S1 proves they are unavoidable immediate compile dependencies:
- `AIGMCounselorManagementGump`
- `AIGMProposalAugmenter`
- `AIGMExecutionLog`
- related counselor/UI support surfaces

### Why split this way
- keeps the model/API contract separate from UI/support layer churn
- avoids dragging counselor/gump code into the first reconciliation patch if not needed
- lets us answer whether the full integration target can be greened with only model-layer alignment first

## Section 5 — Existing patch bundle coverage (read-only comparison)

Observed from existing preserved Dev patch bundles:
- `aigm-core-command-runtime.patch` contains some request/intent/action surfaces but is too broad for this phase
- `aigm-bridge-memory-speech.patch` contains bridge-side references and likely assumes the model surfaces already exist
- `aigm-movement-travel-tracking.patch` is unrelated to the current blocker and should remain deferred
- `aigm-services.patch` is not the right place to solve these model/API mismatches
- `companions-dakeyras-danyal-basehire.patch` is unrelated to the current blocker
- `servuo-embedded-umg-surfaces.patch` is also not the right place to solve the current compile gap

Conclusion:
- the missing model surfaces are partially represented across existing patches, but importing whole subsystem patches now would be too broad
- a **minimal extracted model-contract patch** is safer than applying an existing broad subsystem patch

## Section 6 — Explicit Non-Actions

Confirmed in this phase:
- no subsystem patches applied
- no wholesale `AIGMRequest.cs` replacement
- no server restart
- no runtime bundle loader work
- no Production-Preserved-Original mutation

## Section 7 — Recommended Next Phase

Recommended next phase:
- **Phase 56O-S1 — Minimal AIGM Core Model Contract Patch**

That patch should stay narrow and target only the model/API reconciliation needed to unblock baseline compile, leaving counselor/gump support to a later follow-up unless directly required.

## Bottom line

The current red baseline is no longer an abstract missing-type problem. It is a coherent **AIGM model surface mismatch** problem. The safest next move is a narrow core model contract reconciliation patch, not a broad subsystem patch.
