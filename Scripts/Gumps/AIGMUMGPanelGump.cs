using System;
using System.Collections.Generic;
using System.Text;
using Server.Custom.AIGM.UMG;
using Server.Network;

namespace Server.Gumps
{
    public class AIGMUMGPanelGump : Gump
    {
        private readonly Mobile m_From;
        private readonly Serial m_TargetSerial;
        private readonly int m_Tab;

        public AIGMUMGPanelGump(Mobile from, Mobile target, int tab)
            : base(70, 60)
        {
            m_From = from;
            m_TargetSerial = target != null ? target.Serial : Server.Serial.MinusOne;
            m_Tab = tab < 0 ? 0 : tab;

            Closable = true;
            Disposable = true;
            Dragable = true;

            AddPage(0);
            AddBackground(0, 0, 760, 560, 5054);
            AddImageTiled(12, 12, 736, 32, 2624);
            AddAlphaRegion(12, 12, 736, 32);
            AddHtml(22, 19, 710, 20, HeaderHtml(target), false, false);

            AddTabs();

            AddImageTiled(12, 86, 190, 420, 2624);
            AddAlphaRegion(12, 86, 190, 420);
            AddHtml(22, 96, 170, 400, LeftHtml(target), true, true);

            AddImageTiled(210, 86, 538, 420, 2624);
            AddAlphaRegion(210, 86, 538, 420);
            AddHtml(222, 96, 510, 400, BodyHtml(target), true, true);

            AddButton(150, 520, 4005, 4007, 5000, GumpButtonType.Reply, 0);
            AddHtml(184, 520, 92, 20, "<BASEFONT COLOR=#FFFFFF>Add Draft</BASEFONT>", false, false);
            AddButton(285, 520, 4005, 4007, 5001, GumpButtonType.Reply, 0);
            AddHtml(319, 520, 74, 20, "<BASEFONT COLOR=#FFFFFF>Preview</BASEFONT>", false, false);
            AddButton(410, 520, 4005, 4007, 5002, GumpButtonType.Reply, 0);
            AddHtml(444, 520, 74, 20, "<BASEFONT COLOR=#FFFFFF>Submit</BASEFONT>", false, false);

            AddButton(24, 520, 4005, 4007, 900, GumpButtonType.Reply, 0);
            AddHtml(58, 520, 70, 20, "<BASEFONT COLOR=#FFFFFF>Refresh</BASEFONT>", false, false);
            AddButton(610, 520, 4005, 4007, 0, GumpButtonType.Reply, 0);
            AddHtml(644, 520, 60, 20, "<BASEFONT COLOR=#FFFFFF>Close</BASEFONT>", false, false);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (sender == null || sender.Mobile == null || info == null)
                return;

            if (info.ButtonID == 0)
                return;

            Mobile target = World.FindMobile(m_TargetSerial);
            if (target == null || target.Deleted)
            {
                sender.Mobile.SendMessage(38, "UMG panel target is no longer available.");
                return;
            }

            if (info.ButtonID == 5000)
                sender.Mobile.SendMessage(68, AIGMUMGComposerService.AddDraft(target, "Frontline Defender", AIGMUMGComposerService.ParseParameters("pursuit_tiles=8 retreat_health_pct=25 protect_group=Heroes"), sender.Mobile.Name));
            else if (info.ButtonID == 5001)
                sender.Mobile.SendMessage(68, AIGMUMGComposerService.Preview(target, "Frontline Defender"));
            else if (info.ButtonID == 5002)
                sender.Mobile.SendMessage(68, "Submit recorded as Draft review step. Use [umgapprove <actor> <assignment-id>] for explicit approval.");

            int nextTab = info.ButtonID >= 100 && info.ButtonID < 200
                ? info.ButtonID - 100
                : m_Tab;

            sender.Mobile.CloseGump(typeof(AIGMUMGPanelGump));
            sender.Mobile.SendGump(new AIGMUMGPanelGump(sender.Mobile, target, nextTab));
        }

