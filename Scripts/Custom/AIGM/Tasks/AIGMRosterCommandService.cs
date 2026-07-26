using System;
using System.Collections.Generic;

using Server;
using Server.Custom.AIGM.Characters.Waylander;
using Server.Custom.AIGM.Navigation;
using Server.Mobiles;

namespace Server.Custom.AIGM.Tasks
{
    public static class AIGMRosterCommandService
    {
        private sealed class ParsedRosterCommand
        {
            public string Address;
            public string CommandText;
            public AIGMRosterTaskMode Mode;
            public AIGMRosterTargetSelector Selector;
            public string TargetCanonicalId;
            public AIGMRosterFaction TargetFaction;
            public bool ClearTask;
            public bool ReturnHome;
            public bool StatusOnly;
            public bool Bind;
            public bool Unbind;
            public bool StandDown;
            public bool AbsoluteHold;
            public bool Resume;
            public string TravelDestinationText;
            public string TravelDestinationName;
        }

        private sealed class CommandReceiptLine
        {
            public string Name;
            public string Result;
        }

        private sealed class RecentDispatch
        {
            public string Speech;
            public DateTime Utc;
        }

        private static readonly Dictionary<int, RecentDispatch> LastDispatchBySpeaker = new Dictionary<int, RecentDispatch>();
        private static readonly string[] AddressPrefixes =
        {
            "dark brotherhood",
            "the thirty",
            "sathuli lord",
            "duke of kydor",
            "yu yu liang",
            "tenaka khan",
            "kesa khan",
            "ansi chen",
            "waylander",
            "dakeyras",
            "dardalion",
            "danyal",
            "heroes",
            "brotherhood",
            "joinings",
            "everyone"
        };

