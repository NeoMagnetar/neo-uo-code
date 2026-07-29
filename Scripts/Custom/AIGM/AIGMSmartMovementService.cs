using System;
using System.Collections.Generic;

using Server;
using Server.Custom.AIGM.Tasks;

namespace Server.Custom.AIGM
{
    public enum AIGMSmartMovementKind
    {
        None,
        FollowMobile,
        MoveToPoint,
        RouteToPoint
    }

    public enum AIGMSmartMovementPhase
    {
        Idle,
        Starting,
        Moving,
        HoldingDistance,
        SoftBlocked,
        Backtracking,
        BoundaryFollow,
        LateralDetour,
        ReacquireGoal,
        Arrived,
        Blocked,
        PathBlockedFinal,
        Cancelled,
        Invalid
    }

    public sealed class AIGMSmartMovementState
    {
        public int ActorSerial;
        public int RequesterSerial;
        public AIGMSmartMovementKind Kind;
        public AIGMSmartMovementPhase Phase;
        public Point3D Destination;
        public Map DestinationMap;
        public Point3D FinalDestination;
        public Map FinalDestinationMap;
        public int TargetSerial;
        public string TargetName;
        public string Label;
        public string FinalLabel;
        public int CurrentWaypointIndex;
        public List<Point3D> RouteWaypoints;
        public List<string> RouteWaypointLabels;
        public int GoalMinDistance;
        public int GoalMaxDistance;
        public int GoalRadius;
        public Point3D LastPosition;
        public int LastDistance;
        public int BestDistance;
        public int StuckCounter;
        public int CorrectionAttempts;
        public Direction LastDirection;
        public Direction LastBlockedDirection;
        public Direction BacktrackDirection;
        public Direction DetourDirection;
        public Direction BoundaryDirection;
        public int BacktrackStepsRemaining;
        public int DetourStepsRemaining;
        public int BoundaryStepsTaken;
        public int BoundaryStepsRemaining;
        public int BoundaryOpeningTicks;
        public int BoundaryHandSwitches;
        public int BoundaryNextOpeningCheckStep;
        public int DetourCycles;
        public int ReacquireTicks;
        public int ProgressTicks;
        public bool PreferRightDetour;
        public int DetourRotationDirection;
        public int DetourRotationStep;
        public string LastBlockedTile;
        public string BoundaryStartTile;
        public int BoundaryStartDistance;
        public int BoundaryBestDistance;
        public string LastResult;
        public string LastStopReason;
        public Point3D LastTargetPosition;
        public DateTime NextFollowStepUtc;
        public DateTime StartedUtc;
        public DateTime UpdatedUtc;
        public Queue<string> RecentTiles;
        public Queue<Point3D> RecentPositions;
        public Dictionary<string, int> FailedTiles;

        public AIGMSmartMovementState()
        {
            RecentTiles = new Queue<string>();
            RecentPositions = new Queue<Point3D>();
            FailedTiles = new Dictionary<string, int>();
            RouteWaypoints = new List<Point3D>();
            RouteWaypointLabels = new List<string>();
            Reset();
        }

        public void Reset()
        {
            Kind = AIGMSmartMovementKind.None;
            Phase = AIGMSmartMovementPhase.Idle;
            Destination = Point3D.Zero;
            DestinationMap = null;
            FinalDestination = Point3D.Zero;
            FinalDestinationMap = null;
            TargetSerial = 0;
            TargetName = String.Empty;
            Label = String.Empty;
            FinalLabel = String.Empty;
            CurrentWaypointIndex = 0;
            RouteWaypoints.Clear();
            RouteWaypointLabels.Clear();
            GoalMinDistance = 1;
            GoalMaxDistance = 2;
            GoalRadius = 1;
            LastPosition = Point3D.Zero;
            LastDistance = -1;
            BestDistance = Int32.MaxValue;
            StuckCounter = 0;
            CorrectionAttempts = 0;
            LastDirection = Direction.North;
            LastBlockedDirection = Direction.North;
            BacktrackDirection = Direction.South;
            DetourDirection = Direction.East;
            BoundaryDirection = Direction.East;
            BacktrackStepsRemaining = 0;
            DetourStepsRemaining = 0;
            BoundaryStepsTaken = 0;
            BoundaryStepsRemaining = 0;
            BoundaryOpeningTicks = 0;
            BoundaryHandSwitches = 0;
            BoundaryNextOpeningCheckStep = 0;
            DetourCycles = 0;
            ReacquireTicks = 0;
            ProgressTicks = 0;
            PreferRightDetour = true;
            DetourRotationDirection = 1;
            DetourRotationStep = 0;
            LastBlockedTile = String.Empty;
            BoundaryStartTile = String.Empty;
            BoundaryStartDistance = Int32.MaxValue;
            BoundaryBestDistance = Int32.MaxValue;
            LastResult = "idle";
            LastStopReason = String.Empty;
            LastTargetPosition = Point3D.Zero;
            NextFollowStepUtc = DateTime.MinValue;
            StartedUtc = DateTime.MinValue;
            UpdatedUtc = DateTime.UtcNow;
            RecentTiles.Clear();
            RecentPositions.Clear();
            FailedTiles.Clear();
        }
    }

    public static class AIGMSmartMovementService
    {
        private static readonly Dictionary<int, AIGMSmartMovementState> States = new Dictionary<int, AIGMSmartMovementState>();
        private static readonly Dictionary<int, SmartMovementTimer> Timers = new Dictionary<int, SmartMovementTimer>();
        private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(0.25);
        private const int MaxStuckTicks = 12;
        private const int MaxCorrectionAttempts = 34;
        private const int MaxDetourCycles = 6;
        private const int MaxDetourRotationStep = 7;
        private const int RecentTileLimit = 12;
        private const int BacktrackStepCount = 3;
        private const int BaseDetourStepCount = 5;
        private const int MaxDetourStepCount = 8;
        private const int MinBoundaryFollowSteps = 20;
        private const int MaxBoundaryFollowSteps = 80;
        private const int OpeningRequiredProgressTicks = 3;
        private const int OpeningProbeRetryDelaySteps = 6;
        private const int MaxBoundaryHandSwitches = 1;
        private const int BoundaryLoopNearStartDistance = 3;
        private const int ReacquireRequiredProgressTicks = 2;
        private const int ReacquireRetryTicks = 4;
        private const int FollowHoldDistance = 2;
        private const int FollowResumeDistance = 5;
        private const int FollowTrailingDistance = 3;

        public static bool StartFollow(Mobile actor, Mobile target, Mobile requester, out string response)
        {
            response = null;

            if (!IsValidActor(actor, out response))
                return false;

            if (!AIGMOperationalControlService.CanOperate(actor, AIGMOperationalAction.Follow, AIGMOperationalControlService.GetEpoch(actor), false))
            {
                response = "Operational control rejected follow.";
                return false;
            }

            if (!IsValidTarget(target))
            {
                response = "That is not a valid follow target.";
                return false;
            }

            if (actor == target)
            {
                response = "You cannot follow yourself.";
                return false;
            }

            if (actor.Map == null || target.Map == null || actor.Map != target.Map)
            {
                response = "You must be on the same map as the follow target.";
                return false;
            }

            AIGMSmartMovementState state = PrepareState(actor, requester, "follow:" + SafeName(target));
            state.Kind = AIGMSmartMovementKind.FollowMobile;
            state.TargetSerial = target.Serial.Value;
            state.TargetName = SafeName(target);
            state.Destination = target.Location;
            state.DestinationMap = target.Map;
            state.GoalMinDistance = FollowHoldDistance;
            state.GoalMaxDistance = FollowResumeDistance;
            state.GoalRadius = FollowHoldDistance;
            state.LastTargetPosition = target.Location;
            state.NextFollowStepUtc = DateTime.MinValue;
            state.Phase = AIGMSmartMovementPhase.Starting;
            StartTimer(actor);

            response = String.Format("Server-assisted smooth follow started for {0}.", state.TargetName);
            AIGMExecutionLog.Write("AIGM_SMART_MOVE_START actor={0} kind=FollowMobile target={1} distance={2} hold={3} resume={4} cadence=0.25 formation=True", Describe(actor), Describe(target), GetDistance(actor.Location, target.Location), state.GoalMinDistance, state.GoalMaxDistance);
            return true;
        }

