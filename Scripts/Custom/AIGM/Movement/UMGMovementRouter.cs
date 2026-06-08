using System;

namespace Server.Custom.AIGM
{
    public class UMGMovementRouter
    {
        private readonly UMGMovementState _state;

        public UMGMovementRouter(UMGMovementState existingState = null)
        {
            _state = existingState ?? new UMGMovementState();
        }

        public UMGMovementState State
        {
            get { return _state; }
        }

        public bool TrySubmitIntent(UMGMovementIntent intent, out string reason)
        {
            reason = null;

            if (intent == null)
            {
                reason = "Intent is required.";
                return false;
            }

            switch (intent.Kind)
            {
                case UMGMovementIntentKind.Idle:
                    _state.Apply(intent);
                    _state.LastMovementDecision = "Idle";
                    _state.LastMovementDecisionUtc = DateTime.UtcNow;
                    return true;

                case UMGMovementIntentKind.FollowPlayer:
                case UMGMovementIntentKind.ReturnToPlayer:
                case UMGMovementIntentKind.TravelToNamedDestination:
                case UMGMovementIntentKind.MoveToPoint:
                case UMGMovementIntentKind.PursueTrackedTarget:
                case UMGMovementIntentKind.GuardTarget:
                case UMGMovementIntentKind.RecoverFromStuck:
                    _state.Apply(intent);
                    _state.LastMovementDecision = intent.Kind.ToString();
                    _state.LastMovementDecisionUtc = DateTime.UtcNow;
                    return true;

                case UMGMovementIntentKind.HoldPosition:
                    Hold(intent.Reason);
                    return true;

                default:
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
            if (_state.ActiveIntent != UMGMovementIntentKind.Idle && _state.ActiveIntent != UMGMovementIntentKind.HoldPosition)
                _state.SuspendedIntent = _state.ActiveIntent;

            _state.ActiveIntent = UMGMovementIntentKind.HoldPosition;
            _state.InterruptReason = reason;
            _state.TrackingMode = "Idle";
            _state.UpdatedUtc = DateTime.UtcNow;
            _state.LastMovementDecision = "HoldPosition";
            _state.LastMovementDecisionUtc = DateTime.UtcNow;
        }

        public void Stop(string reason = null)
        {
            if (_state.ActiveIntent != UMGMovementIntentKind.Idle)
                _state.SuspendedIntent = _state.ActiveIntent;

            _state.ActiveIntent = UMGMovementIntentKind.Idle;
            _state.InterruptReason = reason;
            _state.TrackingMode = "Idle";
            _state.UpdatedUtc = DateTime.UtcNow;
            _state.LastMovementDecision = "Stop";
            _state.LastMovementDecisionUtc = DateTime.UtcNow;
        }

        public void Suspend(string reason = null)
        {
            if (_state.ActiveIntent != UMGMovementIntentKind.Idle && _state.ActiveIntent != _state.SuspendedIntent)
                _state.SuspendedIntent = _state.ActiveIntent;

            _state.ActiveIntent = UMGMovementIntentKind.Idle;
            _state.InterruptReason = reason;
            _state.UpdatedUtc = DateTime.UtcNow;
            _state.LastMovementDecision = "Suspend";
            _state.LastMovementDecisionUtc = DateTime.UtcNow;
        }

        public void Resume(string reason = null)
        {
            if (_state.SuspendedIntent != UMGMovementIntentKind.Idle)
            {
                _state.ActiveIntent = _state.SuspendedIntent;
                _state.SuspendedIntent = UMGMovementIntentKind.Idle;
            }

            _state.InterruptReason = reason;
            _state.UpdatedUtc = DateTime.UtcNow;
            _state.LastMovementDecision = "Resume";
            _state.LastMovementDecisionUtc = DateTime.UtcNow;
        }

        public void CombatInterruption(string reason = null)
        {
            if (_state.ActiveIntent == UMGMovementIntentKind.TravelToNamedDestination
                || _state.ActiveIntent == UMGMovementIntentKind.MoveToPoint
                || _state.ActiveIntent == UMGMovementIntentKind.PursueTrackedTarget
                || _state.ActiveIntent == UMGMovementIntentKind.FollowPlayer
                || _state.ActiveIntent == UMGMovementIntentKind.ReturnToPlayer
                || _state.ActiveIntent == UMGMovementIntentKind.GuardTarget)
            {
                Suspend(reason ?? "combat_interruption");
                _state.LastMovementDecision = "CombatInterruption";
                _state.LastMovementDecisionUtc = DateTime.UtcNow;
            }
        }

        public void Clear()
        {
            _state.Clear();
            _state.LastMovementDecision = "Clear";
            _state.LastMovementDecisionUtc = DateTime.UtcNow;
        }

        public UMGMovementState GetState()
        {
            return _state;
        }
    }
}
