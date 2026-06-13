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

            if (target == companion)
                return new AIGMCompanionTargetValidationResult(false, "self_target");

            Mobile owner = companion.GetOwner();
            if (owner != null && target == owner)
                return new AIGMCompanionTargetValidationResult(false, "owner_target");

            IAIGMCompanionActor companionActor = target as IAIGMCompanionActor;
            if (companionActor != null)
                return new AIGMCompanionTargetValidationResult(false, "companion_target");

            if (target.Player)
                return new AIGMCompanionTargetValidationResult(false, "player_target");

            BaseCreature creature = target as BaseCreature;
            if (creature == null)
                return new AIGMCompanionTargetValidationResult(false, "not_creature");

            if (creature.Blessed || creature.IsInvulnerable)
                return new AIGMCompanionTargetValidationResult(false, "invulnerable_target");

            if (creature.Controlled || creature.Summoned)
                return new AIGMCompanionTargetValidationResult(false, "controlled_or_summoned_target");

            if (owner != null && creature.ControlMaster == owner)
                return new AIGMCompanionTargetValidationResult(false, "owner_controlled_ally");

            if (creature is BaseVendor || creature is BaseEscortable)
                return new AIGMCompanionTargetValidationResult(false, "civilian_or_vendor_target");

            if (creature.Body != null && creature.Body.IsHuman)
                return new AIGMCompanionTargetValidationResult(false, "human_target");

            if (scanRange > 0 && !companion.InRange(target, scanRange))
                return new AIGMCompanionTargetValidationResult(false, "out_of_range");

            if (creature.Team == companion.Team && creature.Team != 0)
                return new AIGMCompanionTargetValidationResult(false, "same_team");

            return new AIGMCompanionTargetValidationResult(true, "monster_target_valid");
        }
    }
}
