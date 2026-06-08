using System;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public sealed class AIGMCounselorInventoryCapability : IAIGMInventoryCapability
    {
        private readonly AIGMCounselor _counselor;

        public AIGMCounselorInventoryCapability(AIGMCounselor counselor)
        {
            _counselor = counselor;
        }

        public bool HasAccessiblePack
        {
            get
            {
                return GetPrimaryContainer() != null;
            }
        }

        public Container GetPrimaryContainer()
        {
            if (_counselor == null || _counselor.Deleted)
                return null;

            return _counselor.Backpack;
        }

        public bool TryOpenPrimaryContainer(Mobile requester, out string message)
        {
            message = null;

            Container pack = GetPrimaryContainer();
            if (requester == null || pack == null)
            {
                message = "The counselor pack could not be prepared.";
                return false;
            }

            requester.SendGump(new PropertiesGump(requester, pack));
            message = "Opened the counselor's pack.";
            return true;
        }

        public bool TryDropItem(Mobile requester, Item item, out string message)
        {
            message = null;

            Container pack = GetPrimaryContainer();
            if (requester == null || pack == null || item == null)
            {
                message = "The counselor pack could not be prepared.";
                return false;
            }

            if (!pack.TryDropItem(requester, item, false))
            {
                message = "The counselor pack could not receive that item.";
                return false;
            }

            message = "Item placed into the counselor's pack.";
            return true;
        }

        public bool TryDropCreatedItem(Mobile requester, Item item, string requestedName, out string message)
        {
            message = null;

            if (item == null)
            {
                message = "Item creation returned no result.";
                return false;
            }

            Container pack = GetPrimaryContainer();
            if (pack == null)
            {
                message = "The counselor pack could not be prepared.";
                return false;
            }

            pack.DropItem(item);

            int finalAmount = GetEffectiveDisplayAmount(item);
            string displayName = GetDisplayItemName(item, requestedName);

            if (finalAmount > 1)
                message = String.Format("Created {0} {1} in the counselor's pack.", finalAmount, displayName);
            else
                message = String.Format("Created {0} in the counselor's pack.", displayName);

            return true;
        }

        private static int GetEffectiveDisplayAmount(Item item)
        {
            if (item == null)
                return 1;

            if (item.Stackable && item.Amount > 1)
                return item.Amount;

            return 1;
        }

        private static string GetDisplayItemName(Item item, string fallbackRawName)
        {
            if (item != null)
            {
                if (!String.IsNullOrWhiteSpace(item.Name))
                    return item.Name;

                string label = item.GetType().Name;
                if (!String.IsNullOrWhiteSpace(label))
                    return label;
            }

            return String.IsNullOrWhiteSpace(fallbackRawName) ? "item" : fallbackRawName.Trim();
        }
    }
}