        private void AddTabs()
        {
            string[] tabs =
            {
                "Operator",
                "Architect",
                "Why",
                "Library",
                "Versions"
            };

            for (int i = 0; i < tabs.Length; i++)
            {
                int x = 16 + (i * 116);
                AddButton(x, 58, i == m_Tab ? 4006 : 4005, 4007, 100 + i, GumpButtonType.Reply, 0);
                AddHtml(x + 30, 58, 88, 20, String.Format("<BASEFONT COLOR=#FFFFFF>{0}</BASEFONT>", Utility.FixHtml(tabs[i])), false, false);
            }
        }

        private static string HeaderHtml(Mobile target)
        {
            string name = target != null ? (target.Name ?? target.GetType().Name) : "No target";
            return String.Format(
                "<BASEFONT COLOR=#FFFFFF><CENTER>UMGComposer - {0} - PREVIEW ONLY / NO GAMEPLAY EXECUTION</CENTER></BASEFONT>",
                Utility.FixHtml(name));
        }

        private static string LeftHtml(Mobile target)
        {
            if (target == null)
                return "<BASEFONT COLOR=#FF6666>No target.</BASEFONT>";

            AIGMCapabilitySnapshot snapshot = AIGMCapabilityRegistry.CreateSnapshot(target);
            AIGMUMGSleeve sleeve = AIGMUMGRepository.GetSleeve(snapshot.ActorId);
            List<AIGMUMGAssignment> assignments = AIGMUMGRepository.GetAssignmentsForTarget(snapshot.ActorId);
            StringBuilder sb = Begin();
            sb.Append(ColorLine(AIGMUMGMoltType.Primary, "Actor", snapshot.ActorName));
            sb.Append(Line("Autonomy", "Manual"));
            sb.Append(Line("PreviewOnly", "true"));
            sb.Append(Line("Dispatch", AIGMUMGPhase64C2Invariant.TacticalDispatchEnabled ? "enabled" : "disabled"));
            sb.Append("<BR>");
            if (sleeve != null)
            {
                for (int i = 0; i < sleeve.NeoStacks.Count; i++)
                {
                    AIGMUMGNeoStack stack = sleeve.NeoStacks[i];
                    sb.AppendFormat("<BASEFONT COLOR=#99CCFF>{0}</BASEFONT><BR>NeoBlocks:{1}<BR>MOLT:{2}<BR>Active:{3}<BR>Draft:{4}<BR><BR>",
                        Utility.FixHtml(stack.Name),
                        CountNeoBlocks(stack),
                        CountMoltBlocks(stack),
                        CountMoltBlocksByState(stack, AIGMUMGBlockState.Active),
                        CountMoltBlocksByState(stack, AIGMUMGBlockState.Draft));
                }
            }
            sb.Append(Line("Assignments", assignments.Count.ToString()));
            return End(sb);
        }

        private string BodyHtml(Mobile target)
        {
            if (target == null)
                return "<BASEFONT COLOR=#FF6666>No NPC selected.</BASEFONT>";

            AIGMCapabilitySnapshot snapshot = AIGMCapabilityRegistry.CreateSnapshot(target);
            AIGMUMGSleeve sleeve = AIGMUMGRepository.GetSleeve(snapshot.ActorId);

            switch (m_Tab)
            {
                case 1:
                    return ArchitectHtml(target, sleeve, snapshot);
                case 2:
                    return TraceHtml(target);
                case 3:
                    return LibraryHtml(target);
                case 4:
                    return VersionsHtml(target, sleeve);
                default:
                    return OperatorHtml(target, sleeve, snapshot);
            }
        }

        private static string OperatorHtml(Mobile target, AIGMUMGSleeve sleeve, AIGMCapabilitySnapshot snapshot)
        {
            StringBuilder sb = Begin();
            sb.Append(ColorLine(AIGMUMGMoltType.Primary, "Operator Mode", AIGMUMGComposerService.BuildOperatorSummary(target)));
            sb.Append(Line("Schema", AIGMUMGSleeve.CurrentSchemaName));
            sb.Append(Line("Quick Templates", "Recommended, Compatible, Draft, Approved Preview"));
            sb.Append(Line("Quick Parameters", "Aggression, Defense, Pursuit, Range, Retreat, Protect, Mana, Healing, Assistance"));
            sb.Append(Line("Actions", "Browse Recommended | Add as Draft | Edit Parameters | Preview | Compare | Submit | Approve | Reject | Suspend/Resume | Why | Versions"));
            sb.Append("<BR>");
            sb.Append(ColorLine(AIGMUMGMoltType.Instruction, "Invariant", AIGMUMGPhase64C2Invariant.ExecutionStatus));
            sb.Append(ColorLine(AIGMUMGMoltType.Directive, "Warnings", "Draft and Approved Preview doctrine cannot cause combat, movement, tracking, healing, spellcasting, looting, spawning, deletion, travel, tasks, targets, or operational-state changes."));
            return End(sb);
        }

