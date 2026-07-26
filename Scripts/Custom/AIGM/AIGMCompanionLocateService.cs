using System;
using System.Collections.Generic;

using Server;
using Server.Mobiles;
using Server.Custom.AIGM.Tasks;

namespace Server.Custom.AIGM
{
    public enum AIGMCompanionLocateKind
    {
        Locate,
        Regroup
    }

    public sealed class AIGMCompanionLocateState
    {
        public int CompanionSerial;
        public int OwnerSerial;
        public int TargetSerial;
        public string TargetName;
        public AIGMCompanionLocateKind Kind;
        public int Radius;
        public DateTime StartedUtc;
        public DateTime UpdatedUtc;
        public Point3D LastTargetLocation;
        public string LastResult;
    }

    public sealed class AIGMNamedMobileResolution
    {
        public Mobile Target;
        public List<Mobile> Matches = new List<Mobile>();
        public string Message;
        public bool Ambiguous;
    }

    public static class AIGMCompanionLocateService
    {
        private static readonly Dictionary<int, AIGMCompanionLocateState> States = new Dictionary<int, AIGMCompanionLocateState>();
        private static readonly Dictionary<int, LocateTimer> Timers = new Dictionary<int, LocateTimer>();
        private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(1.0);
        private const int DefaultRange = 64;
        private const int DefaultRadius = 2;

        public static bool TryHandleSpeech(BaseHire companion, Mobile owner, string speech, AIGMCompanionCommandRouteDecision decision, bool shouldSpeak, out string response)
        {
            response = null;
            if (companion == null || owner == null || String.IsNullOrWhiteSpace(speech))
                return false;

            if (decision != null && decision.VerbKind == AIGMCompanionCommandVerbKind.Travel)
                return false;

            string normalized = Normalize(speech);
            bool isRegroup;
            string actorName;
            string targetName;
            if (!TryParseLocateSpeech(companion, owner, normalized, decision, out isRegroup, out actorName, out targetName))
                return false;

            if (String.IsNullOrWhiteSpace(actorName) || IsGroupToken(actorName))
            {
                response = StartGroupLocate(owner, targetName, isRegroup);
                return true;
            }

            BaseHire actor = ResolveOwnedCompanion(owner, actorName, DefaultRange);
            if (actor == null)
            {
                response = "No owned companion matched '" + actorName + "'.";
                return true;
            }

            response = StartLocateByName(actor, owner, targetName, isRegroup ? AIGMCompanionLocateKind.Regroup : AIGMCompanionLocateKind.Locate);
            return true;
        }

        public static string StartGroupLocate(Mobile owner, string targetName, bool regroup)
        {
            if (owner == null)
                return "No owner mobile supplied.";

            Mobile target;
            if (String.IsNullOrWhiteSpace(targetName) || targetName == "me")
                target = owner;
            else if (targetName == "each other")
                target = ResolveOwnedCompanion(owner, "dakeyras", DefaultRange) ?? owner;
            else
            {
                AIGMNamedMobileResolution resolution = ResolveNamedMobile(owner, targetName, DefaultRange);
                if (resolution.Target == null)
                    return resolution.Message;
                target = resolution.Target;
            }

            List<BaseHire> companions = AIGMCompanionControlStopService.GetOwnedCompanions(owner, 0);
            int started = 0;
            for (int i = 0; i < companions.Count; i++)
            {
                BaseHire hire = companions[i];
                if (hire == null || hire == target)
                    continue;

                string ignored;
                if (StartLocate(hire, owner, target, regroup ? AIGMCompanionLocateKind.Regroup : AIGMCompanionLocateKind.Locate, DefaultRadius, out ignored))
                    started++;
            }

            AIGMExecutionLog.Write("AIGM_REGROUP_START owner={0} target={1} companions={2} kind={3}", Describe(owner), Describe(target), started, regroup ? "regroup" : "locate");
            return started == 0 ? "No companion needed to move." : String.Format("{0} companion{1} moving toward {2}.", started, started == 1 ? String.Empty : "s", SafeName(target));
        }

