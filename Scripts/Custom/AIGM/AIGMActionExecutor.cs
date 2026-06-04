using System;
using System.Collections.Generic;
using System.Text;
using Server.Commands;
using Server.Commands.Generic;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Spells;

namespace Server.Custom.AIGM
{
    public static partial class AIGMActionExecutor
    {
        public static string BuildFollowupSuggestion(AIGMActionProposal action)
        {
            if (action == null)
                return null;

            switch (action.ActionKind)
            {
                case "inspect_target":
                    return "Want me to review the likely controlling file next?";
                case "goto_target":
                    return "You are at the target now. Want props or a file analysis next?";
                case "review_file":
                    return "Want me to inspect the next related file or reopen target props?";
                case "inspect_vendor_stock":
                    return "Want me to inspect the concrete vendor class or restock the vendor next?";
                case "run_gm_command":
                    if (action.Parameters != null)
                    {
                        string commandName;
                        if (action.Parameters.TryGetValue("commandName", out commandName))
                        {
                            switch (commandName)
                            {
                                case AIGMCommandAction.RestockVendor:
                                    return "Vendor restocked. Want me to inspect stock-related files next?";
                                case AIGMCommandAction.SpawnTestCopy:
                                    return "Test copy created, marked, and opened in props. Want another duplication or cleanup next?";
                                case AIGMCommandAction.CleanupTestCopies:
                                    return "Nearby marked test copies cleaned up. Want me to create a fresh one now?";
                                case AIGMCommandAction.ListTestCopies:
                                    return "Nearby marked test copies listed. Want me to clean them up or inspect one next?";
                                case AIGMCommandAction.OpenPropsOnTarget:
                                    return "Props opened. Want file analysis for this target next?";
                                case AIGMCommandAction.ViewEquipOnTarget:
                                    return "Equipment shown. Want target props or file analysis next?";
                                case AIGMCommandAction.InspectNearbyMobiles:
                                    return "Nearby mobiles listed. Want me to inspect one target or scan the area again?";
                                case AIGMCommandAction.InspectNearbyItems:
                                    return "Nearby items listed. Want me to inspect a specific one or compare distances?";
                                case AIGMCommandAction.InspectRegion:
                                    return "Region summary opened. Want me to scan mobiles, items, or move somewhere nearby?";
                                case AIGMCommandAction.ScanAroundTarget:
                                    return "Target-centered world scan opened. Want me to move there or inspect a listed entity?";
                                case AIGMCommandAction.GoToCoordinates:
                                    return "Moved to the requested coordinates. Want a fresh local scan now?";
                                case AIGMCommandAction.FollowMobile:
                                    return "Follow mode is active. Want me to stop following or head somewhere specific next?";
                                case AIGMCommandAction.StopFollowing:
                                    return "Movement stopped. Want me to follow again or walk to a location?";
                                case AIGMCommandAction.PathToCoordinates:
                                    return "Pathing to the requested coordinates. Want a status check or a nearby scan when I arrive?";
                                case AIGMCommandAction.PathToNamedLocation:
                                    return "Pathing to the named location. Want me to stop there or keep following you after arrival?";
                                case AIGMCommandAction.MovementStatus:
                                    return "Movement status checked. Want me to keep going, stop, or redirect somewhere else?";
                                case AIGMCommandAction.SetArrivalAction:
                                    return "Arrival behavior updated. Want me to move somewhere now and use it?";
                                case AIGMCommandAction.PauseMovement:
                                    return "Movement paused. Want me to resume, cancel, or reroute?";
                                case AIGMCommandAction.ResumeMovement:
                                    return "Movement resumed. Want a status check or a different destination?";
                                case AIGMCommandAction.CancelMovement:
                                    return "Movement canceled. Want a new destination or follow order?";
                                case AIGMCommandAction.QueueNamedRouteStop:
                                    return "Queued the next named stop. Want to add another stop or start the route?";
                                case AIGMCommandAction.QueueCoordinateRouteStop:
                                    return "Queued the next coordinate stop. Want another stop or should I start moving?";
                                case AIGMCommandAction.PathToCurrentTarget:
                                    return "Pathing to the current target. Want me to stop there, follow it, or scan on arrival?";
                                case AIGMCommandAction.FollowCurrentTarget:
                                    return "Following the current target now. Want me to keep shadowing it or stop at a destination instead?";
                                case AIGMCommandAction.OpenCounselorPack:
                                    return "Counselor pack opened. Want me to create an item in it now?";
                                case AIGMCommandAction.SpawnItemToCounselorPack:
                                    return "Item created in counselor pack. Want me to open the pack or create another item?";
                                case AIGMCommandAction.InspectNearestVendor:
                                    return "Nearest vendor inspected. Want me to move there or inspect stock next?";
                                case AIGMCommandAction.GoToNearestVendor:
                                    return "Moved to the nearest vendor. Want a fresh local scan or stock inspection next?";
                                case AIGMCommandAction.InspectNearestContainer:
                                    return "Nearest container inspected. Want me to compare its contents or nearby items next?";
                                case AIGMCommandAction.InspectNearestDoor:
                                    return "Nearest door inspected. Want me to move there or inspect surrounding items next?";
                                case AIGMCommandAction.InspectNearestMobile:
                                    return "Nearest mobile inspected. Want me to move there or inspect its equipment/props next?";
                                case AIGMCommandAction.GoToNearestByType:
                                    return "Moved to the nearest matching type. Want a fresh local scan now?";
                                case AIGMCommandAction.InspectDoorState:
                                    return "Door state inspected. Want me to move there or scan around it next?";
                                case AIGMCommandAction.InspectVendorRuntimeStock:
                                    return "Vendor runtime stock inspected. Want me to compare it to likely code files next?";
                                case AIGMCommandAction.ExecuteNextStep:
                                    return "Executed the next suggested AI GM step. Want me to continue or rescan?";
                            }
                        }
                    }
                    break;
            }

            return null;
        }

