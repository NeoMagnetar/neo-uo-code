# Phase64D1C ClassicUO Paperdoll Sleeve Access

## Objective

Phase64D1C connects the accepted server-side AIGM Sleeve access system to ClassicUO paperdolls without weakening the server boundary.

The client does only one thing: when a non-player paperdoll belongs to a mobile with the accepted AIGM companion backpack marker, it displays a second scroll beside the existing profile scroll. Double-clicking that scroll says the normal `[umgsleeve 0xXXXXXXXX` command for the paperdoll mobile serial.

## Source Boundary

Only the pinned ClassicUO source lane was modified.

- Pinned source commit: `a7cf920e42436ce62b9f26b846da8665c9fd3364`
- Modified file: `src/ClassicUO.Client/Game/UI/Gumps/PaperdollGump.cs`
- Publication patch: `client/classicuo/patches/phase64d1c-aigm-paperdoll-sleeve.patch`
- Publication overlay: `client/classicuo/overlays/src/ClassicUO.Client/Game/UI/Gumps/PaperdollGump.cs`

Protected files were not modified:

- ClassicUO `PacketHandlers.cs`
- ClassicUO `Profile.cs`
- ServUO `PlayerMobile.cs`
- ServUO `Profile.cs`
- ServUO `PacketHandlers.cs`
- `AIGMCompanionMemoryLoader.cs`
- `AIGMCompanionTurnCoordinator.cs`

## Detection Rule

A paperdoll is marker-positive only when all of these are true:

- `LocalSerial` is not the player serial.
- The mobile exists in the client world.
- The mobile has an equipped item on `Layer.Backpack`.
- The equipped backpack has item id/graphic `0x0E75`.
- The equipped backpack hue normalizes to `1175`.

The client does not inspect name, title, body, notoriety, tooltip, or backpack contents. The client does not read `MarkerVersion`.

## UI Behavior

Non-player marker-positive paperdolls keep the original profile scroll and add one UMG Sleeve scroll:

- Profile scroll: existing non-player position, `X 25`, `Y 196`.
- UMG Sleeve scroll: `X 39`, `Y 196`.
- Scroll graphic: `0x07D2`.
- Spacing: `14`.
- Tooltip: `UMG Sleeve`.

Player paperdolls keep the profile scroll and party-manifest scrolls. They do not receive the UMG Sleeve scroll.

## Refresh Behavior

The paperdoll refresh helper is idempotent:

- Adds the UMG scroll once when the marker appears.
- Removes the UMG scroll when the marker disappears.
- Does not create duplicate controls.
- Preserves the existing paperdoll and event subscriptions.
- Avoids a full paperdoll rebuild during equipment updates.

Live refresh proof used Hewla's existing companion backpack `0x4002360F` while the paperdoll remained open:

- Setting hue to `0` removed the UMG scroll.
- Restoring hue to `1175` returned exactly one UMG scroll.
- The backpack was not removed or replaced.

## Command Path

On left double-click, the client re-fetches the mobile, revalidates the marker, then sends:

```text
[umgsleeve 0xXXXXXXXX
```

The command is sent through `GameActions.Say`. The server still decides registration, authorization, range, map, and whether the Sleeve Selector opens.

Live proof recorded the paperdoll scroll for Hewla sending:

```text
umgsleeve 0x00004DD9
```

The UMG runtime log recorded the matching server result as command-source, accepted, and opened by server authorization.

## Server Baseline

- `Scripts.dll`: `4AC823D6532C723FC4ADD0A128E2FBE35A1A4C8F3E09DE0101832090AA7F3FF0`
- `versions_v2.json`: `0EB27E13320CDC327597662334D8220F87DD47C94F5011DB2867E93A8D1D6C2F`
- UMG records: 10 total, 8 Druss, 1 Dardalion, 1 Miriel.
- Companion inventory: 22 registered live companions, 22 normalized AIGM backpacks.
- Tactical dispatch: disabled.
- Autonomous inventory use: not implemented.

## Security Boundary

The client marker is visibility only. It grants no access.

Security proof retained the existing reversible D1B Player-level fixture:

- Authorized control access remained allowed.
- Unrelated Player-level access remained denied.
- The client may display marker-positive UI, but the server remains the identity and authorization authority.

## Exclusions

This publication does not include client binaries, profile/settings data, account data, saves, logs, private screenshots, runtime sidecars, or audit ZIPs.
