using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Server.Custom.AIGM.Characters.Waylander
{
    public static class WaylanderPersonaRepository
    {
        private static readonly object SyncRoot = new object();
        private static Dictionary<string, string> _cache;

        public static string GetPersonaBlock(string assetKey)
        {
            if (String.IsNullOrWhiteSpace(assetKey))
                return String.Empty;

            EnsureLoaded();

            string value;
            return _cache.TryGetValue(assetKey.Trim(), out value) ? value : String.Empty;
        }

        public static IReadOnlyDictionary<string, string> GetAll()
        {
            EnsureLoaded();
            return _cache;
        }

        private static void EnsureLoaded()
        {
            if (_cache != null)
                return;

            lock (SyncRoot)
            {
                if (_cache != null)
                    return;

                Dictionary<string, string> loaded = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                string path = Path.Combine(Core.BaseDirectory, "Data", "AIGM", "Personas", "Waylander", "waylander_roster_personas.txt");

                if (File.Exists(path))
                    ParseAssetFile(loaded, File.ReadAllLines(path));

                _cache = loaded;
            }
        }

        private static void ParseAssetFile(Dictionary<string, string> loaded, string[] lines)
        {
            if (loaded == null || lines == null || lines.Length == 0)
                return;

            string currentKey = null;
            StringBuilder block = new StringBuilder();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i] ?? String.Empty;

                if (line.StartsWith("=== ", StringComparison.Ordinal) && line.EndsWith(" ===", StringComparison.Ordinal))
                {
                    Flush(loaded, currentKey, block);
                    currentKey = line.Substring(4, line.Length - 8).Trim();
                    block.Length = 0;
                    continue;
                }

                if (currentKey != null)
                    block.AppendLine(line);
            }

            Flush(loaded, currentKey, block);
        }

        private static void Flush(Dictionary<string, string> loaded, string key, StringBuilder block)
        {
            if (loaded == null || String.IsNullOrWhiteSpace(key) || block == null)
                return;

            loaded[key] = block.ToString().Trim();
        }
    }
}
