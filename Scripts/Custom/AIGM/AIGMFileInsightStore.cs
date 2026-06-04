using System.Collections.Generic;

namespace Server.Custom.AIGM
{
    public static class AIGMFileInsightStore
    {
        private static readonly Dictionary<string, string> Insights = new Dictionary<string, string>();

        public static void Set(string path, string insight)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            Insights[path] = insight ?? string.Empty;
        }

        public static string Get(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            string value;
            return Insights.TryGetValue(path, out value) ? value : null;
        }
    }
}
