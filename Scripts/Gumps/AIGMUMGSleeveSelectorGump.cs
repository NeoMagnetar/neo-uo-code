using System;
using System.Collections.Generic;
using System.Text;
using Server.Custom.AIGM.Inventory;
using Server.Custom.AIGM.UMG;
using Server.Network;

namespace Server.Gumps
{
    public class AIGMUMGSleeveSelectorGump : Gump
    {
        private const int FamilyButtonBase = 100;
        private const int StackButtonBase = 200;
        private const int ReferenceButtonBase = 400;
        private const int ButtonToggleMode = 900;
        private const int ButtonBackMain = 901;
        private const int ButtonMoveUp = 1000;
        private const int ButtonMoveDown = 1001;
        private const int ButtonMoveTop = 1002;
        private const int ButtonMoveBottom = 1003;
        private const int ButtonMoveToStack = 1004;
        private const int ButtonToggleEnabled = 1100;
        private const int ButtonToggleLock = 1101;
        private const int ButtonAddStack = 1200;
        private const int ButtonRenameStack = 1201;
        private const int ButtonAddBlockPage = 1202;
        private const int ButtonRemoveReference = 1203;
        private const int ButtonApplyFilter = 1204;
        private const int ButtonPreview = 1300;
        private const int ButtonWhy = 1301;
        private const int ButtonCapability = 1302;
        private const int ButtonConflicts = 1303;
        private const int ButtonProvenance = 1304;
        private const int ButtonVersions = 1400;
        private const int ButtonRollback = 1401;
        private const int ButtonReload = 1402;
        private const int ButtonResetCompare = 1403;
        private const int ButtonSaveDraft = 1500;
        private const int ButtonApprovePreview = 1501;
        private const int ButtonCancel = 1502;
        private const int ButtonConfirmCancel = 1503;
        private const int ButtonConfirmReload = 1504;
        private const int ButtonConfirmRemove = 1505;
        private const int ButtonPrevPage = 1600;
        private const int ButtonNextPage = 1601;
        private const int ButtonComposer = 1700;
        private const int ButtonBackpack = 1701;
        private const int SecondaryButtonBase = 1800;
        private const int VersionButtonBase = 1900;

        private const int TextEntryStackName = 1;
        private const int TextEntryFilter = 2;
        private const int RowsPerPage = 16;
        private const int Width = 790;
        private const int Height = 585;
        private const int RowHeight = 22;
        private const int TextHue = 0x0481;
        private const int DimHue = 0x03B2;
        private const int WarningHue = 0x0026;
        private const int ErrorHue = 0x0025;
        private const int GreenHue = 0x0044;

        private readonly Serial _actorSerial;
        private readonly string _sessionId;

        private sealed class HierarchyRow
        {
            public string Kind;
            public string Id;
            public int Indent;
            public string Name;
            public string Status;
            public bool Expanded;
            public bool Selected;
        }

        public AIGMUMGSleeveSelectorGump(Mobile from, Mobile actor, int expandedMask)
            : this(from, actor, StartSession(from, actor))
        {
        }

        public AIGMUMGSleeveSelectorGump(Mobile from, Mobile actor, string sessionId)
            : base(SkillsGump.GumpOffsetX, SkillsGump.GumpOffsetY)
        {
            _actorSerial = actor != null ? actor.Serial : Server.Serial.MinusOne;
            _sessionId = sessionId ?? String.Empty;

            Closable = true;
            Disposable = true;
            Dragable = true;

            AddPage(0);

            AIGMUMGOperationalLayoutEditSession session = AIGMUMGOperationalLayoutService.GetSession(_sessionId);
            if (session == null || session.WorkingLayout == null)
            {
                DrawUnavailable(actor);
                return;
            }

            AIGMUMGOperationalLayoutService.PrepareRenderMaps(session);
            DrawFrame(actor, session);

            string view = session.View ?? "Main";
            if (String.Equals(view, "Library", StringComparison.OrdinalIgnoreCase))
                DrawLibraryPage(actor, session);
            else if (String.Equals(view, "Versions", StringComparison.OrdinalIgnoreCase))
                DrawVersionsPage(actor, session);
            else if (String.Equals(view, "Move", StringComparison.OrdinalIgnoreCase))
                DrawMovePage(session);
            else if (String.Equals(view, "ConfirmCancel", StringComparison.OrdinalIgnoreCase))
                DrawConfirmation(session, "Discard unsaved organizer changes?", ButtonConfirmCancel);
            else if (String.Equals(view, "ConfirmReload", StringComparison.OrdinalIgnoreCase))
                DrawConfirmation(session, "Reload newest persisted layout and discard unsaved changes?", ButtonConfirmReload);
            else if (String.Equals(view, "ConfirmRemove", StringComparison.OrdinalIgnoreCase))
                DrawConfirmation(session, "Remove reference with local data or notes?", ButtonConfirmRemove);
            else
                DrawMain(actor, session);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (sender == null || sender.Mobile == null || info == null)
                return;

            Mobile caller = sender.Mobile;
            if (info.ButtonID == 0)
            {
                AIGMUMGOperationalLayoutService.CancelSession(_sessionId);
                return;
            }

            Mobile actor = World.FindMobile(_actorSerial);
            if (actor == null || actor.Deleted)
            {
                caller.SendMessage(38, "UMG organizer target is no longer available.");
                AIGMUMGOperationalLayoutService.CancelSession(_sessionId);
                return;
            }

            bool requiresArchitect = IsArchitectMutation(info.ButtonID);
            bool rejectsStale = IsPersistentMutation(info.ButtonID);
            AIGMUMGOperationalLayoutActionResult touch = AIGMUMGOperationalLayoutService.TouchSession(caller, actor, _sessionId, requiresArchitect, rejectsStale);
            AIGMUMGOperationalLayoutEditSession session = touch.Session ?? AIGMUMGOperationalLayoutService.GetSession(_sessionId);
            if (!touch.Accepted)
            {
                caller.SendMessage(38, touch.Message);
                if (session != null)
                {
                    session.LastMessage = touch.Message;
                    Resend(caller, actor, session);
                }
                return;
            }

            if (session == null)
            {
                caller.SendMessage(38, "UMG organizer session expired.");
                return;
            }

            if (HandleRenderedSelection(info, session))
            {
                Resend(caller, actor, session);
                return;
            }

            if (HandleSecondary(caller, actor, info, session))
            {
                if (AIGMUMGOperationalLayoutService.GetSession(session.SessionId) != null)
                    Resend(caller, actor, session);
                return;
            }

            HandlePrimary(caller, actor, info, session);

            if (AIGMUMGOperationalLayoutService.GetSession(session.SessionId) != null)
                Resend(caller, actor, session);
        }

        private static string StartSession(Mobile from, Mobile actor)
        {
            AIGMUMGOperationalLayoutActionResult result = AIGMUMGOperationalLayoutService.BeginSession(from, actor);
            if (result == null || !result.Accepted || result.Session == null)
            {
                if (from != null && result != null)
                    from.SendMessage(38, result.Message);
                return String.Empty;
            }

            return result.Session.SessionId;
        }

