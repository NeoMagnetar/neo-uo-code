using System;
using System.Reflection;

using Server.Custom.AIGM;
using Server.Mobiles;
using Server.Services.AIGM;
using Server.Targeting;

namespace Server.Commands
{
    public static class AIGMPlayerFollowCommand
    {
        private static readonly MethodInfo StopFollowingMethod = typeof(PlayerAutoFollow).GetMethod(
            "StopFollowing",
            BindingFlags.NonPublic | BindingFlags.Static,
            null,
            new[] { typeof(Mobile), typeof(bool) },
            null);

        public static void Initialize()
        {
            CommandSystem.Register("follow", AccessLevel.Player, OnFollow);
            CommandSystem.Register("stopfollow", AccessLevel.Player, OnStopFollow);
            Timer.DelayCall(TimeSpan.Zero, RegisterCompatibilityAliases);
        }

        private static void RegisterCompatibilityAliases()
        {
            CommandSystem.Register("AutoFollow", AccessLevel.Player, OnAutoFollowAlias);
            CommandSystem.Register("StopAutoFollow", AccessLevel.Player, OnStopAutoFollowAlias);
        }

        [Usage("follow | follow <name> | follow stop")]
        [Description("Compatibility command for the recovered old Dev PlayerAutoFollow service.")]
        private static void OnFollow(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            string arg = e.ArgString != null ? e.ArgString.Trim() : String.Empty;

            if (String.Equals(arg, "stop", StringComparison.OrdinalIgnoreCase))
            {
                StopOldDevFollow(e.Mobile, true);
                return;
            }

            if (!String.IsNullOrWhiteSpace(arg))
            {
                Mobile target = ResolveNearbyMobileByName(e.Mobile, arg, 64);
                if (target == null)
                {
                    e.Mobile.SendMessage("No unique nearby mobile matched that name.");
                    return;
                }

                StartOldDevFollow(e.Mobile, target);
                return;
            }

            e.Mobile.SendMessage("Target the mobile you want to follow.");
            e.Mobile.Target = new FollowTarget();
        }

        [Usage("stopfollow")]
        [Description("Stops old Dev autofollow for your character.")]
        private static void OnStopFollow(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            StopOldDevFollow(e.Mobile, true);
        }

        private static void OnAutoFollowAlias(CommandEventArgs e)
        {
            OnFollow(e);
        }

        private static void OnStopAutoFollowAlias(CommandEventArgs e)
        {
            OnStopFollow(e);
        }

        private static void StartOldDevFollow(Mobile from, Mobile target)
        {
            PlayerMobile player = from as PlayerMobile;
            if (player == null || player.Deleted)
            {
                if (from != null)
                    from.SendMessage("Only a player can use follow.");
                return;
            }

            if (target == null || target.Deleted || target == from)
            {
                from.SendMessage("That is not a valid follow target.");
                return;
            }

            AIGMSmartMovementService.Stop(player, "old_dev_autofollow_start", out _);
            StopOldDevServerFollow(player, false);
            SendSmoothFollowDirective(player, target);
            AIGMExecutionLog.Write("AIGM_PLAYER_SMOOTHFOLLOW_START actor={0} target={1} serial=0x{2:X8} primitive=razor_client_run source=razor_smoothfollow_bridge", Describe(player), Describe(target), target.Serial.Value);
        }

        private static void StopOldDevFollow(Mobile from, bool notify)
        {
            if (from == null)
                return;

            StopOldDevServerFollow(from, false);
            AIGMSmartMovementService.Stop(from, "old_dev_autofollow_stop", out _);
            SendSmoothFollowStopDirective(from, notify);
            AIGMExecutionLog.Write("AIGM_PLAYER_SMOOTHFOLLOW_STOP actor={0} source=razor_smoothfollow_bridge", Describe(from));
        }

        private static void StopOldDevServerFollow(Mobile from, bool notify)
        {
            if (from == null || StopFollowingMethod == null)
                return;

            StopFollowingMethod.Invoke(null, new object[] { from, notify });
        }

        private static void SendSmoothFollowDirective(PlayerMobile player, Mobile target)
        {
            if (player == null || target == null)
                return;

            player.SendMessage(68, "AIGM_SMOOTHFOLLOW_START serial=0x{0:X8} name={1}", target.Serial.Value, SanitizeName(target));
        }

        private static void SendSmoothFollowStopDirective(Mobile from, bool notify)
        {
            if (from == null)
                return;

            from.SendMessage(68, "AIGM_SMOOTHFOLLOW_STOP");
            if (notify)
                from.SendMessage("You stop auto-following.");
        }

        private static Mobile ResolveNearbyMobileByName(Mobile from, string name, int range)
        {
            if (from == null || from.Map == null || String.IsNullOrWhiteSpace(name))
                return null;

            Mobile match = null;
            double bestDistance = Double.MaxValue;
            string normalized = name.Trim().ToLowerInvariant();
            IPooledEnumerable mobiles = from.Map.GetMobilesInRange(from.Location, range);
            foreach (Mobile mobile in mobiles)
            {
                if (mobile == null || mobile == from || mobile.Deleted || !from.CanSee(mobile))
                    continue;

                string mobileName = mobile.Name != null ? mobile.Name.Trim().ToLowerInvariant() : String.Empty;
                if (!IsNameMatch(mobileName, normalized))
                    continue;

                double distance = from.GetDistanceToSqrt(mobile);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    match = mobile;
                }
            }
            mobiles.Free();

            return match;
        }

        private static bool IsNameMatch(string mobileName, string requested)
        {
            if (String.IsNullOrWhiteSpace(mobileName) || String.IsNullOrWhiteSpace(requested))
                return false;

            return mobileName == requested
                || mobileName.StartsWith(requested + " ", StringComparison.OrdinalIgnoreCase)
                || requested.StartsWith(mobileName + " ", StringComparison.OrdinalIgnoreCase);
        }

        private sealed class FollowTarget : Target
        {
            public FollowTarget()
                : base(12, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                PlayerMobile player = from as PlayerMobile;
                Mobile target = targeted as Mobile;
                if (player == null)
                    return;

                if (target == null || target.Deleted || target == from)
                {
                    from.SendMessage("That is not a valid follow target.");
                    return;
                }

                StartOldDevFollow(player, target);
            }
        }

        private static string Describe(Mobile mobile)
        {
            return mobile == null ? "missing" : ((mobile.Name ?? mobile.GetType().Name) + "[" + mobile.Serial + "]");
        }

        private static string SanitizeName(Mobile mobile)
        {
            string name = mobile != null ? (mobile.Name ?? mobile.GetType().Name) : "target";
            return name.Replace('\r', ' ').Replace('\n', ' ');
        }

    }
}
