# Neo UO Code

This repository is the implementation repository for the hardcore one-life Felucca shard.

It is intended to hold the ServUO codebase or imported server working tree, shard-specific custom scripts, managed configuration work, technical implementation notes, controlled tooling, and test support.

This is not the primary project-memory repository. Long-form planning, publish logs, governance, task history, and broader operational memory belong in the HQ repo.

## Purpose

This repo exists to hold the implementation surface of the project.

It is meant to preserve:
- controlled code changes
- configuration discipline
- technical documentation
- clean separation from project planning
- reproducible implementation work
- future automation support

## Repository rules

- Read this README first.
- Treat the HQ repo as the source of truth for planning, governance, and active task intent.
- Do not assume this repo alone explains the full project context.
- Do not commit secrets, credentials, tokens, or private keys.
- Do not commit unmanaged runtime backups or random save archives.
- Do not dump scratch files or temporary clutter into the working tree.
- Keep shard-specific custom work clearly separated from upstream or imported server code.
- Prefer controlled structure over improvisation.

## Primary usage

Use this repo for:
- ServUO source import or mirror
- shard-specific code changes
- custom scripts
- managed config work
- technical implementation notes
- build/run support
- controlled developer tooling
- test support

## Directory intent

### `server`
Reserved for the actual ServUO codebase or equivalent implementation root.

### `custom/scripts`
Shard-specific custom scripts, overrides, and implementation additions.

### `custom/config`
Managed configuration files, config overlays, or environment-specific config notes.

### `custom/assets`
Controlled asset-side materials if they are later tracked here.

### `docs/architecture`
Code-relevant architecture notes that directly support implementation.

### `docs/implementation-notes`
Technical notes, caveats, assumptions, and implementation reminders.

### `docs/change-notes`
Technical change notes tied to implementation work.

### `ops/build`
Build instructions, build notes, and controlled build helpers.

### `ops/run`
Run/start instructions and environment-specific run notes.

### `ops/restore`
Restore and rollback notes for the implementation environment.

### `tools`
Controlled helper scripts, automation, and developer support tooling.

### `tests`
Test support, test notes, and future validation materials.

## OpenClaw startup behavior

When OpenClaw is pointed at this repo, it should:
1. read this README first
2. read the relevant active task packet from the HQ repo
3. read any directly relevant technical notes in this repo
4. work only inside the implementation surface needed for the assigned task
5. avoid inventing undocumented structure unless explicitly instructed
6. report meaningful completed work back into the HQ repo after implementation

## Bootstrap note

This repo is intentionally scaffold-only at initial setup.

The directory structure should exist, but substantive code population should happen only when explicitly directed.
Do not auto-populate the repo beyond the root README and `.gitkeep` placeholders unless instructed.

## Current Publication State

This publication branch began at Phase64C1E and is now cumulative through the accepted Phase64D1B baseline.

Included implementation surface:
- Phase64C2 UMG Composer authoring preview, version rollback, and JSON encoding repair.
- Phase64D1A server-side companion Sleeve access service, context-menu entry, and read-only Sleeve Selector Gump.
- Phase64D1B normalized AIGM companion backpacks, inventory registry/service, marker contract, inventory commands, and lifecycle protections.

Runtime authority remains server-side. The companion backpack marker is a client discovery hint only:
- Layer: `Layer.Backpack`
- ItemID: `0x0E75`
- Hue: `1175`
- MarkerVersion: `1`
- Server class: `Server.Custom.AIGM.Inventory.AIGMCompanionBackpack`

Current accepted hashes:
- `Scripts.dll`: `4AC823D6532C723FC4ADD0A128E2FBE35A1A4C8F3E09DE0101832090AA7F3FF0`
- `versions_v2.json`: `0EB27E13320CDC327597662334D8220F87DD47C94F5011DB2867E93A8D1D6C2F`

Publication controls:
- No saves, accounts, live world state, private UMG sidecars, audit ZIPs, build binaries, logs, credentials, tokens, or temporary build output are tracked.
- Tactical dispatch remains disabled.
- Autonomous item use is not implemented.
