using System;
using System.Collections.Generic;
using Server.Custom.AIGM;
using Server.Custom.AIGM.Tasks;
using Server.Custom.AIGM.UMG;
using Server.Gumps;
using Server.Mobiles;

namespace Server.Commands
{
    public static class AIGMUMGCommand
    {
        public static void Initialize()
        {
            CommandSystem.Register("umg", AccessLevel.GameMaster, OnOpen);
            CommandSystem.Register("umgstatus", AccessLevel.GameMaster, OnStatus);
            CommandSystem.Register("umgblocks", AccessLevel.GameMaster, OnBlocks);
            CommandSystem.Register("umgwhy", AccessLevel.GameMaster, OnWhy);
            CommandSystem.Register("umgtrace", AccessLevel.GameMaster, OnTrace);
            CommandSystem.Register("umgreload", AccessLevel.GameMaster, OnReload);
            CommandSystem.Register("umgvalidate", AccessLevel.GameMaster, OnValidate);
            CommandSystem.Register("umgtemplate", AccessLevel.GameMaster, OnTemplate);
            CommandSystem.Register("umgproposal", AccessLevel.GameMaster, OnProposal);
            CommandSystem.Register("umgactivate", AccessLevel.GameMaster, OnDeferredMutationCommand);
            CommandSystem.Register("umgdeactivate", AccessLevel.GameMaster, OnDeferredMutationCommand);
            CommandSystem.Register("umgsuspend", AccessLevel.GameMaster, OnDeferredMutationCommand);
            CommandSystem.Register("umgresume", AccessLevel.GameMaster, OnDeferredMutationCommand);
        }

        private static void OnOpen(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            string arg = Clean(e.ArgString);
            if (String.IsNullOrWhiteSpace(arg))
            {
                e.Mobile.SendMessage(68, "Usage: [umg <actor>] | [umgstatus <actor/group>] | [umgtemplate <actor/group> <template>]");
                e.Mobile.SendMessage(68, AIGMUMGRepository.BuildInventorySummary());
                e.Mobile.SendMessage(68, AIGMUMGRuntimeService.BuildTemplateList());
                return;
            }

            Mobile actor = ResolveSingleActor(arg);
            if (actor == null)
            {
                e.Mobile.SendMessage(38, "No live UMG actor matched '{0}'.", arg);
                return;
            }

            e.Mobile.CloseGump(typeof(AIGMUMGPanelGump));
            e.Mobile.SendGump(new AIGMUMGPanelGump(e.Mobile, actor, 0));
        }

        private static void OnStatus(CommandEventArgs e)
        {
            ForEachTarget(e, delegate (Mobile actor)
            {
                e.Mobile.SendMessage(68, AIGMUMGRuntimeService.BuildStatus(actor));
            });
        }

        private static void OnBlocks(CommandEventArgs e)
        {
            ForEachTarget(e, delegate (Mobile actor)
            {
                e.Mobile.SendMessage(68, AIGMUMGRuntimeService.BuildBlockList(actor));
            });
        }

        private static void OnWhy(CommandEventArgs e)
        {
            ForEachTarget(e, delegate (Mobile actor)
            {
                e.Mobile.SendMessage(68, AIGMUMGRuntimeService.BuildWhy(actor));
            });
        }

        private static void OnTrace(CommandEventArgs e)
        {
            ForEachTarget(e, delegate (Mobile actor)
            {
                e.Mobile.SendMessage(68, AIGMUMGRuntimeService.BuildTraceList(actor));
            });
        }

        private static void OnValidate(CommandEventArgs e)
        {
            ForEachTarget(e, delegate (Mobile actor)
            {
                AIGMCapabilitySnapshot snapshot = AIGMCapabilityRegistry.CreateSnapshot(actor);
                AIGMUMGSleeve sleeve = AIGMUMGRepository.GetSleeve(snapshot.ActorId);
                if (sleeve == null)
                {
                    e.Mobile.SendMessage(38, "{0}: no sleeve.", actor.Name);
                    return;
                }

                AIGMCapabilityValidationResult result = AIGMUMGRuntimeService.ValidateActiveBlocks(actor, sleeve);
                e.Mobile.SendMessage(result.IsValid ? 68 : 38, "{0}: {1}", actor.Name, result.BuildSummary());
            });
        }

        private static void OnReload(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            AIGMUMGRepository.Reload();
            e.Mobile.SendMessage(68, "UMG data reloaded. {0}", AIGMUMGRepository.BuildInventorySummary());
        }

        private static void OnTemplate(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            string targetText;
            string templateText;
            SplitFirst(Clean(e.ArgString), out targetText, out templateText);
            if (String.IsNullOrWhiteSpace(targetText) || String.IsNullOrWhiteSpace(templateText))
            {
                e.Mobile.SendMessage(68, "Usage: [umgtemplate <actor/group> <template>]");
                e.Mobile.SendMessage(68, AIGMUMGRuntimeService.BuildTemplateList());
                return;
            }

            List<Mobile> targets = ResolveTargets(targetText);
            if (targets.Count == 0)
            {
                e.Mobile.SendMessage(38, "No live UMG targets matched '{0}'.", targetText);
                return;
            }

            for (int i = 0; i < targets.Count; i++)
                e.Mobile.SendMessage(68, AIGMUMGRuntimeService.PreviewTemplate(targets[i], templateText));
        }

