using System;
using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public enum AIGMMovementMode
    {
        None,
        Follow,
        PathToPoint,
        PathToNamedLocation
    }

    public class AIGMMovementState
    {
        private static readonly Dictionary<Serial, AIGMMovementState> States = new Dictionary<Serial, AIGMMovementState>();

        public static AIGMMovementState Get(Mobile from)
        {
            if (from == null)
                return null;

            AIGMMovementState state;
            if (!States.TryGetValue(from.Serial, out state))
            {
                state = new AIGMMovementState();
                States[from.Serial] = state;
            }

            return state;
        }

        public AIGMMovementMode Mode { get; set; }
        public Serial CounselorSerial { get; set; }
        public Serial FollowerSerial { get; set; }
        public Point3D Destination { get; set; }
        public string DestinationName { get; set; }
        public string LastStatus { get; set; }
        public string ArrivalActionKind { get; set; }
        public string ArrivalActionArgument { get; set; }
        public bool IsPaused { get; set; }
        public AIGMMovementMode PausedMode { get; set; }
        public Serial PausedFollowerSerial { get; set; }
        public Point3D PausedDestination { get; set; }
        public string PausedDestinationName { get; set; }
        public DateTime StartedUtc { get; set; }
        public DateTime LastProgressUtc { get; set; }
        public Point3D LastObservedLocation { get; set; }
        public int StuckTicks { get; set; }
        public int PathFailureCount { get; set; }
        public bool LastMoveWasRunning { get; set; }
        public bool UsingDetourWaypoint { get; set; }
        public Point3D FinalDestination { get; set; }
        public string FinalDestinationName { get; set; }
        public int DetourAttempts { get; set; }
        public AIGMTravelPathStrategy CurrentStrategy { get; set; }
        public PathFollower ActivePathFollower { get; set; }
        public DateTime LastPathFollowerRepathUtc { get; set; }
        public int ConsecutiveBlockedSteps { get; set; }
        public int ConsecutiveNoProgressChecks { get; set; }
        public int ConsecutivePathFollowerFailures { get; set; }
        public int ConsecutiveDetourFailures { get; set; }
        public Queue<Point3D> Breadcrumbs { get; set; }
        public List<Point3D> FailedWaypoints { get; set; }
        public string ActiveRouteBand { get; set; }
        public Point3D ActiveDetourPoint { get; set; }
        public int DetourCommitmentPulsesRemaining { get; set; }
        public int SuccessfulDetourMoves { get; set; }
        public Point3D DetourStartLocation { get; set; }
        public int OscillationCount { get; set; }
        public string SuppressedRouteBand { get; set; }
        public DateTime SuppressedRouteBandUntilUtc { get; set; }
        public List<AIGMRouteStop> RouteStops { get; set; }
        public AIGMMovementTimer Timer { get; set; }

        public AIGMMovementState()
        {
            RouteStops = new List<AIGMRouteStop>();
            Breadcrumbs = new Queue<Point3D>();
            FailedWaypoints = new List<Point3D>();
            CurrentStrategy = AIGMTravelPathStrategy.DirectStep;
        }
    }

    public sealed class AIGMMovementTimer : Timer
    {
        private readonly Serial _owner;

        public AIGMMovementTimer(Serial owner)
            : base(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0))
        {
            Priority = TimerPriority.OneSecond;
            _owner = owner;
        }

        protected override void OnTick()
        {
            Mobile owner = World.FindMobile(_owner);
            if (owner == null || owner.Deleted)
            {
                Stop();
                return;
            }

            AIGMMovementController.OnTick(owner);
        }
    }

    public static class AIGMMovementController
    {
        private static readonly Dictionary<string, AIGMNamedDestination> NamedDestinations = new Dictionary<string, AIGMNamedDestination>(StringComparer.OrdinalIgnoreCase)
        {
            { "britain", new AIGMNamedDestination("Britain", Map.Felucca, new Point3D(1496, 1628, 20)) },
            { "britain bank", new AIGMNamedDestination("Britain Bank", Map.Felucca, new Point3D(1496, 1628, 20)) },
            { "britain bank entrance", new AIGMNamedDestination("Britain Bank Entrance", Map.Felucca, new Point3D(1495, 1629, 20)) },
            { "brit bank", new AIGMNamedDestination("Britain Bank", Map.Felucca, new Point3D(1496, 1628, 20)) },
            { "brit bank entrance", new AIGMNamedDestination("Britain Bank Entrance", Map.Felucca, new Point3D(1495, 1629, 20)) },
            { "buccaneers den", new AIGMNamedDestination("Buccaneer's Den", Map.Felucca, new Point3D(2713, 2163, 0)) },
            { "bucs den", new AIGMNamedDestination("Buccaneer's Den", Map.Felucca, new Point3D(2713, 2163, 0)) },
            { "buccaneers den bank", new AIGMNamedDestination("Buccaneer's Den Bank", Map.Felucca, new Point3D(2731, 2189, 0)) },
            { "buccaneers den bank entrance", new AIGMNamedDestination("Buccaneer's Den Bank Entrance", Map.Felucca, new Point3D(2732, 2190, 0)) },
            { "bucs den bank", new AIGMNamedDestination("Buccaneer's Den Bank", Map.Felucca, new Point3D(2731, 2189, 0)) },
            { "cove", new AIGMNamedDestination("Cove", Map.Felucca, new Point3D(2234, 1198, 0)) },
            { "cove bank", new AIGMNamedDestination("Cove Bank", Map.Felucca, new Point3D(2269, 1207, 0)) },
            { "cove bank entrance", new AIGMNamedDestination("Cove Bank Entrance", Map.Felucca, new Point3D(2268, 1206, 0)) },
            { "jhelom", new AIGMNamedDestination("Jhelom", Map.Felucca, new Point3D(1378, 3827, 0)) },
            { "jhelom bank", new AIGMNamedDestination("Jhelom Bank", Map.Felucca, new Point3D(1417, 3821, 0)) },
            { "jhelom bank entrance", new AIGMNamedDestination("Jhelom Bank Entrance", Map.Felucca, new Point3D(1416, 3822, 0)) },
            { "magincia", new AIGMNamedDestination("Magincia", Map.Felucca, new Point3D(3675, 2258, 20)) },
            { "magincia bank", new AIGMNamedDestination("Magincia Bank", Map.Felucca, new Point3D(3701, 2252, 20)) },
            { "magincia bank entrance", new AIGMNamedDestination("Magincia Bank Entrance", Map.Felucca, new Point3D(3700, 2251, 20)) },
            { "minoc", new AIGMNamedDestination("Minoc", Map.Felucca, new Point3D(2471, 413, 15)) },
            { "minoc bank", new AIGMNamedDestination("Minoc Bank", Map.Felucca, new Point3D(2501, 561, 0)) },
            { "minoc bank entrance", new AIGMNamedDestination("Minoc Bank Entrance", Map.Felucca, new Point3D(2502, 562, 0)) },
            { "moonglow", new AIGMNamedDestination("Moonglow", Map.Felucca, new Point3D(4467, 1283, 5)) },
            { "moonglow bank", new AIGMNamedDestination("Moonglow Bank", Map.Felucca, new Point3D(4408, 1169, 0)) },
            { "moonglow bank entrance", new AIGMNamedDestination("Moonglow Bank Entrance", Map.Felucca, new Point3D(4407, 1168, 0)) },
            { "nujelm", new AIGMNamedDestination("Nujelm", Map.Felucca, new Point3D(3771, 1280, 0)) },
            { "nujelm bank", new AIGMNamedDestination("Nujelm Bank", Map.Felucca, new Point3D(3763, 1317, 0)) },
            { "nujelm bank entrance", new AIGMNamedDestination("Nujelm Bank Entrance", Map.Felucca, new Point3D(3762, 1318, 0)) },
            { "occlo", new AIGMNamedDestination("Occlo", Map.Felucca, new Point3D(3650, 2653, 0)) },
            { "occlo bank", new AIGMNamedDestination("Occlo Bank", Map.Felucca, new Point3D(3623, 2610, 0)) },
            { "occlo bank entrance", new AIGMNamedDestination("Occlo Bank Entrance", Map.Felucca, new Point3D(3622, 2609, 0)) },
            { "serpents hold", new AIGMNamedDestination("Serpent's Hold", Map.Felucca, new Point3D(2895, 3476, 15)) },
            { "serpent's hold", new AIGMNamedDestination("Serpent's Hold", Map.Felucca, new Point3D(2895, 3476, 15)) },
            { "serpents hold bank", new AIGMNamedDestination("Serpent's Hold Bank", Map.Felucca, new Point3D(2892, 3526, 0)) },
            { "serpent's hold bank", new AIGMNamedDestination("Serpent's Hold Bank", Map.Felucca, new Point3D(2892, 3526, 0)) },
            { "serpents hold bank entrance", new AIGMNamedDestination("Serpent's Hold Bank Entrance", Map.Felucca, new Point3D(2891, 3525, 0)) },
            { "skarabra", new AIGMNamedDestination("Skarabra", Map.Felucca, new Point3D(586, 2236, 0)) },
            { "skarabra bank", new AIGMNamedDestination("Skarabra Bank", Map.Felucca, new Point3D(594, 2137, 0)) },
            { "skarabra bank entrance", new AIGMNamedDestination("Skarabra Bank Entrance", Map.Felucca, new Point3D(593, 2138, 0)) },
            { "trinsic", new AIGMNamedDestination("Trinsic", Map.Felucca, new Point3D(1828, 2821, 0)) },
            { "trinsic bank", new AIGMNamedDestination("Trinsic Bank", Map.Felucca, new Point3D(1909, 2719, 20)) },
            { "trinsic bank entrance", new AIGMNamedDestination("Trinsic Bank Entrance", Map.Felucca, new Point3D(1908, 2718, 20)) },
            { "vesper", new AIGMNamedDestination("Vesper", Map.Felucca, new Point3D(2899, 676, 0)) },
            { "vesper bank", new AIGMNamedDestination("Vesper Bank", Map.Felucca, new Point3D(2897, 686, 0)) },
            { "vesper bank entrance", new AIGMNamedDestination("Vesper Bank Entrance", Map.Felucca, new Point3D(2896, 685, 0)) },
            { "wind", new AIGMNamedDestination("Wind", Map.Felucca, new Point3D(5198, 3975, 37)) },
            { "wind bank", new AIGMNamedDestination("Wind Bank", Map.Felucca, new Point3D(5164, 4009, 37)) },
            { "wind bank entrance", new AIGMNamedDestination("Wind Bank Entrance", Map.Felucca, new Point3D(5163, 4008, 37)) },
            { "yew", new AIGMNamedDestination("Yew", Map.Felucca, new Point3D(633, 858, 0)) },
            { "yew bank", new AIGMNamedDestination("Yew Bank", Map.Felucca, new Point3D(346, 857, 20)) },
            { "yew bank entrance", new AIGMNamedDestination("Yew Bank Entrance", Map.Felucca, new Point3D(347, 858, 20)) },

            { "moonglow moongate", new AIGMNamedDestination("Moonglow Moongate", Map.Felucca, new Point3D(4467, 1283, 5)) },
            { "moonglow gate", new AIGMNamedDestination("Moonglow Moongate", Map.Felucca, new Point3D(4467, 1283, 5)) },
            { "britain moongate", new AIGMNamedDestination("Britain Moongate", Map.Felucca, new Point3D(1336, 1997, 5)) },
            { "britain gate", new AIGMNamedDestination("Britain Moongate", Map.Felucca, new Point3D(1336, 1997, 5)) },
            { "brit moongate", new AIGMNamedDestination("Britain Moongate", Map.Felucca, new Point3D(1336, 1997, 5)) },
            { "jhelom moongate", new AIGMNamedDestination("Jhelom Moongate", Map.Felucca, new Point3D(1499, 3771, 5)) },
            { "jhelom gate", new AIGMNamedDestination("Jhelom Moongate", Map.Felucca, new Point3D(1499, 3771, 5)) },
            { "yew moongate", new AIGMNamedDestination("Yew Moongate", Map.Felucca, new Point3D(771, 752, 5)) },
            { "yew gate", new AIGMNamedDestination("Yew Moongate", Map.Felucca, new Point3D(771, 752, 5)) },
            { "minoc moongate", new AIGMNamedDestination("Minoc Moongate", Map.Felucca, new Point3D(2701, 692, 5)) },
            { "minoc gate", new AIGMNamedDestination("Minoc Moongate", Map.Felucca, new Point3D(2701, 692, 5)) },
            { "trinsic moongate", new AIGMNamedDestination("Trinsic Moongate", Map.Felucca, new Point3D(1828, 2948, -20)) },
            { "trinsic gate", new AIGMNamedDestination("Trinsic Moongate", Map.Felucca, new Point3D(1828, 2948, -20)) },
            { "skara brae moongate", new AIGMNamedDestination("Skara Brae Moongate", Map.Felucca, new Point3D(643, 2067, 5)) },
            { "skara brae gate", new AIGMNamedDestination("Skara Brae Moongate", Map.Felucca, new Point3D(643, 2067, 5)) },
            { "skarabra moongate", new AIGMNamedDestination("Skara Brae Moongate", Map.Felucca, new Point3D(643, 2067, 5)) },
            { "skarabra gate", new AIGMNamedDestination("Skara Brae Moongate", Map.Felucca, new Point3D(643, 2067, 5)) },
            { "magincia moongate", new AIGMNamedDestination("Magincia Moongate", Map.Felucca, new Point3D(3563, 2139, 0)) },
            { "magincia gate", new AIGMNamedDestination("Magincia Moongate", Map.Felucca, new Point3D(3563, 2139, 0)) },
            { "bucs den moongate", new AIGMNamedDestination("Buccaneer's Den Moongate", Map.Felucca, new Point3D(2711, 2234, 0)) },
            { "buccaneers den moongate", new AIGMNamedDestination("Buccaneer's Den Moongate", Map.Felucca, new Point3D(2711, 2234, 0)) },
            { "bucs den gate", new AIGMNamedDestination("Buccaneer's Den Moongate", Map.Felucca, new Point3D(2711, 2234, 0)) },
            { "new haven moongate", new AIGMNamedDestination("New Haven Moongate", Map.Trammel, new Point3D(3450, 2677, 25)) },
            { "new haven gate", new AIGMNamedDestination("New Haven Moongate", Map.Trammel, new Point3D(3450, 2677, 25)) },
            { "luna moongate", new AIGMNamedDestination("Luna Moongate", Map.Malas, new Point3D(1015, 527, -65)) },
            { "luna gate", new AIGMNamedDestination("Luna Moongate", Map.Malas, new Point3D(1015, 527, -65)) },
            { "umbra moongate", new AIGMNamedDestination("Umbra Moongate", Map.Malas, new Point3D(1997, 1386, -85)) },
            { "umbra gate", new AIGMNamedDestination("Umbra Moongate", Map.Malas, new Point3D(1997, 1386, -85)) },
            { "royal city moongate", new AIGMNamedDestination("Royal City Moongate", Map.TerMur, new Point3D(850, 3525, -38)) },
            { "royal city gate", new AIGMNamedDestination("Royal City Moongate", Map.TerMur, new Point3D(850, 3525, -38)) },
            { "isamu-jima moongate", new AIGMNamedDestination("Isamu-Jima Moongate", Map.Tokuno, new Point3D(1169, 998, 41)) },
            { "isamu jima moongate", new AIGMNamedDestination("Isamu-Jima Moongate", Map.Tokuno, new Point3D(1169, 998, 41)) },
            { "makoto-jima moongate", new AIGMNamedDestination("Makoto-Jima Moongate", Map.Tokuno, new Point3D(802, 1204, 25)) },
            { "makoto jima moongate", new AIGMNamedDestination("Makoto-Jima Moongate", Map.Tokuno, new Point3D(802, 1204, 25)) },
            { "homare-jima moongate", new AIGMNamedDestination("Homare-Jima Moongate", Map.Tokuno, new Point3D(270, 628, 15)) },
            { "homare jima moongate", new AIGMNamedDestination("Homare-Jima Moongate", Map.Tokuno, new Point3D(270, 628, 15)) },
            { "compassion gate", new AIGMNamedDestination("Compassion Gate", Map.Ilshenar, new Point3D(1215, 467, -13)) },
            { "honesty gate", new AIGMNamedDestination("Honesty Gate", Map.Ilshenar, new Point3D(722, 1366, -60)) },
            { "honor gate", new AIGMNamedDestination("Honor Gate", Map.Ilshenar, new Point3D(744, 724, -28)) },
            { "humility gate", new AIGMNamedDestination("Humility Gate", Map.Ilshenar, new Point3D(281, 1016, 0)) },
            { "justice gate", new AIGMNamedDestination("Justice Gate", Map.Ilshenar, new Point3D(987, 1011, -32)) },
            { "sacrifice gate", new AIGMNamedDestination("Sacrifice Gate", Map.Ilshenar, new Point3D(1174, 1286, -30)) },
            { "spirituality gate", new AIGMNamedDestination("Spirituality Gate", Map.Ilshenar, new Point3D(1532, 1340, -3)) },
            { "valor gate", new AIGMNamedDestination("Valor Gate", Map.Ilshenar, new Point3D(528, 216, -45)) },
            { "chaos gate", new AIGMNamedDestination("Chaos Gate", Map.Ilshenar, new Point3D(1721, 218, 96)) }
        };

        public static bool StartFollow(Mobile from, BaseCreature counselor, Mobile target, out string message)
        {
            message = null;

            if (from == null || counselor == null || target == null)
            {
                message = "Follow could not start because the actor, counselor, or target was missing.";
                return false;
            }

            if (counselor.Deleted || target.Deleted || counselor.Map == null || target.Map == null)
            {
                message = "Follow could not start because the counselor or target is no longer valid.";
                return false;
            }

            if (counselor.Map != target.Map)
            {
                message = "Follow currently requires the counselor and target to be on the same map.";
                return false;
            }

            if (!counselor.SetControlMaster(from))
            {
                message = "The counselor could not be placed under temporary staff control for follow mode.";
                return false;
            }

            counselor.CantWalk = false;
            counselor.ControlTarget = target;
            counselor.ControlOrder = OrderType.Follow;
            counselor.Home = target.Location;
            counselor.RangeHome = 32;

            AIGMMovementState state = AIGMMovementState.Get(from);
            StopTimer(state);
            state.Mode = AIGMMovementMode.Follow;
            state.CounselorSerial = counselor.Serial;
            state.FollowerSerial = target.Serial;
            state.Destination = target.Location;
            state.DestinationName = target.Name;
            state.LastStatus = "Following";
            state.StartedUtc = DateTime.UtcNow;
            state.LastProgressUtc = DateTime.UtcNow;
            state.LastObservedLocation = counselor.Location;
            state.StuckTicks = 0;
            state.PathFailureCount = 0;
            state.LastMoveWasRunning = false;
            state.RouteStops.Clear();
            state.Timer = new AIGMMovementTimer(from.Serial);
            state.Timer.Start();

            message = String.Format("{0} is now following {1}.", counselor.Name ?? "The counselor", target.Name ?? "you");
            return true;
        }

        public static bool Stop(Mobile from, BaseCreature counselor, out string message)
        {
            return Stop(from, counselor, null, out message);
        }

        public static bool Stop(Mobile from, BaseCreature counselor, string statusOverride, out string message)
        {
            message = null;
            if (from == null || counselor == null)
            {
                message = "No counselor was available to stop.";
                return false;
            }

            AIGMMovementState state = AIGMMovementState.Get(from);
            StopTimer(state);
            state.Mode = AIGMMovementMode.None;
            state.LastStatus = !String.IsNullOrWhiteSpace(statusOverride) ? statusOverride : "Stopped";
            state.FollowerSerial = Serial.MinusOne;
            state.DestinationName = null;
            state.StuckTicks = 0;
            state.PathFailureCount = 0;
            state.LastMoveWasRunning = false;

            counselor.ControlTarget = null;
            counselor.ControlOrder = OrderType.Stay;
            counselor.CantWalk = true;
            counselor.Home = counselor.Location;
            counselor.RangeHome = 0;
            counselor.SetControlMaster(null);

            message = String.Format("{0} stopped moving.", counselor.Name ?? "The counselor");
            return true;
        }

        public static bool StartPathToPoint(Mobile from, BaseCreature counselor, Point3D destination, Map map, string destinationName, out string message)
        {
            return StartPathToPoint(from, counselor, destination, map, destinationName, AIGMMovementStartOptions.ForCounselorPathing(), out message);
        }

        public static bool StartPathToPoint(Mobile requester, BaseCreature actor, Point3D destination, Map map, string destinationName, AIGMMovementStartOptions options, out string message)
        {
            message = null;
            if (requester == null || actor == null || map == null)
            {
                message = "Pathing could not start because the actor or map was missing.";
                return false;
            }

            if (actor.Map != map)
            {
                message = "Pathing currently requires staying on the current map.";
                return false;
            }

            if (options == null)
                options = AIGMMovementStartOptions.ForCounselorPathing();

            Point3D adjusted = NormalizeDestination(map, destination);
            if (!CanOccupy(map, adjusted))
            {
                message = String.Format("The destination {0},{1},{2} is not a valid standing location.", adjusted.X, adjusted.Y, adjusted.Z);
                return false;
            }

            LogMovement("MOVEMENT_START_REQUEST actor=" + SafeName(actor) + " requester=" + SafeName(requester) + " mode=" + options.ControlMode + " reason=" + (options.Reason ?? String.Empty));
            LogCompanionState("COMPANION_SAFE_MOVEMENT_STATE_BEFORE", actor, requester);

            if (options.ControlMode == AIGMMovementControlMode.CounselorTemporaryControl)
            {
                if (!actor.SetControlMaster(requester))
                {
                    message = "The counselor could not be placed under temporary staff control for pathing.";
                    return false;
                }
            }
            else
            {
                LogMovement("COMPANION_SAFE_MOVEMENT_SKIP_SETCONTROLMASTER reason=" + (options.Reason ?? String.Empty));
            }

            actor.CantWalk = false;
            actor.ControlTarget = null;
            actor.ControlOrder = OrderType.Stop;
            actor.Home = adjusted;
            actor.RangeHome = 0;

            AIGMMovementState state = AIGMMovementState.Get(requester);
            StopTimer(state);
            state.Mode = String.IsNullOrWhiteSpace(destinationName) ? AIGMMovementMode.PathToPoint : AIGMMovementMode.PathToNamedLocation;
            state.CounselorSerial = actor.Serial;
            state.FollowerSerial = Serial.MinusOne;
            state.Destination = adjusted;
            state.DestinationName = destinationName;
            state.FinalDestination = adjusted;
            state.FinalDestinationName = destinationName;
            state.UsingDetourWaypoint = false;
            state.DetourAttempts = 0;
            state.LastStatus = "Pathing";
            state.StartedUtc = DateTime.UtcNow;
            state.LastProgressUtc = DateTime.UtcNow;
            state.LastObservedLocation = actor.Location;
            state.StuckTicks = 0;
            state.PathFailureCount = 0;
            state.LastMoveWasRunning = false;
            state.CurrentStrategy = AIGMTravelPathStrategy.DirectStep;
            state.ActivePathFollower = null;
            state.LastPathFollowerRepathUtc = DateTime.MinValue;
            state.ConsecutiveBlockedSteps = 0;
            state.ConsecutiveNoProgressChecks = 0;
            state.ConsecutivePathFollowerFailures = 0;
            state.ConsecutiveDetourFailures = 0;
            state.ActiveRouteBand = null;
            state.ActiveDetourPoint = Point3D.Zero;
            state.DetourCommitmentPulsesRemaining = 0;
            state.SuccessfulDetourMoves = 0;
            state.DetourStartLocation = Point3D.Zero;
            state.OscillationCount = 0;
            state.SuppressedRouteBand = null;
            state.SuppressedRouteBandUntilUtc = DateTime.MinValue;
            state.Breadcrumbs.Clear();
            state.FailedWaypoints.Clear();
            state.RouteStops.Clear();
            state.Timer = new AIGMMovementTimer(requester.Serial);
            state.Timer.Start();

            LogCompanionState("COMPANION_SAFE_MOVEMENT_STATE_AFTER", actor, requester);
            LogMovement("COMPANION_SAFE_MOVEMENT_START actor=" + SafeName(actor) + " requester=" + SafeName(requester) + " dest=" + FormatPoint(adjusted));
            message = String.Format("{0} is moving to {1}.", actor.Name ?? "The counselor", !String.IsNullOrWhiteSpace(destinationName) ? destinationName : String.Format("{0},{1},{2}", adjusted.X, adjusted.Y, adjusted.Z));
            return true;
        }

        public static bool StartCompanionPathToPoint(BaseCreature companion, Mobile requester, Map map, Point3D destination, string label, out string message)
        {
            message = null;
            if (companion == null || requester == null)
            {
                message = "Companion travel could not start.";
                return false;
            }

            Mobile controlMaster = companion.ControlMaster;
            bool trusted = requester == controlMaster || companion.IsPetFriend(requester) || requester.AccessLevel >= AccessLevel.GameMaster;
            if (!trusted)
            {
                message = "You are not trusted to direct that companion.";
                LogMovement("COMPANION_TRAVEL_REJECT untrusted requester=" + SafeName(requester) + " actor=" + SafeName(companion));
                return false;
            }

            LogMovement("COMPANION_TRAVEL_REQUEST destination=" + (label ?? FormatPoint(destination)));
            return StartPathToPoint(requester, companion, destination, map, label, AIGMMovementStartOptions.ForCompanionTravel(), out message);
        }

        public static bool StartNamedPath(Mobile from, BaseCreature counselor, string rawDestination, out string message)
        {
            message = null;
            AIGMNamedDestination destination;
            if (!TryResolveNamedDestination(rawDestination, counselor != null ? counselor.Map : null, out destination) || destination == null)
            {
                message = "I do not know that destination.";
                return false;
            }

            return StartPathToPoint(from, counselor, destination.Location, destination.Map, destination.Name, out message);
        }

        public static bool StartCompanionNamedPath(BaseCreature companion, Mobile requester, string rawDestination, out string message)
        {
            message = null;
            AIGMNamedDestination destination;
            if (!TryResolveNamedDestination(rawDestination, companion != null ? companion.Map : null, out destination) || destination == null)
            {
                message = "I do not know that destination.";
                return false;
            }

            return StartCompanionPathToPoint(companion, requester, destination.Map, destination.Location, destination.Name, out message);
        }

        public static string GetStatus(Mobile from)
        {
            AIGMMovementState state = AIGMMovementState.Get(from);
            if (state == null || state.Mode == AIGMMovementMode.None)
                return "I am not traveling anywhere right now.";

            if (!String.IsNullOrWhiteSpace(state.DestinationName))
                return "I am headed to " + state.DestinationName + ".";

            return "I am traveling.";
        }

        public static bool TryResolveNamedDestination(string raw, Map currentMap, out AIGMNamedDestination destination)
        {
            destination = null;
            if (String.IsNullOrWhiteSpace(raw))
                return false;

            string key = raw.Trim();
            if (NamedDestinations.TryGetValue(key, out destination))
                return true;

            key = key.Replace("  ", " ").Trim();
            return NamedDestinations.TryGetValue(key, out destination);
        }

        public static void QueueNamedStop(Mobile from, string destinationName, Point3D location)
        {
            AIGMMovementState state = AIGMMovementState.Get(from);
            if (state == null)
                return;

            state.RouteStops.Add(new AIGMRouteStop(destinationName, location));
        }

        public static void QueueCoordinateStop(Mobile from, Point3D location)
        {
            AIGMMovementState state = AIGMMovementState.Get(from);
            if (state == null)
                return;

            state.RouteStops.Add(new AIGMRouteStop(null, location));
        }

        public static void OnTick(Mobile owner)
        {
            AIGMMovementState state = AIGMMovementState.Get(owner);
            if (state == null)
                return;

            BaseCreature counselor = World.FindMobile(state.CounselorSerial) as BaseCreature;
            if (state.IsPaused)
            {
                StopTimer(state);
                state.LastStatus = "Paused";
                return;
            }

            if (counselor == null || counselor.Deleted || counselor.Map == null)
            {
                StopTimer(state);
                state.Mode = AIGMMovementMode.None;
                return;
            }

            switch (state.Mode)
            {
                case AIGMMovementMode.Follow:
                    TickFollow(owner, counselor, state);
                    break;
                case AIGMMovementMode.PathToPoint:
                case AIGMMovementMode.PathToNamedLocation:
                    TickPath(owner, counselor, state);
                    break;
                default:
                    StopTimer(state);
                    break;
            }
        }

        private static void TickFollow(Mobile owner, BaseCreature counselor, AIGMMovementState state)
        {
            Mobile target = World.FindMobile(state.FollowerSerial);
            if (target == null || target.Deleted || target.Map == null || target.Map != counselor.Map)
            {
                string ignore;
                Stop(owner, counselor, "Follow target lost", out ignore);
                return;
            }

            counselor.CantWalk = false;
            counselor.ControlTarget = target;
            counselor.ControlOrder = OrderType.Follow;
            state.Destination = target.Location;
            UpdateProgress(counselor, state);
        }

        private static void TickPath(Mobile owner, BaseCreature counselor, AIGMMovementState state)
        {
            if (counselor.Map == null)
            {
                string ignore;
                Stop(owner, counselor, out ignore);
                return;
            }

            Point3D destination = NormalizeDestination(counselor.Map, state.Destination);
            state.Destination = destination;

            if (Reached(counselor.Location, destination, 1))
            {
                if (state.UsingDetourWaypoint)
                {
                    bool detourCommittedEnough = state.SuccessfulDetourMoves >= 3 || Utility.InRange(state.DetourStartLocation, counselor.Location, 3) == false;
                    if (!detourCommittedEnough && state.DetourCommitmentPulsesRemaining > 0)
                    {
                        state.DetourCommitmentPulsesRemaining--;
                        state.LastStatus = "Holding detour band before final rejoin";
                        return;
                    }

                    if (state.CurrentStrategy == AIGMTravelPathStrategy.CommittedDetour || state.CurrentStrategy == AIGMTravelPathStrategy.BreadcrumbBacktrack)
                        state.FailedWaypoints.Add(state.ActiveDetourPoint == Point3D.Zero ? destination : state.ActiveDetourPoint);

                    state.Destination = state.FinalDestination;
                    state.DestinationName = state.FinalDestinationName;
                    state.UsingDetourWaypoint = false;
                    state.ActiveRouteBand = null;
                    state.ActiveDetourPoint = Point3D.Zero;
                    state.SuccessfulDetourMoves = 0;
                    state.DetourStartLocation = Point3D.Zero;
                    state.StuckTicks = 0;
                    state.PathFailureCount = 0;
                    state.LastStatus = "Detour complete, resuming final route";
                    return;
                }

                string arrivedStatus = String.Format("Arrived at {0}", !String.IsNullOrWhiteSpace(state.DestinationName) ? state.DestinationName : FormatPoint(destination));
                ExecuteArrivalAction(owner, counselor, state, arrivedStatus);
                return;
            }

            bool shouldRun = ShouldRun(counselor, destination);
            RememberBreadcrumb(state, counselor.Location);
            if (IsOscillating(state, counselor.Location))
            {
                state.OscillationCount++;
                LogMovement("TRAVEL_OSCILLATION_DETECTED count=" + state.OscillationCount + " location=" + FormatPoint(counselor.Location));

                if (!String.IsNullOrWhiteSpace(state.ActiveRouteBand))
                {
                    state.SuppressedRouteBand = state.ActiveRouteBand;
                    state.SuppressedRouteBandUntilUtc = DateTime.UtcNow + TimeSpan.FromSeconds(20.0);
                }

                Point3D backtrack = GetBreadcrumbBacktrack(state, counselor.Location);
                if (backtrack != counselor.Location)
                {
                    state.Destination = backtrack;
                    state.DestinationName = "breadcrumb backtrack";
                    state.UsingDetourWaypoint = true;
                    state.CurrentStrategy = AIGMTravelPathStrategy.BreadcrumbBacktrack;
                    state.DetourCommitmentPulsesRemaining = 8;
                    state.PathFailureCount = 0;
                    state.StuckTicks = 0;
                    LogMovement("TRAVEL_BACKTRACK_START target=" + FormatPoint(backtrack));
                }
            }

            bool moved;
            if ((state.PathFailureCount >= 3 || state.ConsecutiveNoProgressChecks >= 3) && TryPathFollowerStep(counselor, state, destination, shouldRun, 1))
            {
                moved = true;
                state.CurrentStrategy = AIGMTravelPathStrategy.PathFollower;
                LogMovement("TRAVEL_STRATEGY strategy=PathFollower");
            }
            else
            {
                moved = StepToward(counselor, destination, shouldRun);
                state.CurrentStrategy = AIGMTravelPathStrategy.DirectStep;
            }

            if (moved && counselor.Location != state.LastObservedLocation)
            {
                state.LastObservedLocation = counselor.Location;
                state.LastProgressUtc = DateTime.UtcNow;
                state.StuckTicks = 0;
                state.PathFailureCount = 0;
                state.ConsecutiveBlockedSteps = 0;
                state.ConsecutiveNoProgressChecks = 0;
                state.ConsecutivePathFollowerFailures = 0;
                if (state.UsingDetourWaypoint)
                    state.SuccessfulDetourMoves++;
                state.LastMoveWasRunning = shouldRun;
                state.LastStatus = String.Format("Moving toward {0}", !String.IsNullOrWhiteSpace(state.DestinationName) ? state.DestinationName : FormatPoint(destination));
                return;
            }

            state.StuckTicks++;
            state.PathFailureCount++;
            state.ConsecutiveBlockedSteps++;
            state.ConsecutiveNoProgressChecks++;
            LogMovement("TRAVEL_DIRECT_BLOCKED count=" + state.ConsecutiveBlockedSteps);

            if (state.StuckTicks >= 3)
            {
                Point3D alternative = GetAlternativeStep(counselor.Map, counselor.Location, destination);
                if (alternative != counselor.Location && TryMoveTo(counselor, alternative, shouldRun))
                {
                    state.LastObservedLocation = counselor.Location;
                    state.LastProgressUtc = DateTime.UtcNow;
                    state.StuckTicks = 0;
                    state.LastStatus = "Repathing around obstacle";
                    return;
                }
            }

            if (state.PathFailureCount >= 5 && !state.UsingDetourWaypoint && state.DetourAttempts < 4)
            {
                Point3D detour = GetFallbackWaypoint(counselor.Map, counselor.Location, state.FinalDestination, state.DetourAttempts);
                if (detour != counselor.Location && !ContainsNearbyFailedWaypoint(state, detour))
                {
                    string band = GetRouteBand(counselor.Location, detour);
                    if (String.IsNullOrWhiteSpace(state.SuppressedRouteBand) || DateTime.UtcNow >= state.SuppressedRouteBandUntilUtc || !String.Equals(state.SuppressedRouteBand, band, StringComparison.OrdinalIgnoreCase))
                    {
                        state.Destination = detour;
                        state.DestinationName = "detour waypoint";
                        state.UsingDetourWaypoint = true;
                        state.DetourAttempts++;
                        state.ConsecutiveDetourFailures = 0;
                        state.DetourCommitmentPulsesRemaining = 10;
                        state.SuccessfulDetourMoves = 0;
                        state.DetourStartLocation = counselor.Location;
                        state.ActiveDetourPoint = detour;
                        state.ActiveRouteBand = band;
                        state.StuckTicks = 0;
                        state.PathFailureCount = 0;
                        state.CurrentStrategy = AIGMTravelPathStrategy.CommittedDetour;
                        state.LastStatus = "Attempting detour waypoint";
                        LogMovement("TRAVEL_DETOUR_CHOSEN band=" + (state.ActiveRouteBand ?? "unknown") + " point=" + FormatPoint(detour));
                        return;
                    }
                }
            }

            if (state.PathFailureCount >= 8)
            {
                string ignore;
                Stop(owner, counselor, String.Format("Blocked before reaching {0}", !String.IsNullOrWhiteSpace(state.FinalDestinationName) ? state.FinalDestinationName : FormatPoint(state.FinalDestination)), out ignore);
                return;
            }

            state.LastStatus = state.UsingDetourWaypoint ? "Following detour waypoint" : "Pathing around obstacle";
        }

        private static bool StepToward(BaseCreature counselor, Point3D destination, bool shouldRun)
        {
            Point3D[] candidates = BuildStepCandidates(counselor, destination);
            for (int i = 0; i < candidates.Length; i++)
            {
                Point3D candidate = candidates[i];
                if (candidate != counselor.Location && TryMoveTo(counselor, candidate, shouldRun))
                    return true;
            }

            return false;
        }

        private static void UpdateProgress(BaseCreature counselor, AIGMMovementState state)
        {
            if (counselor.Location != state.LastObservedLocation)
            {
                state.LastObservedLocation = counselor.Location;
                state.LastProgressUtc = DateTime.UtcNow;
                state.StuckTicks = 0;
                state.PathFailureCount = 0;
                state.LastStatus = "Following";
            }
            else
            {
                state.StuckTicks = Math.Min(state.StuckTicks + 1, 9999);
            }
        }

        private static bool ShouldRun(BaseCreature counselor, Point3D destination)
        {
            return counselor != null && counselor.GetDistanceToSqrt(destination) > 6;
        }

        private static Point3D[] BuildStepCandidates(BaseCreature counselor, Point3D destination)
        {
            int dx = Math.Sign(destination.X - counselor.X);
            int dy = Math.Sign(destination.Y - counselor.Y);

            Point3D primary = NormalizeDestination(counselor.Map, new Point3D(counselor.X + dx, counselor.Y + dy, counselor.Z));
            Point3D xOnly = NormalizeDestination(counselor.Map, new Point3D(counselor.X + dx, counselor.Y, counselor.Z));
            Point3D yOnly = NormalizeDestination(counselor.Map, new Point3D(counselor.X, counselor.Y + dy, counselor.Z));
            Point3D sideA = NormalizeDestination(counselor.Map, new Point3D(counselor.X + dx, counselor.Y - dy, counselor.Z));
            Point3D sideB = NormalizeDestination(counselor.Map, new Point3D(counselor.X - dx, counselor.Y + dy, counselor.Z));

            return new Point3D[] { primary, xOnly, yOnly, sideA, sideB };
        }

        private static bool TryMoveTo(BaseCreature counselor, Point3D candidate, bool shouldRun)
        {
            if (counselor == null || counselor.Map == null || candidate == counselor.Location || !CanOccupy(counselor.Map, candidate))
                return false;

            Direction dir = counselor.GetDirectionTo(candidate);
            if (shouldRun)
                dir |= Direction.Running;

            if (!counselor.Move(dir))
                return false;

            counselor.ProcessDelta();
            return true;
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

        private static bool TryPathFollowerStep(BaseCreature counselor, AIGMMovementState state, Point3D goal, bool run, int range)
        {
            if (counselor == null || counselor.Map == null || !Utility.InRange(counselor.Location, goal, 38))
                return false;

            if (state.ActivePathFollower == null || DateTime.UtcNow >= state.LastPathFollowerRepathUtc + TimeSpan.FromSeconds(2.0))
            {
                state.ActivePathFollower = new PathFollower(counselor, goal);
                state.LastPathFollowerRepathUtc = DateTime.UtcNow;
                LogMovement("TRAVEL_PATHFOLLOWER_START goal=" + FormatPoint(goal));
            }

            bool arrived = state.ActivePathFollower.Follow(run, range);
            LogMovement("TRAVEL_PATHFOLLOWER_STEP result=" + arrived + " goal=" + FormatPoint(goal));
            if (!arrived && counselor.Location == state.LastObservedLocation)
            {
                state.ConsecutivePathFollowerFailures++;
                LogMovement("TRAVEL_PATHFOLLOWER_FAIL count=" + state.ConsecutivePathFollowerFailures);
                if (state.ConsecutivePathFollowerFailures >= 2)
                    state.ActivePathFollower.ForceRepath();
            }

            return true;
        }

        private static void RememberBreadcrumb(AIGMMovementState state, Point3D location)
        {
            if (state == null)
                return;

            Point3D[] points = state.Breadcrumbs.ToArray();
            if (points.Length == 0 || points[points.Length - 1] != location)
            {
                state.Breadcrumbs.Enqueue(location);
                while (state.Breadcrumbs.Count > 24)
                    state.Breadcrumbs.Dequeue();
            }
        }

        private static bool IsOscillating(AIGMMovementState state, Point3D location)
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

        private static Point3D GetBreadcrumbBacktrack(AIGMMovementState state, Point3D fallback)
        {
            if (state == null || state.Breadcrumbs == null || state.Breadcrumbs.Count == 0)
                return fallback;

            Point3D[] points = state.Breadcrumbs.ToArray();
            int index = Math.Max(0, points.Length - 10);
            return points[index];
        }

        private static bool ContainsNearbyFailedWaypoint(AIGMMovementState state, Point3D point)
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

            if (System.Math.Abs(dx) >= System.Math.Abs(dy))
                return dy >= 0 ? "SouthBypass" : "NorthBypass";

            return dx >= 0 ? "EastBypass" : "WestBypass";
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

        public static void ConfigureArrivalAction(Mobile from, string actionKind, string actionArgument)
        {
            AIGMMovementState state = AIGMMovementState.Get(from);
            if (state == null)
                return;

            state.ArrivalActionKind = String.IsNullOrWhiteSpace(actionKind) ? null : actionKind.Trim();
            state.ArrivalActionArgument = String.IsNullOrWhiteSpace(actionArgument) ? null : actionArgument.Trim();
        }

        public static bool Pause(Mobile from, out string message)
        {
            message = null;
            AIGMMovementState state = AIGMMovementState.Get(from);
            if (state == null || state.Mode == AIGMMovementMode.None)
            {
                message = "There is no active movement to pause.";
                return false;
            }

            BaseCreature counselor = World.FindMobile(state.CounselorSerial) as BaseCreature;
            if (counselor == null || counselor.Deleted)
            {
                message = "The counselor is not available to pause.";
                return false;
            }

            state.IsPaused = true;
            state.PausedMode = state.Mode;
            state.PausedFollowerSerial = state.FollowerSerial;
            state.PausedDestination = state.Destination;
            state.PausedDestinationName = state.DestinationName;
            state.Mode = AIGMMovementMode.None;
            state.LastStatus = "Paused";
            StopTimer(state);

            counselor.ControlTarget = null;
            counselor.ControlOrder = OrderType.Stay;
            counselor.CantWalk = true;
            counselor.SetControlMaster(null);

            message = String.Format("{0} paused movement.", counselor.Name ?? "The counselor");
            return true;
        }

        public static bool Resume(Mobile from, out string message)
        {
            message = null;
            AIGMMovementState state = AIGMMovementState.Get(from);
            if (state == null || !state.IsPaused)
            {
                message = "There is no paused movement to resume.";
                return false;
            }

            BaseCreature counselor = World.FindMobile(state.CounselorSerial) as BaseCreature;
            if (counselor == null || counselor.Deleted)
            {
                message = "The counselor is not available to resume.";
                return false;
            }

            if (state.PausedMode == AIGMMovementMode.Follow)
            {
                Mobile target = World.FindMobile(state.PausedFollowerSerial);
                if (target == null || target.Deleted)
                {
                    message = "The paused follow target is no longer available.";
                    return false;
                }

                bool ok = StartFollow(from, counselor, target, out message);
                if (ok)
                {
                    state.IsPaused = false;
                    state.LastStatus = "Resumed follow";
                }
                return ok;
            }

            if (state.PausedMode == AIGMMovementMode.PathToPoint || state.PausedMode == AIGMMovementMode.PathToNamedLocation)
            {
                bool ok = StartPathToPoint(from, counselor, state.PausedDestination, counselor.Map, state.PausedDestinationName, out message);
                if (ok)
                {
                    state.IsPaused = false;
                    state.LastStatus = "Resumed pathing";
                }
                return ok;
            }

            message = "The paused movement mode could not be resumed.";
            return false;
        }

        public static bool Cancel(Mobile from, out string message)
        {
            message = null;
            AIGMMovementState state = AIGMMovementState.Get(from);
            if (state == null)
            {
                message = "There is no movement state to cancel.";
                return false;
            }

            BaseCreature counselor = World.FindMobile(state.CounselorSerial) as BaseCreature;
            if (counselor == null || counselor.Deleted)
            {
                state.Mode = AIGMMovementMode.None;
                state.IsPaused = false;
                state.LastStatus = "Canceled";
                message = "Movement state cleared.";
                return true;
            }

            state.IsPaused = false;
            state.PausedMode = AIGMMovementMode.None;
            state.PausedFollowerSerial = Serial.MinusOne;
            state.PausedDestination = Point3D.Zero;
            state.PausedDestinationName = null;
            return Stop(from, counselor, "Canceled", out message);
        }

        private static void ExecuteArrivalAction(Mobile owner, BaseCreature counselor, AIGMMovementState state, string arrivedStatus)
        {
            string action = state != null ? state.ArrivalActionKind : null;
            string argument = state != null ? state.ArrivalActionArgument : null;

            if (state != null && state.RouteStops != null && state.RouteStops.Count > 0)
            {
                AIGMRouteStop nextStop = state.RouteStops[0];
                state.RouteStops.RemoveAt(0);
                string ignore;
                StartPathToPoint(owner, counselor, nextStop.Location, counselor.Map, nextStop.Name, out ignore);
                AIGMMovementState refreshed = AIGMMovementState.Get(owner);
                if (refreshed != null)
                    refreshed.LastStatus = arrivedStatus + " and continuing to next stop";
                return;
            }

            if (String.IsNullOrWhiteSpace(action) || String.Equals(action, "wait", StringComparison.OrdinalIgnoreCase))
            {
                string ignore;
                Stop(owner, counselor, arrivedStatus + " and waiting", out ignore);
                return;
            }

            if (String.Equals(action, "follow_me", StringComparison.OrdinalIgnoreCase))
            {
                string ignore;
                StartFollow(owner, counselor, owner, out ignore);
                AIGMMovementState refreshed = AIGMMovementState.Get(owner);
                if (refreshed != null)
                    refreshed.LastStatus = arrivedStatus + " and resumed follow";
                return;
            }

            if (String.Equals(action, "scan_nearby_mobiles", StringComparison.OrdinalIgnoreCase))
            {
                string ignore;
                Stop(owner, counselor, arrivedStatus + " and scanned nearby mobiles", out ignore);
                AIGMActionExecutor.ExecuteSystemAction(owner, AIGMCommandAction.InspectNearbyMobiles, out ignore);
                return;
            }

            if (String.Equals(action, "scan_nearby_items", StringComparison.OrdinalIgnoreCase))
            {
                string ignore;
                Stop(owner, counselor, arrivedStatus + " and scanned nearby items", out ignore);
                AIGMActionExecutor.ExecuteSystemAction(owner, AIGMCommandAction.InspectNearbyItems, out ignore);
                return;
            }

            string fallback;
            Stop(owner, counselor, arrivedStatus, out fallback);
        }

        private static string SafeName(Mobile mob)
        {
            if (mob == null)
                return "(null)";

            return (mob.Name ?? mob.GetType().Name) + "[0x" + mob.Serial.Value.ToString("X8") + "]";
        }

        private static void LogCompanionState(string prefix, BaseCreature actor, Mobile requester)
        {
            LogMovement(prefix
                + " controlled=" + (actor != null && actor.Controlled)
                + " master=" + SafeName(actor != null ? actor.ControlMaster : null)
                + " order=" + (actor != null ? actor.ControlOrder.ToString() : String.Empty)
                + " target=" + ((actor != null && actor.ControlTarget is Mobile) ? SafeName((Mobile)actor.ControlTarget) : "(null)")
                + " slots=" + (actor != null ? actor.ControlSlots.ToString() : String.Empty)
                + " followers=" + (requester != null ? requester.Followers.ToString() : String.Empty)
                + " followersMax=" + (requester != null ? requester.FollowersMax.ToString() : String.Empty));
        }

        private static void LogMovement(string message)
        {
            try
            {
                string path = System.IO.Path.Combine(Core.BaseDirectory, "Logs", "AIGMMovement.log");
                System.IO.File.AppendAllText(path, DateTime.UtcNow.ToString("o") + " " + (message ?? String.Empty) + Environment.NewLine);
            }
            catch
            {
            }
        }

        private static void StopTimer(AIGMMovementState state)
        {
            if (state != null && state.Timer != null)
            {
                state.Timer.Stop();
                state.Timer = null;
            }
        }

        private static bool Reached(Point3D a, Point3D b, int tolerance)
        {
            return Math.Abs(a.X - b.X) <= tolerance && Math.Abs(a.Y - b.Y) <= tolerance;
        }

        private static string FormatPoint(Point3D p)
        {
            return String.Format("{0},{1},{2}", p.X, p.Y, p.Z);
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

        private static Point3D GetNudgeDestination(Map map, Point3D current, Point3D destination)
        {
            int[] deltas = new int[] { 1, 0, -1 };
            foreach (int dx in deltas)
            {
                foreach (int dy in deltas)
                {
                    if (dx == 0 && dy == 0)
                        continue;

                    Point3D candidate = NormalizeDestination(map, new Point3D(current.X + dx, current.Y + dy, current.Z));
                    if (CanOccupy(map, candidate))
                        return candidate;
                }
            }

            return current;
        }
    }

    public class AIGMRouteStop
    {
        public string Name { get; private set; }
        public Point3D Location { get; private set; }

        public AIGMRouteStop(string name, Point3D location)
        {
            Name = name;
            Location = location;
        }
    }

    public class AIGMNamedDestination
    {
        public string Name { get; private set; }
        public Map Map { get; private set; }
        public Point3D Location { get; private set; }

        public AIGMNamedDestination(string name, Map map, Point3D location)
        {
            Name = name;
            Map = map;
            Location = location;
        }
    }
}
