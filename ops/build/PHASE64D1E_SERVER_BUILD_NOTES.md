# Phase64D1E Server Build Notes

Build command:

```powershell
dotnet build .\ServUO.sln -c Release
```

Result:

- 0 warnings
- 0 errors

Server DLL hashes:

- Previous accepted D1D `Scripts.dll`: `EE6956036DDD769E052CD42FACC16751C4411B8EF0C32142A862F71AAE2BE8F2`
- Phase64D1E `Scripts.dll`: `54BA81F2B995AAD5C4068EE1ABCC66CDE15888C77AF1258E27B2EC7FC524A6A8`

Runtime proof summary:

- Dardalion approved operational layout was the only normal approved-layout participant.
- Duplicate-name Dardalion serial isolation returned `NO_APPROVED_OPERATIONAL_LAYOUT` for the duplicate serial.
- Druss and Miriel normal runtime previews returned `NO_APPROVED_OPERATIONAL_LAYOUT`; no D1E layout or version was fabricated.
- Synthetic scenarios and CurrentWorld preview produced structured receipts only.
- Passive watch was opt-in, bounded, coalesced, and disabled after proof.
- Restart proof rebuilt cold runtime state and reproduced the same `enemy-mage` decision fingerprint.
- Adapter mappings may be nonzero; invocation attempts and invocations remained zero.

Publication excludes private trace logs, screenshots, saves, accounts, deployed binaries, local launchers, and private absolute paths.

