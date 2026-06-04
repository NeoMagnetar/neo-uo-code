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
                    AIGMCompanionTrackingCycle.SuspendPursuitAndTravel(companion);
                    AIGMCompanionStateAccess.ClearHoldPosition(companion);
                    return UMGMovementRouter.RouteIntent(companion, new UMGMovementIntent
                    {
                        Kind = UMGMovementIntentKind.ReturnToPlayer,
                        Requester = speaker,
                        Reason = "travel_lane_removed"
                    }, out response);
                case AIGMCompanionIntentKind.StopTravel:
                {
                    AIGMCompanionTravelController.ClearTravelState(companion, false, true, "Travel lane removed");
                    UMGMovementRouter.RecordIntent(companion, UMGMovementIntent.Create(UMGMovementIntentKind.Idle));
                    response = "I am not using destination travel anymore.";
                    return true;
                }
                case AIGMCompanionIntentKind.ReportTravelStatus:
                    response = "I am not using destination travel anymore. I can follow, hold, come, guard, and make short pursuit moves.";
                    return true;
                case AIGMCompanionIntentKind.ReturnHome:
                    AIGMCompanionTrackingCycle.SuspendPursuitAndTravel(companion);
                    AIGMCompanionStateAccess.ClearHoldPosition(companion);
                    return UMGMovementRouter.RouteIntent(companion, new UMGMovementIntent
                    {
                        Kind = UMGMovementIntentKind.ReturnToPlayer,
                        Requester = speaker,
                        Reason = "return_home_as_come"
                    }, out response);
                case AIGMCompanionIntentKind.FollowCompanion:
                    return TryFollowCompanion(companion, intent != null ? intent.DestinationName : null, out response);
                case AIGMCompanionIntentKind.GreetCompanion:
                    return TryGreetCompanion(companion, intent != null ? intent.DestinationName : null, out response);
                case AIGMCompanionIntentKind.FollowOwner:
                    return UMGMovementRouter.RouteIntent(companion, new UMGMovementIntent
                    {
                        Kind = UMGMovementIntentKind.FollowPlayer,
                        Requester = speaker
                    }, out response);
                case AIGMCompanionIntentKind.Stay:
                    return UMGMovementRouter.RouteIntent(companion, new UMGMovementIntent
                    {
                        Kind = UMGMovementIntentKind.HoldPosition,
                        Requester = speaker
                    }, out response);
                case AIGMCompanionIntentKind.Come:
                    return UMGMovementRouter.RouteIntent(companion, new UMGMovementIntent
                    {
                        Kind = UMGMovementIntentKind.FollowPlayer,
                        Requester = speaker
                    }, out response);
                case AIGMCompanionIntentKind.GuardOwner:
                    return UMGMovementRouter.RouteIntent(companion, new UMGMovementIntent
                    {
                        Kind = UMGMovementIntentKind.GuardTarget,
                        Requester = speaker,
                        TargetMobile = speaker
                    }, out response);
                case AIGMCompanionIntentKind.StopCombat:
                    IssueStopCombatOrder(companion);
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
                case AIGMCompanionIntentKind.ReportThreats:
                    return TryReportThreats(companion, out response);
                case AIGMCompanionIntentKind.ShareAwareness:
                    return AIGMCompanionAwarenessBus.ShareLatestThreatSummary(companion, out response);
                case AIGMCompanionIntentKind.StartTrackingCycle:
                    return AIGMCompanionTrackingCycle.Start(companion, out response);
                case AIGMCompanionIntentKind.StopTrackingCycle:
                    return AIGMCompanionTrackingCycle.Stop(companion, out response);
                case AIGMCompanionIntentKind.ReportTrackingStatus:
                    return AIGMCompanionTrackingCycle.ReportStatus(companion, out response);
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

            bool underAttack = IsUnderAttack(companion);
            Mobile directAttacker = companion.Combatant as Mobile;
            DateTime nextSupportActionUtc = AIGMCompanionStateAccess.GetNextSupportActionUtc(companion);
            bool supportReady = DateTime.UtcNow >= nextSupportActionUtc;
            int criticalSelfHealThreshold = Math.Max(35, companion.HitsMax / 2);
            int sustainedSelfHealThreshold = Math.Max(45, (int)(companion.HitsMax * 0.75));

            if (underAttack && directAttacker != null && !directAttacker.Deleted && directAttacker.Alive && directAttacker.Map == companion.Map)
            {
                IssueAttackOrder(companion, directAttacker);

                if (!companion.InRange(directAttacker, 1))
                {
                    AIGMCompanionTrackingEntry attackerEntry = new AIGMCompanionTrackingEntry();
                    attackerEntry.TargetSerial = directAttacker.Serial.Value;
                    attackerEntry.Name = directAttacker.Name ?? directAttacker.GetType().Name;
                    attackerEntry.TypeName = directAttacker.GetType().Name;
                    attackerEntry.Category = AIGMTrackingCategory.Monsters;
                    attackerEntry.Distance = (int)Math.Round(companion.GetDistanceToSqrt(directAttacker));
                    attackerEntry.DirectionApprox = companion.GetDirectionTo(directAttacker).ToString();
                    attackerEntry.MapName = directAttacker.Map != null ? directAttacker.Map.Name : String.Empty;
                    attackerEntry.X = directAttacker.X;
                    attackerEntry.Y = directAttacker.Y;
                    attackerEntry.Z = directAttacker.Z;
                    attackerEntry.HiddenKnown = directAttacker.Hidden;
                    attackerEntry.IsAlive = directAttacker.Alive;
                    attackerEntry.ThreatHint = AIGMCompanionThreatClassifier.Classify(companion, directAttacker, AIGMTrackingCategory.Monsters, attackerEntry.Distance);
                    attackerEntry.TimestampUtc = DateTime.UtcNow;
                    AIGMCompanionTravelController.StartTrackedPursuit(companion, attackerEntry);
                }

                if (supportReady && companion.Hits < sustainedSelfHealThreshold)
                {
                    string ignored;
                    if (AIGMCompanionSkillExecutor.TryUseBandages(companion, companion, out ignored))
                        return true;
                }

                return true;
            }

            if (!underAttack && supportReady && companion.Hits < sustainedSelfHealThreshold)
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

            if (underAttack && supportReady && companion.Hits < criticalSelfHealThreshold)
            {
                string ignored;
                if (AIGMCompanionSkillExecutor.TryUseBandages(companion, companion, out ignored))
                    return true;
            }

            if (supportReady && companion.Hits < sustainedSelfHealThreshold)
            {
                string ignored;
                if (AIGMCompanionSkillExecutor.TryHealTarget(companion, companion, true, out ignored))
                    return true;
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

            BaseHire targetCompanion = FindLinkedCompanionByName(companion, companionName);
            if (targetCompanion == null)
            {
                response = "I cannot find that companion nearby.";
                return false;
            }

            if (!companion.InRange(targetCompanion, 20))
            {
                response = "I cannot find that companion nearby.";
                return false;
            }

            UMGMovementIntent followMi = UMGMovementIntent.Create(UMGMovementIntentKind.FollowPlayer);
            followMi.Requester = targetCompanion;
            followMi.TargetMobile = targetCompanion;
            if (!UMGMovementRouter.RouteIntent(companion, followMi, out response))
                return false;
            response = "I will follow " + targetCompanion.Name + ".";
            return true;
        }

        private static bool TryGreetCompanion(BaseHire companion, string companionName, out string response)
        {
            response = null;
            if (companion == null || companion.Deleted || companion.Map == null || String.IsNullOrWhiteSpace(companionName))
            {
                response = "I cannot greet that companion right now.";
                return false;
            }

            BaseHire targetCompanion = FindLinkedCompanionByName(companion, companionName);
            if (targetCompanion == null)
            {
                response = "I cannot find that companion.";
                return false;
            }

            companion.Say("Greetings, " + targetCompanion.Name + ".");
            response = "I have greeted " + targetCompanion.Name + ".";
            return true;
        }

        private static BaseHire FindLinkedCompanionByName(BaseHire companion, string companionName)
        {
            if (companion == null || companion.Map == null || String.IsNullOrWhiteSpace(companionName))
                return null;

            Mobile owner = companion.GetOwner();
            if (owner == null)
                return null;

            foreach (Mobile mobile in World.Mobiles.Values)
            {
                BaseHire ally = mobile as BaseHire;
                if (ally == null || ally == companion || ally.Deleted)
                    continue;

                if (ally.Map != companion.Map)
                    continue;

                if (ally.GetOwner() != owner)
                    continue;

                string allyName = ally.Name == null ? String.Empty : ally.Name.Trim();
                if (allyName.Equals(companionName, StringComparison.OrdinalIgnoreCase))
                    return ally;
            }

            return null;
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

            AIGMCompanionTrackingCycle.SuspendPursuitAndTravel(companion);
            AIGMCompanionStateAccess.ClearHoldPosition(companion);
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

        private static bool IsUnderAttack(BaseHire companion)
        {
            if (companion == null || companion.Deleted)
                return false;

            Mobile combatant = companion.Combatant as Mobile;
            if (combatant != null && !combatant.Deleted && combatant.Alive)
                return true;

            Mobile owner = companion.GetOwner();
            Mobile ownerCombatant = owner != null ? owner.Combatant as Mobile : null;
            if (ownerCombatant != null && !ownerCombatant.Deleted && ownerCombatant.Alive && owner.InRange(companion, 10))
                return true;

            return false;
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

        private static void IssueGuardOrder(BaseHire companion, Mobile target)
        {
            companion.CantWalk = false;
            companion.Combatant = null;
            companion.ControlTarget = target;
            companion.ControlOrder = OrderType.Guard;
        }

        private static void IssueAttackOrder(BaseHire companion, Mobile target)
        {
            if (companion == null || companion.Deleted || target == null || target.Deleted || !target.Alive)
                return;

            if (target == companion || target == companion.GetOwner())
                return;

            if (target.Map != companion.Map)
                return;

            if (!companion.CanBeHarmful(target, false))
                return;

            companion.CantWalk = false;
            companion.Warmode = true;
            companion.ControlTarget = target;
            companion.Combatant = target;
            companion.ControlOrder = OrderType.Attack;

            if (!companion.InRange(target, 1))
                companion.CurrentSpeed = companion.ActiveSpeed;
        }

        private static void IssueStopCombatOrder(BaseHire companion)
        {
            companion.Combatant = null;
            companion.ControlTarget = null;
            companion.ControlOrder = OrderType.Stay;
        }
    }
}
