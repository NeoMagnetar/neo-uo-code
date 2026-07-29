using System;

namespace Server.Custom.AIGM
{
    public sealed class AIGMUnifiedCommand
    {
        public string Verb;
        public string Actor;
        public string Payload;
        public bool Capacity;
        public int CapacityValue;
        public bool Help;
    }

    public static class AIGMUnifiedCommandParser
    {
        public static bool TryParse(string verb, string argString, out AIGMUnifiedCommand command, out string error)
        {
            command = null;
            error = null;

            string normalizedVerb = NormalizeToken(verb);
            string args = argString == null ? String.Empty : argString.Trim();

            if (normalizedVerb == "aicommands")
            {
                command = new AIGMUnifiedCommand { Verb = normalizedVerb, Help = true };
                return true;
            }

            if (normalizedVerb == "capacity")
            {
                command = new AIGMUnifiedCommand { Verb = normalizedVerb, Capacity = true };
                if (!String.IsNullOrWhiteSpace(args))
                {
                    int value;
                    if (!Int32.TryParse(args, out value) || value <= 0)
                    {
                        error = "Usage: [capacity] or [capacity <number>]";
                        return false;
                    }

                    command.CapacityValue = value;
                }

                return true;
            }

            string actor;
            string payload;
            if (!TrySplitActorPayload(args, out actor, out payload))
            {
                error = BuildUsage(normalizedVerb);
                return false;
            }

            command = new AIGMUnifiedCommand
            {
                Verb = normalizedVerb,
                Actor = NormalizeAddress(actor),
                Payload = payload
            };

            return true;
        }

        public static string ToRosterCommandText(AIGMUnifiedCommand command)
        {
            if (command == null)
                return String.Empty;

            switch (command.Verb)
            {
                case "aifollow":
                    return "follow me";
                case "stay":
                    return "stay";
                case "stop":
                    return "stop";
                case "standdown":
                    return "stand down";
                case "hold":
                    return "hold";
                case "resume":
                    return "resume";
                case "status":
                    return "status";
                case "bind":
                    return "bind";
                case "unbind":
                    return "unbind";
                case "returnhome":
                    return "return home";
                case "track":
                case "hunt":
                case "guard":
                case "travel":
                    return command.Verb + " " + command.Payload;
                default:
                    return String.Empty;
            }
        }

        private static bool TrySplitActorPayload(string args, out string actor, out string payload)
        {
            actor = String.Empty;
            payload = String.Empty;

            if (String.IsNullOrWhiteSpace(args))
                return false;

            string[] parts = args.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            actor = parts[0].Trim();
            payload = parts.Length > 1 ? parts[1].Trim() : String.Empty;
            return !String.IsNullOrWhiteSpace(actor);
        }

        private static string BuildUsage(string verb)
        {
            switch (verb)
            {
                case "track":
                case "hunt":
                case "guard":
                case "travel":
                    return "Usage: [" + verb + " <actor/group> <target or destination>]";
                default:
                    return "Usage: [" + verb + " <actor/group>]";
            }
        }

        private static string NormalizeToken(string value)
        {
            return (value ?? String.Empty).Trim().ToLowerInvariant();
        }

        private static string NormalizeAddress(string value)
        {
            string normalized = NormalizeToken(value);
            switch (normalized)
            {
                case "darkbrotherhood":
                    return "dark brotherhood";
                case "thethirty":
                    return "the thirty";
                case "dukeofkydor":
                    return "duke of kydor";
                case "sathulilord":
                    return "sathuli lord";
                case "yuyuliang":
                    return "yu yu liang";
                case "tenakakhan":
                    return "tenaka khan";
                case "kesakhan":
                    return "kesa khan";
                case "ansichen":
                    return "ansi chen";
                default:
                    return normalized;
            }
        }
    }
}