        public static bool StartMoveToPoint(Mobile actor, Point3D destination, Map map, string label, int radius, Mobile requester, out string response)
        {
            response = null;

            if (!IsValidActor(actor, out response))
                return false;

            if (!AIGMOperationalControlService.CanOperate(actor, AIGMOperationalAction.Movement, AIGMOperationalControlService.GetEpoch(actor), false))
            {
                response = "Operational control rejected movement.";
                return false;
            }

            if (map == null || actor.Map == null || actor.Map != map)
            {
                response = "The destination must be on your current map.";
                return false;
            }

            Point3D normalized = NormalizeDestination(map, destination);

            if (!IsWithinBounds(map, normalized))
            {
                response = "That destination is outside the current map bounds.";
                return false;
            }

            AIGMSmartMovementState state = PrepareState(actor, requester, label);
            state.Kind = AIGMSmartMovementKind.MoveToPoint;
            state.Destination = normalized;
            state.DestinationMap = map;
            state.GoalRadius = Math.Max(0, radius);
            state.GoalMinDistance = 0;
            state.GoalMaxDistance = state.GoalRadius;
            state.Phase = AIGMSmartMovementPhase.Starting;
            StartTimer(actor);

            response = String.Format("Moving toward {0} ({1},{2},{3}).", String.IsNullOrWhiteSpace(label) ? "destination" : label, normalized.X, normalized.Y, normalized.Z);
            AIGMExecutionLog.Write("AIGM_SMART_MOVE_START actor={0} kind=MoveToPoint label=\"{1}\" destination={2}", Describe(actor), SafeLog(label), FormatPoint(normalized));
            return true;
        }

        public static bool StartRoute(Mobile actor, AIGMNavigationRoute route, int radius, Mobile requester, out string response)
        {
            response = null;

            if (!IsValidActor(actor, out response))
                return false;

            if (!AIGMOperationalControlService.CanOperate(actor, AIGMOperationalAction.Travel, AIGMOperationalControlService.GetEpoch(actor), false))
            {
                response = "Operational control rejected route movement.";
                return false;
            }

            if (route == null || route.Map == null || route.Waypoints == null || route.Waypoints.Count == 0)
            {
                response = "No usable route was found.";
                return false;
            }

            if (actor.Map == null || actor.Map != route.Map)
            {
                response = "The route must be on your current map.";
                return false;
            }

            AIGMSmartMovementState state = PrepareState(actor, requester, route.DestinationName);
            state.Kind = AIGMSmartMovementKind.RouteToPoint;
            state.FinalDestination = NormalizeDestination(route.Map, route.FinalDestination);
            state.FinalDestinationMap = route.Map;
            state.FinalLabel = route.DestinationName ?? String.Empty;
            state.GoalRadius = Math.Max(0, radius);
            state.GoalMinDistance = 0;
            state.GoalMaxDistance = state.GoalRadius;

            for (int i = 0; i < route.Waypoints.Count; i++)
            {
                state.RouteWaypoints.Add(NormalizeDestination(route.Map, route.Waypoints[i]));
                if (route.WaypointLabels != null && i < route.WaypointLabels.Count)
                    state.RouteWaypointLabels.Add(route.WaypointLabels[i] ?? "waypoint");
                else
                    state.RouteWaypointLabels.Add("waypoint");
            }

            state.CurrentWaypointIndex = 0;
            state.Destination = state.RouteWaypoints[0];
            state.DestinationMap = route.Map;
            state.Phase = AIGMSmartMovementPhase.Starting;
            StartTimer(actor);

            response = String.Format("Routing toward {0} via {1} waypoint{2}.", state.FinalLabel, state.RouteWaypoints.Count, state.RouteWaypoints.Count == 1 ? String.Empty : "s");
            AIGMExecutionLog.Write("AIGM_SMART_MOVE_ROUTE_START actor={0} destination=\"{1}\" waypoints={2} source={3}", Describe(actor), SafeLog(state.FinalLabel), state.RouteWaypoints.Count, SafeLog(route.SourceNodeId));
            AIGMExecutionLog.Write("AIGM_NAV_TARGET actor={0} kind=RouteToPoint current={1} waypoint=\"{2}\" waypointIndex={3}/{4} waypoint={5} final=\"{6}\" finalPoint={7} distanceToWaypoint={8} distanceToFinal={9}", Describe(actor), FormatPoint(actor.Location), SafeLog(FormatWaypoint(state)), state.CurrentWaypointIndex + 1, state.RouteWaypoints.Count, FormatPoint(state.Destination), SafeLog(state.FinalLabel), FormatPoint(state.FinalDestination), GetDistance(actor.Location, state.Destination), GetDistance(actor.Location, state.FinalDestination));
            return true;
        }
        public static bool Stop(Mobile actor, string reason, out string response)
        {
            response = null;

            if (actor == null)
            {
                response = "No movement actor was supplied.";
                return false;
            }

            AIGMSmartMovementState state = GetOrCreateState(actor);
            state.Phase = AIGMSmartMovementPhase.Cancelled;
            state.Kind = AIGMSmartMovementKind.None;
            state.LastStopReason = String.IsNullOrWhiteSpace(reason) ? "cancelled" : reason;
            state.LastResult = "cancelled";
            state.UpdatedUtc = DateTime.UtcNow;
            StopTimer(actor.Serial.Value);

            response = "Movement cancelled.";
            AIGMExecutionLog.Write("AIGM_SMART_MOVE_STOP actor={0} reason={1}", Describe(actor), SafeLog(state.LastStopReason));
            return true;
        }

        public static AIGMSmartMovementState GetState(Mobile actor)
        {
            return actor == null ? null : GetOrCreateState(actor);
        }

        public static string GetStatus(Mobile actor)
        {
            if (actor == null)
                return "No movement actor.";

            AIGMSmartMovementState state = GetOrCreateState(actor);
            return String.Format(
                "movement={0}; phase={1}; target={2}; waypoint={3}; destination={4}; final={5}; distance={6}; best={7}; stuck={8}; corrections={9}; cycles={10}; last={11}",
                state.Kind,
                state.Phase,
                String.IsNullOrWhiteSpace(state.TargetName) ? "none" : state.TargetName,
                FormatWaypoint(state),
                FormatPoint(state.Destination),
                FormatPoint(state.FinalDestination == Point3D.Zero ? state.Destination : state.FinalDestination),
                state.LastDistance,
                state.BestDistance == Int32.MaxValue ? -1 : state.BestDistance,
                state.StuckCounter,
                state.CorrectionAttempts,
                state.DetourCycles,
                String.IsNullOrWhiteSpace(state.LastResult) ? "none" : state.LastResult);
        }

        public static string Tick(Mobile actor)
        {
            string validation;
            if (!IsValidActor(actor, out validation))
                return Finish(actor, AIGMSmartMovementPhase.Invalid, validation);

            AIGMSmartMovementState state = GetOrCreateState(actor);
            state.UpdatedUtc = DateTime.UtcNow;

            if (state.Kind == AIGMSmartMovementKind.None)
                return Finish(actor, AIGMSmartMovementPhase.Idle, "inactive");

            AIGMOperationalAction action = state.Kind == AIGMSmartMovementKind.FollowMobile
                ? AIGMOperationalAction.Follow
                : (state.Kind == AIGMSmartMovementKind.RouteToPoint ? AIGMOperationalAction.Travel : AIGMOperationalAction.Movement);
            if (!AIGMOperationalControlService.CanOperate(actor, action, AIGMOperationalControlService.GetEpoch(actor), false))
                return Finish(actor, AIGMSmartMovementPhase.Cancelled, "operational_control_blocked");

            Mobile target = null;
            if (state.Kind == AIGMSmartMovementKind.FollowMobile)
            {
                target = World.FindMobile(state.TargetSerial);
                if (!IsValidTarget(target))
                    return Finish(actor, AIGMSmartMovementPhase.Invalid, "target_invalid");

                if (target.Map != actor.Map)
                    return Finish(actor, AIGMSmartMovementPhase.Invalid, "target_map_changed");

                state.Destination = BuildFollowDestination(actor, target, state);
                state.DestinationMap = target.Map;
                int formationDistance = GetDistance(actor.Location, state.Destination);
                state.LastDistance = formationDistance;

                if (formationDistance <= state.GoalMinDistance
                    || (state.Phase == AIGMSmartMovementPhase.HoldingDistance && formationDistance <= state.GoalMaxDistance))
                {
                    state.Phase = AIGMSmartMovementPhase.HoldingDistance;
                    state.LastResult = "smooth_follow_hold";
                    ResetProgressIfCloser(state, formationDistance);
                    return state.LastResult;
                }
            }
            else if (state.DestinationMap == null || state.DestinationMap != actor.Map)
            {
                return Finish(actor, AIGMSmartMovementPhase.Invalid, "destination_map_changed");
            }

            int distance = GetDistance(actor.Location, state.Destination);
            state.LastDistance = distance;

            if (state.Kind == AIGMSmartMovementKind.MoveToPoint || state.Kind == AIGMSmartMovementKind.RouteToPoint)
            {
                Point3D final = state.FinalDestination == Point3D.Zero ? state.Destination : state.FinalDestination;
                AIGMExecutionLog.Write("AIGM_NAV_TARGET actor={0} kind={1} phase={2} current={3} waypoint=\"{4}\" waypointIndex={5}/{6} waypoint={7} final=\"{8}\" finalPoint={9} distanceToWaypoint={10} distanceToFinal={11}", Describe(actor), state.Kind, state.Phase, FormatPoint(actor.Location), SafeLog(FormatWaypoint(state)), state.CurrentWaypointIndex + 1, state.RouteWaypoints.Count, FormatPoint(state.Destination), SafeLog(String.IsNullOrWhiteSpace(state.FinalLabel) ? state.Label : state.FinalLabel), FormatPoint(final), distance, GetDistance(actor.Location, final));
            }

            if ((state.Kind == AIGMSmartMovementKind.MoveToPoint || state.Kind == AIGMSmartMovementKind.RouteToPoint) && distance <= state.GoalRadius)
                return state.Kind == AIGMSmartMovementKind.RouteToPoint && AdvanceRouteWaypoint(state) ? "waypoint_arrived" : Finish(actor, AIGMSmartMovementPhase.Arrived, "arrived");

            string result;
            if (state.Kind == AIGMSmartMovementKind.FollowMobile)
                result = TickFollowStep(actor, target, state, distance);
            else if (state.Phase == AIGMSmartMovementPhase.Backtracking)
                result = TickBacktracking(actor, state);
            else if (state.Phase == AIGMSmartMovementPhase.BoundaryFollow)
                result = TickBoundaryFollow(actor, state);
            else if (state.Phase == AIGMSmartMovementPhase.LateralDetour)
                result = TickLateralDetour(actor, state);
            else if (state.Phase == AIGMSmartMovementPhase.ReacquireGoal)
                result = TickReacquireGoal(actor, state);
            else
                result = TickDirectToGoal(actor, state);

            return result;
        }

