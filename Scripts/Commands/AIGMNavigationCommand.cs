using System;

using Server;
using Server.Custom.AIGM;
using Server.Mobiles;

namespace Server.Commands
{
    public static class AIGMNavigationCommand
    {
        private static readonly bool AllowPlayerServerNavigation = false;

        public static void Initialize()
        {
            CommandSystem.Register("nav", AccessLevel.Player, OnNavigate);
            CommandSystem.Register("walkto", AccessLevel.Player, OnNavigate);
        }

        [Usage("nav <location> | nav status | nav stop | nav list [towns|shrines|dungeons|banks|moongates]")]
        [Description("Starts, stops, or reports assisted stepwise movement toward a registered location.")]
        private static void OnNavigate(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            string arg = e.ArgString != null ? e.ArgString.Trim() : String.Empty;

            if (String.IsNullOrWhiteSpace(arg) || StartsWithList(arg))
            {
                string category = ExtractListCategory(arg);
                e.Mobile.SendMessage("Known locations: " + AIGMNavigationLocationRegistry.FormatList(category));
                e.Mobile.SendMessage(AIGMNavigationRouteGraph.FormatGraphSummary());
                return;
            }

            if (String.Equals(arg, "stop", StringComparison.OrdinalIgnoreCase))
            {
                string stopResponse;
                AIGMSmartMovementService.Stop(e.Mobile, "navigation_stop", out stopResponse);
                int companionStops = StopOwnedCompanionNavigation(e.Mobile, "navigation_stop");
                e.Mobile.SendMessage(stopResponse);
                if (companionStops > 0)
                    e.Mobile.SendMessage("Companion navigation stopped.");
                return;
            }

            if (String.Equals(arg, "status", StringComparison.OrdinalIgnoreCase))
            {
                e.Mobile.SendMessage(AIGMSmartMovementService.GetStatus(e.Mobile));
                return;
            }

            AIGMNavigationLocation location;
            if (!AIGMNavigationLocationRegistry.TryResolve(arg, e.Mobile.Map, out location))
            {
                e.Mobile.SendMessage("Unknown location. Use [nav list for known locations.");
                return;
            }

            if (!AllowPlayerServerNavigation)
            {
                e.Mobile.SendMessage("Player auto-navigation is disabled for smooth streaming. Use companion navigation or [follow explicitly.");
                AIGMExecutionLog.Write("AIGM_PLAYER_NAV_DISABLED actor={0} requested=\"{1}\"", Describe(e.Mobile), SafeLog(arg));
                return;
            }

            StartNavigation(e.Mobile, e.Mobile, location);
        }

        internal static void StartNavigation(Mobile actor, Mobile requester, AIGMNavigationLocation location)
        {
            if (actor == null || location == null)
                return;

            string response;
            AIGMNavigationRoute route;
            if (AIGMNavigationRouteGraph.TryBuildRoute(actor.Location, actor.Map, location, out route))
            {
                if (AIGMSmartMovementService.StartRoute(actor, route, 2, requester, out response))
                {
                    actor.SendMessage(response);
                    return;
                }
            }

            if (AIGMSmartMovementService.StartMoveToPoint(actor, location.Point, location.Map, location.DisplayName, 2, requester, out response))
                actor.SendMessage(response);
            else
                actor.SendMessage(response ?? "Unable to begin navigation.");
        }

        private static bool StartsWithList(string arg)
        {
            return String.IsNullOrWhiteSpace(arg) || arg.Equals("list", StringComparison.OrdinalIgnoreCase) || arg.StartsWith("list ", StringComparison.OrdinalIgnoreCase);
        }

        private static string ExtractListCategory(string arg)
        {
            if (String.IsNullOrWhiteSpace(arg))
                return null;

            string trimmed = arg.Trim();
            if (trimmed.Equals("list", StringComparison.OrdinalIgnoreCase))
                return null;

            return trimmed.StartsWith("list ", StringComparison.OrdinalIgnoreCase) ? trimmed.Substring(5).Trim() : null;
        }

        private static int StopOwnedCompanionNavigation(Mobile owner, string reason)
        {
            if (owner == null || owner.Map == null)
                return 0;

            int stopped = 0;
            IPooledEnumerable mobiles = owner.Map.GetMobilesInRange(owner.Location, 30);
            foreach (Mobile mobile in mobiles)
            {
                BaseHire hire = mobile as BaseHire;
                IAIGMCompanionActor actor = mobile as IAIGMCompanionActor;
                if (hire == null || actor == null || hire.Deleted || !hire.Alive || hire.GetOwner() != owner)
                    continue;

                string response;
                if (AIGMNativeNavigationService.Stop(hire, reason, out response))
                    stopped++;

                AIGMSmartMovementService.Stop(hire, reason, out response);
            }
            mobiles.Free();

            return stopped;
        }

        private static string Describe(Mobile mobile)
        {
            return mobile == null ? "missing" : ((mobile.Name ?? mobile.GetType().Name) + "[" + mobile.Serial + "]");
        }

        private static string SafeLog(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return String.Empty;

            return value.Replace('"', '\'').Replace('\r', ' ').Replace('\n', ' ');
        }
    }
}
