using System;
using System.Collections.Generic;
using System.Globalization;
using Server.ContextMenus;
using Server.Custom.AIGM.Tasks;
using Server.Gumps;
using Server.Mobiles;

namespace Server.Custom.AIGM.UMG
{
    public enum AIGMUMGSleeveAccessSource
    {
        Command,
        ContextMenu,
        GumpButton
    }

    public sealed class AIGMUMGSleeveAccessResult
    {
        public bool Accepted { get; set; }
        public string ResultCode { get; set; }
        public string Message { get; set; }
        public string CorrelationId { get; set; }
        public Mobile Actor { get; set; }
        public IAIGMCompanionActor Companion { get; set; }
        public string Authorization { get; set; }

        public AIGMUMGSleeveAccessResult()
        {
            ResultCode = String.Empty;
            Message = String.Empty;
            CorrelationId = String.Empty;
            Authorization = String.Empty;
        }
    }

    public static class AIGMUMGSleeveAccessService
    {
        public const int AccessRange = 12;

        // Stock ServUO context entries are cliloc-backed; use the visible "Open" row until the client/cliloc lane can provide a sleeve-specific label.
        private const int OpenSleeveEntryLocalization = 6122;
        private static bool _initialized;

        public static void Initialize()
        {
            if (_initialized)
                return;

            _initialized = true;
            EventSink.ContextMenu += OnContextMenu;
        }

        public static void OpenFromCommand(Mobile caller, string selector)
        {
            string correlationId = NewCorrelationId();
            AIGMUMGSleeveAccessResult result = ResolveAndValidate(caller, selector, AIGMUMGSleeveAccessSource.Command, correlationId, true);
            if (!result.Accepted)
            {
                SendResult(caller, result);
                return;
            }

            OpenGump(caller, result.Actor, result, AIGMUMGSleeveAccessSource.Command);
        }

        public static void OpenFromContextMenu(Mobile caller, Mobile target)
        {
            string correlationId = NewCorrelationId();
            AIGMUMGSleeveAccessResult result = ValidateResolved(caller, target, AIGMUMGSleeveAccessSource.ContextMenu, correlationId, true);
            if (!result.Accepted)
            {
                SendResult(caller, result);
                return;
            }

            OpenGump(caller, result.Actor, result, AIGMUMGSleeveAccessSource.ContextMenu);
        }

        public static AIGMUMGSleeveAccessResult ValidateForGumpButton(Mobile caller, Mobile target)
        {
            return ValidateResolved(caller, target, AIGMUMGSleeveAccessSource.GumpButton, NewCorrelationId(), true);
        }

        public static bool ShouldOfferContextEntry(Mobile caller, Mobile target)
        {
            AIGMUMGSleeveAccessResult result = ValidateResolved(caller, target, AIGMUMGSleeveAccessSource.ContextMenu, NewCorrelationId(), false);
            return result.Accepted;
        }

        public static bool IsRegisteredAIGMCompanion(Mobile mobile, out IAIGMCompanionActor companion)
        {
            companion = null;
            if (mobile == null || mobile.Deleted || !mobile.Serial.IsValid)
                return false;

            companion = mobile as IAIGMCompanionActor;
            if (companion == null || !companion.IsAIGMCompanion || String.IsNullOrWhiteSpace(companion.CompanionId))
                return false;

            if (companion.Shell != null && companion.Shell != mobile)
                return false;

            IAIGMRosterTaskAgent roster = mobile as IAIGMRosterTaskAgent;
            if (roster != null && roster.RosterDefinition == null)
                return false;

            return true;
        }

        public static string FormatSerial(Mobile mobile)
        {
            return mobile != null ? String.Format("0x{0:X8}", mobile.Serial.Value) : "none";
        }

        public static bool TryResolveCompanionSelector(string selector, out Mobile actor, out string failureCode, out string failureMessage)
        {
            return TryResolveCompanion(selector, out actor, out failureCode, out failureMessage);
        }

