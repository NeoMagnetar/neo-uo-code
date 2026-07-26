using System;
using System.Collections.Generic;

using Server;
using Server.Custom.AIGM.Navigation;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public enum AIGMNavigationSpeechScope
    {
        None,
        Player,
        NamedCompanion,
        GroupCompanions
    }

    public sealed class AIGMNavigationSpeechIntent
    {
        public AIGMNavigationSpeechScope Scope;
        public string CompanionId;
        public string RawSpeech;
        public string Verb;
        public string DestinationText;
        public string CanonicalDestinationText;
        public string CommandType;
        public bool LeadParty;
        public bool EnableTravelSweep;
    }

    public static class AIGMNavigationIntentParser
    {
        private static readonly string[] MovementPrefixes =
        {
            "go to ",
            "go ",
            "walk to ",
            "travel to ",
            "navigate to ",
            "head to ",
            "move to ",
            "follow the road to ",
            "take me to ",
            "take us to ",
            "lead me to ",
            "lead us to "
        };

        private static readonly Dictionary<int, RecentNavigationSpeech> RecentSpeech = new Dictionary<int, RecentNavigationSpeech>();
        private static readonly TimeSpan DuplicateWindow = TimeSpan.FromSeconds(2.0);
        private const int NearTargetRange = 40;

        public static bool TryParse(string speech, out AIGMNavigationSpeechIntent intent)
        {
            intent = null;
            string normalized = NormalizeSpeech(speech);
            if (String.IsNullOrWhiteSpace(normalized) || LooksLikeDialogue(normalized))
                return false;

            AIGMNavigationSpeechScope scope = AIGMNavigationSpeechScope.GroupCompanions;
            string companionId = null;
            bool leadParty = false;

            string payload = normalized;
            if (TryStripNamedCompanion(payload, out companionId, out payload))
                scope = AIGMNavigationSpeechScope.NamedCompanion;
            else if (TryStripGroupPrefix(payload, out payload))
                scope = AIGMNavigationSpeechScope.GroupCompanions;

            string destination;
            string verb;
            bool enableSweep;
            if (!TryExtractDestination(payload, out destination, out verb, out leadParty, out enableSweep))
                return false;

            destination = NormalizeDestinationPhrase(destination);
            if (String.IsNullOrWhiteSpace(destination))
                return false;

            intent = new AIGMNavigationSpeechIntent();
            intent.Scope = scope;
            intent.CompanionId = companionId;
            intent.RawSpeech = speech;
            intent.Verb = verb;
            intent.DestinationText = destination;
            intent.CanonicalDestinationText = destination;
            intent.CommandType = "Travel";
            intent.LeadParty = leadParty;
            intent.EnableTravelSweep = enableSweep;
            AIGMExecutionLog.Write("AIGM_NAV_PARSE raw=\"{0}\" normalized=\"{1}\" scope={2} companion={3} destination=\"{4}\" leadParty={5} sweep={6}", SafeLog(speech), SafeLog(normalized), intent.Scope, SafeLog(intent.CompanionId), SafeLog(intent.DestinationText), intent.LeadParty, intent.EnableTravelSweep);
            AIGMExecutionLog.Write("AIGM_NAV_SPEECH_INTENT rawSpeech=\"{0}\" companion={1} verb={2} destinationText=\"{3}\" canonicalDestinationText=\"{4}\" commandType={5}", SafeLog(intent.RawSpeech), SafeLog(intent.CompanionId), SafeLog(intent.Verb), SafeLog(intent.DestinationText), SafeLog(intent.CanonicalDestinationText), SafeLog(intent.CommandType));
            return true;
        }

        public static bool TryHandle(BaseHire listener, Mobile speaker, string speech, AIGMCompanionCommandRouteDecision decision, bool shouldSpeak, out string response)
        {
            response = null;

            if (listener == null || listener.Deleted || speaker == null || speaker.Deleted || String.IsNullOrWhiteSpace(speech))
                return false;

            Mobile owner = listener.GetOwner();
            if (owner != speaker)
                return false;

            if (TryHandleTravelSweepSpeech(listener, speaker, speech, out response))
                return true;

            if (TryHandleStopSpeech(listener, speaker, speech, decision, shouldSpeak, out response))
                return true;

            AIGMNavigationSpeechIntent intent;
            if (!TryParse(speech, out intent) || intent == null)
                return false;

            if (!ShouldListenerHandle(listener, speaker, speech, intent))
                return false;

            AIGMNavigationLocation location;
            if (!AIGMNavigationLocationRegistry.TryResolve(intent.DestinationText, speaker.Map, out location))
            {
                response = "I do not know that road yet.";
                AIGMExecutionLog.Write("AIGM_NAV_SPEECH_RESOLVE_FAILED speaker={0} speech=\"{1}\" destination=\"{2}\" scope={3}", Describe(speaker), SafeLog(speech), SafeLog(intent.DestinationText), intent.Scope);
                return true;
            }

            intent.CanonicalDestinationText = location.DisplayName;
            AIGMExecutionLog.Write("AIGM_NAV_RESOLVER_INPUT caller=natural_speech rawInput=\"{0}\" destinationText=\"{1}\" canonicalDestinationText=\"{2}\"", SafeLog(intent.RawSpeech), SafeLog(intent.DestinationText), SafeLog(intent.CanonicalDestinationText));
            AIGMExecutionLog.Write("AIGM_NAV_RESOLVER_PARITY naturalSpeechDestination=\"{0}\" commandEquivalent=\"{1}\" sameCanonical={2} nodeId={3} legacyLocationMatch={4}", SafeLog(intent.DestinationText), SafeLog(location.DisplayName), String.Equals(intent.DestinationText, location.DisplayName, StringComparison.OrdinalIgnoreCase), SafeLog(location.Key), true);

            AIGMExecutionLog.Write("AIGM_NAV_RESOLVE raw=\"{0}\" destination=\"{1}\" canonical=\"{2}\" key={3} category={4} map={5} point={6}", SafeLog(speech), SafeLog(intent.DestinationText), SafeLog(location.DisplayName), SafeLog(location.Key), SafeLog(location.Category), SafeMap(location.Map), FormatPoint(location.Point));
            AIGMExecutionLog.Write("AIGM_NAV_COMMAND_RECEIVED speaker={0} companion={1} rawSpeech=\"{2}\" parsedDestination=\"{3}\" commandSource=natural useGraphRequested=False", Describe(speaker), intent.Scope == AIGMNavigationSpeechScope.NamedCompanion ? SafeLog(intent.CompanionId) : intent.Scope.ToString(), SafeLog(speech), SafeLog(location.DisplayName));

            int started = 0;
            if (intent.Scope == AIGMNavigationSpeechScope.NamedCompanion)
            {
                BaseHire target = FindOwnedCompanion(speaker, intent.CompanionId, 30);
                if (target == null)
                {
                    response = "I do not see that companion close enough to send.";
                    return true;
                }

                string startMessage;
                string routeMode;
                started += StartNavigationJob(target, speaker, location, intent.RawSpeech, intent.DestinationText, intent.EnableTravelSweep, out startMessage, out routeMode);
                response = started > 0 ? String.Format("{0}: {1}", SafeName(target), startMessage) : String.Format("{0}: {1}", SafeName(target), BuildRefusalMessage(target, speaker, location, intent));
            }
            else if (intent.Scope == AIGMNavigationSpeechScope.GroupCompanions)
            {
                List<BaseHire> companions = FindOwnedCompanions(speaker, 0);
                List<string> receiptLines = new List<string>();
                for (int i = 0; i < companions.Count; i++)
                {
                    if (!companions[i].InRange(speaker, 30))
                    {
                        receiptLines.Add(String.Format("{0}: out of range", SafeName(companions[i])));
                        continue;
                    }

                    string startMessage;
                    string routeMode;
                    int result = StartNavigationJob(companions[i], speaker, location, intent.RawSpeech, intent.DestinationText, intent.EnableTravelSweep && i == 0, out startMessage, out routeMode);
                    started += result;
                    receiptLines.Add(String.Format("{0}: {1}", SafeName(companions[i]), result > 0 ? "started" : "command rejected"));
                }

                response = receiptLines.Count > 0
                    ? BuildGroupTravelReceipt(location.DisplayName, receiptLines)
                    : BuildRefusalMessage(listener, speaker, location, intent);
            }
            else
            {
                string startMessage;
                string routeMode;
                started += StartNavigationJob(speaker, speaker, location, intent.RawSpeech, intent.DestinationText, false, out startMessage, out routeMode);
                response = started > 0 ? String.Format("You begin walking toward {0}.", location.DisplayName) : "You cannot start that road.";
            }

            AIGMExecutionLog.Write("AIGM_NAV_SPEECH_START speaker={0} listener={1} scope={2} companion={3} destination=\"{4}\" started={5}", Describe(speaker), Describe(listener), intent.Scope, SafeLog(intent.CompanionId), SafeLog(location.DisplayName), started);
            return true;
        }

        private static bool TryHandleStopSpeech(BaseHire listener, Mobile speaker, string speech, AIGMCompanionCommandRouteDecision decision, bool shouldSpeak, out string response)
        {
            response = null;
            string normalized = NormalizeSpeech(speech);
            if (!IsStopNavigationSpeech(normalized))
                return false;

            if (!ShouldHandleStop(listener, speaker, normalized))
                return false;

            bool handled = AIGMCompanionControlStopService.TryHandleSpeech(listener, speaker, speech, decision, shouldSpeak, out response);
            if (handled)
            {
                AIGMExecutionLog.Write("AIGM_NAV_SPEECH_STOP speaker={0} listener={1} speech=\"{2}\" delegated=canonical_stop_service", Describe(speaker), Describe(listener), SafeLog(speech));
                return true;
            }

            return false;
        }

        private static bool TryHandleTravelSweepSpeech(BaseHire listener, Mobile speaker, string speech, out string response)
        {
            response = null;
            string normalized = NormalizeSpeech(speech);
            if (String.IsNullOrWhiteSpace(normalized))
                return false;

            string companionId;
            string payload = normalized;
            bool named = TryStripNamedCompanion(payload, out companionId, out payload);
            if (named)
            {
                IAIGMCompanionActor actor = listener as IAIGMCompanionActor;
                if (actor == null || !String.Equals(actor.CompanionId, companionId, StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            else if (!IsPreferredCompanion(listener, speaker))
            {
                return false;
            }

            if (payload == "turn on travel tracking" || payload == "travel with passive tracking" || payload == "guard the road while traveling")
            {
                response = "That navigation test command is not available in the legacy baseline.";
                return true;
            }

            if (payload == "stop travel tracking" || payload == "turn off travel tracking")
            {
                response = "That navigation test command is not available in the legacy baseline.";
                return true;
            }

            return false;
        }

        private static bool ShouldHandleStop(BaseHire listener, Mobile speaker, string normalizedSpeech)
        {
            if (TryStripGroupPrefix(normalizedSpeech, out _))
                return IsPreferredCompanion(listener, speaker);

            return IsPreferredCompanion(listener, speaker);
        }

        private static bool IsStopNavigationSpeech(string normalized)
        {
            if (String.IsNullOrWhiteSpace(normalized))
                return false;

            return normalized.Equals("stop walking", StringComparison.Ordinal)
                || normalized.Equals("stop moving", StringComparison.Ordinal)
                || normalized.Equals("stop navigation", StringComparison.Ordinal)
                || normalized.Equals("stop traveling", StringComparison.Ordinal)
                || normalized.Equals("stop travel tracking", StringComparison.Ordinal)
                || normalized.Equals("turn off travel tracking", StringComparison.Ordinal)
                || normalized.Equals("stop travel", StringComparison.Ordinal)
                || normalized.Equals("cancel navigation", StringComparison.Ordinal)
                || normalized.Equals("cancel walking", StringComparison.Ordinal)
                || normalized.Equals("cancel traveling", StringComparison.Ordinal)
                || normalized.Equals("cancel travel", StringComparison.Ordinal)
                || normalized.Equals("halt", StringComparison.Ordinal)
                || normalized.Equals("everyone stop", StringComparison.Ordinal)
                || normalized.Equals("everybody stop", StringComparison.Ordinal)
                || normalized.Equals("companions stop", StringComparison.Ordinal)
                || normalized.Equals("all companions stop", StringComparison.Ordinal);
        }

        private static bool ShouldListenerHandle(BaseHire listener, Mobile speaker, string speech, AIGMNavigationSpeechIntent intent)
        {
            if (intent == null)
                return false;

            if (intent.Scope == AIGMNavigationSpeechScope.NamedCompanion)
            {
                IAIGMCompanionActor actor = listener as IAIGMCompanionActor;
                return actor != null && String.Equals(actor.CompanionId, intent.CompanionId, StringComparison.OrdinalIgnoreCase);
            }

            if (!IsPreferredCompanion(listener, speaker))
                return false;

            int speakerSerial = speaker != null ? speaker.Serial.Value : 0;
            string key = NormalizeSpeech(speech) + "|" + intent.Scope + "|" + intent.DestinationText;
            RecentNavigationSpeech recent;
            DateTime now = DateTime.UtcNow;
            if (RecentSpeech.TryGetValue(speakerSerial, out recent) && recent != null && recent.Key == key && now - recent.Utc < DuplicateWindow)
                return false;

            RecentSpeech[speakerSerial] = new RecentNavigationSpeech { Key = key, Utc = now };
            return true;
        }

        private static bool IsPreferredCompanion(BaseHire listener, Mobile speaker)
        {
            if (listener == null || speaker == null)
                return false;

            BaseHire preferred = null;
            List<BaseHire> companions = FindOwnedCompanions(speaker, 30);
            for (int i = 0; i < companions.Count; i++)
            {
                IAIGMCompanionActor actor = companions[i] as IAIGMCompanionActor;
                if (actor != null && String.Equals(actor.CompanionId, "dakeyras", StringComparison.OrdinalIgnoreCase))
                {
                    preferred = companions[i];
                    break;
                }

                if (preferred == null)
                    preferred = companions[i];
            }

            return preferred == listener;
        }

        private static int StartNavigationJob(Mobile actor, Mobile requester, AIGMNavigationLocation location, string rawSpeech, string destinationText, bool enableSweep, out string visibleResponse, out string routeMode)
        {
            visibleResponse = null;
            routeMode = "NoKnownDestination";
            if (actor == null || location == null)
                return 0;

            BaseHire companion = actor as BaseHire;
            AIGMNavigationDestinationResolution resolution = AIGMNavigationDestinationResolver.Resolve(location, actor.Location, actor.Map);
            AIGMExecutionLog.Write("AIGM_NAV_DEST_RESOLVE actor={0} canonical=\"{1}\" canonicalPoint={2} standTarget={3} arrivalRadius={4} standable={5} reason={6}", Describe(actor), SafeLog(location.DisplayName), FormatPoint(location.Point), FormatPoint(resolution.Target), resolution.ArrivalRadius, resolution.IsStandable, SafeLog(resolution.Reason));
            int targetDistance = AIGMNavigationLocationRegistry.GetDistance(actor.Location, resolution.Target);

            if (AIGMNavigationLocationRegistry.GetDistance(actor.Location, location.Point) <= resolution.ArrivalRadius || AIGMNavigationLocationRegistry.GetDistance(actor.Location, resolution.Target) <= resolution.ArrivalRadius)
            {
                AIGMExecutionLog.Write("AIGM_NAV_ARRIVED_NEAR actor={0} canonical=\"{1}\" current={2} canonicalPoint={3} standTarget={4} arrivalRadius={5}", Describe(actor), SafeLog(location.DisplayName), FormatPoint(actor.Location), FormatPoint(location.Point), FormatPoint(resolution.Target), resolution.ArrivalRadius);
                routeMode = "AlreadyNear";
                visibleResponse = "I am already near " + location.DisplayName + ".";
                LogDefaultTravelTrace(rawSpeech, actor, destinationText, location.DisplayName, resolution.Target, targetDistance, routeMode, "none", false, false, true);
                return 1;
            }

            if (!resolution.IsStandable)
            {
                AIGMExecutionLog.Write("AIGM_NAV_DEST_UNREACHABLE actor={0} canonical=\"{1}\" canonicalPoint={2} standTarget={3} arrivalRadius={4} reason={5}", Describe(actor), SafeLog(location.DisplayName), FormatPoint(location.Point), FormatPoint(resolution.Target), resolution.ArrivalRadius, SafeLog(resolution.Reason));
                LogDefaultTravelTrace(rawSpeech, actor, destinationText, location.DisplayName, resolution.Target, targetDistance, routeMode, "none", false, false, false);
                return 0;
            }

            AIGMNavigationLocation movementLocation = new AIGMNavigationLocation(location.Key, location.DisplayName, location.Category, location.Map, resolution.Target, location.Aliases);
            if (companion != null)
            {
                if (targetDistance <= NearTargetRange)
                    return StartNearbyDirectNavigation(companion, requester, location, movementLocation, resolution, rawSpeech, destinationText, targetDistance, out visibleResponse, out routeMode);

                return StartLegacyDirectionalNavigation(companion, requester, location, movementLocation, resolution, rawSpeech, destinationText, targetDistance, enableSweep, out visibleResponse, out routeMode);
            }

            return 0;
        }

        private static int StartNearbyDirectNavigation(BaseHire companion, Mobile requester, AIGMNavigationLocation location, AIGMNavigationLocation movementLocation, AIGMNavigationDestinationResolution resolution, string rawSpeech, string destinationText, int targetDistance, out string visibleResponse, out string routeMode)
        {
            visibleResponse = null;
            routeMode = "DirectNearbyTravel";

            string response;
            if (!AIGMLegacyTravelService.StartTravel(companion, movementLocation, resolution.ArrivalRadius, requester, out response))
            {
                LogDefaultTravelTrace(rawSpeech, companion, destinationText, location.DisplayName, resolution.Target, targetDistance, routeMode, "AIGMLegacyTravelService.StartTravel", false, false, true);
                return 0;
            }

            visibleResponse = SafeName(companion) + " is moving toward " + location.DisplayName + ".";
            LogDefaultTravelTrace(rawSpeech, companion, destinationText, location.DisplayName, resolution.Target, targetDistance, routeMode, "AIGMLegacyTravelService.DirectMove", false, false, true);
            return 1;
        }

        private static int StartLegacyDirectionalNavigation(BaseHire companion, Mobile requester, AIGMNavigationLocation location, AIGMNavigationLocation movementLocation, AIGMNavigationDestinationResolution resolution, string rawSpeech, string destinationText, int targetDistance, bool enableSweep, out string visibleResponse, out string routeMode)
        {
            visibleResponse = null;
            routeMode = "LegacyDirectionalTravel";

            string response;
            if (!AIGMLegacyTravelService.StartTravel(companion, movementLocation, resolution.ArrivalRadius, requester, out response))
                return 0;

            visibleResponse = SafeName(companion) + " is traveling toward " + location.DisplayName + " using general travel.";
            if (enableSweep)
                visibleResponse += " That navigation test command is not available in the legacy baseline.";

            LogDefaultTravelTrace(rawSpeech, companion, destinationText, location.DisplayName, resolution.Target, targetDistance, routeMode, "AIGMLegacyTravelService.DirectMove", false, false, true);
            AIGMExecutionLog.Write("AIGM_NAV_ROUTE actor={0} requester={1} canonical=\"{2}\" start={3} map={4} route=legacy_direct_step native=False final={5} canonicalPoint={6} arrivalRadius={7} result=True response=\"{8}\"",
                Describe(companion),
                Describe(requester),
                SafeLog(location.DisplayName),
                FormatPoint(companion.Location),
                SafeMap(companion.Map),
                FormatPoint(resolution.Target),
                FormatPoint(location.Point),
                resolution.ArrivalRadius,
                SafeLog(visibleResponse));
            return 1;
        }

        private static string BuildGroupTravelResponse(int started, string destinationName, string routeMode, string representativeMessage, bool enableSweep)
        {
            if (started <= 0)
                return "No companion started moving.";

            string suffix = enableSweep ? " Dakeyras is watching the road." : String.Empty;
            if (String.Equals(routeMode, "AlreadyNear", StringComparison.OrdinalIgnoreCase))
                return String.Format("{0} companion{1} already near {2}.", started, started == 1 ? String.Empty : "s", destinationName);
            if (String.Equals(routeMode, "DirectNearbyTravel", StringComparison.OrdinalIgnoreCase))
                return String.Format("{0} companion{1}: moving toward {2} using nearby general travel.", started, started == 1 ? String.Empty : "s", destinationName);
            if (String.Equals(routeMode, "LegacyDirectionalTravel", StringComparison.OrdinalIgnoreCase))
                return String.Format("{0} companion{1}: moving toward {2} using general travel.{3}", started, started == 1 ? String.Empty : "s", destinationName, suffix);
            if (String.Equals(routeMode, "CertifiedGraphRoute", StringComparison.OrdinalIgnoreCase))
                return String.Format("{0} companion{1}: using certified waypoint route to {2}.{3}", started, started == 1 ? String.Empty : "s", destinationName, suffix);
            if (String.Equals(routeMode, "UnverifiedGraphRoute", StringComparison.OrdinalIgnoreCase))
                return String.Format("{0} companion{1}: unverified waypoint route to {2}; they will follow it carefully and mark bad segments.{3}", started, started == 1 ? String.Empty : "s", destinationName, suffix);
            if (String.Equals(routeMode, "PartialGraphRoute", StringComparison.OrdinalIgnoreCase))
                return String.Format("{0} companion{1}: partial waypoint route to {2}; they will stop at the unmapped gap.{3}", started, started == 1 ? String.Empty : "s", destinationName, suffix);
            if (!String.IsNullOrWhiteSpace(representativeMessage))
                return String.Format("{0} companion{1}: {2}", started, started == 1 ? String.Empty : "s", representativeMessage);

            return String.Format("{0} companion{1} moving toward {2}.{3}", started, started == 1 ? String.Empty : "s", destinationName, suffix);
        }

        private static string BuildGroupTravelReceipt(string destinationName, List<string> receiptLines)
        {
            if (receiptLines == null || receiptLines.Count == 0)
                return "No companion started moving.";

            List<string> parts = new List<string>();
            parts.Add("Travel to " + destinationName + ":");
            parts.AddRange(receiptLines);
            return String.Join(" | ", parts.ToArray());
        }

        private static string BuildRefusalMessage(Mobile actor, Mobile requester, AIGMNavigationLocation location, AIGMNavigationSpeechIntent intent)
        {
            if (actor == null || location == null)
                return "I do not have a useful waypoint route yet.";

            AIGMNavigationDestinationResolution resolution = AIGMNavigationDestinationResolver.Resolve(location, actor.Location, actor.Map);
            string destinationText = intent != null && !String.IsNullOrWhiteSpace(intent.CanonicalDestinationText) ? intent.CanonicalDestinationText : location.DisplayName;
            string rawInput = intent != null && !String.IsNullOrWhiteSpace(intent.DestinationText) ? intent.DestinationText : location.DisplayName;
            AIGMExecutionLog.Write("AIGM_NAV_RESOLVER_INPUT caller=natural_speech rawInput=\"{0}\" destinationText=\"{1}\" canonicalDestinationText=\"{2}\"", SafeLog(intent != null ? intent.RawSpeech : rawInput), SafeLog(rawInput), SafeLog(destinationText));
            AIGMNavRoutePlan plan = AIGMNavRoutePlanner.BuildRoute(actor, resolution.Target, actor.Map, location.DisplayName, destinationText);
            if (plan != null && !String.IsNullOrWhiteSpace(plan.OwnerRefusalMessage))
                return plan.OwnerRefusalMessage;

            return "I do not have a useful waypoint route to " + location.DisplayName + " yet.";
        }

        private static void LogDefaultTravelTrace(string rawSpeech, Mobile actor, string destinationText, string resolvedDestination, Point3D targetPoint, int targetDistance, string selectedTravelMode, string movementService, bool graphPlannerInvoked, bool beaconInvoked, bool legacyFallbackInvoked)
        {
            AIGMExecutionLog.Write(
                "AIGM_DEFAULT_TRAVEL_TRACE rawSpeech=\"{0}\" companion={1} destinationText=\"{2}\" resolvedDestination=\"{3}\" targetPoint={4} targetDistance={5} selectedTravelMode={6} movementService={7} graphPlannerInvoked={8} beaconInvoked={9} legacyFallbackInvoked={10}",
                SafeLog(rawSpeech),
                Describe(actor),
                SafeLog(destinationText),
                SafeLog(resolvedDestination),
                FormatPoint(targetPoint),
                targetDistance,
                SafeLog(selectedTravelMode),
                SafeLog(movementService),
                graphPlannerInvoked,
                beaconInvoked,
                legacyFallbackInvoked);
        }

        private static List<BaseHire> FindOwnedCompanions(Mobile owner, int range)
        {
            List<BaseHire> companions = new List<BaseHire>();
            if (owner == null || owner.Map == null)
                return companions;

            foreach (Mobile mobile in World.Mobiles.Values)
            {
                BaseHire hire = mobile as BaseHire;
                IAIGMCompanionActor actor = mobile as IAIGMCompanionActor;
                if (hire == null || actor == null || hire.Deleted || !hire.Alive || hire.Map != owner.Map || hire.GetOwner() != owner)
                    continue;

                if (range > 0 && !hire.InRange(owner, range))
                    continue;

                companions.Add(hire);
            }

            companions.Sort(CompareCompanionOrder);
            return companions;
        }

        private static BaseHire FindOwnedCompanion(Mobile owner, string companionId, int range)
        {
            if (String.IsNullOrWhiteSpace(companionId))
                return null;

            List<BaseHire> companions = FindOwnedCompanions(owner, range);
            for (int i = 0; i < companions.Count; i++)
            {
                IAIGMCompanionActor actor = companions[i] as IAIGMCompanionActor;
                if (actor != null && String.Equals(actor.CompanionId, companionId, StringComparison.OrdinalIgnoreCase))
                    return companions[i];
            }

            return null;
        }

        private static int CompareCompanionOrder(BaseHire left, BaseHire right)
        {
            return CompanionRank(left).CompareTo(CompanionRank(right));
        }

        private static int CompanionRank(BaseHire hire)
        {
            IAIGMCompanionActor actor = hire as IAIGMCompanionActor;
            string id = actor != null ? actor.CompanionId : null;
            if (String.Equals(id, "dakeyras", StringComparison.OrdinalIgnoreCase))
                return 0;
            if (String.Equals(id, "danyal", StringComparison.OrdinalIgnoreCase))
                return 1;
            if (String.Equals(id, "dardalion", StringComparison.OrdinalIgnoreCase))
                return 2;
            return 10;
        }

        private static bool TryStripNamedCompanion(string speech, out string companionId, out string payload)
        {
            companionId = null;
            payload = speech;
            return TryStripAlias(speech, "dakeyras", new[] { "dakeyras", "dak", "dake", "waylander" }, out companionId, out payload)
                || TryStripAlias(speech, "danyal", new[] { "danyal", "dan" }, out companionId, out payload)
                || TryStripAlias(speech, "dardalion", new[] { "dardalion", "dard", "dar" }, out companionId, out payload);
        }

        private static bool TryStripAlias(string speech, string id, string[] aliases, out string companionId, out string payload)
        {
            companionId = null;
            payload = speech;
            for (int i = 0; aliases != null && i < aliases.Length; i++)
            {
                string alias = aliases[i];
                if (speech.Equals(alias, StringComparison.Ordinal))
                    return false;

                if (speech.StartsWith(alias + " ", StringComparison.Ordinal))
                {
                    companionId = id;
                    payload = speech.Substring(alias.Length).Trim();
                    return true;
                }
            }

            return false;
        }

        private static bool TryStripGroupPrefix(string speech, out string payload)
        {
            payload = speech;
            string[] prefixes =
            {
                "companions ",
                "all companions ",
                "everyone ",
                "everybody ",
                "all of you ",
                "you all ",
                "you three ",
                "three of you ",
                "party "
            };

            for (int i = 0; i < prefixes.Length; i++)
            {
                if (speech.StartsWith(prefixes[i], StringComparison.Ordinal))
                {
                    payload = speech.Substring(prefixes[i].Length).Trim();
                    return true;
                }
            }

            return false;
        }

        private static bool TryExtractDestination(string speech, out string destination, out string verb, out bool leadParty, out bool enableSweep)
        {
            destination = null;
            verb = null;
            leadParty = false;
            enableSweep = false;

            for (int i = 0; i < MovementPrefixes.Length; i++)
            {
                string prefix = MovementPrefixes[i];
                if (speech.StartsWith(prefix, StringComparison.Ordinal))
                {
                    destination = speech.Substring(prefix.Length).Trim();
                    verb = prefix.Trim();
                    enableSweep = StripSweepSuffix(ref destination);
                    leadParty = prefix.StartsWith("take us", StringComparison.Ordinal) || prefix.StartsWith("lead us", StringComparison.Ordinal);
                    return !String.IsNullOrWhiteSpace(destination);
                }
            }

            return false;
        }

        private static bool StripSweepSuffix(ref string destination)
        {
            string value = NormalizeSpeech(destination);
            string[] markers =
            {
                " and watch for monsters",
                " and hunt anything close",
                " and clear monsters",
                " with passive tracking",
                " with travel tracking",
                " while tracking monsters",
                " sweep"
            };

            for (int i = 0; i < markers.Length; i++)
            {
                if (value.EndsWith(markers[i], StringComparison.Ordinal))
                {
                    destination = value.Substring(0, value.Length - markers[i].Length).Trim();
                    return true;
                }
            }

            return false;
        }

        private static bool LooksLikeDialogue(string speech)
        {
            return speech.Contains("what do you think")
                || speech.Contains("do you remember")
                || speech.Contains("remember ")
                || speech.Contains("discuss")
                || speech.Contains("what do you see")
                || speech.Contains("what do you make")
                || speech.Contains("tell me about")
                || speech.Contains("tell us about");
        }

        private static string NormalizeDestinationPhrase(string destination)
        {
            string value = NormalizeSpeech(destination);
            if (value.StartsWith("the ", StringComparison.Ordinal))
                value = value.Substring(4).Trim();

            if (value.StartsWith("shrine of ", StringComparison.Ordinal))
                value = value.Substring(10).Trim() + " shrine";
            else if (value.StartsWith("the shrine of ", StringComparison.Ordinal))
                value = value.Substring(14).Trim() + " shrine";

            if (value.EndsWith(" please", StringComparison.Ordinal))
                value = value.Substring(0, value.Length - 7).Trim();

            return value;
        }

        private static string NormalizeSpeech(string speech)
        {
            if (String.IsNullOrWhiteSpace(speech))
                return String.Empty;

            string normalized = speech.Trim().ToLowerInvariant();
            normalized = normalized.Replace(",", " ").Replace(".", " ").Replace("!", " ").Replace("?", " ").Replace(";", " ").Replace(":", " ");
            normalized = normalized.Replace("'", String.Empty).Replace("-", " ");
            while (normalized.Contains("  "))
                normalized = normalized.Replace("  ", " ");
            return normalized.Trim();
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

        private static string SafeMap(Map map)
        {
            return map == null ? "null" : (map.Name ?? map.ToString());
        }

        private static string FormatPoint(Point3D point)
        {
            return point.X + "," + point.Y + "," + point.Z;
        }

        private static string FormatWaypoints(AIGMNavigationRoute route)
        {
            if (route == null || route.Waypoints == null || route.Waypoints.Count == 0)
                return String.Empty;

            List<string> parts = new List<string>();
            for (int i = 0; i < route.Waypoints.Count; i++)
            {
                string label = route.WaypointLabels != null && i < route.WaypointLabels.Count ? route.WaypointLabels[i] : "waypoint";
                parts.Add((i + 1) + ":" + SafeLog(label) + "@" + FormatPoint(route.Waypoints[i]));
            }

            return String.Join(";", parts.ToArray());
        }

        private sealed class RecentNavigationSpeech
        {
            public string Key;
            public DateTime Utc;
        }
    }
}
