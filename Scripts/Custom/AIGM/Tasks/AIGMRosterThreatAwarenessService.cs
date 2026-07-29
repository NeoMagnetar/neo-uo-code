using System;
using System.Collections.Generic;

using Server;
using Server.Mobiles;

namespace Server.Custom.AIGM.Tasks
{
    public static class AIGMRosterThreatAwarenessService
    {
        private static readonly TimeSpan AwarenessThrottle = TimeSpan.FromSeconds(2.0);

        public static void Pulse(IAIGMRosterTaskAgent agent, BaseCreature creature, AIGMRosterTaskState state)
        {
            if (agent == null || creature == null || state == null || creature.Map == null || creature.Map == Map.Internal)
                return;

            DateTime now = DateTime.UtcNow;
            if (state.LastAwarenessUtc != DateTime.MinValue && (now - state.LastAwarenessUtc) < AwarenessThrottle)
                return;

            state.LastAwarenessUtc = now;

            List<Mobile> threats = FindNearbyThreats(agent, creature, state);
            if (threats.Count == 0)
            {
                state.Awareness = AIGMRosterThreatAwarenessLevel.Unaware;
                state.LastThreatResponse = AIGMRosterThreatResponse.None;
                return;
            }

            Mobile nearest = threats[0];
            int nearestDistance = (int)creature.GetDistanceToSqrt(nearest);
            state.Awareness = nearestDistance <= 3
                ? AIGMRosterThreatAwarenessLevel.ImmediateThreat
                : (nearestDistance <= 8 ? AIGMRosterThreatAwarenessLevel.EnemyLocated : AIGMRosterThreatAwarenessLevel.TrailDetected);

            if (!AIGMOperationalControlService.CanOperate(creature, AIGMOperationalAction.ThreatAwareness, state.ControlEpoch, false))
            {
                state.LastThreatResponse = AIGMRosterThreatResponse.None;
                return;
            }

            AIGMRosterThreatResponse response = SelectResponse(agent, creature, state, threats, nearest);
            state.LastThreatResponse = response;

            AIGMExecutionLog.Write(
                "AIGM_THREAT_AWARENESS agent={0} awareness={1} nearest={2} distance={3} threatCount={4}",
                Describe(creature),
                state.Awareness,
                Describe(nearest),
                nearestDistance,
                threats.Count);
            AIGMExecutionLog.Write(
                "AIGM_THREAT_RESPONSE_SELECT agent={0} response={1} awareness={2} threatCount={3}",
                Describe(creature),
                response,
                state.Awareness,
                threats.Count);

            ApplyResponse(agent, creature, state, response, threats, nearest);
        }

        private static List<Mobile> FindNearbyThreats(IAIGMRosterTaskAgent agent, BaseCreature creature, AIGMRosterTaskState state)
        {
            List<Mobile> threats = new List<Mobile>();
            int range = Math.Min(22, 10 + (int)(creature.Skills[SkillName.Tracking].Value / 10.0) + (int)(creature.Skills[SkillName.DetectHidden].Value / 12.0));
            IPooledEnumerable eable = creature.GetMobilesInRange(range);
            try
            {
                foreach (Mobile mobile in eable)
                {
                    if (mobile == null || mobile.Deleted || !mobile.Alive || mobile == creature || mobile.IsStaff())
                        continue;

                    if (!IsThreat(agent, creature, state, mobile))
                        continue;

                    threats.Add(mobile);
                }
            }
            finally
            {
                eable.Free();
            }

            threats.Sort(delegate (Mobile left, Mobile right)
            {
                return ((int)creature.GetDistanceToSqrt(left)).CompareTo((int)creature.GetDistanceToSqrt(right));
            });
            return threats;
        }

        private static bool IsThreat(IAIGMRosterTaskAgent agent, BaseCreature creature, AIGMRosterTaskState state, Mobile mobile)
        {
            if (mobile == null || mobile == creature)
                return false;

            if (mobile.Combatant == creature)
                return true;

            if (mobile is IAIGMRosterTaskAgent otherAgent)
            {
                AIGMRosterFaction selfFaction = AIGMRosterFactionService.ResolveFaction(agent.RosterCharacterId);
                AIGMRosterFaction otherFaction = AIGMRosterFactionService.ResolveFaction(otherAgent.RosterCharacterId);

                if (AIGMRosterFactionService.AreEnemies(selfFaction, otherFaction)
                    || AIGMRosterFactionService.HasExplicitEnemyRelationship(creature, mobile))
                {
                    AIGMRosterTaskState otherState = AIGMRosterTaskService.GetOrCreateState(otherAgent);
                    if (otherState != null)
                    {
                        if (otherState.Mode == AIGMRosterTaskMode.Hunt || otherState.Mode == AIGMRosterTaskMode.Track)
                        {
                            if (otherState.Selector == AIGMRosterTargetSelector.Heroes && AIGMRosterFactionService.IsHero(agent.RosterCharacterId))
                                return true;

                            if (otherState.Selector == AIGMRosterTargetSelector.NamedCharacter
                                && String.Equals(otherState.TargetCanonicalId, agent.RosterCharacterId, StringComparison.OrdinalIgnoreCase))
                                return true;

                            if (otherState.Selector == AIGMRosterTargetSelector.Faction
                                && otherState.TargetFaction == selfFaction)
                                return true;
                        }
                    }

                    return true;
                }
            }

            return mobile is BaseCreature hostileCreature
                && hostileCreature.Combatant == creature;
        }

