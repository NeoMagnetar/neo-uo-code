using System;
using System.Collections.Generic;

using Server;
using Server.Mobiles;
using Server.Custom.AIGM.Tasks;

namespace Server.Custom.AIGM
{
    public enum AIGMNativeNavigationPhase
    {
        None,
        Starting,
        FollowingNativePath,
        FollowingBeacon,
        ArrivedWaypoint,
        Arrived,
        Cancelled,
        Invalid
    }

    public sealed class AIGMNativeNavigationState
    {
        public int ActorSerial;
        public int RequesterSerial;
        public int BeaconSerial;
        public string DestinationName;
        public Point3D FinalDestination;
        public Map Map;
        public List<Point3D> Waypoints;
        public List<string> WaypointLabels;
        public int CurrentWaypointIndex;
        public int Radius;
        public OrderType PreviousOrder;
        public int PreviousTargetSerial;
        public bool PreviousTargetWasSet;
        public AIGMNativeNavigationPhase Phase;
        public int LastDistanceToWaypoint;
        public int BestDistanceToWaypoint;
        public int NonImprovingTicks;
        public Point3D LastPosition;
        public string LastResult;
        public DateTime StartedUtc;
        public DateTime UpdatedUtc;
        public bool NearbyDirect;
        public bool AllowSmartEscape;
        public string MovementPrimitive;

        public AIGMNativeNavigationState()
        {
            Waypoints = new List<Point3D>();
            WaypointLabels = new List<string>();
            Reset();
        }

        public void Reset()
        {
            ActorSerial = 0;
            RequesterSerial = 0;
            BeaconSerial = 0;
            DestinationName = String.Empty;
            FinalDestination = Point3D.Zero;
            Map = null;
            Waypoints.Clear();
            WaypointLabels.Clear();
            CurrentWaypointIndex = 0;
            Radius = 2;
            PreviousOrder = OrderType.None;
            PreviousTargetSerial = 0;
            PreviousTargetWasSet = false;
            Phase = AIGMNativeNavigationPhase.None;
            LastDistanceToWaypoint = -1;
            BestDistanceToWaypoint = Int32.MaxValue;
            NonImprovingTicks = 0;
            LastPosition = Point3D.Zero;
            LastResult = "inactive";
            StartedUtc = DateTime.MinValue;
            UpdatedUtc = DateTime.UtcNow;
            NearbyDirect = false;
            AllowSmartEscape = false;
            MovementPrimitive = String.Empty;
        }
    }

    public static class AIGMNativeNavigationService
    {
        private static readonly Dictionary<int, AIGMNativeNavigationState> States = new Dictionary<int, AIGMNativeNavigationState>();
        private static readonly Dictionary<int, NativeNavigationTimer> Timers = new Dictionary<int, NativeNavigationTimer>();
        private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(0.75);
        private const int MaxBeaconLeadDistance = 8;
        private const int BeaconAdvanceRadius = 1;
        private const int MaxNonImprovingTicks = 80;
        private const int MaxNearbyNonImprovingTicks = 12;
        private const int NativeEscapeTriggerTicks = 8;
        private static readonly bool UseDirectAIMoveTo = true;

        public static void Initialize()
        {
            EventSink.ServerStarted += CleanupOrphanBeacons;
        }

        public static bool StartRoute(BaseHire companion, AIGMNavigationRoute route, int radius, Mobile requester, out string response)
        {
            return StartRoute(companion, route, radius, requester, false, out response);
        }

        public static bool StartRoute(BaseHire companion, AIGMNavigationRoute route, int radius, Mobile requester, bool allowSmartEscape, out string response)
        {
            response = null;

            if (!IsValidCompanion(companion, out response))
                return false;

            if (route == null || route.Map == null || route.Waypoints == null || route.Waypoints.Count == 0)
            {
                response = "No usable native route was found.";
                return false;
            }

            if (companion.Map == null || companion.Map != route.Map)
            {
                response = "The route must be on the companion's current map.";
                return false;
            }

            AIGMNativeNavigationState state = PrepareState(companion, requester, route.DestinationName, route.FinalDestination, route.Map, radius, allowSmartEscape);
            for (int i = 0; i < route.Waypoints.Count; i++)
            {
                state.Waypoints.Add(NormalizeDestination(route.Map, route.Waypoints[i]));
                state.WaypointLabels.Add(route.WaypointLabels != null && i < route.WaypointLabels.Count ? route.WaypointLabels[i] ?? "waypoint" : "waypoint");
            }

            return StartPreparedState(companion, state, "graph", route.SourceNodeId, out response);
        }

        public static bool StartDirect(BaseHire companion, AIGMNavigationLocation location, int radius, Mobile requester, out string response)
        {
            return StartDirect(companion, location, radius, requester, false, out response);
        }

        public static bool StartDirect(BaseHire companion, AIGMNavigationLocation location, int radius, Mobile requester, bool allowSmartEscape, out string response)
        {
            response = null;

            if (!IsValidCompanion(companion, out response))
                return false;

            if (location == null || location.Map == null)
            {
                response = "No usable native destination was found.";
                return false;
            }

            if (companion.Map == null || companion.Map != location.Map)
            {
                response = "The destination must be on the companion's current map.";
                return false;
            }

            AIGMNativeNavigationState state = PrepareState(companion, requester, location.DisplayName, location.Point, location.Map, radius, allowSmartEscape);
            state.Waypoints.Add(NormalizeDestination(location.Map, location.Point));
            state.WaypointLabels.Add(location.DisplayName ?? "destination");
            return StartPreparedState(companion, state, "direct", "none", out response);
        }

