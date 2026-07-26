using System;
using System.Collections.Generic;
using Server.Custom.AIGM;
using Server.Custom.AIGM.Characters.Waylander;
using Server.Custom.AIGM.Tasks;
using Server.Custom.AIGM.UMG;
using Server.Mobiles;

namespace Server.Commands
{
    public static class AIGMPhase64C1DExactCleanupCommand
    {
        private static bool _serverStartedHooked;

        private sealed class CleanupTarget
        {
            public int SerialValue;
            public Type RequiredType;
            public string Reason;
        }

        private static readonly Dictionary<int, CleanupTarget> AllowedTargets = new Dictionary<int, CleanupTarget>
        {
            { 0x0000507E, new CleanupTarget { SerialValue = 0x0000507E, RequiredType = typeof(WaylanderDarkBrotherhoodKnight), Reason = "Phase64C1C accidental direct-alias test spawn" } },
            { 0x0000512D, new CleanupTarget { SerialValue = 0x0000512D, RequiredType = typeof(WaylanderJoining), Reason = "Phase64C1C accidental direct-alias test spawn" } },
            { 0x0000529D, new CleanupTarget { SerialValue = 0x0000529D, RequiredType = typeof(WaylanderUstarte), Reason = "Phase64C1C accidental direct-alias test spawn" } }
        };

        public static void Initialize()
        {
            CommandSystem.Register("AIGMPhase64C1DExactCleanup", AccessLevel.Administrator, OnCommand);
            CommandSystem.Register("C1D", AccessLevel.Administrator, OnCommand);
            CommandSystem.Register("C1DR", AccessLevel.Administrator, OnResumeProofCommand);
            CommandSystem.Register("C1DP", AccessLevel.Administrator, OnProofCommand);

            if (!_serverStartedHooked)
            {
                _serverStartedHooked = true;
                EventSink.ServerStarted += OnServerStarted;
            }
        }

        private static void OnServerStarted()
        {
            Timer.DelayCall(TimeSpan.FromSeconds(2.0), WritePostSaveRestartProof);
        }

        private static void OnCommand(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            string[] tokens = SplitArgs(e.ArgString);
            if (tokens.Length == 0)
            {
                e.Mobile.SendMessage(38, "Usage: [AIGMPhase64C1DExactCleanup 0x0000507E 0x0000512D 0x0000529D");
                return;
            }

            int removed = 0;
            for (int i = 0; i < tokens.Length; i++)
            {
                int serialValue;
                if (!TryParseSerial(tokens[i], out serialValue))
                {
                    e.Mobile.SendMessage(38, "Refused invalid serial '{0}'.", tokens[i]);
                    continue;
                }

                CleanupTarget target;
                if (!AllowedTargets.TryGetValue(serialValue, out target))
                {
                    e.Mobile.SendMessage(38, "Refused unknown serial 0x{0:X8}.", serialValue);
                    continue;
                }

                Mobile mobile = World.FindMobile(serialValue);
                if (mobile == null || mobile.Deleted)
                {
                    e.Mobile.SendMessage(68, "Serial 0x{0:X8} is already absent.", serialValue);
                    AIGMExecutionLog.Write("PHASE64C1D_EXACT_CLEANUP serial=0x{0:X8} result=already_absent caller={1}", serialValue, Safe(e.Mobile.Name));
                    continue;
                }

                if (mobile.GetType() != target.RequiredType)
                {
                    e.Mobile.SendMessage(38, "Refused serial 0x{0:X8}: expected {1}, found {2}.", serialValue, target.RequiredType.Name, mobile.GetType().Name);
                    AIGMExecutionLog.Write("PHASE64C1D_EXACT_CLEANUP serial=0x{0:X8} result=refused_type expected={1} actual={2} caller={3}",
                        serialValue,
                        target.RequiredType.Name,
                        mobile.GetType().Name,
                        Safe(e.Mobile.Name));
                    continue;
                }

                string name = mobile.Name ?? mobile.GetType().Name;
                string map = mobile.Map != null ? mobile.Map.Name : "null";
                int x = mobile.X;
                int y = mobile.Y;
                int z = mobile.Z;
                mobile.Delete();
                removed++;
                e.Mobile.SendMessage(68, "Removed {0}[0x{1:X8}] at {2} {3},{4},{5}.", name, serialValue, map, x, y, z);
                AIGMExecutionLog.Write("PHASE64C1D_EXACT_CLEANUP serial=0x{0:X8} result=deleted type={1} name={2} map={3} x={4} y={5} z={6} reason={7} caller={8}",
                    serialValue,
                    target.RequiredType.Name,
                    Safe(name),
                    Safe(map),
                    x,
                    y,
                    z,
                    Safe(target.Reason),
                    Safe(e.Mobile.Name));
            }

            e.Mobile.SendMessage(68, "Phase64C1D exact cleanup complete; removed {0} mobile(s).", removed);
        }

