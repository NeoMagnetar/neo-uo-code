using System;

namespace Server.Custom.AIGM
{
    public class UMGMovementRouter
    {
        private readonly UMGMovementState _state;
        private readonly IUMGMovementExecutor _executor;

        public UMGMovementRouter(UMGMovementState existingState = null)
            : this(existingState, null)
        {
        }

        public UMGMovementRouter(UMGMovementState existingState, IUMGMovementExecutor executor)
        {
            _state = existingState ?? new UMGMovementState();
            _executor = executor ?? new UMGMovementNoOpExecutor();
        }

        public UMGMovementState State
        {
            get { return _state; }
        }

        public UMGMovementExecutionResult LastExecutionResult { get; private set; }

        public bool TrySubmitIntent(UMGMovementIntent intent, out string reason)
        {
            UMGMovementGateDecision decision = EvaluateIntentGate(intent);
            reason = decision != null ? decision.Reason : null;

            if (decision == null || !decision.IsAllowed || !decision.AllowsStateUpdate)
            {
                LastExecutionResult = null;
                return false;
            }

            if (intent == null)
            {
                LastExecutionResult = null;
                return false;
            }

            switch (intent.Kind)
            {
                case UMGMovementIntentKind.HoldPosition:
                    LastExecutionResult = null;
                    Hold(intent.Reason);
                    return true;
                case UMGMovementIntentKind.Idle:
                case UMGMovementIntentKind.FollowPlayer:
                case UMGMovementIntentKind.ReturnToPlayer:
                case UMGMovementIntentKind.TravelToNamedDestination:
                case UMGMovementIntentKind.MoveToPoint:
                case UMGMovementIntentKind.PursueTrackedTarget:
                case UMGMovementIntentKind.GuardTarget:
                case UMGMovementIntentKind.RecoverFromStuck:
                    ApplyIntentToState(intent);
                    LastExecutionResult = TryExecuteNoOp(intent, decision);
                    return true;
                default:
                    LastExecutionResult = null;
                    reason = "Unsupported movement intent kind.";
                    return false;
            }
        }

        public void ApplyIntentToState(UMGMovementIntent intent)
        {
            if (intent == null)
                return;

            _state.Apply(intent);
            _state.LastMovementDecision = intent.Kind.ToString();
            _state.LastMovementDecisionUtc = DateTime.UtcNow;
        }

        public void Hold(string reason = null)
        {
            UMGMovementGateDecision decision = EvaluateHoldGate(reason);
            if (decision == null || !decision.IsAllowed || !decision.AllowsStateUpdate)
                return;

            if (CanSuspendCurrentIntentForStateTransition(_state.ActiveIntent))
                _state.SuspendedIntent = _state.ActiveIntent;

            _state.ActiveIntent = UMGMovementIntentKind.HoldPosition;
            _state.InterruptReason = String.IsNullOrWhiteSpace(reason) ? "hold_requested" : reason;
            _state.TrackingMode = "Idle";
            _state.UpdatedUtc = DateTime.UtcNow;
            _state.LastMovementDecision = "HoldPosition";
            _state.LastMovementDecisionUtc = DateTime.UtcNow;
        }

        public void Stop(string reason = null)
        {
            UMGMovementGateDecision decision = EvaluateStopGate(reason);
            if (decision == null || !decision.IsAllowed || !decision.AllowsStateUpdate)
                return;

            if (decision.SuspendsActiveIntent && CanSuspendCurrentIntentForStateTransition(_state.ActiveIntent))
                _state.SuspendedIntent = _state.ActiveIntent;

            if (decision.ClearsActiveIntent)
                _state.ActiveIntent = UMGMovementIntentKind.Idle;

            _state.InterruptReason = String.IsNullOrWhiteSpace(reason) ? "stop_requested" : reason;
            _state.TrackingMode = "Idle";
            _state.UpdatedUtc = DateTime.UtcNow;
            _state.LastMovementDecision = "Stop";
            _state.LastMovementDecisionUtc = DateTime.UtcNow;
        }

