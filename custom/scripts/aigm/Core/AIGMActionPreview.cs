using System;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMActionPreview
    {
        public static string BuildTargetSummary(AIGMActionProposal action)
        {
            if (action == null || action.Parameters == null)
                return "No target details available.";

            string rawSerial;
            if (!action.Parameters.TryGetValue("targetSerial", out rawSerial) || String.IsNullOrWhiteSpace(rawSerial))
                return "No target details available.";

            int serial;
            if (!Int32.TryParse(rawSerial, out serial))
                return "Target serial invalid.";

            IEntity ent = World.FindEntity(serial);
            if (ent == null)
                return "Target entity not found.";

            Item item = ent as Item;
            if (item != null)
                return String.Format("Item: {0} [{1}] at {2},{3},{4}", item.Name ?? item.GetType().Name, item.GetType().Name, item.Location.X, item.Location.Y, item.Location.Z);

            Mobile mob = ent as Mobile;
            if (mob != null)
                return String.Format("Mobile: {0} [{1}] at {2},{3},{4}", mob.Name ?? mob.GetType().Name, mob.GetType().Name, mob.Location.X, mob.Location.Y, mob.Location.Z);

            return String.Format("Entity serial: {0}", serial);
        }

        public static string BuildMutationWarning(AIGMActionProposal action)
        {
            if (action == null)
                return "This action may change world or shard state.";

            if (action.ActionKind == "run_gm_command" && action.Parameters != null)
            {
                string commandName;
                if (action.Parameters.TryGetValue("commandName", out commandName))
                {
                    switch (commandName)
                    {
                        case AIGMCommandAction.RestockVendor:
                            return "This will immediately refresh vendor stock on the live shard and can affect what players see or buy.";
                        case AIGMCommandAction.SpawnTestCopy:
                            return "This will create a duplicate test item near your GM character and add a new world object to the shard.";
                        case AIGMCommandAction.FollowMobile:
                        case AIGMCommandAction.StopFollowing:
                        case AIGMCommandAction.PathToCoordinates:
                        case AIGMCommandAction.PathToNamedLocation:
                        case AIGMCommandAction.SetArrivalAction:
                        case AIGMCommandAction.PauseMovement:
                        case AIGMCommandAction.ResumeMovement:
                        case AIGMCommandAction.CancelMovement:
                        case AIGMCommandAction.PathToCurrentTarget:
                        case AIGMCommandAction.FollowCurrentTarget:
                            return "This will change the counselor's movement state in the live world.";
                    }
                }
            }

            return "This action may change world or shard state.";
        }

        public static string BuildPreviewText(AIGMActionProposal action)
        {
            if (action == null)
                return "No preview supplied.";

            if (!String.IsNullOrWhiteSpace(action.PreviewText))
                return action.PreviewText;

            if (action.ActionKind == "run_gm_command" && action.Parameters != null)
            {
                string commandName;
                if (action.Parameters.TryGetValue("commandName", out commandName))
                {
                    switch (commandName)
                    {
                        case AIGMCommandAction.RestockVendor:
                            return "Refreshes the selected vendor's available stock immediately.";
                        case AIGMCommandAction.SpawnTestCopy:
                            return "Duplicates the selected item and places the copy at your current location.";
                        case AIGMCommandAction.FollowMobile:
                            return "Puts the counselor into follow mode for the chosen mobile.";
                        case AIGMCommandAction.StopFollowing:
                            return "Stops the counselor's active follow or travel behavior.";
                        case AIGMCommandAction.PathToCoordinates:
                            return "Walks the counselor toward the requested map coordinates.";
                        case AIGMCommandAction.PathToNamedLocation:
                            return "Walks the counselor toward a known named destination such as a town or bank.";
                        case AIGMCommandAction.MovementStatus:
                            return "Reports the counselor's current movement mode and destination.";
                        case AIGMCommandAction.SetArrivalAction:
                            return "Configures what the counselor should do automatically after arriving.";
                        case AIGMCommandAction.PauseMovement:
                            return "Pauses the counselor's current route without discarding it.";
                        case AIGMCommandAction.ResumeMovement:
                            return "Resumes the counselor's paused route or follow behavior.";
                        case AIGMCommandAction.CancelMovement:
                            return "Cancels the counselor's current route or paused movement plan.";
                        case AIGMCommandAction.PathToCurrentTarget:
                            return "Moves the counselor toward the currently selected AI GM target.";
                        case AIGMCommandAction.FollowCurrentTarget:
                            return "Puts the counselor into follow mode for the currently selected AI GM target.";
                    }
                }
            }

            return "No preview supplied.";
        }
    }
}