        private static string TickFollowStep(Mobile actor, Mobile target, AIGMSmartMovementState state, int beforeDistance)
        {
            Direction[] candidates = BuildCandidateDirections(actor, state.Destination, state);
            Direction movedDirection;
            Point3D after;
            int afterDistance;

            bool shouldRun = beforeDistance > state.GoalMaxDistance || GetDistance(actor.Location, target.Location) > state.GoalMaxDistance + 2;
            if (TryMoveUsingCandidates(actor, state, candidates, beforeDistance, state.Destination, false, out movedDirection, out after, out afterDistance, shouldRun))
            {
                state.Phase = AIGMSmartMovementPhase.Moving;
                state.LastResult = String.Format("smooth_follow_step {0} run={1} distance {2}->{3}", movedDirection, shouldRun, beforeDistance, afterDistance);
                AIGMExecutionLog.Write("AIGM_SMART_MOVE_TICK actor={0} phase={1} result=\"{2}\"", Describe(actor), state.Phase, SafeLog(state.LastResult));
                return state.LastResult;
            }

            state.StuckCounter++;
            state.CorrectionAttempts++;
            state.Phase = AIGMSmartMovementPhase.HoldingDistance;
            state.LastResult = "smooth_follow_blocked_single_step";
            AIGMExecutionLog.Write("AIGM_SMART_MOVE_FOLLOW_BLOCKED actor={0} target={1} distance={2} stuck={3}", Describe(actor), Describe(target), beforeDistance, state.StuckCounter);

            if (state.StuckCounter >= MaxStuckTicks)
            {
                state.StuckCounter = 0;
                state.CorrectionAttempts = 0;
                state.FailedTiles.Clear();
                state.Phase = AIGMSmartMovementPhase.ReacquireGoal;
                state.LastResult = "smooth_follow_recovering";
                return state.LastResult;
            }

            return state.LastResult;
        }

        private static string TickDirectToGoal(Mobile actor, AIGMSmartMovementState state)
        {
            int beforeDistance = GetDistance(actor.Location, state.Destination);
            Direction[] candidates = BuildCandidateDirections(actor, state.Destination, state);
            Direction movedDirection;
            Point3D after;
            int afterDistance;

            if (TryMoveUsingCandidates(actor, state, candidates, beforeDistance, state.Destination, true, out movedDirection, out after, out afterDistance))
            {
                LogNavigationProgress(actor, state, beforeDistance, afterDistance, after, movedDirection);

                if (afterDistance < beforeDistance || afterDistance < state.BestDistance)
                {
                    NoteProgressResumed(actor, state, beforeDistance, afterDistance);
                    state.Phase = AIGMSmartMovementPhase.Moving;
                    state.LastResult = String.Format("moved {0} distance {1}->{2}", movedDirection, beforeDistance, afterDistance);
                }
                else
                {
                    state.StuckCounter++;
                    state.CorrectionAttempts++;
                    state.LastBlockedDirection = movedDirection;
                    state.Phase = AIGMSmartMovementPhase.Moving;
                    state.LastResult = String.Format("non_improving_step {0} distance {1}->{2}", movedDirection, beforeDistance, afterDistance);
                }

                AIGMExecutionLog.Write("AIGM_SMART_MOVE_TICK actor={0} phase={1} result=\"{2}\"", Describe(actor), state.Phase, SafeLog(state.LastResult));

                if (ShouldEnterSoftBlocked(state))
                    EnterSoftBlocked(actor, state, beforeDistance, "no_progress");

                return state.LastResult;
            }

            state.StuckCounter++;
            state.CorrectionAttempts++;
            state.LastBlockedDirection = actor.GetDirectionTo(state.Destination) & Direction.Mask;
            EnterSoftBlocked(actor, state, beforeDistance, "no_candidate_progress");
            return state.LastResult;
        }

        private static string TickBacktracking(Mobile actor, AIGMSmartMovementState state)
        {
            int beforeDistance = GetDistance(actor.Location, state.Destination);
            Direction[] candidates = BuildBacktrackDirections(actor, state);
            Direction movedDirection;
            Point3D after;
            int afterDistance;

            if (TryMoveUsingCandidates(actor, state, candidates, beforeDistance, state.Destination, false, out movedDirection, out after, out afterDistance))
            {
                LogNavigationProgress(actor, state, beforeDistance, afterDistance, after, movedDirection);
                state.BacktrackStepsRemaining--;
                state.CorrectionAttempts++;
                state.LastResult = String.Format("backtrack {0} distance {1}->{2} remaining={3}", movedDirection, beforeDistance, afterDistance, Math.Max(0, state.BacktrackStepsRemaining));
                AIGMExecutionLog.Write("AIGM_SMART_MOVE_BACKTRACK actor={0} direction={1} distance={2}->{3} remaining={4} cycle={5}", Describe(actor), movedDirection, beforeDistance, afterDistance, Math.Max(0, state.BacktrackStepsRemaining), state.DetourCycles);

                if (state.BacktrackStepsRemaining <= 0)
                    EnterBoundaryFollow(actor, state, "backtrack_complete");

                return state.LastResult;
            }

            state.CorrectionAttempts++;
            EnterBoundaryFollow(actor, state, "backtrack_blocked");
            state.LastResult = "backtrack_blocked_try_boundary";
            AIGMExecutionLog.Write("AIGM_SMART_MOVE_BACKTRACK actor={0} result=blocked_try_boundary cycle={1}", Describe(actor), state.DetourCycles);
            return state.LastResult;
        }

        private static string TickBoundaryFollow(Mobile actor, AIGMSmartMovementState state)
        {
            int beforeDistance = GetDistance(actor.Location, state.Destination);

            if (state.BoundaryStepsTaken >= MinBoundaryFollowSteps
                && state.BoundaryStepsTaken >= state.BoundaryNextOpeningCheckStep
                && TryOpeningMove(actor, state, beforeDistance))
            {
                if (state.BoundaryOpeningTicks >= OpeningRequiredProgressTicks)
                {
                    state.LastResult = "wallfollow_reacquired";
                    AIGMExecutionLog.Write("AIGM_WALLFOLLOW_REACQUIRE actor={0} steps={1} openingTicks={2} distance={3} hand={4}", Describe(actor), state.BoundaryStepsTaken, state.BoundaryOpeningTicks, state.LastDistance, state.DetourRotationDirection);
                    ClearStuckState(actor, state, "wallfollow_reacquired");
                    return state.LastResult;
                }

                return state.LastResult;
            }

            Direction[] candidates = BuildBoundaryDirections(state);
            Direction movedDirection;
            Point3D after;
            int afterDistance;

            if (TryMoveUsingCandidates(actor, state, candidates, beforeDistance, state.Destination, false, out movedDirection, out after, out afterDistance))
            {
                LogNavigationProgress(actor, state, beforeDistance, afterDistance, after, movedDirection);
                state.BoundaryDirection = movedDirection;
                state.DetourDirection = movedDirection;
                state.BoundaryStepsTaken++;
                state.BoundaryStepsRemaining--;
                state.BoundaryOpeningTicks = 0;
                state.BoundaryBestDistance = Math.Min(state.BoundaryBestDistance, afterDistance);
                state.LastResult = String.Format("wallfollow {0} distance {1}->{2} steps={3}", movedDirection, beforeDistance, afterDistance, state.BoundaryStepsTaken);
                AIGMExecutionLog.Write("AIGM_WALLFOLLOW_STEP actor={0} direction={1} distance={2}->{3} steps={4}/{5} hand={6} startTile={7} current={8}", Describe(actor), movedDirection, beforeDistance, afterDistance, state.BoundaryStepsTaken, MaxBoundaryFollowSteps, state.DetourRotationDirection, SafeLog(state.BoundaryStartTile), FormatPoint(after));

                if (IsBoundaryLoop(actor, state))
                    return SwitchHandOrFinish(actor, state, "boundary_loop");

                if (state.BoundaryStepsTaken >= MaxBoundaryFollowSteps || state.BoundaryStepsRemaining <= 0)
                    return SwitchHandOrFinish(actor, state, "boundary_max_steps");

                return state.LastResult;
            }

            state.CorrectionAttempts++;
            return SwitchHandOrFinish(actor, state, "boundary_blocked");
        }

