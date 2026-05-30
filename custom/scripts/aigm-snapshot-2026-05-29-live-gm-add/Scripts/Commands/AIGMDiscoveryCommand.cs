using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Server.Items;
using Server.Mobiles;
using Server.Multis;

namespace Server.Commands
{
    public static class AIGMDiscoveryCommand
    {
        public static void Initialize()
        {
            CommandSystem.Register("AIGMDumpConstructables", AccessLevel.GameMaster, new CommandEventHandler(AIGMDumpConstructables_OnCommand));
        }

        [Usage("AIGMDumpConstructables")]
        [Description("Dumps registered GM commands and constructable item/mobile surfaces for AIGM discovery.")]
        private static void AIGMDumpConstructables_OnCommand(CommandEventArgs e)
        {
            string logsDir = Path.Combine(Core.BaseDirectory, "Logs");
            Directory.CreateDirectory(logsDir);

            List<ConstructableEntry> entries = BuildConstructableEntries();
            List<CommandEntryInfo> commands = BuildCommandEntries();

            WriteJson(Path.Combine(logsDir, "AIGMConstructables.json"), entries, commands);
            WriteText(Path.Combine(logsDir, "AIGMConstructables.Items.txt"), entries.Where(x => x.IsItem));
            WriteText(Path.Combine(logsDir, "AIGMConstructables.Mobiles.txt"), entries.Where(x => x.IsMobile));
            WriteText(Path.Combine(logsDir, "AIGMConstructables.Houses.txt"), entries.Where(x => x.IsBaseHouse || x.IsHouseDeed));
            WriteText(Path.Combine(logsDir, "AIGMConstructables.Addons.txt"), entries.Where(x => x.IsAddonDeed));
            WriteCommands(Path.Combine(logsDir, "AIGMCommands.txt"), commands);

            int vendors = entries.Count(x => x.IsVendor);
            int monsters = entries.Count(x => x.IsMonster);
            int bosses = entries.Count(x => x.IsBoss);
            int resources = entries.Count(x => x.IsResource);
            int containers = entries.Count(x => x.IsContainer);
            int items = entries.Count(x => x.IsItem);
            int mobiles = entries.Count(x => x.IsMobile);
            int houseDeeds = entries.Count(x => x.IsHouseDeed);
            int addonDeeds = entries.Count(x => x.IsAddonDeed);

            e.Mobile.SendMessage("AIGM discovery dump complete.");
            e.Mobile.SendMessage("Commands: {0}", commands.Count);
            e.Mobile.SendMessage("Items: {0} Mobiles: {1} HouseDeeds: {2} AddonDeeds: {3}", items, mobiles, houseDeeds, addonDeeds);
            e.Mobile.SendMessage("Vendors: {0} Monsters: {1} Bosses: {2} Resources: {3} Containers: {4}", vendors, monsters, bosses, resources, containers);
        }

        private static List<ConstructableEntry> BuildConstructableEntries()
        {
            List<ConstructableEntry> list = new List<ConstructableEntry>();
            Type[] types = ScriptCompiler.Assemblies.SelectMany(a => SafeGetTypes(a)).ToArray();

            for (int i = 0; i < types.Length; i++)
            {
                Type type = types[i];
                if (type == null || type.IsAbstract || type.IsInterface)
                    continue;

                ConstructorInfo[] ctors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
                ConstructorInfo[] constructableCtors = ctors.Where(c => c.GetCustomAttributes(typeof(ConstructableAttribute), false).Length > 0).ToArray();
                if (constructableCtors.Length == 0)
                    continue;

                ConstructableEntry entry = new ConstructableEntry();
                entry.TypeName = type.Name;
                entry.FullName = type.FullName;
                entry.BaseClass = type.BaseType != null ? type.BaseType.FullName : String.Empty;
                entry.Namespace = type.Namespace ?? String.Empty;
                entry.IsItem = typeof(Item).IsAssignableFrom(type);
                entry.IsMobile = typeof(Mobile).IsAssignableFrom(type);
                entry.IsContainer = typeof(Container).IsAssignableFrom(type);
                entry.IsBaseHouse = typeof(BaseHouse).IsAssignableFrom(type);
                entry.IsHouseDeed = type.Name.IndexOf("Deed", StringComparison.OrdinalIgnoreCase) >= 0 && type.Name.IndexOf("House", StringComparison.OrdinalIgnoreCase) >= 0;
                entry.IsAddonDeed = type.Name.IndexOf("AddonDeed", StringComparison.OrdinalIgnoreCase) >= 0;
                entry.IsWeapon = typeof(BaseWeapon).IsAssignableFrom(type);
                entry.IsArmor = typeof(BaseArmor).IsAssignableFrom(type);
                entry.IsResource = IsResourceType(type);
                entry.IsVendor = typeof(BaseVendor).IsAssignableFrom(type) || type.Name.IndexOf("Vendor", StringComparison.OrdinalIgnoreCase) >= 0;
                entry.IsMonster = entry.IsMobile && typeof(BaseCreature).IsAssignableFrom(type) && !entry.IsVendor && !IsLikelyHumanNpc(type);
                entry.IsBoss = entry.IsMonster && IsBossType(type);
                entry.Constructors = constructableCtors.Select(FormatConstructor).ToList();
                list.Add(entry);
            }

            return list.OrderBy(x => x.FullName).ToList();
        }

