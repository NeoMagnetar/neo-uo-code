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

            if (action == null)
                return Fail("Action is missing.");

            string requestedTypeName = action.GetParameter("typeName", String.Empty);
            int requestedAmount = action.GetIntParameter("amount", 1);

            AIGMConstructableResolution resolution = AIGMConstructableResolver.Resolve(requestedTypeName, AIGMConstructableKind.Any);
            if (resolution == null || !resolution.Success)
                return Fail(resolution == null ? "Constructable resolver returned null." : resolution.FailureReason);

            if (!resolution.IsItem)
            {
                if (resolution.IsMobile)
                    return Fail("Resolved mobile types are not executable through gm_add_world_item: " + resolution.CanonicalTypeName);

                return Fail("Resolved type is not an item: " + resolution.CanonicalTypeName);
            }

            AIGMAddPolicyResult policy = AIGMAddPolicy.ValidateWorldItemAdd(requester, resolution, requestedAmount);
            if (policy == null || !policy.Ok)
                return Fail(policy == null ? "Add policy returned null." : policy.Message);

            Point3D location = requester.Location;
            Map map = requester.Map;

            AIGMAddCreateResult createResult = AIGMAddCommandUtility.TryCreateAndPlace(
                requester,
                resolution,
                policy.SanitizedAmount,
                location,
                map);

            if (createResult == null || !createResult.Ok)
                return Fail(createResult == null ? "Native Add utility returned null." : createResult.Message);

            string createdTypeName = !String.IsNullOrWhiteSpace(createResult.CreatedTypeName)
                ? createResult.CreatedTypeName
                : resolution.CanonicalTypeName;

            AIGMExecutionLog.Write(
                "NATIVE_ADD_SUCCESS requestedType={0} resolvedType={1} amount={2} serial={3} map={4} x={5} y={6} z={7}",
                requestedTypeName,
                createdTypeName,
                policy.SanitizedAmount,
                createResult.CreatedSerial,
                map,
                location.X,
                location.Y,
                location.Z);

            requester.SendMessage(68, "AIGM Add created {0} {1} at your feet.", policy.SanitizedAmount, createdTypeName);

            return AIGMExecutionResult.Success(
                String.Format("Created {0} {1} at {2},{3},{4}.", policy.SanitizedAmount, createdTypeName, location.X, location.Y, location.Z),
                createResult.CreatedSerial);
        }

        public static AIGMExecutionResult AddWorldMobile(Mobile requester, Mobile counselor, AIGMActionProposal action)
        {
            AIGMExecutionLog.Write(
                "NATIVE_ADD_MOBILE_START requester={0} counselor={1}",
                SafeName(requester),
                SafeName(counselor));

            if (action == null)
                return Fail("Action is missing.");

            string requestedTypeName = action.GetParameter("typeName", String.Empty);
            int requestedAmount = action.GetIntParameter("amount", 1);

            AIGMConstructableResolution resolution = AIGMConstructableResolver.Resolve(requestedTypeName, AIGMConstructableKind.Any);
            if (resolution == null || !resolution.Success)
                return Fail(resolution == null ? "Constructable resolver returned null." : resolution.FailureReason);

            if (!resolution.IsMobile)
            {
                if (resolution.IsItem)
                    return Fail("Resolved item types are not executable through gm_add_world_mobile: " + resolution.CanonicalTypeName);

                return Fail("Resolved type is not a mobile: " + resolution.CanonicalTypeName);
            }

            AIGMAddPolicyResult policy = AIGMAddPolicy.ValidateWorldMobileAdd(requester, resolution, requestedAmount);
            if (policy == null || !policy.Ok)
                return Fail(policy == null ? "Add policy returned null." : policy.Message);

            Point3D location = requester.Location;
            Map map = requester.Map;

            AIGMAddCreateResult createResult = AIGMAddCommandUtility.TryCreateAndPlaceMobile(
                requester,
                resolution,
                policy.SanitizedAmount,
                location,
                map);

            if (createResult == null || !createResult.Ok)
                return Fail(createResult == null ? "Native Add mobile utility returned null." : createResult.Message);

            string createdTypeName = !String.IsNullOrWhiteSpace(createResult.CreatedTypeName)
                ? createResult.CreatedTypeName
                : resolution.CanonicalTypeName;

            AIGMExecutionLog.Write(
                "NATIVE_ADD_MOBILE_SUCCESS requestedType={0} resolvedType={1} amount={2} serial={3} map={4} x={5} y={6} z={7}",
                requestedTypeName,
                createdTypeName,
                policy.SanitizedAmount,
                createResult.CreatedSerial,
                map,
                location.X,
                location.Y,
                location.Z);

            requester.SendMessage(68, "AIGM Add created {0} {1} at your feet.", policy.SanitizedAmount, createdTypeName);

            return AIGMExecutionResult.Success(
                String.Format("Created {0} {1} at {2},{3},{4}.", policy.SanitizedAmount, createdTypeName, location.X, location.Y, location.Z),
                createResult.CreatedSerial);
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
