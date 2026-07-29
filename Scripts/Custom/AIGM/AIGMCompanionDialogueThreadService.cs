using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Server;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionDialogueThreadService
    {
        private enum ConversationState
        {
            Idle,
            DirectReply,
            CompanionReplyPending,
            GroupDiscussionActive,
            PausedForCombat,
            AfterActionCommentary,
            QuietSuppressed
        }

        private sealed class DialogueTurn
        {
            public string SpeakerId;
            public string TargetId;
            public string Text;
            public DateTime TimeUtc;
        }

        private sealed class DialogueThread
        {
            public int OwnerSerial;
            public string ThreadId;
            public string Topic;
            public string Initiator;
            public string LastSpeaker;
            public string LastTarget;
            public string IntendedNextSpeaker;
            public List<IAIGMCompanionActor> Participants;
            public List<DialogueTurn> RecentTurns;
            public ConversationState State;
            public DateTime ActiveSince;
            public DateTime LastTurnUtc;
            public DateTime LastScheduleUtc;
            public DateTime CombatPauseStarted;
            public DateTime LastCombatEnded;
            public DateTime LastAfterActionUtc;
            public DateTime TalkFreelyUntilUtc;
            public bool QuietMode;
            public bool PostCombatCommentaryPending;
            public string PausedReason;
            public int TurnCursor;
            public int TurnCount;
            public int MaxBurstTurns;
            public int BurstTurnsRemaining;
            public int OngoingCadenceSeconds;
            public int AfterActionTurnsRemaining;
            public int Generation;
            public bool ContinuationInFlight;
            public string ContinuationRequestId;
            public DateTime ContinuationStartedUtc;
            public bool TalkFreelyRecent;
            public bool Stopped;
        }

        private static readonly ConcurrentDictionary<int, DialogueThread> ThreadsByOwner = new ConcurrentDictionary<int, DialogueThread>();
        private static readonly ConcurrentDictionary<string, string> SuppressedConversationRequestIds = new ConcurrentDictionary<string, string>();
        private static readonly TimeSpan ActiveBurstCadence = TimeSpan.FromSeconds(7.0);
        private static readonly TimeSpan AfterActionCadence = TimeSpan.FromSeconds(5.0);
        private static readonly TimeSpan CombatClearWindow = TimeSpan.FromSeconds(10.0);
        private static readonly TimeSpan QuietResumeWindow = TimeSpan.FromMinutes(8.0);
        private static readonly TimeSpan TalkFreelyWindow = TimeSpan.FromMinutes(8.0);
        private static readonly TimeSpan AfterActionCooldown = TimeSpan.FromSeconds(45.0);
        private const int MaxRecentTurns = 8;
        private const int DefaultBurstTurns = 3;

        public static int ActiveThreadCount
        {
            get { return ThreadsByOwner.Count; }
        }

        public static void TryStart(BaseHire sourceCompanion, Mobile owner, string rawSpeech, AIGMCompanionCommandRouteDecision decision)
        {
            if (sourceCompanion == null || sourceCompanion.Deleted || owner == null || String.IsNullOrWhiteSpace(rawSpeech))
                return;

            TryStartActor(sourceCompanion as IAIGMCompanionActor, owner, rawSpeech, decision);
        }

        public static void TryStartActor(IAIGMCompanionActor sourceActor, Mobile owner, string rawSpeech, AIGMCompanionCommandRouteDecision decision)
        {
            if (sourceActor == null || sourceActor.Shell == null || sourceActor.Shell.Deleted || owner == null || String.IsNullOrWhiteSpace(rawSpeech))
                return;

            if (!ShouldStartThread(rawSpeech, decision))
                return;

            List<IAIGMCompanionActor> participants = AIGMCompanionSpeechBus.GetLinkedCompanionsIncludingSource(sourceActor, owner);
            if (participants == null || participants.Count < 2)
                return;

            SortByPartyRole(participants);

            int ownerSerial = owner.Serial.Value;
            DialogueThread thread = GetOrCreateThread(ownerSerial, sourceActor, rawSpeech, participants, ResolveInitialState(rawSpeech, decision));
            thread.Topic = BuildTopic(rawSpeech);
            thread.Participants = participants;
            thread.Initiator = sourceActor != null ? sourceActor.CompanionId : null;
            thread.IntendedNextSpeaker = ResolveLikelyNextSpeaker(rawSpeech, sourceActor, participants, decision);
            thread.MaxBurstTurns = ResolveMaxBurstTurns(rawSpeech, decision);
            thread.BurstTurnsRemaining = thread.MaxBurstTurns;
            thread.OngoingCadenceSeconds = ResolveCadenceSeconds(thread.ThreadId);
            thread.QuietMode = AIGMCompanionDialogueControlService.IsQuiet(owner);
            thread.Stopped = false;

            if (thread.QuietMode)
            {
                thread.State = ConversationState.QuietSuppressed;
                SetParticipantMode(participants, AIGMCompanionMode.DialogueQuietMode, "conversation_quiet");
                AIGMExecutionLog.Write("AIGM_CONVERSATION_QUIET owner={0} thread={1} reason=start_suppressed topic=\"{2}\"", ownerSerial, SafeLog(thread.ThreadId), SafeLog(thread.Topic));
                return;
            }

            AIGMExecutionLog.Write("AIGM_CONVERSATION_THREAD_START owner={0} thread={1} state={2} topic=\"{3}\" participants=\"{4}\" next={5}",
                ownerSerial,
                SafeLog(thread.ThreadId),
                thread.State,
                SafeLog(thread.Topic),
                FormatParticipants(participants),
                SafeLog(thread.IntendedNextSpeaker));
            SetParticipantMode(participants, AIGMCompanionMode.DialogueGroupBanter, "conversation_thread_start");
        }

        public static void StopForOwner(Mobile owner, string reason)
        {
            if (owner == null)
                return;

            DialogueThread thread;
            if (!ThreadsByOwner.TryRemove(owner.Serial.Value, out thread) || thread == null)
                return;

            thread.Stopped = true;
            thread.Generation++;
            MarkInFlightSuppressed(thread, reason);
            thread.ContinuationInFlight = false;
            thread.ContinuationRequestId = null;
            AIGMExecutionLog.Write("AIGM_CONVERSATION_END owner={0} thread={1} reason={2} topic=\"{3}\"", owner.Serial.Value, SafeLog(thread.ThreadId), SafeLog(reason), SafeLog(thread.Topic));
            ClearParticipantMode(thread.Participants, AIGMCompanionMode.DialogueGroupBanter, "conversation_" + SafeLog(reason));
        }

        public static void StopForCommand(Mobile owner, AIGMCompanionCommandRouteDecision decision)
        {
            if (owner == null || decision == null || !decision.IsCompanionCommand)
                return;

            if (decision.VerbKind == AIGMCompanionCommandVerbKind.TrackingStatus || decision.VerbKind == AIGMCompanionCommandVerbKind.ReportThreats || decision.VerbKind == AIGMCompanionCommandVerbKind.Scan)
                return;

            if (decision.VerbKind == AIGMCompanionCommandVerbKind.GreetCompanion)
                return;

            StopForOwner(owner, "command_interrupt");
        }

        public static void MarkQuiet(Mobile owner, string reason)
        {
            if (owner == null)
                return;

            DialogueThread thread;
            if (!ThreadsByOwner.TryGetValue(owner.Serial.Value, out thread) || thread == null)
                return;

            thread.QuietMode = true;
            thread.State = ConversationState.QuietSuppressed;
            thread.PausedReason = reason;
            thread.Generation++;
            MarkInFlightSuppressed(thread, reason);
            thread.ContinuationInFlight = false;
            thread.ContinuationRequestId = null;
            AIGMExecutionLog.Write("AIGM_CONVERSATION_QUIET owner={0} thread={1} reason={2} topic=\"{3}\"", owner.Serial.Value, SafeLog(thread.ThreadId), SafeLog(reason), SafeLog(thread.Topic));
            SetParticipantMode(thread.Participants, AIGMCompanionMode.DialogueQuietMode, "conversation_quiet");
        }

        public static void ResumeFromQuiet(IAIGMCompanionActor companion, Mobile owner, string speech)
        {
            if (owner == null)
                return;

            DialogueThread thread;
            if (!ThreadsByOwner.TryGetValue(owner.Serial.Value, out thread) || thread == null)
                return;

            thread.QuietMode = false;
            thread.TalkFreelyRecent = true;
            thread.TalkFreelyUntilUtc = DateTime.UtcNow + TalkFreelyWindow;
            thread.PausedReason = null;
            thread.Generation++;
            thread.ContinuationInFlight = false;
            thread.ContinuationRequestId = null;
            ClearParticipantMode(thread.Participants, AIGMCompanionMode.DialogueQuietMode, "conversation_resume");

            if ((DateTime.UtcNow - thread.LastTurnUtc) > QuietResumeWindow)
            {
                StopForOwner(owner, "resume_stale");
                return;
            }

            thread.State = HasGroupTopic(thread) ? ConversationState.GroupDiscussionActive : ConversationState.CompanionReplyPending;
            thread.BurstTurnsRemaining = Math.Max(1, Math.Min(2, thread.MaxBurstTurns));
            thread.IntendedNextSpeaker = SelectNextSpeakerId(thread, thread.LastSpeaker);
            AIGMExecutionLog.Write("AIGM_CONVERSATION_RESUME_AFTER_COMBAT owner={0} thread={1} reason=quiet_cleared next={2}", owner.Serial.Value, SafeLog(thread.ThreadId), SafeLog(thread.IntendedNextSpeaker));
            ScheduleNext(owner, thread, TimeSpan.FromSeconds(2.0));
        }

        public static void RecordVisibleTurn(AIGMCompanionSpeechRequest request, string visibleReply)
        {
            if (request == null || request.Companion == null || request.Companion.Shell == null || request.OwnerSerial == Serial.MinusOne)
                return;

            Mobile owner = ResolveOwner(request);
            if (owner == null)
                return;

            DialogueThread thread;
            if (!ThreadsByOwner.TryGetValue(owner.Serial.Value, out thread) || thread == null)
            {
                thread = TryCreateFromCompanionDialogue(request, owner);
                if (thread == null)
                    return;
            }

            if (thread.Stopped)
                return;

            if (thread.State == ConversationState.GroupDiscussionActive
                && !String.Equals(request.DialogueMode, "owner_relay_dialogue", StringComparison.OrdinalIgnoreCase))
                return;

            if (thread.ContinuationInFlight && String.Equals(thread.ContinuationRequestId, request.RequestId, StringComparison.OrdinalIgnoreCase))
            {
                thread.ContinuationInFlight = false;
                thread.ContinuationRequestId = null;
            }

            string speakerId = request.CompanionId;
            string targetId = !String.IsNullOrWhiteSpace(request.DialogueTargetCompanionId) ? request.DialogueTargetCompanionId : request.AddressedCompanionId;
            AddRecentTurn(thread, speakerId, targetId, visibleReply);
            thread.LastSpeaker = speakerId;
            thread.LastTarget = targetId;
            thread.LastTurnUtc = DateTime.UtcNow;
            thread.TurnCount++;

            if (!String.IsNullOrWhiteSpace(targetId) && !String.Equals(targetId, speakerId, StringComparison.OrdinalIgnoreCase))
                thread.IntendedNextSpeaker = targetId;
            else
                thread.IntendedNextSpeaker = SelectNextSpeakerId(thread, speakerId);

            AIGMExecutionLog.Write("AIGM_CONVERSATION_THREAD_TURN owner={0} thread={1} state={2} speaker={3} target={4} turns={5} next={6}",
                owner.Serial.Value,
                SafeLog(thread.ThreadId),
                thread.State,
                SafeLog(speakerId),
                SafeLog(targetId),
                thread.TurnCount,
                SafeLog(thread.IntendedNextSpeaker));

            if (request.IsContextOnly || thread.QuietMode || AIGMCompanionDialogueControlService.IsQuiet(owner))
                return;

            if (IsOwnerTurnAwaitingAutomaticCompanionReply(request, speakerId, targetId))
                return;

            if (ShouldEndAfterVisibleTurn(thread))
            {
                StopForOwner(owner, "natural_close");
                return;
            }

            TimeSpan delay = thread.BurstTurnsRemaining > 0 ? ActiveBurstCadence : TimeSpan.FromSeconds(thread.OngoingCadenceSeconds);
            ScheduleNext(owner, thread, delay);
        }

        public static bool ShouldSuppressVisibleTurn(AIGMCompanionSpeechRequest request, out string reason)
        {
            reason = null;

            if (request == null || request.OwnerSerial == Serial.MinusOne)
                return false;

            if (!String.Equals(request.DialogueMode, "companion_dialogue", StringComparison.OrdinalIgnoreCase)
                && !String.Equals(request.DialogueMode, "owner_relay_dialogue", StringComparison.OrdinalIgnoreCase))
                return false;

            string suppressedReason;
            if (SuppressedConversationRequestIds.TryRemove(request.RequestId, out suppressedReason))
            {
                reason = String.IsNullOrWhiteSpace(suppressedReason) ? "stale_generation" : suppressedReason;
                return true;
            }

            Mobile owner = ResolveOwner(request);
            if (owner == null)
                return false;

            DialogueThread thread;
            if (!ThreadsByOwner.TryGetValue(owner.Serial.Value, out thread) || thread == null)
                return false;

            if (thread.ContinuationInFlight && String.Equals(thread.ContinuationRequestId, request.RequestId, StringComparison.OrdinalIgnoreCase))
            {
                thread.ContinuationInFlight = false;
                thread.ContinuationRequestId = null;
            }

            if (thread.Stopped)
            {
                reason = "thread_stopped";
                return true;
            }

            if (thread.QuietMode || thread.State == ConversationState.QuietSuppressed || AIGMCompanionDialogueControlService.IsQuiet(owner))
            {
                reason = "quiet_generation";
                return true;
            }

            return false;
        }

        public static void RecordSuppressedBridge(AIGMCompanionSpeechRequest request, string source, string reason)
        {
            if (request == null || request.OwnerSerial == Serial.MinusOne)
                return;

            Mobile owner = ResolveOwner(request);
            if (owner == null)
                return;

            DialogueThread thread;
            if (!ThreadsByOwner.TryGetValue(owner.Serial.Value, out thread) || thread == null)
                return;

            if (thread.ContinuationInFlight && String.Equals(thread.ContinuationRequestId, request.RequestId, StringComparison.OrdinalIgnoreCase))
            {
                thread.ContinuationInFlight = false;
                thread.ContinuationRequestId = null;
            }

            AIGMExecutionLog.Write("AIGM_CONVERSATION_SUPPRESS_BRIDGE_UNHEALTHY owner={0} thread={1} source={2} reason={3} state={4}",
                owner.Serial.Value,
                SafeLog(thread.ThreadId),
                SafeLog(source),
                SafeLog(reason),
                thread.State);

            if (IsBridgeFailureReason(source, reason))
                StopForOwner(owner, "bridge_unhealthy");
        }

        public static string BuildStatusSummary(Mobile owner)
        {
            if (owner == null)
                return "no owner";

            DialogueThread thread;
            if (!ThreadsByOwner.TryGetValue(owner.Serial.Value, out thread) || thread == null)
                return "activeThread=none bridge={" + AIGMBridgeClient.GetChatbotStatusSummary() + "}";

            return String.Format("activeThread={0} generation={1} inFlight={2} inFlightRequest={3} topic=\"{4}\" participants={5} lastSpeaker={6} intendedNext={7} state={8} quiet={9} talkFreely={10} pausedReason={11} recentTurns={12} bridge={{{13}}}",
                SafeLog(thread.ThreadId),
                thread.Generation,
                thread.ContinuationInFlight,
                String.IsNullOrWhiteSpace(thread.ContinuationRequestId) ? "none" : SafeLog(thread.ContinuationRequestId),
                SafeLog(thread.Topic),
                FormatParticipants(thread.Participants),
                String.IsNullOrWhiteSpace(thread.LastSpeaker) ? "none" : thread.LastSpeaker,
                String.IsNullOrWhiteSpace(thread.IntendedNextSpeaker) ? "none" : thread.IntendedNextSpeaker,
                thread.State,
                thread.QuietMode || AIGMCompanionDialogueControlService.IsQuiet(owner),
                IsTalkFreelyActive(thread),
                String.IsNullOrWhiteSpace(thread.PausedReason) ? "none" : SafeLog(thread.PausedReason),
                thread.RecentTurns != null ? thread.RecentTurns.Count : 0,
                AIGMBridgeClient.GetChatbotStatusSummary());
        }

        private static DialogueThread GetOrCreateThread(int ownerSerial, IAIGMCompanionActor sourceActor, string rawSpeech, List<IAIGMCompanionActor> participants, ConversationState state)
        {
            DialogueThread existing;
            if (ThreadsByOwner.TryGetValue(ownerSerial, out existing) && existing != null && !existing.Stopped)
            {
                existing.State = state;
                existing.ActiveSince = existing.ActiveSince == DateTime.MinValue ? DateTime.UtcNow : existing.ActiveSince;
                return existing;
            }

            DialogueThread thread = new DialogueThread();
            thread.OwnerSerial = ownerSerial;
            thread.ThreadId = Guid.NewGuid().ToString("N").Substring(0, 8);
            thread.Topic = BuildTopic(rawSpeech);
            thread.Initiator = sourceActor != null ? sourceActor.CompanionId : null;
            thread.Participants = participants;
            thread.RecentTurns = new List<DialogueTurn>();
            thread.State = state;
            thread.ActiveSince = DateTime.UtcNow;
            thread.LastTurnUtc = DateTime.UtcNow;
            thread.MaxBurstTurns = DefaultBurstTurns;
            thread.BurstTurnsRemaining = DefaultBurstTurns;
            thread.OngoingCadenceSeconds = ResolveCadenceSeconds(thread.ThreadId);
            thread.Generation = 1;
            ThreadsByOwner[ownerSerial] = thread;
            return thread;
        }

        private static DialogueThread TryCreateFromCompanionDialogue(AIGMCompanionSpeechRequest request, Mobile owner)
        {
            if (request == null || !String.Equals(request.DialogueMode, "companion_dialogue", StringComparison.OrdinalIgnoreCase) || request.HopCount != 0)
                return null;

            List<IAIGMCompanionActor> participants = AIGMCompanionSpeechBus.GetLinkedCompanionsIncludingSource(request.Companion, owner);
            if (request.Companion.Shell == null || participants == null || participants.Count < 2)
                return null;

            SortByPartyRole(participants);
            DialogueThread thread = GetOrCreateThread(owner.Serial.Value, request.Companion, request.RawSpeech, participants, ConversationState.CompanionReplyPending);
            thread.IntendedNextSpeaker = request.OriginCompanionId;
            thread.MaxBurstTurns = 2;
            thread.BurstTurnsRemaining = 1;
            AIGMExecutionLog.Write("AIGM_CONVERSATION_THREAD_START owner={0} thread={1} state={2} topic=\"{3}\" participants=\"{4}\" next={5}",
                owner.Serial.Value,
                SafeLog(thread.ThreadId),
                thread.State,
                SafeLog(thread.Topic),
                FormatParticipants(participants),
                SafeLog(thread.IntendedNextSpeaker));
            return thread;
        }

        private static bool ShouldStartThread(string rawSpeech, AIGMCompanionCommandRouteDecision decision)
        {
            if (decision != null && decision.IsCompanionCommand)
                return false;

            string speech = Normalize(rawSpeech);
            if (String.IsNullOrWhiteSpace(speech))
                return false;

            if (IsQuietOrResumeSpeech(speech))
                return false;

            bool groupAddressed = IsGroupAddressed(speech);
            bool namedDialogue = decision != null && decision.RouteKind == AIGMCompanionCommandRouteKind.NamedCompanion;
            bool ownerDirected = AIGMCompanionTurnCoordinator.ShouldRelayOwnerSpeechAsCompanionDialogue(rawSpeech);
            bool relationship = speech.Contains("relationship") || speech.Contains("trust") || speech.Contains("think of") || speech.Contains("agree");
            bool directTalk = speech.Contains(" talk to ") || speech.Contains(" greet ") || speech.Contains(" say hello to ");

            if (groupAddressed && (speech.Contains("discuss") || speech.Contains("talk") || speech.Contains("what do you think") || speech.Contains("what do you make") || speech.Contains("should ") || speech.Contains("whether ")))
                return true;

            if (ownerDirected)
                return true;

            if (namedDialogue && relationship)
                return true;

            return namedDialogue && (directTalk || speech.Contains("tell me") || speech.Contains("tell ") || speech.Contains("ask ") || speech.Contains("answer ") || speech.Contains("respond") || speech.Contains("what do you think"));
        }

        private static ConversationState ResolveInitialState(string rawSpeech, AIGMCompanionCommandRouteDecision decision)
        {
            string speech = Normalize(rawSpeech);
            if (IsGroupAddressed(speech))
                return ConversationState.GroupDiscussionActive;

            if (AIGMCompanionTurnCoordinator.ShouldRelayOwnerSpeechAsCompanionDialogue(rawSpeech))
                return ConversationState.CompanionReplyPending;

            if (speech.Contains(" talk to ") || speech.Contains(" greet ") || speech.Contains(" say hello to "))
                return ConversationState.CompanionReplyPending;

            return ConversationState.DirectReply;
        }

        private static int ResolveMaxBurstTurns(string rawSpeech, AIGMCompanionCommandRouteDecision decision)
        {
            string speech = Normalize(rawSpeech);
            if (IsGroupAddressed(speech))
                return 3;

            if (AIGMCompanionTurnCoordinator.ShouldRelayOwnerSpeechAsCompanionDialogue(rawSpeech)
                || speech.Contains(" talk to ")
                || speech.Contains(" greet ")
                || speech.Contains(" say hello to "))
                return 3;

            return 2;
        }

        private static void ScheduleNext(Mobile owner, DialogueThread thread, TimeSpan delay)
        {
            if (owner == null || thread == null || thread.Stopped)
                return;

            if (thread.QuietMode || AIGMCompanionDialogueControlService.IsQuiet(owner))
                return;

            if (thread.ContinuationInFlight)
            {
                AIGMExecutionLog.Write("AIGM_CONVERSATION_SUPPRESS_BRIDGE_UNHEALTHY owner={0} thread={1} source=queue reason=thread_in_flight requestId={2}",
                    owner.Serial.Value,
                    SafeLog(thread.ThreadId),
                    SafeLog(thread.ContinuationRequestId));
                return;
            }

            DateTime now = DateTime.UtcNow;
            if ((now - thread.LastScheduleUtc) < TimeSpan.FromSeconds(1.0))
                return;

            thread.LastScheduleUtc = now;
            int scheduledGeneration = thread.Generation;
            Timer.DelayCall(delay, delegate
            {
                RunTurn(owner, thread, scheduledGeneration);
            });
        }

        private static void RunTurn(Mobile owner, DialogueThread thread, int scheduledGeneration)
        {
            if (owner == null || owner.Deleted || thread == null || thread.Stopped)
                return;

            DialogueThread active;
            if (!ThreadsByOwner.TryGetValue(owner.Serial.Value, out active) || active != thread)
                return;

            if (thread.Generation != scheduledGeneration)
            {
                AIGMExecutionLog.Write("AIGM_CONVERSATION_SUPPRESS_BRIDGE_UNHEALTHY owner={0} thread={1} source=scheduler reason=stale_generation scheduled={2} current={3}",
                    owner.Serial.Value,
                    SafeLog(thread.ThreadId),
                    scheduledGeneration,
                    thread.Generation);
                return;
            }

            if (thread.ContinuationInFlight)
            {
                AIGMExecutionLog.Write("AIGM_CONVERSATION_SUPPRESS_BRIDGE_UNHEALTHY owner={0} thread={1} source=queue reason=thread_in_flight requestId={2}",
                    owner.Serial.Value,
                    SafeLog(thread.ThreadId),
                    SafeLog(thread.ContinuationRequestId));
                return;
            }

            if (thread.QuietMode || AIGMCompanionDialogueControlService.IsQuiet(owner))
            {
                thread.State = ConversationState.QuietSuppressed;
                AIGMExecutionLog.Write("AIGM_CONVERSATION_QUIET owner={0} thread={1} reason=scheduled_turn topic=\"{2}\"", owner.Serial.Value, SafeLog(thread.ThreadId), SafeLog(thread.Topic));
                return;
            }

            if (IsPartyInCombat(owner, thread.Participants))
            {
                PauseForCombat(owner, thread);
                return;
            }

            if (thread.State == ConversationState.PausedForCombat)
            {
                if (thread.LastCombatEnded == DateTime.MinValue)
                    thread.LastCombatEnded = DateTime.UtcNow;

                if ((DateTime.UtcNow - thread.LastCombatEnded) < CombatClearWindow)
                {
                    ScheduleNext(owner, thread, TimeSpan.FromSeconds(4.0));
                    return;
                }

                BeginAfterAction(owner, thread);
                return;
            }

            IAIGMCompanionActor participant = SelectParticipant(thread, thread.IntendedNextSpeaker);
            if (participant == null || participant.Shell == null || participant.Shell.Deleted)
            {
                StopForOwner(owner, "participant_unavailable");
                return;
            }

            string prompt;
            if (thread.State == ConversationState.AfterActionCommentary)
                prompt = BuildAfterActionPrompt(participant, thread);
            else
                prompt = BuildContinuationPrompt(participant, thread);

            AIGMExecutionLog.Write("AIGM_CONVERSATION_NEXT_SPEAKER owner={0} thread={1} companion={2} state={3} burstRemaining={4} topic=\"{5}\"",
                owner.Serial.Value,
                SafeLog(thread.ThreadId),
                SafeLog(participant.CompanionId),
                thread.State,
                thread.BurstTurnsRemaining,
                SafeLog(thread.Topic));
            AIGMCompanionModeService.SetMode(participant.Shell, AIGMCompanionMode.DialogueGroupBanter, "conversation_thread_turn");

            string rejection;
            AIGMCompanionSpeechRequest request = new AIGMCompanionSpeechRequest(participant, owner, prompt, "owner_relay_dialogue", null, null, 0, false, true, false);
            if (!AIGMCompanionSpeechQueue.TryEnqueue(request, true, out rejection))
            {
                AIGMExecutionLog.Write("AIGM_CONVERSATION_SUPPRESS_BRIDGE_UNHEALTHY owner={0} thread={1} source=queue reason={2}", owner.Serial.Value, SafeLog(thread.ThreadId), SafeLog(rejection));
                return;
            }

            thread.ContinuationInFlight = true;
            thread.ContinuationRequestId = request.RequestId;
            thread.ContinuationStartedUtc = DateTime.UtcNow;

            if (thread.State == ConversationState.AfterActionCommentary)
                thread.AfterActionTurnsRemaining--;
            else if (thread.BurstTurnsRemaining > 0)
                thread.BurstTurnsRemaining--;
        }

        private static void PauseForCombat(Mobile owner, DialogueThread thread)
        {
            if (thread == null)
                return;

            if (thread.State != ConversationState.PausedForCombat)
            {
                thread.State = ConversationState.PausedForCombat;
                thread.PausedReason = "combat";
                thread.CombatPauseStarted = DateTime.UtcNow;
                thread.LastCombatEnded = DateTime.MinValue;
                thread.PostCombatCommentaryPending = true;
                AIGMExecutionLog.Write("AIGM_CONVERSATION_PAUSE_COMBAT owner={0} thread={1} topic=\"{2}\"", owner != null ? owner.Serial.Value : 0, SafeLog(thread.ThreadId), SafeLog(thread.Topic));
            }

            ScheduleNext(owner, thread, TimeSpan.FromSeconds(5.0));
        }

        private static void BeginAfterAction(Mobile owner, DialogueThread thread)
        {
            if (thread == null)
                return;

            DateTime now = DateTime.UtcNow;
            if (!thread.PostCombatCommentaryPending || (now - thread.LastAfterActionUtc) < AfterActionCooldown)
            {
                thread.State = ConversationState.GroupDiscussionActive;
                ScheduleNext(owner, thread, TimeSpan.FromSeconds(thread.OngoingCadenceSeconds));
                return;
            }

            thread.State = ConversationState.AfterActionCommentary;
            thread.PausedReason = null;
            thread.PostCombatCommentaryPending = false;
            thread.LastAfterActionUtc = now;
            thread.AfterActionTurnsRemaining = Math.Min(3, thread.Participants != null ? thread.Participants.Count : 1);
            AIGMExecutionLog.Write("AIGM_CONVERSATION_RESUME_AFTER_COMBAT owner={0} thread={1} afterActionTurns={2} topic=\"{3}\"", owner.Serial.Value, SafeLog(thread.ThreadId), thread.AfterActionTurnsRemaining, SafeLog(thread.Topic));
            ScheduleNext(owner, thread, TimeSpan.FromSeconds(1.0));
        }

        private static bool ShouldEndAfterVisibleTurn(DialogueThread thread)
        {
            if (thread == null)
                return true;

            if (thread.State == ConversationState.AfterActionCommentary)
            {
                if (thread.AfterActionTurnsRemaining <= 0)
                {
                    thread.State = HasGroupTopic(thread) ? ConversationState.GroupDiscussionActive : ConversationState.DirectReply;
                    thread.BurstTurnsRemaining = 1;
                }

                return false;
            }

            if (IsTalkFreelyActive(thread))
                return false;

            if (thread.BurstTurnsRemaining > 0)
                return false;

            return true;
        }

        private static bool IsOwnerTurnAwaitingAutomaticCompanionReply(AIGMCompanionSpeechRequest request, string speakerId, string targetId)
        {
            if (request == null)
                return false;

            if (!String.Equals(request.DialogueMode, "owner_or_world_speech", StringComparison.OrdinalIgnoreCase))
                return false;

            return request.AllowRemoteRelay
                && request.HopCount == 0
                && !String.IsNullOrWhiteSpace(targetId)
                && !String.Equals(targetId, speakerId, StringComparison.OrdinalIgnoreCase);
        }

        private static void MarkInFlightSuppressed(DialogueThread thread, string reason)
        {
            if (thread == null || String.IsNullOrWhiteSpace(thread.ContinuationRequestId))
                return;

            SuppressedConversationRequestIds[thread.ContinuationRequestId] = String.IsNullOrWhiteSpace(reason) ? "stale_generation" : reason;
        }

        private static bool IsTalkFreelyActive(DialogueThread thread)
        {
            return thread != null && thread.TalkFreelyRecent && thread.TalkFreelyUntilUtc > DateTime.UtcNow;
        }

        private static bool IsBridgeFailureReason(string source, string reason)
        {
            string normalizedSource = (source ?? String.Empty).Trim().ToLowerInvariant();
            string normalizedReason = (reason ?? String.Empty).Trim().ToLowerInvariant();

            return normalizedSource == "none"
                || normalizedSource == "exception"
                || normalizedSource.Contains("fallback")
                || normalizedSource.Contains("timeout")
                || normalizedReason == "null_response"
                || normalizedReason.Contains("timeout")
                || normalizedReason.Contains("sanitizer_empty");
        }

        private static IAIGMCompanionActor SelectParticipant(DialogueThread thread, string preferredId)
        {
            if (thread == null || thread.Participants == null || thread.Participants.Count == 0)
                return null;

            if (!String.IsNullOrWhiteSpace(preferredId))
            {
                for (int i = 0; i < thread.Participants.Count; i++)
                {
                    IAIGMCompanionActor actor = thread.Participants[i];
                    if (actor != null && String.Equals(actor.CompanionId, preferredId, StringComparison.OrdinalIgnoreCase) && !String.Equals(actor.CompanionId, thread.LastSpeaker, StringComparison.OrdinalIgnoreCase))
                        return actor;
                }
            }

            for (int i = 0; i < thread.Participants.Count; i++)
            {
                IAIGMCompanionActor actor = thread.Participants[thread.TurnCursor % thread.Participants.Count];
                thread.TurnCursor++;
                if (actor != null && !String.Equals(actor.CompanionId, thread.LastSpeaker, StringComparison.OrdinalIgnoreCase))
                    return actor;
            }

            return thread.Participants[0];
        }

        private static string SelectNextSpeakerId(DialogueThread thread, string lastSpeaker)
        {
            IAIGMCompanionActor actor = SelectParticipant(thread, null);
            return actor != null ? actor.CompanionId : null;
        }

        private static string BuildContinuationPrompt(IAIGMCompanionActor participant, DialogueThread thread)
        {
            string name = participant != null ? participant.CompanionDisplayName : "Companion";
            string topic = thread != null ? thread.Topic : "the current situation";
            string recent = FormatRecentTurns(thread);
            string target = thread != null && !String.IsNullOrWhiteSpace(thread.LastSpeaker) ? thread.LastSpeaker : "the party";
            return String.Format("{0}, continue the live companion conversation about {1}. Answer or challenge {2} naturally if that fits. Keep it one short in-character turn, no narration, no status text. Recent turns: {3}", name, topic, target, recent);
        }

        private static string BuildAfterActionPrompt(IAIGMCompanionActor participant, DialogueThread thread)
        {
            string id = participant != null ? participant.CompanionId : String.Empty;
            string focus = "the party's survival and whether to resume the earlier topic";
            if (String.Equals(id, "dakeyras", StringComparison.OrdinalIgnoreCase))
                focus = "threats, positioning, and whether the road is clear";
            else if (String.Equals(id, "danyal", StringComparison.OrdinalIgnoreCase))
                focus = "wounds, supplies, burden, and morale";
            else if (String.Equals(id, "dardalion", StringComparison.OrdinalIgnoreCase))
                focus = "cost, healing, poison, mercy, and survival";

            string topic = thread != null ? thread.Topic : "the interrupted conversation";
            return String.Format("{0}, give one short after-action companion line after the fight. Focus on {1}. Then either reconnect to or close the earlier topic: {2}. No narration, no status text.", participant != null ? participant.CompanionDisplayName : "Companion", focus, topic);
        }

        private static bool IsPartyInCombat(Mobile owner, List<IAIGMCompanionActor> participants)
        {
            if (IsCombatActive(owner))
                return true;

            if (participants != null)
            {
                for (int i = 0; i < participants.Count; i++)
                {
                    Mobile mobile = participants[i] != null ? participants[i].Shell : null;
                    if (IsCombatActive(mobile))
                        return true;
                }
            }

            if (owner == null || owner.Map == null)
                return false;

            foreach (Mobile mobile in World.Mobiles.Values)
            {
                if (mobile == null || mobile.Deleted || !mobile.Alive || mobile.Map != owner.Map || !owner.InRange(mobile, 12))
                    continue;

                Mobile combatant = mobile.Combatant as Mobile;
                if (combatant == owner || IsParticipant(combatant, participants))
                    return true;
            }

            return false;
        }

        private static bool IsCombatActive(Mobile mobile)
        {
            if (mobile == null || mobile.Deleted || !mobile.Alive)
                return false;

            Mobile combatant = mobile.Combatant as Mobile;
            return (combatant != null && !combatant.Deleted && combatant.Alive) || mobile.Warmode;
        }

        private static bool IsParticipant(Mobile mobile, List<IAIGMCompanionActor> participants)
        {
            if (mobile == null || participants == null)
                return false;

            for (int i = 0; i < participants.Count; i++)
            {
                if (participants[i] != null && participants[i].Shell == mobile)
                    return true;
            }

            return false;
        }

        private static void AddRecentTurn(DialogueThread thread, string speakerId, string targetId, string visibleReply)
        {
            if (thread.RecentTurns == null)
                thread.RecentTurns = new List<DialogueTurn>();

            DialogueTurn turn = new DialogueTurn();
            turn.SpeakerId = speakerId;
            turn.TargetId = targetId;
            turn.Text = visibleReply;
            turn.TimeUtc = DateTime.UtcNow;
            thread.RecentTurns.Add(turn);

            while (thread.RecentTurns.Count > MaxRecentTurns)
                thread.RecentTurns.RemoveAt(0);
        }

        private static string FormatRecentTurns(DialogueThread thread)
        {
            if (thread == null || thread.RecentTurns == null || thread.RecentTurns.Count == 0)
                return "none";

            List<string> parts = new List<string>();
            int start = Math.Max(0, thread.RecentTurns.Count - 4);
            for (int i = start; i < thread.RecentTurns.Count; i++)
            {
                DialogueTurn turn = thread.RecentTurns[i];
                if (turn == null)
                    continue;

                string text = SafeLog(turn.Text);
                if (text.Length > 70)
                    text = text.Substring(0, 70);
                parts.Add(String.Format("{0}->{1}: {2}", SafeLog(turn.SpeakerId), String.IsNullOrWhiteSpace(turn.TargetId) ? "party" : SafeLog(turn.TargetId), text));
            }

            return parts.Count == 0 ? "none" : String.Join(" | ", parts.ToArray());
        }

        private static Mobile ResolveOwner(AIGMCompanionSpeechRequest request)
        {
            if (request == null || request.Companion == null)
                return null;

            return AIGMCompanionSpeechBus.ResolveOwner(request.Companion);
        }

        private static string ResolveLikelyNextSpeaker(string rawSpeech, IAIGMCompanionActor sourceActor, List<IAIGMCompanionActor> participants, AIGMCompanionCommandRouteDecision decision)
        {
            string target = AIGMCompanionTurnCoordinator.ResolveOwnerDirectedDialogueTarget(rawSpeech);
            if (!String.IsNullOrWhiteSpace(target))
                return target;

            List<string> addressed = AIGMCompanionCommandBoundary.GetAddressedCompanionIds(rawSpeech);
            if (addressed != null && addressed.Count > 0)
            {
                for (int i = 0; i < addressed.Count; i++)
                {
                    if (sourceActor == null || !String.Equals(addressed[i], sourceActor.CompanionId, StringComparison.OrdinalIgnoreCase))
                        return addressed[i];
                }
            }

            if (decision != null && !String.IsNullOrWhiteSpace(decision.CompanionKey) && (sourceActor == null || !String.Equals(decision.CompanionKey, sourceActor.CompanionId, StringComparison.OrdinalIgnoreCase)))
                return decision.CompanionKey;

            return SelectNextSpeakerId(new DialogueThread { Participants = participants, LastSpeaker = sourceActor != null ? sourceActor.CompanionId : null }, sourceActor != null ? sourceActor.CompanionId : null);
        }

        private static bool HasGroupTopic(DialogueThread thread)
        {
            return thread != null && thread.Participants != null && thread.Participants.Count > 2;
        }

        private static bool IsGroupAddressed(string speech)
        {
            if (String.IsNullOrWhiteSpace(speech))
                return false;

            return speech.Contains("companions")
                || speech.Contains("all companions")
                || speech.Contains("all of you")
                || speech.Contains("you all")
                || speech.Contains("you three")
                || speech.Contains("three of you")
                || speech.Contains("everyone")
                || speech.Contains("everybody")
                || speech.StartsWith("party ", StringComparison.Ordinal);
        }

        private static bool IsQuietOrResumeSpeech(string speech)
        {
            return speech == "quiet"
                || speech == "shh"
                || speech == "shhh"
                || speech == "stop talking"
                || speech == "enough talk"
                || speech == "silence"
                || speech == "hold your tongues"
                || speech == "no chatter"
                || speech == "talk freely"
                || speech == "speak freely"
                || speech == "you can talk"
                || speech == "resume talking"
                || speech == "continue"
                || speech == "what were you saying";
        }

        private static int ResolveCadenceSeconds(string threadId)
        {
            int seed = 0;
            if (!String.IsNullOrWhiteSpace(threadId))
            {
                for (int i = 0; i < threadId.Length; i++)
                    seed += threadId[i];
            }

            return 22 + (seed % 17);
        }

        private static string BuildTopic(string rawSpeech)
        {
            string speech = Normalize(rawSpeech);
            if (String.IsNullOrWhiteSpace(speech))
                return "the current situation";

            string[] removals = new[] { "companions", "all of you", "you all", "you three", "everyone", "please", "discuss", "talk about", "talk through", "tell me about", "tell us about" };
            for (int i = 0; i < removals.Length; i++)
                speech = speech.Replace(removals[i], " ");

            while (speech.Contains("  "))
                speech = speech.Replace("  ", " ");

            speech = speech.Trim();
            if (speech.Length == 0)
                return "the current situation";

            if (speech.Length > 90)
                speech = speech.Substring(0, 90).Trim();

            return speech;
        }

        private static void SetParticipantMode(List<IAIGMCompanionActor> participants, AIGMCompanionMode mode, string reason)
        {
            if (participants == null)
                return;

            for (int i = 0; i < participants.Count; i++)
            {
                if (participants[i] != null)
                    AIGMCompanionModeService.SetMode(participants[i].Shell, mode, reason);
            }
        }

        private static void ClearParticipantMode(List<IAIGMCompanionActor> participants, AIGMCompanionMode mode, string reason)
        {
            if (participants == null)
                return;

            for (int i = 0; i < participants.Count; i++)
            {
                if (participants[i] != null)
                    AIGMCompanionModeService.ClearIfMode(participants[i].Shell, mode, reason);
            }
        }

        private static void SortByPartyRole(List<IAIGMCompanionActor> participants)
        {
            if (participants == null)
                return;

            participants.Sort((a, b) => GetPartyOrder(a).CompareTo(GetPartyOrder(b)));
        }

        private static int GetPartyOrder(IAIGMCompanionActor actor)
        {
            if (actor == null)
                return 99;

            switch ((actor.CompanionId ?? String.Empty).Trim().ToLowerInvariant())
            {
                case "dakeyras":
                    return 0;
                case "danyal":
                    return 1;
                case "dardalion":
                    return 2;
                default:
                    return 50;
            }
        }

        private static string FormatParticipants(List<IAIGMCompanionActor> participants)
        {
            if (participants == null || participants.Count == 0)
                return "none";

            List<string> names = new List<string>();
            for (int i = 0; i < participants.Count; i++)
            {
                if (participants[i] != null && !String.IsNullOrWhiteSpace(participants[i].CompanionId))
                    names.Add(participants[i].CompanionId);
            }

            return names.Count == 0 ? "none" : String.Join(",", names.ToArray());
        }

        private static string Normalize(string rawSpeech)
        {
            if (String.IsNullOrWhiteSpace(rawSpeech))
                return String.Empty;

            string normalized = rawSpeech.Trim().ToLowerInvariant();
            normalized = normalized.Replace(",", " ").Replace(".", " ").Replace("!", " ").Replace("?", " ").Replace(";", " ").Replace(":", " ");
            while (normalized.Contains("  "))
                normalized = normalized.Replace("  ", " ");
            return normalized.Trim();
        }

        private static string SafeLog(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return String.Empty;

            string text = value.Replace("\r", " ").Replace("\n", " ").Replace("\"", "'");
            return text.Length > 180 ? text.Substring(0, 180) : text;
        }
    }
}