        private void DrawUnavailable(Mobile actor)
        {
            AddBackground(0, 0, 420, 130, SkillsGump.BackGumpID);
            AddImageTiled(12, 12, 396, 106, SkillsGump.OffsetGumpID);
            AddLabel(24, 24, ErrorHue, "UMG Sleeve Organizer unavailable");
            AddLabelCropped(24, 52, 360, 22, DimHue, actor != null ? Safe(actor.Name) : "No actor");
            AddLabel(24, 82, WarningHue, "Open again from the companion paperdoll or [umgsleeve].");
        }

        private void DrawFrame(Mobile actor, AIGMUMGOperationalLayoutEditSession session)
        {
            AddBackground(0, 0, Width, Height, SkillsGump.BackGumpID);
            AddImageTiled(12, 12, Width - 24, 42, SkillsGump.HeaderGumpID);
            AddImageTiled(12, 62, 446, 398, SkillsGump.OffsetGumpID);
            AddImageTiled(466, 62, 312, 398, SkillsGump.OffsetGumpID);
            AddImageTiled(12, 468, Width - 24, 88, SkillsGump.OffsetGumpID);

            AddLabelCropped(24, 20, 742, 20, TextHue, Header(actor, session));
            AddLabelCropped(24, 38, 742, 16, WarningHue, AIGMUMGOperationalLayoutService.PreviewOnlyLabel);
        }

        private void DrawMain(Mobile actor, AIGMUMGOperationalLayoutEditSession session)
        {
            AIGMUMGOperationalLayoutValidationResult validation = AIGMUMGOperationalLayoutService.ValidateLayout(actor, session.WorkingLayout);
            DrawHeaderSummary(actor, session, validation);
            DrawHierarchy(session);
            DrawDetail(actor, session, validation);
            DrawControls(session, validation);
            DrawMessage(session);
        }

        private void DrawHeaderSummary(Mobile actor, AIGMUMGOperationalLayoutEditSession session, AIGMUMGOperationalLayoutValidationResult validation)
        {
            AIGMUMGSleeve sleeve = actor != null ? AIGMUMGRepository.GetSleeve(AIGMUMGRuntimeService.ResolveActorId(actor)) : null;
            int y = 70;
            AddSummaryRow(478, y, "Actor", actor != null ? Safe(actor.Name) : "missing", "Serial", actor != null ? String.Format("0x{0:X8}", actor.Serial.Value) : "missing");
            y += 22;
            AddSummaryRow(478, y, "Sleeve", sleeve != null ? Safe(sleeve.SleeveId) : "missing", "Layout", session.WorkingLayout.State.ToString());
            y += 22;
            AddSummaryRow(478, y, "Revision", session.BaseRevision.ToString(), "Base", String.IsNullOrWhiteSpace(session.BaseVersionId) ? "UNSAVED" : session.BaseVersionId);
            y += 22;
            AddSummaryRow(478, y, "Mode", session.ArchitectMode ? "Architect" : "Operator", "Dirty", session.Dirty ? "yes" : "no");
            y += 22;
            AddSummaryRow(478, y, "Errors", validation.StructuralErrors.Count.ToString(), "Warnings", validation.Warnings.Count.ToString());
            y += 22;
            AddSummaryRow(478, y, "Dispatch", AIGMUMGPhase64C2Invariant.TacticalDispatchEnabled ? "enabled" : "disabled", "Preview", AIGMUMGOperationalLayoutService.PreviewResult);
        }

        private void DrawHierarchy(AIGMUMGOperationalLayoutEditSession session)
        {
            List<HierarchyRow> rows = BuildHierarchyRows(session);
            int pageCount = Math.Max(1, (rows.Count + RowsPerPage - 1) / RowsPerPage);
            if (session.Page < 0)
                session.Page = 0;
            if (session.Page >= pageCount)
                session.Page = pageCount - 1;

            AddLabel(24, 68, TextHue, "Family / Operational NeoStack / NeoBlock Reference");
            int start = session.Page * RowsPerPage;
            int end = Math.Min(rows.Count, start + RowsPerPage);
            int y = 92;

            for (int i = start; i < end; i++)
            {
                HierarchyRow row = rows[i];
                int buttonId = ButtonForRow(session, row);
                DrawHierarchyRow(24 + row.Indent, y, 420 - row.Indent, buttonId, row);
                y += RowHeight;
            }

            AddLabelCropped(24, 444, 280, 18, DimHue, String.Format("Page {0}/{1}  Rows {2}", session.Page + 1, pageCount, rows.Count));
            AddButton(312, 442, SkillsGump.PrevButtonID1, SkillsGump.PrevButtonID2, ButtonPrevPage, GumpButtonType.Reply, 0);
            AddButton(356, 442, SkillsGump.NextButtonID1, SkillsGump.NextButtonID2, ButtonNextPage, GumpButtonType.Reply, 0);
        }

        private void DrawHierarchyRow(int x, int y, int width, int buttonId, HierarchyRow row)
        {
            AddImageTiled(x, y, 24, SkillsGump.EntryHeight, SkillsGump.HeaderGumpID);
            AddButton(x + 4, y + 2, row.Expanded ? 0x15E2 : 0x15E1, row.Expanded ? 0x15E6 : 0x15E5, buttonId, GumpButtonType.Reply, 0);
            AddImageTiled(x + 26, y, width - 116, SkillsGump.EntryHeight, row.Selected ? SkillsGump.HeaderGumpID : SkillsGump.EntryGumpID);
            AddLabelCropped(x + 32, y + 1, width - 126, SkillsGump.EntryHeight, row.Selected ? GreenHue : TextHue, row.Name);
            AddImageTiled(x + width - 88, y + 1, 84, SkillsGump.EntryHeight - 2, SkillsGump.EntryGumpID);
            AddLabelCropped(x + width - 82, y + 1, 78, SkillsGump.EntryHeight - 2, DimHue, row.Status);
        }

