using System;
using System.Collections.Generic;

namespace Server.Custom.AIGM
{
    public sealed class AIGMPropertySnapshot
    {
        public string Kind { get; set; }
        public string TypeName { get; set; }
        public string Name { get; set; }
        public string Serial { get; set; }
        public string MapName { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public int Hue { get; set; }
        public bool Deleted { get; set; }
        public bool Movable { get; set; }
        public bool? Alive { get; set; }
        public bool? Blessed { get; set; }
        public int? Amount { get; set; }
        public int? ItemCount { get; set; }
        public int? Hits { get; set; }
        public int? HitsMax { get; set; }
        public int? Mana { get; set; }
        public int? ManaMax { get; set; }
        public int? Stam { get; set; }
        public int? StamMax { get; set; }
        public int? Str { get; set; }
        public int? Dex { get; set; }
        public int? Int { get; set; }
        public int? ItemID { get; set; }
        public double? Weight { get; set; }
        public string Layer { get; set; }
        public string AccessLevel { get; set; }
        public List<KeyValuePair<string, string>> Properties { get; private set; }

        public AIGMPropertySnapshot()
        {
            Properties = new List<KeyValuePair<string, string>>();
        }

        public string ToDisplayHtml()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendFormat("Kind: {0}<br>", Utility.FixHtml(Kind ?? "Unknown"));
            sb.AppendFormat("Type: {0}<br>", Utility.FixHtml(TypeName ?? "Unknown"));
            sb.AppendFormat("Name: {0}<br>", Utility.FixHtml(Name ?? "(none)"));
            sb.AppendFormat("Serial: {0}<br>", Utility.FixHtml(Serial ?? "Unknown"));
            sb.AppendFormat("Map: {0}<br>", Utility.FixHtml(MapName ?? "Unknown"));
            sb.AppendFormat("Location: {0},{1},{2}<br>", X, Y, Z);
            sb.AppendFormat("Hue: {0}<br>", Hue);
            sb.AppendFormat("Deleted: {0}<br>", Deleted);
            sb.AppendFormat("Movable: {0}<br>", Movable);

            if (Alive.HasValue)
                sb.AppendFormat("Alive: {0}<br>", Alive.Value);

            if (Blessed.HasValue)
                sb.AppendFormat("Blessed: {0}<br>", Blessed.Value);

            if (Amount.HasValue)
                sb.AppendFormat("Amount: {0}<br>", Amount.Value);

            if (ItemCount.HasValue)
                sb.AppendFormat("Contained Items: {0}<br>", ItemCount.Value);

            if (Hits.HasValue || HitsMax.HasValue)
                sb.AppendFormat("Hits: {0}/{1}<br>", Hits.HasValue ? Hits.Value.ToString() : "?", HitsMax.HasValue ? HitsMax.Value.ToString() : "?");

            if (Mana.HasValue || ManaMax.HasValue)
                sb.AppendFormat("Mana: {0}/{1}<br>", Mana.HasValue ? Mana.Value.ToString() : "?", ManaMax.HasValue ? ManaMax.Value.ToString() : "?");

            if (Stam.HasValue || StamMax.HasValue)
                sb.AppendFormat("Stam: {0}/{1}<br>", Stam.HasValue ? Stam.Value.ToString() : "?", StamMax.HasValue ? StamMax.Value.ToString() : "?");

            if (Str.HasValue || Dex.HasValue || Int.HasValue)
                sb.AppendFormat("Stats: STR {0} / DEX {1} / INT {2}<br>", Str.HasValue ? Str.Value.ToString() : "?", Dex.HasValue ? Dex.Value.ToString() : "?", Int.HasValue ? Int.Value.ToString() : "?");

            if (ItemID.HasValue)
                sb.AppendFormat("ItemID: {0}<br>", ItemID.Value);

            if (Weight.HasValue)
                sb.AppendFormat("Weight: {0}<br>", Weight.Value);

            if (!String.IsNullOrWhiteSpace(Layer))
                sb.AppendFormat("Layer: {0}<br>", Utility.FixHtml(Layer));

            if (!String.IsNullOrWhiteSpace(AccessLevel))
                sb.AppendFormat("AccessLevel: {0}<br>", Utility.FixHtml(AccessLevel));

            if (Properties != null && Properties.Count > 0)
            {
                sb.Append("<br>Properties:<br>");
                for (int i = 0; i < Properties.Count; i++)
                {
                    KeyValuePair<string, string> pair = Properties[i];
                    sb.AppendFormat("- {0}: {1}<br>", Utility.FixHtml(pair.Key), Utility.FixHtml(pair.Value));
                }
            }

            return sb.ToString();
        }
    }
}
