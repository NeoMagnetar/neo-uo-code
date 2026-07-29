using System;

using Server;
using Server.Custom.AIGM;
using Server.Custom.AIGM.Navigation;
using Server.Mobiles;

namespace Server.Commands
{
    public static class AIGMCompanionNavigationCommand
    {
        private const int NearTargetRange = 40;

        public static void Initialize()
        {
            CommandSystem.Register("AIGMNav", AccessLevel.GameMaster, OnCommand);
            CommandSystem.Register("AIGMGo", AccessLevel.GameMaster, OnCommand);
            CommandSystem.Register("AIGMMoveStatus", AccessLevel.GameMaster, OnStatus);
            CommandSystem.Register("AIGMRoutePreview", AccessLevel.GameMaster, OnRoutePreview);
            CommandSystem.Register("AIGMLandmarks", AccessLevel.GameMaster, OnLandmarks);
        }

        [Usage("AIGMNav <location> | AIGMNav status | AIGMNav stop | AIGMNav list [towns|shrines|dungeons|banks|moongates]")]
        [Description("Moves the preferred nearby AIGM companion toward a registered location using native AI movement.")]
        private static void OnCommand(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            string arg = e.ArgString != null ? e.ArgString.Trim() : String.Empty;
            bool sweep = StripSweepSuffix(ref arg);

            if (String.IsNullOrWhiteSpace(arg) || StartsWithList(arg))
            {
                string category = ExtractListCategory(arg);
                e.Mobile.SendMessage("Known locations: " + AIGMNavigationLocationRegistry.FormatList(category));
                e.Mobile.SendMessage(AIGMNavigationRouteGraph.FormatGraphSummary());
                return;
            }

            BaseHire companion = ResolvePreferredCompanion(e.Mobile, 18);
            if (companion == null)
            {
                e.Mobile.SendMessage("No nearby AIGM companion found.");
                return;
            }

            if (String.Equals(arg, "stop", StringComparison.OrdinalIgnoreCase))
            {
                string stopResponse;
                string nativeResponse;
                string legacyResponse;
                AIGMLegacyTravelService.Stop(companion, "aigm_navigation_stop", out legacyResponse);
                bool stoppedNative = AIGMNativeNavigationService.Stop(companion, "aigm_navigation_stop", out nativeResponse);
                AIGMSmartMovementService.Stop(companion, "aigm_navigation_stop", out stopResponse);
                e.Mobile.SendMessage(companion.Name + ": " + (!String.IsNullOrWhiteSpace(legacyResponse) ? legacyResponse : (stoppedNative ? nativeResponse : stopResponse)));
                return;
            }

            if (String.Equals(arg, "status", StringComparison.OrdinalIgnoreCase))
            {
                e.Mobile.SendMessage(companion.Name + ": " + AIGMLegacyTravelService.GetStatus(companion));
                e.Mobile.SendMessage(companion.Name + ": " + AIGMNativeNavigationService.GetStatus(companion));
                e.Mobile.SendMessage(companion.Name + " legacy: " + AIGMSmartMovementService.GetStatus(companion));
                return;
            }

            AIGMNavigationLocation location;
            if (!AIGMNavigationLocationRegistry.TryResolve(arg, companion.Map, out location))
            {
                e.Mobile.SendMessage("Unknown location. Use [AIGMNav list for known locations.");
                return;
            }

            AIGMExecutionLog.Write("AIGM_NAV_RESOLVER_INPUT caller=bracket_command rawInput=\"{0}\" destinationText=\"{1}\" canonicalDestinationText=\"{2}\"", SafeLog(arg), SafeLog(arg), SafeLog(location.DisplayName));
            AIGMExecutionLog.Write("AIGM_NAV_RESOLVER_PARITY naturalSpeechDestination=\"{0}\" commandEquivalent=\"{1}\" sameCanonical={2} nodeId={3} legacyLocationMatch={4}", SafeLog(arg), SafeLog(location.DisplayName), String.Equals(arg, location.DisplayName, StringComparison.OrdinalIgnoreCase), SafeLog(location.Key), true);

            AIGMExecutionLog.Write("AIGM_NAV_COMMAND_RECEIVED speaker={0} companion={1} rawSpeech=\"[AIGMNav {2}\" parsedDestination=\"{3}\" commandSource=command useGraphRequested=False", Describe(e.Mobile), Describe(companion), SafeLog(arg), SafeLog(location.DisplayName));

            string response;
            AIGMNavigationDestinationResolution resolution = AIGMNavigationDestinationResolver.Resolve(location, companion.Location, companion.Map);
            int targetDistance = AIGMNavigationLocationRegistry.GetDistance(companion.Location, resolution.Target);
            AIGMExecutionLog.Write("AIGM_NAV_DEST_RESOLVE actor={0} canonical=\"{1}\" canonicalPoint={2} standTarget={3} arrivalRadius={4} standable={5} reason={6}", Describe(companion), SafeLog(location.DisplayName), FormatPoint(location.Point), FormatPoint(resolution.Target), resolution.ArrivalRadius, resolution.IsStandable, SafeLog(resolution.Reason));
            if (AIGMNavigationLocationRegistry.GetDistance(companion.Location, location.Point) <= resolution.ArrivalRadius || AIGMNavigationLocationRegistry.GetDistance(companion.Location, resolution.Target) <= resolution.ArrivalRadius)
            {
                AIGMExecutionLog.Write("AIGM_NAV_ARRIVED_NEAR actor={0} canonical=\"{1}\" current={2} canonicalPoint={3} standTarget={4} arrivalRadius={5}", Describe(companion), SafeLog(location.DisplayName), FormatPoint(companion.Location), FormatPoint(location.Point), FormatPoint(resolution.Target), resolution.ArrivalRadius);
                LogDefaultTravelTrace("[AIGMNav " + arg + "]", companion, arg, location.DisplayName, resolution.Target, targetDistance, "AlreadyNear", "none", false, false, true);
                e.Mobile.SendMessage(companion.Name + ": already near " + location.DisplayName + ".");
                return;
            }

            if (!resolution.IsStandable)
            {
                AIGMExecutionLog.Write("AIGM_NAV_DEST_UNREACHABLE actor={0} canonical=\"{1}\" canonicalPoint={2} standTarget={3} arrivalRadius={4} reason={5}", Describe(companion), SafeLog(location.DisplayName), FormatPoint(location.Point), FormatPoint(resolution.Target), resolution.ArrivalRadius, SafeLog(resolution.Reason));
                e.Mobile.SendMessage(companion.Name + ": " + location.DisplayName + " has no reachable stand tile yet.");
                return;
            }

            AIGMNavigationLocation movementLocation = new AIGMNavigationLocation(location.Key, location.DisplayName, location.Category, location.Map, resolution.Target, location.Aliases);
            if (targetDistance <= NearTargetRange)
            {
                if (AIGMLegacyTravelService.StartTravel(companion, movementLocation, resolution.ArrivalRadius, e.Mobile, out response))
                {
                    LogDefaultTravelTrace("[AIGMNav " + arg + "]", companion, arg, location.DisplayName, resolution.Target, targetDistance, "DirectNearbyTravel", "AIGMLegacyTravelService.DirectMove", false, false, true);
                    e.Mobile.SendMessage(companion.Name + ": " + companion.Name + " is moving toward " + location.DisplayName + ".");
                    return;
                }

                LogDefaultTravelTrace("[AIGMNav " + arg + "]", companion, arg, location.DisplayName, resolution.Target, targetDistance, "DirectNearbyTravel", "AIGMLegacyTravelService.StartTravel", false, false, true);
            }

            if (AIGMLegacyTravelService.StartTravel(companion, movementLocation, resolution.ArrivalRadius, e.Mobile, out response))
            {
                string ownerMessage = companion.Name + " is traveling toward " + location.DisplayName + " using general travel.";
                LogDefaultTravelTrace("[AIGMNav " + arg + "]", companion, arg, location.DisplayName, resolution.Target, targetDistance, "LegacyDirectionalTravel", "AIGMLegacyTravelService.DirectMove", false, false, true);
                if (sweep)
                {
                    e.Mobile.SendMessage("That navigation test command is not available in the legacy baseline.");
                }
                e.Mobile.SendMessage(companion.Name + ": " + ownerMessage);
            }
            else
                e.Mobile.SendMessage(companion.Name + ": " + (response ?? "Unable to begin navigation."));
        }

