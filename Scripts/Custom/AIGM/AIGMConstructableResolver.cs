using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Server.Commands;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public enum AIGMConstructableKind
    {
        Any,
        Item,
        Mobile
    }

    public sealed class AIGMConstructableResolution
    {
        public string RawInput;
        public string NormalizedInput;
        public string CanonicalTypeName;
        public string MatchSource;
        public string FailureReason;
        public Type ResolvedType;
        public bool IsItem;
        public bool IsMobile;
        public bool HasConstructableConstructor;

        public bool Success
        {
            get { return ResolvedType != null && HasConstructableConstructor; }
        }
    }

    public static class AIGMConstructableResolver
    {
        private static readonly Dictionary<string, string> Aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "bandage", "Bandage" },
            { "bandages", "Bandage" },
            { "gold", "Gold" },
            { "gold coins", "Gold" },
            { "gold coin", "Gold" },
            { "coins", "Gold" },
            { "backpack", "Backpack" },
            { "bag", "Backpack" },
            { "scissors", "Scissors" },
            { "torch", "Torch" },
            { "apple", "Apple" },
            { "katana", "Katana" },
            { "dragon", "Dragon" },
            { "dragons", "Dragon" }
        };

        public static AIGMConstructableResolution Resolve(string rawInput, AIGMConstructableKind kind)
        {
            AIGMConstructableResolution result = new AIGMConstructableResolution();
            result.RawInput = rawInput ?? String.Empty;
            result.NormalizedInput = Normalize(rawInput);

            if (String.IsNullOrWhiteSpace(result.NormalizedInput))
            {
                result.FailureReason = "Missing constructable type name.";
                return result;
            }

            List<string> candidates = BuildCandidates(rawInput);
            for (int i = 0; i < candidates.Count; i++)
            {
                AIGMConstructableResolution match = TryResolveCandidate(rawInput, result.NormalizedInput, candidates[i], kind, "find_type");
                if (match.Success)
                    return match;
            }

            Type[] allTypes = ScriptCompiler.Assemblies.SelectMany(SafeGetTypes).ToArray();
            for (int i = 0; i < allTypes.Length; i++)
            {
                Type type = allTypes[i];
                if (!IsCandidateType(type, kind) || !HasConstructableConstructor(type))
                    continue;

                if (String.Equals(Normalize(type.Name), result.NormalizedInput, StringComparison.OrdinalIgnoreCase)
                    || String.Equals(Normalize(type.FullName), result.NormalizedInput, StringComparison.OrdinalIgnoreCase))
                {
                    return BuildResolution(rawInput, result.NormalizedInput, type, "normalized_scan");
                }
            }

            result.FailureReason = "No constructable type matched '" + rawInput + "'.";
            return result;
        }

        public static bool HasConstructableConstructor(Type type)
        {
            if (type == null)
                return false;

            ConstructorInfo[] ctors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < ctors.Length; i++)
            {
                if (Add.IsConstructable(ctors[i], AccessLevel.Administrator))
                    return true;
            }

            return false;
        }

        public static string Normalize(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return String.Empty;

            char[] chars = value.Trim().ToLowerInvariant().ToCharArray();
            List<char> buffer = new List<char>(chars.Length);
            bool lastWasSpace = false;

            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                if (Char.IsLetterOrDigit(c))
                {
                    buffer.Add(c);
                    lastWasSpace = false;
                }
                else if (!lastWasSpace)
                {
                    buffer.Add(' ');
                    lastWasSpace = true;
                }
            }

            return new string(buffer.ToArray()).Trim();
        }

        private static List<string> BuildCandidates(string rawInput)
        {
            List<string> list = new List<string>();
            string trimmed = (rawInput ?? String.Empty).Trim();
            string normalized = Normalize(trimmed);
            string singular = SingularizePhrase(normalized);
            string alias;

            AddCandidate(list, trimmed);

            if (Aliases.TryGetValue(trimmed, out alias))
                AddCandidate(list, alias);

            if (Aliases.TryGetValue(normalized, out alias))
                AddCandidate(list, alias);

            AddCandidate(list, singular);

            if (Aliases.TryGetValue(singular, out alias))
                AddCandidate(list, alias);

            AddCandidate(list, ToTypeToken(normalized));
            AddCandidate(list, ToTypeToken(singular));
            AddCandidate(list, normalized.Replace(" ", String.Empty));
            AddCandidate(list, singular.Replace(" ", String.Empty));

            return list;
        }

        private static void AddCandidate(List<string> list, string candidate)
        {
            if (String.IsNullOrWhiteSpace(candidate))
                return;

            for (int i = 0; i < list.Count; i++)
            {
                if (String.Equals(list[i], candidate, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            list.Add(candidate);
        }

        private static AIGMConstructableResolution TryResolveCandidate(string rawInput, string normalizedInput, string candidate, AIGMConstructableKind kind, string matchSource)
        {
            Type type = ScriptCompiler.FindTypeByName(candidate);
            if (!IsCandidateType(type, kind))
                return new AIGMConstructableResolution { RawInput = rawInput, NormalizedInput = normalizedInput, FailureReason = "No type match." };

            if (!HasConstructableConstructor(type))
            {
                return new AIGMConstructableResolution
                {
                    RawInput = rawInput,
                    NormalizedInput = normalizedInput,
                    ResolvedType = type,
                    CanonicalTypeName = type.Name,
                    IsItem = typeof(Item).IsAssignableFrom(type),
                    IsMobile = typeof(Mobile).IsAssignableFrom(type),
                    HasConstructableConstructor = false,
                    MatchSource = matchSource,
                    FailureReason = "Type matched but has no constructable constructor."
                };
            }

            return BuildResolution(rawInput, normalizedInput, type, matchSource);
        }

        private static AIGMConstructableResolution BuildResolution(string rawInput, string normalizedInput, Type type, string matchSource)
        {
            return new AIGMConstructableResolution
            {
                RawInput = rawInput,
                NormalizedInput = normalizedInput,
                ResolvedType = type,
                CanonicalTypeName = type.Name,
                MatchSource = matchSource,
                IsItem = typeof(Item).IsAssignableFrom(type),
                IsMobile = typeof(Mobile).IsAssignableFrom(type),
                HasConstructableConstructor = true
            };
        }

        private static bool IsCandidateType(Type type, AIGMConstructableKind kind)
        {
            if (type == null || type.IsAbstract || type.IsInterface)
                return false;

            switch (kind)
            {
                case AIGMConstructableKind.Item:
                    return typeof(Item).IsAssignableFrom(type);
                case AIGMConstructableKind.Mobile:
                    return typeof(Mobile).IsAssignableFrom(type);
                default:
                    return typeof(Item).IsAssignableFrom(type) || typeof(Mobile).IsAssignableFrom(type);
            }
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

        private static string SingularizePhrase(string input)
        {
            if (String.IsNullOrWhiteSpace(input))
                return input;

            string[] parts = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return input;

            string last = parts[parts.Length - 1];
            if (last.Length > 3 && last.EndsWith("ies", StringComparison.OrdinalIgnoreCase))
                last = last.Substring(0, last.Length - 3) + "y";
            else if (last.Length > 2 && last.EndsWith("es", StringComparison.OrdinalIgnoreCase) && !last.EndsWith("ss", StringComparison.OrdinalIgnoreCase))
                last = last.Substring(0, last.Length - 2);
            else if (last.Length > 1 && last.EndsWith("s", StringComparison.OrdinalIgnoreCase) && !last.EndsWith("ss", StringComparison.OrdinalIgnoreCase))
                last = last.Substring(0, last.Length - 1);

            parts[parts.Length - 1] = last;
            return String.Join(" ", parts);
        }

        private static string ToTypeToken(string input)
        {
            if (String.IsNullOrWhiteSpace(input))
                return input;

            string[] parts = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                parts[i] = Char.ToUpperInvariant(part[0]) + (part.Length > 1 ? part.Substring(1) : String.Empty);
            }

            return String.Join(String.Empty, parts);
        }
    }
}