        public static string StartLocateByName(BaseHire companion, Mobile owner, string targetName, AIGMCompanionLocateKind kind)
        {
            if (companion == null || owner == null)
                return "No companion or owner supplied.";

            if (String.IsNullOrWhiteSpace(targetName) || targetName == "me")
            {
                string ownerResponse;
                StartLocate(companion, owner, owner, kind, DefaultRadius, out ownerResponse);
                return ownerResponse;
            }

            AIGMNamedMobileResolution resolution = ResolveNamedMobile(owner, targetName, DefaultRange);
            if (resolution.Target == null)
                return resolution.Message;

            string response;
            StartLocate(companion, owner, resolution.Target, kind, DefaultRadius, out response);
            return response;
        }

        public static bool StartLocate(BaseHire companion, Mobile owner, Mobile target, AIGMCompanionLocateKind kind, int radius, out string response)
        {
            response = null;
            if (!IsValidCompanion(companion))
            {
                response = "Companion cannot move right now.";
                return false;
            }

            if (target == null || target.Deleted || !target.Alive || target.Map == null || target.Map == Map.Internal)
            {
                response = "Target is not available.";
                return false;
            }

            if (companion.Map != target.Map)
            {
                response = "Target is not on the same map.";
                return false;
            }

            string operationalRejection;
            if (!AIGMOperationalControlService.BeginExplicitCommand(companion, owner, kind == AIGMCompanionLocateKind.Regroup ? "regroup" : "locate", out operationalRejection))
            {
                response = "Operational control rejected locate: " + operationalRejection + ".";
                return false;
            }

            AIGMCompanionControlStopService.StopCompanionActivity(companion, AIGMCompanionStopScope.Movement | AIGMCompanionStopScope.Navigation | AIGMCompanionStopScope.Regroup | AIGMCompanionStopScope.Tracking | AIGMCompanionStopScope.Hunt, "locate_takeover");

            if (companion.InRange(target, Math.Max(1, radius)))
            {
                companion.ControlTarget = null;
                companion.ControlOrder = OrderType.Stay;
                AIGMCompanionModeService.SetMode(companion, kind == AIGMCompanionLocateKind.Regroup ? AIGMCompanionMode.Regroup : AIGMCompanionMode.LocateMobile, "locate_already_near");
                response = companion.Name + " is already near " + SafeName(target) + ".";
                AIGMExecutionLog.Write("AIGM_LOCATE_ARRIVED companion={0} target={1} distance={2} reason=already_near", Describe(companion), Describe(target), GetDistance(companion, target));
                return true;
            }

            AIGMCompanionLocateState state = GetOrCreateState(companion);
            state.CompanionSerial = companion.Serial.Value;
            state.OwnerSerial = owner != null ? owner.Serial.Value : 0;
            state.TargetSerial = target.Serial.Value;
            state.TargetName = SafeName(target);
            state.Kind = kind;
            state.Radius = Math.Max(1, radius);
            state.StartedUtc = DateTime.UtcNow;
            state.UpdatedUtc = state.StartedUtc;
            state.LastTargetLocation = target.Location;
            state.LastResult = "started";

            string moveResponse;
            AIGMSmartMovementService.StartMoveToPoint(companion, target.Location, target.Map, "locate:" + SafeName(target), state.Radius, owner, out moveResponse);
            AIGMCompanionModeService.SetMode(companion, kind == AIGMCompanionLocateKind.Regroup ? AIGMCompanionMode.Regroup : AIGMCompanionMode.LocateMobile, kind == AIGMCompanionLocateKind.Regroup ? "regroup_start" : "locate_start");
            StartTimer(companion);

            response = String.Format("{0} moving toward {1}, no attack order set.", companion.Name, SafeName(target));
            AIGMExecutionLog.Write("AIGM_LOCATE_START companion={0} target={1} kind={2} radius={3} distance={4}", Describe(companion), Describe(target), kind, state.Radius, GetDistance(companion, target));
            return true;
        }

