using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.AIGM.Characters.Waylander
{
    public static partial class WaylanderRosterCatalog
    {
        private static IEnumerable<WaylanderCharacterDefinition> BuildCrossoverDefinitions()
        {
            List<WaylanderCharacterDefinition> list = new List<WaylanderCharacterDefinition>();

            WaylanderCharacterDefinition druss = Allied(Def("druss", "druss", "crossover", "Druss", "the Legend", "Drenai", "Legendary warrior"));
            druss.Aliases = new[] { "druss the legend" };
            druss.Body = 0x190;
            druss.Hue = Utility.RandomSkinHue();
            druss.HairItemId = 0x203C;
            druss.HairHue = 0x47E;
            druss.FacialHairItemId = 0x2041;
            druss.FacialHairHue = 0x47E;
            druss.Str = 120;
            druss.Dex = 95;
            druss.Int = 75;
            druss.Hits = 220;
            druss.DamageMin = 14;
            druss.DamageMax = 20;
            druss.VirtualArmor = 40;
            druss.IdentityLine = "Druss. If a wall still stands, good. If not, point me at the reason.";
            druss.WaylanderLine = "Waylander is a different kind of hard man than I am. Useful thing, difference, when a nation is in trouble.";
            druss.FactionLine = "Drenai, defenders, and anyone still willing to plant his feet where others start backing up.";
            druss.PhilosophyLine = "Courage is work done while afraid. Anyone telling you otherwise is usually trying to sell something.";
            druss.Relationships.Add(Rel("regnak", "ally", "legendary kinship", 84, "Regnak defends the same fortress in a later age.", WaylanderRelationshipTimelineState.Future, "Regnak belongs to the sort of wall that makes or unmakes men. I respect that before meeting him.", null));
            druss.Relationships.Add(Rel("tenaka_khan", "historical_predecessor", "martial respect", 70, "Tenaka stands in later Drenai/Nadir history.", WaylanderRelationshipTimelineState.Future, "Tenaka belongs to later wars and harder choices than some men survive cleanly. That earns interest.", null));
            druss.EnemyIds = new[] { "kuan_hador_demon_lord" };
            druss.EquipAction = delegate (WaylanderRosterMobile mobile)
            {
                Wear(mobile, new Boots(), null, 0);
                Wear(mobile, new LeatherChest(), "Black Jerkin", 1175);
                Wear(mobile, new DoubleAxe(), "Snaga", 1175);
            };
            Skill(druss, SkillName.Swords, 115.0);
            Skill(druss, SkillName.Tactics, 105.0);
            list.Add(druss);

            WaylanderCharacterDefinition tenaka = Allied(Def("tenaka_khan", "tenaka_khan", "future", "Tenaka Khan", "the Uniter", "Future Nadir / Drenai", "Commander"));
            tenaka.Body = 0x190;
            tenaka.Hue = Utility.RandomSkinHue();
            tenaka.HairItemId = 0x203B;
            tenaka.HairHue = 0x455;
            tenaka.Str = 105;
            tenaka.Dex = 95;
            tenaka.Int = 95;
            tenaka.Hits = 190;
            tenaka.DamageMin = 11;
            tenaka.DamageMax = 17;
            tenaka.VirtualArmor = 34;
            tenaka.IsLoreOnly = true;
            tenaka.RequiresLoreFlag = true;
            tenaka.IdentityLine = "Tenaka Khan. Men remember the prophecy and forget the labor required to survive becoming it.";
            tenaka.WaylanderLine = "Waylander belongs to the line of choices that kept later futures alive long enough to become burdens of their own.";
            tenaka.FactionLine = "Nadir and Drenai both, though I answer to neither simplification when they are used to shrink the truth.";
            tenaka.PhilosophyLine = "Destiny is pressure, not obedience. Blood only matters if a living man chooses what to do with it.";
            tenaka.Relationships.Add(Rel("zhu_chao", "historical_enemy", "ancestral hostility", 0, "Zhu Chao sought to prevent his future.", WaylanderRelationshipTimelineState.Historical, "Zhu Chao tried to murder a future before it learned to draw breath. I consider that a proper reason for memory.", null));
            tenaka.Relationships.Add(Rel("miriel", "ancestral_descendant", "reverent distance", 70, "Project lineage links him to Miriel.", WaylanderRelationshipTimelineState.Historical, "Miriel stands in my line like flint at the root of a fire.", null));
            tenaka.Relationships.Add(Rel("senta", "ancestral_descendant", "reverent distance", 70, "Project lineage links him to Senta.", WaylanderRelationshipTimelineState.Historical, "Senta's blood carried farther than his lifetime.", null));
            tenaka.EquipAction = delegate (WaylanderRosterMobile mobile)
            {
                Wear(mobile, new Boots(), null, 0);
                Wear(mobile, new StuddedChest(), "Uniter's Harness", 0x455);
                Wear(mobile, new Spear(), "Cavalry Spear", 0x455);
            };
            Skill(tenaka, SkillName.Fencing, 100.0);
            Skill(tenaka, SkillName.Tactics, 102.0);
            list.Add(tenaka);

            WaylanderCharacterDefinition niallad = Neutral(Def("niallad", "niallad", "historical", "Niallad", "King of the Drenai", "Drenai crown", "Historical king"));
            niallad.Body = 0x190;
            niallad.Hue = Utility.RandomSkinHue();
            niallad.HairItemId = 0x203B;
            niallad.HairHue = 0x59C;
            niallad.FacialHairItemId = 0x2041;
            niallad.FacialHairHue = 0x59C;
            niallad.Str = 90;
            niallad.Dex = 75;
            niallad.Int = 95;
            niallad.Hits = 155;
            niallad.DamageMin = 8;
            niallad.DamageMax = 12;
            niallad.VirtualArmor = 28;
            niallad.IsLoreOnly = true;
            niallad.RequiresLoreFlag = true;
            niallad.IdentityLine = "I am Niallad, king while the crown still sat warm on a living brow.";
            niallad.WaylanderLine = "Waylander is the assassin history tied to my death. I do not owe him easy absolution.";
            niallad.FactionLine = "The Drenai crown. Legitimacy is more than a chair and less than a myth.";
            niallad.PhilosophyLine = "A crown does not erase consequence. Death merely changes which hands must bear it next.";
            niallad.Relationships.Add(Rel("orien", "parent", "royal reverence", 90, "Orien is his father.", WaylanderRelationshipTimelineState.Current, "Orien is my father, and he taught me that a nation is a burden before it is an ornament.", null));
            niallad.Relationships.Add(Rel("dakeyras", "enemy", "historical consequence", 5, "Waylander is his assassin.", WaylanderRelationshipTimelineState.Current, "Waylander is the blade history chose to remember whenever my name is spoken sharply.", null));
            niallad.Relationships.Add(Rel("kaem", "enemy", "political hatred", 10, "Kaem arranged his assassination.", WaylanderRelationshipTimelineState.Current, "Kaem lacked the courage to kill a king openly and called the difference strategy.", null));
            niallad.EquipAction = delegate (WaylanderRosterMobile mobile)
            {
                Wear(mobile, new Boots(), null, 0);
                Wear(mobile, new FancyShirt(), "Royal Tunic", 0x59C);
                Wear(mobile, new PlateChest(), "Royal Harness", 0x59C);
                Wear(mobile, new Longsword(), "King's Sword", 0x59C);
            };
            Skill(niallad, SkillName.Swords, 86.0);
            Skill(niallad, SkillName.Tactics, 88.0);
            list.Add(niallad);

            WaylanderCharacterDefinition sathuli = Neutral(Def("sathuli_lord", "sathuli_lord", "crossover", "Sathuli Lord", "of the tribe", "Sathuli", "Tribal ruler"));
            sathuli.Body = 0x190;
            sathuli.Hue = Utility.RandomSkinHue();
            sathuli.HairItemId = 0x203C;
            sathuli.HairHue = 0x455;
            sathuli.Str = 95;
            sathuli.Dex = 88;
            sathuli.Int = 85;
            sathuli.Hits = 165;
            sathuli.DamageMin = 9;
            sathuli.DamageMax = 14;
            sathuli.VirtualArmor = 28;
            sathuli.IdentityLine = "I am the Sathuli Lord. Hospitality is not obedience, and alliance is not surrender.";
            sathuli.WaylanderLine = "Waylander brings violence with him like dust on boots, but not always without purpose.";
            sathuli.FactionLine = "Sathuli sovereignty, tribal law, and the right to negotiate as equals rather than ornaments.";
            sathuli.PhilosophyLine = "A foreign power always mistakes courtesy for softness. I prefer letting the mistake ripen before correcting it.";
            sathuli.Relationships.Add(Rel("karnak", "political_opponent", "wary respect", 60, "Karnak is an attempted ally and negotiator.", WaylanderRelationshipTimelineState.Current, "Karnak negotiates like a man testing whether the table can also serve as a battlefield.", null));
            sathuli.Relationships.Add(Rel("morak", "enemy", "territorial hostility", 18, "Morak's final conflict touches Sathuli territory.", WaylanderRelationshipTimelineState.Current, "Morak spilled trouble across Sathuli ground and mistook distance for permission.", null));
            sathuli.Relationships.Add(Rel("zhu_chao", "enemy", "sovereign hostility", 12, "Zhu Chao is a political threat.", WaylanderRelationshipTimelineState.Current, "Zhu Chao treats sovereign peoples like counters on a board. That is reason enough for resistance.", null));
            sathuli.EquipAction = delegate (WaylanderRosterMobile mobile)
            {
                Wear(mobile, new Boots(), null, 0);
                Wear(mobile, new StuddedChest(), "Sathuli Harness", 0x455);
                Wear(mobile, new Scimitar(), "Curved Blade", 0x455);
            };
            Skill(sathuli, SkillName.Swords, 92.0);
            Skill(sathuli, SkillName.Tactics, 90.0);
            list.Add(sathuli);

            return list;
        }

        private static void BuildGroups(Dictionary<string, string[]> groups)
        {
            groups["book1"] = new[]
            {
                "dakeyras",
                "dardalion",
                "danyal",
                "durmast",
                "cadoras",
                "karnak",
                "egel",
                "gellan",
                "jonat",
                "sarvaj",
                "kaem",
                "orien",
                "hewla",
                "kai",
                "krylla",
                "miriel",
                "kesa_khan",
                "joining"
            };

            groups["book2"] = new[]
            {
                "angel",
                "senta",
                "belash",
                "morak",
                "zhu_chao",
                "bodalen",
                "ansi_chen",
                "innicas",
                "regnak",
                "scar",
                "dark_brotherhood_knight"
            };

            groups["book3"] = new[]
            {
                "dakeyras.grey_man",
                "kysumu",
                "yu_yu_liang",
                "ustarte",
                "keeva_taliana",
                "matze_chai",
                "aric",
                "duke_of_kydor",
                "kuan_hador_demon_lord"
            };

            groups["crossover"] = new[]
            {
                "druss",
                "tenaka_khan",
                "niallad",
                "sathuli_lord"
            };

            groups["dark_brotherhood_squad"] = new[]
            {
                "innicas",
                "dark_brotherhood_knight",
                "dark_brotherhood_knight",
                "dark_brotherhood_knight"
            };

            groups["wolfshead_group"] = new[]
            {
                "ansi_chen",
                "belash",
                "kesa_khan"
            };

            groups["kuan_hador_encounter_group"] = new[]
            {
                "kuan_hador_demon_lord",
                "kuan_hador_demon_lord",
                "ustarte",
                "kysumu",
                "yu_yu_liang"
            };
        }
    }
}