        public static bool StartDirectNearby(BaseHire companion, AIGMNavigationLocation location, int radius, Mobile requester, out string response)
        {
            response = null;

            if (!IsValidCompanion(companion, out response))
                return false;

            if (location == null || location.Map == null)
            {
                response = "No usable nearby destination was found.";
                return false;
            }

            if (companion.Map == null || companion.Map != location.Map)
            {
                response = "The nearby destination must be on the companion's current map.";
                return false;
            }

            AIGMNativeNavigationState state = PrepareState(companion, requester, location.DisplayName, location.Point, location.Map, radius, false);
            state.NearbyDirect = true;
            state.Waypoints.Add(NormalizeDestination(location.Map, location.Point));
            state.WaypointLabels.Add(location.DisplayName ?? "destination");
            return StartPreparedState(companion, state, "nearby_direct", "none", out response);
        }

        public static bool Stop(Mobile actor, string reason, out string response)
        {
            response = null;

            BaseHire companion = actor as BaseHire;
            if (companion == null)
            {
                response = "No native companion navigation actor was supplied.";
                return false;
            }

            AIGMNativeNavigationState state;
            if (!States.TryGetValue(companion.Serial.Value, out state) || state == null || state.Phase == AIGMNativeNavigationPhase.None)
            {
                response = "No native companion navigation is active.";
                return false;
            }

            Finish(companion, state, AIGMNativeNavigationPhase.Cancelled, String.IsNullOrWhiteSpace(reason) ? "cancelled" : reason, false, true);
            response = "Native companion navigation cancelled.";
            return true;
        }

        public static AIGMNativeNavigationState GetState(Mobile actor)
        {
            if (actor == null)
                return null;

            AIGMNativeNavigationState state;
            return States.TryGetValue(actor.Serial.Value, out state) ? state : null;
        }

        public static string GetStatus(Mobile actor)
        {
            if (actor == null)
                return "nativeNav=none";

            AIGMNativeNavigationState state = GetState(actor);
            if (state == null || state.Phase == AIGMNativeNavigationPhase.None)
                return "nativeNav=inactive";

            BaseHire companion = actor as BaseHire;
            string order = companion != null ? companion.ControlOrder.ToString() : "n/a";
            string target = companion != null ? DescribeTarget(companion.ControlTarget) : "n/a";

            return String.Format(
                "nativeNav={0}; primitive=BaseAI.MoveTo; destination={1}; waypoint={2}/{3}:{4}; point={5}; final={6}; distance={7}; best={8}; nonImproving={9}; order={10}; target={11}; last={12}",
                state.Phase,
                state.DestinationName,
                state.CurrentWaypointIndex + 1,
                state.Waypoints.Count,
                CurrentWaypointLabel(state),
                FormatPoint(CurrentWaypoint(state)),
                FormatPoint(state.FinalDestination),
                state.LastDistanceToWaypoint,
                state.BestDistanceToWaypoint == Int32.MaxValue ? -1 : state.BestDistanceToWaypoint,
                state.NonImprovingTicks,
                order,
                target,
                state.LastResult);
        }

