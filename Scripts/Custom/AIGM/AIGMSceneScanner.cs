using System;

namespace Server.Custom.AIGM
{
    public static class AIGMSceneScanner
    {
        public static AIGMSceneContext Capture(Mobile from, int range)
        {
            AIGMSceneContext scene = new AIGMSceneContext();

            if (from == null)
                return scene;

            scene.MapName = from.Map != null ? from.Map.Name : null;
            scene.RegionName = from.Region != null ? from.Region.Name : null;
            scene.X = from.Location.X;
            scene.Y = from.Location.Y;
            scene.Z = from.Location.Z;

            if (from.Map == null)
                return scene;

            IPooledEnumerable mobiles = from.Map.GetMobilesInRange(from.Location, range);
            foreach (Mobile mob in mobiles)
            {
                if (mob == null || mob == from || mob.Deleted)
                    continue;

                scene.NearbyMobiles.Add(new AIGMSceneEntitySummary
                {
                    Serial = mob.Serial.Value,
                    Kind = "Mobile",
                    Name = mob.Name,
                    TypeName = mob.GetType().Name,
                    X = mob.Location.X,
                    Y = mob.Location.Y,
                    Z = mob.Location.Z
                });

                if (scene.NearbyMobiles.Count >= 32)
                    break;
            }
            mobiles.Free();

            IPooledEnumerable items = from.Map.GetItemsInRange(from.Location, range);
            foreach (Item item in items)
            {
                if (item == null || item.Deleted)
                    continue;

                scene.NearbyItems.Add(new AIGMSceneEntitySummary
                {
                    Serial = item.Serial.Value,
                    Kind = "Item",
                    Name = item.Name,
                    TypeName = item.GetType().Name,
                    X = item.Location.X,
                    Y = item.Location.Y,
                    Z = item.Location.Z
                });

                if (scene.NearbyItems.Count >= 16)
                    break;
            }
            items.Free();

            return scene;
        }
    }
}