        public void Suspend(string reason = null)
        {
            if (!CanSuspendCurrentIntentForStateTransition(_state.ActiveIntent))
            {
                _state.InterruptReason = String.IsNullOrWhiteSpace(reason) ? "suspend_ignored" : reason;
                _state.UpdatedUtc = DateTime.UtcNow;
                _state.LastMovementDecision = "SuspendIgnored";
                _state.LastMovementDecisionUtc = DateTime.UtcNow;
                return;
            }

            _state.SuspendedIntent = _state.ActiveIntent;
            _state.ActiveIntent = UMGMovementIntentKind.Idle;
            _state.InterruptReason = String.IsNullOrWhiteSpace(reason) ? "suspend_requested" : reason;
            _state.UpdatedUtc = DateTime.UtcNow;
            _state.LastMovementDecision = "Suspend";
            _state.LastMovementDecisionUtc = DateTime.UtcNow;
        }

        public void Resume(string reason = null)
        {
            UMGMovementGateDecision decision = EvaluateResumeGate(reason);
            if (decision == null || !decision.IsAllowed || !decision.AllowsStateUpdate)
                return;

            UMGMovementIntentKind resumedIntent = _state.SuspendedIntent;
            if (resumedIntent != UMGMovementIntentKind.Idle)
            {
                _state.ActiveIntent = resumedIntent;
                _state.SuspendedIntent = UMGMovementIntentKind.Idle;
            }

            _state.InterruptReason = String.IsNullOrWhiteSpace(reason) ? "resume_requested" : reason;
            _state.UpdatedUtc = DateTime.UtcNow;
            _state.LastMovementDecision = resumedIntent != UMGMovementIntentKind.Idle ? "Resume" : "ResumeNoOp";
            _state.LastMovementDecisionUtc = DateTime.UtcNow;
        }

        public void CombatInterruption(string reason = null)
        {
            UMGMovementGateDecision decision = EvaluateCombatInterruptionGate(reason);
            if (decision == null || !decision.IsAllowed || !decision.AllowsStateUpdate)
                return;

            if (decision.SuspendsActiveIntent && IsCombatInterruptibleIntent(_state.ActiveIntent))
            {
                Suspend(reason ?? "combat_interruption");
                _state.LastMovementDecision = "CombatInterruption";
                _state.LastMovementDecisionUtc = DateTime.UtcNow;
                return;
            }

            _state.InterruptReason = String.IsNullOrWhiteSpace(reason) ? "combat_interruption_ignored" : reason;
            _state.UpdatedUtc = DateTime.UtcNow;
            _state.LastMovementDecision = "CombatInterruptionIgnored";
            _state.LastMovementDecisionUtc = DateTime.UtcNow;
        }

        public void Clear()
        {
            _state.Clear();
            _state.InterruptReason = "clear_requested";
            _state.UpdatedUtc = DateTime.UtcNow;
            _state.LastMovementDecision = "Clear";
            _state.LastMovementDecisionUtc = DateTime.UtcNow;
            LastExecutionResult = null;
        }

        public UMGMovementState GetState()
        {
            return _state;
        }

        public UMGMovementGateDecision EvaluateGate(UMGMovementGateKind gateKind, UMGMovementIntent intent = null, string reason = null)
        {
            switch (gateKind)
            {
                case UMGMovementGateKind.MovementExecution:
                    return UMGMovementGateDecision.Deny(
                        gateKind,
                        reason ?? "Live movement execution is disabled in the current router phase.",
                        allowsStateUpdate: true,
                        allowsLiveMovement: false,
                        preservesDestination: true);
                case UMGMovementGateKind.TrackingScan:
                    return UMGMovementGateDecision.Allow(
                        gateKind,
                        reason ?? "Tracking scan/report remains non-movement.",
                        allowsStateUpdate: true,
                        allowsLiveMovement: false,
                        isScanOnly: true,
                        preservesDestination: true);
                case UMGMovementGateKind.Pursuit:
                    return EvaluatePursuitGate(intent);
                case UMGMovementGateKind.Travel:
                    return UMGMovementGateDecision.Allow(
                        gateKind,
                        reason ?? "Travel intent may be recorded but not executed.",
                        allowsStateUpdate: true,
                        allowsLiveMovement: false,
                        preservesDestination: true);
                case UMGMovementGateKind.FollowGuard:
                    return UMGMovementGateDecision.Allow(
                        gateKind,
                        reason ?? "Follow/guard intent may be recorded but not executed.",
                        allowsStateUpdate: true,
                        allowsLiveMovement: false,
                        preservesDestination: true);
                case UMGMovementGateKind.Hold:
                    return EvaluateHoldGate(reason);
                case UMGMovementGateKind.Stop:
                    return EvaluateStopGate(reason);
                case UMGMovementGateKind.Resume:
                    return EvaluateResumeGate(reason);
                case UMGMovementGateKind.CombatInterruption:
                    return EvaluateCombatInterruptionGate(reason);
                case UMGMovementGateKind.StateAccessBoundary:
                    return UMGMovementGateDecision.Deny(
                        gateKind,
                        reason ?? "StateAccess may not bypass router authority.",
                        allowsStateUpdate: false,
                        allowsLiveMovement: false,
                        preservesDestination: true);
                case UMGMovementGateKind.ParserBoundary:
                    return UMGMovementGateDecision.Deny(
                        gateKind,
                        reason ?? "Parser output is intent-only and may not execute movement.",
                        allowsStateUpdate: false,
                        allowsLiveMovement: false,
                        preservesDestination: true);
                case UMGMovementGateKind.SkillExecutorBoundary:
                    return UMGMovementGateDecision.Deny(
                        gateKind,
                        reason ?? "Skill executor may not execute movement.",
                        allowsStateUpdate: false,
                        allowsLiveMovement: false,
                        preservesDestination: true);
                default:
                    return UMGMovementGateDecision.Deny(
                        gateKind,
                        reason ?? "Unknown movement gate.",
                        allowsStateUpdate: false,
                        allowsLiveMovement: false,
                        preservesDestination: true);
            }
        }

