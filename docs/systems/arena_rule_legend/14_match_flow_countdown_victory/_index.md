# Match flow, countdown, and victory

Core game loop rules for readiness, validation, countdown, victory conditions, forfeits, rematches, and ring lock timing.

## Scope

This chapter governs how matches begin, progress, resolve, and return players to lobby or rematch flow, including best-of variants and win conditions.

## Rule files

- [require_ready_check.md](require_ready_check.md) — Require ready-check
- [validate_before_start.md](validate_before_start.md) — Validate before start
- [freeze_until_begin_signal.md](freeze_until_begin_signal.md) — Freeze until begin signal
- [auto_start_when_both_ready.md](auto_start_when_both_ready.md) — Auto-start when both ready
- [auto_forfeit_on_leave.md](auto_forfeit_on_leave.md) — Auto-forfeit on leave
- [auto_forfeit_on_disconnect.md](auto_forfeit_on_disconnect.md) — Auto-forfeit on disconnect
- [best_of_one.md](best_of_one.md) — Best of one
- [best_of_three.md](best_of_three.md) — Best of three
- [best_of_five.md](best_of_five.md) — Best of five
- [last_alive_wins.md](last_alive_wins.md) — Last alive wins
- [first_kill_wins.md](first_kill_wins.md) — First kill wins
- [hold_the_point_wins.md](hold_the_point_wins.md) — Hold-the-point wins
- [time_limit_wins.md](time_limit_wins.md) — Time-limit wins
- [rematch_prompt_on_finish.md](rematch_prompt_on_finish.md) — Rematch prompt on finish
- [return_fighters_to_lobby_on_finish.md](return_fighters_to_lobby_on_finish.md) — Return fighters to lobby on finish
- [lock_the_ring_while_results_display.md](lock_the_ring_while_results_display.md) — Lock the ring while results display

## Related surfaces

- DuelContext
- Match controller
- Arena state tracker
