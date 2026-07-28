# Phase64D1C Sanitized Test Summary

## Baseline

- ServUO listener: `127.0.0.1:2595`.
- Middleware listener: `127.0.0.1:4876`.
- Middleware fallback: `0`.
- Middleware timeout: `0`.
- Middleware last error: `null`.
- `Scripts.dll`: `4AC823D6532C723FC4ADD0A128E2FBE35A1A4C8F3E09DE0101832090AA7F3FF0`
- `versions_v2.json`: `0EB27E13320CDC327597662334D8220F87DD47C94F5011DB2867E93A8D1D6C2F`
- Registered live companions: 22.
- Normalized AIGM companion backpacks: 22.

## Client Build

- Pinned ClassicUO commit: `a7cf920e42436ce62b9f26b846da8665c9fd3364`
- Build result: 0 errors.
- Warning result: no new warning codes or messages caused by D1C.
- `cuo.dll`: `722D511EC94B6C6C10989454BEE3659E7E61075E27CB5C7B589E05100DED09FD`
- `cuo.pdb`: `9F759AAA665A5807ED9146D1A1FA55848C5AB6D594363B8A607CEBB8AD833BE1`

## Isolated Test Lane

- Built D1C client deployed first to a separate `ClassicUO-Phase64D1C-Test` lane.
- Original copied `cuo.dll` and `cuo.pdb` were preserved inside the test lane.
- Test profile/data paths were isolated before launch.
- Normal live client folder was not altered until isolated proof passed.

## UI Proof

- Player paperdoll stayed unchanged: profile scroll and party-manifest scroll remained; no UMG Sleeve scroll appeared.
- Ordinary NPC checks did not show broad marker leakage.
- Marker-positive companion paperdolls showed the original profile scroll plus one UMG Sleeve scroll.
- Tooltip proof showed `UMG Sleeve`.
- No duplicate scroll was observed.
- No paperdoll corruption, flicker loop, client crash, reconnect loop, or profile corruption was observed.

## Scroll Command Proof

The client scroll sent normal speech command text; the server opened the Sleeve Selector only after authorization.

- Hewla: `0x00004DD9`
- Druss: `0x00000304`
- Dardalion: `0x00000193`
- Miriel: `0x00002AA5`
- Durmast: `0x00003575`

The Sleeve Selector remained PreviewOnly with dispatch disabled.

## Dynamic Refresh Proof

Hewla's already-open paperdoll was used for reversible marker refresh proof:

- Existing backpack serial: `0x4002360F`.
- Temporary command: `Serial 0x4002360F Set Hue 0`.
- Result: UMG Sleeve scroll disappeared.
- Restore command: `Serial 0x4002360F Set Hue 1175`.
- Result: exactly one UMG Sleeve scroll returned.

The backpack was not removed or replaced.

## Security Boundary

The server remains authoritative. The marker is display-only.

- Unauthorized Player-level access remained denied under the existing reversible D1B proof fixture.
- The scroll path did not bypass registration, authorization, range, or map checks.

## Regression

- Direct Sleeve command access still worked.
- Context-menu access still worked.
- Backpack button remained available.
- Preview remained dry-run/no-dispatch.
- No tactical adapter executed.
- No autonomous item use occurred.
- Dialogue and movement behavior remained operational.
