using System;

namespace Server.Custom.AIGM
{
    public enum AIGMCompanionLootProfileKind
    {
        Default,
        GoldOnly,
        Supplies,
        Equipment,
        AllMonsterLoot
    }

    public sealed class AIGMCompanionLootProfile
    {
        public AIGMCompanionLootProfileKind Kind { get; private set; }
        public string Name { get; private set; }

        private AIGMCompanionLootProfile(AIGMCompanionLootProfileKind kind, string name)
        {
            Kind = kind;
            Name = name;
        }

        public static readonly AIGMCompanionLootProfile Default = new AIGMCompanionLootProfile(AIGMCompanionLootProfileKind.Default, "default");
        public static readonly AIGMCompanionLootProfile GoldOnly = new AIGMCompanionLootProfile(AIGMCompanionLootProfileKind.GoldOnly, "gold");
        public static readonly AIGMCompanionLootProfile Supplies = new AIGMCompanionLootProfile(AIGMCompanionLootProfileKind.Supplies, "supplies");
        public static readonly AIGMCompanionLootProfile Equipment = new AIGMCompanionLootProfile(AIGMCompanionLootProfileKind.Equipment, "equipment");
        public static readonly AIGMCompanionLootProfile AllMonsterLoot = new AIGMCompanionLootProfile(AIGMCompanionLootProfileKind.AllMonsterLoot, "all");

        public static AIGMCompanionLootProfile FromText(string text)
        {
            string normalized = Normalize(text);

            if (normalized.Contains("all") || normalized.Contains("everything"))
                return AllMonsterLoot;

            if (normalized.Contains("equipment") || normalized.Contains("weapon") || normalized.Contains("armor") || normalized.Contains("jewel"))
                return Equipment;

            if (normalized.Contains("supplies") || normalized.Contains("supply") || normalized.Contains("reagent") || normalized.Contains("potion") || normalized.Contains("bandage") || normalized.Contains("arrows") || normalized.Contains("bolts") || normalized.Contains("gems"))
                return Supplies;

            if (normalized.Contains("gold") || normalized.Contains("coin"))
                return GoldOnly;

            return Default;
        }

        private static string Normalize(string text)
        {
            if (String.IsNullOrWhiteSpace(text))
                return String.Empty;

            string normalized = text.Trim().ToLowerInvariant();
            normalized = normalized.Replace(",", " ").Replace(".", " ").Replace("!", " ").Replace("?", " ").Replace(";", " ").Replace(":", " ");

            while (normalized.Contains("  "))
                normalized = normalized.Replace("  ", " ");

            return normalized.Trim();
        }
    }
}
