# Arena Rule Legend

## Purpose

This subsystem is a stable documentation surface for arena-rule design in Neo UO code. It serves three purposes at once:

1. a clean fallback reference for the agent
2. a navigable design legend for future arena systems
3. an implementation-aware progress ledger that can accumulate code paths, status, notes, and completed work over time

It is intentionally optimized for AI retrieval and update discipline: many small files, stable chapter ordering, predictable naming, and one rule phrase per file.

## Navigation

- Start with `manifest.md` for the full tree and canonical file list.
- Use `implementation_status.md` for a fast rollup of where work stands.
- Use `verb_index.md` to retrieve rules by behavioral family such as `allow_*`, `deny_*`, `block_*`, `clear_*`, or `reset_*`.
- Open a chapter `_index.md` when you know the rule domain but not the exact rule file.
- Open an individual rule file when working on one specific command phrase.

## Naming rules

- Chapter folders use fixed numeric prefixes so order never drifts.
- Each chapter contains `_index.md` plus one rule file per command phrase.
- Rule files use lower snake case and preserve the leading verb in the filename.
- Each rule file preserves the authoritative human-readable command phrase inside the file body.
- File naming collisions are resolved with a short suffix while preserving clarity.

## Update rules for agents and contributors

- Do not merge multiple command phrases into one file.
- Do not move rules between chapters unless the corpus itself is corrected.
- Update `implementation_status.md` whenever a rule status changes.
- Update `verb_index.md` and `manifest.md` after structural or rule-file additions.
- Record meaningful structural or status changes in `change_log.md`.
- Prefer additive progress notes over destructive rewrites.
- Keep `Status` values inside rule files and status rollups limited to: `legend_only`, `planned`, `in_progress`, `implemented`, `tested`, `blocked`, `deprecated`, and `replaced`.

## Current scope

This initial version is documentation scaffolding plus command-corpus ingestion only. It anticipates future implementation tracking, test notes, code touchpoints, and completion progress without requiring future file-layout changes.