        public static string Tick(Mobile mobile)
        {
            BaseHire companion = mobile as BaseHire;
            if (companion == null)
                return "actor_invalid";

            string validation;
            if (!IsValidCompanion(companion, out validation))
                return Finish(companion, GetOrCreateState(companion), AIGMNativeNavigationPhase.Invalid, validation, true, true);

            AIGMNativeNavigationState state = GetOrCreateState(companion);
            state.UpdatedUtc = DateTime.UtcNow;

            if (state.Phase == AIGMNativeNavigationPhase.None)
                return "inactive";

            if (!AIGMOperationalControlService.CanOperate(companion, AIGMOperationalAction.Travel, AIGMOperationalControlService.GetEpoch(companion), false))
                return Finish(companion, state, AIGMNativeNavigationPhase.Cancelled, "operational_control_blocked", false, true);

            AIGMNavigationBeacon beacon = World.FindMobile(state.BeaconSerial) as AIGMNavigationBeacon;
            if (UseDirectAIMoveTo)
                return TickDirectAIMoveTo(companion, state);

            if (beacon == null || beacon.Deleted)
                return Finish(companion, state, AIGMNativeNavigationPhase.Invalid, "beacon_missing", true, true);

            if (state.Map == null || companion.Map == null || beacon.Map == null || companion.Map != state.Map || beacon.Map != state.Map)
                return Finish(companion, state, AIGMNativeNavigationPhase.Invalid, "map_mismatch", true, true);

            if (companion.ControlTarget != beacon || companion.ControlOrder != OrderType.Follow)
            {
                AIGMExecutionLog.Write("AIGM_NAV_ORDER actor={0} repair=True beforeOrder={1} beforeTarget={2} beacon={3}", Describe(companion), companion.ControlOrder, DescribeTarget(companion.ControlTarget), Describe(beacon));
                companion.ControlTarget = beacon;
                companion.ControlOrder = OrderType.Follow;
            }

            Point3D waypoint = CurrentWaypoint(state);
            int distanceToBeacon = GetDistance(companion.Location, beacon.Location);
            int distanceToWaypointBefore = GetDistance(companion.Location, waypoint);
            if (distanceToWaypointBefore > state.Radius && (distanceToBeacon <= BeaconAdvanceRadius || distanceToBeacon > MaxBeaconLeadDistance + 3 || beacon.Map != state.Map))
                MoveBeaconTowardWaypoint(companion, state, beacon, "roll");

            int beforeDistance = state.LastDistanceToWaypoint < 0 ? GetDistance(state.LastPosition == Point3D.Zero ? companion.Location : state.LastPosition, waypoint) : state.LastDistanceToWaypoint;
            int afterDistance = GetDistance(companion.Location, waypoint);
            state.LastDistanceToWaypoint = afterDistance;
            if (afterDistance < state.BestDistanceToWaypoint)
            {
                state.BestDistanceToWaypoint = afterDistance;
                state.NonImprovingTicks = 0;
            }
            else if (afterDistance > state.Radius)
            {
                state.NonImprovingTicks++;
            }

            state.LastPosition = companion.Location;
            state.Phase = AIGMNativeNavigationPhase.FollowingNativePath;
            state.LastResult = afterDistance < beforeDistance ? "distance_decreased" : (afterDistance == beforeDistance ? "distance_held" : "distance_increased");

            AIGMExecutionLog.Write(
                "AIGM_NAV_PROGRESS actor={0} native=True phase={1} current={2} waypoint=\"{3}\" waypointIndex={4}/{5} waypoint={6} beaconPoint={7} final=\"{8}\" finalPoint={9} vector={10} distanceToBeacon={11} distanceToWaypoint={12}->{13} distanceToFinal={14} order={15} target={16}",
                Describe(companion),
                state.Phase,
                FormatPoint(companion.Location),
                SafeLog(CurrentWaypointLabel(state)),
                state.CurrentWaypointIndex + 1,
                state.Waypoints.Count,
                FormatPoint(waypoint),
                FormatPoint(beacon.Location),
                SafeLog(state.DestinationName),
                FormatPoint(state.FinalDestination),
                FormatVector(companion.Location, waypoint),
                GetDistance(companion.Location, beacon.Location),
                beforeDistance,
                afterDistance,
                GetDistance(companion.Location, state.FinalDestination),
                companion.ControlOrder,
                DescribeTarget(companion.ControlTarget));

            if (state.NonImprovingTicks >= NativeEscapeTriggerTicks)
                return StartSmartEscape(companion, state, waypoint, "native_no_progress");

            if (state.NonImprovingTicks >= MaxNonImprovingTicks)
                return Finish(companion, state, AIGMNativeNavigationPhase.Invalid, "native_path_stalled", false, true);

            if (afterDistance <= state.Radius)
            {
                if (AdvanceWaypoint(companion, state, beacon))
                    return "waypoint_arrived";

                return Finish(companion, state, AIGMNativeNavigationPhase.Arrived, "arrived", false, true);
            }

            return state.LastResult;
        }

        private static AIGMNativeNavigationState PrepareState(BaseHire companion, Mobile requester, string destinationName, Point3D finalDestination, Map map, int radius, bool allowSmartEscape)
        {
            string stopResponse;
            Stop(companion, "native_navigation_replaced", out stopResponse);
            AIGMSmartMovementService.Stop(companion, "native_navigation_takeover", out stopResponse);

            AIGMNativeNavigationState state = GetOrCreateState(companion);
            state.Reset();
            state.ActorSerial = companion.Serial.Value;
            state.RequesterSerial = requester != null ? requester.Serial.Value : 0;
            state.DestinationName = destinationName ?? "destination";
            state.FinalDestination = NormalizeDestination(map, finalDestination);
            state.Map = map;
            state.Radius = Math.Max(0, radius);
            state.PreviousOrder = companion.ControlOrder;
            Mobile previousTarget = companion.ControlTarget as Mobile;
            state.PreviousTargetWasSet = previousTarget != null;
            state.PreviousTargetSerial = previousTarget != null ? previousTarget.Serial.Value : 0;
            state.StartedUtc = DateTime.UtcNow;
            state.UpdatedUtc = state.StartedUtc;
            state.Phase = AIGMNativeNavigationPhase.Starting;
            state.LastPosition = companion.Location;
            state.LastDistanceToWaypoint = -1;
            state.BestDistanceToWaypoint = Int32.MaxValue;
            state.AllowSmartEscape = allowSmartEscape;
            state.NearbyDirect = false;
            state.MovementPrimitive = UseDirectAIMoveTo ? "BaseAI.MoveTo" : "PathFollower.Follow";
            return state;
        }

