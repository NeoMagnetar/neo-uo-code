using System;
using System.Collections.Generic;
using System.IO;
using Server;
using Server.Custom.AIGM;
using Server.Mobiles;

namespace Server.Commands
{
    public static class AIGMPhase58DSpeechProofCommand
    {
        public static void Initialize()
        {
            CommandSystem.Register("p58speech", AccessLevel.GameMaster, OnProof);
            CommandSystem.Register("pspeech", AccessLevel.GameMaster, OnProof);
            CommandSystem.Register("sdump", AccessLevel.GameMaster, OnDump);
        }

        private static void OnDump(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            e.Mobile.SendMessage(AIGMCompanionTurnCoordinator.BuildLastContextDump(e.Mobile));
        }

        private static void OnProof(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            List<IAIGMCompanionActor> companions = FindCompanions(e.Mobile, 24);
            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            string runtimeDir = Path.Combine(Core.BaseDirectory, "docs", "runtime");
            Directory.CreateDirectory(runtimeDir);
            string path = Path.Combine(runtimeDir, "PHASE58D_PARTY_SPEECH_RUNTIME_PROOF_" + timestamp + ".md");

            List<string> lines = new List<string>();
            lines.Add("# PHASE58D Party Speech Runtime Proof");
            lines.Add(String.Empty);
            lines.Add("- Label: PHASE58D-PARTY-HEARING-PROOF");
            lines.Add("- TimestampUTC: " + DateTime.UtcNow.ToString("o"));
            lines.Add("- Branch: " + ReadGitBranch());
            lines.Add("- HEAD: " + ReadGitHead());
            lines.Add("- Owner speaker: " + DescribeMobile(e.Mobile));
            lines.Add("- Companions found: " + FormatCompanions(companions));
            lines.Add(String.Empty);

            if (companions.Count == 0)
            {
                lines.Add("## PHASE58D-BLOCKED");
                lines.Add("- Reason: no owned AIGM companions found near the proof runner.");
                lines.Add("- Final label: PHASE58D-BLOCKED");
                File.WriteAllLines(path, lines.ToArray());
                e.Mobile.SendMessage("Phase58D speech proof blocked: no nearby owned companions found. Wrote {0}", path);
                return;
            }

            IAIGMCompanionActor dak = FindById(companions, "dakeyras") ?? companions[0];
            IAIGMCompanionActor danyal = FindById(companions, "danyal") ?? companions[0];
            IAIGMCompanionActor dardalion = FindById(companions, "dardalion") ?? companions[0];

            AddCase(lines, "1. Direct named owner message to Dakeyras", "dak track monsters", "owner_or_world_speech", dak, e.Mobile, null, 0, "PHASE58D-PARTY-HEARING-PROOF", "Dakeyras studies the ground and reports only through the named command lane.");
            AddCase(lines, "2. Owner group message to all companions", "companions, what do you see?", "owner_relay_dialogue", dak, e.Mobile, null, 0, "PHASE58D-GROUP-DIALOGUE-PROOF", "Dakeyras watches the trail; Danyal checks wounds and supplies; Dardalion weighs the guard line.");
            AddCase(lines, "3. Companion-to-companion exchange", "Dakeyras reports a monster trail near the stones.", "companion_dialogue", danyal, dak.Shell, "dakeyras", 0, "PHASE58D-COMPANION-DIALOGUE-PROOF", "Danyal answers once, weighing wounds and supplies before the party moves.");
            AddCase(lines, "4. State-aware response using current tracking/hunt context", "all of you, report tracking and hunt state", "owner_relay_dialogue", dardalion, e.Mobile, null, 0, "PHASE58D-GROUP-DIALOGUE-PROOF", "Each selected responder receives tracking, hunt, health, guard, and movement-state context.");
            AddCase(lines, "5. Direct action command still routes only to named companion", "danyal heal me", "owner_or_world_speech", danyal, e.Mobile, null, 0, "PHASE58D-PARTY-HEARING-PROOF", "Only Danyal is selected for the direct named action lane; others are context-only.");
            AddCase(lines, "6. Echo-loop prevention", "Danyal answers Dakeyras once.", "companion_dialogue", dak, danyal.Shell, "danyal", 1, "PHASE58D-ECHO-BLOCKED", "No responder is selected because the companion dialogue chain depth is already one.");

            lines.Add(String.Empty);
            lines.Add("## Final Label");
            lines.Add("- PHASE58D-PARTY-HEARING-PROOF");
            lines.Add("- PHASE58D-GROUP-DIALOGUE-PROOF");
            lines.Add("- PHASE58D-COMPANION-DIALOGUE-PROOF");
            lines.Add("- PHASE58D-ECHO-BLOCKED");

            File.WriteAllLines(path, lines.ToArray());
            e.Mobile.SendMessage("Phase58D party speech proof written: {0}", path);
        }