        public UMGMovementGateDecision EvaluateIntentGate(UMGMovementIntent intent)
        {
            if (intent == null)
            {
                return UMGMovementGateDecision.Deny(
                    UMGMovementGateKind.MovementExecution,
                    "Intent is required.",
                    allowsStateUpdate: false,
                    allowsLiveMovement: false,
                    preservesDestination: true);
            }

            switch (intent.Kind)
            {
                case UMGMovementIntentKind.Idle:
                case UMGMovementIntentKind.RecoverFromStuck:
                    return UMGMovementGateDecision.Allow(
                        UMGMovementGateKind.MovementExecution,
                        "Intent may update bounded state only.",
                        allowsStateUpdate: true,
                        allowsLiveMovement: false,
                        preservesDestination: true);
                case UMGMovementIntentKind.HoldPosition:
                    return EvaluateHoldGate(intent.Reason);
                case UMGMovementIntentKind.TravelToNamedDestination:
                case UMGMovementIntentKind.MoveToPoint:
                    return EvaluateGate(UMGMovementGateKind.Travel, intent, intent.Reason);
                case UMGMovementIntentKind.FollowPlayer:
                case UMGMovementIntentKind.ReturnToPlayer:
                case UMGMovementIntentKind.GuardTarget:
                    return EvaluateGate(UMGMovementGateKind.FollowGuard, intent, intent.Reason);
                case UMGMovementIntentKind.PursueTrackedTarget:
                    return EvaluatePursuitGate(intent);
                default:
                    return UMGMovementGateDecision.Deny(
                        UMGMovementGateKind.MovementExecution,
                        "Unsupported movement intent kind.",
                        allowsStateUpdate: false,
                        allowsLiveMovement: false,
                        preservesDestination: true);
            }
        }

        public UMGMovementGateDecision EvaluateTrackingGate(UMGMovementIntent intent)
        {
            return UMGMovementGateDecision.Allow(
                UMGMovementGateKind.TrackingScan,
                "Tracking scan/report cannot cause movement.",
                allowsStateUpdate: true,
                allowsLiveMovement: false,
                isScanOnly: true,
                isReportOnly: intent == null,
                preservesDestination: true);
        }

        public UMGMovementGateDecision EvaluatePursuitGate(UMGMovementIntent intent)
        {
            if (intent == null || intent.Kind != UMGMovementIntentKind.PursueTrackedTarget)
            {
                return UMGMovementGateDecision.Deny(
                    UMGMovementGateKind.Pursuit,
                    "Pursuit requires explicit pursuit intent.",
                    allowsStateUpdate: false,
                    allowsLiveMovement: false,
                    requiresExplicitIntent: true,
                    preservesDestination: true);
            }

            return UMGMovementGateDecision.Allow(
                UMGMovementGateKind.Pursuit,
                "Explicit pursuit intent may be recorded without movement execution.",
                allowsStateUpdate: true,
                allowsLiveMovement: false,
                requiresExplicitIntent: true,
                preservesDestination: true);
        }

        public UMGMovementGateDecision EvaluateHoldGate(string reason = null)
        {
            return UMGMovementGateDecision.Allow(
                UMGMovementGateKind.Hold,
                reason ?? "Hold may suppress autonomy in bounded state only.",
                allowsStateUpdate: true,
                allowsLiveMovement: false,
                suspendsActiveIntent: true,
                preservesDestination: true);
        }