        private static string TickLateralDetour(Mobile actor, AIGMSmartMovementState state)
        {
            int beforeDistance = GetDistance(actor.Location, state.Destination);
            Direction[] candidates = BuildDetourDirections(state);
            Direction movedDirection;
            Point3D after;
            int afterDistance;

            if (TryMoveUsingCandidates(actor, state, candidates, beforeDistance, state.Destination, false, out movedDirection, out after, out afterDistance))
            {
                LogNavigationProgress(actor, state, beforeDistance, afterDistance, after, movedDirection);
                state.DetourStepsRemaining--;
                state.CorrectionAttempts++;
                state.LastResult = String.Format("detour {0} distance {1}->{2} remaining={3}", movedDirection, beforeDistance, afterDistance, Math.Max(0, state.DetourStepsRemaining));
                AIGMExecutionLog.Write("AIGM_SMART_MOVE_DETOUR actor={0} direction={1} distance={2}->{3} remaining={4} cycle={5}", Describe(actor), movedDirection, beforeDistance, afterDistance, Math.Max(0, state.DetourStepsRemaining), state.DetourCycles);

                if (state.DetourStepsRemaining <= 0)
                    EnterReacquire(actor, state);

                return state.LastResult;
            }

            state.CorrectionAttempts++;
            if (state.DetourCycles >= MaxDetourCycles || state.CorrectionAttempts >= MaxCorrectionAttempts)
                return FinishPathBlocked(actor, state, "detour_blocked");

            RotateDetourBias(state, "detour_blocked");
            EnterBacktracking(actor, state, "detour_blocked_retry");
            return state.LastResult;
        }

        private static string TickReacquireGoal(Mobile actor, AIGMSmartMovementState state)
        {
            int beforeDistance = GetDistance(actor.Location, state.Destination);
            Direction[] candidates = BuildCandidateDirections(actor, state.Destination, state);
            Direction movedDirection;
            Point3D after;
            int afterDistance;

            if (TryMoveUsingCandidates(actor, state, candidates, beforeDistance, state.Destination, true, out movedDirection, out after, out afterDistance))
            {
                LogNavigationProgress(actor, state, beforeDistance, afterDistance, after, movedDirection);
                state.ReacquireTicks++;
                if (afterDistance < beforeDistance || afterDistance < state.BestDistance)
                {
                    state.ProgressTicks++;
                    state.BestDistance = Math.Min(state.BestDistance, afterDistance);
                    state.LastResult = String.Format("reacquire_progress {0} distance {1}->{2}", movedDirection, beforeDistance, afterDistance);
                    AIGMExecutionLog.Write("AIGM_SMART_MOVE_REACQUIRE actor={0} direction={1} distance={2}->{3} progressTicks={4} cycle={5}", Describe(actor), movedDirection, beforeDistance, afterDistance, state.ProgressTicks, state.DetourCycles);

                    if (state.ProgressTicks >= ReacquireRequiredProgressTicks)
                    {
                        ClearStuckState(actor, state, "reacquired_goal");
                        return state.LastResult;
                    }
                }
                else
                {
                    state.StuckCounter++;
                    state.CorrectionAttempts++;
                    state.LastResult = String.Format("reacquire_no_progress {0} distance {1}->{2}", movedDirection, beforeDistance, afterDistance);
                    AIGMExecutionLog.Write("AIGM_SMART_MOVE_REACQUIRE actor={0} direction={1} distance={2}->{3} progressTicks={4} cycle={5}", Describe(actor), movedDirection, beforeDistance, afterDistance, state.ProgressTicks, state.DetourCycles);
                }

                if (state.ReacquireTicks >= ReacquireRetryTicks)
                    return RetryOrFinish(actor, state, "reacquire_no_progress");

                return state.LastResult;
            }

            state.StuckCounter++;
            state.CorrectionAttempts++;
            return RetryOrFinish(actor, state, "reacquire_blocked");
        }

        private static bool TryMoveUsingCandidates(Mobile actor, AIGMSmartMovementState state, Direction[] candidates, int beforeDistance, Point3D distanceTarget, bool avoidFailedAfterDirect, out Direction movedDirection, out Point3D after, out int afterDistance, bool run = false)
        {
            movedDirection = Direction.North;
            after = actor != null ? actor.Location : Point3D.Zero;
            afterDistance = beforeDistance;

            for (int i = 0; candidates != null && i < candidates.Length; i++)
            {
                Direction direction = candidates[i] & Direction.Mask;
                Point3D candidatePoint = OffsetPoint(actor.Location, direction);
                string tileKey = TileKey(candidatePoint);

                if (avoidFailedAfterDirect && IsRecentlyFailed(state, tileKey) && i > 1)
                    continue;

                int newZ;
                if (!actor.CheckMovement(direction, out newZ))
                {
                    MarkFailed(state, tileKey);
                    state.LastBlockedDirection = direction;
                    state.LastBlockedTile = tileKey;
                    AIGMExecutionLog.Write("AIGM_SMART_MOVE_BLOCKED_DETAIL actor={0} phase={1} attemptedDirection={2} attemptedTile={3} reason=check_movement_failed failedCount={4}", Describe(actor), state != null ? state.Phase.ToString() : "unknown", direction, tileKey, FailedCount(state, tileKey));
                    continue;
                }

                if (!TryMoveOneStep(actor, direction, run, out after))
                {
                    MarkFailed(state, tileKey);
                    state.LastBlockedDirection = direction;
                    state.LastBlockedTile = tileKey;
                    AIGMExecutionLog.Write("AIGM_SMART_MOVE_BLOCKED_DETAIL actor={0} phase={1} attemptedDirection={2} attemptedTile={3} reason=move_failed failedCount={4}", Describe(actor), state != null ? state.Phase.ToString() : "unknown", direction, tileKey, FailedCount(state, tileKey));
                    continue;
                }

                movedDirection = direction;
                state.LastDirection = direction;
                state.LastPosition = after;
                RememberTile(state, after);
                afterDistance = GetDistance(after, distanceTarget);
                state.LastDistance = afterDistance;
                return true;
            }

            return false;
        }

        private static void LogNavigationProgress(Mobile actor, AIGMSmartMovementState state, int beforeDistance, int afterDistance, Point3D after, Direction direction)
        {
            if (actor == null || state == null)
                return;

            if (state.Kind != AIGMSmartMovementKind.MoveToPoint && state.Kind != AIGMSmartMovementKind.RouteToPoint)
                return;

            Point3D final = state.FinalDestination == Point3D.Zero ? state.Destination : state.FinalDestination;
            AIGMExecutionLog.Write("AIGM_NAV_PROGRESS actor={0} kind={1} phase={2} direction={3} position={4} waypoint=\"{5}\" waypoint={6} final=\"{7}\" finalPoint={8} distanceToWaypoint={9}->{10} distanceToFinal={11}", Describe(actor), state.Kind, state.Phase, direction & Direction.Mask, FormatPoint(after), SafeLog(FormatWaypoint(state)), FormatPoint(state.Destination), SafeLog(String.IsNullOrWhiteSpace(state.FinalLabel) ? state.Label : state.FinalLabel), FormatPoint(final), beforeDistance, afterDistance, GetDistance(after, final));
        }

        private static bool ShouldEnterSoftBlocked(AIGMSmartMovementState state)
        {
            if (state == null)
                return false;

            string currentTile = LastRecentTile(state);
            return state.StuckCounter >= 3
                || (!String.IsNullOrWhiteSpace(currentTile) && CountRecentTile(state, currentTile) >= 3)
                || state.CorrectionAttempts >= MaxCorrectionAttempts;
        }

        private static void EnterSoftBlocked(Mobile actor, AIGMSmartMovementState state, int distance, string reason)
        {
            if (state == null)
                return;

            state.Phase = AIGMSmartMovementPhase.SoftBlocked;
            state.LastResult = "soft_blocked:" + reason;
            AIGMExecutionLog.Write("AIGM_SMART_MOVE_SOFT_BLOCKED actor={0} distance={1} stuck={2} corrections={3} cycle={4} blockedDirection={5} blockedTile={6} reason={7}", Describe(actor), distance, state.StuckCounter, state.CorrectionAttempts, state.DetourCycles, state.LastBlockedDirection & Direction.Mask, SafeLog(state.LastBlockedTile), SafeLog(reason));

            if (state.DetourCycles >= MaxDetourCycles || state.CorrectionAttempts >= MaxCorrectionAttempts)
            {
                FinishPathBlocked(actor, state, reason);
                return;
            }

            EnterBacktracking(actor, state, reason);
        }