        private static string ArchitectHtml(Mobile target, AIGMUMGSleeve sleeve, AIGMCapabilitySnapshot snapshot)
        {
            StringBuilder sb = Begin();
            sb.Append(ColorLine(AIGMUMGMoltType.Blueprint, "Architect Mode", AIGMUMGComposerService.BuildArchitectSummary(target)));
            sb.Append(Line("Surfaces", "Sleeve | NeoStacks | NeoBlocks | Block Stacks | MOLT Builder | Library | Assignments | Proposals | Conflicts | Capability | Runtime | Trace | Versions | Raw"));
            sb.Append(Line("Allowed Draft Edits", "create, clone/fork, edit MOLT prose, parameters, scope, priority, dependencies, conflicts, fallback, expiry"));
            sb.Append(Line("Protected", "SystemInvariant, generated Capability, canonical raw persona journals, Active governance, live runtime state"));
            sb.Append("<BR>");
            sb.Append(MoltHtml(sleeve));
            return End(sb);
        }

        private static string NeoStacksHtml(AIGMUMGSleeve sleeve)
        {
            StringBuilder sb = Begin();
            if (sleeve == null)
                return End(sb.Append("No sleeve loaded."));

            for (int i = 0; i < sleeve.NeoStacks.Count; i++)
            {
                AIGMUMGNeoStack stack = sleeve.NeoStacks[i];
                sb.AppendFormat("<BASEFONT COLOR=#99CCFF>{0}</BASEFONT><BR> enabled={1} priorityOrder={2}<BR> NeoBlocks: {3} | Block Stacks: {4} | MOLT Blocks: {5} | Active: {6} | Draft: {7}<BR><BR>",
                    Utility.FixHtml(stack.Name),
                    stack.Enabled,
                    stack.PriorityOrder,
                    CountNeoBlocks(stack),
                    CountBlockStacks(stack),
                    CountMoltBlocks(stack),
                    CountMoltBlocksByState(stack, AIGMUMGBlockState.Active),
                    CountMoltBlocksByState(stack, AIGMUMGBlockState.Draft));
            }

            return End(sb);
        }

        private static string MoltHtml(AIGMUMGSleeve sleeve)
        {
            StringBuilder sb = Begin();
            if (sleeve == null)
                return End(sb.Append("No sleeve loaded."));

            List<AIGMUMGBlock> blocks = AIGMUMGCompiler.FlattenOrderedBlocks(sleeve);
            if (blocks.Count == 0)
                sb.Append("No active MOLT blocks.<BR>");

            for (int i = 0; i < blocks.Count && i < 24; i++)
            {
                AIGMUMGBlock block = blocks[i];
                sb.Append(ColorLine(block.MoltType, block.Name, String.Format("{0} state={1} source={2} scope={3} priorityOrder={4}",
                    block.Summary,
                    block.BlockState,
                    block.Source,
                    block.Scope,
                    block.PriorityOrder)));
            }

            return End(sb);
        }

