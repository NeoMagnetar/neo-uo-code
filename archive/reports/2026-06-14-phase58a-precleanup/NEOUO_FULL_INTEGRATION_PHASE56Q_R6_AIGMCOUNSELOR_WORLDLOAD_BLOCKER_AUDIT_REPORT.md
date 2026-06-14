# NEOUO FULL INTEGRATION PHASE56Q R6 AIGMCOUNSELOR WORLDLOAD BLOCKER AUDIT REPORT

- selected session/agent: ultima-online
- workspace path: C:\.openclaw\workspace-ultima-online
- target repo path: C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg
- branch: neo/staging-aigm
- HEAD: 4ddcccac9
- build result: success (0 warnings, 0 errors)

## Current AIGMCounselor Serialization Summary
- class namespace: `Server.Mobiles`
- base class: `PlayerVendor`
- constructor signatures:
  - `[Constructable] public AIGMCounselor() : base(null, null)`
  - `public AIGMCounselor(Serial serial) : base(serial)`
- `Serialize` method:
  - calls `base.Serialize(writer)`
  - writes version integer `0`
- `Deserialize` method:
  - calls `base.Deserialize(reader)`
  - reads version integer via `int version = reader.ReadInt();`
  - does not branch on version
  - then resets runtime presentation/state fields directly:
    - `Name`
    - `Title`
    - `Female`
    - `Body`
    - `Hue`
    - `SpeechHue`
    - `Blessed`
    - `CantWalk`
    - `VendorSearch`
    - `HoldGold`
    - `BankAccount`
  - schedules `EnsureCounselorPresentation` using `Timer.DelayCall(TimeSpan.Zero, EnsureCounselorPresentation)`
- fields read/write order:
  - write: only version `0` after base
  - read: only version int after base
- any recent changes that could break saved object loading:
  - relative to old Dev, the current integration added companion-boundary classification logic in `OnSpeech`
  - serialization shape itself appears materially unchanged
  - no namespace move, no base-class change, no constructor signature change, and no extra serialized fields were introduced in the current code compared to old Dev

## Old Dev AIGMCounselor Comparison
Old Dev file found and inspected:
- path: `C:\UO\Server\Neo Ultima Online\NeoUO-Dev\Scripts\Mobiles\NPCs\AIGMCounselor.cs`

Comparison result:
- serialize version: same (`0`)
- deserialize version handling: same (read int, no version branching)
- fields read/write order: same (base serialization + one version int)
- base class: same (`PlayerVendor`)
- constructor signatures: same (`base(null, null)` and `base(serial)`)

Conclusion from code comparison:
- this does **not** look like a straightforward current-vs-Dev serialization version mismatch
- the current companion/counselor boundary work changed `OnSpeech`, but not the persisted data layout in `Serialize`/`Deserialize`

## Save / World Folder Inventory
- save folder exists: yes
- backups folder exists: yes
- save/world directories present under current integration repo:
  - `Saves`
  - `Backups`
- latest observed save timestamp in `Saves`: `2026-05-28 14:46:28`
- latest observed backup timestamps:
  - `Backups\Automatic`: `2026-05-28 14:46:28`
  - `Backups\Crashed`: `2026-04-10 14:37:40`
- save structure indicates world/object persistence is present:
  - `Saves\Mobiles`
  - `Saves\Items`
  - other standard save subfolders
- whether object files appear to include mobile/world saves: yes

## Exact Startup Prompt / Error if Captured
Previously observed at runtime during manual startup attempt:
- object type: `Server.Mobiles.AIGMCounselor`
- serial: `0x00ee4701`
- prompt: `Delete the object? (y/n)`

Not captured in this audit:
- exact exception type
- stack trace
- field/version failure line

Reason not captured:
- this audit intentionally avoided desktop automation/OCR/window tooling
- operator console paste is preferred for exact runtime exception capture

