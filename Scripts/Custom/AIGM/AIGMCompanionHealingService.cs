using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionHealingService
    {
        private static readonly Dictionary<int, HealingSupportState> States = new Dictionary<int, HealingSupportState>();

        public static string BuildHealingStatus(BaseHire healer)
        {
            if (!IsValidHealer(healer))
                return "I cannot assess my healing readiness right now.";

            double healing = AIGMCompanionSkillReadiness.GetSkillValue(healer, SkillName.Healing);
            double anatomy = AIGMCompanionSkillReadiness.GetSkillValue(healer, SkillName.Anatomy);
            int bandages = CountBandages(healer);
            return String.Format("My Healing is {0} at {1:0.0}, Anatomy is {2} at {3:0.0}, and I carry {4} bandages.",
                AIGMCompanionSkillReadiness.BuildTier(healing), healing,
                AIGMCompanionSkillReadiness.BuildTier(anatomy), anatomy,
                bandages);
        }

        public static string BuildSupportStatus(BaseHire healer)
        {
            if (!IsValidHealer(healer))
                return "I cannot assess my support readiness right now.";

            return BuildHealingStatus(healer) + " Spell healing remains deferred in this pass.";
        }

        public static bool TryHandleExplicitHealingCommand(BaseHire healer, Mobile speaker, string rawSpeech, out string response)
        {
            response = null;
            if (!IsValidHealer(healer) || speaker == null || String.IsNullOrWhiteSpace(rawSpeech))
                return false;

            string normalizedSpeech = rawSpeech.Trim().ToLowerInvariant();
            if (normalizedSpeech == "stop")
            {
                HealingSupportState state = GetState(healer);
                if (!state.CommandedActive)
                    return false;

                StopCommandedHealing(healer, "command_stop", true);
                response = healer.Name + " stops tending wounds.";
                return true;
            }

            HealingCommand command = ParseHealingCommand(rawSpeech);
            if (!command.IsHealingCommand)
                return false;

            Log("AIGM_HEAL_COMMAND_PARSED raw=\"" + EscapeLog(rawSpeech)
                + "\" healer=" + FormatHealer(healer)
                + " kind=" + command.Kind
                + " targetKind=" + command.TargetKind
                + " targetName=\"" + EscapeLog(command.TargetName) + "\""
                + " group=" + command.IsGroupHealing);

            if (command.IsStatusOnly)
            {
                response = command.Kind == HealingCommandKind.SupportStatus ? BuildSupportStatus(healer) : BuildHealingStatus(healer);
                return true;
            }

            if (command.Kind == HealingCommandKind.Stop)
            {
                StopCommandedHealing(healer, "command_stop", true);
                response = healer.Name + " stops tending wounds.";
                return true;
            }

            if (command.Kind == HealingCommandKind.CastHeal || command.Kind == HealingCommandKind.CastCure || command.Kind == HealingCommandKind.Cure)
            {
                response = "I can assess the wound, but that healing method is not safely wired yet.";
                return true;
            }

            Mobile target;
            BaseHire actingHealer = healer;
            if (command.IsGroupHealing)
            {
                target = ResolveTargetForGroupCommand(healer, speaker, command);
                LogResolved(rawSpeech, healer, target, "group_command");
                if (target == null)
                {
                    response = "That target is not allowed for companion healing.";
                    return true;
                }

                actingHealer = SelectBestHealer(healer, target);
                if (actingHealer == null)
                {
                    response = "No companion is ready to bandage that target right now.";
                    return true;
                }
            }
            else
            {
                target = ResolveTargetForDirectCommand(healer, speaker, command);
                LogResolved(rawSpeech, healer, target, "direct_command");
                if (target == null)
                {
                    response = "That target is not allowed for companion healing.";
                    return true;
                }

                if (command.TargetKind == HealingTargetKind.NamedCompanion && !IsAddressedToHealer(healer, rawSpeech))
                {
                    actingHealer = SelectBestHealer(healer, target);
                    if (actingHealer == null)
                    {
                        response = "No companion is ready to bandage that target right now.";
                        return true;
                    }
                }
            }

            return StartCommandedHealing(actingHealer, target, out response);
        }

        public static bool TryBeginSelfBandage(BaseHire healer, out string response)
        {
            return TryBeginBandage(healer, healer, out response);
        }

        public static void Pulse(BaseHire healer)
        {
            if (!IsValidHealer(healer))
                return;

            HealingSupportState state = GetState(healer);
            if (state.CommandedActive)
            {
                PulseCommandedHealing(healer, state);
                return;
            }

            AIGMCompanionSelfSustainService.TryAutoSelfBandage(healer);
        }

        public static bool StopCommandedHealing(BaseHire healer, string reason)
        {
            return StopCommandedHealing(healer, reason, false);
        }

        public static bool ShouldRouteHealingCommand(BaseHire healer, Mobile speaker, string rawSpeech, out string reason)
        {
            reason = "generic_not_healing";
            if (!IsValidHealer(healer) || speaker == null || String.IsNullOrWhiteSpace(rawSpeech))
                return false;

            string normalized = NormalizeRouteSpeech(rawSpeech);
            if (String.IsNullOrWhiteSpace(normalized))
                return false;

            if (normalized.Contains("stop healing") || normalized.Contains("cancel healing") || normalized.Contains("stop bandaging") || normalized.Contains("cancel bandaging"))
            {
                reason = "stop_healing";
                return true;
            }

            if (normalized.Contains("bandage"))
            {
                reason = "contains_bandage";
                return true;
            }

            if (!ContainsCommandWord(normalized, "heal"))
                return false;

            if (ContainsCommandWord(normalized, "me"))
            {
                reason = "heal_targeted";
                return true;
            }

            if (StartsWithKnownCompanionAlias(normalized) || ContainsKnownCompanionAlias(normalized))
            {
                reason = "alias_detected";
                return true;
            }

            return false;
        }

        public static void LogCommandRoute(BaseHire healer, Mobile speaker, string rawSpeech, string reason)
        {
            Log("AIGM_HEAL_COMMAND_ROUTE raw=\"" + EscapeLog(rawSpeech)
                + "\" normalized=\"" + EscapeLog(NormalizeRouteSpeech(rawSpeech))
                + "\" speaker=" + SafeName(speaker)
                + " selectedHealer=" + FormatHealer(healer)
                + " reason=" + (reason ?? "generic_not_healing"));
        }

        private static bool StartCommandedHealing(BaseHire healer, Mobile target, out string response)
        {
            response = null;
            if (!IsValidHealer(healer))
            {
                response = "I cannot tend wounds right now.";
                return true;
            }

            if (!IsAllowedTarget(healer, target))
            {
                response = "That target is not allowed for companion healing.";
                Log("AIGM_HEAL_BLOCKED healer=" + SafeName(healer) + " target=" + SafeName(target) + " reason=target_not_allowed");
                return true;
            }

            if (!target.Alive || target.Deleted)
            {
                response = "That target cannot be healed this way.";
                Log("AIGM_HEAL_BLOCKED healer=" + SafeName(healer) + " target=" + SafeName(target) + " reason=target_invalid");
                return true;
            }

            HealingSupportState state = GetState(healer);
            state.CommandedActive = true;
            state.TargetSerial = target.Serial.Value;
            state.StartedUtc = DateTime.UtcNow;
            state.UpdatedUtc = DateTime.UtcNow;
            state.LastReason = "commanded_healing";
            Log("AIGM_HEAL_TARGET_START healer=" + FormatHealer(healer) + " target=" + FormatTarget(healer, target) + " bandages=" + CountBandages(healer));

            if (!target.Poisoned && target.Hits >= target.HitsMax)
            {
                StopCommandedHealing(healer, "target_full", false);
                response = DescribeTarget(target) + " is already steady.";
                return true;
            }

            if (!healer.InRange(target, Bandage.Range))
            {
                TryMoveTowardTarget(healer, target, state);
                response = healer.Name + " moves to bandage " + DescribeTarget(target) + ".";
                return true;
            }

            string beginResponse;
            TryBeginBandage(healer, target, out beginResponse);
            response = beginResponse;
            return true;
        }

        private static void PulseCommandedHealing(BaseHire healer, HealingSupportState state)
        {
            if (state == null || !state.CommandedActive)
                return;

            Mobile target = World.FindMobile(state.TargetSerial);
            if (!IsAllowedTarget(healer, target) || target == null || target.Deleted || !target.Alive)
            {
                StopCommandedHealing(healer, "target_invalid", false);
                return;
            }

            state.UpdatedUtc = DateTime.UtcNow;

            if (!target.Poisoned && target.Hits >= target.HitsMax)
            {
                Log("AIGM_HEAL_TARGET_FULL healer=" + FormatHealer(healer) + " target=" + FormatTarget(healer, target) + " reason=full");
                StopCommandedHealing(healer, "target_full", false);
                return;
            }

            if (!healer.InRange(target, Bandage.Range))
            {
                TryMoveTowardTarget(healer, target, state);
                return;
            }

            if (CountBandages(healer) <= 0)
            {
                MaybeSayNoBandages(healer, state, false);
                Log("AIGM_HEAL_NO_BANDAGES healer=" + FormatHealer(healer) + " target=" + FormatTarget(healer, target) + " reason=commanded");
                StopCommandedHealing(healer, "no_bandages", false);
                return;
            }

            if (BandageContext.GetContext(healer) != null)
                return;

            IAIGMCompanionActor actor = healerActor(healer);
            if (actor != null && DateTime.UtcNow < actor.NextSupportActionUtc)
                return;

            string response;
            TryBeginBandage(healer, target, out response);
        }

        private static bool TryBeginBandage(BaseHire healer, Mobile target, out string response)
        {
            response = null;
            if (!IsValidHealer(healer))
            {
                response = "I cannot tend wounds right now.";
                return true;
            }

            if (!IsAllowedTarget(healer, target))
            {
                response = "That target is not allowed for companion healing.";
                return true;
            }

            if (!target.Alive || target.Deleted)
            {
                response = "That target cannot be healed this way.";
                return true;
            }

            if (BandageContext.GetContext(healer) != null)
            {
                response = healer == target ? "I am already bandaging my wounds." : "I am already bandaging someone.";
                return true;
            }

            IAIGMCompanionActor actor = healerActor(healer);
            if (actor != null && DateTime.UtcNow < actor.NextSupportActionUtc)
            {
                response = "I am already committed for the moment.";
                return true;
            }

            if (!healer.InRange(target, Bandage.Range))
            {
                response = healer == target ? "I need a moment to tend to myself." : "You are too far away.";
                return true;
            }

            double healing = AIGMCompanionSkillReadiness.GetSkillValue(healer, SkillName.Healing);
            double anatomy = AIGMCompanionSkillReadiness.GetSkillValue(healer, SkillName.Anatomy);
            if (healing < 30.0 || anatomy < 30.0)
            {
                response = "My Healing and Anatomy are not sufficient for that.";
                return true;
            }

            if (!target.Poisoned && target.Hits >= target.HitsMax)
            {
                response = healer == target ? "I am not wounded." : DescribeTarget(target) + " is not wounded.";
                return true;
            }

            Bandage bandage = FindBandage(healer);
            if (bandage == null || bandage.Amount <= 0)
            {
                response = "I cannot bandage without supplies.";
                HealingSupportState state = GetState(healer);
                MaybeSayNoBandages(healer, state, healer == target);
                Log("AIGM_HEAL_NO_BANDAGES healer=" + FormatHealer(healer) + " target=" + FormatTarget(healer, target) + " reason=no_supplies");
                return true;
            }

            BandageContext context = BandageContext.BeginHeal(healer, target, bandage is EnhancedBandage);
            if (context == null)
            {
                response = target.Poisoned ? "I cannot begin treating that poison right now." : "I cannot begin bandaging right now.";
                Log("AIGM_HEAL_BLOCKED healer=" + FormatHealer(healer) + " target=" + FormatTarget(healer, target) + " reason=begin_heal_failed");
                return true;
            }

            bandage.Consume();
            if (actor != null)
                actor.NextSupportActionUtc = DateTime.UtcNow + BandageContext.GetDelay(healer, target);

            Log((healer == target ? "AIGM_HEAL_SELF_START" : "AIGM_HEAL_BANDAGE_APPLY")
                + " healer=" + FormatHealer(healer)
                + " target=" + FormatTarget(healer, target)
                + " bandages=" + CountBandages(healer)
                + " delaySeconds=" + (int)BandageContext.GetDelay(healer, target).TotalSeconds);

            if (healer == target)
                response = healer.Name + " begins bandaging their own wounds.";
            else if (target == healer.GetOwner())
                response = healer.Name + " begins bandaging you.";
            else
                response = healer.Name + " begins bandaging " + DescribeTarget(target) + ".";

            return true;
        }

        private static bool StopCommandedHealing(BaseHire healer, string reason, bool explicitStop)
        {
            if (!IsValidHealer(healer))
                return false;

            HealingSupportState state = GetState(healer);
            bool wasActive = state.CommandedActive;
            Mobile target = state.TargetSerial != 0 ? World.FindMobile(state.TargetSerial) : null;
            state.CommandedActive = false;
            state.TargetSerial = 0;
            state.LastReason = reason ?? String.Empty;
            state.UpdatedUtc = DateTime.UtcNow;

            if (wasActive || explicitStop)
                Log("AIGM_HEAL_STOP healer=" + FormatHealer(healer) + " target=" + FormatTarget(healer, target) + " reason=" + (reason ?? String.Empty));

            return wasActive;
        }

        private static bool TryMoveTowardTarget(BaseHire healer, Mobile target, HealingSupportState state)
        {
            if (!IsValidHealer(healer) || target == null || target.Deleted || healer.Map == null || target.Map != healer.Map)
            {
                Log("AIGM_HEAL_BLOCKED healer=" + SafeName(healer) + " target=" + SafeName(target) + " reason=move_invalid");
                return false;
            }

            int distanceBefore = (int)Math.Round(healer.GetDistanceToSqrt(target));
            Direction direction = healer.GetDirectionTo(target) & Direction.Mask;
            if ((healer.Direction & Direction.Mask) != direction)
                healer.Direction = direction;

            bool moved = healer.Move(direction);
            if (!moved)
            {
                AIGMCompanionDoorResult door = AIGMCompanionDoorService.TryOpenNearbyDoorDetailed(healer);
                if (door.Opened)
                    moved = healer.Move(direction);
            }

            int distanceAfter = (int)Math.Round(healer.GetDistanceToSqrt(target));
            if (state != null)
            {
                state.LastMoveUtc = DateTime.UtcNow;
                state.LastMoveSucceeded = moved;
            }

            Log("AIGM_HEAL_MOVE_TO_TARGET healer=" + FormatHealer(healer)
                + " target=" + FormatTarget(healer, target)
                + " distanceBefore=" + distanceBefore
                + " distanceAfter=" + distanceAfter
                + " direction=" + direction
                + " moved=" + moved);

            return moved;
        }

        private static BaseHire SelectBestHealer(BaseHire requesterCompanion, Mobile target)
        {
            Mobile owner = requesterCompanion.GetOwner();
            if (owner == null)
                return null;

            List<BaseHire> candidates = new List<BaseHire>();
            foreach (Mobile mobile in World.Mobiles.Values)
            {
                BaseHire ally = mobile as BaseHire;
                if (!IsValidHealer(ally))
                    continue;
                if (ally.GetOwner() != owner)
                    continue;
                if (!IsAllowedTarget(ally, target))
                    continue;
                candidates.Add(ally);
            }

            candidates.Sort((a, b) => ScoreHealer(b, target).CompareTo(ScoreHealer(a, target)));
            return candidates.Count > 0 && ScoreHealer(candidates[0], target) > 0 ? candidates[0] : null;
        }

        private static int ScoreHealer(BaseHire healer, Mobile target)
        {
            if (!IsValidHealer(healer) || !IsAllowedTarget(healer, target))
                return -1;
            if (BandageContext.GetContext(healer) != null)
                return 0;
            IAIGMCompanionActor actor = healerActor(healer);
            if (actor == null)
                return 0;
            if (DateTime.UtcNow < actor.NextSupportActionUtc)
                return 0;
            if (CountBandages(healer) <= 0)
                return 0;
            if (!healer.InRange(target, Bandage.Range))
                return 1;

            int score = (int)Math.Round(AIGMCompanionSkillReadiness.GetSkillValue(healer, SkillName.Healing)
                + AIGMCompanionSkillReadiness.GetSkillValue(healer, SkillName.Anatomy));

            if (healer == target)
                score -= 25;

            if (actor != null && String.Equals(actor.CompanionId, "danyal", StringComparison.OrdinalIgnoreCase))
                score += 1;

            return score;
        }

        private static Mobile ResolveTargetForGroupCommand(BaseHire healer, Mobile speaker, HealingCommand command)
        {
            if (command.TargetKind == HealingTargetKind.Owner)
                return speaker;
            return ResolveNamedCompanionTarget(healer, command.TargetName);
        }

        private static Mobile ResolveTargetForDirectCommand(BaseHire healer, Mobile speaker, HealingCommand command)
        {
            switch (command.TargetKind)
            {
                case HealingTargetKind.Owner:
                    return speaker;
                case HealingTargetKind.Self:
                    return healer;
                case HealingTargetKind.NamedCompanion:
                    return ResolveNamedCompanionTarget(healer, command.TargetName);
                default:
                    return null;
            }
        }

        private static Mobile ResolveNamedCompanionTarget(BaseHire healer, string targetName)
        {
            if (!IsValidHealer(healer) || String.IsNullOrWhiteSpace(targetName))
                return null;

            targetName = NormalizeTargetName(targetName);
            if (String.IsNullOrWhiteSpace(targetName))
                return null;

            Mobile owner = healer.GetOwner();
            if (owner == null)
                return null;

            foreach (Mobile mobile in World.Mobiles.Values)
            {
                BaseHire ally = mobile as BaseHire;
                if (!IsValidHealer(ally))
                    continue;
                if (ally.GetOwner() != owner)
                    continue;
                if (String.Equals(ally.Name, targetName, StringComparison.OrdinalIgnoreCase))
                    return ally;

                IAIGMCompanionActor actor = healerActor(ally);
                if (actor != null)
                {
                    string display = NormalizeTargetName(actor.CompanionDisplayName);
                    string id = NormalizeTargetName(actor.CompanionId);
                    if (String.Equals(display, targetName, StringComparison.OrdinalIgnoreCase)
                        || String.Equals(id, targetName, StringComparison.OrdinalIgnoreCase)
                        || IsCompanionAliasMatch(id, targetName))
                        return ally;
                }
            }

            return null;
        }

        private static bool IsAllowedTarget(BaseHire healer, Mobile target)
        {
            if (!IsValidHealer(healer) || target == null || target.Deleted)
                return false;

            Mobile owner = healer.GetOwner();
            if (target == owner || target == healer)
                return true;

            BaseHire ally = target as BaseHire;
            return ally != null && ally.GetOwner() == owner && !ally.Deleted;
        }

        private static bool IsValidHealer(BaseHire healer)
        {
            return healer != null && !healer.Deleted && healer.Alive && healer.Map != null;
        }

        private static IAIGMCompanionActor healerActor(BaseHire healer)
        {
            return healer as IAIGMCompanionActor;
        }

        private static Bandage FindBandage(BaseHire healer)
        {
            if (healer == null || healer.Backpack == null)
                return null;

            return healer.Backpack.FindItemByType(typeof(Bandage), true) as Bandage;
        }

        private static int CountBandages(BaseHire healer)
        {
            Bandage bandage = FindBandage(healer);
            return bandage != null ? bandage.Amount : 0;
        }

        private static string DescribeTarget(Mobile target)
        {
            return target != null ? (target.Name ?? target.GetType().Name) : "that target";
        }

        private static HealingSupportState GetState(BaseHire healer)
        {
            HealingSupportState state;
            int serial = healer != null ? healer.Serial.Value : 0;
            if (!States.TryGetValue(serial, out state))
            {
                state = new HealingSupportState();
                States[serial] = state;
            }

            return state;
        }

        private static void MaybeSayNoBandages(BaseHire healer, HealingSupportState state, bool self)
        {
            if (healer == null || state == null || DateTime.UtcNow < state.NextNoBandagesSpeechUtc)
                return;

            state.NextNoBandagesSpeechUtc = DateTime.UtcNow + TimeSpan.FromSeconds(60.0);
            healer.Say(self ? "I need bandages to tend my wounds." : "I need bandages before I can tend that wound.");
        }

        private static bool IsAddressedToHealer(BaseHire healer, string rawSpeech)
        {
            if (healer == null || String.IsNullOrWhiteSpace(rawSpeech))
                return false;

            string normalized = rawSpeech.Trim().ToLowerInvariant();
            string name = healer.Name != null ? healer.Name.Trim().ToLowerInvariant() : String.Empty;
            if (!String.IsNullOrWhiteSpace(name) && (normalized.Equals(name, StringComparison.Ordinal) || normalized.StartsWith(name + " ", StringComparison.Ordinal)))
                return true;

            IAIGMCompanionActor actor = healerActor(healer);
            if (actor == null)
                return false;

            string id = actor.CompanionId != null ? actor.CompanionId.Trim().ToLowerInvariant() : String.Empty;
            if (!String.IsNullOrWhiteSpace(id) && (normalized.Equals(id, StringComparison.Ordinal) || normalized.StartsWith(id + " ", StringComparison.Ordinal)))
                return true;

            return IsCompanionAliasMatch(id, FirstWord(normalized));
        }

        private static string FormatHealer(BaseHire healer)
        {
            return SafeName(healer) + " hits=" + (healer != null ? healer.Hits.ToString() : "0") + "/" + (healer != null ? healer.HitsMax.ToString() : "0");
        }

        private static string FormatTarget(BaseHire healer, Mobile target)
        {
            string distance = "n/a";
            if (healer != null && target != null && healer.Map == target.Map)
                distance = ((int)Math.Round(healer.GetDistanceToSqrt(target))).ToString();

            return SafeName(target) + " hits=" + (target != null ? target.Hits.ToString() : "0") + "/" + (target != null ? target.HitsMax.ToString() : "0") + " distance=" + distance;
        }

        private static string SafeName(Mobile mob)
        {
            if (mob == null)
                return "(null)";

            return (mob.Name ?? mob.GetType().Name) + "[0x" + mob.Serial.Value.ToString("X8") + "]";
        }

        private static void Log(string message)
        {
            try
            {
                string path = System.IO.Path.Combine(Core.BaseDirectory, "Logs", "AIGMExecution.log");
                System.IO.File.AppendAllText(path, DateTime.UtcNow.ToString("o") + " " + (message ?? String.Empty) + Environment.NewLine);
            }
            catch
            {
            }
        }

        private static void LogResolved(string rawSpeech, BaseHire healer, Mobile target, string reason)
        {
            Log("AIGM_HEAL_TARGET_RESOLVED raw=\"" + EscapeLog(rawSpeech)
                + "\" healer=" + FormatHealer(healer)
                + " target=" + FormatTarget(healer, target)
                + " bandages=" + CountBandages(healer)
                + " reason=" + (reason ?? String.Empty));
        }

        private enum HealingCommandKind
        {
            None,
            HealingStatus,
            SupportStatus,
            Heal,
            Bandage,
            Cure,
            CastHeal,
            CastCure,
            Stop
        }

        private enum HealingTargetKind
        {
            None,
            Owner,
            Self,
            NamedCompanion
        }

        private sealed class HealingCommand
        {
            public bool IsHealingCommand;
            public bool IsStatusOnly;
            public bool IsGroupHealing;
            public HealingCommandKind Kind;
            public HealingTargetKind TargetKind;
            public string TargetName;
        }

        private static HealingCommand ParseHealingCommand(string rawSpeech)
        {
            HealingCommand cmd = new HealingCommand();
            if (String.IsNullOrWhiteSpace(rawSpeech))
                return cmd;

            string s = rawSpeech.Trim().ToLowerInvariant();
            while (s.Contains("  "))
                s = s.Replace("  ", " ");

            if (s.Contains("healing status"))
            {
                cmd.IsHealingCommand = true;
                cmd.IsStatusOnly = true;
                cmd.Kind = HealingCommandKind.HealingStatus;
                return cmd;
            }

            if (s.Contains("support status"))
            {
                cmd.IsHealingCommand = true;
                cmd.IsStatusOnly = true;
                cmd.Kind = HealingCommandKind.SupportStatus;
                return cmd;
            }

            if (s == "stop healing" || s == "cancel healing" || s == "stop bandaging" || s == "cancel bandaging")
            {
                cmd.IsHealingCommand = true;
                cmd.Kind = HealingCommandKind.Stop;
                return cmd;
            }

            if (s.Contains("all heal me") || s.Contains("companions heal me"))
            {
                cmd.IsHealingCommand = true;
                cmd.IsGroupHealing = true;
                cmd.Kind = HealingCommandKind.Heal;
                cmd.TargetKind = HealingTargetKind.Owner;
                return cmd;
            }

            if (s.Contains("cast heal"))
            {
                cmd.IsHealingCommand = true;
                cmd.Kind = HealingCommandKind.CastHeal;
                return cmd;
            }

            if (s.Contains("cast cure"))
            {
                cmd.IsHealingCommand = true;
                cmd.Kind = HealingCommandKind.CastCure;
                return cmd;
            }

            if (s.Contains("cure "))
            {
                cmd.IsHealingCommand = true;
                cmd.Kind = HealingCommandKind.Cure;
                if (s.Contains("yourself") || s.Contains("self"))
                    cmd.TargetKind = HealingTargetKind.Self;
                else if (s.Contains(" me"))
                    cmd.TargetKind = HealingTargetKind.Owner;
                else
                {
                    cmd.TargetKind = HealingTargetKind.NamedCompanion;
                    cmd.TargetName = ExtractNamedTarget(rawSpeech, "cure");
                }
                return cmd;
            }

            if (s.Contains("bandage"))
            {
                cmd.IsHealingCommand = true;
                cmd.Kind = HealingCommandKind.Bandage;
                if (s.Contains("yourself") || s.Contains("self"))
                    cmd.TargetKind = HealingTargetKind.Self;
                else if (ContainsCommandWord(s, "me") || EndsWithCommandWord(s, "bandage"))
                    cmd.TargetKind = HealingTargetKind.Owner;
                else
                {
                    cmd.TargetKind = HealingTargetKind.NamedCompanion;
                    cmd.TargetName = ExtractNamedTarget(rawSpeech, "bandage");
                    if (String.IsNullOrWhiteSpace(cmd.TargetName))
                        cmd.TargetKind = HealingTargetKind.Owner;
                }
                return cmd;
            }

            if (s.Contains("heal"))
            {
                cmd.IsHealingCommand = true;
                cmd.Kind = HealingCommandKind.Heal;
                if (s.Contains("yourself") || s.Contains("self"))
                    cmd.TargetKind = HealingTargetKind.Self;
                else if (ContainsCommandWord(s, "me") || EndsWithCommandWord(s, "heal"))
                    cmd.TargetKind = HealingTargetKind.Owner;
                else
                {
                    cmd.TargetKind = HealingTargetKind.NamedCompanion;
                    cmd.TargetName = ExtractNamedTarget(rawSpeech, "heal");
                    if (String.IsNullOrWhiteSpace(cmd.TargetName))
                        cmd.TargetKind = HealingTargetKind.Owner;
                }
            }

            return cmd;
        }

        private static string ExtractNamedTarget(string rawSpeech, string verb)
        {
            if (String.IsNullOrWhiteSpace(rawSpeech) || String.IsNullOrWhiteSpace(verb))
                return null;

            string lowered = rawSpeech.ToLowerInvariant();
            int idx = lowered.IndexOf(verb, StringComparison.Ordinal);
            if (idx < 0)
                return null;

            string rest = rawSpeech.Substring(idx + verb.Length).Trim();
            if (String.IsNullOrWhiteSpace(rest))
                return null;

            string loweredRest = NormalizeTargetName(rest);

            if (String.Equals(loweredRest, "me", StringComparison.Ordinal)
                || String.Equals(loweredRest, "yourself", StringComparison.Ordinal)
                || String.Equals(loweredRest, "self", StringComparison.Ordinal))
                return null;

            return loweredRest;
        }

        private static bool EndsWithCommandWord(string speech, string word)
        {
            if (String.IsNullOrWhiteSpace(speech) || String.IsNullOrWhiteSpace(word))
                return false;

            return speech.Equals(word, StringComparison.Ordinal)
                || speech.EndsWith(" " + word, StringComparison.Ordinal);
        }

        private static bool ContainsCommandWord(string speech, string word)
        {
            if (String.IsNullOrWhiteSpace(speech) || String.IsNullOrWhiteSpace(word))
                return false;

            string padded = " " + speech.Trim() + " ";
            return padded.Contains(" " + word.Trim() + " ");
        }

        private static string NormalizeTargetName(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return null;

            string normalized = value.Trim().ToLowerInvariant();
            normalized = normalized.Replace(",", " ").Replace(".", " ").Replace("!", " ").Replace("?", " ").Replace(";", " ").Replace(":", " ");
            while (normalized.Contains("  "))
                normalized = normalized.Replace("  ", " ");

            normalized = normalized.Trim();
            if (normalized.StartsWith("the ", StringComparison.Ordinal))
                normalized = normalized.Substring(4).Trim();
            if (normalized.StartsWith("to ", StringComparison.Ordinal))
                normalized = normalized.Substring(3).Trim();

            return normalized;
        }

        private static string NormalizeRouteSpeech(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return String.Empty;

            string normalized = value.Trim().ToLowerInvariant();
            normalized = normalized.Replace(",", " ").Replace(".", " ").Replace("!", " ").Replace("?", " ").Replace(";", " ").Replace(":", " ");
            while (normalized.Contains("  "))
                normalized = normalized.Replace("  ", " ");

            return normalized.Trim();
        }

        private static bool StartsWithKnownCompanionAlias(string normalized)
        {
            string first = FirstWord(normalized);
            return IsCompanionAliasMatch("dakeyras", first)
                || IsCompanionAliasMatch("danyal", first)
                || IsCompanionAliasMatch("dardalion", first);
        }

        private static bool ContainsKnownCompanionAlias(string normalized)
        {
            if (String.IsNullOrWhiteSpace(normalized))
                return false;

            string[] words = normalized.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];
                if (IsCompanionAliasMatch("dakeyras", word)
                    || IsCompanionAliasMatch("danyal", word)
                    || IsCompanionAliasMatch("dardalion", word))
                    return true;
            }

            return false;
        }

        private static bool IsCompanionAliasMatch(string companionId, string alias)
        {
            companionId = NormalizeTargetName(companionId);
            alias = NormalizeTargetName(alias);
            if (String.IsNullOrWhiteSpace(companionId) || String.IsNullOrWhiteSpace(alias))
                return false;

            if (String.Equals(companionId, alias, StringComparison.Ordinal))
                return true;

            if (String.Equals(companionId, "dakeyras", StringComparison.Ordinal))
                return alias == "dak" || alias == "dake" || alias == "waylander";
            if (String.Equals(companionId, "danyal", StringComparison.Ordinal))
                return alias == "dan";
            if (String.Equals(companionId, "dardalion", StringComparison.Ordinal))
                return alias == "dar" || alias == "dard";

            return false;
        }

        private static string FirstWord(string value)
        {
            value = NormalizeTargetName(value);
            if (String.IsNullOrWhiteSpace(value))
                return String.Empty;

            int space = value.IndexOf(' ');
            return space < 0 ? value : value.Substring(0, space);
        }

        private static string EscapeLog(string value)
        {
            if (String.IsNullOrEmpty(value))
                return String.Empty;

            return value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", " ").Replace("\n", " ");
        }

        private sealed class HealingSupportState
        {
            public bool CommandedActive;
            public int TargetSerial;
            public DateTime StartedUtc;
            public DateTime UpdatedUtc;
            public DateTime LastMoveUtc;
            public DateTime NextNoBandagesSpeechUtc;
            public bool LastMoveSucceeded;
            public string LastReason;
        }
    }
}
