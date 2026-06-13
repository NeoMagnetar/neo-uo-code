using Server.Items;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionDoorService
    {
        public static bool TryOpenNearbyDoor(BaseHire companion)
        {
            if (companion == null || companion.Deleted || !companion.Alive || companion.Map == null)
                return false;

            return BaseDoor.TryAutoOpenDoor(companion, false);
        }
    }
}