        private static void EnterBacktracking(Mobile actor, AIGMSmartMovementState state, string reason)
        {
            if (state == null)
                return;

            state.DetourCycles++;
            state.Phase = AIGMSmartMovementPhase.Backtracking;
            state.BacktrackStepsRemaining = BacktrackStepCount;
            state.BacktrackDirection = OppositeDirection(state.LastBlockedDirection);
            state.DetourStepsRemaining = 0;
            state.ReacquireTicks = 0;
            state.ProgressTicks = 0;
            state.LastResult = "backtracking:" + reason;
            AIGMExecutionLog.Write("AIGM_SMART_MOVE_BACKTRACK actor={0} direction={1} steps={2} cycle={3} blockedDirection={4} blockedTile={5} reason={6}", Describe(actor), state.BacktrackDirection, state.BacktrackStepsRemaining, state.DetourCycles, state.LastBlockedDirection & Direction.Mask, SafeLog(state.LastBlockedTile), SafeLog(reason));
        }

        private static void EnterLateralDetour(Mobile actor, AIGMSmartMovementState state)
        {
            if (state == null)
                return;

            Direction direct = actor.GetDirectionTo(state.Destination) & Direction.Mask;
            if (state.DetourCycles <= 1 && state.DetourRotationStep == 0)
            {
                state.DetourRotationDirection = ChooseDetourRotation(actor, state, direct);
                state.PreferRightDetour = state.DetourRotationDirection > 0;
            }

            state.DetourDirection = RotateDirection(direct, state.DetourRotationDirection * (2 + state.DetourRotationStep));
            state.DetourStepsRemaining = Math.Min(MaxDetourStepCount, BaseDetourStepCount + Math.Max(0, state.DetourCycles - 1));
            state.Phase = AIGMSmartMovementPhase.LateralDetour;
            state.LastResult = "lateral_detour";
            AIGMExecutionLog.Write("AIGM_SMART_MOVE_DETOUR actor={0} direction={1} steps={2} cycle={3} rotationDirection={4} rotationStep={5} direct={6}", Describe(actor), state.DetourDirection, state.DetourStepsRemaining, state.DetourCycles, state.DetourRotationDirection, state.DetourRotationStep, direct);
        }

        private static void EnterBoundaryFollow(Mobile actor, AIGMSmartMovementState state, string reason)
        {
            if (actor == null || state == null)
                return;

            Direction direct = actor.GetDirectionTo(state.Destination) & Direction.Mask;
            if (state.BoundaryStepsTaken <= 0 && state.BoundaryHandSwitches == 0)
            {
                state.DetourRotationDirection = ChooseBoundaryHand(actor, state, direct);
                state.PreferRightDetour = state.DetourRotationDirection > 0;
            }

            state.BoundaryDirection = RotateDirection(direct, state.DetourRotationDirection);
            state.DetourDirection = state.BoundaryDirection;
            state.DetourStepsRemaining = MaxBoundaryFollowSteps;
            state.BoundaryStepsTaken = 0;
            state.BoundaryStepsRemaining = MaxBoundaryFollowSteps;
            state.BoundaryOpeningTicks = 0;
            state.BoundaryNextOpeningCheckStep = 0;
            state.BoundaryStartTile = TileKey(actor.Location);
            state.BoundaryStartDistance = GetDistance(actor.Location, state.Destination);
            state.BoundaryBestDistance = state.BoundaryStartDistance;
            state.Phase = AIGMSmartMovementPhase.BoundaryFollow;
            state.LastResult = "boundary_follow:" + reason;
            AIGMExecutionLog.Write("AIGM_WALLFOLLOW_START actor={0} boundaryDirection={1} hand={2} minSteps={3} maxSteps={4} cycle={5} blockedDirection={6} blockedTile={7} direct={8} start={9} distance={10} reason={11}", Describe(actor), state.BoundaryDirection, state.DetourRotationDirection, MinBoundaryFollowSteps, MaxBoundaryFollowSteps, state.DetourCycles, state.LastBlockedDirection & Direction.Mask, SafeLog(state.LastBlockedTile), direct, FormatPoint(actor.Location), state.BoundaryStartDistance, SafeLog(reason));
        }

        private static void EnterReacquire(Mobile actor, AIGMSmartMovementState state)
        {
            if (state == null)
                return;

            state.Phase = AIGMSmartMovementPhase.ReacquireGoal;
            state.ReacquireTicks = 0;
            state.ProgressTicks = 0;
            state.LastResult = "reacquire_goal";
            AIGMExecutionLog.Write("AIGM_SMART_MOVE_REACQUIRE actor={0} cycle={1} destination={2}", Describe(actor), state.DetourCycles, FormatPoint(state.Destination));
        }

        private static string RetryOrFinish(Mobile actor, AIGMSmartMovementState state, string reason)
        {
            if (state.DetourCycles >= MaxDetourCycles || state.CorrectionAttempts >= MaxCorrectionAttempts || state.StuckCounter >= MaxStuckTicks)
                return FinishPathBlocked(actor, state, reason);

            RotateDetourBias(state, reason);
            EnterBacktracking(actor, state, reason);
            return state.LastResult;
        }

        private static string FinishPathBlocked(Mobile actor, AIGMSmartMovementState state, string reason)
        {
            if (state != null)
            {
                state.Phase = AIGMSmartMovementPhase.PathBlockedFinal;
                state.LastResult = "path_blocked";
                AIGMExecutionLog.Write("AIGM_SMART_MOVE_PATH_BLOCKED actor={0} reason={1} stuck={2} corrections={3} cycles={4} blockedDirection={5} blockedTile={6} rotationDirection={7} rotationStep={8}", Describe(actor), SafeLog(reason), state.StuckCounter, state.CorrectionAttempts, state.DetourCycles, state.LastBlockedDirection & Direction.Mask, SafeLog(state.LastBlockedTile), state.DetourRotationDirection, state.DetourRotationStep);
            }

            return Finish(actor, AIGMSmartMovementPhase.PathBlockedFinal, "path_blocked");
        }

        private static void NoteProgressResumed(Mobile actor, AIGMSmartMovementState state, int beforeDistance, int afterDistance)
        {
            bool wasRecovering = state.Phase == AIGMSmartMovementPhase.SoftBlocked
                || state.Phase == AIGMSmartMovementPhase.Backtracking
                || state.Phase == AIGMSmartMovementPhase.BoundaryFollow
                || state.Phase == AIGMSmartMovementPhase.LateralDetour
                || state.Phase == AIGMSmartMovementPhase.ReacquireGoal;

            state.StuckCounter = 0;
            state.CorrectionAttempts = 0;
            state.ReacquireTicks = 0;
            state.ProgressTicks++;
            state.BestDistance = Math.Min(state.BestDistance, afterDistance);

            if (wasRecovering)
                AIGMExecutionLog.Write("AIGM_SMART_MOVE_PROGRESS_RESUMED actor={0} distance={1}->{2}", Describe(actor), beforeDistance, afterDistance);
        }

        private static void ClearStuckState(Mobile actor, AIGMSmartMovementState state, string reason)
        {
            state.StuckCounter = 0;
            state.CorrectionAttempts = 0;
            state.DetourCycles = 0;
            state.DetourRotationStep = 0;
            state.BacktrackStepsRemaining = 0;
            state.DetourStepsRemaining = 0;
            state.BoundaryStepsTaken = 0;
            state.BoundaryStepsRemaining = 0;
            state.BoundaryOpeningTicks = 0;
            state.BoundaryHandSwitches = 0;
            state.BoundaryNextOpeningCheckStep = 0;
            state.BoundaryStartTile = String.Empty;
            state.BoundaryStartDistance = Int32.MaxValue;
            state.BoundaryBestDistance = Int32.MaxValue;
            state.ReacquireTicks = 0;
            state.ProgressTicks = 0;
            state.Phase = AIGMSmartMovementPhase.Moving;
            state.LastResult = reason;
            AIGMExecutionLog.Write("AIGM_SMART_MOVE_PROGRESS_RESUMED actor={0} reason={1} distance={2}", Describe(actor), SafeLog(reason), state.LastDistance);
        }

        private static Direction[] BuildBacktrackDirections(Mobile actor, AIGMSmartMovementState state)
        {
            List<Direction> directions = new List<Direction>();
            AddUnique(directions, state.BacktrackDirection);

            Point3D[] recent = state.RecentPositions.ToArray();
            for (int i = recent.Length - 4; i >= 0 && directions.Count < 3; i--)
            {
                if (recent[i] != actor.Location)
                    AddUnique(directions, actor.GetDirectionTo(recent[i]) & Direction.Mask);
            }

            AddUnique(directions, RotateDirection(state.BacktrackDirection, state.DetourRotationDirection));
            AddUnique(directions, RotateDirection(state.BacktrackDirection, state.DetourRotationDirection * 2));
            return directions.ToArray();
        }