        private static AIGMRosterThreatResponse SelectResponse(
            IAIGMRosterTaskAgent agent,
            BaseCreature creature,
            AIGMRosterTaskState state,
            List<Mobile> threats,
            Mobile nearest)
        {
            string characterId = agent.RosterCharacterId ?? String.Empty;
            int allies = CountNearbyAllies(agent, creature, 12);
            int enemies = threats.Count;
            double healthPct = creature.HitsMax > 0 ? (double)creature.Hits / creature.HitsMax : 1.0;

            switch (characterId.ToLowerInvariant())
            {
                case "druss":
                    return healthPct < 0.20 && enemies > allies + 2
                        ? AIGMRosterThreatResponse.ProtectMobile
                        : AIGMRosterThreatResponse.Fight;
                case "dakeyras":
                case "dakeyras.grey_man":
                    if (healthPct < 0.35 && enemies >= 2)
                        return creature.Skills[SkillName.Hiding].Value >= 60.0
                            ? AIGMRosterThreatResponse.Hide
                            : AIGMRosterThreatResponse.Flee;
                    return enemies > allies + 1 ? AIGMRosterThreatResponse.Reposition : AIGMRosterThreatResponse.Fight;
                case "miriel":
                    return enemies > allies + 1 ? AIGMRosterThreatResponse.Reposition : AIGMRosterThreatResponse.Fight;
                case "dardalion":
                    return enemies > allies ? AIGMRosterThreatResponse.SeekAllies : AIGMRosterThreatResponse.ProtectMobile;
                case "danyal":
                    return healthPct < 0.45 || enemies > allies + 1 ? AIGMRosterThreatResponse.Regroup : AIGMRosterThreatResponse.ProtectMobile;
                case "krylla":
                    return creature.Skills[SkillName.Hiding].Value > 0 ? AIGMRosterThreatResponse.Hide : AIGMRosterThreatResponse.Flee;
                case "ustarte":
                    return AIGMRosterThreatResponse.ProtectMobile;
                default:
                    return enemies > allies + 1 ? AIGMRosterThreatResponse.AlertSquad : AIGMRosterThreatResponse.Fight;
            }
        }

        private static void ApplyResponse(
            IAIGMRosterTaskAgent agent,
            BaseCreature creature,
            AIGMRosterTaskState state,
            AIGMRosterThreatResponse response,
            List<Mobile> threats,
            Mobile nearest)
        {
            if (!AIGMOperationalControlService.CanOperate(creature, AIGMOperationalAction.ThreatAwareness, state.ControlEpoch, false))
                return;

            switch (response)
            {
                case AIGMRosterThreatResponse.Fight:
                case AIGMRosterThreatResponse.HoldGround:
                    if (!AIGMOperationalControlService.CanOperate(creature, AIGMOperationalAction.CombatAssignment, state.ControlEpoch, false))
                        return;
                    creature.Combatant = nearest;
                    creature.FocusMob = nearest;
                    creature.Warmode = true;
                    if (creature.AIObject != null)
                        creature.AIObject.MoveTo(nearest, true, 1);
                    AIGMExecutionLog.Write("AIGM_THREAT_ENGAGE agent={0} target={1}", Describe(creature), Describe(nearest));
                    break;
                case AIGMRosterThreatResponse.Hide:
                    if (creature.Skills[SkillName.Hiding].Value > 0)
                        creature.UseSkill(SkillName.Hiding);
                    Point3D hidePoint = AIGMRosterTaskService.FindRetreatPoint(creature, nearest, 6);
                    state.Mode = AIGMRosterTaskMode.Hide;
                    state.AssignedLocation = hidePoint;
                    AIGMRosterTaskService.MoveToward(creature, hidePoint, true, 1);
                    AIGMExecutionLog.Write("AIGM_THREAT_HIDE agent={0} point={1}", Describe(creature), FormatPoint(hidePoint));
                    break;
                case AIGMRosterThreatResponse.Flee:
                case AIGMRosterThreatResponse.Regroup:
                    Point3D fleePoint = AIGMRosterTaskService.FindRetreatPoint(creature, nearest, 8);
                    state.Mode = response == AIGMRosterThreatResponse.Regroup
                        ? AIGMRosterTaskMode.Regroup
                        : AIGMRosterTaskMode.Flee;
                    state.AssignedLocation = fleePoint;
                    AIGMRosterTaskService.MoveToward(creature, fleePoint, true, 1);
                    AIGMExecutionLog.Write("AIGM_THREAT_FLEE agent={0} point={1}", Describe(creature), FormatPoint(fleePoint));
                    break;
                case AIGMRosterThreatResponse.Reposition:
                    Point3D reposition = AIGMRosterTaskService.FindRetreatPoint(creature, nearest, 4);
                    state.AssignedLocation = reposition;
                    AIGMRosterTaskService.MoveToward(creature, reposition, true, 1);
                    AIGMExecutionLog.Write("AIGM_THREAT_FLEE agent={0} point={1}", Describe(creature), FormatPoint(reposition));
                    break;
                case AIGMRosterThreatResponse.SeekAllies:
                case AIGMRosterThreatResponse.AlertSquad:
                    AlertNearbyAllies(agent, creature, state, nearest);
                    break;
                case AIGMRosterThreatResponse.ProtectMobile:
                    Mobile protect = ResolveProtectedMobile(agent, creature);
                    if (protect != null)
                    {
                        state.Mode = AIGMRosterTaskMode.GuardMobile;
                        state.TargetSerial = protect.Serial;
                        state.TargetCanonicalId = AIGMRosterFactionService.ResolveCharacterId(protect);
                        AIGMRosterTaskService.MoveToward(creature, protect, true, 1, AIGMOperationalAction.Guard, state.ControlEpoch);
                    }
                    else
                    {
                        if (!AIGMOperationalControlService.CanOperate(creature, AIGMOperationalAction.CombatAssignment, state.ControlEpoch, false))
                            return;
                        creature.Combatant = nearest;
                        creature.FocusMob = nearest;
                    }
                    break;
            }
        }