        private static List<CommandEntryInfo> BuildCommandEntries()
        {
            List<CommandEntryInfo> list = new List<CommandEntryInfo>();
            IDictionary table = Server.Custom.AIGM.AIGMCommandSurfaceDiscovery.DumpCommandSurface() >= 0 ? GetCommandTable() : null;
            if (table == null)
                return list;

            foreach (DictionaryEntry entry in table)
            {
                string name = entry.Key as string;
                if (String.IsNullOrWhiteSpace(name))
                    continue;

                object value = entry.Value;
                if (value == null)
                    continue;

                Type type = value.GetType();
                CommandEntryInfo info = new CommandEntryInfo();
                info.Name = name;
                info.EntryType = type.FullName;

                PropertyInfo accessProp = type.GetProperty("AccessLevel", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (accessProp != null)
                {
                    object access = accessProp.GetValue(value, null);
                    info.AccessLevel = access != null ? access.ToString() : String.Empty;
                }

                list.Add(info);
            }

            return list.OrderBy(x => x.Name).ToList();
        }

        private static IDictionary GetCommandTable()
        {
            Type type = typeof(CommandSystem);
            FieldInfo[] fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);

            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];
                if (!typeof(IDictionary).IsAssignableFrom(field.FieldType))
                    continue;

                IDictionary dict = field.GetValue(null) as IDictionary;
                if (dict != null && dict.Count > 0)
                    return dict;
            }