        [Usage("AIGMMoveStatus")]
        [Description("Reports smart movement state for the preferred nearby AIGM companion.")]
        private static void OnStatus(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            e.Mobile.SendMessage("Player: " + AIGMSmartMovementService.GetStatus(e.Mobile));

            BaseHire companion = ResolvePreferredCompanion(e.Mobile, 18);
            if (companion == null)
            {
                e.Mobile.SendMessage("No nearby AIGM companion found.");
                return;
            }

            e.Mobile.SendMessage(companion.Name + ": " + AIGMLegacyTravelService.GetStatus(companion));
            e.Mobile.SendMessage(companion.Name + ": " + AIGMNativeNavigationService.GetStatus(companion));
            e.Mobile.SendMessage(companion.Name + " legacy: " + AIGMSmartMovementService.GetStatus(companion));
        }

        private static void OnLandmarks(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            string arg = e.ArgString == null ? String.Empty : e.ArgString.Trim().ToLowerInvariant();
            bool near = arg == "near" || arg.StartsWith("near ", StringComparison.Ordinal);
            e.Mobile.SendMessage(AIGMNavigationRouteGraph.FormatLandmarks(e.Mobile.Location, e.Mobile.Map, near));
            AIGMExecutionLog.Write("AIGM_LANDMARKS actor={0} near={1} current={2}", Describe(e.Mobile), near, FormatPoint(e.Mobile.Location));
        }

