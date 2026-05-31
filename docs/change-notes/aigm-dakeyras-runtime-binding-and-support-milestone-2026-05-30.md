# AIGM Dakeyras Runtime Binding and Support Milestone — 2026-05-30

## Scope

This note records the technical implementation milestone that moved Dakeyras companion support commands from uncertain runtime behavior into proven live direct execution on `NeoUO-Dev`.

## Root blocker resolved

The primary blocker was not support phrase coverage alone.

The real blocker was that current source edits were not reliably becoming the live root script assembly used by the shard. Runtime logs continued to show older speech handler behavior until the deployment path was verified.

## Runtime binding proof work

### Added runtime stamp instrumentation
`Scripts/Mobiles/NPCs/AIGMCompanionDakeyras.cs` was extended with a runtime proof marker:

- `DAK_RUNTIME_BINDING_PROOF_20260531_V1`

Instrumentation additions included:

- constructor runtime stamp logging
- deserialize runtime stamp logging
- OnSpeech runtime stamp logging
- emergency trusted speech proof for:
  - `runtime stamp`
  - `version`
  - `who are you`

### Result
Once the correct script assembly was deployed, the live shard produced the new `DAK_RUNTIME_STAMP` and `DAK_SPEECH_START` log labels, proving the edited Dakeyras source path was finally live.

## Build / deploy finding

`Scripts/Scripts.csproj` is configured to build `Scripts` as the assembly name, but successful build alone did not update the live root:

- live root target: `C:\UO\Server\Neo Ultima Online\NeoUO-Dev\Scripts.dll`
- build output observed: `C:\UO\Server\Neo Ultima Online\NeoUO-Dev\Scripts\bin\Debug\Scripts.dll`

The breakthrough deployment step was manually copying the built DLL onto the root live DLL:

```powershell
Copy-Item "C:\UO\Server\Neo Ultima Online\NeoUO-Dev\Scripts\bin\Debug\Scripts.dll" "C:\UO\Server\Neo Ultima Online\NeoUO-Dev\Scripts.dll" -Force
```

This deployment fact should now be treated as required operational knowledge for the current dev lane unless/until the build pipeline is normalized.

## Companion support implementation state

### Companion command surfaces involved
- `Scripts/Mobiles/NPCs/AIGMCompanionDakeyras.cs`
- `Scripts/Custom/AIGM/AIGMCompanionIntent.cs`
- `Scripts/Custom/AIGM/AIGMCompanionIntentParser.cs`
- `Scripts/Custom/AIGM/AIGMCompanionActionExecutor.cs`
- `Scripts/Custom/AIGM/AIGMCompanionSkillExecutor.cs`
- `Scripts/Custom/AIGM/AIGMCompanionSpeechQueue.cs`
- `Scripts/Custom/AIGM/AIGMCompanionResponseActionExtractor.cs`
- `Scripts/Custom/AIGM/AIGMCompanionDirectActionPolicy.cs`
- `Scripts/Custom/AIGM/AIGMExecutionMode.cs`

### Terminology routing established
Current intended support mapping:

- `bandage self` / `bandage yourself` -> `TryUseBandages` on self
- `bandage me` -> `TryUseBandages` on owner
- `heal self` / `heal yourself` -> `TryUseMageryHeal` on self
- `heal me` -> `TryUseMageryHeal` on owner
- `cure self` / `cure yourself` -> `TryUseCurePotion` on self
- `cure me` -> `TryUseMageryCure` on owner

### Confirmed direct local results from logs
The following were confirmed on live runtime logs after correct deployment:

- `bandage self` parsed locally and executed directly
- `bandage yourself` parsed locally and executed directly
- `heal yourself` parsed locally and executed through `TryUseMageryHeal`
- `cure yourself` parsed locally and executed through `TryUseCurePotion`

### Confirmed bandage execution details
Execution log evidence showed:

- `TryUseBandages`
- bandage item found in backpack
- heal amount applied
- target hit points increased

This is the first clean proof that Dakeyras support execution was working as actual local shard behavior rather than AI paraphrase only.

## Async rescue behavior still present

For malformed variants such as misspellings (`heal yoruself`, `heal youyrself`), local parsing still fails. However, the async queue / response-extractor path can later infer a trusted action and attempt direct execution during apply.

This is useful as a fallback but currently produces one known behavior flaw:

- if extracted direct execution fails locally (for example cooldown), the spoken reply may still be the async paraphrase rather than the local failure reason

## Immediate follow-up recommendations

1. Add typo-tolerant support phrase normalization for common self-target spelling variants.
2. Prevent trusted support intents from speaking async paraphrase text when a direct local failure reason exists.
3. Normalize or automate live root `Scripts.dll` deployment.
4. Later extend heal/cure spell paths with more faithful magery requirements, reagent checks, spellbook checks, and higher-tier spells such as greater heal.

## Milestone close

This milestone establishes that Dakeyras trusted support behavior is now a real live implementation surface on the dev shard, not just an architectural intention.