        public static void StopLocate(BaseHire companion, string reason)
        {
            if (companion == null)
                return;

            AIGMCompanionLocateState state;
            if (States.TryGetValue(companion.Serial.Value, out state))
            {
                state.LastResult = String.IsNullOrWhiteSpace(reason) ? "stopped" : reason;
                States.Remove(companion.Serial.Value);
            }

            StopTimer(companion.Serial.Value);
            string smartResponse;
            AIGMSmartMovementService.Stop(companion, String.IsNullOrWhiteSpace(reason) ? "locate_stop" : reason, out smartResponse);
            AIGMCompanionModeService.ClearIfAny(companion, "locate_stop", AIGMCompanionMode.LocateMobile, AIGMCompanionMode.Regroup);
            AIGMExecutionLog.Write("AIGM_LOCATE_STOP companion={0} reason={1}", Describe(companion), SafeLog(reason));
        }

        public static string GetStatus(BaseHire companion)
        {
            if (companion == null)
                return "locate=none";

            AIGMCompanionLocateState state;
            if (!States.TryGetValue(companion.Serial.Value, out state))
                return "locate=inactive";

            Mobile target = World.FindMobile(state.TargetSerial);
            int distance = target != null && target.Map == companion.Map ? GetDistance(companion, target) : -1;
            return String.Format("locate={0}; target={1}; distance={2}; radius={3}; last={4}", state.Kind, state.TargetName, distance, state.Radius, state.LastResult);
        }

        public static AIGMNamedMobileResolution ResolveNamedMobile(Mobile origin, string name, int range)
        {
            AIGMNamedMobileResolution result = new AIGMNamedMobileResolution();
            if (origin == null || origin.Map == null)
            {
                result.Message = "No map context for lookup.";
                return result;
            }

            string needle = NormalizeTargetName(name);
            if (String.IsNullOrWhiteSpace(needle) || needle == "me")
            {
                result.Target = origin;
                return result;
            }

            BaseHire ownedCompanion = ResolveOwnedCompanion(origin, needle, 0);
            if (ownedCompanion != null)
            {
                result.Target = ownedCompanion;
                result.Matches.Add(ownedCompanion);
                return result;
            }

            IPooledEnumerable mobiles = origin.Map.GetMobilesInRange(origin.Location, Math.Max(1, range));
            foreach (Mobile mobile in mobiles)
            {
                if (mobile == null || mobile.Deleted || !mobile.Alive || mobile.Map != origin.Map || mobile == origin)
                    continue;

                if (MatchesTarget(mobile, needle))
                    result.Matches.Add(mobile);
            }
            mobiles.Free();

            if (result.Matches.Count == 0)
            {
                result.Message = "No mobile named '" + name + "' found within " + range + " tiles.";
                AIGMExecutionLog.Write("AIGM_LOCATE_NOT_FOUND origin={0} target=\"{1}\" range={2}", Describe(origin), SafeLog(name), range);
                return result;
            }

            result.Matches.Sort(delegate(Mobile a, Mobile b)
            {
                return GetDistance(origin, a).CompareTo(GetDistance(origin, b));
            });

            if (result.Matches.Count > 1 && IsAmbiguous(result.Matches, needle))
            {
                result.Ambiguous = true;
                result.Message = "Ambiguous target '" + name + "': " + FormatMatches(result.Matches, origin, 4) + ".";
                AIGMExecutionLog.Write("AIGM_LOCATE_AMBIGUOUS origin={0} target=\"{1}\" matches=\"{2}\"", Describe(origin), SafeLog(name), SafeLog(FormatMatches(result.Matches, origin, 6)));
                return result;
            }

            result.Target = result.Matches[0];
            return result;
        }