## Likely Cause Classification
Current best classification:
- `G. Unknown; needs full console stack trace`

Secondary likelihood notes:
- `A. Serialization version mismatch`: low confidence based on current-vs-Dev code comparison; serialization version and read/write shape match
- `B. Field read/write order mismatch`: low confidence for the same reason
- `C. Base class mismatch`: low confidence; base class matches Dev
- `D. Deleted/moved type or namespace mismatch`: low confidence; type and namespace still exist as `Server.Mobiles.AIGMCounselor`
- `E. Constructor/deserialization exception`: plausible
- `F. Save file from old Dev incompatible with current integration`: plausible at the broader runtime/object graph level, even if the counselor file itself matches, because base/contained object state or dependent object expectations may differ

Most likely practical reading right now:
- the saved `AIGMCounselor` instance or something reachable during its load/presentation path is failing during world load, but the direct `AIGMCounselor` serialize/deserialize code alone does not explain it

## Safe Recovery Options Evaluated
### Option 1 — Serialization compatibility repair
- patch `AIGMCounselor.Deserialize` only after exact exception/stack trace is captured
- preserve saved object
- preferred if this saved counselor instance matters and if the failure is truly deserialization-time compatible code
- current blocker: not enough exact runtime exception detail yet

### Option 2 — Backup Saves, then delete only the failed saved counselor object
- back up current `Saves` first
- answer `y` only after explicit user approval in a dedicated phase
- allows server to continue without that counselor instance
- counselor can be respawned later if needed
- risk: mutates world state and may discard context tied to that object

### Option 3 — Clean test world
- create/use a disposable test save/world for runtime UX validation
- avoids mutating the real current save state
- useful if natural speech UX testing is the immediate priority
- strong candidate if preserving the current world matters more than preserving this exact test path

### Option 4 — Defer runtime test
- do not continue current integration startup until compatibility is understood/fixed
- safest if production-like world state must remain untouched

## Recommended Recovery Option
Recommended now:
- **Option 3 — Clean test world** if the immediate goal is to validate companion natural speech UX without risking current save state

Why:
- current phase goal was runtime speech UX testing, not world-save recovery
- the direct counselor serialization code does not obviously explain the failure
- answering `y` would mutate world state before we fully understand the failure
- a clean/disposable test world gives the fastest safe path to continue UX validation

Secondary recommendation:
- if preserving and reusing the current saved counselor matters, next do a dedicated trace-first repair phase before any deletion decision:
  - capture the full console exception text manually
  - then decide between compatibility repair vs backup-and-delete-object

## Risk of Answering `y`
- permanently mutates current world state by deleting the failed saved counselor object
- may lose any world placement/context attached to that instance
- may hide a broader compatibility issue by treating the symptom only
- may allow startup to continue, but without proving underlying cause

## Risk of Answering `n`
- likely prevents startup from proceeding cleanly
- runtime UX testing remains blocked
- preserves the failed state for later analysis, which is good for diagnosis

## Whether Clean Test World Is Recommended
- yes, recommended if the primary objective is to proceed with natural speech UX runtime testing safely and quickly

## Confirmations
- confirmation no y/n prompt was answered: yes
- confirmation no Saves files changed: yes
- confirmation no code files changed: yes
- confirmation no staging: yes
- confirmation no commit: yes
- confirmation no desktop automation/AHK/OCR/window-control tools were used: yes

## Recommendation for Next Phase
Primary recommendation:
- `PHASE 56Q-R6-CLEAN-RUNTIME-TEST-WORLD-PREP`

Alternative if preserving current world/counselor instance is more important:
- `PHASE 56Q-R6-AIGMCOUNSELOR-DESERIALIZE-COMPAT-REPAIR`

Alternative if user explicitly prefers delete-after-backup once backup procedure is locked:
- `PHASE 56Q-R6-AIGMCOUNSELOR-SAVE-BACKUP-AND-DELETE-OBJECT`
