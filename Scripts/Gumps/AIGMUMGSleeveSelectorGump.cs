using System;
using System.Collections.Generic;
using Server.Custom.AIGM;
using Server.Custom.AIGM.Inventory;
using Server.Custom.AIGM.UMG;
using Server.Network;

namespace Server.Gumps
{
    public class AIGMUMGSleeveSelectorGump : Gump
    {
        private const int ButtonPreview = 200;
        private const int ButtonWhy = 201;
        private const int ButtonVersions = 202;
        private const int ButtonComposer = 203;
        private const int ButtonBackpack = 204;
        private const int SectionButtonBase = 100;
        private const int ActionTextHue = 0x0481;
        private const int ActionTextShadowHue = 0x0001;

        private static readonly string[] SectionNames =
        {
            "Always-On Spine",
            "Combat",
            "Positioning",
            "Resources",
            "Protection",
            "Capability",
            "Versions"
        };

        private readonly Server.Serial _actorSerial;
        private readonly int _expandedMask;

        public AIGMUMGSleeveSelectorGump(Mobile from, Mobile actor, int expandedMask)
            : base(SkillsGump.GumpOffsetX, SkillsGump.GumpOffsetY)
        {
            _actorSerial = actor != null ? actor.Serial : Server.Serial.MinusOne;
            _expandedMask = NormalizeExpandedMask(expandedMask);

            Closable = true;
            Disposable = true;
            Dragable = true;

            AddPage(0);
            AddBackground(0, 0, 620, 520, SkillsGump.BackGumpID);
            AddImageTiled(10, 10, 600, 500, SkillsGump.OffsetGumpID);

            AddImageTiled(18, 18, 584, SkillsGump.EntryHeight, SkillsGump.HeaderGumpID);
            AddLabelCropped(26, 18, 560, SkillsGump.EntryHeight, SkillsGump.TextHue, Header(actor));

            DrawSummary(actor);
            DrawSections(actor);
            DrawButtons();
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (sender == null || sender.Mobile == null || info == null || info.ButtonID == 0)
                return;

            Mobile actor = _actorSerial.IsValid ? World.FindMobile(_actorSerial) : null;
            AIGMUMGSleeveAccessResult access = AIGMUMGSleeveAccessService.ValidateForGumpButton(sender.Mobile, actor);
            if (!access.Accepted)
            {
                sender.Mobile.SendMessage(38, "{0} correlation={1}", access.Message, access.CorrelationId);
                return;
            }

            int button = info.ButtonID;
            if (button >= SectionButtonBase && button < SectionButtonBase + SectionNames.Length)
            {
                int section = button - SectionButtonBase;
                int requested = 1 << section;
                int mask = (_expandedMask == requested) ? 0 : requested;
                sender.Mobile.SendGump(new AIGMUMGSleeveSelectorGump(sender.Mobile, actor, mask));
                return;
            }

            if (button == ButtonPreview)
            {
                string selector = CurrentAssignmentSelector(actor);
                if (!String.IsNullOrWhiteSpace(selector))
                    sender.Mobile.SendMessage(68, AIGMUMGComposerService.Preview(actor, selector));
                else
                {
                    AIGMUMGDecisionTrace trace = AIGMUMGRuntimeService.CompileDryRun(actor);
                    sender.Mobile.SendMessage(68, trace != null ? trace.BuildWhySummary() : AIGMUMGPhase64C2Invariant.ExecutionStatus);
                }
            }
            else if (button == ButtonWhy)
            {
                sender.Mobile.SendMessage(68, AIGMUMGRuntimeService.BuildWhy(actor));
            }
            else if (button == ButtonVersions)
            {
                sender.Mobile.SendMessage(68, AIGMUMGComposerService.BuildVersionSummary(actor));
            }
            else if (button == ButtonComposer)
            {
                sender.Mobile.CloseGump(typeof(AIGMUMGPanelGump));
                sender.Mobile.SendGump(new AIGMUMGPanelGump(sender.Mobile, actor, 0));
                return;
            }
            else if (button == ButtonBackpack)
            {
                AIGMCompanionInventoryService.OpenBackpack(sender.Mobile, actor);
            }

            sender.Mobile.SendGump(new AIGMUMGSleeveSelectorGump(sender.Mobile, actor, _expandedMask));
        }

