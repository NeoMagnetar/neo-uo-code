using System;
using Server.Custom.AIGM;
using Server.Mobiles;

namespace Server.Commands
{
    public static class AIGMCompanionCommand
    {
        private static readonly string[] CompanionNames =
        {
            "Dakeyras",
            "Danyal",
            "Dardalion"
        };

        public static void Initialize()
        {
            CommandSystem.Register("AIGMCompanion", AccessLevel.GameMaster, new CommandEventHandler(OnAIGMCompanionCommand));
            CommandSystem.Register("AIGMCompanionStatus", AccessLevel.GameMaster, new CommandEventHandler(OnAIGMCompanionCommand));
            CommandSystem.Register("AIGMCompanionRoute", AccessLevel.GameMaster, new CommandEventHandler(OnAIGMCompanionRouteCommand));
            CommandSystem.Register("AIGMCompanionSpawnCheck", AccessLevel.GameMaster, new CommandEventHandler(OnAIGMCompanionSpawnCheckCommand));
            CommandSystem.Register("Dakeyras", AccessLevel.GameMaster, new CommandEventHandler(OnSpawnDakeyrasCommand));
            CommandSystem.Register("Danyal", AccessLevel.GameMaster, new CommandEventHandler(OnSpawnDanyalCommand));
            CommandSystem.Register("Dardalion", AccessLevel.GameMaster, new CommandEventHandler(OnSpawnDardalionCommand));
        }

        [Usage("AIGMCompanion")]
        [Description("Reports the current companion-lane shell status. Live movement is deferred.")]
        private static void OnAIGMCompanionCommand(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            e.Mobile.SendMessage("Companion lane shell is present.");
            e.Mobile.SendMessage("Companions: {0}.", String.Join(", ", CompanionNames));
            e.Mobile.SendMessage("Movement is disabled in this phase.");
            e.Mobile.SendMessage("Deferred commands: follow, come, guard, stay.");
        }

        [Usage("AIGMCompanionRoute <speech>")]
        [Description("Classifies speech for companion-lane routing without executing commands.")]
        private static void OnAIGMCompanionRouteCommand(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            string speech = e.ArgString ?? String.Empty;
            AIGMCompanionCommandRouteDecision decision = AIGMCompanionCommandBoundary.Classify(speech);

            e.Mobile.SendMessage("Route kind: {0}", decision.RouteKind);
            e.Mobile.SendMessage("Companion key: {0}", String.IsNullOrWhiteSpace(decision.CompanionKey) ? "(none)" : decision.CompanionKey);
            e.Mobile.SendMessage("Companion name: {0}", String.IsNullOrWhiteSpace(decision.CompanionName) ? "(none)" : decision.CompanionName);
            e.Mobile.SendMessage("Command verb: {0}", String.IsNullOrWhiteSpace(decision.CommandVerb) ? "(none)" : decision.CommandVerb);
            e.Mobile.SendMessage("Blocks counselor lane: {0}", decision.BlocksCounselorLane ? "yes" : "no");
            e.Mobile.SendMessage("Executable now: {0}", decision.IsExecutableNow ? "yes" : "no");
            e.Mobile.SendMessage("Reason: {0}", decision.Reason ?? "(none)");
            e.Mobile.SendMessage("Movement deferred: yes");
        }

        [Usage("AIGMCompanionSpawnCheck")]
        [Description("Reports whether companion shell types are loaded and constructable.")]
        private static void OnAIGMCompanionSpawnCheckCommand(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            ReportSpawnable<AIGMCompanionDakeyras>(e, "Dakeyras");
            ReportSpawnable<AIGMCompanionDanyal>(e, "Danyal");
            ReportSpawnable<AIGMCompanionDardalion>(e, "Dardalion");
            e.Mobile.SendMessage("Movement remains disabled in this phase.");
        }

        [Usage("Dakeyras")]
        [Description("Spawns Dakeyras at your location.")]
        private static void OnSpawnDakeyrasCommand(CommandEventArgs e)
        {
            SpawnCompanion<AIGMCompanionDakeyras>(e, "Dakeyras");
        }

        [Usage("Danyal")]
        [Description("Spawns Danyal at your location.")]
        private static void OnSpawnDanyalCommand(CommandEventArgs e)
        {
            SpawnCompanion<AIGMCompanionDanyal>(e, "Danyal");
        }

        [Usage("Dardalion")]
        [Description("Spawns Dardalion at your location.")]
        private static void OnSpawnDardalionCommand(CommandEventArgs e)
        {
            SpawnCompanion<AIGMCompanionDardalion>(e, "Dardalion");
        }

        private static void ReportSpawnable<T>(CommandEventArgs e, string name) where T : Mobile
        {
            Type type = typeof(T);
            bool constructable = type.GetConstructor(Type.EmptyTypes) != null;
            bool serialCtor = type.GetConstructor(new[] { typeof(Serial) }) != null;

            e.Mobile.SendMessage("{0}: loaded=yes constructable={1} serialCtor={2}", name, constructable ? "yes" : "no", serialCtor ? "yes" : "no");
        }

        private static void SpawnCompanion<T>(CommandEventArgs e, string name) where T : Mobile, new()
        {
            if (e == null || e.Mobile == null)
                return;

            Map map = e.Mobile.Map;
            if (map == null)
            {
                e.Mobile.SendMessage("Cannot spawn {0}: no active map.", name);
                return;
            }

            T mobile = new T();
            mobile.MoveToWorld(e.Mobile.Location, map);
            e.Mobile.SendMessage("Spawned {0}.", name);
        }
    }
}
