using System;
using Server.Custom.AIGM.UMG;

namespace Server.Commands
{
    public static class AIGMUMGOperationalLayoutProofCommand
    {
        public static void Initialize()
        {
            CommandSystem.Register("umglayoutproof", AccessLevel.GameMaster, OnCommand);
            Timer.DelayCall(TimeSpan.FromSeconds(45.0), RunQueuedProof);
        }

        private static void OnCommand(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            try
            {
                string report = AIGMUMGOperationalLayoutService.RunPhase64D1DProof("gm_command");
                e.Mobile.SendMessage(68, "Phase64D1D organizer proof complete: {0}", report);
            }
            catch (Exception ex)
            {
                e.Mobile.SendMessage(38, "Phase64D1D organizer proof failed: {0}", ex.Message);
            }
        }

        private static void RunQueuedProof()
        {
            AIGMUMGOperationalLayoutService.RunQueuedPhase64D1DProofIfRequested();
        }
    }
}
