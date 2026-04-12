# Manifest

Canonical file tree for the arena rule legend subsystem.

```text
docs/systems/arena_rule_legend/
  README.md
  manifest.md
  rule_file_template.md
  verb_index.md
  implementation_status.md
  change_log.md
  00_implementation_surfaces/
    _index.md
    facet_and_map_rules.md
    region.md
    guardedregion.md
    spellhelper_and_travel_validation.md
    duelcontext_and_rulesets.md
    moongate.md
    teleporter.md
    custom_controller.md
    match_controller.md
    arena_state_tracker.md
    entry_filter.md
    exit_router.md
    boundary_validator.md
    interference_handler.md
    status_and_progress_tracker.md
  01_facet_map_rules/
    _index.md
    set_arena_facet_to_felucca_rules.md
    set_arena_facet_to_trammel_rules.md
    keep_the_build_world_safe_make_the_arena_world_lethal.md
    use_a_dedicated_arena_facet.md
    mirror_the_arena_on_a_test_facet_first.md
    keep_town_and_vendor_space_protected_outside_arena.md
    decide_facet_law_first_local_arena_law_second.md
  02_region_geometry_boundaries/
    _index.md
    define_arena_rectangle.md
    define_arena_z_floor.md
    define_arena_z_ceiling.md
    set_region_priority.md
    create_inner_ring_and_outer_shell_as_separate_regions.md
    create_staging_room_region.md
    create_spectator_region.md
    add_internal_invisible_wall.md
    bounce_players_back_on_invalid_edge_cross.md
    track_entry_and_exit_events.md
    separate_ring_lobby_and_reward_room.md
  03_entry_exit_routing/
    _index.md
    add_arena_moongate_in.md
    add_arena_moongate_out.md
    add_fallback_teleporter_out.md
    lock_entry_during_active_match.md
    allow_entry_only_from_staging_room.md
    deny_entry_to_non_participants.md
    deny_re_entry_after_elimination.md
    send_eliminated_players_to_exit_pad.md
    route_spectators_to_stands.md
    route_staff_to_observer_deck.md
    enable_combat_check_on_exit_teleporter.md
    enable_criminal_check_on_exit_teleporter.md
  04_travel_suppression/
    _index.md
    block_recall_in_bounds.md
    block_recall_into_bounds.md
    block_gate_travel_in_bounds.md
    block_gate_travel_into_bounds.md
    block_mark_in_bounds.md
    block_teleport_in_bounds.md
    block_teleport_while_flagged_in_match.md
    block_travel_while_in_combat.md
    block_emergency_escape_abilities.md
    block_stuck_menu_in_bounds.md
    send_invalid_travel_message.md
    allow_only_arena_exit_gate_as_legal_travel.md
  05_combat_permissions/
    _index.md
    allow_participant_versus_participant_harm.md
    deny_outsider_versus_participant_harm.md
    deny_participant_versus_outsider_harm.md
    enable_friendly_fire.md
    disable_friendly_fire.md
    allow_pvp_only_after_countdown.md
    deny_pre_match_aggression.md
    allow_damage_to_summoned_creatures.md
    deny_damage_to_summoned_creatures.md
    allow_npc_interference.md
    deny_npc_interference.md
    suppress_harmful_acts_outside_ring_core.md
  06_beneficial_healing/
    _index.md
    allow_self_heal_only.md
    allow_team_heal_only.md
    deny_cross_heal_between_opponents.md
    deny_outsider_healing.md
    deny_outsider_buffs.md
    deny_resurrection_support.md
    allow_bandages.md
    deny_bandages.md
    allow_cure_potions.md
    deny_cure_potions.md
    allow_cleanse_between_rounds_only.md
    scale_healing_received.md
    scale_healing_done.md
    strip_active_buffs_on_entry.md
  07_criminal_guard_legal_state/
    _index.md
    clear_criminal_state_on_entry.md
    preserve_criminal_state_on_entry.md
    disable_guards_in_bounds.md
    enable_guards_in_bounds.md
    deny_guard_calls_in_arena.md
    auto_flag_interference_as_criminal.md
    remove_aggressions_at_match_start.md
    reset_combatant_on_round_reset.md
    clear_yellow_bar_legal_noise_on_entry.md
    suppress_criminal_warnings_during_sanctioned_duel.md
    apply_criminal_state_to_ring_jumpers.md
    force_clean_slate_before_round_one.md
  08_team_ally_party/
    _index.md
    one_versus_one_only.md
    two_versus_two_only.md
    team_versus_team_only.md
    free_for_all.md
    lock_team_membership_at_ready_up.md
    randomize_starting_pads_by_team.md
    allow_ally_heals.md
    deny_ally_heals.md
    enable_friendly_fire_2.md
    disable_friendly_fire_2.md
    separate_team_staging_rooms.md
    disallow_guild_advantages.md
    disallow_party_spillover_from_outside_arena.md
  09_pets_summons_followers/
    _index.md
    disable_pets_in_bounds.md
    auto_stable_pets_on_entry.md
    auto_dismiss_summons_on_entry.md
    deny_new_summons_after_countdown.md
    allow_pets_only_in_beast_arena_mode.md
    count_followers_against_eligibility.md
    deny_outsider_pet_healing.md
    deny_pet_versus_pet_friendly_fire.md
    freeze_followers_outside_ring.md
    bounce_uncontrolled_creatures_out_of_bounds.md
    strip_escorts_and_companions_at_gate.md
    allow_only_cosmetic_companions.md
  10_mount_movement_locomotion/
    _index.md
    disable_mounts_in_bounds.md
    auto_dismount_on_entry.md
    deny_remount_in_bounds.md
    freeze_fighters_during_countdown.md
    deny_crossing_start_line_before_begin.md
    deny_wall_clipping_via_teleports.md
    bounce_from_spectator_barrier.md
    reveal_on_boundary_cross.md
    disable_stealth_in_ring.md
    disable_flying_in_ring.md
    slow_movement_in_hazard_zones.md
    force_walk_mode_in_staging_room.md
  11_spell_skill_special_move/
    _index.md
    deny_all_spellcasting.md
    allow_only_magery.md
    allow_only_melee_specials.md
    ban_invisibility.md
    ban_teleport_spells.md
    ban_paralyze.md
    ban_resurrection_spells.md
    ban_summon_spells.md
    ban_field_spells.md
    ban_area_of_effect_spells.md
    ban_special_moves.md
    ban_selected_skills.md
    deny_targeting_outside_arena.md
    deny_targeting_through_spectator_wall.md
    use_whitelist_only_ability_sets.md
    use_blacklist_only_ability_sets.md
  12_item_equipment_consumable/
    _index.md
    ban_potions.md
    allow_potions.md
    ban_trapped_boxes.md
    ban_wands.md
    ban_bolas_and_nets.md
    ban_pre_charged_consumables.md
    require_weapon_class_lock.md
    require_armor_tier_lock.md
    require_naked_duel.md
    strip_illegal_items_on_entry.md
    bounce_illegal_items_to_backpack.md
    deny_gear_swap_after_countdown.md
    allow_free_consumables.md
    consume_normal_resources.md
    disable_item_use_except_bandages.md
    disable_scroll_use.md
  13_death_corpse_resurrection_loot/
    _index.md
    instant_elimination_on_death.md
    best_of_rounds_elimination.md
    keep_corpse_in_arena.md
    bounce_corpse_contents_out.md
    delete_corpse_on_round_end.md
    deny_in_match_resurrection.md
    auto_resurrect_after_match.md
    send_ghosts_to_spectator_zone.md
    lossless_death_in_test_arena.md
    full_loot_death_in_hardcore_arena.md
    refresh_stats_after_round.md
    strip_buffs_on_revive.md
    restore_saved_loadout_on_rematch.md
  14_match_flow_countdown_victory/
    _index.md
    require_ready_check.md
    validate_before_start.md
    freeze_until_begin_signal.md
    auto_start_when_both_ready.md
    auto_forfeit_on_leave.md
    auto_forfeit_on_disconnect.md
    best_of_one.md
    best_of_three.md
    best_of_five.md
    last_alive_wins.md
    first_kill_wins.md
    hold_the_point_wins.md
    time_limit_wins.md
    rematch_prompt_on_finish.md
    return_fighters_to_lobby_on_finish.md
    lock_the_ring_while_results_display.md
  15_anti_stall_sudden_death/
    _index.md
    start_sudden_death_after_n_seconds.md
    warn_sudden_death_at_n_minus_10.md
    disable_healing_in_sudden_death.md
    increase_damage_in_sudden_death.md
    shrink_safe_area_over_time.md
    reveal_all_hidden_fighters.md
    disable_mounts_in_sudden_death.md
    disable_ranged_only_play_in_sudden_death.md
    end_as_tie_at_hard_cap.md
    end_by_damage_leader_at_hard_cap.md
    collapse_walls_inward.md
    spawn_hazard_ring.md
  16_spectator_outsider_interference/
    _index.md
    allow_spectators_in_stands_only.md
    deny_spectator_targeting_into_ring.md
    deny_spectator_healing_into_ring.md
    deny_item_toss_into_ring.md
    deny_corpse_looting_by_outsiders.md
    bounce_outsiders_who_cross_line.md
    separate_spectator_and_participant_gates.md
    allow_staff_invisible_observer_mode.md
    hide_spectator_chat_from_fighters.md
    deny_pet_entry_from_spectator_zone.md
    deny_field_effects_crossing_from_stands.md
  17_environment_hazard_atmosphere/
    _index.md
    change_local_light_level.md
    change_local_music.md
    apply_periodic_arena_damage.md
    scale_spell_damage_up.md
    scale_spell_damage_down.md
    scale_melee_damage_up.md
    scale_healing_down.md
    trigger_trap_tiles.md
    trigger_announcer_speech_on_entry.md
    trigger_round_start_broadcast.md
    spawn_line_of_sight_blockers.md
    spawn_temporary_walls.md
    turn_ring_into_lava_poison_or_ice_mode.md
    add_thematic_weather_and_effects_layer.md
  18_npc_spawn_housing_economy/
    _index.md
    deny_housing_in_arena_footprint.md
    deny_normal_creature_spawns.md
    allow_only_arena_controller_npcs.md
    deny_vendor_access_in_bounds.md
    disable_banking_in_bounds.md
    disable_harvesting_in_bounds.md
    despawn_wandering_npcs_on_match_start.md
    prevent_escort_and_pathing_through_arena.md
    keep_arena_economy_neutral.md
    keep_reward_vendors_outside_ring_only.md
  19_login_logout_stuck_persistence/
    _index.md
    extend_logout_delay_in_bounds.md
    deny_stuck_menu_in_match.md
    auto_forfeit_disconnects.md
    re_place_reconnecting_fighter_into_lobby.md
    eject_offline_characters_from_ring.md
    clear_aggressions_on_exit.md
    clear_target_and_cursor_state_on_exit.md
    close_arena_gumps_on_finish.md
    remove_temporary_walls_on_reset.md
    reset_arena_controller_after_each_match.md
    persist_ladder_only.md
    do_not_persist_temporary_round_state.md
  20_staff_test_override/
    _index.md
    allow_staff_to_bypass_entry_lock.md
    allow_staff_spell_bypass.md
    allow_staff_hidden_observer_mode.md
    allow_staff_forced_ejection_command.md
    allow_staff_instant_reset.md
    allow_staff_duel_start_override.md
    disable_staff_immunity_in_live_testing.md
    log_all_staff_arena_interventions.md
```

