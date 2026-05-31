# Companion Identity Separation and NeoStack Integration — 2026-05-30

## Scope

This change note records the technical and personality-layer work that completed the first true multi-companion identity separation pass for the Neo UO AIGM companion system.

Companions involved:
- Dakeyras
- Danyal

## Technical problem solved

Danyal could spawn and respond, but she continued to identify herself as Dakeyras during natural-language chat.

The final diagnosis required several layers of investigation:

1. ServUO runtime identity was proven correct using runtime stamps and per-companion log files.
2. The shard bridge payload was confirmed to send correct companion-specific fields:
   - `CompanionName`
   - `CompanionTypeName`
   - `CompanionProfileKey`
   - `CompanionRuntimeIdentity`
   - `CompanionMemory`
3. Middleware logs proved those fields were reaching the local bridge process.
4. The middleware prompt builder was found to still use counselor-oriented generic prompt framing for companion requests.
5. A deeper hidden bug was then found: `normalizeRequest(...)` was not preserving companion-specific fields, so later prompt/session logic was operating without them.
6. Another architectural issue was found: all companion traffic reused a single OpenClaw session id (`aigm-counselor`), causing conversational selfhood bleed between companions.

## Middleware changes

### File
- `C:\.openclaw\workspace-ultima-online\aigm-middleware-service.js`

### Changes made

#### 1. Companion prompt specialization
`buildPrompt(...)` now branches for `companion_speech` mode and injects explicit identity contract rules, including:

- fixed companion identity
- prohibition against identifying as another companion
- prohibition against answering as the counselor
- explicit use of companion profile memory as authoritative grounding

#### 2. Companion-specific session ids
The middleware now derives session ids for `companion_speech` traffic from companion identity rather than routing all requests through the shared:

- `aigm-counselor`

This isolates long-lived LLM conversation state between Dakeyras and Danyal.

#### 3. Request normalization fix
`normalizeRequest(...)` was patched to preserve companion fields:

- `mode`
- `companionName`
- `companionTypeName`
- `companionProfileKey`
- `companionRuntimeIdentity`
- `companionMemory`

This was the critical hidden bug preventing earlier fixes from taking effect.

## ServUO shard changes

### Files
- `Scripts/Mobiles/NPCs/AIGMCompanionDakeyras.cs`
- `Scripts/Mobiles/NPCs/AIGMCompanionDanyal.cs`

### Identity symmetry fix
Both companions now follow the same rule:

- `runtime stamp` -> debug runtime proof
- `version` -> debug runtime proof
- `who are you` -> normal natural-language identity response

This removed old debug-lane asymmetry where Dakeyras or Danyal could answer identity questions with runtime stamp strings.

## Confirmed result

After middleware restart and shard rebuild/deploy cycles:

- Dakeyras identifies as Dakeyras
- Danyal identifies as Danyal
- Danyal no longer aliases into Dakeyras
- both companions remain on separate conversational identity paths

This is the first clean proof of distinct multi-companion conversational selfhood in the current AIGM system.

## NeoStack personality integration

### Updated long-form profile files
- `memory/dakeyras-profile.md`
- `memory/danyal-profile.md`

### Added compact operational summaries
- `memory/dakeyras-ops.md`
- `memory/danyal-ops.md`

These files formalize:

- fixed identity
- trust posture
- speech style
- priority instincts
- memory salience
- anti-identity-bleed guardrails

They are intended to support future middleware prompt shaping and later autonomous decision differentiation.

## Architectural significance

This work establishes the foundation required before more advanced cognition features can be added safely:

- tracking
- coordinate awareness
- destination navigation
- companion-to-companion perception
- differentiated tactical interpretation

Without identity separation, all higher autonomy layers would be compromised by selfhood bleed.

## Recommended next steps

1. Multi-companion speech arbitration and routing.
2. Shared perception design without uncontrolled mutual chatter.
3. Tracking + coordinate-awareness implementation plan.
4. Optional middleware refinement to favor compact ops summaries for routine prompts and full profile files for deeper grounding.

## Milestone close

The companion system now supports Dakeyras and Danyal as separate AI entities with distinct memory/personality grounding and isolated conversation state, which is the necessary base layer for richer multi-agent world behavior.