        public static bool Execute(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            if (from == null || action == null)
            {
                message = "No action was available to execute.";
                return false;
            }

            if (from.AccessLevel < AIGMSettings.RequiredAccess)
            {
                message = "You do not have access to execute AI GM actions.";
                return false;
            }

            AIGMExecutionLog.Write("EXECUTE_START kind={0}", action == null ? "(null)" : action.ActionKind);

            switch (action.ActionKind)
            {
                case "inspect_target":
                    return ExecuteInspectTarget(from, action, out message);
                case "goto_target":
                    return ExecuteGotoTarget(from, action, out message);
                case "review_file":
                    return ExecuteReviewFile(from, action, out message);
                case "inspect_vendor_stock":
                    return ExecuteInspectVendorStock(from, action, out message);
                case "gm_add_world_item":
                    return ExecuteNativeAddWorldItem(from, action, out message);
                case "gm_add_container_item":
                    return ExecuteNativeAddContainerItem(from, action, out message);
                case "gm_add_world_mobile":
                    return ExecuteNativeAddWorldMobile(from, action, out message);
                case "gm_props_read":
                    return ExecuteNativePropsRead(from, action, out message);
                case "gm_follow_requester":
                    return ExecuteFollowRequester(from, action, out message);
                case "gm_stop_follow":
                    return ExecuteStopFollowCanonical(from, action, out message);
                case "gm_resume_follow":
                    return ExecuteResumeFollowCanonical(from, action, out message);
                case "gm_go_to_requester":
                    return ExecuteGoToRequester(from, action, out message);
                case "run_gm_command":
                    return ExecuteRunGmCommand(from, action, out message);
                default:
                    AIGMExecutionLog.Write("EXECUTE_UNSUPPORTED kind={0}", action.ActionKind ?? String.Empty);
                    message = "That AI GM action is not executable yet.";
                    return false;
            }
        }

        private static bool ExecuteNativeAddWorldItem(Mobile from, AIGMActionProposal action, out string message)
        {
            AIGMExecutionResult result = AIGMNativeAddAdapter.AddWorldItem(from, null, action);
            message = result != null ? result.Message : "Native Add adapter returned no result.";
            return result != null && result.Ok;
        }

        private static bool ExecuteNativeAddContainerItem(Mobile from, AIGMActionProposal action, out string message)
        {
            AIGMExecutionResult result = AIGMNativeAddAdapter.AddContainerItem(from, null, action);
            message = result != null ? result.Message : "Native Add container adapter returned no result.";
            return result != null && result.Ok;
        }

        private static bool ExecuteNativeAddWorldMobile(Mobile from, AIGMActionProposal action, out string message)
        {
            AIGMExecutionResult result = AIGMNativeAddAdapter.AddWorldMobile(from, null, action);
            message = result != null ? result.Message : "Native Add mobile adapter returned no result.";
            return result != null && result.Ok;
        }