## File list

- `docs/systems/arena_rule_legend/README.md`
- `docs/systems/arena_rule_legend/manifest.md`
- `docs/systems/arena_rule_legend/rule_file_template.md`
- `docs/systems/arena_rule_legend/verb_index.md`
- `docs/systems/arena_rule_legend/implementation_status.md`
- `docs/systems/arena_rule_legend/change_log.md`
- `docs/systems/arena_rule_legend/00_implementation_surfaces/_index.md`
- `docs/systems/arena_rule_legend/00_implementation_surfaces/facet_and_map_rules.md`
- `docs/systems/arena_rule_legend/00_implementation_surfaces/region.md`
- `docs/systems/arena_rule_legend/00_implementation_surfaces/guardedregion.md`
- `docs/systems/arena_rule_legend/00_implementation_surfaces/spellhelper_and_travel_validation.md`
- `docs/systems/arena_rule_legend/00_implementation_surfaces/duelcontext_and_rulesets.md`
- `docs/systems/arena_rule_legend/00_implementation_surfaces/moongate.md`
- `docs/systems/arena_rule_legend/00_implementation_surfaces/teleporter.md`
- `docs/systems/arena_rule_legend/00_implementation_surfaces/custom_controller.md`
- `docs/systems/arena_rule_legend/00_implementation_surfaces/match_controller.md`
- `docs/systems/arena_rule_legend/00_implementation_surfaces/arena_state_tracker.md`
- `docs/systems/arena_rule_legend/00_implementation_surfaces/entry_filter.md`
- `docs/systems/arena_rule_legend/00_implementation_surfaces/exit_router.md`
- `docs/systems/arena_rule_legend/00_implementation_surfaces/boundary_validator.md`
- `docs/systems/arena_rule_legend/00_implementation_surfaces/interference_handler.md`
- `docs/systems/arena_rule_legend/00_implementation_surfaces/status_and_progress_tracker.md`
- `docs/systems/arena_rule_legend/01_facet_map_rules/_index.md`
- `docs/systems/arena_rule_legend/01_facet_map_rules/set_arena_facet_to_felucca_rules.md`
- `docs/systems/arena_rule_legend/01_facet_map_rules/set_arena_facet_to_trammel_rules.md`
- `docs/systems/arena_rule_legend/01_facet_map_rules/keep_the_build_world_safe_make_the_arena_world_lethal.md`
- `docs/systems/arena_rule_legend/01_facet_map_rules/use_a_dedicated_arena_facet.md`
- `docs/systems/arena_rule_legend/01_facet_map_rules/mirror_the_arena_on_a_test_facet_first.md`
- `docs/systems/arena_rule_legend/01_facet_map_rules/keep_town_and_vendor_space_protected_outside_arena.md`
- `docs/systems/arena_rule_legend/01_facet_map_rules/decide_facet_law_first_local_arena_law_second.md`
- `docs/systems/arena_rule_legend/02_region_geometry_boundaries/_index.md`
- `docs/systems/arena_rule_legend/02_region_geometry_boundaries/define_arena_rectangle.md`
- `docs/systems/arena_rule_legend/02_region_geometry_boundaries/define_arena_z_floor.md`
- `docs/systems/arena_rule_legend/02_region_geometry_boundaries/define_arena_z_ceiling.md`
- `docs/systems/arena_rule_legend/02_region_geometry_boundaries/set_region_priority.md`
- `docs/systems/arena_rule_legend/02_region_geometry_boundaries/create_inner_ring_and_outer_shell_as_separate_regions.md`
- `docs/systems/arena_rule_legend/02_region_geometry_boundaries/create_staging_room_region.md`
- `docs/systems/arena_rule_legend/02_region_geometry_boundaries/create_spectator_region.md`
- `docs/systems/arena_rule_legend/02_region_geometry_boundaries/add_internal_invisible_wall.md`
- `docs/systems/arena_rule_legend/02_region_geometry_boundaries/bounce_players_back_on_invalid_edge_cross.md`
- `docs/systems/arena_rule_legend/02_region_geometry_boundaries/track_entry_and_exit_events.md`
- `docs/systems/arena_rule_legend/02_region_geometry_boundaries/separate_ring_lobby_and_reward_room.md`
- `docs/systems/arena_rule_legend/03_entry_exit_routing/_index.md`
- `docs/systems/arena_rule_legend/03_entry_exit_routing/add_arena_moongate_in.md`
- `docs/systems/arena_rule_legend/03_entry_exit_routing/add_arena_moongate_out.md`
- `docs/systems/arena_rule_legend/03_entry_exit_routing/add_fallback_teleporter_out.md`
- `docs/systems/arena_rule_legend/03_entry_exit_routing/lock_entry_during_active_match.md`
- `docs/systems/arena_rule_legend/03_entry_exit_routing/allow_entry_only_from_staging_room.md`
- `docs/systems/arena_rule_legend/03_entry_exit_routing/deny_entry_to_non_participants.md`
- `docs/systems/arena_rule_legend/03_entry_exit_routing/deny_re_entry_after_elimination.md`
- `docs/systems/arena_rule_legend/03_entry_exit_routing/send_eliminated_players_to_exit_pad.md`
- `docs/systems/arena_rule_legend/03_entry_exit_routing/route_spectators_to_stands.md`
- `docs/systems/arena_rule_legend/03_entry_exit_routing/route_staff_to_observer_deck.md`
- `docs/systems/arena_rule_legend/03_entry_exit_routing/enable_combat_check_on_exit_teleporter.md`
- `docs/systems/arena_rule_legend/03_entry_exit_routing/enable_criminal_check_on_exit_teleporter.md`
- `docs/systems/arena_rule_legend/04_travel_suppression/_index.md`
- `docs/systems/arena_rule_legend/04_travel_suppression/block_recall_in_bounds.md`
- `docs/systems/arena_rule_legend/04_travel_suppression/block_recall_into_bounds.md`
- `docs/systems/arena_rule_legend/04_travel_suppression/block_gate_travel_in_bounds.md`
- `docs/systems/arena_rule_legend/04_travel_suppression/block_gate_travel_into_bounds.md`
- `docs/systems/arena_rule_legend/04_travel_suppression/block_mark_in_bounds.md`
- `docs/systems/arena_rule_legend/04_travel_suppression/block_teleport_in_bounds.md`
- `docs/systems/arena_rule_legend/04_travel_suppression/block_teleport_while_flagged_in_match.md`
- `docs/systems/arena_rule_legend/04_travel_suppression/block_travel_while_in_combat.md`
- `docs/systems/arena_rule_legend/04_travel_suppression/block_emergency_escape_abilities.md`
- `docs/systems/arena_rule_legend/04_travel_suppression/block_stuck_menu_in_bounds.md`
- `docs/systems/arena_rule_legend/04_travel_suppression/send_invalid_travel_message.md`
- `docs/systems/arena_rule_legend/04_travel_suppression/allow_only_arena_exit_gate_as_legal_travel.md`
- `docs/systems/arena_rule_legend/05_combat_permissions/_index.md`
- `docs/systems/arena_rule_legend/05_combat_permissions/allow_participant_versus_participant_harm.md`
- `docs/systems/arena_rule_legend/05_combat_permissions/deny_outsider_versus_participant_harm.md`
- `docs/systems/arena_rule_legend/05_combat_permissions/deny_participant_versus_outsider_harm.md`
- `docs/systems/arena_rule_legend/05_combat_permissions/enable_friendly_fire.md`
- `docs/systems/arena_rule_legend/05_combat_permissions/disable_friendly_fire.md`
- `docs/systems/arena_rule_legend/05_combat_permissions/allow_pvp_only_after_countdown.md`
- `docs/systems/arena_rule_legend/05_combat_permissions/deny_pre_match_aggression.md`
- `docs/systems/arena_rule_legend/05_combat_permissions/allow_damage_to_summoned_creatures.md`
- `docs/systems/arena_rule_legend/05_combat_permissions/deny_damage_to_summoned_creatures.md`
- `docs/systems/arena_rule_legend/05_combat_permissions/allow_npc_interference.md`
- `docs/systems/arena_rule_legend/05_combat_permissions/deny_npc_interference.md`
- `docs/systems/arena_rule_legend/05_combat_permissions/suppress_harmful_acts_outside_ring_core.md`
- `docs/systems/arena_rule_legend/06_beneficial_healing/_index.md`
- `docs/systems/arena_rule_legend/06_beneficial_healing/allow_self_heal_only.md`
- `docs/systems/arena_rule_legend/06_beneficial_healing/allow_team_heal_only.md`
- `docs/systems/arena_rule_legend/06_beneficial_healing/deny_cross_heal_between_opponents.md`
- `docs/systems/arena_rule_legend/06_beneficial_healing/deny_outsider_healing.md`
- `docs/systems/arena_rule_legend/06_beneficial_healing/deny_outsider_buffs.md`
- `docs/systems/arena_rule_legend/06_beneficial_healing/deny_resurrection_support.md`
- `docs/systems/arena_rule_legend/06_beneficial_healing/allow_bandages.md`
- `docs/systems/arena_rule_legend/06_beneficial_healing/deny_bandages.md`
- `docs/systems/arena_rule_legend/06_beneficial_healing/allow_cure_potions.md`
- `docs/systems/arena_rule_legend/06_beneficial_healing/deny_cure_potions.md`
- `docs/systems/arena_rule_legend/06_beneficial_healing/allow_cleanse_between_rounds_only.md`
- `docs/systems/arena_rule_legend/06_beneficial_healing/scale_healing_received.md`
- `docs/systems/arena_rule_legend/06_beneficial_healing/scale_healing_done.md`
- `docs/systems/arena_rule_legend/06_beneficial_healing/strip_active_buffs_on_entry.md`
- `docs/systems/arena_rule_legend/07_criminal_guard_legal_state/_index.md`
- `docs/systems/arena_rule_legend/07_criminal_guard_legal_state/clear_criminal_state_on_entry.md`
- `docs/systems/arena_rule_legend/07_criminal_guard_legal_state/preserve_criminal_state_on_entry.md`
- `docs/systems/arena_rule_legend/07_criminal_guard_legal_state/disable_guards_in_bounds.md`
- `docs/systems/arena_rule_legend/07_criminal_guard_legal_state/enable_guards_in_bounds.md`
- `docs/systems/arena_rule_legend/07_criminal_guard_legal_state/deny_guard_calls_in_arena.md`
- `docs/systems/arena_rule_legend/07_criminal_guard_legal_state/auto_flag_interference_as_criminal.md`
- `docs/systems/arena_rule_legend/07_criminal_guard_legal_state/remove_aggressions_at_match_start.md`
- `docs/systems/arena_rule_legend/07_criminal_guard_legal_state/reset_combatant_on_round_reset.md`
- `docs/systems/arena_rule_legend/07_criminal_guard_legal_state/clear_yellow_bar_legal_noise_on_entry.md`
- `docs/systems/arena_rule_legend/07_criminal_guard_legal_state/suppress_criminal_warnings_during_sanctioned_duel.md`
- `docs/systems/arena_rule_legend/07_criminal_guard_legal_state/apply_criminal_state_to_ring_jumpers.md`
- `docs/systems/arena_rule_legend/07_criminal_guard_legal_state/force_clean_slate_before_round_one.md`
- `docs/systems/arena_rule_legend/08_team_ally_party/_index.md`
- `docs/systems/arena_rule_legend/08_team_ally_party/one_versus_one_only.md`
- `docs/systems/arena_rule_legend/08_team_ally_party/two_versus_two_only.md`
- `docs/systems/arena_rule_legend/08_team_ally_party/team_versus_team_only.md`
- `docs/systems/arena_rule_legend/08_team_ally_party/free_for_all.md`
- `docs/systems/arena_rule_legend/08_team_ally_party/lock_team_membership_at_ready_up.md`
- `docs/systems/arena_rule_legend/08_team_ally_party/randomize_starting_pads_by_team.md`
- `docs/systems/arena_rule_legend/08_team_ally_party/allow_ally_heals.md`
- `docs/systems/arena_rule_legend/08_team_ally_party/deny_ally_heals.md`
- `docs/systems/arena_rule_legend/08_team_ally_party/enable_friendly_fire_2.md`
- `docs/systems/arena_rule_legend/08_team_ally_party/disable_friendly_fire_2.md`
- `docs/systems/arena_rule_legend/08_team_ally_party/separate_team_staging_rooms.md`
- `docs/systems/arena_rule_legend/08_team_ally_party/disallow_guild_advantages.md`
- `docs/systems/arena_rule_legend/08_team_ally_party/disallow_party_spillover_from_outside_arena.md`
- `docs/systems/arena_rule_legend/09_pets_summons_followers/_index.md`
- `docs/systems/arena_rule_legend/09_pets_summons_followers/disable_pets_in_bounds.md`
- `docs/systems/arena_rule_legend/09_pets_summons_followers/auto_stable_pets_on_entry.md`
- `docs/systems/arena_rule_legend/09_pets_summons_followers/auto_dismiss_summons_on_entry.md`
- `docs/systems/arena_rule_legend/09_pets_summons_followers/deny_new_summons_after_countdown.md`
- `docs/systems/arena_rule_legend/09_pets_summons_followers/allow_pets_only_in_beast_arena_mode.md`
- `docs/systems/arena_rule_legend/09_pets_summons_followers/count_followers_against_eligibility.md`
- `docs/systems/arena_rule_legend/09_pets_summons_followers/deny_outsider_pet_healing.md`
- `docs/systems/arena_rule_legend/09_pets_summons_followers/deny_pet_versus_pet_friendly_fire.md`
- `docs/systems/arena_rule_legend/09_pets_summons_followers/freeze_followers_outside_ring.md`
- `docs/systems/arena_rule_legend/09_pets_summons_followers/bounce_uncontrolled_creatures_out_of_bounds.md`
- `docs/systems/arena_rule_legend/09_pets_summons_followers/strip_escorts_and_companions_at_gate.md`
- `docs/systems/arena_rule_legend/09_pets_summons_followers/allow_only_cosmetic_companions.md`
- `docs/systems/arena_rule_legend/10_mount_movement_locomotion/_index.md`
- `docs/systems/arena_rule_legend/10_mount_movement_locomotion/disable_mounts_in_bounds.md`
- `docs/systems/arena_rule_legend/10_mount_movement_locomotion/auto_dismount_on_entry.md`
- `docs/systems/arena_rule_legend/10_mount_movement_locomotion/deny_remount_in_bounds.md`
- `docs/systems/arena_rule_legend/10_mount_movement_locomotion/freeze_fighters_during_countdown.md`
- `docs/systems/arena_rule_legend/10_mount_movement_locomotion/deny_crossing_start_line_before_begin.md`
- `docs/systems/arena_rule_legend/10_mount_movement_locomotion/deny_wall_clipping_via_teleports.md`
- `docs/systems/arena_rule_legend/10_mount_movement_locomotion/bounce_from_spectator_barrier.md`
- `docs/systems/arena_rule_legend/10_mount_movement_locomotion/reveal_on_boundary_cross.md`
- `docs/systems/arena_rule_legend/10_mount_movement_locomotion/disable_stealth_in_ring.md`
- `docs/systems/arena_rule_legend/10_mount_movement_locomotion/disable_flying_in_ring.md`
- `docs/systems/arena_rule_legend/10_mount_movement_locomotion/slow_movement_in_hazard_zones.md`
- `docs/systems/arena_rule_legend/10_mount_movement_locomotion/force_walk_mode_in_staging_room.md`
- `docs/systems/arena_rule_legend/11_spell_skill_special_move/_index.md`
- `docs/systems/arena_rule_legend/11_spell_skill_special_move/deny_all_spellcasting.md`
- `docs/systems/arena_rule_legend/11_spell_skill_special_move/allow_only_magery.md`
- `docs/systems/arena_rule_legend/11_spell_skill_special_move/allow_only_melee_specials.md`
- `docs/systems/arena_rule_legend/11_spell_skill_special_move/ban_invisibility.md`
- `docs/systems/arena_rule_legend/11_spell_skill_special_move/ban_teleport_spells.md`
- `docs/systems/arena_rule_legend/11_spell_skill_special_move/ban_paralyze.md`
- `docs/systems/arena_rule_legend/11_spell_skill_special_move/ban_resurrection_spells.md`
- `docs/systems/arena_rule_legend/11_spell_skill_special_move/ban_summon_spells.md`
- `docs/systems/arena_rule_legend/11_spell_skill_special_move/ban_field_spells.md`
- `docs/systems/arena_rule_legend/11_spell_skill_special_move/ban_area_of_effect_spells.md`
- `docs/systems/arena_rule_legend/11_spell_skill_special_move/ban_special_moves.md`
- `docs/systems/arena_rule_legend/11_spell_skill_special_move/ban_selected_skills.md`
- `docs/systems/arena_rule_legend/11_spell_skill_special_move/deny_targeting_outside_arena.md`
- `docs/systems/arena_rule_legend/11_spell_skill_special_move/deny_targeting_through_spectator_wall.md`
- `docs/systems/arena_rule_legend/11_spell_skill_special_move/use_whitelist_only_ability_sets.md`
- `docs/systems/arena_rule_legend/11_spell_skill_special_move/use_blacklist_only_ability_sets.md`
- `docs/systems/arena_rule_legend/12_item_equipment_consumable/_index.md`
- `docs/systems/arena_rule_legend/12_item_equipment_consumable/ban_potions.md`
- `docs/systems/arena_rule_legend/12_item_equipment_consumable/allow_potions.md`
- `docs/systems/arena_rule_legend/12_item_equipment_consumable/ban_trapped_boxes.md`
- `docs/systems/arena_rule_legend/12_item_equipment_consumable/ban_wands.md`
- `docs/systems/arena_rule_legend/12_item_equipment_consumable/ban_bolas_and_nets.md`
- `docs/systems/arena_rule_legend/12_item_equipment_consumable/ban_pre_charged_consumables.md`
- `docs/systems/arena_rule_legend/12_item_equipment_consumable/require_weapon_class_lock.md`
- `docs/systems/arena_rule_legend/12_item_equipment_consumable/require_armor_tier_lock.md`
- `docs/systems/arena_rule_legend/12_item_equipment_consumable/require_naked_duel.md`
- `docs/systems/arena_rule_legend/12_item_equipment_consumable/strip_illegal_items_on_entry.md`
- `docs/systems/arena_rule_legend/12_item_equipment_consumable/bounce_illegal_items_to_backpack.md`
- `docs/systems/arena_rule_legend/12_item_equipment_consumable/deny_gear_swap_after_countdown.md`
- `docs/systems/arena_rule_legend/12_item_equipment_consumable/allow_free_consumables.md`
- `docs/systems/arena_rule_legend/12_item_equipment_consumable/consume_normal_resources.md`
- `docs/systems/arena_rule_legend/12_item_equipment_consumable/disable_item_use_except_bandages.md`
- `docs/systems/arena_rule_legend/12_item_equipment_consumable/disable_scroll_use.md`
- `docs/systems/arena_rule_legend/13_death_corpse_resurrection_loot/_index.md`
- `docs/systems/arena_rule_legend/13_death_corpse_resurrection_loot/instant_elimination_on_death.md`
- `docs/systems/arena_rule_legend/13_death_corpse_resurrection_loot/best_of_rounds_elimination.md`
- `docs/systems/arena_rule_legend/13_death_corpse_resurrection_loot/keep_corpse_in_arena.md`
- `docs/systems/arena_rule_legend/13_death_corpse_resurrection_loot/bounce_corpse_contents_out.md`
- `docs/systems/arena_rule_legend/13_death_corpse_resurrection_loot/delete_corpse_on_round_end.md`
- `docs/systems/arena_rule_legend/13_death_corpse_resurrection_loot/deny_in_match_resurrection.md`
- `docs/systems/arena_rule_legend/13_death_corpse_resurrection_loot/auto_resurrect_after_match.md`
- `docs/systems/arena_rule_legend/13_death_corpse_resurrection_loot/send_ghosts_to_spectator_zone.md`
- `docs/systems/arena_rule_legend/13_death_corpse_resurrection_loot/lossless_death_in_test_arena.md`
- `docs/systems/arena_rule_legend/13_death_corpse_resurrection_loot/full_loot_death_in_hardcore_arena.md`
- `docs/systems/arena_rule_legend/13_death_corpse_resurrection_loot/refresh_stats_after_round.md`
- `docs/systems/arena_rule_legend/13_death_corpse_resurrection_loot/strip_buffs_on_revive.md`
- `docs/systems/arena_rule_legend/13_death_corpse_resurrection_loot/restore_saved_loadout_on_rematch.md`
- `docs/systems/arena_rule_legend/14_match_flow_countdown_victory/_index.md`
- `docs/systems/arena_rule_legend/14_match_flow_countdown_victory/require_ready_check.md`
- `docs/systems/arena_rule_legend/14_match_flow_countdown_victory/validate_before_start.md`
- `docs/systems/arena_rule_legend/14_match_flow_countdown_victory/freeze_until_begin_signal.md`
- `docs/systems/arena_rule_legend/14_match_flow_countdown_victory/auto_start_when_both_ready.md`
- `docs/systems/arena_rule_legend/14_match_flow_countdown_victory/auto_forfeit_on_leave.md`
- `docs/systems/arena_rule_legend/14_match_flow_countdown_victory/auto_forfeit_on_disconnect.md`
- `docs/systems/arena_rule_legend/14_match_flow_countdown_victory/best_of_one.md`
- `docs/systems/arena_rule_legend/14_match_flow_countdown_victory/best_of_three.md`
- `docs/systems/arena_rule_legend/14_match_flow_countdown_victory/best_of_five.md`
- `docs/systems/arena_rule_legend/14_match_flow_countdown_victory/last_alive_wins.md`
- `docs/systems/arena_rule_legend/14_match_flow_countdown_victory/first_kill_wins.md`
- `docs/systems/arena_rule_legend/14_match_flow_countdown_victory/hold_the_point_wins.md`
- `docs/systems/arena_rule_legend/14_match_flow_countdown_victory/time_limit_wins.md`
- `docs/systems/arena_rule_legend/14_match_flow_countdown_victory/rematch_prompt_on_finish.md`
- `docs/systems/arena_rule_legend/14_match_flow_countdown_victory/return_fighters_to_lobby_on_finish.md`
- `docs/systems/arena_rule_legend/14_match_flow_countdown_victory/lock_the_ring_while_results_display.md`
- `docs/systems/arena_rule_legend/15_anti_stall_sudden_death/_index.md`
- `docs/systems/arena_rule_legend/15_anti_stall_sudden_death/start_sudden_death_after_n_seconds.md`
- `docs/systems/arena_rule_legend/15_anti_stall_sudden_death/warn_sudden_death_at_n_minus_10.md`
- `docs/systems/arena_rule_legend/15_anti_stall_sudden_death/disable_healing_in_sudden_death.md`
- `docs/systems/arena_rule_legend/15_anti_stall_sudden_death/increase_damage_in_sudden_death.md`
- `docs/systems/arena_rule_legend/15_anti_stall_sudden_death/shrink_safe_area_over_time.md`
- `docs/systems/arena_rule_legend/15_anti_stall_sudden_death/reveal_all_hidden_fighters.md`
- `docs/systems/arena_rule_legend/15_anti_stall_sudden_death/disable_mounts_in_sudden_death.md`
- `docs/systems/arena_rule_legend/15_anti_stall_sudden_death/disable_ranged_only_play_in_sudden_death.md`
- `docs/systems/arena_rule_legend/15_anti_stall_sudden_death/end_as_tie_at_hard_cap.md`
- `docs/systems/arena_rule_legend/15_anti_stall_sudden_death/end_by_damage_leader_at_hard_cap.md`
- `docs/systems/arena_rule_legend/15_anti_stall_sudden_death/collapse_walls_inward.md`
- `docs/systems/arena_rule_legend/15_anti_stall_sudden_death/spawn_hazard_ring.md`
- `docs/systems/arena_rule_legend/16_spectator_outsider_interference/_index.md`
- `docs/systems/arena_rule_legend/16_spectator_outsider_interference/allow_spectators_in_stands_only.md`
- `docs/systems/arena_rule_legend/16_spectator_outsider_interference/deny_spectator_targeting_into_ring.md`
- `docs/systems/arena_rule_legend/16_spectator_outsider_interference/deny_spectator_healing_into_ring.md`
- `docs/systems/arena_rule_legend/16_spectator_outsider_interference/deny_item_toss_into_ring.md`
- `docs/systems/arena_rule_legend/16_spectator_outsider_interference/deny_corpse_looting_by_outsiders.md`
- `docs/systems/arena_rule_legend/16_spectator_outsider_interference/bounce_outsiders_who_cross_line.md`
- `docs/systems/arena_rule_legend/16_spectator_outsider_interference/separate_spectator_and_participant_gates.md`
- `docs/systems/arena_rule_legend/16_spectator_outsider_interference/allow_staff_invisible_observer_mode.md`
- `docs/systems/arena_rule_legend/16_spectator_outsider_interference/hide_spectator_chat_from_fighters.md`
- `docs/systems/arena_rule_legend/16_spectator_outsider_interference/deny_pet_entry_from_spectator_zone.md`
- `docs/systems/arena_rule_legend/16_spectator_outsider_interference/deny_field_effects_crossing_from_stands.md`
- `docs/systems/arena_rule_legend/17_environment_hazard_atmosphere/_index.md`
- `docs/systems/arena_rule_legend/17_environment_hazard_atmosphere/change_local_light_level.md`
- `docs/systems/arena_rule_legend/17_environment_hazard_atmosphere/change_local_music.md`
- `docs/systems/arena_rule_legend/17_environment_hazard_atmosphere/apply_periodic_arena_damage.md`
- `docs/systems/arena_rule_legend/17_environment_hazard_atmosphere/scale_spell_damage_up.md`
- `docs/systems/arena_rule_legend/17_environment_hazard_atmosphere/scale_spell_damage_down.md`
- `docs/systems/arena_rule_legend/17_environment_hazard_atmosphere/scale_melee_damage_up.md`
- `docs/systems/arena_rule_legend/17_environment_hazard_atmosphere/scale_healing_down.md`
- `docs/systems/arena_rule_legend/17_environment_hazard_atmosphere/trigger_trap_tiles.md`
- `docs/systems/arena_rule_legend/17_environment_hazard_atmosphere/trigger_announcer_speech_on_entry.md`
- `docs/systems/arena_rule_legend/17_environment_hazard_atmosphere/trigger_round_start_broadcast.md`
- `docs/systems/arena_rule_legend/17_environment_hazard_atmosphere/spawn_line_of_sight_blockers.md`
- `docs/systems/arena_rule_legend/17_environment_hazard_atmosphere/spawn_temporary_walls.md`
- `docs/systems/arena_rule_legend/17_environment_hazard_atmosphere/turn_ring_into_lava_poison_or_ice_mode.md`
- `docs/systems/arena_rule_legend/17_environment_hazard_atmosphere/add_thematic_weather_and_effects_layer.md`
- `docs/systems/arena_rule_legend/18_npc_spawn_housing_economy/_index.md`
- `docs/systems/arena_rule_legend/18_npc_spawn_housing_economy/deny_housing_in_arena_footprint.md`
- `docs/systems/arena_rule_legend/18_npc_spawn_housing_economy/deny_normal_creature_spawns.md`
- `docs/systems/arena_rule_legend/18_npc_spawn_housing_economy/allow_only_arena_controller_npcs.md`
- `docs/systems/arena_rule_legend/18_npc_spawn_housing_economy/deny_vendor_access_in_bounds.md`
- `docs/systems/arena_rule_legend/18_npc_spawn_housing_economy/disable_banking_in_bounds.md`
- `docs/systems/arena_rule_legend/18_npc_spawn_housing_economy/disable_harvesting_in_bounds.md`
- `docs/systems/arena_rule_legend/18_npc_spawn_housing_economy/despawn_wandering_npcs_on_match_start.md`
- `docs/systems/arena_rule_legend/18_npc_spawn_housing_economy/prevent_escort_and_pathing_through_arena.md`
- `docs/systems/arena_rule_legend/18_npc_spawn_housing_economy/keep_arena_economy_neutral.md`
- `docs/systems/arena_rule_legend/18_npc_spawn_housing_economy/keep_reward_vendors_outside_ring_only.md`
- `docs/systems/arena_rule_legend/19_login_logout_stuck_persistence/_index.md`
- `docs/systems/arena_rule_legend/19_login_logout_stuck_persistence/extend_logout_delay_in_bounds.md`
- `docs/systems/arena_rule_legend/19_login_logout_stuck_persistence/deny_stuck_menu_in_match.md`
- `docs/systems/arena_rule_legend/19_login_logout_stuck_persistence/auto_forfeit_disconnects.md`
- `docs/systems/arena_rule_legend/19_login_logout_stuck_persistence/re_place_reconnecting_fighter_into_lobby.md`
- `docs/systems/arena_rule_legend/19_login_logout_stuck_persistence/eject_offline_characters_from_ring.md`
- `docs/systems/arena_rule_legend/19_login_logout_stuck_persistence/clear_aggressions_on_exit.md`
- `docs/systems/arena_rule_legend/19_login_logout_stuck_persistence/clear_target_and_cursor_state_on_exit.md`
- `docs/systems/arena_rule_legend/19_login_logout_stuck_persistence/close_arena_gumps_on_finish.md`
- `docs/systems/arena_rule_legend/19_login_logout_stuck_persistence/remove_temporary_walls_on_reset.md`
- `docs/systems/arena_rule_legend/19_login_logout_stuck_persistence/reset_arena_controller_after_each_match.md`
- `docs/systems/arena_rule_legend/19_login_logout_stuck_persistence/persist_ladder_only.md`
- `docs/systems/arena_rule_legend/19_login_logout_stuck_persistence/do_not_persist_temporary_round_state.md`
- `docs/systems/arena_rule_legend/20_staff_test_override/_index.md`
- `docs/systems/arena_rule_legend/20_staff_test_override/allow_staff_to_bypass_entry_lock.md`
- `docs/systems/arena_rule_legend/20_staff_test_override/allow_staff_spell_bypass.md`
- `docs/systems/arena_rule_legend/20_staff_test_override/allow_staff_hidden_observer_mode.md`
- `docs/systems/arena_rule_legend/20_staff_test_override/allow_staff_forced_ejection_command.md`
- `docs/systems/arena_rule_legend/20_staff_test_override/allow_staff_instant_reset.md`
- `docs/systems/arena_rule_legend/20_staff_test_override/allow_staff_duel_start_override.md`
- `docs/systems/arena_rule_legend/20_staff_test_override/disable_staff_immunity_in_live_testing.md`
- `docs/systems/arena_rule_legend/20_staff_test_override/log_all_staff_arena_interventions.md`
