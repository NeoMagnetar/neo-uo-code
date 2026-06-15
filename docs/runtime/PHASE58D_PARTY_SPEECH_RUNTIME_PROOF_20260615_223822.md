# PHASE58D Party Speech Runtime Proof

- Label: PHASE58D-PARTY-HEARING-PROOF
- TimestampUTC: 2026-06-15T22:38:22.8422995Z
- Branch: neo/phase56t-clean-speech-recovery
- HEAD: 0d222f621ec8c22fa3640fe9433b5e49091e78d9
- Owner speaker: NeoMagnetar serial=401
- Companions found: Dakeyras/dakeyras, Danyal/danyal, Dardalion/dardalion

## 1. Direct named owner message to Dakeyras
- Label: PHASE58D-PARTY-HEARING-PROOF
- Tested text: dak track monsters
- Dialogue mode: DirectNamedCommand
- Owner speaker: NeoMagnetar
- Listener set: dakeyras, danyal, dardalion
- Selected responders: dakeyras
- Suppressed responders: danyal, dardalion
- Suppressed reasons: danyal=direct_named_context_only; dardalion=direct_named_context_only
- Parsed intent: track_monsters
- State context summary: dakeyras: tracking=inactive, hunt=inactive, guard=Follow, health=122/122, bandages=21, movement=available_if_phase58c_present | danyal: tracking=inactive, hunt=inactive, guard=Follow, health=111/111, bandages=25, movement=available_if_phase58c_present | dardalion: tracking=inactive, hunt=inactive, guard=Follow, health=134/134, bandages=30, movement=available_if_phase58c_present
- Turn coordinator decision: direct_named_command_one_primary
- Generated/queued speech lines: Dakeyras studies the ground and reports only through the named command lane.
- Final label: PHASE58D-PARTY-HEARING-PROOF

## 2. Owner group message to all companions
- Label: PHASE58D-GROUP-DIALOGUE-PROOF
- Tested text: companions, what do you see?
- Dialogue mode: StateCommentary
- Owner speaker: NeoMagnetar
- Listener set: dakeyras, danyal, dardalion
- Selected responders: dakeyras, danyal, dardalion
- Suppressed responders: none
- Suppressed reasons: none
- Parsed intent: not_companion_command
- State context summary: dakeyras: tracking=inactive, hunt=inactive, guard=Follow, health=122/122, bandages=21, movement=available_if_phase58c_present | danyal: tracking=inactive, hunt=inactive, guard=Follow, health=111/111, bandages=25, movement=available_if_phase58c_present | dardalion: tracking=inactive, hunt=inactive, guard=Follow, health=134/134, bandages=30, movement=available_if_phase58c_present
- Turn coordinator decision: group_bounded_multi_responder
- Generated/queued speech lines: Dakeyras watches the trail; Danyal checks wounds and supplies; Dardalion weighs the guard line.
- Final label: PHASE58D-GROUP-DIALOGUE-PROOF

## 3. Companion-to-companion exchange
- Label: PHASE58D-COMPANION-DIALOGUE-PROOF
- Tested text: Dakeyras reports a monster trail near the stones.
- Dialogue mode: CompanionToCompanion
- Owner speaker: Dakeyras
- Listener set: dakeyras, danyal, dardalion
- Selected responders: danyal
- Suppressed responders: dakeyras, dardalion
- Suppressed reasons: dakeyras=PHASE58D-ECHO-BLOCKED own_generated_speech; dardalion=companion_dialogue_one_followup_limit
- Parsed intent: not_companion_command
- State context summary: dakeyras: tracking=inactive, hunt=inactive, guard=Follow, health=122/122, bandages=21, movement=available_if_phase58c_present | danyal: tracking=inactive, hunt=inactive, guard=Follow, health=111/111, bandages=25, movement=available_if_phase58c_present | dardalion: tracking=inactive, hunt=inactive, guard=Follow, health=134/134, bandages=30, movement=available_if_phase58c_present
- Turn coordinator decision: companion_dialogue_one_followup
- Generated/queued speech lines: Danyal answers once, weighing wounds and supplies before the party moves.
- Final label: PHASE58D-COMPANION-DIALOGUE-PROOF

