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
            AIGMUMGSleeveAccessService.Initialize();

            CommandSystem.Register("umg", AccessLevel.GameMaster, OnOpen);
            CommandSystem.Register("umgcompose", AccessLevel.GameMaster, OnOpen);
            CommandSystem.Register("umgarchitect", AccessLevel.GameMaster, OnArchitect);
            CommandSystem.Register("umglibrary", AccessLevel.GameMaster, OnLibrary);
            CommandSystem.Register("umgassign", AccessLevel.GameMaster, OnAssign);
            CommandSystem.Register("umgpreview", AccessLevel.GameMaster, OnPreview);
            CommandSystem.Register("umgapprove", AccessLevel.GameMaster, OnApprove);
            CommandSystem.Register("umgreject", AccessLevel.GameMaster, OnReject);
            CommandSystem.Register("umgversions", AccessLevel.GameMaster, OnVersions);
            CommandSystem.Register("umgrollback", AccessLevel.GameMaster, OnRollback);
            CommandSystem.Register("umgcompare", AccessLevel.GameMaster, OnCompare);
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
            CommandSystem.Register("umgsuspend", AccessLevel.GameMaster, OnSuspend);
            CommandSystem.Register("umgresume", AccessLevel.GameMaster, OnResume);
            CommandSystem.Register("umgsleeve", AccessLevel.Player, OnSleeve);
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

        private static void OnArchitect(CommandEventArgs e)
        {
            OpenTab(e, 1);
        }

        private static void OnSleeve(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            AIGMUMGSleeveAccessService.OpenFromCommand(e.Mobile, Clean(e.ArgString));
        }

        private static void OnLibrary(CommandEventArgs e)
        {
            OpenTab(e, 3);
        }

        private static void OpenTab(CommandEventArgs e, int tab)
        {
            if (e == null || e.Mobile == null)
                return;

            string arg = Clean(e.ArgString);
            if (String.IsNullOrWhiteSpace(arg))
            {
                e.Mobile.SendMessage(68, "Usage: [{0} <actor>]", e.Command);
                e.Mobile.SendMessage(68, AIGMUMGRepository.BuildInventorySummary());
                return;
            }

            Mobile actor = ResolveSingleActor(arg);
            if (actor == null)
            {
                e.Mobile.SendMessage(38, "No live UMG actor matched '{0}'.", arg);
                return;
            }

            e.Mobile.CloseGump(typeof(AIGMUMGPanelGump));
            e.Mobile.SendGump(new AIGMUMGPanelGump(e.Mobile, actor, tab));
        }

        private static void OnAssign(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            string targetText;
            string rest;
            SplitFirst(Clean(e.ArgString), out targetText, out rest);
            string definitionText;
            string parameters;
            SplitFirst(rest, out definitionText, out parameters);
            if (String.IsNullOrWhiteSpace(targetText) || String.IsNullOrWhiteSpace(definitionText))
            {
                e.Mobile.SendMessage(68, "Usage: [umgassign <actor/group> <template> [param=value ...]]");
                return;
            }

            List<Mobile> targets = ResolveTargets(targetText);
            if (targets.Count == 0)
            {
                e.Mobile.SendMessage(38, "No live UMG targets matched '{0}'.", targetText);
                return;
            }

            for (int i = 0; i < targets.Count; i++)
                e.Mobile.SendMessage(68, AIGMUMGComposerService.AddDraft(targets[i], definitionText, AIGMUMGComposerService.ParseParameters(parameters), Author(e)));
        }

        private static void OnPreview(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            string targetText;
            string selectorText;
            SplitFirst(Clean(e.ArgString), out targetText, out selectorText);
            if (String.IsNullOrWhiteSpace(targetText))
            {
                e.Mobile.SendMessage(68, "Usage: [umgpreview <actor/group> [assignment/template]]");
                return;
            }

            List<Mobile> targets = ResolveTargets(targetText);
            if (targets.Count == 0)
            {
                e.Mobile.SendMessage(38, "No live UMG targets matched '{0}'.", targetText);
                return;
            }

            for (int i = 0; i < targets.Count; i++)
                e.Mobile.SendMessage(68, AIGMUMGComposerService.Preview(targets[i], selectorText));
        }

        private static void OnApprove(CommandEventArgs e)
        {
            ChangeAssignment(e, delegate (Mobile actor, string id) { return AIGMUMGComposerService.Approve(actor, id, Author(e)); }, "Usage: [umgapprove <actor> <assignment-id>]");
        }

        private static void OnReject(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            string targetText;
            string rest;
            SplitFirst(Clean(e.ArgString), out targetText, out rest);
            string id;
            string reason;
            SplitFirst(rest, out id, out reason);
            if (String.IsNullOrWhiteSpace(targetText) || String.IsNullOrWhiteSpace(id))
            {
                e.Mobile.SendMessage(68, "Usage: [umgreject <actor> <assignment-id> [reason]]");
                return;
            }

            Mobile actor = ResolveSingleActor(targetText);
            if (actor == null)
            {
                e.Mobile.SendMessage(38, "No live UMG actor matched '{0}'.", targetText);
                return;
            }

            e.Mobile.SendMessage(68, AIGMUMGComposerService.Reject(actor, id, Author(e), reason));
        }

        private static void OnSuspend(CommandEventArgs e)
        {
            ChangeAssignment(e, delegate (Mobile actor, string id) { return AIGMUMGComposerService.Suspend(actor, id, Author(e)); }, "Usage: [umgsuspend <actor> <assignment-id>]");
        }

        private static void OnResume(CommandEventArgs e)
        {
            ChangeAssignment(e, delegate (Mobile actor, string id) { return AIGMUMGComposerService.Resume(actor, id, Author(e)); }, "Usage: [umgresume <actor> <assignment-id>]");
        }

        private static void OnVersions(CommandEventArgs e)
        {
            ForEachTarget(e, delegate (Mobile actor)
            {
                e.Mobile.SendMessage(68, AIGMUMGComposerService.BuildVersionSummary(actor));
            });
        }

        private static void OnRollback(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            string targetText;
            string versionText;
            SplitFirst(Clean(e.ArgString), out targetText, out versionText);
            if (String.IsNullOrWhiteSpace(targetText) || String.IsNullOrWhiteSpace(versionText))
            {
                e.Mobile.SendMessage(68, "Usage: [umgrollback <actor> <version-id>]");
                return;
            }

            Mobile actor = ResolveSingleActor(targetText);
            if (actor == null)
            {
                e.Mobile.SendMessage(38, "No live UMG actor matched '{0}'.", targetText);
                return;
            }

            e.Mobile.SendMessage(68, AIGMUMGComposerService.Rollback(actor, versionText, Author(e)));
        }

        private static void OnCompare(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            string targetText;
            string selectorText;
            SplitFirst(Clean(e.ArgString), out targetText, out selectorText);
            if (String.IsNullOrWhiteSpace(targetText) || String.IsNullOrWhiteSpace(selectorText))
            {
                e.Mobile.SendMessage(68, "Usage: [umgcompare <actor/group> <assignment>]");
                return;
            }

            ForEachResolvedTarget(e, targetText, delegate (Mobile actor)
            {
                e.Mobile.SendMessage(68, AIGMUMGComposerService.Compare(actor, selectorText));
            });
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

            Mobile actor = ResolveSingleActor(actorText);
            if (actor != null && proposalText.IndexOf("defensive", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                e.Mobile.SendMessage(68, AIGMUMGComposerService.ForkDefinition("Warrior Priest", "Dardalion Defensive Support", "agent"));
                e.Mobile.SendMessage(68, AIGMUMGComposerService.AddDraft(actor, "Dardalion Defensive Support", AIGMUMGComposerService.ParseParameters("mana_reserve_pct=50 heal_ally_below_pct=45 protect_group=Heroes"), "agent"));
                return;
            }

            e.Mobile.SendMessage(68, AIGMUMGRuntimeService.CreateAgentProposal(actorId, proposalText));
        }

        private static void OnDeferredMutationCommand(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            e.Mobile.SendMessage(38, AIGMUMGPhase64C2Invariant.DisabledReason);
        }

        private static void ChangeAssignment(CommandEventArgs e, Func<Mobile, string, string> action, string usage)
        {
            if (e == null || e.Mobile == null || action == null)
                return;

            string targetText;
            string idText;
            SplitFirst(Clean(e.ArgString), out targetText, out idText);
            if (String.IsNullOrWhiteSpace(targetText) || String.IsNullOrWhiteSpace(idText))
            {
                e.Mobile.SendMessage(68, usage);
                return;
            }

            Mobile actor = ResolveSingleActor(targetText);
            if (actor == null)
            {
                e.Mobile.SendMessage(38, "No live UMG actor matched '{0}'.", targetText);
                return;
            }

            e.Mobile.SendMessage(68, action(actor, idText));
        }

        private static void ForEachResolvedTarget(CommandEventArgs e, string selector, Action<Mobile> action)
        {
            List<Mobile> targets = ResolveTargets(selector);
            if (targets.Count == 0)
            {
                e.Mobile.SendMessage(38, "No live UMG target matched '{0}'.", selector);
                return;
            }

            for (int i = 0; i < targets.Count; i++)
                action(targets[i]);
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

        private static string Author(CommandEventArgs e)
        {
            return e != null && e.Mobile != null ? (e.Mobile.Name ?? e.Mobile.AccessLevel.ToString()) : "unknown";
        }

        private static string NormalizeSelector(string value)
        {
            return (value ?? String.Empty).Trim().ToLowerInvariant().Replace(" ", "_").Replace("-", "_");
        }
    }
}