        private static void OnResumeProofCommand(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            int released = 0;
            foreach (Mobile mobile in World.Mobiles.Values)
            {
                if (mobile == null || mobile.Deleted || !mobile.Alive)
                    continue;

                if (mobile is AIGMCompanionDanyal || mobile is WaylanderDruss)
                {
                    AIGMOperationalControlService.Release(mobile, e.Mobile, "Phase64C1D exact actor resume proof");
                    released++;
                }
            }

            AIGMExecutionLog.Write("PHASE64C1D_RESUME_PROOF released={0} caller={1}", released, Safe(e.Mobile.Name));
            e.Mobile.SendMessage(68, "Phase64C1D resume proof released {0} exact actor(s).", released);
            WriteProofSnapshot(e.Mobile);
        }

        private static void OnProofCommand(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            WriteProofSnapshot(e.Mobile);
            e.Mobile.SendMessage(68, "Phase64C1D proof snapshot written.");
        }

        private static void WriteProofSnapshot(Mobile caller)
        {
            AIGMExecutionLog.Write("PHASE64C1D_PROOF_SNAPSHOT_BEGIN caller={0}", Safe(caller != null ? caller.Name : String.Empty));
            LogSerialState(0x0000507E, typeof(WaylanderDarkBrotherhoodKnight));
            LogSerialState(0x0000512D, typeof(WaylanderJoining));
            LogSerialState(0x0000529D, typeof(WaylanderUstarte));

            int danyal = 0;
            int druss = 0;
            int darkBrotherhood = 0;
            int joining = 0;
            int ustarte = 0;

            foreach (Mobile mobile in World.Mobiles.Values)
            {
                if (mobile == null || mobile.Deleted)
                    continue;

                if (mobile is AIGMCompanionDanyal)
                {
                    danyal++;
                    LogOperationalActor("Danyal", mobile);
                }
                else if (mobile is WaylanderDruss)
                {
                    druss++;
                    LogOperationalActor("Druss", mobile);
                }

                if (mobile is WaylanderDarkBrotherhoodKnight)
                {
                    darkBrotherhood++;
                    LogRosterActor("WaylanderDarkBrotherhoodKnight", mobile);
                }
                else if (mobile is WaylanderJoining)
                {
                    joining++;
                    LogRosterActor("WaylanderJoining", mobile);
                }
                else if (mobile is WaylanderUstarte)
                {
                    ustarte++;
                    LogRosterActor("WaylanderUstarte", mobile);
                }
            }

            AIGMExecutionLog.Write(
                "PHASE64C1D_PROOF_COUNTS danyal={0} druss={1} darkBrotherhoodKnight={2} joining={3} ustarte={4}",
                danyal,
                druss,
                darkBrotherhood,
                joining,
                ustarte);
            AIGMExecutionLog.Write("PHASE64C1D_PROOF_SNAPSHOT_END");
        }

