using System;
using Server;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class UMGMovementRouter
    {
        public static bool RouteIntent(BaseHire companion, UMGMovementIntent intent, out string response)
        {
            response = null;

            if (companion == null || companion.Deleted || intent == null)
                return false;

            switch (intent.Kind)
            {
                case UMGMovementIntentKind.Idle:
                    RecordIntent(companion, intent);
                    response = "I am standing by.";
                    return true;
                case UMGMovementIntentKind.FollowPlayer:
                    return RouteFollowPlayer(companion, intent, out response);
                case UMGMovementIntentKind.HoldPosition:
                    return RouteHoldPosition(companion, intent, out response);
                case UMGMovementIntentKind.ReturnToPlayer:
                    return RouteReturnToPlayer(companion, intent, out response);
                case UMGMovementIntentKind.TravelToNamedDestination:
                    response = "I am no longer using destination travel.";
                    return false;
                case UMGMovementIntentKind.MoveToPoint:
                    response = "I am no longer using coordinate travel.";
                    return false;
                case UMGMovementIntentKind.PursueTrackedTarget:
                    return RoutePursueTrackedTarget(companion, intent, out response);
                case UMGMovementIntentKind.GuardTarget:
                    return RouteGuardTarget(companion, intent, out response);
                case UMGMovementIntentKind.RecoverFromStuck:
                    RecordIntent(companion, intent);
                    response = "Recovering movement.";
                    return true;
                default:
                    return false;
            }
        }

        public static void RecordIntent(BaseHire companion, UMGMovementIntent intent)
        {
            if (companion == null)
                return;

            UMGMovementState state = AIGMCompanionStateAccess.GetMovementState(companion) ?? new UMGMovementState();
            state.Apply(intent);
            AIGMCompanionStateAccess.SetMovementState(companion, state);
        }

        public static void SetTrackedPursuitState(BaseHire companion, AIGMCompanionTrackingEntry entry)
        {
            if (companion == null || entry == null)
                return;

            UMGMovementIntent intent = UMGMovementIntent.Create(UMGMovementIntentKind.PursueTrackedTarget);
            intent.TargetSerial = entry.TargetSerial;
            intent.TargetName = entry.Name;
            intent.Reason = entry.Category.ToString();
            RecordIntent(companion, intent);
        }

        public static void ClearTrackedPursuitState(BaseHire companion, string reason)
        {
            if (companion == null)
                return;

            UMGMovementIntent intent = UMGMovementIntent.Create(UMGMovementIntentKind.Idle);
            intent.Reason = reason;
            RecordIntent(companion, intent);
        }

        public static void NoteRecoverFromStuck(BaseHire companion, string reason)
        {
            if (companion == null)
                return;

            UMGMovementIntent intent = UMGMovementIntent.Create(UMGMovementIntentKind.RecoverFromStuck);
            intent.Reason = reason;
            RecordIntent(companion, intent);
        }

        private static bool RouteFollowPlayer(BaseHire companion, UMGMovementIntent intent, out string response)
        {
            Mobile target = intent.TargetMobile ?? intent.Requester ?? companion.GetOwner();
            if (target == null)
            {
                response = "I do not know whom to follow.";
                return false;
            }

            AIGMCompanionTrackingCycle.SuspendPursuitAndTravel(companion);
            AIGMCompanionStateAccess.ClearHoldPosition(companion);

            companion.CantWalk = false;
            companion.Combatant = null;
            companion.ControlTarget = target;
            companion.ControlOrder = OrderType.Follow;

            RecordIntent(companion, intent);
            response = "I am with you.";
            return true;
        }

        private static bool RouteHoldPosition(BaseHire companion, UMGMovementIntent intent, out string response)
        {
            AIGMCompanionStateAccess.ResetAllCompanionIntentState(companion);
            AIGMCompanionStateAccess.LatchHoldPosition(companion);
            UMGMovementRouter.ClearTrackedPursuitState(companion, "hold_position");

            RecordIntent(companion, intent);
            response = "I will hold here.";
            return true;
        }

        private static bool RouteReturnToPlayer(BaseHire companion, UMGMovementIntent intent, out string response)
        {
            Mobile requester = intent.Requester ?? companion.GetOwner();
            if (requester == null)
            {
                response = "I do not know where to return.";
                return false;
            }

            AIGMCompanionTrackingCycle.SuspendPursuitAndTravel(companion);
            AIGMCompanionStateAccess.ClearHoldPosition(companion);

            companion.CantWalk = false;
            companion.Combatant = null;
            companion.ControlTarget = requester;
            companion.ControlOrder = OrderType.Come;

            RecordIntent(companion, intent);
            response = "On my way.";
            return true;
        }

        private static bool RouteTravelToNamedDestination(BaseHire companion, UMGMovementIntent intent, out string response)
        {
            if (String.IsNullOrWhiteSpace(intent.DestinationName))
            {
                response = "I do not know that destination.";
                return false;
            }

            bool started = AIGMCompanionTravelController.StartTravel(companion, intent.DestinationName, out response);
            if (started)
                RecordIntent(companion, intent);

            return started;
        }

        private static bool RouteMoveToPoint(BaseHire companion, UMGMovementIntent intent, out string response)
        {
            Mobile requester = intent.Requester ?? companion.GetOwner();
            if (requester == null)
            {
                response = "I cannot move there without an owner.";
                return false;
            }

            Map map = intent.DestinationMap ?? companion.Map;
            string label = String.IsNullOrWhiteSpace(intent.DestinationName) ? null : intent.DestinationName;
            bool started = AIGMMovementController.StartPathToPoint(requester, companion, intent.DestinationPoint, map, label, AIGMMovementStartOptions.ForCompanionTravel(), out response);
            if (started)
                RecordIntent(companion, intent);

            return started;
        }

        private static bool RoutePursueTrackedTarget(BaseHire companion, UMGMovementIntent intent, out string response)
        {
            Mobile target = intent.TargetMobile;
            if (target == null && intent.TargetSerial != 0)
                target = World.FindMobile(intent.TargetSerial);

            if (target == null || target.Deleted || !target.Alive)
            {
                response = "I cannot pursue that target right now.";
                return false;
            }

            AIGMCompanionStateAccess.ClearHoldPosition(companion);
            companion.CantWalk = false;
            companion.ControlTarget = target;
            RecordIntent(companion, intent);
            response = "I am moving on the tracked target.";
            return true;
        }

        private static bool RouteGuardTarget(BaseHire companion, UMGMovementIntent intent, out string response)
        {
            Mobile target = intent.TargetMobile ?? intent.Requester ?? companion.GetOwner();
            if (target == null)
            {
                response = "I do not know whom to guard.";
                return false;
            }

            AIGMCompanionTrackingCycle.SuspendPursuitAndTravel(companion);
            AIGMCompanionStateAccess.ClearHoldPosition(companion);
            AIGMCompanionStateAccess.SetGuardOwnerMode(companion, true);

            companion.CantWalk = false;
            companion.Combatant = null;
            companion.ControlTarget = target;
            companion.ControlOrder = OrderType.Guard;

            RecordIntent(companion, intent);
            response = "I will guard you.";
            return true;
        }
    }
}
