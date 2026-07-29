using System;
using System.Collections.Generic;

using Server;
using Server.Custom.AIGM.Navigation;
using Server.Custom.AIGM.Tasks;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    [Flags]
    public enum AIGMCompanionStopScope
    {
        None = 0,
        Movement = 1,
        Tracking = 2,
        Hunt = 4,
        Navigation = 8,
        Regroup = 16,
        Conversation = 32,
        AllAIGMActivity = Movement | Tracking | Hunt | Navigation | Regroup | Conversation
    }

    public sealed class AIGMCompanionActivityStatus
    {
        public string CompanionName;
        public string Mode;
        public string SmartMovement;
        public string NativeNavigation;
        public string Tracking;
        public string Locate;
        public OrderType ControlOrder;
        public string ControlTarget;
    }

    public static class AIGMCompanionControlStopService
    {
        private static readonly Dictionary<Serial, DateTime> LastVisibleStopNoticeByOwner = new Dictionary<Serial, DateTime>();

        public static bool TryHandleSpeech(BaseHire heardBy, Mobile owner, string speech, AIGMCompanionCommandRouteDecision decision, bool shouldSpeak, out string response)
        {
            response = null;

            if (owner == null || String.IsNullOrWhiteSpace(speech))
                return false;

            string normalized = Normalize(speech);
            bool stopAll = IsBroadStopAll(normalized) || (decision != null && decision.VerbKind == AIGMCompanionCommandVerbKind.StopAll);
            bool help = normalized == "tracking help" || normalized == "track help" || normalized == "find help" || normalized == "locate help";
            if (help || (decision != null && decision.VerbKind == AIGMCompanionCommandVerbKind.TrackingHelp))
            {
                response = BuildTrackingHelp();
                return true;
            }

            if (IsResumePhrase(normalized))
            {
                int resumed = ApplyOperationalReleaseAll(owner, "speech:" + normalized);
                response = resumed > 0 ? BuildOperationalReceipt(owner, "Resume", "resumed") : "No owned AIGM companions found.";
                if (!shouldSpeak)
                    TrySendVisibleOwnerStopNotice(owner, response);
                return true;
            }

            if (IsStandDownPhrase(normalized))
            {
                int stoodDown = ApplyOperationalStopAll(owner, AIGMOperationalStopMode.PassiveStandDown, "speech:" + normalized);
                if ((AIGMCompanionStopScope.Conversation & AIGMCompanionStopScope.Conversation) != 0)
                    AIGMCompanionDialogueThreadService.StopForOwner(owner, "stand_down");

                response = stoodDown > 0 ? BuildOperationalReceipt(owner, "Stand down", "standing down") : "No owned AIGM companions found.";
                if (!shouldSpeak)
                    TrySendVisibleOwnerStopNotice(owner, response);
                return true;
            }

            if (IsHuntLifecycleStopPhrase(normalized) && AIGMTrackingHuntService.HasActiveMonsterHuntGroup(owner))
            {
                response = AIGMTrackingHuntService.StopMonsterHuntGroup(owner, "speech:" + normalized);
                if (!shouldSpeak && !String.IsNullOrWhiteSpace(response))
                    TrySendVisibleOwnerStopNotice(owner, response);

                return true;
            }

            if (!stopAll)
                return false;

            AIGMCompanionStopScope scope = ParseScope(normalized);
            ApplyOperationalStopAll(owner, AIGMOperationalStopMode.CancelMission, "speech:" + normalized);
            if ((scope & AIGMCompanionStopScope.Conversation) != 0)
                AIGMCompanionDialogueThreadService.StopForOwner(owner, "stop_all");

            response = BuildOperationalReceipt(owner, "Stop", "stopped");
            if (!shouldSpeak)
                TrySendVisibleOwnerStopNotice(owner, response);

            return true;
        }

        private static int ApplyOperationalStopAll(Mobile owner, AIGMOperationalStopMode mode, string reason)
        {
            int count = 0;
            List<BaseHire> companions = GetOwnedCompanions(owner, 0);
            for (int i = 0; i < companions.Count; i++)
            {
                AIGMOperationalControlService.ClearAllOperationalState(companions[i], mode, owner, reason);
                count++;
            }

            AIGMExecutionLog.Write("AIGM_STOP_ALL owner={0} operationalMode={1} count={2} reason={3}", Describe(owner), mode, count, SafeLog(reason));
            return count;
        }

        private static int ApplyOperationalReleaseAll(Mobile owner, string reason)
        {
            int count = 0;
            List<BaseHire> companions = GetOwnedCompanions(owner, 0);
            for (int i = 0; i < companions.Count; i++)
            {
                AIGMOperationalControlService.Release(companions[i], owner, reason);
                count++;
            }

            return count;
        }

        public static int StopAllCompanionActivity(Mobile owner, AIGMCompanionStopScope scope, string reason)
        {
            int count = 0;
            List<BaseHire> companions = GetOwnedCompanions(owner, 0);
            for (int i = 0; i < companions.Count; i++)
            {
                StopCompanionActivity(companions[i], scope, reason);
                count++;
            }

            AIGMExecutionLog.Write("AIGM_STOP_ALL owner={0} scope={1} count={2} reason={3}", Describe(owner), scope, count, SafeLog(reason));
            return count;
        }

        public static void StopCompanionActivity(BaseCreature companion, AIGMCompanionStopScope scope, string reason)
        {
            BaseHire hire = companion as BaseHire;
            if (hire == null || hire.Deleted)
                return;

            string smartResponse;
            string nativeResponse;

            if ((scope & AIGMCompanionStopScope.Regroup) != 0)
                AIGMCompanionLocateService.StopLocate(hire, reason);

            if ((scope & AIGMCompanionStopScope.Navigation) != 0 || (scope & AIGMCompanionStopScope.Movement) != 0)
            {
                string legacyResponse;
                AIGMLegacyTravelService.Stop(hire, reason, out legacyResponse);
                AIGMNativeNavigationService.Stop(hire, reason, out nativeResponse);
                AIGMSmartMovementService.Stop(hire, reason, out smartResponse);
            }

            if ((scope & AIGMCompanionStopScope.Tracking) != 0 || (scope & AIGMCompanionStopScope.Hunt) != 0)
            {
                AIGMCompanionTrackingService.StopTracking(hire, hire.GetOwner(), false);
            }

            if ((scope & AIGMCompanionStopScope.Hunt) != 0)
            {
                AIGMCompanionExecutionSpine.StopMonsterHunt(hire, reason);
                hire.Combatant = null;
                hire.Warmode = false;
            }

            if ((scope & (AIGMCompanionStopScope.Movement | AIGMCompanionStopScope.Navigation | AIGMCompanionStopScope.Regroup)) != 0)
            {
                AIGMCompanionControlStateService.RestoreSoftHold(hire, reason);
            }

            AIGMCompanionModeService.SetMode(hire, AIGMCompanionMode.Stopped, reason);
            AIGMExecutionLog.Write("AIGM_STOP_COMPANION companion={0} scope={1} order={2} target={3} cantWalk={4} home={5} rangeHome={6} reason={7}", Describe(hire), scope, hire.ControlOrder, Describe(hire.ControlTarget as Mobile), hire.CantWalk, FormatPoint(hire.Home), hire.RangeHome, SafeLog(reason));
        }

        public static AIGMCompanionActivityStatus GetActiveActivityStatus(BaseCreature companion)
        {
            BaseHire hire = companion as BaseHire;
            if (hire == null)
                return null;

            AIGMCompanionTrackingState tracking = AIGMCompanionTrackingService.GetState(hire);
            AIGMCompanionActivityStatus status = new AIGMCompanionActivityStatus();
            status.CompanionName = hire.Name ?? hire.GetType().Name;
            status.Mode = AIGMCompanionModeService.FormatMode(AIGMCompanionModeService.GetMode(hire));
            status.SmartMovement = AIGMSmartMovementService.GetStatus(hire);
            status.NativeNavigation = AIGMLegacyTravelService.GetStatus(hire) + " | " + AIGMNativeNavigationService.GetStatus(hire);
            status.Tracking = tracking != null && tracking.IsActive ? tracking.Mode + "/" + tracking.ActionMode + "/" + tracking.CurrentTargetName : "inactive";
            status.Locate = AIGMCompanionLocateService.GetStatus(hire);
            status.ControlOrder = hire.ControlOrder;
            status.ControlTarget = Describe(hire.ControlTarget as Mobile);
            return status;
        }

        public static string FormatStatus(Mobile owner)
        {
            List<BaseHire> companions = GetOwnedCompanions(owner, 0);
            if (companions.Count == 0)
                return "No owned AIGM companions found.";

            List<string> lines = new List<string>();
            for (int i = 0; i < companions.Count; i++)
            {
                AIGMCompanionActivityStatus s = GetActiveActivityStatus(companions[i]);
                if (s == null)
                    continue;

                lines.Add(String.Format("{0}: mode={1}; order={2}; target={3}; tracking={4}; locate={5}", s.CompanionName, s.Mode, s.ControlOrder, s.ControlTarget, s.Tracking, s.Locate));
            }

            return String.Join(" | ", lines.ToArray());
        }

        public static string BuildTrackingHelp()
        {
            return "Tracking help: start tracking = default monster tracking; Dakeyras start tracking = Dakeyras tracks monsters; Dakeyras start tracking Danyal = Dakeyras locates Danyal without attacking; Dakeyras hunt monster = hostile hunt if a valid target is found; stop tracking stops active tracking; stop everything clears AIGM movement, tracking, hunt, navigation, and regroup.";
        }

        private static string BuildOperationalReceipt(Mobile owner, string title, string result)
        {
            List<BaseHire> companions = GetOwnedCompanions(owner, 0);
            if (companions.Count == 0)
                return "No owned AIGM companions found.";

            List<string> lines = new List<string>();
            lines.Add(title + ":");
            for (int i = 0; i < companions.Count; i++)
            {
                BaseHire companion = companions[i];
                string lineResult = companion.Deleted || !companion.Alive ? "dead/deleted" : result;
                lines.Add(String.Format("{0}: {1}", companion.Name ?? companion.GetType().Name, lineResult));
            }

            return String.Join(" | ", lines.ToArray());
        }

        public static List<BaseHire> GetOwnedCompanions(Mobile owner, int range)
        {
            List<BaseHire> result = new List<BaseHire>();
            if (owner == null || owner.Map == null)
                return result;

            if (range <= 0)
            {
                foreach (Mobile mobile in World.Mobiles.Values)
                {
                    BaseHire hire = mobile as BaseHire;
                    IAIGMCompanionActor actor = mobile as IAIGMCompanionActor;
                    if (hire == null || actor == null || hire.Deleted || !hire.Alive || hire.GetOwner() != owner || hire.Map != owner.Map)
                        continue;

                    result.Add(hire);
                }

                return result;
            }

            IPooledEnumerable mobiles = owner.Map.GetMobilesInRange(owner.Location, range);
            foreach (Mobile mobile in mobiles)
            {
                BaseHire hire = mobile as BaseHire;
                IAIGMCompanionActor actor = mobile as IAIGMCompanionActor;
                if (hire == null || actor == null || hire.Deleted || !hire.Alive || hire.GetOwner() != owner)
                    continue;

                result.Add(hire);
            }
            mobiles.Free();
            return result;
        }

        private static AIGMCompanionStopScope ParseScope(string normalized)
        {
            if (normalized.Contains("tracking") || normalized.Contains("hunting"))
                return AIGMCompanionStopScope.Tracking | AIGMCompanionStopScope.Hunt | AIGMCompanionStopScope.Regroup | AIGMCompanionStopScope.Movement | AIGMCompanionStopScope.Navigation;

            if (normalized.Contains("navigation") || normalized.Contains("travel") || normalized.Contains("moving") || normalized.Contains("movement"))
                return AIGMCompanionStopScope.Movement | AIGMCompanionStopScope.Navigation | AIGMCompanionStopScope.Regroup;

            return AIGMCompanionStopScope.AllAIGMActivity;
        }

        private static bool IsBroadStopAll(string normalized)
        {
            if (String.IsNullOrWhiteSpace(normalized))
                return false;

            return normalized == "stop everything"
                || normalized == "everyone stop"
                || normalized == "companions stop"
                || normalized == "stop all"
                || normalized == "halt all"
                || normalized == "stop moving"
                || normalized == "stop traveling"
                || normalized == "stop tracking"
                || normalized == "stop hunting"
                || normalized == "stop navigation"
                || normalized == "cancel all movement"
                || normalized == "cancel tracking"
                || normalized == "regroup stop"
                || normalized == "hold position"
                || normalized == "everyone hold";
        }

        private static bool IsHuntLifecycleStopPhrase(string normalized)
        {
            if (String.IsNullOrWhiteSpace(normalized))
                return false;

            return normalized == "stop hunting"
                || normalized == "stop hunt"
                || normalized == "stop tracking";
        }

        private static bool IsStandDownPhrase(string normalized)
        {
            if (String.IsNullOrWhiteSpace(normalized))
                return false;

            return normalized == "stand down"
                || normalized == "standdown"
                || normalized == "everyone stand down"
                || normalized == "companions stand down"
                || normalized == "all stand down";
        }

        private static bool IsResumePhrase(string normalized)
        {
            if (String.IsNullOrWhiteSpace(normalized))
                return false;

            return normalized == "resume"
                || normalized == "everyone resume"
                || normalized == "companions resume"
                || normalized == "resume operations";
        }

        private static void TrySendVisibleOwnerStopNotice(Mobile owner, string response)
        {
            if (owner == null || owner.Deleted || String.IsNullOrWhiteSpace(response))
                return;

            DateTime now = DateTime.UtcNow;
            DateTime last;
            if (LastVisibleStopNoticeByOwner.TryGetValue(owner.Serial, out last)
                && (now - last) < TimeSpan.FromSeconds(1.0))
                return;

            LastVisibleStopNoticeByOwner[owner.Serial] = now;
            owner.SendMessage(response);
        }

        private static string Normalize(string text)
        {
            string value = text == null ? String.Empty : text.Trim().ToLowerInvariant();
            value = value.Replace(",", " ").Replace(".", " ").Replace("!", " ").Replace("?", " ").Replace(";", " ").Replace(":", " ");
            while (value.Contains("  "))
                value = value.Replace("  ", " ");
            return value.Trim();
        }

        private static string Describe(Mobile mobile)
        {
            return mobile == null ? "none" : ((mobile.Name ?? mobile.GetType().Name) + "[" + mobile.Serial + "]");
        }

        private static string SafeLog(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return String.Empty;
            return value.Replace('"', '\'').Replace('\r', ' ').Replace('\n', ' ');
        }

        private static string FormatPoint(Point3D point)
        {
            return String.Format("{0},{1},{2}", point.X, point.Y, point.Z);
        }
    }
}
