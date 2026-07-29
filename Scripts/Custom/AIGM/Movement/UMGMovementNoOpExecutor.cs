namespace Server.Custom.AIGM
{
    public sealed class UMGMovementNoOpExecutor : IUMGMovementExecutor
    {
        public UMGMovementExecutionResult Execute(UMGMovementExecutionRequest request)
        {
            if (request == null)
            {
                return UMGMovementExecutionResult.Failure(
                    "request_null",
                    "No movement execution request was provided.");
            }

            if (request.GateDecision == null)
            {
                return UMGMovementExecutionResult.Denied(
                    "gate_decision_missing",
                    "No movement gate decision was provided for execution.");
            }

            if (!request.GateDecision.IsAllowed)
            {
                return UMGMovementExecutionResult.Denied(
                    request.GateDecision.Reason ?? "gate_denied",
                    "Movement execution was denied by the supplied gate decision.",
                    request.GateDecision);
            }

            if (request.IsDryRun)
            {
                return UMGMovementExecutionResult.DryRun(
                    request.Reason ?? "dry_run",
                    "Dry-run request accepted; no live movement was performed.",
                    request.GateDecision);
            }

            if (!request.GateDecision.AllowsLiveMovement)
            {
                return UMGMovementExecutionResult.DryRun(
                    request.GateDecision.Reason ?? request.Reason ?? "live_movement_not_allowed",
                    "Request accepted as a no-op; gate allows no live movement in this phase.",
                    request.GateDecision);
            }

            return UMGMovementExecutionResult.DryRun(
                request.Reason ?? "no_op_executor",
                "No-op executor does not perform live movement.",
                request.GateDecision);
        }
    }
}