        public static bool IsAuthorizedCaller(Mobile caller, Mobile actor, out string authorization)
        {
            return IsAuthorized(caller, actor, out authorization);
        }

        public static bool IsCallerInAccessRange(Mobile caller, Mobile actor)
        {
            return IsInAccessRange(caller, actor);
        }

        public static bool IsActorAliveForAccess(Mobile actor)
        {
            return actor != null
                && actor.Alive
                && !actor.IsDeadBondedPet;
        }

        private static void OnContextMenu(ContextMenuEventArgs e)
        {
            if (e == null || e.Mobile == null || e.Entries == null)
                return;

            Mobile target = e.Target as Mobile;
            if (!ShouldOfferContextEntry(e.Mobile, target))
                return;

            e.Entries.Add(new OpenSleeveContextEntry(target));
        }

        private static AIGMUMGSleeveAccessResult ResolveAndValidate(Mobile caller, string selector, AIGMUMGSleeveAccessSource source, string correlationId, bool log)
        {
            if (String.IsNullOrWhiteSpace(selector))
            {
                AIGMUMGSleeveAccessResult result = Reject("missing_selector", "Usage: [umgsleeve <serial|name>]", correlationId);
                LogAccess(caller, null, source, result);
                return result;
            }

            Mobile actor;
            string failureCode;
            string failureMessage;
            if (!TryResolveCompanion(selector, out actor, out failureCode, out failureMessage))
            {
                AIGMUMGSleeveAccessResult result = Reject(failureCode, failureMessage, correlationId);
                if (log)
                    LogAccess(caller, null, source, result);
                return result;
            }

            return ValidateResolved(caller, actor, source, correlationId, log);
        }

        private static AIGMUMGSleeveAccessResult ValidateResolved(Mobile caller, Mobile actor, AIGMUMGSleeveAccessSource source, string correlationId, bool log)
        {
            IAIGMCompanionActor companion;
            if (!IsRegisteredAIGMCompanion(actor, out companion))
            {
                AIGMUMGSleeveAccessResult rejected = Reject("non_aigm_mobile", "That target is not a registered AIGM companion.", correlationId);
                if (log)
                    LogAccess(caller, actor, source, rejected);
                return rejected;
            }

            if (!IsValidActor(actor))
            {
                AIGMUMGSleeveAccessResult rejected = Reject("invalid_actor", "That AIGM companion is not currently valid for sleeve access.", correlationId);
                rejected.Actor = actor;
                rejected.Companion = companion;
                if (log)
                    LogAccess(caller, actor, source, rejected);
                return rejected;
            }

            string authorization;
            if (!IsAuthorized(caller, actor, out authorization))
            {
                AIGMUMGSleeveAccessResult rejected = Reject("unauthorized", "UMG sleeve access rejected: you are not this companion's owner, trusted commander, or a Game Master.", correlationId);
                rejected.Actor = actor;
                rejected.Companion = companion;
                rejected.Authorization = authorization;
                if (log)
                    LogAccess(caller, actor, source, rejected);
                return rejected;
            }

            if (caller.AccessLevel < AccessLevel.GameMaster && !IsActorAliveForAccess(actor))
            {
                AIGMUMGSleeveAccessResult rejected = Reject("actor_dead", "UMG sleeve access rejected: companion is dead.", correlationId);
                rejected.Actor = actor;
                rejected.Companion = companion;
                rejected.Authorization = authorization;
                if (log)
                    LogAccess(caller, actor, source, rejected);
                return rejected;
            }

            if (!IsInAccessRange(caller, actor))
            {
                AIGMUMGSleeveAccessResult rejected = Reject("out_of_range", String.Format("UMG sleeve access rejected: stand within {0} tiles on the same map.", AccessRange), correlationId);
                rejected.Actor = actor;
                rejected.Companion = companion;
                rejected.Authorization = authorization;
                if (log)
                    LogAccess(caller, actor, source, rejected);
                return rejected;
            }

            AIGMUMGSleeveAccessResult accepted = new AIGMUMGSleeveAccessResult
            {
                Accepted = true,
                ResultCode = "opened",
                Message = String.Format("Opening UMG Sleeve for {0} ({1}).", SafeName(actor), FormatSerial(actor)),
                CorrelationId = correlationId,
                Actor = actor,
                Companion = companion,
                Authorization = authorization
            };

            if (log)
                LogAccess(caller, actor, source, accepted);

            return accepted;
        }