        private static void AlertNearbyAllies(IAIGMRosterTaskAgent agent, BaseCreature creature, AIGMRosterTaskState state, Mobile nearest)
        {
            if (!AIGMOperationalControlService.CanOperate(creature, AIGMOperationalAction.SquadAlert, state.ControlEpoch, false))
                return;

            foreach (IAIGMRosterTaskAgent ally in AIGMRosterTaskService.GetNearbyAllies(agent, creature, 12))
            {
                BaseCreature allyCreature = ally as BaseCreature;
                AIGMRosterTaskState allyState = AIGMRosterTaskService.GetOrCreateState(ally);
                if (allyCreature == null || allyState == null)
                    continue;

                if (!AIGMOperationalControlService.CanOperate(allyCreature, AIGMOperationalAction.SquadAlert, allyState.ControlEpoch, false))
                    continue;

                allyState.LastKnownTargetLocation = nearest.Location;
                allyState.LastKnownTargetMap = nearest.Map;
                allyState.TargetSerial = nearest.Serial;
                allyState.TrailConfidence = Math.Max(allyState.TrailConfidence, 55.0);
                AIGMExecutionLog.Write("AIGM_THREAT_ALERT_SQUAD agent={0} alerted={1} target={2}", Describe(creature), Describe(allyCreature), Describe(nearest));
            }
        }

        private static Mobile ResolveProtectedMobile(IAIGMRosterTaskAgent agent, BaseCreature creature)
        {
            string[] preferred = Array.Empty<string>();
            switch ((agent.RosterCharacterId ?? String.Empty).ToLowerInvariant())
            {
                case "dakeyras":
                case "dakeyras.grey_man":
                    preferred = new[] { "danyal", "miriel", "krylla" };
                    break;
                case "danyal":
                    preferred = new[] { "miriel", "krylla", "dakeyras" };
                    break;
                case "dardalion":
                    preferred = new[] { "danyal", "krylla" };
                    break;
                case "ustarte":
                    preferred = new[] { "dakeyras", "kysumu", "yu_yu_liang" };
                    break;
            }

            for (int i = 0; i < preferred.Length; i++)
            {
                foreach (Mobile mobile in World.Mobiles.Values)
                {
                    if (mobile is IAIGMRosterTaskAgent candidate
                        && String.Equals(candidate.RosterCharacterId, preferred[i], StringComparison.OrdinalIgnoreCase)
                        && mobile.Map == creature.Map
                        && mobile.Alive
                        && !mobile.Deleted
                        && mobile.InRange(creature, 12))
                    {
                        return mobile;
                    }
                }
            }

            return null;
        }

        private static int CountNearbyAllies(IAIGMRosterTaskAgent agent, BaseCreature creature, int range)
        {
            int count = 0;
            foreach (IAIGMRosterTaskAgent ally in AIGMRosterTaskService.GetNearbyAllies(agent, creature, range))
                count++;
            return count;
        }

        private static string Describe(Mobile mobile)
        {
            return mobile == null ? "none" : String.Format("{0}[{1}]", String.IsNullOrWhiteSpace(mobile.Name) ? mobile.GetType().Name : mobile.Name, mobile.Serial);
        }

        private static string FormatPoint(Point3D point)
        {
            return String.Format("{0},{1},{2}", point.X, point.Y, point.Z);
        }
    }
}
