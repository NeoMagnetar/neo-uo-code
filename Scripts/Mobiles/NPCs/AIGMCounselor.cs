using System;
using System.Collections.Generic;
using System.IO;
using Server.ContextMenus;
using Server.Custom.AIGM;
using Server.Gumps;
using Server.Items;
using Server.Multis;
using Server.Network;

namespace Server.Mobiles
{
    public class AIGMCounselor : PlayerVendor, IAIGMActor
    {
        private const int CounselorHue = 0x455;

        [Constructable]
        public AIGMCounselor()
            : base(null, null)
        {
            Name = AIGMSettings.CounselorName;
            Title = AIGMSettings.CounselorTitle;
            Female = false;
            Body = 0x190;
            Hue = Utility.RandomSkinHue();
            SpeechHue = 0x3B2;
            Blessed = true;
            CantWalk = true;
            VendorSearch = false;
            HoldGold = 0;
            BankAccount = 0;

            EnsureCounselorPresentation();
        }

        public override bool IsCommission { get { return true; } }

        public Mobile Shell { get { return this; } }
        public string ActorId { get { return Serial.Value.ToString(); } }
        public string DisplayName { get { return Name ?? "AI Counselor"; } }
        public IAIGMInventoryCapability Inventory { get { return new AIGMCounselorInventoryCapability(this); } }

        public override void InitOutfit()
        {
            Utility.AssignRandomHair(this);
            EnsureCounselorPresentation();
        }

        public override bool IsOwner(Mobile m)
        {
            return m != null && m.AccessLevel >= AIGMSettings.RequiredAccess;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null)
                return;

            LogVendorFlow("OnDoubleClick from=" + SafeName(from) + " owner=" + IsOwner(from) + " newVendorSystem=" + BaseHouse.NewVendorSystem + " house=" + (House != null) + " placeholder=" + (Placeholder != null) + " backpack=" + (Backpack != null ? Backpack.GetType().Name : "null"));

            if (from.AccessLevel < AIGMSettings.RequiredAccess)
            {
                SayTo(from, "These archives are reserved for staff.");
                return;
            }

            from.CloseGump(typeof(AIGMCounselorManagementGump));
            from.SendGump(new AIGMCounselorManagementGump(this));
        }

        public override void DisplayPaperdollTo(Mobile m)
        {
            LogVendorFlow("DisplayPaperdollTo viewer=" + SafeName(m) + " owner=" + IsOwner(m) + " newVendorSystem=" + BaseHouse.NewVendorSystem);
            base.DisplayPaperdollTo(m);
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);

