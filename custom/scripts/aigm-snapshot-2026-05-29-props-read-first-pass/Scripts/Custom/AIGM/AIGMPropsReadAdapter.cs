using System;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMPropsReadAdapter
    {
        public static AIGMExecutionResult Read(Mobile requester, AIGMActionProposal action)
        {
            if (requester == null || requester.Deleted)
                return AIGMExecutionResult.Fail("Requester is missing or deleted.");

            string targetMode = action != null ? action.GetParameter("targetMode", "nearest_mobile") : "nearest_mobile";
            string targetName = action != null ? action.GetParameter("targetName", String.Empty) : String.Empty;
            string targetContainerKind = action != null ? action.GetParameter("targetContainerKind", String.Empty) : String.Empty;

            if (!String.IsNullOrWhiteSpace(targetContainerKind) && !String.IsNullOrWhiteSpace(targetName))
                targetMode = "container_named_item";

            AIGMExecutionLog.Write("GM_PROPS_READ_REQUEST mode={0} targetName={1} targetContainerKind={2}", targetMode, targetName ?? String.Empty, targetContainerKind ?? String.Empty);
            object target = AIGMTargetResolver.Resolve(requester, targetMode, targetName, targetContainerKind);
            if (target == null)
                return AIGMExecutionResult.Fail("No readable target was found for mode: " + targetMode);

            AIGMPropertySnapshot snapshot = BuildSnapshot(target);
            if (snapshot == null)
                return AIGMExecutionResult.Fail("Could not build a property snapshot for the selected target.");

            requester.SendGump(new AIGMWorldInsightGump("AI GM Props Read", snapshot.ToDisplayHtml()));
            AIGMExecutionLog.Write("GM_PROPS_READ_OK mode={0} type={1} serial={2}", targetMode, snapshot.TypeName ?? String.Empty, snapshot.Serial ?? String.Empty);
            return AIGMExecutionResult.Success("Opened AI GM props read for " + (snapshot.Name ?? snapshot.TypeName ?? "target") + ".", Serial.Zero);
        }

        private static AIGMPropertySnapshot BuildSnapshot(object target)
        {
            Mobile mob = target as Mobile;
            if (mob != null)
                return BuildMobileSnapshot(mob);

            Item item = target as Item;
            if (item != null)
                return BuildItemSnapshot(item);

            return null;
        }

        private static AIGMPropertySnapshot BuildMobileSnapshot(Mobile mob)
        {
            AIGMPropertySnapshot snap = new AIGMPropertySnapshot();
            snap.Kind = "Mobile";
            snap.TypeName = mob.GetType().Name;
            snap.Name = mob.Name;
            snap.Serial = String.Format("0x{0:X8}", mob.Serial.Value);
            snap.MapName = mob.Map != null ? mob.Map.Name : null;
            snap.X = mob.X;
            snap.Y = mob.Y;
            snap.Z = mob.Z;
            snap.Hue = mob.Hue;
            snap.Deleted = mob.Deleted;
            snap.Movable = false;
            snap.Alive = mob.Alive;
            snap.Blessed = mob.Blessed;
            snap.Hits = mob.Hits;
            snap.HitsMax = mob.HitsMax;
            snap.Mana = mob.Mana;
            snap.ManaMax = mob.ManaMax;
            snap.Stam = mob.Stam;
            snap.StamMax = mob.StamMax;
            snap.Str = mob.Str;
            snap.Dex = mob.Dex;
            snap.Int = mob.Int;
            snap.AccessLevel = mob.AccessLevel.ToString();
            snap.Properties.Add(new System.Collections.Generic.KeyValuePair<string, string>("Player", mob.Player.ToString()));
            snap.Properties.Add(new System.Collections.Generic.KeyValuePair<string, string>("Female", mob.Female.ToString()));
            snap.Properties.Add(new System.Collections.Generic.KeyValuePair<string, string>("Body", mob.Body.ToString()));
            snap.Properties.Add(new System.Collections.Generic.KeyValuePair<string, string>("Direction", mob.Direction.ToString()));
            snap.Properties.Add(new System.Collections.Generic.KeyValuePair<string, string>("Fame", mob.Fame.ToString()));
            snap.Properties.Add(new System.Collections.Generic.KeyValuePair<string, string>("Karma", mob.Karma.ToString()));
            return snap;
        }

        private static AIGMPropertySnapshot BuildItemSnapshot(Item item)
        {
            AIGMPropertySnapshot snap = new AIGMPropertySnapshot();
            snap.Kind = item is Container ? "Container" : "Item";
            snap.TypeName = item.GetType().Name;
            snap.Name = item.Name;
            snap.Serial = String.Format("0x{0:X8}", item.Serial.Value);
            snap.MapName = item.Map != null ? item.Map.Name : null;
            Point3D loc = item.GetWorldLocation();
            snap.X = loc.X;
            snap.Y = loc.Y;
            snap.Z = loc.Z;
            snap.Hue = item.Hue;
            snap.Deleted = item.Deleted;
            snap.Movable = item.Movable;
            snap.Blessed = item.LootType == LootType.Blessed;
            snap.Amount = item.Amount;
            snap.ItemID = item.ItemID;
            snap.Weight = item.Weight;
            snap.Layer = item.Layer.ToString();

            Container container = item as Container;
            if (container != null)
                snap.ItemCount = container.Items != null ? container.Items.Count : 0;

            snap.Properties.Add(new System.Collections.Generic.KeyValuePair<string, string>("ItemID", item.ItemID.ToString()));
            snap.Properties.Add(new System.Collections.Generic.KeyValuePair<string, string>("LootType", item.LootType.ToString()));
            snap.Properties.Add(new System.Collections.Generic.KeyValuePair<string, string>("Visible", item.Visible.ToString()));
            snap.Properties.Add(new System.Collections.Generic.KeyValuePair<string, string>("Stackable", item.Stackable.ToString()));
            snap.Properties.Add(new System.Collections.Generic.KeyValuePair<string, string>("Parent", item.Parent != null ? item.Parent.GetType().Name : "(world)"));
            return snap;
        }
    }
}
