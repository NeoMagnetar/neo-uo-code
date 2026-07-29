# Phase64D1B Companion Inventory And Marker

Verdict:
`PHASE64D1B_COMPANION_INVENTORY_NORMALIZED_AND_CLIENT_MARKER_ACCEPTED_NO_AUTONOMOUS_ITEM_USE`

Related closure:
`DEFERRED.D1A.AUTHORIZED_NONGM_RANGE_MATRIX_CLOSED`

Deferred to D1C:
`STOCK_CLASSICUO_COMPANION_MARKER_VISIBILITY_DEFERRED_TO_PHASE64D1C`

Accepted scope:
- Added `AIGMCompanionBackpack` as the controlled companion backpack class.
- Added companion inventory service, registry, and log helpers.
- Added inventory commands for open, audit, ensure, migrate, and normalize-all workflows.
- Added Backpack button/status integration to the Sleeve Selector Gump.
- Added death and cleanup protections through companion-class overrides.
- Normalized 22 registered live AIGM companions to exactly one AIGM backpack each.

Marker contract:
- Layer: `Layer.Backpack`
- ItemID: `0x0E75`
- Hue: `1175`
- MarkerVersion: `1`
- Server class: `Server.Custom.AIGM.Inventory.AIGMCompanionBackpack`

Accepted runtime proof:
- Registered live roster: 22.
- Normalized AIGM backpacks: 22.
- Unique valid backpack serials: 22.
- Current `Scripts.dll` SHA-256: `4AC823D6532C723FC4ADD0A128E2FBE35A1A4C8F3E09DE0101832090AA7F3FF0`.
- Current `versions_v2.json` SHA-256: `0EB27E13320CDC327597662334D8220F87DD47C94F5011DB2867E93A8D1D6C2F`.
- Tactical dispatch disabled.
- Autonomous item use not implemented.

Private audit reference:
- `AIGM_AUDIT_PHASE64D1B_R1_EXISTING_BACKPACK_ADOPTION_20260728-0940.zip`
- SHA-256: `51DE6E9695074A0CD436CDCBB5EC9CC27FED619E9F8FF5F6ED7E0A6990FDA696`
