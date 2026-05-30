using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public interface IAIGMActor
    {
        Mobile Shell { get; }
        string ActorId { get; }
        string DisplayName { get; }
        IAIGMInventoryCapability Inventory { get; }
    }
}
