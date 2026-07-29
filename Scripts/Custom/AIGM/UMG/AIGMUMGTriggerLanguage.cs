using System;
using System.Collections.Generic;

namespace Server.Custom.AIGM.UMG
{
    public static class AIGMUMGTriggerLanguage
    {
        private static readonly HashSet<string> AllowedFacts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "self.health_pct", "self.mana_pct", "self.stamina_pct", "self.poisoned", "self.bleeding",
            "self.hidden", "self.mounted", "self.current_task", "self.operational_mode", "self.current_target",
            "self.ammunition_count", "self.bandage_count", "self.potion_count", "self.reagent_count",
            "ally.ally_count", "ally.ally_health_pct", "ally.protected_ally_health_pct", "ally.commander_health_pct",
            "ally.healer_available", "ally.ally_incapacitated", "ally.ally_under_attack",
            "enemy.enemy_count", "enemy.enemy_distance", "enemy.enemy_faction", "enemy.enemy_is_caster",
            "enemy.enemy_is_archer", "enemy.enemy_is_melee", "enemy.enemy_is_hidden", "enemy.enemy_is_demon",
            "enemy.enemy_is_undead", "enemy.enemy_is_boss", "enemy.enemy_is_fleeing", "enemy.enemy_attacking_protected_ally",
            "environment.guarded_region", "environment.town", "environment.dungeon", "environment.open_field",
            "environment.bridge", "environment.doorway", "environment.assigned_region", "environment.near_home",
            "environment.near_waypoint", "environment.destination_reached",
            "mission.following", "mission.guarding", "mission.tracking", "mission.hunting", "mission.traveling",
            "mission.patrolling", "mission.regrouping", "mission.returning_home", "mission.target_lost",
            "mission.task_interrupted",
            "squad.squad_count", "squad.outnumbered", "squad.squad_split", "squad.commander_down",
            "squad.healer_down", "squad.shared_target_acquired"
        };

        public static Dictionary<string, string> BuildFacts(AIGMCapabilitySnapshot snapshot)
        {
            Dictionary<string, string> facts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (snapshot == null)
                return facts;

            facts["self.health_pct"] = snapshot.HealthPct.ToString("0.##");
            facts["self.mana_pct"] = snapshot.ManaPct.ToString("0.##");
            facts["self.stamina_pct"] = snapshot.StaminaPct.ToString("0.##");
            facts["self.poisoned"] = snapshot.Poisoned ? "true" : "false";
            facts["self.bleeding"] = "false";
            facts["self.hidden"] = snapshot.Hidden ? "true" : "false";
            facts["self.mounted"] = snapshot.Mounted ? "true" : "false";
            facts["self.current_task"] = snapshot.CurrentTask ?? String.Empty;
            facts["self.operational_mode"] = snapshot.OperationalMode ?? String.Empty;
            facts["self.current_target"] = "none";
            facts["self.ammunition_count"] = snapshot.AmmunitionCount.ToString();
            facts["self.bandage_count"] = snapshot.BandageCount.ToString();
            facts["self.potion_count"] = snapshot.PotionCount.ToString();
            facts["self.reagent_count"] = snapshot.ReagentCount.ToString();
            facts["environment.guarded_region"] = "unknown";
            facts["environment.town"] = "unknown";
            facts["environment.dungeon"] = "unknown";
            facts["mission.following"] = Contains(snapshot.CurrentTask, "FollowOwner").ToString().ToLowerInvariant();
            facts["mission.guarding"] = Contains(snapshot.CurrentTask, "Guard").ToString().ToLowerInvariant();
            facts["mission.tracking"] = Contains(snapshot.CurrentTask, "Track").ToString().ToLowerInvariant();
            facts["mission.hunting"] = Contains(snapshot.CurrentTask, "Hunt").ToString().ToLowerInvariant();
            facts["mission.traveling"] = Contains(snapshot.CurrentTask, "Travel").ToString().ToLowerInvariant();
            facts["mission.patrolling"] = Contains(snapshot.CurrentTask, "Patrol").ToString().ToLowerInvariant();
            facts["mission.regrouping"] = Contains(snapshot.CurrentTask, "Regroup").ToString().ToLowerInvariant();
            facts["mission.returning_home"] = Contains(snapshot.CurrentTask, "ReturnHome").ToString().ToLowerInvariant();
            facts["mission.target_lost"] = Contains(snapshot.CurrentTask, "lost").ToString().ToLowerInvariant();
            facts["mission.task_interrupted"] = Contains(snapshot.CurrentTask, "interrupt").ToString().ToLowerInvariant();
            return facts;
        }

