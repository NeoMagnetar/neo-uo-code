# Implementation Status

Fast rollup of arena-rule status across the legend subsystem.

Allowed status values: `legend_only`, `planned`, `in_progress`, `implemented`, `tested`, `blocked`, `deprecated`, `replaced`.

| Rule | Chapter | Status | Code touchpoints |
|---|---|---|---|
| [Facet and map rules](00_implementation_surfaces/facet_and_map_rules.md) | Implementation surfaces | legend_only | `docs/systems/arena_rule_legend/*; server/*` |
| [Region](00_implementation_surfaces/region.md) | Implementation surfaces | legend_only | `docs/systems/arena_rule_legend/*; server/*` |
| [GuardedRegion](00_implementation_surfaces/guardedregion.md) | Implementation surfaces | legend_only | `docs/systems/arena_rule_legend/*; server/*` |
| [SpellHelper and travel validation](00_implementation_surfaces/spellhelper_and_travel_validation.md) | Implementation surfaces | legend_only | `docs/systems/arena_rule_legend/*; server/*` |
| [DuelContext and rulesets](00_implementation_surfaces/duelcontext_and_rulesets.md) | Implementation surfaces | legend_only | `docs/systems/arena_rule_legend/*; server/*` |
| [Moongate](00_implementation_surfaces/moongate.md) | Implementation surfaces | legend_only | `docs/systems/arena_rule_legend/*; server/*` |
| [Teleporter](00_implementation_surfaces/teleporter.md) | Implementation surfaces | legend_only | `docs/systems/arena_rule_legend/*; server/*` |
| [Custom controller](00_implementation_surfaces/custom_controller.md) | Implementation surfaces | legend_only | `docs/systems/arena_rule_legend/*; server/*` |
| [Match controller](00_implementation_surfaces/match_controller.md) | Implementation surfaces | legend_only | `docs/systems/arena_rule_legend/*; server/*` |
| [Arena state tracker](00_implementation_surfaces/arena_state_tracker.md) | Implementation surfaces | legend_only | `docs/systems/arena_rule_legend/*; server/*` |
| [Entry filter](00_implementation_surfaces/entry_filter.md) | Implementation surfaces | legend_only | `docs/systems/arena_rule_legend/*; server/*` |
| [Exit router](00_implementation_surfaces/exit_router.md) | Implementation surfaces | legend_only | `docs/systems/arena_rule_legend/*; server/*` |
| [Boundary validator](00_implementation_surfaces/boundary_validator.md) | Implementation surfaces | legend_only | `docs/systems/arena_rule_legend/*; server/*` |
| [Interference handler](00_implementation_surfaces/interference_handler.md) | Implementation surfaces | legend_only | `docs/systems/arena_rule_legend/*; server/*` |
| [Status and progress tracker](00_implementation_surfaces/status_and_progress_tracker.md) | Implementation surfaces | legend_only | `docs/systems/arena_rule_legend/*; server/*` |
| [Set arena facet to Felucca rules](01_facet_map_rules/set_arena_facet_to_felucca_rules.md) | Facet and map rules | legend_only | `server/*/MapDefinitions.cs; server/*/Map.cs` |
| [Set arena facet to Trammel rules](01_facet_map_rules/set_arena_facet_to_trammel_rules.md) | Facet and map rules | legend_only | `server/*/MapDefinitions.cs; server/*/Map.cs` |
| [Keep the build world safe, make the arena world lethal](01_facet_map_rules/keep_the_build_world_safe_make_the_arena_world_lethal.md) | Facet and map rules | legend_only | `server/*/MapDefinitions.cs; server/*/Map.cs` |
| [Use a dedicated arena facet](01_facet_map_rules/use_a_dedicated_arena_facet.md) | Facet and map rules | legend_only | `server/*/MapDefinitions.cs; server/*/Map.cs` |
| [Mirror the arena on a test facet first](01_facet_map_rules/mirror_the_arena_on_a_test_facet_first.md) | Facet and map rules | legend_only | `server/*/MapDefinitions.cs; server/*/Map.cs` |
| [Keep town and vendor space protected outside arena](01_facet_map_rules/keep_town_and_vendor_space_protected_outside_arena.md) | Facet and map rules | legend_only | `server/*/MapDefinitions.cs; server/*/Map.cs` |
| [Decide facet law first, local arena law second](01_facet_map_rules/decide_facet_law_first_local_arena_law_second.md) | Facet and map rules | legend_only | `server/*/MapDefinitions.cs; server/*/Map.cs` |
| [Define arena rectangle](02_region_geometry_boundaries/define_arena_rectangle.md) | Region geometry and boundaries | legend_only | `server/*/Region.cs; server/*/GuardedRegion.cs` |
| [Define arena Z floor](02_region_geometry_boundaries/define_arena_z_floor.md) | Region geometry and boundaries | legend_only | `server/*/Region.cs; server/*/GuardedRegion.cs` |
| [Define arena Z ceiling](02_region_geometry_boundaries/define_arena_z_ceiling.md) | Region geometry and boundaries | legend_only | `server/*/Region.cs; server/*/GuardedRegion.cs` |
| [Set region priority](02_region_geometry_boundaries/set_region_priority.md) | Region geometry and boundaries | legend_only | `server/*/Region.cs; server/*/GuardedRegion.cs` |
| [Create inner ring and outer shell as separate regions](02_region_geometry_boundaries/create_inner_ring_and_outer_shell_as_separate_regions.md) | Region geometry and boundaries | legend_only | `server/*/Region.cs; server/*/GuardedRegion.cs` |
| [Create staging room region](02_region_geometry_boundaries/create_staging_room_region.md) | Region geometry and boundaries | legend_only | `server/*/Region.cs; server/*/GuardedRegion.cs` |
| [Create spectator region](02_region_geometry_boundaries/create_spectator_region.md) | Region geometry and boundaries | legend_only | `server/*/Region.cs; server/*/GuardedRegion.cs` |
| [Add internal invisible wall](02_region_geometry_boundaries/add_internal_invisible_wall.md) | Region geometry and boundaries | legend_only | `server/*/Region.cs; server/*/GuardedRegion.cs` |
| [Bounce players back on invalid edge-cross](02_region_geometry_boundaries/bounce_players_back_on_invalid_edge_cross.md) | Region geometry and boundaries | legend_only | `server/*/Region.cs; server/*/GuardedRegion.cs` |
| [Track entry and exit events](02_region_geometry_boundaries/track_entry_and_exit_events.md) | Region geometry and boundaries | legend_only | `server/*/Region.cs; server/*/GuardedRegion.cs` |
| [Separate ring, lobby, and reward room](02_region_geometry_boundaries/separate_ring_lobby_and_reward_room.md) | Region geometry and boundaries | legend_only | `server/*/Region.cs; server/*/GuardedRegion.cs` |
| [Add arena moongate in](03_entry_exit_routing/add_arena_moongate_in.md) | Entry, exit, and routing | legend_only | `server/*/Moongate*.cs; server/*/Teleporter*.cs` |
| [Add arena moongate out](03_entry_exit_routing/add_arena_moongate_out.md) | Entry, exit, and routing | legend_only | `server/*/Moongate*.cs; server/*/Teleporter*.cs` |
| [Add fallback teleporter out](03_entry_exit_routing/add_fallback_teleporter_out.md) | Entry, exit, and routing | legend_only | `server/*/Moongate*.cs; server/*/Teleporter*.cs` |
| [Lock entry during active match](03_entry_exit_routing/lock_entry_during_active_match.md) | Entry, exit, and routing | legend_only | `server/*/Moongate*.cs; server/*/Teleporter*.cs` |
| [Allow entry only from staging room](03_entry_exit_routing/allow_entry_only_from_staging_room.md) | Entry, exit, and routing | legend_only | `server/*/Moongate*.cs; server/*/Teleporter*.cs` |
| [Deny entry to non-participants](03_entry_exit_routing/deny_entry_to_non_participants.md) | Entry, exit, and routing | legend_only | `server/*/Moongate*.cs; server/*/Teleporter*.cs` |
| [Deny re-entry after elimination](03_entry_exit_routing/deny_re_entry_after_elimination.md) | Entry, exit, and routing | legend_only | `server/*/Moongate*.cs; server/*/Teleporter*.cs` |
| [Send eliminated players to exit pad](03_entry_exit_routing/send_eliminated_players_to_exit_pad.md) | Entry, exit, and routing | legend_only | `server/*/Moongate*.cs; server/*/Teleporter*.cs` |
| [Route spectators to stands](03_entry_exit_routing/route_spectators_to_stands.md) | Entry, exit, and routing | legend_only | `server/*/Moongate*.cs; server/*/Teleporter*.cs` |
| [Route staff to observer deck](03_entry_exit_routing/route_staff_to_observer_deck.md) | Entry, exit, and routing | legend_only | `server/*/Moongate*.cs; server/*/Teleporter*.cs` |
| [Enable combat-check on exit teleporter](03_entry_exit_routing/enable_combat_check_on_exit_teleporter.md) | Entry, exit, and routing | legend_only | `server/*/Moongate*.cs; server/*/Teleporter*.cs` |
| [Enable criminal-check on exit teleporter](03_entry_exit_routing/enable_criminal_check_on_exit_teleporter.md) | Entry, exit, and routing | legend_only | `server/*/Moongate*.cs; server/*/Teleporter*.cs` |
| [Block recall in bounds](04_travel_suppression/block_recall_in_bounds.md) | Travel suppression | legend_only | `server/*/SpellHelper.cs; server/*/Region.cs` |
| [Block recall into bounds](04_travel_suppression/block_recall_into_bounds.md) | Travel suppression | legend_only | `server/*/SpellHelper.cs; server/*/Region.cs` |
| [Block gate travel in bounds](04_travel_suppression/block_gate_travel_in_bounds.md) | Travel suppression | legend_only | `server/*/SpellHelper.cs; server/*/Region.cs` |
| [Block gate travel into bounds](04_travel_suppression/block_gate_travel_into_bounds.md) | Travel suppression | legend_only | `server/*/SpellHelper.cs; server/*/Region.cs` |
| [Block mark in bounds](04_travel_suppression/block_mark_in_bounds.md) | Travel suppression | legend_only | `server/*/SpellHelper.cs; server/*/Region.cs` |
| [Block teleport in bounds](04_travel_suppression/block_teleport_in_bounds.md) | Travel suppression | legend_only | `server/*/SpellHelper.cs; server/*/Region.cs` |
| [Block teleport while flagged in match](04_travel_suppression/block_teleport_while_flagged_in_match.md) | Travel suppression | legend_only | `server/*/SpellHelper.cs; server/*/Region.cs` |
| [Block travel while in combat](04_travel_suppression/block_travel_while_in_combat.md) | Travel suppression | legend_only | `server/*/SpellHelper.cs; server/*/Region.cs` |
| [Block emergency escape abilities](04_travel_suppression/block_emergency_escape_abilities.md) | Travel suppression | legend_only | `server/*/SpellHelper.cs; server/*/Region.cs` |
| [Block stuck menu in bounds](04_travel_suppression/block_stuck_menu_in_bounds.md) | Travel suppression | legend_only | `server/*/SpellHelper.cs; server/*/Region.cs` |
| [Send invalid-travel message](04_travel_suppression/send_invalid_travel_message.md) | Travel suppression | legend_only | `server/*/SpellHelper.cs; server/*/Region.cs` |
| [Allow only arena exit gate as legal travel](04_travel_suppression/allow_only_arena_exit_gate_as_legal_travel.md) | Travel suppression | legend_only | `server/*/SpellHelper.cs; server/*/Region.cs` |
| [Allow participant-versus-participant harm](05_combat_permissions/allow_participant_versus_participant_harm.md) | Combat permissions | legend_only | `server/*/Region.cs; server/*/GuardedRegion.cs` |
| [Deny outsider-versus-participant harm](05_combat_permissions/deny_outsider_versus_participant_harm.md) | Combat permissions | legend_only | `server/*/Region.cs; server/*/GuardedRegion.cs` |
| [Deny participant-versus-outsider harm](05_combat_permissions/deny_participant_versus_outsider_harm.md) | Combat permissions | legend_only | `server/*/Region.cs; server/*/GuardedRegion.cs` |
| [Enable friendly fire](05_combat_permissions/enable_friendly_fire.md) | Combat permissions | legend_only | `server/*/Region.cs; server/*/GuardedRegion.cs` |
| [Disable friendly fire](05_combat_permissions/disable_friendly_fire.md) | Combat permissions | legend_only | `server/*/Region.cs; server/*/GuardedRegion.cs` |
| [Allow PvP only after countdown](05_combat_permissions/allow_pvp_only_after_countdown.md) | Combat permissions | legend_only | `server/*/Region.cs; server/*/GuardedRegion.cs` |
| [Deny pre-match aggression](05_combat_permissions/deny_pre_match_aggression.md) | Combat permissions | legend_only | `server/*/Region.cs; server/*/GuardedRegion.cs` |
| [Allow damage to summoned creatures](05_combat_permissions/allow_damage_to_summoned_creatures.md) | Combat permissions | legend_only | `server/*/Region.cs; server/*/GuardedRegion.cs` |
| [Deny damage to summoned creatures](05_combat_permissions/deny_damage_to_summoned_creatures.md) | Combat permissions | legend_only | `server/*/Region.cs; server/*/GuardedRegion.cs` |
| [Allow NPC interference](05_combat_permissions/allow_npc_interference.md) | Combat permissions | legend_only | `server/*/Region.cs; server/*/GuardedRegion.cs` |
| [Deny NPC interference](05_combat_permissions/deny_npc_interference.md) | Combat permissions | legend_only | `server/*/Region.cs; server/*/GuardedRegion.cs` |
| [Suppress harmful acts outside ring core](05_combat_permissions/suppress_harmful_acts_outside_ring_core.md) | Combat permissions | legend_only | `server/*/Region.cs; server/*/GuardedRegion.cs` |
| [Allow self-heal only](06_beneficial_healing/allow_self_heal_only.md) | Beneficial and healing | legend_only | `server/*/Region.cs; server/*/GuardedRegion.cs` |
| [Allow team-heal only](06_beneficial_healing/allow_team_heal_only.md) | Beneficial and healing | legend_only | `server/*/Region.cs; server/*/GuardedRegion.cs` |
| [Deny cross-heal between opponents](06_beneficial_healing/deny_cross_heal_between_opponents.md) | Beneficial and healing | legend_only | `server/*/Region.cs; server/*/GuardedRegion.cs` |
| [Deny outsider healing](06_beneficial_healing/deny_outsider_healing.md) | Beneficial and healing | legend_only | `server/*/Region.cs; server/*/GuardedRegion.cs` |
| [Deny outsider buffs](06_beneficial_healing/deny_outsider_buffs.md) | Beneficial and healing | legend_only | `server/*/Region.cs; server/*/GuardedRegion.cs` |
| [Deny resurrection support](06_beneficial_healing/deny_resurrection_support.md) | Beneficial and healing | legend_only | `server/*/Region.cs; server/*/GuardedRegion.cs` |
| [Allow bandages](06_beneficial_healing/allow_bandages.md) | Beneficial and healing | legend_only | `server/*/Region.cs; server/*/GuardedRegion.cs` |
| [Deny bandages](06_beneficial_healing/deny_bandages.md) | Beneficial and healing | legend_only | `server/*/Region.cs; server/*/GuardedRegion.cs` |
| [Allow cure potions](06_beneficial_healing/allow_cure_potions.md) | Beneficial and healing | legend_only | `server/*/Region.cs; server/*/GuardedRegion.cs` |
| [Deny cure potions](06_beneficial_healing/deny_cure_potions.md) | Beneficial and healing | legend_only | `server/*/Region.cs; server/*/GuardedRegion.cs` |
| [Allow cleanse between rounds only](06_beneficial_healing/allow_cleanse_between_rounds_only.md) | Beneficial and healing | legend_only | `server/*/Region.cs; server/*/GuardedRegion.cs` |
| [Scale healing received](06_beneficial_healing/scale_healing_received.md) | Beneficial and healing | legend_only | `server/*/Region.cs; server/*/GuardedRegion.cs` |
| [Scale healing done](06_beneficial_healing/scale_healing_done.md) | Beneficial and healing | legend_only | `server/*/Region.cs; server/*/GuardedRegion.cs` |
| [Strip active buffs on entry](06_beneficial_healing/strip_active_buffs_on_entry.md) | Beneficial and healing | legend_only | `server/*/Region.cs; server/*/GuardedRegion.cs` |
| [Clear criminal state on entry](07_criminal_guard_legal_state/clear_criminal_state_on_entry.md) | Criminal, guard, and legal state | legend_only | `server/*/GuardedRegion.cs; server/*/Region.cs` |
| [Preserve criminal state on entry](07_criminal_guard_legal_state/preserve_criminal_state_on_entry.md) | Criminal, guard, and legal state | legend_only | `server/*/GuardedRegion.cs; server/*/Region.cs` |
| [Disable guards in bounds](07_criminal_guard_legal_state/disable_guards_in_bounds.md) | Criminal, guard, and legal state | legend_only | `server/*/GuardedRegion.cs; server/*/Region.cs` |
| [Enable guards in bounds](07_criminal_guard_legal_state/enable_guards_in_bounds.md) | Criminal, guard, and legal state | legend_only | `server/*/GuardedRegion.cs; server/*/Region.cs` |
| [Deny guard calls in arena](07_criminal_guard_legal_state/deny_guard_calls_in_arena.md) | Criminal, guard, and legal state | legend_only | `server/*/GuardedRegion.cs; server/*/Region.cs` |
| [Auto-flag interference as criminal](07_criminal_guard_legal_state/auto_flag_interference_as_criminal.md) | Criminal, guard, and legal state | legend_only | `server/*/GuardedRegion.cs; server/*/Region.cs` |
| [Remove aggressions at match start](07_criminal_guard_legal_state/remove_aggressions_at_match_start.md) | Criminal, guard, and legal state | legend_only | `server/*/GuardedRegion.cs; server/*/Region.cs` |
| [Reset combatant on round reset](07_criminal_guard_legal_state/reset_combatant_on_round_reset.md) | Criminal, guard, and legal state | legend_only | `server/*/GuardedRegion.cs; server/*/Region.cs` |
| [Clear yellow-bar legal noise on entry](07_criminal_guard_legal_state/clear_yellow_bar_legal_noise_on_entry.md) | Criminal, guard, and legal state | legend_only | `server/*/GuardedRegion.cs; server/*/Region.cs` |
| [Suppress criminal warnings during sanctioned duel](07_criminal_guard_legal_state/suppress_criminal_warnings_during_sanctioned_duel.md) | Criminal, guard, and legal state | legend_only | `server/*/GuardedRegion.cs; server/*/Region.cs` |
| [Apply criminal state to ring jumpers](07_criminal_guard_legal_state/apply_criminal_state_to_ring_jumpers.md) | Criminal, guard, and legal state | legend_only | `server/*/GuardedRegion.cs; server/*/Region.cs` |
| [Force clean slate before round one](07_criminal_guard_legal_state/force_clean_slate_before_round_one.md) | Criminal, guard, and legal state | legend_only | `server/*/GuardedRegion.cs; server/*/Region.cs` |
| [One-versus-one only](08_team_ally_party/one_versus_one_only.md) | Team, ally, and party | legend_only | `server/*/DuelContext*.cs; server/*/Party*.cs` |
| [Two-versus-two only](08_team_ally_party/two_versus_two_only.md) | Team, ally, and party | legend_only | `server/*/DuelContext*.cs; server/*/Party*.cs` |
| [Team-versus-team only](08_team_ally_party/team_versus_team_only.md) | Team, ally, and party | legend_only | `server/*/DuelContext*.cs; server/*/Party*.cs` |
| [Free-for-all](08_team_ally_party/free_for_all.md) | Team, ally, and party | legend_only | `server/*/DuelContext*.cs; server/*/Party*.cs` |
| [Lock team membership at ready-up](08_team_ally_party/lock_team_membership_at_ready_up.md) | Team, ally, and party | legend_only | `server/*/DuelContext*.cs; server/*/Party*.cs` |
| [Randomize starting pads by team](08_team_ally_party/randomize_starting_pads_by_team.md) | Team, ally, and party | legend_only | `server/*/DuelContext*.cs; server/*/Party*.cs` |
| [Allow ally heals](08_team_ally_party/allow_ally_heals.md) | Team, ally, and party | legend_only | `server/*/DuelContext*.cs; server/*/Party*.cs` |
| [Deny ally heals](08_team_ally_party/deny_ally_heals.md) | Team, ally, and party | legend_only | `server/*/DuelContext*.cs; server/*/Party*.cs` |
| [Enable friendly fire](08_team_ally_party/enable_friendly_fire_2.md) | Team, ally, and party | legend_only | `server/*/DuelContext*.cs; server/*/Party*.cs` |
| [Disable friendly fire](08_team_ally_party/disable_friendly_fire_2.md) | Team, ally, and party | legend_only | `server/*/DuelContext*.cs; server/*/Party*.cs` |
| [Separate team staging rooms](08_team_ally_party/separate_team_staging_rooms.md) | Team, ally, and party | legend_only | `server/*/DuelContext*.cs; server/*/Party*.cs` |
| [Disallow guild advantages](08_team_ally_party/disallow_guild_advantages.md) | Team, ally, and party | legend_only | `server/*/DuelContext*.cs; server/*/Party*.cs` |
| [Disallow party spillover from outside arena](08_team_ally_party/disallow_party_spillover_from_outside_arena.md) | Team, ally, and party | legend_only | `server/*/DuelContext*.cs; server/*/Party*.cs` |
| [Disable pets in bounds](09_pets_summons_followers/disable_pets_in_bounds.md) | Pets, summons, and followers | legend_only | `server/*/Mobile.cs; server/*/SpellHelper.cs` |
| [Auto-stable pets on entry](09_pets_summons_followers/auto_stable_pets_on_entry.md) | Pets, summons, and followers | legend_only | `server/*/Mobile.cs; server/*/SpellHelper.cs` |
| [Auto-dismiss summons on entry](09_pets_summons_followers/auto_dismiss_summons_on_entry.md) | Pets, summons, and followers | legend_only | `server/*/Mobile.cs; server/*/SpellHelper.cs` |
| [Deny new summons after countdown](09_pets_summons_followers/deny_new_summons_after_countdown.md) | Pets, summons, and followers | legend_only | `server/*/Mobile.cs; server/*/SpellHelper.cs` |
| [Allow pets only in beast arena mode](09_pets_summons_followers/allow_pets_only_in_beast_arena_mode.md) | Pets, summons, and followers | legend_only | `server/*/Mobile.cs; server/*/SpellHelper.cs` |
| [Count followers against eligibility](09_pets_summons_followers/count_followers_against_eligibility.md) | Pets, summons, and followers | legend_only | `server/*/Mobile.cs; server/*/SpellHelper.cs` |
| [Deny outsider pet healing](09_pets_summons_followers/deny_outsider_pet_healing.md) | Pets, summons, and followers | legend_only | `server/*/Mobile.cs; server/*/SpellHelper.cs` |
| [Deny pet-versus-pet friendly fire](09_pets_summons_followers/deny_pet_versus_pet_friendly_fire.md) | Pets, summons, and followers | legend_only | `server/*/Mobile.cs; server/*/SpellHelper.cs` |
| [Freeze followers outside ring](09_pets_summons_followers/freeze_followers_outside_ring.md) | Pets, summons, and followers | legend_only | `server/*/Mobile.cs; server/*/SpellHelper.cs` |
| [Bounce uncontrolled creatures out of bounds](09_pets_summons_followers/bounce_uncontrolled_creatures_out_of_bounds.md) | Pets, summons, and followers | legend_only | `server/*/Mobile.cs; server/*/SpellHelper.cs` |
| [Strip escorts and companions at gate](09_pets_summons_followers/strip_escorts_and_companions_at_gate.md) | Pets, summons, and followers | legend_only | `server/*/Mobile.cs; server/*/SpellHelper.cs` |
| [Allow only cosmetic companions](09_pets_summons_followers/allow_only_cosmetic_companions.md) | Pets, summons, and followers | legend_only | `server/*/Mobile.cs; server/*/SpellHelper.cs` |
| [Disable mounts in bounds](10_mount_movement_locomotion/disable_mounts_in_bounds.md) | Mount, movement, and locomotion | legend_only | `server/*/Region.cs; server/*/Mobile.cs` |
| [Auto-dismount on entry](10_mount_movement_locomotion/auto_dismount_on_entry.md) | Mount, movement, and locomotion | legend_only | `server/*/Region.cs; server/*/Mobile.cs` |
| [Deny remount in bounds](10_mount_movement_locomotion/deny_remount_in_bounds.md) | Mount, movement, and locomotion | legend_only | `server/*/Region.cs; server/*/Mobile.cs` |
| [Freeze fighters during countdown](10_mount_movement_locomotion/freeze_fighters_during_countdown.md) | Mount, movement, and locomotion | legend_only | `server/*/Region.cs; server/*/Mobile.cs` |
| [Deny crossing start line before begin](10_mount_movement_locomotion/deny_crossing_start_line_before_begin.md) | Mount, movement, and locomotion | legend_only | `server/*/Region.cs; server/*/Mobile.cs` |
| [Deny wall clipping via teleports](10_mount_movement_locomotion/deny_wall_clipping_via_teleports.md) | Mount, movement, and locomotion | legend_only | `server/*/Region.cs; server/*/Mobile.cs` |
| [Bounce from spectator barrier](10_mount_movement_locomotion/bounce_from_spectator_barrier.md) | Mount, movement, and locomotion | legend_only | `server/*/Region.cs; server/*/Mobile.cs` |
| [Reveal on boundary cross](10_mount_movement_locomotion/reveal_on_boundary_cross.md) | Mount, movement, and locomotion | legend_only | `server/*/Region.cs; server/*/Mobile.cs` |
| [Disable stealth in ring](10_mount_movement_locomotion/disable_stealth_in_ring.md) | Mount, movement, and locomotion | legend_only | `server/*/Region.cs; server/*/Mobile.cs` |
| [Disable flying in ring](10_mount_movement_locomotion/disable_flying_in_ring.md) | Mount, movement, and locomotion | legend_only | `server/*/Region.cs; server/*/Mobile.cs` |
| [Slow movement in hazard zones](10_mount_movement_locomotion/slow_movement_in_hazard_zones.md) | Mount, movement, and locomotion | legend_only | `server/*/Region.cs; server/*/Mobile.cs` |
| [Force walk mode in staging room](10_mount_movement_locomotion/force_walk_mode_in_staging_room.md) | Mount, movement, and locomotion | legend_only | `server/*/Region.cs; server/*/Mobile.cs` |
| [Deny all spellcasting](11_spell_skill_special_move/deny_all_spellcasting.md) | Spell, skill, and special move | legend_only | `server/*/Region.cs; server/*/SpellHelper.cs` |
| [Allow only magery](11_spell_skill_special_move/allow_only_magery.md) | Spell, skill, and special move | legend_only | `server/*/Region.cs; server/*/SpellHelper.cs` |
| [Allow only melee specials](11_spell_skill_special_move/allow_only_melee_specials.md) | Spell, skill, and special move | legend_only | `server/*/Region.cs; server/*/SpellHelper.cs` |
| [Ban invisibility](11_spell_skill_special_move/ban_invisibility.md) | Spell, skill, and special move | legend_only | `server/*/Region.cs; server/*/SpellHelper.cs` |
| [Ban teleport spells](11_spell_skill_special_move/ban_teleport_spells.md) | Spell, skill, and special move | legend_only | `server/*/Region.cs; server/*/SpellHelper.cs` |
| [Ban paralyze](11_spell_skill_special_move/ban_paralyze.md) | Spell, skill, and special move | legend_only | `server/*/Region.cs; server/*/SpellHelper.cs` |
| [Ban resurrection spells](11_spell_skill_special_move/ban_resurrection_spells.md) | Spell, skill, and special move | legend_only | `server/*/Region.cs; server/*/SpellHelper.cs` |
| [Ban summon spells](11_spell_skill_special_move/ban_summon_spells.md) | Spell, skill, and special move | legend_only | `server/*/Region.cs; server/*/SpellHelper.cs` |
| [Ban field spells](11_spell_skill_special_move/ban_field_spells.md) | Spell, skill, and special move | legend_only | `server/*/Region.cs; server/*/SpellHelper.cs` |
| [Ban area-of-effect spells](11_spell_skill_special_move/ban_area_of_effect_spells.md) | Spell, skill, and special move | legend_only | `server/*/Region.cs; server/*/SpellHelper.cs` |
| [Ban special moves](11_spell_skill_special_move/ban_special_moves.md) | Spell, skill, and special move | legend_only | `server/*/Region.cs; server/*/SpellHelper.cs` |
| [Ban selected skills](11_spell_skill_special_move/ban_selected_skills.md) | Spell, skill, and special move | legend_only | `server/*/Region.cs; server/*/SpellHelper.cs` |
| [Deny targeting outside arena](11_spell_skill_special_move/deny_targeting_outside_arena.md) | Spell, skill, and special move | legend_only | `server/*/Region.cs; server/*/SpellHelper.cs` |
| [Deny targeting through spectator wall](11_spell_skill_special_move/deny_targeting_through_spectator_wall.md) | Spell, skill, and special move | legend_only | `server/*/Region.cs; server/*/SpellHelper.cs` |
| [Use whitelist-only ability sets](11_spell_skill_special_move/use_whitelist_only_ability_sets.md) | Spell, skill, and special move | legend_only | `server/*/Region.cs; server/*/SpellHelper.cs` |
| [Use blacklist-only ability sets](11_spell_skill_special_move/use_blacklist_only_ability_sets.md) | Spell, skill, and special move | legend_only | `server/*/Region.cs; server/*/SpellHelper.cs` |
| [Ban potions](12_item_equipment_consumable/ban_potions.md) | Item, equipment, and consumable | legend_only | `server/*/DuelContext*.cs; server/*/Mobile.cs` |
| [Allow potions](12_item_equipment_consumable/allow_potions.md) | Item, equipment, and consumable | legend_only | `server/*/DuelContext*.cs; server/*/Mobile.cs` |
| [Ban trapped boxes](12_item_equipment_consumable/ban_trapped_boxes.md) | Item, equipment, and consumable | legend_only | `server/*/DuelContext*.cs; server/*/Mobile.cs` |
| [Ban wands](12_item_equipment_consumable/ban_wands.md) | Item, equipment, and consumable | legend_only | `server/*/DuelContext*.cs; server/*/Mobile.cs` |
| [Ban bolas and nets](12_item_equipment_consumable/ban_bolas_and_nets.md) | Item, equipment, and consumable | legend_only | `server/*/DuelContext*.cs; server/*/Mobile.cs` |
| [Ban pre-charged consumables](12_item_equipment_consumable/ban_pre_charged_consumables.md) | Item, equipment, and consumable | legend_only | `server/*/DuelContext*.cs; server/*/Mobile.cs` |
| [Require weapon class lock](12_item_equipment_consumable/require_weapon_class_lock.md) | Item, equipment, and consumable | legend_only | `server/*/DuelContext*.cs; server/*/Mobile.cs` |
| [Require armor tier lock](12_item_equipment_consumable/require_armor_tier_lock.md) | Item, equipment, and consumable | legend_only | `server/*/DuelContext*.cs; server/*/Mobile.cs` |
| [Require naked duel](12_item_equipment_consumable/require_naked_duel.md) | Item, equipment, and consumable | legend_only | `server/*/DuelContext*.cs; server/*/Mobile.cs` |
| [Strip illegal items on entry](12_item_equipment_consumable/strip_illegal_items_on_entry.md) | Item, equipment, and consumable | legend_only | `server/*/DuelContext*.cs; server/*/Mobile.cs` |
| [Bounce illegal items to backpack](12_item_equipment_consumable/bounce_illegal_items_to_backpack.md) | Item, equipment, and consumable | legend_only | `server/*/DuelContext*.cs; server/*/Mobile.cs` |
| [Deny gear swap after countdown](12_item_equipment_consumable/deny_gear_swap_after_countdown.md) | Item, equipment, and consumable | legend_only | `server/*/DuelContext*.cs; server/*/Mobile.cs` |
| [Allow free consumables](12_item_equipment_consumable/allow_free_consumables.md) | Item, equipment, and consumable | legend_only | `server/*/DuelContext*.cs; server/*/Mobile.cs` |
| [Consume normal resources](12_item_equipment_consumable/consume_normal_resources.md) | Item, equipment, and consumable | legend_only | `server/*/DuelContext*.cs; server/*/Mobile.cs` |
| [Disable item use except bandages](12_item_equipment_consumable/disable_item_use_except_bandages.md) | Item, equipment, and consumable | legend_only | `server/*/DuelContext*.cs; server/*/Mobile.cs` |
| [Disable scroll use](12_item_equipment_consumable/disable_scroll_use.md) | Item, equipment, and consumable | legend_only | `server/*/DuelContext*.cs; server/*/Mobile.cs` |
| [Instant elimination on death](13_death_corpse_resurrection_loot/instant_elimination_on_death.md) | Death, corpse, resurrection, and loot | legend_only | `server/*/Region.cs; server/*/Corpse*.cs` |
| [Best-of-rounds elimination](13_death_corpse_resurrection_loot/best_of_rounds_elimination.md) | Death, corpse, resurrection, and loot | legend_only | `server/*/Region.cs; server/*/Corpse*.cs` |
| [Keep corpse in arena](13_death_corpse_resurrection_loot/keep_corpse_in_arena.md) | Death, corpse, resurrection, and loot | legend_only | `server/*/Region.cs; server/*/Corpse*.cs` |
| [Bounce corpse contents out](13_death_corpse_resurrection_loot/bounce_corpse_contents_out.md) | Death, corpse, resurrection, and loot | legend_only | `server/*/Region.cs; server/*/Corpse*.cs` |
| [Delete corpse on round end](13_death_corpse_resurrection_loot/delete_corpse_on_round_end.md) | Death, corpse, resurrection, and loot | legend_only | `server/*/Region.cs; server/*/Corpse*.cs` |
| [Deny in-match resurrection](13_death_corpse_resurrection_loot/deny_in_match_resurrection.md) | Death, corpse, resurrection, and loot | legend_only | `server/*/Region.cs; server/*/Corpse*.cs` |
| [Auto-resurrect after match](13_death_corpse_resurrection_loot/auto_resurrect_after_match.md) | Death, corpse, resurrection, and loot | legend_only | `server/*/Region.cs; server/*/Corpse*.cs` |
| [Send ghosts to spectator zone](13_death_corpse_resurrection_loot/send_ghosts_to_spectator_zone.md) | Death, corpse, resurrection, and loot | legend_only | `server/*/Region.cs; server/*/Corpse*.cs` |
| [Lossless death in test arena](13_death_corpse_resurrection_loot/lossless_death_in_test_arena.md) | Death, corpse, resurrection, and loot | legend_only | `server/*/Region.cs; server/*/Corpse*.cs` |
| [Full-loot death in hardcore arena](13_death_corpse_resurrection_loot/full_loot_death_in_hardcore_arena.md) | Death, corpse, resurrection, and loot | legend_only | `server/*/Region.cs; server/*/Corpse*.cs` |
| [Refresh stats after round](13_death_corpse_resurrection_loot/refresh_stats_after_round.md) | Death, corpse, resurrection, and loot | legend_only | `server/*/Region.cs; server/*/Corpse*.cs` |
| [Strip buffs on revive](13_death_corpse_resurrection_loot/strip_buffs_on_revive.md) | Death, corpse, resurrection, and loot | legend_only | `server/*/Region.cs; server/*/Corpse*.cs` |
| [Restore saved loadout on rematch](13_death_corpse_resurrection_loot/restore_saved_loadout_on_rematch.md) | Death, corpse, resurrection, and loot | legend_only | `server/*/Region.cs; server/*/Corpse*.cs` |
| [Require ready-check](14_match_flow_countdown_victory/require_ready_check.md) | Match flow, countdown, and victory | legend_only | `server/*/DuelContext*.cs; custom/controllers/*Match*` |
| [Validate before start](14_match_flow_countdown_victory/validate_before_start.md) | Match flow, countdown, and victory | legend_only | `server/*/DuelContext*.cs; custom/controllers/*Match*` |
| [Freeze until begin signal](14_match_flow_countdown_victory/freeze_until_begin_signal.md) | Match flow, countdown, and victory | legend_only | `server/*/DuelContext*.cs; custom/controllers/*Match*` |
| [Auto-start when both ready](14_match_flow_countdown_victory/auto_start_when_both_ready.md) | Match flow, countdown, and victory | legend_only | `server/*/DuelContext*.cs; custom/controllers/*Match*` |
| [Auto-forfeit on leave](14_match_flow_countdown_victory/auto_forfeit_on_leave.md) | Match flow, countdown, and victory | legend_only | `server/*/DuelContext*.cs; custom/controllers/*Match*` |
| [Auto-forfeit on disconnect](14_match_flow_countdown_victory/auto_forfeit_on_disconnect.md) | Match flow, countdown, and victory | legend_only | `server/*/DuelContext*.cs; custom/controllers/*Match*` |
| [Best of one](14_match_flow_countdown_victory/best_of_one.md) | Match flow, countdown, and victory | legend_only | `server/*/DuelContext*.cs; custom/controllers/*Match*` |
| [Best of three](14_match_flow_countdown_victory/best_of_three.md) | Match flow, countdown, and victory | legend_only | `server/*/DuelContext*.cs; custom/controllers/*Match*` |
| [Best of five](14_match_flow_countdown_victory/best_of_five.md) | Match flow, countdown, and victory | legend_only | `server/*/DuelContext*.cs; custom/controllers/*Match*` |
| [Last alive wins](14_match_flow_countdown_victory/last_alive_wins.md) | Match flow, countdown, and victory | legend_only | `server/*/DuelContext*.cs; custom/controllers/*Match*` |
| [First kill wins](14_match_flow_countdown_victory/first_kill_wins.md) | Match flow, countdown, and victory | legend_only | `server/*/DuelContext*.cs; custom/controllers/*Match*` |
| [Hold-the-point wins](14_match_flow_countdown_victory/hold_the_point_wins.md) | Match flow, countdown, and victory | legend_only | `server/*/DuelContext*.cs; custom/controllers/*Match*` |
| [Time-limit wins](14_match_flow_countdown_victory/time_limit_wins.md) | Match flow, countdown, and victory | legend_only | `server/*/DuelContext*.cs; custom/controllers/*Match*` |
| [Rematch prompt on finish](14_match_flow_countdown_victory/rematch_prompt_on_finish.md) | Match flow, countdown, and victory | legend_only | `server/*/DuelContext*.cs; custom/controllers/*Match*` |
| [Return fighters to lobby on finish](14_match_flow_countdown_victory/return_fighters_to_lobby_on_finish.md) | Match flow, countdown, and victory | legend_only | `server/*/DuelContext*.cs; custom/controllers/*Match*` |
| [Lock the ring while results display](14_match_flow_countdown_victory/lock_the_ring_while_results_display.md) | Match flow, countdown, and victory | legend_only | `server/*/DuelContext*.cs; custom/controllers/*Match*` |
| [Start sudden death after N seconds](15_anti_stall_sudden_death/start_sudden_death_after_n_seconds.md) | Anti-stall and sudden death | legend_only | `server/*/DuelContext*.cs; custom/controllers/*SuddenDeath*` |
| [Warn sudden death at N-minus-10](15_anti_stall_sudden_death/warn_sudden_death_at_n_minus_10.md) | Anti-stall and sudden death | legend_only | `server/*/DuelContext*.cs; custom/controllers/*SuddenDeath*` |
| [Disable healing in sudden death](15_anti_stall_sudden_death/disable_healing_in_sudden_death.md) | Anti-stall and sudden death | legend_only | `server/*/DuelContext*.cs; custom/controllers/*SuddenDeath*` |
| [Increase damage in sudden death](15_anti_stall_sudden_death/increase_damage_in_sudden_death.md) | Anti-stall and sudden death | legend_only | `server/*/DuelContext*.cs; custom/controllers/*SuddenDeath*` |
| [Shrink safe area over time](15_anti_stall_sudden_death/shrink_safe_area_over_time.md) | Anti-stall and sudden death | legend_only | `server/*/DuelContext*.cs; custom/controllers/*SuddenDeath*` |
| [Reveal all hidden fighters](15_anti_stall_sudden_death/reveal_all_hidden_fighters.md) | Anti-stall and sudden death | legend_only | `server/*/DuelContext*.cs; custom/controllers/*SuddenDeath*` |
| [Disable mounts in sudden death](15_anti_stall_sudden_death/disable_mounts_in_sudden_death.md) | Anti-stall and sudden death | legend_only | `server/*/DuelContext*.cs; custom/controllers/*SuddenDeath*` |
| [Disable ranged-only play in sudden death](15_anti_stall_sudden_death/disable_ranged_only_play_in_sudden_death.md) | Anti-stall and sudden death | legend_only | `server/*/DuelContext*.cs; custom/controllers/*SuddenDeath*` |
| [End as tie at hard cap](15_anti_stall_sudden_death/end_as_tie_at_hard_cap.md) | Anti-stall and sudden death | legend_only | `server/*/DuelContext*.cs; custom/controllers/*SuddenDeath*` |
| [End by damage leader at hard cap](15_anti_stall_sudden_death/end_by_damage_leader_at_hard_cap.md) | Anti-stall and sudden death | legend_only | `server/*/DuelContext*.cs; custom/controllers/*SuddenDeath*` |
| [Collapse walls inward](15_anti_stall_sudden_death/collapse_walls_inward.md) | Anti-stall and sudden death | legend_only | `server/*/DuelContext*.cs; custom/controllers/*SuddenDeath*` |
| [Spawn hazard ring](15_anti_stall_sudden_death/spawn_hazard_ring.md) | Anti-stall and sudden death | legend_only | `server/*/DuelContext*.cs; custom/controllers/*SuddenDeath*` |
| [Allow spectators in stands only](16_spectator_outsider_interference/allow_spectators_in_stands_only.md) | Spectator, outsider, and interference | legend_only | `server/*/Region.cs; custom/controllers/*Interference*` |
| [Deny spectator targeting into ring](16_spectator_outsider_interference/deny_spectator_targeting_into_ring.md) | Spectator, outsider, and interference | legend_only | `server/*/Region.cs; custom/controllers/*Interference*` |
| [Deny spectator healing into ring](16_spectator_outsider_interference/deny_spectator_healing_into_ring.md) | Spectator, outsider, and interference | legend_only | `server/*/Region.cs; custom/controllers/*Interference*` |
| [Deny item toss into ring](16_spectator_outsider_interference/deny_item_toss_into_ring.md) | Spectator, outsider, and interference | legend_only | `server/*/Region.cs; custom/controllers/*Interference*` |
| [Deny corpse looting by outsiders](16_spectator_outsider_interference/deny_corpse_looting_by_outsiders.md) | Spectator, outsider, and interference | legend_only | `server/*/Region.cs; custom/controllers/*Interference*` |
| [Bounce outsiders who cross line](16_spectator_outsider_interference/bounce_outsiders_who_cross_line.md) | Spectator, outsider, and interference | legend_only | `server/*/Region.cs; custom/controllers/*Interference*` |
| [Separate spectator and participant gates](16_spectator_outsider_interference/separate_spectator_and_participant_gates.md) | Spectator, outsider, and interference | legend_only | `server/*/Region.cs; custom/controllers/*Interference*` |
| [Allow staff invisible observer mode](16_spectator_outsider_interference/allow_staff_invisible_observer_mode.md) | Spectator, outsider, and interference | legend_only | `server/*/Region.cs; custom/controllers/*Interference*` |
| [Hide spectator chat from fighters](16_spectator_outsider_interference/hide_spectator_chat_from_fighters.md) | Spectator, outsider, and interference | legend_only | `server/*/Region.cs; custom/controllers/*Interference*` |
| [Deny pet entry from spectator zone](16_spectator_outsider_interference/deny_pet_entry_from_spectator_zone.md) | Spectator, outsider, and interference | legend_only | `server/*/Region.cs; custom/controllers/*Interference*` |
| [Deny field effects crossing from stands](16_spectator_outsider_interference/deny_field_effects_crossing_from_stands.md) | Spectator, outsider, and interference | legend_only | `server/*/Region.cs; custom/controllers/*Interference*` |
| [Change local light level](17_environment_hazard_atmosphere/change_local_light_level.md) | Environment, hazard, and atmosphere | legend_only | `server/*/Region.cs; custom/controllers/*Hazard*` |
| [Change local music](17_environment_hazard_atmosphere/change_local_music.md) | Environment, hazard, and atmosphere | legend_only | `server/*/Region.cs; custom/controllers/*Hazard*` |
| [Apply periodic arena damage](17_environment_hazard_atmosphere/apply_periodic_arena_damage.md) | Environment, hazard, and atmosphere | legend_only | `server/*/Region.cs; custom/controllers/*Hazard*` |
| [Scale spell damage up](17_environment_hazard_atmosphere/scale_spell_damage_up.md) | Environment, hazard, and atmosphere | legend_only | `server/*/Region.cs; custom/controllers/*Hazard*` |
| [Scale spell damage down](17_environment_hazard_atmosphere/scale_spell_damage_down.md) | Environment, hazard, and atmosphere | legend_only | `server/*/Region.cs; custom/controllers/*Hazard*` |
| [Scale melee damage up](17_environment_hazard_atmosphere/scale_melee_damage_up.md) | Environment, hazard, and atmosphere | legend_only | `server/*/Region.cs; custom/controllers/*Hazard*` |
| [Scale healing down](17_environment_hazard_atmosphere/scale_healing_down.md) | Environment, hazard, and atmosphere | legend_only | `server/*/Region.cs; custom/controllers/*Hazard*` |
| [Trigger trap tiles](17_environment_hazard_atmosphere/trigger_trap_tiles.md) | Environment, hazard, and atmosphere | legend_only | `server/*/Region.cs; custom/controllers/*Hazard*` |
| [Trigger announcer speech on entry](17_environment_hazard_atmosphere/trigger_announcer_speech_on_entry.md) | Environment, hazard, and atmosphere | legend_only | `server/*/Region.cs; custom/controllers/*Hazard*` |
| [Trigger round-start broadcast](17_environment_hazard_atmosphere/trigger_round_start_broadcast.md) | Environment, hazard, and atmosphere | legend_only | `server/*/Region.cs; custom/controllers/*Hazard*` |
| [Spawn line-of-sight blockers](17_environment_hazard_atmosphere/spawn_line_of_sight_blockers.md) | Environment, hazard, and atmosphere | legend_only | `server/*/Region.cs; custom/controllers/*Hazard*` |
| [Spawn temporary walls](17_environment_hazard_atmosphere/spawn_temporary_walls.md) | Environment, hazard, and atmosphere | legend_only | `server/*/Region.cs; custom/controllers/*Hazard*` |
| [Turn ring into lava, poison, or ice mode](17_environment_hazard_atmosphere/turn_ring_into_lava_poison_or_ice_mode.md) | Environment, hazard, and atmosphere | legend_only | `server/*/Region.cs; custom/controllers/*Hazard*` |
| [Add thematic weather and effects layer](17_environment_hazard_atmosphere/add_thematic_weather_and_effects_layer.md) | Environment, hazard, and atmosphere | legend_only | `server/*/Region.cs; custom/controllers/*Hazard*` |
| [Deny housing in arena footprint](18_npc_spawn_housing_economy/deny_housing_in_arena_footprint.md) | NPC, spawn, housing, and economy | legend_only | `server/*/Region.cs; server/*/GuardedRegion.cs` |
| [Deny normal creature spawns](18_npc_spawn_housing_economy/deny_normal_creature_spawns.md) | NPC, spawn, housing, and economy | legend_only | `server/*/Region.cs; server/*/GuardedRegion.cs` |
| [Allow only arena-controller NPCs](18_npc_spawn_housing_economy/allow_only_arena_controller_npcs.md) | NPC, spawn, housing, and economy | legend_only | `server/*/Region.cs; server/*/GuardedRegion.cs` |
| [Deny vendor access in bounds](18_npc_spawn_housing_economy/deny_vendor_access_in_bounds.md) | NPC, spawn, housing, and economy | legend_only | `server/*/Region.cs; server/*/GuardedRegion.cs` |
| [Disable banking in bounds](18_npc_spawn_housing_economy/disable_banking_in_bounds.md) | NPC, spawn, housing, and economy | legend_only | `server/*/Region.cs; server/*/GuardedRegion.cs` |
| [Disable harvesting in bounds](18_npc_spawn_housing_economy/disable_harvesting_in_bounds.md) | NPC, spawn, housing, and economy | legend_only | `server/*/Region.cs; server/*/GuardedRegion.cs` |
| [Despawn wandering NPCs on match start](18_npc_spawn_housing_economy/despawn_wandering_npcs_on_match_start.md) | NPC, spawn, housing, and economy | legend_only | `server/*/Region.cs; server/*/GuardedRegion.cs` |
| [Prevent escort and pathing through arena](18_npc_spawn_housing_economy/prevent_escort_and_pathing_through_arena.md) | NPC, spawn, housing, and economy | legend_only | `server/*/Region.cs; server/*/GuardedRegion.cs` |
| [Keep arena economy-neutral](18_npc_spawn_housing_economy/keep_arena_economy_neutral.md) | NPC, spawn, housing, and economy | legend_only | `server/*/Region.cs; server/*/GuardedRegion.cs` |
| [Keep reward vendors outside ring only](18_npc_spawn_housing_economy/keep_reward_vendors_outside_ring_only.md) | NPC, spawn, housing, and economy | legend_only | `server/*/Region.cs; server/*/GuardedRegion.cs` |
| [Extend logout delay in bounds](19_login_logout_stuck_persistence/extend_logout_delay_in_bounds.md) | Login, logout, stuck, and persistence | legend_only | `server/*/Region.cs; server/*/DuelContext*.cs` |
| [Deny stuck menu in match](19_login_logout_stuck_persistence/deny_stuck_menu_in_match.md) | Login, logout, stuck, and persistence | legend_only | `server/*/Region.cs; server/*/DuelContext*.cs` |
| [Auto-forfeit disconnects](19_login_logout_stuck_persistence/auto_forfeit_disconnects.md) | Login, logout, stuck, and persistence | legend_only | `server/*/Region.cs; server/*/DuelContext*.cs` |
| [Re-place reconnecting fighter into lobby](19_login_logout_stuck_persistence/re_place_reconnecting_fighter_into_lobby.md) | Login, logout, stuck, and persistence | legend_only | `server/*/Region.cs; server/*/DuelContext*.cs` |
| [Eject offline characters from ring](19_login_logout_stuck_persistence/eject_offline_characters_from_ring.md) | Login, logout, stuck, and persistence | legend_only | `server/*/Region.cs; server/*/DuelContext*.cs` |
| [Clear aggressions on exit](19_login_logout_stuck_persistence/clear_aggressions_on_exit.md) | Login, logout, stuck, and persistence | legend_only | `server/*/Region.cs; server/*/DuelContext*.cs` |
| [Clear target and cursor state on exit](19_login_logout_stuck_persistence/clear_target_and_cursor_state_on_exit.md) | Login, logout, stuck, and persistence | legend_only | `server/*/Region.cs; server/*/DuelContext*.cs` |
| [Close arena gumps on finish](19_login_logout_stuck_persistence/close_arena_gumps_on_finish.md) | Login, logout, stuck, and persistence | legend_only | `server/*/Region.cs; server/*/DuelContext*.cs` |
| [Remove temporary walls on reset](19_login_logout_stuck_persistence/remove_temporary_walls_on_reset.md) | Login, logout, stuck, and persistence | legend_only | `server/*/Region.cs; server/*/DuelContext*.cs` |
| [Reset arena controller after each match](19_login_logout_stuck_persistence/reset_arena_controller_after_each_match.md) | Login, logout, stuck, and persistence | legend_only | `server/*/Region.cs; server/*/DuelContext*.cs` |
| [Persist ladder only](19_login_logout_stuck_persistence/persist_ladder_only.md) | Login, logout, stuck, and persistence | legend_only | `server/*/Region.cs; server/*/DuelContext*.cs` |
| [Do not persist temporary round state](19_login_logout_stuck_persistence/do_not_persist_temporary_round_state.md) | Login, logout, stuck, and persistence | legend_only | `server/*/Region.cs; server/*/DuelContext*.cs` |
| [Allow staff to bypass entry lock](20_staff_test_override/allow_staff_to_bypass_entry_lock.md) | Staff, test, and override | legend_only | `custom/controllers/*Arena*; custom/controllers/*Admin*` |
| [Allow staff spell bypass](20_staff_test_override/allow_staff_spell_bypass.md) | Staff, test, and override | legend_only | `custom/controllers/*Arena*; custom/controllers/*Admin*` |
| [Allow staff hidden observer mode](20_staff_test_override/allow_staff_hidden_observer_mode.md) | Staff, test, and override | legend_only | `custom/controllers/*Arena*; custom/controllers/*Admin*` |
| [Allow staff forced ejection command](20_staff_test_override/allow_staff_forced_ejection_command.md) | Staff, test, and override | legend_only | `custom/controllers/*Arena*; custom/controllers/*Admin*` |
| [Allow staff instant reset](20_staff_test_override/allow_staff_instant_reset.md) | Staff, test, and override | legend_only | `custom/controllers/*Arena*; custom/controllers/*Admin*` |
| [Allow staff duel-start override](20_staff_test_override/allow_staff_duel_start_override.md) | Staff, test, and override | legend_only | `custom/controllers/*Arena*; custom/controllers/*Admin*` |
| [Disable staff immunity in live testing](20_staff_test_override/disable_staff_immunity_in_live_testing.md) | Staff, test, and override | legend_only | `custom/controllers/*Arena*; custom/controllers/*Admin*` |
| [Log all staff arena interventions](20_staff_test_override/log_all_staff_arena_interventions.md) | Staff, test, and override | legend_only | `custom/controllers/*Arena*; custom/controllers/*Admin*` |