        public UMGMovementGateDecision EvaluateStopGate(string reason = null)
        {
            return UMGMovementGateDecision.Allow(
                UMGMovementGateKind.Stop,
                reason ?? "Stop may clear or suspend bounded state only.",
                allowsStateUpdate: true,
                allowsLiveMovement: false,
                suspendsActiveIntent: true,
                clearsActiveIntent: true,
                preservesDestination: true);
        }

        public UMGMovementGateDecision EvaluateResumeGate(string reason = null)
        {
            if (_state.SuspendedIntent == UMGMovementIntentKind.Idle)
            {
                return UMGMovementGateDecision.Deny(
                    UMGMovementGateKind.Resume,
                    reason ?? "There is no suspended movement intent to resume.",
                    allowsStateUpdate: false,
                    allowsLiveMovement: false,
                    preservesDestination: true);
            }

            if (_state.ActiveIntent == UMGMovementIntentKind.HoldPosition)
            {
                return UMGMovementGateDecision.Deny(
                    UMGMovementGateKind.Resume,
                    reason ?? "Resume is blocked while hold remains active.",
                    allowsStateUpdate: false,
                    allowsLiveMovement: false,
                    preservesDestination: true);
            }

            return UMGMovementGateDecision.Allow(
                UMGMovementGateKind.Resume,
                reason ?? "Suspended movement intent may be restored in bounded state only.",
                allowsStateUpdate: true,
                allowsLiveMovement: false,
                preservesDestination: true);
        }

        public UMGMovementGateDecision EvaluateCombatInterruptionGate(string reason = null)
        {
            return UMGMovementGateDecision.Allow(
                UMGMovementGateKind.CombatInterruption,
                reason ?? "Combat interruption may suspend bounded movement state without erasing destination.",
                allowsStateUpdate: true,
                allowsLiveMovement: false,
                suspendsActiveIntent: true,
                preservesDestination: true);
        }

        private UMGMovementExecutionResult TryExecuteNoOp(UMGMovementIntent intent, UMGMovementGateDecision decision)
        {
            if (!ShouldSubmitToExecutor(intent, decision))
                return null;

            UMGMovementExecutionRequest request = BuildExecutionRequest(intent, decision);
            IUMGMovementExecutor executor = _executor ?? new UMGMovementNoOpExecutor();
            return executor.Execute(request);
        }

        private bool ShouldSubmitToExecutor(UMGMovementIntent intent, UMGMovementGateDecision decision)
        {
            if (intent == null || decision == null)
                return false;

            if (!decision.IsAllowed || !decision.AllowsStateUpdate)
                return false;

            switch (intent.Kind)
            {
                case UMGMovementIntentKind.MoveToPoint:
                case UMGMovementIntentKind.TravelToNamedDestination:
                case UMGMovementIntentKind.FollowPlayer:
                case UMGMovementIntentKind.ReturnToPlayer:
                case UMGMovementIntentKind.GuardTarget:
                case UMGMovementIntentKind.PursueTrackedTarget:
                    return true;
                default:
                    return false;
            }
        }

        private UMGMovementExecutionRequest BuildExecutionRequest(UMGMovementIntent intent, UMGMovementGateDecision decision)
        {
            return new UMGMovementExecutionRequest
            {
                RequestId = Guid.NewGuid().ToString("N"),
                Source = "UMGMovementRouter",
                Reason = intent != null ? intent.Reason : null,
                IsDryRun = true,
                Intent = intent,
                State = _state,
                GateDecision = decision,
                ActorId = null,
                ActorProfileKey = _state.RoleProfileKey
            };
        }

        private static bool CanSuspendCurrentIntentForStateTransition(UMGMovementIntentKind intentKind)
        {
            return intentKind != UMGMovementIntentKind.Idle && intentKind != UMGMovementIntentKind.HoldPosition;
        }

        private static bool IsCombatInterruptibleIntent(UMGMovementIntentKind intentKind)
        {
            return intentKind == UMGMovementIntentKind.TravelToNamedDestination
                || intentKind == UMGMovementIntentKind.MoveToPoint
                || intentKind == UMGMovementIntentKind.PursueTrackedTarget
                || intentKind == UMGMovementIntentKind.FollowPlayer
                || intentKind == UMGMovementIntentKind.ReturnToPlayer
                || intentKind == UMGMovementIntentKind.GuardTarget;
        }
    }
}
