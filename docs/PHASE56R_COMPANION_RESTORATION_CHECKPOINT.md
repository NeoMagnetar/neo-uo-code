# PHASE 56R Companion Restoration Checkpoint

## Target source

C:\UO\Server\Neo Ultima Online\NeoUO-FullIntegration-aigm-umg

## Branch

neo/staging-aigm

## Reference behavior source

C:\UO\Server\Neo Ultima Online\NeoUO-Dev

## Preserved backup

C:\UO\Server\Neo Ultima Online\Backups\NeoUO-Dev-preserve-aigm-working-20260607-072757

## Purpose

This checkpoint preserves the recovered companion lane before additional restoration work. The UMG source server is now the candidate new Dev source, but old NeoUO-Dev remains reference-only until feature parity and promotion are complete.

## Working now

* Dakeyras, Danyal, and Dardalion can spawn.
* Companion admin/debug spawn surfaces exist.
* Local natural speech commands work:

 * follow / come
 * stop / stay / hold / wait
 * guard / protect / defend
* Shared local commands fan out to eligible owned companions.
* One spokesperson behavior prevents speech spam.
* Companions no longer expire as paid hirelings.
* Companions opt out of BaseHire payroll.
* Speech-only chatbot/OpenClaw lane is restored.
* Trusted owner ordinary speech can reach companion chatbot fallback.
* AI response handling uses ReplyText only.
* AI proposed actions are ignored.

## Files changed

* Scripts/Commands/AIGMCompanionCommand.cs
* Scripts/Custom/AIGM/AIGMBridgeClient.cs
* Scripts/Custom/AIGM/AIGMCompanionSpeechQueue.cs
* Scripts/Mobiles/NPCs/AIGMCompanionDakeyras.cs
* Scripts/Mobiles/NPCs/AIGMCompanionDanyal.cs
* Scripts/Mobiles/NPCs/AIGMCompanionDardalion.cs
* Scripts/Mobiles/NPCs/BaseHire.cs

## New/restored file policy

Only one justified source file was added/restored:
Scripts\Custom\AIGM\AIGMCompanionSpeechQueue.cs

Reason:
The old working companion chatbot behavior required a speech queue / arbitration / async fallback layer. The UMG target already had bridge transport, but it lacked the active old behavior layer. The restored queue is trimmed to speech-only orchestration.

## Intentionally not imported

* AIGMCompanionStateAccess.cs
* AIGMCompanionTravelController.cs
* AIGMCompanionAutoPathNavigator.cs
* old AIGMCompanionActionExecutor.cs wholesale
* old AIGMCompanionDirectActionPolicy.cs wholesale
* old AIGMCompanionSpeechQueue.cs wholesale
* movement executor imports
* travel/autopath logic
* trusted action extraction from AI replies
* middleware/action execution coupling
* support/heal execution
* scan/report/tracking execution

## Deferred capabilities

Still recognized/deferred, not executed:

* scan
* report
* tracking
* heal/cure/support
* attack
* travel / return home

## Runtime validation checklist

After build/restart:

* [Dakeyras
* [Danyal
* [Dardalion
* follow me
* stop
* stay here
* guard me
* hi
* hello
* dak hello
* danyal hello
* dar hello
* companions hello
* scan the area
* heal me
* hello counselor

Expected:

* spawn works
* local commands work
* chatbot/fallback responds for ordinary trusted owner speech
* no “I have already been hired” for companion-routed/trusted owner speech
* deferred actions do not execute
* counselor phrases remain separate

## Next recommended phase

PHASE 56T-READONLY-SCAN-REPORT-TRACKING-RESTORE

Goal:
Restore scan/report/tracking as read-only awareness first:

* no movement
* no travel
* no pursuit
* no old StateAccess import

## Promotion note

Do not rename or repoint Dev yet. This UMG source is now a candidate new Dev, but old NeoUO-Dev remains the reference lane until feature parity is complete.
