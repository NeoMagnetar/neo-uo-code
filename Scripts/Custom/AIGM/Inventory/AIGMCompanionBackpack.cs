using System;
using Server.Custom.AIGM.UMG;
using Server.Items;
using Server.Network;
using Server.Targeting;

namespace Server.Custom.AIGM.Inventory
{
    public class AIGMCompanionBackpack : Backpack
    {
        public const int MarkerItemID = 0x0E75;
        public const int MarkerHue = 1175;
        public const int MarkerVersionCurrent = 1;

        private Serial _expectedActorSerial;
        private int _markerVersion;
        private string _provenance;
        private Serial _previousBackpackSerial;
        private DateTime _migrationTimestampUtc;

        [Constructable]
        public AIGMCompanionBackpack()
            : this(null, "manual_constructable", Serial.MinusOne)
        {
        }

        public AIGMCompanionBackpack(Mobile actor, string provenance, Serial previousBackpackSerial)
            : base()
        {
            _expectedActorSerial = actor != null ? actor.Serial : Serial.MinusOne;
            _markerVersion = MarkerVersionCurrent;
            _provenance = String.IsNullOrWhiteSpace(provenance) ? "created" : provenance;
            _previousBackpackSerial = previousBackpackSerial;
            _migrationTimestampUtc = DateTime.UtcNow;
            ApplyMarker();
        }

