using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

using Server;
using Server.Custom.AIGM.Navigation;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionTrackingCommandService
    {
        private const int LocateRange = 64;
        private const int HardCommandRadius = 40;
        private static readonly ConcurrentDictionary<int, HardCommandDispatchState> LastHardDispatchByOwner = new ConcurrentDictionary<int, HardCommandDispatchState>();

        public static bool TryHandleSpeech(BaseHire companion, Mobile owner, string speech, AIGMCompanionCommandRouteDecision decision, bool shouldSpeak, out string response)
        {
            response = null;
            if (companion == null || owner == null || String.IsNullOrWhiteSpace(speech))
                return false;

            ParsedTrackingCommand command;
            if (!TryParseTrackingCommand(speech, decision, out command))
                return false;

            AIGMExecutionLog.Write("AIGM_TRACK_COMMAND_PARSE companion={0} owner={1} rawSpeech=\"{2}\" normalized=\"{3}\" category={4} intent={5} handled={6} fallthroughAllowed={7}",
                Describe(companion),
                Describe(owner),
                SafeLog(speech),
                SafeLog(command.NormalizedSpeech),
                SafeLog(command.Category),
                SafeLog(command.Intent),
                !command.IsDialogue,
                command.IsDialogue);
            AIGMExecutionLog.Write("AIGM_TRACK_COMMAND_PRECEDENCE rawSpeech=\"{0}\" matchedTracking={1} blockedDialogueFallback={2} route={3}",
                SafeLog(speech),
                !command.IsDialogue,
                !command.IsDialogue,
                command.IsDialogue ? "dialogue" : "tracking");

            if (command.IsDialogue)
                return false;

            AIGMCompanionDialogueThreadService.StopForCommand(owner, decision);

            switch (command.Action)
            {
                case TrackingCommandAction.Status:
                    response = AIGMCompanionTrackingService.GetTrackingStatus(companion, owner);
                    AIGMExecutionLog.Write("AIGM_TRACK_COMMAND_ROUTE companion={0} route=status response=\"{1}\"", Describe(companion), SafeLog(response));
                    SendVisibleReply(owner, companion, command.Category, "status", response, shouldSpeak, ref response);
                    return true;
                case TrackingCommandAction.StartWatch:
                    AIGMExecutionLog.Write("AIGM_HUNT_COMMAND_RECEIVED phrase={0} mode={1} companion={2} owner={3}",
                        SafeLog(speech),
                        "TrackHunt",
                        Describe(companion),
                        Describe(owner));
                    response = DispatchHardHuntStart(companion, owner, speech, "start_tracking", true);
                    AIGMExecutionLog.Write("AIGM_TRACK_COMMAND_ROUTE companion={0} route=start_watch response=\"{1}\"", Describe(companion), SafeLog(response));
                    if (!String.IsNullOrWhiteSpace(response))
                    {
                        AIGMExecutionLog.Write("AIGM_TRACK_START companion={0} active=True mode={1} action={2}", companion.Serial.Value, AIGMCompanionTrackingMode.Monsters, AIGMCompanionTrackingActionMode.TrackHunt);
                        SendVisibleReply(owner, companion, command.Category, "status", response, shouldSpeak, ref response);
                    }
                    return true;
                case TrackingCommandAction.Stop:
                    response = DispatchHardHuntStop(companion, owner, speech, "stop_hunting");
                    AIGMExecutionLog.Write("AIGM_TRACK_COMMAND_ROUTE companion={0} route=stop response=\"{1}\"", Describe(companion), SafeLog(response));
                    if (!String.IsNullOrWhiteSpace(response))
                        SendVisibleReply(owner, companion, command.Category, "status", response, shouldSpeak, ref response);
                    return true;
                case TrackingCommandAction.Locate:
                    response = StartLocate(companion, owner, command.TargetText);
                    AIGMExecutionLog.Write("AIGM_TRACK_COMMAND_ROUTE companion={0} route=locate target=\"{1}\" response=\"{2}\"", Describe(companion), SafeLog(command.TargetText), SafeLog(response));
                    SendVisibleReply(owner, companion, command.Category, "stats", response, shouldSpeak, ref response);
                    return true;
                case TrackingCommandAction.LocationList:
                    response = BuildLocationList(companion, owner);
                    AIGMExecutionLog.Write("AIGM_TRACK_COMMAND_ROUTE companion={0} route=location_list response=\"{1}\"", Describe(companion), SafeLog(response));
                    SendVisibleReply(owner, companion, command.Category, "stats", response, shouldSpeak, ref response);
                    return true;
                case TrackingCommandAction.LocationTrack:
                    response = BuildLocationStats(companion, owner, command.TargetText);
                    AIGMExecutionLog.Write("AIGM_TRACK_COMMAND_ROUTE companion={0} route=location target=\"{1}\" response=\"{2}\"", Describe(companion), SafeLog(command.TargetText), SafeLog(response));
                    SendVisibleReply(owner, companion, command.Category, "stats", response, shouldSpeak, ref response);
                    return true;
                case TrackingCommandAction.AnimalTrack:
                    response = AIGMCompanionTrackingService.StartTrackingAction(companion, owner, AIGMCompanionTrackingMode.Animals, AIGMCompanionTrackingActionMode.TrackOnly);
                    AIGMExecutionLog.Write("AIGM_TRACK_COMMAND_ROUTE companion={0} route=animal_track response=\"{1}\"", Describe(companion), SafeLog(response));
                    AIGMExecutionLog.Write("AIGM_TRACK_STATS companion={0} mode=Animals action=TrackOnly response=\"{1}\"", Describe(companion), SafeLog(response));
                    SendVisibleReply(owner, companion, command.Category, ClassifyReplyType(response), response, shouldSpeak, ref response);
                    return true;
                case TrackingCommandAction.NpcTrack:
                    response = AIGMCompanionTrackingService.StartTrackingAction(companion, owner, AIGMCompanionTrackingMode.NPCs, AIGMCompanionTrackingActionMode.TrackOnly);
                    AIGMExecutionLog.Write("AIGM_TRACK_COMMAND_ROUTE companion={0} route=npc_track response=\"{1}\"", Describe(companion), SafeLog(response));
                    AIGMExecutionLog.Write("AIGM_TRACK_STATS companion={0} mode=NPCs action=TrackOnly response=\"{1}\"", Describe(companion), SafeLog(response));
                    SendVisibleReply(owner, companion, command.Category, ClassifyReplyType(response), response, shouldSpeak, ref response);
                    return true;
                case TrackingCommandAction.PlayerTrack:
                    response = AIGMCompanionTrackingService.StartTrackingAction(companion, owner, AIGMCompanionTrackingMode.Players, AIGMCompanionTrackingActionMode.TrackOnly);
                    AIGMExecutionLog.Write("AIGM_TRACK_COMMAND_ROUTE companion={0} route=player_track response=\"{1}\"", Describe(companion), SafeLog(response));
                    AIGMExecutionLog.Write("AIGM_TRACK_STATS companion={0} mode=Players action=TrackOnly response=\"{1}\"", Describe(companion), SafeLog(response));
                    SendVisibleReply(owner, companion, command.Category, ClassifyReplyType(response), response, shouldSpeak, ref response);
                    return true;
                case TrackingCommandAction.RefuseCivilianHunt:
                    response = "I will mark civilians or players if asked, but I will not hunt or attack them.";
                    AIGMExecutionLog.Write("AIGM_TRACK_COMMAND_ROUTE companion={0} route=refuse_civilian_hunt target=\"{1}\" response=\"{2}\"", Describe(companion), SafeLog(command.TargetText), SafeLog(response));
                    SendVisibleReply(owner, companion, command.Category, "status", response, shouldSpeak, ref response);
                    return true;
                case TrackingCommandAction.MonsterTrack:
                    response = AIGMCompanionTrackingService.StartTrackingAction(companion, owner, AIGMCompanionTrackingMode.Monsters, AIGMCompanionTrackingActionMode.TrackOnly);
                    AIGMExecutionLog.Write("AIGM_TRACK_COMMAND_ROUTE companion={0} route=monster_track response=\"{1}\"", Describe(companion), SafeLog(response));
                    AIGMExecutionLog.Write("AIGM_TRACK_STATS companion={0} mode=Monsters action=TrackOnly response=\"{1}\"", Describe(companion), SafeLog(response));
                    SendVisibleReply(owner, companion, command.Category, ClassifyReplyType(response), response, shouldSpeak, ref response);
                    return true;
                case TrackingCommandAction.MonsterHunt:
                    AIGMExecutionLog.Write("AIGM_HUNT_COMMAND_RECEIVED phrase={0} mode={1} companion={2} owner={3}",
                        SafeLog(speech),
                        "TrackHunt",
                        Describe(companion),
                        Describe(owner));
                    response = DispatchHardHuntStart(companion, owner, speech, "hunt_monsters", false);
                    AIGMExecutionLog.Write("AIGM_TRACK_COMMAND_ROUTE companion={0} route=monster_hunt response=\"{1}\"", Describe(companion), SafeLog(response));
                    if (!String.IsNullOrWhiteSpace(response))
                        SendVisibleReply(owner, companion, command.Category, ClassifyReplyType(response), response, shouldSpeak, ref response);
                    return true;
                default:
                    return false;
            }
        }

        private static string StartLocate(BaseHire companion, Mobile owner, string targetText)
        {
            AIGMExecutionLog.Write("AIGM_TRACK_TO_LOCATE companion={0} target=\"{1}\"", Describe(companion), SafeLog(targetText));

            AIGMNamedMobileResolution resolution = AIGMCompanionLocateService.ResolveNamedMobile(owner, targetText, LocateRange);
            if (resolution.Target == null)
                return resolution.Message;

            string response;
            AIGMCompanionLocateService.StartLocate(companion, owner, resolution.Target, AIGMCompanionLocateKind.Locate, 2, out response);
            return companion.Name + " is tracking " + SafeName(resolution.Target) + ".";
        }

        private static bool TryParseTrackingCommand(string speech, AIGMCompanionCommandRouteDecision decision, out ParsedTrackingCommand command)
        {
            command = new ParsedTrackingCommand();
            string normalized = Normalize(speech);
            if (String.IsNullOrWhiteSpace(normalized))
                return false;

            command.NormalizedSpeech = normalized;

            string payload = StripCompanionPrefix(normalized);
            if (IsDialogueAboutTracking(payload))
            {
                command.IsDialogue = true;
                command.Intent = "dialogue";
                command.Category = "dialogue";
                return true;
            }

            if (payload == "tracking status" || payload == "track status" || payload == "report tracking" || payload == "report tracking status" || payload == "what are you tracking")
            {
                command.Action = TrackingCommandAction.Status;
                command.Intent = "status";
                command.Category = "status";
                return true;
            }

            if (payload == "track locations" || payload == "track location" || payload == "scan locations" || payload == "scan landmarks" || payload == "track landmarks" || payload == "nearest landmarks")
            {
                command.Action = TrackingCommandAction.LocationList;
                command.Intent = "report";
                command.Category = "locations";
                return true;
            }

            if (payload == "stop tracking" || payload == "end tracking" || payload == "track off" || payload == "track cycle off" || payload == "stop trail" || payload == "stop hunting")
            {
                command.Action = TrackingCommandAction.Stop;
                command.Intent = "stop";
                command.Category = "status";
                return true;
            }

            if (payload == "start tracking" || payload == "begin tracking" || payload == "track on" || payload == "companions start tracking" || payload == "everyone start tracking")
            {
                command.Action = TrackingCommandAction.StartWatch;
                command.Intent = "start";
                command.Category = "monsters";
                return true;
            }

            string target;
            bool huntVerb = false;
            if (TryExtractTrackingTarget(payload, out target, out huntVerb))
            {
                command.TargetText = target;
                if (IsCivilianTarget(target))
                {
                    command.Action = huntVerb ? TrackingCommandAction.RefuseCivilianHunt : TrackingCommandAction.NpcTrack;
                    command.Intent = huntVerb ? "hunt" : "report";
                    command.Category = "npcs";
                    return true;
                }

                if (IsPlayerTarget(target))
                {
                    command.Action = huntVerb ? TrackingCommandAction.RefuseCivilianHunt : TrackingCommandAction.PlayerTrack;
                    command.Intent = huntVerb ? "hunt" : "report";
                    command.Category = "players";
                    return true;
                }

                if (IsAnimalTarget(target))
                {
                    command.Action = huntVerb ? TrackingCommandAction.RefuseCivilianHunt : TrackingCommandAction.AnimalTrack;
                    command.Intent = huntVerb ? "hunt" : "report";
                    command.Category = "animals";
                    return true;
                }

                if (IsMonsterTarget(target))
                {
                    command.Action = huntVerb ? TrackingCommandAction.MonsterHunt : TrackingCommandAction.MonsterTrack;
                    command.Intent = huntVerb ? "hunt" : "report";
                    command.Category = "monsters";
                    return true;
                }

                AIGMNavigationLocation location;
                if (AIGMNavigationLocationRegistry.TryResolve(target, null, out location))
                {
                    command.Action = TrackingCommandAction.LocationTrack;
                    command.Intent = "report";
                    command.Category = "locations";
                    return true;
                }

                if (!String.IsNullOrWhiteSpace(target))
                {
                    command.Action = TrackingCommandAction.Locate;
                    command.Intent = "report";
                    command.Category = "locate";
                    return true;
                }

                command.Action = TrackingCommandAction.StartWatch;
                command.Intent = "start";
                command.Category = "monsters";
                return true;
            }

            if (decision != null && (decision.VerbKind == AIGMCompanionCommandVerbKind.StartTracking || decision.VerbKind == AIGMCompanionCommandVerbKind.TrackReadOnly))
            {
                command.Action = decision.VerbKind == AIGMCompanionCommandVerbKind.StartTracking ? TrackingCommandAction.StartWatch : TrackingCommandAction.MonsterTrack;
                command.Intent = decision.VerbKind == AIGMCompanionCommandVerbKind.StartTracking ? "start" : "report";
                command.Category = "monsters";
                return true;
            }

            return false;
        }

        private static string BuildLocationList(BaseHire companion, Mobile owner)
        {
            Point3D from = companion != null ? companion.Location : (owner != null ? owner.Location : Point3D.Zero);
            Map map = companion != null ? companion.Map : (owner != null ? owner.Map : null);
            string stats = AIGMNavigationLocationRegistry.FormatNearest(from, map, 6);
            string graphStats = FormatNearestNavNodes(from, map, 6);
            string response = "Tracking locations: landmarks: " + stats + " | nav nodes: " + graphStats;
            AIGMExecutionLog.Write("AIGM_LOCATION_TRACK_PARSE companion={0} target=\"locations\" result=list", Describe(companion));
            AIGMExecutionLog.Write("AIGM_LOCATION_TRACK_STATS companion={0} current={1} response=\"{2}\"", Describe(companion), FormatPoint(from), SafeLog(response));
            return response;
        }

        private static string BuildLocationStats(BaseHire companion, Mobile owner, string targetText)
        {
            Point3D from = companion != null ? companion.Location : (owner != null ? owner.Location : Point3D.Zero);
            Map map = companion != null ? companion.Map : (owner != null ? owner.Map : null);
            AIGMNavigationLocation location;
            AIGMNavNode node = AIGMNavGraphStore.Graph.FindNode(targetText);
            if (node != null)
            {
                AIGMNavRoutePlan plan = AIGMNavRoutePlanner.BuildRoute(companion ?? owner, node.Location, map, node.Name);
                string nodeResponse = String.Format("Tracking location: {0}: node={1}; type={2}; point={3}; radius={4}; distance={5} tiles {6}; route={7}; steps={8}.",
                    node.Name,
                    node.Id,
                    node.Type,
                    FormatPoint(node.Location),
                    node.ArrivalRadius,
                    AIGMNavGraph.Distance(from, node.Location),
                    AIGMNavigationLocationRegistry.FormatDirection(from, node.Location),
                    plan.Success ? "graph" : plan.Reason,
                    plan.Success ? plan.Steps.Count : 0);
                AIGMExecutionLog.Write("AIGM_LOCATION_TRACK_PARSE companion={0} target=\"{1}\" result=nav_node node={2}", Describe(companion), SafeLog(targetText), SafeLog(node.Id));
                AIGMExecutionLog.Write("AIGM_LOCATION_TRACK_STATS companion={0} target=\"{1}\" current={2} nodePoint={3} arrivalRadius={4} response=\"{5}\"", Describe(companion), SafeLog(node.Name), FormatPoint(from), FormatPoint(node.Location), node.ArrivalRadius, SafeLog(nodeResponse));
                return nodeResponse;
            }

            if (!AIGMNavigationLocationRegistry.TryResolve(targetText, map, out location))
            {
                AIGMExecutionLog.Write("AIGM_LOCATION_TRACK_PARSE companion={0} target=\"{1}\" result=failed", Describe(companion), SafeLog(targetText));
                return "I do not know that landmark yet.";
            }

            string stats = AIGMNavigationLocationRegistry.FormatLocationStats(location, from, map);
            AIGMNavigationRoutePreview preview = AIGMNavigationRouteGraph.BuildPreview(from, map, location);
            string response = "Tracking location: " + stats + " Route=" + (preview.UsesGraph ? "graph" : "direct") + "; first=" + FormatPoint(preview.FirstWaypoint) + "; canonical=" + FormatPoint(preview.CanonicalDestination) + "; standTarget=" + FormatPoint(preview.ResolvedStandableTarget) + ".";
            AIGMExecutionLog.Write("AIGM_LOCATION_TRACK_PARSE companion={0} target=\"{1}\" result=resolved canonical=\"{2}\"", Describe(companion), SafeLog(targetText), SafeLog(location.DisplayName));
            AIGMExecutionLog.Write("AIGM_LOCATION_TRACK_STATS companion={0} target=\"{1}\" current={2} canonical={3} standTarget={4} arrivalRadius={5} response=\"{6}\"", Describe(companion), SafeLog(location.DisplayName), FormatPoint(from), FormatPoint(location.Point), FormatPoint(preview.ResolvedStandableTarget), preview.ArrivalRadius, SafeLog(response));
            return response;
        }

        private static string DispatchHardHuntStart(BaseHire sourceCompanion, Mobile owner, string speech, string mode, bool sourceIsStartTracking)
        {
            string normalized = Normalize(speech);
            if (IsDuplicateHardCommand(owner, normalized))
            {
                AIGMExecutionLog.Write("AIGM_HUNT_COMMAND_DUPLICATE_SUPPRESSED owner={0} phrase={1}",
                    Describe(owner),
                    SafeLog(speech));
                return String.Empty;
            }

            List<BaseHire> eligible = GetEligibleHardCommandCompanions(owner);
            AIGMExecutionLog.Write("AIGM_HUNT_COMMAND_DISPATCH owner={0} phrase={1} mode={2} sourceCompanion={3} eligibleCount={4}",
                Describe(owner),
                SafeLog(speech),
                SafeLog(mode),
                Describe(sourceCompanion),
                eligible.Count);

            RememberHardCommand(owner, normalized);
            return AIGMTrackingHuntService.StartMonsterHuntGroup(owner, speech, sourceIsStartTracking);
        }

        private static string DispatchHardHuntStop(BaseHire sourceCompanion, Mobile owner, string speech, string mode)
        {
            string normalized = Normalize(speech);
            if (IsDuplicateHardCommand(owner, normalized))
            {
                AIGMExecutionLog.Write("AIGM_HUNT_COMMAND_DUPLICATE_SUPPRESSED owner={0} phrase={1}",
                    Describe(owner),
                    SafeLog(speech));
                return String.Empty;
            }

            List<BaseHire> eligible = GetEligibleHardCommandCompanions(owner);
            AIGMExecutionLog.Write("AIGM_HUNT_COMMAND_DISPATCH owner={0} phrase={1} mode={2} sourceCompanion={3} eligibleCount={4}",
                Describe(owner),
                SafeLog(speech),
                SafeLog(mode),
                Describe(sourceCompanion),
                eligible.Count);

            RememberHardCommand(owner, normalized);
            return AIGMTrackingHuntService.StopMonsterHuntGroup(owner, "stop_hunting");
        }

        private static List<BaseHire> GetEligibleHardCommandCompanions(Mobile owner)
        {
            List<BaseHire> companions = AIGMCompanionControlStopService.GetOwnedCompanions(owner, HardCommandRadius);
            List<BaseHire> eligible = new List<BaseHire>();
            for (int i = 0; i < companions.Count; i++)
            {
                BaseHire companion = companions[i];
                if (companion == null || companion.Deleted || !companion.Alive || owner == null || owner.Deleted || owner.Map == null || companion.Map != owner.Map)
                    continue;

                if (companion.Map != Map.Felucca)
                    continue;

                eligible.Add(companion);
            }

            return eligible;
        }

        private static bool IsDuplicateHardCommand(Mobile owner, string normalized)
        {
            if (owner == null || String.IsNullOrWhiteSpace(normalized))
                return false;

            HardCommandDispatchState state;
            if (!LastHardDispatchByOwner.TryGetValue(owner.Serial.Value, out state) || state == null)
                return false;

            return String.Equals(state.Phrase, normalized, StringComparison.Ordinal)
                && (DateTime.UtcNow - state.Utc) < TimeSpan.FromSeconds(1.0);
        }

        private static void RememberHardCommand(Mobile owner, string normalized)
        {
            if (owner == null || String.IsNullOrWhiteSpace(normalized))
                return;

            LastHardDispatchByOwner[owner.Serial.Value] = new HardCommandDispatchState
            {
                Phrase = normalized,
                Utc = DateTime.UtcNow
            };
        }

        private static string FormatNearestNavNodes(Point3D from, Map map, int count)
        {
            List<AIGMNavNode> nodes = AIGMNavGraphStore.Graph.Nearest(from, map, count, 0);
            if (nodes.Count == 0)
                return "none";

            List<string> parts = new List<string>();
            for (int i = 0; i < nodes.Count; i++)
                parts.Add(String.Format("{0}, {1} tiles {2}, {3}", nodes[i].Name, AIGMNavGraph.Distance(from, nodes[i].Location), AIGMNavigationLocationRegistry.FormatDirection(from, nodes[i].Location), FormatPoint(nodes[i].Location)));

            return String.Join(" | ", parts.ToArray());
        }

        private static bool TryExtractTrackingTarget(string payload, out string target, out bool huntVerb)
        {
            target = String.Empty;
            huntVerb = false;
            if (String.IsNullOrWhiteSpace(payload))
                return false;

            string[] huntPrefixes = { "hunt ", "start hunting ", "track and hunt ", "find and kill " };
            for (int i = 0; i < huntPrefixes.Length; i++)
            {
                if (payload.StartsWith(huntPrefixes[i], StringComparison.Ordinal))
                {
                    target = CleanTarget(payload.Substring(huntPrefixes[i].Length));
                    huntVerb = true;
                    return true;
                }
            }

            string[] trackPrefixes = { "start tracking ", "track ", "trail ", "find trail ", "follow trail ", "follow trail to ", "track and move to " };
            for (int i = 0; i < trackPrefixes.Length; i++)
            {
                if (payload.StartsWith(trackPrefixes[i], StringComparison.Ordinal))
                {
                    target = CleanTarget(payload.Substring(trackPrefixes[i].Length));
                    return true;
                }
            }

            if (payload == "track")
                return true;

            return false;
        }

        private static bool IsDialogueAboutTracking(string payload)
        {
            if (String.IsNullOrWhiteSpace(payload))
                return false;

            return payload.StartsWith("tell me about tracking", StringComparison.Ordinal)
                || payload.StartsWith("tell me of tracking", StringComparison.Ordinal)
                || payload.StartsWith("what do you think of tracking", StringComparison.Ordinal)
                || payload.StartsWith("what do you think about tracking", StringComparison.Ordinal)
                || payload.StartsWith("explain tracking", StringComparison.Ordinal)
                || payload.StartsWith("talk about tracking", StringComparison.Ordinal);
        }

        private static bool IsMonsterTarget(string target)
        {
            string value = Normalize(target);
            return String.IsNullOrWhiteSpace(value)
                || value == "monster"
                || value == "monsters"
                || value == "hostile"
                || value == "hostiles"
                || value == "enemy"
                || value == "enemies";
        }

        private static bool IsAnimalTarget(string target)
        {
            string value = Normalize(target);
            return value == "animal"
                || value == "animals";
        }

        private static bool IsCivilianTarget(string target)
        {
            string value = Normalize(target);
            return value == "npc"
                || value == "npcs"
                || value == "human"
                || value == "humans"
                || value == "human npc"
                || value == "human npcs"
                || value == "people"
                || value == "townsfolk";
        }

        private static bool IsPlayerTarget(string target)
        {
            string value = Normalize(target);
            return value == "player"
                || value == "players";
        }

        private static string StripCompanionPrefix(string normalized)
        {
            string[] aliases = { "dakeyras", "dak", "waylander", "danyal", "dan", "dardalion", "dard", "dar", "companions", "everyone" };
            for (int i = 0; i < aliases.Length; i++)
            {
                string alias = aliases[i];
                if (normalized.StartsWith(alias + " ", StringComparison.Ordinal))
                    return normalized.Substring(alias.Length).Trim();
            }

            return normalized;
        }

        private static string CleanTarget(string target)
        {
            string value = Normalize(target);
            if (value.StartsWith("the ", StringComparison.Ordinal))
                value = value.Substring(4).Trim();
            if (value.EndsWith(" trail", StringComparison.Ordinal))
                value = value.Substring(0, value.Length - 6).Trim();
            return value;
        }

        private static string Normalize(string text)
        {
            string value = text == null ? String.Empty : text.Trim().ToLowerInvariant();
            value = value.Replace(",", " ").Replace(".", " ").Replace("!", " ").Replace("?", " ").Replace(";", " ").Replace(":", " ");
            while (value.Contains("  "))
                value = value.Replace("  ", " ");
            return value.Trim();
        }

        private static string SafeName(Mobile mobile)
        {
            return mobile == null ? "unknown" : (String.IsNullOrWhiteSpace(mobile.Name) ? mobile.GetType().Name : mobile.Name);
        }

        private static string Describe(Mobile mobile)
        {
            return mobile == null ? "none" : (SafeName(mobile) + "[" + mobile.Serial + "]");
        }

        private static string SafeLog(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return String.Empty;
            value = value.Replace('"', '\'').Replace('\r', ' ').Replace('\n', ' ');
            return value.Length > 220 ? value.Substring(0, 220) : value;
        }

        private static string FormatPoint(Point3D point)
        {
            return point.X + "," + point.Y + "," + point.Z;
        }

        private enum TrackingCommandAction
        {
            None,
            StartWatch,
            AnimalTrack,
            MonsterTrack,
            MonsterHunt,
            Locate,
            LocationList,
            LocationTrack,
            NpcTrack,
            PlayerTrack,
            RefuseCivilianHunt,
            Stop,
            Status
        }

        private struct ParsedTrackingCommand
        {
            public TrackingCommandAction Action;
            public string TargetText;
            public bool IsDialogue;
            public string NormalizedSpeech;
            public string Category;
            public string Intent;
        }

        private sealed class HardCommandDispatchState
        {
            public string Phrase;
            public DateTime Utc;
        }

        private static void SendVisibleReply(Mobile owner, BaseHire companion, string category, string replyType, string text, bool shouldSpeak, ref string response)
        {
            if (owner == null || String.IsNullOrWhiteSpace(text))
                return;

            if (!shouldSpeak)
            {
                owner.SendMessage(68, text);
                response = null;
            }

            AIGMExecutionLog.Write("AIGM_TRACK_VISIBLE_REPLY owner={0} companion={1} category={2} replyType={3} sentToJournal={4}",
                Describe(owner),
                Describe(companion),
                SafeLog(category),
                SafeLog(replyType),
                !shouldSpeak);
        }

        private static string ClassifyReplyType(string response)
        {
            string normalized = Normalize(response);
            if (normalized.Contains("no ") && normalized.Contains("trail"))
                return "no_target";
            if (normalized.Contains("is not tracking anything") || normalized.Contains("tracking "))
                return "status";
            if (normalized.Contains("tracking location") || normalized.Contains("i find ") || normalized.Contains("is tracking "))
                return "stats";
            return "stats";
        }
    }
}