        private static string CapabilityHtml(AIGMCapabilitySnapshot snapshot)
        {
            StringBuilder sb = Begin();
            sb.Append(ColorLine(AIGMUMGMoltType.Primary, "Snapshot", AIGMCapabilityRegistry.BuildCompactSummary(snapshot)));
            sb.Append(Line("Weapon source", snapshot.ActiveWeaponSource));
            sb.Append(Line("Implementation item", snapshot.ImplementationItem));
            sb.Append(Line("Authoritative combat skill", snapshot.AuthoritativeCombatSkill));
            sb.Append(Line("Fallback combat skill", snapshot.FallbackCombatSkill));
            sb.Append(Line("Wrestling fallback", snapshot.WrestlingFallback ? "Yes" : "No"));
            sb.Append(Line("Weapon", snapshot.ActiveWeapon + " / " + snapshot.ActiveWeaponSkill));
            sb.Append(Line("Animation", snapshot.ActiveWeaponAnimation + " hit=" + snapshot.ActiveWeaponHitSound + " miss=" + snapshot.ActiveWeaponMissSound));
            sb.Append(Line("Capability status", "native combat=" + snapshot.NativeCombatStatus + "; tracking=" + snapshot.TrackingStatus + "; waypoint travel=" + snapshot.WaypointTravelStatus + "; autonomous task=" + snapshot.AutonomousTaskStatus + "; offensive spells=" + snapshot.OffensiveSpellcastingStatus));
            sb.Append(Line("Resources", "bandages=" + snapshot.BandageCount + " potions=" + snapshot.PotionCount + " reagents=" + snapshot.ReagentCount + " ammo=" + snapshot.AmmunitionCount + " spellbooks=" + snapshot.SpellbookCount));
            sb.Append("<BR><BASEFONT COLOR=#FFFFFF>Capabilities</BASEFONT><BR>");
            for (int i = 0; i < snapshot.CapabilityEvidence.Count; i++)
                sb.Append(Utility.FixHtml(snapshot.CapabilityEvidence[i])).Append("<BR>");

            if (snapshot.KnownDefectsOrUnsupportedServices.Count > 0)
            {
                sb.Append("<BR><BASEFONT COLOR=#FFCC99>Unsupported / defects</BASEFONT><BR>");
                for (int i = 0; i < snapshot.KnownDefectsOrUnsupportedServices.Count; i++)
                    sb.Append(Utility.FixHtml(snapshot.KnownDefectsOrUnsupportedServices[i])).Append("<BR>");
            }

            return End(sb);
        }

        private static string RuntimeHtml(Mobile target, AIGMUMGSleeve sleeve)
        {
            StringBuilder sb = Begin();
            sb.Append(Line("Status", AIGMUMGRuntimeService.BuildStatus(target)));
            sb.Append(Line("Adapters", AIGMUMGAdapterRegistry.DescribeConnectedAdapters()));
            sb.Append("<BR>");
            AIGMUMGDecisionTrace trace = AIGMUMGRuntimeService.CompileDryRun(target);
            sb.Append(ColorLine(AIGMUMGMoltType.Blueprint, "Preview", trace != null ? trace.BuildWhySummary() : "No trace."));
            return End(sb);
        }

        private static string TraceHtml(Mobile target)
        {
            StringBuilder sb = Begin();
            sb.Append(Line("Recent", AIGMUMGRuntimeService.BuildTraceList(target)));
            sb.Append("<BR>");
            sb.Append(ColorLine(AIGMUMGMoltType.Philosophy, "Why", AIGMUMGRuntimeService.BuildWhy(target)));
            return End(sb);
        }

        private static string LibraryHtml(Mobile target)
        {
            StringBuilder sb = Begin();
            sb.Append(ColorLine(AIGMUMGMoltType.Blueprint, "Library", AIGMUMGComposerService.BuildLibrarySummary(target)));
            sb.Append(Line("Filters", "Recommended | Compatible | CompatibleWithFallback | Degraded | Incompatible | Approved Preview | Draft | Identity | Combat | Defense | Healing | Spells | Movement | Tracking | Squad | Situational | Governance | MOLT | Source | Scope | Tag"));
            sb.Append("<BR>");
            sb.Append(ColorLine(AIGMUMGMoltType.Instruction, "Workflow", "Add as Draft -> Validate -> Preview -> Compare -> Submit -> Approve PreviewOnly."));
            return End(sb);
        }

        private static string VersionsHtml(Mobile target, AIGMUMGSleeve sleeve)
        {
            StringBuilder sb = Begin();
            sb.Append(Line("Schema", AIGMUMGSleeve.CurrentSchemaName));
            sb.Append(Line("Sleeve Version", sleeve != null ? sleeve.Version.ToString() : "missing"));
            sb.Append(Line("Migration Version", sleeve != null ? sleeve.MigrationVersion : "missing"));
            sb.Append(Line("Versions", AIGMUMGComposerService.BuildVersionSummary(target)));
            sb.Append(Line("Rollback", "Confirmed ChangeSet rollback restores assignment snapshots without rewriting history."));
            return End(sb);
        }

