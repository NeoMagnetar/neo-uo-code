using System;

namespace Server.Custom.AIGM.UMG
{
    public sealed class UMGPartyCognitionBlock
    {
        public string Id { get; private set; }
        public string Category { get; private set; }
        public string Name { get; private set; }
        public string Summary { get; private set; }
        public string[] Triggers { get; private set; }
        public string[] Directives { get; private set; }
        public string[] Constraints { get; private set; }
        public string[] Tags { get; private set; }

        public UMGPartyCognitionBlock(string id, string category, string name, string summary, string[] triggers, string[] directives, string[] constraints, string[] tags)
        {
            Id = Clean(id);
            Category = Clean(category);
            Name = Clean(name);
            Summary = Clean(summary);
            Triggers = CleanArray(triggers);
            Directives = CleanArray(directives);
            Constraints = CleanArray(constraints);
            Tags = CleanArray(tags);
        }

        public string FormatCompact()
        {
            return String.Format("{0}/{1}: {2}", Category, Name, Summary);
        }

        private static string Clean(string value)
        {
            return String.IsNullOrWhiteSpace(value) ? String.Empty : value.Trim();
        }

        private static string[] CleanArray(string[] values)
        {
            if (values == null || values.Length == 0)
                return new string[0];

            string[] clean = new string[values.Length];
            for (int i = 0; i < values.Length; i++)
                clean[i] = Clean(values[i]);

            return clean;
        }
    }
}