        public static bool TryHandleSpeech(IAIGMRosterTaskAgent listener, Mobile speaker, string rawSpeech, out string response)
        {
            response = null;

            if (speaker == null || String.IsNullOrWhiteSpace(rawSpeech))
                return false;

            ParsedRosterCommand command;
            if (!TryParseSpeech(rawSpeech, out command))
                return false;

            if (IsDuplicateDispatch(speaker, rawSpeech))
            {
                response = String.Empty;
                return true;
            }

            List<IAIGMRosterTaskAgent> recipients = ResolveRecipients(listener, speaker, command.Address);
            if (recipients.Count == 0)
                return false;

            int handled = 0;
            int attempted = 0;
            string firstStatus = null;
            List<CommandReceiptLine> receipt = new List<CommandReceiptLine>();
            for (int i = 0; i < recipients.Count; i++)
            {
                IAIGMRosterTaskAgent agent = recipients[i];
                if (command.Bind)
                {
                    attempted++;
                    if (AIGMRosterCompanionBindingService.TryBind(agent, speaker, out string bindResponse))
                    {
                        handled++;
                        AddReceipt(receipt, agent, bindResponse.Contains("Already") ? "already in requested state" : "started");
                        if (firstStatus == null)
                            firstStatus = bindResponse;
                    }
                    else
                    {
                        AddReceipt(receipt, agent, "not eligible - " + bindResponse);
                        if (firstStatus == null)
                            firstStatus = bindResponse;
                    }
                    continue;
                }

                if (command.Unbind)
                {
                    attempted++;
                    if (AIGMRosterCompanionBindingService.TryUnbind(agent, speaker, out string unbindResponse))
                    {
                        handled++;
                        AddReceipt(receipt, agent, unbindResponse.Contains("Already") ? "already in requested state" : "stopped");
                        if (firstStatus == null)
                            firstStatus = unbindResponse;
                    }
                    else
                    {
                        AddReceipt(receipt, agent, "not eligible - " + unbindResponse);
                        if (firstStatus == null)
                            firstStatus = unbindResponse;
                    }
                    continue;
                }

                if (!AIGMRosterCompanionCapacityService.IsTrustedCommander(agent, speaker))
                {
                    AddReceipt(receipt, agent, "command rejected");
                    continue;
                }

                if (command.StatusOnly)
                {
                    handled++;
                    AddReceipt(receipt, agent, "status");
                    if (firstStatus == null)
                        firstStatus = AIGMRosterTaskService.BuildStatus(agent);
                    continue;
                }

                if (command.ClearTask)
                {
                    AIGMRosterTaskService.ClearTask(agent, rawSpeech);
                    handled++;
                    AddReceipt(receipt, agent, "stopped");
                    if (firstStatus == null)
                        firstStatus = "Task cleared.";
                    continue;
                }

                if (command.StandDown || command.AbsoluteHold || command.Resume)
                {
                    Mobile mobile = agent as Mobile;
                    if (mobile == null)
                    {
                        AddReceipt(receipt, agent, "dead/deleted");
                        continue;
                    }

                    if (command.AbsoluteHold && speaker.AccessLevel < AccessLevel.GameMaster)
                    {
                        AddReceipt(receipt, agent, "command rejected");
                        continue;
                    }

                    if (command.Resume)
                    {
                        AIGMOperationalControlService.Release(mobile, speaker, rawSpeech);
                        handled++;
                        AddReceipt(receipt, agent, "resumed");
                        if (firstStatus == null)
                            firstStatus = "Operational control released.";
                    }
                    else
                    {
                        AIGMOperationalControlService.ClearAllOperationalState(
                            mobile,
                            command.AbsoluteHold ? AIGMOperationalStopMode.AbsoluteGMHold : AIGMOperationalStopMode.PassiveStandDown,
                            speaker,
                            rawSpeech);
                        handled++;
                        AddReceipt(receipt, agent, command.AbsoluteHold ? "standing down (absolute hold)" : "standing down");
                        if (firstStatus == null)
                            firstStatus = command.AbsoluteHold ? "Absolute GM hold applied." : "Standing down.";
                    }

                    continue;
                }

                if (command.ReturnHome)
                {
                    AIGMRosterTaskService.ReturnHome(agent, rawSpeech);
                    handled++;
                    AddReceipt(receipt, agent, "started");
                    if (firstStatus == null)
                        firstStatus = "Returning home.";
                    continue;
                }

                BaseCreature creature = agent as BaseCreature;
                if (creature == null)
                {
                    AddReceipt(receipt, agent, "dead/deleted");
                    continue;
                }

                Serial targetSerial = Serial.MinusOne;
                if (!String.IsNullOrWhiteSpace(command.TargetCanonicalId))
                {
                    foreach (Mobile mobile in World.Mobiles.Values)
                    {
                        if (mobile is IAIGMRosterTaskAgent taskTarget
                            && String.Equals(taskTarget.RosterCharacterId, command.TargetCanonicalId, StringComparison.OrdinalIgnoreCase)
                            && !mobile.Deleted
                            && mobile.Alive)
                        {
                            targetSerial = mobile.Serial;
                            break;
                        }
                    }
                }

                Point3D assignedLocation = Point3D.Zero;
                Map assignedMap = creature.Map;
                int assignedRange = 0;
                string groupId = Normalize(command.Address).Replace(" ", String.Empty);

                if (command.Mode == AIGMRosterTaskMode.GuardOwner)
                {
                    assignedLocation = speaker.Location;
                }
                else if (command.Mode == AIGMRosterTaskMode.Regroup)
                {
                    assignedLocation = speaker.Location;
                    assignedRange = 2;
                }
                else if (command.Mode == AIGMRosterTaskMode.FollowOwner)
                {
                    assignedLocation = speaker.Location;
                    assignedRange = 3;
                }
                else if (command.Mode == AIGMRosterTaskMode.Stay)
                {
                    assignedLocation = creature.Location;
                    assignedRange = 0;
                }
                else if (command.Mode == AIGMRosterTaskMode.Travel)
                {
                    AIGMNavigationLocation destination;
                    if (!AIGMNavigationLocationRegistry.TryResolve(command.TravelDestinationText, creature.Map, out destination))
                    {
                        AddReceipt(receipt, agent, "command rejected");
                        continue;
                    }

                    AIGMNavigationDestinationResolution resolution = AIGMNavigationDestinationResolver.Resolve(destination, creature.Location, creature.Map);
                    if (!resolution.IsStandable)
                    {
                        AddReceipt(receipt, agent, "command rejected");
                        continue;
                    }

                    assignedLocation = resolution.Target;
                    assignedMap = destination.Map;
                    assignedRange = resolution.ArrivalRadius;
                    command.TravelDestinationName = destination.DisplayName;
                }
                else if (command.Mode == AIGMRosterTaskMode.Patrol || command.Mode == AIGMRosterTaskMode.GuardArea)
                {
                    assignedLocation = creature.Location;
                    assignedRange = 8;
                }

                if (AIGMRosterTaskService.AssignTask(
                    agent,
                    speaker,
                    command.Mode,
                    command.Selector,
                    targetSerial,
                    command.TargetCanonicalId,
                    command.TargetFaction,
                    assignedLocation,
                    assignedMap,
                    assignedRange,
                    groupId,
                    true,
                    out string assignmentResponse))
                {
                    handled++;
                    AddReceipt(receipt, agent, CommandResultLabel(command));
                    if (firstStatus == null)
                        firstStatus = assignmentResponse;
                }
                else
                {
                    AddReceipt(receipt, agent, "command rejected");
                }
            }

            if (handled == 0 && attempted == 0)
                return false;

            RememberDispatch(speaker, rawSpeech);
            if (handled == 0 && attempted > 0)
            {
                response = receipt.Count > 1 ? BuildReceipt(command, receipt) : firstStatus;
            }
            else
            {
                response = receipt.Count > 1
                    ? BuildReceipt(command, receipt)
                    : (handled == 1 || String.IsNullOrWhiteSpace(firstStatus)
                    ? firstStatus
                    : String.Format("{0} roster agents updated. {1}", handled, firstStatus));
            }
            return true;
        }

