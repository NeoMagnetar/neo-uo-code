using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.AIGM.Characters.Waylander
{
    public static partial class WaylanderRosterCatalog
    {
        private static readonly object SyncRoot = new object();
        private static Dictionary<string, WaylanderCharacterDefinition> _definitions;
        private static Dictionary<string, string[]> _groups;
        private static List<KeyValuePair<string, string>> _aliasIndex;

        public static WaylanderCharacterDefinition GetDefinition(string characterId)
        {
            EnsureLoaded();

            if (String.IsNullOrWhiteSpace(characterId))
                return null;

            WaylanderCharacterDefinition definition;
            return _definitions.TryGetValue(characterId.Trim(), out definition) ? definition : null;
        }

        public static IReadOnlyDictionary<string, WaylanderCharacterDefinition> GetAllDefinitions()
        {
            EnsureLoaded();
            return _definitions;
        }

        public static string[] GetGroup(string groupKey)
        {
            EnsureLoaded();

            if (String.IsNullOrWhiteSpace(groupKey))
                return Array.Empty<string>();

            string[] members;
            return _groups.TryGetValue(groupKey.Trim(), out members) ? members : Array.Empty<string>();
        }

        public static string ResolveCharacterIdFromSpeech(string speech, string selfCharacterId)
        {
            EnsureLoaded();

            if (String.IsNullOrWhiteSpace(speech))
                return String.Empty;

            string normalized = NormalizeSpeech(speech);
            for (int i = 0; i < _aliasIndex.Count; i++)
            {
                string alias = _aliasIndex[i].Key;
                string canonical = _aliasIndex[i].Value;
                if (String.Equals(canonical, selfCharacterId, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (normalized.Contains(alias))
                    return canonical;
            }

            return String.Empty;
        }

        private static void EnsureLoaded()
        {
            if (_definitions != null)
                return;

            lock (SyncRoot)
            {
                if (_definitions != null)
                    return;

                _definitions = new Dictionary<string, WaylanderCharacterDefinition>(StringComparer.OrdinalIgnoreCase);
                _groups = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
                _aliasIndex = new List<KeyValuePair<string, string>>();

                RegisterDefinitions(BuildBook1Definitions());
                RegisterDefinitions(BuildBook2Definitions());
                RegisterDefinitions(BuildBook3Definitions());
                RegisterDefinitions(BuildCrossoverDefinitions());
                ApplyControlPolicies(_definitions);
                BuildGroups(_groups);
                BuildAliasIndex();
            }
        }

        private static void RegisterDefinitions(IEnumerable<WaylanderCharacterDefinition> definitions)
        {
            if (definitions == null)
                return;

            foreach (WaylanderCharacterDefinition definition in definitions)
            {
                if (definition == null || String.IsNullOrWhiteSpace(definition.CharacterId))
                    continue;

                _definitions[definition.CharacterId] = definition;
            }
        }

        private static void BuildAliasIndex()
        {
            foreach (WaylanderCharacterDefinition definition in _definitions.Values)
            {
                AddAlias(definition.CanonicalPersonId, definition.CanonicalPersonId);
                AddAlias(definition.VisibleName, definition.CanonicalPersonId);
                AddAlias(definition.CharacterId, definition.CanonicalPersonId);

                for (int i = 0; i < definition.Aliases.Length; i++)
                    AddAlias(definition.Aliases[i], definition.CanonicalPersonId);
            }

            _aliasIndex.Sort(delegate (KeyValuePair<string, string> left, KeyValuePair<string, string> right)
            {
                return right.Key.Length.CompareTo(left.Key.Length);
            });
        }

        private static void AddAlias(string alias, string canonicalCharacterId)
        {
            string normalized = NormalizeSpeech(alias);
            if (String.IsNullOrWhiteSpace(normalized) || String.IsNullOrWhiteSpace(canonicalCharacterId))
                return;

            for (int i = 0; i < _aliasIndex.Count; i++)
            {
                if (String.Equals(_aliasIndex[i].Key, normalized, StringComparison.OrdinalIgnoreCase)
                    && String.Equals(_aliasIndex[i].Value, canonicalCharacterId, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            _aliasIndex.Add(new KeyValuePair<string, string>(normalized, canonicalCharacterId));
        }

        private static void ApplyControlPolicies(Dictionary<string, WaylanderCharacterDefinition> definitions)
        {
            SetControlPolicy(definitions, WaylanderRosterControlPolicy.BindableCompanion,
                "dakeyras",
                "dakeyras.grey_man",
                "dardalion",
                "danyal",
                "miriel",
                "druss",
                "angel",
                "belash",
                "kysumu",
                "yu_yu_liang",
                "ustarte",
                "keeva_taliana",
                "kai",
                "egel",
                "gellan",
                "jonat",
                "sarvaj");

            SetControlPolicy(definitions, WaylanderRosterControlPolicy.ScenarioBindable,
                "durmast",
                "cadoras",
                "karnak",
                "senta",
                "kesa_khan",
                "bodalen",
                "ansi_chen",
                "sathuli_lord",
                "tenaka_khan",
                "regnak",
                "orien",
                "hewla",
                "duke_of_kydor",
                "matze_chai",
                "krylla");

            SetControlPolicy(definitions, WaylanderRosterControlPolicy.AutonomousOnly,
                "kaem",
                "morak",
                "zhu_chao",
                "innicas",
                "aric",
                "dark_brotherhood_knight",
                "joining",
                "kuan_hador_demon_lord");

            SetControlPolicy(definitions, WaylanderRosterControlPolicy.MountOnly, "scar");
            SetControlPolicy(definitions, WaylanderRosterControlPolicy.NonCompanion, "niallad");
        }

        private static void SetControlPolicy(
            Dictionary<string, WaylanderCharacterDefinition> definitions,
            WaylanderRosterControlPolicy policy,
            params string[] characterIds)
        {
            if (definitions == null || characterIds == null)
                return;

            for (int i = 0; i < characterIds.Length; i++)
            {
                WaylanderCharacterDefinition definition;
                if (!String.IsNullOrWhiteSpace(characterIds[i]) && definitions.TryGetValue(characterIds[i], out definition))
                    definition.ControlPolicy = policy;
            }
        }

        internal static string NormalizeSpeech(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return String.Empty;

            char[] chars = value.ToLowerInvariant().ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (!Char.IsLetterOrDigit(chars[i]) && !Char.IsWhiteSpace(chars[i]))
                    chars[i] = ' ';
            }

            return new string(chars).Trim();
        }

        private static WaylanderCharacterDefinition Def(string id, string canonical, string era, string name, string title, string faction, string combatRole)
        {
            return new WaylanderCharacterDefinition
            {
                CharacterId = id,
                CanonicalPersonId = canonical,
                EraKey = era,
                VisibleName = name,
                Title = title,
                Faction = faction,
                CombatRole = combatRole,
                Alignment = faction,
                PersonaAssetKey = id,
                Aliases = Array.Empty<string>()
            };
        }

        private static WaylanderCharacterDefinition Allied(WaylanderCharacterDefinition definition)
        {
            return SetDisposition(definition, WaylanderRosterDisposition.InnocentAllied);
        }

        private static WaylanderCharacterDefinition Neutral(WaylanderCharacterDefinition definition)
        {
            return SetDisposition(definition, WaylanderRosterDisposition.NeutralNonAggressive);
        }

        private static WaylanderCharacterDefinition Hostile(WaylanderCharacterDefinition definition)
        {
            return SetDisposition(definition, WaylanderRosterDisposition.HostileMurderer);
        }

        private static WaylanderCharacterDefinition Animal(WaylanderCharacterDefinition definition)
        {
            return SetDisposition(definition, WaylanderRosterDisposition.AnimalNonAggressive);
        }

        private static WaylanderCharacterDefinition SetDisposition(WaylanderCharacterDefinition definition, WaylanderRosterDisposition disposition)
        {
            if (definition == null)
                return null;

            definition.Disposition = disposition;

            switch (disposition)
            {
                case WaylanderRosterDisposition.HostileMurderer:
                    definition.IsHostile = true;
                    definition.FightMode = FightMode.Closest;
                    break;
                case WaylanderRosterDisposition.AnimalNonAggressive:
                    definition.IsHostile = false;
                    definition.FightMode = FightMode.None;
                    break;
                default:
                    definition.IsHostile = false;
                    definition.FightMode = FightMode.Aggressor;
                    break;
            }

            return definition;
        }

        private static void Skill(WaylanderCharacterDefinition definition, SkillName skill, double value)
        {
            definition.FixedSkills[skill] = value;
        }

        private static void Fact(WaylanderCharacterDefinition definition, string relatedId, string line)
        {
            definition.KnownCharacterFacts[relatedId] = line;
        }

        private static WaylanderRelationshipDefinition Rel(
            string relatedId,
            string type,
            string stance,
            int trust,
            string context,
            WaylanderRelationshipTimelineState timelineState,
            string fact,
            string hiddenFact)
        {
            return new WaylanderRelationshipDefinition(
                relatedId,
                type,
                stance,
                trust,
                context,
                timelineState,
                new[] { fact },
                String.IsNullOrWhiteSpace(hiddenFact) ? Array.Empty<string>() : new[] { hiddenFact });
        }

        private static void Wear(BaseCreature mobile, Item item, string displayName, int hue)
        {
            if (mobile == null || item == null)
                return;

            if (!String.IsNullOrWhiteSpace(displayName))
                item.Name = displayName;

            if (hue >= 0)
                item.Hue = hue;

            item.Movable = false;
            item.LootType = LootType.Blessed;
            mobile.AddItem(item);
        }

        private static void Pack(BaseCreature mobile, Item item, string displayName, int hue)
        {
            if (mobile == null || item == null)
                return;

            if (!String.IsNullOrWhiteSpace(displayName))
                item.Name = displayName;

            if (hue >= 0)
                item.Hue = hue;

            item.Movable = false;
            item.LootType = LootType.Blessed;
            mobile.PackItem(item);
        }

        private static void EquipWaylanderLeathers(WaylanderRosterMobile mobile, bool olderGreyMan)
        {
            Wear(mobile, new Boots(), null, 0);
            Wear(mobile, new WaylanderCloak(), olderGreyMan ? "Grey Man's Cloak" : null, -1);
            Wear(mobile, new LeatherChest(), olderGreyMan ? "Worn Dark Leather" : "Dark Leather Jerkin", 1109);
            Wear(mobile, new LeatherArms(), null, 1109);
            Wear(mobile, new LeatherGloves(), null, 1109);
            Wear(mobile, new LeatherGorget(), null, 1109);
            Wear(mobile, new LeatherLegs(), null, 1109);
            Wear(mobile, new WaylanderCrossbow(), olderGreyMan ? "Waylander's Ventrian Crossbow" : null, -1);
            Pack(mobile, new WaylanderFightingKnife(), null, -1);
            Pack(mobile, new WaylanderFightingKnife(), null, -1);
            Pack(mobile, new WaylanderThrowingKnife(), null, -1);
            Pack(mobile, new WaylanderThrowingKnife(), null, -1);
            Pack(mobile, new WaylanderThrowingKnife(), null, -1);
            Pack(mobile, new WaylanderBootKnife(), null, -1);
            Pack(mobile, new WaylanderHuntingKnife(), null, -1);
        }

        private static void EquipThirtyArmor(WaylanderRosterMobile mobile, bool robesOnly)
        {
            Wear(mobile, new Boots(), null, 0);

            if (robesOnly)
            {
                Wear(mobile, new OakenwoodSourceRobe(), null, -1);
                return;
            }

            Wear(mobile, new Cloak(), "Cloak of the Thirty", 1153);
            Wear(mobile, new TheThirty_Chest(), null, -1);
            Wear(mobile, new TheThirty_Arms(), null, -1);
            Wear(mobile, new TheThirty_Gloves(), null, -1);
            Wear(mobile, new TheThirty_Helm(), null, -1);
            Wear(mobile, new TheThirty_Legs(), null, -1);
            Wear(mobile, new TheThirtySilverSword(), null, -1);
            Wear(mobile, new TheThirtySilverShield(), null, -1);
        }

        private static void EquipDarkBrotherhoodArmor(WaylanderRosterMobile mobile, Item weapon, bool occultGuard)
        {
            Wear(mobile, new Boots(), null, 1175);
            Wear(mobile, new DarkBrotherhood_Chest(), null, -1);
            Wear(mobile, new DarkBrotherhood_Arms(), null, -1);
            Wear(mobile, new DarkBrotherhood_Gloves(), null, -1);
            Wear(mobile, new DarkBrotherhood_Helm(), null, -1);
            Wear(mobile, new DarkBrotherhood_Legs(), null, -1);
            Wear(mobile, weapon, null, 1175);
            if (occultGuard)
                Pack(mobile, new Spellbook(), "Occult Guard Grimoire", 1175);
        }

        private static void EquipSimpleLeatherArcher(WaylanderRosterMobile mobile, string chestName, int hue)
        {
            Wear(mobile, new Boots(), null, 0);
            Wear(mobile, new FancyShirt(), null, hue);
            Wear(mobile, new LeatherChest(), chestName, hue);
            Wear(mobile, new LeatherArms(), null, hue);
            Wear(mobile, new LeatherGloves(), null, hue);
            Wear(mobile, new LeatherGorget(), null, hue);
            Wear(mobile, new LeatherLegs(), null, hue);
            Wear(mobile, new Bow(), null, hue);
            Pack(mobile, new Arrow(60), null, -1);
        }

    }
}