        private static void WritePostSaveRestartProof()
        {
            AIGMExecutionLog.Write("PHASE64C1E_POST_SAVE_RESTART_PROOF_BEGIN");
            WriteProofSnapshot(null);
            WriteProposalCountProof();
            AIGMExecutionLog.Write("PHASE64C1E_POST_SAVE_RESTART_PROOF_END");
        }

        private static void WriteProposalCountProof()
        {
            int totalProposals = 0;
            int preserveEmergencyManaRows = 0;
            int structuralDraftRows = 0;
            int activeSleeveBlocks = 0;

            List<AIGMUMGBlockProposal> proposals = AIGMUMGRepository.GetProposals();
            totalProposals = proposals.Count;

            for (int i = 0; i < proposals.Count; i++)
            {
                AIGMUMGBlockProposal proposal = proposals[i];
                if (proposal == null || !String.Equals(proposal.BlockName, "PreserveEmergencyMana", StringComparison.OrdinalIgnoreCase))
                    continue;

                preserveEmergencyManaRows++;

                if (String.Equals(NormalizeId(proposal.TargetActorOrGroup), "dardalion", StringComparison.OrdinalIgnoreCase)
                    && (String.IsNullOrWhiteSpace(proposal.Status) || proposal.Status.IndexOf("Draft", StringComparison.OrdinalIgnoreCase) >= 0)
                    && proposal.Block != null
                    && proposal.Block.BlockState == AIGMUMGBlockState.Draft
                    && proposal.Block.Source == AIGMUMGBlockSource.AgentProposal)
                {
                    structuralDraftRows++;
                }
            }

            AIGMUMGSleeve sleeve = AIGMUMGRepository.GetSleeve("dardalion");
            if (sleeve != null)
                activeSleeveBlocks = CountActiveBlocksByName(sleeve, "PreserveEmergencyMana");

            AIGMExecutionLog.Write(
                "PHASE64C1E_PROPOSAL_COUNT target=dardalion block=PreserveEmergencyMana liveProposalRows={0} structuralDraftRows={1} activeSleeveBlocks={2} totalProposalRows={3} backupFilesExcluded=True runtimeProposalFile=Data/AIGM/UMG/Proposals/agent_proposals.json verdict={4}",
                preserveEmergencyManaRows,
                structuralDraftRows,
                activeSleeveBlocks,
                totalProposals,
                preserveEmergencyManaRows == 1 && structuralDraftRows == 1 && activeSleeveBlocks == 0 ? "reconciled" : "mismatch");
        }

        private static int CountActiveBlocksByName(AIGMUMGSleeve sleeve, string blockName)
        {
            int count = 0;
            if (sleeve == null)
                return count;

            for (int i = 0; i < sleeve.NeoStacks.Count; i++)
            {
                AIGMUMGNeoStack neoStack = sleeve.NeoStacks[i];
                if (neoStack == null)
                    continue;

                for (int j = 0; j < neoStack.NeoBlocks.Count; j++)
                {
                    AIGMUMGNeoBlock neoBlock = neoStack.NeoBlocks[j];
                    if (neoBlock == null)
                        continue;

                    for (int k = 0; k < neoBlock.BlockStacks.Count; k++)
                    {
                        AIGMUMGBlockStack blockStack = neoBlock.BlockStacks[k];
                        if (blockStack == null)
                            continue;

                        for (int l = 0; l < blockStack.MoltBlocks.Count; l++)
                        {
                            AIGMUMGBlock block = blockStack.MoltBlocks[l];
                            if (block != null
                                && block.IsActiveNow()
                                && String.Equals(block.Name, blockName, StringComparison.OrdinalIgnoreCase))
                            {
                                count++;
                            }
                        }
                    }
                }
            }

            return count;
        }

