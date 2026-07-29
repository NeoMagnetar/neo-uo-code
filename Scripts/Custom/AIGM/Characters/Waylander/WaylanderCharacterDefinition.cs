using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.AIGM.Characters.Waylander
{
    public enum WaylanderRosterControlPolicy
    {
        BindableCompanion = 0,
        ScenarioBindable = 1,
        AutonomousOnly = 2,
        NonCompanion = 3,
        MountOnly = 4
    }

    public sealed class WaylanderCharacterDefinition
    {
        public string CharacterId { get; set; }
        public string CanonicalPersonId { get; set; }
        public string EraKey { get; set; }
        public string VisibleName { get; set; }
        public string Title { get; set; }
        public string[] Aliases { get; set; }
        public bool Female { get; set; }
        public int Body { get; set; }
        public int Hue { get; set; }
        public int SpeechHue { get; set; }
        public int HairItemId { get; set; }
        public int HairHue { get; set; }
        public int FacialHairItemId { get; set; }
        public int FacialHairHue { get; set; }
        public AIType AiType { get; set; }
        public FightMode FightMode { get; set; }
        public WaylanderRosterDisposition Disposition { get; set; }
        public bool IsUnique { get; set; }
        public bool AllowMultiples { get; set; }
        public bool IsHostile { get; set; }
        public bool IsHumanoid { get; set; }
        public bool IsLoreOnly { get; set; }
        public bool RequiresLoreFlag { get; set; }
        public bool IsMount { get; set; }
        public bool CanTalk { get; set; }
        public WaylanderRosterControlPolicy ControlPolicy { get; set; }
        public string Faction { get; set; }
        public string CombatRole { get; set; }
        public string Alignment { get; set; }
        public string PersonaAssetKey { get; set; }
        public string IdentityLine { get; set; }
        public string HiddenIdentityLine { get; set; }
        public string WaylanderLine { get; set; }
        public string FactionLine { get; set; }
        public string PhilosophyLine { get; set; }
        public string UnknownLine { get; set; }
        public string DefaultReplyLine { get; set; }
        public string[] FactionKeywords { get; set; }
        public string[] PhilosophyKeywords { get; set; }
        public string[] EnemyIds { get; set; }
        public Dictionary<string, string> KnownCharacterFacts { get; private set; }
        public Dictionary<SkillName, double> FixedSkills { get; private set; }
        public List<WaylanderRelationshipDefinition> Relationships { get; private set; }
        public int Str { get; set; }
        public int Dex { get; set; }
        public int Int { get; set; }
        public int Hits { get; set; }
        public int DamageMin { get; set; }
        public int DamageMax { get; set; }
        public int VirtualArmor { get; set; }
        public int Fame { get; set; }
        public int Karma { get; set; }
        public int ControlSlots { get; set; }
        public int BaseSoundId { get; set; }
        public Action<WaylanderRosterMobile> EquipAction { get; set; }
        public Action<WaylanderRosterMobile> PostConfigureAction { get; set; }

        public WaylanderCharacterDefinition()
        {
            CharacterId = String.Empty;
            CanonicalPersonId = String.Empty;
            EraKey = String.Empty;
            VisibleName = String.Empty;
            Title = String.Empty;
            Aliases = Array.Empty<string>();
            SpeechHue = 0x3B2;
            AiType = AIType.AI_Melee;
            FightMode = FightMode.Closest;
            Disposition = WaylanderRosterDisposition.NeutralNonAggressive;
            IsUnique = true;
            AllowMultiples = false;
            IsHostile = false;
            IsHumanoid = true;
            IsLoreOnly = false;
            RequiresLoreFlag = false;
            IsMount = false;
            CanTalk = true;
            ControlPolicy = WaylanderRosterControlPolicy.NonCompanion;
            Faction = String.Empty;
            CombatRole = String.Empty;
            Alignment = String.Empty;
            PersonaAssetKey = String.Empty;
            IdentityLine = String.Empty;
            HiddenIdentityLine = String.Empty;
            WaylanderLine = String.Empty;
            FactionLine = String.Empty;
            PhilosophyLine = String.Empty;
            UnknownLine = "I will not invent what I do not know.";
            DefaultReplyLine = "Ask cleanly and I will answer cleanly.";
            FactionKeywords = Array.Empty<string>();
            PhilosophyKeywords = Array.Empty<string>();
            EnemyIds = Array.Empty<string>();
            KnownCharacterFacts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            FixedSkills = new Dictionary<SkillName, double>();
            Relationships = new List<WaylanderRelationshipDefinition>();
            Str = 75;
            Dex = 75;
            Int = 50;
            Hits = 120;
            DamageMin = 6;
            DamageMax = 10;
            VirtualArmor = 25;
            Fame = 1000;
            Karma = 0;
            ControlSlots = 0;
            BaseSoundId = 0x190;
        }

        public WaylanderRelationshipDefinition FindRelationship(string targetCharacterId)
        {
            if (String.IsNullOrWhiteSpace(targetCharacterId))
                return null;

            for (int i = 0; i < Relationships.Count; i++)
            {
                if (String.Equals(Relationships[i].RelatedCharacterId, targetCharacterId, StringComparison.OrdinalIgnoreCase))
                    return Relationships[i];
            }

            return null;
        }

        public bool MatchesAlias(string text)
        {
            if (String.IsNullOrWhiteSpace(text))
                return false;

            if (String.Equals(text, CharacterId, StringComparison.OrdinalIgnoreCase)
                || String.Equals(text, CanonicalPersonId, StringComparison.OrdinalIgnoreCase)
                || String.Equals(text, VisibleName, StringComparison.OrdinalIgnoreCase))
                return true;

            for (int i = 0; i < Aliases.Length; i++)
            {
                if (String.Equals(text, Aliases[i], StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
