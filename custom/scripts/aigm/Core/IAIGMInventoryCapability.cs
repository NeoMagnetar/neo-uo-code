using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public interface IAIGMInventoryCapability
    {
        bool HasAccessiblePack { get; }
        Container GetPrimaryContainer();
        bool TryOpenPrimaryContainer(Mobile requester, out string message);
        bool TryDropItem(Mobile requester, Item item, out string message);
        bool TryDropCreatedItem(Mobile requester, Item item, string requestedName, out string message);
    }
}
