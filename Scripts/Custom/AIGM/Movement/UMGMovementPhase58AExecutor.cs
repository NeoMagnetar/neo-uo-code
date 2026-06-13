using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public sealed class UMGMovementPhase58AExecutor : IUMGMovementExecutor
    {
        public UMGMovementExecutionResult Execute(UMGMovementExecutionRequest request)
        {
            if (request == null || request.Intent == null)
                return UMGMovementExecutionResult.Failure("invalid_request", "Request or intent was null.");

            BaseHire companion = request.Intent.Requester as BaseHire;
            Mobile target = request.Intent.TargetMobile;

            if (companion == null || companion.Deleted || !companion.Alive || companion.Map == null)
                return UMGMovementExecutionResult.Failure("invalid_companion", "Requester was not a live companion.");

            if (request.Intent.Kind != UMGMovementIntentKind.PursueTrackedTarget)
                return UMGMovementExecutionResult.Denied("intent_not_supported", "Phase58A executor only supports PursueTrackedTarget.");

            AIGMCompanionTargetValidationResult validation = AIGMCompanionTargetValidator.ValidateMonsterTarget(companion, target, 12);
            if (!validation.Allowed)
                return UMGMovementExecutionResult.Denied(validation.Reason, "Target failed monster-only pursuit validation.");

            if (companion.InRange(target, 1))
                return UMGMovementExecutionResult.Success("already_in_range", "Companion is already adjacent to target.");

            if (!companion.InRange(target, 12))
                return UMGMovementExecutionResult.Denied("target_out_of_short_range", "Phase58A movement remains short-range only.");

            Direction direction = companion.GetDirectionTo(target);
            companion.Direction = direction;
            bool stepped = companion.Move(direction);

            return stepped
                ? UMGMovementExecutionResult.Success("step_toward_target", "Companion took a bounded pursuit step.")
                : UMGMovementExecutionResult.Failure("step_blocked", "Companion could not take a bounded pursuit step.");
        }
    }
}
