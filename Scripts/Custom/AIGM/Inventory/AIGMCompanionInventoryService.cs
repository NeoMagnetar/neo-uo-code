using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Server.Custom.AIGM.UMG;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.AIGM.Inventory
{
    public enum AIGMCompanionBackpackClassification
    {
        NoBackpack,
        StockBackpackEmpty,
        StockBackpackWithContents,
        CreatureBackpack,
        StrongBackpack,
        AIGMBackpack,
        OtherCustomBackpack,
        MultipleBackpacks,
        InvalidBackpackParent,
        Unknown
    }

    public sealed class AIGMCompanionInventoryResult
    {
        public bool Accepted { get; set; }
        public string ResultCode { get; set; }
        public string Message { get; set; }
        public string CorrelationId { get; set; }
        public string Authorization { get; set; }
        public Mobile Actor { get; set; }
        public AIGMCompanionBackpack Backpack { get; set; }
        public AIGMCompanionBackpackAudit Audit { get; set; }

        public AIGMCompanionInventoryResult()
        {
            ResultCode = String.Empty;
            Message = String.Empty;
            CorrelationId = String.Empty;
            Authorization = String.Empty;
        }
    }

    public sealed class AIGMCompanionBackpackAudit
    {
        public AIGMCompanionBackpackClassification Classification { get; set; }
        public Mobile Actor { get; set; }
        public Container Backpack { get; set; }
        public int BackpackLayerItemCount { get; set; }
        public int DirectItemCount { get; set; }
        public int RecursiveItemCount { get; set; }
        public int NestedContainerCount { get; set; }
        public double TotalWeight { get; set; }
        public string ValidationStatus { get; set; }
        public string Provenance { get; set; }
        public int MarkerVersion { get; set; }

        public bool IsNormalized
        {
            get { return Classification == AIGMCompanionBackpackClassification.AIGMBackpack && Backpack is AIGMCompanionBackpack; }
        }
    }

    public static class AIGMCompanionInventoryService
    {
        public const int AccessRange = AIGMUMGSleeveAccessService.AccessRange;
        private static bool _initialized;

        [ThreadStatic]
        private static bool _internalMutation;

        public static void Initialize()
        {
            if (_initialized)
                return;

            _initialized = true;
            AIGMCompanionInventoryRegistry.Reload();
            AIGMCompanionInventoryLog.Write("inventory_service_initialized", null, AIGMCompanionInventoryLog.Fields("markerItemId", String.Format("0x{0:X}", AIGMCompanionBackpack.MarkerItemID), "markerHue", AIGMCompanionBackpack.MarkerHue.ToString(CultureInfo.InvariantCulture), "markerVersion", AIGMCompanionBackpack.MarkerVersionCurrent.ToString(CultureInfo.InvariantCulture)));
        }

        public static bool IsInternalMutation
        {
            get { return _internalMutation; }
        }

        public static List<Mobile> EnumerateRegisteredLiveCompanions()
        {
            List<Mobile> result = new List<Mobile>();
            foreach (Mobile mobile in World.Mobiles.Values)
            {
                IAIGMCompanionActor ignored;
                if (AIGMUMGSleeveAccessService.IsRegisteredAIGMCompanion(mobile, out ignored))
                    result.Add(mobile);
            }

            return result;
        }

        public static AIGMCompanionBackpack GetBackpack(Mobile actor)
        {
            AIGMCompanionBackpackAudit audit = AuditBackpack(actor);
            return audit != null ? audit.Backpack as AIGMCompanionBackpack : null;
        }

        public static AIGMCompanionBackpackAudit AuditBackpack(Mobile actor)
        {
            AIGMCompanionBackpackAudit audit = new AIGMCompanionBackpackAudit
            {
                Actor = actor,
                ValidationStatus = "unchecked",
                Provenance = String.Empty
            };

            if (!IsRegistered(actor))
            {
                audit.Classification = AIGMCompanionBackpackClassification.Unknown;
                audit.ValidationStatus = "not_aigm";
                return audit;
            }

            List<Container> backpacks = FindLayerBackpacks(actor);
            audit.BackpackLayerItemCount = backpacks.Count;

            if (backpacks.Count == 0)
            {
                audit.Classification = AIGMCompanionBackpackClassification.NoBackpack;
                audit.ValidationStatus = "missing";
                return audit;
            }

            if (backpacks.Count > 1)
            {
                audit.Classification = AIGMCompanionBackpackClassification.MultipleBackpacks;
                audit.ValidationStatus = "duplicate_backpacks";
                return audit;
            }

            Container backpack = backpacks[0];
            audit.Backpack = backpack;
            RefreshTotals(backpack);
            audit.DirectItemCount = backpack.Items.Count;
            audit.RecursiveItemCount = CountRecursiveItems(backpack);
            audit.NestedContainerCount = CountNestedContainers(backpack);
            audit.TotalWeight = backpack.TotalWeight;

            if (!Object.ReferenceEquals(backpack.Parent, actor))
            {
                audit.Classification = AIGMCompanionBackpackClassification.InvalidBackpackParent;
                audit.ValidationStatus = "backpack_owner_mismatch";
                return audit;
            }

            AIGMCompanionBackpack aigmBackpack = backpack as AIGMCompanionBackpack;
            if (aigmBackpack != null)
            {
                audit.Classification = AIGMCompanionBackpackClassification.AIGMBackpack;
                audit.MarkerVersion = aigmBackpack.MarkerVersion;
                audit.Provenance = aigmBackpack.Provenance;
                audit.ValidationStatus = ValidateOwnership(aigmBackpack).ResultCode;
                return audit;
            }

            if (backpack.GetType() == typeof(Backpack))
            {
                audit.Classification = audit.RecursiveItemCount > 0
                    ? AIGMCompanionBackpackClassification.StockBackpackWithContents
                    : AIGMCompanionBackpackClassification.StockBackpackEmpty;
                audit.ValidationStatus = "stock_backpack_requires_migration";
                return audit;
            }

            if (backpack is CreatureBackpack)
            {
                audit.Classification = AIGMCompanionBackpackClassification.CreatureBackpack;
                audit.ValidationStatus = "creature_backpack_conflict";
                return audit;
            }

            if (backpack is StrongBackpack)
            {
                audit.Classification = AIGMCompanionBackpackClassification.StrongBackpack;
                audit.ValidationStatus = "strong_backpack_conflict";
                return audit;
            }

            if (backpack is Backpack)
            {
                audit.Classification = AIGMCompanionBackpackClassification.OtherCustomBackpack;
                audit.ValidationStatus = "custom_backpack_requires_review";
                return audit;
            }

            audit.Classification = AIGMCompanionBackpackClassification.Unknown;
            audit.ValidationStatus = "unknown_backpack";
            return audit;
        }

        public static AIGMCompanionInventoryResult EnsureBackpack(Mobile actor, string provenance)
        {
            string correlationId = NewCorrelationId("ensure");
            AIGMCompanionInventoryResult preflight = ValidateActorForMutation(actor, correlationId);
            if (!preflight.Accepted)
                return preflight;

            AIGMCompanionBackpackAudit audit = AuditBackpack(actor);
            if (audit.Classification == AIGMCompanionBackpackClassification.NoBackpack)
                return CreateBackpack(actor, String.IsNullOrWhiteSpace(provenance) ? "created" : provenance, correlationId);

            if (audit.Classification == AIGMCompanionBackpackClassification.AIGMBackpack)
            {
                AIGMCompanionBackpack backpack = (AIGMCompanionBackpack)audit.Backpack;
                ApplyMarker(backpack);
                AIGMCompanionInventoryRegistry.Upsert(actor, backpack, backpack.Provenance, Serial.MinusOne, audit.ValidationStatus);
                Log("ensure_existing", actor, correlationId, "result", audit.ValidationStatus, "backpack", Format(backpack));
                return Accept(actor, backpack, audit, "ensured", String.Format("AIGM backpack already present: {0}.", Format(backpack)), correlationId);
            }

            if (audit.Classification == AIGMCompanionBackpackClassification.StockBackpackEmpty
                || audit.Classification == AIGMCompanionBackpackClassification.StockBackpackWithContents)
            {
                return MigrateBackpack(actor, String.IsNullOrWhiteSpace(provenance) ? "migrated_stock_backpack" : provenance, correlationId);
            }

            Log("ensure_denied", actor, correlationId, "result", "backpack_conflict", "classification", audit.Classification.ToString());
            return Reject(actor, audit, "backpack_conflict", "Backpack normalization stopped: " + audit.ValidationStatus + ".", correlationId);
        }

        public static AIGMCompanionInventoryResult MigrateBackpack(Mobile actor, string provenance)
        {
            return MigrateBackpack(actor, provenance, NewCorrelationId("migration"));
        }

        public static AIGMCompanionInventoryResult OpenBackpack(Mobile caller, Mobile actor)
        {
            string correlationId = NewCorrelationId("open");
            AIGMCompanionInventoryResult access = CanAccess(caller, actor, correlationId);
            if (!access.Accepted)
            {
                Send(caller, access, 38);
                Log("open_denied", actor, correlationId, "result", access.ResultCode, "caller", Describe(caller), "authorization", access.Authorization);
                return access;
            }

            AIGMCompanionBackpackAudit audit = AuditBackpack(actor);
            if (!audit.IsNormalized)
            {
                string code = audit.Classification == AIGMCompanionBackpackClassification.NoBackpack ? "backpack_missing" : "backpack_conflict";
                AIGMCompanionInventoryResult rejected = Reject(actor, audit, code, "Companion backpack is not normalized: " + audit.ValidationStatus + ".", correlationId);
                Send(caller, rejected, 38);
                Log("open_denied", actor, correlationId, "result", rejected.ResultCode, "caller", Describe(caller), "classification", audit.Classification.ToString());
                return rejected;
            }

            AIGMCompanionBackpack backpack = (AIGMCompanionBackpack)audit.Backpack;
            backpack.DisplayTo(caller);
            AIGMCompanionInventoryResult accepted = Accept(actor, backpack, audit, "opened", String.Format("Opening backpack {0} for {1}.", Format(backpack), SafeName(actor)), correlationId);
            accepted.Authorization = access.Authorization;
            Send(caller, accepted, 68);
            Log("open_allowed", actor, correlationId, "result", "opened", "caller", Describe(caller), "backpack", Format(backpack), "authorization", access.Authorization);
            return accepted;
        }

        public static AIGMCompanionInventoryResult CanAccess(Mobile caller, Mobile actor, string correlationId)
        {
            if (String.IsNullOrWhiteSpace(correlationId))
                correlationId = NewCorrelationId("access");

            if (actor == null || actor.Deleted || !actor.Serial.IsValid)
                return Reject(actor, null, "invalid_actor", "Backpack access rejected: invalid actor.", correlationId);

            IAIGMCompanionActor companion;
            if (!AIGMUMGSleeveAccessService.IsRegisteredAIGMCompanion(actor, out companion))
                return Reject(actor, null, "not_aigm", "Backpack access rejected: target is not a registered AIGM companion.", correlationId);

            if (caller == null || caller.Deleted)
                return Reject(actor, null, "unauthorized", "Backpack access rejected: invalid caller.", correlationId);

            string authorization;
            if (!AIGMUMGSleeveAccessService.IsAuthorizedCaller(caller, actor, out authorization))
            {
                AIGMCompanionInventoryResult rejected = Reject(actor, null, "unauthorized", "Backpack access rejected: you are not authorized for this companion.", correlationId);
                rejected.Authorization = authorization;
                return rejected;
            }

            if (caller.AccessLevel < AccessLevel.GameMaster && !AIGMUMGSleeveAccessService.IsActorAliveForAccess(actor))
            {
                AIGMCompanionInventoryResult rejected = Reject(actor, null, "actor_dead", "Backpack access rejected: companion is dead.", correlationId);
                rejected.Authorization = authorization;
                return rejected;
            }

            if (caller.AccessLevel < AccessLevel.GameMaster)
            {
                if (caller.Map != actor.Map)
                {
                    AIGMCompanionInventoryResult rejected = Reject(actor, null, "map_mismatch", "Backpack access rejected: stand on the same map.", correlationId);
                    rejected.Authorization = authorization;
                    return rejected;
                }

                if (!actor.InRange(caller, AccessRange))
                {
                    AIGMCompanionInventoryResult rejected = Reject(actor, null, "out_of_range", String.Format("Backpack access rejected: stand within {0} tiles.", AccessRange), correlationId);
                    rejected.Authorization = authorization;
                    return rejected;
                }
            }

            AIGMCompanionInventoryResult accepted = new AIGMCompanionInventoryResult
            {
                Accepted = true,
                ResultCode = "allowed",
                Message = "Backpack access allowed.",
                CorrelationId = correlationId,
                Actor = actor,
                Authorization = authorization
            };
            return accepted;
        }

        public static AIGMCompanionInventoryResult ValidateOwnership(AIGMCompanionBackpack backpack)
        {
            string correlationId = NewCorrelationId("validate");
            if (backpack == null || backpack.Deleted)
                return Reject(null, null, "backpack_missing", "Backpack validation failed: missing backpack.", correlationId);

            Mobile actor = backpack.Parent as Mobile;
            if (actor == null)
                return Reject(null, null, "backpack_owner_mismatch", "Backpack validation failed: parent is not a mobile.", correlationId);

            IAIGMCompanionActor companion;
            if (!AIGMUMGSleeveAccessService.IsRegisteredAIGMCompanion(actor, out companion))
                return Reject(actor, null, "not_aigm", "Backpack validation failed: parent is not a registered AIGM companion.", correlationId);

            if (backpack.ExpectedActorSerial.IsValid && backpack.ExpectedActorSerial != actor.Serial)
                return Reject(actor, null, "backpack_owner_mismatch", "Backpack validation failed: expected actor serial does not match parent.", correlationId);

            if (backpack.ItemID != AIGMCompanionBackpack.MarkerItemID || backpack.Hue != AIGMCompanionBackpack.MarkerHue || backpack.MarkerVersion != AIGMCompanionBackpack.MarkerVersionCurrent)
                return Reject(actor, null, "marker_mismatch", "Backpack validation failed: marker tuple does not match.", correlationId);

            return Accept(actor, backpack, null, "valid", "Backpack ownership valid.", correlationId);
        }

        public static bool ShouldRetainBackpackContentOnDeath(Mobile actor, Item item)
        {
            if (actor == null || item == null)
                return false;

            AIGMCompanionBackpack backpack = actor.Backpack as AIGMCompanionBackpack;
            if (backpack == null || backpack.Deleted)
                return false;

            bool retain = item.IsChildOf(backpack);
            if (retain)
                Log("death_content_retain", actor, NewCorrelationId("death"), "item", Format(item), "backpack", Format(backpack));

            return retain;
        }

        public static string BuildCompactStatus(Mobile actor)
        {
            AIGMCompanionBackpackAudit audit = AuditBackpack(actor);
            if (audit == null || audit.Backpack == null)
                return "Missing";

            AIGMCompanionBackpack backpack = audit.Backpack as AIGMCompanionBackpack;
            if (backpack == null)
                return String.Format("{0} {1}", audit.Classification, audit.ValidationStatus);

            return String.Format("Present {0} items={1} weight={2} v{3} {4}",
                Format(backpack),
                audit.RecursiveItemCount,
                audit.TotalWeight.ToString(CultureInfo.InvariantCulture),
                backpack.MarkerVersion,
                String.IsNullOrWhiteSpace(backpack.Provenance) ? "unknown" : backpack.Provenance);
        }

        public static string BuildAuditLine(Mobile actor)
        {
            AIGMCompanionBackpackAudit audit = AuditBackpack(actor);
            string pack = audit.Backpack != null ? Format(audit.Backpack) + " " + audit.Backpack.GetType().FullName : "none";
            return String.Format("{0} {1}: {2}; pack={3}; marker=0x{4:X}/hue {5}; items={6}; weight={7}; status={8}",
                SafeName(actor),
                Format(actor),
                audit.Classification,
                pack,
                audit.Backpack != null ? audit.Backpack.ItemID : 0,
                audit.Backpack != null ? audit.Backpack.Hue : 0,
                audit.RecursiveItemCount,
                audit.TotalWeight.ToString(CultureInfo.InvariantCulture),
                audit.ValidationStatus);
        }

        public static AIGMCompanionInventoryResult ResolveSelector(string selector, out Mobile actor)
        {
            actor = null;
            string correlationId = NewCorrelationId("resolve");
            string failureCode;
            string failureMessage;
            if (!AIGMUMGSleeveAccessService.TryResolveCompanionSelector(selector, out actor, out failureCode, out failureMessage))
                return Reject(null, null, NormalizeFailureCode(failureCode), failureMessage, correlationId);

            return new AIGMCompanionInventoryResult
            {
                Accepted = true,
                ResultCode = "resolved",
                Message = String.Format("Resolved {0} ({1}).", SafeName(actor), Format(actor)),
                CorrelationId = correlationId,
                Actor = actor
            };
        }

        private static AIGMCompanionInventoryResult MigrateBackpack(Mobile actor, string provenance, string correlationId)
        {
            AIGMCompanionInventoryResult preflight = ValidateActorForMutation(actor, correlationId);
            if (!preflight.Accepted)
                return preflight;

            AIGMCompanionBackpackAudit audit = AuditBackpack(actor);
            if (audit.Classification != AIGMCompanionBackpackClassification.StockBackpackEmpty
                && audit.Classification != AIGMCompanionBackpackClassification.StockBackpackWithContents)
                return Reject(actor, audit, "backpack_conflict", "Migration requires one stock backpack; found " + audit.Classification + ".", correlationId);

            Container oldPack = audit.Backpack;
            InventorySnapshot before = InventorySnapshot.Capture(oldPack);
            Serial oldSerial = oldPack.Serial;
            AIGMCompanionBackpack newPack = new AIGMCompanionBackpack(actor, provenance, oldSerial);
            bool attached = false;

            try
            {
                _internalMutation = true;
                List<Item> contents = new List<Item>(oldPack.Items);
                for (int i = 0; i < contents.Count; i++)
                    newPack.AddItem(contents[i]);

                RefreshTotals(newPack);
                InventorySnapshot afterMove = InventorySnapshot.Capture(newPack);
                string mismatch;
                if (!InventorySnapshot.Matches(before, afterMove, oldSerial, newPack.Serial, out mismatch))
                    throw new InvalidOperationException("content reconciliation failed: " + mismatch);

                actor.RemoveItem(oldPack);
                actor.AddItem(newPack);
                attached = true;
                ApplyMarker(newPack);
                RefreshTotals(newPack);

                if (!Object.ReferenceEquals(actor.Backpack, newPack) || !Object.ReferenceEquals(newPack.Parent, actor))
                    throw new InvalidOperationException("new backpack did not attach to Layer.Backpack");

                if (oldPack.Items.Count != 0)
                    throw new InvalidOperationException("old backpack is not empty after migration");

                AIGMCompanionBackpackAudit post = AuditBackpack(actor);
                if (!post.IsNormalized)
                    throw new InvalidOperationException("post-migration audit is not normalized: " + post.ValidationStatus);

                AIGMCompanionInventoryRegistry.Upsert(actor, newPack, provenance, oldSerial, "migrated");
                oldPack.Delete();
                Log("migration_allowed", actor, correlationId, "oldBackpack", Format(oldSerial), "newBackpack", Format(newPack), "items", before.Rows.Count.ToString(CultureInfo.InvariantCulture), "weight", before.TotalWeight.ToString(CultureInfo.InvariantCulture));
                return Accept(actor, newPack, post, "migrated", String.Format("Migrated {0} to {1}; preserved {2} item serials.", Format(oldSerial), Format(newPack), before.Rows.Count), correlationId);
            }
            catch (Exception ex)
            {
                try
                {
                    if (attached)
                        actor.RemoveItem(newPack);

                    if (!Object.ReferenceEquals(oldPack.Parent, actor))
                        actor.AddItem(oldPack);

                    List<Item> moved = new List<Item>(newPack.Items);
                    for (int i = 0; i < moved.Count; i++)
                        oldPack.AddItem(moved[i]);

                    newPack.Delete();
                }
                catch (Exception rollback)
                {
                    Log("migration_rollback_error", actor, correlationId, "error", rollback.Message);
                }
                finally
                {
                    _internalMutation = false;
                }

                Log("migration_denied", actor, correlationId, "oldBackpack", Format(oldSerial), "result", "migration_failed", "error", ex.Message);
                return Reject(actor, audit, "migration_failed", "Backpack migration failed and rollback was attempted: " + ex.Message, correlationId);
            }
            finally
            {
                _internalMutation = false;
            }
        }

        private static AIGMCompanionInventoryResult CreateBackpack(Mobile actor, string provenance, string correlationId)
        {
            AIGMCompanionBackpack backpack = new AIGMCompanionBackpack(actor, provenance, Serial.MinusOne);
            _internalMutation = true;
            try
            {
                actor.AddItem(backpack);
            }
            finally
            {
                _internalMutation = false;
            }

            AIGMCompanionBackpackAudit audit = AuditBackpack(actor);
            if (!audit.IsNormalized)
            {
                backpack.Delete();
                Log("ensure_denied", actor, correlationId, "result", "creation_failed", "status", audit.ValidationStatus);
                return Reject(actor, audit, "creation_failed", "Backpack creation failed: " + audit.ValidationStatus, correlationId);
            }

            AIGMCompanionInventoryRegistry.Upsert(actor, backpack, provenance, Serial.MinusOne, "created");
            Log("ensure_allowed", actor, correlationId, "result", "created", "backpack", Format(backpack), "markerHue", backpack.Hue.ToString(CultureInfo.InvariantCulture));
            return Accept(actor, backpack, audit, "created", String.Format("Created AIGM backpack {0}.", Format(backpack)), correlationId);
        }

        private static AIGMCompanionInventoryResult ValidateActorForMutation(Mobile actor, string correlationId)
        {
            if (actor == null || actor.Deleted || !actor.Serial.IsValid)
                return Reject(actor, null, "invalid_actor", "Inventory mutation rejected: invalid actor.", correlationId);

            IAIGMCompanionActor ignored;
            if (!AIGMUMGSleeveAccessService.IsRegisteredAIGMCompanion(actor, out ignored))
                return Reject(actor, null, "not_aigm", "Inventory mutation rejected: target is not a registered AIGM companion.", correlationId);

            return new AIGMCompanionInventoryResult { Accepted = true, ResultCode = "valid_actor", CorrelationId = correlationId, Actor = actor };
        }

        private static List<Container> FindLayerBackpacks(Mobile actor)
        {
            List<Container> backpacks = new List<Container>();
            if (actor == null)
                return backpacks;

            List<Item> items = actor.Items;
            for (int i = 0; i < items.Count; i++)
            {
                Container container = items[i] as Container;
                if (container != null && container.Layer == Layer.Backpack)
                    backpacks.Add(container);
            }

            return backpacks;
        }

        private static bool IsRegistered(Mobile actor)
        {
            IAIGMCompanionActor ignored;
            return AIGMUMGSleeveAccessService.IsRegisteredAIGMCompanion(actor, out ignored);
        }

        private static void ApplyMarker(AIGMCompanionBackpack backpack)
        {
            if (backpack == null)
                return;

            backpack.ItemID = AIGMCompanionBackpack.MarkerItemID;
            backpack.Hue = AIGMCompanionBackpack.MarkerHue;
            backpack.Layer = Layer.Backpack;
            backpack.Movable = false;
            backpack.LootType = LootType.Blessed;
            backpack.MarkerVersion = AIGMCompanionBackpack.MarkerVersionCurrent;
        }

        private static void RefreshTotals(Container container)
        {
            if (container == null)
                return;

            for (int i = 0; i < container.Items.Count; i++)
            {
                Container child = container.Items[i] as Container;
                if (child != null)
                    RefreshTotals(child);
            }

            container.UpdateTotals();
        }

        private static int CountRecursiveItems(Container container)
        {
            if (container == null)
                return 0;

            int count = 0;
            for (int i = 0; i < container.Items.Count; i++)
            {
                count++;
                Container child = container.Items[i] as Container;
                if (child != null)
                    count += CountRecursiveItems(child);
            }

            return count;
        }

        private static int CountNestedContainers(Container container)
        {
            if (container == null)
                return 0;

            int count = 0;
            for (int i = 0; i < container.Items.Count; i++)
            {
                Container child = container.Items[i] as Container;
                if (child == null)
                    continue;

                count++;
                count += CountNestedContainers(child);
            }

            return count;
        }

        private static string NormalizeFailureCode(string code)
        {
            if (String.Equals(code, "non_aigm_mobile", StringComparison.OrdinalIgnoreCase))
                return "not_aigm";

            return String.IsNullOrWhiteSpace(code) ? "rejected" : code;
        }

        private static AIGMCompanionInventoryResult Accept(Mobile actor, AIGMCompanionBackpack backpack, AIGMCompanionBackpackAudit audit, string code, string message, string correlationId)
        {
            return new AIGMCompanionInventoryResult
            {
                Accepted = true,
                ResultCode = code,
                Message = message,
                CorrelationId = correlationId,
                Actor = actor,
                Backpack = backpack,
                Audit = audit
            };
        }

        private static AIGMCompanionInventoryResult Reject(Mobile actor, AIGMCompanionBackpackAudit audit, string code, string message, string correlationId)
        {
            return new AIGMCompanionInventoryResult
            {
                Accepted = false,
                ResultCode = code ?? "rejected",
                Message = message ?? "Backpack operation rejected.",
                CorrelationId = correlationId,
                Actor = actor,
                Audit = audit
            };
        }

        private static void Send(Mobile caller, AIGMCompanionInventoryResult result, int hue)
        {
            if (caller != null && result != null)
                caller.SendMessage(hue, "{0} correlation={1}", result.Message, result.CorrelationId);
        }

        private static void Log(string eventName, Mobile actor, string correlationId, params string[] fields)
        {
            List<string> values = new List<string>();
            values.Add("correlationId");
            values.Add(correlationId ?? String.Empty);
            if (fields != null)
                values.AddRange(fields);

            AIGMCompanionInventoryLog.Write(eventName, actor, AIGMCompanionInventoryLog.Fields(values.ToArray()));
        }

        private static string NewCorrelationId(string verb)
        {
            return "phase64d1b-" + (String.IsNullOrWhiteSpace(verb) ? "inventory" : verb) + "-" + Guid.NewGuid().ToString("N").Substring(0, 10);
        }

        private static string SafeName(Mobile mobile)
        {
            if (mobile == null)
                return "none";

            return String.IsNullOrWhiteSpace(mobile.Name) ? mobile.GetType().Name : mobile.Name.Replace('"', '\'').Replace('\r', ' ').Replace('\n', ' ');
        }

        private static string Describe(Mobile mobile)
        {
            return mobile == null ? "none" : SafeName(mobile) + "[" + Format(mobile) + "]";
        }

        private static string Format(Mobile mobile)
        {
            return mobile != null ? Format(mobile.Serial) : "none";
        }

        private static string Format(Item item)
        {
            return item != null ? Format(item.Serial) : "none";
        }

        private static string Format(Serial serial)
        {
            return serial.IsValid ? String.Format("0x{0:X8}", serial.Value) : "none";
        }

        private sealed class InventorySnapshot
        {
            public List<ItemSnapshotRow> Rows { get; private set; }
            public double TotalWeight { get; private set; }

            public static InventorySnapshot Capture(Container container)
            {
                InventorySnapshot snapshot = new InventorySnapshot { Rows = new List<ItemSnapshotRow>() };
                if (container != null)
                {
                    RefreshTotals(container);
                    snapshot.TotalWeight = container.TotalWeight;
                    CaptureRows(container, snapshot.Rows);
                }

                snapshot.Rows.Sort(delegate (ItemSnapshotRow left, ItemSnapshotRow right) { return left.Serial.CompareTo(right.Serial); });
                return snapshot;
            }

            public static bool Matches(InventorySnapshot before, InventorySnapshot after, Serial oldPackSerial, Serial newPackSerial, out string mismatch)
            {
                mismatch = String.Empty;
                if (before == null || after == null)
                {
                    mismatch = "missing snapshot";
                    return false;
                }

                if (before.Rows.Count != after.Rows.Count)
                {
                    mismatch = "item count changed";
                    return false;
                }

                if (Math.Abs(before.TotalWeight - after.TotalWeight) > 0.001)
                {
                    mismatch = "weight changed";
                    return false;
                }

                Dictionary<int, ItemSnapshotRow> afterBySerial = new Dictionary<int, ItemSnapshotRow>();
                for (int i = 0; i < after.Rows.Count; i++)
                    afterBySerial[after.Rows[i].Serial] = after.Rows[i];

                for (int i = 0; i < before.Rows.Count; i++)
                {
                    ItemSnapshotRow oldRow = before.Rows[i];
                    ItemSnapshotRow newRow;
                    if (!afterBySerial.TryGetValue(oldRow.Serial, out newRow))
                    {
                        mismatch = "missing item serial " + Format((Server.Serial)oldRow.Serial);
                        return false;
                    }

                    int expectedParent = oldRow.ParentSerial == oldPackSerial.Value ? newPackSerial.Value : oldRow.ParentSerial;
                    if (newRow.ParentSerial != expectedParent)
                    {
                        mismatch = "parent changed for " + Format((Server.Serial)oldRow.Serial);
                        return false;
                    }

                    if (oldRow.Amount != newRow.Amount || oldRow.TypeName != newRow.TypeName || oldRow.ItemID != newRow.ItemID || oldRow.Hue != newRow.Hue || oldRow.Name != newRow.Name || oldRow.X != newRow.X || oldRow.Y != newRow.Y || oldRow.Z != newRow.Z || oldRow.HitPoints != newRow.HitPoints || oldRow.MaxHitPoints != newRow.MaxHitPoints || oldRow.Charges != newRow.Charges)
                    {
                        mismatch = "properties changed for " + Format((Server.Serial)oldRow.Serial);
                        return false;
                    }
                }

                return true;
            }

            private static void CaptureRows(Container container, List<ItemSnapshotRow> rows)
            {
                for (int i = 0; i < container.Items.Count; i++)
                {
                    Item item = container.Items[i];
                    rows.Add(ItemSnapshotRow.FromItem(item));

                    Container child = item as Container;
                    if (child != null)
                        CaptureRows(child, rows);
                }
            }
        }

        private sealed class ItemSnapshotRow
        {
            public int Serial { get; private set; }
            public int ParentSerial { get; private set; }
            public string TypeName { get; private set; }
            public int ItemID { get; private set; }
            public int Hue { get; private set; }
            public string Name { get; private set; }
            public int Amount { get; private set; }
            public int X { get; private set; }
            public int Y { get; private set; }
            public int Z { get; private set; }
            public int? HitPoints { get; private set; }
            public int? MaxHitPoints { get; private set; }
            public string Charges { get; private set; }

            public static ItemSnapshotRow FromItem(Item item)
            {
                return new ItemSnapshotRow
                {
                    Serial = item.Serial.Value,
                    ParentSerial = ResolveParentSerial(item),
                    TypeName = item.GetType().FullName,
                    ItemID = item.ItemID,
                    Hue = item.Hue,
                    Name = item.Name ?? String.Empty,
                    Amount = item.Amount,
                    X = item.Location.X,
                    Y = item.Location.Y,
                    Z = item.Location.Z,
                    HitPoints = OptionalInt(item, "HitPoints"),
                    MaxHitPoints = OptionalInt(item, "MaxHitPoints"),
                    Charges = OptionalString(item, "Charges") ?? OptionalString(item, "UsesRemaining") ?? OptionalString(item, "Uses")
                };
            }

            private static int ResolveParentSerial(Item item)
            {
                if (item.Parent is Item)
                    return ((Item)item.Parent).Serial.Value;

                if (item.Parent is Mobile)
                    return ((Mobile)item.Parent).Serial.Value;

                return Server.Serial.MinusOne.Value;
            }

            private static int? OptionalInt(Item item, string propertyName)
            {
                object value = OptionalValue(item, propertyName);
                if (value == null)
                    return null;

                try
                {
                    return Convert.ToInt32(value, CultureInfo.InvariantCulture);
                }
                catch
                {
                    return null;
                }
            }

            private static string OptionalString(Item item, string propertyName)
            {
                object value = OptionalValue(item, propertyName);
                return value != null ? Convert.ToString(value, CultureInfo.InvariantCulture) : null;
            }

            private static object OptionalValue(Item item, string propertyName)
            {
                try
                {
                    PropertyInfo property = item.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
                    return property != null ? property.GetValue(item, null) : null;
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}
