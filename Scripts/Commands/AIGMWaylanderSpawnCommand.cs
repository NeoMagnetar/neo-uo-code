using System;
using System.Collections.Generic;
using Server.Custom.AIGM;
using Server.Custom.AIGM.Characters.Waylander;
using Server.Mobiles;

namespace Server.Commands
{
    public static class AIGMWaylanderSpawnCommand
    {
        private static readonly Dictionary<string, Type> SpawnTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            { "dakeyras", typeof(AIGMCompanionDakeyras) },
            { "waylander", typeof(AIGMCompanionDakeyras) },
            { "dakeyras.grey_man", typeof(WaylanderGreyMan) },
            { "grey man", typeof(WaylanderGreyMan) },
            { "gentleman", typeof(WaylanderGreyMan) },
            { "danyal", typeof(AIGMCompanionDanyal) },
            { "dardalion", typeof(AIGMCompanionDardalion) },
            { "durmast", typeof(WaylanderDurmast) },
            { "cadoras", typeof(WaylanderCadoras) },
            { "karnak", typeof(WaylanderKarnak) },
            { "egel", typeof(WaylanderEgel) },
            { "gellan", typeof(WaylanderGellan) },
            { "jonat", typeof(WaylanderJonat) },
            { "sarvaj", typeof(WaylanderSarvaj) },
            { "kaem", typeof(WaylanderKaem) },
            { "orien", typeof(WaylanderOrien) },
            { "hewla", typeof(WaylanderHewla) },
            { "kai", typeof(WaylanderKai) },
            { "krylla", typeof(WaylanderKrylla) },
            { "miriel", typeof(WaylanderMiriel) },
            { "kesa_khan", typeof(WaylanderKesaKhan) },
            { "joining", typeof(WaylanderJoining) },
            { "angel", typeof(WaylanderAngel) },
            { "senta", typeof(WaylanderSenta) },
            { "belash", typeof(WaylanderBelash) },
            { "morak", typeof(WaylanderMorak) },
            { "zhu_chao", typeof(WaylanderZhuChao) },
            { "bodalen", typeof(WaylanderBodalen) },
            { "ansi_chen", typeof(WaylanderAnsiChen) },
            { "innicas", typeof(WaylanderInnicas) },
            { "regnak", typeof(WaylanderRegnak) },
            { "scar", typeof(WaylanderScar) },
            { "dark_brotherhood_knight", typeof(WaylanderDarkBrotherhoodKnight) },
            { "kysumu", typeof(WaylanderKysumu) },
            { "yu_yu_liang", typeof(WaylanderYuYuLiang) },
            { "ustarte", typeof(WaylanderUstarte) },
            { "keeva_taliana", typeof(WaylanderKeevaTaliana) },
            { "matze_chai", typeof(WaylanderMatzeChai) },
            { "aric", typeof(WaylanderAric) },
            { "duke_of_kydor", typeof(WaylanderDukeOfKydor) },
            { "kuan_hador_demon_lord", typeof(WaylanderKuanHadorDemonLord) },
            { "druss", typeof(WaylanderDruss) },
            { "tenaka_khan", typeof(WaylanderTenakaKhan) },
            { "niallad", typeof(WaylanderNiallad) },
            { "sathuli_lord", typeof(WaylanderSathuliLord) }
        };

