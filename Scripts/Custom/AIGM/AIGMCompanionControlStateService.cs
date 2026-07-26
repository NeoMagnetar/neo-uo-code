using System;

using Server;
using Server.Custom.AIGM.Tasks;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionControlStateService
    {
        public static void RestoreFollowOwner(BaseHire companion, Mobile owner, string reason)
        {
            if (companion == null || companion.Deleted || owner == null || owner.Deleted)
                return;

            string operationalRejection;
            if (!AIGMOperationalControlService.BeginExplicitCommand(companion, owner, "follow_owner", out operationalRejection))
            {
                AIGMExecutionLog.Write(
                    "AIGM_COMPANION_FOLLOW_RESTORE_REJECTED companion={0} reason={1} rejection={2}",
                    Describe(companion),
                    SafeLog(reason),
                    SafeLog(operationalRejection));
                return;
            }

            string ignored;
            AIGMLegacyTravelService.Stop(companion, reason, out ignored);

            AIGMMovementLease lease;
            string rejectionReason;
            if (!AIGMMovementOwnershipService.TryAcquireOrReplace(
                companion,
                AIGMMovementLeaseOwnerType.Follow,
                owner,
                null,
                AIGMMovementOwnershipService.GetDefaultPolicy(AIGMMovementLeaseOwnerType.Follow),
                reason,
                true,
                out lease,
                out rejectionReason))
            {
                AIGMExecutionLog.Write(
                    "AIGM_COMPANION_FOLLOW_RESTORE_REJECTED companion={0} reason={1} rejection={2}",
                    Describe(companion),
                    SafeLog(reason),
                    SafeLog(rejectionReason));
                return;
            }

            companion.CantWalk = false;
            companion.Frozen = false;
            companion.Paralyzed = false;
            companion.Combatant = null;
            companion.Warmode = false;
            companion.ControlTarget = owner;
            companion.ControlOrder = OrderType.Follow;
            companion.Home = owner.Location;
            companion.RangeHome = 24;

            string smartResponse;
            AIGMSmartMovementService.StartFollow(companion, owner, owner, out smartResponse);

            AIGMCompanionModeService.SetMode(companion, AIGMCompanionMode.FollowOwner, reason);

            AIGMExecutionLog.Write(
                "AIGM_COMPANION_FOLLOW_RESTORE companion={0} reason={1} order={2} cantWalk={3} home={4} rangeHome={5} ownerDist={6}",
                Describe(companion),
                SafeLog(reason),
                companion.ControlOrder,
                companion.CantWalk,
                FormatPoint(companion.Home),
                companion.RangeHome,
                GetOwnerDistance(companion, owner));
        }

        public static void RestoreSoftHold(BaseHire companion, string reason)
        {
            if (companion == null || companion.Deleted)
                return;

            Mobile requester = companion.GetOwner();
            AIGMMovementLease lease;
            string rejectionReason;
            if (!AIGMMovementOwnershipService.TryAcquireOrReplace(
                companion,
                AIGMMovementLeaseOwnerType.Stop,
                requester,
                null,
                AIGMMovementOwnershipService.GetDefaultPolicy(AIGMMovementLeaseOwnerType.Stop),
                reason,
                true,
                out lease,
                out rejectionReason))
            {
                AIGMExecutionLog.Write(
                    "AIGM_COMPANION_SOFT_HOLD_REJECTED companion={0} reason={1} rejection={2}",
                    Describe(companion),
                    SafeLog(reason),
                    SafeLog(rejectionReason));
                return;
            }

            companion.CantWalk = false;
            companion.Combatant = null;
            companion.Warmode = false;
            companion.ControlTarget = null;
            companion.ControlOrder = OrderType.Stay;
            companion.Home = companion.Location;
            companion.RangeHome = 8;
        }

        private static int GetOwnerDistance(BaseHire companion, Mobile owner)
        {
            if (companion == null || owner == null || companion.Map == null || owner.Map == null || companion.Map != owner.Map)
                return -1;

            int dx = companion.X - owner.X;
            int dy = companion.Y - owner.Y;
            return (int)Math.Round(Math.Sqrt((dx * dx) + (dy * dy)));
        }

        private static string Describe(Mobile mobile)
        {
            if (mobile == null)
                return "none";

            return String.Format("{0}[0x{1:X8}]", mobile.Name ?? mobile.GetType().Name, mobile.Serial.Value);
        }

        private static string FormatPoint(Point3D point)
        {
            return String.Format("{0},{1},{2}", point.X, point.Y, point.Z);
        }

        private static string SafeLog(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return String.Empty;

            return value.Replace('"', '\'').Replace('\r', ' ').Replace('\n', ' ');
        }
    }
}