        private static Direction[] BuildDetourDirections(AIGMSmartMovementState state)
        {
            List<Direction> directions = new List<Direction>();
            AddUnique(directions, state.DetourDirection);
            AddUnique(directions, RotateDirection(state.DetourDirection, state.DetourRotationDirection));
            AddUnique(directions, RotateDirection(state.DetourDirection, state.DetourRotationDirection * 2));
            AddUnique(directions, RotateDirection(state.DetourDirection, state.DetourRotationDirection * 3));
            return directions.ToArray();
        }

        private static Direction[] BuildBoundaryDirections(AIGMSmartMovementState state)
        {
            List<Direction> directions = new List<Direction>();
            AddUnique(directions, state.BoundaryDirection);
            AddUnique(directions, RotateDirection(state.BoundaryDirection, state.DetourRotationDirection));
            AddUnique(directions, RotateDirection(state.BoundaryDirection, state.DetourRotationDirection * 2));
            AddUnique(directions, RotateDirection(state.BoundaryDirection, -state.DetourRotationDirection));
            AddUnique(directions, RotateDirection(state.BoundaryDirection, state.DetourRotationDirection * 3));
            return directions.ToArray();
        }

        private static bool TryOpeningMove(Mobile actor, AIGMSmartMovementState state, int beforeDistance)
        {
            if (actor == null || state == null)
                return false;

            Direction direct = actor.GetDirectionTo(state.Destination) & Direction.Mask;
            Direction[] candidates = new Direction[]
            {
                direct,
                RotateDirection(direct, state.DetourRotationDirection),
                RotateDirection(direct, -state.DetourRotationDirection)
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                Direction direction = candidates[i] & Direction.Mask;
                int newZ;
                if (!actor.CheckMovement(direction, out newZ))
                    continue;

                Point3D candidate = OffsetPoint(actor.Location, direction);
                candidate.Z = newZ;
                int candidateDistance = GetDistance(candidate, state.Destination);
                AIGMExecutionLog.Write("AIGM_WALLFOLLOW_OPENING_CHECK actor={0} direction={1} candidate={2} distance={3}->{4} steps={5} hand={6}", Describe(actor), direction, FormatPoint(candidate), beforeDistance, candidateDistance, state.BoundaryStepsTaken, state.DetourRotationDirection);

                if (candidateDistance >= beforeDistance)
                    continue;

                Direction movedDirection;
                Point3D after;
                int afterDistance;
                if (TryMoveUsingCandidates(actor, state, new Direction[] { direction }, beforeDistance, state.Destination, false, out movedDirection, out after, out afterDistance))
                {
                    LogNavigationProgress(actor, state, beforeDistance, afterDistance, after, movedDirection);
                    state.BoundaryStepsTaken++;
                    state.BoundaryStepsRemaining--;
                    state.BoundaryOpeningTicks++;
                    state.BoundaryBestDistance = Math.Min(state.BoundaryBestDistance, afterDistance);
                    state.LastResult = String.Format("wallfollow_opening {0} distance {1}->{2} openingTicks={3}", movedDirection, beforeDistance, afterDistance, state.BoundaryOpeningTicks);
                    AIGMExecutionLog.Write("AIGM_WALLFOLLOW_OPENING_CHECK actor={0} result=moved direction={1} distance={2}->{3} openingTicks={4}/{5} steps={6} hand={7}", Describe(actor), movedDirection, beforeDistance, afterDistance, state.BoundaryOpeningTicks, OpeningRequiredProgressTicks, state.BoundaryStepsTaken, state.DetourRotationDirection);
                    return true;
                }
            }

            if (state.BoundaryOpeningTicks > 0)
                state.BoundaryNextOpeningCheckStep = state.BoundaryStepsTaken + OpeningProbeRetryDelaySteps;

            state.BoundaryOpeningTicks = 0;
            AIGMExecutionLog.Write("AIGM_WALLFOLLOW_OPENING_CHECK actor={0} result=closed distance={1} steps={2} hand={3}", Describe(actor), beforeDistance, state.BoundaryStepsTaken, state.DetourRotationDirection);
            return false;
        }

        private static bool IsBoundaryLoop(Mobile actor, AIGMSmartMovementState state)
        {
            if (actor == null || state == null || state.BoundaryStepsTaken < MinBoundaryFollowSteps)
                return false;

            bool nearStart = false;
            Point3D start;
            if (TryParseTileKey(state.BoundaryStartTile, out start))
                nearStart = GetDistance(actor.Location, start) <= BoundaryLoopNearStartDistance
                    && state.BoundaryBestDistance >= state.BoundaryStartDistance - 1;

            string current = TileKey(actor.Location);
            bool repeatedCluster = state.BoundaryStepsTaken > MinBoundaryFollowSteps + 4 && CountRecentTile(state, current) >= 3;
            if (nearStart || repeatedCluster)
                AIGMExecutionLog.Write("AIGM_WALLFOLLOW_LOOP actor={0} nearStart={1} repeatedCluster={2} current={3} startTile={4} steps={5} best={6} startDistance={7} hand={8}", Describe(actor), nearStart, repeatedCluster, FormatPoint(actor.Location), SafeLog(state.BoundaryStartTile), state.BoundaryStepsTaken, state.BoundaryBestDistance, state.BoundaryStartDistance, state.DetourRotationDirection);

            return nearStart || repeatedCluster;
        }

        private static string SwitchHandOrFinish(Mobile actor, AIGMSmartMovementState state, string reason)
        {
            if (state == null)
                return "path_blocked";

            if (state.BoundaryHandSwitches < MaxBoundaryHandSwitches)
            {
                state.BoundaryHandSwitches++;
                state.DetourRotationDirection = -state.DetourRotationDirection;
                state.DetourRotationStep = 0;
                state.BoundaryOpeningTicks = 0;
                state.BoundaryNextOpeningCheckStep = 0;
                state.LastResult = "wallfollow_switch_hand:" + reason;
                AIGMExecutionLog.Write("AIGM_WALLFOLLOW_SWITCH_HAND actor={0} reason={1} switches={2}/{3} newHand={4} steps={5} current={6}", Describe(actor), SafeLog(reason), state.BoundaryHandSwitches, MaxBoundaryHandSwitches, state.DetourRotationDirection, state.BoundaryStepsTaken, actor != null ? FormatPoint(actor.Location) : "missing");
                EnterBacktracking(actor, state, reason);
                return state.LastResult;
            }

            AIGMExecutionLog.Write("AIGM_WALLFOLLOW_BLOCKED_FINAL actor={0} reason={1} steps={2} hand={3} switches={4} startTile={5} current={6}", Describe(actor), SafeLog(reason), state.BoundaryStepsTaken, state.DetourRotationDirection, state.BoundaryHandSwitches, SafeLog(state.BoundaryStartTile), actor != null ? FormatPoint(actor.Location) : "missing");
            return FinishPathBlocked(actor, state, reason);
        }

        private static int ChooseBoundaryHand(Mobile actor, AIGMSmartMovementState state, Direction direct)
        {
            if (actor == null || state == null)
                return 1;

            Direction clockwise = RotateDirection(direct, 1);
            Direction counterClockwise = RotateDirection(direct, -1);
            int clockwiseScore = ScoreBoundaryDirection(actor, state, clockwise);
            int counterScore = ScoreBoundaryDirection(actor, state, counterClockwise);
            return clockwiseScore <= counterScore ? 1 : -1;
        }

        private static int ScoreBoundaryDirection(Mobile actor, AIGMSmartMovementState state, Direction direction)
        {
            Point3D point = OffsetPoint(actor.Location, direction);
            int score = GetDistance(point, state.Destination) + FailedPenalty(state, point);
            int newZ;
            if (!actor.CheckMovement(direction, out newZ))
                score += 20;
            return score;
        }

        private static int ChooseDetourRotation(Mobile actor, AIGMSmartMovementState state, Direction direct)
        {
            if (actor == null || state == null)
                return 1;

            Direction right = RotateDirection(direct, 2);
            Direction left = RotateDirection(direct, -2);
            Point3D rightPoint = OffsetPoint(actor.Location, right);
            Point3D leftPoint = OffsetPoint(actor.Location, left);
            int rightScore = GetDistance(rightPoint, state.Destination) + FailedPenalty(state, rightPoint);
            int leftScore = GetDistance(leftPoint, state.Destination) + FailedPenalty(state, leftPoint);
            return rightScore <= leftScore ? 1 : -1;
        }

        private static void RotateDetourBias(AIGMSmartMovementState state, string reason)
        {
            if (state == null)
                return;

            state.DetourRotationStep = Math.Min(MaxDetourRotationStep, state.DetourRotationStep + 1);
            state.LastResult = "rotate_bias:" + reason;
            AIGMExecutionLog.Write("AIGM_SMART_MOVE_ROTATE_BIAS actor={0} rotationDirection={1} rotationStep={2} reason={3}", state.ActorSerial, state.DetourRotationDirection, state.DetourRotationStep, SafeLog(reason));
        }

