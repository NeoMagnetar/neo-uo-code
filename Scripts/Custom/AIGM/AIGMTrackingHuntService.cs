using System;
using System.Collections.Generic;
using Server;
using Server.Custom.AIGM.Tasks;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMTrackingHuntService
    {
        private const int GroupCommandRadius = 40;

        public static string StartMonsterHunt(BaseHire companion, Mobile owner)
        {
            Mobile actualOwner = owner ?? (companion != null ? companion.GetOwner() : null);
            return StartMonsterHuntGroup(actualOwner, "hunt monsters", false);
        }

        public static string StartMonsterHuntGroup(Mobile owner, string sourcePhrase, bool sourceIsStartTracking)
        {
            if (owner == null || owner.Deleted || !owner.Alive)
                return "I need my owner present before I hunt.";

            List<BaseHire> companions = GetEligibleCompanions(owner);
            List<string> names = new List<string>();
            int started = 0;
            string firstResponse = null;

            for (int i = 0; i < companions.Count; i++)
                names.Add(SafeName(companions[i]));

            AIGMExecutionLog.Write("AIGM_HUNT_GROUP_START owner={0} companions={1} count={2} mode=monster sourcePhrase={3}",
                Describe(owner),
                SafeLog(String.Join(",", names.ToArray())),
                companions.Count,
                SafeLog(sourcePhrase));

            for (int i = 0; i < companions.Count; i++)
            {
                BaseHire companion = companions[i];
                string validationFailure = ValidateEligibility(companion, owner);
                if (!String.IsNullOrWhiteSpace(validationFailure))
                {
                    AIGMExecutionLog.Write("AIGM_HUNT_COMPANION_SKIP companion={0} reason={1}",
                        Describe(companion),
                        SafeLog(validationFailure));
                    continue;
                }

                string operationalRejection;
                if (!AIGMOperationalControlService.BeginExplicitCommand(companion, owner, "hunt_monsters", out operationalRejection))
                {
                    AIGMExecutionLog.Write("AIGM_HUNT_COMPANION_SKIP companion={0} reason=operational_rejected:{1}",
                        Describe(companion),
                        SafeLog(operationalRejection));
                    continue;
                }

                AIGMMovementLease lease;
                string rejectionReason;
                if (!AIGMMovementOwnershipService.TryAcquireOrReplace(
                    companion,
                    AIGMMovementLeaseOwnerType.Hunt,
                    owner,
                    null,
                    AIGMMovementOwnershipService.GetDefaultPolicy(AIGMMovementLeaseOwnerType.Hunt),
                    "group_hunt_start",
                    true,
                    out lease,
                    out rejectionReason))
                {
                    AIGMExecutionLog.Write("AIGM_HUNT_COMPANION_SKIP companion={0} reason=lease_rejected:{1}",
                        Describe(companion),
                        SafeLog(rejectionReason));
                    continue;
                }

                AIGMExecutionLog.Write("AIGM_HUNT_COMPANION_JOIN companion={0}", Describe(companion));
                string ignored;
                AIGMLegacyTravelService.Stop(companion, "group_hunt_takeover", out ignored);
                AIGMCompanionLocateService.StopLocate(companion, "group_hunt_start");
                string response = AIGMCompanionTrackingService.StartTrackingAction(companion, owner, AIGMCompanionTrackingMode.Monsters, AIGMCompanionTrackingActionMode.TrackHunt);
                if (!IsActiveMonsterHunt(companion))
                {
                    AIGMMovementOwnershipService.ReleaseIfOwned(companion, AIGMMovementLeaseOwnerType.Hunt, owner, null, "group_hunt_start_failed", AIGMMovementLeaseTerminalStatus.Rejected);
                    AIGMExecutionLog.Write("AIGM_HUNT_COMPANION_SKIP companion={0} reason=start_tracking_failed", Describe(companion));
                    continue;
                }

                AIGMMovementOwnershipService.Heartbeat(companion, AIGMMovementLeaseOwnerType.Hunt, owner, null, "group_hunt_started");
                if (String.IsNullOrWhiteSpace(firstResponse))
                    firstResponse = response;
                started++;
            }

            if (started == 0)
                return "No eligible companions can hunt right now.";

            return !String.IsNullOrWhiteSpace(firstResponse)
                ? firstResponse
                : started.ToString() + " companions begin hunting monsters.";
        }

        public static string StopMonsterHuntGroup(Mobile owner, string reason)
        {
            if (owner == null || owner.Deleted)
                return "No eligible companions are hunting right now.";

            List<BaseHire> companions = GetEligibleCompanions(owner);
            List<string> activeNames = new List<string>();
            int stopped = 0;
            for (int i = 0; i < companions.Count; i++)
            {
                BaseHire companion = companions[i];
                if (CancelHunt(companion, owner, reason))
                {
                    stopped++;
                    activeNames.Add(SafeName(companion));
                }
            }

            AIGMExecutionLog.Write("AIGM_HUNT_STOP_GROUP owner={0} count={1} reason={2} companions={3}",
                Describe(owner),
                stopped,
                SafeLog(reason),
                SafeLog(String.Join(",", activeNames.ToArray())));

            return stopped > 0
                ? stopped.ToString() + " companions stop hunting."
                : "No eligible companions are hunting right now.";
        }

        public static bool HasActiveMonsterHuntGroup(Mobile owner)
        {
            if (owner == null || owner.Deleted)
                return false;

            List<BaseHire> companions = GetEligibleCompanions(owner);
            for (int i = 0; i < companions.Count; i++)
            {
                if (IsActiveMonsterHunt(companions[i]))
                    return true;
            }

            return false;
        }

        public static bool Stop(BaseHire companion, Mobile owner, string reason, bool restoreFollow)
        {
            if (companion == null || companion.Deleted)
                return false;

            AIGMCompanionTrackingState state = AIGMCompanionTrackingService.GetState(companion);
            bool wasActiveHunt = state != null
                && state.IsActive
                && state.ActionMode == AIGMCompanionTrackingActionMode.TrackHunt
                && state.Mode == AIGMCompanionTrackingMode.Monsters;

            AIGMCompanionLocateService.StopLocate(companion, reason);
            string response = AIGMCompanionTrackingService.StopTracking(companion, owner, restoreFollow);

            if (restoreFollow)
                AIGMCompanionControlStateService.RestoreFollowOwner(companion, owner ?? companion.GetOwner(), reason);
            else
                AIGMMovementOwnershipService.ReleaseIfOwned(companion, AIGMMovementLeaseOwnerType.Hunt, owner ?? companion.GetOwner(), null, reason, AIGMMovementLeaseTerminalStatus.Released);

            AIGMExecutionLog.Write("AIGM_HUNT_STOP companion={0} owner={1} reason={2} response={3}",
                Describe(companion),
                Describe(owner ?? companion.GetOwner()),
                SafeLog(reason),
                SafeLog(response));

            return wasActiveHunt;
        }

        public static bool CancelHunt(BaseHire companion, Mobile owner, string reason)
        {
            return Stop(companion, owner, reason, false);
        }

        public static bool CancelHuntAndFollowOwner(BaseHire companion, Mobile owner, string reason)
        {
            return Stop(companion, owner, reason, true);
        }

        public static void Pulse(BaseHire companion)
        {
            if (companion == null || companion.Deleted)
                return;

            AIGMCompanionTrackingService.Pulse(companion);
        }

        private static List<BaseHire> GetEligibleCompanions(Mobile owner)
        {
            List<BaseHire> companions = AIGMCompanionControlStopService.GetOwnedCompanions(owner, GroupCommandRadius);
            List<BaseHire> eligible = new List<BaseHire>();
            for (int i = 0; i < companions.Count; i++)
            {
                BaseHire companion = companions[i];
                if (String.IsNullOrWhiteSpace(ValidateEligibility(companion, owner)))
                    eligible.Add(companion);
            }

            return eligible;
        }

        private static bool IsActiveMonsterHunt(BaseHire companion)
        {
            if (companion == null || companion.Deleted)
                return false;

            AIGMCompanionTrackingState state = AIGMCompanionTrackingService.GetState(companion);
            return state != null
                && state.IsActive
                && state.ActionMode == AIGMCompanionTrackingActionMode.TrackHunt
                && state.Mode == AIGMCompanionTrackingMode.Monsters;
        }

        private static string ValidateEligibility(BaseHire companion, Mobile owner)
        {
            if (owner == null || owner.Deleted || !owner.Alive)
                return "owner_missing";

            if (companion == null || companion.Deleted || !companion.Alive)
                return "companion_invalid";

            if (companion.GetOwner() != owner)
                return "owner_mismatch";

            if (owner.Map == null || companion.Map == null || companion.Map != owner.Map)
                return "different_map";

            if (companion.Map != Map.Felucca)
                return "not_felucca";

            return String.Empty;
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
    }
}