        private static bool TryResolveCompanion(string selector, out Mobile actor, out string failureCode, out string failureMessage)
        {
            actor = null;
            failureCode = "not_found";
            failureMessage = String.Format("No live AIGM companion matched '{0}'. Use an exact serial or unique companion name.", selector);

            Serial serial;
            if (TryParseSerial(selector, out serial))
            {
                Mobile bySerial = World.FindMobile(serial);
                if (bySerial == null || bySerial.Deleted)
                {
                    failureCode = "serial_not_found";
                    failureMessage = String.Format("No live mobile matched serial {0}.", serial);
                    return false;
                }

                IAIGMCompanionActor ignored;
                if (!IsRegisteredAIGMCompanion(bySerial, out ignored))
                {
                    failureCode = "non_aigm_mobile";
                    failureMessage = String.Format("Serial {0} is not a registered AIGM companion.", serial);
                    return false;
                }

                actor = bySerial;
                return true;
            }

            List<Mobile> matches = FindNameMatches(selector);
            matches = PreferAuthoritativeBindingMatches(matches);
            if (matches.Count == 1)
            {
                actor = matches[0];
                return true;
            }

            if (matches.Count > 1)
            {
                failureCode = "ambiguous_name";
                failureMessage = String.Format("AIGM companion name '{0}' is ambiguous: {1}. Use exact serial.", selector, DescribeMatches(matches));
                return false;
            }

            return false;
        }

        private static List<Mobile> FindNameMatches(string selector)
        {
            List<Mobile> matches = new List<Mobile>();
            string needle = NormalizeSelector(selector);
            if (String.IsNullOrWhiteSpace(needle))
                return matches;

            foreach (Mobile mobile in World.Mobiles.Values)
            {
                IAIGMCompanionActor companion;
                if (!IsRegisteredAIGMCompanion(mobile, out companion))
                    continue;

                if (MatchesSelector(mobile, companion, needle))
                    matches.Add(mobile);
            }

            return matches;
        }

        private static List<Mobile> PreferAuthoritativeBindingMatches(List<Mobile> matches)
        {
            if (matches == null || matches.Count <= 1)
                return matches ?? new List<Mobile>();

            List<Mobile> pinned = new List<Mobile>();
            HashSet<int> seen = new HashSet<int>();
            for (int i = 0; i < matches.Count; i++)
            {
                Mobile mobile = matches[i];
                IAIGMCompanionActor companion;
                if (!IsRegisteredAIGMCompanion(mobile, out companion))
                    continue;

                if (!IsAssignmentBoundRuntimeSerial(mobile, companion))
                    continue;

                if (seen.Add(mobile.Serial.Value))
                    pinned.Add(mobile);
            }

            return pinned.Count > 0 ? pinned : matches;
        }

        private static bool IsAssignmentBoundRuntimeSerial(Mobile mobile, IAIGMCompanionActor companion)
        {
            if (mobile == null || companion == null)
                return false;

            List<string> actorIds = new List<string>();
            AddActorId(actorIds, companion.CompanionId);
            AddActorId(actorIds, companion.ActorId);
            AddActorId(actorIds, companion.DisplayName);
            AddActorId(actorIds, companion.CompanionDisplayName);
            AddActorId(actorIds, mobile.Name);

            for (int i = 0; i < actorIds.Count; i++)
            {
                List<AIGMUMGAssignment> assignments = AIGMUMGRepository.GetAssignmentsForTarget(actorIds[i]);
                for (int j = 0; j < assignments.Count; j++)
                {
                    AIGMUMGAssignment assignment = assignments[j];
                    if (assignment == null || String.IsNullOrWhiteSpace(assignment.TargetRuntimeSerial))
                        continue;

                    Serial serial;
                    if (TryParseSerial(assignment.TargetRuntimeSerial, out serial) && serial == mobile.Serial)
                        return true;
                }
            }

            return false;
        }

