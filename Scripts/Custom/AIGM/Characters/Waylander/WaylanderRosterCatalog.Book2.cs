using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.AIGM.Characters.Waylander
{
    public static partial class WaylanderRosterCatalog
    {
        private static IEnumerable<WaylanderCharacterDefinition> BuildBook2Definitions()
        {
            List<WaylanderCharacterDefinition> list = new List<WaylanderCharacterDefinition>();

            WaylanderCharacterDefinition angel = Allied(Def("angel", "angel", "book2", "Angel", "the gladiator", "Waylander's allies", "Veteran swordsman"));
            angel.Body = 0x190;
            angel.Hue = Utility.RandomSkinHue();
            angel.HairItemId = 0x203B;
            angel.HairHue = 0x47E;
            angel.FacialHairItemId = 0x2041;
            angel.FacialHairHue = 0x47E;
            angel.Str = 105;
            angel.Dex = 95;
            angel.Int = 70;
            angel.Hits = 180;
            angel.DamageMin = 11;
            angel.DamageMax = 16;
            angel.VirtualArmor = 34;
            angel.IdentityLine = "Angel. A hard old survivor who finally found people worth standing beside.";
            angel.WaylanderLine = "Waylander was easier to respect than to like, which is not the same thing as a criticism.";
            angel.FactionLine = "I stand where my comrades stand. That is enough of a flag for me.";
            angel.PhilosophyLine = "Survival means little until it is spent defending something other than your own skin.";
            angel.Relationships.Add(Rel("miriel", "ally", "protective loyalty", 88, "Miriel is his close ally and later traveling companion.", WaylanderRelationshipTimelineState.Current, "Miriel carries more steel than years should allow. I stood beside her gladly.", null));
            angel.Relationships.Add(Rel("senta", "ally", "scarred rivalry", 76, "Senta is a gladiator peer and rival.", WaylanderRelationshipTimelineState.Current, "Senta never met a mirror he disliked. He was still worth having in the line.", null));
            angel.Relationships.Add(Rel("dakeyras", "ally", "earned respect", 80, "Waylander is his ally.", WaylanderRelationshipTimelineState.Current, "Waylander wastes fewer words than most commanders waste men.", null));
            angel.EquipAction = delegate (WaylanderRosterMobile mobile)
            {
                Wear(mobile, new Boots(), null, 0);
                Wear(mobile, new StuddedChest(), "Gladiator Harness", 0x497);
                Wear(mobile, new StuddedArms(), null, 0x497);
                Wear(mobile, new StuddedLegs(), null, 0x497);
                Wear(mobile, new Longsword(), "Arena Sword", 0x497);
                Pack(mobile, new Broadsword(), "Spare Arena Sword", 0x497);
            };
            Skill(angel, SkillName.Swords, 102.0);
            Skill(angel, SkillName.Tactics, 96.0);
            list.Add(angel);

            WaylanderCharacterDefinition senta = Allied(Def("senta", "senta", "book2", "Senta", "the undefeated", "Waylander's allies", "Duelist"));
            senta.Body = 0x190;
            senta.Hue = Utility.RandomSkinHue();
            senta.HairItemId = 0x2047;
            senta.HairHue = 0x8A5;
            senta.Str = 95;
            senta.Dex = 110;
            senta.Int = 75;
            senta.Hits = 170;
            senta.DamageMin = 10;
            senta.DamageMax = 15;
            senta.VirtualArmor = 30;
            senta.IdentityLine = "Senta. Skill first, apologies later, sincerity only when it cannot be escaped.";
            senta.WaylanderLine = "Waylander began as a contract and ended as the sort of rival a man would rather die beside than against.";
            senta.FactionLine = "I stand with the comrades I chose, not the buyers who thought skill alone was enough to own me.";
            senta.PhilosophyLine = "Pride can make a weapon of a man or break him into glittering pieces. The trick is choosing the first.";
            senta.Relationships.Add(Rel("miriel", "ally", "devotion", 94, "Miriel is his lover.", WaylanderRelationshipTimelineState.Current, "Miriel made it impossible to keep pretending skill was the only thing I cared to lose.", null));
            senta.Relationships.Add(Rel("dakeyras", "ally", "competitive respect", 82, "Waylander was first his target and later his ally.", WaylanderRelationshipTimelineState.Current, "Waylander was worth crossing blades with and worth trusting after that. Both are rare.", null));
            senta.Relationships.Add(Rel("angel", "ally", "rivalry", 78, "Angel is his gladiator peer.", WaylanderRelationshipTimelineState.Current, "Angel groans like an old wall and hits like one too.", null));
            Fact(senta, "regnak", "If blood keeps running after death, then some of us go on in stranger ways than song.");
            senta.EquipAction = delegate (WaylanderRosterMobile mobile)
            {
                Wear(mobile, new Boots(), null, 0);
                Wear(mobile, new StuddedChest(), "Duelist's Harness", 0x8A5);
                Wear(mobile, new Longsword(), "Senta's Sword", 0x8A5);
                Pack(mobile, new Longsword(), "Senta's Sword", 0x8A5);
            };
            Skill(senta, SkillName.Swords, 104.0);
            Skill(senta, SkillName.Tactics, 94.0);
            list.Add(senta);

            WaylanderCharacterDefinition belash = Allied(Def("belash", "belash", "book2", "Belash", "the Wolfshead warrior", "Nadir Wolfshead", "Bladesman"));
            belash.Body = 0x190;
            belash.Hue = Utility.RandomSkinHue();
            belash.HairItemId = 0x203C;
            belash.HairHue = 0x455;
            belash.Str = 100;
            belash.Dex = 100;
            belash.Int = 70;
            belash.Hits = 175;
            belash.DamageMin = 10;
            belash.DamageMax = 15;
            belash.VirtualArmor = 30;
            belash.IdentityLine = "Belash of the Wolfshead. I remember blood debts and I do not turn from them.";
            belash.WaylanderLine = "Waylander began as captor and ended as a man I would call brother if the need were sharp enough.";
            belash.FactionLine = "Wolfshead Nadir. Tribe first, then the men who proved themselves worthy beside it.";
            belash.PhilosophyLine = "Pain is not permission to stop. The wound comes first; the question is what you still do afterward.";
            belash.Relationships.Add(Rel("ansi_chen", "commander", "filial loyalty", 94, "Ansi Chen is his chieftain.", WaylanderRelationshipTimelineState.Current, "Ansi Chen carried the tribe on his shoulders. I would have bled for him without being asked.", null));
            belash.Relationships.Add(Rel("innicas", "enemy", "vengeful hatred", 5, "Innicas killed his father and Ansi Chen.", WaylanderRelationshipTimelineState.Current, "Innicas owed the dead more than one life could pay. I collected what I could.", null));
            belash.Relationships.Add(Rel("dakeyras", "ally", "earned brotherhood", 82, "Waylander is captor turned ally.", WaylanderRelationshipTimelineState.Current, "Waylander proved himself in deeds, not tribe. That is a harder currency and I honor it.", null));
            belash.EquipAction = delegate (WaylanderRosterMobile mobile)
            {
                Wear(mobile, new Boots(), null, 0);
                Wear(mobile, new StuddedChest(), "Wolfshead Harness", 0x455);
                Wear(mobile, new Katana(), "Nadir Blade", 0x455);
                Pack(mobile, new Dagger(), "Spare Nadir Knife", 0x455);
            };
            Skill(belash, SkillName.Swords, 100.0);
            Skill(belash, SkillName.Tactics, 95.0);
            list.Add(belash);

            WaylanderCharacterDefinition morak = Hostile(Def("morak", "morak", "book2", "Morak", "the torturer", "Zhu Chao's contract web", "Assassin"));
            morak.Body = 0x190;
            morak.Hue = Utility.RandomSkinHue();
            morak.HairItemId = 0x203C;
            morak.HairHue = 0x455;
            morak.Str = 100;
            morak.Dex = 105;
            morak.Int = 75;
            morak.Hits = 175;
            morak.DamageMin = 10;
            morak.DamageMax = 16;
            morak.VirtualArmor = 28;
            morak.IdentityLine = "Morak. Contracts speak more honestly when fear does half the work for them.";
            morak.WaylanderLine = "Waylander was the sort of target that earns a professional man's full attention.";
            morak.FactionLine = "I follow the contract and the advantage it buys. That is all the banner I need.";
            morak.PhilosophyLine = "Pain strips a captive faster than conversation. I preferred efficiency, even when it wore a slower face.";
            morak.Relationships.Add(Rel("dakeyras", "target", "predatory focus", 8, "Waylander is his primary target.", WaylanderRelationshipTimelineState.Current, "Waylander forced more honesty out of my profession than I found comfortable.", null));
            morak.Relationships.Add(Rel("senta", "ally", "contempt", 20, "Senta worked in the same contract web.", WaylanderRelationshipTimelineState.Current, "Senta wasted polish on a world that should have taught him sharper lessons.", null));
            morak.EnemyIds = new[] { "dakeyras", "miriel", "belash" };
            morak.EquipAction = delegate (WaylanderRosterMobile mobile)
            {
                Wear(mobile, new Boots(), null, 0);
                Wear(mobile, new StuddedChest(), "Assassin's Harness", 1175);
                Wear(mobile, new Katana(), null, 1175);
                Pack(mobile, new Dagger(), "Torturer's Knife", 1175);
            };
            Skill(morak, SkillName.Swords, 102.0);
            Skill(morak, SkillName.Tactics, 96.0);
            list.Add(morak);

            WaylanderCharacterDefinition zhuChao = Hostile(Def("zhu_chao", "zhu_chao", "book2", "Zhu Chao", "chief sorcerer", "Gothir / Dark Brotherhood", "Sorcerer / strategist"));
            zhuChao.Body = 0x190;
            zhuChao.Hue = Utility.RandomSkinHue();
            zhuChao.HairItemId = 0x203C;
            zhuChao.HairHue = 0x47E;
            zhuChao.Str = 85;
            zhuChao.Dex = 70;
            zhuChao.Int = 120;
            zhuChao.Hits = 165;
            zhuChao.DamageMin = 7;
            zhuChao.DamageMax = 12;
            zhuChao.VirtualArmor = 24;
            zhuChao.IdentityLine = "I am Zhu Chao. Men mistake patience for softness right up to the moment it finishes killing them.";
            zhuChao.WaylanderLine = "Waylander is not the objective. Bloodlines are.";
            zhuChao.FactionLine = "Gothir power and the Dark Brotherhood are merely different instruments of the same design.";
            zhuChao.PhilosophyLine = "The future is not mystical. It is strategic. If a bloodline births disaster, cut the bloodline before the disaster learns to ride.";
            zhuChao.Relationships.Add(Rel("kesa_khan", "faction_enemy", "cold eradication", 0, "Kesa Khan guards the future Zhu Chao wants destroyed.", WaylanderRelationshipTimelineState.Current, "Kesa Khan defends a tribal future I would rather erase before it learns its own name.", null));
            zhuChao.Relationships.Add(Rel("dark_brotherhood_knight", "commander", "impersonal control", 70, "Dark Brotherhood Knights serve him.", WaylanderRelationshipTimelineState.Current, "The Brotherhood serves because purpose is easier to command than personality.", null));
            zhuChao.Relationships.Add(Rel("ansi_chen", "target", "generational hatred", 0, "Ansi Chen stands inside the bloodline Zhu Chao seeks to erase.", WaylanderRelationshipTimelineState.Current, "Ansi Chen guards a future that should never be permitted to arrive.", null));
            zhuChao.EnemyIds = new[] { "kesa_khan", "belash", "dakeyras", "tenaka_khan" };
            zhuChao.EquipAction = delegate (WaylanderRosterMobile mobile)
            {
                Wear(mobile, new Sandals(), null, 0);
                Wear(mobile, new Robe(), "Gothir Sorcerer's Robes", 0x47E);
                Wear(mobile, new QuarterStaff(), "Occult Command Staff", 0x47E);
                Pack(mobile, new Spellbook(), "Prophetic Ledger", 0x47E);
            };
            Skill(zhuChao, SkillName.Magery, 110.0);
            Skill(zhuChao, SkillName.SpiritSpeak, 100.0);
            Skill(zhuChao, SkillName.Meditation, 100.0);
            list.Add(zhuChao);

            WaylanderCharacterDefinition bodalen = Neutral(Def("bodalen", "bodalen", "book2", "Bodalen", "the compromised officer", "Gothir / Karnak's legacy", "Officer"));
            bodalen.Body = 0x190;
            bodalen.Hue = Utility.RandomSkinHue();
            bodalen.HairItemId = 0x203B;
            bodalen.HairHue = 0x8A5;
            bodalen.Str = 95;
            bodalen.Dex = 88;
            bodalen.Int = 88;
            bodalen.Hits = 165;
            bodalen.DamageMin = 9;
            bodalen.DamageMax = 14;
            bodalen.VirtualArmor = 30;
            bodalen.IdentityLine = "Bodalen. A son, an officer, and a man who learned too late how expensive borrowed power becomes.";
            bodalen.WaylanderLine = "Waylander is the sort of obstacle that turns politics honest by force.";
            bodalen.FactionLine = "Command, legacy, and survival have pulled me under more than one banner, none of them cleanly.";
            bodalen.PhilosophyLine = "A compromised man is still capable of one necessary act, if he chooses before the chain is tightened all the way.";
            bodalen.Relationships.Add(Rel("karnak", "parent", "complicated loyalty", 86, "Karnak is his father.", WaylanderRelationshipTimelineState.Current, "Karnak is my father. Inheritance is a heavier harness than armor.", null));
            bodalen.Relationships.Add(Rel("zhu_chao", "subordinate", "resentful obedience", 40, "Zhu Chao manipulates him.", WaylanderRelationshipTimelineState.Current, "Zhu Chao mistakes control for permanence. He is clever enough to be dangerous and blind enough to overreach.", null));
            bodalen.Relationships.Add(Rel("kesa_khan", "target", "military hostility", 15, "He leads the force that kills Kesa Khan.", WaylanderRelationshipTimelineState.Current, "Kesa Khan was a military target wrapped around a worse sort of ritual problem.", null));
            bodalen.EquipAction = delegate (WaylanderRosterMobile mobile)
            {
                Wear(mobile, new Boots(), null, 0);
                Wear(mobile, new PlateChest(), "Officer's Plate", 0x8A5);
                Wear(mobile, new PlateArms(), null, 0x8A5);
                Wear(mobile, new PlateLegs(), null, 0x8A5);
                Wear(mobile, new Longsword(), null, 0x8A5);
            };
            Skill(bodalen, SkillName.Swords, 92.0);
            Skill(bodalen, SkillName.Tactics, 90.0);
            list.Add(bodalen);

            WaylanderCharacterDefinition ansiChen = Allied(Def("ansi_chen", "ansi_chen", "book2", "Ansi Chen", "Wolfshead chieftain", "Nadir Wolfshead", "Chieftain"));
            ansiChen.Body = 0x190;
            ansiChen.Hue = Utility.RandomSkinHue();
            ansiChen.HairItemId = 0x203C;
            ansiChen.HairHue = 0x455;
            ansiChen.Str = 100;
            ansiChen.Dex = 90;
            ansiChen.Int = 85;
            ansiChen.Hits = 175;
            ansiChen.DamageMin = 10;
            ansiChen.DamageMax = 15;
            ansiChen.VirtualArmor = 32;
            ansiChen.IdentityLine = "Ansi Chen. A chieftain belongs to his tribe before he belongs to himself.";
            ansiChen.WaylanderLine = "Waylander is an outsider who nonetheless proved the sort of courage tribes remember.";
            ansiChen.FactionLine = "Wolfshead. I guard the tribe, the children, and the future still breathing through them.";
            ansiChen.PhilosophyLine = "Hospitality, truth, and blood obligation all matter more when the enemy means to erase your people entirely.";
            ansiChen.Relationships.Add(Rel("belash", "commander", "paternal trust", 92, "Belash is his warrior.", WaylanderRelationshipTimelineState.Current, "Belash carries grief like a drawn blade. He is still one of mine.", null));
            ansiChen.Relationships.Add(Rel("kesa_khan", "ally", "tribal trust", 86, "Kesa Khan is his shaman.", WaylanderRelationshipTimelineState.Current, "Kesa Khan guards the tribe's spirit just as I guard its flesh.", null));
            ansiChen.Relationships.Add(Rel("innicas", "enemy", "mortal enmity", 0, "Innicas kills him.", WaylanderRelationshipTimelineState.Current, "Innicas is the kind of commander who calls desecration discipline.", null));
            ansiChen.EquipAction = delegate (WaylanderRosterMobile mobile)
            {
                Wear(mobile, new Boots(), null, 0);
                Wear(mobile, new StuddedChest(), "Wolfshead Chieftain's Harness", 0x455);
                Wear(mobile, new Katana(), "Chieftain's Blade", 0x455);
                Pack(mobile, new Dagger(), "Tribal Knife", 0x455);
            };
            Skill(ansiChen, SkillName.Swords, 96.0);
            Skill(ansiChen, SkillName.Tactics, 92.0);
            list.Add(ansiChen);

            WaylanderCharacterDefinition innicas = Hostile(Def("innicas", "innicas", "book2", "Innicas", "Brotherhood commander", "Dark Brotherhood", "Commander"));
            innicas.Body = 0x190;
            innicas.Hue = Utility.RandomSkinHue();
            innicas.HairItemId = 0x203C;
            innicas.HairHue = 0x455;
            innicas.Str = 105;
            innicas.Dex = 95;
            innicas.Int = 80;
            innicas.Hits = 180;
            innicas.DamageMin = 11;
            innicas.DamageMax = 16;
            innicas.VirtualArmor = 36;
            innicas.IdentityLine = "Innicas. The Brotherhood does not need your admiration, only your collapse.";
            innicas.WaylanderLine = "Waylander remains a target. That is enough identity for any enemy worth naming.";
            innicas.FactionLine = "Dark Brotherhood. Order, secrecy, ritual security, and obedience.";
            innicas.PhilosophyLine = "A commander exists to convert fear into disciplined action and lives into objectives.";
            innicas.Relationships.Add(Rel("zhu_chao", "subordinate", "disciplined loyalty", 82, "Zhu Chao is his superior.", WaylanderRelationshipTimelineState.Current, "Zhu Chao sees farther than most rulers dare. My concern is making the nearer killing efficient.", null));
            innicas.Relationships.Add(Rel("dark_brotherhood_knight", "commander", "severe control", 86, "Dark Brotherhood Knights are his subordinates.", WaylanderRelationshipTimelineState.Current, "The Knights are not sons or pets. They are formation, purpose, and sacrifice on command.", null));
            innicas.Relationships.Add(Rel("belash", "enemy", "mortal contempt", 5, "Belash kills him after being mortally wounded.", WaylanderRelationshipTimelineState.Current, "Belash had the stubbornness of a worthy enemy, which only made him more troublesome to finish.", null));
            innicas.EnemyIds = new[] { "belash", "ansi_chen", "dardalion" };
            innicas.EquipAction = delegate (WaylanderRosterMobile mobile) { EquipDarkBrotherhoodArmor(mobile, new Longsword(), true); };
            Skill(innicas, SkillName.Swords, 102.0);
            Skill(innicas, SkillName.Tactics, 98.0);
            Skill(innicas, SkillName.SpiritSpeak, 70.0);
            list.Add(innicas);

            WaylanderCharacterDefinition regnak = Allied(Def("regnak", "regnak", "future", "Regnak", "Earl of Bronze", "Future Drenai", "Future defender"));
            regnak.Aliases = new[] { "rek" };
            regnak.Body = 0x190;
            regnak.Hue = Utility.RandomSkinHue();
            regnak.HairItemId = 0x203B;
            regnak.HairHue = 0x59C;
            regnak.Str = 100;
            regnak.Dex = 90;
            regnak.Int = 80;
            regnak.Hits = 170;
            regnak.DamageMin = 10;
            regnak.DamageMax = 15;
            regnak.VirtualArmor = 32;
            regnak.IsLoreOnly = true;
            regnak.RequiresLoreFlag = true;
            regnak.IdentityLine = "I am Regnak, called Rek, Earl of Bronze in an age later than most of you should be speaking to.";
            regnak.WaylanderLine = "Waylander belongs to the lineage of burdens that made my walls necessary.";
            regnak.FactionLine = "Drenai, and more specifically Dros Delnoch, where inheritance stops being ceremony and becomes siege duty.";
            regnak.PhilosophyLine = "History is not a trophy. It is a bill delivered to descendants who still have to hold the gate.";
            regnak.Relationships.Add(Rel("druss", "ally", "legendary respect", 86, "Druss is a fellow defender of Dros Delnoch.", WaylanderRelationshipTimelineState.Future, "Druss taught later generations that courage can sound like a laugh and still hold a wall.", null));
            regnak.Relationships.Add(Rel("miriel", "ancestral_descendant", "reverent distance", 75, "Project lineage links him to Miriel.", WaylanderRelationshipTimelineState.Historical, "Miriel stands behind my line like a drawn bow behind a name. I respect the debt even across centuries.", null));
            regnak.Relationships.Add(Rel("senta", "ancestral_descendant", "reverent distance", 72, "Project lineage links him to Senta.", WaylanderRelationshipTimelineState.Historical, "Senta's blood passed forward with more consequence than he ever had time to see.", null));
            regnak.EquipAction = delegate (WaylanderRosterMobile mobile)
            {
                Wear(mobile, new Boots(), null, 0);
                Wear(mobile, new PlateChest(), "Bronze Heir's Cuirass", 2213);
                Wear(mobile, new PlateArms(), null, 2213);
                Wear(mobile, new PlateLegs(), null, 2213);
                Wear(mobile, new Longsword(), "Bronze Legacy Sword", 2213);
            };
            Skill(regnak, SkillName.Swords, 95.0);
            Skill(regnak, SkillName.Tactics, 92.0);
            list.Add(regnak);

            WaylanderCharacterDefinition scar = Animal(Def("scar", "scar", "book2", "Scar", String.Empty, "Waylander's household", "Mount"));
            scar.IsMount = true;
            scar.IsHumanoid = false;
            scar.CanTalk = false;
            scar.IsUnique = true;
            scar.Body = 0xC8;
            scar.Str = 85;
            scar.Dex = 95;
            scar.Int = 20;
            scar.Hits = 145;
            scar.DamageMin = 5;
            scar.DamageMax = 8;
            scar.VirtualArmor = 12;
            scar.IdentityLine = String.Empty;
            scar.WaylanderLine = String.Empty;
            scar.FactionLine = String.Empty;
            scar.PhilosophyLine = String.Empty;
            list.Add(scar);

            WaylanderCharacterDefinition darkBrotherhoodKnight = Hostile(Def("dark_brotherhood_knight", "dark_brotherhood_knight", "book2", "Dark Brotherhood Knight", String.Empty, "Dark Brotherhood", "Occult soldier"));
            darkBrotherhoodKnight.Aliases = new[] { "dark brotherhood", "brotherhood knight" };
            darkBrotherhoodKnight.Body = 0x190;
            darkBrotherhoodKnight.Hue = Utility.RandomSkinHue();
            darkBrotherhoodKnight.HairItemId = 0x203C;
            darkBrotherhoodKnight.HairHue = 0x455;
            darkBrotherhoodKnight.IsUnique = false;
            darkBrotherhoodKnight.AllowMultiples = true;
            darkBrotherhoodKnight.Str = 100;
            darkBrotherhoodKnight.Dex = 90;
            darkBrotherhoodKnight.Int = 70;
            darkBrotherhoodKnight.Hits = 170;
            darkBrotherhoodKnight.DamageMin = 10;
            darkBrotherhoodKnight.DamageMax = 15;
            darkBrotherhoodKnight.VirtualArmor = 34;
            darkBrotherhoodKnight.IdentityLine = "Dark Brotherhood Knight. The order is enough. My personal name is not part of this exchange.";
            darkBrotherhoodKnight.WaylanderLine = "Waylander is a hostile objective, not a subject for confession.";
            darkBrotherhoodKnight.FactionLine = "Dark Brotherhood. Command hierarchy, secrecy, coordinated pressure.";
            darkBrotherhoodKnight.PhilosophyLine = "Identity is weaker than purpose. We preserve the order and spend the self if required.";
            darkBrotherhoodKnight.DefaultReplyLine = "The Brotherhood has no reason to explain itself to you.";
            darkBrotherhoodKnight.Relationships.Add(Rel("innicas", "commander", "disciplined obedience", 84, "Innicas is field commander.", WaylanderRelationshipTimelineState.Current, "Innicas commands the field. That is sufficient.", null));
            darkBrotherhoodKnight.Relationships.Add(Rel("zhu_chao", "subordinate", "strategic obedience", 72, "Zhu Chao is supreme authority.", WaylanderRelationshipTimelineState.Current, "Zhu Chao's design outranks my curiosity.", null));
            darkBrotherhoodKnight.Relationships.Add(Rel("dardalion", "enemy", "ritual hostility", 10, "Dardalion and the Thirty are spiritual enemies.", WaylanderRelationshipTimelineState.Current, "The Thirty interfere where they should have learned to kneel or die.", null));
            darkBrotherhoodKnight.EnemyIds = new[] { "dardalion", "belash", "ansi_chen", "dakeyras" };
            darkBrotherhoodKnight.EquipAction = delegate (WaylanderRosterMobile mobile)
            {
                Item weapon;
                switch (Utility.Random(5))
                {
                    case 0: weapon = new Longsword(); break;
                    case 1: weapon = new Halberd(); break;
                    case 2: weapon = new Spear(); break;
                    case 3: weapon = new Bow(); break;
                    default: weapon = new Broadsword(); break;
                }

                EquipDarkBrotherhoodArmor(mobile, weapon, Utility.RandomBool());
            };
            Skill(darkBrotherhoodKnight, SkillName.Swords, 92.0);
            Skill(darkBrotherhoodKnight, SkillName.Tactics, 92.0);
            Skill(darkBrotherhoodKnight, SkillName.Parry, 82.0);
            list.Add(darkBrotherhoodKnight);

            return list;
        }
    }
}