        private void DrawSummary(Mobile actor)
        {
            AIGMUMGSleeve sleeve = null;
            List<AIGMUMGAssignment> assignments = new List<AIGMUMGAssignment>();
            List<AIGMUMGVersionRecord> versions = new List<AIGMUMGVersionRecord>();
            AIGMUMGAssignment current = null;
            AIGMCapabilityValidationResult validation = null;
            string actorId = actor != null ? AIGMUMGRuntimeService.ResolveActorId(actor) : String.Empty;

            if (!String.IsNullOrWhiteSpace(actorId))
            {
                sleeve = AIGMUMGRepository.GetSleeve(actorId);
                assignments = AIGMUMGRepository.GetAssignmentsForTarget(actorId);
                versions = AIGMUMGRepository.GetVersionsForTarget(actorId);
                current = SelectCurrentAssignment(assignments);
                validation = AIGMUMGRuntimeService.ValidateActiveBlocks(actor, sleeve);
            }

            int x = 24;
            int y = 50;
            AddSummaryRow(x, y, "Actor", actor != null ? Safe(actor.Name) : "missing", "Serial", AIGMUMGSleeveAccessService.FormatSerial(actor));
            y += 23;
            AddSummaryRow(x, y, "Sleeve", sleeve != null ? sleeve.SleeveId : "missing", "Mode", sleeve != null ? sleeve.AutonomyMode.ToString() : "Manual");
            y += 23;
            AddSummaryRow(x, y, "Assignment", current != null ? current.State.ToString() : "none", "Execution", current != null ? current.ExecutionMode.ToString() : "PreviewOnly");
            y += 23;
            AddSummaryRow(x, y, "PREVIEW ONLY", AIGMUMGPhase64C2Invariant.ExecutionStatus, "Dispatch", AIGMUMGPhase64C2Invariant.TacticalDispatchEnabled ? "enabled" : "disabled");
            y += 23;
            AddSummaryRow(x, y, "Definition", CurrentDefinitionName(current), "Versions", versions.Count.ToString());
            y += 23;
            AddSummaryRow(x, y, "Warnings", CountWarnings(validation).ToString(), "Schema", AIGMUMGSleeve.CurrentSchemaName);
            y += 23;
            AddSummaryRow(x, y, "Backpack", AIGMCompanionInventoryService.BuildCompactStatus(actor), "Marker", AIGMCompanionBackpack.MarkerVersionCurrent.ToString());
        }

        private void DrawSections(Mobile actor)
        {
            int x = 24;
            int y = 225;
            for (int i = 0; i < SectionNames.Length; i++)
            {
                bool expanded = IsExpanded(i);
                AddImageTiled(x, y, 570, SkillsGump.EntryHeight, SkillsGump.EntryGumpID);
                AddButton(x + 4, y + 2, expanded ? 0x15E2 : 0x15E1, expanded ? 0x15E6 : 0x15E5, SectionButtonBase + i, GumpButtonType.Reply, 0);
                AddLabelCropped(x + 30, y, 390, SkillsGump.EntryHeight, SkillsGump.TextHue, SectionNames[i]);
                AddLabelCropped(x + 462, y, 100, SkillsGump.EntryHeight, SkillsGump.TextHue, SectionStatus(actor, i));
                y += SkillsGump.EntryHeight + SkillsGump.OffsetSize;

                if (!expanded)
                    continue;

                List<string> lines = SectionLines(actor, i);
                for (int j = 0; j < lines.Count && j < 5; j++)
                {
                    AddImageTiled(x + 22, y, 548, SkillsGump.EntryHeight, SkillsGump.EntryGumpID);
                    AddLabelCropped(x + 42, y, 506, SkillsGump.EntryHeight, SkillsGump.TextHue, lines[j]);
                    y += SkillsGump.EntryHeight + SkillsGump.OffsetSize;
                }
            }
        }

