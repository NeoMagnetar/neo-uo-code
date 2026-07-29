using System;

namespace Server.Custom.AIGM
{
    public sealed class UMGMovementExecutionResult
    {
        public bool Succeeded { get; set; }
        public bool WasExecuted { get; set; }
        public bool WasDryRun { get; set; }
        public bool WasDenied { get; set; }
        public string Reason { get; set; }
        public string Detail { get; set; }
        public UMGMovementGateDecision GateDecision { get; set; }
        public DateTime CompletedUtc { get; set; }

        public static UMGMovementExecutionResult Success(string reason = null, string detail = null, UMGMovementGateDecision gateDecision = null)
        {
            return new UMGMovementExecutionResult
            {
                Succeeded = true,
                WasExecuted = true,
                WasDryRun = false,
                WasDenied = false,
                Reason = reason,
                Detail = detail,
                GateDecision = gateDecision,
                CompletedUtc = DateTime.UtcNow
            };
        }

        public static UMGMovementExecutionResult Failure(string reason, string detail = null, UMGMovementGateDecision gateDecision = null)
        {
            return new UMGMovementExecutionResult
            {
                Succeeded = false,
                WasExecuted = false,
                WasDryRun = false,
                WasDenied = false,
                Reason = reason,
                Detail = detail,
                GateDecision = gateDecision,
                CompletedUtc = DateTime.UtcNow
            };
        }

        public static UMGMovementExecutionResult Denied(string reason, string detail = null, UMGMovementGateDecision gateDecision = null)
        {
            return new UMGMovementExecutionResult
            {
                Succeeded = false,
                WasExecuted = false,
                WasDryRun = false,
                WasDenied = true,
                Reason = reason,
                Detail = detail,
                GateDecision = gateDecision,
                CompletedUtc = DateTime.UtcNow
            };
        }

        public static UMGMovementExecutionResult DryRun(string reason = null, string detail = null, UMGMovementGateDecision gateDecision = null)
        {
            return new UMGMovementExecutionResult
            {
                Succeeded = true,
                WasExecuted = false,
                WasDryRun = true,
                WasDenied = false,
                Reason = reason,
                Detail = detail,
                GateDecision = gateDecision,
                CompletedUtc = DateTime.UtcNow
            };
        }
    }
}
