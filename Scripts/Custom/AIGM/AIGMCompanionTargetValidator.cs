using System;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public sealed class AIGMCompanionTargetValidationResult
    {
        public bool Allowed { get; set; }
        public string Reason { get; set; }

        public AIGMCompanionTargetValidationResult(bool allowed, string reason)
        {
            Allowed = allowed;
            Reason = reason ?? String.Empty;
        }
    }

    public static class AIGMCompanionTargetValidator
    {
        public static bool IsHostileMonsterCandidate(BaseHire companion, Mobile target, out string reason)
        {
            reason = String.Empty;

            if (companion == null || companion.Deleted || !companion.Alive || companion.Map == null)
            {
                reason = "invalid_companion";
                return false;
            }

            if (target == null)
            {
                reason = "null_target";
                return false;
            }

            if (target.Deleted)
            {
                reason = "deleted_target";
                return false;
            }

            if (!target.Alive)
            {
                reason = "dead_target";
                return false;
            }

            if (target.Map == null || target.Map != companion.Map)
            {
                reason = "different_map";
                return false;
            }

            if (target == companion)
            {
                reason = "self_target";
                return false;
            }

            Mobile owner = companion.GetOwner();
            if (owner != null && target == owner)
            {
                reason = "owner_target";
                return false;
            }

            if (target is IAIGMCompanionActor)
            {
                reason = "companion_target";
                return false;
            }

            if (target.Player)
            {
                reason = "player_target";
                return false;
            }

            BaseCreature creature = target as BaseCreature;
            if (creature == null)
            {
                reason = "not_creature";
                return false;
            }

            if (creature.Blessed || creature.IsInvulnerable)
            {
                reason = "invulnerable_target";
                return false;
            }

            if (creature.Controlled || creature.Summoned)
            {
                reason = "controlled_or_summoned_target";
                return false;
            }

            if (owner != null && creature.ControlMaster == owner)
            {
                reason = "owner_controlled_ally";
                return false;
            }

            if (creature is BaseVendor || creature is BaseEscortable)
            {
                reason = "civilian_or_vendor_target";
                return false;
            }

            if (creature.Body != null && creature.Body.IsHuman)
            {
                reason = "human_target";
                return false;
            }

            if (creature.Body != null && creature.Body.IsAnimal)
            {
                reason = "animal_target";
                return false;
            }

            if (creature.Body == null || !creature.Body.IsMonster)
            {
                reason = "not_monster_body";
                return false;
            }

            if (creature.Team == companion.Team && creature.Team != 0)
            {
                reason = "same_team";
                return false;
            }

            reason = "hostile_monster_candidate";
            return true;
        }

        public static AIGMCompanionTargetValidationResult ValidateMonsterTarget(BaseHire companion, Mobile target, int scanRange)
        {
            if (companion == null || companion.Deleted || !companion.Alive || companion.Map == null)
                return new AIGMCompanionTargetValidationResult(false, "invalid_companion");

            if (target == null)
                return new AIGMCompanionTargetValidationResult(false, "null_target");

            if (target.Deleted)
                return new AIGMCompanionTargetValidationResult(false, "deleted_target");

            if (!target.Alive)
                return new AIGMCompanionTargetValidationResult(false, "dead_target");

            if (target.Map == null || target.Map != companion.Map)
                return new AIGMCompanionTargetValidationResult(false, "different_map");

            string hostileReason;
            if (!IsHostileMonsterCandidate(companion, target, out hostileReason))
                return new AIGMCompanionTargetValidationResult(false, hostileReason);

            if (scanRange > 0 && !companion.InRange(target, scanRange))
                return new AIGMCompanionTargetValidationResult(false, "out_of_range");

            return new AIGMCompanionTargetValidationResult(true, "monster_target_valid");
        }

        public static bool IsTrackableAnimalCandidate(BaseHire companion, Mobile target, out string reason)
        {
            reason = String.Empty;

            if (companion == null || companion.Deleted || !companion.Alive || companion.Map == null)
            {
                reason = "invalid_companion";
                return false;
            }

            if (target == null)
            {
                reason = "null_target";
                return false;
            }

            if (target.Deleted)
            {
                reason = "deleted_target";
                return false;
            }

            if (!target.Alive)
            {
                reason = "dead_target";
                return false;
            }

            if (target.Map == null || target.Map != companion.Map)
            {
                reason = "different_map";
                return false;
            }

            if (target == companion)
            {
                reason = "self_target";
                return false;
            }

            Mobile owner = companion.GetOwner();
            if (owner != null && target == owner)
            {
                reason = "owner_target";
                return false;
            }

            if (target.Player)
            {
                reason = "player_target";
                return false;
            }

            if (target is IAIGMCompanionActor || target is BaseHire)
            {
                reason = "companion_target";
                return false;
            }

            BaseCreature creature = target as BaseCreature;
            if (creature == null)
            {
                reason = "not_creature";
                return false;
            }

            if (creature.Blessed || creature.IsInvulnerable)
            {
                reason = "invulnerable_target";
                return false;
            }

            if (creature.Controlled || creature.Summoned)
            {
                reason = "controlled_or_summoned_target";
                return false;
            }

            if (owner != null && creature.ControlMaster == owner)
            {
                reason = "owner_controlled_ally";
                return false;
            }

            if (creature is BaseVendor || creature is BaseEscortable)
            {
                reason = "civilian_or_vendor_target";
                return false;
            }

            if (creature.Body == null || !creature.Body.IsAnimal)
            {
                reason = "not_animal_body";
                return false;
            }

            if (creature.Team == companion.Team && creature.Team != 0)
            {
                reason = "same_team";
                return false;
            }

            reason = "animal_candidate";
            return true;
        }

        public static AIGMCompanionTargetValidationResult ValidateAnimalTarget(BaseHire companion, Mobile target, int scanRange)
        {
            if (companion == null || companion.Deleted || !companion.Alive || companion.Map == null)
                return new AIGMCompanionTargetValidationResult(false, "invalid_companion");

            if (target == null)
                return new AIGMCompanionTargetValidationResult(false, "null_target");

            if (target.Deleted)
                return new AIGMCompanionTargetValidationResult(false, "deleted_target");

            if (!target.Alive)
                return new AIGMCompanionTargetValidationResult(false, "dead_target");

            if (target.Map == null || target.Map != companion.Map)
                return new AIGMCompanionTargetValidationResult(false, "different_map");

            string animalReason;
            if (!IsTrackableAnimalCandidate(companion, target, out animalReason))
                return new AIGMCompanionTargetValidationResult(false, animalReason);

            if (scanRange > 0 && !companion.InRange(target, scanRange))
                return new AIGMCompanionTargetValidationResult(false, "out_of_range");

            return new AIGMCompanionTargetValidationResult(true, "animal_target_valid");
        }
    }
}