        public AIGMCompanionBackpack(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Serial ExpectedActorSerial
        {
            get { return _expectedActorSerial; }
            set { _expectedActorSerial = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MarkerVersion
        {
            get { return _markerVersion; }
            set { _markerVersion = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Provenance
        {
            get { return _provenance ?? String.Empty; }
            set { _provenance = value ?? String.Empty; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Serial PreviousBackpackSerial
        {
            get { return _previousBackpackSerial; }
            set { _previousBackpackSerial = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime MigrationTimestampUtc
        {
            get { return _migrationTimestampUtc; }
            set { _migrationTimestampUtc = value; }
        }

        public override bool DisplaysContent { get { return true; } }

        public override void OnDoubleClick(Mobile from)
        {
            Mobile actor = ResolveOwner();
            AIGMCompanionInventoryService.OpenBackpack(from, actor);
        }

        public override void DisplayTo(Mobile to)
        {
            if (AIGMCompanionInventoryService.IsInternalMutation)
            {
                base.DisplayTo(to);
                return;
            }

            Mobile actor = ResolveOwner();
            AIGMCompanionInventoryResult access = AIGMCompanionInventoryService.CanAccess(to, actor, "phase64d1b-display-" + Guid.NewGuid().ToString("N").Substring(0, 10));
            if (!access.Accepted)
            {
                if (to != null)
                    to.SendMessage(38, "{0} correlation={1}", access.Message, access.CorrelationId);

                AIGMCompanionInventoryLog.Write("content_display_denied", actor, AIGMCompanionInventoryLog.Fields("correlationId", access.CorrelationId, "result", access.ResultCode, "caller", Describe(to), "backpack", Describe(this)));
                return;
            }

            AIGMCompanionInventoryLog.Write("content_display_allowed", actor, AIGMCompanionInventoryLog.Fields("correlationId", access.CorrelationId, "result", "allowed", "caller", Describe(to), "backpack", Describe(this), "authorization", access.Authorization));
            base.DisplayTo(to);
        }

        public override bool IsAccessibleTo(Mobile m)
        {
            if (AIGMCompanionInventoryService.IsInternalMutation)
                return base.IsAccessibleTo(m);

            Mobile actor = ResolveOwner();
            AIGMCompanionInventoryResult access = AIGMCompanionInventoryService.CanAccess(m, actor, "phase64d1b-access-" + Guid.NewGuid().ToString("N").Substring(0, 10));
            return access.Accepted && base.IsAccessibleTo(m);
        }

        public override bool CheckTarget(Mobile from, Target targ, object targeted)
        {
            if (AIGMCompanionInventoryService.IsInternalMutation)
                return base.CheckTarget(from, targ, targeted);

            Mobile actor = ResolveOwner();
            AIGMCompanionInventoryResult access = AIGMCompanionInventoryService.CanAccess(from, actor, "phase64d1b-target-" + Guid.NewGuid().ToString("N").Substring(0, 10));
            if (!access.Accepted)
            {
                AIGMCompanionInventoryLog.Write("target_denied", actor, AIGMCompanionInventoryLog.Fields("correlationId", access.CorrelationId, "result", access.ResultCode, "caller", Describe(from), "backpack", Describe(this)));
                return false;
            }

            return base.CheckTarget(from, targ, targeted);
        }

        public override bool CheckItemUse(Mobile from, Item item)
        {
            if (AIGMCompanionInventoryService.IsInternalMutation)
                return base.CheckItemUse(from, item);

            Mobile actor = ResolveOwner();
            AIGMCompanionInventoryResult access = AIGMCompanionInventoryService.CanAccess(from, actor, "phase64d1b-use-" + Guid.NewGuid().ToString("N").Substring(0, 10));
            if (!access.Accepted)
            {
                AIGMCompanionInventoryLog.Write("item_use_denied", actor, AIGMCompanionInventoryLog.Fields("correlationId", access.CorrelationId, "result", access.ResultCode, "caller", Describe(from), "item", Describe(item), "backpack", Describe(this)));
                return false;
            }

            return base.CheckItemUse(from, item);
        }

        public override bool CheckLift(Mobile from, Item item, ref LRReason reject)
        {
            if (AIGMCompanionInventoryService.IsInternalMutation)
                return base.CheckLift(from, item, ref reject);

            Mobile actor = ResolveOwner();
            AIGMCompanionInventoryResult access = AIGMCompanionInventoryService.CanAccess(from, actor, "phase64d1b-lift-" + Guid.NewGuid().ToString("N").Substring(0, 10));
            if (!access.Accepted)
            {
                reject = LRReason.CannotLift;
                AIGMCompanionInventoryLog.Write("lift_denied", actor, AIGMCompanionInventoryLog.Fields("correlationId", access.CorrelationId, "result", access.ResultCode, "caller", Describe(from), "item", Describe(item), "backpack", Describe(this)));
                return false;
            }

            bool allowed = base.CheckLift(from, item, ref reject);
            AIGMCompanionInventoryLog.Write(allowed ? "lift_allowed" : "lift_denied", actor, AIGMCompanionInventoryLog.Fields("correlationId", access.CorrelationId, "result", allowed ? "allowed" : reject.ToString(), "caller", Describe(from), "item", Describe(item), "backpack", Describe(this), "authorization", access.Authorization));
            return allowed;
        }

        public override bool CheckHold(Mobile m, Item item, bool message, bool checkItems, int plusItems, int plusWeight)
        {
            if (AIGMCompanionInventoryService.IsInternalMutation)
                return base.CheckHold(m, item, message, checkItems, plusItems, plusWeight);

            Mobile actor = ResolveOwner();
            AIGMCompanionInventoryResult access = AIGMCompanionInventoryService.CanAccess(m, actor, "phase64d1b-drop-" + Guid.NewGuid().ToString("N").Substring(0, 10));
            if (!access.Accepted)
            {
                if (message && m != null)
                    m.SendMessage(38, "{0} correlation={1}", access.Message, access.CorrelationId);

                AIGMCompanionInventoryLog.Write("drop_denied", actor, AIGMCompanionInventoryLog.Fields("correlationId", access.CorrelationId, "result", access.ResultCode, "caller", Describe(m), "item", Describe(item), "backpack", Describe(this)));
                return false;
            }

            bool allowed = base.CheckHold(m, item, message, checkItems, plusItems, plusWeight);
            AIGMCompanionInventoryLog.Write(allowed ? "drop_allowed" : "drop_denied", actor, AIGMCompanionInventoryLog.Fields("correlationId", access.CorrelationId, "result", allowed ? "allowed" : "stock_checkhold_denied", "caller", Describe(m), "item", Describe(item), "backpack", Describe(this), "authorization", access.Authorization));
            return allowed;
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            return AIGMCompanionInventoryService.IsInternalMutation
                ? base.OnDragDrop(from, dropped)
                : base.OnDragDrop(from, dropped);
        }

        public override bool OnDragDropInto(Mobile from, Item item, Point3D p)
        {
            return AIGMCompanionInventoryService.IsInternalMutation
                ? base.OnDragDropInto(from, item, p)
                : base.OnDragDropInto(from, item, p);
        }

        public override bool TryDropItem(Mobile from, Item dropped, bool sendFullMessage)
        {
            return AIGMCompanionInventoryService.IsInternalMutation
                ? base.TryDropItem(from, dropped, sendFullMessage)
                : base.TryDropItem(from, dropped, sendFullMessage);
        }

        public override bool VerifyMove(Mobile from)
        {
            return false;
        }

        public override bool OnDragLift(Mobile from)
        {
            AIGMCompanionInventoryLog.Write("backpack_lift_denied", ResolveOwner(), AIGMCompanionInventoryLog.Fields("correlationId", "phase64d1b-packmove-" + Guid.NewGuid().ToString("N").Substring(0, 10), "caller", Describe(from), "backpack", Describe(this), "result", "backpack_not_movable"));
            return false;
        }

        public override bool CanEquip(Mobile m)
        {
            if (m == null || m.Player)
                return false;

            IAIGMCompanionActor ignored;
            if (!AIGMUMGSleeveAccessService.IsRegisteredAIGMCompanion(m, out ignored))
                return false;

            if (_expectedActorSerial.IsValid && _expectedActorSerial != m.Serial)
                return false;

            return base.CanEquip(m);
        }

        public override bool OnEquip(Mobile from)
        {
            return CanEquip(from) && base.OnEquip(from);
        }

        public override DeathMoveResult OnInventoryDeath(Mobile parent)
        {
            return DeathMoveResult.MoveToBackpack;
        }

        public override void OnParentDeleted(object parent)
        {
            AIGMCompanionInventoryLog.Write("deletion_cleanup", parent as Mobile, AIGMCompanionInventoryLog.Fields("correlationId", "phase64d1b-delete-" + Guid.NewGuid().ToString("N").Substring(0, 10), "backpack", Describe(this), "expectedActor", Format(_expectedActorSerial)));
            base.OnParentDeleted(parent);
        }

        public override void OnAfterDelete()
        {
            AIGMCompanionInventoryRegistry.MarkDeleted(this, "backpack_deleted");
            base.OnAfterDelete();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1);
            writer.Write(_markerVersion);
            writer.Write(_expectedActorSerial);
            writer.Write(_provenance ?? String.Empty);
            writer.Write(_previousBackpackSerial);
            writer.Write(_migrationTimestampUtc);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            if (version >= 1)
            {
                _markerVersion = reader.ReadInt();
                _expectedActorSerial = reader.ReadInt();
                _provenance = reader.ReadString();
                _previousBackpackSerial = reader.ReadInt();
                _migrationTimestampUtc = reader.ReadDateTime();
            }
            else
            {
                _markerVersion = MarkerVersionCurrent;
                _expectedActorSerial = Serial.MinusOne;
                _provenance = "legacy_deserialize";
                _previousBackpackSerial = Serial.MinusOne;
                _migrationTimestampUtc = DateTime.MinValue;
            }
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add("AIGM Companion Backpack");
            list.Add(String.Format("Marker v{0}", _markerVersion));
        }

        private void ApplyMarker()
        {
            ItemID = MarkerItemID;
            Hue = MarkerHue;
            Layer = Layer.Backpack;
            Weight = 3.0;
            Movable = false;
            LootType = LootType.Blessed;
            Name = "AIGM Companion Backpack";
        }

        private Mobile ResolveOwner()
        {
            Mobile parent = Parent as Mobile;
            if (parent != null)
                return parent;

            return _expectedActorSerial.IsValid ? World.FindMobile(_expectedActorSerial) : null;
        }

        private static string Describe(Mobile mobile)
        {
            if (mobile == null)
                return "none";

            string name = String.IsNullOrWhiteSpace(mobile.Name) ? mobile.GetType().Name : mobile.Name.Replace('"', '\'').Replace('\r', ' ').Replace('\n', ' ');
            return name + "[" + Format(mobile.Serial) + "]";
        }

        private static string Describe(Item item)
        {
            return item == null ? "none" : item.GetType().Name + "[" + Format(item.Serial) + "]";
        }

        private static string Format(Serial serial)
        {
            return serial.IsValid ? String.Format("0x{0:X8}", serial.Value) : "none";
        }
    }
}
