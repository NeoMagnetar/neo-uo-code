using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Server.Accounting;
using Server.Custom.AIGM.Characters.Waylander;
using Server.Custom.AIGM.Inventory;
using Server.Custom.AIGM.UMG;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Commands
{
    public static class AIGMCompanionInventoryProofCommand
    {
        private const string StateFileName = "gate12_14_temp_fixture_state.txt";

        public static void Initialize()
        {
            CommandSystem.Register("umgpackproof", AccessLevel.GameMaster, OnCommand);
        }

        private static void OnCommand(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            string action = (e.ArgString ?? String.Empty).Trim().ToLowerInvariant();
            if (String.IsNullOrWhiteSpace(action))
                action = "setup";

            try
            {
                if (action == "setup")
                {
                    RunSetup(e.Mobile);
                    return;
                }

                if (action == "verify")
                {
                    RunVerify(e.Mobile);
                    return;
                }

                if (action == "cleanup")
                {
                    RunCleanup(e.Mobile);
                    return;
                }

                e.Mobile.SendMessage(38, "Usage: [umgpackproof setup|verify|cleanup]");
            }
            catch (Exception ex)
            {
                string report = WriteReport("error", "ERROR: " + ex);
                e.Mobile.SendMessage(38, "umgpackproof failed; report={0}", report);
            }
        }

        private static void RunSetup(Mobile gm)
        {
            if (File.Exists(GetStatePath()))
                throw new InvalidOperationException("existing proof fixture state is present; run [umgpackproof cleanup] before setup");

            int accountsBeforeStaleCleanup = Accounts.Count;
            int mobilesBeforeStaleCleanup = World.Mobiles.Count;
            int itemsBeforeStaleCleanup = World.Items.Count;
            int staleRemoved = CleanupStaleProofArtifacts();

            ProofContext ctx = new ProofContext();
            ctx.StartUtc = DateTime.UtcNow;
            ctx.AccountsBefore = Accounts.Count;
            ctx.MobilesBefore = World.Mobiles.Count;
            ctx.ItemsBefore = World.Items.Count;
            ctx.ReportLines.Add("# Phase64D1B-R1 Temporary Access And Death Proof Setup");
            ctx.ReportLines.Add("");
            ctx.ReportLines.Add("- GeneratedUtc: " + ctx.StartUtc.ToString("o", CultureInfo.InvariantCulture));
            ctx.ReportLines.Add("- GM: " + Describe(gm));
            ctx.ReportLines.Add("- StaleFixturesRemovedBeforeSetup: " + staleRemoved);
            ctx.ReportLines.Add("- AccountsBeforeStaleCleanup: " + accountsBeforeStaleCleanup);
            ctx.ReportLines.Add("- MobilesBeforeStaleCleanup: " + mobilesBeforeStaleCleanup);
            ctx.ReportLines.Add("- ItemsBeforeStaleCleanup: " + itemsBeforeStaleCleanup);
            ctx.ReportLines.Add("- AccountsBefore: " + ctx.AccountsBefore);
            ctx.ReportLines.Add("- MobilesBefore: " + ctx.MobilesBefore);
            ctx.ReportLines.Add("- ItemsBefore: " + ctx.ItemsBefore);

            Point3D origin = FindNearbyFit(gm.Map, gm.Location, 4);
            Point3D actorLoc = FindNearbyFit(gm.Map, new Point3D(origin.X + 1, origin.Y, origin.Z), 4);
            Point3D authLoc = FindNearbyFit(gm.Map, new Point3D(origin.X, origin.Y + 1, origin.Z), 4);
            Point3D unrelatedLoc = FindNearbyFit(gm.Map, new Point3D(origin.X + 2, origin.Y + 1, origin.Z), 4);

            string suffix = DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            Account authorizedAccount = new Account("phase64d1b_authorized_" + suffix, Guid.NewGuid().ToString("N"));
            Account unrelatedAccount = new Account("phase64d1b_unrelated_" + suffix, Guid.NewGuid().ToString("N"));

            PlayerMobile authorized = new PlayerMobile();
            authorized.Name = "PHASE64D1B Authorized Player";
            authorized.AccessLevel = AccessLevel.Player;
            authorizedAccount[0] = authorized;
            authorized.MoveToWorld(authLoc, gm.Map);

            PlayerMobile unrelated = new PlayerMobile();
            unrelated.Name = "PHASE64D1B Unrelated Player";
            unrelated.AccessLevel = AccessLevel.Player;
            unrelatedAccount[0] = unrelated;
            unrelated.MoveToWorld(unrelatedLoc, gm.Map);

            WaylanderJoining actor = new WaylanderJoining();
            actor.Name = "PHASE64D1B Temporary Joining";
            actor.MoveToWorld(actorLoc, gm.Map);
            bool controlSet = actor.SetControlMaster(authorized);
            actor.IsBonded = true;
            actor.BondingBegin = DateTime.MinValue;
            actor.ControlOrder = OrderType.Stay;
            actor.ControlTarget = null;

            AIGMCompanionInventoryResult ensured = AIGMCompanionInventoryService.EnsureBackpack(actor, "phase64d1b_r1_temp_proof_setup");
            Require(ensured.Accepted, "temporary ensure failed: " + ensured.ResultCode);
            AIGMCompanionBackpack pack = ensured.Backpack;
            Require(pack != null && !pack.Deleted, "temporary backpack missing after ensure");

            Dagger dagger = new Dagger();
            dagger.Name = "PHASE64D1B_PROOF_DAGGER";
            Bandage bandage = new Bandage(2);
            bandage.Name = "PHASE64D1B_PROOF_BANDAGE_STACK";
            pack.DropItem(dagger);
            pack.DropItem(bandage);
            pack.UpdateTotals();

            ctx.AuthorizedAccountName = authorizedAccount.Username;
            ctx.UnrelatedAccountName = unrelatedAccount.Username;
            ctx.AuthorizedPlayerSerial = authorized.Serial;
            ctx.UnrelatedPlayerSerial = unrelated.Serial;
            ctx.ActorSerial = actor.Serial;
            ctx.BackpackSerial = pack.Serial;
            ctx.ItemSerials.Add(dagger.Serial);
            ctx.ItemSerials.Add(bandage.Serial);

            ctx.ReportLines.Add("- SpawnCommandEquivalent: new WaylanderJoining() via GM-only proof command");
            ctx.ReportLines.Add("- TemporaryActor: " + Describe(actor));
            ctx.ReportLines.Add("- TemporaryActorLocation: " + DescribeLocation(actor));
            ctx.ReportLines.Add("- TemporaryActorType: " + actor.GetType().FullName);
            ctx.ReportLines.Add("- ControlMasterSet: " + controlSet);
            ctx.ReportLines.Add("- ControlMaster: " + Describe(actor.ControlMaster));
            ctx.ReportLines.Add("- Controlled: " + actor.Controlled);
            ctx.ReportLines.Add("- BondedForResurrectionProof: " + actor.IsBonded);
            ctx.ReportLines.Add("- AuthorizedPlayer: " + Describe(authorized));
            ctx.ReportLines.Add("- UnrelatedPlayer: " + Describe(unrelated));
            ctx.ReportLines.Add("- Backpack: " + Describe(pack) + " itemId=0x" + pack.ItemID.ToString("X", CultureInfo.InvariantCulture) + " hue=" + pack.Hue + " markerVersion=" + pack.MarkerVersion);
            ctx.ReportLines.Add("- InitialProofItems: " + Describe(dagger) + ", " + Describe(bandage) + " amount=" + bandage.Amount);

            RunAccessMatrix(ctx, gm, authorized, unrelated, actor, pack, dagger);
            RunDeathAndResurrection(ctx, gm, authorized, unrelated, actor, pack, dagger, bandage);

            WriteState(ctx);
            string report = WriteReport("setup", JoinLines(ctx.ReportLines));
            gm.SendMessage(68, "umgpackproof setup complete: actor={0} pack={1} report={2}", Format(ctx.ActorSerial), Format(ctx.BackpackSerial), report);
        }

        private static void RunVerify(Mobile gm)
        {
            ProofContext ctx = ReadState();
            List<string> lines = new List<string>();
            lines.Add("# Phase64D1B-R1 Temporary Access And Death Proof Restart Verify");
            lines.Add("");
            lines.Add("- GeneratedUtc: " + DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
            lines.Add("- GM: " + Describe(gm));

            Mobile actorMobile = World.FindMobile(ctx.ActorSerial);
            PlayerMobile authorized = World.FindMobile(ctx.AuthorizedPlayerSerial) as PlayerMobile;
            PlayerMobile unrelated = World.FindMobile(ctx.UnrelatedPlayerSerial) as PlayerMobile;
            AIGMCompanionBackpack pack = World.FindItem(ctx.BackpackSerial) as AIGMCompanionBackpack;

            Require(actorMobile != null && !actorMobile.Deleted, "temporary actor missing after restart");
            Require(authorized != null && !authorized.Deleted, "authorized player missing after restart");
            Require(unrelated != null && !unrelated.Deleted, "unrelated player missing after restart");
            Require(pack != null && !pack.Deleted, "temporary backpack missing after restart");
            Require(Object.ReferenceEquals(pack.Parent, actorMobile), "temporary backpack parent mismatch after restart");

            BaseCreature actorCreature = actorMobile as BaseCreature;
            Require(actorCreature != null, "temporary actor is not a BaseCreature after restart");
            Require(actorCreature.Alive && !actorCreature.IsDeadPet, "temporary actor not alive after restart verification");

            AIGMCompanionBackpackAudit audit = AIGMCompanionInventoryService.AuditBackpack(actorMobile);
            Require(audit != null && audit.IsNormalized && audit.Backpack == pack, "post-restart audit not normalized");

            List<string> foundItems = new List<string>();
            for (int i = 0; i < ctx.ItemSerials.Count; i++)
            {
                Item item = World.FindItem(ctx.ItemSerials[i]);
                Require(item != null && !item.Deleted, "proof item missing after restart: " + Format(ctx.ItemSerials[i]));
                Require(item.IsChildOf(pack), "proof item not in backpack after restart: " + Format(item));
                foundItems.Add(Describe(item) + " parent=" + Describe(item.Parent as Item));
            }

            AIGMCompanionInventoryResult ensure = AIGMCompanionInventoryService.EnsureBackpack(actorMobile, "phase64d1b_r1_temp_proof_restart_verify");
            Require(ensure.Accepted && ensure.Backpack == pack, "post-restart ensure changed or rejected backpack");

            AIGMCompanionInventoryResult access = AIGMCompanionInventoryService.CanAccess(authorized, actorMobile, "phase64d1b-proof-restart-authorized");

            lines.Add("- TemporaryActor: " + Describe(actorMobile));
            lines.Add("- TemporaryActorLocation: " + DescribeLocation(actorMobile));
            lines.Add("- Backpack: " + Describe(pack) + " itemId=0x" + pack.ItemID.ToString("X", CultureInfo.InvariantCulture) + " hue=" + pack.Hue + " markerVersion=" + pack.MarkerVersion);
            lines.Add("- Audit: classification=" + audit.Classification + " direct=" + audit.DirectItemCount + " recursive=" + audit.RecursiveItemCount + " weight=" + audit.TotalWeight.ToString(CultureInfo.InvariantCulture) + " status=" + audit.ValidationStatus);
            lines.Add("- ProofItems: " + String.Join("; ", foundItems.ToArray()));
            lines.Add("- RepeatedEnsureAfterRestart: accepted=true backpack=" + Format(ensure.Backpack));
            lines.Add("- AuthorizedAccessAfterRestart: " + access.ResultCode + " authorization=" + access.Authorization);
            lines.Add("- AuthorizedAccessAfterRestartNote: temporary ControlMaster is not used as the backpack persistence invariant; the authorized non-GM range matrix is proven in setup before restart");
            lines.Add("- Result: restart_verify_passed");

            string report = WriteReport("verify", JoinLines(lines));
            gm.SendMessage(68, "umgpackproof verify complete: actor={0} pack={1} report={2}", Format(ctx.ActorSerial), Format(ctx.BackpackSerial), report);
        }

        private static void RunCleanup(Mobile gm)
        {
            ProofContext ctx = ReadState();
            List<string> lines = new List<string>();
            lines.Add("# Phase64D1B-R1 Temporary Fixture Cleanup Proof");
            lines.Add("");
            lines.Add("- GeneratedUtc: " + DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
            lines.Add("- GM: " + Describe(gm));
            lines.Add("- AccountsBeforeCleanup: " + Accounts.Count);
            lines.Add("- MobilesBeforeCleanup: " + World.Mobiles.Count);
            lines.Add("- ItemsBeforeCleanup: " + World.Items.Count);

            Mobile actor = World.FindMobile(ctx.ActorSerial);
            BaseCreature creature = actor as BaseCreature;
            if (creature != null)
                creature.SetControlMaster(null);

            Account authorizedAccount = Accounts.GetAccount(ctx.AuthorizedAccountName) as Account;
            Account unrelatedAccount = Accounts.GetAccount(ctx.UnrelatedAccountName) as Account;

            if (actor != null && !actor.Deleted)
                actor.Delete();

            if (authorizedAccount != null)
                authorizedAccount.Delete();
            else
            {
                Mobile authorized = World.FindMobile(ctx.AuthorizedPlayerSerial);
                if (authorized != null && !authorized.Deleted)
                    authorized.Delete();
            }

            if (unrelatedAccount != null)
                unrelatedAccount.Delete();
            else
            {
                Mobile unrelated = World.FindMobile(ctx.UnrelatedPlayerSerial);
                if (unrelated != null && !unrelated.Deleted)
                    unrelated.Delete();
            }

            Item pack = World.FindItem(ctx.BackpackSerial);
            Require(pack == null || pack.Deleted, "temporary backpack remained after cleanup: " + Format(ctx.BackpackSerial));

            for (int i = 0; i < ctx.ItemSerials.Count; i++)
            {
                Item item = World.FindItem(ctx.ItemSerials[i]);
                Require(item == null || item.Deleted, "temporary proof item remained after cleanup: " + Format(ctx.ItemSerials[i]));
            }

            Require(Accounts.GetAccount(ctx.AuthorizedAccountName) == null, "authorized temp account remained");
            Require(Accounts.GetAccount(ctx.UnrelatedAccountName) == null, "unrelated temp account remained");

            lines.Add("- ActorDeleted: " + (World.FindMobile(ctx.ActorSerial) == null));
            lines.Add("- BackpackDeletedOrGone: " + (World.FindItem(ctx.BackpackSerial) == null));
            lines.Add("- ProofItemsDeletedOrGone: true");
            lines.Add("- TempAccountsDeleted: true");
            lines.Add("- AccountsAfterCleanup: " + Accounts.Count);
            lines.Add("- MobilesAfterCleanup: " + World.Mobiles.Count);
            lines.Add("- ItemsAfterCleanup: " + World.Items.Count);
            lines.Add("- Result: cleanup_passed");

            string report = WriteReport("cleanup", JoinLines(lines));
            string statePath = GetStatePath();
            if (File.Exists(statePath))
                File.Delete(statePath);

            gm.SendMessage(68, "umgpackproof cleanup complete: report={0}", report);
        }

        private static int CleanupStaleProofArtifacts()
        {
            int removed = 0;

            List<Mobile> staleMobiles = new List<Mobile>();
            foreach (Mobile mobile in World.Mobiles.Values)
            {
                if (mobile != null && IsProofMobile(mobile))
                    staleMobiles.Add(mobile);
            }

            for (int i = 0; i < staleMobiles.Count; i++)
            {
                BaseCreature creature = staleMobiles[i] as BaseCreature;
                if (creature != null)
                    creature.SetControlMaster(null);

                if (!staleMobiles[i].Deleted)
                {
                    staleMobiles[i].Delete();
                    removed++;
                }
            }

            List<Item> staleItems = new List<Item>();
            foreach (Item item in World.Items.Values)
            {
                if (item != null && IsProofItem(item))
                    staleItems.Add(item);
            }

            for (int i = 0; i < staleItems.Count; i++)
            {
                if (!staleItems[i].Deleted)
                {
                    staleItems[i].Delete();
                    removed++;
                }
            }

            List<Account> staleAccounts = new List<Account>();
            foreach (IAccount account in Accounts.GetAccounts())
            {
                Account typed = account as Account;
                if (typed != null && IsProofAccount(typed))
                    staleAccounts.Add(typed);
            }

            for (int i = 0; i < staleAccounts.Count; i++)
            {
                staleAccounts[i].Delete();
                removed++;
            }

            return removed;
        }

        private static bool IsProofMobile(Mobile mobile)
        {
            return HasProofPrefix(mobile != null ? mobile.Name : null);
        }

        private static bool IsProofItem(Item item)
        {
            if (HasProofPrefix(item != null ? item.Name : null))
                return true;

            AIGMCompanionBackpack backpack = item as AIGMCompanionBackpack;
            return backpack != null
                && !String.IsNullOrWhiteSpace(backpack.Provenance)
                && backpack.Provenance.StartsWith("phase64d1b_r1_temp_proof_", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsProofAccount(Account account)
        {
            return account != null
                && (account.Username.StartsWith("phase64d1b_authorized_", StringComparison.OrdinalIgnoreCase)
                    || account.Username.StartsWith("phase64d1b_unrelated_", StringComparison.OrdinalIgnoreCase));
        }

        private static bool HasProofPrefix(string value)
        {
            return !String.IsNullOrWhiteSpace(value)
                && value.StartsWith("PHASE64D1B", StringComparison.OrdinalIgnoreCase);
        }

        private static void RunAccessMatrix(ProofContext ctx, Mobile gm, PlayerMobile authorized, PlayerMobile unrelated, BaseCreature actor, AIGMCompanionBackpack pack, Item liftItem)
        {
            ctx.ReportLines.Add("");
            ctx.ReportLines.Add("## Access Matrix");

            AIGMUMGSleeveAccessResult sleeveNear = AIGMUMGSleeveAccessService.ValidateForGumpButton(authorized, actor);
            Require(sleeveNear.Accepted && sleeveNear.Authorization == "control_master", "authorized sleeve near failed: " + sleeveNear.ResultCode);
            bool contextNear = AIGMUMGSleeveAccessService.ShouldOfferContextEntry(authorized, actor);
            Require(contextNear, "authorized sleeve context entry not offered nearby");

            AIGMCompanionInventoryResult packNear = AIGMCompanionInventoryService.CanAccess(authorized, actor, "phase64d1b-proof-pack-near");
            Require(packNear.Accepted && packNear.Authorization == "control_master", "authorized pack near failed: " + packNear.ResultCode);
            AIGMCompanionInventoryResult openNear = AIGMCompanionInventoryService.OpenBackpack(authorized, actor);
            Require(openNear.Accepted, "authorized pack open failed: " + openNear.ResultCode);

            Bandage authorizedDrop = new Bandage(1);
            authorizedDrop.Name = "PHASE64D1B_PROOF_AUTHORIZED_DROP";
            bool authorizedDropAllowed = pack.TryDropItem(authorized, authorizedDrop, false);
            Require(authorizedDropAllowed && authorizedDrop.IsChildOf(pack), "authorized drop failed");
            LRReason liftReject = LRReason.Inspecific;
            bool authorizedLiftAllowed = pack.CheckLift(authorized, authorizedDrop, ref liftReject);
            Require(authorizedLiftAllowed, "authorized lift check failed: " + liftReject);
            pack.RemoveItem(authorizedDrop);
            authorizedDrop.Delete();

            AIGMCompanionInventoryResult unrelatedAccess = AIGMCompanionInventoryService.CanAccess(unrelated, actor, "phase64d1b-proof-pack-unrelated");
            Require(!unrelatedAccess.Accepted && unrelatedAccess.ResultCode == "unauthorized", "unrelated access was not denied: " + unrelatedAccess.ResultCode);
            AIGMCompanionInventoryResult unrelatedOpen = AIGMCompanionInventoryService.OpenBackpack(unrelated, actor);
            Require(!unrelatedOpen.Accepted, "unrelated open unexpectedly accepted");
            pack.DisplayTo(unrelated);
            LRReason unrelatedLiftReject = LRReason.Inspecific;
            bool unrelatedLift = pack.CheckLift(unrelated, liftItem, ref unrelatedLiftReject);
            Require(!unrelatedLift, "unrelated lift unexpectedly accepted");
            Bandage unauthorizedDrop = new Bandage(1);
            unauthorizedDrop.Name = "PHASE64D1B_PROOF_UNAUTHORIZED_DROP";
            bool unrelatedDrop = pack.TryDropItem(unrelated, unauthorizedDrop, false);
            Require(!unrelatedDrop, "unrelated drop unexpectedly accepted");
            pack.OnDoubleClick(unrelated);
            unauthorizedDrop.Delete();

            Point3D near = authorized.Location;
            authorized.MoveToWorld(new Point3D(actor.X + 20, actor.Y, actor.Z), actor.Map);
            AIGMUMGSleeveAccessResult sleeveFar = AIGMUMGSleeveAccessService.ValidateForGumpButton(authorized, actor);
            Require(!sleeveFar.Accepted && sleeveFar.ResultCode == "out_of_range", "far sleeve did not deny out_of_range: " + sleeveFar.ResultCode);
            bool contextFar = AIGMUMGSleeveAccessService.ShouldOfferContextEntry(authorized, actor);
            Require(!contextFar, "far context entry unexpectedly offered");
            AIGMCompanionInventoryResult packFar = AIGMCompanionInventoryService.CanAccess(authorized, actor, "phase64d1b-proof-pack-far");
            Require(!packFar.Accepted && packFar.ResultCode == "out_of_range", "far pack did not deny out_of_range: " + packFar.ResultCode);

            Map originalMap = actor.Map;
            Map otherMap = originalMap == Map.Trammel ? Map.Felucca : Map.Trammel;
            authorized.MoveToWorld(new Point3D(actor.X, actor.Y, actor.Z), otherMap);
            AIGMUMGSleeveAccessResult sleeveOtherMap = AIGMUMGSleeveAccessService.ValidateForGumpButton(authorized, actor);
            Require(!sleeveOtherMap.Accepted, "other-map sleeve unexpectedly accepted");
            bool contextOtherMap = AIGMUMGSleeveAccessService.ShouldOfferContextEntry(authorized, actor);
            Require(!contextOtherMap, "other-map context entry unexpectedly offered");
            AIGMCompanionInventoryResult packOtherMap = AIGMCompanionInventoryService.CanAccess(authorized, actor, "phase64d1b-proof-pack-map");
            Require(!packOtherMap.Accepted && packOtherMap.ResultCode == "map_mismatch", "other-map pack did not deny map_mismatch: " + packOtherMap.ResultCode);

            authorized.MoveToWorld(near, originalMap);
            AIGMUMGSleeveAccessResult sleeveReturned = AIGMUMGSleeveAccessService.ValidateForGumpButton(authorized, actor);
            Require(sleeveReturned.Accepted, "returned sleeve access failed: " + sleeveReturned.ResultCode);
            AIGMCompanionInventoryResult packReturned = AIGMCompanionInventoryService.CanAccess(authorized, actor, "phase64d1b-proof-pack-returned");
            Require(packReturned.Accepted, "returned backpack access failed: " + packReturned.ResultCode);

            AIGMCompanionInventoryResult gmAccess = AIGMCompanionInventoryService.CanAccess(gm, actor, "phase64d1b-proof-pack-gm");
            Require(gmAccess.Accepted && gmAccess.Authorization == "game_master", "GM access failed");

            ctx.ReportLines.Add("- AuthorizedNormalWithin12Sleeve: " + sleeveNear.ResultCode + " authorization=" + sleeveNear.Authorization);
            ctx.ReportLines.Add("- AuthorizedNormalWithin12ContextEntry: " + contextNear);
            ctx.ReportLines.Add("- AuthorizedNormalWithin12Backpack: " + packNear.ResultCode + " authorization=" + packNear.Authorization);
            ctx.ReportLines.Add("- AuthorizedDropAllowed: " + authorizedDropAllowed);
            ctx.ReportLines.Add("- AuthorizedLiftAllowed: " + authorizedLiftAllowed);
            ctx.ReportLines.Add("- UnrelatedBackpackAccess: " + unrelatedAccess.ResultCode);
            ctx.ReportLines.Add("- UnrelatedOpenDenied: " + !unrelatedOpen.Accepted);
            ctx.ReportLines.Add("- UnrelatedLiftDenied: " + !unrelatedLift + " reject=" + unrelatedLiftReject);
            ctx.ReportLines.Add("- UnrelatedDropDenied: " + !unrelatedDrop);
            ctx.ReportLines.Add("- AuthorizedBeyond12Sleeve: " + sleeveFar.ResultCode);
            ctx.ReportLines.Add("- AuthorizedBeyond12ContextEntry: " + contextFar);
            ctx.ReportLines.Add("- AuthorizedBeyond12Backpack: " + packFar.ResultCode);
            ctx.ReportLines.Add("- AuthorizedOtherMapSleeve: " + sleeveOtherMap.ResultCode);
            ctx.ReportLines.Add("- AuthorizedOtherMapContextEntry: " + contextOtherMap);
            ctx.ReportLines.Add("- AuthorizedOtherMapBackpack: " + packOtherMap.ResultCode);
            ctx.ReportLines.Add("- AuthorizedReturnSleeve: " + sleeveReturned.ResultCode);
            ctx.ReportLines.Add("- AuthorizedReturnBackpack: " + packReturned.ResultCode);
            ctx.ReportLines.Add("- GMBackpackAccess: " + gmAccess.ResultCode + " authorization=" + gmAccess.Authorization);
        }

        private static void RunDeathAndResurrection(ProofContext ctx, Mobile gm, PlayerMobile authorized, PlayerMobile unrelated, BaseCreature actor, AIGMCompanionBackpack pack, Item firstItem, Item secondItem)
        {
            ctx.ReportLines.Add("");
            ctx.ReportLines.Add("## Death, Corpse, And Resurrection");
            Serial packSerial = pack.Serial;
            Serial firstSerial = firstItem.Serial;
            Serial secondSerial = secondItem.Serial;
            int backpackCountBefore = CountAIGMBackpacks(actor);

            actor.Kill();
            Require(!actor.Deleted, "bonded temporary actor deleted on death");
            bool actorDeadBondedAfterKill = actor.IsDeadPet;
            Require(actorDeadBondedAfterKill, "temporary actor did not enter dead bonded pet state");
            Require(World.FindItem(packSerial) == pack && Object.ReferenceEquals(pack.Parent, actor), "backpack not retained on dead actor");
            Require(World.FindItem(firstSerial) == firstItem && firstItem.IsChildOf(pack), "first proof item not retained in backpack on death");
            Require(World.FindItem(secondSerial) == secondItem && secondItem.IsChildOf(pack), "second proof item not retained in backpack on death");
            Require(CountAIGMBackpacks(actor) == backpackCountBefore, "duplicate backpack detected after death");

            Container corpse = actor.Corpse;
            bool corpseHasPack = ContainsRecursive(corpse, packSerial);
            bool corpseHasFirst = ContainsRecursive(corpse, firstSerial);
            bool corpseHasSecond = ContainsRecursive(corpse, secondSerial);
            Require(!corpseHasPack, "backpack moved to corpse");
            Require(!corpseHasFirst && !corpseHasSecond, "proof contents moved to corpse");

            AIGMCompanionInventoryResult deadNormal = AIGMCompanionInventoryService.CanAccess(authorized, actor, "phase64d1b-proof-pack-dead-normal");
            Require(!deadNormal.Accepted && deadNormal.ResultCode == "actor_dead", "dead actor normal access did not deny actor_dead: " + deadNormal.ResultCode);
            AIGMCompanionInventoryResult deadGm = AIGMCompanionInventoryService.CanAccess(gm, actor, "phase64d1b-proof-pack-dead-gm");
            Require(deadGm.Accepted && deadGm.Authorization == "game_master", "GM dead actor access failed: " + deadGm.ResultCode);
            LRReason deadUnrelatedLiftReject = LRReason.Inspecific;
            bool deadUnrelatedLift = pack.CheckLift(unrelated, firstItem, ref deadUnrelatedLiftReject);
            Require(!deadUnrelatedLift, "unrelated lift succeeded while actor dead");

            actor.ResurrectPet();
            Require(actor.Alive && !actor.IsDeadPet, "temporary actor did not resurrect through stock pet method");
            Require(World.FindItem(packSerial) == pack && Object.ReferenceEquals(pack.Parent, actor), "backpack serial or parent changed after resurrection");
            Require(World.FindItem(firstSerial) == firstItem && firstItem.IsChildOf(pack), "first proof item missing after resurrection");
            Require(World.FindItem(secondSerial) == secondItem && secondItem.IsChildOf(pack), "second proof item missing after resurrection");

            ctx.ReportLines.Add("- BackpackBeforeDeath: " + Format(packSerial));
            ctx.ReportLines.Add("- ContentsBeforeDeath: " + Format(firstSerial) + ", " + Format(secondSerial));
            ctx.ReportLines.Add("- ActorDeadBondedAfterKill: " + actorDeadBondedAfterKill);
            ctx.ReportLines.Add("- Corpse: " + Describe(corpse));
            ctx.ReportLines.Add("- CorpseHasBackpack: " + corpseHasPack);
            ctx.ReportLines.Add("- CorpseHasProofItems: " + (corpseHasFirst || corpseHasSecond));
            ctx.ReportLines.Add("- DuplicateBackpackAfterDeath: false");
            ctx.ReportLines.Add("- NormalPlayerDeadActorAccess: " + deadNormal.ResultCode);
            ctx.ReportLines.Add("- GMDeadActorAccess: " + deadGm.ResultCode + " authorization=" + deadGm.Authorization);
            ctx.ReportLines.Add("- UnrelatedDeadLiftDenied: " + !deadUnrelatedLift + " reject=" + deadUnrelatedLiftReject);
            ctx.ReportLines.Add("- ResurrectedWithStockResurrectPet: true");
            ctx.ReportLines.Add("- BackpackAfterResurrection: " + Format(pack.Serial));
            ctx.ReportLines.Add("- ContentsAfterResurrection: " + Format(firstItem.Serial) + ", " + Format(secondItem.Serial));
        }

        private static int CountAIGMBackpacks(Mobile actor)
        {
            int count = 0;
            if (actor == null)
                return count;

            List<Item> items = actor.Items;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] is AIGMCompanionBackpack)
                    count++;
            }

            return count;
        }

        private static bool ContainsRecursive(Container container, Serial serial)
        {
            if (container == null || !serial.IsValid)
                return false;

            for (int i = 0; i < container.Items.Count; i++)
            {
                Item item = container.Items[i];
                if (item != null && item.Serial == serial)
                    return true;

                Container child = item as Container;
                if (child != null && ContainsRecursive(child, serial))
                    return true;
            }

            return false;
        }

        private static Point3D FindNearbyFit(Map map, Point3D origin, int radius)
        {
            if (map == null || map == Map.Internal)
                return origin;

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int x = origin.X + dx;
                    int y = origin.Y + dy;
                    int z = origin.Z;
                    if (map.CanFit(x, y, z, 16, false, false))
                        return new Point3D(x, y, z);

                    z = map.GetAverageZ(x, y);
                    if (map.CanFit(x, y, z, 16, false, false))
                        return new Point3D(x, y, z);
                }
            }

            return origin;
        }

        private static string WriteReport(string stage, string content)
        {
            string dir = GetProofDirectory();
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, String.Format("umgpackproof_{0}_{1:yyyyMMdd_HHmmss}.md", stage, DateTime.UtcNow));
            File.WriteAllText(path, content);
            return path;
        }

        private static void WriteState(ProofContext ctx)
        {
            string dir = GetProofDirectory();
            Directory.CreateDirectory(dir);
            List<string> lines = new List<string>();
            lines.Add("authorizedAccount=" + ctx.AuthorizedAccountName);
            lines.Add("unrelatedAccount=" + ctx.UnrelatedAccountName);
            lines.Add("authorizedPlayer=" + ctx.AuthorizedPlayerSerial.Value.ToString(CultureInfo.InvariantCulture));
            lines.Add("unrelatedPlayer=" + ctx.UnrelatedPlayerSerial.Value.ToString(CultureInfo.InvariantCulture));
            lines.Add("actor=" + ctx.ActorSerial.Value.ToString(CultureInfo.InvariantCulture));
            lines.Add("backpack=" + ctx.BackpackSerial.Value.ToString(CultureInfo.InvariantCulture));
            for (int i = 0; i < ctx.ItemSerials.Count; i++)
                lines.Add("item=" + ctx.ItemSerials[i].Value.ToString(CultureInfo.InvariantCulture));

            File.WriteAllLines(GetStatePath(), lines.ToArray());
        }

        private static ProofContext ReadState()
        {
            string path = GetStatePath();
            if (!File.Exists(path))
                throw new InvalidOperationException("proof state file is missing: " + path);

            ProofContext ctx = new ProofContext();
            string[] lines = File.ReadAllLines(path);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                int equals = line.IndexOf('=');
                if (equals <= 0)
                    continue;

                string key = line.Substring(0, equals);
                string value = line.Substring(equals + 1);
                if (key == "authorizedAccount")
                    ctx.AuthorizedAccountName = value;
                else if (key == "unrelatedAccount")
                    ctx.UnrelatedAccountName = value;
                else if (key == "authorizedPlayer")
                    ctx.AuthorizedPlayerSerial = ParseSerialValue(value);
                else if (key == "unrelatedPlayer")
                    ctx.UnrelatedPlayerSerial = ParseSerialValue(value);
                else if (key == "actor")
                    ctx.ActorSerial = ParseSerialValue(value);
                else if (key == "backpack")
                    ctx.BackpackSerial = ParseSerialValue(value);
                else if (key == "item")
                    ctx.ItemSerials.Add(ParseSerialValue(value));
            }

            return ctx;
        }

        private static Serial ParseSerialValue(string value)
        {
            int serial;
            if (!Int32.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out serial))
                return Serial.MinusOne;

            return (Serial)serial;
        }

        private static string GetStatePath()
        {
            return Path.Combine(GetProofDirectory(), StateFileName);
        }

        private static string GetProofDirectory()
        {
            string auditPointer = Path.Combine(Core.BaseDirectory, "PHASE64D1B_R1_AUDIT_PATH.txt");
            if (File.Exists(auditPointer))
            {
                string root = File.ReadAllText(auditPointer).Trim();
                if (!String.IsNullOrWhiteSpace(root))
                    return Path.Combine(root, "reports", "gate12_14_temp_lifecycle");
            }

            return Path.Combine(Core.BaseDirectory, "docs", "runtime", "phase64d1b_r1_temp_lifecycle");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private static string JoinLines(List<string> lines)
        {
            return String.Join(Environment.NewLine, lines.ToArray()) + Environment.NewLine;
        }

        private static string Describe(Mobile mobile)
        {
            if (mobile == null)
                return "none";

            return SafeName(mobile) + "[" + Format(mobile.Serial) + "]";
        }

        private static string Describe(Item item)
        {
            if (item == null)
                return "none";

            return item.GetType().FullName + "[" + Format(item.Serial) + "]";
        }

        private static string Describe(object parentItem)
        {
            Item item = parentItem as Item;
            if (item != null)
                return Describe(item);

            Mobile mobile = parentItem as Mobile;
            if (mobile != null)
                return Describe(mobile);

            return "none";
        }

        private static string DescribeLocation(Mobile mobile)
        {
            if (mobile == null)
                return "none";

            return String.Format("{0} {1},{2},{3}", mobile.Map != null ? mobile.Map.Name : "null", mobile.X, mobile.Y, mobile.Z);
        }

        private static string Format(Item item)
        {
            return item != null ? Format(item.Serial) : "none";
        }

        private static string Format(Serial serial)
        {
            return serial.IsValid ? String.Format("0x{0:X8}", serial.Value) : "none";
        }

        private static string SafeName(Mobile mobile)
        {
            string name = mobile == null || String.IsNullOrWhiteSpace(mobile.Name) ? "Mobile" : mobile.Name;
            return name.Replace('"', '\'').Replace('\r', ' ').Replace('\n', ' ');
        }

        private sealed class ProofContext
        {
            public DateTime StartUtc;
            public int AccountsBefore;
            public int MobilesBefore;
            public int ItemsBefore;
            public string AuthorizedAccountName;
            public string UnrelatedAccountName;
            public Serial AuthorizedPlayerSerial;
            public Serial UnrelatedPlayerSerial;
            public Serial ActorSerial;
            public Serial BackpackSerial;
            public readonly List<Serial> ItemSerials = new List<Serial>();
            public readonly List<string> ReportLines = new List<string>();
        }
    }
}