        private static void LogSerialState(int serialValue, Type expectedType)
        {
            Mobile mobile = World.FindMobile(serialValue);
            if (mobile == null || mobile.Deleted)
            {
                AIGMExecutionLog.Write("PHASE64C1D_SERIAL_STATE serial=0x{0:X8} expected={1} state=absent", serialValue, expectedType.Name);
                return;
            }

            AIGMExecutionLog.Write(
                "PHASE64C1D_SERIAL_STATE serial=0x{0:X8} expected={1} actual={2} name={3} map={4} x={5} y={6} z={7} controlled={8} controlMaster={9}",
                serialValue,
                expectedType.Name,
                mobile.GetType().Name,
                Safe(mobile.Name),
                Safe(mobile.Map != null ? mobile.Map.Name : "null"),
                mobile.X,
                mobile.Y,
                mobile.Z,
                mobile is BaseCreature creature && creature.Controlled,
                Describe((mobile as BaseCreature)?.ControlMaster));
        }

        private static void LogOperationalActor(string label, Mobile mobile)
        {
            AIGMExecutionLog.Write(
                "PHASE64C1D_OPERATIONAL_STATE label={0} mobile={1} type={2} map={3} x={4} y={5} z={6} alive={7} controlled={8} controlMaster={9} state={10}",
                label,
                Describe(mobile),
                mobile.GetType().Name,
                Safe(mobile.Map != null ? mobile.Map.Name : "null"),
                mobile.X,
                mobile.Y,
                mobile.Z,
                mobile.Alive,
                mobile is BaseCreature creature && creature.Controlled,
                Describe((mobile as BaseCreature)?.ControlMaster),
                Safe(AIGMOperationalControlService.DescribeState(mobile)));
        }

        private static void LogRosterActor(string label, Mobile mobile)
        {
            IAIGMRosterTaskAgent agent = mobile as IAIGMRosterTaskAgent;
            AIGMRosterTaskState state = agent != null ? agent.RosterTaskState : null;
            AIGMExecutionLog.Write(
                "PHASE64C1D_ROSTER_STATE label={0} mobile={1} type={2} map={3} x={4} y={5} z={6} alive={7} bound={8} trustedCommander=0x{9:X8} operational={10} task={11}",
                label,
                Describe(mobile),
                mobile.GetType().Name,
                Safe(mobile.Map != null ? mobile.Map.Name : "null"),
                mobile.X,
                mobile.Y,
                mobile.Z,
                mobile.Alive,
                agent != null && agent.RosterBoundCompanion,
                agent != null ? agent.RosterTrustedCommanderSerial.Value : -1,
                state != null ? state.OperationalMode.ToString() : "no_state",
                state != null ? Safe(state.TaskStatus) : "no_state");
        }

        private static bool TryParseSerial(string raw, out int serial)
        {
            serial = 0;
            if (String.IsNullOrWhiteSpace(raw))
                return false;

            string text = raw.Trim();
            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return Int32.TryParse(text.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out serial);

            if (text.IndexOfAny(new[] { 'a', 'b', 'c', 'd', 'e', 'f', 'A', 'B', 'C', 'D', 'E', 'F' }) >= 0)
                return Int32.TryParse(text, System.Globalization.NumberStyles.HexNumber, null, out serial);

            return Int32.TryParse(text, out serial);
        }

        private static string[] SplitArgs(string args)
        {
            return String.IsNullOrWhiteSpace(args)
                ? Array.Empty<string>()
                : args.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static string Safe(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return String.Empty;

            string text = value.Replace("\r", " ").Replace("\n", " ").Replace("\"", "'");
            return text.Length > 120 ? text.Substring(0, 120) : text;
        }

        private static string NormalizeId(string value)
        {
            return String.IsNullOrWhiteSpace(value)
                ? String.Empty
                : value.Trim().ToLowerInvariant().Replace(" ", String.Empty).Replace("_", String.Empty).Replace("-", String.Empty);
        }

        private static string Describe(Mobile mobile)
        {
            return mobile == null ? "none" : String.Format("{0}[0x{1:X8}]", Safe(mobile.Name ?? mobile.GetType().Name), mobile.Serial.Value);
        }
    }
}
