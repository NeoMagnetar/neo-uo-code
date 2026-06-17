using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
using Server.Spells;
using Server.Spells.Necromancy;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionTrackingService
    {
        private static readonly Dictionary<int, AIGMCompanionTrackingState> States = new Dictionary<int, AIGMCompanionTrackingState>();

        public static string StartTracking(BaseHire companion, Mobile speaker, AIGMCompanionTrackingMode mode)
        {
            if (!IsValidCompanion(companion))
                return "I cannot begin a tracking watch right now.";

            AIGMCompanionTrackingState state = GetOrCreateState(companion);
            state.Mode = mode == AIGMCompanionTrackingMode.None ? AIGMCompanionTrackingMode.General : mode;
            state.IsActive = true;
            state.StartedUtc = DateTime.UtcNow;
            state.SkillValue = AIGMCompanionSkillReadiness.GetSkillValue(companion, SkillName.Tracking);
            state.SkillTier = AIGMCompanionSkillReadiness.BuildTier(state.SkillValue);
            state.LastConfidence = BuildConfidence(companion, state.Mode);
            state.OwnerSerial = companion.GetOwner() != null ? companion.GetOwner().Serial.Value : 0;

            string sweep = BuildTrackingSweepReport(companion, speaker, state.Mode);
            state.LastReport = sweep;
            state.LastScanUtc = DateTime.UtcNow;

            AIGMExecutionLog.Write("TRACKING_AWARENESS_START companion={0} active={1} mode={2} summary=\"{3}\"", companion.Serial.Value, state.IsActive, state.Mode, SafeLog(BuildHiddenTrackingSummary(companion)));
            return BuildStartLine(companion, state);
        }

        public static string StopTracking(BaseHire companion, Mobile speaker)
        {
            if (!IsValidCompanion(companion))
                return "I cannot stop tracking right now.";

            AIGMCompanionTrackingState state = GetOrCreateState(companion);
            state.IsActive = false;
            state.Mode = AIGMCompanionTrackingMode.None;
            state.ActionMode = AIGMCompanionTrackingActionMode.TrackOnly;
            state.StopReason = "stop_command";
            bool restored = RestorePursuitAuthority(companion, state, "stop_tracking");
            ClearActionTarget(state);
            if (!restored)
                AIGMCompanionCombatController.StopCombat(companion);
            else
            {
                companion.Combatant = null;
                companion.Warmode = false;
            }
            AIGMExecutionLog.Write("TRACKING_AWARENESS_STOP companion={0} active={1} summary=\"{2}\"", companion.Serial.Value, state.IsActive, SafeLog(BuildHiddenTrackingSummary(companion)));
            return BuildStopLine(companion);
        }

        public static string StartTrackingAction(BaseHire companion, Mobile speaker, AIGMCompanionTrackingMode mode, AIGMCompanionTrackingActionMode actionMode)
        {
            if (!IsValidCompanion(companion))
                return "I cannot begin a tracking watch right now.";

            if (mode == AIGMCompanionTrackingMode.None || mode == AIGMCompanionTrackingMode.General)
                mode = AIGMCompanionTrackingMode.Monsters;

            if (actionMode == AIGMCompanionTrackingActionMode.TrackHunt && !CanHuntMode(mode))
                return StartBlockedHunt(companion, speaker, mode);

            if (actionMode == AIGMCompanionTrackingActionMode.TrackMove && IsCivilianMode(mode))
                actionMode = AIGMCompanionTrackingActionMode.TrackOnly;

            AIGMCompanionTrackingState state = GetOrCreateState(companion);
            state.Mode = mode;
            state.ActionMode = actionMode;
            state.IsActive = actionMode != AIGMCompanionTrackingActionMode.TrackOnly;
            state.StartedUtc = DateTime.UtcNow;
            state.LastActionUtc = DateTime.MinValue;
            state.StopReason = String.Empty;
            state.EngagementAllowed = actionMode == AIGMCompanionTrackingActionMode.TrackHunt && CanHuntMode(mode);
            state.OwnerSerial = companion.GetOwner() != null ? companion.GetOwner().Serial.Value : 0;
            state.SkillValue = AIGMCompanionSkillReadiness.GetSkillValue(companion, SkillName.Tracking);
            state.SkillTier = AIGMCompanionSkillReadiness.BuildTier(state.SkillValue);
            state.LastConfidence = BuildConfidence(companion, state.Mode);

            if (actionMode == AIGMCompanionTrackingActionMode.TrackOnly)
            {
                state.LastReport = BuildTrackingSweepReport(companion, speaker, mode);
                state.IsActive = false;
                state.Mode = AIGMCompanionTrackingMode.None;
                state.ActionMode = AIGMCompanionTrackingActionMode.TrackOnly;
                state.LastActionResult = "track_only_report";
                AIGMExecutionLog.Write("TRACKING_ACTION_REPORT companion={0} mode={1} action={2} summary=\"{3}\"", companion.Serial.Value, mode, actionMode, SafeLog(BuildHiddenTrackingSummary(companion)));
                return state.LastReport;
            }

            string tick = TickTrackingAction(companion);
            if (String.IsNullOrWhiteSpace(tick))
                tick = BuildActionStartLine(companion, state);

            AIGMExecutionLog.Write("TRACKING_ACTION_START companion={0} active={1} mode={2} action={3} target={4} summary=\"{5}\"", companion.Serial.Value, state.IsActive, state.Mode, state.ActionMode, state.CurrentTargetSerial, SafeLog(BuildHiddenTrackingSummary(companion)));
            return tick;
        }

        public static string StartTrackingActionFromIntent(BaseHire companion, Mobile speaker, AIGMCompanionIntent intent)
        {
            string kind = intent != null ? intent.Kind : String.Empty;
            string raw = intent != null ? intent.RawText : String.Empty;

            if (kind == AIGMCompanionIntentKind.StopTrackingCycle || kind == AIGMCompanionIntentKind.StopMonsterHunt)
                return StopTracking(companion, speaker);

            if (kind == AIGMCompanionIntentKind.ReportTrackingStatus || kind == AIGMCompanionIntentKind.ReportMonsterHuntStatus)
                return GetTrackingStatus(companion, speaker);

            AIGMCompanionTrackingMode mode = GetModeFromIntentKind(kind);
            if (kind == AIGMCompanionIntentKind.StartMonsterHunt)
                mode = AIGMCompanionTrackingMode.Monsters;
            if (kind == AIGMCompanionIntentKind.HuntAnimals)
                mode = AIGMCompanionTrackingMode.Animals;

            AIGMCompanionTrackingActionMode actionMode = GetActionModeFromSpeech(raw, kind, mode);
            return StartTrackingAction(companion, speaker, mode, actionMode);
        }

        public static string GetTrackingStatus(BaseHire companion, Mobile speaker)
        {
            if (!IsValidCompanion(companion))
                return "I cannot report tracking right now.";

            AIGMCompanionTrackingState state = GetOrCreateState(companion);
            state.SkillValue = AIGMCompanionSkillReadiness.GetSkillValue(companion, SkillName.Tracking);
            state.SkillTier = AIGMCompanionSkillReadiness.BuildTier(state.SkillValue);
            state.LastConfidence = BuildConfidence(companion, state.Mode);

            if (!state.IsActive)
                return BuildInactiveStatusLine(companion, state);

            if (ShouldRefresh(state))
            {
                state.LastReport = BuildTrackingSweepReport(companion, speaker, state.Mode);
                state.LastScanUtc = DateTime.UtcNow;
            }

            AIGMExecutionLog.Write("TRACKING_AWARENESS_STATUS companion={0} active={1} mode={2} summary=\"{3}\"", companion.Serial.Value, state.IsActive, state.Mode, SafeLog(BuildHiddenTrackingSummary(companion)));
            return BuildActiveStatusLine(companion, state);
        }

        public static string BuildTrackingSweepReport(BaseHire companion, Mobile speaker, AIGMCompanionTrackingMode mode)
        {
            if (!IsValidCompanion(companion))
                return "I cannot get a clear read just now.";

            double tracking = AIGMCompanionSkillReadiness.GetSkillValue(companion, SkillName.Tracking);
            string tier = AIGMCompanionSkillReadiness.BuildTier(tracking);
            AIGMCompanionTrackingState state = GetOrCreateState(companion);
            state.SkillValue = tracking;
            state.SkillTier = tier;
            state.LastConfidence = BuildConfidence(companion, mode);

            AIGMTrackingSelectionResult selection = AIGMCompanionTrackingSensor.SelectNearest(companion, mode, GetTrackingRange(companion));
            bool oneShotReport = !state.IsActive;
            state.LastCandidateSummary = selection.AcceptedSummary;
            state.LastRejectedCandidates = selection.RejectedSummary;
            state.LastScanUtc = DateTime.UtcNow;

            if (mode == AIGMCompanionTrackingMode.All)
            {
                ClearLastTarget(state);
                state.LastKnownTargetDescription = selection.SelectedName;
                state.LastKnownDirectionText = selection.SelectedDirection;
                state.LastKnownDistanceText = selection.SelectedDistanceText;
                state.LastKnownTileText = selection.SelectedTileText;

                string summary = BuildNaturalAllSummary(selection.CategorySummary);
                if (oneShotReport)
                {
                    state.IsActive = false;
                    state.Mode = AIGMCompanionTrackingMode.None;
                }
                return BuildAllSignsLine(companion, summary);
            }

            if (selection.SelectedTarget == null)
            {
                ClearLastTarget(state);
                if (oneShotReport)
                {
                    state.IsActive = false;
                    state.Mode = AIGMCompanionTrackingMode.None;
                }
                return BuildNoTrailLine(companion, mode);
            }

            state.LastKnownTargetDescription = selection.SelectedName;
            state.LastKnownDirectionText = selection.SelectedDirection;
            state.LastKnownDistanceText = selection.SelectedDistanceText;
            state.LastKnownTileText = selection.SelectedTileText;

            string category = DescribeResultCategory(mode);
            int count = CountAccepted(selection.AcceptedSummary);

            if (oneShotReport)
            {
                state.IsActive = false;
                state.Mode = AIGMCompanionTrackingMode.None;
            }

            if (count <= 1)
                return BuildTrailLine(companion, category, selection.SelectedName, selection.SelectedDirection, selection.SelectedDistanceText, selection.SelectedTileText, 1);

            return BuildTrailLine(companion, category, selection.SelectedName, selection.SelectedDirection, selection.SelectedDistanceText, selection.SelectedTileText, count);
        }

        public static void Pulse(BaseHire companion)
        {
            if (!IsValidCompanion(companion))
                return;

            AIGMCompanionTrackingState state = GetOrCreateState(companion);
            if (state == null || !state.IsActive)
                return;

            if (state.ActionMode != AIGMCompanionTrackingActionMode.TrackOnly)
                TickTrackingAction(companion);

            if (!ShouldRefresh(state))
                return;

            Mobile owner = companion.GetOwner();
            state.LastReport = BuildTrackingSweepReport(companion, owner, state.Mode);
            state.LastScanUtc = DateTime.UtcNow;
            AIGMExecutionLog.Write("TRACKING_CYCLE_SWEEP companion={0} mode={1} report=\"{2}\"", companion.Serial.Value, state.Mode, SafeLog(state.LastReport));
        }

        public static string TickTrackingAction(BaseHire companion)
        {
            if (!IsValidCompanion(companion))
                return String.Empty;

            AIGMCompanionTrackingState state = GetOrCreateState(companion);
            if (state == null || !state.IsActive || state.ActionMode == AIGMCompanionTrackingActionMode.TrackOnly)
                return String.Empty;

            if (state.LastActionUtc != DateTime.MinValue && (DateTime.UtcNow - state.LastActionUtc).TotalSeconds < 1.0)
                return String.Empty;

            state.LastActionUtc = DateTime.UtcNow;

            Mobile target = ResolveCurrentTrackingTarget(companion, state);
            if (target == null)
                target = AcquireNearestActionTarget(companion, state.Mode, state);

            if (target == null)
            {
                state.StopReason = "no_valid_" + DescribeMode(state.Mode);
                state.LastActionResult = "no_valid_target";
                state.LastReport = BuildNoTrailLine(companion, state.Mode);
                RestorePursuitAuthority(companion, state, "no_valid_target");
                ClearActionTarget(state);
                if (state.ActionMode == AIGMCompanionTrackingActionMode.TrackHunt)
                    state.IsActive = false;
                AIGMExecutionLog.Write("TRACKING_ACTION_IDLE companion={0} mode={1} action={2} reason={3} accepted=\"{4}\" rejected=\"{5}\"", companion.Serial.Value, state.Mode, state.ActionMode, SafeLog(state.StopReason), SafeLog(state.LastCandidateSummary), SafeLog(state.LastRejectedCandidates));
                return state.LastReport;
            }

            CaptureActionTarget(companion, target, state);
            AIGMExecutionLog.Write("TRACKING_ACTION_TARGET companion={0} mode={1} action={2} target={3} name=\"{4}\" distance=\"{5}\" location=\"{6}\"", companion.Serial.Value, state.Mode, state.ActionMode, state.CurrentTargetSerial, SafeLog(state.CurrentTargetName), SafeLog(state.LastKnownDistanceText), SafeLog(state.LastKnownTileText));

            if (state.ActionMode == AIGMCompanionTrackingActionMode.TrackMove)
            {
                if (IsCivilianMode(state.Mode))
                {
                    state.LastActionResult = "civilian_move_hold";
                    state.LastReport = BuildCivilianMoveHoldLine(companion, state.Mode, target);
                    return state.LastReport;
                }

                if (companion.InRange(target, 2))
                {
                    state.LastActionResult = "arrived_no_combat";
                    state.LastReport = BuildArrivedLine(companion, state.Mode, target);
                    return state.LastReport;
                }

                if (TryStepTowardTarget(companion, target, state))
                {
                    state.LastActionResult = "moving_toward_target";
                    state.LastReport = BuildMoveLine(companion, state.Mode, target);
                    AIGMExecutionLog.Write("TRACKING_ACTION_MOVE companion={0} mode={1} action={2} target={3} result={4} fromDistance=\"{5}\" nowDistance=\"{6}\"", companion.Serial.Value, state.Mode, state.ActionMode, target.Serial.Value, state.LastActionResult, SafeLog(state.LastKnownDistanceText), SafeLog(((int)Math.Round(companion.GetDistanceToSqrt(target))) + " tiles"));
                    return state.LastReport;
                }

                state.LastActionResult = "movement_blocked";
                state.LastReport = BuildMovementBlockedLine(companion, target);
                AIGMExecutionLog.Write("TRACKING_ACTION_MOVE_BLOCKED companion={0} mode={1} action={2} target={3} distance=\"{4}\"", companion.Serial.Value, state.Mode, state.ActionMode, target.Serial.Value, SafeLog(((int)Math.Round(companion.GetDistanceToSqrt(target))) + " tiles"));
                return state.LastReport;
            }

            if (state.ActionMode == AIGMCompanionTrackingActionMode.TrackHunt)
            {
                if (!CanHuntMode(state.Mode))
                    return StartBlockedHunt(companion, companion.GetOwner(), state.Mode);

                int nativeOrderRange = AIGMCompanionCombatController.GetNativeOrderRange();
                int currentDistance = (int)Math.Round(companion.GetDistanceToSqrt(target));
                if (currentDistance > nativeOrderRange)
                {
                    AIGMExecutionLog.Write("TRACKING_ACTION_PURSUIT_REQUIRED companion={0} mode={1} target={2} distance={3} orderRange={4}", companion.Serial.Value, state.Mode, target.Serial.Value, currentDistance, nativeOrderRange);
                    AcquirePursuitAuthority(companion, target, state, currentDistance);

                    if (TryStepTowardTarget(companion, target, state))
                    {
                        state.LastActionResult = "pursuing_target";
                        state.LastReport = BuildHuntPursuitLine(companion, state.Mode, target);
                        AIGMExecutionLog.Write("TRACKING_ACTION_MOVE companion={0} name=\"{1}\" mode={2} action={3} target={4} targetName=\"{5}\" result={6} controlOrder={7} ownerDistance={8} from=\"{9}\" to=\"{10}\" beforeDistance=\"{11}\" afterDistance=\"{12}\" direction={13} moved=True", companion.Serial.Value, SafeLog(companion.Name), state.Mode, state.ActionMode, target.Serial.Value, SafeLog(DescribeMobile(target)), SafeLog(state.LastMoveResult), companion.ControlOrder, GetOwnerDistance(companion), FormatPoint(state.LastMoveFrom), FormatPoint(state.LastMoveTo), state.LastMoveDistanceBefore + " tiles", state.LastMoveDistanceAfter + " tiles", SafeLog(state.LastMoveDirection));
                        if (state.ConsecutivePursuitNoProgress == 0 && state.LastMoveDistanceAfter < state.LastMoveDistanceBefore)
                            AIGMExecutionLog.Write("TRACKING_ACTION_MOVE_RECOVERED companion={0} target={1} distance={2}", companion.Serial.Value, target.Serial.Value, state.LastMoveDistanceAfter);
                        else if (state.ConsecutivePursuitNoProgress >= 4)
                            AIGMExecutionLog.Write("TRACKING_ACTION_MOVE_STALLED companion={0} target={1} distance={2} noProgressTicks={3} reason={4}", companion.Serial.Value, target.Serial.Value, state.LastMoveDistanceAfter, state.ConsecutivePursuitNoProgress, SafeLog(state.LastMoveResult));
                        return state.LastReport;
                    }

                    state.LastActionResult = "pursuit_blocked";
                    state.LastReport = BuildMovementBlockedLine(companion, target);
                    AIGMExecutionLog.Write("TRACKING_ACTION_MOVE_BLOCKED companion={0} name=\"{1}\" mode={2} action={3} target={4} targetName=\"{5}\" controlOrder={6} ownerDistance={7} from=\"{8}\" to=\"{9}\" beforeDistance=\"{10}\" afterDistance=\"{11}\" direction={12} reason={13}", companion.Serial.Value, SafeLog(companion.Name), state.Mode, state.ActionMode, target.Serial.Value, SafeLog(DescribeMobile(target)), companion.ControlOrder, GetOwnerDistance(companion), FormatPoint(state.LastMoveFrom), FormatPoint(state.LastMoveTo), state.LastMoveDistanceBefore + " tiles", state.LastMoveDistanceAfter + " tiles", SafeLog(state.LastMoveDirection), SafeLog(state.LastMoveResult));
                    if (state.ConsecutivePursuitNoProgress >= 4)
                        AIGMExecutionLog.Write("TRACKING_ACTION_MOVE_STALLED companion={0} target={1} distance={2} noProgressTicks={3} reason={4}", companion.Serial.Value, target.Serial.Value, state.LastMoveDistanceAfter, state.ConsecutivePursuitNoProgress, SafeLog(state.LastMoveResult));
                    return state.LastReport;
                }

                ReleasePursuitAuthorityForCombat(companion, state, target, currentDistance);

                bool nativeOrderNeeded = companion.ControlTarget != target || companion.ControlOrder != OrderType.Attack;
                string nativeOrderResult = state.LastActionResult;
                if (nativeOrderNeeded)
                {
                    bool nativeOrdered = state.Mode == AIGMCompanionTrackingMode.Animals
                        ? AIGMCompanionCombatController.TryEngageAnimal(companion, target, out nativeOrderResult)
                        : AIGMCompanionCombatController.TryEngageMonster(companion, target, out nativeOrderResult);

                    state.LastActionResult = nativeOrderResult;
                    AIGMExecutionLog.Write("TRACKING_ACTION_NATIVE_KILL_ORDER companion={0} mode={1} target={2} ordered={3} result={4} distance={5} combatantAfter={6} warmodeAfter={7} controlTargetAfter={8} orderAfter={9}", companion.Serial.Value, state.Mode, target.Serial.Value, nativeOrdered, SafeLog(nativeOrderResult), currentDistance, companion.Combatant != null ? companion.Combatant.Serial.Value : 0, companion.Warmode, companion.ControlTarget != null ? companion.ControlTarget.Serial.Value : 0, companion.ControlOrder);

                    if (!nativeOrdered)
                    {
                        AIGMExecutionLog.Write("TRACKING_ACTION_TARGET_CLEAR companion={0} mode={1} target={2} reason={3}", companion.Serial.Value, state.Mode, target.Serial.Value, SafeLog(nativeOrderResult));
                        RestorePursuitAuthority(companion, state, nativeOrderResult);
                        ClearActionTarget(state);
                        state.LastReport = "I lost the mark and will find the next clean sign.";
                        return state.LastReport;
                    }
                }

                int engageRange = AIGMCompanionCombatController.GetPreferredEngagementRange(companion);
                if (!companion.InRange(target, engageRange))
                {
                    if (TryStepTowardTarget(companion, target, state))
                    {
                        state.LastActionResult = "pursuing_target";
                        state.LastReport = BuildHuntPursuitLine(companion, state.Mode, target);
                        AIGMExecutionLog.Write("TRACKING_ACTION_MOVE companion={0} mode={1} action={2} target={3} result={4} fromDistance=\"{5}\" nowDistance=\"{6}\"", companion.Serial.Value, state.Mode, state.ActionMode, target.Serial.Value, state.LastActionResult, SafeLog(state.LastKnownDistanceText), SafeLog(((int)Math.Round(companion.GetDistanceToSqrt(target))) + " tiles"));
                        return state.LastReport;
                    }

                    state.LastActionResult = "pursuit_blocked";
                    state.LastReport = BuildMovementBlockedLine(companion, target);
                    AIGMExecutionLog.Write("TRACKING_ACTION_MOVE_BLOCKED companion={0} mode={1} action={2} target={3} distance=\"{4}\"", companion.Serial.Value, state.Mode, state.ActionMode, target.Serial.Value, SafeLog(((int)Math.Round(companion.GetDistanceToSqrt(target))) + " tiles"));
                    return state.LastReport;
                }

                if (companion.ControlTarget == target && companion.ControlOrder == OrderType.Attack)
                {
                    state.LastActionResult = "native_kill_sustained";
                    state.LastReport = BuildEngageLine(companion, state.Mode, target);
                    AIGMExecutionLog.Write("TRACKING_ACTION_COMBAT_SUSTAIN companion={0} mode={1} target={2} distance=\"{3}\" combatant={4} warmode={5} controlTarget={6} order={7}", companion.Serial.Value, state.Mode, target.Serial.Value, SafeLog(((int)Math.Round(companion.GetDistanceToSqrt(target))) + " tiles"), companion.Combatant != null ? companion.Combatant.Serial.Value : 0, companion.Warmode, companion.ControlTarget != null ? companion.ControlTarget.Serial.Value : 0, companion.ControlOrder);
                    return state.LastReport;
                }

                string combatResult;
                AIGMExecutionLog.Write("TRACKING_ACTION_COMBAT_ATTEMPT companion={0} mode={1} target={2} distance=\"{3}\" combatantBefore={4} warmodeBefore={5} orderBefore={6}", companion.Serial.Value, state.Mode, target.Serial.Value, SafeLog(((int)Math.Round(companion.GetDistanceToSqrt(target))) + " tiles"), companion.Combatant != null ? companion.Combatant.Serial.Value : 0, companion.Warmode, companion.ControlOrder);
                bool engaged = state.Mode == AIGMCompanionTrackingMode.Animals
                    ? AIGMCompanionCombatController.TryEngageAnimal(companion, target, out combatResult)
                    : AIGMCompanionCombatController.TryEngageMonster(companion, target, out combatResult);

                state.LastActionResult = combatResult;
                AIGMExecutionLog.Write("TRACKING_ACTION_COMBAT_RESULT companion={0} mode={1} target={2} engaged={3} result={4} combatantAfter={5} warmodeAfter={6} controlTargetAfter={7} orderAfter={8}", companion.Serial.Value, state.Mode, target.Serial.Value, engaged, SafeLog(combatResult), companion.Combatant != null ? companion.Combatant.Serial.Value : 0, companion.Warmode, companion.ControlTarget != null ? companion.ControlTarget.Serial.Value : 0, companion.ControlOrder);
                if (engaged)
                {
                    state.LastReport = BuildEngageLine(companion, state.Mode, target);
                    return state.LastReport;
                }

                RestorePursuitAuthority(companion, state, combatResult);
                ClearActionTarget(state);
                state.LastReport = "I lost the mark and will find the next clean sign.";
                return state.LastReport;
            }

            return String.Empty;
        }

        public static AIGMCompanionTrackingMode GetModeFromIntentKind(string intentKind)
        {
            switch (intentKind)
            {
                case AIGMCompanionIntentKind.TrackAnimals:
                case AIGMCompanionIntentKind.StartTrackingAnimals:
                    return AIGMCompanionTrackingMode.Animals;
                case AIGMCompanionIntentKind.TrackPlayers:
                case AIGMCompanionIntentKind.StartTrackingPlayers:
                    return AIGMCompanionTrackingMode.Players;
                case AIGMCompanionIntentKind.TrackHumanNPCs:
                case AIGMCompanionIntentKind.StartTrackingHumanNPCs:
                    return AIGMCompanionTrackingMode.HumanNPCs;
                case AIGMCompanionIntentKind.TrackNPCs:
                case AIGMCompanionIntentKind.StartTrackingNPCs:
                    return AIGMCompanionTrackingMode.NPCs;
                case AIGMCompanionIntentKind.TrackAll:
                case AIGMCompanionIntentKind.StartTrackingAll:
                    return AIGMCompanionTrackingMode.All;
                case AIGMCompanionIntentKind.ReportThreats:
                    return AIGMCompanionTrackingMode.Threats;
                case AIGMCompanionIntentKind.TrackMonsters:
                case AIGMCompanionIntentKind.StartTrackingMonsters:
                    return AIGMCompanionTrackingMode.Monsters;
                default:
                    return AIGMCompanionTrackingMode.General;
            }
        }

        public static bool IsExplicitGroupTrackingCommand(string speech)
        {
            if (String.IsNullOrWhiteSpace(speech))
                return false;

            string normalized = NormalizeSpeech(speech);
            return normalized.Equals("start tracking all")
                || normalized.Equals("all start tracking")
                || normalized.Equals("companions start tracking")
                || normalized.Equals("everyone start tracking")
                || normalized.Equals("all tracking status")
                || normalized.Equals("all track animals")
                || normalized.Equals("all track monsters")
                || normalized.Equals("all track players")
                || normalized.Equals("all track npcs")
                || normalized.Equals("all track humans")
                || normalized.Equals("all track human npcs");
        }

        public static AIGMCompanionTrackingMode GetModeFromSpeech(string speech)
        {
            string normalized = NormalizeSpeech(speech);
            if (normalized.Contains("track animals") || normalized.Contains("track animal"))
                return AIGMCompanionTrackingMode.Animals;
            if (normalized.Contains("track monsters") || normalized.Contains("track monster") || normalized.Contains("track enemies") || normalized.Contains("track hostiles"))
                return AIGMCompanionTrackingMode.Monsters;
            if (normalized.Contains("track players") || normalized.Contains("track player"))
                return AIGMCompanionTrackingMode.Players;
            if (normalized.Contains("track npcs") || normalized.Contains("track npc"))
                return AIGMCompanionTrackingMode.NPCs;
            if (normalized.Contains("track humans") || normalized.Contains("track human") || normalized.Contains("track human npcs") || normalized.Contains("track people"))
                return AIGMCompanionTrackingMode.HumanNPCs;
            if (normalized.Contains("track all") || normalized.Contains("start tracking all") || normalized.Contains("all start tracking") || normalized.Contains("companions start tracking") || normalized.Contains("everyone start tracking"))
                return AIGMCompanionTrackingMode.All;
            return AIGMCompanionTrackingMode.General;
        }

        public static AIGMCompanionTrackingState GetState(BaseHire companion)
        {
            if (companion == null || companion.Deleted)
                return null;

            return GetOrCreateState(companion);
        }

        public static string DescribeModeForCognition(AIGMCompanionTrackingMode mode)
        {
            return DescribeMode(mode);
        }

        public static string BuildHiddenTrackingSummary(BaseHire companion)
        {
            AIGMCompanionTrackingState state = GetState(companion);
            if (state == null)
                return "not tracking; no recent trail";

            if (!state.IsActive && String.IsNullOrWhiteSpace(state.LastReport))
                return "not tracking; no recent trail";

            List<string> parts = new List<string>();
            parts.Add(state.IsActive ? "tracking active" : "not tracking");
            parts.Add("focus=" + DescribeMode(state.Mode));
            if (!String.IsNullOrWhiteSpace(state.SkillTier))
                parts.Add("skill=" + state.SkillTier);
            parts.Add("action=" + DescribeActionModeForCognition(state.ActionMode));
            string nearest = BuildNearestTrailSummary(state);
            if (!String.IsNullOrWhiteSpace(nearest))
                parts.Add("nearest=" + nearest);
            string threat = BuildThreatSummary(state);
            if (!String.IsNullOrWhiteSpace(threat))
                parts.Add("threat=" + threat);
            if (!String.IsNullOrWhiteSpace(state.LastCandidateSummary))
                parts.Add("signs=" + Shorten(NaturalizeCandidateSummary(state.LastCandidateSummary), 90));

            return String.Join("; ", parts.ToArray());
        }

        public static string BuildNearestTrailSummary(AIGMCompanionTrackingState state)
        {
            if (state == null || String.IsNullOrWhiteSpace(state.LastKnownTargetDescription))
                return "none";

            List<string> parts = new List<string>();
            parts.Add(state.LastKnownTargetDescription.Trim());
            if (!String.IsNullOrWhiteSpace(state.LastKnownDirectionText))
                parts.Add(state.LastKnownDirectionText.Trim());
            if (!String.IsNullOrWhiteSpace(state.LastKnownDistanceText))
                parts.Add(state.LastKnownDistanceText.Trim());

            return String.Join(", ", parts.ToArray());
        }

        public static string BuildThreatSummary(AIGMCompanionTrackingState state)
        {
            if (state == null)
                return "no tracked threat";

            bool threatMode = state.Mode == AIGMCompanionTrackingMode.Threats || state.Mode == AIGMCompanionTrackingMode.Monsters || state.Mode == AIGMCompanionTrackingMode.All;
            if (!threatMode && String.IsNullOrWhiteSpace(state.LastKnownTargetDescription))
                return "no tracked threat";

            if (String.IsNullOrWhiteSpace(state.LastKnownTargetDescription))
                return "no tracked threat";

            return BuildNearestTrailSummary(state);
        }

        public static string BuildTrackingCapabilitySummary(BaseHire companion, AIGMCompanionTrackingState state)
        {
            if (companion == null || companion.Deleted)
                return "tracking unavailable";

            double tracking = state != null && state.SkillValue > 0.0 ? state.SkillValue : AIGMCompanionSkillReadiness.GetSkillValue(companion, SkillName.Tracking);
            string tier = !String.IsNullOrWhiteSpace(state != null ? state.SkillTier : null) ? state.SkillTier : AIGMCompanionSkillReadiness.BuildTier(tracking);
            string action = state != null ? DescribeActionModeForCognition(state.ActionMode) : "track only";
            return String.Format("Tracking {0:0.0}, {1}; action {2}", tracking, tier, action);
        }

        public static string BuildTrackingRecommendationSummary(AIGMCompanionTrackingState state)
        {
            if (state == null || !state.IsActive)
                return "not tracking; start a scan before relying on trail signs";

            if (String.IsNullOrWhiteSpace(state.LastKnownTargetDescription))
                return "keep scanning; no clear fresh trail nearby";

            if (state.ActionMode == AIGMCompanionTrackingActionMode.TrackHunt)
                return "continue the hunt while valid " + DescribeMode(state.Mode) + " remain nearby; nearest sign is " + BuildNearestTrailSummary(state);

            if (state.ActionMode == AIGMCompanionTrackingActionMode.TrackMove)
                return "follow the trail without combat; nearest sign is " + BuildNearestTrailSummary(state);

            return "keep the trail under watch; nearest sign is " + BuildNearestTrailSummary(state);
        }

        public static bool IsHostileMonsterSummary(AIGMSceneEntitySummary mob)
        {
            if (mob == null)
                return false;

            Mobile target = World.FindMobile(mob.Serial);
            if (target == null || target.Deleted)
                return false;

            string rejectReason;
            return AIGMCompanionTargetValidator.IsHostileMonsterCandidate(null, target, out rejectReason);
        }

        public static Mobile FindClosestHostileMonster(BaseHire companion, out string acceptedSummary, out string rejectedSummary)
        {
            acceptedSummary = "accepted=none";
            rejectedSummary = "none";

            if (!IsValidCompanion(companion))
                return null;

            AIGMCompanionTrackingState state = GetOrCreateState(companion);
            double tracking = AIGMCompanionSkillReadiness.GetSkillValue(companion, SkillName.Tracking);
            state.SkillValue = tracking;
            state.SkillTier = AIGMCompanionSkillReadiness.BuildTier(tracking);
            state.LastConfidence = BuildConfidence(companion, AIGMCompanionTrackingMode.Monsters);

            AIGMTrackingSelectionResult selection = AIGMCompanionTrackingSensor.SelectClosestHostileMonster(companion, GetTrackingRange(companion));
            acceptedSummary = selection.AcceptedSummary;
            rejectedSummary = selection.RejectedSummary;
            state.LastCandidateSummary = acceptedSummary;
            state.LastRejectedCandidates = rejectedSummary;
            state.LastScanUtc = DateTime.UtcNow;

            if (selection.SelectedTarget == null)
            {
                ClearLastTarget(state);
                return null;
            }

            state.LastKnownTargetDescription = selection.SelectedName;
            state.LastKnownDirectionText = selection.SelectedDirection;
            state.LastKnownDistanceText = selection.SelectedDistanceText;
            state.LastKnownTileText = selection.SelectedTileText;
            return selection.SelectedTarget;
        }

        public static string DescribeActionModeForCognition(AIGMCompanionTrackingActionMode actionMode)
        {
            switch (actionMode)
            {
                case AIGMCompanionTrackingActionMode.TrackMove: return "track and move";
                case AIGMCompanionTrackingActionMode.TrackHunt: return "track and hunt";
                default: return "track only";
            }
        }

        public static string BuildTrackingActionSummary(BaseHire companion, AIGMCompanionTrackingState state)
        {
            if (state == null)
                return "track only; no active target";

            List<string> parts = new List<string>();
            parts.Add("action=" + DescribeActionModeForCognition(state.ActionMode));
            parts.Add("category=" + DescribeMode(state.Mode));
            if (!String.IsNullOrWhiteSpace(state.CurrentTargetName))
                parts.Add("target=" + state.CurrentTargetName);
            if (!String.IsNullOrWhiteSpace(state.LastKnownDistanceText))
                parts.Add("distance=" + state.LastKnownDistanceText);
            parts.Add("engagementAllowed=" + state.EngagementAllowed);
            if (!String.IsNullOrWhiteSpace(state.LastActionResult))
                parts.Add("lastAction=" + state.LastActionResult);
            if (!String.IsNullOrWhiteSpace(state.StopReason))
                parts.Add("stopReason=" + state.StopReason);

            return String.Join("; ", parts.ToArray());
        }

        private static bool IsValidCompanion(BaseHire companion)
        {
            return companion != null && !companion.Deleted && companion.Alive && companion.Map != null;
        }

        private static AIGMCompanionTrackingActionMode GetActionModeFromSpeech(string speech, string kind, AIGMCompanionTrackingMode mode)
        {
            string normalized = NormalizeSpeech(speech);

            if (kind == AIGMCompanionIntentKind.StartMonsterHunt || kind == AIGMCompanionIntentKind.HuntAnimals)
                return AIGMCompanionTrackingActionMode.TrackHunt;

            if (ContainsAny(normalized, "track only", "find closest", "find nearest", "where is the nearest", "scan "))
                return AIGMCompanionTrackingActionMode.TrackOnly;

            if (ContainsAny(normalized, "track and move", "move to", "follow trail to"))
                return AIGMCompanionTrackingActionMode.TrackMove;

            if (ContainsAny(normalized, "hunt", "kill", "attack", "bring it down"))
                return AIGMCompanionTrackingActionMode.TrackHunt;

            if (kind == AIGMCompanionIntentKind.StartTrackingCycle || kind == AIGMCompanionIntentKind.StartTrackingMonsters || kind == AIGMCompanionIntentKind.TrackMonsters)
                return AIGMCompanionTrackingActionMode.TrackHunt;

            if (kind == AIGMCompanionIntentKind.StartTrackingAnimals || kind == AIGMCompanionIntentKind.TrackAnimals)
                return AIGMCompanionTrackingActionMode.TrackHunt;

            if (mode == AIGMCompanionTrackingMode.Players || mode == AIGMCompanionTrackingMode.NPCs || mode == AIGMCompanionTrackingMode.HumanNPCs)
                return AIGMCompanionTrackingActionMode.TrackOnly;

            return AIGMCompanionTrackingActionMode.TrackOnly;
        }

        private static bool CanHuntMode(AIGMCompanionTrackingMode mode)
        {
            return mode == AIGMCompanionTrackingMode.Monsters || mode == AIGMCompanionTrackingMode.Animals;
        }

        private static bool IsCivilianMode(AIGMCompanionTrackingMode mode)
        {
            return mode == AIGMCompanionTrackingMode.Players || mode == AIGMCompanionTrackingMode.NPCs || mode == AIGMCompanionTrackingMode.HumanNPCs;
        }

        private static string StartBlockedHunt(BaseHire companion, Mobile speaker, AIGMCompanionTrackingMode mode)
        {
            AIGMCompanionTrackingState state = GetOrCreateState(companion);
            state.Mode = mode;
            state.ActionMode = AIGMCompanionTrackingActionMode.TrackOnly;
            state.IsActive = false;
            state.EngagementAllowed = false;
            state.StopReason = "hunt_blocked_" + DescribeMode(mode);
            state.LastActionResult = "hunt_blocked";
            state.LastReport = BuildTrackingSweepReport(companion, speaker, mode);
            AIGMCompanionCombatController.StopCombat(companion);
            AIGMExecutionLog.Write("TRACKING_ACTION_BLOCKED companion={0} mode={1} action=TrackHunt reason={2}", companion.Serial.Value, mode, SafeLog(state.StopReason));
            return "I can find them, but I will not take steel to them without a stronger order and proper authority.";
        }

        private static Mobile ResolveCurrentTrackingTarget(BaseHire companion, AIGMCompanionTrackingState state)
        {
            if (companion == null || state == null || state.CurrentTargetSerial == 0)
                return null;

            Mobile target = World.FindMobile(state.CurrentTargetSerial);
            if (!IsValidActionTarget(companion, target, state.Mode, state.ActionMode, GetTrackingRange(companion)))
            {
                state.LastActionResult = "target_invalid_or_lost";
                RestorePursuitAuthority(companion, state, "target_invalid_or_lost");
                return null;
            }

            return target;
        }

        private static Mobile AcquireNearestActionTarget(BaseHire companion, AIGMCompanionTrackingMode mode, AIGMCompanionTrackingState state)
        {
            if (!IsValidCompanion(companion) || state == null)
                return null;

            int range = GetTrackingRange(companion);
            Mobile best = null;
            double bestDistance = Double.MaxValue;
            List<string> accepted = new List<string>();
            List<string> rejected = new List<string>();

            IPooledEnumerable eable = companion.GetMobilesInRange(range);
            foreach (Mobile mob in eable)
            {
                if (mob == null || mob == companion || mob.Deleted || mob.Map != companion.Map)
                    continue;

                string reason;
                if (!IsValidActionTarget(companion, mob, mode, state.ActionMode, range, out reason))
                {
                    if (rejected.Count < 16)
                        rejected.Add(DescribeMobile(mob) + "=" + reason);
                    continue;
                }

                double distance = companion.GetDistanceToSqrt(mob);
                if (accepted.Count < 16)
                    accepted.Add(DescribeMobile(mob) + "@" + (int)Math.Round(distance));

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = mob;
                }
            }
            eable.Free();

            state.LastCandidateSummary = accepted.Count > 0 ? "accepted=" + String.Join(", ", accepted.ToArray()) : "accepted=none";
            state.LastRejectedCandidates = rejected.Count > 0 ? String.Join(", ", rejected.ToArray()) : "none";
            state.LastScanUtc = DateTime.UtcNow;

            if (best == null)
            {
                RestorePursuitAuthority(companion, state, "no_selected_target");
                ClearActionTarget(state);
                return null;
            }

            CaptureActionTarget(companion, best, state);
            return best;
        }

        private static bool IsValidActionTarget(BaseHire companion, Mobile target, AIGMCompanionTrackingMode mode, AIGMCompanionTrackingActionMode actionMode, int range)
        {
            string reason;
            return IsValidActionTarget(companion, target, mode, actionMode, range, out reason);
        }

        private static bool IsValidActionTarget(BaseHire companion, Mobile target, AIGMCompanionTrackingMode mode, AIGMCompanionTrackingActionMode actionMode, int range, out string reason)
        {
            reason = String.Empty;

            if (mode == AIGMCompanionTrackingMode.Monsters || mode == AIGMCompanionTrackingMode.Threats)
            {
                AIGMCompanionTargetValidationResult validation = AIGMCompanionTargetValidator.ValidateMonsterTarget(companion, target, range);
                reason = validation.Reason;
                return validation.Allowed;
            }

            if (mode == AIGMCompanionTrackingMode.Animals)
            {
                AIGMCompanionTargetValidationResult validation = AIGMCompanionTargetValidator.ValidateAnimalTarget(companion, target, range);
                reason = validation.Reason;
                return validation.Allowed;
            }

            AIGMTrackingSelectionResult selected = AIGMCompanionTrackingSensor.SelectNearest(companion, mode, range);
            if (selected.SelectedTarget == null || selected.SelectedTarget != target)
            {
                reason = "not_selected_track_target";
                return false;
            }

            reason = "track_target_valid";
            return true;
        }

        private static void CaptureActionTarget(BaseHire companion, Mobile target, AIGMCompanionTrackingState state)
        {
            if (companion == null || target == null || state == null)
                return;

            string name = DescribeMobile(target);
            state.CurrentTargetSerial = target.Serial.Value;
            state.CurrentTargetName = name;
            state.LastKnownTargetDescription = name;
            state.LastKnownDirectionText = companion.GetDirectionTo(target).ToString();
            state.LastKnownDistanceText = ((int)Math.Round(companion.GetDistanceToSqrt(target))) + " tiles";
            state.LastKnownTileText = String.Format("{0},{1},{2}", target.X, target.Y, target.Z);
            state.LastKnownTargetLocation = target.Location;
        }

        private static void AcquirePursuitAuthority(BaseHire companion, Mobile target, AIGMCompanionTrackingState state, int distance)
        {
            if (companion == null || target == null || state == null)
                return;

            if (!state.PursuitAuthorityActive)
            {
                state.PursuitAuthorityActive = true;
                state.PreviousControlOrder = companion.ControlOrder;
                Mobile previousTarget = companion.ControlTarget as Mobile;
                state.PreviousControlTargetWasSet = previousTarget != null;
                state.PreviousControlTargetSerial = previousTarget != null ? previousTarget.Serial.Value : 0;
                AIGMExecutionLog.Write("TRACKING_ACTION_PURSUIT_AUTHORITY_ACQUIRED companion={0} target={1} distance={2} previousOrder={3} previousTarget={4} ownerDistance={5}", companion.Serial.Value, target.Serial.Value, distance, state.PreviousControlOrder, state.PreviousControlTargetSerial, GetOwnerDistance(companion));
            }
            else if (companion.ControlOrder == OrderType.Follow || companion.ControlOrder == OrderType.Come)
            {
                AIGMExecutionLog.Write("TRACKING_ACTION_PURSUIT_AUTHORITY_ACQUIRED companion={0} target={1} distance={2} previousOrder={3} previousTarget={4} ownerDistance={5} reason=reassert_after_follow", companion.Serial.Value, target.Serial.Value, distance, state.PreviousControlOrder, state.PreviousControlTargetSerial, GetOwnerDistance(companion));
            }

            companion.CantWalk = false;
            companion.ControlTarget = null;
            companion.Combatant = null;
            companion.Warmode = false;
            if (companion.ControlOrder == OrderType.Follow || companion.ControlOrder == OrderType.Come || companion.ControlOrder == OrderType.Attack)
                companion.ControlOrder = OrderType.Stay;
            companion.CurrentSpeed = companion.ActiveSpeed;
        }

        private static void ReleasePursuitAuthorityForCombat(BaseHire companion, AIGMCompanionTrackingState state, Mobile target, int distance)
        {
            if (companion == null || state == null || !state.PursuitAuthorityActive)
                return;

            AIGMExecutionLog.Write("TRACKING_ACTION_PURSUIT_AUTHORITY_RESTORED companion={0} target={1} distance={2} restored=False reason=enter_native_order_range currentOrder={3}", companion.Serial.Value, target != null ? target.Serial.Value : 0, distance, companion.ControlOrder);
            state.PursuitAuthorityActive = false;
            state.PreviousControlOrder = OrderType.None;
            state.PreviousControlTargetSerial = 0;
            state.PreviousControlTargetWasSet = false;
        }

        private static bool RestorePursuitAuthority(BaseHire companion, AIGMCompanionTrackingState state, string reason)
        {
            if (companion == null || state == null || !state.PursuitAuthorityActive)
                return false;

            OrderType restoreOrder = state.PreviousControlOrder;
            if (restoreOrder == OrderType.Attack)
                restoreOrder = OrderType.Follow;

            Mobile restoreTarget = null;
            if (state.PreviousControlTargetWasSet && state.PreviousControlTargetSerial != 0)
                restoreTarget = World.FindMobile(state.PreviousControlTargetSerial);

            if ((restoreOrder == OrderType.Follow || restoreOrder == OrderType.Come) && (restoreTarget == null || restoreTarget.Deleted || restoreTarget.Map != companion.Map))
                restoreTarget = companion.GetOwner();

            companion.ControlTarget = restoreTarget;
            companion.ControlOrder = restoreOrder;
            companion.CantWalk = false;

            AIGMExecutionLog.Write("TRACKING_ACTION_PURSUIT_AUTHORITY_RESTORED companion={0} restored=True reason={1} order={2} target={3} ownerDistance={4}", companion.Serial.Value, SafeLog(reason), companion.ControlOrder, restoreTarget != null ? restoreTarget.Serial.Value : 0, GetOwnerDistance(companion));

            state.PursuitAuthorityActive = false;
            state.PreviousControlOrder = OrderType.None;
            state.PreviousControlTargetSerial = 0;
            state.PreviousControlTargetWasSet = false;
            return true;
        }

        private static void ClearActionTarget(AIGMCompanionTrackingState state)
        {
            if (state == null)
                return;

            state.CurrentTargetSerial = 0;
            state.CurrentTargetName = String.Empty;
            state.LastKnownTargetLocation = Point3D.Zero;
            state.PursuitPathFollower = null;
            state.PursuitPathTargetSerial = 0;
            state.LastPursuitLocation = Point3D.Zero;
            state.LastPursuitDistance = -1;
            state.ConsecutivePursuitNoProgress = 0;
            state.LastMoveDirection = String.Empty;
            state.LastMoveFrom = Point3D.Zero;
            state.LastMoveTo = Point3D.Zero;
            state.LastMoveDistanceBefore = -1;
            state.LastMoveDistanceAfter = -1;
            state.LastMoveResult = String.Empty;
            state.PursuitAuthorityActive = false;
            state.PreviousControlOrder = OrderType.None;
            state.PreviousControlTargetSerial = 0;
            state.PreviousControlTargetWasSet = false;
            ClearLastTarget(state);
        }

        private static bool TryStepTowardTarget(BaseHire companion, Mobile target, AIGMCompanionTrackingState state)
        {
            if (companion == null || target == null || state == null || companion.Map == null || target.Map != companion.Map)
                return false;

            companion.CantWalk = false;
            Point3D before = companion.Location;
            int beforeDistance = (int)Math.Round(companion.GetDistanceToSqrt(target));
            if (state.LastMoveTo != Point3D.Zero && before != state.LastMoveTo)
                AIGMExecutionLog.Write("TRACKING_ACTION_MOVE_STALLED companion={0} target={1} distance={2} previousAfter=\"{3}\" nextTickStart=\"{4}\" controlOrder={5} ownerDistance={6} reason=next_tick_start_changed", companion.Serial.Value, target.Serial.Value, beforeDistance, FormatPoint(state.LastMoveTo), FormatPoint(before), companion.ControlOrder, GetOwnerDistance(companion));
            state.LastMoveFrom = before;
            state.LastMoveTo = before;
            state.LastMoveDistanceBefore = beforeDistance;
            state.LastMoveDistanceAfter = beforeDistance;
            state.LastMoveDirection = (companion.GetDirectionTo(target) & Direction.Mask).ToString();
            state.LastMoveResult = "not_attempted";

            if (state.PursuitPathFollower == null || state.PursuitPathTargetSerial != target.Serial.Value)
            {
                state.PursuitPathFollower = new PathFollower(companion, target);
                state.PursuitPathTargetSerial = target.Serial.Value;
                state.ConsecutivePursuitNoProgress = 0;
                state.LastPursuitDistance = beforeDistance;
                state.LastPursuitLocation = before;
            }

            if (TryPathFollowerStep(companion, target, state, before, beforeDistance))
                return true;

            Point3D[] candidates = BuildStepCandidates(companion, target.Location);
            for (int i = 0; i < candidates.Length; i++)
            {
                if (candidates[i] == companion.Location)
                    continue;

                Direction direction = companion.GetDirectionTo(candidates[i]) & Direction.Mask;
                state.LastMoveDirection = direction.ToString();
                if (TryMove(companion, direction))
                {
                    RecordPursuitStep(companion, target, state, before, beforeDistance, "direct_step");
                    return true;
                }

                AIGMCompanionDoorResult door = AIGMCompanionDoorService.TryOpenNearbyDoorDetailed(companion);
                if (door.Opened && TryMove(companion, direction))
                {
                    RecordPursuitStep(companion, target, state, before, beforeDistance, "direct_step_after_door:" + door.Target);
                    return true;
                }
            }

            state.LastMoveTo = companion.Location;
            state.LastMoveDistanceAfter = (int)Math.Round(companion.GetDistanceToSqrt(target));
            state.LastMoveResult = "movement_blocked";
            NotePursuitProgress(state, before, beforeDistance);
            return false;
        }

        private static bool TryPathFollowerStep(BaseHire companion, Mobile target, AIGMCompanionTrackingState state, Point3D before, int beforeDistance)
        {
            if (state.PursuitPathFollower == null)
                return false;

            state.LastMoveDirection = (companion.GetDirectionTo(target) & Direction.Mask).ToString();
            bool followed = state.PursuitPathFollower.Follow(true, 1);
            if (companion.Location != before)
            {
                RecordPursuitStep(companion, target, state, before, beforeDistance, "path_follower_step");
                return true;
            }

            if (followed)
            {
                RecordPursuitStep(companion, target, state, before, beforeDistance, "path_follower_in_range");
                return true;
            }

            AIGMCompanionDoorResult door = AIGMCompanionDoorService.TryOpenNearbyDoorDetailed(companion);
            if (door.Opened)
            {
                state.PursuitPathFollower.ForceRepath();
                Point3D doorBefore = companion.Location;
                bool afterDoor = state.PursuitPathFollower.Follow(true, 1);
                if (companion.Location != doorBefore || afterDoor)
                {
                    RecordPursuitStep(companion, target, state, before, beforeDistance, "path_follower_after_door:" + door.Target);
                    return true;
                }
            }

            state.PursuitPathFollower.ForceRepath();
            return false;
        }

        private static void RecordPursuitStep(BaseHire companion, Mobile target, AIGMCompanionTrackingState state, Point3D before, int beforeDistance, string result)
        {
            state.LastMoveFrom = before;
            state.LastMoveTo = companion.Location;
            state.LastMoveDistanceBefore = beforeDistance;
            state.LastMoveDistanceAfter = (int)Math.Round(companion.GetDistanceToSqrt(target));
            state.LastMoveDirection = before != companion.Location
                ? DirectionFromStep(before, companion.Location).ToString()
                : (companion.GetDirectionTo(target) & Direction.Mask).ToString();
            state.LastMoveResult = result;
            NotePursuitProgress(state, before, beforeDistance);
        }

        private static void NotePursuitProgress(AIGMCompanionTrackingState state, Point3D before, int beforeDistance)
        {
            bool progressed = state.LastMoveDistanceAfter < beforeDistance;
            if (progressed)
                state.ConsecutivePursuitNoProgress = 0;
            else
                state.ConsecutivePursuitNoProgress++;

            state.LastPursuitLocation = state.LastMoveTo;
            state.LastPursuitDistance = state.LastMoveDistanceAfter;
        }

        private static Point3D[] BuildStepCandidates(BaseHire companion, Point3D destination)
        {
            int dx = Math.Sign(destination.X - companion.X);
            int dy = Math.Sign(destination.Y - companion.Y);

            return new Point3D[]
            {
                new Point3D(companion.X + dx, companion.Y + dy, companion.Z),
                new Point3D(companion.X + dx, companion.Y, companion.Z),
                new Point3D(companion.X, companion.Y + dy, companion.Z),
                new Point3D(companion.X + dx, companion.Y - dy, companion.Z),
                new Point3D(companion.X - dx, companion.Y + dy, companion.Z)
            };
        }

        private static bool TryMove(BaseHire companion, Direction direction)
        {
            if (companion == null)
                return false;

            Direction masked = direction & Direction.Mask;
            if ((companion.Direction & Direction.Mask) != masked)
                companion.Direction = masked;

            return companion.Move(masked);
        }

        private static Direction DirectionFromStep(Point3D from, Point3D to)
        {
            int dx = Math.Sign(to.X - from.X);
            int dy = Math.Sign(to.Y - from.Y);

            if (dx == 0 && dy < 0)
                return Direction.North;
            if (dx > 0 && dy < 0)
                return Direction.Right;
            if (dx > 0 && dy == 0)
                return Direction.East;
            if (dx > 0 && dy > 0)
                return Direction.Down;
            if (dx == 0 && dy > 0)
                return Direction.South;
            if (dx < 0 && dy > 0)
                return Direction.Left;
            if (dx < 0 && dy == 0)
                return Direction.West;
            if (dx < 0 && dy < 0)
                return Direction.Up;

            return Direction.North;
        }

        private static string FormatPoint(Point3D p)
        {
            return String.Format("{0},{1},{2}", p.X, p.Y, p.Z);
        }

        private static int GetOwnerDistance(BaseHire companion)
        {
            Mobile owner = companion != null ? companion.GetOwner() : null;
            if (companion == null || owner == null || owner.Map != companion.Map)
                return -1;

            return (int)Math.Round(companion.GetDistanceToSqrt(owner));
        }

        private static string DescribeMobile(Mobile target)
        {
            if (target == null)
                return "nothing";

            return String.IsNullOrWhiteSpace(target.Name) ? target.GetType().Name : target.Name.Replace(",", String.Empty);
        }

        private static AIGMCompanionTrackingState GetOrCreateState(BaseHire companion)
        {
            AIGMCompanionTrackingState state;
            if (!States.TryGetValue(companion.Serial.Value, out state))
            {
                state = new AIGMCompanionTrackingState();
                state.CompanionSerial = companion.Serial.Value;
                States[companion.Serial.Value] = state;
            }

            return state;
        }

        private static bool ShouldRefresh(AIGMCompanionTrackingState state)
        {
            return state != null && (state.LastScanUtc == DateTime.MinValue || (DateTime.UtcNow - state.LastScanUtc).TotalSeconds >= 10.0);
        }

        private static int GetTrackingRange(BaseHire companion)
        {
            double tracking = AIGMCompanionSkillReadiness.GetSkillValue(companion, SkillName.Tracking);
            return 10 + (int)(tracking / 10.0);
        }

        private static string DescribeMode(AIGMCompanionTrackingMode mode)
        {
            switch (mode)
            {
                case AIGMCompanionTrackingMode.Animals: return "animals";
                case AIGMCompanionTrackingMode.Monsters: return "monsters";
                case AIGMCompanionTrackingMode.Players: return "players";
                case AIGMCompanionTrackingMode.HumanNPCs: return "human NPCs";
                case AIGMCompanionTrackingMode.NPCs: return "NPCs";
                case AIGMCompanionTrackingMode.Threats: return "threats";
                case AIGMCompanionTrackingMode.All: return "all signs";
                default: return "the nearby ground";
            }
        }

        private static string BuildStartLine(BaseHire companion, AIGMCompanionTrackingState state)
        {
            string focus = DescribeMode(state != null ? state.Mode : AIGMCompanionTrackingMode.General);
            switch (GetCompanionId(companion))
            {
                case "dakeyras":
                    return String.Format("I will read {0}. If anything crossed near us, I will find the sign.", focus);
                case "danyal":
                    return String.Format("I will keep watch while the trail is read. Nothing moves through {0} for free.", focus);
                case "dardalion":
                    return String.Format("I will hold the line and mark what stirs in {0}.", focus);
                default:
                    return String.Format("I will read {0} and keep it scan-only.", focus);
            }
        }

        private static string BuildActionStartLine(BaseHire companion, AIGMCompanionTrackingState state)
        {
            string focus = DescribeMode(state != null ? state.Mode : AIGMCompanionTrackingMode.Monsters);
            if (state != null && state.ActionMode == AIGMCompanionTrackingActionMode.TrackHunt)
            {
                if (state.Mode == AIGMCompanionTrackingMode.Animals)
                    return "I will follow the nearest animal sign and bring it down if it stays in range.";

                return "I have the trail. I will take the nearest monster first and keep moving until the ground goes quiet.";
            }

            if (state != null && state.ActionMode == AIGMCompanionTrackingActionMode.TrackMove)
                return String.Format("I will follow the nearest {0} sign and hold my steel.", focus);

            return BuildStartLine(companion, state);
        }

        private static string BuildCivilianMoveHoldLine(BaseHire companion, AIGMCompanionTrackingMode mode, Mobile target)
        {
            if (mode == AIGMCompanionTrackingMode.Players)
                return "I can mark the nearest player trail, but I will not close on them or raise steel.";

            return "I can find the nearest townsfolk, but I will not hunt them.";
        }

        private static string BuildArrivedLine(BaseHire companion, AIGMCompanionTrackingMode mode, Mobile target)
        {
            return String.Format("I have reached the nearest {0} sign: {1}. I will hold and watch.", DescribeResultCategory(mode), SafeSpeech(DescribeMobile(target), "the mark"));
        }

        private static string BuildMoveLine(BaseHire companion, AIGMCompanionTrackingMode mode, Mobile target)
        {
            return String.Format("I have the {0} sign. Moving toward {1}, no steel drawn.", DescribeResultCategory(mode), SafeSpeech(DescribeMobile(target), "the mark"));
        }

        private static string BuildHuntPursuitLine(BaseHire companion, AIGMCompanionTrackingMode mode, Mobile target)
        {
            string prey = mode == AIGMCompanionTrackingMode.Animals ? "animal" : "monster";
            return String.Format("I have the {0} trail. Closing on {1}.", prey, SafeSpeech(DescribeMobile(target), "the mark"));
        }

        private static string BuildMovementBlockedLine(BaseHire companion, Mobile target)
        {
            return String.Format("The trail is marked, but my path to {0} is blocked. I will keep watch.", SafeSpeech(DescribeMobile(target), "the mark"));
        }

        private static string BuildEngageLine(BaseHire companion, AIGMCompanionTrackingMode mode, Mobile target)
        {
            string prey = mode == AIGMCompanionTrackingMode.Animals ? "animal" : "monster";
            return String.Format("I engage the nearest {0}: {1}.", prey, SafeSpeech(DescribeMobile(target), "the mark"));
        }

        private static string BuildStopLine(BaseHire companion)
        {
            switch (GetCompanionId(companion))
            {
                case "dakeyras":
                    return "I will lift my eyes from the trail. No false sign.";
                case "danyal":
                    return "The watch eases. I will keep my eyes open without reading the ground.";
                case "dardalion":
                    return "The tracking watch is ended. I remain ready.";
                default:
                    return "I will stop tracking and keep watch.";
            }
        }

        private static string BuildInactiveStatusLine(BaseHire companion, AIGMCompanionTrackingState state)
        {
            double tracking = state != null && state.SkillValue > 0.0 ? state.SkillValue : AIGMCompanionSkillReadiness.GetSkillValue(companion, SkillName.Tracking);
            string tier = !String.IsNullOrWhiteSpace(state != null ? state.SkillTier : null) ? state.SkillTier : AIGMCompanionSkillReadiness.BuildTier(tracking);
            string capability = String.Format("My Tracking is {0:0.0}, {1}", tracking, tier);
            switch (GetCompanionId(companion))
            {
                case "dakeyras":
                    return "I am not reading a trail right now. " + capability + ".";
                case "danyal":
                    return "No active trail watch. " + capability + ".";
                case "dardalion":
                    return "No tracking watch is set. " + capability + ".";
                default:
                    return "No tracking watch is active. " + capability + ".";
            }
        }

        private static string BuildActiveStatusLine(BaseHire companion, AIGMCompanionTrackingState state)
        {
            string nearest = BuildNearestTrailSummary(state);
            string age = BuildSweepAge(state != null ? state.LastScanUtc : DateTime.MinValue);
            if (String.IsNullOrWhiteSpace(nearest) || nearest.Equals("none", StringComparison.OrdinalIgnoreCase))
                nearest = "no clear trail";

            return String.Format("Tracking {0}; last sweep {1}. {2}.", DescribeMode(state.Mode), age, NaturalizeNearestSentence(nearest));
        }

        private static string BuildAllSignsLine(BaseHire companion, string summary)
        {
            return "I read the nearby signs: " + Shorten(summary, 180) + ".";
        }

        private static string BuildNoTrailLine(BaseHire companion, AIGMCompanionTrackingMode mode)
        {
            string trail = DescribeTrailFocus(mode);
            switch (GetCompanionId(companion))
            {
                case "dakeyras":
                    return String.Format("No clean {0} shows near us. I will keep reading the ground.", trail);
                case "danyal":
                    return String.Format("I do not sense a clear {0} nearby. The watch stays sharp.", trail);
                case "dardalion":
                    return String.Format("No clear {0} nearby. I will mark any change.", trail);
                default:
                    return String.Format("No clear {0} nearby.", trail);
            }
        }

        private static string DescribeTrailFocus(AIGMCompanionTrackingMode mode)
        {
            switch (mode)
            {
                case AIGMCompanionTrackingMode.Animals: return "animal trail";
                case AIGMCompanionTrackingMode.Monsters: return "monster trail";
                case AIGMCompanionTrackingMode.Players: return "player trail";
                case AIGMCompanionTrackingMode.HumanNPCs: return "human trail";
                case AIGMCompanionTrackingMode.NPCs: return "NPC trail";
                case AIGMCompanionTrackingMode.Threats: return "threat sign";
                case AIGMCompanionTrackingMode.All: return "fresh sign";
                default: return "nearby trail";
            }
        }

        private static string BuildTrailLine(BaseHire companion, string category, string name, string direction, string distance, string tile, int count)
        {
            string countText = count <= 1 ? "one " + category + " sign" : count + " " + category + " signs";
            string where = BuildWhereText(direction, distance, tile);
            return String.Format("I find {0}; nearest is {1}{2}.", countText, SafeSpeech(name, "something"), where);
        }

        private static string BuildWhereText(string direction, string distance, string tile)
        {
            List<string> parts = new List<string>();
            if (!String.IsNullOrWhiteSpace(direction))
                parts.Add(direction.Trim());
            if (!String.IsNullOrWhiteSpace(distance))
                parts.Add(distance.Trim());
            if (!String.IsNullOrWhiteSpace(tile))
                parts.Add("near " + tile.Trim());

            return parts.Count == 0 ? String.Empty : ", " + String.Join(", ", parts.ToArray());
        }

        private static string NaturalizeNearestSentence(string nearest)
        {
            if (String.IsNullOrWhiteSpace(nearest) || nearest.Equals("none", StringComparison.OrdinalIgnoreCase))
                return "No clear trail yet";

            return "Nearest sign: " + nearest;
        }

        private static string BuildSweepAge(DateTime lastScanUtc)
        {
            if (lastScanUtc == DateTime.MinValue)
                return "not yet";

            double seconds = Math.Max(0.0, (DateTime.UtcNow - lastScanUtc).TotalSeconds);
            if (seconds < 2.0)
                return "just now";
            if (seconds < 60.0)
                return String.Format("{0:0}s ago", seconds);

            return String.Format("{0:0}m ago", seconds / 60.0);
        }

        private static string GetCompanionId(BaseHire companion)
        {
            IAIGMCompanionActor actor = companion as IAIGMCompanionActor;
            if (actor != null && !String.IsNullOrWhiteSpace(actor.CompanionId))
                return actor.CompanionId.Trim().ToLowerInvariant();

            return companion != null && companion.Name != null ? companion.Name.Trim().ToLowerInvariant() : String.Empty;
        }

        private static string SafeSpeech(string value, string fallback)
        {
            if (String.IsNullOrWhiteSpace(value))
                return fallback;

            return value.Replace("\r", " ").Replace("\n", " ").Trim();
        }

        private static string NaturalizeCandidateSummary(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return "none";

            string text = value.Trim();
            if (text.StartsWith("accepted=", StringComparison.OrdinalIgnoreCase))
                text = text.Substring(9);
            if (String.IsNullOrWhiteSpace(text))
                return "none";

            return text.Replace("@", " at ").Replace(",", "; ");
        }

        private static string Shorten(string value, int max)
        {
            if (String.IsNullOrWhiteSpace(value))
                return String.Empty;

            string text = value.Replace("\r", " ").Replace("\n", " ").Trim();
            if (max > 0 && text.Length > max)
                text = text.Substring(0, max);

            return text;
        }

        private static string DescribeResultCategory(AIGMCompanionTrackingMode mode)
        {
            switch (mode)
            {
                case AIGMCompanionTrackingMode.Animals: return "animal";
                case AIGMCompanionTrackingMode.Monsters: return "monster";
                case AIGMCompanionTrackingMode.Players: return "player";
                case AIGMCompanionTrackingMode.HumanNPCs: return "human NPC";
                case AIGMCompanionTrackingMode.NPCs: return "NPC";
                case AIGMCompanionTrackingMode.Threats: return "threat";
                default: return "sign";
            }
        }

        private static string BuildConfidence(BaseHire companion, AIGMCompanionTrackingMode mode)
        {
            double tracking = AIGMCompanionSkillReadiness.GetSkillValue(companion, SkillName.Tracking);
            double detectHidden = AIGMCompanionSkillReadiness.GetSkillValue(companion, SkillName.DetectHidden);
            if (mode == AIGMCompanionTrackingMode.Players)
                tracking = tracking + (detectHidden * 0.5);

            return AIGMCompanionSkillReadiness.BuildTier(tracking);
        }

        private static string BuildNaturalAllSummary(string categorySummary)
        {
            if (String.IsNullOrWhiteSpace(categorySummary))
                return "animals none; monsters none; NPCs none; human NPCs none; players none";

            string animals = ExtractCategoryValue(categorySummary, "Animals");
            string monsters = ExtractCategoryValue(categorySummary, "Monsters");
            string npcs = ExtractCategoryValue(categorySummary, "NPCs");
            string humanNpcs = ExtractCategoryValue(categorySummary, "HumanNPCs");
            string players = ExtractCategoryValue(categorySummary, "Players");

            return String.Format("animal={0}; monster={1}; npc={2}; human npc={3}; player={4}",
                NormalizeCategoryDisplay(animals),
                NormalizeCategoryDisplay(monsters),
                NormalizeCategoryDisplay(npcs),
                NormalizeCategoryDisplay(humanNpcs),
                NormalizeCategoryDisplay(players));
        }

        private static string ExtractCategoryValue(string summary, string key)
        {
            if (String.IsNullOrWhiteSpace(summary) || String.IsNullOrWhiteSpace(key))
                return "none";

            string[] parts = summary.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i].Trim();
                if (part.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))
                    return part.Substring(key.Length + 1).Trim();
            }

            return "none";
        }

        private static string NormalizeCategoryDisplay(string value)
        {
            if (String.IsNullOrWhiteSpace(value) || value.Equals("none", StringComparison.OrdinalIgnoreCase))
                return "none";

            int at = value.LastIndexOf('@');
            if (at > 0 && at < value.Length - 1)
            {
                string name = value.Substring(0, at).Trim();
                string distance = value.Substring(at + 1).Trim();
                return name + " at " + distance + " tiles";
            }

            return value;
        }

        private static int CountAccepted(string acceptedSummary)
        {
            if (String.IsNullOrWhiteSpace(acceptedSummary) || acceptedSummary.Equals("accepted=none", StringComparison.OrdinalIgnoreCase))
                return 0;

            string payload = acceptedSummary.StartsWith("accepted=", StringComparison.OrdinalIgnoreCase)
                ? acceptedSummary.Substring(9)
                : acceptedSummary;

            if (String.IsNullOrWhiteSpace(payload))
                return 0;

            return payload.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Length;
        }

        private static void ClearLastTarget(AIGMCompanionTrackingState state)
        {
            if (state == null)
                return;

            state.LastKnownTargetDescription = String.Empty;
            state.LastKnownDirectionText = String.Empty;
            state.LastKnownDistanceText = String.Empty;
            state.LastKnownTileText = String.Empty;
        }

        private static string NormalizeSpeech(string speech)
        {
            if (String.IsNullOrWhiteSpace(speech))
                return String.Empty;

            string normalized = speech.Trim().ToLowerInvariant();
            while (normalized.Contains("  "))
                normalized = normalized.Replace("  ", " ");
            return normalized;
        }

        private static string SafeLog(string value)
        {
            if (String.IsNullOrEmpty(value))
                return String.Empty;

            value = value.Replace("\r", " ").Replace("\n", " ");
            if (value.Length > 220)
                value = value.Substring(0, 220) + "...";

            return value;
        }

        private static bool ContainsAny(string value, params string[] needles)
        {
            if (String.IsNullOrWhiteSpace(value) || needles == null)
                return false;

            for (int i = 0; i < needles.Length; i++)
            {
                if (!String.IsNullOrWhiteSpace(needles[i]) && value.Contains(needles[i]))
                    return true;
            }

            return false;
        }
    }
}