        private void DrawButtons()
        {
            DrawActionButton(32, 476, ButtonPreview, "Preview");
            DrawActionButton(142, 476, ButtonWhy, "Why");
            DrawActionButton(252, 476, ButtonVersions, "Versions");
            DrawActionButton(362, 476, ButtonComposer, "Composer");
            DrawActionButton(472, 453, ButtonBackpack, "Backpack");
            DrawActionButton(492, 476, 0, "Close");
        }

        private void DrawActionButton(int x, int y, int id, string label)
        {
            AddButton(x, y, 4005, 4007, id, GumpButtonType.Reply, 0);
            AddLabelCropped(x + 33, y + 1, 92, 20, ActionTextShadowHue, label);
            AddLabelCropped(x + 32, y, 92, 20, ActionTextHue, label);
        }

        private static int NormalizeExpandedMask(int mask)
        {
            for (int i = 0; i < SectionNames.Length; i++)
            {
                int bit = 1 << i;
                if ((mask & bit) != 0)
                    return bit;
            }

            return 0;
        }

        private void AddSummaryRow(int x, int y, string leftLabel, string leftValue, string rightLabel, string rightValue)
        {
            AddImageTiled(x, y, 570, SkillsGump.EntryHeight, SkillsGump.EntryGumpID);
            AddLabelCropped(x + 8, y, 110, SkillsGump.EntryHeight, 0x0481, leftLabel);
            AddLabelCropped(x + 118, y, 255, SkillsGump.EntryHeight, SkillsGump.TextHue, leftValue);
            AddLabelCropped(x + 382, y, 78, SkillsGump.EntryHeight, 0x0481, rightLabel);
            AddLabelCropped(x + 460, y, 100, SkillsGump.EntryHeight, SkillsGump.TextHue, rightValue);
        }

        private List<string> SectionLines(Mobile actor, int section)
        {
            List<string> lines = new List<string>();
            if (actor == null)
            {
                lines.Add("No actor.");
                return lines;
            }

            string actorId = AIGMUMGRuntimeService.ResolveActorId(actor);
            AIGMUMGSleeve sleeve = AIGMUMGRepository.GetSleeve(actorId);
            List<AIGMUMGAssignment> assignments = AIGMUMGRepository.GetAssignmentsForTarget(actorId);

            switch (section)
            {
                case 0:
                    AddStackLines(lines, sleeve, AIGMUMGNeoStackKind.Identity, AIGMUMGNeoStackKind.Governance);
                    break;
                case 1:
                    AddStackLines(lines, sleeve, AIGMUMGNeoStackKind.CombatDoctrine);
                    AddAssignmentLines(lines, assignments, AIGMUMGNeoStackKind.CombatDoctrine);
                    break;
                case 2:
                    AddStackLines(lines, sleeve, AIGMUMGNeoStackKind.MovementPositioning, AIGMUMGNeoStackKind.TrackingAwareness);
                    AddAssignmentLines(lines, assignments, AIGMUMGNeoStackKind.MovementPositioning);
                    break;
                case 3:
                    AddStackLines(lines, sleeve, AIGMUMGNeoStackKind.SkillsSpellsResources);
                    AddAssignmentLines(lines, assignments, AIGMUMGNeoStackKind.SkillsSpellsResources);
                    break;
                case 4:
                    AddStackLines(lines, sleeve, AIGMUMGNeoStackKind.SituationalOverlays, AIGMUMGNeoStackKind.Governance);
                    break;
                case 5:
                    AIGMCapabilitySnapshot snapshot = AIGMCapabilityRegistry.CreateSnapshot(actor);
                    lines.Add(AIGMCapabilityRegistry.BuildCompactSummary(snapshot));
                    AIGMCapabilityValidationResult result = AIGMUMGRuntimeService.ValidateActiveBlocks(actor, sleeve);
                    lines.Add(result != null ? result.BuildSummary() : "validation unavailable");
                    break;
                case 6:
                    List<AIGMUMGVersionRecord> versions = AIGMUMGRepository.GetVersionsForTarget(actorId);
                    for (int i = 0; i < versions.Count && i < 5; i++)
                        lines.Add(String.Format("{0} {1}", versions[i].VersionId, versions[i].Summary));
                    break;
            }

            if (lines.Count == 0)
                lines.Add("No entries.");

            return lines;
        }

