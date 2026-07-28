# Phase64D1B Companion Inventory Marker Contract

The backpack marker is a client discovery hint, not an authorization grant.

Client discovery tuple:
- Equipped layer: `Layer.Backpack`
- ItemID/graphic: `0x0E75`
- Normalized hue: `1175`
- Marker version: `1`

Server authority:
- `AIGMUMGSleeveAccessService` validates actor identity and caller authority.
- `AIGMCompanionInventoryService` validates companion backpack ownership, marker tuple, access range, map, and actor state.
- The server decides whether a Sleeve Selector or backpack may open.

Inventory invariants:
- Registered live AIGM companions must have exactly one equipped backpack on `Layer.Backpack`.
- The backpack must be `Server.Custom.AIGM.Inventory.AIGMCompanionBackpack`.
- The backpack stores inventory metadata only. It does not store UMG definitions, assignments, versions, Sleeves, traces, governance, or decision data.
- Companion death handling keeps AIGM backpack contents with the backpack and off the corpse.
- Temporary proof accounts, actors, backpacks, and proof items are cleanup-only and are not part of the publication state.

Non-goals:
- No autonomous item use.
- No client trust boundary.
- No custom packets.
- No server inventory migration beyond controlled backpack adoption.
- No tactical dispatch.
- No waypoint or movement overlay work.