        private void DrawDetail(Mobile actor, AIGMUMGOperationalLayoutEditSession session, AIGMUMGOperationalLayoutValidationResult validation)
        {
            AddLabel(478, 214, TextHue, "Selected Detail");

            AIGMUMGOperationalFamily family;
            AIGMUMGOperationalNeoStack stack;
            AIGMUMGNeoBlockReference reference;
            FindSelection(session, out family, out stack, out reference);

            StringBuilder sb = new StringBuilder();
            if (reference != null)
            {
                AIGMUMGLibraryDefinition definition = AIGMUMGRepository.GetLibraryDefinition(reference.DefinitionId);
                sb.Append(Line("Definition", reference.DefinitionId));
                sb.Append(Line("Name", reference.DisplayName));
                sb.Append(Line("Canonical Stack", reference.CanonicalStack));
                sb.Append(Line("State", reference.Enabled ? "Configuration Enabled" : "Configuration Disabled"));
                sb.Append(Line("Lock", reference.Locked ? "Locked" : "Unlocked"));
                sb.Append(Line("Capability", Capability(actor, definition)));
                sb.Append(Line("Conflicts", CountConflicts(reference, validation).ToString()));
                sb.Append(Line("Provenance", reference.Provenance));
                sb.Append(Line("Assignment", String.IsNullOrWhiteSpace(reference.AssignmentId) ? "none" : reference.AssignmentId));
                sb.Append(Line("Parameters", reference.OptionalLocalParameters != null ? reference.OptionalLocalParameters.Count.ToString() : "0"));
                sb.Append(Line("Notes", String.IsNullOrWhiteSpace(reference.Notes) ? "none" : reference.Notes));
            }
            else if (stack != null)
            {
                sb.Append(Line("Stack", stack.StackId));
                sb.Append(Line("Name", stack.DisplayName));
                sb.Append(Line("Family", stack.FamilyId));
                sb.Append(Line("State", stack.Enabled ? "Configuration Enabled" : "Configuration Disabled"));
                sb.Append(Line("Lock", stack.Locked ? "Locked" : "Unlocked"));
                sb.Append(Line("System", stack.SystemDefined ? "yes" : "custom"));
                sb.Append(Line("References", stack.NeoBlockReferences != null ? stack.NeoBlockReferences.Count.ToString() : "0"));
            }
            else if (family != null)
            {
                sb.Append(Line("Family", family.FamilyId));
                sb.Append(Line("Name", family.DisplayName));
                sb.Append(Line("State", family.Enabled ? "Configuration Enabled" : "Configuration Disabled"));
                sb.Append(Line("Lock", family.Locked ? "Locked" : "Unlocked"));
                sb.Append(Line("System", family.SystemDefined ? "yes" : "custom"));
                sb.Append(Line("Stacks", family.OperationalNeoStacks != null ? family.OperationalNeoStacks.Count.ToString() : "0"));
            }
            else
            {
                sb.Append(Line("Selection", "none"));
                sb.Append(Line("Hint", "Select a family, stack, or reference."));
            }

            AddHtml(478, 240, 288, 170, BeginHtml(sb), true, true);
        }

        private void DrawControls(AIGMUMGOperationalLayoutEditSession session, AIGMUMGOperationalLayoutValidationResult validation)
        {
            int y = 474;
            DrawActionButton(24, y, ButtonToggleMode, session.ArchitectMode ? "Operator" : "Architect", 88);
            DrawActionButton(118, y, ButtonPreview, "Preview", 80);
            DrawActionButton(204, y, ButtonWhy, "Why", 62);
            DrawActionButton(272, y, ButtonVersions, "Versions", 86);
            DrawActionButton(364, y, ButtonComposer, "Composer", 92);
            DrawActionButton(462, y, ButtonBackpack, "Backpack", 90);
            DrawActionButton(558, y, ButtonReload, "Reload", 74);
            DrawActionButton(638, y, 0, "Close", 64);

            y += 30;
            DrawStackNameEntry(24, y, session);
            DrawActionButton(240, y, ButtonAddStack, "Add Stack", 88);
            DrawActionButton(334, y, ButtonRenameStack, "Rename", 76);
            DrawActionButton(416, y, ButtonAddBlockPage, "Add Block", 86);
            DrawActionButton(508, y, ButtonRemoveReference, "Remove", 76);
            DrawActionButton(590, y, ButtonSaveDraft, "Save Draft", 92);
            DrawActionButton(688, y, ButtonApprovePreview, "Approve", 76);

            y += 30;
            DrawIconButton(24, y, ButtonMoveUp, 0x983, "Up");
            DrawIconButton(54, y, ButtonMoveDown, 0x985, "Down");
            DrawActionButton(84, y, ButtonMoveTop, "Top", 58);
            DrawActionButton(148, y, ButtonMoveBottom, "Bottom", 74);
            DrawActionButton(228, y, ButtonMoveToStack, "Move To", 84);
            DrawActionButton(318, y, ButtonToggleEnabled, "Enable", 74);
            DrawIconButton(398, y, ButtonToggleLock, 0x82C, "Lock");
            DrawActionButton(430, y, ButtonCapability, "Capability", 98);
            DrawActionButton(534, y, ButtonConflicts, "Conflict", 86);
            DrawActionButton(626, y, ButtonProvenance, "Prov", 58);
            DrawActionButton(690, y, ButtonCancel, "Cancel", 70);

            string state = validation.CanApprovePreviewOnly ? "Preview Eligible" : validation.CanSaveDraft ? "Draft Eligible" : "Blocked";
            AddLabelCropped(478, 424, 288, 18, validation.CanSaveDraft ? GreenHue : ErrorHue, state);
        }

        private void DrawMessage(AIGMUMGOperationalLayoutEditSession session)
        {
            if (session == null || String.IsNullOrWhiteSpace(session.LastMessage))
                return;

            AddLabelCropped(24, 556, 740, 20, WarningHue, session.LastMessage);
        }

        private void DrawLibraryPage(Mobile actor, AIGMUMGOperationalLayoutEditSession session)
        {
            AddLabel(24, 68, TextHue, "Add Canonical NeoBlock Reference");
            AddImageTiled(24, 92, 330, SkillsGump.EntryHeight, SkillsGump.EntryGumpID);
            AddTextEntry(30, 93, 318, SkillsGump.EntryHeight - 2, TextHue, TextEntryFilter, session.FilterText ?? String.Empty);
            DrawActionButton(364, 90, ButtonApplyFilter, "Filter", 72);
            DrawActionButton(444, 90, ButtonBackMain, "Back", 62);

            List<AIGMUMGLibraryDefinition> definitions = FilterDefinitions(session.FilterText);
            int pageCount = Math.Max(1, (definitions.Count + RowsPerPage - 1) / RowsPerPage);
            if (session.Page < 0)
                session.Page = 0;
            if (session.Page >= pageCount)
                session.Page = pageCount - 1;

            int start = session.Page * RowsPerPage;
            int end = Math.Min(definitions.Count, start + RowsPerPage);
            int y = 124;
            for (int i = start; i < end; i++)
            {
                AIGMUMGLibraryDefinition definition = definitions[i];
                int buttonId = SecondaryButtonBase + session.RenderedDefinitionIds.Count;
                session.RenderedDefinitionIds.Add(definition.DefinitionId);

                AddImageTiled(24, y, 24, SkillsGump.EntryHeight, SkillsGump.HeaderGumpID);
                AddButton(28, y + 2, 0x15E1, 0x15E5, buttonId, GumpButtonType.Reply, 0);
                AddImageTiled(50, y, 502, SkillsGump.EntryHeight, SkillsGump.EntryGumpID);
                AddLabelCropped(56, y + 1, 250, SkillsGump.EntryHeight, TextHue, Safe(definition.Name));
                AddLabelCropped(310, y + 1, 150, SkillsGump.EntryHeight, DimHue, definition.DefinitionId);
                AddLabelCropped(464, y + 1, 84, SkillsGump.EntryHeight, DimHue, Capability(actor, definition));
                y += RowHeight;
            }

            DrawPagedFooter(session.Page, definitions.Count, pageCount);
            DrawMessage(session);
        }

