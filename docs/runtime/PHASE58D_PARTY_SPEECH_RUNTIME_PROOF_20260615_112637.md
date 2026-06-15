# PHASE58D Party Speech Runtime Proof

- Label: PHASE58D-PARTY-HEARING-PROOF
- TimestampUTC: 2026-06-15T20:26:37Z
- Branch: neo/phase56t-clean-speech-recovery
- HEAD: 205fba5e9651dc3f0d960744e5f3bd475516c65b
- Proof source: deterministic implementation proof packet; live shard GM command `[p58speech]` / `[pspeech]` is implemented but was not executed because ServUO was not running in this pass.
- Owner speaker: pending live GM invocation
- Companions found: pending live GM invocation

## 1. Direct Named Owner Message To Dakeyras

- Label: PHASE58D-PARTY-HEARING-PROOF
- Tested text: `dak track monsters`
- Expected dialogue mode: DirectNamedCommand
- Listener set: Dakeyras, Danyal, Dardalion when owned party is present
- Selected responders: dakeyras
- Suppressed responders and reasons: danyal=direct_named_context_only; dardalion=direct_named_context_only
- Generated/queued speech lines: Dakeyras receives the visible named command response; sibling companions receive context only.
- Turn coordinator decision: direct_named_command_one_primary
- Final label: PHASE58D-PARTY-HEARING-PROOF

## 2. Owner Group Message To All Companions

- Label: PHASE58D-GROUP-DIALOGUE-PROOF
- Tested text: `companions, what do you see?`
- Expected dialogue mode: GroupConversation
- Listener set: Dakeyras, Danyal, Dardalion when owned party is present
- Selected responders: dakeyras, danyal, dardalion
- Suppressed responders and reasons: none
- Generated/queued speech lines: Dakeyras answers as scout/tracker; Danyal answers as healer/support; Dardalion answers as guardian/protector.
- Turn coordinator decision: group_bounded_multi_responder
- Final label: PHASE58D-GROUP-DIALOGUE-PROOF

## 3. Companion-To-Companion Exchange

- Label: PHASE58D-COMPANION-DIALOGUE-PROOF
- Tested text: `Dakeyras reports a monster trail near the stones.`
- Expected dialogue mode: CompanionToCompanion
- Listener set: Dakeyras, Danyal, Dardalion when owned party is present
- Selected responders: one non-source companion
- Suppressed responders and reasons: source companion=PHASE58D-ECHO-BLOCKED own_generated_speech; additional non-source companion=companion_dialogue_one_followup_limit
- Generated/queued speech lines: one bounded follow-up response may reference the reporting companion by name.
- Turn coordinator decision: companion_dialogue_one_followup
- Final label: PHASE58D-COMPANION-DIALOGUE-PROOF

## 4. State-Aware Party Response

- Label: PHASE58D-GROUP-DIALOGUE-PROOF
- Tested text: `all of you, report tracking and hunt state`
- Expected dialogue mode: SystemStatus or StateCommentary with group addressing
- Listener set: Dakeyras, Danyal, Dardalion when owned party is present
- Selected responders: up to three companions
- State context summary: tracking mode/result, hunt phase/target, health/bandage readiness, guard order, and movement availability are included in `AIGMCompanionSpeechRequest`.
- Generated/queued speech lines: responses may use state as context, but speech alone does not execute gated actions.
- Turn coordinator decision: group_bounded_multi_responder
- Final label: PHASE58D-GROUP-DIALOGUE-PROOF

## 5. Direct Action Command Still Routes Only To Named Companion

- Label: PHASE58D-PARTY-HEARING-PROOF
- Tested text: `danyal heal me`
- Expected dialogue mode: DirectNamedCommand
- Listener set: Dakeyras, Danyal, Dardalion when owned party is present
- Selected responders: danyal
- Suppressed responders and reasons: dakeyras=direct_named_context_only; dardalion=direct_named_context_only
- Generated/queued speech lines: only Danyal may visibly answer or execute the named healing lane through existing gates.
- Turn coordinator decision: direct_named_command_one_primary
- Final label: PHASE58D-PARTY-HEARING-PROOF

## 6. Echo-Loop Prevention

- Label: PHASE58D-ECHO-BLOCKED
- Tested text: companion dialogue at chain depth 1
- Expected dialogue mode: CompanionToCompanion
- Listener set: Dakeyras, Danyal, Dardalion when owned party is present
- Selected responders: none
- Suppressed responders and reasons: all eligible companions suppressed by `PHASE58D-ECHO-BLOCKED chain_depth`
- Generated/queued speech lines: no second follow-up chain is queued.
- Turn coordinator decision: PHASE58D-ECHO-BLOCKED
- Final label: PHASE58D-ECHO-BLOCKED

## Live Invocation Status

- Label: PHASE58D-BLOCKED
- Reason: ServUO was not running, so the in-game GM command could not be invoked from this implementation pass.
- Implemented proof commands: `[p58speech]`, `[pspeech]`
- Implemented dump command: `[sdump]`
- Live command output path: `docs/runtime/PHASE58D_PARTY_SPEECH_RUNTIME_PROOF_<timestamp>.md`
- Final label: PHASE58D-BLOCKED
