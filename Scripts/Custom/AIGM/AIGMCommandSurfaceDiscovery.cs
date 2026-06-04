using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Server.Commands;

namespace Server.Custom.AIGM
{
    public static class AIGMCommandSurfaceDiscovery
    {
        public static void Initialize()
        {
            CommandSystem.Register("AIGMDumpGMSurface", AccessLevel.Administrator, new CommandEventHandler(OnCommand));
        }

        private static void OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            try
            {
                int count = DumpCommandSurface();
                from.SendMessage(68, "AIGM command surface dumped. Commands={0}", count);
            }
            catch (Exception ex)
            {
                from.SendMessage(33, "AIGM command surface dump failed: {0}", ex.Message);
                AIGMExecutionLog.Write("COMMAND_SURFACE_DUMP_FAIL {0}", ex);
            }
        }

        public static int DumpCommandSurface()
        {
            string logDir = Path.Combine(Core.BaseDirectory, "Logs");

            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);

            string txtPath = Path.Combine(logDir, "AIGMCommands.txt");
            string jsonPath = Path.Combine(logDir, "AIGMCommands.json");

            IDictionary entries = FindCommandEntries();

            if (entries == null)
            {
                File.WriteAllText(txtPath, "NO_COMMAND_DICTIONARY_FOUND" + Environment.NewLine);
                File.WriteAllText(jsonPath, "[]" + Environment.NewLine);
                return 0;
            }

            List<CommandSurfaceEntry> list = new List<CommandSurfaceEntry>();

            foreach (DictionaryEntry pair in entries)
            {
                string name = pair.Key == null ? String.Empty : pair.Key.ToString();
                object entry = pair.Value;

                if (String.IsNullOrWhiteSpace(name) || entry == null)
                    continue;

                CommandSurfaceEntry surfaceEntry = BuildEntry(name, entry);
                list.Add(surfaceEntry);
            }

