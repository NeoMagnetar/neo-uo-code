using System;
using Server.Items;
using Server.Mobiles;

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

        public static AIGMExecutionResult AddContainerItem(Mobile requester, Mobile counselor, AIGMActionProposal action)
        {
            AIGMExecutionLog.Write(
                "NATIVE_ADD_CONTAINER_START requester={0} counselor={1}",
                SafeName(requester),
                SafeName(counselor));

            if (action == null)
                return Fail("Action is missing.");

            string requestedTypeName = action.GetParameter("typeName", String.Empty);
            int requestedAmount = action.GetIntParameter("amount", 1);
            string targetKind = action.GetParameter("targetContainerKind", "counselor_pack");

            AIGMConstructableResolution resolution = AIGMConstructableResolver.Resolve(requestedTypeName, AIGMConstructableKind.Any);
            if (resolution == null || !resolution.Success)
                return Fail(resolution == null ? "Constructable resolver returned null." : resolution.FailureReason);

            if (!resolution.IsItem)
                return Fail("Resolved type is not an item: " + resolution.CanonicalTypeName);

            AIGMAddPolicyResult policy = AIGMAddPolicy.ValidateWorldItemAdd(requester, resolution, requestedAmount);
            if (policy == null || !policy.Ok)
                return Fail(policy == null ? "Add policy returned null." : policy.Message);

            Container targetContainer = ResolveTargetContainer(requester, counselor, targetKind);
            if (targetContainer == null)
                return Fail("Target container could not be resolved for kind: " + targetKind);

            AIGMAddCreateResult createResult = AIGMAddCommandUtility.TryCreateAndPlaceInContainer(
                requester,
                resolution,
                policy.SanitizedAmount,
                targetContainer);

            if (createResult == null || !createResult.Ok)
                return Fail(createResult == null ? "Native Add container utility returned null." : createResult.Message);

            string createdTypeName = !String.IsNullOrWhiteSpace(createResult.CreatedTypeName)
                ? createResult.CreatedTypeName
                : resolution.CanonicalTypeName;

            AIGMExecutionLog.Write(
                "NATIVE_ADD_CONTAINER_SUCCESS requestedType={0} resolvedType={1} amount={2} serial={3} target={4}",
                requestedTypeName,
                createdTypeName,
                policy.SanitizedAmount,
                createResult.CreatedSerial,
                targetKind);

            requester.SendMessage(68, "AIGM Add created {0} {1} in {2}.", policy.SanitizedAmount, createdTypeName, targetKind);

            return AIGMExecutionResult.Success(
                String.Format("Created {0} {1} in {2}.", policy.SanitizedAmount, createdTypeName, targetKind),
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

        private static Container ResolveTargetContainer(Mobile requester, Mobile counselor, string targetKind)
        {
            if (String.Equals(targetKind, "requester_backpack", StringComparison.OrdinalIgnoreCase))
                return requester != null ? requester.Backpack : null;

            if (String.Equals(targetKind, "counselor_pack", StringComparison.OrdinalIgnoreCase))
            {
                AIGMCounselor aiCounselor = counselor as AIGMCounselor;
                if (aiCounselor == null && requester != null && requester.Map != null)
                {
                    IPooledEnumerable mobiles = requester.Map.GetMobilesInRange(requester.Location, 48);
                    foreach (Mobile mob in mobiles)
                    {
                        aiCounselor = mob as AIGMCounselor;
                        if (aiCounselor != null && !aiCounselor.Deleted)
                            break;
                    }
                    mobiles.Free();
                }

                IAIGMInventoryCapability inventory = aiCounselor != null ? aiCounselor.Inventory : null;
                return inventory != null ? inventory.GetPrimaryContainer() : null;
            }

            return null;
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