        private static string Tick(BaseHire companion)
        {
            if (companion == null || companion.Deleted)
                return "actor_invalid";

            AIGMCompanionLocateState state;
            if (!States.TryGetValue(companion.Serial.Value, out state))
                return "inactive";

            AIGMOperationalAction action = state.Kind == AIGMCompanionLocateKind.Regroup ? AIGMOperationalAction.Regroup : AIGMOperationalAction.Locate;
            if (!AIGMOperationalControlService.CanOperate(companion, action, AIGMOperationalControlService.GetEpoch(companion), false))
            {
                StopLocate(companion, "operational_control_blocked");
                return "operational_control_blocked";
            }

            Mobile target = World.FindMobile(state.TargetSerial);
            if (target == null || target.Deleted || !target.Alive || target.Map == null || target.Map != companion.Map)
            {
                StopLocate(companion, "target_invalid");
                return "target_invalid";
            }

            int distance = GetDistance(companion, target);
            if (distance <= state.Radius)
            {
                string smartResponse;
                AIGMSmartMovementService.Stop(companion, "locate_arrived", out smartResponse);
                companion.Combatant = null;
                companion.Warmode = false;
                companion.ControlTarget = null;
                companion.ControlOrder = OrderType.Stay;
                StopTimer(companion.Serial.Value);
                States.Remove(companion.Serial.Value);
                AIGMCompanionModeService.SetMode(companion, state.Kind == AIGMCompanionLocateKind.Regroup ? AIGMCompanionMode.Regroup : AIGMCompanionMode.LocateMobile, "locate_arrived");
                AIGMExecutionLog.Write("AIGM_LOCATE_ARRIVED companion={0} target={1} distance={2} kind={3}", Describe(companion), Describe(target), distance, state.Kind);
                return "arrived";
            }

            if (target.Location != state.LastTargetLocation)
            {
                state.LastTargetLocation = target.Location;
                string moveResponse;
                AIGMSmartMovementService.StartMoveToPoint(companion, target.Location, target.Map, "locate:" + SafeName(target), state.Radius, World.FindMobile(state.OwnerSerial), out moveResponse);
                AIGMExecutionLog.Write("AIGM_LOCATE_TARGET_MOVED companion={0} target={1} distance={2} destination={3}", Describe(companion), Describe(target), distance, FormatPoint(target.Location));
            }

            state.UpdatedUtc = DateTime.UtcNow;
            state.LastResult = "moving";
            AIGMExecutionLog.Write("AIGM_LOCATE_PROGRESS companion={0} target={1} distance={2} radius={3} kind={4}", Describe(companion), Describe(target), distance, state.Radius, state.Kind);
            return "moving";
        }

        private static bool TryParseLocateSpeech(BaseHire companion, Mobile owner, string normalized, AIGMCompanionCommandRouteDecision decision, out bool isRegroup, out string actorName, out string targetName)
        {
            isRegroup = false;
            actorName = null;
            targetName = null;

            if (String.IsNullOrWhiteSpace(normalized))
                return false;

            string speech = StripCompanionPrefix(normalized, out actorName);
            if (String.IsNullOrWhiteSpace(actorName))
                actorName = decision != null && decision.RouteKind == AIGMCompanionCommandRouteKind.NamedCompanion ? decision.CompanionKey : null;

            if (speech == "companions find each other")
            {
                isRegroup = true;
                actorName = "companions";
                targetName = "each other";
                return true;
            }

            string[] groupPrefixes = new[] { "companions regroup on ", "everyone regroup on ", "regroup on ", "companions find ", "everyone to " };
            for (int i = 0; i < groupPrefixes.Length; i++)
            {
                if (speech.StartsWith(groupPrefixes[i], StringComparison.Ordinal))
                {
                    isRegroup = true;
                    actorName = "companions";
                    targetName = speech.Substring(groupPrefixes[i].Length).Trim();
                    return true;
                }
            }

            if (speech == "regroup" || speech == "companions regroup" || speech == "everyone regroup")
            {
                isRegroup = true;
                actorName = "companions";
                targetName = "me";
                return true;
            }

            string[] locatePrefixes = new[] { "start tracking ", "track ", "find ", "locate ", "go to " };
            for (int i = 0; i < locatePrefixes.Length; i++)
            {
                if (speech.StartsWith(locatePrefixes[i], StringComparison.Ordinal))
                {
                    targetName = speech.Substring(locatePrefixes[i].Length).Trim();
                    if (IsReservedTrackingTarget(targetName))
                        return false;
                    if (String.IsNullOrWhiteSpace(actorName))
                        actorName = companion != null ? companion.Name : null;
                    return !String.IsNullOrWhiteSpace(targetName);
                }
            }

            return false;
        }