        private string SectionStatus(Mobile actor, int section)
        {
            if (actor == null)
                return "missing";

            string actorId = AIGMUMGRuntimeService.ResolveActorId(actor);
            AIGMUMGSleeve sleeve = AIGMUMGRepository.GetSleeve(actorId);
            List<AIGMUMGAssignment> assignments = AIGMUMGRepository.GetAssignmentsForTarget(actorId);

            switch (section)
            {
                case 0:
                    return CountStacks(sleeve, AIGMUMGNeoStackKind.Identity, AIGMUMGNeoStackKind.Governance) + " stacks";
                case 1:
                    return CountAssignments(assignments, AIGMUMGNeoStackKind.CombatDoctrine) + " assigns";
                case 2:
                    return CountAssignments(assignments, AIGMUMGNeoStackKind.MovementPositioning) + " assigns";
                case 3:
                    return CountAssignments(assignments, AIGMUMGNeoStackKind.SkillsSpellsResources) + " assigns";
                case 4:
                    return CountStacks(sleeve, AIGMUMGNeoStackKind.SituationalOverlays, AIGMUMGNeoStackKind.Governance) + " stacks";
                case 5:
                    return CountWarnings(AIGMUMGRuntimeService.ValidateActiveBlocks(actor, sleeve)) + " warnings";
                case 6:
                    return AIGMUMGRepository.GetVersionsForTarget(actorId).Count + " versions";
                default:
                    return String.Empty;
            }
        }

        private static void AddStackLines(List<string> lines, AIGMUMGSleeve sleeve, params AIGMUMGNeoStackKind[] kinds)
        {
            if (sleeve == null || sleeve.NeoStacks == null)
                return;

            for (int i = 0; i < sleeve.NeoStacks.Count; i++)
            {
                AIGMUMGNeoStack stack = sleeve.NeoStacks[i];
                if (stack == null || !ContainsKind(kinds, stack.StackKind))
                    continue;

                lines.Add(String.Format("{0}: enabled={1} active={2} draft={3}", stack.Name, stack.Enabled, CountMoltBlocks(stack, AIGMUMGBlockState.Active), CountMoltBlocks(stack, AIGMUMGBlockState.Draft)));
            }
        }

        private static void AddAssignmentLines(List<string> lines, List<AIGMUMGAssignment> assignments, AIGMUMGNeoStackKind kind)
        {
            for (int i = 0; i < assignments.Count; i++)
            {
                AIGMUMGAssignment assignment = assignments[i];
                if (assignment == null || !String.Equals(NormalizeStackId(kind), Normalize(assignment.NeoStackId), StringComparison.OrdinalIgnoreCase))
                    continue;

                lines.Add(String.Format("{0}: {1} {2} {3}", assignment.AssignmentId, assignment.State, assignment.ExecutionMode, assignment.PreviewParticipation));
            }
        }

        private static AIGMUMGAssignment SelectCurrentAssignment(List<AIGMUMGAssignment> assignments)
        {
            AIGMUMGAssignment fallback = null;
            for (int i = 0; i < assignments.Count; i++)
            {
                AIGMUMGAssignment assignment = assignments[i];
                if (assignment == null)
                    continue;

                if (fallback == null)
                    fallback = assignment;

                if (assignment.State == AIGMUMGAssignmentState.Approved
                    && assignment.ExecutionMode == AIGMUMGExecutionMode.PreviewOnly
                    && assignment.PreviewParticipation == AIGMUMGPreviewParticipation.EnabledPreview)
                    return assignment;
            }

            return fallback;
        }

