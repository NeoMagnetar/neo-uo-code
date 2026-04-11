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