        private static bool ExecuteNativePropsRead(Mobile from, AIGMActionProposal action, out string message)
        {
            AIGMExecutionResult result = AIGMPropsReadAdapter.Read(from, action);
            message = result != null ? result.Message : "Native props-read adapter returned no result.";
            return result != null && result.Ok;
        }

        private static bool ExecuteRunGmCommand(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            string commandName;
            if (action.Parameters == null || !action.Parameters.TryGetValue("commandName", out commandName) || String.IsNullOrWhiteSpace(commandName))
            {
                message = "No GM command action name was provided.";
                return false;
            }

            switch (commandName)
            {
                case AIGMCommandAction.TeleportToTarget:
                    return ExecuteGotoTarget(from, action, out message);
                case AIGMCommandAction.OpenPropsOnTarget:
                    return ExecuteInspectTarget(from, action, out message);
                case AIGMCommandAction.RestockVendor:
                    return ExecuteRestockVendor(from, action, out message);
                case AIGMCommandAction.ViewEquipOnTarget:
                    return ExecuteViewEquipOnTarget(from, action, out message);
                case AIGMCommandAction.SpawnTestCopy:
                    return ExecuteSpawnTestCopy(from, action, out message);
                case AIGMCommandAction.CleanupTestCopies:
                    return ExecuteCleanupTestCopies(from, action, out message);
                case AIGMCommandAction.ListTestCopies:
                    return ExecuteListTestCopies(from, action, out message);
                case AIGMCommandAction.InspectNearbyMobiles:
                    return ExecuteInspectNearbyMobiles(from, action, out message);
                case AIGMCommandAction.InspectNearbyItems:
                    return ExecuteInspectNearbyItems(from, action, out message);
                case AIGMCommandAction.InspectRegion:
                    return ExecuteInspectRegion(from, action, out message);
                case AIGMCommandAction.ScanAroundTarget:
                    return ExecuteScanAroundTarget(from, action, out message);
                case AIGMCommandAction.GoToCoordinates:
                    return ExecuteGotoCoordinates(from, action, out message);
                case AIGMCommandAction.FollowMobile:
                    return ExecuteFollowMobile(from, action, out message);
                case AIGMCommandAction.StopFollowing:
                    return ExecuteStopFollowing(from, action, out message);
                case AIGMCommandAction.PathToCoordinates:
                    return ExecutePathToCoordinates(from, action, out message);
                case AIGMCommandAction.PathToNamedLocation:
                    return ExecutePathToNamedLocation(from, action, out message);
                case AIGMCommandAction.MovementStatus:
                    return ExecuteMovementStatus(from, action, out message);
                case AIGMCommandAction.SetArrivalAction:
                    return ExecuteSetArrivalAction(from, action, out message);
                case AIGMCommandAction.PauseMovement:
                    return ExecutePauseMovement(from, action, out message);
                case AIGMCommandAction.ResumeMovement:
                    return ExecuteResumeMovement(from, action, out message);
                case AIGMCommandAction.CancelMovement:
                    return ExecuteCancelMovement(from, action, out message);
                case AIGMCommandAction.QueueNamedRouteStop:
                    return ExecuteQueueNamedRouteStop(from, action, out message);
                case AIGMCommandAction.QueueCoordinateRouteStop:
                    return ExecuteQueueCoordinateRouteStop(from, action, out message);
                case AIGMCommandAction.PathToCurrentTarget:
                    return ExecutePathToCurrentTarget(from, action, out message);
                case AIGMCommandAction.FollowCurrentTarget:
                    return ExecuteFollowCurrentTarget(from, action, out message);
                case AIGMCommandAction.InspectNearestVendor:
                    return ExecuteInspectNearestVendor(from, action, out message);
                case AIGMCommandAction.GoToNearestVendor:
                    return ExecuteGoToNearestVendor(from, action, out message);
                case AIGMCommandAction.InspectNearestContainer:
                    return ExecuteInspectNearestContainer(from, action, out message);
                case AIGMCommandAction.InspectNearestDoor:
                    return ExecuteInspectNearestDoor(from, action, out message);
                case AIGMCommandAction.InspectNearestMobile:
                    return ExecuteInspectNearestMobile(from, action, out message);
                case AIGMCommandAction.GoToNearestByType:
                    return ExecuteGoToNearestByType(from, action, out message);
                case AIGMCommandAction.InspectDoorState:
                    return ExecuteInspectDoorState(from, action, out message);
                case AIGMCommandAction.InspectVendorRuntimeStock:
                    return ExecuteInspectVendorRuntimeStock(from, action, out message);
                case AIGMCommandAction.ExecuteNextStep:
                    return ExecuteNextStep(from, action, out message);
                case AIGMCommandAction.OpenCounselorPack:
                    return ExecuteOpenCounselorPack(from, action, out message);
                case AIGMCommandAction.SpawnItemToCounselorPack:
                    return ExecuteSpawnItemToCounselorPack(from, action, out message);
                default:
                    message = "That GM command action is not whitelisted.";
                    return false;
            }
        }