        private static string StripCompanionPrefix(string normalized, out string actorName)
        {
            actorName = null;
            string[] aliases = new[] { "dakeyras", "dak", "waylander", "danyal", "dan", "dardalion", "dard", "dar", "companions", "everyone" };
            for (int i = 0; i < aliases.Length; i++)
            {
                string alias = aliases[i];
                if (normalized.StartsWith(alias + " ", StringComparison.Ordinal))
                {
                    actorName = alias;
                    return normalized.Substring(alias.Length).Trim();
                }
            }

            return normalized;
        }

        private static bool IsReservedTrackingTarget(string targetName)
        {
            string target = Normalize(targetName);
            return target == "all"
                || target == "animals"
                || target == "animal"
                || target == "monsters"
                || target == "monster"
                || target == "hostiles"
                || target == "enemies"
                || target == "npcs"
                || target == "npc"
                || target == "players"
                || target == "player"
                || target == "people"
                || target == "humans";
        }

        private static BaseHire ResolveOwnedCompanion(Mobile owner, string name, int range)
        {
            if (owner == null || String.IsNullOrWhiteSpace(name))
                return null;

            string needle = NormalizeTargetName(name);
            if (needle == "dak" || needle == "waylander")
                needle = "dakeyras";
            else if (needle == "dan")
                needle = "danyal";
            else if (needle == "dar" || needle == "dard")
                needle = "dardalion";

            List<BaseHire> companions = AIGMCompanionControlStopService.GetOwnedCompanions(owner, range <= 0 ? 0 : range);
            for (int i = 0; i < companions.Count; i++)
            {
                BaseHire hire = companions[i];
                IAIGMCompanionActor actor = hire as IAIGMCompanionActor;
                string id = actor != null ? NormalizeTargetName(actor.CompanionId) : String.Empty;
                string display = NormalizeTargetName(hire.Name);
                if (id == needle || display == needle)
                    return hire;
            }

            return null;
        }

        private static bool IsGroupToken(string name)
        {
            string value = NormalizeTargetName(name);
            return value == "companions" || value == "everyone" || value == "all";
        }

        private static bool MatchesTarget(Mobile mobile, string needle)
        {
            if (mobile == null || String.IsNullOrWhiteSpace(needle))
                return false;

            IAIGMCompanionActor actor = mobile as IAIGMCompanionActor;
            string name = NormalizeTargetName(mobile.Name);
            string type = NormalizeTargetName(mobile.GetType().Name);
            string id = actor != null ? NormalizeTargetName(actor.CompanionId) : String.Empty;

            return name == needle || id == needle || type == needle || name.StartsWith(needle, StringComparison.Ordinal) || type.StartsWith(needle, StringComparison.Ordinal);
        }

        private static bool IsAmbiguous(List<Mobile> matches, string needle)
        {
            if (matches == null || matches.Count <= 1)
                return false;

            int exact = 0;
            for (int i = 0; i < matches.Count; i++)
            {
                if (NormalizeTargetName(matches[i].Name) == needle)
                    exact++;
            }

            return exact != 1;
        }

