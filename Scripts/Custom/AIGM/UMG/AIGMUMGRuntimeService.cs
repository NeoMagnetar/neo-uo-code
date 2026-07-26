using System;
using System.Collections.Generic;
using Server.Custom.AIGM.Tasks;
using Server.Mobiles;

namespace Server.Custom.AIGM.UMG
{
    public static class AIGMUMGRuntimeService
    {
        public static AIGMUMGDecisionTrace CompileDryRun(Mobile actor)
        {
            if (actor == null)
                return null;

            string actorId = ResolveActorId(actor);
            AIGMUMGSleeve sleeve = AIGMUMGRepository.GetSleeve(actorId);
            AIGMUMGDecisionTrace trace = AIGMUMGCompiler.Compile(actor, sleeve, true);
            AIGMUMGDecisionTraceService.Record(actor, trace);
            return trace;
        }

        public static string BuildStatus(Mobile actor)
        {
            if (actor == null)
                return "UMG actor not found.";

            AIGMCapabilitySnapshot snapshot = AIGMCapabilityRegistry.CreateSnapshot(actor);
            AIGMUMGSleeve sleeve = AIGMUMGRepository.GetSleeve(snapshot.ActorId);
            if (sleeve == null)
                return String.Format("{0}: no UMG sleeve found.", snapshot.ActorName);

            List<AIGMUMGBlock> blocks = AIGMUMGCompiler.FlattenOrderedBlocks(sleeve);
            AIGMCapabilityValidationResult aggregate = ValidateActiveBlocks(actor, sleeve);
            return String.Format(
                "{0}: sleeve={1} v{2}; schema={3}; autonomy={4}; stacks={5}; activeBlocks={6}; capability={7}; validation={8}",
                snapshot.ActorName,
                sleeve.SleeveId,
                sleeve.Version,
                sleeve.SchemaVersion,
                sleeve.AutonomyMode,
                sleeve.NeoStacks.Count,
                blocks.Count,
                AIGMCapabilityRegistry.BuildCompactSummary(snapshot),
                aggregate.BuildSummary());
        }

        public static AIGMCapabilityValidationResult ValidateActiveBlocks(Mobile actor, AIGMUMGSleeve sleeve)
        {
            AIGMCapabilitySnapshot snapshot = AIGMCapabilityRegistry.CreateSnapshot(actor);
            AIGMCapabilityValidationResult aggregate = new AIGMCapabilityValidationResult();
            List<AIGMUMGBlock> blocks = AIGMUMGCompiler.FlattenOrderedBlocks(sleeve);
            for (int i = 0; i < blocks.Count; i++)
            {
                AIGMCapabilityValidationResult result = AIGMCapabilityRegistry.Validate(snapshot, blocks[i].CapabilityRequirements);
                if (!result.IsValid)
                {
                    aggregate.IsValid = false;
                    for (int f = 0; f < result.Failed.Count; f++)
                        aggregate.Failed.Add(blocks[i].BlockId + ":" + result.Failed[f]);
                }
                else
                {
                    aggregate.Passed.Add(blocks[i].BlockId);
                }
            }

            return aggregate;
        }

        public static string BuildBlockList(Mobile actor)
        {
            if (actor == null)
                return "UMG actor not found.";

            string actorId = ResolveActorId(actor);
            AIGMUMGSleeve sleeve = AIGMUMGRepository.GetSleeve(actorId);
            if (sleeve == null)
                return actor.Name + ": no sleeve.";

            List<AIGMUMGBlock> blocks = AIGMUMGCompiler.FlattenOrderedBlocks(sleeve);
            if (blocks.Count == 0)
                return actor.Name + ": no active UMG blocks.";

            List<string> parts = new List<string>();
            for (int i = 0; i < blocks.Count && i < 12; i++)
                parts.Add(String.Format("{0}:{1}/{2}/p{3}", blocks[i].BlockId, blocks[i].MoltType, blocks[i].BlockState, blocks[i].PriorityOrder));

            if (blocks.Count > 12)
                parts.Add("+" + (blocks.Count - 12) + " more");

            return actor.Name + ": " + String.Join(" | ", parts.ToArray());
        }

