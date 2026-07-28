# Phase64D1A Companion Sleeve Access Foundation

Verdict:
`PHASE64D1A_COMPANION_SLEEVE_ACCESS_FOUNDATION_ACCEPTED_PREVIEW_ONLY_WITH_AUTHORIZED_NONGM_RANGE_PROOF_DEFERRED`

Closure:
`DEFERRED.D1A.AUTHORIZED_NONGM_RANGE_MATRIX_CLOSED`

Accepted scope:
- Added `[umgsleeve <serial|name>]` server command.
- Added server-side companion resolution by exact hex serial, exact decimal serial, and unique companion name.
- Added rejection for missing selector, ambiguous names, non-AIGM targets, invalid actors, unauthorized callers, map mismatch, and out-of-range callers.
- Added context-menu access through the same server validation path.
- Added read-only Skills-style Sleeve Selector Gump.
- Preserved PreviewOnly behavior. No tactical dispatch was enabled.

Authority boundary:
- ServUO remains the identity and authorization authority.
- Client entry points only ask the server to open the Gump.
- The server validates registration, caller authority, range, map, and actor state before opening.

Private audit reference:
- `PHASE64D1A_COMPANION_SLEEVE_ACCESS_FOUNDATION_20260727-1351.zip`
- SHA-256: `7D647A76BF995E23558480E283723D17388E4504465CFA5D90C17A2FC24A3074`
