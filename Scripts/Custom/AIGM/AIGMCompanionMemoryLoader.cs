using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionMemoryLoader
    {
        private sealed class CacheEntry
        {
            public string Text;
            public DateTime CachedUtc;
        }

        private static readonly ConcurrentDictionary<string, CacheEntry> Cache = new ConcurrentDictionary<string, CacheEntry>(StringComparer.OrdinalIgnoreCase);
        private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(10.0);
        private static readonly string MemoryDirectory = @"C:\.openclaw\workspace-ultima-online\memory";

        public static string LoadDakeyrasMemory()
        {
            return LoadMemoryFromPath(Path.Combine(MemoryDirectory, "dakeyras-profile.md"));
        }

        public static string LoadDanyalMemory()
        {
            return LoadMemoryFromPath(Path.Combine(MemoryDirectory, "danyal-profile.md"));
        }

        public static string LoadDardalionMemory()
        {
            return LoadMemoryFromPath(Path.Combine(MemoryDirectory, "dardalion-profile.md"));
        }

        public static string LoadMemoryForCompanion(Mobile companion)
        {
            if (companion is AIGMCompanionDanyal)
                return LoadDanyalMemory();

            if (companion is AIGMCompanionDardalion)
                return LoadDardalionMemory();

            return LoadDakeyrasMemory();
        }

        private static string LoadMemoryFromPath(string path)
        {
            try
            {
                if (String.IsNullOrWhiteSpace(path))
                    return String.Empty;

                DateTime now = DateTime.UtcNow;
                CacheEntry entry;
                if (Cache.TryGetValue(path, out entry) && entry != null && !String.IsNullOrWhiteSpace(entry.Text) && (now - entry.CachedUtc) < CacheDuration)
                    return entry.Text;

                if (!File.Exists(path))
                    return String.Empty;

                string text = File.ReadAllText(path, Encoding.UTF8) ?? String.Empty;
                Cache[path] = new CacheEntry { Text = text, CachedUtc = now };
                return text;
            }
            catch
            {
                return String.Empty;
            }
        }
    }
}
