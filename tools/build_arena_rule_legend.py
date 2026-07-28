from pathlib import Path
from collections import defaultdict
from datetime import date

ROOT = Path(__file__).resolve().parents[1] / "docs" / "systems" / "arena_rule_legend"
TODAY = "2026-04-11"
AGENT = "OpenClaw"

SURFACES = [
    "Facet and map rules",
    "Region",
    "GuardedRegion",
    "SpellHelper and travel validation",
    "DuelContext and rulesets",
    "Moongate",
    "Teleporter",
    "Custom controller",
    "Match controller",
    "Arena state tracker",
    "Entry filter",
    "Exit router",
    "Boundary validator",
    "Interference handler",
    "Status and progress tracker",
]

CHAPTERS = [
    {
        "folder": "00_implementation_surfaces",
        "title": "Implementation surfaces",
        "summary": "Top-level map of the engine and shard surfaces likely to carry arena behavior. Use this chapter to orient future rule placement before opening deeper domain chapters.",
        "governs": "This chapter governs where arena rules probably belong in code: map/facet rules, region law, guarded/legal behavior, travel validation, duel context, gates, teleporters, and custom controllers.",
        "surfaces": ["Region", "GuardedRegion", "SpellHelper", "DuelContext", "Moongate", "Teleporter", "Custom controllers"],
        "rules": [
            "Facet and map rules",
            "Region",
            "GuardedRegion",
            "SpellHelper and travel validation",
            "DuelContext and rulesets",
            "Moongate",
            "Teleporter",
            "Custom controller",
            "Match controller",
            "Arena state tracker",
            "Entry filter",
            "Exit router",
            "Boundary validator",
            "Interference handler",
            "Status and progress tracker",
        ],
    },
    {
        "folder": "01_facet_map_rules",
        "title": "Facet and map rules",
        "summary": "Baseline legal model for the arena before local overrides. These rules decide whether the broader environment behaves more like Felucca, Trammel, or a dedicated arena facet before regional law is layered on top.",
        "governs": "This chapter governs shard-wide or facet-wide PvP posture, safe-vs-lethal world selection, test-facet mirroring, and the relationship between global law and local arena law.",
        "surfaces": ["Map rules", "Facet definitions", "MapDefinitions", "Custom controller"],
        "rules": [
            "Set arena facet to Felucca rules",
            "Set arena facet to Trammel rules",
            "Keep the build world safe, make the arena world lethal",
            "Use a dedicated arena facet",
            "Mirror the arena on a test facet first",
            "Keep town and vendor space protected outside arena",
            "Decide facet law first, local arena law second",
        ],
    },
    {
        "folder": "02_region_geometry_boundaries",
        "title": "Region geometry and boundaries",
        "summary": "Physical definition of arena space. These rules establish bounds, z-limits, containment layers, and boundary behavior for rings, lobbies, shells, and observer spaces.",
        "governs": "This chapter governs where the arena begins and ends, how nested regions are structured, and what happens when players or creatures touch invalid edges.",
        "surfaces": ["Region", "Boundary validator", "DuelContext", "Internal wall controllers"],
        "rules": [
            "Define arena rectangle",
            "Define arena Z floor",
            "Define arena Z ceiling",
            "Set region priority",
            "Create inner ring and outer shell as separate regions",
            "Create staging room region",
            "Create spectator region",
            "Add internal invisible wall",
            "Bounce players back on invalid edge-cross",
            "Track entry and exit events",
            "Separate ring, lobby, and reward room",
        ],
    },
    {
        "folder": "03_entry_exit_routing",
        "title": "Entry, exit, and routing",
        "summary": "Ingress and egress rules for fighters, spectators, staff, and eliminated players. This chapter turns abstract arena spaces into controlled movement routes.",
        "governs": "This chapter governs who may enter, from where they may enter, where they are routed after elimination, and how exit gates and teleporters enforce legality.",
        "surfaces": ["Moongate", "Teleporter", "Entry filter", "Exit router", "RegionControl"],
        "rules": [
            "Add arena moongate in",
            "Add arena moongate out",
            "Add fallback teleporter out",
            "Lock entry during active match",
            "Allow entry only from staging room",
            "Deny entry to non-participants",
            "Deny re-entry after elimination",
            "Send eliminated players to exit pad",
            "Route spectators to stands",
            "Route staff to observer deck",
            "Enable combat-check on exit teleporter",
            "Enable criminal-check on exit teleporter",
        ],
    },
    {
        "folder": "04_travel_suppression",
        "title": "Travel suppression",
        "summary": "Recall, gate, mark, teleport, combat-travel, and emergency escape controls. This chapter keeps arena routing explicit instead of porous.",
        "governs": "This chapter governs legal and illegal travel in or around the arena, including spell-based movement, stuck handling, and user-facing invalid-travel messaging.",
        "surfaces": ["SpellHelper", "Travel validation", "Region", "Moongate", "Teleporter"],
        "rules": [
            "Block recall in bounds",
            "Block recall into bounds",
            "Block gate travel in bounds",
            "Block gate travel into bounds",
            "Block mark in bounds",
            "Block teleport in bounds",
            "Block teleport while flagged in match",
            "Block travel while in combat",
            "Block emergency escape abilities",
            "Block stuck menu in bounds",
            "Send invalid-travel message",
            "Allow only arena exit gate as legal travel",
        ],
    },
    {
        "folder": "05_combat_permissions",
        "title": "Combat permissions",
        "summary": "Harmful action law for participants, outsiders, summoned units, and ring edges. This chapter defines who may damage whom and when aggression becomes legal.",
        "governs": "This chapter governs participant-vs-participant harm, outsider interference, friendly fire posture, countdown aggression, summon damage, and suppression of harmful acts outside core combat space.",
        "surfaces": ["Region", "GuardedRegion", "DuelContext", "Combat permission hooks"],
        "rules": [
            "Allow participant-versus-participant harm",
            "Deny outsider-versus-participant harm",
            "Deny participant-versus-outsider harm",
            "Enable friendly fire",
            "Disable friendly fire",
            "Allow PvP only after countdown",
            "Deny pre-match aggression",
            "Allow damage to summoned creatures",
            "Deny damage to summoned creatures",
            "Allow NPC interference",
            "Deny NPC interference",
            "Suppress harmful acts outside ring core",
        ],
    },
    {
        "folder": "06_beneficial_healing",
        "title": "Beneficial and healing",
        "summary": "Support-effect governance for heals, buffs, cures, bandages, resurrection support, and round-to-round cleanse timing.",
        "governs": "This chapter governs beneficial actions across self, team, opponents, outsiders, and consumable healing systems, including scaling and buff stripping.",
        "surfaces": ["Region", "GuardedRegion", "OnHeal hooks", "Buff controllers"],
        "rules": [
            "Allow self-heal only",
            "Allow team-heal only",
            "Deny cross-heal between opponents",
            "Deny outsider healing",
            "Deny outsider buffs",
            "Deny resurrection support",
            "Allow bandages",
            "Deny bandages",
            "Allow cure potions",
            "Deny cure potions",
            "Allow cleanse between rounds only",
            "Scale healing received",
            "Scale healing done",
            "Strip active buffs on entry",
        ],
    },
    {
        "folder": "07_criminal_guard_legal_state",
        "title": "Criminal, guard, and legal state",
        "summary": "Arena legal-state posture: guards, criminal flags, aggression cleanup, sanctioned-duel messaging, and clean-slate enforcement.",
        "governs": "This chapter governs criminal preservation or clearing, guard access, aggressor cleanup, warning suppression, and legal-state reactions to interference or ring jumping.",
        "surfaces": ["GuardedRegion", "Region", "Mobile legal state", "DuelContext"],
        "rules": [
            "Clear criminal state on entry",
            "Preserve criminal state on entry",
            "Disable guards in bounds",
            "Enable guards in bounds",
            "Deny guard calls in arena",
            "Auto-flag interference as criminal",
            "Remove aggressions at match start",
            "Reset combatant on round reset",
            "Clear yellow-bar legal noise on entry",
            "Suppress criminal warnings during sanctioned duel",
            "Apply criminal state to ring jumpers",
            "Force clean slate before round one",
        ],
    },
    {
        "folder": "08_team_ally_party",
        "title": "Team, ally, and party",
        "summary": "Match topology rules for solo, duo, team, and free-for-all structures, including staging, ally logic, and party isolation from the outside world.",
        "governs": "This chapter governs legal participant grouping, team locking, ally behavior, starting-pad assignment, and leakage of guild or party advantages into arena matches.",
        "surfaces": ["DuelContext", "Party checks", "Team controllers", "Ready-up flow"],
        "rules": [
            "One-versus-one only",
            "Two-versus-two only",
            "Team-versus-team only",
            "Free-for-all",
            "Lock team membership at ready-up",
            "Randomize starting pads by team",
            "Allow ally heals",
            "Deny ally heals",
            "Enable friendly fire",
            "Disable friendly fire",
            "Separate team staging rooms",
            "Disallow guild advantages",
            "Disallow party spillover from outside arena",
        ],
    },
    {
        "folder": "09_pets_summons_followers",
        "title": "Pets, summons, and followers",
        "summary": "Follower-side governance for bonded pets, summons, escorts, companions, and uncontrolled creatures that could distort arena fairness.",
        "governs": "This chapter governs whether pets and summons may enter, remain, heal, attack, count toward eligibility, or be forcibly removed from the arena space.",
        "surfaces": ["Mobile follower counts", "Summon handling", "Region", "Entry filter"],
        "rules": [
            "Disable pets in bounds",
            "Auto-stable pets on entry",
            "Auto-dismiss summons on entry",
            "Deny new summons after countdown",
            "Allow pets only in beast arena mode",
            "Count followers against eligibility",
            "Deny outsider pet healing",
            "Deny pet-versus-pet friendly fire",
            "Freeze followers outside ring",
            "Bounce uncontrolled creatures out of bounds",
            "Strip escorts and companions at gate",
            "Allow only cosmetic companions",
        ],
    },
    {
        "folder": "10_mount_movement_locomotion",
        "title": "Mount, movement, and locomotion",
        "summary": "Mobility posture rules for mounts, countdown freezing, start-line discipline, stealth/flying limits, and movement behavior in staging or hazard zones.",
        "governs": "This chapter governs mounted state, remounting, movement suppression or scaling, wall-clipping prevention, and visibility changes triggered by boundary behavior.",
        "surfaces": ["Region movement hooks", "Mount checks", "Boundary validator", "Custom locomotion controllers"],
        "rules": [
            "Disable mounts in bounds",
            "Auto-dismount on entry",
            "Deny remount in bounds",
            "Freeze fighters during countdown",
            "Deny crossing start line before begin",
            "Deny wall clipping via teleports",
            "Bounce from spectator barrier",
            "Reveal on boundary cross",
            "Disable stealth in ring",
            "Disable flying in ring",
            "Slow movement in hazard zones",
            "Force walk mode in staging room",
        ],
    },
    {
        "folder": "11_spell_skill_special_move",
        "title": "Spell, skill, and special move",
        "summary": "Ability-set curation for spells, skills, special moves, targeting restrictions, and whitelist/blacklist models.",
        "governs": "This chapter governs what participants may cast, target, activate, or suppress while inside the arena, including highly specific bans and broad ability models.",
        "surfaces": ["Region spell hooks", "OnSkillUse", "Target validation", "DuelContext rulesets"],
        "rules": [
            "Deny all spellcasting",
            "Allow only magery",
            "Allow only melee specials",
            "Ban invisibility",
            "Ban teleport spells",
            "Ban paralyze",
            "Ban resurrection spells",
            "Ban summon spells",
            "Ban field spells",
            "Ban area-of-effect spells",
            "Ban special moves",
            "Ban selected skills",
            "Deny targeting outside arena",
            "Deny targeting through spectator wall",
            "Use whitelist-only ability sets",
            "Use blacklist-only ability sets",
        ],
    },
    {
        "folder": "12_item_equipment_consumable",
        "title": "Item, equipment, and consumable",
        "summary": "Loadout governance for potions, gear locks, naked duels, consumable posture, bandage exceptions, and legal-vs-illegal equipment handling.",
        "governs": "This chapter governs what may be equipped or used before and during a match, what gets stripped or bounced to backpack, and whether consumables are free or normal-cost.",
        "surfaces": ["DuelContext", "Item equip checks", "Item use checks", "Backpack routing"],
        "rules": [
            "Ban potions",
            "Allow potions",
            "Ban trapped boxes",
            "Ban wands",
            "Ban bolas and nets",
            "Ban pre-charged consumables",
            "Require weapon class lock",
            "Require armor tier lock",
            "Require naked duel",
            "Strip illegal items on entry",
            "Bounce illegal items to backpack",
            "Deny gear swap after countdown",
            "Allow free consumables",
            "Consume normal resources",
            "Disable item use except bandages",
            "Disable scroll use",
        ],
    },
    {
        "folder": "13_death_corpse_resurrection_loot",
        "title": "Death, corpse, resurrection, and loot",
        "summary": "Post-death meaning inside the arena: elimination style, corpse treatment, ghost routing, post-match resurrection, loot posture, and rematch restoration.",
        "governs": "This chapter governs what happens when a fighter dies, where ghosts or corpses go, whether resurrection is legal in-match, and what gets restored after rounds or rematches.",
        "surfaces": ["OnDeath", "OnResurrect", "Corpse handling", "DuelContext cleanup"],
        "rules": [
            "Instant elimination on death",
            "Best-of-rounds elimination",
            "Keep corpse in arena",
            "Bounce corpse contents out",
            "Delete corpse on round end",
            "Deny in-match resurrection",
            "Auto-resurrect after match",
            "Send ghosts to spectator zone",
            "Lossless death in test arena",
            "Full-loot death in hardcore arena",
            "Refresh stats after round",
            "Strip buffs on revive",
            "Restore saved loadout on rematch",
        ],
    },
    {
        "folder": "14_match_flow_countdown_victory",
        "title": "Match flow, countdown, and victory",
        "summary": "Core game loop rules for readiness, validation, countdown, victory conditions, forfeits, rematches, and ring lock timing.",
        "governs": "This chapter governs how matches begin, progress, resolve, and return players to lobby or rematch flow, including best-of variants and win conditions.",
        "surfaces": ["DuelContext", "Match controller", "Arena state tracker"],
        "rules": [
            "Require ready-check",
            "Validate before start",
            "Freeze until begin signal",
            "Auto-start when both ready",
            "Auto-forfeit on leave",
            "Auto-forfeit on disconnect",
            "Best of one",
            "Best of three",
            "Best of five",
            "Last alive wins",
            "First kill wins",
            "Hold-the-point wins",
            "Time-limit wins",
            "Rematch prompt on finish",
            "Return fighters to lobby on finish",
            "Lock the ring while results display",
        ],
    },
    {
        "folder": "15_anti_stall_sudden_death",
        "title": "Anti-stall and sudden death",
        "summary": "Anti-passivity and match-closure pressure systems for long fights, including sudden death, hard caps, mounting restrictions, reveals, and shrinking safe areas.",
        "governs": "This chapter governs how stalled matches escalate toward conclusion and how the arena changes over time to force resolution.",
        "surfaces": ["DuelContext", "Arena state tracker", "Hazard controllers", "Wall controllers"],
        "rules": [
            "Start sudden death after N seconds",
            "Warn sudden death at N-minus-10",
            "Disable healing in sudden death",
            "Increase damage in sudden death",
            "Shrink safe area over time",
            "Reveal all hidden fighters",
            "Disable mounts in sudden death",
            "Disable ranged-only play in sudden death",
            "End as tie at hard cap",
            "End by damage leader at hard cap",
            "Collapse walls inward",
            "Spawn hazard ring",
        ],
    },
    {
        "folder": "16_spectator_outsider_interference",
        "title": "Spectator, outsider, and interference",
        "summary": "Protection against non-fighter contamination of the arena. This chapter separates stands, observer decks, outsider routing, and interference law.",
        "governs": "This chapter governs what spectators and outsiders may see, say, target, heal, toss, loot, summon, or cross while fighters are active.",
        "surfaces": ["Region", "Interference handler", "Target validation", "Gate routing"],
        "rules": [
            "Allow spectators in stands only",
            "Deny spectator targeting into ring",
            "Deny spectator healing into ring",
            "Deny item toss into ring",
            "Deny corpse looting by outsiders",
            "Bounce outsiders who cross line",
            "Separate spectator and participant gates",
            "Allow staff invisible observer mode",
            "Hide spectator chat from fighters",
            "Deny pet entry from spectator zone",
            "Deny field effects crossing from stands",
        ],
    },
    {
        "folder": "17_environment_hazard_atmosphere",
        "title": "Environment, hazard, and atmosphere",
        "summary": "Arena ambience and environmental pressure systems. This chapter covers light, music, hazard layers, temporary walls, thematic weather, and damage scaling.",
        "governs": "This chapter governs how the arena feels and what non-fighter environmental mechanics alter the combat space over time or by mode.",
        "surfaces": ["Region", "SpellDamageScalar", "Hazard controller", "Announcer systems"],
        "rules": [
            "Change local light level",
            "Change local music",
            "Apply periodic arena damage",
            "Scale spell damage up",
            "Scale spell damage down",
            "Scale melee damage up",
            "Scale healing down",
            "Trigger trap tiles",
            "Trigger announcer speech on entry",
            "Trigger round-start broadcast",
            "Spawn line-of-sight blockers",
            "Spawn temporary walls",
            "Turn ring into lava, poison, or ice mode",
            "Add thematic weather and effects layer",
        ],
    },
    {
        "folder": "18_npc_spawn_housing_economy",
        "title": "NPC, spawn, housing, and economy",
        "summary": "Sterility and economy-neutrality rules for the arena footprint. This chapter keeps world systems from leaking into controlled combat space.",
        "governs": "This chapter governs housing prohibition, spawn suppression, vendor/bank/harvest restrictions, NPC pathing, and placement of reward vendors outside the ring.",
        "surfaces": ["Region", "GuardedRegion", "Spawn control", "Vendor access checks"],
        "rules": [
            "Deny housing in arena footprint",
            "Deny normal creature spawns",
            "Allow only arena-controller NPCs",
            "Deny vendor access in bounds",
            "Disable banking in bounds",
            "Disable harvesting in bounds",
            "Despawn wandering NPCs on match start",
            "Prevent escort and pathing through arena",
            "Keep arena economy-neutral",
            "Keep reward vendors outside ring only",
        ],
    },
    {
        "folder": "19_login_logout_stuck_persistence",
        "title": "Login, logout, stuck, and persistence",
        "summary": "Cleanup, reconnect, logout, reset, and persistence rules that determine what survives matches and what gets reinitialized.",
        "governs": "This chapter governs disconnect handling, reconnect placement, offline ejection, aggressor cleanup, UI cleanup, reset cleanup, and what state is or is not persisted.",
        "surfaces": ["Region logout hooks", "DuelContext", "Arena controller", "Persistence trackers"],
        "rules": [
            "Extend logout delay in bounds",
            "Deny stuck menu in match",
            "Auto-forfeit disconnects",
            "Re-place reconnecting fighter into lobby",
            "Eject offline characters from ring",
            "Clear aggressions on exit",
            "Clear target and cursor state on exit",
            "Close arena gumps on finish",
            "Remove temporary walls on reset",
            "Reset arena controller after each match",
            "Persist ladder only",
            "Do not persist temporary round state",
        ],
    },
    {
        "folder": "20_staff_test_override",
        "title": "Staff, test, and override",
        "summary": "Development and moderation exceptions for arena systems. This chapter keeps powerful override behavior explicit instead of hidden in special-case code.",
        "governs": "This chapter governs staff bypasses, hidden observation, ejection and reset powers, duel-start overrides, live-testing immunity posture, and intervention logging.",
        "surfaces": ["Custom controller", "Staff access checks", "Observer routing", "Audit logging"],
        "rules": [
            "Allow staff to bypass entry lock",
            "Allow staff spell bypass",
            "Allow staff hidden observer mode",
            "Allow staff forced ejection command",
            "Allow staff instant reset",
            "Allow staff duel-start override",
            "Disable staff immunity in live testing",
            "Log all staff arena interventions",
        ],
    },
]

