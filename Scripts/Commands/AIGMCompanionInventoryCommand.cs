using System;
using System.Collections.Generic;
using System.Globalization;
using Server.Custom.AIGM.Inventory;
using Server.Mobiles;

namespace Server.Commands
{
    public static class AIGMCompanionInventoryCommand
    {
        private static readonly Serial[] PilotSerials =
        {
            (Serial)0x00002AA5, // Miriel
            (Serial)0x00000193, // Dardalion
            (Serial)0x00000304, // Druss
            (Serial)0x00003575  // Durmast
        };

        public static void Initialize()
        {
            AIGMCompanionInventoryService.Initialize();
            CommandSystem.Register("umgpack", AccessLevel.Player, OnPack);
            CommandSystem.Register("umgpackadmin", AccessLevel.GameMaster, OnAdmin);
        }

        private static void OnPack(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            string selector = e.ArgString != null ? e.ArgString.Trim() : String.Empty;
            if (String.IsNullOrWhiteSpace(selector))
            {
                from.SendMessage(38, "Usage: [umgpack <serial|name>]");
                return;
            }

            Mobile actor;
            AIGMCompanionInventoryResult resolved = AIGMCompanionInventoryService.ResolveSelector(selector, out actor);
            if (!resolved.Accepted)
            {
                from.SendMessage(38, "{0} correlation={1}", resolved.Message, resolved.CorrelationId);
                return;
            }

            AIGMCompanionInventoryService.OpenBackpack(from, actor);
        }

        private static void OnAdmin(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            string action = e.GetString(0);
            string selector = e.Arguments.Length > 1 ? Join(e.Arguments, 1) : String.Empty;

            if (String.IsNullOrWhiteSpace(action))
            {
                Usage(from);
                return;
            }

            action = action.ToLowerInvariant();
            if (action == "audit" && String.Equals(selector, "all", StringComparison.OrdinalIgnoreCase))
            {
                AuditAll(from);
                return;
            }

            if (action == "ensure" && String.Equals(selector, "pilot", StringComparison.OrdinalIgnoreCase))
            {
                EnsurePilot(from);
                return;
            }

            if (action == "normalize" && String.Equals(selector, "all", StringComparison.OrdinalIgnoreCase))
            {
                NormalizeAll(from);
                return;
            }

            if ((action == "status" || action == "audit" || action == "ensure" || action == "migrate") && !String.IsNullOrWhiteSpace(selector))
            {
                RunActorAction(from, action, selector);
                return;
            }

            Usage(from);
        }

        private static void RunActorAction(Mobile from, string action, string selector)
        {
            Mobile actor;
            AIGMCompanionInventoryResult resolved = AIGMCompanionInventoryService.ResolveSelector(selector, out actor);
            if (!resolved.Accepted)
            {
                from.SendMessage(38, "{0} correlation={1}", resolved.Message, resolved.CorrelationId);
                return;
            }

            if (action == "status" || action == "audit")
            {
                from.SendMessage(68, AIGMCompanionInventoryService.BuildAuditLine(actor));
                return;
            }

            AIGMCompanionInventoryResult result = action == "migrate"
                ? AIGMCompanionInventoryService.MigrateBackpack(actor, "admin_migrate_command")
                : AIGMCompanionInventoryService.EnsureBackpack(actor, "admin_ensure_command");

            from.SendMessage(result.Accepted ? 68 : 38, "{0} correlation={1}", result.Message, result.CorrelationId);
            from.SendMessage(result.Accepted ? 68 : 38, AIGMCompanionInventoryService.BuildAuditLine(actor));
        }

        private static void AuditAll(Mobile from)
        {
            Dictionary<AIGMCompanionBackpackClassification, int> counts = new Dictionary<AIGMCompanionBackpackClassification, int>();
            List<Mobile> actors = AIGMCompanionInventoryService.EnumerateRegisteredLiveCompanions();
            actors.Sort(CompareMobiles);

            for (int i = 0; i < actors.Count; i++)
            {
                AIGMCompanionBackpackAudit audit = AIGMCompanionInventoryService.AuditBackpack(actors[i]);
                int count;
                counts.TryGetValue(audit.Classification, out count);
                counts[audit.Classification] = count + 1;
                from.SendMessage(68, AIGMCompanionInventoryService.BuildAuditLine(actors[i]));
            }

            from.SendMessage(68, "AIGM backpack audit all: registered_live={0}; {1}", actors.Count, FormatCounts(counts));
        }

        private static void EnsurePilot(Mobile from)
        {
            for (int i = 0; i < PilotSerials.Length; i++)
            {
                Mobile actor = World.FindMobile(PilotSerials[i]);
                if (actor == null)
                {
                    from.SendMessage(38, "Pilot {0} not found; stopping.", Format(PilotSerials[i]));
                    return;
                }

                AIGMCompanionInventoryResult result = AIGMCompanionInventoryService.EnsureBackpack(actor, "pilot_ensure_command");
                from.SendMessage(result.Accepted ? 68 : 38, "{0}: {1} correlation={2}", Format(actor.Serial), result.Message, result.CorrelationId);
                if (!result.Accepted)
                    return;
            }
        }

        private static void NormalizeAll(Mobile from)
        {
            List<Mobile> actors = AIGMCompanionInventoryService.EnumerateRegisteredLiveCompanions();
            actors.Sort(CompareMobiles);
            int normalized = 0;

            for (int i = 0; i < actors.Count; i++)
            {
                AIGMCompanionInventoryResult result = AIGMCompanionInventoryService.EnsureBackpack(actors[i], "roster_normalize_all_command");
                from.SendMessage(result.Accepted ? 68 : 38, "{0}: {1} correlation={2}", Format(actors[i].Serial), result.Message, result.CorrelationId);
                if (!result.Accepted)
                {
                    from.SendMessage(38, "Roster normalization stopped after {0} successful actors.", normalized);
                    return;
                }

                normalized++;
            }

            from.SendMessage(68, "Roster normalization completed for {0} registered live AIGM companions.", normalized);
        }

        private static int CompareMobiles(Mobile left, Mobile right)
        {
            int leftSerial = left != null ? left.Serial.Value : 0;
            int rightSerial = right != null ? right.Serial.Value : 0;
            return leftSerial.CompareTo(rightSerial);
        }

        private static string FormatCounts(Dictionary<AIGMCompanionBackpackClassification, int> counts)
        {
            List<string> parts = new List<string>();
            foreach (AIGMCompanionBackpackClassification value in Enum.GetValues(typeof(AIGMCompanionBackpackClassification)))
            {
                int count;
                if (counts.TryGetValue(value, out count) && count > 0)
                    parts.Add(value + "=" + count.ToString(CultureInfo.InvariantCulture));
            }

            return String.Join(", ", parts.ToArray());
        }

        private static string Join(string[] parts, int start)
        {
            List<string> values = new List<string>();
            for (int i = start; i < parts.Length; i++)
                values.Add(parts[i]);

            return String.Join(" ", values.ToArray()).Trim();
        }

        private static string Format(Serial serial)
        {
            return serial.IsValid ? String.Format("0x{0:X8}", serial.Value) : "none";
        }

        private static void Usage(Mobile from)
        {
            from.SendMessage(68, "Usage: [umgpackadmin status|audit|ensure|migrate <serial|name>]");
            from.SendMessage(68, "Usage: [umgpackadmin audit all | ensure pilot | normalize all]");
        }
    }
}