            return null;
        }

        private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null);
            }
            catch
            {
                return Type.EmptyTypes;
            }
        }

        private static bool IsResourceType(Type type)
        {
            string n = type.Name;
            return n.IndexOf("Ingot", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Leather", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Log", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Board", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Cloth", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Reagent", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Gem", StringComparison.OrdinalIgnoreCase) >= 0
                || n.Equals("Gold", StringComparison.OrdinalIgnoreCase)
                || n.Equals("Bandage", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsLikelyHumanNpc(Type type)
        {
            string n = type.Name;
            return n.IndexOf("Healer", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Banker", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Mage", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Smith", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Vendor", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Counselor", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsBossType(Type type)
        {
            string n = type.Name;
            return n.IndexOf("Champion", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Peerless", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Harrower", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Ancient", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Queen", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Lord", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string FormatConstructor(ConstructorInfo ctor)
        {
            ParameterInfo[] args = ctor.GetParameters();
            StringBuilder sb = new StringBuilder();
            sb.Append(ctor.DeclaringType != null ? ctor.DeclaringType.Name : "ctor");
            sb.Append('(');
            for (int i = 0; i < args.Length; i++)
            {
                if (i > 0)
                    sb.Append(", ");

                sb.Append(args[i].ParameterType.Name);
                sb.Append(' ');
                sb.Append(args[i].Name);
            }
            sb.Append(')');
            return sb.ToString();
        }

        private static void WriteText(string path, IEnumerable<ConstructableEntry> entries)
        {
            StringBuilder sb = new StringBuilder();
            foreach (ConstructableEntry e in entries)
            {
                sb.AppendLine(e.FullName);
            }
            File.WriteAllText(path, sb.ToString());
        }

        private static void WriteCommands(string path, IEnumerable<CommandEntryInfo> commands)
        {
            StringBuilder sb = new StringBuilder();
            foreach (CommandEntryInfo c in commands)
            {
                sb.Append(c.Name);
                if (!String.IsNullOrWhiteSpace(c.AccessLevel))
                {
                    sb.Append(" | ");
                    sb.Append(c.AccessLevel);
                }
                sb.AppendLine();
            }
            File.WriteAllText(path, sb.ToString());
        }

        private static void WriteJson(string path, List<ConstructableEntry> entries, List<CommandEntryInfo> commands)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("  \"commands\": [");
            for (int i = 0; i < commands.Count; i++)
            {
                CommandEntryInfo c = commands[i];
                sb.Append("    { \"name\": \"").Append(Escape(c.Name)).Append("\", \"accessLevel\": \"").Append(Escape(c.AccessLevel)).Append("\", \"entryType\": \"").Append(Escape(c.EntryType)).Append("\" }");
                if (i < commands.Count - 1)
                    sb.Append(',');
                sb.AppendLine();
            }
            sb.AppendLine("  ],");
            sb.AppendLine("  \"constructables\": [");
            for (int i = 0; i < entries.Count; i++)
            {
                ConstructableEntry e = entries[i];
                sb.Append("    {");
                sb.Append("\"typeName\":\"").Append(Escape(e.TypeName)).Append("\",");
                sb.Append("\"fullName\":\"").Append(Escape(e.FullName)).Append("\",");
                sb.Append("\"baseClass\":\"").Append(Escape(e.BaseClass)).Append("\",");
                sb.Append("\"namespace\":\"").Append(Escape(e.Namespace)).Append("\",");
                sb.Append("\"isItem\":").Append(e.IsItem.ToString().ToLowerInvariant()).Append(',');
                sb.Append("\"isMobile\":").Append(e.IsMobile.ToString().ToLowerInvariant()).Append(',');
                sb.Append("\"isContainer\":").Append(e.IsContainer.ToString().ToLowerInvariant()).Append(',');
                sb.Append("\"isBaseHouse\":").Append(e.IsBaseHouse.ToString().ToLowerInvariant()).Append(',');
                sb.Append("\"isHouseDeed\":").Append(e.IsHouseDeed.ToString().ToLowerInvariant()).Append(',');
                sb.Append("\"isAddonDeed\":").Append(e.IsAddonDeed.ToString().ToLowerInvariant()).Append(',');
                sb.Append("\"isWeapon\":").Append(e.IsWeapon.ToString().ToLowerInvariant()).Append(',');
                sb.Append("\"isArmor\":").Append(e.IsArmor.ToString().ToLowerInvariant()).Append(',');
                sb.Append("\"isResource\":").Append(e.IsResource.ToString().ToLowerInvariant()).Append(',');
                sb.Append("\"isVendor\":").Append(e.IsVendor.ToString().ToLowerInvariant()).Append(',');
                sb.Append("\"isMonster\":").Append(e.IsMonster.ToString().ToLowerInvariant()).Append(',');
                sb.Append("\"isBoss\":").Append(e.IsBoss.ToString().ToLowerInvariant()).Append(',');
                sb.Append("\"constructors\":[");
                for (int j = 0; j < e.Constructors.Count; j++)
                {
                    sb.Append('"').Append(Escape(e.Constructors[j])).Append('"');
                    if (j < e.Constructors.Count - 1)
                        sb.Append(',');
                }
                sb.Append(']');
                sb.Append('}');
                if (i < entries.Count - 1)
                    sb.Append(',');
                sb.AppendLine();
            }
            sb.AppendLine("  ]");
            sb.AppendLine("}");
            File.WriteAllText(path, sb.ToString());
        }

        private static string Escape(string s)
        {
            return (s ?? String.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private sealed class ConstructableEntry
        {
            public string TypeName;
            public string FullName;
            public string BaseClass;
            public string Namespace;
            public bool IsItem;
            public bool IsMobile;
            public bool IsContainer;
            public bool IsBaseHouse;
            public bool IsHouseDeed;
            public bool IsAddonDeed;
            public bool IsWeapon;
            public bool IsArmor;
            public bool IsResource;
            public bool IsVendor;
            public bool IsMonster;
            public bool IsBoss;
            public List<string> Constructors;
        }

        private sealed class CommandEntryInfo
        {
            public string Name;
            public string AccessLevel;
            public string EntryType;
        }
    }
}
