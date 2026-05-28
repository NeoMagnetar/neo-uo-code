using System;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Commands
{
    public static class AIGMCommand
    {
        public static void Initialize()
        {
            CommandSystem.Register("AIGM", AccessLevel.GameMaster, new CommandEventHandler(AIGM_OnCommand));
        }

        [Usage("AIGM")]
        [Description("Places an AI GM counselor at the targeted location.")]
        private static void AIGM_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target where you want to place the AI GM counselor.");
            e.Mobile.Target = new AIGMTarget();
        }

        private class AIGMTarget : Target
        {
            public AIGMTarget()
                : base(-1, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                IPoint3D p = targeted as IPoint3D;
                if (p == null)
                {
                    from.SendMessage("That is not a valid placement target.");
                    return;
                }

                if (p is Item)
                    p = ((Item)p).GetWorldTop();
                else if (p is Mobile)
                    p = ((Mobile)p).Location;

                Point3D loc = new Point3D(p);
                Map map = from.Map;

                AIGMCounselor counselor = new AIGMCounselor();
                counselor.MoveToWorld(loc, map);
                from.SendMessage("AI GM counselor placed.");
            }
        }
    }
}
