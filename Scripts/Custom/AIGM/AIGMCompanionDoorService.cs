using System;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public sealed class AIGMCompanionDoorResult
    {
        public bool Attempted { get; set; }
        public bool Opened { get; set; }
        public string Status { get; set; }
        public string Target { get; set; }

        public AIGMCompanionDoorResult()
        {
            Status = "none";
            Target = String.Empty;
        }
    }

    public static class AIGMCompanionDoorService
    {
        public static bool TryOpenNearbyDoor(BaseHire companion)
        {
            AIGMCompanionDoorResult result = TryOpenNearbyDoorDetailed(companion);
            return result != null && result.Opened;
        }

        public static AIGMCompanionDoorResult TryOpenNearbyDoorDetailed(BaseHire companion)
        {
            AIGMCompanionDoorResult result = new AIGMCompanionDoorResult();
            if (companion == null || companion.Deleted || !companion.Alive || companion.Map == null)
            {
                result.Status = "blocked";
                result.Target = "invalid_companion";
                return result;
            }

            BaseDoor door = FindNearestClosedDoor(companion);
            result.Attempted = true;
            if (door == null)
            {
                result.Status = "none";
                result.Target = String.Empty;
                return result;
            }

            result.Target = DescribeDoor(door);
            bool opened = BaseDoor.TryAutoOpenDoor(companion, false);
            result.Opened = opened;
            result.Status = opened ? "opened" : "blocked";
            return result;
        }

        private static BaseDoor FindNearestClosedDoor(Mobile mobile)
        {
            if (mobile == null || mobile.Map == null)
                return null;

            IPooledEnumerable items = mobile.Map.GetItemsInRange(mobile.Location, 2);
            BaseDoor best = null;
            int bestScore = Int32.MaxValue;

            foreach (Item item in items)
            {
                BaseDoor door = item as BaseDoor;
                if (door == null || door.Deleted || door.Map != mobile.Map || door.Open)
                    continue;

                if (!mobile.InRange(door.GetWorldLocation(), 2))
                    continue;

                int score = Math.Abs(door.X - mobile.X) + Math.Abs(door.Y - mobile.Y);
                if (score < bestScore)
                {
                    best = door;
                    bestScore = score;
                }
            }

            items.Free();
            return best;
        }

        private static string DescribeDoor(BaseDoor door)
        {
            if (door == null)
                return String.Empty;

            return String.Format("{0}@({1},{2},{3})", door.GetType().Name, door.X, door.Y, door.Z);
        }
    }
}