        private static string RawHtml(Mobile target, AIGMUMGSleeve sleeve, AIGMCapabilitySnapshot snapshot)
        {
            StringBuilder sb = Begin();
            sb.Append(Line("Actor", snapshot.ActorName + " " + snapshot.Serial));
            sb.Append(Line("Sleeve", sleeve != null ? sleeve.SleeveId : "missing"));
            sb.Append(Line("Profile", target.Profile != null ? Trim(target.Profile, 320) : "empty"));
            sb.Append("<BR>");
            sb.Append(ColorLine(AIGMUMGMoltType.Subject, "Advanced", "Raw JSON is stored outside the world save; this view is a summary, not the primary editor."));
            return End(sb);
        }

        private static StringBuilder Begin()
        {
            return new StringBuilder("<BASEFONT COLOR=#FFFFFF>");
        }

        private static string End(StringBuilder sb)
        {
            sb.Append("</BASEFONT>");
            return sb.ToString();
        }

        private static string Line(string label, string value)
        {
            return String.Format("<BASEFONT COLOR=#CCCCCC>{0}:</BASEFONT> {1}<BR>",
                Utility.FixHtml(label ?? String.Empty),
                Utility.FixHtml(value ?? String.Empty));
        }

        private static string ColorLine(AIGMUMGMoltType type, string label, string value)
        {
            AIGMUMGSemanticColor color = AIGMUMGSemanticColorRegistry.Get(type);
            return String.Format("<BASEFONT COLOR={0}>[{1}] {2}</BASEFONT> {3}<BR>",
                color.Hex,
                Utility.FixHtml(AIGMUMGSemanticColorRegistry.FormatLabel(type)),
                Utility.FixHtml(label ?? String.Empty),
                Utility.FixHtml(value ?? String.Empty));
        }

        private static int CountNeoBlocks(AIGMUMGNeoStack stack)
        {
            return stack != null && stack.NeoBlocks != null ? stack.NeoBlocks.Count : 0;
        }

        private static int CountBlockStacks(AIGMUMGNeoStack stack)
        {
            int count = 0;
            if (stack == null || stack.NeoBlocks == null)
                return count;

            for (int i = 0; i < stack.NeoBlocks.Count; i++)
            {
                AIGMUMGNeoBlock block = stack.NeoBlocks[i];
                if (block != null && block.BlockStacks != null)
                    count += block.BlockStacks.Count;
            }

            return count;
        }

        private static int CountMoltBlocks(AIGMUMGNeoStack stack)
        {
            return CountMoltBlocksByState(stack, null);
        }

        private static int CountMoltBlocksByState(AIGMUMGNeoStack stack, AIGMUMGBlockState? state)
        {
            int count = 0;
            if (stack == null || stack.NeoBlocks == null)
                return count;

            for (int i = 0; i < stack.NeoBlocks.Count; i++)
            {
                AIGMUMGNeoBlock block = stack.NeoBlocks[i];
                if (block == null || block.BlockStacks == null)
                    continue;

                for (int j = 0; j < block.BlockStacks.Count; j++)
                {
                    AIGMUMGBlockStack blockStack = block.BlockStacks[j];
                    if (blockStack == null || blockStack.MoltBlocks == null)
                        continue;

                    for (int k = 0; k < blockStack.MoltBlocks.Count; k++)
                    {
                        AIGMUMGBlock molt = blockStack.MoltBlocks[k];
                        if (molt != null && (!state.HasValue || molt.BlockState == state.Value))
                            count++;
                    }
                }
            }

            return count;
        }

        private static string Trim(string value, int max)
        {
            if (String.IsNullOrWhiteSpace(value))
                return String.Empty;

            string text = value.Trim().Replace("\r", " ").Replace("\n", " ");
            return text.Length <= max ? text : text.Substring(0, max) + "...";
        }
    }
}