        private static readonly Dictionary<string, string> DirectSpawnAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "dakeyras", "dakeyras" },
            { "waylander", "dakeyras" },
            { "greyman", "dakeyras.grey_man" },
            { "gentleman", "dakeyras.grey_man" },
            { "danyal", "danyal" },
            { "dardalion", "dardalion" },
            { "durmast", "durmast" },
            { "cadoras", "cadoras" },
            { "karnak", "karnak" },
            { "egel", "egel" },
            { "gellan", "gellan" },
            { "jonat", "jonat" },
            { "sarvaj", "sarvaj" },
            { "kaem", "kaem" },
            { "orien", "orien" },
            { "hewla", "hewla" },
            { "kai", "kai" },
            { "krylla", "krylla" },
            { "miriel", "miriel" },
            { "kesakhan", "kesa_khan" },
            { "joining", "joining" },
            { "angel", "angel" },
            { "senta", "senta" },
            { "belash", "belash" },
            { "morak", "morak" },
            { "zhuchao", "zhu_chao" },
            { "bodalen", "bodalen" },
            { "ansichen", "ansi_chen" },
            { "innicas", "innicas" },
            { "regnak", "regnak" },
            { "scar", "scar" },
            { "darkbrotherhoodknight", "dark_brotherhood_knight" },
            { "kysumu", "kysumu" },
            { "yuyuliang", "yu_yu_liang" },
            { "ustarte", "ustarte" },
            { "keeva", "keeva_taliana" },
            { "keevataliana", "keeva_taliana" },
            { "matze", "matze_chai" },
            { "matzechai", "matze_chai" },
            { "aric", "aric" },
            { "dukeofkydor", "duke_of_kydor" },
            { "kuanhador", "kuan_hador_demon_lord" },
            { "kuanhadordemonlord", "kuan_hador_demon_lord" },
            { "druss", "druss" },
            { "tenakakhan", "tenaka_khan" },
            { "niallad", "niallad" },
            { "sathulilord", "sathuli_lord" }
        };

        public static void Initialize()
        {
            CommandSystem.Register("AIGMSpawn", AccessLevel.GameMaster, OnSpawn);
            CommandSystem.Register("AIGMSpawnGroup", AccessLevel.GameMaster, OnSpawnGroup);
            CommandSystem.Register("AIGMSpawnList", AccessLevel.GameMaster, OnSpawnList);
            CommandSystem.Register("AIGMWaylanderCleanup", AccessLevel.GameMaster, OnCleanup);
        }

        private static void OnSpawn(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            string[] tokens = SplitArgs(e.ArgString);
            if (tokens.Length == 0)
            {
                e.Mobile.SendMessage("Usage: [AIGMSpawn <character> [duplicate] [lore] [test]");
                return;
            }

            string key = tokens[0];
            bool allowDuplicate = HasToken(tokens, "duplicate");
            bool loreFlag = HasToken(tokens, "lore") || HasToken(tokens, "historical");
            bool testMode = HasToken(tokens, "test") || HasToken(tokens, "passive");

            Mobile spawned = SpawnSingle(e.Mobile, key, allowDuplicate, loreFlag, testMode, out string message);
            e.Mobile.SendMessage(message);

            if (spawned != null)
                e.Mobile.Target = null;
        }

        private static void OnSpawnGroup(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            string[] tokens = SplitArgs(e.ArgString);
            if (tokens.Length == 0)
            {
                e.Mobile.SendMessage("Usage: [AIGMSpawnGroup <book1|book2|book3|crossover|dark_brotherhood_squad|wolfshead_group|kuan_hador_encounter_group> [lore]");
                return;
            }

            string[] members = WaylanderRosterCatalog.GetGroup(tokens[0]);
            if (members.Length == 0)
            {
                e.Mobile.SendMessage("Unknown group '{0}'.", tokens[0]);
                return;
            }

            bool loreFlag = HasToken(tokens, "lore") || HasToken(tokens, "historical");
            bool testMode = HasToken(tokens, "test") || HasToken(tokens, "passive");
            int spawnedCount = 0;
            for (int i = 0; i < members.Length; i++)
            {
                Point3D point = new Point3D(e.Mobile.X + (i % 3), e.Mobile.Y + (i / 3) + 1, e.Mobile.Z);
                Mobile spawned = SpawnSingleAt(e.Mobile, members[i], true, loreFlag, testMode, point, out string ignored);
                if (spawned != null)
                    spawnedCount++;
            }

            e.Mobile.SendMessage("Spawned {0} member(s) from group '{1}'.", spawnedCount, tokens[0]);
        }

        private static void OnSpawnList(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            foreach (KeyValuePair<string, WaylanderCharacterDefinition> entry in WaylanderRosterCatalog.GetAllDefinitions())
            {
                e.Mobile.SendMessage("{0} -> {1}{2}", entry.Key, entry.Value.VisibleName, entry.Value.RequiresLoreFlag ? " [lore]" : String.Empty);
            }
        }

        private static void OnCleanup(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            int radius = 24;
            if (Int32.TryParse(e.ArgString, out int parsed) && parsed > 0)
                radius = parsed;

            int deleted = 0;
            List<Mobile> targets = new List<Mobile>();
            foreach (Mobile mobile in World.Mobiles.Values)
            {
                if (mobile == null || mobile.Deleted || mobile.Map != e.Mobile.Map)
                    continue;

                if (!mobile.InRange(e.Mobile.Location, radius))
                    continue;

                if (mobile is WaylanderRosterMobile || mobile is WaylanderRosterMount || mobile is AIGMCompanionDakeyras || mobile is AIGMCompanionDanyal || mobile is AIGMCompanionDardalion)
                    targets.Add(mobile);
            }

            for (int i = 0; i < targets.Count; i++)
            {
                targets[i].Delete();
                deleted++;
            }

            e.Mobile.SendMessage("Deleted {0} Waylander roster mobile(s) within {1} tiles.", deleted, radius);
        }

        private static void OnDirectSpawn(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            string key;
            if (!DirectSpawnAliases.TryGetValue(e.Command, out key))
            {
                e.Mobile.SendMessage("No Waylander spawn mapping exists for '{0}'.", e.Command);
                return;
            }

            string[] tokens = SplitArgs(e.ArgString);
            bool allowDuplicate = HasToken(tokens, "duplicate");
            bool loreFlag = HasToken(tokens, "lore") || HasToken(tokens, "historical");
            bool testMode = HasToken(tokens, "test") || HasToken(tokens, "passive");

            Mobile spawned = SpawnSingle(e.Mobile, key, allowDuplicate, loreFlag, testMode, out string message);
            e.Mobile.SendMessage(message);

            if (spawned != null)
                e.Mobile.Target = null;
        }

        private static Mobile SpawnSingle(Mobile caller, string key, bool allowDuplicate, bool loreFlag, bool testMode, out string message)
        {
            return SpawnSingleAt(caller, key, allowDuplicate, loreFlag, testMode, caller.Location, out message);
        }

        private static Mobile SpawnSingleAt(Mobile caller, string key, bool allowDuplicate, bool loreFlag, bool testMode, Point3D location, out string message)
        {
            message = "Spawn failed.";

            string normalizedKey = NormalizeKey(key);
            if (String.IsNullOrWhiteSpace(normalizedKey))
            {
                message = "No character key provided.";
                return null;
            }

            WaylanderCharacterDefinition definition = WaylanderRosterCatalog.GetDefinition(normalizedKey);
            if (definition != null && definition.RequiresLoreFlag && !loreFlag)
            {
                message = String.Format("{0} requires the explicit 'lore' flag.", definition.VisibleName);
                return null;
            }

            if (!allowDuplicate && IsUniqueAlreadyAlive(normalizedKey))
            {
                message = String.Format("A live canonical instance of '{0}' is already present.", normalizedKey);
                return null;
            }

            if (!SpawnTypes.TryGetValue(normalizedKey, out Type type))
            {
                message = String.Format("No spawn type registered for '{0}'.", normalizedKey);
                return null;
            }

            Mobile mobile = Activator.CreateInstance(type) as Mobile;
            if (mobile == null)
            {
                message = String.Format("Spawn type '{0}' did not produce a mobile.", type.FullName);
                return null;
            }

            if (mobile is WaylanderRosterMobile rosterMobile)
                rosterMobile.ConfigureSpawnMode(testMode);
            else if (mobile is WaylanderRosterMount rosterMount)
                rosterMount.ConfigureSpawnMode(testMode);

            mobile.MoveToWorld(location, caller.Map);
            AIGMExecutionLog.Write(
                "AIGM_WAYLANDER_SPAWN actor={0} type={1} serial=0x{2:X8} map={3} x={4} y={5} z={6} caller={7} explicitCommand=AIGMSpawn",
                SafeLog(mobile.Name),
                mobile.GetType().Name,
                mobile.Serial.Value,
                mobile.Map != null ? mobile.Map.Name : "null",
                mobile.X,
                mobile.Y,
                mobile.Z,
                SafeLog(caller != null ? caller.Name : null));
            ApplyVariantPresentation(mobile, normalizedKey);
            message = testMode
                ? String.Format("Spawned {0} in passive GM test mode.", mobile.Name ?? normalizedKey)
                : String.Format("Spawned {0}.", mobile.Name ?? normalizedKey);
            return mobile;
        }

        private static bool IsUniqueAlreadyAlive(string normalizedKey)
        {
            WaylanderCharacterDefinition definition = WaylanderRosterCatalog.GetDefinition(normalizedKey);
            if (definition != null && !definition.IsUnique && !definition.RequiresLoreFlag)
                return false;

            string canonical = definition != null ? definition.CanonicalPersonId : normalizedKey;
            foreach (Mobile mobile in World.Mobiles.Values)
            {
                if (mobile == null || mobile.Deleted || !mobile.Alive)
                    continue;

                if (mobile is WaylanderRosterMobile rosterMobile)
                {
                    WaylanderCharacterDefinition live = rosterMobile.Definition;
                    if (live != null && String.Equals(live.CanonicalPersonId, canonical, StringComparison.OrdinalIgnoreCase) && live.IsUnique)
                        return true;
                }
                else if (canonical == "dakeyras" && mobile is AIGMCompanionDakeyras)
                {
                    return true;
                }
                else if (canonical == "danyal" && mobile is AIGMCompanionDanyal)
                {
                    return true;
                }
                else if (canonical == "dardalion" && mobile is AIGMCompanionDardalion)
                {
                    return true;
                }
            }

            return false;
        }

        private static void ApplyVariantPresentation(Mobile mobile, string normalizedKey)
        {
            if (mobile == null)
                return;

            if (String.Equals(normalizedKey, "dakeyras.grey_man", StringComparison.OrdinalIgnoreCase))
            {
                WaylanderCharacterDefinition definition = WaylanderRosterCatalog.GetDefinition(normalizedKey);
                if (definition != null)
                {
                    mobile.Name = definition.VisibleName;
                    mobile.Title = definition.Title;
                }
            }
        }

        private static string NormalizeKey(string key)
        {
            if (String.IsNullOrWhiteSpace(key))
                return String.Empty;

            key = key.Trim().ToLowerInvariant().Replace(' ', '_');
            if (key == "waylander" || key == "dak" || key == "grey_man" || key == "gentleman")
                return "dakeyras";
            return key;
        }

        private static string[] SplitArgs(string args)
        {
            return String.IsNullOrWhiteSpace(args)
                ? Array.Empty<string>()
                : args.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static bool HasToken(string[] tokens, string probe)
        {
            if (tokens == null || String.IsNullOrWhiteSpace(probe))
                return false;

            for (int i = 0; i < tokens.Length; i++)
            {
                if (String.Equals(tokens[i], probe, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static string SafeLog(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return String.Empty;

            string text = value.Replace("\r", " ").Replace("\n", " ").Replace("\"", "'");
            return text.Length > 120 ? text.Substring(0, 120) : text;
        }

        private static void RegisterDirectSpawnAliases()
        {
            foreach (KeyValuePair<string, string> entry in DirectSpawnAliases)
            {
                if (CommandSystem.Entries.ContainsKey(entry.Key))
                    continue;

                CommandSystem.Register(entry.Key, AccessLevel.GameMaster, OnDirectSpawn);
            }
        }
    }
}
