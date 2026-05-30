# AIGM Runtime Handoff
_Date: 2026-05-29_

## Purpose

This note preserves the code-facing runtime truths discovered during current AIGM counselor work in the NeoUO dev shard.

## Critical runtime truths

### 1. Fresh counselors only
Stale serialized `AIGMCounselor` mobiles in `Saves\Mobiles\*` produced misleading runtime behavior and invalidated earlier observations.

Symptoms included:
- missing or inconsistent paperdoll / backpack behavior
- code changes appearing not to apply
- load problems around `Server.Mobiles.AIGMCounselor`

Use fresh counselor instances when validating behavior after structural counselor changes.

### 2. Root runtime assembly matters
A successful build is not enough if the live root runtime assembly was not updated.

Live server runtime assembly:
- `C:\UO\Server\Neo Ultima Online\NeoUO-Dev\Scripts.dll`

Required build command:
```powershell
dotnet build .\Scripts\Scripts.csproj -c Debug -p:Platform=x64
```

### 3. Stop server before build/deploy validation
If `ServUO.exe` is running, the root `Scripts.dll` may remain stale due to file locking.

Practical sequence:
1. stop ServUO
2. build with `-p:Platform=x64`
3. confirm root `Scripts.dll` timestamp changed
4. restart from repo root executable path
5. test with a fresh counselor

### 4. Start ServUO from repo root
ServUO must be launched from:
- `C:\UO\Server\Neo Ultima Online\NeoUO-Dev\ServUO.exe`

Launching from `Server\bin\Debug\ServUO.exe` breaks dynamic script compile assumptions because `Core.BaseDirectory` then points at the wrong location and the runtime cannot resolve the `Scripts` folder correctly.

## Live execution path

Confirmed active chain:
`AIGMQuestionGump`
→ `AIGMBridgeClient.Ask(...)`
→ `AIGMResponse`
→ `AIGMProposalAugmenter`
→ `AIGMResponseGump`
→ optional `AIGMConfirmActionGump`
→ `AIGMActionExecutor`

## Canonical action contract

The live system executes through `AIGMActionProposal`.
Do not split the execution model by building a second parallel primary path around `AIGMCommandAction`.

## Immediate engineering target

The next implementation target is generalized native Add-backed capability over the discovered constructable item surface, followed by container/mobile add lanes and props-read capability.
