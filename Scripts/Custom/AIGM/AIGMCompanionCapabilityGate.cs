using System;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionCapabilityGate
    {
        public static AIGMCompanionCapabilityDecision Decide(AIGMCompanionCapabilityRequest request)
        {
            AIGMCompanionCapabilityDecision decision = new AIGMCompanionCapabilityDecision();
            decision.Capability = request != null ? request.Capability : AIGMCompanionCapabilityKind.None;

            switch (decision.Capability)
            {
                case AIGMCompanionCapabilityKind.Follow:
                case AIGMCompanionCapabilityKind.Stay:
                case AIGMCompanionCapabilityKind.Guard:
                    decision.Allowed = true;
                    decision.Deferred = false;
                    decision.RequiresFutureExecutor = false;
                    decision.Reason = "live_local_shell";
                    break;
                case AIGMCompanionCapabilityKind.ScanReadOnly:
                case AIGMCompanionCapabilityKind.ReportThreatsReadOnly:
                case AIGMCompanionCapabilityKind.ShareAwarenessReadOnly:
                case AIGMCompanionCapabilityKind.ReportTrackingStatus:
                case AIGMCompanionCapabilityKind.TravelReadOnly:
                    decision.Allowed = true;
                    decision.Deferred = false;
                    decision.RequiresFutureExecutor = false;
                    decision.Reason = "read_only_live";
                    break;
                case AIGMCompanionCapabilityKind.TrackReadOnly:
                case AIGMCompanionCapabilityKind.TrackingCycle:
                    decision.Allowed = true;
                    decision.Deferred = false;
                    decision.RequiresFutureExecutor = false;
                    decision.Reason = "tracking_read_only_live";
                    break;
                case AIGMCompanionCapabilityKind.MonsterHunt:
                case AIGMCompanionCapabilityKind.MonsterHuntStop:
                case AIGMCompanionCapabilityKind.MonsterHuntStatus:
                    decision.Allowed = true;
                    decision.Deferred = false;
                    decision.RequiresFutureExecutor = false;
                    decision.Reason = "phase58a_monster_hunt_live";
                    break;
                case AIGMCompanionCapabilityKind.None:
                    decision.Allowed = false;
                    decision.Deferred = false;
                    decision.RequiresFutureExecutor = false;
                    decision.Reason = "no_capability";
                    break;
                default:
                    decision.Allowed = false;
                    decision.Deferred = true;
                    decision.RequiresFutureExecutor = true;
                    decision.Reason = "future_executor_required";
                    decision.VisibleResponse = "That action lane is still gated for now.";
                    break;
            }

            return decision;
        }
    }
}