        private void DrawVersionsPage(Mobile actor, AIGMUMGOperationalLayoutEditSession session)
        {
            AddLabel(24, 68, TextHue, "Operational Layout Versions");
            AddLabelCropped(24, 90, 740, 18, DimHue, "Select one version for rollback, then select a second version to compare.");

            List<AIGMUMGOperationalLayoutVersionRecord> versions = AIGMUMGOperationalLayoutService.GetLayoutVersionsForActor(session.ActorKey);
            int pageCount = Math.Max(1, (versions.Count + RowsPerPage - 1) / RowsPerPage);
            if (session.Page < 0)
                session.Page = 0;
            if (session.Page >= pageCount)
                session.Page = pageCount - 1;

            int start = session.Page * RowsPerPage;
            int end = Math.Min(versions.Count, start + RowsPerPage);
            int y = 120;
            for (int i = start; i < end; i++)
            {
                AIGMUMGOperationalLayoutVersionRecord version = versions[i];
                int buttonId = VersionButtonBase + session.RenderedVersionIds.Count;
                session.RenderedVersionIds.Add(version.VersionId);

                AddImageTiled(24, y, 24, SkillsGump.EntryHeight, SkillsGump.HeaderGumpID);
                AddButton(28, y + 2, 0x15E1, 0x15E5, buttonId, GumpButtonType.Reply, 0);
                AddImageTiled(50, y, 666, SkillsGump.EntryHeight, SkillsGump.EntryGumpID);
                AddLabelCropped(56, y + 1, 210, SkillsGump.EntryHeight, IsVersionSelected(session, version.VersionId) ? GreenHue : TextHue, version.VersionId);
                AddLabelCropped(270, y + 1, 58, SkillsGump.EntryHeight, DimHue, "r" + version.Revision);
                AddLabelCropped(332, y + 1, 128, SkillsGump.EntryHeight, DimHue, version.State.ToString());
                AddLabelCropped(464, y + 1, 92, SkillsGump.EntryHeight, DimHue, version.Author);
                AddLabelCropped(560, y + 1, 150, SkillsGump.EntryHeight, DimHue, version.TimestampUtc.ToString("yyyy-MM-dd HH:mm"));
                y += RowHeight;
            }

            DrawPagedFooter(session.Page, versions.Count, pageCount);
            DrawActionButton(24, 502, ButtonRollback, "Rollback", 86);
            DrawActionButton(116, 502, ButtonResetCompare, "Clear", 64);
            DrawActionButton(186, 502, ButtonReload, "Reload", 74);
            DrawActionButton(266, 502, ButtonBackMain, "Back", 62);
            AddLabelCropped(344, 504, 420, 18, WarningHue, CompareSummary(session));
            DrawMessage(session);
        }

        private void DrawMovePage(AIGMUMGOperationalLayoutEditSession session)
        {
            AddLabel(24, 68, TextHue, "Move Reference To Operational NeoStack");
            List<AIGMUMGOperationalNeoStack> stacks = BuildMoveTargets(session);
            int pageCount = Math.Max(1, (stacks.Count + RowsPerPage - 1) / RowsPerPage);
            if (session.Page < 0)
                session.Page = 0;
            if (session.Page >= pageCount)
                session.Page = pageCount - 1;

            int start = session.Page * RowsPerPage;
            int end = Math.Min(stacks.Count, start + RowsPerPage);
            int y = 98;
            for (int i = start; i < end; i++)
            {
                AIGMUMGOperationalNeoStack stack = stacks[i];
                int buttonId = SecondaryButtonBase + session.RenderedStackIds.Count;
                session.RenderedStackIds.Add(stack.StackId);
                AddImageTiled(24, y, 24, SkillsGump.EntryHeight, SkillsGump.HeaderGumpID);
                AddButton(28, y + 2, 0x15E1, 0x15E5, buttonId, GumpButtonType.Reply, 0);
                AddImageTiled(50, y, 420, SkillsGump.EntryHeight, SkillsGump.EntryGumpID);
                AddLabelCropped(56, y + 1, 250, SkillsGump.EntryHeight, TextHue, stack.DisplayName);
                AddLabelCropped(310, y + 1, 150, SkillsGump.EntryHeight, DimHue, stack.FamilyId);
                y += RowHeight;
            }

            DrawPagedFooter(session.Page, stacks.Count, pageCount);
            DrawActionButton(24, 502, ButtonBackMain, "Back", 62);
            DrawMessage(session);
        }

        private void DrawConfirmation(AIGMUMGOperationalLayoutEditSession session, string text, int confirmButton)
        {
            AddLabel(24, 74, WarningHue, text);
            AddLabelCropped(24, 104, 720, 22, DimHue, "This confirmation creates no persistence write unless the confirmed action is Save, Approval, or Rollback.");
            DrawActionButton(170, 150, confirmButton, "Confirm", 86);
            DrawActionButton(276, 150, ButtonBackMain, "Back", 62);
            DrawActionButton(358, 150, 0, "Close", 64);
            DrawMessage(session);
        }

        private bool HandleRenderedSelection(RelayInfo info, AIGMUMGOperationalLayoutEditSession session)
        {
            if (info.ButtonID >= FamilyButtonBase && info.ButtonID < StackButtonBase)
            {
                string familyId = AIGMUMGOperationalLayoutService.ResolveRenderedFamily(session, info.ButtonID - FamilyButtonBase);
                if (String.IsNullOrWhiteSpace(familyId))
                    return true;

                Toggle(session.ExpandedFamilyIds, familyId);
                session.SelectedFamilyId = familyId;
                session.SelectedStackId = String.Empty;
                session.SelectedReferenceId = String.Empty;
                session.View = "Main";
                return true;
            }

            if (info.ButtonID >= StackButtonBase && info.ButtonID < ReferenceButtonBase)
            {
                string stackId = AIGMUMGOperationalLayoutService.ResolveRenderedStack(session, info.ButtonID - StackButtonBase);
                if (String.IsNullOrWhiteSpace(stackId))
                    return true;

                Toggle(session.ExpandedStackIds, stackId);
                session.SelectedStackId = stackId;
                session.SelectedReferenceId = String.Empty;
                AIGMUMGOperationalNeoStack stack = AIGMUMGOperationalLayoutService.FindStack(session.WorkingLayout, stackId);
                if (stack != null)
                    session.SelectedFamilyId = stack.FamilyId;
                session.View = "Main";
                return true;
            }

            if (info.ButtonID >= ReferenceButtonBase && info.ButtonID < 900)
            {
                string referenceId = AIGMUMGOperationalLayoutService.ResolveRenderedReference(session, info.ButtonID - ReferenceButtonBase);
                if (String.IsNullOrWhiteSpace(referenceId))
                    return true;

                session.SelectedReferenceId = referenceId;
                AIGMUMGOperationalFamily family;
                AIGMUMGOperationalNeoStack stack;
                AIGMUMGNeoBlockReference reference;
                FindReference(session.WorkingLayout, referenceId, out family, out stack, out reference);
                if (stack != null)
                    session.SelectedStackId = stack.StackId;
                if (family != null)
                    session.SelectedFamilyId = family.FamilyId;
                session.View = "Main";
                return true;
            }

            return false;
        }