        public static bool TryHandleCommandText(
            Mobile speaker,
            string address,
            string commandText,
            out string response)
        {
            response = null;
            if (speaker == null || String.IsNullOrWhiteSpace(address) || String.IsNullOrWhiteSpace(commandText))
                return false;

            return TryHandleSpeech(null, speaker, address + ", " + commandText, out response);
        }

        private static bool TryParseSpeech(string rawSpeech, out ParsedRosterCommand command)
        {
            command = null;
            string normalized = Normalize(rawSpeech);
            if (String.IsNullOrWhiteSpace(normalized))
                return false;

            string address;
            string commandText;
            if (!TrySplitAddress(normalized, out address, out commandText))
                return false;

            ParsedRosterCommand parsed = new ParsedRosterCommand
            {
                Address = address,
                CommandText = commandText
            };

            if (commandText == "status" || commandText == "task status")
            {
                parsed.StatusOnly = true;
                command = parsed;
                return true;
            }

            if (commandText == "follow" || commandText == "follow me" || commandText == "follow owner")
            {
                parsed.Mode = AIGMRosterTaskMode.FollowOwner;
                parsed.Selector = AIGMRosterTargetSelector.None;
                command = parsed;
                return true;
            }

            if (commandText == "stay" || commandText == "stay here")
            {
                parsed.Mode = AIGMRosterTaskMode.Stay;
                parsed.Selector = AIGMRosterTargetSelector.None;
                command = parsed;
                return true;
            }

            if (commandText == "bind to me" || commandText == "bind" || commandText == "join me" || commandText == "become my companion")
            {
                parsed.Bind = true;
                command = parsed;
                return true;
            }

            if (commandText == "unbind" || commandText == "release" || commandText == "leave my service" || commandText == "return to your own path")
            {
                parsed.Unbind = true;
                command = parsed;
                return true;
            }

            if (commandText == "stand down" || commandText == "standdown")
            {
                parsed.StandDown = true;
                command = parsed;
                return true;
            }

            if (commandText == "hold" || commandText == "absolute hold" || commandText == "gm hold")
            {
                parsed.AbsoluteHold = true;
                command = parsed;
                return true;
            }

            if (commandText == "resume" || commandText == "release hold" || commandText == "release stand down")
            {
                parsed.Resume = true;
                command = parsed;
                return true;
            }

            if (commandText == "return home")
            {
                parsed.ReturnHome = true;
                command = parsed;
                return true;
            }

            if (commandText == "stop" || commandText == "stop hunting" || commandText == "clear task")
            {
                parsed.ClearTask = true;
                command = parsed;
                return true;
            }

            if (commandText == "regroup")
            {
                parsed.Mode = AIGMRosterTaskMode.Regroup;
                parsed.Selector = AIGMRosterTargetSelector.None;
                command = parsed;
                return true;
            }

            if (commandText == "patrol")
            {
                parsed.Mode = AIGMRosterTaskMode.Patrol;
                parsed.Selector = AIGMRosterTargetSelector.None;
                command = parsed;
                return true;
            }

            if (commandText == "guard me")
            {
                parsed.Mode = AIGMRosterTaskMode.GuardOwner;
                parsed.Selector = AIGMRosterTargetSelector.None;
                command = parsed;
                return true;
            }

            string remainder;
            if (TryTakePrefix(commandText, "travel ", out remainder))
            {
                parsed.Mode = AIGMRosterTaskMode.Travel;
                parsed.Selector = AIGMRosterTargetSelector.None;
                parsed.TravelDestinationText = remainder;
                command = parsed;
                return true;
            }

            if (TryTakePrefix(commandText, "track ", out remainder))
            {
                parsed.Mode = AIGMRosterTaskMode.Track;
                if (!TryResolveSelector(remainder, out parsed.Selector, out parsed.TargetCanonicalId, out parsed.TargetFaction))
                    return false;
                command = parsed;
                return true;
            }

            if (TryTakePrefix(commandText, "hunt ", out remainder))
            {
                parsed.Mode = AIGMRosterTaskMode.Hunt;
                if (!TryResolveSelector(remainder, out parsed.Selector, out parsed.TargetCanonicalId, out parsed.TargetFaction))
                    return false;
                command = parsed;
                return true;
            }

            if (TryTakePrefix(commandText, "guard ", out remainder))
            {
                parsed.Mode = AIGMRosterTaskMode.GuardMobile;
                if (String.Equals(remainder, "area", StringComparison.OrdinalIgnoreCase) || String.Equals(remainder, "here", StringComparison.OrdinalIgnoreCase))
                {
                    parsed.Mode = AIGMRosterTaskMode.GuardArea;
                    parsed.Selector = AIGMRosterTargetSelector.None;
                    command = parsed;
                    return true;
                }

                if (!TryResolveSelector(remainder, out parsed.Selector, out parsed.TargetCanonicalId, out parsed.TargetFaction))
                    return false;

                if (parsed.Selector != AIGMRosterTargetSelector.NamedCharacter && parsed.Selector != AIGMRosterTargetSelector.MobileSerial)
                    return false;

                command = parsed;
                return true;
            }

            return false;
        }

