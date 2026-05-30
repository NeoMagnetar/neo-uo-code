using System;

namespace Server.Custom.AIGM
{
    public static class AIGMNativeAddAdapter
    {
        public static AIGMExecutionResult AddWorldItem(Mobile requester, Mobile counselor, AIGMActionProposal action)
        {
            AIGMExecutionLog.Write(
                "NATIVE_ADD_START requester={0} counselor={1}",
                SafeName(requester),
                SafeName(counselor));

            if (requester == null || requester.Deleted)
                return Fail("Requester is missing or deleted.");

            if (requester.AccessLevel < AccessLevel.GameMaster)
                return Fail("Requester does not have permission to use AIGM Add actions.");

            if (requester.Map == null || requester.Map == Map.Internal)
                return Fail("Requester is not on a valid map.");

            if (action == null)
                return Fail("Action is missing.");

            string typeName = action.GetParameter("typeName", "Bandage");
            int amount = action.GetIntParameter("amount", 1);

            if (amount < 1)
                amount = 1;

            if (amount > 100)
                amount = 100;

            if (!IsAllowedProofType(typeName))
                return Fail("Type is not allowed for this proof lane: " + typeName);

            Point3D location = requester.Location;
            Map map = requester.Map;

            AIGMAddCreateResult createResult = AIGMAddCommandUtility.TryCreateAndPlace(
                requester,
                typeName,
                amount,
                location,
                map);

            if (createResult == null || !createResult.Ok)
                return Fail(createResult == null ? "Native Add utility returned null." : createResult.Message);

            AIGMExecutionLog.Write(
                "NATIVE_ADD_SUCCESS type={0} amount={1} serial={2} map={3} x={4} y={5} z={6}",
                typeName,
                amount,
                createResult.CreatedSerial,
                map,
                location.X,
                location.Y,
                location.Z);

            requester.SendMessage(68, "AIGM Add created {0} {1} at your feet.", amount, typeName);

            return AIGMExecutionResult.Success(
                String.Format("Created {0} {1} at {2},{3},{4}.", amount, typeName, location.X, location.Y, location.Z),
                createResult.CreatedSerial);
        }

        private static bool IsAllowedProofType(string typeName)
        {
            if (typeName == null)
                return false;

            return typeName.Equals("Bandage", StringComparison.OrdinalIgnoreCase)
                || typeName.Equals("Scissors", StringComparison.OrdinalIgnoreCase)
                || typeName.Equals("Torch", StringComparison.OrdinalIgnoreCase)
                || typeName.Equals("Apple", StringComparison.OrdinalIgnoreCase)
                || typeName.Equals("Katana", StringComparison.OrdinalIgnoreCase);
        }

        private static AIGMExecutionResult Fail(string message)
        {
            AIGMExecutionLog.Write("NATIVE_ADD_FAIL {0}", message);
            return AIGMExecutionResult.Fail(message);
        }

        private static string SafeName(Mobile m)
        {
            if (m == null)
                return "(null)";

            return String.Format("{0}/{1}", m.Name, m.Serial);
        }
    }
}