        private static bool StartPreparedState(BaseHire companion, AIGMNativeNavigationState state, string routeKind, string sourceNodeId, out string response)
        {
            response = null;

            if (state.Waypoints.Count == 0)
            {
                response = "No native waypoint was available.";
                return false;
            }

            if (!AIGMOperationalControlService.CanOperate(companion, AIGMOperationalAction.Travel, AIGMOperationalControlService.GetEpoch(companion), false))
            {
                response = "Operational control rejected native navigation.";
                return false;
            }

            if (UseDirectAIMoveTo)
            {
                Point3D directWaypoint = CurrentWaypoint(state);

                AIGMExecutionLog.Write(
                    "AIGM_NAV_ORDER actor={0} native=True primitive=BaseAI.MoveTo action=start previousOrder={1} previousTarget={2} newOrder=Stay waypoint=\"{3}\" waypointPoint={4}",
                    Describe(companion),
                    state.PreviousOrder,
                    state.PreviousTargetWasSet ? state.PreviousTargetSerial.ToString() : "none",
                    SafeLog(CurrentWaypointLabel(state)),
                    FormatPoint(directWaypoint));

                companion.Combatant = null;
                companion.Warmode = false;
                companion.ControlTarget = null;
                companion.ControlOrder = OrderType.Stay;

                state.Phase = AIGMNativeNavigationPhase.FollowingNativePath;
                state.LastDistanceToWaypoint = GetDistance(companion.Location, directWaypoint);
                state.BestDistanceToWaypoint = state.LastDistanceToWaypoint;
                state.LastResult = "native_ai_moveto_started";
                StartTimer(companion);

                if (state.NearbyDirect)
                {
                    AIGMExecutionLog.Write(
                        "AIGM_NEARBY_TRAVEL_START companion={0} destination=\"{1}\" currentPoint={2} targetPoint={3} distance={4} arrivalRadius={5} movementPrimitive={6}",
                        Describe(companion),
                        SafeLog(state.DestinationName),
                        FormatPoint(companion.Location),
                        FormatPoint(directWaypoint),
                        state.LastDistanceToWaypoint,
                        state.Radius,
                        SafeLog(state.MovementPrimitive));
                }

                AIGMExecutionLog.Write(
                    "AIGM_NAV_TARGET actor={0} native=True primitive=BaseAI.MoveTo route={1} source={2} current={3} waypoint=\"{4}\" waypointIndex={5}/{6} waypoint={7} final=\"{8}\" finalPoint={9} vector={10} distanceToWaypoint={11} distanceToFinal={12} order={13} target={14}",
                    Describe(companion),
                    SafeLog(routeKind),
                    SafeLog(sourceNodeId),
                    FormatPoint(companion.Location),
                    SafeLog(CurrentWaypointLabel(state)),
                    state.CurrentWaypointIndex + 1,
                    state.Waypoints.Count,
                    FormatPoint(directWaypoint),
                    SafeLog(state.DestinationName),
                    FormatPoint(state.FinalDestination),
                    FormatVector(companion.Location, directWaypoint),
                    state.LastDistanceToWaypoint,
                    GetDistance(companion.Location, state.FinalDestination),
                    companion.ControlOrder,
                    DescribeTarget(companion.ControlTarget));

                response = String.Format("Native AI route toward {0} via {1} waypoint{2}.", state.DestinationName, state.Waypoints.Count, state.Waypoints.Count == 1 ? String.Empty : "s");
                return true;
            }

            AIGMNavigationBeacon beacon = new AIGMNavigationBeacon();
            Point3D waypoint = CurrentWaypoint(state);
            MoveBeaconTowardWaypoint(companion, state, beacon, "start");
            state.BeaconSerial = beacon.Serial.Value;

            AIGMExecutionLog.Write(
                "AIGM_NAV_ORDER actor={0} native=True action=start previousOrder={1} previousTarget={2} newOrder=Follow beacon={3} waypoint=\"{4}\" waypointPoint={5} beaconPoint={6}",
                Describe(companion),
                state.PreviousOrder,
                state.PreviousTargetWasSet ? state.PreviousTargetSerial.ToString() : "none",
                Describe(beacon),
                SafeLog(CurrentWaypointLabel(state)),
                FormatPoint(waypoint),
                FormatPoint(beacon.Location));

            companion.Combatant = null;
            companion.Warmode = false;
            companion.ControlTarget = beacon;
            companion.ControlOrder = OrderType.Follow;

            state.Phase = beacon == null ? AIGMNativeNavigationPhase.FollowingNativePath : AIGMNativeNavigationPhase.FollowingBeacon;
            state.LastDistanceToWaypoint = GetDistance(companion.Location, waypoint);
            state.BestDistanceToWaypoint = state.LastDistanceToWaypoint;
            state.LastResult = "native_follow_beacon_started";
            StartTimer(companion);

            AIGMExecutionLog.Write(
                "AIGM_NAV_TARGET actor={0} native=True route={1} source={2} current={3} waypoint=\"{4}\" waypointIndex={5}/{6} waypoint={7} beaconPoint={8} final=\"{9}\" finalPoint={10} vector={11} distanceToBeacon={12} distanceToWaypoint={13} distanceToFinal={14} order={15} target={16}",
                Describe(companion),
                SafeLog(routeKind),
                SafeLog(sourceNodeId),
                FormatPoint(companion.Location),
                SafeLog(CurrentWaypointLabel(state)),
                state.CurrentWaypointIndex + 1,
                state.Waypoints.Count,
                FormatPoint(waypoint),
                FormatPoint(beacon.Location),
                SafeLog(state.DestinationName),
                FormatPoint(state.FinalDestination),
                FormatVector(companion.Location, waypoint),
                GetDistance(companion.Location, beacon.Location),
                state.LastDistanceToWaypoint,
                GetDistance(companion.Location, state.FinalDestination),
                companion.ControlOrder,
                DescribeTarget(companion.ControlTarget));

            response = String.Format("Native route toward {0} via {1} waypoint{2}.", state.DestinationName, state.Waypoints.Count, state.Waypoints.Count == 1 ? String.Empty : "s");
            return true;
        }