        private static Direction RotateDirection(Direction direction, int offset)
        {
            return (Direction)(((int)(direction & Direction.Mask) + offset + 8) & 0x7);
        }

        private static Direction OppositeDirection(Direction direction)
        {
            return RotateDirection(direction, 4);
        }

        private static bool AdvanceRouteWaypoint(AIGMSmartMovementState state)
        {
            if (state == null || state.RouteWaypoints == null)
                return false;

            if (state.CurrentWaypointIndex + 1 >= state.RouteWaypoints.Count)
                return false;

            state.CurrentWaypointIndex++;
            state.Destination = state.RouteWaypoints[state.CurrentWaypointIndex];
            state.LastDistance = -1;
            state.BestDistance = Int32.MaxValue;
            state.StuckCounter = 0;
            state.CorrectionAttempts = 0;
            state.DetourCycles = 0;
            state.BacktrackStepsRemaining = 0;
            state.DetourStepsRemaining = 0;
            state.BoundaryStepsTaken = 0;
            state.BoundaryStepsRemaining = 0;
            state.BoundaryOpeningTicks = 0;
            state.BoundaryHandSwitches = 0;
            state.BoundaryNextOpeningCheckStep = 0;
            state.BoundaryStartTile = String.Empty;
            state.BoundaryStartDistance = Int32.MaxValue;
            state.BoundaryBestDistance = Int32.MaxValue;
            state.ReacquireTicks = 0;
            state.ProgressTicks = 0;
            state.FailedTiles.Clear();
            state.RecentTiles.Clear();
            state.RecentPositions.Clear();
            state.Phase = AIGMSmartMovementPhase.Moving;
            state.LastResult = "advanced_waypoint";
            AIGMExecutionLog.Write("AIGM_SMART_MOVE_WAYPOINT actor={0} waypoint={1}/{2} label=\"{3}\" destination={4}", state.ActorSerial, state.CurrentWaypointIndex + 1, state.RouteWaypoints.Count, SafeLog(CurrentWaypointLabel(state)), FormatPoint(state.Destination));
            return true;
        }
        private static AIGMSmartMovementState PrepareState(Mobile actor, Mobile requester, string label)
        {
            AIGMSmartMovementState state = GetOrCreateState(actor);
            state.Reset();
            state.ActorSerial = actor.Serial.Value;
            state.RequesterSerial = requester != null ? requester.Serial.Value : 0;
            state.Label = label ?? String.Empty;
            state.LastPosition = actor.Location;
            state.LastDistance = 0;
            state.BestDistance = Int32.MaxValue;
            state.StartedUtc = DateTime.UtcNow;
            state.UpdatedUtc = state.StartedUtc;
            return state;
        }

        private static AIGMSmartMovementState GetOrCreateState(Mobile actor)
        {
            AIGMSmartMovementState state;
            int serial = actor != null ? actor.Serial.Value : 0;
            if (!States.TryGetValue(serial, out state))
            {
                state = new AIGMSmartMovementState();
                state.ActorSerial = serial;
                States[serial] = state;
            }

            return state;
        }

        private static void StartTimer(Mobile actor)
        {
            if (actor == null)
                return;

            StopTimer(actor.Serial.Value);
            SmartMovementTimer timer = new SmartMovementTimer(actor.Serial, AIGMOperationalControlService.GetEpoch(actor));
            Timers[actor.Serial.Value] = timer;
            timer.Start();
        }

        private static void StopTimer(int serial)
        {
            SmartMovementTimer timer;
            if (Timers.TryGetValue(serial, out timer) && timer != null)
                timer.Stop();
            Timers.Remove(serial);
        }

        private static string Finish(Mobile actor, AIGMSmartMovementPhase phase, string reason)
        {
            if (actor == null)
                return reason ?? "invalid_actor";

            AIGMSmartMovementState state = GetOrCreateState(actor);
            state.Phase = phase;
            state.Kind = AIGMSmartMovementKind.None;
            state.LastStopReason = reason ?? String.Empty;
            state.LastResult = reason ?? phase.ToString();
            state.UpdatedUtc = DateTime.UtcNow;
            StopTimer(actor.Serial.Value);
            AIGMExecutionLog.Write("AIGM_SMART_MOVE_FINISH actor={0} phase={1} reason={2}", Describe(actor), phase, SafeLog(reason));
            return state.LastResult;
        }

        private static bool IsValidActor(Mobile actor, out string reason)
        {
            reason = null;

            if (actor == null || actor.Deleted)
                reason = "actor_invalid";
            else if (!actor.Alive || actor.IsDeadBondedPet)
                reason = "actor_dead";
            else if (actor.Map == null || actor.Map == Map.Internal)
                reason = "actor_map_invalid";
            else if (actor.Frozen)
                reason = "actor_frozen";
            else if (actor.Paralyzed)
                reason = "actor_paralyzed";
            else if (actor.CantWalk)
                reason = "actor_cannot_walk";

            return reason == null;
        }

        private static bool IsValidTarget(Mobile target)
        {
            return target != null && !target.Deleted && target.Alive && !target.IsDeadBondedPet && target.Map != null && target.Map != Map.Internal;
        }

        private static Direction[] BuildCandidateDirections(Mobile actor, Point3D destination, AIGMSmartMovementState state)
        {
            Direction direct = actor.GetDirectionTo(destination) & Direction.Mask;
            List<Direction> directions = new List<Direction>();
            AddUnique(directions, direct);
            AddUnique(directions, (Direction)(((int)direct + 1) & 0x7));
            AddUnique(directions, (Direction)(((int)direct + 7) & 0x7));
            AddUnique(directions, (Direction)(((int)direct + 2) & 0x7));
            AddUnique(directions, (Direction)(((int)direct + 6) & 0x7));
            AddUnique(directions, (Direction)(((int)direct + 3) & 0x7));
            AddUnique(directions, (Direction)(((int)direct + 5) & 0x7));
            AddUnique(directions, (Direction)(((int)direct + 4) & 0x7));

            directions.Sort(delegate(Direction left, Direction right)
            {
                Point3D lp = OffsetPoint(actor.Location, left);
                Point3D rp = OffsetPoint(actor.Location, right);
                int ls = GetDistance(lp, destination) + FailedPenalty(state, lp);
                int rs = GetDistance(rp, destination) + FailedPenalty(state, rp);
                return ls.CompareTo(rs);
            });

            return directions.ToArray();
        }

        private static void AddUnique(List<Direction> directions, Direction direction)
        {
            Direction masked = direction & Direction.Mask;
            for (int i = 0; i < directions.Count; i++)
            {
                if ((directions[i] & Direction.Mask) == masked)
                    return;
            }

            directions.Add(masked);
        }

        private static bool TryMoveOneStep(Mobile actor, Direction direction, bool run, out Point3D after)
        {
            after = actor != null ? actor.Location : Point3D.Zero;
            if (actor == null)
                return false;

            Direction masked = direction & Direction.Mask;
            if ((actor.Direction & Direction.Mask) != masked)
                actor.Direction = masked;

            Point3D before = actor.Location;
            Direction moveDirection = run ? (masked | Direction.Running) : masked;
            bool result = actor.Move(moveDirection);
            after = actor.Location;
            return result && after != before;
        }

        private static Point3D BuildFollowDestination(Mobile actor, Mobile target, AIGMSmartMovementState state)
        {
            if (target == null || state == null)
                return actor != null ? actor.Location : Point3D.Zero;

            Point3D targetPoint = target.Location;
            Point3D previous = state.LastTargetPosition;
            state.LastTargetPosition = targetPoint;

            int dx = Sign(targetPoint.X - previous.X);
            int dy = Sign(targetPoint.Y - previous.Y);
            if (dx == 0 && dy == 0)
                DirectionToDelta(target.Direction & Direction.Mask, out dx, out dy);

            if (target.Map == null || (dx == 0 && dy == 0))
                return targetPoint;

            int lateral = GetFormationLateralSlot(actor);
            int rearX = -dx;
            int rearY = -dy;
            int lateralX = -dy;
            int lateralY = dx;
            Point3D desired = new Point3D(
                targetPoint.X + (rearX * FollowTrailingDistance) + (lateralX * lateral),
                targetPoint.Y + (rearY * FollowTrailingDistance) + (lateralY * lateral),
                targetPoint.Z);

            Point3D fit;
            if (TryNormalizeFit(target.Map, desired, out fit))
                return fit;

            Point3D[] alternatives =
            {
                new Point3D(targetPoint.X + (rearX * FollowTrailingDistance), targetPoint.Y + (rearY * FollowTrailingDistance), targetPoint.Z),
                new Point3D(targetPoint.X + (rearX * (FollowTrailingDistance + 1)) + (lateralX * lateral), targetPoint.Y + (rearY * (FollowTrailingDistance + 1)) + (lateralY * lateral), targetPoint.Z),
                new Point3D(targetPoint.X + (rearX * FollowTrailingDistance) - (lateralX * lateral), targetPoint.Y + (rearY * FollowTrailingDistance) - (lateralY * lateral), targetPoint.Z)
            };

            for (int i = 0; i < alternatives.Length; i++)
            {
                if (TryNormalizeFit(target.Map, alternatives[i], out fit))
                    return fit;
            }

            return targetPoint;
        }