        private bool HandleSecondary(Mobile caller, Mobile actor, RelayInfo info, AIGMUMGOperationalLayoutEditSession session)
        {
            if (info.ButtonID == ButtonConfirmCancel)
            {
                AIGMUMGOperationalLayoutService.CancelSession(session.SessionId);
                caller.SendMessage(68, "Organizer changes canceled. No layout data was written.");
                return true;
            }

            if (info.ButtonID == ButtonConfirmReload)
            {
                ApplyResult(session, AIGMUMGOperationalLayoutService.ReloadSession(caller, actor, session));
                session.View = "Main";
                session.Page = 0;
                return true;
            }

            if (info.ButtonID == ButtonConfirmRemove)
            {
                if (!RequireArchitect(session))
                    return true;
                ApplyResult(session, AIGMUMGOperationalLayoutService.RemoveReference(session, true));
                session.View = "Main";
                return true;
            }

            if (info.ButtonID >= SecondaryButtonBase && info.ButtonID < VersionButtonBase)
            {
                if (String.Equals(session.View, "Library", StringComparison.OrdinalIgnoreCase))
                {
                    if (!RequireArchitect(session))
                        return true;
                    string definitionId = AIGMUMGOperationalLayoutService.ResolveRenderedDefinition(session, info.ButtonID - SecondaryButtonBase);
                    ApplyResult(session, AIGMUMGOperationalLayoutService.AddReference(actor, session, definitionId, SafeAuthor(caller)));
                    session.View = "Main";
                    return true;
                }

                if (String.Equals(session.View, "Move", StringComparison.OrdinalIgnoreCase))
                {
                    if (!RequireArchitect(session))
                        return true;
                    string stackId = AIGMUMGOperationalLayoutService.ResolveRenderedStack(session, info.ButtonID - SecondaryButtonBase);
                    ApplyResult(session, AIGMUMGOperationalLayoutService.MoveReference(session, "stack", stackId));
                    session.View = "Main";
                    return true;
                }
            }

            if (info.ButtonID >= VersionButtonBase && info.ButtonID < 2000)
            {
                string versionId = AIGMUMGOperationalLayoutService.ResolveRenderedVersion(session, info.ButtonID - VersionButtonBase);
                if (!String.IsNullOrWhiteSpace(versionId))
                {
                    if (String.IsNullOrWhiteSpace(session.CompareLeftVersionId) || !String.IsNullOrWhiteSpace(session.CompareRightVersionId))
                    {
                        session.CompareLeftVersionId = versionId;
                        session.CompareRightVersionId = String.Empty;
                        session.LastMessage = "Selected rollback/compare version: " + versionId;
                    }
                    else
                    {
                        session.CompareRightVersionId = versionId;
                        session.LastMessage = AIGMUMGOperationalLayoutService.CompareVersions(session.CompareLeftVersionId, session.CompareRightVersionId);
                    }
                }
                return true;
            }

            return false;
        }

        private void HandlePrimary(Mobile caller, Mobile actor, RelayInfo info, AIGMUMGOperationalLayoutEditSession session)
        {
            switch (info.ButtonID)
            {
                case ButtonToggleMode:
                    ApplyResult(session, AIGMUMGOperationalLayoutService.ToggleArchitectMode(caller, actor, session));
                    break;
                case ButtonBackMain:
                    session.View = "Main";
                    session.Page = 0;
                    break;
                case ButtonAddStack:
                    if (RequireArchitect(session))
                        ApplyResult(session, AIGMUMGOperationalLayoutService.CreateStack(session, SelectedFamilyOrDefault(session), GetText(info, TextEntryStackName), SafeAuthor(caller)));
                    break;
                case ButtonRenameStack:
                    if (RequireArchitect(session))
                        ApplyResult(session, AIGMUMGOperationalLayoutService.RenameStack(session, GetText(info, TextEntryStackName)));
                    break;
                case ButtonAddBlockPage:
                    if (RequireArchitect(session))
                    {
                        session.FilterText = GetText(info, TextEntryFilter);
                        session.View = "Library";
                        session.Page = 0;
                    }
                    break;
                case ButtonApplyFilter:
                    session.FilterText = GetText(info, TextEntryFilter);
                    session.View = "Library";
                    session.Page = 0;
                    break;
                case ButtonRemoveReference:
                    if (RequireArchitect(session))
                    {
                        AIGMUMGOperationalLayoutActionResult result = AIGMUMGOperationalLayoutService.RemoveReference(session, false);
                        ApplyResult(session, result);
                        if (String.Equals(result.Code, "remove_requires_confirmation", StringComparison.OrdinalIgnoreCase))
                            session.View = "ConfirmRemove";
                    }
                    break;
                case ButtonMoveUp:
                    MoveSelected(session, "up");
                    break;
                case ButtonMoveDown:
                    MoveSelected(session, "down");
                    break;
                case ButtonMoveTop:
                    MoveSelected(session, "top");
                    break;
                case ButtonMoveBottom:
                    MoveSelected(session, "bottom");
                    break;
                case ButtonMoveToStack:
                    if (RequireArchitect(session))
                    {
                        session.View = "Move";
                        session.Page = 0;
                    }
                    break;
                case ButtonToggleEnabled:
                    if (RequireArchitect(session))
                        ApplyResult(session, AIGMUMGOperationalLayoutService.ToggleReferenceEnabled(session));
                    break;
                case ButtonToggleLock:
                    if (RequireArchitect(session))
                        ApplyResult(session, AIGMUMGOperationalLayoutService.ToggleReferenceLock(session));
                    break;
                case ButtonPreview:
                    session.LastMessage = AIGMUMGOperationalLayoutService.Preview(actor, session);
                    session.View = "Main";
                    break;
                case ButtonWhy:
                    session.LastMessage = AIGMUMGOperationalLayoutService.BuildWhy(actor, session);
                    session.View = "Main";
                    break;
                case ButtonCapability:
                    session.LastMessage = AIGMUMGOperationalLayoutService.ValidateLayout(actor, session.WorkingLayout).BuildCompactSummary();
                    break;
                case ButtonConflicts:
                    session.LastMessage = BuildConflictMessage(actor, session);
                    break;
                case ButtonProvenance:
                    session.LastMessage = BuildProvenanceMessage(session);
                    break;
                case ButtonVersions:
                    session.View = "Versions";
                    session.Page = 0;
                    session.LastMessage = AIGMUMGOperationalLayoutService.BuildVersionsSummary(session.ActorKey);
                    break;
                case ButtonRollback:
                    if (RequireArchitect(session))
                    {
                        string versionId = session.CompareLeftVersionId;
                        if (String.IsNullOrWhiteSpace(versionId))
                            session.LastMessage = "Select a historical version first.";
                        else
                            ApplyResult(session, AIGMUMGOperationalLayoutService.Rollback(caller, actor, session, versionId, SafeAuthor(caller)));
                    }
                    break;
                case ButtonReload:
                    if (session.Dirty)
                        session.View = "ConfirmReload";
                    else
                        ApplyResult(session, AIGMUMGOperationalLayoutService.ReloadSession(caller, actor, session));
                    break;
                case ButtonResetCompare:
                    session.CompareLeftVersionId = String.Empty;
                    session.CompareRightVersionId = String.Empty;
                    session.LastMessage = "Version compare selection cleared.";
                    break;
                case ButtonSaveDraft:
                    if (RequireArchitect(session))
                        ApplyResult(session, AIGMUMGOperationalLayoutService.SaveDraft(caller, actor, session, SafeAuthor(caller)));
                    break;
                case ButtonApprovePreview:
                    if (RequireArchitect(session))
                        ApplyResult(session, AIGMUMGOperationalLayoutService.ApprovePreview(caller, actor, session, SafeAuthor(caller)));
                    break;
                case ButtonCancel:
                    if (session.Dirty)
                        session.View = "ConfirmCancel";
                    else
                    {
                        AIGMUMGOperationalLayoutService.CancelSession(session.SessionId);
                        caller.SendMessage(68, "Organizer closed. No layout data was written.");
                    }
                    break;
                case ButtonPrevPage:
                    session.Page = Math.Max(0, session.Page - 1);
                    break;
                case ButtonNextPage:
                    session.Page++;
                    break;
                case ButtonComposer:
                    caller.CloseGump(typeof(AIGMUMGPanelGump));
                    caller.SendGump(new AIGMUMGPanelGump(caller, actor, 0));
                    session.LastMessage = "Composer opened in PreviewOnly boundary.";
                    break;
                case ButtonBackpack:
                    AIGMCompanionInventoryService.OpenBackpack(caller, actor);
                    session.LastMessage = "Backpack access used inventory service. No organizer data was written.";
                    break;
            }
        }

