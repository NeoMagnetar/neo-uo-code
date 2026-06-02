using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionActionExecutor
    {
        public static bool TryExecuteIntent(BaseHire companion, Mobile speaker, AIGMCompanionIntent intent, out string response)
        {
            response = null;

            if (companion == null || speaker == null || intent == null || String.IsNullOrWhiteSpace(intent.Kind))
                return false;

            switch (intent.Kind)
            {
                case AIGMCompanionIntentKind.TravelToDestination:
                    return AIGMCompanionTravelController.StartTravel(companion, intent != null ? intent.DestinationName : null, out response);
                case AIGMCompanionIntentKind.StopTravel:
                    return AIGMCompanionTravelController.StopTravel(companion, null, out response);
                case AIGMCompanionIntentKind.ReportTravelStatus:
                    return AIGMCompanionTravelController.ReportStatus(companion, out response);
                case AIGMCompanionIntentKind.ReturnHome:
                    return AIGMCompanionTravelController.StartTravel(companion, "Britain", out response);
                case AIGMCompanionIntentKind.FollowCompanion:
                    return TryFollowCompanion(companion, intent != null ? intent.DestinationName : null, out response);
                case AIGMCompanionIntentKind.FollowOwner:
                    IssueFollowOrder(companion, speaker);
                    response = "I am with you.";
                    return true;
                case AIGMCompanionIntentKind.Stay:
                    IssueFullStopOrder(companion, speaker);
                    response = "I will hold here.";
                    return true;
                case AIGMCompanionIntentKind.Come:
                    IssueComeOrder(companion, speaker);
                    response = "On my way.";
                    return true;
                case AIGMCompanionIntentKind.GuardOwner:
                    AIGMCompanionStateAccess.SetGuardOwnerMode(companion, true);
                    IssueGuardOrder(companion, speaker);
                    response = "I will guard you.";
                    return true;
                case AIGMCompanionIntentKind.StopCombat:
                    IssueFullStopOrder(companion, speaker);
                    response = "I am disengaging.";
                    return true;
                case AIGMCompanionIntentKind.AttackTarget:
                    return TryAttackTarget(companion, speaker, intent, out response);
                case AIGMCompanionIntentKind.BandageSelf:
                    return AIGMCompanionSkillExecutor.TryUseBandages(companion, companion, out response);
                case AIGMCompanionIntentKind.HealSelf:
                    return AIGMCompanionSkillExecutor.TryUseMageryHeal(companion, companion, out response);
                case AIGMCompanionIntentKind.BandageOwner:
                    if (speaker != companion.GetOwner())
                    {
                        response = "I only tend my bonded companion that way.";
                        return false;
                    }

                    return AIGMCompanionSkillExecutor.TryUseBandages(companion, speaker, out response);
                case AIGMCompanionIntentKind.HealOwner:
                    if (speaker != companion.GetOwner())
                    {
                        response = "I only tend my bonded companion that way.";
                        return false;
                    }

                    return AIGMCompanionSkillExecutor.TryUseMageryHeal(companion, speaker, out response);
                case AIGMCompanionIntentKind.CureSelf:
                    return AIGMCompanionSkillExecutor.TryUseCurePotion(companion, companion, out response);
                case AIGMCompanionIntentKind.CureOwner:
                    if (speaker != companion.GetOwner())
                    {
                        response = "I only tend my bonded companion that way.";
                        return false;
                    }

                    return AIGMCompanionSkillExecutor.TryUseMageryCure(companion, speaker, out response);
                case AIGMCompanionIntentKind.ReportLocation:
                    response = AIGMCompanionLocationService.FormatReport(AIGMCompanionLocationService.Capture(companion));
                    return true;
                case AIGMCompanionIntentKind.ScanArea:
                    return TryReportScanArea(companion, out response);
                case AIGMCompanionIntentKind.TrackAnimals:
                    return TryReportTracking(companion, AIGMTrackingCategory.Animals, out response);
                case AIGMCompanionIntentKind.TrackMonsters:
                    return TryReportTracking(companion, AIGMTrackingCategory.Monsters, out response);
                case AIGMCompanionIntentKind.TrackHumanNPCs:
                    return TryReportTracking(companion, AIGMTrackingCategory.HumanNPCs, out response);
                case AIGMCompanionIntentKind.TrackPlayers:
                    return TryReportTracking(companion, AIGMTrackingCategory.Players, out response);
                case AIGMCompanionIntentKind.StartTracking:
                    return AIGMCompanionTrackingController.StartTracking(companion, out response);
                case AIGMCompanionIntentKind.StopTracking:
                    return AIGMCompanionTrackingController.StopTracking(companion, out response);
                case AIGMCompanionIntentKind.ReportTrackingStatus:
                    return AIGMCompanionTrackingController.ReportStatus(companion, out response);
                case AIGMCompanionIntentKind.ReportThreats:
                    return TryReportThreats(companion, out response);
                case AIGMCompanionIntentKind.ShareAwareness:
                    return AIGMCompanionAwarenessBus.ShareLatestThreatSummary(companion, out response);
                case AIGMCompanionIntentKind.UseHealingSkill:
                    return AIGMCompanionSkillExecutor.TryUseHealingSkill(companion, companion, out response);
                case AIGMCompanionIntentKind.UseBandages:
                    return AIGMCompanionSkillExecutor.TryUseBandages(companion, companion, out response);
                case AIGMCompanionIntentKind.CastHeal:
                    return AIGMCompanionSkillExecutor.TryUseMageryHeal(companion, companion, out response);
                case AIGMCompanionIntentKind.CastCure:
                    return AIGMCompanionSkillExecutor.TryUseMageryCure(companion, companion, out response);
                default:
                    return false;
            }
        }

        public static bool TryReactiveSupport(BaseHire companion)
        {
            if (companion == null || companion.Deleted || !companion.Alive)
                return false;

            if (DateTime.UtcNow < AIGMCompanionStateAccess.GetNextSupportActionUtc(companion))
                return false;

            if (companion.Hits < Math.Max(25, companion.HitsMax / 2))
            {
                string ignored;
                if (AIGMCompanionSkillExecutor.TryHealTarget(companion, companion, true, out ignored))
                    return true;
            }

            Mobile owner = companion.GetOwner();
            Mobile attacker = owner != null ? owner.Combatant as Mobile : null;
            if (AIGMCompanionStateAccess.GetGuardOwnerMode(companion) && owner != null && attacker != null && owner.InRange(companion, 10))
            {
                if (!attacker.Deleted && attacker.Alive)
                {
                    IssueAttackOrder(companion, attacker);
                    AIGMCompanionStateAccess.SetNextSupportActionUtc(companion, DateTime.UtcNow + TimeSpan.FromSeconds(2.0));
                    return true;
                }
            }

            return false;
        }

        private static bool TryFollowCompanion(BaseHire companion, string companionName, out string response)
        {
            response = null;
            if (companion == null || companion.Deleted || companion.Map == null || String.IsNullOrWhiteSpace(companionName))
            {
                response = "I cannot follow that companion right now.";
                return false;
            }

            IPooledEnumerable eable = companion.GetMobilesInRange(16);
            foreach (Mobile mobile in eable)
            {
                BaseHire ally = mobile as BaseHire;
                if (ally == null || ally == companion || ally.Deleted)
                    continue;

                string name = ally.Name == null ? String.Empty : ally.Name.ToLowerInvariant();
                if (name == companionName.ToLowerInvariant())
                {
                    IssueFollowOrder(companion, ally);
                    response = "I will follow " + ally.Name + ".";
                    eable.Free();
                    return true;
                }
            }
            eable.Free();

            response = "I cannot find that companion nearby.";
            return false;
        }

        private static bool TryAttackTarget(BaseHire companion, Mobile speaker, AIGMCompanionIntent intent, out string response)
        {
            response = null;

            Mobile target = null;
            if (intent != null && intent.HasTarget)
                target = World.FindMobile(intent.TargetSerial);

            if (target == null || target.Deleted || !target.Alive)
            {
                response = "I do not have a valid target.";
                return false;
            }

            AIGMCompanionStateAccess.SetGuardOwnerMode(companion, false);
            IssueAttackOrder(companion, target);
            response = "Attacking now.";
            return true;
        }

        private static bool TryReportScanArea(BaseHire companion, out string response)
        {
            response = null;
            List<AIGMCompanionTrackingSweep> sweeps = AIGMCompanionTrackingSensor.SweepAll(companion);
            int total = 0;
            int threats = 0;

            for (int i = 0; i < sweeps.Count; i++)
            {
                AIGMCompanionTrackingSweep sweep = sweeps[i];
                if (sweep == null)
                    continue;

                AIGMCompanionPerceptionBuffer.RecordSweep(companion, sweep);
                total += sweep.Entries != null ? sweep.Entries.Count : 0;

                if (sweep.Entries != null)
                {
                    for (int j = 0; j < sweep.Entries.Count; j++)
                    {
                        AIGMCompanionTrackingEntry entry = sweep.Entries[j];
                        if (entry != null && (entry.ThreatHint == AIGMCompanionThreatLevel.PotentialThreat || entry.ThreatHint == AIGMCompanionThreatLevel.ImmediateThreat))
                            threats++;
                    }
                }
            }

            response = String.Format("I have swept the area. I marked {0} nearby presences, with {1} notable threats.", total, threats);
            return true;
        }

        private static bool TryReportTracking(BaseHire companion, AIGMTrackingCategory category, out string response)
        {
            response = null;
            AIGMCompanionTrackingSweep sweep = AIGMCompanionTrackingSensor.Sweep(companion, category);
            if (sweep == null)
            {
                response = "I cannot get a clear read just now.";
                return false;
            }

            AIGMCompanionPerceptionBuffer.RecordSweep(companion, sweep);
            if (sweep.Entries == null || sweep.Entries.Count == 0)
            {
                response = String.Format("I find no clear {0} nearby.", category.ToString().ToLowerInvariant());
                return true;
            }

            int count = Math.Min(3, sweep.Entries.Count);
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("I marked ");
            sb.Append(sweep.Entries.Count);
            sb.Append(' ');
            sb.Append(category.ToString().ToLowerInvariant());
            sb.Append(" nearby. Closest: ");

            for (int i = 0; i < count; i++)
            {
                AIGMCompanionTrackingEntry entry = sweep.Entries[i];
                if (i > 0)
                    sb.Append("; ");

                sb.Append(entry.Name);
                sb.Append(" at ");
                sb.Append(entry.Distance);
                sb.Append(" tiles");
            }

            response = sb.ToString();
            return true;
        }

        private static bool TryReportThreats(BaseHire companion, out string response)
        {
            response = null;
            AIGMCompanionPerceptionState state = AIGMCompanionPerceptionBuffer.Get(companion);
            if (state == null || state.LastSightingsBySerial.Count == 0)
            {
                List<AIGMCompanionTrackingSweep> sweeps = AIGMCompanionTrackingSensor.SweepAll(companion);
                for (int i = 0; i < sweeps.Count; i++)
                {
                    if (sweeps[i] != null)
                        AIGMCompanionPerceptionBuffer.RecordSweep(companion, sweeps[i]);
                }

                state = AIGMCompanionPerceptionBuffer.Get(companion);
                if (state == null || state.LastSightingsBySerial.Count == 0)
                {
                    response = "I have no immediate threat report.";
                    return true;
                }
            }

            List<AIGMCompanionTrackingEntry> threats = new List<AIGMCompanionTrackingEntry>();
            foreach (KeyValuePair<int, AIGMCompanionTrackingEntry> pair in state.LastSightingsBySerial)
            {
                AIGMCompanionTrackingEntry entry = pair.Value;
                if (entry == null)
                    continue;

                if (entry.ThreatHint == AIGMCompanionThreatLevel.PotentialThreat || entry.ThreatHint == AIGMCompanionThreatLevel.ImmediateThreat)
                    threats.Add(entry);
            }

            threats.Sort(delegate(AIGMCompanionTrackingEntry x, AIGMCompanionTrackingEntry y)
            {
                return x.Distance.CompareTo(y.Distance);
            });

            if (threats.Count == 0)
            {
                response = "I have no immediate threat report.";
                return true;
            }

            int count = Math.Min(3, threats.Count);
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("Threats nearest us: ");
            for (int i = 0; i < count; i++)
            {
                AIGMCompanionTrackingEntry entry = threats[i];
                if (i > 0)
                    sb.Append("; ");

                sb.Append(entry.Name);
                sb.Append(" at ");
                sb.Append(entry.Distance);
                sb.Append(" tiles");
            }

            response = sb.ToString();
            return true;
        }

        private static void IssueFollowOrder(BaseHire companion, Mobile target)
        {
            companion.CantWalk = false;
            companion.Combatant = null;
            companion.ControlTarget = target;
            companion.ControlOrder = OrderType.Follow;
        }

        private static void IssueComeOrder(BaseHire companion, Mobile target)
        {
            companion.CantWalk = false;
            companion.Combatant = null;
            companion.ControlTarget = target;
            companion.ControlOrder = OrderType.Come;
        }

        private static void IssueStayOrder(BaseHire companion)
        {
            companion.Combatant = null;
            companion.ControlTarget = null;
            companion.ControlOrder = OrderType.Stay;
        }

        private static void IssueFullStopOrder(BaseHire companion, Mobile speaker)
        {
            if (companion == null)
                return;

            AIGMCompanionTrackingController.NotifyHoldPosition(companion);

            string ignored;
            Mobile owner = speaker ?? companion.GetOwner();
            if (owner != null)
                AIGMCompanionTravelController.StopTravel(companion, "I am holding here.", out ignored);

            IssueStayOrder(companion);
            companion.CantWalk = true;
            companion.Home = companion.Location;
            companion.RangeHome = 0;
        }

        private static void IssueGuardOrder(BaseHire companion, Mobile target)
        {
            companion.CantWalk = false;
            companion.Combatant = null;
            companion.ControlTarget = target;
            companion.ControlOrder = OrderType.Guard;
        }

        public static void IssueAttackOrder(BaseHire companion, Mobile target)
        {
            companion.CantWalk = false;
            companion.ControlTarget = target;
            companion.Combatant = target;
            companion.ControlOrder = OrderType.Attack;
        }

        private static void IssueStopCombatOrder(BaseHire companion)
        {
            companion.Combatant = null;
            companion.ControlTarget = null;
            companion.ControlOrder = OrderType.Stay;
        }
    }
}
