using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Server.Custom.AIGM.UMG
{
    public static class AIGMUMGLog
    {
        private static readonly object SyncRoot = new object();

        public static void Write(string eventName, Mobile actor, IDictionary<string, string> fields)
        {
            try
            {
                string dir = Path.Combine(Core.BaseDirectory, "Logs", "AIGM", "UMG");
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string path = Path.Combine(dir, "umg_runtime.jsonl");
                StringBuilder sb = new StringBuilder();
                sb.Append('{');
                AppendJson(sb, "timestampUtc", DateTime.UtcNow.ToString("o"), true);
                AppendJson(sb, "event", eventName ?? String.Empty, false);
                AppendJson(sb, "npc", actor != null ? (actor.Name ?? actor.GetType().Name) : String.Empty, false);
                AppendJson(sb, "serial", actor != null ? String.Format("0x{0:X8}", actor.Serial.Value) : String.Empty, false);

                if (fields != null)
                {
                    foreach (KeyValuePair<string, string> pair in fields)
                        AppendJson(sb, pair.Key, pair.Value, false);
                }

                sb.Append('}');

                lock (SyncRoot)
                {
                    File.AppendAllText(path, sb.ToString() + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch
            {
            }
        }

        public static Dictionary<string, string> Fields(params string[] values)
        {
            Dictionary<string, string> fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (values == null)
                return fields;

            for (int i = 0; i + 1 < values.Length; i += 2)
                fields[values[i] ?? String.Empty] = values[i + 1] ?? String.Empty;

            return fields;
        }

        private static void AppendJson(StringBuilder sb, string key, string value, bool first)
        {
            if (!first)
                sb.Append(',');

            sb.Append('"').Append(Escape(key)).Append("\":\"").Append(Escape(value)).Append('"');
        }

        private static string Escape(string value)
        {
            if (String.IsNullOrEmpty(value))
                return String.Empty;

            return value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", " ").Replace("\n", " ");
        }
    }
}
