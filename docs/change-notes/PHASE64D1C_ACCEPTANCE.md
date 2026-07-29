# Phase64D1C Acceptance

Verdict:

`PHASE64D1C_CLASSICUO_COMPANION_PAPERDOLL_SLEEVE_ACCESS_ACCEPTED_SERVER_AUTHORIZED`

## Accepted Scope

Phase64D1C adds a ClassicUO paperdoll launcher for server-marked AIGM companions.

- The implementation is source-only and limited to `PaperdollGump.cs`.
- The launcher uses the accepted AIGM backpack marker contract.
- The launcher sends only ordinary speech command text.
- Server-side Sleeve access remains authoritative.
- No custom packet was added.
- No server-side tactical dispatch was added.
- No autonomous item use was added.

## Build Evidence

- Pinned upstream commit: `a7cf920e42436ce62b9f26b846da8665c9fd3364`
- Release/NativeAOT publish result: 0 errors.
- Warning comparison: no new warning codes or messages from NeoUO changes.
- Final `cuo.dll`: `722D511EC94B6C6C10989454BEE3659E7E61075E27CB5C7B589E05100DED09FD`
- Final `cuo.pdb`: `9F759AAA665A5807ED9146D1A1FA55848C5AB6D594363B8A607CEBB8AD833BE1`
- Live `ClassicUO.exe` remained unchanged: `107795FC907BDAF667F8948258948FF15E77735BBB5C4E123D70D7291844E07C`

## Runtime Evidence

- Player paperdoll: unchanged; no UMG Sleeve scroll.
- Ordinary NPC paperdoll: no broad UMG scroll.
- Marker-positive AIGM companions: profile scroll plus one UMG Sleeve scroll.
- Tooltip: `UMG Sleeve`.
- Hewla scroll activation: command-source sleeve access for `0x00004DD9`, server opened.
- Primary command proof also covered Druss `0x00000304`, Dardalion `0x00000193`, Miriel `0x00002AA5`, and Durmast `0x00003575`.
- Dynamic marker refresh: scroll removed on temporary marker hue invalidation and returned once after hue restore.

## Deployment Evidence

The client was deployed through a reversible lane.

- Isolated test copy: `ClassicUO-Phase64D1C-Test`.
- Live deployment replaced only `cuo.dll` and `cuo.pdb`.
- `ClassicUO.exe` was not replaced.
- Rollback copy was preserved before live replacement.
- Normal local launcher connected after deployment.

## Server Invariants

- `Scripts.dll`: `4AC823D6532C723FC4ADD0A128E2FBE35A1A4C8F3E09DE0101832090AA7F3FF0`
- `versions_v2.json`: `0EB27E13320CDC327597662334D8220F87DD47C94F5011DB2867E93A8D1D6C2F`
- Companion roster remained 22 of 22 normalized.
- Preview remained dry-run/no-dispatch.
- No assignment or UMG sidecar write occurred during paperdoll scroll proof.

## Publication Controls

This repository records source, patch, commands, hashes, and sanitized evidence summaries only. It excludes built DLLs, EXEs, PDBs, private audit ZIPs, accounts, saves, profile/settings files, screenshots, logs, credentials, and runtime sidecars.
