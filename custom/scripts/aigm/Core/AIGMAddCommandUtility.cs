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

        private AIGMAddCreateResult(bool ok, string message, Serial createdSerial)
        {
            Ok = ok;
            Message = message;
            CreatedSerial = createdSerial;
        }

        public static AIGMAddCreateResult Success(string message, Serial serial)
        {
            return new AIGMAddCreateResult(true, message, serial);
        }

        public static AIGMAddCreateResult Fail(string message)
        {
            return new AIGMAddCreateResult(false, message, Serial.Zero);
        }
    }

    public static class AIGMAddCommandUtility
    {
        public static AIGMAddCreateResult TryCreateAndPlace(Mobile requester, string typeName, int amount, Point3D location, Map map)
        {
            if (requester == null || requester.Deleted)
                return AIGMAddCreateResult.Fail("Requester is missing or deleted.");

            if (map == null || map == Map.Internal)
                return AIGMAddCreateResult.Fail("Invalid map.");

            if (String.IsNullOrWhiteSpace(typeName))
                return AIGMAddCreateResult.Fail("Missing type name.");

            Type type = ScriptCompiler.FindTypeByName(typeName);
            if (type != null && typeof(Item).IsAssignableFrom(type))
            {
                ConstructorInfo[] ctors = type.GetConstructors();
                string[] args = new string[] { amount.ToString() };

                for (int i = 0; i < ctors.Length; ++i)
                {
                    ConstructorInfo ctor = ctors[i];
                    if (!Add.IsConstructable(ctor, requester.AccessLevel))
                        continue;

                    ParameterInfo[] paramList = ctor.GetParameters();
                    string[] ctorArgs = paramList.Length == 0 ? new string[0] : args;
                    if (paramList.Length != ctorArgs.Length)
                        continue;

                    object[] values = Add.ParseValues(paramList, ctorArgs);
                    if (values == null)
                        continue;

                    bool sendError = false;
                    IEntity built = Add.Build(requester, ctor, values, null, null, ref sendError);
                    Item item = built as Item;
                    if (item == null)
                        continue;

                    item.MoveToWorld(location, map);
                    return AIGMAddCreateResult.Success(
                        String.Format("Created {0} at {1},{2},{3}.", item.GetType().Name, location.X, location.Y, location.Z),
                        item.Serial);
                }
            }

            Item fallback = CreateAllowedFallback(typeName, amount);
            if (fallback != null)
            {
                fallback.MoveToWorld(location, map);
                AIGMExecutionLog.Write("TEMP_SAFE_ITEM_UTILITY type={0} amount={1} serial={2} x={3} y={4} z={5}", typeName, amount, fallback.Serial, location.X, location.Y, location.Z);
                return AIGMAddCreateResult.Success(
                    String.Format("Created {0} {1} at {2},{3},{4}.", amount, typeName, location.X, location.Y, location.Z),
                    fallback.Serial);
            }

            return AIGMAddCreateResult.Fail("No supported native Add path succeeded for type: " + typeName);
        }

        private static Item CreateAllowedFallback(string typeName, int amount)
        {
            if (typeName.Equals("Bandage", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    return new Bandage(amount);
                }
                catch
                {
                    Bandage bandage = new Bandage();
                    bandage.Amount = amount;
                    return bandage;
                }
            }

            if (typeName.Equals("Scissors", StringComparison.OrdinalIgnoreCase))
                return new Scissors();

            if (typeName.Equals("Torch", StringComparison.OrdinalIgnoreCase))
                return new Torch();

            if (typeName.Equals("Apple", StringComparison.OrdinalIgnoreCase))
                return new Apple();

            if (typeName.Equals("Katana", StringComparison.OrdinalIgnoreCase))
                return new Katana();

            return null;
        }
    }
}
