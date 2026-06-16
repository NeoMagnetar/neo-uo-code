using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionTurnCoordinator
    {
        private static readonly ConcurrentDictionary<int, AIGMCompanionPartySpeechContext> LastContextByOwner = new ConcurrentDictionary<int, AIGMCompanionPartySpeechContext>();
        private static readonly TimeSpan CompanionDialogueCooldown = TimeSpan.FromSeconds(6.0);
        private static readonly ConcurrentDictionary<string, DateTime> NextCompanionDialogueUtc = new ConcurrentDictionary<string, DateTime>();

        public static bool ShouldCompanionTakeVisibleTurn(IAIGMCompanionActor companion, Mobile speaker, string rawSpeech)
        {
            AIGMCompanionPartySpeechContext context = BuildContext(companion, speaker, rawSpeech, "owner_or_world_speech", null, 0);
            return context != null && context.IsSelected(companion != null ? companion.CompanionId : null);
        }

        public static bool ShouldCompanionTakeVisibleTurn(IAIGMCompanionActor companion, Mobile speaker, string rawSpeech, string dialogueMode, string originCompanionId, int hopCount)
        {
            AIGMCompanionPartySpeechContext context = BuildContext(companion, speaker, rawSpeech, dialogueMode, originCompanionId, hopCount);
            return context != null && context.IsSelected(companion != null ? companion.CompanionId : null);
        }

        public static bool ShouldCompanionTakeVisibleTurn(IAIGMCompanionActor companion, Mobile speaker, string rawSpeech, string dialogueMode, string originCompanionId, int hopCount, string dialogueTargetCompanionId)
        {
            AIGMCompanionPartySpeechContext context = BuildContext(companion, speaker, rawSpeech, dialogueMode, originCompanionId, hopCount, dialogueTargetCompanionId);
            return context != null && context.IsSelected(companion != null ? companion.CompanionId : null);
        }

        public static AIGMCompanionPartySpeechContext BuildContext(IAIGMCompanionActor companion, Mobile speaker, string rawSpeech, string dialogueMode, string originCompanionId, int hopCount)
        {
            return BuildContext(companion, speaker, rawSpeech, dialogueMode, originCompanionId, hopCount, null);
        }

        public static AIGMCompanionPartySpeechContext BuildContext(IAIGMCompanionActor companion, Mobile speaker, string rawSpeech, string dialogueMode, string originCompanionId, int hopCount, string dialogueTargetCompanionId)
        {
            AIGMCompanionPartySpeechContext context = new AIGMCompanionPartySpeechContext();
            context.OwnerSpeaker = speaker != null ? speaker.Name ?? speaker.GetType().Name : "unknown";
            context.RawMessage = rawSpeech ?? String.Empty;
            context.CompanionDialogueChainDepth = hopCount;
            context.DialogueTargetCompanionId = dialogueTargetCompanionId ?? String.Empty;

            BaseHire self = companion != null ? companion.Shell as BaseHire : null;
            Mobile owner = ResolveOwner(self, speaker);
            List<IAIGMCompanionActor> listeners = GetListenerSet(companion, owner);
            for (int i = 0; i < listeners.Count; i++)
                context.ListenerCompanions.Add(listeners[i].CompanionId);

            AIGMCompanionCommandRouteDecision route = AIGMCompanionCommandBoundary.Classify(rawSpeech);
            AIGMCompanionIntent intent;
            bool parsedIntent = AIGMCompanionIntentParser.TryParse(self, speaker, rawSpeech, out intent);
            context.ParsedIntent = parsedIntent && intent != null ? intent.Kind : (route != null ? route.Reason : "none");

            List<string> addressed = AIGMCompanionCommandBoundary.GetAddressedCompanionIds(rawSpeech);
            bool groupAddressed = IsGroupAddressed(rawSpeech) || (addressed != null && addressed.Count > 1);
            context.GroupAddressed = groupAddressed;
            if (route != null && route.RouteKind == AIGMCompanionCommandRouteKind.NamedCompanion)
                context.AddressedCompanionId = route.CompanionKey ?? String.Empty;
            else if (IsOwnerDirectedCompanionDialogue(rawSpeech) && addressed != null && addressed.Count >= 2)
                context.AddressedCompanionId = addressed[0];
            else if (addressed != null && addressed.Count == 1)
                context.AddressedCompanionId = addressed[0];
            if (String.IsNullOrWhiteSpace(context.DialogueTargetCompanionId))
                context.DialogueTargetCompanionId = ResolveOwnerDirectedDialogueTarget(rawSpeech);

            context.DialogueMode = ResolveDialogueMode(route, rawSpeech, dialogueMode, speaker, groupAddressed);
            context.StateContextSummary = BuildStateContextSummary(listeners);
            context.LastStateSummaryUsed = context.StateContextSummary;

            SelectResponders(context, listeners, route, originCompanionId, speaker);
            context.LastPersonaProfileUsed = BuildSelectedPersonaSummary(listeners, context.SelectedResponders);
            Remember(owner, context);
            return context;
        }

        public static AIGMCompanionPartySpeechContext GetLastContext(Mobile owner)
        {
            if (owner == null)
                return null;

            AIGMCompanionPartySpeechContext context;
            return LastContextByOwner.TryGetValue(owner.Serial.Value, out context) ? context : null;
        }

        public static string BuildLastContextDump(Mobile owner)
        {
            AIGMCompanionPartySpeechContext context = GetLastContext(owner);
            if (context == null)
                return "No Phase58D party speech context has been recorded yet.";

            return String.Format(
                "LastOwnerSpeech={0}; ListenerSet={1}; AddressedCompanion={2}; DialogueTargetCompanion={3}; GroupAddressed={4}; DialogueMode={5}; SelectedResponders={6}; SuppressedResponders={7}; SuppressionReasons={8}; LastPersonaProfileUsed={9}; LastStateSummaryUsed={10}; CompanionReplyChainDepth={11}; EchoSuppressionReason={12}; ParsedIntent={13}; TurnCoordinatorDecision={14}; UMGContext=active",
                context.RawMessage,
                context.FormatListenerSet(),
                String.IsNullOrWhiteSpace(context.AddressedCompanionId) ? "none" : context.AddressedCompanionId,
                String.IsNullOrWhiteSpace(context.DialogueTargetCompanionId) ? "none" : context.DialogueTargetCompanionId,
                context.GroupAddressed,
                context.DialogueMode,
                context.FormatSelectedResponders(),
                context.FormatSuppressedResponders(),
                context.FormatSuppressedReasons(),
                String.IsNullOrWhiteSpace(context.LastPersonaProfileUsed) ? "none" : context.LastPersonaProfileUsed,
                context.LastStateSummaryUsed,
                context.CompanionDialogueChainDepth,
                String.IsNullOrWhiteSpace(context.EchoSuppressionReason) ? "none" : context.EchoSuppressionReason,
                context.ParsedIntent,
                context.TurnCoordinatorDecision);
        }

        private static void SelectResponders(AIGMCompanionPartySpeechContext context, List<IAIGMCompanionActor> listeners, AIGMCompanionCommandRouteDecision route, string originCompanionId, Mobile speaker)
        {
            if (context == null || listeners == null)
                return;

            if (context.DialogueMode == AIGMCompanionDialogueMode.DirectNamedCommand)
            {
                SelectNamed(context, listeners, context.AddressedCompanionId);
                context.TurnCoordinatorDecision = "direct_named_command_one_primary";
                return;
            }

            if (context.DialogueMode == AIGMCompanionDialogueMode.CompanionToCompanion)
            {
                SelectCompanionDialogueResponder(context, listeners, originCompanionId, speaker);
                return;
            }

            if (context.DialogueMode == AIGMCompanionDialogueMode.OwnerDirectedCompanionDialogue)
            {
                SelectOwnerDirectedDialogueStarter(context, listeners, context.AddressedCompanionId);
                return;
            }

            if (context.DialogueMode == AIGMCompanionDialogueMode.GroupConversation ||
                context.DialogueMode == AIGMCompanionDialogueMode.GroupCommand ||
                context.DialogueMode == AIGMCompanionDialogueMode.StateCommentary ||
                context.DialogueMode == AIGMCompanionDialogueMode.SystemStatus)
            {
                int max = context.GroupAddressed || IsPlayerPartyBroadcast(context, speaker) ? 3 : 1;
                SelectGroup(context, listeners, max);
                context.TurnCoordinatorDecision = max > 1
                    ? (context.GroupAddressed ? "group_bounded_multi_responder" : "party_broadcast_multi_responder")
                    : "single_default_responder";
                return;
            }

            SelectGroup(context, listeners, 1);
            context.TurnCoordinatorDecision = "fallback_single_responder";
        }

        private static bool IsPlayerPartyBroadcast(AIGMCompanionPartySpeechContext context, Mobile speaker)
        {
            if (context == null || speaker is BaseHire)
                return false;

            if (!String.IsNullOrWhiteSpace(context.AddressedCompanionId))
                return false;

            return context.DialogueMode == AIGMCompanionDialogueMode.GroupConversation
                || context.DialogueMode == AIGMCompanionDialogueMode.StateCommentary
                || context.DialogueMode == AIGMCompanionDialogueMode.SystemStatus;
        }

        private static void SelectNamed(AIGMCompanionPartySpeechContext context, List<IAIGMCompanionActor> listeners, string addressedCompanionId)
        {
            bool selected = false;
            for (int i = 0; i < listeners.Count; i++)
            {
                IAIGMCompanionActor actor = listeners[i];
                if (!selected && String.Equals(actor.CompanionId, addressedCompanionId, StringComparison.OrdinalIgnoreCase))
                {
                    context.SelectedResponders.Add(actor.CompanionId);
                    selected = true;
                }
                else
                {
                    context.Suppress(actor.CompanionId, "direct_named_context_only");
                }
            }
        }

        private static void SelectGroup(AIGMCompanionPartySpeechContext context, List<IAIGMCompanionActor> listeners, int maxResponders)
        {
            SortByPartyRole(listeners);
            int selected = 0;
            for (int i = 0; i < listeners.Count; i++)
            {
                IAIGMCompanionActor actor = listeners[i];
                if (selected < maxResponders)
                {
                    context.SelectedResponders.Add(actor.CompanionId);
                    selected++;
                }
                else
                {
                    context.Suppress(actor.CompanionId, "responder_limit");
                }
            }
        }

        private static void SelectOwnerDirectedDialogueStarter(AIGMCompanionPartySpeechContext context, List<IAIGMCompanionActor> listeners, string starterCompanionId)
        {
            bool selected = false;
            for (int i = 0; i < listeners.Count; i++)
            {
                IAIGMCompanionActor actor = listeners[i];
                if (!selected && String.Equals(actor.CompanionId, starterCompanionId, StringComparison.OrdinalIgnoreCase))
                {
                    context.SelectedResponders.Add(actor.CompanionId);
                    selected = true;
                }
                else if (String.Equals(actor.CompanionId, context.DialogueTargetCompanionId, StringComparison.OrdinalIgnoreCase))
                {
                    context.Suppress(actor.CompanionId, "awaiting_companion_dialogue_reply");
                }
                else
                {
                    context.Suppress(actor.CompanionId, "owner_directed_dialogue_not_initial");
                }
            }

            context.TurnCoordinatorDecision = selected ? "owner_directed_companion_dialogue_starter" : "PHASE58D-LIVE-ROUTING-BLOCKED";
        }

        private static void SelectCompanionDialogueResponder(AIGMCompanionPartySpeechContext context, List<IAIGMCompanionActor> listeners, string originCompanionId, Mobile speaker)
        {
            if (context.CompanionDialogueChainDepth > 0)
            {
                for (int i = 0; i < listeners.Count; i++)
                    context.Suppress(listeners[i].CompanionId, "PHASE58D-ECHO-BLOCKED chain_depth");

                context.TurnCoordinatorDecision = "PHASE58D-ECHO-BLOCKED";
                context.EchoSuppressionReason = "chain_depth";
                return;
            }

            SortByPartyRole(listeners);
            DateTime now = DateTime.UtcNow;
            bool selected = false;
            string speakerId = ResolveSpeakerCompanionId(speaker, originCompanionId);
            string preferredTargetId = context.DialogueTargetCompanionId;

            if (!String.IsNullOrWhiteSpace(preferredTargetId))
            {
                for (int i = 0; i < listeners.Count; i++)
                {
                    IAIGMCompanionActor actor = listeners[i];
                    if (String.Equals(actor.CompanionId, speakerId, StringComparison.OrdinalIgnoreCase))
                    {
                        context.Suppress(actor.CompanionId, "PHASE58D-ECHO-BLOCKED own_generated_speech");
                        context.EchoSuppressionReason = "own_generated_speech";
                        continue;
                    }

                    if (String.Equals(actor.CompanionId, preferredTargetId, StringComparison.OrdinalIgnoreCase))
                    {
                        string cooldownKey = speakerId + ">" + actor.CompanionId;
                        DateTime targetNext;
                        if (NextCompanionDialogueUtc.TryGetValue(cooldownKey, out targetNext) && now < targetNext)
                        {
                            context.Suppress(actor.CompanionId, "companion_dialogue_cooldown");
                            continue;
                        }

                        context.SelectedResponders.Add(actor.CompanionId);
                        NextCompanionDialogueUtc[cooldownKey] = now + CompanionDialogueCooldown;
                        selected = true;
                    }
                    else
                    {
                        context.Suppress(actor.CompanionId, "companion_dialogue_target_only");
                    }
                }

                context.TurnCoordinatorDecision = selected ? "companion_dialogue_targeted_followup" : "PHASE58D-ECHO-BLOCKED";
                if (!selected && String.IsNullOrWhiteSpace(context.EchoSuppressionReason))
                    context.EchoSuppressionReason = "target_unavailable_or_cooldown";
                return;
            }

            for (int i = 0; i < listeners.Count; i++)
            {
                IAIGMCompanionActor actor = listeners[i];
                if (String.Equals(actor.CompanionId, speakerId, StringComparison.OrdinalIgnoreCase))
                {
                    context.Suppress(actor.CompanionId, "PHASE58D-ECHO-BLOCKED own_generated_speech");
                    context.EchoSuppressionReason = "own_generated_speech";
                    continue;
                }

                string cooldownKey = speakerId + ">" + actor.CompanionId;
                DateTime next;
                if (NextCompanionDialogueUtc.TryGetValue(cooldownKey, out next) && now < next)
                {
                    context.Suppress(actor.CompanionId, "companion_dialogue_cooldown");
                    continue;
                }

                if (!selected)
                {
                    context.SelectedResponders.Add(actor.CompanionId);
                    NextCompanionDialogueUtc[cooldownKey] = now + CompanionDialogueCooldown;
                    selected = true;
                }
                else
                {
                    context.Suppress(actor.CompanionId, "companion_dialogue_one_followup_limit");
                }
            }

            context.TurnCoordinatorDecision = selected ? "companion_dialogue_one_followup" : "PHASE58D-ECHO-BLOCKED";
            if (!selected && String.IsNullOrWhiteSpace(context.EchoSuppressionReason))
                context.EchoSuppressionReason = "no_followup_selected";
        }

        private static AIGMCompanionDialogueMode ResolveDialogueMode(AIGMCompanionCommandRouteDecision route, string rawSpeech, string dialogueMode, Mobile speaker, bool groupAddressed)
        {
            if (String.Equals(dialogueMode, "companion_dialogue", StringComparison.OrdinalIgnoreCase) || speaker is BaseHire)
                return AIGMCompanionDialogueMode.CompanionToCompanion;

            if (route != null && route.RouteKind == AIGMCompanionCommandRouteKind.NamedCompanion)
                return AIGMCompanionDialogueMode.DirectNamedCommand;

            if (IsOwnerDirectedCompanionDialogue(rawSpeech))
                return AIGMCompanionDialogueMode.OwnerDirectedCompanionDialogue;

            List<string> addressed = AIGMCompanionCommandBoundary.GetAddressedCompanionIds(rawSpeech);
            if (!groupAddressed && addressed != null && addressed.Count == 1)
                return AIGMCompanionDialogueMode.DirectNamedCommand;

            string normalized = Normalize(rawSpeech);
            if (IsStatusSpeech(normalized))
                return AIGMCompanionDialogueMode.SystemStatus;

            if (IsStateQuestion(normalized))
                return AIGMCompanionDialogueMode.StateCommentary;

            if (route != null && route.RouteKind == AIGMCompanionCommandRouteKind.SharedCompanion)
                return groupAddressed ? AIGMCompanionDialogueMode.GroupCommand : AIGMCompanionDialogueMode.StateCommentary;

            return groupAddressed ? AIGMCompanionDialogueMode.GroupConversation : AIGMCompanionDialogueMode.StateCommentary;
        }

        public static bool ShouldRelayOwnerSpeechAsCompanionDialogue(string rawSpeech)
        {
            return IsOwnerDirectedCompanionDialogue(rawSpeech) && !String.IsNullOrWhiteSpace(ResolveOwnerDirectedDialogueTarget(rawSpeech));
        }

        public static string ResolveOwnerDirectedDialogueTarget(string rawSpeech)
        {
            List<string> addressed = AIGMCompanionCommandBoundary.GetAddressedCompanionIds(rawSpeech);
            if (addressed == null || addressed.Count < 2)
                return String.Empty;

            return addressed[1];
        }

        private static bool IsOwnerDirectedCompanionDialogue(string rawSpeech)
        {
            string speech = Normalize(rawSpeech);
            if (String.IsNullOrWhiteSpace(speech))
                return false;

            List<string> addressed = AIGMCompanionCommandBoundary.GetAddressedCompanionIds(rawSpeech);
            if (addressed == null || addressed.Count < 2)
                return false;

            return speech.Contains(" tell ")
                || speech.StartsWith("tell ", StringComparison.Ordinal)
                || speech.Contains(" respond")
                || speech.Contains(" answer")
                || speech.Contains(" ask ");
        }

        private static bool IsGroupAddressed(string rawSpeech)
        {
            string speech = Normalize(rawSpeech);
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
                || speech.Contains("can you all hear me")
                || speech.Contains("do you all hear me")
                || speech.Contains("can all of you hear me")
                || speech.Contains("can everyone hear me")
                || speech.Contains("can everybody hear me")
                || speech.Contains("are you all hearing me")
                || speech.Contains("can you both hear me")
                || speech.Contains("can you all hear")
                || speech.Contains("all report")
                || speech.Contains("all stay")
                || speech.StartsWith("all ", StringComparison.Ordinal)
                || speech.StartsWith("party ", StringComparison.Ordinal);
        }

        private static bool IsStatusSpeech(string normalized)
        {
            return normalized.Contains("status") || normalized.Contains("report");
        }

        private static bool IsStateQuestion(string normalized)
        {
            return normalized.Contains("what do you see")
                || normalized.Contains("what do you think")
                || normalized.Contains("stay sharp")
                || normalized.Contains("wounds")
                || normalized.Contains("supplies")
                || normalized.Contains("tracking")
                || normalized.Contains("hunt")
                || normalized.Contains("guard")
                || normalized.Contains("posture");
        }

        private static List<IAIGMCompanionActor> GetListenerSet(IAIGMCompanionActor companion, Mobile owner)
        {
            if (companion == null || companion.Shell == null || owner == null)
                return new List<IAIGMCompanionActor>();

            List<IAIGMCompanionActor> listeners = AIGMCompanionSpeechBus.GetLinkedCompanionsIncludingSource(companion, owner);
            SortByPartyRole(listeners);
            return listeners;
        }

        private static Mobile ResolveOwner(BaseHire self, Mobile speaker)
        {
            if (self != null && self.GetOwner() != null)
                return self.GetOwner();

            BaseHire speakerCompanion = speaker as BaseHire;
            if (speakerCompanion != null && speakerCompanion.GetOwner() != null)
                return speakerCompanion.GetOwner();

            return speaker;
        }

        private static string BuildStateContextSummary(List<IAIGMCompanionActor> listeners)
        {
            if (listeners == null || listeners.Count == 0)
                return "no_companions";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < listeners.Count; i++)
            {
                BaseHire hire = listeners[i].Shell as BaseHire;
                if (hire == null)
                    continue;

                if (sb.Length > 0)
                    sb.Append(" | ");

                AIGMCompanionTrackingState tracking = AIGMCompanionTrackingService.GetState(hire);
                AIGMCompanionExecutionState hunt = AIGMCompanionExecutionSpine.GetState(hire);
                sb.Append(listeners[i].CompanionId);
                sb.Append(": tracking=");
                sb.Append(tracking != null && tracking.IsActive ? tracking.Mode.ToString() : "inactive");
                sb.Append(", hunt=");
                sb.Append(hunt != null && hunt.HuntActive ? hunt.Phase.ToString() : "inactive");
                sb.Append(", guard=");
                sb.Append(hire.ControlOrder == OrderType.Guard ? "guard" : hire.ControlOrder.ToString());
                sb.Append(", health=");
                sb.Append(hire.Hits);
                sb.Append("/");
                sb.Append(hire.HitsMax);
                sb.Append(", bandages=");
                sb.Append(CountBandages(hire));
                sb.Append(", movement=available_if_phase58c_present");
            }

            return sb.ToString();
        }

        private static int CountBandages(BaseHire hire)
        {
            if (hire == null || hire.Backpack == null)
                return 0;

            Item item = hire.Backpack.FindItemByType(typeof(Bandage));
            Bandage bandage = item as Bandage;
            return bandage != null ? bandage.Amount : 0;
        }

        private static string ResolveSpeakerCompanionId(Mobile speaker, string originCompanionId)
        {
            if (!String.IsNullOrWhiteSpace(originCompanionId))
                return originCompanionId;

            IAIGMCompanionActor actor = speaker as IAIGMCompanionActor;
            return actor != null ? actor.CompanionId : String.Empty;
        }

        private static string BuildSelectedPersonaSummary(List<IAIGMCompanionActor> listeners, List<string> selectedResponders)
        {
            if (listeners == null || selectedResponders == null || selectedResponders.Count == 0)
                return String.Empty;

            List<string> parts = new List<string>();
            for (int i = 0; i < selectedResponders.Count; i++)
            {
                string selected = selectedResponders[i];
                for (int j = 0; j < listeners.Count; j++)
                {
                    IAIGMCompanionActor actor = listeners[j];
                    if (actor == null || !String.Equals(actor.CompanionId, selected, StringComparison.OrdinalIgnoreCase))
                        continue;

                    AIGMCompanionPersonaContext persona = AIGMCompanionProfileLibrary.BuildPersonaContext(actor);
                    parts.Add(actor.CompanionId + ":" + actor.CompanionProfileKey + ":" + (persona != null ? persona.RoleSummary : actor.CompanionRole));
                    break;
                }
            }

            return parts.Count == 0 ? String.Empty : String.Join(" | ", parts.ToArray());
        }

        private static void Remember(Mobile owner, AIGMCompanionPartySpeechContext context)
        {
            if (owner != null && context != null)
                LastContextByOwner[owner.Serial.Value] = context;
        }

        private static void SortByPartyRole(List<IAIGMCompanionActor> listeners)
        {
            if (listeners == null)
                return;

            listeners.Sort((a, b) => GetPartyOrder(a).CompareTo(GetPartyOrder(b)));
        }

        private static int GetPartyOrder(IAIGMCompanionActor actor)
        {
            if (actor == null)
                return 99;

            switch ((actor.CompanionId ?? String.Empty).ToLowerInvariant())
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
    }
}