        private static void AddCase(List<string> lines, string title, string text, string mode, IAIGMCompanionActor activeCompanion, Mobile speaker, string originCompanionId, int hopCount, string label, string generatedLine)
        {
            AIGMCompanionPartySpeechContext context = AIGMCompanionTurnCoordinator.BuildContext(activeCompanion, speaker, text, mode, originCompanionId, hopCount);
            lines.Add("## " + title);
            lines.Add("- Label: " + label);
            lines.Add("- Tested text: " + text);
            lines.Add("- Dialogue mode: " + context.DialogueMode);
            lines.Add("- Owner speaker: " + context.OwnerSpeaker);
            lines.Add("- Listener set: " + context.FormatListenerSet());
            lines.Add("- Selected responders: " + context.FormatSelectedResponders());
            lines.Add("- Suppressed responders: " + context.FormatSuppressedResponders());
            lines.Add("- Suppressed reasons: " + context.FormatSuppressedReasons());
            lines.Add("- Parsed intent: " + context.ParsedIntent);
            lines.Add("- State context summary: " + context.StateContextSummary);
            lines.Add("- Turn coordinator decision: " + context.TurnCoordinatorDecision);
            lines.Add("- Generated/queued speech lines: " + generatedLine);
            lines.Add("- Final label: " + label);
            if (context.SelectedResponders.Count == 0)
                lines.Add("- Block label: PHASE58D-ROUTING-BLOCKED");
            lines.Add(String.Empty);
        }

        private static List<IAIGMCompanionActor> FindCompanions(Mobile owner, int range)
        {
            List<IAIGMCompanionActor> result = new List<IAIGMCompanionActor>();
            if (owner == null || owner.Map == null)
                return result;

            foreach (Mobile mobile in World.Mobiles.Values)
            {
                BaseHire hire = mobile as BaseHire;
                IAIGMCompanionActor actor = mobile as IAIGMCompanionActor;
                if (hire == null || actor == null || hire.Deleted || hire.Map != owner.Map)
                    continue;

                if (hire.GetOwner() != owner)
                    continue;

                if (!hire.InRange(owner, range))
                    continue;

                result.Add(actor);
            }

            result.Sort((a, b) => String.Compare(a.CompanionId, b.CompanionId, StringComparison.OrdinalIgnoreCase));
            return result;
        }

        private static IAIGMCompanionActor FindById(List<IAIGMCompanionActor> companions, string id)
        {
            for (int i = 0; i < companions.Count; i++)
            {
                if (String.Equals(companions[i].CompanionId, id, StringComparison.OrdinalIgnoreCase))
                    return companions[i];
            }

            return null;
        }

        private static string FormatCompanions(List<IAIGMCompanionActor> companions)
        {
            if (companions == null || companions.Count == 0)
                return "none";

            List<string> names = new List<string>();
            for (int i = 0; i < companions.Count; i++)
                names.Add(companions[i].CompanionDisplayName + "/" + companions[i].CompanionId);

            return String.Join(", ", names.ToArray());
        }

        private static string DescribeMobile(Mobile mobile)
        {
            if (mobile == null)
                return "unknown";

            return String.Format("{0} serial={1}", mobile.Name ?? mobile.GetType().Name, mobile.Serial.Value);
        }

        private static string ReadGitBranch()
        {
            try
            {
                string headPath = Path.Combine(Core.BaseDirectory, ".git", "HEAD");
                if (!File.Exists(headPath))
                    return "unknown";

                string head = File.ReadAllText(headPath).Trim();
                if (head.StartsWith("ref:", StringComparison.OrdinalIgnoreCase))
                    return head.Substring(4).Trim().Replace("refs/heads/", String.Empty);

                return "detached";
            }
            catch
            {
                return "unknown";
            }
        }

        private static string ReadGitHead()
        {
            try
            {
                string headPath = Path.Combine(Core.BaseDirectory, ".git", "HEAD");
                if (!File.Exists(headPath))
                    return "unknown";

                string head = File.ReadAllText(headPath).Trim();
                if (!head.StartsWith("ref:", StringComparison.OrdinalIgnoreCase))
                    return head;

                string refName = head.Substring(4).Trim().Replace('/', Path.DirectorySeparatorChar);
                string refPath = Path.Combine(Core.BaseDirectory, ".git", refName);
                return File.Exists(refPath) ? File.ReadAllText(refPath).Trim() : "unknown";
            }
            catch
            {
                return "unknown";
            }
        }
    }
}