        public static string BuildWhy(Mobile actor)
        {
            if (actor == null)
                return "UMG actor not found.";

            AIGMUMGDecisionTrace trace = AIGMUMGDecisionTraceService.GetLatest(actor);
            if (trace == null)
                trace = CompileDryRun(actor);

            return trace != null ? trace.BuildWhySummary() : "No decision trace available.";
        }

        public static string BuildTraceList(Mobile actor)
        {
            if (actor == null)
                return "UMG actor not found.";

            List<AIGMUMGDecisionTrace> traces = AIGMUMGDecisionTraceService.GetRecent(actor, 5);
            if (traces.Count == 0)
            {
                CompileDryRun(actor);
                traces = AIGMUMGDecisionTraceService.GetRecent(actor, 5);
            }

            List<string> parts = new List<string>();
            for (int i = 0; i < traces.Count; i++)
            {
                AIGMUMGDecisionTrace trace = traces[i];
                parts.Add(String.Format("{0}:{1}:{2}", trace.TimestampUtc.ToString("HH:mm:ss"), trace.CorrelationId.Substring(0, 8), trace.SelectedDecision != null ? trace.SelectedDecision.IntentType.ToString() : "None"));
            }

            return actor.Name + " traces: " + String.Join(" | ", parts.ToArray());
        }

        public static string PreviewTemplate(Mobile actor, string templateName)
        {
            if (actor == null)
                return "UMG actor not found.";

            AIGMUMGTemplate template = AIGMUMGRepository.GetTemplate(templateName);
            if (template == null)
                return "UMG template not found.";

            AIGMCapabilitySnapshot snapshot = AIGMCapabilityRegistry.CreateSnapshot(actor);
            List<string> results = new List<string>();
            for (int i = 0; i < template.Blocks.Count; i++)
            {
                AIGMCapabilityValidationResult validation = AIGMCapabilityRegistry.Validate(snapshot, template.Blocks[i].CapabilityRequirements);
                results.Add(template.Blocks[i].Name + "=" + validation.BuildSummary());
            }

            return String.Format("Template preview for {0}: {1}; targetStack={2}; activation requires explicit approve/apply command in later gate.",
                actor.Name,
                String.Join(" | ", results.ToArray()),
                template.TargetNeoStack);
        }

        public static string BuildTemplateList()
        {
            List<AIGMUMGTemplate> templates = AIGMUMGRepository.GetTemplates();
            List<string> names = new List<string>();
            for (int i = 0; i < templates.Count; i++)
                names.Add(templates[i].TemplateId);
            return "UMG templates: " + String.Join(", ", names.ToArray());
        }

        public static string CreateAgentProposal(string actorId, string proposalKind)
        {
            if (String.IsNullOrWhiteSpace(proposalKind) || proposalKind.IndexOf("mana", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                AIGMUMGBlockProposal proposal = AIGMUMGRepository.ProposeEmergencyManaReserve(actorId);
                return String.Format("Proposal {0} saved as Draft for {1}: {2}. Not active.", proposal.ProposalId, proposal.TargetActorOrGroup, proposal.BlockName);
            }

            return "No matching proposal template.";
        }

        public static string ResolveActorId(Mobile actor)
        {
            if (actor == null)
                return String.Empty;

            IAIGMRosterTaskAgent roster = actor as IAIGMRosterTaskAgent;
            if (roster != null && !String.IsNullOrWhiteSpace(roster.RosterCharacterId))
                return roster.RosterCharacterId.Trim().ToLowerInvariant();

            IAIGMCompanionActor companion = actor as IAIGMCompanionActor;
            if (companion != null && !String.IsNullOrWhiteSpace(companion.CompanionId))
                return companion.CompanionId.Trim().ToLowerInvariant();

            return (actor.Name ?? actor.GetType().Name).Trim().ToLowerInvariant().Replace(" ", "_");
        }
    }
}