VERB_ALIASES = {
    "one": "one",
    "two": "two",
    "free": "free",
    "use": "use",
    "do": "do",
    "keep": "keep",
    "set": "set",
    "decide": "decide",
    "create": "create",
    "add": "add",
    "lock": "lock",
    "allow": "allow",
    "deny": "deny",
    "send": "send",
    "route": "route",
    "enable": "enable",
    "block": "block",
    "suppress": "suppress",
    "clear": "clear",
    "preserve": "preserve",
    "auto-flag": "auto",
    "auto": "auto",
    "remove": "remove",
    "reset": "reset",
    "force": "force",
    "randomize": "randomize",
    "disallow": "disallow",
    "disable": "disable",
    "count": "count",
    "bounce": "bounce",
    "strip": "strip",
    "reveal": "reveal",
    "slow": "slow",
    "ban": "ban",
    "require": "require",
    "consume": "consume",
    "instant": "instant",
    "best": "best",
    "last": "last",
    "first": "first",
    "hold-the-point": "hold",
    "time-limit": "time-limit",
    "warn": "warn",
    "increase": "increase",
    "shrink": "shrink",
    "end": "end",
    "collapse": "collapse",
    "change": "change",
    "apply": "apply",
    "scale": "scale",
    "trigger": "trigger",
    "spawn": "spawn",
    "turn": "turn",
    "extend": "extend",
    "re-place": "re-place",
    "eject": "eject",
    "close": "close",
    "persist": "persist",
    "log": "log",
    "facet": "facet",
    "region": "region",
    "guardedregion": "guardedregion",
    "spellhelper": "spellhelper",
    "duelcontext": "duelcontext",
    "moongate": "moongate",
    "teleporter": "teleporter",
    "match": "match",
    "entry": "entry",
    "exit": "exit",
    "boundary": "boundary",
    "interference": "interference",
    "status": "status",
    "define": "define",
    "mirror": "mirror",
    "freeze": "freeze",
}