        private static bool TryResolveSelector(string rawTarget, out AIGMRosterTargetSelector selector, out string targetCanonicalId, out AIGMRosterFaction targetFaction)
        {
            selector = AIGMRosterTargetSelector.None;
            targetCanonicalId = String.Empty;
            targetFaction = AIGMRosterFaction.None;

            string target = Normalize(rawTarget);
            if (String.IsNullOrWhiteSpace(target))
                return false;

            if (target == "heroes")
            {
                selector = AIGMRosterTargetSelector.Heroes;
                targetFaction = AIGMRosterFaction.Heroes;
                return true;
            }

            if (target == "enemies")
            {
                selector = AIGMRosterTargetSelector.Enemies;
                return true;
            }

            if (target == "monsters" || target == "monster")
            {
                selector = AIGMRosterTargetSelector.Monsters;
                return true;
            }

            if (target == "reds" || target == "red")
            {
                selector = AIGMRosterTargetSelector.Reds;
                return true;
            }

            if (target == "blues" || target == "blue")
            {
                selector = AIGMRosterTargetSelector.Blues;
                return true;
            }

            if (target == "all" || target == "all hostiles" || target == "allhostiles" || target == "hostiles")
            {
                selector = AIGMRosterTargetSelector.AllHostiles;
                return true;
            }

            if (TryTakePrefix(target, "faction ", out string factionText))
            {
                targetFaction = ResolveFactionAlias(factionText);
                if (targetFaction == AIGMRosterFaction.None)
                    return false;

                selector = AIGMRosterTargetSelector.Faction;
                return true;
            }

            targetCanonicalId = WaylanderRosterCatalog.ResolveCharacterIdFromSpeech(target, String.Empty);
            if (!String.IsNullOrWhiteSpace(targetCanonicalId))
            {
                selector = AIGMRosterTargetSelector.NamedCharacter;
                return true;
            }

            targetFaction = ResolveFactionAlias(target);
            if (targetFaction != AIGMRosterFaction.None)
            {
                selector = AIGMRosterTargetSelector.Faction;
                return true;
            }

            return false;
        }

