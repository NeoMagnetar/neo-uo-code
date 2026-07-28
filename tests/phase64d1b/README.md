# Phase64D1B Sanitized Test Summary

Verdict:
`PHASE64D1B_COMPANION_INVENTORY_NORMALIZED_AND_CLIENT_MARKER_ACCEPTED_NO_AUTONOMOUS_ITEM_USE`

Sanitized acceptance summary:
- 22 registered live AIGM companions were present.
- 22 registered live AIGM companions had exactly one normalized AIGM backpack.
- All accepted backpack markers matched `Layer.Backpack`, `0x0E75`, hue `1175`, marker version `1`.
- Authorized normal-player within-range access was allowed in temporary proof.
- Unauthorized, out-of-range, other-map, and non-AIGM access was rejected.
- Temporary death and resurrection proof retained backpack contents off corpse.
- Temporary proof actor, accounts, backpack, and proof items were cleaned up.
- Sleeve command access, context-menu access, Backpack button access, dialogue, and PreviewOnly behavior regressed cleanly.
- UMG sidecar hash diffs were 0.
- Tactical dispatch did not execute.
- Autonomous item use was not implemented.

Accepted hashes:
- `Scripts.dll`: `4AC823D6532C723FC4ADD0A128E2FBE35A1A4C8F3E09DE0101832090AA7F3FF0`
- `versions_v2.json`: `0EB27E13320CDC327597662334D8220F87DD47C94F5011DB2867E93A8D1D6C2F`
- Phase audit ZIP: `AIGM_AUDIT_PHASE64D1B_R1_EXISTING_BACKPACK_ADOPTION_20260728-0940.zip`
- Audit ZIP SHA-256: `51DE6E9695074A0CD436CDCBB5EC9CC27FED619E9F8FF5F6ED7E0A6990FDA696`