def slugify(text: str) -> str:
    s = text.lower()
    replacements = {
        "—": " ",
        "–": " ",
        "-": " ",
        "/": " ",
        ",": " ",
        ".": " ",
        "'": "",
        "(": " ",
        ")": " ",
        ":": " ",
    }
    for k, v in replacements.items():
        s = s.replace(k, v)
    s = " ".join(s.split())
    return s.replace(" ", "_")


def verb_for_phrase(phrase: str) -> str:
    first = phrase.lower().split()[0]
    return VERB_ALIASES.get(first, first)


def filename_for_phrase(phrase: str, seen: dict) -> str:
    slug = slugify(phrase)
    count = seen.get(slug, 0)
    seen[slug] = count + 1
    if count:
        return f"{slug}_{count+1}.md"
    return f"{slug}.md"


def likely_touchpoints(chapter_title: str, phrase: str):
    base = {
        "Implementation surfaces": ["docs/systems/arena_rule_legend/*", "server/*", "custom/*"],
        "Facet and map rules": ["server/*/MapDefinitions.cs", "server/*/Map.cs", "custom/controllers/*Arena*"],
        "Region geometry and boundaries": ["server/*/Region.cs", "server/*/GuardedRegion.cs", "custom/controllers/*Boundary*", "custom/controllers/*Arena*"],
        "Entry, exit, and routing": ["server/*/Moongate*.cs", "server/*/Teleporter*.cs", "custom/controllers/*Entry*", "custom/controllers/*Exit*"],
        "Travel suppression": ["server/*/SpellHelper.cs", "server/*/Region.cs", "custom/controllers/*Travel*"],
        "Combat permissions": ["server/*/Region.cs", "server/*/GuardedRegion.cs", "custom/controllers/*Arena*", "server/*/DuelContext*.cs"],
        "Beneficial and healing": ["server/*/Region.cs", "server/*/GuardedRegion.cs", "server/*/SpellHelper.cs", "custom/controllers/*Arena*"],
        "Criminal, guard, and legal state": ["server/*/GuardedRegion.cs", "server/*/Region.cs", "server/*/Mobile.cs", "server/*/DuelContext*.cs"],
        "Team, ally, and party": ["server/*/DuelContext*.cs", "server/*/Party*.cs", "custom/controllers/*Match*"],
        "Pets, summons, and followers": ["server/*/Mobile.cs", "server/*/SpellHelper.cs", "server/*/BaseCreature*.cs", "custom/controllers/*Arena*"],
        "Mount, movement, and locomotion": ["server/*/Region.cs", "server/*/Mobile.cs", "custom/controllers/*Boundary*", "custom/controllers/*Arena*"],
        "Spell, skill, and special move": ["server/*/Region.cs", "server/*/SpellHelper.cs", "server/*/DuelContext*.cs", "custom/controllers/*Ruleset*"],
        "Item, equipment, and consumable": ["server/*/DuelContext*.cs", "server/*/Mobile.cs", "server/*/Item*.cs", "custom/controllers/*Loadout*"],
        "Death, corpse, resurrection, and loot": ["server/*/Region.cs", "server/*/Corpse*.cs", "server/*/DuelContext*.cs", "custom/controllers/*Match*"],
        "Match flow, countdown, and victory": ["server/*/DuelContext*.cs", "custom/controllers/*Match*", "custom/controllers/*ArenaState*"],
        "Anti-stall and sudden death": ["server/*/DuelContext*.cs", "custom/controllers/*SuddenDeath*", "custom/controllers/*Hazard*"],
        "Spectator, outsider, and interference": ["server/*/Region.cs", "custom/controllers/*Interference*", "custom/controllers/*Entry*"],
        "Environment, hazard, and atmosphere": ["server/*/Region.cs", "custom/controllers/*Hazard*", "custom/controllers/*Announcer*"],
        "NPC, spawn, housing, and economy": ["server/*/Region.cs", "server/*/GuardedRegion.cs", "custom/controllers/*Arena*", "server/*/Spawner*.cs"],
        "Login, logout, stuck, and persistence": ["server/*/Region.cs", "server/*/DuelContext*.cs", "custom/controllers/*ArenaState*", "custom/controllers/*Persistence*"],
        "Staff, test, and override": ["custom/controllers/*Arena*", "custom/controllers/*Admin*", "server/*/AccessLevel*.cs"],
    }
    return base.get(chapter_title, ["custom/controllers/*", "server/*"])


