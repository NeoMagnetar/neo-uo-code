using System;
using Server.Items;
using Server.Mobiles;
using Server.Multis;

namespace Server.Custom.AIGM
{
    public sealed class AIGMAddPolicyResult
    {
        public bool Ok;
        public string Message;
        public int SanitizedAmount;

        public static AIGMAddPolicyResult Success(int amount)
        {
            return new AIGMAddPolicyResult { Ok = true, SanitizedAmount = amount, Message = String.Empty };
        }

        public static AIGMAddPolicyResult Fail(string message)
        {
            return new AIGMAddPolicyResult { Ok = false, SanitizedAmount = 0, Message = message ?? "Policy rejected request." };
        }
    }

    public static class AIGMAddPolicy
    {
        public static AIGMAddPolicyResult ValidateWorldItemAdd(Mobile requester, AIGMConstructableResolution resolution, int amount)
        {
            if (requester == null || requester.Deleted)
                return AIGMAddPolicyResult.Fail("Requester is missing or deleted.");

            if (requester.AccessLevel < AccessLevel.GameMaster)
                return AIGMAddPolicyResult.Fail("Requester does not have permission to create world items.");

            if (requester.Map == null || requester.Map == Map.Internal)
                return AIGMAddPolicyResult.Fail("Requester is not on a valid map.");

            if (resolution == null || !resolution.Success || !resolution.IsItem)
                return AIGMAddPolicyResult.Fail("Requested type is not a constructable item.");

            if (amount < 1)
                amount = 1;

            if (amount > 60000)
                amount = 60000;

            if (typeof(BaseMulti).IsAssignableFrom(resolution.ResolvedType))
                return AIGMAddPolicyResult.Fail("World add policy blocks direct multi placement from this action path.");

            return AIGMAddPolicyResult.Success(amount);
        }

        public static AIGMAddPolicyResult ValidateWorldMobileAdd(Mobile requester, AIGMConstructableResolution resolution, int amount)
        {
            if (requester == null || requester.Deleted)
                return AIGMAddPolicyResult.Fail("Requester is missing or deleted.");

            if (requester.AccessLevel < AccessLevel.GameMaster)
                return AIGMAddPolicyResult.Fail("Requester does not have permission to create world mobiles.");

            if (requester.Map == null || requester.Map == Map.Internal)
                return AIGMAddPolicyResult.Fail("Requester is not on a valid map.");

            if (resolution == null || !resolution.Success || !resolution.IsMobile)
                return AIGMAddPolicyResult.Fail("Requested type is not a constructable mobile.");

            if (amount < 1)
                amount = 1;

            if (amount > 25)
                amount = 25;

            return AIGMAddPolicyResult.Success(amount);
        }
    }
}