        private static void MoveSelected(AIGMUMGOperationalLayoutEditSession session, string direction)
        {
            if (!RequireArchitect(session))
                return;

            ApplyResult(session, AIGMUMGOperationalLayoutService.MoveReference(session, direction, String.Empty));
        }

        private static bool RequireArchitect(AIGMUMGOperationalLayoutEditSession session)
        {
            if (session != null && session.ArchitectMode)
                return true;

            if (session != null)
                session.LastMessage = "Enter Architect Mode before structural organizer edits.";
            return false;
        }

        private static bool IsArchitectMutation(int buttonId)
        {
            return buttonId == ButtonMoveUp
                || buttonId == ButtonMoveDown
                || buttonId == ButtonMoveTop
                || buttonId == ButtonMoveBottom
                || buttonId == ButtonMoveToStack
                || buttonId == ButtonToggleEnabled
                || buttonId == ButtonToggleLock
                || buttonId == ButtonAddStack
                || buttonId == ButtonRenameStack
                || buttonId == ButtonRemoveReference
                || buttonId == ButtonRollback
                || buttonId == ButtonSaveDraft
                || buttonId == ButtonApprovePreview
                || buttonId == ButtonConfirmRemove
                || (buttonId >= SecondaryButtonBase && buttonId < VersionButtonBase);
        }

        private static bool IsPersistentMutation(int buttonId)
        {
            return buttonId == ButtonSaveDraft
                || buttonId == ButtonApprovePreview
                || buttonId == ButtonRollback;
        }

        private static List<HierarchyRow> BuildHierarchyRows(AIGMUMGOperationalLayoutEditSession session)
        {
            List<HierarchyRow> rows = new List<HierarchyRow>();
            if (session == null || session.WorkingLayout == null || session.WorkingLayout.Families == null)
                return rows;

            for (int f = 0; f < session.WorkingLayout.Families.Count; f++)
            {
                AIGMUMGOperationalFamily family = session.WorkingLayout.Families[f];
                if (family == null)
                    continue;

                bool familyExpanded = ContainsId(session.ExpandedFamilyIds, family.FamilyId);
                rows.Add(new HierarchyRow
                {
                    Kind = "family",
                    Id = family.FamilyId,
                    Indent = 0,
                    Name = family.DisplayName,
                    Status = family.Locked ? "Locked" : "Family",
                    Expanded = familyExpanded,
                    Selected = IsSelected(session.SelectedFamilyId, family.FamilyId) && String.IsNullOrWhiteSpace(session.SelectedStackId) && String.IsNullOrWhiteSpace(session.SelectedReferenceId)
                });

                if (!familyExpanded || family.OperationalNeoStacks == null)
                    continue;

                for (int s = 0; s < family.OperationalNeoStacks.Count; s++)
                {
                    AIGMUMGOperationalNeoStack stack = family.OperationalNeoStacks[s];
                    if (stack == null)
                        continue;

                    bool stackExpanded = ContainsId(session.ExpandedStackIds, stack.StackId);
                    rows.Add(new HierarchyRow
                    {
                        Kind = "stack",
                        Id = stack.StackId,
                        Indent = 18,
                        Name = stack.DisplayName,
                        Status = stack.SystemDefined ? "System" : "Custom",
                        Expanded = stackExpanded,
                        Selected = IsSelected(session.SelectedStackId, stack.StackId) && String.IsNullOrWhiteSpace(session.SelectedReferenceId)
                    });

                    if (!stackExpanded || stack.NeoBlockReferences == null)
                        continue;

                    for (int r = 0; r < stack.NeoBlockReferences.Count; r++)
                    {
                        AIGMUMGNeoBlockReference reference = stack.NeoBlockReferences[r];
                        if (reference == null)
                            continue;

                        rows.Add(new HierarchyRow
                        {
                            Kind = "reference",
                            Id = reference.ReferenceId,
                            Indent = 40,
                            Name = reference.DisplayName,
                            Status = reference.Locked ? "Lock" : reference.Enabled ? "Up" : "Down",
                            Expanded = false,
                            Selected = IsSelected(session.SelectedReferenceId, reference.ReferenceId)
                        });
                    }
                }
            }

            return rows;
        }

        private int ButtonForRow(AIGMUMGOperationalLayoutEditSession session, HierarchyRow row)
        {
            if (String.Equals(row.Kind, "family", StringComparison.OrdinalIgnoreCase))
            {
                int id = FamilyButtonBase + session.RenderedFamilyIds.Count;
                session.RenderedFamilyIds.Add(row.Id);
                return id;
            }
            if (String.Equals(row.Kind, "stack", StringComparison.OrdinalIgnoreCase))
            {
                int id = StackButtonBase + session.RenderedStackIds.Count;
                session.RenderedStackIds.Add(row.Id);
                return id;
            }

            int referenceId = ReferenceButtonBase + session.RenderedReferenceIds.Count;
            session.RenderedReferenceIds.Add(row.Id);
            return referenceId;
        }

        private static void FindSelection(AIGMUMGOperationalLayoutEditSession session, out AIGMUMGOperationalFamily family, out AIGMUMGOperationalNeoStack stack, out AIGMUMGNeoBlockReference reference)
        {
            family = null;
            stack = null;
            reference = null;
            if (session == null || session.WorkingLayout == null)
                return;

            if (!String.IsNullOrWhiteSpace(session.SelectedReferenceId)
                && FindReference(session.WorkingLayout, session.SelectedReferenceId, out family, out stack, out reference))
                return;

            if (!String.IsNullOrWhiteSpace(session.SelectedStackId))
            {
                stack = AIGMUMGOperationalLayoutService.FindStack(session.WorkingLayout, session.SelectedStackId);
                family = stack != null ? FindFamily(session.WorkingLayout, stack.FamilyId) : null;
                return;
            }

            if (!String.IsNullOrWhiteSpace(session.SelectedFamilyId))
                family = FindFamily(session.WorkingLayout, session.SelectedFamilyId);
        }