def write_text(path: Path, text: str):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")


ROOT.mkdir(parents=True, exist_ok=True)

seen = {}
all_rule_entries = []
verb_groups = defaultdict(list)
chapter_file_map = {}

# Precompute filenames and register entries
for chapter in CHAPTERS:
    chapter_entries = []
    for phrase in chapter["rules"]:
        filename = filename_for_phrase(phrase, seen)
        rel = f"{chapter['folder']}/{filename}"
        chapter_entries.append((phrase, filename, rel))
        all_rule_entries.append({
            "chapter_folder": chapter["folder"],
            "chapter_title": chapter["title"],
            "phrase": phrase,
            "filename": filename,
            "rel": rel,
            "status": "legend_only",
            "touchpoints": likely_touchpoints(chapter["title"], phrase),
            "verb": verb_for_phrase(phrase),
        })
        verb_groups[verb_for_phrase(phrase)].append((phrase, rel))
    chapter_file_map[chapter["folder"]] = chapter_entries

# README
readme = f"""# Arena Rule Legend

## Purpose

This subsystem is a stable documentation surface for arena-rule design in Neo UO code. It serves three purposes at once:

1. a clean fallback reference for the agent
2. a navigable design legend for future arena systems
3. an implementation-aware progress ledger that can accumulate code paths, status, notes, and completed work over time

It is intentionally optimized for AI retrieval and update discipline: many small files, stable chapter ordering, predictable naming, and one rule phrase per file.

## Navigation

- Start with `manifest.md` for the full tree and canonical file list.
- Use `implementation_status.md` for a fast rollup of where work stands.
- Use `verb_index.md` to retrieve rules by behavioral family such as `allow_*`, `deny_*`, `block_*`, `clear_*`, or `reset_*`.
- Open a chapter `_index.md` when you know the rule domain but not the exact rule file.
- Open an individual rule file when working on one specific command phrase.

## Naming rules

- Chapter folders use fixed numeric prefixes so order never drifts.
- Each chapter contains `_index.md` plus one rule file per command phrase.
- Rule files use lower snake case and preserve the leading verb in the filename.
- Each rule file preserves the authoritative human-readable command phrase inside the file body.
- File naming collisions are resolved with a short suffix while preserving clarity.

## Update rules for agents and contributors

- Do not merge multiple command phrases into one file.
- Do not move rules between chapters unless the corpus itself is corrected.
- Update `implementation_status.md` whenever a rule status changes.
- Update `verb_index.md` and `manifest.md` after structural or rule-file additions.
- Record meaningful structural or status changes in `change_log.md`.
- Prefer additive progress notes over destructive rewrites.
- Keep `Status` values inside rule files and status rollups limited to: `legend_only`, `planned`, `in_progress`, `implemented`, `tested`, `blocked`, `deprecated`, and `replaced`.

## Current scope

This initial version is documentation scaffolding plus command-corpus ingestion only. It anticipates future implementation tracking, test notes, code touchpoints, and completion progress without requiring future file-layout changes.
"""
write_text(ROOT / "README.md", readme)

