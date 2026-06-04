using System;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionLocationService
    {
        public static AIGMCompanionLocationSnapshot Capture(BaseHire companion)
        {
            if (companion == null || companion.Deleted || companion.Map == null)
                return null;

            return new AIGMCompanionLocationSnapshot
            {
                CompanionSerial = companion.Serial.Value,
                CompanionName = companion.Name ?? companion.GetType().Name,
                MapName = companion.Map != null ? companion.Map.Name : String.Empty,
                RegionName = companion.Region != null ? companion.Region.Name ?? String.Empty : String.Empty,
                X = companion.X,
                Y = companion.Y,
                Z = companion.Z,
                SextantText = Sextant.GetCoords(companion),
                TimestampUtc = DateTime.UtcNow
            };
        }

        public static string FormatReport(AIGMCompanionLocationSnapshot snapshot)
        {
            if (snapshot == null)
                return "I cannot place our location just now.";

            string regionPart = !String.IsNullOrWhiteSpace(snapshot.RegionName) ? snapshot.RegionName : "an unknown region";
            string sextantPart = !String.IsNullOrWhiteSpace(snapshot.SextantText) ? " Sextant mark: " + snapshot.SextantText + "." : String.Empty;

            return String.Format(
                "I am at {0},{1},{2} on {3}, in {4}.{5}",
                snapshot.X,
                snapshot.Y,
                snapshot.Z,
                !String.IsNullOrWhiteSpace(snapshot.MapName) ? snapshot.MapName : "an unknown map",
                regionPart,
                sextantPart);
        }
    }
}