            if (IsOwner(from))
                list.Add(new ConsultCounselorEntry(from, this));
        }

        public override bool HandlesOnSpeech(Mobile from)
        {
            return from != null && from.Alive && from.GetDistanceToSqrt(this) <= 3;
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            base.OnSpeech(e);

            Mobile from = e.Mobile;
            if (e.Handled || from == null || !from.Alive || from.GetDistanceToSqrt(this) > 3)
                return;

            if (!IsOwner(from))
                return;

            string rawSpeech = e.Speech == null ? String.Empty : e.Speech.Trim();
            AIGMCompanionCommandRouteDecision routeDecision = AIGMCompanionCommandBoundary.Classify(rawSpeech);
            if (routeDecision != null && routeDecision.BlocksCounselorLane)
            {
                LogVendorFlow("CounselorEarlyExit speech=" + rawSpeech + " routeKind=" + routeDecision.RouteKind + " reason=" + (routeDecision.Reason ?? String.Empty));
                return;
            }

            string speech = rawSpeech.ToLowerInvariant();
            if (speech.IndexOf("hello counselor") >= 0 || speech.IndexOf("consult counselor") >= 0 || speech.IndexOf("question counselor") >= 0 || speech.IndexOf("archives") >= 0)
            {
                OpenConsultInterface(from);
                e.Handled = true;
            }
        }

        public new void SendOwnerGump(Mobile to)
        {
            LogVendorFlow("SendOwnerGump to=" + SafeName(to) + " owner=" + IsOwner(to) + " newVendorSystem=" + BaseHouse.NewVendorSystem + " backpack=" + (Backpack != null ? Backpack.GetType().Name : "null"));

            if (to == null || !IsOwner(to))
                return;

            base.SendOwnerGump(to);
        }

        public override bool OnDragDrop(Mobile from, Item item)
        {
            LogVendorFlow("OnDragDrop from=" + SafeName(from) + " item=" + (item != null ? item.GetType().Name : "null") + " owner=" + IsOwner(from));

            if (!IsOwner(from))
            {
                SayTo(from, "These archives are reserved for staff.");
                return false;
            }

            return base.OnDragDrop(from, item);
        }

        public override bool CheckNonlocalDrop(Mobile from, Item item, Item target)
        {
            bool allowed = IsOwner(from) && base.CheckNonlocalDrop(from, item, target);
            LogVendorFlow("CheckNonlocalDrop from=" + SafeName(from) + " item=" + (item != null ? item.GetType().Name : "null") + " target=" + (target != null ? target.GetType().Name : "null") + " allowed=" + allowed);
            return allowed;
        }

        public override bool CheckNonlocalLift(Mobile from, Item item)
        {
            bool allowed = base.CheckNonlocalLift(from, item);
            LogVendorFlow("CheckNonlocalLift from=" + SafeName(from) + " item=" + (item != null ? item.GetType().Name : "null") + " parentIsBackpack=" + (item != null && Backpack != null && item.IsChildOf(Backpack)) + " allowed=" + allowed);
            return allowed;
        }

        public override bool AllowEquipFrom(Mobile from)
        {
            bool allowed = base.AllowEquipFrom(from);
            LogVendorFlow("AllowEquipFrom from=" + SafeName(from) + " owner=" + IsOwner(from) + " allowed=" + allowed + " newVendorSystem=" + BaseHouse.NewVendorSystem);
            return allowed;
        }

        public void OpenConsultInterface(Mobile from)
        {
            if (from == null || !IsOwner(from))
                return;

            from.CloseGump(typeof(AIGMQuestionGump));
            from.CloseGump(typeof(AIGMResponseGump));
            from.SendGump(new AIGMQuestionGump(from, this));
            SayTo(from, "State your question, counselor.");
        }

        public void OpenManagementBackpack(Mobile from)
        {
            if (from == null || !IsOwner(from))
                return;

            OpenBackpack(from);
        }

        public void OpenManagementPaperdoll(Mobile from)
        {
            if (from == null || !IsOwner(from))
                return;

            base.DisplayPaperdollTo(from);
        }

        private void EnsureCounselorPresentation()
        {
            EnsureCounselorBackpack();
            EnsureItem<Robe>(Layer.OuterTorso, CounselorHue);
            EnsureItem<Sandals>(Layer.Shoes, CounselorHue);
        }

        private void EnsureCounselorBackpack()
        {
            if (!(Backpack is VendorBackpack) || Backpack.Deleted)
            {
                Container oldPack = Backpack;
                Container newPack = new VendorBackpack();
                newPack.Movable = false;
                AddItem(newPack);

                if (oldPack != null && !oldPack.Deleted)
                {
                    Item[] items = oldPack.Items.ToArray();
                    for (int i = 0; i < items.Length; i++)
                        newPack.DropItem(items[i]);

                    oldPack.Delete();
                }
            }

            if (Backpack != null)
            {
                Backpack.Layer = Layer.Backpack;
                Backpack.Movable = false;
            }
        }

        private void EnsureItem<TItem>(Layer layer, int hue) where TItem : Item, new()
        {
            Item item = FindItemOnLayer(layer);
            TItem typed = item as TItem;

            if (typed == null)
            {
                if (item != null && item.Parent == this)
                    item.Delete();

                typed = new TItem();
                typed.Hue = hue;
                AddItem(typed);
            }
            else
            {
                typed.Hue = hue;
            }
        }

        public AIGMCounselor(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            Name = AIGMSettings.CounselorName;
            Title = AIGMSettings.CounselorTitle;
            Female = false;
            Body = 0x190;
            Hue = Utility.RandomSkinHue();
            SpeechHue = 0x3B2;
            Blessed = true;
            CantWalk = true;
            VendorSearch = false;
            HoldGold = 0;
            BankAccount = 0;
            Timer.DelayCall(TimeSpan.Zero, EnsureCounselorPresentation);
        }

        private static string SafeName(Mobile mob)
        {
            if (mob == null)
                return "null";

            return String.Format("{0}({1})", mob.Name ?? mob.GetType().Name, mob.Serial.Value);
        }

        private static void LogVendorFlow(string message)
        {
            if (!AIGMSettings.EnableDebugLogging)
                return;

            try
            {
                string path = Path.Combine(Core.BaseDirectory, "Logs", "AIGMCounselorVendorFlow.log");
                File.AppendAllText(path, DateTime.UtcNow.ToString("o") + " " + (message ?? String.Empty) + Environment.NewLine);
            }
            catch
            {
            }
        }

        private sealed class ConsultCounselorEntry : ContextMenuEntry
        {
            private readonly Mobile _from;
            private readonly AIGMCounselor _counselor;

            public ConsultCounselorEntry(Mobile from, AIGMCounselor counselor)
                : base(6146, 3)
            {
                _from = from;
                _counselor = counselor;
            }

            public override void OnClick()
            {
                if (_counselor == null || _counselor.Deleted || _from == null)
                    return;

                LogVendorFlow("ConsultCounselorEntry click from=" + SafeName(_from));
                _counselor.OpenConsultInterface(_from);
            }
        }
    }
}
