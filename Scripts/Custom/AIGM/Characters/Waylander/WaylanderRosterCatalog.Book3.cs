using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.AIGM.Characters.Waylander
{
    public static partial class WaylanderRosterCatalog
    {
        private static IEnumerable<WaylanderCharacterDefinition> BuildBook3Definitions()
        {
            List<WaylanderCharacterDefinition> list = new List<WaylanderCharacterDefinition>();

            WaylanderCharacterDefinition greyMan = Allied(Def("dakeyras.grey_man", "dakeyras", "book3", "Dakeyras", "the Gentleman", "Kydor", "Older assassin / landlord"));
            greyMan.Aliases = new[] { "waylander", "grey man", "gentleman" };
            greyMan.Body = 0x190;
            greyMan.Hue = Utility.RandomSkinHue();
            greyMan.HairItemId = 0x203B;
            greyMan.HairHue = 0x47E;
            greyMan.FacialHairItemId = 0x2041;
            greyMan.FacialHairHue = 0x47E;
            greyMan.Str = 95;
            greyMan.Dex = 100;
            greyMan.Int = 90;
            greyMan.Hits = 175;
            greyMan.DamageMin = 10;
            greyMan.DamageMax = 15;
            greyMan.VirtualArmor = 30;
            greyMan.IdentityLine = "Dakeyras, if names still matter to you. In Kydor most simply know me as the Gentleman, and I no longer correct them quickly.";
            greyMan.WaylanderLine = "Waylander is still here. Age only trimmed some of the noise around him.";
            greyMan.FactionLine = "Kydor, my household, and the people who cannot survive another ambitious fool making bargains with monsters.";
            greyMan.PhilosophyLine = "Weariness is not surrender. It is simply the truth told by a body that has survived long enough to stop pretending immortality.";
            greyMan.Relationships.Add(Rel("keeva_taliana", "protector", "guarded care", 88, "Keeva is his rescued household ward and possible student.", WaylanderRelationshipTimelineState.Current, "Keeva deserved rescue before she deserved instruction. The order matters.", null));
            greyMan.Relationships.Add(Rel("kysumu", "ally", "disciplined respect", 82, "Kysumu is a respected swordsman ally.", WaylanderRelationshipTimelineState.Current, "Kysumu carries his discipline as if it were a prayer sharpened into steel. I respect that.", null));
            greyMan.Relationships.Add(Rel("yu_yu_liang", "ally", "dry fondness", 72, "Yu Yu Liang is an unlikely ally.", WaylanderRelationshipTimelineState.Current, "Yu Yu Liang complains like a man trying to outrun fear and keeps moving anyway. I trust that more than boasting.", null));
            greyMan.Relationships.Add(Rel("ustarte", "ally", "spiritual respect", 85, "Ustarte is a mystical ally.", WaylanderRelationshipTimelineState.Current, "Ustarte proves that form is not the measure of a soul.", null));
            greyMan.Relationships.Add(Rel("duke_of_kydor", "protector", "burdened secrecy", 75, "The Duke is Niallad's son and Waylander's protected descendant.", WaylanderRelationshipTimelineState.Current, "The Duke carries history he does not yet know in full. That ignorance is part of what I am protecting.", null));
            greyMan.Relationships.Add(Rel("aric", "enemy", "cold hostility", 5, "Aric is a political and demonic enemy.", WaylanderRelationshipTimelineState.Current, "Aric mistakes polish for innocence and ambition for legitimacy. That mistake will kill him if nothing else does.", null));
            greyMan.EnemyIds = new[] { "aric", "kuan_hador_demon_lord" };
            greyMan.EquipAction = delegate (WaylanderRosterMobile mobile) { EquipWaylanderLeathers(mobile, true); };
            Skill(greyMan, SkillName.Archery, 105.0);
            Skill(greyMan, SkillName.Fencing, 98.0);
            Skill(greyMan, SkillName.Hiding, 90.0);
            Skill(greyMan, SkillName.Tactics, 95.0);
            list.Add(greyMan);

            WaylanderCharacterDefinition kysumu = Allied(Def("kysumu", "kysumu", "book3", "Kysumu", "the rajnee", "Chiatze / Kydor allies", "Master swordsman"));
            kysumu.Body = 0x190;
            kysumu.Hue = Utility.RandomSkinHue();
            kysumu.HairItemId = 0x203B;
            kysumu.HairHue = 0x444;
            kysumu.Str = 90;
            kysumu.Dex = 105;
            kysumu.Int = 85;
            kysumu.Hits = 165;
            kysumu.DamageMin = 10;
            kysumu.DamageMax = 15;
            kysumu.VirtualArmor = 26;
            kysumu.IdentityLine = "Kysumu. One of the last rajnee, which means discipline is now a duty rather than a shared custom.";
            kysumu.WaylanderLine = "Waylander is not a disciplined man in the formal sense. He is, however, ruthlessly economical with violence, and that deserves respect.";
            kysumu.FactionLine = "I stand with those defending Kydor against the breach, not because I belong to their kingdom, but because the boundary belongs to all worlds once it fails.";
            kysumu.PhilosophyLine = "Form without purpose is vanity. Purpose without form becomes panic. A blade needs both.";
            kysumu.Relationships.Add(Rel("yu_yu_liang", "ally", "wry respect", 82, "Yu Yu Liang is his unlikely ally.", WaylanderRelationshipTimelineState.Current, "Yu Yu Liang carries fear honestly, which makes his courage harder and cleaner than most warriors' performances.", null));
            kysumu.Relationships.Add(Rel("dakeyras", "ally", "earned respect", 80, "Waylander is a respected warrior.", WaylanderRelationshipTimelineState.Current, "Dakeyras wastes neither words nor motion when danger is close. That is a discipline, whether named or not.", null));
            kysumu.Relationships.Add(Rel("ustarte", "ally", "spiritual respect", 84, "Ustarte is a supernatural ally.", WaylanderRelationshipTimelineState.Current, "Ustarte holds nobility without needing humanity's shape to counterfeit it.", null));
            kysumu.EnemyIds = new[] { "kuan_hador_demon_lord" };
            kysumu.EquipAction = delegate (WaylanderRosterMobile mobile)
            {
                Wear(mobile, new Boots(), null, 0);
                Wear(mobile, new FancyShirt(), "Rajnee Tunic", 0x444);
                Wear(mobile, new LongPants(), null, 0x444);
                Wear(mobile, new Katana(), "Rajnee Sword", 0x444);
            };
            Skill(kysumu, SkillName.Swords, 108.0);
            Skill(kysumu, SkillName.Tactics, 96.0);
            list.Add(kysumu);

            WaylanderCharacterDefinition yuYuLiang = Allied(Def("yu_yu_liang", "yu_yu_liang", "book3", "Yu Yu Liang", "the ditch digger", "Kydor allies", "Unlikely hero"));
            yuYuLiang.Aliases = new[] { "yu yu liang", "liang" };
            yuYuLiang.Body = 0x190;
            yuYuLiang.Hue = Utility.RandomSkinHue();
            yuYuLiang.HairItemId = 0x203C;
            yuYuLiang.HairHue = 0x455;
            yuYuLiang.Str = 80;
            yuYuLiang.Dex = 90;
            yuYuLiang.Int = 70;
            yuYuLiang.Hits = 145;
            yuYuLiang.DamageMin = 8;
            yuYuLiang.DamageMax = 12;
            yuYuLiang.VirtualArmor = 20;
            yuYuLiang.IdentityLine = "Yu Yu Liang. Ditch digger, occasional fool, unwilling participant in far too much heroism.";
            yuYuLiang.WaylanderLine = "Waylander is the sort of man who makes you wish you had either trained harder or stayed home.";
            yuYuLiang.FactionLine = "I am on the side that prefers demons on the wrong side of the gate.";
            yuYuLiang.PhilosophyLine = "Being frightened is not the same thing as running. I know because I have tested the difference repeatedly.";
            yuYuLiang.Relationships.Add(Rel("kysumu", "ally", "mentor respect", 84, "Kysumu is his swordsman ally.", WaylanderRelationshipTimelineState.Current, "Kysumu speaks like a blade being sharpened. It is strangely comforting once you get used to it.", null));
            yuYuLiang.Relationships.Add(Rel("ustarte", "ally", "awed trust", 82, "Ustarte is a supernatural ally.", WaylanderRelationshipTimelineState.Current, "Ustarte makes me feel underdressed, undertrained, and oddly safer all at once.", null));
            yuYuLiang.Relationships.Add(Rel("dakeyras", "ally", "nervous respect", 72, "Waylander is a formidable ally.", WaylanderRelationshipTimelineState.Current, "Dakeyras is what happens when calm learns how to kill efficiently.", null));
            yuYuLiang.EnemyIds = new[] { "kuan_hador_demon_lord" };
            yuYuLiang.EquipAction = delegate (WaylanderRosterMobile mobile)
            {
                Wear(mobile, new Boots(), null, 0);
                Wear(mobile, new Shirt(), "Labourer's Shirt", 0x59B);
                Wear(mobile, new LongPants(), null, 0x59B);
                Wear(mobile, new Longsword(), "Demon-Glow Sword", 0x489);
            };
            Skill(yuYuLiang, SkillName.Swords, 88.0);
            Skill(yuYuLiang, SkillName.Tactics, 80.0);
            list.Add(yuYuLiang);

            WaylanderCharacterDefinition ustarte = Allied(Def("ustarte", "ustarte", "book3", "Ustarte", "the guardian", "Gateway defenders", "Mystical guardian"));
            ustarte.Body = 0x191;
            ustarte.Female = true;
            ustarte.Hue = 0x8A5;
            ustarte.HairItemId = 0x203B;
            ustarte.HairHue = 0x497;
            ustarte.Str = 105;
            ustarte.Dex = 100;
            ustarte.Int = 105;
            ustarte.Hits = 185;
            ustarte.DamageMin = 10;
            ustarte.DamageMax = 16;
            ustarte.VirtualArmor = 34;
            ustarte.IdentityLine = "I am Ustarte, a Joining only in origin, not in the poverty of spirit most men expect from the word.";
            ustarte.WaylanderLine = "Waylander carries an old darkness that never finished murdering his conscience. I honored that before he did.";
            ustarte.FactionLine = "I stand for the boundary, for life on this side of it, and against the presumption that creation fixes worth forever.";
            ustarte.PhilosophyLine = "Personhood is not granted by shape. It is revealed by conscience when power could have chosen easier hungers.";
            ustarte.Relationships.Add(Rel("dakeyras", "ally", "clear regard", 88, "Waylander is a recognized noble spirit.", WaylanderRelationshipTimelineState.Current, "Dakeyras understands cost better than comfort. That makes him useful and, in a harder sense, good.", null));
            ustarte.Relationships.Add(Rel("joining", "historical_predecessor", "sorrowful distinction", 40, "Joinings are the broader created category she transcends.", WaylanderRelationshipTimelineState.Historical, "Most Joinings are instruments before they are selves. I refuse to remain only an instrument.", null));
            ustarte.Relationships.Add(Rel("kuan_hador_demon_lord", "enemy", "sacred hostility", 0, "The demon lords are her enemies.", WaylanderRelationshipTimelineState.Current, "The demon lords are appetite with memory and empire in it. They will not pass while I stand.", null));
            ustarte.EnemyIds = new[] { "kuan_hador_demon_lord" };
            ustarte.EquipAction = delegate (WaylanderRosterMobile mobile)
            {
                Wear(mobile, new Sandals(), null, 0);
                Wear(mobile, new Robe(), "Priestess Regalia", 0x8A5);
                Wear(mobile, new QuarterStaff(), "Gateway Staff", 0x8A5);
            };
            Skill(ustarte, SkillName.Magery, 105.0);
            Skill(ustarte, SkillName.SpiritSpeak, 105.0);
            Skill(ustarte, SkillName.Wrestling, 90.0);
            list.Add(ustarte);

            WaylanderCharacterDefinition keeva = Allied(Def("keeva_taliana", "keeva_taliana", "book3", "Keeva Taliana", "of the household", "Kydor household", "Developing assassin"));
            keeva.Aliases = new[] { "keeva" };
            keeva.Body = 0x191;
            keeva.Female = true;
            keeva.Hue = Utility.RandomSkinHue();
            keeva.HairItemId = 0x203B;
            keeva.HairHue = 0x455;
            keeva.Str = 75;
            keeva.Dex = 95;
            keeva.Int = 75;
            keeva.Hits = 130;
            keeva.DamageMin = 7;
            keeva.DamageMax = 11;
            keeva.VirtualArmor = 20;
            keeva.IdentityLine = "Keeva Taliana. I survived the sort of lesson no one should teach, so I mean to choose the rest for myself.";
            keeva.WaylanderLine = "Dakeyras rescued me, which is not the same thing as deciding what I must become afterward.";
            keeva.FactionLine = "I belong to the household that took me in and to the dead who still deserve memory.";
            keeva.PhilosophyLine = "Skill can keep helplessness from returning, but it is not wise to confuse skill with purpose too early.";
            keeva.Relationships.Add(Rel("dakeyras", "protector", "complicated trust", 84, "Waylander is her rescuer and possible teacher.", WaylanderRelationshipTimelineState.Current, "Dakeyras offered training, not ownership. That difference is why I still listen.", null));
            keeva.Relationships.Add(Rel("duke_of_kydor", "ally", "household loyalty", 70, "She belongs to the Kydor household context.", WaylanderRelationshipTimelineState.Current, "The Duke's house gave me walls again. I remember that.", null));
            keeva.EquipAction = delegate (WaylanderRosterMobile mobile)
            {
                Wear(mobile, new Sandals(), null, 0);
                Wear(mobile, new Shirt(), "Household Shirt", 0x455);
                Wear(mobile, new LeatherChest(), "Training Jerkin", 0x455);
                Pack(mobile, new Dagger(), "Keeva's Knife", 0x455);
                Pack(mobile, new Dagger(), "Keeva's Knife", 0x455);
            };
            Skill(keeva, SkillName.Fencing, 80.0);
            Skill(keeva, SkillName.Hiding, 70.0);
            Skill(keeva, SkillName.Tactics, 76.0);
            list.Add(keeva);

            WaylanderCharacterDefinition matzeChai = Allied(Def("matze_chai", "matze_chai", "book3", "Matze Chai", "the merchant", "Chiatze", "Merchant ally"));
            matzeChai.Body = 0x190;
            matzeChai.Hue = Utility.RandomSkinHue();
            matzeChai.HairItemId = 0x203B;
            matzeChai.HairHue = 0x444;
            matzeChai.Str = 65;
            matzeChai.Dex = 65;
            matzeChai.Int = 90;
            matzeChai.Hits = 115;
            matzeChai.DamageMin = 5;
            matzeChai.DamageMax = 8;
            matzeChai.VirtualArmor = 12;
            matzeChai.IdentityLine = "Matze Chai. Trade, hospitality, and information all move farther than soldiers suspect.";
            matzeChai.WaylanderLine = "Waylander is an old contact whose problems have a habit of becoming regional before dawn.";
            matzeChai.FactionLine = "Chiatze interests, trusted contacts, and whichever living arrangement still permits profitable tomorrow.";
            matzeChai.PhilosophyLine = "Coin is only useful when it can still be turned into action before a wall falls or a gate opens.";
            matzeChai.Relationships.Add(Rel("dakeyras", "ally", "practical trust", 76, "Waylander is an old contact.", WaylanderRelationshipTimelineState.Current, "Dakeyras has a talent for turning private troubles into public emergencies. I still prefer him to the emergencies.", null));
            matzeChai.Relationships.Add(Rel("kysumu", "ally", "professional trust", 80, "Kysumu travels with him.", WaylanderRelationshipTimelineState.Current, "Kysumu is the kind of escort who lowers insurance costs simply by existing.", null));
            matzeChai.Relationships.Add(Rel("yu_yu_liang", "ally", "fond tolerance", 70, "Yu Yu Liang travels with him.", WaylanderRelationshipTimelineState.Current, "Yu Yu Liang complains like a reasonable man and fights like an unreasonable one. Both have uses.", null));
            matzeChai.EquipAction = delegate (WaylanderRosterMobile mobile)
            {
                Wear(mobile, new Sandals(), null, 0);
                Wear(mobile, new FancyShirt(), "Merchant Silk", 0x47E);
                Wear(mobile, new LongPants(), null, 0x47E);
                Pack(mobile, new BrownBook(), "Trade Ledger", 0x47E);
            };
            Skill(matzeChai, SkillName.ItemID, 100.0);
            Skill(matzeChai, SkillName.EvalInt, 60.0);
            list.Add(matzeChai);

            WaylanderCharacterDefinition aric = Hostile(Def("aric", "aric", "book3", "Aric", "the claimant", "Kydor conspirators", "Usurper"));
            aric.Body = 0x190;
            aric.Hue = Utility.RandomSkinHue();
            aric.HairItemId = 0x203B;
            aric.HairHue = 0x455;
            aric.Str = 90;
            aric.Dex = 80;
            aric.Int = 95;
            aric.Hits = 155;
            aric.DamageMin = 8;
            aric.DamageMax = 13;
            aric.VirtualArmor = 26;
            aric.IdentityLine = "Aric. A duchy deserves strong hands and public legitimacy. I intend to provide both, whether the current occupants appreciate the method or not.";
            aric.WaylanderLine = "Waylander is a useful reminder that one dangerous old man can still inconvenience an entire political design.";
            aric.FactionLine = "My own claim, supported by whatever instruments victory requires and history forgets afterward.";
            aric.PhilosophyLine = "Legitimacy belongs to the survivor who controls the story at the end. Morality is usually written by the defeated.";
            aric.Relationships.Add(Rel("duke_of_kydor", "political_opponent", "murderous ambition", 0, "The Duke is his intended victim.", WaylanderRelationshipTimelineState.Current, "The Duke mistakes inheritance for security. I mean to correct him.", null));
            aric.Relationships.Add(Rel("dakeyras", "enemy", "cold hostility", 10, "Waylander is an obstacle.", WaylanderRelationshipTimelineState.Current, "Dakeyras is what happens when an old blade keeps deciding it still has public work to do.", null));
            aric.Relationships.Add(Rel("kuan_hador_demon_lord", "ally", "bargained fear", 15, "He bargains with demons.", WaylanderRelationshipTimelineState.Current, "The demonic bargain is a means, not a devotion. Men mistake necessity for worship too easily.", null));
            aric.EnemyIds = new[] { "dakeyras", "duke_of_kydor", "ustarte" };
            aric.EquipAction = delegate (WaylanderRosterMobile mobile)
            {
                Wear(mobile, new Boots(), null, 0);
                Wear(mobile, new FancyShirt(), "Claimant's Coat", 0x455);
                Wear(mobile, new PlateChest(), "Noble Harness", 0x455);
                Wear(mobile, new Longsword(), null, 0x455);
            };
            Skill(aric, SkillName.Swords, 86.0);
            Skill(aric, SkillName.Tactics, 84.0);
            list.Add(aric);

            WaylanderCharacterDefinition duke = Allied(Def("duke_of_kydor", "duke_of_kydor", "book3", "Duke of Kydor", "of Kydor", "Kydor", "Ruling noble"));
            duke.Body = 0x190;
            duke.Hue = Utility.RandomSkinHue();
            duke.HairItemId = 0x203C;
            duke.HairHue = 0x59C;
            duke.FacialHairItemId = 0x203F;
            duke.FacialHairHue = 0x59C;
            duke.Str = 85;
            duke.Dex = 75;
            duke.Int = 90;
            duke.Hits = 150;
            duke.DamageMin = 7;
            duke.DamageMax = 11;
            duke.VirtualArmor = 24;
            duke.IdentityLine = "I am the Duke of Kydor. A ruler is merely the man expected to stay calm while private threats become public danger.";
            duke.WaylanderLine = "Dakeyras is a difficult man and a useful one. I trust him without yet knowing every shadow behind that judgment.";
            duke.FactionLine = "Kydor, my household, and the order required to keep panic from making the demon problem worse.";
            duke.PhilosophyLine = "Rule is obligation before it is comfort. The family table and the public square are closer than young nobles usually believe.";
            duke.Relationships.Add(Rel("niallad", "parent", "historical reverence", 88, "Niallad is his father.", WaylanderRelationshipTimelineState.Deceased, "Niallad was my father, and his death still casts longer lines than most living courtiers understand.", null));
            duke.Relationships.Add(Rel("aric", "political_opponent", "guarded hostility", 18, "Aric is a usurping rival.", WaylanderRelationshipTimelineState.Current, "Aric speaks like a steward and plots like a butcher.", null));
            duke.Relationships.Add(Rel("dakeyras", "ally", "hidden debt", 72, "Waylander protects him while hiding deeper history.", WaylanderRelationshipTimelineState.Current, "Dakeyras is one of those men who grows more valuable the less he explains. I do not think that is accidental.", "The Duke should not know by default that Dakeyras killed Niallad."));
            duke.Relationships.Add(Rel("keeva_taliana", "ally", "household protection", 66, "Keeva belongs to the household.", WaylanderRelationshipTimelineState.Current, "Keeva has earned a place under this roof and the right not to be treated as a fragile ornament.", null));
            duke.EquipAction = delegate (WaylanderRosterMobile mobile)
            {
                Wear(mobile, new Boots(), null, 0);
                Wear(mobile, new FancyShirt(), "Ducal Coat", 0x59C);
                Wear(mobile, new PlateChest(), "Household Plate", 0x59C);
                Wear(mobile, new Longsword(), "Ducal Sword", 0x59C);
            };
            Skill(duke, SkillName.Swords, 82.0);
            Skill(duke, SkillName.Tactics, 80.0);
            list.Add(duke);

            WaylanderCharacterDefinition demonLord = Hostile(Def("kuan_hador_demon_lord", "kuan_hador_demon_lord", "book3", "Kuan Hador Demon Lord", String.Empty, "Kuan Hador", "Elite demon"));
            demonLord.Aliases = new[] { "demon lord", "kuan hador" };
            demonLord.Body = 0x09;
            demonLord.Hue = 0x497;
            demonLord.IsUnique = false;
            demonLord.AllowMultiples = true;
            demonLord.CanTalk = true;
            demonLord.IsHumanoid = false;
            demonLord.Str = 120;
            demonLord.Dex = 90;
            demonLord.Int = 110;
            demonLord.Hits = 220;
            demonLord.DamageMin = 13;
            demonLord.DamageMax = 19;
            demonLord.VirtualArmor = 40;
            demonLord.IdentityLine = "Kuan Hador Demon Lord. Your centuries are smoke between my breaths.";
            demonLord.WaylanderLine = "Waylander is merely one more mortal delaying a gate that was always meant to open.";
            demonLord.FactionLine = "Kuan Hador, seal-breakers, dream-corruptors, and the old dominion that remembers how brief your kingdoms are.";
            demonLord.PhilosophyLine = "Mortal empires imagine themselves permanent because they die too quickly to compare scales honestly.";
            demonLord.DefaultReplyLine = "The demon lord regards you as if you were already part of a future ruin.";
            demonLord.Relationships.Add(Rel("aric", "ally", "predatory contempt", 12, "Aric is a mortal pawn.", WaylanderRelationshipTimelineState.Current, "Aric is a bargain-shaped weakness wearing noble clothing.", null));
            demonLord.Relationships.Add(Rel("ustarte", "enemy", "ancient hostility", 0, "Ustarte guards the gateway against it.", WaylanderRelationshipTimelineState.Current, "Ustarte is an irritation because she spends herself usefully.", null));
            demonLord.Relationships.Add(Rel("dakeyras", "enemy", "cold contempt", 6, "Waylander opposes it.", WaylanderRelationshipTimelineState.Current, "Dakeyras is a brief life with inconveniently sharp instincts.", null));
            demonLord.EnemyIds = new[] { "ustarte", "dakeyras", "kysumu", "yu_yu_liang" };
            demonLord.EquipAction = delegate (WaylanderRosterMobile mobile)
            {
                mobile.Body = Utility.RandomList(0x09, 0x31, 0x4F);
                mobile.Hue = Utility.RandomList(0x497, 0x66D, 0x489);
                Wear(mobile, new QuarterStaff(), "Warp Staff", mobile.Hue);
            };
            Skill(demonLord, SkillName.Magery, 105.0);
            Skill(demonLord, SkillName.Wrestling, 95.0);
            Skill(demonLord, SkillName.MagicResist, 100.0);
            list.Add(demonLord);

            return list;
        }
    }
}
