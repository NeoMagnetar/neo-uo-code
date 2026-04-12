# Login, logout, stuck, and persistence

Cleanup, reconnect, logout, reset, and persistence rules that determine what survives matches and what gets reinitialized.

## Scope

This chapter governs disconnect handling, reconnect placement, offline ejection, aggressor cleanup, UI cleanup, reset cleanup, and what state is or is not persisted.

## Rule files

- [extend_logout_delay_in_bounds.md](extend_logout_delay_in_bounds.md) — Extend logout delay in bounds
- [deny_stuck_menu_in_match.md](deny_stuck_menu_in_match.md) — Deny stuck menu in match
- [auto_forfeit_disconnects.md](auto_forfeit_disconnects.md) — Auto-forfeit disconnects
- [re_place_reconnecting_fighter_into_lobby.md](re_place_reconnecting_fighter_into_lobby.md) — Re-place reconnecting fighter into lobby
- [eject_offline_characters_from_ring.md](eject_offline_characters_from_ring.md) — Eject offline characters from ring
- [clear_aggressions_on_exit.md](clear_aggressions_on_exit.md) — Clear aggressions on exit
- [clear_target_and_cursor_state_on_exit.md](clear_target_and_cursor_state_on_exit.md) — Clear target and cursor state on exit
- [close_arena_gumps_on_finish.md](close_arena_gumps_on_finish.md) — Close arena gumps on finish
- [remove_temporary_walls_on_reset.md](remove_temporary_walls_on_reset.md) — Remove temporary walls on reset
- [reset_arena_controller_after_each_match.md](reset_arena_controller_after_each_match.md) — Reset arena controller after each match
- [persist_ladder_only.md](persist_ladder_only.md) — Persist ladder only
- [do_not_persist_temporary_round_state.md](do_not_persist_temporary_round_state.md) — Do not persist temporary round state

## Related surfaces

- Region logout hooks
- DuelContext
- Arena controller
- Persistence trackers