        private static bool ExecuteRestockVendor(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            IEntity ent = ResolveEntity(action, out message);
            if (ent == null)
                return false;

            BaseVendor vendor = ent as BaseVendor;
            if (vendor == null)
            {
                message = "That target is not a vendor.";
                return false;
            }

            vendor.Restock();
            message = "Vendor restocked through whitelisted AI GM command action.";
            return true;
        }

        private static bool ExecuteViewEquipOnTarget(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            IEntity ent = ResolveEntity(action, out message);
            if (ent == null)
                return false;

            Mobile mob = ent as Mobile;
            if (mob == null)
            {
                message = "That target is not a mobile.";
                return false;
            }

            from.SendGump(new AIGMEquipInsightGump(mob));
            message = "Opened equipment insight for the selected target.";
            return true;
        }

        private static bool ExecuteNextStep(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            AIGMSessionState session = AIGMSessionState.Get(from);
            if (session == null || session.LastResponse == null || session.LastResponse.NextAction == null)
            {
                message = "No AI GM next step is available to execute.";
                return false;
            }

            AIGMActionProposal next = session.LastResponse.NextAction;
            if (next.Category != null && next.Category.Equals("mutate", StringComparison.OrdinalIgnoreCase))
            {
                message = "Next step requires explicit confirmation because it is mutating.";
                return false;
            }

            AIGMExecutionLoopState loop = AIGMExecutionLoopState.Get(from);
            if (loop != null && loop.StepsExecuted >= 3)
            {
                message = "AI GM execution loop reached its safety step limit for now.";
                return false;
            }

            string inner;
            bool ok = Execute(from, next, out inner);
            if (loop != null)
            {
                loop.StepsExecuted++;
                loop.LastStepUtc = DateTime.UtcNow;
                loop.LastPlanSummary = session.LastResponse.Plan != null ? String.Join(" -> ", session.LastResponse.Plan.ToArray()) : null;
                loop.LastStopReason = session.LastResponse.StopReason;
            }

            session.LastActionDescription = next.Description;
            session.LastActionResult = inner;
            session.ExecutionStepCount = loop != null ? loop.StepsExecuted : (session.ExecutionStepCount + 1);

            if (ok && !String.IsNullOrWhiteSpace(session.LastQuestion))
            {
                AIGMResponse followup = AIGMBridgeClient.ContinueAfterAction(from, session.LastQuestion, session.CurrentTarget, next.Description, inner, session.ExecutionStepCount);
                if (followup != null)
                {
                    session.LastResponse = followup;

                    bool autoContinued = false;
                    if (loop != null && !loop.AutoContinueUsed && followup.NextAction != null && IsSafeAutoContinue(followup.NextAction) && loop.StepsExecuted < 3)
                    {
                        loop.AutoContinueUsed = true;
                        string chainedMessage;
                        autoContinued = ExecuteNextStep(from, action, out chainedMessage);
                        if (!String.IsNullOrWhiteSpace(chainedMessage))
                            inner = (inner ?? String.Empty) + " | Auto-continue: " + chainedMessage;
                    }

                    if (!autoContinued)
                        from.SendGump(new AIGMResponseGump(from, null, followup));
                }
            }

            message = ok ? (inner ?? "Executed next AI GM step.") : (inner ?? "Failed to execute next AI GM step.");
            return ok;
        }

        private static IEntity ResolveEntity(AIGMActionProposal action, out string message)
        {
            message = null;

            string rawSerial;
            if (action.Parameters == null || !action.Parameters.TryGetValue("targetSerial", out rawSerial) || String.IsNullOrWhiteSpace(rawSerial))
            {
                message = "No target serial was provided for this action.";
                return null;
            }

            int serial;
            if (!TryParseSerial(rawSerial, out serial))
            {
                message = "Target serial was invalid.";
                return null;
            }

            IEntity ent = World.FindEntity(serial);
            if (ent == null)
            {
                message = "Target entity was not found.";
                return null;
            }

            return ent;
        }