        private static void AddActorId(List<string> actorIds, string value)
        {
            string normalized = NormalizeSelector(value);
            if (String.IsNullOrWhiteSpace(normalized))
                return;

            for (int i = 0; i < actorIds.Count; i++)
            {
                if (String.Equals(actorIds[i], normalized, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            actorIds.Add(normalized);
        }

        private static bool MatchesSelector(Mobile mobile, IAIGMCompanionActor companion, string needle)
        {
            List<string> keys = new List<string>();
            keys.Add(companion.CompanionId);
            keys.Add(companion.ActorId);
            keys.Add(companion.DisplayName);
            keys.Add(companion.CompanionDisplayName);
            keys.Add(mobile.Name);
            keys.Add(mobile.GetType().Name);
            keys.Add(StripKnownPrefix(mobile.GetType().Name, "Waylander"));
            keys.Add(StripKnownPrefix(mobile.GetType().Name, "AIGMCompanion"));
            keys.Add(NormalizeTitle(mobile.Title));

            if (IsGenericSelector(needle))
                keys.Add("companion");

            for (int i = 0; i < keys.Count; i++)
            {
                if (String.Equals(NormalizeSelector(keys[i]), needle, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static bool IsValidActor(Mobile actor)
        {
            return actor != null
                && !actor.Deleted
                && actor.Serial.IsValid
                && actor.Map != null
                && actor.Map != Map.Internal;
        }

        private static bool IsAuthorized(Mobile caller, Mobile actor, out string authorization)
        {
            authorization = "invalid_caller";
            if (caller == null || caller.Deleted)
                return false;

            if (caller.AccessLevel >= AccessLevel.GameMaster)
            {
                authorization = "game_master";
                return true;
            }

            IAIGMRosterTaskAgent roster = actor as IAIGMRosterTaskAgent;
            if (roster != null && AIGMRosterCompanionCapacityService.IsTrustedCommander(roster, caller))
            {
                authorization = "trusted_commander";
                return true;
            }

            BaseCreature creature = actor as BaseCreature;
            if (creature != null && creature.Controlled && creature.ControlMaster == caller)
            {
                authorization = "control_master";
                return true;
            }

            authorization = "not_owner_or_commander";
            return false;
        }

        private static bool IsInAccessRange(Mobile caller, Mobile actor)
        {
            if (caller == null || actor == null)
                return false;

            if (caller.AccessLevel >= AccessLevel.GameMaster)
                return true;

            return caller.Map == actor.Map && actor.InRange(caller, AccessRange);
        }

        private static void OpenGump(Mobile caller, Mobile actor, AIGMUMGSleeveAccessResult result, AIGMUMGSleeveAccessSource source)
        {
            if (caller == null || actor == null)
                return;

            caller.CloseGump(typeof(AIGMUMGSleeveSelectorGump));
            caller.SendGump(new AIGMUMGSleeveSelectorGump(caller, actor, 0));
            caller.SendMessage(68, "{0} correlation={1}", result.Message, result.CorrelationId);
        }

        private static void SendResult(Mobile caller, AIGMUMGSleeveAccessResult result)
        {
            if (caller == null || result == null)
                return;

            caller.SendMessage(38, "{0} correlation={1}", result.Message, result.CorrelationId);
        }

        private static AIGMUMGSleeveAccessResult Reject(string code, string message, string correlationId)
        {
            return new AIGMUMGSleeveAccessResult
            {
                Accepted = false,
                ResultCode = code ?? "rejected",
                Message = message ?? "UMG sleeve access rejected.",
                CorrelationId = correlationId
            };
        }

        private static void LogAccess(Mobile caller, Mobile actor, AIGMUMGSleeveAccessSource source, AIGMUMGSleeveAccessResult result)
        {
            if (result == null)
                return;

            AIGMUMGLog.Write(
                "sleeve_access_attempt",
                actor,
                AIGMUMGLog.Fields(
                    "correlationId", result.CorrelationId,
                    "source", source.ToString(),
                    "result", result.ResultCode,
                    "accepted", result.Accepted ? "true" : "false",
                    "caller", Describe(caller),
                    "actor", Describe(actor),
                    "authorization", result.Authorization,
                    "range", AccessRange.ToString(CultureInfo.InvariantCulture)));
        }

        private static bool TryParseSerial(string selector, out Serial serial)
        {
            serial = Serial.MinusOne;
            if (String.IsNullOrWhiteSpace(selector))
                return false;

            string text = selector.Trim();
            int value;
            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                if (!Int32.TryParse(text.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value))
                    return false;

                serial = value;
                return serial.IsValid;
            }

            if (!Int32.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                return false;

            serial = value;
            return serial.IsValid;
        }

        private static string NormalizeSelector(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return String.Empty;

            return value.Trim().ToLowerInvariant().Replace(" ", "_").Replace("-", "_").Replace("'", String.Empty);
        }

        private static string NormalizeTitle(string title)
        {
            string normalized = NormalizeSelector(title);
            if (normalized.StartsWith("the_", StringComparison.OrdinalIgnoreCase))
                return normalized.Substring(4);

            return normalized;
        }

        private static string StripKnownPrefix(string value, string prefix)
        {
            if (String.IsNullOrWhiteSpace(value) || String.IsNullOrWhiteSpace(prefix))
                return value;

            return value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? value.Substring(prefix.Length)
                : value;
        }

        private static bool IsGenericSelector(string normalized)
        {
            return String.Equals(normalized, "companion", StringComparison.OrdinalIgnoreCase)
                || String.Equals(normalized, "companions", StringComparison.OrdinalIgnoreCase)
                || String.Equals(normalized, "aigm_companion", StringComparison.OrdinalIgnoreCase)
                || String.Equals(normalized, "aigm_companions", StringComparison.OrdinalIgnoreCase)
                || String.Equals(normalized, "hero", StringComparison.OrdinalIgnoreCase)
                || String.Equals(normalized, "heroes", StringComparison.OrdinalIgnoreCase);
        }

        private static string DescribeMatches(List<Mobile> matches)
        {
            List<string> parts = new List<string>();
            for (int i = 0; i < matches.Count && i < 8; i++)
                parts.Add(Describe(matches[i]));

            if (matches.Count > 8)
                parts.Add("+" + (matches.Count - 8) + " more");

            return String.Join(", ", parts.ToArray());
        }

        private static string Describe(Mobile mobile)
        {
            return mobile == null ? "none" : String.Format("{0}[0x{1:X8}]", SafeName(mobile), mobile.Serial.Value);
        }

        private static string SafeName(Mobile mobile)
        {
            if (mobile == null)
                return "none";

            string name = !String.IsNullOrWhiteSpace(mobile.Name) ? mobile.Name : mobile.GetType().Name;
            return name.Replace('"', '\'').Replace('\r', ' ').Replace('\n', ' ');
        }

        private static string NewCorrelationId()
        {
            return "phase64d1a-" + Guid.NewGuid().ToString("N").Substring(0, 12);
        }

        private sealed class OpenSleeveContextEntry : ContextMenuEntry
        {
            private readonly Serial _targetSerial;

            public OpenSleeveContextEntry(Mobile target)
                : base(OpenSleeveEntryLocalization, AccessRange)
            {
                _targetSerial = target != null ? target.Serial : Serial.MinusOne;
            }

            public override void OnClick()
            {
                Mobile from = Owner != null ? Owner.From : null;
                Mobile target = _targetSerial.IsValid ? World.FindMobile(_targetSerial) : null;
                OpenFromContextMenu(from, target);
            }
        }
    }
}