        private static bool AdvanceWaypoint(BaseHire companion, AIGMNativeNavigationState state, AIGMNavigationBeacon beacon)
        {
            AIGMExecutionLog.Write(
                "AIGM_NAV_ARRIVE_WAYPOINT actor={0} waypoint=\"{1}\" waypointIndex={2}/{3} point={4} distance={5}",
                Describe(companion),
                SafeLog(CurrentWaypointLabel(state)),
                state.CurrentWaypointIndex + 1,
                state.Waypoints.Count,
                FormatPoint(CurrentWaypoint(state)),
                state.LastDistanceToWaypoint);

            if (state.CurrentWaypointIndex + 1 >= state.Waypoints.Count)
                return false;

            state.CurrentWaypointIndex++;
            Point3D next = CurrentWaypoint(state);
            if (beacon != null)
                MoveBeaconTowardWaypoint(companion, state, beacon, "advance");
            state.LastDistanceToWaypoint = GetDistance(companion.Location, next);
            state.BestDistanceToWaypoint = state.LastDistanceToWaypoint;
            state.NonImprovingTicks = 0;
            state.Phase = AIGMNativeNavigationPhase.FollowingNativePath;
            state.LastResult = "advanced_waypoint";

            AIGMExecutionLog.Write(
                "AIGM_NAV_TARGET actor={0} native=True route=advance current={1} waypoint=\"{2}\" waypointIndex={3}/{4} waypoint={5} beaconPoint={6} final=\"{7}\" finalPoint={8} vector={9} distanceToBeacon={10} distanceToWaypoint={11} distanceToFinal={12} order={13} target={14}",
                Describe(companion),
                FormatPoint(companion.Location),
                SafeLog(CurrentWaypointLabel(state)),
                state.CurrentWaypointIndex + 1,
                state.Waypoints.Count,
                FormatPoint(next),
                beacon != null ? FormatPoint(beacon.Location) : "none",
                SafeLog(state.DestinationName),
                FormatPoint(state.FinalDestination),
                FormatVector(companion.Location, next),
                beacon != null ? GetDistance(companion.Location, beacon.Location) : -1,
                state.LastDistanceToWaypoint,
                GetDistance(companion.Location, state.FinalDestination),
                companion.ControlOrder,
                DescribeTarget(companion.ControlTarget));

            return true;
        }

        private static string TickDirectAIMoveTo(BaseHire companion, AIGMNativeNavigationState state)
        {
            if (companion.AIObject == null)
                return Finish(companion, state, AIGMNativeNavigationPhase.Invalid, "ai_missing", false, true);

            Point3D waypoint = CurrentWaypoint(state);
            Point3D beforePoint = companion.Location;
            int beforeDistance = state.LastDistanceToWaypoint < 0 ? GetDistance(state.LastPosition == Point3D.Zero ? companion.Location : state.LastPosition, waypoint) : state.LastDistanceToWaypoint;
            bool moveResult = companion.AIObject.MoveTo(waypoint, true, state.Radius);
            int afterDistance = GetDistance(companion.Location, waypoint);

            state.LastDistanceToWaypoint = afterDistance;
            if (afterDistance < state.BestDistanceToWaypoint)
            {
                state.BestDistanceToWaypoint = afterDistance;
                state.NonImprovingTicks = 0;
            }
            else if (afterDistance > state.Radius)
            {
                state.NonImprovingTicks++;
            }

            state.LastPosition = companion.Location;
            state.Phase = AIGMNativeNavigationPhase.FollowingBeacon;
            state.LastResult = afterDistance < beforeDistance ? "distance_decreased" : (afterDistance == beforeDistance ? "distance_held" : "distance_increased");

            if (state.NearbyDirect)
            {
                AIGMExecutionLog.Write(
                    "AIGM_NEARBY_TRAVEL_TICK companion={0} currentPoint={1} targetPoint={2} distance={3} previousDistance={4} improved={5} moved={6}",
                    Describe(companion),
                    FormatPoint(companion.Location),
                    FormatPoint(waypoint),
                    afterDistance,
                    beforeDistance,
                    afterDistance < beforeDistance,
                    companion.Location != beforePoint);
            }

            AIGMExecutionLog.Write(
                "AIGM_NAV_PROGRESS actor={0} native=True primitive=BaseAI.MoveTo phase={1} current={2} waypoint=\"{3}\" waypointIndex={4}/{5} waypoint={6} final=\"{7}\" finalPoint={8} vector={9} distanceToWaypoint={10}->{11} distanceToFinal={12} order={13} target={14} aiMoveResult={15} nonImproving={16}",
                Describe(companion),
                state.Phase,
                FormatPoint(companion.Location),
                SafeLog(CurrentWaypointLabel(state)),
                state.CurrentWaypointIndex + 1,
                state.Waypoints.Count,
                FormatPoint(waypoint),
                SafeLog(state.DestinationName),
                FormatPoint(state.FinalDestination),
                FormatVector(companion.Location, waypoint),
                beforeDistance,
                afterDistance,
                GetDistance(companion.Location, state.FinalDestination),
                companion.ControlOrder,
                DescribeTarget(companion.ControlTarget),
                moveResult,
                state.NonImprovingTicks);

            if (state.AllowSmartEscape && state.NonImprovingTicks >= NativeEscapeTriggerTicks)
                return StartSmartEscape(companion, state, waypoint, "native_no_progress");

            if (state.NearbyDirect && state.NonImprovingTicks >= MaxNearbyNonImprovingTicks)
                return Finish(companion, state, AIGMNativeNavigationPhase.Invalid, "nearby_no_progress", false, true);

            if (state.NonImprovingTicks >= MaxNonImprovingTicks)
                return Finish(companion, state, AIGMNativeNavigationPhase.Invalid, "native_path_stalled", false, true);

            if (afterDistance <= state.Radius)
                return AdvanceWaypoint(companion, state, null) ? "waypoint_arrived" : Finish(companion, state, AIGMNativeNavigationPhase.Arrived, "arrived", false, true);

            return state.LastResult;
        }