        private static bool FindReference(AIGMUMGOperationalLayout layout, string referenceId, out AIGMUMGOperationalFamily family, out AIGMUMGOperationalNeoStack stack, out AIGMUMGNeoBlockReference reference)
        {
            family = null;
            stack = null;
            reference = null;
            if (layout == null || layout.Families == null || String.IsNullOrWhiteSpace(referenceId))
                return false;

            for (int f = 0; f < layout.Families.Count; f++)
            {
                AIGMUMGOperationalFamily candidateFamily = layout.Families[f];
                if (candidateFamily == null || candidateFamily.OperationalNeoStacks == null)
                    continue;
                for (int s = 0; s < candidateFamily.OperationalNeoStacks.Count; s++)
                {
                    AIGMUMGOperationalNeoStack candidateStack = candidateFamily.OperationalNeoStacks[s];
                    if (candidateStack == null || candidateStack.NeoBlockReferences == null)
                        continue;
                    for (int r = 0; r < candidateStack.NeoBlockReferences.Count; r++)
                    {
                        AIGMUMGNeoBlockReference candidateReference = candidateStack.NeoBlockReferences[r];
                        if (candidateReference != null && String.Equals(candidateReference.ReferenceId, referenceId, StringComparison.OrdinalIgnoreCase))
                        {
                            family = candidateFamily;
                            stack = candidateStack;
                            reference = candidateReference;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static AIGMUMGOperationalFamily FindFamily(AIGMUMGOperationalLayout layout, string familyId)
        {
            if (layout == null || layout.Families == null || String.IsNullOrWhiteSpace(familyId))
                return null;

            for (int i = 0; i < layout.Families.Count; i++)
            {
                AIGMUMGOperationalFamily family = layout.Families[i];
                if (family != null && String.Equals(family.FamilyId, familyId, StringComparison.OrdinalIgnoreCase))
                    return family;
            }

            return null;
        }

        private static List<AIGMUMGOperationalNeoStack> BuildMoveTargets(AIGMUMGOperationalLayoutEditSession session)
        {
            List<AIGMUMGOperationalNeoStack> stacks = new List<AIGMUMGOperationalNeoStack>();
            if (session == null || session.WorkingLayout == null || session.WorkingLayout.Families == null)
                return stacks;

            for (int f = 0; f < session.WorkingLayout.Families.Count; f++)
            {
                AIGMUMGOperationalFamily family = session.WorkingLayout.Families[f];
                if (family == null || family.OperationalNeoStacks == null || String.Equals(family.FamilyId, AIGMUMGOperationalLayoutService.FamilyAlwaysOn, StringComparison.OrdinalIgnoreCase))
                    continue;
                for (int s = 0; s < family.OperationalNeoStacks.Count; s++)
                {
                    AIGMUMGOperationalNeoStack stack = family.OperationalNeoStacks[s];
                    if (stack != null && !stack.Locked && !String.Equals(stack.StackId, session.SelectedStackId, StringComparison.OrdinalIgnoreCase))
                        stacks.Add(stack);
                }
            }

            return stacks;
        }

        private static List<AIGMUMGLibraryDefinition> FilterDefinitions(string filter)
        {
            List<AIGMUMGLibraryDefinition> definitions = AIGMUMGRepository.GetLibraryDefinitions();
            definitions.Sort(delegate (AIGMUMGLibraryDefinition left, AIGMUMGLibraryDefinition right)
            {
                int byStack = String.Compare(left != null ? left.IntendedNeoStack.ToString() : String.Empty, right != null ? right.IntendedNeoStack.ToString() : String.Empty, StringComparison.OrdinalIgnoreCase);
                if (byStack != 0)
                    return byStack;
                return String.Compare(left != null ? left.DefinitionId : String.Empty, right != null ? right.DefinitionId : String.Empty, StringComparison.OrdinalIgnoreCase);
            });

            string needle = String.IsNullOrWhiteSpace(filter) ? String.Empty : filter.Trim();
            if (needle.Length == 0)
                return definitions;

            List<AIGMUMGLibraryDefinition> filtered = new List<AIGMUMGLibraryDefinition>();
            for (int i = 0; i < definitions.Count; i++)
            {
                AIGMUMGLibraryDefinition definition = definitions[i];
                if (definition == null)
                    continue;
                if (Contains(definition.Name, needle) || Contains(definition.DefinitionId, needle) || Contains(definition.Category, needle) || Contains(definition.IntendedNeoStack.ToString(), needle))
                    filtered.Add(definition);
            }

            return filtered;
        }

        private static string Capability(Mobile actor, AIGMUMGLibraryDefinition definition)
        {
            if (definition == null)
                return "Ready";
            AIGMUMGCompatibilityResult result = AIGMUMGLibraryService.CheckCompatibility(actor, definition);
            if (result == null)
                return "Unproven";
            if (result.State == AIGMUMGCompatibilityState.Compatible)
                return "Ready";
            if (result.State == AIGMUMGCompatibilityState.Degraded || result.State == AIGMUMGCompatibilityState.CompatibleWithFallback)
                return "Degraded";
            if (result.State == AIGMUMGCompatibilityState.Incompatible)
                return "Blocked";
            return "Unproven";
        }

        private static int CountConflicts(AIGMUMGNeoBlockReference reference, AIGMUMGOperationalLayoutValidationResult validation)
        {
            if (reference == null || validation == null)
                return 0;

            int count = 0;
            for (int i = 0; i < validation.HardConflicts.Count; i++)
            {
                if (Contains(validation.HardConflicts[i], reference.DefinitionId))
                    count++;
            }
            for (int i = 0; i < validation.SoftConflicts.Count; i++)
            {
                if (Contains(validation.SoftConflicts[i], reference.DefinitionId))
                    count++;
            }
            return count;
        }

        private static string BuildConflictMessage(Mobile actor, AIGMUMGOperationalLayoutEditSession session)
        {
            AIGMUMGOperationalLayoutValidationResult validation = AIGMUMGOperationalLayoutService.ValidateLayout(actor, session.WorkingLayout);
            if (validation.HardConflicts.Count == 0 && validation.SoftConflicts.Count == 0)
                return "Conflicts: none.";
            return "Conflicts hard=" + String.Join("|", validation.HardConflicts.ToArray()) + " soft=" + String.Join("|", validation.SoftConflicts.ToArray());
        }

        private static string BuildProvenanceMessage(AIGMUMGOperationalLayoutEditSession session)
        {
            AIGMUMGOperationalFamily family;
            AIGMUMGOperationalNeoStack stack;
            AIGMUMGNeoBlockReference reference;
            FindSelection(session, out family, out stack, out reference);
            if (reference != null)
                return "Provenance reference=" + reference.ReferenceId + "; origin=" + reference.Provenance + "; assignment=" + (String.IsNullOrWhiteSpace(reference.AssignmentId) ? "none" : reference.AssignmentId) + "; canonical=" + reference.DefinitionId;
            if (stack != null)
                return "Provenance stack=" + stack.StackId + "; " + (stack.SystemDefined ? "system_defined" : "custom_operational_stack");
            if (family != null)
                return "Provenance family=" + family.FamilyId + "; system_defined=" + family.SystemDefined;
            return "Provenance unavailable.";
        }

        private static void ApplyResult(AIGMUMGOperationalLayoutEditSession session, AIGMUMGOperationalLayoutActionResult result)
        {
            if (session == null || result == null)
                return;

            session.LastMessage = String.IsNullOrWhiteSpace(result.Message) ? result.Code : result.Message;
        }

        private static void Toggle(List<string> ids, string id)
        {
            if (ids == null || String.IsNullOrWhiteSpace(id))
                return;
            if (ContainsId(ids, id))
                ids.RemoveAll(delegate (string value) { return String.Equals(value, id, StringComparison.OrdinalIgnoreCase); });
            else
                ids.Add(id);
        }

        private static bool ContainsId(List<string> ids, string id)
        {
            if (ids == null || String.IsNullOrWhiteSpace(id))
                return false;
            for (int i = 0; i < ids.Count; i++)
            {
                if (String.Equals(ids[i], id, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static string SelectedFamilyOrDefault(AIGMUMGOperationalLayoutEditSession session)
        {
            if (session == null)
                return AIGMUMGOperationalLayoutService.FamilyCombat;
            if (!String.IsNullOrWhiteSpace(session.SelectedFamilyId))
                return session.SelectedFamilyId;
            return AIGMUMGOperationalLayoutService.FamilyCombat;
        }

        private static bool IsSelected(string left, string right)
        {
            return !String.IsNullOrWhiteSpace(left) && String.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsVersionSelected(AIGMUMGOperationalLayoutEditSession session, string versionId)
        {
            return String.Equals(session.CompareLeftVersionId, versionId, StringComparison.OrdinalIgnoreCase)
                || String.Equals(session.CompareRightVersionId, versionId, StringComparison.OrdinalIgnoreCase);
        }

        private static string CompareSummary(AIGMUMGOperationalLayoutEditSession session)
        {
            if (session == null || String.IsNullOrWhiteSpace(session.CompareLeftVersionId))
                return "No version selected.";
            if (String.IsNullOrWhiteSpace(session.CompareRightVersionId))
                return "Selected: " + session.CompareLeftVersionId;
            return "Compare: " + session.CompareLeftVersionId + " -> " + session.CompareRightVersionId;
        }

        private static string GetText(RelayInfo info, int entryId)
        {
            TextRelay relay = info != null ? info.GetTextEntry(entryId) : null;
            return relay != null && relay.Text != null ? relay.Text.Trim() : String.Empty;
        }

        private static string Header(Mobile actor, AIGMUMGOperationalLayoutEditSession session)
        {
            string name = actor != null ? Safe(actor.Name) : "No actor";
            string serial = actor != null ? String.Format("0x{0:X8}", actor.Serial.Value) : "missing";
            string mode = session != null && session.ArchitectMode ? "Architect Mode" : "Operator Mode";
            return String.Format("UMG Sleeve Organizer - {0} {1} - {2}", name, serial, mode);
        }

        private void AddSummaryRow(int x, int y, string leftLabel, string leftValue, string rightLabel, string rightValue)
        {
            AddImageTiled(x, y, 138, SkillsGump.EntryHeight, SkillsGump.EntryGumpID);
            AddImageTiled(x + 142, y, 138, SkillsGump.EntryHeight, SkillsGump.EntryGumpID);
            AddLabelCropped(x + 4, y + 1, 58, SkillsGump.EntryHeight, DimHue, leftLabel);
            AddLabelCropped(x + 62, y + 1, 72, SkillsGump.EntryHeight, TextHue, Safe(leftValue));
            AddLabelCropped(x + 146, y + 1, 58, SkillsGump.EntryHeight, DimHue, rightLabel);
            AddLabelCropped(x + 204, y + 1, 72, SkillsGump.EntryHeight, TextHue, Safe(rightValue));
        }

        private void DrawStackNameEntry(int x, int y, AIGMUMGOperationalLayoutEditSession session)
        {
            string value = String.Empty;
            AIGMUMGOperationalNeoStack stack = session != null ? AIGMUMGOperationalLayoutService.FindStack(session.WorkingLayout, session.SelectedStackId) : null;
            if (stack != null && !stack.SystemDefined)
                value = stack.DisplayName;
            AddImageTiled(x, y, 208, SkillsGump.EntryHeight, SkillsGump.EntryGumpID);
            AddTextEntry(x + 6, y + 1, 196, SkillsGump.EntryHeight - 2, TextHue, TextEntryStackName, value);
        }

        private void DrawActionButton(int x, int y, int id, string label, int width)
        {
            AddButton(x, y, 4005, 4007, id, GumpButtonType.Reply, 0);
            AddLabelCropped(x + 32, y, width - 32, 20, TextHue, label);
        }

        private void DrawIconButton(int x, int y, int id, int graphic, string label)
        {
            AddButton(x, y + 2, graphic, graphic, id, GumpButtonType.Reply, 0);
            AddLabelCropped(x + 24, y, 34, 20, DimHue, label);
        }

        private void DrawPagedFooter(int page, int total, int pageCount)
        {
            AddLabelCropped(24, 456, 260, 18, DimHue, String.Format("Page {0}/{1}  Rows {2}", page + 1, pageCount, total));
            AddButton(312, 454, SkillsGump.PrevButtonID1, SkillsGump.PrevButtonID2, ButtonPrevPage, GumpButtonType.Reply, 0);
            AddButton(356, 454, SkillsGump.NextButtonID1, SkillsGump.NextButtonID2, ButtonNextPage, GumpButtonType.Reply, 0);
        }

        private static string BeginHtml(StringBuilder sb)
        {
            return "<BASEFONT COLOR=#FFFFFF>" + sb + "</BASEFONT>";
        }

        private static string Line(string label, string value)
        {
            return String.Format("<BASEFONT COLOR=#99CCFF>{0}</BASEFONT>: {1}<BR>", Utility.FixHtml(label ?? String.Empty), Utility.FixHtml(value ?? String.Empty));
        }

        private static string Safe(string value)
        {
            return value ?? String.Empty;
        }

        private static bool Contains(string value, string needle)
        {
            return value != null && needle != null && value.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string SafeAuthor(Mobile mobile)
        {
            return mobile != null && !String.IsNullOrWhiteSpace(mobile.Name) ? mobile.Name : "unknown";
        }

        private static void Resend(Mobile caller, Mobile actor, AIGMUMGOperationalLayoutEditSession session)
        {
            if (caller == null || actor == null || session == null)
                return;
            caller.CloseGump(typeof(AIGMUMGSleeveSelectorGump));
            caller.SendGump(new AIGMUMGSleeveSelectorGump(caller, actor, session.SessionId));
        }
    }
}