        private static AIGMRosterFaction ResolveFactionAlias(string value)
        {
            switch (Normalize(value))
            {
                case "heroes":
                    return AIGMRosterFaction.Heroes;
                case "darkbrotherhood":
                case "dark brotherhood":
                case "brotherhood":
                    return AIGMRosterFaction.DarkBrotherhood;
                case "joinings":
                case "joining":
                    return AIGMRosterFaction.Joinings;
                case "wolfshead":
                    return AIGMRosterFaction.Wolfshead;
                case "thethirty":
                case "the thirty":
                    return AIGMRosterFaction.TheThirty;
                case "drenai":
                    return AIGMRosterFaction.Drenai;
                case "gothir":
                    return AIGMRosterFaction.Gothir;
                case "kuanhador":
                case "kuan hador":
                    return AIGMRosterFaction.KuanHador;
                case "sathuli":
                    return AIGMRosterFaction.Sathuli;
                case "kydor":
                    return AIGMRosterFaction.Kydor;
                case "nadir":
                    return AIGMRosterFaction.Nadir;
                default:
                    return AIGMRosterFaction.None;
            }
        }

        private static void AddReceipt(List<CommandReceiptLine> receipt, IAIGMRosterTaskAgent agent, string result)
        {
            if (receipt == null)
                return;

            BaseCreature creature = agent as BaseCreature;
            receipt.Add(new CommandReceiptLine
            {
                Name = creature != null ? (creature.Name ?? creature.GetType().Name) : "unknown",
                Result = String.IsNullOrWhiteSpace(result) ? "command rejected" : result
            });
        }

        private static string CommandResultLabel(ParsedRosterCommand command)
        {
            if (command == null)
                return "started";

            if (command.Mode == AIGMRosterTaskMode.Stay)
                return "stopped";

            return "started";
        }

        private static string BuildReceipt(ParsedRosterCommand command, List<CommandReceiptLine> receipt)
        {
            string title = "Command receipt";
            if (command != null)
            {
                if (command.Mode == AIGMRosterTaskMode.Travel && !String.IsNullOrWhiteSpace(command.TravelDestinationName))
                    title = "Travel to " + command.TravelDestinationName;
                else if (!String.IsNullOrWhiteSpace(command.CommandText))
                    title = "Command: " + command.CommandText;
            }

            List<string> parts = new List<string>();
            parts.Add(title + ":");
            for (int i = 0; i < receipt.Count; i++)
                parts.Add(String.Format("{0}: {1}", receipt[i].Name, receipt[i].Result));

            return String.Join(" | ", parts.ToArray());
        }

        private static List<IAIGMRosterTaskAgent> ResolveRecipients(IAIGMRosterTaskAgent listener, Mobile speaker, string address)
        {
            List<IAIGMRosterTaskAgent> recipients = new List<IAIGMRosterTaskAgent>();
            string normalizedAddress = Normalize(address);
            if (String.IsNullOrWhiteSpace(normalizedAddress))
                return recipients;

            int range = 24;
            if (normalizedAddress == "everyone")
            {
                foreach (IAIGMRosterTaskAgent agent in AIGMRosterTaskService.EnumerateAgents(speaker.Map, speaker.Location, range))
                    recipients.Add(agent);
                return recipients;
            }

            foreach (IAIGMRosterTaskAgent agent in AIGMRosterTaskService.EnumerateAgents(speaker.Map, speaker.Location, range))
            {
                if (!(agent is BaseCreature creature))
                    continue;

                string characterId = agent.RosterCharacterId ?? String.Empty;
                string canonicalAddress = WaylanderRosterCatalog.ResolveCharacterIdFromSpeech(normalizedAddress, String.Empty);
                if (!String.IsNullOrWhiteSpace(canonicalAddress)
                    && String.Equals(characterId, canonicalAddress, StringComparison.OrdinalIgnoreCase))
                {
                    recipients.Add(agent);
                    continue;
                }

                AIGMRosterFaction addressFaction = ResolveFactionAlias(normalizedAddress);
                if (addressFaction != AIGMRosterFaction.None)
                {
                    if (AIGMRosterFactionService.ResolveFaction(characterId) == addressFaction)
                        recipients.Add(agent);
                    continue;
                }

                if (MatchesHeroGroup(normalizedAddress) && AIGMRosterFactionService.IsHero(characterId))
                {
                    recipients.Add(agent);
                    continue;
                }

                string[] groupMembers = ToArray(AIGMRosterFactionService.GetGroupCharacterIds(normalizedAddress));
                for (int i = 0; i < groupMembers.Length; i++)
                {
                    if (String.Equals(groupMembers[i], characterId, StringComparison.OrdinalIgnoreCase))
                    {
                        recipients.Add(agent);
                        break;
                    }
                }
            }

            if (recipients.Count == 0 && listener != null)
            {
                BaseCreature listenerCreature = listener as BaseCreature;
                if (listenerCreature != null && listenerCreature.Map == speaker.Map && listenerCreature.InRange(speaker.Location, range))
                    recipients.Add(listener);
            }

            return recipients;
        }

