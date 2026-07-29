using System;

using Server.Custom.AIGM.Tasks;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public enum AIGMNativeCombatBridgeResult
    {
        Started,
        AlreadyEngaged,
        InvalidTarget,
        BlockedByStandDown,
        BlockedByPolicy,
        Failed
    }

    public static class AIGMNativeCombatBridge
    {
        public static AIGMNativeCombatBridgeResult TryStartNativeCombat(
            BaseCreature attacker,
            Mobile target,
            Mobile commander,
            int expectedEpoch,
            string source,
            out string detail)
        {
            detail = "failed";

            AIGMNativeCombatBridgeResult validation = Validate(attacker, target, expectedEpoch, out detail);
            if (validation != AIGMNativeCombatBridgeResult.Started)
            {
                LogReject(attacker, target, validation, detail, source);
                return validation;
            }

            if (IsNativeCombatActive(attacker, target))
            {
                detail = "already_engaged";
                LogAlreadyActive(attacker, target, source);
                return AIGMNativeCombatBridgeResult.AlreadyEngaged;
            }

            if (attacker.AIObject == null)
            {
                detail = "no_ai_object";
                LogReject(attacker, target, AIGMNativeCombatBridgeResult.Failed, detail, source);
                return AIGMNativeCombatBridgeResult.Failed;
            }

            attacker.CantWalk = false;

            if (attacker.Controlled && commander != null && commander.Map == attacker.Map && commander.InRange(attacker, 14))
            {
                attacker.AIObject.EndPickTarget(commander, target, OrderType.Attack);

                if (attacker.ControlTarget != target || attacker.ControlOrder != OrderType.Attack)
                {
                    detail = "native_pick_target_rejected";
                    LogReject(attacker, target, AIGMNativeCombatBridgeResult.Failed, detail, source);
                    return AIGMNativeCombatBridgeResult.Failed;
                }
            }
            else
            {
                attacker.ControlTarget = target;
                attacker.ControlOrder = OrderType.Attack;
            }

            attacker.Combatant = target;
            attacker.FocusMob = null;
            attacker.Warmode = true;
            attacker.CurrentSpeed = attacker.ActiveSpeed;
            attacker.AIObject.Action = ActionType.Combat;
            attacker.AIObject.Obey();

            detail = "native_combat_started";
            AIGMExecutionLog.Write(
                "AIGM_NATIVE_COMBAT_START attacker={0} target={1} commander={2} source={3} order={4} combatant={5}",
                Describe(attacker),
                Describe(target),
                Describe(commander),
                Safe(source),
                attacker.ControlOrder,
                Describe(attacker.Combatant as Mobile));

            LogWeapon(attacker, target);
            return AIGMNativeCombatBridgeResult.Started;
        }

        public static bool IsNativeCombatActive(BaseCreature attacker, Mobile target)
        {
            if (attacker == null || target == null || target.Deleted || !target.Alive)
                return false;

            if (attacker.Deleted || !attacker.Alive || attacker.Map != target.Map)
                return false;

            return attacker.Combatant == target
                || (attacker.ControlOrder == OrderType.Attack && attacker.ControlTarget == target);
        }

        public static void LogAlreadyActive(BaseCreature attacker, Mobile target, string source)
        {
            AIGMExecutionLog.Write(
                "AIGM_NATIVE_COMBAT_ALREADY_ACTIVE attacker={0} target={1} source={2} order={3} combatant={4}",
                Describe(attacker),
                Describe(target),
                Safe(source),
                attacker != null ? attacker.ControlOrder.ToString() : "none",
                attacker != null ? Describe(attacker.Combatant as Mobile) : "none");
        }

        public static void EndNativeCombat(BaseCreature attacker, Mobile target, string reason, bool clearMobileState)
        {
            if (attacker == null || attacker.Deleted)
                return;

            AIGMExecutionLog.Write(
                "AIGM_NATIVE_COMBAT_END attacker={0} target={1} reason={2} order={3} combatant={4}",
                Describe(attacker),
                Describe(target),
                Safe(reason),
                attacker.ControlOrder,
                Describe(attacker.Combatant as Mobile));

            if (!clearMobileState)
                return;

            if (target == null || attacker.Combatant == target)
                attacker.Combatant = null;

            if (target == null || attacker.FocusMob == target)
                attacker.FocusMob = null;

            if (target == null || attacker.ControlTarget == target)
            {
                attacker.ControlTarget = null;
                if (attacker.ControlOrder == OrderType.Attack)
                    attacker.ControlOrder = OrderType.None;
            }

            if (attacker.Combatant == null)
                attacker.Warmode = false;
        }

        private static AIGMNativeCombatBridgeResult Validate(BaseCreature attacker, Mobile target, int expectedEpoch, out string detail)
        {
            detail = "ok";

            if (attacker == null || attacker.Deleted || !attacker.Alive)
            {
                detail = "invalid_attacker";
                return AIGMNativeCombatBridgeResult.InvalidTarget;
            }

            if (target == null || target.Deleted || !target.Alive)
            {
                detail = "invalid_target";
                return AIGMNativeCombatBridgeResult.InvalidTarget;
            }

            if (target == attacker)
            {
                detail = "self_target";
                return AIGMNativeCombatBridgeResult.InvalidTarget;
            }

            if (target.Map != attacker.Map || attacker.Map == null || attacker.Map == Map.Internal)
            {
                detail = "different_map";
                return AIGMNativeCombatBridgeResult.InvalidTarget;
            }

            if (!AIGMOperationalControlService.CanOperate(attacker, AIGMOperationalAction.CombatAssignment, expectedEpoch, false))
            {
                AIGMOperationalMode mode = AIGMOperationalControlService.GetMode(attacker);
                detail = "operational_" + mode.ToString().ToLowerInvariant();
                return mode == AIGMOperationalMode.PassiveStandDown || mode == AIGMOperationalMode.AbsoluteGMHold
                    ? AIGMNativeCombatBridgeResult.BlockedByStandDown
                    : AIGMNativeCombatBridgeResult.BlockedByPolicy;
            }

            if (attacker.ControlMaster != null && target == attacker.ControlMaster)
            {
                detail = "blocked_control_master";
                return AIGMNativeCombatBridgeResult.BlockedByPolicy;
            }

            if (target is BaseGuard || target is BaseVendor || target is PlayerVendor || target.Blessed || target.IsStaff())
            {
                detail = "protected_target";
                return AIGMNativeCombatBridgeResult.BlockedByPolicy;
            }

            IAIGMRosterTaskAgent attackerAgent = attacker as IAIGMRosterTaskAgent;
            IAIGMRosterTaskAgent targetAgent = target as IAIGMRosterTaskAgent;
            if (attackerAgent != null && targetAgent != null)
            {
                AIGMRosterFaction attackerFaction = AIGMRosterFactionService.ResolveFaction(attackerAgent.RosterCharacterId);
                AIGMRosterFaction targetFaction = AIGMRosterFactionService.ResolveFaction(targetAgent.RosterCharacterId);
                if (AIGMRosterFactionService.AreAllied(attackerFaction, targetFaction)
                    && !AIGMRosterFactionService.HasExplicitEnemyRelationship(attacker, target))
                {
                    detail = "allied_roster_target";
                    return AIGMNativeCombatBridgeResult.BlockedByPolicy;
                }
            }

            if (!attacker.CanBeHarmful(target, false))
            {
                detail = "cannot_be_harmful";
                return AIGMNativeCombatBridgeResult.BlockedByPolicy;
            }

            return AIGMNativeCombatBridgeResult.Started;
        }

        private static void LogReject(BaseCreature attacker, Mobile target, AIGMNativeCombatBridgeResult result, string detail, string source)
        {
            AIGMExecutionLog.Write(
                "AIGM_NATIVE_COMBAT_REJECT attacker={0} target={1} result={2} detail={3} source={4}",
                Describe(attacker),
                Describe(target),
                result,
                Safe(detail),
                Safe(source));
        }

        private static void LogWeapon(BaseCreature attacker, Mobile target)
        {
            IWeapon activeWeapon = attacker != null ? attacker.Weapon : null;
            Item weaponItem = activeWeapon as Item;
            BaseWeapon baseWeapon = activeWeapon as BaseWeapon;

            AIGMExecutionLog.Write(
                "AIGM_NATIVE_COMBAT_WEAPON attacker={0} serial={1} target={2} targetSerial={3} weaponClass={4} itemSerial={5} layer={6} weaponSkill={7} animation={8} hitSound={9} missSound={10}",
                Describe(attacker),
                attacker != null ? attacker.Serial.Value : 0,
                Describe(target),
                target != null ? target.Serial.Value : 0,
                activeWeapon != null ? activeWeapon.GetType().Name : "none",
                weaponItem != null ? weaponItem.Serial.Value : 0,
                weaponItem != null ? weaponItem.Layer.ToString() : "none",
                baseWeapon != null ? baseWeapon.Skill.ToString() : "none",
                baseWeapon != null ? baseWeapon.Animation.ToString() : "none",
                baseWeapon != null ? baseWeapon.HitSound.ToString() : "none",
                baseWeapon != null ? baseWeapon.MissSound.ToString() : "none");
        }

        private static string Describe(Mobile mobile)
        {
            return mobile == null
                ? "none"
                : String.Format("{0}[0x{1:X8}]", String.IsNullOrWhiteSpace(mobile.Name) ? mobile.GetType().Name : mobile.Name, mobile.Serial.Value);
        }

        private static string Safe(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return String.Empty;

            value = value.Replace('"', '\'').Replace('\r', ' ').Replace('\n', ' ');
            return value.Length > 220 ? value.Substring(0, 220) : value;
        }
    }
}
