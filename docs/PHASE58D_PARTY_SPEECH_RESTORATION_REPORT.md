# PHASE58D Party Speech Restoration Report

## Summary

Phase58D restores old Dev-style party hearing and bounded companion dialogue under the current UMG shell.

The core change is that speech routing now builds a party context instead of collapsing every owner message into one rotating visible responder. Direct named commands still select one primary companion, while group speech can select up to three in-character responders and companion-to-companion dialogue is limited to one follow-up chain.

## Implemented

- Added `AIGMCompanionPartySpeechContext` to record owner speaker, raw message, addressed companion, group addressing, listener set, selected responders, suppressed responders/reasons, dialogue mode, parsed intent, turn decision, and state context.
- Reworked `AIGMCompanionTurnCoordinator` around these modes:
  - `DirectNamedCommand`
  - `GroupCommand`
  - `GroupConversation`
  - `CompanionToCompanion`
  - `StateCommentary`
  - `SystemStatus`
- Updated companion speech relay handling in Dakeyras, Danyal, and Dardalion so relayed owner/group/companion speech uses coordinator-selected visibility.
- Direct named owner commands now relay context silently to non-addressed companions without allowing them to execute or visibly answer.
- Group-addressed owner speech can select Dakeyras, Danyal, and Dardalion together, preserving role-specific response opportunities.
- Companion-to-companion speech blocks self-replies and suppresses second-hop echo chains with `PHASE58D-ECHO-BLOCKED`.
- UMG bridge prompts now receive listener set, selected/suppressed responders, turn decision, parsed intent, capability/safety posture, and current state summary.
- Queue throttling no longer collapses group-selected owner dialogue to a single responder.
- Added GM proof commands `[p58speech]` / `[pspeech]`.
- Added `[sdump]` for the latest party speech context.

## Safety

- Speech context does not execute actions.
- Named command execution remains routed through existing command parsing, intent parsing, and capability gates.
- Tracking/hunt/healing/movement state is included as context only unless an explicit allowed command path triggers the existing gated executor.
- Companion dialogue publishes no further chain from `companion_dialogue` replies.
- Direct named commands select one primary responder.
- Group conversation is capped at three responders.
- Companion-to-companion dialogue is capped at one follow-up.

## Build

Command:

```text
dotnet build .\ServUO.sln -c Release
```

Result:

- Build succeeded.
- 0 errors.
- 15 warnings, all unreachable-code warnings from existing AIGM UI/bridge/counselor surfaces.

## Proof

Implemented live proof command:

- `[p58speech]`
- `[pspeech]`

Implemented context dump:

- `[sdump]`

Source-generated proof packet:

- `docs/runtime/PHASE58D_PARTY_SPEECH_RUNTIME_PROOF_20260615_112637.md`

Live command execution remains pending because ServUO was not running during this pass.

## Final Labels

- `PHASE58D-PARTY-HEARING-PROOF`
- `PHASE58D-GROUP-DIALOGUE-PROOF`
- `PHASE58D-COMPANION-DIALOGUE-PROOF`
- `PHASE58D-ECHO-BLOCKED`
- `PHASE58D-BLOCKED` for live proof execution only
