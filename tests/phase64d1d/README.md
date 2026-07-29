# Phase64D1D Sanitized Test Summary

## Baseline

- ServUO listener: `127.0.0.1:2595`.
- Middleware listener: `127.0.0.1:4876`.
- Middleware fallback: `0`.
- Middleware timeout: `0`.
- Middleware last error: `null`.
- Prior accepted `Scripts.dll`: `4AC823D6532C723FC4ADD0A128E2FBE35A1A4C8F3E09DE0101832090AA7F3FF0`
- Final D1D deployed/runtime `Scripts.dll`: `EE6956036DDD769E052CD42FACC16751C4411B8EF0C32142A862F71AAE2BE8F2`
- `versions_v2.json`: `0EB27E13320CDC327597662334D8220F87DD47C94F5011DB2867E93A8D1D6C2F`
- Registered live companions: 22.
- Normalized AIGM companion backpacks: 22.
- Duplicate companion backpacks: 0.
- D1C client `cuo.dll`: `722D511EC94B6C6C10989454BEE3659E7E61075E27CB5C7B589E05100DED09FD`
- D1C client `cuo.pdb`: `9F759AAA665A5807ED9146D1A1FA55848C5AB6D594363B8A607CEBB8AD833BE1`

## Organizer Proof

Primary actor: Dardalion `0x00000193`.

Proof covered Operator Mode open, Architect Mode authorization, custom stack creation, canonical block references, move up/down/top/bottom, move to another stack, enable/disable, lock, illegal Always-On move rejection, mandatory reference removal rejection, preview without write, Draft save, restart persistence, Approve PreviewOnly, compare, second Draft, rollback clone, and historical immutability.

Preview final result:

```text
PREVIEW_ONLY_NOT_DISPATCHED
```

## Regression Actors

- Druss `0x00000304`: Phase64C2 versions remained readable and immutable.
- Miriel `0x00002AA5`: ranged capability stayed visible and was not misclassified as melee.
- Duplicate-name Dardalion `0x00003B7A`: serial-specific layout key avoided cross-instance leakage.
- Joining `0x00005A14`: deterministic default layout and Unclassified handling stayed intact.

## Cancel, Stale, and Expiration

- Cancel after unsaved changes created no version and no write.
- A stale session save after another session advanced the revision was refused.
- Expired session mutation was refused and created no write.

## Persistence

- First open created no sidecar.
- First Save Draft created the first immutable layout version.
- Approve PreviewOnly created a new immutable approved version.
- Rollback cloned historical state into a new Draft version.
- Two restart cycles preserved pointer and version sidecar hashes.
- No BOM was present on layout sidecars.
- Interrupted-write recovery was tested in a copied recovery lab rather than against live sidecars.

## Permissions and Safety

- Server-side authorization, range, and map checks remain in the D1A path.
- Unauthorized access cannot enter Architect Mode, save, approve, or rollback.
- Game Master administrative operations remain logged.
- No canonical definition was edited through the organizer.
- No UMG adapter executed.
- Tactical dispatch remained disabled.
- Draft and Approved PreviewOnly layouts did not execute.
- No autonomous inventory use occurred.
- No ClassicUO source or deployed client binary changed during D1D.