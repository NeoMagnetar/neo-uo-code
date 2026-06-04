using System;
using System.Collections.Generic;
using Server.Commands;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Services.AIGM
{
    public static class PlayerAutoFollow
    {
        private static readonly Dictionary<int, AutoFollowState> States = new Dictionary<int, AutoFollowState>();

        public static void Initialize()
        {
            CommandSystem.Register("AutoFollow", AccessLevel.Player, OnAutoFollowCommand);
            CommandSystem.Register("StopAutoFollow", AccessLevel.Player, OnStopAutoFollowCommand);
        }

        private static void OnAutoFollowCommand(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            e.Mobile.SendMessage("Target the mobile you want to follow.");
            e.Mobile.Target = new AutoFollowTarget();
        }

        private static void OnStopAutoFollowCommand(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            StopFollowing(e.Mobile, true);
        }

        private static void StartFollowing(PlayerMobile from, Mobile target)
        {
            if (from == null || from.Deleted || target == null || target.Deleted)
                return;

            StopFollowing(from, false);

            AutoFollowState state = new AutoFollowState(from, target);
            States[from.Serial.Value] = state;
            state.Start();
            from.SendMessage("You begin following {0}.", target.Name ?? target.GetType().Name);
        }

        private static void StopFollowing(Mobile from, bool notify)
        {
            if (from == null)
                return;

            AutoFollowState state;
            if (!States.TryGetValue(from.Serial.Value, out state))
            {
                if (notify)
                    from.SendMessage("You are not auto-following anyone.");
                return;
            }

            States.Remove(from.Serial.Value);
            state.Stop();
            if (notify)
                from.SendMessage("You stop auto-following.");
        }

        private sealed class AutoFollowTarget : Target
        {
            public AutoFollowTarget() : base(12, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                PlayerMobile player = from as PlayerMobile;
                Mobile mobile = targeted as Mobile;
                if (player == null)
                    return;

                if (mobile == null || mobile.Deleted || mobile == from)
                {
                    from.SendMessage("That is not a valid follow target.");
                    return;
                }

                StartFollowing(player, mobile);
            }
        }

        private sealed class AutoFollowState
        {
            private readonly PlayerMobile _from;
            private readonly Mobile _target;
            private readonly QuestArrow _arrow;
            private readonly Timer _timer;
            private PathFollower _pathFollower;

            public AutoFollowState(PlayerMobile from, Mobile target)
            {
                _from = from;
                _target = target;
                _arrow = new Server.SkillHandlers.TrackArrow(from, target, 24);
                _pathFollower = new PathFollower(from, target);
                _timer = Timer.DelayCall(TimeSpan.FromSeconds(0.25), TimeSpan.FromSeconds(0.25), OnTick);
            }

            public void Start()
            {
                _from.QuestArrow = _arrow;
            }

            public void Stop()
            {
                if (_from != null && _from.QuestArrow == _arrow)
                    _from.QuestArrow = null;

                if (_arrow != null && _arrow.Running)
                    _arrow.Stop();

                if (_timer != null && _timer.Running)
                    _timer.Stop();
            }

            private void OnTick()
            {
                if (_from == null || _from.Deleted || _from.NetState == null || _target == null || _target.Deleted || _from.Map != _target.Map)
                {
                    StopFollowing(_from, true);
                    return;
                }

                if (_pathFollower == null)
                    _pathFollower = new PathFollower(_from, _target);

                if (_from.InRange(_target, 1))
                    return;

                _pathFollower.ForceRepath();
                _pathFollower.Follow(true, 1);
            }
        }
    }
}