        private static string StartSmartEscape(BaseHire companion, AIGMNativeNavigationState state, Point3D waypoint, string reason)
        {
            if (companion == null || state == null || state.Map == null)
                return Finish(companion, state, AIGMNativeNavigationPhase.Invalid, reason, false, true);

            List<Point3D> waypoints = new List<Point3D>();
            List<string> labels = new List<string>();
            int start = Math.Max(0, Math.Min(state.CurrentWaypointIndex, state.Waypoints.Count - 1));
            for (int i = start; i < state.Waypoints.Count; i++)
            {
                waypoints.Add(state.Waypoints[i]);
                labels.Add(state.WaypointLabels != null && i < state.WaypointLabels.Count ? state.WaypointLabels[i] : "waypoint");
            }

            if (waypoints.Count == 0)
            {
                waypoints.Add(waypoint);
                labels.Add(CurrentWaypointLabel(state));
            }

            AIGMNavigationRoute escapeRoute = new AIGMNavigationRoute(
                state.DestinationName,
                state.DestinationName,
                state.FinalDestination == Point3D.Zero ? waypoints[waypoints.Count - 1] : state.FinalDestination,
                state.Map,
                waypoints,
                labels,
                "native_escape");

            Mobile requester = state.RequesterSerial != 0 ? World.FindMobile(state.RequesterSerial) : companion.GetOwner();
            AIGMExecutionLog.Write(
                "AIGM_NAV_ESCAPE_TAKEOVER actor={0} reason={1} current={2} waypoint=\"{3}\" waypointIndex={4}/{5} waypoint={6} nonImproving={7} routeWaypoints={8}",
                Describe(companion),
                SafeLog(reason),
                FormatPoint(companion.Location),
                SafeLog(CurrentWaypointLabel(state)),
                state.CurrentWaypointIndex + 1,
                state.Waypoints.Count,
                FormatPoint(waypoint),
                state.NonImprovingTicks,
                waypoints.Count);

            Finish(companion, state, AIGMNativeNavigationPhase.Cancelled, "native_escape_takeover", false, true);

            string response;
            if (AIGMSmartMovementService.StartRoute(companion, escapeRoute, state.Radius, requester, out response))
            {
                AIGMExecutionLog.Write("AIGM_NAV_ESCAPE_START actor={0} result=smart_route response=\"{1}\"", Describe(companion), SafeLog(response));
                return "native_escape_takeover";
            }

            AIGMExecutionLog.Write("AIGM_NAV_ESCAPE_START actor={0} result=failed response=\"{1}\"", Describe(companion), SafeLog(response));
            return "native_escape_failed";
        }