        private static bool TryParseSerial(string rawSerial, out int serial)
        {
            serial = 0;

            if (String.IsNullOrWhiteSpace(rawSerial))
                return false;

            rawSerial = rawSerial.Trim();

            if (rawSerial.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return Int32.TryParse(rawSerial.Substring(2), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out serial);
            }

            return Int32.TryParse(rawSerial, out serial);
        }

        private static bool IsSafeAutoContinue(AIGMActionProposal action)
        {
            if (action == null)
                return false;

            string category = action.Category ?? "read";
            if (category.Equals("mutate", StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        public static bool ExecuteSystemAction(Mobile from, string commandName, out string message)
        {
            message = null;
            if (String.IsNullOrWhiteSpace(commandName))
            {
                message = "No system action was provided.";
                return false;
            }

            AIGMActionProposal action = new AIGMActionProposal();
            action.ActionKind = "run_gm_command";
            action.Description = commandName;
            action.Category = "read";
            action.RequiresConfirmation = false;
            action.Parameters["commandName"] = commandName;
            return ExecuteRunGmCommand(from, action, out message);
        }

        private static bool ExecuteOpenCounselorPack(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;

            AIGMCounselor counselor = FindCounselor(from, 48);
            if (counselor == null)
            {
                message = "No nearby AI GM counselor pack was available.";
                return false;
            }

            IAIGMInventoryCapability inventory = counselor.Inventory;
            if (inventory == null)
            {
                message = "The counselor pack could not be prepared.";
                return false;
            }

            return inventory.TryOpenPrimaryContainer(from, out message);
        }

        private static bool ExecuteSpawnItemToCounselorPack(Mobile from, AIGMActionProposal action, out string message)
        {
            message = null;
            LogExecution("ExecuteSpawnItemToCounselorPack start from=" + SafeMobileName(from));

            AIGMCounselor counselor = FindCounselor(from, 48);
            if (counselor == null)
            {
                message = "No nearby AI GM counselor was found for item delivery.";
                return false;
            }

            IAIGMInventoryCapability inventory = counselor.Inventory;
            if (inventory == null)
            {
                message = "The counselor pack could not be prepared.";
                return false;
            }

            string rawItemName = null;
            string rawAmount = null;
            string rawBlessed = null;

            if (action != null && action.Parameters != null)
            {
                action.Parameters.TryGetValue("itemName", out rawItemName);

                if (String.IsNullOrWhiteSpace(rawItemName))
                    action.Parameters.TryGetValue("itemAlias", out rawItemName);

                action.Parameters.TryGetValue("amount", out rawAmount);
                action.Parameters.TryGetValue("blessed", out rawBlessed);
            }

            if (String.IsNullOrWhiteSpace(rawItemName))
            {
                message = "No item name was provided for counselor pack creation.";
                LogExecution("spawn fail no item name");
                return false;
            }

            LogExecution("spawn request item=" + rawItemName + " rawAmount=" + (rawAmount ?? String.Empty) + " rawBlessed=" + (rawBlessed ?? String.Empty));

            int amount = 1;
            if (!String.IsNullOrWhiteSpace(rawAmount))
            {
                int parsedAmount;
                if (Int32.TryParse(rawAmount, out parsedAmount))
                    amount = parsedAmount;
            }

            if (amount < 1)
                amount = 1;

            bool blessed = false;
            if (!String.IsNullOrWhiteSpace(rawBlessed))
                Boolean.TryParse(rawBlessed, out blessed);

            Container pack = inventory.GetPrimaryContainer();
            if (pack == null)
            {
                message = "The counselor pack could not be prepared.";
                return false;
            }

            string[] ctorArgs = BuildAddConstructorArgs(rawItemName, amount);

            Item item;
            string error;
            if (!AIGMAddAdapter.TryCreateItemInContainer(from, pack, rawItemName, ctorArgs, null, out item, out error))
            {
                LogExecution("add adapter failed error=" + (error ?? String.Empty) + " fallingBack=true");
                if (!TryCreateAllowedCounselorPackItem(rawItemName, amount, blessed, out item, out error))
                {
                    message = !String.IsNullOrWhiteSpace(error)
                        ? error
                        : "I do not yet know how to create that item safely.";
                    LogExecution("fallback failed error=" + (message ?? String.Empty));
                    return false;
                }

                if (item == null)
                {
                    message = "Item creation returned no result.";
                    LogExecution("fallback built null item");
                    return false;
                }

                bool droppedFallback = inventory.TryDropCreatedItem(from, item, rawItemName, out message);
                LogExecution("fallback success dropped=" + droppedFallback + " message=" + (message ?? String.Empty));
                return droppedFallback;
            }

            TryApplyBlessed(item, blessed);
            bool dropped = inventory.TryDropCreatedItem(from, item, rawItemName, out message);
            LogExecution("add adapter success dropped=" + dropped + " message=" + (message ?? String.Empty));
            return dropped;
        }

        private static string[] BuildAddConstructorArgs(string itemName, int amount)
        {
            string alias = NormalizeItemAlias(itemName);
            if (String.IsNullOrWhiteSpace(alias))
                return new string[0];

            switch (alias)
            {
                case "bandage":
                case "bandages":
                case "blank scroll":
                case "blank scrolls":
                case "gold":
                case "black pearl":
                case "bloodmoss":
                case "garlic":
                case "ginseng":
                case "mandrake root":
                case "nightshade":
                case "sulfurous ash":
                case "spider silk":
                case "spiders silk":
                    return new string[] { Math.Max(1, amount).ToString() };
                default:
                    return new string[0];
            }
        }

        private static bool TryCreateAllowedCounselorPackItem(string itemName, int amount, bool blessed, out Item item, out string error)
        {
            item = null;
            error = null;

            string alias = NormalizeItemAlias(itemName);
            if (String.IsNullOrWhiteSpace(alias))
            {
                error = "I do not yet know how to create that item safely.";
                return false;
            }

            amount = Math.Max(1, amount);

            switch (alias)
            {
                case "katana":
                    item = new Katana();
                    break;
                case "longsword":
                    item = new Longsword();
                    break;
                case "broadsword":
                    item = new Broadsword();
                    break;
                case "dagger":
                    item = new Dagger();
                    break;
                case "bag":
                    item = new Bag();
                    break;
                case "backpack":
                    item = new Backpack();
                    break;
                case "bandage":
                case "bandages":
                    item = new Bandage(Math.Min(amount, 1000));
                    break;
                case "blank scroll":
                case "blank scrolls":
                    item = new BlankScroll(Math.Min(amount, 1000));
                    break;
                case "recall rune":
                case "rune":
                    item = new RecallRune();
                    break;
                case "gold":
                    item = new Gold(Math.Min(amount, 100000));
                    break;
                case "black pearl":
                    item = new BlackPearl(Math.Min(amount, 1000));
                    break;
                case "bloodmoss":
                    item = new Bloodmoss(Math.Min(amount, 1000));
                    break;
                case "garlic":
                    item = new Garlic(Math.Min(amount, 1000));
                    break;
                case "ginseng":
                    item = new Ginseng(Math.Min(amount, 1000));
                    break;
                case "mandrake root":
                    item = new MandrakeRoot(Math.Min(amount, 1000));
                    break;
                case "nightshade":
                    item = new Nightshade(Math.Min(amount, 1000));
                    break;
                case "sulfurous ash":
                    item = new SulfurousAsh(Math.Min(amount, 1000));
                    break;
                case "spiders silk":
                case "spider silk":
                    item = new SpidersSilk(Math.Min(amount, 1000));
                    break;
                default:
                    error = "I do not yet know how to create that item safely.";
                    return false;
            }

            TryApplyBlessed(item, blessed);
            return true;
        }

        private static string NormalizeItemAlias(string raw)
        {
            if (String.IsNullOrWhiteSpace(raw))
                return null;

            string alias = raw.Trim().ToLowerInvariant();

            while (alias.Contains("  "))
                alias = alias.Replace("  ", " ");

            if (alias == "black pearls")
                alias = "black pearl";
            else if (alias == "mandrake")
                alias = "mandrake root";
            else if (alias == "mandrakes")
                alias = "mandrake root";
            else if (alias == "spider silk")
                alias = "spiders silk";
            else if (alias == "recall runes")
                alias = "recall rune";

            return alias;
        }

        private static void TryApplyBlessed(Item item, bool blessed)
        {
            if (item == null || !blessed)
                return;

            try
            {
                item.LootType = LootType.Blessed;
            }
            catch
            {
            }
        }


    }
}