        private static bool MatchesHeroGroup(string normalizedAddress)
        {
            return normalizedAddress == "hero" || normalizedAddress == "heroes";
        }

        private static string[] ToArray(IEnumerable<string> values)
        {
            List<string> list = new List<string>();
            foreach (string value in values)
                list.Add(value);
            return list.ToArray();
        }

        private static bool TrySplitAddress(string normalizedSpeech, out string address, out string commandText)
        {
            address = String.Empty;
            commandText = String.Empty;

            int comma = normalizedSpeech.IndexOf(',');
            if (comma > 0)
            {
                address = normalizedSpeech.Substring(0, comma).Trim();
                commandText = normalizedSpeech.Substring(comma + 1).Trim();
                return !String.IsNullOrWhiteSpace(address) && !String.IsNullOrWhiteSpace(commandText);
            }

            for (int i = 0; i < AddressPrefixes.Length; i++)
            {
                string prefix = AddressPrefixes[i];
                if (normalizedSpeech == prefix)
                    continue;

                if (normalizedSpeech.StartsWith(prefix + " ", StringComparison.Ordinal))
                {
                    address = prefix;
                    commandText = normalizedSpeech.Substring(prefix.Length).Trim();
                    return !String.IsNullOrWhiteSpace(commandText);
                }
            }

            return false;
        }

        private static bool TryTakePrefix(string text, string prefix, out string remainder)
        {
            remainder = String.Empty;
            if (String.IsNullOrWhiteSpace(text) || String.IsNullOrWhiteSpace(prefix))
                return false;

            if (!text.StartsWith(prefix, StringComparison.Ordinal))
                return false;

            remainder = text.Substring(prefix.Length).Trim();
            return !String.IsNullOrWhiteSpace(remainder);
        }

        private static bool IsDuplicateDispatch(Mobile speaker, string rawSpeech)
        {
            if (speaker == null || String.IsNullOrWhiteSpace(rawSpeech))
                return false;

            lock (LastDispatchBySpeaker)
            {
                if (LastDispatchBySpeaker.TryGetValue(speaker.Serial.Value, out RecentDispatch dispatch))
                {
                    if (String.Equals(dispatch.Speech, Normalize(rawSpeech), StringComparison.Ordinal)
                        && (DateTime.UtcNow - dispatch.Utc) < TimeSpan.FromSeconds(0.75))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void RememberDispatch(Mobile speaker, string rawSpeech)
        {
            if (speaker == null || String.IsNullOrWhiteSpace(rawSpeech))
                return;

            lock (LastDispatchBySpeaker)
            {
                LastDispatchBySpeaker[speaker.Serial.Value] = new RecentDispatch
                {
                    Speech = Normalize(rawSpeech),
                    Utc = DateTime.UtcNow
                };
            }
        }

        private static string Normalize(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return String.Empty;

            string normalized = value.Trim().ToLowerInvariant();
            normalized = normalized.Replace(".", " ").Replace("!", " ").Replace("?", " ").Replace(";", " ").Replace(":", " ");
            while (normalized.Contains("  "))
                normalized = normalized.Replace("  ", " ");
            return normalized.Trim();
        }
    }
}