# Rule template
rule_template = """# Title

## Domain

## Command Phrase

## Intent

## Status
legend_only

## Implementation Surface

## Likely Code Touchpoints
- 

## Dependencies
- 

## Edge Cases
- 

## Notes
- 

## Related Rules
- 

## Progress Log
- YYYY-MM-DD — initialized as legend_only.

## Future Tasks
- 
"""
write_text(ROOT / "rule_file_template.md", rule_template)

# Chapter files and rule files
for chapter in CHAPTERS:
    folder = ROOT / chapter["folder"]
    folder.mkdir(parents=True, exist_ok=True)
    entries = chapter_file_map[chapter["folder"]]
    bullets = "\n".join([f"- [{filename}]({filename}) — {phrase}" for phrase, filename, _ in entries])
    surfaces = "\n".join([f"- {s}" for s in chapter["surfaces"]])
    index_text = f"""# {chapter['title']}

{chapter['summary']}

## Scope

{chapter['governs']}

## Rule files

{bullets}

## Related surfaces

{surfaces}
"""
    write_text(folder / "_index.md", index_text)

    rel_lookup = [rel for _, _, rel in entries]
    for idx, (phrase, filename, rel) in enumerate(entries):
        others = [f"../{r}" for _, _, r in entries if r != rel][:5]
        related = []
        if idx > 0:
            related.append(f"- [{entries[idx-1][0]}]({entries[idx-1][1]})")
        if idx + 1 < len(entries):
            related.append(f"- [{entries[idx+1][0]}]({entries[idx+1][1]})")
        status = "legend_only"
        surfaces_list = likely_touchpoints(chapter["title"], phrase)
        touchpoints = "\n".join([f"- `{tp}`" for tp in surfaces_list])
        deps = [
            "- Chapter index for domain context.",
            "- implementation_status.md for current status rollup.",
            "- change_log.md for structural updates over time.",
        ]
        edge_cases = [
            "- Confirm interaction with neighboring region, travel, and duel-state rules before implementation.",
            "- Verify whether staff or test overrides should bypass this rule.",
        ]
        notes = [
            "- Initialized from the imported arena command corpus.",
            "- Keep the command phrase stable even if implementation details evolve.",
        ]
        future = [
            "- Link concrete server/custom code paths when implementation begins.",
            "- Update status when planning, coding, or testing starts.",
        ]
        related_section = "\n".join(related) if related else "- None yet."
        text = f"""# Title
{phrase}

## Domain
{chapter['title']}

## Command Phrase
{phrase}

## Intent
Capture the arena rule \"{phrase}\" as a stable design and implementation tracking unit within the {chapter['title'].lower()} domain.

## Status
{status}

## Implementation Surface
- {', '.join(chapter['surfaces'])}

## Likely Code Touchpoints
{touchpoints}

## Dependencies
{'\n'.join(deps)}

## Edge Cases
{'\n'.join(edge_cases)}

## Notes
{'\n'.join(notes)}

## Related Rules
{related_section}

## Progress Log
- {TODAY} — {AGENT} — initialized from arena command corpus with status `legend_only`.

## Future Tasks
{'\n'.join(future)}
"""
        write_text(folder / filename, text)