        private static string Finish(BaseHire companion, AIGMNativeNavigationState state, AIGMNativeNavigationPhase phase, string reason, bool restorePreviousOrder, bool deleteBeacon)
        {
            if (state == null)
                return reason ?? "invalid";

            StopTimer(state.ActorSerial);

            bool beaconDeleted = false;
            AIGMNavigationBeacon beacon = World.FindMobile(state.BeaconSerial) as AIGMNavigationBeacon;
            if (deleteBeacon && beacon != null && !beacon.Deleted)
            {
                beacon.Delete();
                beaconDeleted = true;
            }

            OrderType orderBefore = companion != null ? companion.ControlOrder : OrderType.None;
            string targetBefore = companion != null ? DescribeTarget(companion.ControlTarget) : "none";

            if (companion != null && !companion.Deleted)
            {
                Mobile restoreTarget = null;
                if (restorePreviousOrder && state.PreviousTargetWasSet && state.PreviousTargetSerial != 0)
                    restoreTarget = World.FindMobile(state.PreviousTargetSerial);

                if (restorePreviousOrder && IsRestorableOrder(state.PreviousOrder, restoreTarget))
                {
                    companion.ControlTarget = restoreTarget;
                    companion.ControlOrder = state.PreviousOrder;
                }
                else
                {
                    companion.ControlTarget = null;
                    companion.ControlOrder = OrderType.Stay;
                }

                companion.Warmode = false;
                AIGMExecutionLog.Write(
                    "AIGM_NAV_RESTORE_ORDER actor={0} reason={1} phase={2} beforeOrder={3} beforeTarget={4} afterOrder={5} afterTarget={6} restoredPrevious={7}",
                    Describe(companion),
                    SafeLog(reason),
                    phase,
                    orderBefore,
                    targetBefore,
                    companion.ControlOrder,
                    DescribeTarget(companion.ControlTarget),
                    restorePreviousOrder && IsRestorableOrder(state.PreviousOrder, restoreTarget));
            }

            state.Phase = phase;
            state.LastResult = reason ?? phase.ToString();
            state.UpdatedUtc = DateTime.UtcNow;

            if (state.NearbyDirect)
            {
                double elapsedSeconds = state.StartedUtc == DateTime.MinValue ? 0.0 : (DateTime.UtcNow - state.StartedUtc).TotalSeconds;
                if (phase == AIGMNativeNavigationPhase.Arrived)
                {
                    AIGMExecutionLog.Write(
                        "AIGM_NEARBY_TRAVEL_ARRIVED companion={0} destination=\"{1}\" finalDistance={2} elapsedSeconds={3:F1}",
                        companion != null ? Describe(companion) : state.ActorSerial.ToString(),
                        SafeLog(state.DestinationName),
                        state.LastDistanceToWaypoint,
                        elapsedSeconds);
                }
                else if (phase == AIGMNativeNavigationPhase.Invalid)
                {
                    AIGMExecutionLog.Write(
                        "AIGM_NEARBY_TRAVEL_FAILED companion={0} destination=\"{1}\" reason={2} distance={3} elapsedSeconds={4:F1}",
                        companion != null ? Describe(companion) : state.ActorSerial.ToString(),
                        SafeLog(state.DestinationName),
                        SafeLog(reason),
                        state.LastDistanceToWaypoint,
                        elapsedSeconds);
                }
            }

            AIGMExecutionLog.Write("AIGM_NAV_STOP actor={0} native=True phase={1} reason={2} beaconDeleted={3}", companion != null ? Describe(companion) : state.ActorSerial.ToString(), phase, SafeLog(reason), beaconDeleted);

            if (phase == AIGMNativeNavigationPhase.Cancelled || phase == AIGMNativeNavigationPhase.Arrived || phase == AIGMNativeNavigationPhase.Invalid)
            {
                States.Remove(state.ActorSerial);
            }

            return state.LastResult;
        }

        private static bool IsRestorableOrder(OrderType order, Mobile target)
        {
            if (order == OrderType.Follow || order == OrderType.Guard || order == OrderType.Attack || order == OrderType.Friend || order == OrderType.Transfer)
                return target != null && !target.Deleted && target.Alive;

            return order == OrderType.Stay || order == OrderType.Come || order == OrderType.None || order == OrderType.Stop;
        }

        private static AIGMNativeNavigationState GetOrCreateState(BaseHire companion)
        {
            int serial = companion != null ? companion.Serial.Value : 0;
            AIGMNativeNavigationState state;
            if (!States.TryGetValue(serial, out state))
            {
                state = new AIGMNativeNavigationState();
                state.ActorSerial = serial;
                States[serial] = state;
            }

            return state;
        }

        private static void StartTimer(BaseHire companion)
        {
            if (companion == null)
                return;

            StopTimer(companion.Serial.Value);
            NativeNavigationTimer timer = new NativeNavigationTimer(companion.Serial, AIGMOperationalControlService.GetEpoch(companion));
            Timers[companion.Serial.Value] = timer;
            timer.Start();
        }

        private static void StopTimer(int serial)
        {
            NativeNavigationTimer timer;
            if (Timers.TryGetValue(serial, out timer) && timer != null)
                timer.Stop();
            Timers.Remove(serial);
        }

        private static bool IsValidCompanion(BaseHire companion, out string reason)
        {
            reason = null;

            if (companion == null || companion.Deleted)
                reason = "actor_invalid";
            else if (!companion.Alive || companion.IsDeadBondedPet)
                reason = "actor_dead";
            else if (companion.Map == null || companion.Map == Map.Internal)
                reason = "actor_map_invalid";
            else if (companion.Frozen)
                reason = "actor_frozen";
            else if (companion.Paralyzed)
                reason = "actor_paralyzed";
            else if (companion.CantWalk)
                reason = "actor_cannot_walk";

            return reason == null;
        }

        private static Point3D CurrentWaypoint(AIGMNativeNavigationState state)
        {
            if (state == null || state.Waypoints == null || state.Waypoints.Count == 0)
                return Point3D.Zero;

            int index = Math.Max(0, Math.Min(state.CurrentWaypointIndex, state.Waypoints.Count - 1));
            return state.Waypoints[index];
        }