        private static int GetFormationLateralSlot(Mobile actor)
        {
            IAIGMCompanionActor companion = actor as IAIGMCompanionActor;
            string id = companion != null ? companion.CompanionId : String.Empty;
            if (String.Equals(id, "dakeyras", StringComparison.OrdinalIgnoreCase))
                return -1;
            if (String.Equals(id, "dardalion", StringComparison.OrdinalIgnoreCase))
                return 1;
            if (String.Equals(id, "danyal", StringComparison.OrdinalIgnoreCase))
                return 0;

            int serial = actor != null ? Math.Abs(actor.Serial.Value) : 0;
            return (serial % 3) - 1;
        }

        private static bool TryNormalizeFit(Map map, Point3D point, out Point3D fit)
        {
            fit = point;
            if (!IsWithinBounds(map, point))
                return false;

            if (map.CanFit(point.X, point.Y, point.Z, 16, false, false))
                return true;

            int z = map.GetAverageZ(point.X, point.Y);
            fit = new Point3D(point.X, point.Y, z);
            return map.CanFit(fit.X, fit.Y, fit.Z, 16, false, false);
        }

        private static void DirectionToDelta(Direction direction, out int dx, out int dy)
        {
            dx = 0;
            dy = 0;
            int x = 0;
            int y = 0;
            Server.Movement.Movement.Offset(direction & Direction.Mask, ref x, ref y);
            dx = Sign(x);
            dy = Sign(y);
        }

        private static TimeSpan GetFollowCadence(int distanceAfterStep)
        {
            if (distanceAfterStep > 12)
                return TimeSpan.FromSeconds(0.85);

            if (distanceAfterStep > 8)
                return TimeSpan.FromSeconds(0.95);

            return TimeSpan.FromSeconds(1.15);
        }

        private static int Sign(int value)
        {
            if (value > 0)
                return 1;
            if (value < 0)
                return -1;
            return 0;
        }

        private static void ResetProgressIfCloser(AIGMSmartMovementState state, int distance)
        {
            if (distance < state.BestDistance)
            {
                state.BestDistance = distance;
                state.StuckCounter = 0;
                state.CorrectionAttempts = 0;
            }
        }

        private static Point3D OffsetPoint(Point3D point, Direction direction)
        {
            int x = point.X;
            int y = point.Y;
            Server.Movement.Movement.Offset(direction, ref x, ref y);
            return new Point3D(x, y, point.Z);
        }

        private static Point3D NormalizeDestination(Map map, Point3D p)
        {
            if (map == null)
                return p;

            int z = p.Z;
            if (!map.CanFit(p.X, p.Y, z, 16, false, false))
                z = map.GetAverageZ(p.X, p.Y);

            return new Point3D(p.X, p.Y, z);
        }

        private static bool IsWithinBounds(Map map, Point3D point)
        {
            return map != null && point.X >= 0 && point.Y >= 0 && point.X < map.Width && point.Y < map.Height;
        }

        private static int GetDistance(Point3D a, Point3D b)
        {
            int dx = a.X - b.X;
            int dy = a.Y - b.Y;
            return (int)Math.Round(Math.Sqrt((dx * dx) + (dy * dy)));
        }

        private static void RememberTile(AIGMSmartMovementState state, Point3D point)
        {
            if (state == null)
                return;

            state.RecentTiles.Enqueue(TileKey(point));
            while (state.RecentTiles.Count > RecentTileLimit)
                state.RecentTiles.Dequeue();

            state.RecentPositions.Enqueue(point);
            while (state.RecentPositions.Count > RecentTileLimit)
                state.RecentPositions.Dequeue();
        }

        private static bool IsRecentlyFailed(AIGMSmartMovementState state, string tileKey)
        {
            return state != null && state.FailedTiles.ContainsKey(tileKey);
        }

        private static int FailedCount(AIGMSmartMovementState state, string tileKey)
        {
            if (state == null || String.IsNullOrWhiteSpace(tileKey))
                return 0;

            int count;
            return state.FailedTiles.TryGetValue(tileKey, out count) ? count : 0;
        }

        private static string LastRecentTile(AIGMSmartMovementState state)
        {
            if (state == null || state.RecentTiles == null || state.RecentTiles.Count == 0)
                return null;

            string last = null;
            foreach (string tile in state.RecentTiles)
                last = tile;

            return last;
        }

        private static int CountRecentTile(AIGMSmartMovementState state, string tileKey)
        {
            if (state == null || state.RecentTiles == null || String.IsNullOrWhiteSpace(tileKey))
                return 0;

            int count = 0;
            foreach (string tile in state.RecentTiles)
            {
                if (tile == tileKey)
                    count++;
            }

            return count;
        }

        private static int FailedPenalty(AIGMSmartMovementState state, Point3D point)
        {
            if (state == null)
                return 0;

            int count;
            return state.FailedTiles.TryGetValue(TileKey(point), out count) ? 6 + count : 0;
        }

        private static void MarkFailed(AIGMSmartMovementState state, string tileKey)
        {
            if (state == null || String.IsNullOrWhiteSpace(tileKey))
                return;

            int count;
            state.FailedTiles.TryGetValue(tileKey, out count);
            state.FailedTiles[tileKey] = count + 1;
        }

        private static string TileKey(Point3D point)
        {
            return point.X + "," + point.Y + "," + point.Z;
        }

        private static bool TryParseTileKey(string value, out Point3D point)
        {
            point = Point3D.Zero;
            if (String.IsNullOrWhiteSpace(value))
                return false;

            string[] parts = value.Split(',');
            if (parts.Length != 3)
                return false;

            int x;
            int y;
            int z;
            if (!Int32.TryParse(parts[0], out x) || !Int32.TryParse(parts[1], out y) || !Int32.TryParse(parts[2], out z))
                return false;

            point = new Point3D(x, y, z);
            return true;
        }

        private static string FormatWaypoint(AIGMSmartMovementState state)
        {
            if (state == null || state.Kind != AIGMSmartMovementKind.RouteToPoint || state.RouteWaypoints == null || state.RouteWaypoints.Count == 0)
                return "none";

            return String.Format("{0}/{1}:{2}", state.CurrentWaypointIndex + 1, state.RouteWaypoints.Count, CurrentWaypointLabel(state));
        }

        private static string CurrentWaypointLabel(AIGMSmartMovementState state)
        {
            if (state == null || state.RouteWaypointLabels == null || state.CurrentWaypointIndex < 0 || state.CurrentWaypointIndex >= state.RouteWaypointLabels.Count)
                return "waypoint";

            return String.IsNullOrWhiteSpace(state.RouteWaypointLabels[state.CurrentWaypointIndex]) ? "waypoint" : state.RouteWaypointLabels[state.CurrentWaypointIndex];
        }
        private static string FormatPoint(Point3D point)
        {
            return String.Format("{0},{1},{2}", point.X, point.Y, point.Z);
        }

        private static string Describe(Mobile mobile)
        {
            return mobile == null ? "missing" : (SafeName(mobile) + "[" + mobile.Serial + "]");
        }

        private static string SafeName(Mobile mobile)
        {
            return mobile == null ? "missing" : (mobile.Name ?? mobile.GetType().Name);
        }

        private static string SafeLog(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return String.Empty;

            return value.Replace('"', '\'').Replace('\r', ' ').Replace('\n', ' ');
        }

        private sealed class SmartMovementTimer : Timer
        {
            private readonly Serial _actorSerial;
            private readonly int _expectedEpoch;

            public SmartMovementTimer(Serial actorSerial, int expectedEpoch)
                : base(TickInterval, TickInterval)
            {
                _actorSerial = actorSerial;
                _expectedEpoch = expectedEpoch;
                Priority = TimerPriority.TwoFiftyMS;
            }

            protected override void OnTick()
            {
                Mobile actor = World.FindMobile(_actorSerial);
                if (!AIGMOperationalControlService.CanOperate(actor, AIGMOperationalAction.Movement, _expectedEpoch, false))
                {
                    string ignored;
                    AIGMSmartMovementService.Stop(actor, "operational_callback_rejected", out ignored);
                    Stop();
                    return;
                }

                string result = AIGMSmartMovementService.Tick(actor);
                if (result == "inactive" || result == "arrived" || result == "path_blocked" || result.EndsWith("_invalid") || result.StartsWith("actor_") || result.StartsWith("target_") || result.StartsWith("destination_"))
                    Stop();
            }
        }
    }
}