# implementation_status.md
status_lines = [
    "# Implementation Status",
    "",
    "Fast rollup of arena-rule status across the legend subsystem.",
    "",
    "Allowed status values: `legend_only`, `planned`, `in_progress`, `implemented`, `tested`, `blocked`, `deprecated`, `replaced`.",
    "",
    "| Rule | Chapter | Status | Code touchpoints |",
    "|---|---|---|---|",
]
for entry in all_rule_entries:
    tp = "; ".join(entry["touchpoints"][:2])
    status_lines.append(f"| [{entry['phrase']}]({entry['chapter_folder']}/{entry['filename']}) | {entry['chapter_title']} | {entry['status']} | `{tp}` |")
write_text(ROOT / "implementation_status.md", "\n".join(status_lines) + "\n")

# verb_index.md
ordered_verbs = [
    "allow", "deny", "block", "clear", "force", "reset", "auto", "require", "scale", "freeze", "reveal", "strip", "protect", "count", "lock", "delay"
]
all_verbs = list(dict.fromkeys(ordered_verbs + sorted(v for v in verb_groups if v not in ordered_verbs)))
verb_lines = [
    "# Verb Index",
    "",
    "Retrieve arena rules by behavior family rather than by chapter.",
    "",
]
for verb in all_verbs:
    items = sorted(verb_groups.get(verb, []), key=lambda x: x[0].lower())
    if not items:
        continue
    verb_lines.append(f"## {verb.title()}")
    verb_lines.append("")
    for phrase, rel in items:
        verb_lines.append(f"- [{phrase}]({rel})")
    verb_lines.append("")
