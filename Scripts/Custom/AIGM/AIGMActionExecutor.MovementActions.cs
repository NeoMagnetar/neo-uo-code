using System;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static partial class AIGMActionExecutor
    {
        private static bool ExecuteFollowRequester(Mobile from, AIGMActionProposal action, out string message)
        {
            AIGMActionProposal mapped = new AIGMActionProposal();
            mapped.ActionKind = "run_gm_command";
            mapped.EnsureParameters();
            mapped.Parameters["commandName"] = AIGMCommandAction.FollowMobile;
            mapped.Parameters["targetKind"] = "requester";
            return ExecuteRunGmCommand(from, mapped, out message);
        }

        private static bool ExecuteStopFollowCanonical(Mobile from, AIGMActionProposal action, out string message)
        {
            AIGMActionProposal mapped = new AIGMActionProposal();
            mapped.ActionKind = "run_gm_command";
            mapped.EnsureParameters();
            mapped.Parameters["commandName"] = AIGMCommandAction.StopFollowing;
            return ExecuteRunGmCommand(from, mapped, out message);
        }

        private static bool ExecuteResumeFollowCanonical(Mobile from, AIGMActionProposal action, out string message)
        {
            AIGMActionProposal mapped = new AIGMActionProposal();
            mapped.ActionKind = "run_gm_command";
            mapped.EnsureParameters();
            mapped.Parameters["commandName"] = AIGMCommandAction.ResumeMovement;
            return ExecuteRunGmCommand(from, mapped, out message);
        }

        private static bool ExecuteGoToRequester(Mobile from, AIGMActionProposal action, out string message)
        {
            AIGMActionProposal mapped = new AIGMActionProposal();
            mapped.ActionKind = "run_gm_command";
            mapped.EnsureParameters();
            mapped.Parameters["commandName"] = AIGMCommandAction.GoToCoordinates;
            mapped.Parameters["x"] = from.X.ToString();
            mapped.Parameters["y"] = from.Y.ToString();
            mapped.Parameters["z"] = from.Z.ToString();
            return ExecuteRunGmCommand(from, mapped, out message);
        }

        private static bool ExecuteGotoCoordinates(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            string rawX, rawY, rawZ;
            if (action.Parameters == null || !action.Parameters.TryGetValue("x", out rawX) || !action.Parameters.TryGetValue("y", out rawY))
            {
                message = "No target coordinates were provided.";
                return false;
            }

            int x, y, z;
            if (!Int32.TryParse(rawX, out x) || !Int32.TryParse(rawY, out y))
            {
                message = "Target coordinates were invalid.";
                return false;
            }

            z = 0;
            if (action.Parameters.TryGetValue("z", out rawZ))
                Int32.TryParse(rawZ, out z);

            from.Location = new Point3D(x, y, z);
            from.ProcessDelta();
            message = String.Format("Teleported to coordinates {0},{1},{2}.", x, y, z);
            return true;
        }

        private static bool ExecuteFollowMobile(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            AIGMCounselor counselor = FindCounselor(from, 24);
            if (counselor == null)
            {
                message = "No nearby AI GM counselor was found to follow you.";
                return false;
            }

            Mobile target = from;
            string rawSerial;
            if (action.Parameters != null && action.Parameters.TryGetValue("targetSerial", out rawSerial) && !String.IsNullOrWhiteSpace(rawSerial))
            {
                int serial;
                if (TryParseSerial(rawSerial, out serial))
                {
                    Mobile explicitTarget = World.FindMobile(serial);
                    if (explicitTarget != null && !explicitTarget.Deleted)
                        target = explicitTarget;
                }
            }

            message = "Movement follow remains unavailable through the current vendor-backed counselor shell.";
            return false;
        }

        private static bool ExecuteStopFollowing(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            AIGMCounselor counselor = FindCounselor(from, 48);
            if (counselor == null)
            {
                message = "No nearby AI GM counselor was found to stop.";
                return false;
            }

            message = "Movement stop remains unavailable through the current vendor-backed counselor shell.";
            return false;
        }

        private static bool ExecutePathToCoordinates(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            AIGMCounselor counselor = FindCounselor(from, 48);
            if (counselor == null)
            {
                message = "No nearby AI GM counselor was found to move.";
                return false;
            }

            string rawX, rawY, rawZ;
            if (action.Parameters == null || !action.Parameters.TryGetValue("x", out rawX) || !action.Parameters.TryGetValue("y", out rawY))
            {
                message = "No path destination coordinates were provided.";
                return false;
            }

            int x, y, z;
            if (!Int32.TryParse(rawX, out x) || !Int32.TryParse(rawY, out y))
            {
                message = "Path destination coordinates were invalid.";
                return false;
            }

            z = from.Map != null ? from.Map.GetAverageZ(x, y) : 0;
            if (action.Parameters.TryGetValue("z", out rawZ))
                Int32.TryParse(rawZ, out z);

            message = "Movement pathing remains unavailable through the current vendor-backed counselor shell.";
            return false;
        }

        private static bool ExecutePathToNamedLocation(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            AIGMCounselor counselor = FindCounselor(from, 48);
            if (counselor == null)
            {
                message = "No nearby AI GM counselor was found to move.";
                return false;
            }

            string rawName;
            if (action.Parameters == null || !action.Parameters.TryGetValue("destinationName", out rawName) || String.IsNullOrWhiteSpace(rawName))
            {
                message = "No named destination was provided.";
                return false;
            }

            AIGMNamedDestination destination;
            if (!AIGMMovementController.TryResolveNamedDestination(rawName, counselor.Map, out destination))
            {
                message = String.Format("I do not know the named destination '{0}' yet.", rawName);
                return false;
            }

            message = "Named pathing is temporarily unavailable while the counselor is being refactored onto vendor-backed actor mechanics.";
            return false;
        }

        private static bool ExecuteMovementStatus(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            AIGMMovementState state = AIGMMovementState.Get(from);
            if (state == null)
            {
                message = "The counselor has no recorded movement state.";
                return true;
            }

            BaseCreature counselor = World.FindMobile(state.CounselorSerial) as BaseCreature;
            string who = counselor != null ? (counselor.Name ?? "The counselor") : "The counselor";
            string where = state.DestinationName;
            if (String.IsNullOrWhiteSpace(where))
                where = String.Format("{0},{1},{2}", state.Destination.X, state.Destination.Y, state.Destination.Z);

            string arrival = String.IsNullOrWhiteSpace(state.ArrivalActionKind) ? "wait" : state.ArrivalActionKind;
            message = String.Format("{0} status: {1}. Destination: {2}. Arrival action: {3}.", who, state.LastStatus ?? state.Mode.ToString(), where, arrival);
            return true;
        }

        private static bool ExecuteSetArrivalAction(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            string arrivalAction = null;
            string arrivalArgument = null;
            if (action != null && action.Parameters != null)
            {
                action.Parameters.TryGetValue("arrivalAction", out arrivalAction);
                action.Parameters.TryGetValue("arrivalArgument", out arrivalArgument);
            }

            if (String.IsNullOrWhiteSpace(arrivalAction))
            {
                message = "No arrival action was provided.";
                return false;
            }

            AIGMMovementController.ConfigureArrivalAction(from, arrivalAction, arrivalArgument);
            message = String.Format("Arrival action set to {0}.", arrivalAction);
            return true;
        }

        private static bool ExecutePauseMovement(Mobile from, AIGMActionProposal action, out string message)
        {
            return AIGMMovementController.Pause(from, out message);
        }

        private static bool ExecuteResumeMovement(Mobile from, AIGMActionProposal action, out string message)
        {
            return AIGMMovementController.Resume(from, out message);
        }

        private static bool ExecuteCancelMovement(Mobile from, AIGMActionProposal action, out string message)
        {
            return AIGMMovementController.Cancel(from, out message);
        }

        private static bool ExecuteQueueNamedRouteStop(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            AIGMCounselor counselor = FindCounselor(from, 48);
            if (counselor == null)
            {
                message = "No nearby AI GM counselor was found for route chaining.";
                return false;
            }

            string rawName;
            if (action.Parameters == null || !action.Parameters.TryGetValue("destinationName", out rawName) || String.IsNullOrWhiteSpace(rawName))
            {
                message = "No queued destination name was provided.";
                return false;
            }

            AIGMNamedDestination destination;
            if (!AIGMMovementController.TryResolveNamedDestination(rawName, counselor.Map, out destination))
            {
                message = String.Format("I do not know the queued destination '{0}' yet.", rawName);
                return false;
            }

            AIGMMovementController.QueueNamedStop(from, destination.Name, destination.Location);
            message = String.Format("Queued next stop: {0}.", destination.Name);
            return true;
        }

        private static bool ExecuteQueueCoordinateRouteStop(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            string rawX, rawY, rawZ;
            if (action.Parameters == null || !action.Parameters.TryGetValue("x", out rawX) || !action.Parameters.TryGetValue("y", out rawY))
            {
                message = "No queued coordinates were provided.";
                return false;
            }

            int x, y, z;
            if (!Int32.TryParse(rawX, out x) || !Int32.TryParse(rawY, out y))
            {
                message = "Queued coordinates were invalid.";
                return false;
            }

            z = from.Map != null ? from.Map.GetAverageZ(x, y) : 0;
            if (action.Parameters.TryGetValue("z", out rawZ))
                Int32.TryParse(rawZ, out z);

            AIGMMovementController.QueueCoordinateStop(from, new Point3D(x, y, z));
            message = String.Format("Queued next coordinate stop: {0},{1},{2}.", x, y, z);
            return true;
        }

        private static bool ExecutePathToCurrentTarget(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            AIGMSessionState session = AIGMSessionState.Get(from);
            if (session == null || session.CurrentTarget == null)
            {
                message = "No current AI GM target is selected.";
                return false;
            }

            AIGMCounselor counselor = FindCounselor(from, 48);
            if (counselor == null)
            {
                message = "No nearby AI GM counselor was found to move.";
                return false;
            }

            Point3D destination = new Point3D(session.CurrentTarget.X, session.CurrentTarget.Y, session.CurrentTarget.Z);
            string destinationName = !String.IsNullOrWhiteSpace(session.CurrentTarget.Name) ? session.CurrentTarget.Name : session.CurrentTarget.TypeName;
            message = "Path-to-target is temporarily unavailable while the counselor is being refactored onto vendor-backed actor mechanics.";
            return false;
        }

        private static bool ExecuteFollowCurrentTarget(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            AIGMSessionState session = AIGMSessionState.Get(from);
            if (session == null || session.CurrentTarget == null)
            {
                message = "No current AI GM target is selected.";
                return false;
            }

            if (!String.Equals(session.CurrentTarget.Kind, "Mobile", StringComparison.OrdinalIgnoreCase))
            {
                message = "The current AI GM target is not a mobile and cannot be followed.";
                return false;
            }

            Mobile target = World.FindMobile(session.CurrentTarget.Serial);
            if (target == null || target.Deleted)
            {
                message = "The current target mobile could not be found.";
                return false;
            }

            AIGMCounselor counselor = FindCounselor(from, 48);
            if (counselor == null)
            {
                message = "No nearby AI GM counselor was found to move.";
                return false;
            }

            message = "Follow-current-target is temporarily unavailable while the counselor is being refactored onto vendor-backed actor mechanics.";
            return false;
        }

        private static AIGMCounselor FindCounselor(Mobile from, int range)
        {
            if (from == null || from.Map == null)
                return null;

            AIGMCounselor best = null;
            int bestDistance = Int32.MaxValue;
            IPooledEnumerable mobiles = from.Map.GetMobilesInRange(from.Location, range);
            foreach (Mobile mob in mobiles)
            {
                AIGMCounselor counselor = mob as AIGMCounselor;
                if (counselor == null || counselor.Deleted)
                    continue;

                int distance = (int)Math.Round(from.GetDistanceToSqrt(counselor.Location));
                if (distance < bestDistance)
                {
                    best = counselor;
                    bestDistance = distance;
                }
            }
            mobiles.Free();
            return best;
        }
    }
}