## 4. State-aware response using current tracking/hunt context
- Label: PHASE58D-GROUP-DIALOGUE-PROOF
- Tested text: all of you, report tracking and hunt state
- Dialogue mode: SystemStatus
- Owner speaker: NeoMagnetar
- Listener set: dakeyras, danyal, dardalion
- Selected responders: dakeyras, danyal, dardalion
- Suppressed responders: none
- Suppressed reasons: none
- Parsed intent: not_companion_command
- State context summary: dakeyras: tracking=inactive, hunt=inactive, guard=Follow, health=122/122, bandages=21, movement=available_if_phase58c_present | danyal: tracking=inactive, hunt=inactive, guard=Follow, health=111/111, bandages=25, movement=available_if_phase58c_present | dardalion: tracking=inactive, hunt=inactive, guard=Follow, health=134/134, bandages=30, movement=available_if_phase58c_present
- Turn coordinator decision: group_bounded_multi_responder
- Generated/queued speech lines: Each selected responder receives tracking, hunt, health, guard, and movement-state context.
- Final label: PHASE58D-GROUP-DIALOGUE-PROOF

## 5. Direct action command still routes only to named companion
- Label: PHASE58D-PARTY-HEARING-PROOF
- Tested text: danyal heal me
- Dialogue mode: DirectNamedCommand
- Owner speaker: NeoMagnetar
- Listener set: dakeyras, danyal, dardalion
- Selected responders: danyal
- Suppressed responders: dakeyras, dardalion
- Suppressed reasons: dakeyras=direct_named_context_only; dardalion=direct_named_context_only
- Parsed intent: heal_owner
- State context summary: dakeyras: tracking=inactive, hunt=inactive, guard=Follow, health=122/122, bandages=21, movement=available_if_phase58c_present | danyal: tracking=inactive, hunt=inactive, guard=Follow, health=111/111, bandages=25, movement=available_if_phase58c_present | dardalion: tracking=inactive, hunt=inactive, guard=Follow, health=134/134, bandages=30, movement=available_if_phase58c_present
- Turn coordinator decision: direct_named_command_one_primary
- Generated/queued speech lines: Only Danyal is selected for the direct named action lane; others are context-only.
- Final label: PHASE58D-PARTY-HEARING-PROOF

## 6. Echo-loop prevention
- Label: PHASE58D-ECHO-BLOCKED
- Tested text: Danyal answers Dakeyras once.
- Dialogue mode: CompanionToCompanion
- Owner speaker: Danyal
- Listener set: dakeyras, danyal, dardalion
- Selected responders: none
- Suppressed responders: dakeyras, danyal, dardalion
- Suppressed reasons: dakeyras=PHASE58D-ECHO-BLOCKED chain_depth; danyal=PHASE58D-ECHO-BLOCKED chain_depth; dardalion=PHASE58D-ECHO-BLOCKED chain_depth
- Parsed intent: not_companion_command
- State context summary: dakeyras: tracking=inactive, hunt=inactive, guard=Follow, health=122/122, bandages=21, movement=available_if_phase58c_present | danyal: tracking=inactive, hunt=inactive, guard=Follow, health=111/111, bandages=25, movement=available_if_phase58c_present | dardalion: tracking=inactive, hunt=inactive, guard=Follow, health=134/134, bandages=30, movement=available_if_phase58c_present
- Turn coordinator decision: PHASE58D-ECHO-BLOCKED
- Generated/queued speech lines: No responder is selected because the companion dialogue chain depth is already one.
- Final label: PHASE58D-ECHO-BLOCKED
- Block label: PHASE58D-ROUTING-BLOCKED


## Final Label
- PHASE58D-PARTY-HEARING-PROOF
- PHASE58D-GROUP-DIALOGUE-PROOF
- PHASE58D-COMPANION-DIALOGUE-PROOF
- PHASE58D-ECHO-BLOCKED