write_text(ROOT / "verb_index.md", "\n".join(verb_lines).rstrip() + "\n")

# change_log.md
change_log = f"""# Change Log

Record meaningful structural changes, major additions, implementation linkage, or large-scale status updates for the arena rule legend subsystem.

- {TODAY} — {AGENT} — subsystem initialization — created `docs/systems/arena_rule_legend/`, scaffolded chapter structure `00` through `20`, ingested the full command corpus into one-file-per-rule markdown files, generated the manifest, built the verb index, and created the initial implementation status rollup.
"""
write_text(ROOT / "change_log.md", change_log)

# manifest.md
manifest_lines = [
    "# Manifest",
    "",
    "Canonical file tree for the arena rule legend subsystem.",
    "",
    "```text",
    "docs/systems/arena_rule_legend/",
    "  README.md",
    "  manifest.md",
    "  rule_file_template.md",
    "  verb_index.md",
    "  implementation_status.md",
    "  change_log.md",
]
for chapter in CHAPTERS:
    manifest_lines.append(f"  {chapter['folder']}/")
    manifest_lines.append("    _index.md")
    for _, filename, _ in chapter_file_map[chapter["folder"]]:
        manifest_lines.append(f"    {filename}")
manifest_lines.append("```")
manifest_lines.append("")
manifest_lines.append("## File list")
manifest_lines.append("")
manifest_lines.extend([f"- `docs/systems/arena_rule_legend/{p}`" for p in [
    "README.md", "manifest.md", "rule_file_template.md", "verb_index.md", "implementation_status.md", "change_log.md"
]])
for chapter in CHAPTERS:
    manifest_lines.append(f"- `docs/systems/arena_rule_legend/{chapter['folder']}/_index.md`")
    for _, filename, _ in chapter_file_map[chapter["folder"]]:
        manifest_lines.append(f"- `docs/systems/arena_rule_legend/{chapter['folder']}/{filename}`")
write_text(ROOT / "manifest.md", "\n".join(manifest_lines) + "\n")

# verification output
paths = [e['rel'] for e in all_rule_entries]
assert len(paths) == len(set(paths)), "Duplicate rule file path detected"

print(f"Created arena rule legend at: {ROOT}")
print(f"Rule files created: {len(all_rule_entries)}")
print(f"Unique rule file paths: {len(set(paths))}")
print("Verification: every corpus entry has exactly one corresponding rule file, and path collisions were resolved.")