        private static AIGMCompanionLocateState GetOrCreateState(BaseHire companion)
        {
            AIGMCompanionLocateState state;
            if (!States.TryGetValue(companion.Serial.Value, out state))
            {
                state = new AIGMCompanionLocateState();
                state.CompanionSerial = companion.Serial.Value;
                States[companion.Serial.Value] = state;
            }
            return state;
        }

        private static void StartTimer(BaseHire companion)
        {
            if (companion == null)
                return;

            StopTimer(companion.Serial.Value);
            LocateTimer timer = new LocateTimer(companion.Serial, AIGMOperationalControlService.GetEpoch(companion));
            Timers[companion.Serial.Value] = timer;
            timer.Start();
        }

        private static void StopTimer(int serial)
        {
            LocateTimer timer;
            if (Timers.TryGetValue(serial, out timer) && timer != null)
                timer.Stop();
            Timers.Remove(serial);
        }

        private static bool IsValidCompanion(BaseHire companion)
        {
            return companion != null && !companion.Deleted && companion.Alive && companion.Map != null && companion.Map != Map.Internal;
        }

        private static int GetDistance(Mobile from, Mobile to)
        {
            if (from == null || to == null || from.Map != to.Map)
                return -1;

            return (int)Math.Round(from.GetDistanceToSqrt(to));
        }

        private static string FormatMatches(List<Mobile> matches, Mobile origin, int limit)
        {
            List<string> parts = new List<string>();
            int count = Math.Min(limit, matches.Count);
            for (int i = 0; i < count; i++)
                parts.Add(SafeName(matches[i]) + "@" + GetDistance(origin, matches[i]));
            return String.Join(", ", parts.ToArray());
        }

        private static string Normalize(string text)
        {
            string value = text == null ? String.Empty : text.Trim().ToLowerInvariant();
            value = value.Replace(",", " ").Replace(".", " ").Replace("!", " ").Replace("?", " ").Replace(";", " ").Replace(":", " ");
            while (value.Contains("  "))
                value = value.Replace("  ", " ");
            return value.Trim();
        }

        private static string NormalizeTargetName(string text)
        {
            return Normalize(text).Replace("the ", String.Empty).Trim();
        }

        private static string SafeName(Mobile mobile)
        {
            return mobile == null ? "unknown" : (String.IsNullOrWhiteSpace(mobile.Name) ? mobile.GetType().Name : mobile.Name);
        }

        private static string Describe(Mobile mobile)
        {
            return mobile == null ? "none" : (SafeName(mobile) + "[" + mobile.Serial + "]");
        }

        private static string FormatPoint(Point3D point)
        {
            return point.X + "," + point.Y + "," + point.Z;
        }

        private static string SafeLog(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return String.Empty;
            return value.Replace('"', '\'').Replace('\r', ' ').Replace('\n', ' ');
        }

        private sealed class LocateTimer : Timer
        {
            private readonly Serial _companionSerial;
            private readonly int _expectedEpoch;

            public LocateTimer(Serial companionSerial, int expectedEpoch)
                : base(TickInterval, TickInterval)
            {
                _companionSerial = companionSerial;
                _expectedEpoch = expectedEpoch;
                Priority = TimerPriority.TwoFiftyMS;
            }

            protected override void OnTick()
            {
                BaseHire companion = World.FindMobile(_companionSerial) as BaseHire;
                if (!AIGMOperationalControlService.CanOperate(companion, AIGMOperationalAction.Locate, _expectedEpoch, false))
                {
                    AIGMCompanionLocateService.StopLocate(companion, "operational_callback_rejected");
                    Stop();
                    return;
                }

                string result = AIGMCompanionLocateService.Tick(companion);
                if (result == "inactive" || result == "arrived" || result.EndsWith("_invalid"))
                    Stop();
            }
        }
    }
}
