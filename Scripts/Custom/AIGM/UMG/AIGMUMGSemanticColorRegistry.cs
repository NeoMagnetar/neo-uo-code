using System;
using System.Collections.Generic;

namespace Server.Custom.AIGM.UMG
{
    public sealed class AIGMUMGSemanticColor
    {
        public AIGMUMGMoltType MoltType { get; private set; }
        public string Name { get; private set; }
        public string Hex { get; private set; }
        public int UOHue { get; private set; }
        public string Marker { get; private set; }
        public string Function { get; private set; }

        public AIGMUMGSemanticColor(AIGMUMGMoltType moltType, string name, string hex, int uoHue, string marker, string function)
        {
            MoltType = moltType;
            Name = name;
            Hex = hex;
            UOHue = uoHue;
            Marker = marker;
            Function = function;
        }
    }

    public static class AIGMUMGSemanticColorRegistry
    {
        private static readonly Dictionary<AIGMUMGMoltType, AIGMUMGSemanticColor> Colors =
            new Dictionary<AIGMUMGMoltType, AIGMUMGSemanticColor>
            {
                { AIGMUMGMoltType.Trigger, new AIGMUMGSemanticColor(AIGMUMGMoltType.Trigger, "Trigger", "#8F2636", 0x22, "!", "initiates activation or generation") },
                { AIGMUMGMoltType.Directive, new AIGMUMGSemanticColor(AIGMUMGMoltType.Directive, "Directive", "#C96B00", 0x2B, ">", "guides the approach and tone") },
                { AIGMUMGMoltType.Instruction, new AIGMUMGSemanticColor(AIGMUMGMoltType.Instruction, "Instruction", "#C9A500", 0x35, "#", "specific rules or format commands") },
                { AIGMUMGMoltType.Subject, new AIGMUMGSemanticColor(AIGMUMGMoltType.Subject, "Subject", "#009B57", 0x3F, "@", "topic, target, entity, or operational focus") },
                { AIGMUMGMoltType.Primary, new AIGMUMGSemanticColor(AIGMUMGMoltType.Primary, "Primary", "#168AC8", 0x59, "*", "core value anchor that must hold") },
                { AIGMUMGMoltType.Philosophy, new AIGMUMGSemanticColor(AIGMUMGMoltType.Philosophy, "Philosophy", "#7D3FA0", 0x54, "~", "interpretive stance or guiding lens") },
                { AIGMUMGMoltType.Blueprint, new AIGMUMGSemanticColor(AIGMUMGMoltType.Blueprint, "Blueprint", "#C52D70", 0x4B, "=", "output, behavior, sequence, or structural plan") }
            };

        public static AIGMUMGSemanticColor Get(AIGMUMGMoltType type)
        {
            AIGMUMGSemanticColor color;
            return Colors.TryGetValue(type, out color)
                ? color
                : Colors[AIGMUMGMoltType.Instruction];
        }

        public static int GetUOHue(AIGMUMGMoltType type)
        {
            return Get(type).UOHue;
        }

        public static string GetHex(AIGMUMGMoltType type)
        {
            return Get(type).Hex;
        }

        public static string FormatLabel(AIGMUMGMoltType type)
        {
            AIGMUMGSemanticColor color = Get(type);
            return color.Marker + " " + color.Name.ToUpperInvariant();
        }

        public static string BuildDocumentationTable()
        {
            List<string> lines = new List<string>();
            lines.Add("MOLT Type,Hex,UO Hue,Marker,Function");
            foreach (AIGMUMGMoltType type in Enum.GetValues(typeof(AIGMUMGMoltType)))
            {
                AIGMUMGSemanticColor color = Get(type);
                lines.Add(String.Format("{0},{1},0x{2:X},{3},{4}", color.Name, color.Hex, color.UOHue, color.Marker, color.Function));
            }

            return String.Join(Environment.NewLine, lines.ToArray());
        }
    }
}