        [Usage("AIGMRoutePreview [companion|companions] <location>")]
        [Description("Previews the selected route start and first waypoint without moving anyone.")]
        private static void OnRoutePreview(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            string arg = e.ArgString != null ? e.ArgString.Trim() : String.Empty;
            if (String.IsNullOrWhiteSpace(arg))
            {
                e.Mobile.SendMessage("Usage: [AIGMRoutePreview [Dakeyras|Danyal|Dardalion|companions] <location>");
                return;
            }

            bool allCompanions = false;
            string companionId;
            string destinationText;
            ParsePreviewArgs(arg, out allCompanions, out companionId, out destinationText);

            AIGMNavigationLocation location;
            if (!AIGMNavigationLocationRegistry.TryResolve(destinationText, e.Mobile.Map, out location))
            {
                e.Mobile.SendMessage("Unknown location. Use [AIGMNav list for known locations.");
                return;
            }

            AIGMExecutionLog.Write("AIGM_NAV_RESOLVER_INPUT caller=bracket_command rawInput=\"{0}\" destinationText=\"{1}\" canonicalDestinationText=\"{2}\"", SafeLog(destinationText), SafeLog(destinationText), SafeLog(location.DisplayName));
            AIGMExecutionLog.Write("AIGM_NAV_RESOLVER_PARITY naturalSpeechDestination=\"{0}\" commandEquivalent=\"{1}\" sameCanonical={2} nodeId={3} legacyLocationMatch={4}", SafeLog(destinationText), SafeLog(location.DisplayName), String.Equals(destinationText, location.DisplayName, StringComparison.OrdinalIgnoreCase), SafeLog(location.Key), true);

            if (allCompanions)
            {
                int count = 0;
                IPooledEnumerable mobiles = e.Mobile.Map.GetMobilesInRange(e.Mobile.Location, 30);
                foreach (Mobile mobile in mobiles)
                {
                    BaseHire hire = mobile as BaseHire;
                    IAIGMCompanionActor actor = mobile as IAIGMCompanionActor;
                    if (hire == null || actor == null || hire.Deleted || !hire.Alive || hire.GetOwner() != e.Mobile)
                        continue;

                    SendPreview(e.Mobile, hire, location);
                    count++;
                }
                mobiles.Free();

                if (count == 0)
                    e.Mobile.SendMessage("No nearby AIGM companions found.");

                return;
            }

            BaseHire companion = String.IsNullOrWhiteSpace(companionId)
                ? ResolvePreferredCompanion(e.Mobile, 18)
                : ResolveCompanionById(e.Mobile, companionId, 30);

            if (companion == null)
            {
                e.Mobile.SendMessage("No matching nearby AIGM companion found.");
                return;
            }

            SendPreview(e.Mobile, companion, location);
        }

