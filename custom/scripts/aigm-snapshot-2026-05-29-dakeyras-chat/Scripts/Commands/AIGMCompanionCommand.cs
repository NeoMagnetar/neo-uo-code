using System;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Commands
{
    public static class AIGMCompanionCommand
    {
        public static void Initialize()
        {
            CommandSystem.Register("AIGMCompanion", AccessLevel.GameMaster, new CommandEventHandler(OnCommand));
            CommandSystem.Register("Dakeyras", AccessLevel.GameMaster, new CommandEventHandler(OnCommand));
        }

        [Usage("AIGMCompanion")]
        [Description("Places Dakeyras, the AI companion, at the targeted location.")]
        private static void OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target where you want to place Dakeyras.");
            e.Mobile.Target = new DakeyrasTarget();
        }

        private class DakeyrasTarget : Target
        {
            public DakeyrasTarget()
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

                AIGMCompanionDakeyras dakeyras = new AIGMCompanionDakeyras();
                dakeyras.MoveToWorld(loc, map);
                from.SendMessage("Dakeyras has been placed.");
            }
        }
    }
}