        public static bool Evaluate(AIGMUMGActivationRule rule, Dictionary<string, string> facts, out string reason)
        {
            reason = "active";
            if (rule == null || rule.AlwaysActive || String.IsNullOrWhiteSpace(rule.TriggerText))
                return true;

            string text = Normalize(rule.TriggerText);
            if (String.IsNullOrWhiteSpace(text))
                return true;

            string[] orParts = SplitOperator(text, " or ");
            if (orParts.Length > 1)
            {
                List<string> rejections = new List<string>();
                for (int i = 0; i < orParts.Length; i++)
                {
                    string childReason;
                    if (EvaluateText(orParts[i], facts, out childReason))
                    {
                        reason = childReason;
                        return true;
                    }
                    rejections.Add(childReason);
                }

                reason = "or_false(" + String.Join(";", rejections.ToArray()) + ")";
                return false;
            }

            string[] andParts = SplitOperator(text, " and ");
            if (andParts.Length > 1)
            {
                for (int i = 0; i < andParts.Length; i++)
                {
                    string childReason;
                    if (!EvaluateText(andParts[i], facts, out childReason))
                    {
                        reason = childReason;
                        return false;
                    }
                }

                reason = "and_true";
                return true;
            }

            return EvaluateText(text, facts, out reason);
        }

        public static bool IsAllowedFact(string fact)
        {
            return AllowedFacts.Contains(NormalizeFact(fact));
        }

        private static bool EvaluateText(string text, Dictionary<string, string> facts, out string reason)
        {
            reason = "active";
            text = Normalize(text);

            if (text.StartsWith("not ", StringComparison.Ordinal))
            {
                string childReason;
                bool child = EvaluateText(text.Substring(4), facts, out childReason);
                reason = child ? "not_false(" + childReason + ")" : "not_true";
                return !child;
            }

            string[] operators = { " not equals ", " less than or equal ", " greater than or equal ", " equals ", " contains ", " in set ", " != ", " <= ", " >= ", " == ", " < ", " > " };
            for (int i = 0; i < operators.Length; i++)
            {
                int index = text.IndexOf(operators[i], StringComparison.Ordinal);
                if (index <= 0)
                    continue;

                string fact = NormalizeFact(text.Substring(0, index));
                string expected = TrimValue(text.Substring(index + operators[i].Length));
                if (!IsAllowedFact(fact))
                {
                    reason = "fact_not_allowed:" + fact;
                    return false;
                }

                string actual;
                if (facts == null || !facts.TryGetValue(fact, out actual))
                {
                    reason = "fact_missing:" + fact;
                    return false;
                }

                bool ok = Compare(actual, expected, operators[i]);
                reason = fact + "=" + actual + (ok ? ":passed" : ":failed");
                return ok;
            }

            reason = "unsupported_trigger_expression";
            return false;
        }

        private static bool Compare(string actual, string expected, string op)
        {
            actual = actual ?? String.Empty;
            expected = expected ?? String.Empty;

            double left = 0.0;
            double right = 0.0;
            bool numeric = Double.TryParse(actual, out left) && Double.TryParse(expected, out right);

            switch (op.Trim())
            {
                case "equals":
                case "==":
                    return String.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
                case "not equals":
                case "!=":
                    return !String.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
                case "less than":
                case "<":
                    return numeric && left < right;
                case "less than or equal":
                case "<=":
                    return numeric && left <= right;
                case "greater than":
                case ">":
                    return numeric && left > right;
                case "greater than or equal":
                case ">=":
                    return numeric && left >= right;
                case "contains":
                    return actual.IndexOf(expected, StringComparison.OrdinalIgnoreCase) >= 0;
                case "in set":
                    string[] values = expected.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < values.Length; i++)
                    {
                        if (String.Equals(actual, values[i].Trim(), StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                    return false;
                default:
                    return false;
            }
        }

        private static string[] SplitOperator(string text, string op)
        {
            return text.Split(new[] { op }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static string NormalizeFact(string value)
        {
            return (value ?? String.Empty).Trim().ToLowerInvariant();
        }

        private static string Normalize(string value)
        {
            string text = (value ?? String.Empty).Trim().ToLowerInvariant();
            while (text.Contains("  "))
                text = text.Replace("  ", " ");
            return text;
        }

        private static string TrimValue(string value)
        {
            return (value ?? String.Empty).Trim().Trim('"').Trim('\'');
        }

        private static bool Contains(string value, string needle)
        {
            return !String.IsNullOrWhiteSpace(value) && value.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
