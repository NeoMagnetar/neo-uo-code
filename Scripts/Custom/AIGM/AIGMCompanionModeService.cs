using System;
using System.Collections.Concurrent;

using Server;

namespace Server.Custom.AIGM
{
    public sealed class AIGMCompanionModeState
    {
        public AIGMCompanionModeState(AIGMCompanionMode mode, string reason, DateTime changedUtc)
        {
            Mode = mode;
            Reason = string.IsNullOrWhiteSpace(reason) ? "unspecified" : reason;
            ChangedUtc = changedUtc;
        }

        public AIGMCompanionMode Mode { get; private set; }
        public string Reason { get; private set; }
        public DateTime ChangedUtc { get; private set; }
    }

    public static class AIGMCompanionModeService
    {
        private static readonly ConcurrentDictionary<int, AIGMCompanionModeState> States =
            new ConcurrentDictionary<int, AIGMCompanionModeState>();

        public static void SetMode(Mobile companion, AIGMCompanionMode mode, string reason)
        {
            if (!IsValid(companion))
            {
                return;
            }

            var serial = companion.Serial.Value;
            var next = new AIGMCompanionModeState(mode, reason, DateTime.UtcNow);
            AIGMCompanionModeState previous;

            if (States.TryGetValue(serial, out previous) &&
                previous.Mode == mode &&
                string.Equals(previous.Reason, next.Reason, StringComparison.Ordinal))
            {
                return;
            }

            States[serial] = next;
            SafeLog(string.Format(
                "AIGM_MODE_SET companion={0} mode={1} reason={2}",
                FormatCompanion(companion),
                FormatMode(mode),
                SanitizeReason(next.Reason)));
        }

        public static AIGMCompanionMode GetMode(Mobile companion)
        {
            return GetSnapshot(companion).Mode;
        }

        public static AIGMCompanionModeState GetSnapshot(Mobile companion)
        {
            if (!IsValid(companion))
            {
                return new AIGMCompanionModeState(AIGMCompanionMode.Idle, "invalid_companion", DateTime.MinValue);
            }

            AIGMCompanionModeState state;

            if (States.TryGetValue(companion.Serial.Value, out state))
            {
                return state;
            }

            return new AIGMCompanionModeState(AIGMCompanionMode.Idle, "unset", DateTime.MinValue);
        }

        public static void ClearMode(Mobile companion, string reason)
        {
            if (!IsValid(companion))
            {
                return;
            }

            AIGMCompanionModeState previous;

            if (States.TryRemove(companion.Serial.Value, out previous))
            {
                SafeLog(string.Format(
                    "AIGM_MODE_CLEAR companion={0} previous={1} reason={2}",
                    FormatCompanion(companion),
                    FormatMode(previous.Mode),
                    SanitizeReason(reason)));
            }
        }

        public static bool ClearIfMode(Mobile companion, AIGMCompanionMode mode, string reason)
        {
            if (!IsValid(companion))
            {
                return false;
            }

            AIGMCompanionModeState previous;

            if (!States.TryGetValue(companion.Serial.Value, out previous) || previous.Mode != mode)
            {
                return false;
            }

            if (States.TryRemove(companion.Serial.Value, out previous))
            {
                SafeLog(string.Format(
                    "AIGM_MODE_CLEAR companion={0} previous={1} reason={2}",
                    FormatCompanion(companion),
                    FormatMode(mode),
                    SanitizeReason(reason)));
                return true;
            }

            return false;
        }

        public static bool ClearIfAny(Mobile companion, string reason, params AIGMCompanionMode[] modes)
        {
            if (!IsValid(companion) || modes == null || modes.Length == 0)
            {
                return false;
            }

            var current = GetMode(companion);

            for (var i = 0; i < modes.Length; i++)
            {
                if (modes[i] == current)
                {
                    return ClearIfMode(companion, current, reason);
                }
            }

            return false;
        }

        public static bool IsInMode(Mobile companion, AIGMCompanionMode mode)
        {
            return GetMode(companion) == mode;
        }

        public static string DescribeForCognition(Mobile companion)
        {
            var state = GetSnapshot(companion);
            var changed = state.ChangedUtc == DateTime.MinValue
                ? "unset"
                : Math.Max(0, (int)(DateTime.UtcNow - state.ChangedUtc).TotalSeconds) + "s_ago";

            return string.Format(
                "mode={0}; reason={1}; changed={2}",
                FormatMode(state.Mode),
                SanitizeReason(state.Reason),
                changed);
        }

        public static string FormatMode(AIGMCompanionMode mode)
        {
            switch (mode)
            {
                case AIGMCompanionMode.FollowOwner: return "FOLLOW_OWNER";
                case AIGMCompanionMode.GuardOwner: return "GUARD_OWNER";
                case AIGMCompanionMode.TrackMonster: return "TRACK_MONSTER";
                case AIGMCompanionMode.HuntMonster: return "HUNT_MONSTER";
                case AIGMCompanionMode.SupportBandageSelf: return "SUPPORT_BANDAGE_SELF";
                case AIGMCompanionMode.SupportBandageTarget: return "SUPPORT_BANDAGE_TARGET";
                case AIGMCompanionMode.SupportOwnerMedic: return "SUPPORT_OWNER_MEDIC";
                case AIGMCompanionMode.DialogueNatural: return "DIALOGUE_NATURAL";
                case AIGMCompanionMode.DialogueGroupBanter: return "DIALOGUE_GROUP_BANTER";
                case AIGMCompanionMode.DialogueCompanionReply: return "DIALOGUE_COMPANION_REPLY";
                case AIGMCompanionMode.DialogueQuietMode: return "DIALOGUE_QUIET_MODE";
                case AIGMCompanionMode.AssessmentExplicit: return "ASSESSMENT_EXPLICIT";
                case AIGMCompanionMode.RefusalPlayerHunt: return "REFUSAL_PLAYER_HUNT";
                case AIGMCompanionMode.LocateMobile: return "LOCATE_MOBILE";
                case AIGMCompanionMode.Regroup: return "REGROUP";
                case AIGMCompanionMode.Stopped: return "STOPPED";
                default: return "IDLE";
            }
        }

        private static bool IsValid(Mobile companion)
        {
            return companion != null && !companion.Deleted;
        }

        private static string FormatCompanion(Mobile companion)
        {
            var name = companion == null ? "unknown" : (companion.Name ?? companion.GetType().Name);
            var serial = companion == null ? "0" : companion.Serial.ToString();

            return string.Format("{0}/{1}", name, serial);
        }

        private static string SanitizeReason(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return "unspecified";
            }

            return reason.Replace(' ', '_').Replace('\t', '_').Replace('\r', '_').Replace('\n', '_');
        }

        private static void SafeLog(string message)
        {
            try
            {
                AIGMExecutionLog.Write(message);
            }
            catch
            {
            }
        }
    }
}
