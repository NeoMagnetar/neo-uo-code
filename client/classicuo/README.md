# ClassicUO Phase64D1C Overlay

This directory publishes the NeoUO ClassicUO client work as a source-only overlay against the pinned upstream source.

## Upstream

- Upstream project: ClassicUO
- Pinned commit: `a7cf920e42436ce62b9f26b846da8665c9fd3364`
- Changed file: `src/ClassicUO.Client/Game/UI/Gumps/PaperdollGump.cs`
- Patch: `client/classicuo/patches/phase64d1c-aigm-paperdoll-sleeve.patch`
- Full overlay file: `client/classicuo/overlays/src/ClassicUO.Client/Game/UI/Gumps/PaperdollGump.cs`

Do not push this work to `ClassicUO/ClassicUO`. Apply it only to the pinned NeoUO client source lane.

## Scope

Phase64D1C adds a second ClassicUO paperdoll scroll for marker-positive AIGM companion paperdolls.

- Player paperdolls are unchanged.
- Ordinary NPC paperdolls are unchanged unless the server equips the accepted marker.
- The profile scroll remains at the existing non-player position.
- The UMG Sleeve scroll uses the existing scroll art and sits immediately beside the profile scroll.
- Double-clicking the scroll says `[umgsleeve 0xXXXXXXXX` through `GameActions.Say`.
- No custom packet is introduced.
- Server-side Sleeve authorization remains authoritative.

## Marker Contract

- Layer: `Layer.Backpack`
- Backpack item id/graphic: `0x0E75`
- Backpack hue: `1175`
- Marker version: `1`
- Server class: `AIGMCompanionBackpack`

The client treats the marker as a discovery hint only. ServUO still decides registration, ownership/authorization, range, map, and whether the Sleeve Selector may open.

## Build

Run from the pinned ClassicUO source root:

```powershell
dotnet publish "src/ClassicUO.Client/ClassicUO.Client.csproj" -c Release -r "win-x64" -o "dist-phase64d1c-client" /p:IS_DEV_BUILD=true /p:AssemblyVersion=1.1.0.301 /p:FileVersion=1.1.0.301 /p:NativeLib=Shared /p:OutputType=Library
```

For the combined output, publish the bootstrap first and the client second, matching the official workflow order.

## Build Result

- Result: 0 errors.
- Upstream baseline warnings: 19 warning lines.
- D1C warning comparison: no new warning codes or messages caused by NeoUO changes.
- `cuo.dll`: `722D511EC94B6C6C10989454BEE3659E7E61075E27CB5C7B589E05100DED09FD`
- `cuo.pdb`: `9F759AAA665A5807ED9146D1A1FA55848C5AB6D594363B8A607CEBB8AD833BE1`
- SourceLink commit: `a7cf920e42436ce62b9f26b846da8665c9fd3364`
- Patch SHA-256: `03326D6CF1EFA2288B56155AE77EAE590BEAF5ECB9563876A4AE56AF0DAEE3C1`
- Overlay source SHA-256: `D3A51334C35783F86A4731FDC8F8ACA22FA86E247452C3A61CB7FB3F0695DBD1`

## Test And Deployment Method

Phase64D1C was first installed into `ClassicUO-Phase64D1C-Test`, using an isolated profile/data lane and preserving the copied original `cuo.dll` and `cuo.pdb`.

After isolated acceptance, the live client was updated by replacing only `cuo.dll` and `cuo.pdb`. `ClassicUO.exe` was not replaced. A rollback copy of the previous live artifacts was preserved before deployment.

Rollback is one command sequence: close ClassicUO, restore the previous `cuo.dll` and `cuo.pdb`, and launch through the normal NeoUO local launcher.

## Acceptance

Accepted verdict:

`PHASE64D1C_CLASSICUO_COMPANION_PAPERDOLL_SLEEVE_ACCESS_ACCEPTED_SERVER_AUTHORIZED`

Boundary confirmations:

- No custom network packet.
- No tactical dispatch.
- No autonomous inventory use.
- No server inventory migration.
- No upstream ClassicUO publication.
- Built DLLs, EXEs, PDBs, profiles, settings, account data, saves, logs, screenshots, and private audit ZIPs are excluded from this repository.
