using Server;
using Server.Custom.AIGM.Characters.Waylander;
using Server.Mobiles;

namespace Server.Custom.AIGM.Tasks
{
    public interface IAIGMRosterTaskAgent
    {
        string RosterCharacterId { get; }
        bool RosterPassiveTestMode { get; }
        bool RosterBoundCompanion { get; set; }
        Serial RosterTrustedCommanderSerial { get; set; }
        AIGMRosterTaskState RosterTaskState { get; set; }
        WaylanderRosterDisposition RosterDisposition { get; }
        WaylanderCharacterDefinition RosterDefinition { get; }
    }
}
