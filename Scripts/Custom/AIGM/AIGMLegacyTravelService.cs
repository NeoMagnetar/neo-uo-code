using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using Server;
using Server.Custom.AIGM.Navigation;
using Server.Custom.AIGM.Tasks;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public enum AIGMLegacyTravelMode
    {
        None,
        Traveling,
        Stopped,
        Arrived,
        Blocked,
        Invalid
    }

    public enum AIGMLegacyTravelStrategy
    {
        DirectStep,
        PathFollower,
        LocalAlternateStep,
        CommittedDetour,
        BreadcrumbBacktrack,
        Stuck
    }

    public sealed class AIGMLegacyTravelState
    {
        public Serial CompanionSerial;
        public Serial OwnerSerial;
        public AIGMLegacyTravelMode Mode;
        public string DestinationName;
        public Point3D Destination;
        public Point3D FinalDestination;
        public string FinalDestinationName;
        public Map Map;
        public int ArrivalRadius;
        public DateTime StartedUtc;
        public DateTime LastProgressUtc;
        public DateTime LastOwnerNoticeUtc;
        public Point3D LastObservedLocation;
        public int LastDistance;
        public int StuckTicks;
        public int PathFailureCount;
        public int ConsecutiveBlockedSteps;
        public int ConsecutiveNoProgressChecks;
        public int ConsecutivePathFollowerFailures;
        public bool LastMoveWasRunning;
        public bool UsingDetourWaypoint;
        public int DetourAttempts;
        public int DetourCommitmentPulsesRemaining;
        public int SuccessfulDetourMoves;
        public int ConsecutiveDetourFailures;
        public Point3D DetourStartLocation;
        public Point3D ActiveDetourPoint;
        public string ActiveRouteBand;
        public string SuppressedRouteBand;
        public DateTime SuppressedRouteBandUntilUtc;
        public int OscillationCount;
        public List<Point3D> SoftWaypoints;
        public List<string> SoftWaypointIds;
        public List<string> SoftWaypointLabels;
        public int CurrentWaypointIndex;
        public Point3D CurrentMoveTarget;
        public bool ExplicitRouteActive;
        public bool WalkedRouteActive;
        public bool StopAtLastWaypoint;
        public string RouteId;
        public string RouteTerminalName;
        public int RouteCompletionRadius;
        public Point3D RouteLastWaypoint;
        public DateTime StartDelayUntilUtc;
        public int RouteStartIndex;
        public int RouteRecoveryOffsetAttempts;
        public int RouteRecoverySkipAttempts;
        public int RouteRecoveryCatchupAttempts;
        public int WalkedRouteSidestepAttempts;
        public Queue<Point3D> Breadcrumbs;
        public List<Point3D> FailedWaypoints;
        public PathFollower ActivePathFollower;
        public string ActivePathFollowerRouteId;
        public int ActivePathFollowerRouteIndex;
        public Point3D ActivePathFollowerTarget;
        public Point3D ActivePathFollowerLastLocation;
        public DateTime ActivePathFollowerLastProgressUtc;
        public DateTime LastPathFollowerRepathUtc;
        public AIGMLegacyTravelStrategy CurrentStrategy;
        public string LastStatus;
        public string TravelLeaseId;
        public double OriginalActiveSpeed;
        public double OriginalPassiveSpeed;
        public bool TravelSpeedApplied;

        public AIGMLegacyTravelState()
        {
            SoftWaypoints = new List<Point3D>();
            SoftWaypointIds = new List<string>();
            SoftWaypointLabels = new List<string>();
            Breadcrumbs = new Queue<Point3D>();
            FailedWaypoints = new List<Point3D>();
            Reset();
        }

        public void Reset()
        {
            CompanionSerial = Serial.MinusOne;
            OwnerSerial = Serial.MinusOne;
            Mode = AIGMLegacyTravelMode.None;
            DestinationName = null;
            Destination = Point3D.Zero;
            FinalDestination = Point3D.Zero;
            FinalDestinationName = null;
            Map = null;
            ArrivalRadius = 2;
            StartedUtc = DateTime.MinValue;
            LastProgressUtc = DateTime.MinValue;
            LastOwnerNoticeUtc = DateTime.MinValue;
            LastObservedLocation = Point3D.Zero;
            LastDistance = 0;
            StuckTicks = 0;
            PathFailureCount = 0;
            ConsecutiveBlockedSteps = 0;
            ConsecutiveNoProgressChecks = 0;
            ConsecutivePathFollowerFailures = 0;
            LastMoveWasRunning = false;
            UsingDetourWaypoint = false;
            DetourAttempts = 0;
            DetourCommitmentPulsesRemaining = 0;
            SuccessfulDetourMoves = 0;
            ConsecutiveDetourFailures = 0;
            DetourStartLocation = Point3D.Zero;
            ActiveDetourPoint = Point3D.Zero;
            ActiveRouteBand = null;
            SuppressedRouteBand = null;
            SuppressedRouteBandUntilUtc = DateTime.MinValue;
            OscillationCount = 0;
            if (SoftWaypoints == null)
                SoftWaypoints = new List<Point3D>();
            else
                SoftWaypoints.Clear();
            if (SoftWaypointIds == null)
                SoftWaypointIds = new List<string>();
            else
                SoftWaypointIds.Clear();
            if (SoftWaypointLabels == null)
                SoftWaypointLabels = new List<string>();
            else
                SoftWaypointLabels.Clear();
            CurrentWaypointIndex = -1;
            CurrentMoveTarget = Point3D.Zero;
            ExplicitRouteActive = false;
            WalkedRouteActive = false;
            StopAtLastWaypoint = false;
            RouteId = null;
            RouteTerminalName = null;
            RouteCompletionRadius = 0;
            RouteLastWaypoint = Point3D.Zero;
            StartDelayUntilUtc = DateTime.MinValue;
            RouteStartIndex = 0;
            RouteRecoveryOffsetAttempts = 0;
            RouteRecoverySkipAttempts = 0;
            RouteRecoveryCatchupAttempts = 0;
            WalkedRouteSidestepAttempts = 0;
            if (Breadcrumbs == null)
                Breadcrumbs = new Queue<Point3D>();
            else
                Breadcrumbs.Clear();
            if (FailedWaypoints == null)
                FailedWaypoints = new List<Point3D>();
            else
            FailedWaypoints.Clear();
            ActivePathFollower = null;
            ActivePathFollowerRouteId = null;
            ActivePathFollowerRouteIndex = -1;
            ActivePathFollowerTarget = Point3D.Zero;
            ActivePathFollowerLastLocation = Point3D.Zero;
            ActivePathFollowerLastProgressUtc = DateTime.MinValue;
            LastPathFollowerRepathUtc = DateTime.MinValue;
            CurrentStrategy = AIGMLegacyTravelStrategy.DirectStep;
            LastStatus = "inactive";
            TravelLeaseId = null;
            OriginalActiveSpeed = 0.0;
            OriginalPassiveSpeed = 0.0;
            TravelSpeedApplied = false;
        }
    }

    public static class AIGMLegacyTravelService
    {
        private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(1.0);
        private static readonly TimeSpan OwnerNoticeCooldown = TimeSpan.FromSeconds(8.0);
        private const int SoftWaypointArrivalRadius = 4;
        private const int WalkedRouteAdvanceRadius = 6;
        private const int WalkedRouteRecoveryNearNodeRadius = 8;
        private const int RouteStartStaggerMs = 750;
        private static readonly string LegacyRoutesPath = Path.Combine(AIGMNavGraphStore.DataDirectory, "legacy-routes.json");
        private static readonly Dictionary<Serial, AIGMLegacyTravelState> States = new Dictionary<Serial, AIGMLegacyTravelState>();
        private static readonly Dictionary<Serial, LegacyTravelTimer> Timers = new Dictionary<Serial, LegacyTravelTimer>();

        [DataContract]
        private sealed class LegacyRouteMetadataFile
        {
            [DataMember(Name = "routes")]
            public LegacyRouteMetadataEntry[] Routes { get; set; }
        }

        [DataContract]
        private sealed class LegacyRouteMetadataEntry
        {
            [DataMember(Name = "destination")]
            public string Destination { get; set; }

            [DataMember(Name = "aliases")]
            public string[] Aliases { get; set; }

            [DataMember(Name = "completionRadius")]
            public int CompletionRadius { get; set; }
        }

        public static bool StartTravel(BaseHire companion, AIGMNavigationLocation location, int arrivalRadius, Mobile requester, out string response)
        {
            response = null;

            if (location == null)
            {
                response = "No destination was supplied.";
                return false;
            }

            return StartTravel(companion, requester, location.Point, location.Map, location.DisplayName, arrivalRadius, out response);
        }

        public static bool StartTravel(BaseHire companion, Mobile requester, Point3D destination, Map map, string destinationName, int arrivalRadius, out string response)
        {
            response = null;

            if (companion == null || companion.Deleted || companion.Map == null)
            {
                response = "I cannot travel just now.";
                return false;
            }

            requester = requester ?? companion.GetOwner();
            if (requester == null)
            {
                response = "I need an owner before I can travel on command.";
                return false;
            }

            string operationalRejection;
            if (!AIGMOperationalControlService.BeginExplicitCommand(companion, requester, "travel", out operationalRejection))
            {
                response = "Operational control rejected travel: " + operationalRejection + ".";
                return false;
            }

            Map destinationMap = map ?? companion.Map;
            if (destinationMap != companion.Map)
            {
                response = "I can only travel on my current map right now.";
                return false;
            }

            Point3D adjusted = NormalizeDestination(destinationMap, destination);
            if (!CanOccupy(destinationMap, adjusted))
            {
                response = String.Format("The destination {0},{1},{2} is not a valid standing location.", adjusted.X, adjusted.Y, adjusted.Z);
                return false;
            }

            string ignored;
            AIGMMovementLease currentLease = AIGMMovementOwnershipService.GetCurrentLease(companion);
            if (currentLease != null && currentLease.OwnerType == AIGMMovementLeaseOwnerType.Travel)
                Stop(companion, "legacy_travel_replaced", out ignored);

            string label = String.IsNullOrWhiteSpace(destinationName) ? FormatPoint(adjusted) : destinationName.Trim();
            int radius = Math.Max(1, arrivalRadius);
            int distance = Distance2D(companion.Location, adjusted);

            AIGMLegacyTravelState state = GetOrCreateState(companion);
            state.Reset();
            state.CompanionSerial = companion.Serial;
            state.OwnerSerial = requester.Serial;
            state.Mode = AIGMLegacyTravelMode.Traveling;
            state.Destination = adjusted;
            state.DestinationName = label;
            state.FinalDestination = adjusted;
            state.FinalDestinationName = label;
            state.Map = destinationMap;
            state.ArrivalRadius = radius;
            state.StartedUtc = DateTime.UtcNow;
            state.LastProgressUtc = DateTime.UtcNow;
            state.LastObservedLocation = companion.Location;
            state.LastDistance = distance;
            state.OriginalActiveSpeed = companion.ActiveSpeed;
            state.OriginalPassiveSpeed = companion.PassiveSpeed;
            state.TravelSpeedApplied = false;
            state.CurrentStrategy = AIGMLegacyTravelStrategy.DirectStep;
            state.LastStatus = "started";

            ApplySoftWaypoints(companion, state);

            AIGMMovementLease lease;
            string rejectionReason;
            if (!AIGMMovementOwnershipService.TryAcquireOrReplace(
                companion,
                AIGMMovementLeaseOwnerType.Travel,
                requester,
                state.RouteId,
                AIGMMovementOwnershipService.GetDefaultPolicy(AIGMMovementLeaseOwnerType.Travel),
                "travel_start",
                true,
                out lease,
                out rejectionReason))
            {
                state.Reset();
                response = String.Format("{0} cannot take travel ownership right now ({1}).", companion.Name ?? "Companion", SafeLog(rejectionReason));
                return false;
            }

            try { AIGMSmartMovementService.Stop(companion, "legacy_travel_takeover", out ignored); } catch { }
            try { AIGMNativeNavigationService.Stop(companion, "legacy_travel_takeover", out ignored); } catch { }

            state.TravelLeaseId = lease != null ? lease.LeaseId : null;
            ClaimLegacyMovementOwnership(companion, state, "start_travel");

            AIGMExecutionLog.Write(
                "AIGM_LEGACY_TRAVEL_START companion={0} owner={1} destination=\"{2}\" currentPoint={3} finalTarget={4} distance={5} arrivalRadius={6} movementPrimitive=PathFollowerPreferred graphPlannerInvoked=False beaconInvoked=False",
                Describe(companion),
                Describe(requester),
                SafeLog(label),
                FormatPoint(companion.Location),
                FormatPoint(adjusted),
                distance,
                radius);

            if (distance <= radius)
            {
                Finish(companion, state, AIGMLegacyTravelMode.Arrived, "already_near", true, true);
                response = "I am already near " + label + ".";
                return true;
            }

            StartTimer(companion, requester);
            response = companion.Name + " is traveling toward " + label + " using old-dev general travel.";
            return true;
        }

        public static bool Stop(Mobile actor, string reason, out string response)
        {
            response = null;
            BaseHire companion = actor as BaseHire;
            if (companion == null)
            {
                response = "No legacy travel actor was supplied.";
                return false;
            }

            AIGMLegacyTravelState state;
            if (!States.TryGetValue(companion.Serial, out state) || state == null || state.Mode == AIGMLegacyTravelMode.None)
            {
                response = "No legacy travel is active.";
                return false;
            }

            Finish(companion, state, AIGMLegacyTravelMode.Stopped, String.IsNullOrWhiteSpace(reason) ? "stopped" : reason, false, true);
            response = "Legacy companion travel stopped.";
            return true;
        }

        public static bool IsTraveling(Mobile actor)
        {
            BaseHire companion = actor as BaseHire;
            if (companion == null)
                return false;

            AIGMLegacyTravelState state;
            return States.TryGetValue(companion.Serial, out state) && state != null && state.Mode == AIGMLegacyTravelMode.Traveling;
        }

        public static string GetStatus(Mobile actor)
        {
            BaseHire companion = actor as BaseHire;
            if (companion == null)
                return "legacyTravel=invalid";

            AIGMLegacyTravelState state;
            if (!States.TryGetValue(companion.Serial, out state) || state == null || state.Mode == AIGMLegacyTravelMode.None)
                return "legacyTravel=inactive";

            return String.Format(
                "legacyTravel={0}; destination={1}; target={2}; distance={3}; strategy={4}; status={5}",
                state.Mode,
                state.FinalDestinationName,
                FormatPoint(state.FinalDestination),
                Distance2D(companion.Location, state.FinalDestination),
                state.CurrentStrategy,
                state.LastStatus);
        }

        private static void StartTimer(BaseHire companion, Mobile owner)
        {
            if (companion == null || owner == null)
                return;

            LegacyTravelTimer old;
            if (Timers.TryGetValue(companion.Serial, out old) && old != null)
                old.Stop();

            LegacyTravelTimer timer = new LegacyTravelTimer(companion.Serial, owner.Serial, AIGMOperationalControlService.GetEpoch(companion));
            Timers[companion.Serial] = timer;
            timer.Start();
        }

        private static void Tick(BaseHire companion, Mobile owner, int expectedEpoch)
        {
            if (companion == null || companion.Deleted || companion.Map == null)
                return;

            if (!AIGMOperationalControlService.CanOperate(companion, AIGMOperationalAction.Travel, expectedEpoch, false))
            {
                AIGMLegacyTravelState blockedState;
                if (States.TryGetValue(companion.Serial, out blockedState) && blockedState != null)
                    Finish(companion, blockedState, AIGMLegacyTravelMode.Stopped, "operational_control_blocked", false, true);
                return;
            }

            AIGMLegacyTravelState state;
            if (!States.TryGetValue(companion.Serial, out state) || state == null || state.Mode != AIGMLegacyTravelMode.Traveling)
                return;

            if (state.Map == null || companion.Map != state.Map)
            {
                Finish(companion, state, AIGMLegacyTravelMode.Invalid, "map_mismatch", true, true);
                return;
            }

            if (!AIGMMovementOwnershipService.Heartbeat(companion, AIGMMovementLeaseOwnerType.Travel, owner, state.RouteId, "legacy_travel_tick"))
            {
                AIGMExecutionLog.Write(
                    "AIGM_LEGACY_TRAVEL_LEASE_LOST companion={0} destination=\"{1}\" finalTarget={2} reason=lease_not_owned",
                    Describe(companion),
                    SafeLog(state.FinalDestinationName),
                    FormatPoint(state.FinalDestination));
                Finish(companion, state, AIGMLegacyTravelMode.Stopped, "lease_lost", false, true);
                return;
            }

            if (!state.TravelSpeedApplied)
            {
                state.OriginalActiveSpeed = companion.ActiveSpeed;
                state.OriginalPassiveSpeed = companion.PassiveSpeed;
                companion.ActiveSpeed = 0.15;
                companion.PassiveSpeed = 0.30;
                state.TravelSpeedApplied = true;
            }

            ClaimLegacyMovementOwnership(companion, state, "tick");

            if (TryFinishExplicitRouteArrival(companion, state, "completion_radius"))
                return;

            if (IsRouteStartDelayed(state))
            {
                state.LastStatus = "Waiting for staggered route start";
                LogRouteProgress(companion, state, GetCurrentMoveTarget(state));
                return;
            }

            if (state.UsingDetourWaypoint && Distance2D(companion.Location, state.Destination) <= state.ArrivalRadius)
            {
                HandleDetourArrival(companion, state);
                return;
            }

            AdvanceSoftWaypointIfReached(companion, state);
            if (TryFinishExplicitRouteArrival(companion, state, "completion_radius"))
                return;

            Point3D destination = GetCurrentMoveTarget(state);
            LogRouteProgress(companion, state, destination);

            if (!state.UsingDetourWaypoint && !HasActiveSoftWaypoint(state) && Distance2D(companion.Location, state.FinalDestination) <= state.ArrivalRadius)
            {
                Finish(companion, state, AIGMLegacyTravelMode.Arrived, "arrived", true, true);
                return;
            }

            bool shouldRun = ShouldRun(companion, destination);
            RememberBreadcrumb(state, companion.Location);
            HandleOscillation(companion, state, destination);

            bool moved = false;
            bool pathFollowerAttempted = false;

            if (TryRoutePathFollowerStep(companion, state, destination, shouldRun, out pathFollowerAttempted))
            {
                moved = true;
                state.CurrentStrategy = AIGMLegacyTravelStrategy.PathFollower;
            }
            else if (pathFollowerAttempted)
            {
                AIGMExecutionLog.Write(
                    "AIGM_LEGACY_ROUTE_DIRECT_FALLBACK companion={0} destination=\"{1}\" route={2} index={3} target={4} reason=path_follower_failed",
                    Describe(companion),
                    SafeLog(state.FinalDestinationName),
                    SafeLog(GetPathFollowerRouteId(state)),
                    GetPathFollowerRouteIndex(state),
                    FormatPoint(destination));

                moved = StepToward(companion, destination, shouldRun);
                state.CurrentStrategy = AIGMLegacyTravelStrategy.DirectStep;
            }
            else
            {
                moved = StepToward(companion, destination, shouldRun);
                state.CurrentStrategy = AIGMLegacyTravelStrategy.DirectStep;
            }

            if (moved && companion.Location != state.LastObservedLocation)
            {
                OnMovementProgress(companion, state, destination, shouldRun);
                return;
            }

            OnMovementBlocked(companion, owner, state, destination, shouldRun);
        }

        private static void HandleDetourArrival(BaseHire companion, AIGMLegacyTravelState state)
        {
            if (state.DetourCommitmentPulsesRemaining > 0 && state.SuccessfulDetourMoves < 2)
            {
                state.DetourCommitmentPulsesRemaining--;
                state.LastStatus = "Holding detour band before final rejoin";
                return;
            }

            if (state.CurrentStrategy == AIGMLegacyTravelStrategy.CommittedDetour || state.CurrentStrategy == AIGMLegacyTravelStrategy.BreadcrumbBacktrack)
                state.FailedWaypoints.Add(state.ActiveDetourPoint == Point3D.Zero ? state.Destination : state.ActiveDetourPoint);

            state.UsingDetourWaypoint = false;
            state.ActiveRouteBand = null;
            state.ActiveDetourPoint = Point3D.Zero;
            state.SuccessfulDetourMoves = 0;
            state.DetourStartLocation = Point3D.Zero;
            state.StuckTicks = 0;
            state.PathFailureCount = 0;
            state.CurrentStrategy = AIGMLegacyTravelStrategy.DirectStep;
            state.LastStatus = "Detour complete, resuming final route";

            RestoreCurrentSoftTarget(companion, state, "detour_complete");
        }

        private static void ApplySoftWaypoints(BaseHire companion, AIGMLegacyTravelState state)
        {
            if (companion == null || companion.Deleted || state == null)
                return;

            AIGMLegacyWaypointRoute route = AIGMLegacyTravelWaypointService.Resolve(companion, state.FinalDestination, state.Map, state.FinalDestinationName);
            if (route == null || route.Waypoints == null || route.Waypoints.Count == 0)
            {
                state.SoftWaypoints.Clear();
                state.SoftWaypointIds.Clear();
                state.SoftWaypointLabels.Clear();
                state.CurrentWaypointIndex = -1;
                state.CurrentMoveTarget = state.FinalDestination;
                state.Destination = state.FinalDestination;
                state.DestinationName = state.FinalDestinationName;
                state.ExplicitRouteActive = false;
                state.WalkedRouteActive = false;
                state.StopAtLastWaypoint = false;
                state.RouteId = null;
                state.RouteTerminalName = null;
                state.RouteCompletionRadius = 0;
                state.RouteLastWaypoint = Point3D.Zero;
                state.StartDelayUntilUtc = DateTime.MinValue;
                return;
            }

            string routeId = ResolveRouteId(route, state.FinalDestinationName);
            bool walkedRoute = IsWalkedRoute(route);
            bool preserveExactWaypoints = walkedRoute || route.PreserveExactWaypoints;

            state.SoftWaypoints.Clear();
            state.SoftWaypointIds.Clear();
            state.SoftWaypointLabels.Clear();
            for (int i = 0; i < route.Waypoints.Count; i++)
            {
                state.SoftWaypoints.Add(preserveExactWaypoints ? route.Waypoints[i] : OffsetSoftWaypoint(companion, state.Map, route.Waypoints[i], i));
                state.SoftWaypointIds.Add(route.WaypointIds != null && i < route.WaypointIds.Count ? route.WaypointIds[i] : ("waypoint_" + (i + 1).ToString()));
                state.SoftWaypointLabels.Add(route.WaypointLabels != null && i < route.WaypointLabels.Count ? route.WaypointLabels[i] : null);
            }

            state.ExplicitRouteActive = true;
            state.WalkedRouteActive = walkedRoute;
            state.StopAtLastWaypoint = route.StopAtLastWaypoint;
            state.RouteId = routeId;
            state.RouteTerminalName = route.WaypointLabels != null && route.WaypointLabels.Count > 0 ? route.WaypointLabels[route.WaypointLabels.Count - 1] : state.FinalDestinationName;
            state.RouteCompletionRadius = route.CompletionRadius > 0 ? route.CompletionRadius : ResolveRouteCompletionRadius(state.FinalDestinationName);
            state.RouteLastWaypoint = ResolveLastWalkedRouteWaypoint(route);
            state.RouteRecoveryOffsetAttempts = 0;
            state.RouteRecoverySkipAttempts = 0;
            state.RouteRecoveryCatchupAttempts = 0;
            state.WalkedRouteSidestepAttempts = 0;
            state.CurrentWaypointIndex = 0;
            state.CurrentMoveTarget = state.SoftWaypoints[0];
            state.Destination = state.CurrentMoveTarget;
            state.DestinationName = BuildWaypointName(route, 0);
            state.LastStatus = "Moving toward " + state.DestinationName;

            if (walkedRoute)
            {
                int startIndex = ResolveRouteStartIndex(companion);
                int delayMs = Math.Max(0, startIndex) * RouteStartStaggerMs;
                state.RouteStartIndex = startIndex;
                state.StartDelayUntilUtc = DateTime.UtcNow + TimeSpan.FromMilliseconds(delayMs);

                AIGMExecutionLog.Write(
                    "AIGM_LEGACY_ROUTE_OFFSET_POLICY companion={0} route={1} mode=disabled reason=walked_route",
                    Describe(companion),
                    SafeLog(routeId));
                AIGMExecutionLog.Write(
                    "AIGM_LEGACY_ROUTE_STAGGER companion={0} index={1} delayMs={2}",
                    Describe(companion),
                    startIndex,
                    delayMs);
            }

            LogWaypointRoutePreview(companion, state, route);
            LogWaypointTarget(companion, state);
        }

        private static void AdvanceSoftWaypointIfReached(BaseHire companion, AIGMLegacyTravelState state)
        {
            if (!HasActiveSoftWaypoint(state))
                return;

            if (state.WalkedRouteActive)
            {
                AdvanceWalkedRouteIfReady(companion, state);
                return;
            }

            if (Distance2D(companion.Location, state.CurrentMoveTarget) > SoftWaypointArrivalRadius)
                return;

            int oldIndex = state.CurrentWaypointIndex;
            int nextIndex = oldIndex + 1;
            if (nextIndex < state.SoftWaypoints.Count)
            {
                SetSoftWaypointTarget(companion, state, nextIndex, true);
                AIGMExecutionLog.Write(
                    "AIGM_LEGACY_WAYPOINT_ADVANCE companion={0} oldIndex={1} newIndex={2} next={3} final={4}",
                    Describe(companion),
                    oldIndex,
                    nextIndex,
                    FormatPoint(state.CurrentMoveTarget),
                    FormatPoint(state.FinalDestination));
                return;
            }

            if (state.StopAtLastWaypoint)
            {
                state.CurrentWaypointIndex = state.SoftWaypoints.Count;
                state.LastStatus = "Reached mapped frontier";
                AIGMExecutionLog.Write(
                    "AIGM_LEGACY_WAYPOINT_TERMINAL companion={0} route={1} mode=frontier_stop terminal={2} final={3}",
                    Describe(companion),
                    SafeLog(state.RouteId),
                    SafeLog(state.RouteTerminalName),
                    FormatPoint(state.FinalDestination));
                Finish(companion, state, AIGMLegacyTravelMode.Arrived, "mapped_frontier_arrived", true, true);
                return;
            }

            state.CurrentWaypointIndex = state.SoftWaypoints.Count;
            state.CurrentMoveTarget = state.FinalDestination;
            state.Destination = state.FinalDestination;
            state.DestinationName = state.FinalDestinationName;
            ResetTargetCounters(state);

            AIGMExecutionLog.Write(
                "AIGM_LEGACY_WAYPOINT_ADVANCE companion={0} oldIndex={1} newIndex={2} next={3} final={4}",
                Describe(companion),
                oldIndex,
                state.CurrentWaypointIndex,
                FormatPoint(state.FinalDestination),
                FormatPoint(state.FinalDestination));
            AIGMExecutionLog.Write(
                "AIGM_LEGACY_WAYPOINT_FINAL_TARGET companion={0} final={1}",
                Describe(companion),
                FormatPoint(state.FinalDestination));
        }

        private static void AdvanceWalkedRouteIfReady(BaseHire companion, AIGMLegacyTravelState state)
        {
            if (companion == null || state == null || !HasActiveSoftWaypoint(state))
                return;

            int currentDistance = Distance2D(companion.Location, state.CurrentMoveTarget);
            bool shouldAdvance = currentDistance <= WalkedRouteAdvanceRadius;
            string reason = "near_node";

            if (!shouldAdvance)
            {
                int nextProbeIndex = state.CurrentWaypointIndex + 1;
                if (state.SoftWaypoints != null && nextProbeIndex < state.SoftWaypoints.Count)
                {
                    int nextDistance = Distance2D(companion.Location, state.SoftWaypoints[nextProbeIndex]);
                    if (nextDistance < currentDistance)
                    {
                        if (currentDistance <= WalkedRouteRecoveryNearNodeRadius)
                        {
                            shouldAdvance = true;
                            reason = "closer_to_next";
                        }
                        else
                        {
                            AIGMExecutionLog.Write(
                                "AIGM_LEGACY_WALKED_ROUTE_ADVANCE_BLOCKED reason=not_anchored companion={0} route={1} fromIndex={2} currentDistance={3} nextDistance={4} currentTarget={5}",
                                Describe(companion),
                                SafeLog(state.RouteId),
                                state.CurrentWaypointIndex,
                                currentDistance,
                                nextDistance,
                                FormatPoint(state.CurrentMoveTarget));
                        }
                    }
                }
            }

            if (!shouldAdvance)
                return;

            int oldIndex = state.CurrentWaypointIndex;
            int nextIndex = oldIndex + 1;
            if (nextIndex < state.SoftWaypoints.Count)
            {
                SetSoftWaypointTarget(companion, state, nextIndex, true);
                LogWalkedRouteAdvance(companion, state, oldIndex, nextIndex, reason, currentDistance);
                AIGMExecutionLog.Write(
                    "AIGM_LEGACY_WAYPOINT_ADVANCE companion={0} oldIndex={1} newIndex={2} next={3} final={4}",
                    Describe(companion),
                    oldIndex,
                    nextIndex,
                    FormatPoint(state.CurrentMoveTarget),
                    FormatPoint(state.FinalDestination));
                return;
            }

            if (state.StopAtLastWaypoint)
            {
                state.CurrentWaypointIndex = state.SoftWaypoints.Count;
                state.LastStatus = "Reached mapped frontier";
                LogWalkedRouteAdvance(companion, state, oldIndex, state.CurrentWaypointIndex, reason, currentDistance);
                AIGMExecutionLog.Write(
                    "AIGM_LEGACY_WAYPOINT_TERMINAL companion={0} route={1} mode=frontier_stop terminal={2} final={3}",
                    Describe(companion),
                    SafeLog(state.RouteId),
                    SafeLog(state.RouteTerminalName),
                    FormatPoint(state.FinalDestination));
                Finish(companion, state, AIGMLegacyTravelMode.Arrived, "mapped_frontier_arrived", true, true);
                return;
            }

            state.CurrentWaypointIndex = state.SoftWaypoints.Count;
            state.CurrentMoveTarget = state.FinalDestination;
            state.Destination = state.FinalDestination;
            state.DestinationName = state.FinalDestinationName;
            ResetTargetCounters(state);

            LogWalkedRouteAdvance(companion, state, oldIndex, state.CurrentWaypointIndex, reason, currentDistance);
            AIGMExecutionLog.Write(
                "AIGM_LEGACY_WAYPOINT_ADVANCE companion={0} oldIndex={1} newIndex={2} next={3} final={4}",
                Describe(companion),
                oldIndex,
                state.CurrentWaypointIndex,
                FormatPoint(state.FinalDestination),
                FormatPoint(state.FinalDestination));
            AIGMExecutionLog.Write(
                "AIGM_LEGACY_WAYPOINT_FINAL_TARGET companion={0} final={1}",
                Describe(companion),
                FormatPoint(state.FinalDestination));
        }

        private static void RestoreCurrentSoftTarget(BaseHire companion, AIGMLegacyTravelState state, string reason)
        {
            if (state == null)
                return;

            if (HasActiveSoftWaypoint(state))
            {
                state.CurrentMoveTarget = state.SoftWaypoints[state.CurrentWaypointIndex];
                state.Destination = state.CurrentMoveTarget;
                state.DestinationName = BuildWaypointName(state, state.CurrentWaypointIndex);
                LogWaypointTarget(companion, state);
                return;
            }

            state.CurrentMoveTarget = state.FinalDestination;
            state.Destination = state.FinalDestination;
            state.DestinationName = state.FinalDestinationName;
            if (!String.IsNullOrWhiteSpace(reason))
                state.LastStatus = "Moving toward final destination";
        }

        private static bool HasActiveSoftWaypoint(AIGMLegacyTravelState state)
        {
            return state != null
                && state.SoftWaypoints != null
                && state.CurrentWaypointIndex >= 0
                && state.CurrentWaypointIndex < state.SoftWaypoints.Count;
        }

        private static string BuildWaypointName(AIGMLegacyWaypointRoute route, int index)
        {
            if (route != null && route.WaypointLabels != null && index >= 0 && index < route.WaypointLabels.Count && !String.IsNullOrWhiteSpace(route.WaypointLabels[index]))
                return route.WaypointLabels[index];

            return "soft waypoint " + (index + 1).ToString();
        }

        private static string BuildWaypointName(AIGMLegacyTravelState state, int index)
        {
            if (state != null
                && state.SoftWaypointLabels != null
                && index >= 0
                && index < state.SoftWaypointLabels.Count
                && !String.IsNullOrWhiteSpace(state.SoftWaypointLabels[index]))
                return state.SoftWaypointLabels[index];

            if (state != null && state.WalkedRouteActive)
                return "walked waypoint " + (index + 1).ToString();

            return "soft waypoint " + (index + 1).ToString();
        }

        private static bool IsRouteStartDelayed(AIGMLegacyTravelState state)
        {
            return state != null
                && state.ExplicitRouteActive
                && state.StartDelayUntilUtc != DateTime.MinValue
                && DateTime.UtcNow < state.StartDelayUntilUtc;
        }

        private static string ResolveRouteId(AIGMLegacyWaypointRoute route, string destinationName)
        {
            if (route != null && route.WaypointIds != null && route.WaypointIds.Count > 0)
            {
                string first = route.WaypointIds[0];
                int marker = !String.IsNullOrWhiteSpace(first) ? first.IndexOf("_route_", StringComparison.OrdinalIgnoreCase) : -1;
                if (marker > 0)
                    return first.Substring(0, marker);
            }

            return AIGMNavNode.BuildId(destinationName);
        }

        private static bool IsWalkedRoute(AIGMLegacyWaypointRoute route)
        {
            if (route == null || route.WaypointIds == null)
                return false;

            for (int i = 0; i < route.WaypointIds.Count; i++)
            {
                string id = route.WaypointIds[i];
                if (!String.IsNullOrWhiteSpace(id) && id.IndexOf("_route_", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        private static int ResolveRouteStartIndex(BaseHire companion)
        {
            string key = GetCompanionRouteKey(companion);
            if (String.Equals(key, "dakeyras", StringComparison.OrdinalIgnoreCase))
                return 0;

            if (String.Equals(key, "danyal", StringComparison.OrdinalIgnoreCase))
                return 1;

            if (String.Equals(key, "dardalion", StringComparison.OrdinalIgnoreCase))
                return 2;

            return companion != null ? Math.Abs(companion.Serial.Value) % 3 : 0;
        }

        private static string GetCompanionRouteKey(BaseHire companion)
        {
            IAIGMCompanionActor actor = companion as IAIGMCompanionActor;
            if (actor != null && !String.IsNullOrWhiteSpace(actor.CompanionId))
                return actor.CompanionId.Trim().ToLowerInvariant();

            return companion != null && !String.IsNullOrWhiteSpace(companion.Name) ? companion.Name.Trim().ToLowerInvariant() : String.Empty;
        }

        private static void LogWalkedRouteAdvance(BaseHire companion, AIGMLegacyTravelState state, int oldIndex, int newIndex, string reason, int distance)
        {
            AIGMExecutionLog.Write(
                "AIGM_LEGACY_WALKED_ROUTE_ADVANCE companion={0} oldIndex={1} newIndex={2} reason={3} distance={4}",
                Describe(companion),
                oldIndex,
                newIndex,
                SafeLog(reason),
                distance);
        }

        private static int ResolveRouteCompletionRadius(string destinationName)
        {
            if (String.IsNullOrWhiteSpace(destinationName) || !File.Exists(LegacyRoutesPath))
                return 0;

            try
            {
                using (FileStream stream = File.Open(LegacyRoutesPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(LegacyRouteMetadataFile));
                    LegacyRouteMetadataFile file = serializer.ReadObject(stream) as LegacyRouteMetadataFile;
                    if (file == null || file.Routes == null)
                        return 0;

                    string normalized = AIGMNavNode.Normalize(destinationName);
                    for (int i = 0; i < file.Routes.Length; i++)
                    {
                        LegacyRouteMetadataEntry entry = file.Routes[i];
                        if (entry == null)
                            continue;

                        if (AIGMNavNode.Normalize(entry.Destination) == normalized || AliasMatches(entry.Aliases, normalized))
                            return Math.Max(0, entry.CompletionRadius);
                    }
                }
            }
            catch (Exception ex)
            {
                AIGMExecutionLog.Write("AIGM_LEGACY_ROUTE_METADATA_LOAD_ERROR source=\"{0}\" error=\"{1}\"", SafeLog(LegacyRoutesPath), SafeLog(ex.Message));
            }

            return 0;
        }

        private static bool AliasMatches(string[] aliases, string normalized)
        {
            for (int i = 0; aliases != null && i < aliases.Length; i++)
            {
                if (AIGMNavNode.Normalize(aliases[i]) == normalized)
                    return true;
            }

            return false;
        }

        private static Point3D ResolveLastWalkedRouteWaypoint(AIGMLegacyWaypointRoute route)
        {
            if (route == null || route.Waypoints == null || route.Waypoints.Count == 0)
                return Point3D.Zero;

            if (route.Waypoints.Count >= 2)
                return route.Waypoints[route.Waypoints.Count - 2];

            return route.Waypoints[route.Waypoints.Count - 1];
        }

        private static bool TryFinishExplicitRouteArrival(BaseHire companion, AIGMLegacyTravelState state, string reason)
        {
            int finalDistance;
            int lastNodeDistance;
            if (!IsWithinExplicitRouteCompletion(companion, state, out finalDistance, out lastNodeDistance))
                return false;

            AIGMExecutionLog.Write(
                "AIGM_LEGACY_ROUTE_ARRIVAL companion={0} reason={1} loc={2} final={3} distance={4} completionRadius={5}",
                Describe(companion),
                SafeLog(String.IsNullOrWhiteSpace(reason) ? "completion_radius" : reason),
                FormatPoint(companion.Location),
                FormatPoint(state.FinalDestination),
                finalDistance,
                state.RouteCompletionRadius);

            Finish(companion, state, AIGMLegacyTravelMode.Arrived, "route_completion_radius", true, true);
            return true;
        }

        private static bool TryConvertRouteBlockToArrival(BaseHire companion, AIGMLegacyTravelState state)
        {
            int finalDistance;
            int lastNodeDistance;
            if (!IsWithinExplicitRouteCompletion(companion, state, out finalDistance, out lastNodeDistance))
                return false;

            AIGMExecutionLog.Write(
                "AIGM_LEGACY_ROUTE_BLOCK_CONVERTED_TO_ARRIVAL companion={0} loc={1} finalDistance={2}",
                Describe(companion),
                FormatPoint(companion.Location),
                finalDistance);

            return TryFinishExplicitRouteArrival(companion, state, "completion_radius");
        }

        private static bool IsWithinExplicitRouteCompletion(BaseHire companion, AIGMLegacyTravelState state, out int finalDistance, out int lastNodeDistance)
        {
            finalDistance = -1;
            lastNodeDistance = -1;

            if (companion == null || state == null || !state.ExplicitRouteActive || state.RouteCompletionRadius <= 0)
                return false;

            finalDistance = Distance2D(companion.Location, state.FinalDestination);
            if (finalDistance <= state.RouteCompletionRadius)
                return true;

            if (state.RouteLastWaypoint != Point3D.Zero)
            {
                lastNodeDistance = Distance2D(companion.Location, state.RouteLastWaypoint);
                if (lastNodeDistance <= Math.Max(state.ArrivalRadius, SoftWaypointArrivalRadius) && finalDistance <= state.RouteCompletionRadius)
                    return true;
            }

            if (state.SoftWaypoints != null && state.CurrentWaypointIndex >= state.SoftWaypoints.Count - 1 && finalDistance <= state.RouteCompletionRadius)
                return true;

            return false;
        }

        private static void LogRouteProgress(BaseHire companion, AIGMLegacyTravelState state, Point3D target)
        {
            if (companion == null || state == null || !state.ExplicitRouteActive)
                return;

            AIGMExecutionLog.Write(
                "AIGM_LEGACY_ROUTE_PROGRESS companion={0} index={1} target={2} loc={3} finalDistance={4} targetDistance={5}",
                Describe(companion),
                state.CurrentWaypointIndex,
                FormatPoint(target),
                FormatPoint(companion.Location),
                Distance2D(companion.Location, state.FinalDestination),
                Distance2D(companion.Location, target));
        }

        private static Point3D GetCurrentMoveTarget(AIGMLegacyTravelState state)
        {
            if (state == null)
                return Point3D.Zero;

            if (state.UsingDetourWaypoint)
                return state.Destination;

            if (state.CurrentMoveTarget != Point3D.Zero)
                return state.CurrentMoveTarget;

            return state.FinalDestination;
        }

        private static void SetSoftWaypointTarget(BaseHire companion, AIGMLegacyTravelState state, int index, bool resetCounters)
        {
            if (state == null || state.SoftWaypoints == null || index < 0 || index >= state.SoftWaypoints.Count)
                return;

            state.CurrentWaypointIndex = index;
            state.CurrentMoveTarget = state.SoftWaypoints[index];
            state.Destination = state.CurrentMoveTarget;
            state.DestinationName = BuildWaypointName(state, index);
            state.LastStatus = "Moving toward " + state.DestinationName;

            if (resetCounters)
                ResetTargetCounters(state);

            LogWaypointTarget(companion, state);
        }

        private static void ResetTargetCounters(AIGMLegacyTravelState state)
        {
            if (state == null)
                return;

            state.StuckTicks = 0;
            state.PathFailureCount = 0;
            state.ConsecutiveBlockedSteps = 0;
            state.ConsecutiveNoProgressChecks = 0;
            state.ConsecutivePathFollowerFailures = 0;
            ResetRoutePathFollowerState(state);
            state.CurrentStrategy = AIGMLegacyTravelStrategy.DirectStep;
            state.RouteRecoveryOffsetAttempts = 0;
            state.RouteRecoverySkipAttempts = 0;
            state.RouteRecoveryCatchupAttempts = 0;
            state.WalkedRouteSidestepAttempts = 0;
        }

        private static void LogWaypointTarget(BaseHire companion, AIGMLegacyTravelState state)
        {
            if (state == null)
                return;

            AIGMExecutionLog.Write(
                "AIGM_LEGACY_WAYPOINT_TARGET companion={0} destination=\"{1}\" index={2} target={3} final={4}",
                Describe(companion),
                SafeLog(state.FinalDestinationName),
                state.CurrentWaypointIndex,
                FormatPoint(state.CurrentMoveTarget),
                FormatPoint(state.FinalDestination));
        }

        private static void LogWaypointRoutePreview(BaseHire companion, AIGMLegacyTravelState state, AIGMLegacyWaypointRoute route)
        {
            if (state == null || route == null || route.Waypoints == null || route.Waypoints.Count == 0)
                return;

            string[] ordered = new string[route.Waypoints.Count];
            for (int i = 0; i < route.Waypoints.Count; i++)
            {
                Point3D actual = i < state.SoftWaypoints.Count ? state.SoftWaypoints[i] : route.Waypoints[i];
                string id = route.WaypointIds != null && i < route.WaypointIds.Count ? route.WaypointIds[i] : ("waypoint_" + (i + 1).ToString());
                ordered[i] = id + "@" + FormatPoint(actual);
            }

            string firstTarget = ordered.Length > 0 ? ordered[0] : "none";
            AIGMExecutionLog.Write(
                "AIGM_LEGACY_WAYPOINT_ROUTE_PREVIEW destination=\"{0}\" final={1} orderedTargets={2} firstTarget={3}",
                SafeLog(state.FinalDestinationName),
                FormatPoint(state.FinalDestination),
                SafeLog(String.Join(">", ordered)),
                SafeLog(firstTarget));
        }

        private static Point3D OffsetSoftWaypoint(BaseHire companion, Map map, Point3D waypoint, int index)
        {
            if (companion == null || map == null)
                return waypoint;

            Point3D normalized = NormalizeDestination(map, waypoint);
            if (!CanOccupy(map, normalized))
                normalized = waypoint;

            int[,] offsets = new int[,]
            {
                { 0, 0 },
                { 1, 0 },
                { 0, 1 },
                { -1, 0 },
                { 0, -1 },
                { 1, 1 },
                { -1, 1 },
                { 1, -1 },
                { -1, -1 },
                { 2, 0 },
                { 0, 2 },
                { -2, 0 },
                { 0, -2 }
            };

            int count = offsets.GetLength(0);
            int serialSeed = companion.Serial.Value;
            int start = Math.Abs(serialSeed + (index * 3)) % count;
            for (int i = 0; i < count; i++)
            {
                int offsetIndex = (start + i) % count;
                Point3D candidate = NormalizeDestination(map, new Point3D(waypoint.X + offsets[offsetIndex, 0], waypoint.Y + offsets[offsetIndex, 1], waypoint.Z));
                if (CanOccupy(map, candidate))
                    return candidate;
            }

            return normalized;
        }

        private static void HandleOscillation(BaseHire companion, AIGMLegacyTravelState state, Point3D destination)
        {
            if (state == null)
                return;

            int currentDistance = Distance2D(companion.Location, state.FinalDestination);
            bool distanceImproving = state.LastDistance <= 0 || currentDistance < state.LastDistance;
            bool progressIsRecent = state.LastProgressUtc != DateTime.MinValue && DateTime.UtcNow < state.LastProgressUtc + TimeSpan.FromSeconds(4.0);

            if (distanceImproving || progressIsRecent)
            {
                AIGMExecutionLog.Write(
                    "AIGM_LEGACY_TRAVEL_OSCILLATION_SUPPRESSED companion={0} distanceImproving={1} progressIsRecent={2} currentDistance={3} lastDistance={4} lastProgressUtc={5:o}",
                    Describe(companion),
                    distanceImproving,
                    progressIsRecent,
                    currentDistance,
                    state.LastDistance,
                    state.LastProgressUtc);
                return;
            }

            if (!IsOscillating(state, companion.Location))
                return;

            state.OscillationCount++;
            AIGMExecutionLog.Write("AIGM_LEGACY_TRAVEL_OSCILLATION companion={0} count={1} location={2}", Describe(companion), state.OscillationCount, FormatPoint(companion.Location));

            if (!String.IsNullOrWhiteSpace(state.ActiveRouteBand))
            {
                state.SuppressedRouteBand = state.ActiveRouteBand;
                state.SuppressedRouteBandUntilUtc = DateTime.UtcNow + TimeSpan.FromSeconds(20.0);
            }

            Point3D backtrack = GetBreadcrumbBacktrack(state, companion.Location);
            if (backtrack != companion.Location)
            {
                state.Destination = backtrack;
                state.DestinationName = "breadcrumb backtrack";
                state.UsingDetourWaypoint = true;
                state.CurrentStrategy = AIGMLegacyTravelStrategy.BreadcrumbBacktrack;
                state.DetourCommitmentPulsesRemaining = 8;
                state.PathFailureCount = 0;
                state.StuckTicks = 0;
                state.ActiveDetourPoint = backtrack;
                state.LastStatus = "Breadcrumb backtrack";
                AIGMExecutionLog.Write("AIGM_LEGACY_TRAVEL_BACKTRACK companion={0} target={1}", Describe(companion), FormatPoint(backtrack));
            }
        }

        private static void OnMovementProgress(BaseHire companion, AIGMLegacyTravelState state, Point3D destination, bool shouldRun)
        {
            int currentDistance = Distance2D(companion.Location, state.FinalDestination);
            state.LastObservedLocation = companion.Location;
            state.LastProgressUtc = DateTime.UtcNow;
            state.StuckTicks = 0;
            state.PathFailureCount = 0;
            state.ConsecutiveBlockedSteps = 0;
            state.ConsecutiveNoProgressChecks = 0;
            state.ConsecutivePathFollowerFailures = 0;
            if (state.UsingDetourWaypoint)
                state.SuccessfulDetourMoves++;
            state.LastMoveWasRunning = shouldRun;
            state.LastDistance = currentDistance;
            state.LastStatus = String.Format("Moving toward {0}", !String.IsNullOrWhiteSpace(state.DestinationName) ? state.DestinationName : FormatPoint(destination));

            AIGMExecutionLog.Write(
                "AIGM_LEGACY_TRAVEL_TICK companion={0} destination=\"{1}\" currentPoint={2} targetPoint={3} finalTarget={4} distance={5} strategy={6} moved=True status=progress graphPlannerInvoked=False beaconInvoked=False",
                Describe(companion),
                SafeLog(state.FinalDestinationName),
                FormatPoint(companion.Location),
                FormatPoint(state.Destination),
                FormatPoint(state.FinalDestination),
                currentDistance,
                state.CurrentStrategy);
        }

        private static void OnMovementBlocked(BaseHire companion, Mobile owner, AIGMLegacyTravelState state, Point3D destination, bool shouldRun)
        {
            state.StuckTicks++;
            state.PathFailureCount++;
            state.ConsecutiveBlockedSteps++;
            state.ConsecutiveNoProgressChecks++;
            state.LastStatus = state.UsingDetourWaypoint ? "Following detour waypoint" : "Pathing around obstacle";

            AIGMExecutionLog.Write(
                "AIGM_LEGACY_TRAVEL_TICK companion={0} destination=\"{1}\" currentPoint={2} targetPoint={3} finalTarget={4} strategy={5} moved=False stuckTicks={6} pathFailureCount={7} graphPlannerInvoked=False beaconInvoked=False",
                Describe(companion),
                SafeLog(state.FinalDestinationName),
                FormatPoint(companion.Location),
                FormatPoint(state.Destination),
                FormatPoint(state.FinalDestination),
                state.CurrentStrategy,
                state.StuckTicks,
                state.PathFailureCount);

            LogRouteProgress(companion, state, destination);

            if (TryConvertRouteBlockToArrival(companion, state))
                return;

            if (state.ExplicitRouteActive && state.StuckTicks >= 3 && TryRouteStuckRecovery(companion, state, destination))
                return;

            if (!state.WalkedRouteActive && state.StuckTicks >= 3)
            {
                Point3D alternative = GetAlternativeStep(companion.Map, companion.Location, destination);
                if (alternative != companion.Location && TryMoveTo(companion, alternative, shouldRun))
                {
                    state.LastObservedLocation = companion.Location;
                    state.LastProgressUtc = DateTime.UtcNow;
                    state.StuckTicks = 0;
                    state.LastStatus = "Repathing around obstacle";
                    state.CurrentStrategy = AIGMLegacyTravelStrategy.LocalAlternateStep;
                    AIGMExecutionLog.Write("AIGM_LEGACY_TRAVEL_ALT_STEP companion={0} point={1} result=True", Describe(companion), FormatPoint(alternative));
                    NoticeOwner(companion, owner, state, "I am adjusting course around an obstacle.");
                    return;
                }
            }

            if (!state.WalkedRouteActive && state.PathFailureCount >= 5 && !state.UsingDetourWaypoint && state.DetourAttempts < 4)
            {
                Point3D detour = GetFallbackWaypoint(companion.Map, companion.Location, state.FinalDestination, state.DetourAttempts);
                if (detour != companion.Location && !ContainsNearbyFailedWaypoint(state, detour))
                {
                    string band = GetRouteBand(companion.Location, detour);
                    if (String.IsNullOrWhiteSpace(state.SuppressedRouteBand) || DateTime.UtcNow >= state.SuppressedRouteBandUntilUtc || !String.Equals(state.SuppressedRouteBand, band, StringComparison.OrdinalIgnoreCase))
                    {
                        state.Destination = detour;
                        state.DestinationName = "detour waypoint";
                        state.UsingDetourWaypoint = true;
                        state.DetourAttempts++;
                        state.ConsecutiveDetourFailures = 0;
                        state.DetourCommitmentPulsesRemaining = 10;
                        state.SuccessfulDetourMoves = 0;
                        state.DetourStartLocation = companion.Location;
                        state.ActiveDetourPoint = detour;
                        state.ActiveRouteBand = band;
                        state.StuckTicks = 0;
                        state.PathFailureCount = 0;
                        state.CurrentStrategy = AIGMLegacyTravelStrategy.CommittedDetour;
                        state.LastStatus = "Attempting detour waypoint";
                        AIGMExecutionLog.Write("AIGM_LEGACY_TRAVEL_DETOUR companion={0} band={1} point={2}", Describe(companion), SafeLog(band), FormatPoint(detour));
                        NoticeOwner(companion, owner, state, "I am adjusting course around an obstacle.");
                        return;
                    }
                }
            }

            if (state.PathFailureCount >= 8)
            {
                if (TryConvertRouteBlockToArrival(companion, state))
                    return;

                if (state.ExplicitRouteActive)
                {
                    AIGMExecutionLog.Write(
                        "AIGM_LEGACY_ROUTE_BLOCKED companion={0} loc={1} target={2} final={3} reason={4}",
                        Describe(companion),
                        FormatPoint(companion.Location),
                        FormatPoint(destination),
                        FormatPoint(state.FinalDestination),
                        state.WalkedRouteActive ? "walked_route_recovery_exhausted" : "route_recovery_exhausted");
                }

                Finish(companion, state, AIGMLegacyTravelMode.Blocked, "blocked", true, true);
            }
        }

        private static bool TryRouteStuckRecovery(BaseHire companion, AIGMLegacyTravelState state, Point3D destination)
        {
            if (companion == null || state == null || !state.ExplicitRouteActive || state.SoftWaypoints == null)
                return false;

            if (state.WalkedRouteActive)
                return TryWalkedRouteStuckRecovery(companion, state, destination);

            if (state.RouteRecoveryOffsetAttempts < 2)
            {
                Point3D offsetTarget = GetRouteOffsetTarget(companion.Map, destination, state.RouteRecoveryOffsetAttempts);
                if (offsetTarget != Point3D.Zero && offsetTarget != destination)
                {
                    state.RouteRecoveryOffsetAttempts++;
                    state.CurrentMoveTarget = offsetTarget;
                    state.Destination = offsetTarget;
                    state.DestinationName = "route recovery offset";
                    ResetBlockedCounters(state);
                    state.LastStatus = "Route recovery offset";
                    AIGMExecutionLog.Write(
                        "AIGM_LEGACY_ROUTE_STUCK_RECOVERY companion={0} mode=offset target={1}",
                        Describe(companion),
                        FormatPoint(offsetTarget));
                    LogWaypointTarget(companion, state);
                    return true;
                }
            }

            if (HasActiveSoftWaypoint(state))
            {
                int oldIndex = state.CurrentWaypointIndex;
                int nextIndex = oldIndex + 1;
                if (nextIndex < state.SoftWaypoints.Count)
                {
                    state.RouteRecoverySkipAttempts++;
                    SetSoftWaypointTarget(companion, state, nextIndex, true);
                    AIGMExecutionLog.Write(
                        "AIGM_LEGACY_ROUTE_STUCK_RECOVERY companion={0} mode=skip_next oldIndex={1} newIndex={2}",
                        Describe(companion),
                        oldIndex,
                        nextIndex);
                    return true;
                }
            }

            AIGMLegacyTravelState leaderState;
            BaseHire leader;
            if (TryFindRouteLeader(state, out leaderState, out leader) && leaderState.CurrentWaypointIndex > state.CurrentWaypointIndex)
            {
                int oldIndex = state.CurrentWaypointIndex;
                int newIndex = Math.Min(leaderState.CurrentWaypointIndex, state.SoftWaypoints.Count - 1);
                if (newIndex > oldIndex)
                {
                    state.RouteRecoveryCatchupAttempts++;
                    SetSoftWaypointTarget(companion, state, newIndex, true);
                    AIGMExecutionLog.Write(
                        "AIGM_LEGACY_ROUTE_STUCK_RECOVERY companion={0} mode=catchup leader={1} oldIndex={2} newIndex={3}",
                        Describe(companion),
                        Describe(leader),
                        oldIndex,
                        newIndex);
                    return true;
                }
            }

            return false;
        }

        private static bool TryWalkedRouteStuckRecovery(BaseHire companion, AIGMLegacyTravelState state, Point3D destination)
        {
            if (companion == null || state == null || !state.WalkedRouteActive || state.SoftWaypoints == null)
                return false;

            if (HasActiveSoftWaypoint(state))
            {
                int oldIndex = state.CurrentWaypointIndex;
                int nextIndex = oldIndex + 1;
                int targetDistance = Distance2D(companion.Location, state.CurrentMoveTarget);

                if (targetDistance <= WalkedRouteRecoveryNearNodeRadius && nextIndex < state.SoftWaypoints.Count)
                {
                    SetSoftWaypointTarget(companion, state, nextIndex, true);
                    AIGMExecutionLog.Write(
                        "AIGM_LEGACY_WALKED_ROUTE_RECOVERY companion={0} mode=advance_near_node oldIndex={1} newIndex={2}",
                        Describe(companion),
                        oldIndex,
                        nextIndex);
                    return true;
                }
            }

            AIGMLegacyTravelState leaderState;
            BaseHire leader;
            if (TryFindRouteLeader(state, out leaderState, out leader) && leaderState.CurrentWaypointIndex > state.CurrentWaypointIndex + 1)
            {
                int oldIndex = state.CurrentWaypointIndex;
                int newIndex = Math.Min(leaderState.CurrentWaypointIndex - 1, state.SoftWaypoints.Count - 1);
                if (newIndex > oldIndex)
                {
                    state.RouteRecoveryCatchupAttempts++;
                    SetSoftWaypointTarget(companion, state, newIndex, true);
                    AIGMExecutionLog.Write(
                        "AIGM_LEGACY_WALKED_ROUTE_RECOVERY companion={0} mode=catchup leader={1} oldIndex={2} newIndex={3}",
                        Describe(companion),
                        Describe(leader),
                        oldIndex,
                        newIndex);
                    return true;
                }
            }

            if (state.WalkedRouteSidestepAttempts < 1)
            {
                state.WalkedRouteSidestepAttempts++;
                Point3D sidestep = GetAlternativeStep(companion.Map, companion.Location, destination);
                if (sidestep != companion.Location)
                {
                    AIGMExecutionLog.Write(
                        "AIGM_LEGACY_WALKED_ROUTE_RECOVERY companion={0} mode=sidestep target={1}",
                        Describe(companion),
                        FormatPoint(sidestep));

                    if (TryMoveTo(companion, sidestep, ShouldRun(companion, destination)))
                    {
                        state.LastObservedLocation = companion.Location;
                        state.LastProgressUtc = DateTime.UtcNow;
                        ResetBlockedCounters(state);
                        state.CurrentStrategy = AIGMLegacyTravelStrategy.LocalAlternateStep;
                        return true;
                    }
                }
            }

            return false;
        }

        private static Point3D GetRouteOffsetTarget(Map map, Point3D target, int attempt)
        {
            if (map == null || target == Point3D.Zero)
                return Point3D.Zero;

            int radius = 2 + Math.Min(2, attempt);
            Point3D[] candidates = new Point3D[]
            {
                new Point3D(target.X + radius, target.Y, target.Z),
                new Point3D(target.X, target.Y + radius, target.Z),
                new Point3D(target.X - radius, target.Y, target.Z),
                new Point3D(target.X, target.Y - radius, target.Z),
                new Point3D(target.X + radius, target.Y + radius, target.Z),
                new Point3D(target.X - radius, target.Y + radius, target.Z),
                new Point3D(target.X + radius, target.Y - radius, target.Z),
                new Point3D(target.X - radius, target.Y - radius, target.Z)
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                Point3D normalized = NormalizeDestination(map, candidates[i]);
                if (CanOccupy(map, normalized))
                    return normalized;
            }

            return Point3D.Zero;
        }

        private static bool TryFindRouteLeader(AIGMLegacyTravelState state, out AIGMLegacyTravelState leaderState, out BaseHire leader)
        {
            leaderState = null;
            leader = null;

            if (state == null || state.CompanionSerial == Serial.MinusOne)
                return false;

            foreach (KeyValuePair<Serial, AIGMLegacyTravelState> pair in States)
            {
                AIGMLegacyTravelState candidate = pair.Value;
                if (candidate == null || candidate == state || !candidate.ExplicitRouteActive || candidate.Mode != AIGMLegacyTravelMode.Traveling)
                    continue;

                if (!String.Equals(AIGMNavNode.Normalize(candidate.FinalDestinationName), AIGMNavNode.Normalize(state.FinalDestinationName), StringComparison.Ordinal))
                    continue;

                if (leaderState == null || candidate.CurrentWaypointIndex > leaderState.CurrentWaypointIndex)
                {
                    BaseHire candidateCompanion = World.FindMobile(pair.Key) as BaseHire;
                    if (candidateCompanion == null || candidateCompanion.Deleted)
                        continue;

                    leaderState = candidate;
                    leader = candidateCompanion;
                }
            }

            return leaderState != null;
        }

        private static void ResetBlockedCounters(AIGMLegacyTravelState state)
        {
            if (state == null)
                return;

            state.StuckTicks = 0;
            state.PathFailureCount = 0;
            state.ConsecutiveBlockedSteps = 0;
            state.ConsecutiveNoProgressChecks = 0;
            state.ConsecutivePathFollowerFailures = 0;
            ResetRoutePathFollowerState(state);
            state.CurrentStrategy = AIGMLegacyTravelStrategy.LocalAlternateStep;
        }

        private static bool StepToward(BaseCreature actor, Point3D destination, bool shouldRun)
        {
            if (!AIGMOperationalControlService.CanOperate(actor, AIGMOperationalAction.Travel, AIGMOperationalControlService.GetEpoch(actor), false))
                return false;

            Point3D[] candidates = BuildStepCandidates(actor, destination);
            for (int i = 0; i < candidates.Length; i++)
            {
                Point3D candidate = candidates[i];
                if (candidate != actor.Location && TryMoveTo(actor, candidate, shouldRun))
                    return true;
            }

            return false;
        }

        private static Point3D[] BuildStepCandidates(BaseCreature actor, Point3D destination)
        {
            int dx = Math.Sign(destination.X - actor.X);
            int dy = Math.Sign(destination.Y - actor.Y);

            Point3D primary = NormalizeDestination(actor.Map, new Point3D(actor.X + dx, actor.Y + dy, actor.Z));
            Point3D xOnly = NormalizeDestination(actor.Map, new Point3D(actor.X + dx, actor.Y, actor.Z));
            Point3D yOnly = NormalizeDestination(actor.Map, new Point3D(actor.X, actor.Y + dy, actor.Z));
            Point3D sideA = NormalizeDestination(actor.Map, new Point3D(actor.X + dx, actor.Y - dy, actor.Z));
            Point3D sideB = NormalizeDestination(actor.Map, new Point3D(actor.X - dx, actor.Y + dy, actor.Z));

            return new Point3D[] { primary, xOnly, yOnly, sideA, sideB };
        }

        private static bool TryMoveTo(BaseCreature actor, Point3D candidate, bool shouldRun)
        {
            if (actor == null || actor.Map == null || candidate == actor.Location || !CanOccupy(actor.Map, candidate))
                return false;

            BaseHire companion = actor as BaseHire;
            AIGMLegacyTravelState state = companion != null ? GetState(companion) : null;
            if (companion != null && !AIGMMovementOwnershipService.IsCurrentOwner(companion, AIGMMovementLeaseOwnerType.Travel))
                return false;

            if (companion != null && state != null)
                ClaimLegacyMovementOwnership(companion, state, "manual_move");

            Point3D before = actor.Location;
            if (companion != null)
                LogOwnershipProbeBefore(companion, before);

            Direction dir = actor.GetDirectionTo(candidate);
            if (shouldRun)
                dir |= Direction.Running;

            bool moved = TryMoveWithAI(actor, dir);
            Point3D after = actor.Location;

            if (companion != null)
            {
                LogOwnershipProbeAfter(companion, before, after, moved);
                ScheduleOwnershipProbeAfterDelay(companion.Serial, candidate);
            }

            if (!moved)
                return false;

            actor.ProcessDelta();
            return true;
        }

        private static PathFollower CreatePathFollower(BaseCreature actor, IPoint3D goal)
        {
            PathFollower follower = new PathFollower(actor, goal);

            if (actor != null && actor.AIObject != null)
                follower.Mover = actor.AIObject.DoMoveImpl;

            return follower;
        }

        private static bool TryMoveWithAI(BaseCreature actor, Direction dir)
        {
            if (actor == null)
                return false;

            if (actor.AIObject != null)
            {
                MoveResult result = actor.AIObject.DoMoveImpl(dir);
                return result == MoveResult.Success || result == MoveResult.SuccessAutoTurn;
            }

            return actor.Move(dir);
        }

        private static void ResetRoutePathFollowerState(AIGMLegacyTravelState state)
        {
            if (state == null)
                return;

            state.ActivePathFollower = null;
            state.ActivePathFollowerRouteId = null;
            state.ActivePathFollowerRouteIndex = -1;
            state.ActivePathFollowerTarget = Point3D.Zero;
            state.ActivePathFollowerLastLocation = Point3D.Zero;
            state.ActivePathFollowerLastProgressUtc = DateTime.MinValue;
            state.LastPathFollowerRepathUtc = DateTime.MinValue;
        }

        private static string GetPathFollowerRouteId(AIGMLegacyTravelState state)
        {
            if (state == null)
                return "legacy_direct_step";

            if (!String.IsNullOrWhiteSpace(state.RouteId))
                return state.RouteId;

            if (state.UsingDetourWaypoint)
                return "legacy_detour_step";

            return "legacy_direct_step";
        }

        private static int GetPathFollowerRouteIndex(AIGMLegacyTravelState state)
        {
            return state != null && state.ExplicitRouteActive ? state.CurrentWaypointIndex : -1;
        }

        private static bool TryRoutePathFollowerStep(BaseHire companion, AIGMLegacyTravelState state, Point3D goal, bool run, out bool attempted)
        {
            attempted = false;

            if (companion == null || companion.Map == null || state == null)
                return false;

            if (!AIGMOperationalControlService.CanOperate(companion, AIGMOperationalAction.Travel, AIGMOperationalControlService.GetEpoch(companion), false))
                return false;

            attempted = true;

            string routeId = GetPathFollowerRouteId(state);
            int routeIndex = GetPathFollowerRouteIndex(state);
            bool targetChanged =
                state.ActivePathFollower == null
                || !String.Equals(state.ActivePathFollowerRouteId ?? String.Empty, routeId, StringComparison.Ordinal)
                || state.ActivePathFollowerRouteIndex != routeIndex
                || state.ActivePathFollowerTarget != goal;

            if (targetChanged)
            {
                state.ActivePathFollower = CreatePathFollower(companion, goal);
                state.ActivePathFollowerRouteId = routeId;
                state.ActivePathFollowerRouteIndex = routeIndex;
                state.ActivePathFollowerTarget = goal;
                state.ActivePathFollowerLastLocation = companion.Location;
                state.ActivePathFollowerLastProgressUtc = DateTime.UtcNow;
                state.LastPathFollowerRepathUtc = DateTime.UtcNow;
                state.ConsecutivePathFollowerFailures = 0;
                AIGMExecutionLog.Write(
                    "AIGM_LEGACY_ROUTE_PATHFOLLOWER_START companion={0} destination=\"{1}\" route={2} index={3} target={4}",
                    Describe(companion),
                    SafeLog(state.FinalDestinationName),
                    SafeLog(routeId),
                    routeIndex,
                    FormatPoint(goal));
            }
            else
            {
                AIGMExecutionLog.Write(
                    "AIGM_LEGACY_ROUTE_PATHFOLLOWER_REUSE companion={0} destination=\"{1}\" route={2} index={3} target={4}",
                    Describe(companion),
                    SafeLog(state.FinalDestinationName),
                    SafeLog(routeId),
                    routeIndex,
                    FormatPoint(goal));
            }

            bool staleNoProgress =
                state.ActivePathFollowerLastLocation == companion.Location
                && state.ActivePathFollowerLastProgressUtc != DateTime.MinValue
                && DateTime.UtcNow >= state.ActivePathFollowerLastProgressUtc + TimeSpan.FromSeconds(2.0);

            if ((staleNoProgress || state.ConsecutiveNoProgressChecks >= 2 || state.PathFailureCount >= 2)
                && DateTime.UtcNow >= state.LastPathFollowerRepathUtc + TimeSpan.FromSeconds(1.0))
            {
                state.ActivePathFollower.ForceRepath();
                state.LastPathFollowerRepathUtc = DateTime.UtcNow;
                AIGMExecutionLog.Write(
                    "AIGM_LEGACY_ROUTE_PATHFOLLOWER_REPATH companion={0} destination=\"{1}\" route={2} index={3} target={4} reason={5}",
                    Describe(companion),
                    SafeLog(state.FinalDestinationName),
                    SafeLog(routeId),
                    routeIndex,
                    FormatPoint(goal),
                    staleNoProgress ? "stale_no_progress" : "blocked");
            }

            Point3D before = companion.Location;
            bool followed = state.ActivePathFollower.Follow(run, Math.Max(1, state.ArrivalRadius));
            Point3D after = companion.Location;
            bool moved = after != before;

            if (moved)
            {
                state.ActivePathFollowerLastLocation = after;
                state.ActivePathFollowerLastProgressUtc = DateTime.UtcNow;
                state.ConsecutivePathFollowerFailures = 0;
                AIGMExecutionLog.Write(
                    "AIGM_LEGACY_ROUTE_PATHFOLLOWER_STEP companion={0} destination=\"{1}\" route={2} index={3} target={4} result=moved from={5} to={6}",
                    Describe(companion),
                    SafeLog(state.FinalDestinationName),
                    SafeLog(routeId),
                    routeIndex,
                    FormatPoint(goal),
                    FormatPoint(before),
                    FormatPoint(after));
                return true;
            }

            if (followed)
            {
                state.ConsecutivePathFollowerFailures = 0;
                AIGMExecutionLog.Write(
                    "AIGM_LEGACY_ROUTE_PATHFOLLOWER_STEP companion={0} destination=\"{1}\" route={2} index={3} target={4} result=in_range_no_move point={5}",
                    Describe(companion),
                    SafeLog(state.FinalDestinationName),
                    SafeLog(routeId),
                    routeIndex,
                    FormatPoint(goal),
                    FormatPoint(after));
                return false;
            }

            state.ConsecutivePathFollowerFailures++;
            if (DateTime.UtcNow >= state.LastPathFollowerRepathUtc + TimeSpan.FromSeconds(1.0))
            {
                state.ActivePathFollower.ForceRepath();
                state.LastPathFollowerRepathUtc = DateTime.UtcNow;
                AIGMExecutionLog.Write(
                    "AIGM_LEGACY_ROUTE_PATHFOLLOWER_REPATH companion={0} destination=\"{1}\" route={2} index={3} target={4} reason=follow_failed",
                    Describe(companion),
                    SafeLog(state.FinalDestinationName),
                    SafeLog(routeId),
                    routeIndex,
                    FormatPoint(goal));
            }

            AIGMExecutionLog.Write(
                "AIGM_LEGACY_ROUTE_PATHFOLLOWER_FAIL companion={0} destination=\"{1}\" route={2} index={3} target={4} failureCount={5} point={6}",
                Describe(companion),
                SafeLog(state.FinalDestinationName),
                SafeLog(routeId),
                routeIndex,
                FormatPoint(goal),
                state.ConsecutivePathFollowerFailures,
                FormatPoint(after));

            if (state.ConsecutivePathFollowerFailures >= 4)
            {
                ResetRoutePathFollowerState(state);
                state.ConsecutivePathFollowerFailures = 4;
            }

            return false;
        }

        private static bool TryPathFollowerStep(BaseCreature actor, AIGMLegacyTravelState state, Point3D goal, bool run, int range)
        {
            if (actor == null || actor.Map == null || !Utility.InRange(actor.Location, goal, 38))
                return false;

            if (state.ActivePathFollower == null || DateTime.UtcNow >= state.LastPathFollowerRepathUtc + TimeSpan.FromSeconds(2.0))
            {
                state.ActivePathFollower = CreatePathFollower(actor, goal);
                state.LastPathFollowerRepathUtc = DateTime.UtcNow;
                AIGMExecutionLog.Write("AIGM_LEGACY_TRAVEL_PATHFOLLOWER_START companion={0} goal={1}", Describe(actor), FormatPoint(goal));
            }

            Point3D before = actor.Location;
            bool arrived = state.ActivePathFollower.Follow(run, range);
            if (actor.Location != before)
            {
                state.ConsecutivePathFollowerFailures = 0;
                return true;
            }

            if (!arrived)
            {
                state.ConsecutivePathFollowerFailures++;
                if (state.ConsecutivePathFollowerFailures >= 2)
                    state.ActivePathFollower.ForceRepath();
            }

            return true;
        }

        private static bool ShouldRun(BaseCreature actor, Point3D destination)
        {
            return actor != null && Distance2D(actor.Location, destination) > 6;
        }

        private static Point3D GetAlternativeStep(Map map, Point3D current, Point3D destination)
        {
            int dx = Math.Sign(destination.X - current.X);
            int dy = Math.Sign(destination.Y - current.Y);

            Point3D[] candidates = new Point3D[]
            {
                NormalizeDestination(map, new Point3D(current.X + dx, current.Y - dy, current.Z)),
                NormalizeDestination(map, new Point3D(current.X - dx, current.Y + dy, current.Z)),
                NormalizeDestination(map, new Point3D(current.X - dx, current.Y, current.Z)),
                NormalizeDestination(map, new Point3D(current.X, current.Y - dy, current.Z)),
                NormalizeDestination(map, new Point3D(current.X + dy, current.Y + dx, current.Z)),
                NormalizeDestination(map, new Point3D(current.X - dy, current.Y - dx, current.Z))
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                if (CanOccupy(map, candidates[i]))
                    return candidates[i];
            }

            return current;
        }

        private static Point3D GetFallbackWaypoint(Map map, Point3D current, Point3D finalDestination, int attempt)
        {
            int dx = Math.Sign(finalDestination.X - current.X);
            int dy = Math.Sign(finalDestination.Y - current.Y);
            int radius = 3 + Math.Min(4, attempt * 2);

            Point3D[] candidates = new Point3D[]
            {
                NormalizeDestination(map, new Point3D(current.X + (dx * radius), current.Y, current.Z)),
                NormalizeDestination(map, new Point3D(current.X, current.Y + (dy * radius), current.Z)),
                NormalizeDestination(map, new Point3D(current.X + (dx * radius), current.Y + (dy * radius), current.Z)),
                NormalizeDestination(map, new Point3D(current.X + (dx * radius), current.Y - (dy * radius), current.Z)),
                NormalizeDestination(map, new Point3D(current.X - (dx * radius), current.Y + (dy * radius), current.Z)),
                NormalizeDestination(map, new Point3D(current.X - (dx * radius), current.Y, current.Z)),
                NormalizeDestination(map, new Point3D(current.X, current.Y - (dy * radius), current.Z))
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                if (CanOccupy(map, candidates[i]))
                    return candidates[i];
            }

            return current;
        }

        private static void RememberBreadcrumb(AIGMLegacyTravelState state, Point3D location)
        {
            if (state == null || state.Breadcrumbs == null)
                return;

            Point3D[] points = state.Breadcrumbs.ToArray();
            if (points.Length == 0 || points[points.Length - 1] != location)
            {
                state.Breadcrumbs.Enqueue(location);
                while (state.Breadcrumbs.Count > 24)
                    state.Breadcrumbs.Dequeue();
            }
        }

        private static bool IsOscillating(AIGMLegacyTravelState state, Point3D location)
        {
            if (state == null || state.Breadcrumbs == null || state.Breadcrumbs.Count < 8)
                return false;

            int revisits = 0;
            Point3D[] points = state.Breadcrumbs.ToArray();
            for (int i = 0; i < points.Length; i++)
            {
                if (Utility.InRange(points[i], location, 1))
                    revisits++;
            }

            return revisits >= 4;
        }

        private static Point3D GetBreadcrumbBacktrack(AIGMLegacyTravelState state, Point3D fallback)
        {
            if (state == null || state.Breadcrumbs == null || state.Breadcrumbs.Count == 0)
                return fallback;

            Point3D[] points = state.Breadcrumbs.ToArray();
            int index = Math.Max(0, points.Length - 10);
            return points[index];
        }

        private static bool ContainsNearbyFailedWaypoint(AIGMLegacyTravelState state, Point3D point)
        {
            if (state == null || state.FailedWaypoints == null)
                return false;

            for (int i = 0; i < state.FailedWaypoints.Count; i++)
            {
                if (Utility.InRange(state.FailedWaypoints[i], point, 2))
                    return true;
            }

            return false;
        }

        private static string GetRouteBand(Point3D from, Point3D to)
        {
            int dx = to.X - from.X;
            int dy = to.Y - from.Y;

            if (Math.Abs(dx) >= Math.Abs(dy))
                return dy >= 0 ? "SouthBypass" : "NorthBypass";

            return dx >= 0 ? "EastBypass" : "WestBypass";
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

        private static bool CanOccupy(Map map, Point3D p)
        {
            return map != null && map.CanFit(p.X, p.Y, p.Z, 16, false, false);
        }

        private static int Distance2D(Point3D a, Point3D b)
        {
            int dx = a.X - b.X;
            int dy = a.Y - b.Y;
            return (int)Math.Round(Math.Sqrt((dx * dx) + (dy * dy)));
        }

        private static void NoticeOwner(BaseHire companion, Mobile owner, AIGMLegacyTravelState state, string message)
        {
            if (companion == null || owner == null || state == null || String.IsNullOrWhiteSpace(message))
                return;

            if (DateTime.UtcNow < state.LastOwnerNoticeUtc + OwnerNoticeCooldown)
                return;

            state.LastOwnerNoticeUtc = DateTime.UtcNow;
            companion.SayTo(owner, message);
        }

        private static void ClaimLegacyMovementOwnership(BaseHire companion, AIGMLegacyTravelState state, string reason)
        {
            if (companion == null || companion.Deleted)
                return;

            companion.CantWalk = false;
            companion.Combatant = null;
            companion.Warmode = false;
            companion.ControlTarget = null;
            companion.ControlOrder = OrderType.Stay;
            companion.Home = companion.Location;
            companion.RangeHome = 64;
        }

        private static void RestoreHoldBehavior(BaseHire companion)
        {
            if (companion == null || companion.Deleted)
                return;

            AIGMCompanionControlStateService.RestoreSoftHold(companion, "legacy_travel_finish");
        }

        private static void LogOwnershipProbeBefore(BaseHire companion, Point3D location)
        {
            AIGMExecutionLog.Write(
                "AIGM_LEGACY_OWNERSHIP_PROBE phase=before companion={0} loc={1} order={2} target={3} home={4} rangeHome={5} cantWalk={6} ownerDist={7}",
                Describe(companion),
                FormatPoint(location),
                SafeLog(companion.ControlOrder.ToString()),
                DescribeControlTarget(companion),
                FormatPoint(companion.Home),
                companion.RangeHome,
                companion.CantWalk,
                GetOwnerDistance(companion, location));
        }

        private static void LogOwnershipProbeAfter(BaseHire companion, Point3D before, Point3D after, bool moved)
        {
            AIGMExecutionLog.Write(
                "AIGM_LEGACY_OWNERSHIP_PROBE phase=after companion={0} before={1} after={2} moved={3} order={4} home={5} ownerDist={6}",
                Describe(companion),
                FormatPoint(before),
                FormatPoint(after),
                moved,
                SafeLog(companion.ControlOrder.ToString()),
                FormatPoint(companion.Home),
                GetOwnerDistance(companion, after));
        }

        private static void ScheduleOwnershipProbeAfterDelay(Serial companionSerial, Point3D expected)
        {
            new OwnershipProbeTimer(companionSerial, expected).Start();
        }

        private static void LogOwnershipProbeAfterDelay(Serial companionSerial, Point3D expected)
        {
            BaseHire companion = World.FindMobile(companionSerial) as BaseHire;
            if (companion == null || companion.Deleted)
                return;

            Point3D actual = companion.Location;
            AIGMExecutionLog.Write(
                "AIGM_LEGACY_OWNERSHIP_PROBE phase=after750 companion={0} expected={1} actual={2} snappedBack={3} order={4} home={5} ownerDist={6}",
                Describe(companion),
                FormatPoint(expected),
                FormatPoint(actual),
                actual != expected,
                SafeLog(companion.ControlOrder.ToString()),
                FormatPoint(companion.Home),
                GetOwnerDistance(companion, actual));
        }

        private static AIGMLegacyTravelState GetState(BaseHire companion)
        {
            if (companion == null)
                return null;

            AIGMLegacyTravelState state;
            States.TryGetValue(companion.Serial, out state);
            return state;
        }

        private static int GetOwnerDistance(BaseHire companion, Point3D location)
        {
            if (companion == null)
                return -1;

            Mobile owner = companion.GetOwner();
            if (owner == null || owner.Deleted || owner.Map != companion.Map)
                return -1;

            return Distance2D(location, owner.Location);
        }

        private static string DescribeControlTarget(BaseHire companion)
        {
            if (companion == null)
                return "none";

            Mobile mobile = companion.ControlTarget as Mobile;
            return mobile == null ? "none" : Describe(mobile);
        }

        private static string Finish(BaseHire companion, AIGMLegacyTravelState state, AIGMLegacyTravelMode mode, string reason, bool visible, bool stopOrder)
        {
            if (state == null)
                return reason ?? "finished";

            LegacyTravelTimer timer;
            if (Timers.TryGetValue(state.CompanionSerial, out timer) && timer != null)
                timer.Stop();
            Timers.Remove(state.CompanionSerial);

            Mobile owner = state.OwnerSerial != Serial.MinusOne ? World.FindMobile(state.OwnerSerial) : null;
            if (owner == null && companion != null)
                owner = companion.GetOwner();

            int finalDistance = companion != null ? Distance2D(companion.Location, state.FinalDestination) : -1;
            double elapsed = state.StartedUtc == DateTime.MinValue ? 0.0 : (DateTime.UtcNow - state.StartedUtc).TotalSeconds;
            string routeContextId = state.RouteId;

            AIGMMovementLeaseTerminalStatus leaseTerminalStatus = AIGMMovementLeaseTerminalStatus.Released;
            switch (mode)
            {
                case AIGMLegacyTravelMode.Arrived:
                    leaseTerminalStatus = AIGMMovementLeaseTerminalStatus.Completed;
                    break;
                case AIGMLegacyTravelMode.Blocked:
                    leaseTerminalStatus = AIGMMovementLeaseTerminalStatus.Blocked;
                    break;
                case AIGMLegacyTravelMode.Invalid:
                    leaseTerminalStatus = AIGMMovementLeaseTerminalStatus.Invalid;
                    break;
            }

            AIGMMovementOwnershipService.ReleaseIfOwned(companion, AIGMMovementLeaseOwnerType.Travel, owner, routeContextId, reason, leaseTerminalStatus);

            if (companion != null && !companion.Deleted)
            {
                if (state.TravelSpeedApplied)
                {
                    companion.ActiveSpeed = state.OriginalActiveSpeed;
                    companion.PassiveSpeed = state.OriginalPassiveSpeed;
                }

                companion.ControlTarget = null;
                companion.Combatant = null;
                companion.Warmode = false;

                if (stopOrder)
                    RestoreHoldBehavior(companion);
            }

            if (mode == AIGMLegacyTravelMode.Arrived)
            {
                if (visible && companion != null && owner != null)
                {
                    if (state.StopAtLastWaypoint)
                        companion.SayTo(owner, "I have reached the mapped frontier near {0}; the rest of the route to {1} is not charted yet.", state.RouteTerminalName ?? "the last waypoint", state.FinalDestinationName ?? state.DestinationName ?? "the destination");
                    else
                        companion.SayTo(owner, "I am near {0}.", state.FinalDestinationName ?? state.DestinationName ?? "the destination");
                }

                AIGMExecutionLog.Write(
                    "AIGM_LEGACY_TRAVEL_ARRIVED companion={0} destination=\"{1}\" finalDistance={2} elapsedSeconds={3:F1} frontierStop={4} frontier=\"{5}\"",
                    Describe(companion),
                    SafeLog(state.FinalDestinationName),
                    finalDistance,
                    elapsed,
                    state.StopAtLastWaypoint,
                    SafeLog(state.RouteTerminalName));
            }
            else if (mode == AIGMLegacyTravelMode.Blocked)
            {
                if (visible && companion != null && owner != null)
                    companion.SayTo(owner, "I am blocked near {0} while traveling to {1}.", FormatPoint(companion.Location), state.FinalDestinationName ?? state.DestinationName ?? "the destination");

                AIGMExecutionLog.Write(
                    "AIGM_LEGACY_TRAVEL_BLOCKED companion={0} destination=\"{1}\" currentPoint={2} finalTarget={3} stuckTicks={4} messageSentToOwner={5}",
                    Describe(companion),
                    SafeLog(state.FinalDestinationName),
                    FormatPoint(companion != null ? companion.Location : Point3D.Zero),
                    FormatPoint(state.FinalDestination),
                    state.StuckTicks,
                    visible && owner != null);
            }
            else
            {
                AIGMExecutionLog.Write(
                    "AIGM_LEGACY_TRAVEL_STOP companion={0} destination=\"{1}\" mode={2} reason={3}",
                    Describe(companion),
                    SafeLog(state.FinalDestinationName),
                    mode,
                    SafeLog(reason));
            }

            States.Remove(state.CompanionSerial);
            state.Reset();
            return reason ?? mode.ToString();
        }

        private static AIGMLegacyTravelState GetOrCreateState(BaseHire companion)
        {
            AIGMLegacyTravelState state;
            if (!States.TryGetValue(companion.Serial, out state) || state == null)
            {
                state = new AIGMLegacyTravelState();
                States[companion.Serial] = state;
            }

            return state;
        }

        private static string FormatPoint(Point3D p)
        {
            return String.Format("{0},{1},{2}", p.X, p.Y, p.Z);
        }

        private static string SafeLog(string value)
        {
            return value == null ? String.Empty : value.Replace("\r", " ").Replace("\n", " ").Replace("\"", "'");
        }

        private static string Describe(Mobile m)
        {
            if (m == null)
                return "null";

            return String.Format("{0}:{1}", m.GetType().Name, m.Serial.Value);
        }

        private sealed class LegacyTravelTimer : Timer
        {
            private readonly Serial _companionSerial;
            private readonly Serial _ownerSerial;
            private readonly int _expectedEpoch;

            public LegacyTravelTimer(Serial companionSerial, Serial ownerSerial, int expectedEpoch)
                : base(TickInterval, TickInterval)
            {
                Priority = TimerPriority.OneSecond;
                _companionSerial = companionSerial;
                _ownerSerial = ownerSerial;
                _expectedEpoch = expectedEpoch;
            }

            protected override void OnTick()
            {
                BaseHire companion = World.FindMobile(_companionSerial) as BaseHire;
                Mobile owner = World.FindMobile(_ownerSerial);

                if (companion == null || companion.Deleted)
                {
                    Stop();
                    Timers.Remove(_companionSerial);
                    States.Remove(_companionSerial);
                    return;
                }

                Tick(companion, owner, _expectedEpoch);

                AIGMLegacyTravelState state;
                if (!States.TryGetValue(_companionSerial, out state) || state == null || state.Mode == AIGMLegacyTravelMode.None)
                    Stop();
            }
        }

        private sealed class OwnershipProbeTimer : Timer
        {
            private readonly Serial _companionSerial;
            private readonly Point3D _expected;

            public OwnershipProbeTimer(Serial companionSerial, Point3D expected)
                : base(TimeSpan.FromMilliseconds(750.0))
            {
                _companionSerial = companionSerial;
                _expected = expected;
                Priority = TimerPriority.TwoFiftyMS;
            }

            protected override void OnTick()
            {
                LogOwnershipProbeAfterDelay(_companionSerial, _expected);
                Stop();
            }
        }
    }
}
