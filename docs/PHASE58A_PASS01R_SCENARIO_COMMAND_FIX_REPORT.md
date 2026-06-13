# Phase58A Pass 01R Scenario Command Fix Report

## summary
This fix pass repaired the `AIGMScenario MonsterHunt` GM harness path so it no longer blocks runtime proof with non-deterministic/modal behavior or fragile scenario-key parsing. The command now directly invokes the Phase58A monster-hunt scaffold, normalizes scenario arguments more defensively, echoes the resolved scenario key in live client output, and writes proof metadata showing raw and normalized arguments.

## baseline
- Baseline commit: `dac4deeb3`
- Baseline message: `feat: scaffold Phase58A monster autonomy execution spine`

## problem before fix
Runtime proof was blocked at label:
- `D1 — Runtime proof blocked by scenario command.`

Observed failure shapes during Pass 01R before this fix:
- scenario path appeared to enter or coincide with a client modal/selection flow instead of giving deterministic proof output
- a later direct runtime attempt showed:
  - `AIGMScenario result: unknown_scenario`
- this meant the harness was no longer purely blocked by modal behavior, but still failed to deterministically execute the requested scenario

## files changed
- `Scripts/Commands/AIGMScenarioCommand.cs`

## changes made
1. Added scenario argument normalization before dispatch.
2. Added explicit scenario-key normalization that accepts robust forms such as:
   - `MonsterHunt`
   - `monsterhunt`
   - `hunt monsters`
   - underscore/hyphen/spacing variants after normalization
3. Made the command directly execute the scenario via:
   - `AIGMCompanionExecutionSpine.StartMonsterHunt(...)`
4. Added explicit client-visible runtime output:
   - `AIGMScenario key: <normalized-key>`
   - `AIGMScenario result: <result>`
5. Expanded proof file content to include:
   - raw argument
   - normalized scenario key
   - scenario result
   - companion execution-state snapshot fields when available

## compile/build result
A rebuild attempted while the shard was running failed due to `Scripts.dll` being locked by `ServUO.exe`.
A clean stop/build/start cycle was then performed successfully.
Final clean build result:
- `dotnet build .\ServUO.sln -c Release`
- `0 warnings`
- `0 errors`

## runtime result after fix
Direct live-client proof after the fix showed:
- `AIGMScenario key: monsterhunt`
- `AIGMScenario result: Danyal: no valid monsters remain nearby.`
- proof file written successfully

This upgrades runtime status from:
- `D1 — Runtime proof blocked by scenario command`

to:
- `B2 — Runtime proof generated; validator/no-target path confirmed.`

## what this fix proves
- the scenario command is deterministic now
- runtime argument parsing is no longer silently drifting into `unknown_scenario`
- the scenario harness now performs real work against the Phase58A execution spine
- the no-target bounded branch is live and proofable

## what this fix does not claim
- no claim of movement/combat proof yet
- no claim of full monster autonomy loop success
- no Pass 02 expansion

## next target
- `B3 — Runtime proof generated; movement/combat trace partially confirmed.`
- recommended next step: create or ensure a nearby valid monster target and rerun the same harness