        private static void OnProposal(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            string actorText;
            string proposalText;
            SplitFirst(Clean(e.ArgString), out actorText, out proposalText);
            if (String.IsNullOrWhiteSpace(actorText))
            {
                e.Mobile.SendMessage(68, "Usage: [umgproposal <actorId> [mana]");
                return;
            }

            string actorId = AIGMUMGRuntimeService.ResolveActorId(ResolveSingleActor(actorText));
            if (String.IsNullOrWhiteSpace(actorId))
                actorId = actorText.Trim().ToLowerInvariant();

            e.Mobile.SendMessage(68, AIGMUMGRuntimeService.CreateAgentProposal(actorId, proposalText));
        }

        private static void OnDeferredMutationCommand(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            e.Mobile.SendMessage(38, "UMG mutation command is registered but deferred in Phase64C Gate 1-3. Use [umgtemplate] and [umgproposal] for preview/draft; activation requires later explicit approval workflow.");
        }

        private static void ForEachTarget(CommandEventArgs e, Action<Mobile> action)
        {
            if (e == null || e.Mobile == null || action == null)
                return;

            string arg = Clean(e.ArgString);
            if (String.IsNullOrWhiteSpace(arg))
            {
                e.Mobile.SendMessage(68, "Usage: [{0} <actor/group>]", e.Command);
                return;
            }

            List<Mobile> targets = ResolveTargets(arg);
            if (targets.Count == 0)
            {
                e.Mobile.SendMessage(38, "No live UMG target matched '{0}'.", arg);
                return;
            }

            for (int i = 0; i < targets.Count; i++)
                action(targets[i]);
        }

        private static List<Mobile> ResolveTargets(string selector)
        {
            List<Mobile> targets = new List<Mobile>();
            if (String.IsNullOrWhiteSpace(selector))
                return targets;

            string[] groupIds = ToArray(AIGMRosterFactionService.GetGroupCharacterIds(selector));
            if (groupIds.Length > 0)
            {
                foreach (Mobile mobile in World.Mobiles.Values)
                {
                    if (mobile == null || mobile.Deleted)
                        continue;

                    string id = AIGMUMGRuntimeService.ResolveActorId(mobile);
                    for (int i = 0; i < groupIds.Length; i++)
                    {
                        if (String.Equals(id, groupIds[i], StringComparison.OrdinalIgnoreCase))
                        {
                            targets.Add(mobile);
                            break;
                        }
                    }
                }

                return targets;
            }

            Mobile single = ResolveSingleActor(selector);
            if (single != null)
                targets.Add(single);
            return targets;
        }

        private static Mobile ResolveSingleActor(string selector)
        {
            if (String.IsNullOrWhiteSpace(selector))
                return null;

            string needle = NormalizeSelector(selector);
            Mobile best = null;
            foreach (Mobile mobile in World.Mobiles.Values)
            {
                if (mobile == null || mobile.Deleted)
                    continue;

                if (!(mobile is IAIGMRosterTaskAgent) && !(mobile is IAIGMCompanionActor))
                    continue;

                string actorId = AIGMUMGRuntimeService.ResolveActorId(mobile);
                string name = NormalizeSelector(mobile.Name);
                string typeName = NormalizeSelector(mobile.GetType().Name);

                if (String.Equals(actorId, needle, StringComparison.OrdinalIgnoreCase)
                    || String.Equals(name, needle, StringComparison.OrdinalIgnoreCase)
                    || String.Equals(typeName, needle, StringComparison.OrdinalIgnoreCase))
                    return mobile;

                if (best == null
                    && (actorId.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0
                        || name.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0))
                    best = mobile;
            }

            return best;
        }

        private static void SplitFirst(string text, out string first, out string rest)
        {
            first = String.Empty;
            rest = String.Empty;
            if (String.IsNullOrWhiteSpace(text))
                return;

            string trimmed = text.Trim();
            int space = trimmed.IndexOf(' ');
            if (space < 0)
            {
                first = trimmed;
                return;
            }

            first = trimmed.Substring(0, space).Trim();
            rest = trimmed.Substring(space + 1).Trim();
        }

        private static string[] ToArray(IEnumerable<string> values)
        {
            if (values == null)
                return new string[0];

            List<string> list = new List<string>();
            foreach (string value in values)
            {
                if (!String.IsNullOrWhiteSpace(value))
                    list.Add(value.Trim().ToLowerInvariant());
            }

            return list.ToArray();
        }

        private static string Clean(string value)
        {
            return value != null ? value.Trim() : String.Empty;
        }

        private static string NormalizeSelector(string value)
        {
            return (value ?? String.Empty).Trim().ToLowerInvariant().Replace(" ", "_").Replace("-", "_");
        }
    }
}