        private static string CurrentWaypointLabel(AIGMNativeNavigationState state)
        {
            if (state == null || state.WaypointLabels == null || state.WaypointLabels.Count == 0)
                return "waypoint";

            int index = Math.Max(0, Math.Min(state.CurrentWaypointIndex, state.WaypointLabels.Count - 1));
            return String.IsNullOrWhiteSpace(state.WaypointLabels[index]) ? "waypoint" : state.WaypointLabels[index];
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

        private static void MoveBeaconTowardWaypoint(BaseHire companion, AIGMNativeNavigationState state, AIGMNavigationBeacon beacon, string reason)
        {
            if (companion == null || state == null || beacon == null || beacon.Deleted || state.Map == null)
                return;

            Point3D waypoint = CurrentWaypoint(state);
            Point3D beaconPoint = BuildBeaconPoint(companion.Location, waypoint, state.Map);
            if (beacon.Map != state.Map || beacon.Location != beaconPoint)
                beacon.MoveToWorld(beaconPoint, state.Map);

            AIGMExecutionLog.Write(
                "AIGM_NAV_TARGET actor={0} native=True route=beacon_{1} current={2} waypoint=\"{3}\" waypointIndex={4}/{5} waypoint={6} beaconPoint={7} vector={8} distanceToBeacon={9} distanceToWaypoint={10}",
                Describe(companion),
                SafeLog(reason),
                FormatPoint(companion.Location),
                SafeLog(CurrentWaypointLabel(state)),
                state.CurrentWaypointIndex + 1,
                state.Waypoints.Count,
                FormatPoint(waypoint),
                FormatPoint(beaconPoint),
                FormatVector(companion.Location, waypoint),
                GetDistance(companion.Location, beaconPoint),
                GetDistance(companion.Location, waypoint));
        }

        private static Point3D BuildBeaconPoint(Point3D from, Point3D waypoint, Map map)
        {
            int distance = GetDistance(from, waypoint);
            if (distance <= MaxBeaconLeadDistance)
                return NormalizeDestination(map, waypoint);

            double scale = (double)MaxBeaconLeadDistance / Math.Max(1, distance);
            int x = from.X + (int)Math.Round((waypoint.X - from.X) * scale);
            int y = from.Y + (int)Math.Round((waypoint.Y - from.Y) * scale);

            if (x == from.X && waypoint.X != from.X)
                x += waypoint.X > from.X ? 1 : -1;

            if (y == from.Y && waypoint.Y != from.Y)
                y += waypoint.Y > from.Y ? 1 : -1;

            return NormalizeDestination(map, new Point3D(x, y, waypoint.Z));
        }

        private static int GetDistance(Point3D a, Point3D b)
        {
            int dx = a.X - b.X;
            int dy = a.Y - b.Y;
            return (int)Math.Round(Math.Sqrt((dx * dx) + (dy * dy)));
        }

        private static string FormatPoint(Point3D point)
        {
            return point.X + "," + point.Y + "," + point.Z;
        }

        private static string FormatVector(Point3D from, Point3D to)
        {
            int dx = to.X - from.X;
            int dy = to.Y - from.Y;
            return dx + "," + dy;
        }

        private static string Describe(Mobile mobile)
        {
            return mobile == null ? "missing" : (SafeName(mobile) + "[" + mobile.Serial + "]");
        }

        private static string SafeName(Mobile mobile)
        {
            return mobile == null ? "missing" : (mobile.Name ?? mobile.GetType().Name);
        }

        private static string DescribeTarget(IDamageable target)
        {
            Mobile mobile = target as Mobile;
            return mobile == null ? "none" : Describe(mobile);
        }

        private static string SafeLog(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return String.Empty;

            return value.Replace('"', '\'').Replace('\r', ' ').Replace('\n', ' ');
        }

        private static void CleanupOrphanBeacons()
        {
            List<AIGMNavigationBeacon> beacons = new List<AIGMNavigationBeacon>();
            foreach (Mobile mobile in World.Mobiles.Values)
            {
                AIGMNavigationBeacon beacon = mobile as AIGMNavigationBeacon;
                if (beacon != null && !beacon.Deleted)
                    beacons.Add(beacon);
            }

            for (int i = 0; i < beacons.Count; i++)
                beacons[i].Delete();

            if (beacons.Count > 0)
                AIGMExecutionLog.Write("AIGM_NAV_STOP cleanupOrphanBeacons={0}", beacons.Count);
        }

        private sealed class NativeNavigationTimer : Timer
        {
            private readonly Serial _actorSerial;
            private readonly int _expectedEpoch;

            public NativeNavigationTimer(Serial actorSerial, int expectedEpoch)
                : base(TickInterval, TickInterval)
            {
                _actorSerial = actorSerial;
                _expectedEpoch = expectedEpoch;
                Priority = TimerPriority.TwoFiftyMS;
            }

            protected override void OnTick()
            {
                Mobile actor = World.FindMobile(_actorSerial);
                if (!AIGMOperationalControlService.CanOperate(actor, AIGMOperationalAction.Travel, _expectedEpoch, false))
                {
                    string ignored;
                    AIGMNativeNavigationService.Stop(actor, "operational_callback_rejected", out ignored);
                    Stop();
                    return;
                }

                string result = AIGMNativeNavigationService.Tick(actor);
                if (result == "inactive" || result == "arrived" || result.EndsWith("_invalid") || result.StartsWith("actor_") || result == "beacon_missing" || result == "map_mismatch")
                    Stop();
            }
        }
    }
}