        private static bool StartsWithList(string arg)
        {
            return String.IsNullOrWhiteSpace(arg) || arg.Equals("list", StringComparison.OrdinalIgnoreCase) || arg.StartsWith("list ", StringComparison.OrdinalIgnoreCase);
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

        private static string ExtractListCategory(string arg)
        {
            if (String.IsNullOrWhiteSpace(arg))
                return null;

            string trimmed = arg.Trim();
            if (trimmed.Equals("list", StringComparison.OrdinalIgnoreCase))
                return null;

            return trimmed.StartsWith("list ", StringComparison.OrdinalIgnoreCase) ? trimmed.Substring(5).Trim() : null;
        }

        private static bool StripSweepSuffix(ref string arg)
        {
            if (String.IsNullOrWhiteSpace(arg))
                return false;

            string lower = arg.ToLowerInvariant();
            string[] markers =
            {
                " with passive tracking",
                " and watch for monsters",
                " and hunt anything close",
                " and clear monsters",
                " sweep"
            };

            for (int i = 0; i < markers.Length; i++)
            {
                if (lower.EndsWith(markers[i], StringComparison.Ordinal))
                {
                    arg = arg.Substring(0, arg.Length - markers[i].Length).Trim();
                    return true;
                }
            }

            return false;
        }

        private static string BuildRefusalMessage(AIGMNavRoutePlan plan, string destinationName)
        {
            if (plan == null)
                return "I do not have a useful waypoint route to " + destinationName + " yet.";

            if (!String.IsNullOrWhiteSpace(plan.OwnerRefusalMessage))
                return plan.OwnerRefusalMessage;

            return "I do not have a useful waypoint route to " + destinationName + " yet.";
        }

        private static BaseHire ResolvePreferredCompanion(Mobile from, int range)
        {
            if (from == null || from.Map == null)
                return null;

            BaseHire dakeyras = null;
            BaseHire danyal = null;
            BaseHire dardalion = null;
            BaseHire closest = null;
            int closestDistance = Int32.MaxValue;

            IPooledEnumerable mobiles = from.Map.GetMobilesInRange(from.Location, range);
            foreach (Mobile mobile in mobiles)
            {
                BaseHire hire = mobile as BaseHire;
                IAIGMCompanionActor actor = mobile as IAIGMCompanionActor;
                if (hire == null || actor == null || mobile.Deleted || !mobile.Alive)
                    continue;

                if (hire.GetOwner() != from)
                    continue;

                if (String.Equals(actor.CompanionId, "dakeyras", StringComparison.OrdinalIgnoreCase))
                    dakeyras = hire;
                else if (String.Equals(actor.CompanionId, "danyal", StringComparison.OrdinalIgnoreCase))
                    danyal = hire;
                else if (String.Equals(actor.CompanionId, "dardalion", StringComparison.OrdinalIgnoreCase))
                    dardalion = hire;

                int distance = (int)Math.Round(from.GetDistanceToSqrt(mobile));
                if (distance < closestDistance)
                {
                    closest = hire;
                    closestDistance = distance;
                }
            }
            mobiles.Free();

            return dakeyras ?? danyal ?? dardalion ?? closest;
        }

        private static BaseHire ResolveCompanionById(Mobile from, string companionId, int range)
        {
            if (from == null || from.Map == null || String.IsNullOrWhiteSpace(companionId))
                return null;

            string normalized = companionId.Trim().ToLowerInvariant();
            if (normalized == "dak")
                normalized = "dakeyras";
            else if (normalized == "dan")
                normalized = "danyal";
            else if (normalized == "dard" || normalized == "dar")
                normalized = "dardalion";

            IPooledEnumerable mobiles = from.Map.GetMobilesInRange(from.Location, range);
            foreach (Mobile mobile in mobiles)
            {
                BaseHire hire = mobile as BaseHire;
                IAIGMCompanionActor actor = mobile as IAIGMCompanionActor;
                if (hire == null || actor == null || hire.Deleted || !hire.Alive || hire.GetOwner() != from)
                    continue;

                if (String.Equals(actor.CompanionId, normalized, StringComparison.OrdinalIgnoreCase) || String.Equals(hire.Name, companionId, StringComparison.OrdinalIgnoreCase))
                {
                    mobiles.Free();
                    return hire;
                }
            }
            mobiles.Free();

            return null;
        }

        private static void ParsePreviewArgs(string arg, out bool allCompanions, out string companionId, out string destinationText)
        {
            allCompanions = false;
            companionId = null;
            destinationText = arg;

            string trimmed = arg.Trim();
            string lower = trimmed.ToLowerInvariant();
            if (lower.StartsWith("companions ", StringComparison.Ordinal) || lower.StartsWith("all companions ", StringComparison.Ordinal))
            {
                allCompanions = true;
                destinationText = lower.StartsWith("all companions ", StringComparison.Ordinal) ? trimmed.Substring(15).Trim() : trimmed.Substring(11).Trim();
                return;
            }

            string[] parts = trimmed.Split(new[] { ' ' }, 2);
            if (parts.Length == 2 && IsCompanionToken(parts[0]))
            {
                companionId = parts[0];
                destinationText = parts[1].Trim();
            }
        }

        private static bool IsCompanionToken(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return false;

            string normalized = value.Trim().ToLowerInvariant();
            return normalized == "dakeyras" || normalized == "dak" || normalized == "danyal" || normalized == "dan" || normalized == "dardalion" || normalized == "dard" || normalized == "dar";
        }

        private static void SendPreview(Mobile viewer, BaseHire actor, AIGMNavigationLocation location)
        {
            AIGMNavigationRoutePreview preview = AIGMNavigationRouteGraph.BuildPreview(actor.Location, actor.Map, location);
            string routeStart = String.IsNullOrWhiteSpace(preview.SourceNodeName) ? "none" : preview.SourceNodeName + "@" + FormatPoint(preview.SourceNodePoint);
            string destinationAnchor = String.IsNullOrWhiteSpace(preview.DestinationAnchorName) ? "none" : preview.DestinationAnchorName + "@" + FormatPoint(preview.DestinationAnchorPoint);
            string firstName = String.IsNullOrWhiteSpace(preview.FirstWaypointName) ? location.DisplayName : preview.FirstWaypointName;
            string waypoints = FormatPreviewWaypoints(preview);

            viewer.SendMessage(
                "{0}: current={1}; destination={2}@{3}; directVector={4}; route={5}; start={6}; destinationAnchor={7}; first={8}@{9}; firstVector={10}",
                actor.Name,
                FormatPoint(actor.Location),
                location.DisplayName,
                FormatPoint(location.Point),
                FormatVector(actor.Location, location.Point),
                preview.UsesGraph ? "graph" : "direct",
                routeStart,
                destinationAnchor,
                firstName,
                FormatPoint(preview.FirstWaypoint),
                FormatVector(actor.Location, preview.FirstWaypoint));

            viewer.SendMessage("{0}: waypoints={1}", actor.Name, waypoints);
            viewer.SendMessage("{0}: canonicalDestination={1}; standTarget={2}; arrivalRadius={3}; standable={4}; resolve={5}", actor.Name, FormatPoint(preview.CanonicalDestination), FormatPoint(preview.ResolvedStandableTarget), preview.ArrivalRadius, preview.DestinationTargetStandable, preview.DestinationResolveReason);
            viewer.SendMessage("{0}: finalDestination={1}; finalApproach={2}; stopsAtExactDestination={3}; destinationIsLandmark={4}", actor.Name, FormatPoint(preview.FinalDestination), FormatPoint(preview.FinalApproachPoint), preview.StopsAtExactDestination, preview.DestinationIsLandmark);
            AIGMNavRoutePlan graphPlan = AIGMNavRoutePlanner.BuildRoute(actor, preview.ResolvedStandableTarget, actor.Map, location.DisplayName, location.DisplayName);
            viewer.SendMessage("{0}: graphRoute={1}; graphReason={2}; plannerUsed=True; algorithm=AStar; fallback={3}; graphSteps={4}", actor.Name, graphPlan.Success, graphPlan.Reason, graphPlan.SafeDirectFallback ? "True" : "False", graphPlan.Steps.Count);
            viewer.SendMessage("{0}: canonicalDestination={1}; canonicalDestinationId={2}; destinationKind={3}; aliasesMatched={4}", actor.Name, graphPlan.CanonicalDestinationName ?? "unknown", graphPlan.CanonicalDestinationId ?? "none", graphPlan.DestinationKind ?? "unknown", String.IsNullOrWhiteSpace(graphPlan.DestinationAliasesMatched) ? "none" : graphPlan.DestinationAliasesMatched);
            viewer.SendMessage("{0}: selectedStartAnchor={1}; selectedDestinationAnchor={2}; selectedDestinationRole={3}; routeEndsAtDestinationNode={4}; candidatePairs={5}; rejectedWrongDirection={6}; rejectedCandidates={7}", actor.Name, graphPlan.StartAnchor != null ? graphPlan.StartAnchor.Id : "none", graphPlan.DestinationAnchor != null ? graphPlan.DestinationAnchor.Id : "none", String.IsNullOrWhiteSpace(graphPlan.SelectedDestinationRole) ? "none" : graphPlan.SelectedDestinationRole, graphPlan.RouteEndsAtDestinationNode, graphPlan.CandidatePairCount, graphPlan.RejectedWrongDirectionCount, graphPlan.RejectedCandidateCount);
            viewer.SendMessage("{0}: routeIds={1}", actor.Name, graphPlan.FormatNodeIds());
            viewer.SendMessage("{0}: routePoints={1}", actor.Name, graphPlan.FormatPointPath());
            viewer.SendMessage("{0}: graphPath={1}", actor.Name, graphPlan.FormatNodePath());
            viewer.SendMessage("{0}: localRecovery=True; doorBackoff=True; noProgressRetrace=10; failedTileMemory=True", actor.Name);

            AIGMExecutionLog.Write(
                "AIGM_ROUTE_PREVIEW actor={0} current={1} destination=\"{2}\" destinationPoint={3} directVector={4} route={5} source=\"{6}\" sourcePoint={7} destinationAnchor=\"{8}\" destinationAnchorPoint={9} first=\"{10}\" firstPoint={11} firstVector={12} finalDestination={13} finalApproach={14} stopsAtExactDestination={15} destinationIsLandmark={16} canonicalDestination={17} standTarget={18} arrivalRadius={19} standable={20} resolve={21} graphRoute={22} graphSteps={23} graphPath=\"{24}\" localRecovery=True doorBackoff=True noProgressRetrace=10 failedTileMemory=True waypoints=\"{25}\"",
                Describe(actor),
                FormatPoint(actor.Location),
                SafeLog(location.DisplayName),
                FormatPoint(location.Point),
                FormatVector(actor.Location, location.Point),
                preview.UsesGraph ? "graph" : "direct",
                SafeLog(preview.SourceNodeName),
                FormatPoint(preview.SourceNodePoint),
                SafeLog(preview.DestinationAnchorName),
                FormatPoint(preview.DestinationAnchorPoint),
                SafeLog(firstName),
                FormatPoint(preview.FirstWaypoint),
                FormatVector(actor.Location, preview.FirstWaypoint),
                FormatPoint(preview.FinalDestination),
                FormatPoint(preview.FinalApproachPoint),
                preview.StopsAtExactDestination,
                preview.DestinationIsLandmark,
                FormatPoint(preview.CanonicalDestination),
                FormatPoint(preview.ResolvedStandableTarget),
                preview.ArrivalRadius,
                preview.DestinationTargetStandable,
                SafeLog(preview.DestinationResolveReason),
                graphPlan.Success,
                graphPlan.Steps.Count,
                SafeLog(graphPlan.FormatNodePath()),
                SafeLog(waypoints));
        }

        private static string FormatPreviewWaypoints(AIGMNavigationRoutePreview preview)
        {
            if (preview == null || preview.Route == null || preview.Route.Waypoints == null || preview.Route.Waypoints.Count == 0)
                return "1:" + preview.FirstWaypointName + "@" + FormatPoint(preview.FirstWaypoint);

            string[] parts = new string[preview.Route.Waypoints.Count];
            for (int i = 0; i < preview.Route.Waypoints.Count; i++)
            {
                string label = preview.Route.WaypointLabels != null && i < preview.Route.WaypointLabels.Count ? preview.Route.WaypointLabels[i] : "waypoint";
                parts[i] = (i + 1) + ":" + label + "@" + FormatPoint(preview.Route.Waypoints[i]);
            }

            return String.Join(";", parts);
        }

        private static string FormatPoint(Point3D point)
        {
            return point.X + "," + point.Y + "," + point.Z;
        }

        private static string FormatVector(Point3D from, Point3D to)
        {
            return (to.X - from.X) + "," + (to.Y - from.Y);
        }

        private static string Describe(Mobile mobile)
        {
            return mobile == null ? "missing" : ((mobile.Name ?? mobile.GetType().Name) + "[" + mobile.Serial + "]");
        }

        private static string SafeLog(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return String.Empty;

            return value.Replace('"', '\'').Replace('\r', ' ').Replace('\n', ' ');
        }
    }
}