        private static string CurrentAssignmentSelector(Mobile actor)
        {
            if (actor == null)
                return String.Empty;

            List<AIGMUMGAssignment> assignments = AIGMUMGRepository.GetAssignmentsForTarget(AIGMUMGRuntimeService.ResolveActorId(actor));
            AIGMUMGAssignment assignment = SelectCurrentAssignment(assignments);
            return assignment != null ? assignment.AssignmentId : String.Empty;
        }

        private static string CurrentDefinitionName(AIGMUMGAssignment assignment)
        {
            if (assignment == null)
                return "none";

            AIGMUMGLibraryDefinition definition = AIGMUMGRepository.GetLibraryDefinition(assignment.DefinitionId);
            return definition != null && !String.IsNullOrWhiteSpace(definition.Name) ? definition.Name : assignment.DefinitionId;
        }

        private bool IsExpanded(int index)
        {
            return (_expandedMask & (1 << index)) != 0;
        }

        private static int CountWarnings(AIGMCapabilityValidationResult validation)
        {
            int count = validation != null && validation.Failed != null ? validation.Failed.Count : 0;
            if (AIGMUMGPhase64C2Invariant.TacticalDispatchEnabled)
                count++;

            return count;
        }

        private static int CountStacks(AIGMUMGSleeve sleeve, params AIGMUMGNeoStackKind[] kinds)
        {
            if (sleeve == null || sleeve.NeoStacks == null)
                return 0;

            int count = 0;
            for (int i = 0; i < sleeve.NeoStacks.Count; i++)
            {
                AIGMUMGNeoStack stack = sleeve.NeoStacks[i];
                if (stack != null && ContainsKind(kinds, stack.StackKind))
                    count++;
            }

            return count;
        }

        private static int CountAssignments(List<AIGMUMGAssignment> assignments, AIGMUMGNeoStackKind kind)
        {
            int count = 0;
            for (int i = 0; i < assignments.Count; i++)
            {
                AIGMUMGAssignment assignment = assignments[i];
                if (assignment != null && String.Equals(NormalizeStackId(kind), Normalize(assignment.NeoStackId), StringComparison.OrdinalIgnoreCase))
                    count++;
            }

            return count;
        }

        private static int CountMoltBlocks(AIGMUMGNeoStack stack, AIGMUMGBlockState state)
        {
            int count = 0;
            if (stack == null || stack.NeoBlocks == null)
                return count;

            for (int i = 0; i < stack.NeoBlocks.Count; i++)
            {
                AIGMUMGNeoBlock neoBlock = stack.NeoBlocks[i];
                if (neoBlock == null || neoBlock.BlockStacks == null)
                    continue;

                for (int j = 0; j < neoBlock.BlockStacks.Count; j++)
                {
                    AIGMUMGBlockStack blockStack = neoBlock.BlockStacks[j];
                    if (blockStack == null || blockStack.MoltBlocks == null)
                        continue;

                    for (int k = 0; k < blockStack.MoltBlocks.Count; k++)
                    {
                        AIGMUMGBlock block = blockStack.MoltBlocks[k];
                        if (block != null && block.BlockState == state)
                            count++;
                    }
                }
            }

            return count;
        }

        private static bool ContainsKind(AIGMUMGNeoStackKind[] kinds, AIGMUMGNeoStackKind kind)
        {
            if (kinds == null)
                return false;

            for (int i = 0; i < kinds.Length; i++)
            {
                if (kinds[i] == kind)
                    return true;
            }

            return false;
        }

        private static string NormalizeStackId(AIGMUMGNeoStackKind kind)
        {
            return Normalize(kind.ToString());
        }

        private static string Normalize(string value)
        {
            return value == null ? String.Empty : value.Trim().ToLowerInvariant().Replace(" ", String.Empty).Replace("_", String.Empty).Replace("-", String.Empty);
        }

        private static string Header(Mobile actor)
        {
            return String.Format("UMG Sleeve - {0} - PREVIEW ONLY", actor != null ? Safe(actor.Name) : "missing actor");
        }

        private static string Safe(string value)
        {
            return String.IsNullOrWhiteSpace(value) ? String.Empty : value.Replace('\r', ' ').Replace('\n', ' ');
        }
    }
}
