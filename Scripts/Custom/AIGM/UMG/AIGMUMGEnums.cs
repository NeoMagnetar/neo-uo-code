using System.Runtime.Serialization;

namespace Server.Custom.AIGM.UMG
{
    [DataContract]
    public enum AIGMUMGMoltType
    {
        [EnumMember] Trigger = 0,
        [EnumMember] Directive = 1,
        [EnumMember] Instruction = 2,
        [EnumMember] Subject = 3,
        [EnumMember] Primary = 4,
        [EnumMember] Philosophy = 5,
        [EnumMember] Blueprint = 6
    }

    [DataContract]
    public enum AIGMUMGBlockState
    {
        [EnumMember] Draft = 0,
        [EnumMember] Active = 1,
        [EnumMember] Inactive = 2,
        [EnumMember] Suspended = 3,
        [EnumMember] Expired = 4,
        [EnumMember] Rejected = 5,
        [EnumMember] InvalidCapability = 6,
        [EnumMember] Conflicted = 7,
        [EnumMember] Archived = 8
    }

    [DataContract]
    public enum AIGMUMGBlockScope
    {
        [EnumMember] NPC = 0,
        [EnumMember] PersonaEra = 1,
        [EnumMember] Squad = 2,
        [EnumMember] Faction = 3,
        [EnumMember] Owner = 4,
        [EnumMember] Scenario = 5,
        [EnumMember] Region = 6,
        [EnumMember] GlobalAIGM = 7
    }

    [DataContract]
    public enum AIGMUMGBlockSource
    {
        [EnumMember] CanonicalMigration = 0,
        [EnumMember] UserManual = 1,
        [EnumMember] AgentProposal = 2,
        [EnumMember] Template = 3,
        [EnumMember] Scenario = 4,
        [EnumMember] RuntimeGenerated = 5,
        [EnumMember] SystemInvariant = 6
    }

    [DataContract]
    public enum AIGMUMGNeoStackKind
    {
        [EnumMember] Identity = 0,
        [EnumMember] Capability = 1,
        [EnumMember] CombatDoctrine = 2,
        [EnumMember] SkillsSpellsResources = 3,
        [EnumMember] MovementPositioning = 4,
        [EnumMember] TrackingAwareness = 5,
        [EnumMember] SquadRelationshipOperations = 6,
        [EnumMember] SituationalOverlays = 7,
        [EnumMember] Governance = 8
    }

    [DataContract]
    public enum AIGMUMGAutonomyMode
    {
        [EnumMember] Manual = 0,
        [EnumMember] Assisted = 1,
        [EnumMember] Tactical = 2,
        [EnumMember] Autonomous = 3,
        [EnumMember] Scenario = 4
    }

    [DataContract]
    public enum AIGMUMGIntentType
    {
        [EnumMember] None = 0,
        [EnumMember] Follow = 1,
        [EnumMember] Stay = 2,
        [EnumMember] StandDown = 3,
        [EnumMember] Hold = 4,
        [EnumMember] Resume = 5,
        [EnumMember] Guard = 6,
        [EnumMember] Engage = 7,
        [EnumMember] Track = 8,
        [EnumMember] Hunt = 9,
        [EnumMember] Heal = 10,
        [EnumMember] Cure = 11,
        [EnumMember] Bandage = 12,
        [EnumMember] UsePotion = 13,
        [EnumMember] CastSpell = 14,
        [EnumMember] Hide = 15,
        [EnumMember] Reveal = 16,
        [EnumMember] Regroup = 17,
        [EnumMember] Retreat = 18,
        [EnumMember] Travel = 19,
        [EnumMember] Patrol = 20,
        [EnumMember] ReturnHome = 21,
        [EnumMember] Loot = 22,
        [EnumMember] Report = 23,
        [EnumMember] AlertSquad = 24
    }

    [DataContract]
    public enum AIGMUMGTriggerOperator
    {
        [EnumMember] Equals = 0,
        [EnumMember] NotEquals = 1,
        [EnumMember] LessThan = 2,
        [EnumMember] LessThanOrEqual = 3,
        [EnumMember] GreaterThan = 4,
        [EnumMember] GreaterThanOrEqual = 5,
        [EnumMember] Contains = 6,
        [EnumMember] InSet = 7,
        [EnumMember] And = 8,
        [EnumMember] Or = 9,
        [EnumMember] Not = 10
    }

    [DataContract]
    public enum AIGMUMGCapabilityKind
    {
        [EnumMember] CanUseSword = 0,
        [EnumMember] CanUseAxe = 1,
        [EnumMember] CanUseBow = 2,
        [EnumMember] CanWrestle = 3,
        [EnumMember] CanHeal = 4,
        [EnumMember] CanBandage = 5,
        [EnumMember] CanCure = 6,
        [EnumMember] CanCastOffensiveSpell = 7,
        [EnumMember] CanCastDefensiveSpell = 8,
        [EnumMember] CanUsePotion = 9,
        [EnumMember] CanTrack = 10,
        [EnumMember] CanDetectHidden = 11,
        [EnumMember] CanHide = 12,
        [EnumMember] CanStealth = 13,
        [EnumMember] CanLoot = 14,
        [EnumMember] CanGuard = 15,
        [EnumMember] CanFollow = 16,
        [EnumMember] CanTravel = 17,
        [EnumMember] CanUseWaypoints = 18,
        [EnumMember] CanReceiveSquadOrders = 19,
        [EnumMember] CanReceiveRemoteTask = 20,
        [EnumMember] CanBind = 21,
        [EnumMember] CanOperateAutonomously = 22
    }
}
