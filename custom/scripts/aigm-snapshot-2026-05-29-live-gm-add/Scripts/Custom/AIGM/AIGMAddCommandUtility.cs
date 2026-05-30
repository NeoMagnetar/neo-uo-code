using System;
using System.Reflection;
using Server.Commands;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public sealed class AIGMAddCreateResult
    {
        public bool Ok { get; private set; }
        public string Message { get; private set; }
        public Serial CreatedSerial { get; private set; }
        public string CreatedTypeName { get; private set; }

        private AIGMAddCreateResult(bool ok, string message, Serial createdSerial, string createdTypeName)
        {
            Ok = ok;
            Message = message;
            CreatedSerial = createdSerial;
            CreatedTypeName = createdTypeName;
        }

        public static AIGMAddCreateResult Success(string message, Serial serial, string createdTypeName)
        {
            return new AIGMAddCreateResult(true, message, serial, createdTypeName);
        }

        public static AIGMAddCreateResult Fail(string message)
        {
            return new AIGMAddCreateResult(false, message, Serial.Zero, String.Empty);
        }
    }

    public static class AIGMAddCommandUtility
    {
        public static AIGMAddCreateResult TryCreateAndPlace(Mobile requester, AIGMConstructableResolution resolution, int amount, Point3D location, Map map)
        {
            if (requester == null || requester.Deleted)
                return AIGMAddCreateResult.Fail("Requester is missing or deleted.");

            if (map == null || map == Map.Internal)
                return AIGMAddCreateResult.Fail("Invalid map.");

            if (resolution == null || !resolution.Success || resolution.ResolvedType == null)
                return AIGMAddCreateResult.Fail("Missing or invalid constructable resolution.");

            if (!resolution.IsItem)
                return AIGMAddCreateResult.Fail("Resolved type is not an item: " + resolution.CanonicalTypeName);

            ConstructorInfo[] ctors = resolution.ResolvedType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

            for (int i = 0; i < ctors.Length; ++i)
            {
                ConstructorInfo ctor = ctors[i];
                if (!Add.IsConstructable(ctor, requester.AccessLevel))
                    continue;

                Item item = TryBuildItem(requester, ctor, amount);
                if (item == null)
                    continue;

                TryApplyAmount(item, amount);
                item.MoveToWorld(location, map);

                return AIGMAddCreateResult.Success(
                    String.Format("Created {0} at {1},{2},{3}.", item.GetType().Name, location.X, location.Y, location.Z),
                    item.Serial,
                    item.GetType().Name);
            }

            return AIGMAddCreateResult.Fail("No supported native Add path succeeded for type: " + resolution.CanonicalTypeName);
        }

        public static AIGMAddCreateResult TryCreateAndPlaceMobile(Mobile requester, AIGMConstructableResolution resolution, int amount, Point3D location, Map map)
        {
            if (requester == null || requester.Deleted)
                return AIGMAddCreateResult.Fail("Requester is missing or deleted.");

            if (map == null || map == Map.Internal)
                return AIGMAddCreateResult.Fail("Invalid map.");

            if (resolution == null || !resolution.Success || resolution.ResolvedType == null)
                return AIGMAddCreateResult.Fail("Missing or invalid constructable resolution.");

            if (!resolution.IsMobile)
                return AIGMAddCreateResult.Fail("Resolved type is not a mobile: " + resolution.CanonicalTypeName);

            if (amount < 1)
                amount = 1;

            Mobile first = null;
            int created = 0;

            for (int count = 0; count < amount; count++)
            {
                Mobile mob = TryBuildMobile(requester, resolution.ResolvedType);
                if (mob == null)
                    continue;

                mob.MoveToWorld(location, map);
                if (first == null)
                    first = mob;

                created++;
            }

            if (first == null || created < 1)
                return AIGMAddCreateResult.Fail("No supported native Add path succeeded for mobile type: " + resolution.CanonicalTypeName);

            return AIGMAddCreateResult.Success(
                String.Format("Created {0} {1} at {2},{3},{4}.", created, first.GetType().Name, location.X, location.Y, location.Z),
                first.Serial,
                first.GetType().Name);
        }

        private static Item TryBuildItem(Mobile requester, ConstructorInfo ctor, int amount)
        {
            ParameterInfo[] paramList = ctor.GetParameters();
            string[] ctorArgs = null;

            if (paramList.Length == 0)
            {
                ctorArgs = new string[0];
            }
            else if (paramList.Length == 1 && paramList[0].ParameterType == typeof(int))
            {
                ctorArgs = new[] { amount.ToString() };
            }
            else
            {
                return null;
            }

            object[] values = Add.ParseValues(paramList, ctorArgs);
            if (values == null)
                return null;

            bool sendError = false;
            IEntity built = Add.Build(requester, ctor, values, null, null, ref sendError);
            return built as Item;
        }

        private static Mobile TryBuildMobile(Mobile requester, Type type)
        {
            if (type == null)
                return null;

            ConstructorInfo[] ctors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < ctors.Length; i++)
            {
                ConstructorInfo ctor = ctors[i];
                if (!Add.IsConstructable(ctor, requester.AccessLevel))
                    continue;

                ParameterInfo[] paramList = ctor.GetParameters();
                if (paramList.Length != 0)
                    continue;

                object[] values = Add.ParseValues(paramList, new string[0]);
                if (values == null)
                    continue;

                bool sendError = false;
                IEntity built = Add.Build(requester, ctor, values, null, null, ref sendError);
                Mobile mob = built as Mobile;
                if (mob != null)
                    return mob;
            }

            return null;
        }

        private static void TryApplyAmount(Item item, int amount)
        {
            if (item == null || amount <= 1)
                return;

            try
            {
                if (item.Stackable)
                    item.Amount = amount;
            }
            catch
            {
            }
        }
    }
}