            list.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name));

            File.WriteAllText(txtPath, BuildText(list));
            File.WriteAllText(jsonPath, BuildJson(list));

            AIGMExecutionLog.Write("COMMAND_SURFACE_DUMP_OK count={0}", list.Count);
            return list.Count;
        }

        private static IDictionary FindCommandEntries()
        {
            Type commandSystemType = typeof(CommandSystem);

            FieldInfo[] fields = commandSystemType.GetFields(
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.NonPublic);

            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];

                if (!typeof(IDictionary).IsAssignableFrom(field.FieldType))
                    continue;

                object value = field.GetValue(null);
                IDictionary dict = value as IDictionary;

                if (dict == null)
                    continue;

                if (LooksLikeCommandTable(dict))
                {
                    AIGMExecutionLog.Write(
                        "COMMAND_SURFACE_DICTIONARY_FOUND field={0} type={1} count={2}",
                        field.Name,
                        field.FieldType.FullName,
                        dict.Count);

                    return dict;
                }
            }

            return null;
        }

        private static bool LooksLikeCommandTable(IDictionary dict)
        {
            if (dict == null || dict.Count == 0)
                return false;

            int inspected = 0;

            foreach (DictionaryEntry pair in dict)
            {
                inspected++;

                if (!(pair.Key is string))
                    return false;

                object entry = pair.Value;

                if (entry == null)
                    continue;

                string access = ReadMember(entry, "AccessLevel", "m_AccessLevel", "_accessLevel");

                if (!String.IsNullOrWhiteSpace(access))
                    return true;

                if (inspected >= 5)
                    break;
            }

            return true;
        }

        private static CommandSurfaceEntry BuildEntry(string name, object entry)
        {
            Type type = entry.GetType();

            CommandSurfaceEntry result = new CommandSurfaceEntry();
            result.Name = name;
            result.AccessLevel = ReadMember(entry, "AccessLevel", "m_AccessLevel", "_accessLevel");
            result.HandlerName = DescribeHandler(ReadObjectMember(entry, "Handler", "m_Handler", "_handler"));
            result.Aliases = ReadAliases(entry);
            result.SourceType = type.FullName;
            result.DeclaringType = ExtractDeclaringType(ReadObjectMember(entry, "Handler", "m_Handler", "_handler"));

            if (String.IsNullOrWhiteSpace(result.AccessLevel))
                result.AccessLevel = "(unknown)";

            if (String.IsNullOrWhiteSpace(result.HandlerName))
                result.HandlerName = "(unknown)";

            return result;
        }

        private static string ReadAliases(object instance)
        {
            object aliases = ReadObjectMember(instance, "Aliases", "m_Aliases", "_aliases");
            IEnumerable enumerable = aliases as IEnumerable;

            if (enumerable == null || aliases is string)
                return String.Empty;

            List<string> parts = new List<string>();
            foreach (object value in enumerable)
            {
                if (value == null)
                    continue;

                string text = value.ToString();
                if (!String.IsNullOrWhiteSpace(text))
                    parts.Add(text);
            }

            return String.Join(",", parts.ToArray());
        }

        private static string DescribeHandler(object handler)
        {
            if (handler == null)
                return null;

            Delegate del = handler as Delegate;
            if (del != null)
            {
                MethodInfo method = del.Method;
                if (method == null)
                    return del.ToString();

                string declaring = method.DeclaringType != null ? method.DeclaringType.FullName : "(unknown)";
                return declaring + "." + method.Name;
            }

            return handler.ToString();
        }

        private static string ExtractDeclaringType(object handler)
        {
            Delegate del = handler as Delegate;
            if (del == null || del.Method == null || del.Method.DeclaringType == null)
                return String.Empty;

            return del.Method.DeclaringType.FullName;
        }

        private static string BuildText(List<CommandSurfaceEntry> list)
        {
            StringBuilder txt = new StringBuilder();

            for (int i = 0; i < list.Count; i++)
            {
                CommandSurfaceEntry entry = list[i];
                txt.Append(entry.Name).Append('\t')
                   .Append(entry.AccessLevel).Append('\t')
                   .Append(entry.HandlerName);

                if (!String.IsNullOrWhiteSpace(entry.Aliases))
                    txt.Append('\t').Append("aliases=").Append(entry.Aliases);

                if (!String.IsNullOrWhiteSpace(entry.DeclaringType))
                    txt.Append('\t').Append("declaring=").Append(entry.DeclaringType);

                txt.AppendLine();
            }

            return txt.ToString();
        }

        private static string BuildJson(List<CommandSurfaceEntry> list)
        {
            StringBuilder json = new StringBuilder();
            json.AppendLine("[");

            for (int i = 0; i < list.Count; i++)
            {
                CommandSurfaceEntry entry = list[i];
                if (i > 0)
                    json.AppendLine(",");

                json.Append("  { ");
                json.AppendFormat("\"name\":\"{0}\", ", EscapeJson(entry.Name));
                json.AppendFormat("\"accessLevel\":\"{0}\", ", EscapeJson(entry.AccessLevel));
                json.AppendFormat("\"handler\":\"{0}\", ", EscapeJson(entry.HandlerName));
                json.AppendFormat("\"aliases\":\"{0}\", ", EscapeJson(entry.Aliases));
                json.AppendFormat("\"sourceType\":\"{0}\", ", EscapeJson(entry.SourceType));
                json.AppendFormat("\"declaringType\":\"{0}\"", EscapeJson(entry.DeclaringType));
                json.Append(" }");
            }

            json.AppendLine();
            json.AppendLine("]");
            return json.ToString();
        }

        private static string ReadMember(object instance, params string[] names)
        {
            object value = ReadObjectMember(instance, names);
            return value == null ? null : value.ToString();
        }

        private static object ReadObjectMember(object instance, params string[] names)
        {
            if (instance == null || names == null)
                return null;

            Type type = instance.GetType();

            for (int i = 0; i < names.Length; i++)
            {
                string name = names[i];

                PropertyInfo prop = type.GetProperty(
                    name,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

                if (prop != null)
                    return prop.GetValue(instance, null);

                FieldInfo field = type.GetField(
                    name,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

                if (field != null)
                    return field.GetValue(instance);
            }

            return null;
        }

        private static string EscapeJson(string value)
        {
            if (value == null)
                return String.Empty;

            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");
        }

        private sealed class CommandSurfaceEntry
        {
            public string Name;
            public string AccessLevel;
            public string HandlerName;
            public string Aliases;
            public string SourceType;
            public string DeclaringType;
        }
    }
}
