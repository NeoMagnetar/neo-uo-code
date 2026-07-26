using System;
using Server.Mobiles;

namespace Server.Custom.AIGM.Characters.Waylander
{
    public class WaylanderRosterMount : Horse
    {
        private string _characterId;
        private bool _adminTestPassive;

        public WaylanderRosterMount(string characterId)
            : base()
        {
            _characterId = characterId ?? String.Empty;
            ApplyDefinition();
        }

        public WaylanderRosterMount(Serial serial)
            : base(serial)
        {
        }

        public WaylanderCharacterDefinition Definition
        {
            get { return WaylanderRosterCatalog.GetDefinition(_characterId); }
        }

        public bool IsAdminTestSpawn
        {
            get { return _adminTestPassive; }
        }

        public override bool HandlesOnSpeech(Mobile from)
        {
            return false;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(2);
            writer.Write(_characterId);
            writer.Write(_adminTestPassive);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            _characterId = version >= 1 ? reader.ReadString() : String.Empty;
            _adminTestPassive = version >= 2 && reader.ReadBool();
            ApplyDefinition();
            ApplySpawnState();
        }

        public void ConfigureSpawnMode(bool passiveTestMode)
        {
            _adminTestPassive = passiveTestMode;
            ApplySpawnState();
        }

        private void ApplyDefinition()
        {
            WaylanderCharacterDefinition definition = Definition;
            if (definition == null)
                return;

            Name = definition.VisibleName;
            Tamable = false;
            ControlSlots = 0;
            Fame = definition.Fame;
            Karma = definition.Karma;
            SetStr(definition.Str);
            SetDex(definition.Dex);
            SetInt(definition.Int);
            SetHits(definition.Hits);
            SetDamage(definition.DamageMin, definition.DamageMax);

            if (!String.IsNullOrWhiteSpace(definition.PersonaAssetKey))
            {
                string personaBlock = WaylanderPersonaRepository.GetPersonaBlock(definition.PersonaAssetKey);
                if (!String.IsNullOrWhiteSpace(personaBlock) && String.IsNullOrWhiteSpace(Profile))
                    Profile = personaBlock;
            }

            ApplySpawnState();
        }

        private void ApplySpawnState()
        {
            FightMode = FightMode.None;
            Combatant = null;
            Warmode = false;
            Aggressors.Clear();
            Aggressed.Clear();
            Criminal = false;
            Kills = 0;
        }
    }
}
