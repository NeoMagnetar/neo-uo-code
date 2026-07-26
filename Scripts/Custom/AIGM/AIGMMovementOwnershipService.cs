using System;
using System.Collections.Generic;

using Server;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMMovementOwnershipService
    {
        private static readonly object SyncRoot = new object();
        private static readonly Dictionary<Serial, AIGMMovementLease> ActiveLeases = new Dictionary<Serial, AIGMMovementLease>();

        public static AIGMMovementLease GetCurrentLease(BaseHire companion)
        {
            if (companion == null)
                return null;

            lock (SyncRoot)
            {
                AIGMMovementLease lease;
                return ActiveLeases.TryGetValue(companion.Serial, out lease) ? lease : null;
            }
        }

        public static bool IsCurrentOwner(BaseHire companion, AIGMMovementLeaseOwnerType ownerType)
        {
            if (companion == null)
                return false;

            lock (SyncRoot)
            {
                AIGMMovementLease lease;
                return ActiveLeases.TryGetValue(companion.Serial, out lease)
                    && lease != null
                    && lease.TerminalStatus == AIGMMovementLeaseTerminalStatus.Active
                    && lease.OwnerType == ownerType;
            }
        }

        public static bool TryAcquireOrReplace(
            BaseHire companion,
            AIGMMovementLeaseOwnerType ownerType,
            Mobile requester,
            string routeContextId,
            AIGMMovementInterruptionPolicy interruptionPolicy,
            string reason,
            bool explicitOwnerCommand,
            out AIGMMovementLease lease,
            out string rejectionReason)
        {
            lease = null;
            rejectionReason = null;

            if (companion == null || companion.Deleted)
            {
                rejectionReason = "companion_invalid";
                return false;
            }

            requester = requester ?? companion.GetOwner();
            if (requester == null || requester.Deleted)
            {
                rejectionReason = "requester_missing";
                return false;
            }

            DateTime now = DateTime.UtcNow;

            lock (SyncRoot)
            {
                AIGMMovementLease current;
                if (!ActiveLeases.TryGetValue(companion.Serial, out current) || current == null || current.TerminalStatus != AIGMMovementLeaseTerminalStatus.Active)
                {
                    lease = CreateLease(companion, ownerType, requester, routeContextId, interruptionPolicy, reason, now);
                    ActiveLeases[companion.Serial] = lease;
                    LogAcquire(companion, lease, "none", reason);
                    return true;
                }

                if (current.OwnerType == ownerType && current.RequesterSerial == requester.Serial)
                {
                    current.LastHeartbeatUtc = now;
                    current.RouteContextId = NormalizeRouteContext(routeContextId, current.RouteContextId);
                    current.LastReason = reason;
                    lease = current;
                    LogHeartbeat(companion, current, reason);
                    return true;
                }

                AIGMMovementLeaseTerminalStatus terminalStatus;
                if (!CanReplace(current, ownerType, explicitOwnerCommand, out terminalStatus, out rejectionReason))
                {
                    LogRejected(companion, current, ownerType, requester, routeContextId, reason, rejectionReason);
                    return false;
                }

                lease = CreateLease(companion, ownerType, requester, routeContextId, interruptionPolicy, reason, now);
                current.TerminalStatus = terminalStatus;
                current.LastReason = reason;
                ActiveLeases[companion.Serial] = lease;
                LogReplace(companion, current, lease, reason, terminalStatus);
                return true;
            }
        }

        public static bool Heartbeat(BaseHire companion, AIGMMovementLeaseOwnerType ownerType, Mobile requester, string routeContextId, string reason)
        {
            if (companion == null || companion.Deleted)
                return false;

            lock (SyncRoot)
            {
                AIGMMovementLease current;
                if (!ActiveLeases.TryGetValue(companion.Serial, out current) || current == null || current.TerminalStatus != AIGMMovementLeaseTerminalStatus.Active || current.OwnerType != ownerType)
                    return false;

                requester = requester ?? current.Requester ?? companion.GetOwner();
                if (requester != null)
                {
                    current.Requester = requester;
                    current.RequesterSerial = requester.Serial;
                }

                current.LastHeartbeatUtc = DateTime.UtcNow;
                current.RouteContextId = NormalizeRouteContext(routeContextId, current.RouteContextId);
                current.LastReason = reason;
                LogHeartbeat(companion, current, reason);
                return true;
            }
        }

        public static bool ReleaseIfOwned(BaseHire companion, AIGMMovementLeaseOwnerType ownerType, Mobile requester, string routeContextId, string reason, AIGMMovementLeaseTerminalStatus terminalStatus)
        {
            if (companion == null)
                return false;

            lock (SyncRoot)
            {
                AIGMMovementLease current;
                if (!ActiveLeases.TryGetValue(companion.Serial, out current) || current == null || current.TerminalStatus != AIGMMovementLeaseTerminalStatus.Active || current.OwnerType != ownerType)
                    return false;

                if (requester != null)
                {
                    current.Requester = requester;
                    current.RequesterSerial = requester.Serial;
                }

                current.RouteContextId = NormalizeRouteContext(routeContextId, current.RouteContextId);
                current.TerminalStatus = terminalStatus;
                current.LastReason = reason;
                ActiveLeases.Remove(companion.Serial);
                LogRelease(companion, current, reason, terminalStatus);
                return true;
            }
        }

        public static AIGMMovementInterruptionPolicy GetDefaultPolicy(AIGMMovementLeaseOwnerType ownerType)
        {
            switch (ownerType)
            {
                case AIGMMovementLeaseOwnerType.Stop:
                    return AIGMMovementInterruptionPolicy.HardPreempt;
                case AIGMMovementLeaseOwnerType.Follow:
                case AIGMMovementLeaseOwnerType.Hunt:
                case AIGMMovementLeaseOwnerType.Travel:
                    return AIGMMovementInterruptionPolicy.ExplicitOwnerCommandRequired;
                default:
                    return AIGMMovementInterruptionPolicy.None;
            }
        }

        private static AIGMMovementLease CreateLease(BaseHire companion, AIGMMovementLeaseOwnerType ownerType, Mobile requester, string routeContextId, AIGMMovementInterruptionPolicy interruptionPolicy, string reason, DateTime now)
        {
            AIGMMovementLease lease = new AIGMMovementLease();
            lease.CompanionSerial = companion.Serial;
            lease.LeaseId = Guid.NewGuid().ToString("N");
            lease.OwnerType = ownerType;
            lease.Requester = requester;
            lease.RequesterSerial = requester != null ? requester.Serial : Serial.MinusOne;
            lease.AcquiredUtc = now;
            lease.LastHeartbeatUtc = now;
            lease.RouteContextId = String.IsNullOrWhiteSpace(routeContextId) ? null : routeContextId.Trim();
            lease.InterruptionPolicy = interruptionPolicy;
            lease.TerminalStatus = AIGMMovementLeaseTerminalStatus.Active;
            lease.LastReason = reason;
            return lease;
        }

        private static bool CanReplace(AIGMMovementLease current, AIGMMovementLeaseOwnerType incomingOwnerType, bool explicitOwnerCommand, out AIGMMovementLeaseTerminalStatus terminalStatus, out string rejectionReason)
        {
            terminalStatus = AIGMMovementLeaseTerminalStatus.Released;
            rejectionReason = null;

            switch (current.OwnerType)
            {
                case AIGMMovementLeaseOwnerType.Travel:
                    if (incomingOwnerType == AIGMMovementLeaseOwnerType.Stop)
                    {
                        terminalStatus = AIGMMovementLeaseTerminalStatus.InterruptedStop;
                        return true;
                    }

                    if (incomingOwnerType == AIGMMovementLeaseOwnerType.Follow)
                    {
                        terminalStatus = AIGMMovementLeaseTerminalStatus.InterruptedFollow;
                        return true;
                    }

                    if (incomingOwnerType == AIGMMovementLeaseOwnerType.Hunt)
                    {
                        terminalStatus = AIGMMovementLeaseTerminalStatus.InterruptedHunt;
                        return true;
                    }

                    if (incomingOwnerType == AIGMMovementLeaseOwnerType.Travel && explicitOwnerCommand)
                    {
                        terminalStatus = AIGMMovementLeaseTerminalStatus.Released;
                        return true;
                    }

                    rejectionReason = "travel_replace_not_allowed";
                    return false;

                case AIGMMovementLeaseOwnerType.Hunt:
                    if (incomingOwnerType == AIGMMovementLeaseOwnerType.Stop)
                    {
                        terminalStatus = AIGMMovementLeaseTerminalStatus.InterruptedStop;
                        return true;
                    }

                    if (incomingOwnerType == AIGMMovementLeaseOwnerType.Follow)
                    {
                        terminalStatus = AIGMMovementLeaseTerminalStatus.InterruptedFollow;
                        return true;
                    }

                    rejectionReason = "hunt_replace_rejected";
                    return false;

                case AIGMMovementLeaseOwnerType.Follow:
                    if (incomingOwnerType == AIGMMovementLeaseOwnerType.Stop)
                    {
                        terminalStatus = AIGMMovementLeaseTerminalStatus.InterruptedStop;
                        return true;
                    }

                    if (incomingOwnerType == AIGMMovementLeaseOwnerType.Travel && explicitOwnerCommand)
                    {
                        terminalStatus = AIGMMovementLeaseTerminalStatus.Released;
                        return true;
                    }

                    rejectionReason = "follow_replace_rejected";
                    return false;

                case AIGMMovementLeaseOwnerType.Stop:
                    if (incomingOwnerType == AIGMMovementLeaseOwnerType.Follow)
                    {
                        terminalStatus = AIGMMovementLeaseTerminalStatus.Released;
                        return true;
                    }

                    if (incomingOwnerType == AIGMMovementLeaseOwnerType.Hunt)
                    {
                        terminalStatus = AIGMMovementLeaseTerminalStatus.Released;
                        return true;
                    }

                    if (incomingOwnerType == AIGMMovementLeaseOwnerType.Travel && explicitOwnerCommand)
                    {
                        terminalStatus = AIGMMovementLeaseTerminalStatus.Released;
                        return true;
                    }

                    rejectionReason = "stop_replace_rejected";
                    return false;
            }

            terminalStatus = AIGMMovementLeaseTerminalStatus.Released;
            return true;
        }

        private static string NormalizeRouteContext(string routeContextId, string fallback)
        {
            if (!String.IsNullOrWhiteSpace(routeContextId))
                return routeContextId.Trim();

            return String.IsNullOrWhiteSpace(fallback) ? null : fallback.Trim();
        }

        private static void LogAcquire(BaseHire companion, AIGMMovementLease lease, string priorOwnerType, string reason)
        {
            AIGMExecutionLog.Write(
                "AIGM_MOVEMENT_LEASE_ACQUIRE companion={0} companionSerial=0x{1:X8} leaseId={2} ownerType={3} requester={4} priorOwnerType={5} routeContextId={6} reason={7}",
                Describe(companion),
                companion != null ? companion.Serial.Value : 0,
                SafeLog(lease != null ? lease.LeaseId : null),
                lease != null ? lease.OwnerType.ToString().ToLowerInvariant() : "none",
                Describe(lease != null ? lease.Requester : null),
                SafeLog(priorOwnerType),
                SafeLog(lease != null ? lease.RouteContextId : null),
                SafeLog(reason));
        }

        private static void LogReplace(BaseHire companion, AIGMMovementLease priorLease, AIGMMovementLease newLease, string reason, AIGMMovementLeaseTerminalStatus terminalStatus)
        {
            AIGMExecutionLog.Write(
                "AIGM_MOVEMENT_LEASE_REPLACE companion={0} companionSerial=0x{1:X8} leaseId={2} ownerType={3} requester={4} priorOwnerType={5} routeContextId={6} reason={7} terminalStatus={8}",
                Describe(companion),
                companion != null ? companion.Serial.Value : 0,
                SafeLog(newLease != null ? newLease.LeaseId : null),
                newLease != null ? newLease.OwnerType.ToString().ToLowerInvariant() : "none",
                Describe(newLease != null ? newLease.Requester : null),
                priorLease != null ? priorLease.OwnerType.ToString().ToLowerInvariant() : "none",
                SafeLog(newLease != null ? newLease.RouteContextId : null),
                SafeLog(reason),
                terminalStatus.ToString().ToLowerInvariant());
        }

        private static void LogRelease(BaseHire companion, AIGMMovementLease lease, string reason, AIGMMovementLeaseTerminalStatus terminalStatus)
        {
            AIGMExecutionLog.Write(
                "AIGM_MOVEMENT_LEASE_RELEASE companion={0} companionSerial=0x{1:X8} leaseId={2} ownerType={3} requester={4} priorOwnerType={5} routeContextId={6} reason={7} terminalStatus={8}",
                Describe(companion),
                companion != null ? companion.Serial.Value : 0,
                SafeLog(lease != null ? lease.LeaseId : null),
                lease != null ? lease.OwnerType.ToString().ToLowerInvariant() : "none",
                Describe(lease != null ? lease.Requester : null),
                lease != null ? lease.OwnerType.ToString().ToLowerInvariant() : "none",
                SafeLog(lease != null ? lease.RouteContextId : null),
                SafeLog(reason),
                terminalStatus.ToString().ToLowerInvariant());
        }

        private static void LogRejected(BaseHire companion, AIGMMovementLease currentLease, AIGMMovementLeaseOwnerType requestedOwnerType, Mobile requester, string routeContextId, string reason, string rejectionReason)
        {
            AIGMExecutionLog.Write(
                "AIGM_MOVEMENT_LEASE_REJECTED companion={0} companionSerial=0x{1:X8} leaseId={2} ownerType={3} requester={4} priorOwnerType={5} routeContextId={6} reason={7} terminalStatus={8}",
                Describe(companion),
                companion != null ? companion.Serial.Value : 0,
                SafeLog(currentLease != null ? currentLease.LeaseId : null),
                requestedOwnerType.ToString().ToLowerInvariant(),
                Describe(requester),
                currentLease != null ? currentLease.OwnerType.ToString().ToLowerInvariant() : "none",
                SafeLog(routeContextId),
                SafeLog(reason),
                SafeLog(rejectionReason));
        }

        private static void LogHeartbeat(BaseHire companion, AIGMMovementLease lease, string reason)
        {
            AIGMExecutionLog.Write(
                "AIGM_MOVEMENT_LEASE_HEARTBEAT companion={0} companionSerial=0x{1:X8} leaseId={2} ownerType={3} requester={4} priorOwnerType={5} routeContextId={6} reason={7}",
                Describe(companion),
                companion != null ? companion.Serial.Value : 0,
                SafeLog(lease != null ? lease.LeaseId : null),
                lease != null ? lease.OwnerType.ToString().ToLowerInvariant() : "none",
                Describe(lease != null ? lease.Requester : null),
                lease != null ? lease.OwnerType.ToString().ToLowerInvariant() : "none",
                SafeLog(lease != null ? lease.RouteContextId : null),
                SafeLog(reason));
        }

        private static string Describe(Mobile mobile)
        {
            if (mobile == null)
                return "none";

            string name = String.IsNullOrWhiteSpace(mobile.Name) ? mobile.GetType().Name : mobile.Name;
            return String.Format("{0}[0x{1:X8}]", name, mobile.Serial.Value);
        }

        private static string SafeLog(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return "null";

            value = value.Replace('"', '\'').Replace('\r', ' ').Replace('\n', ' ');
            return value.Length > 220 ? value.Substring(0, 220) : value;
        }
    }
}
