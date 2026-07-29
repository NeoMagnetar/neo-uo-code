using System;
using System.Collections.Generic;

using Server;
using Server.Mobiles;

namespace Server.Custom.AIGM.Tasks
{
    public static class AIGMRosterCompanionCapacityService
    {
        public static int MaxRosterCompanions { get; set; } = 20;

        public static int CountBoundCompanions(Mobile owner)
        {
            if (owner == null)
                return 0;

            int count = 0;
            foreach (Mobile mobile in World.Mobiles.Values)
            {
                if (mobile is IAIGMRosterTaskAgent agent && agent.RosterBoundCompanion && agent.RosterTrustedCommanderSerial == owner.Serial)
                    count++;
            }

            return count;
        }

        public static bool TryBind(IAIGMRosterTaskAgent agent, Mobile owner, out string response)
        {
            return AIGMRosterCompanionBindingService.TryBind(agent, owner, out response);
        }

        public static void Unbind(IAIGMRosterTaskAgent agent)
        {
            string ignored;
            AIGMRosterCompanionBindingService.TryUnbind(agent, null, out ignored);
        }

        public static bool IsTrustedCommander(IAIGMRosterTaskAgent agent, Mobile speaker)
        {
            if (agent == null || speaker == null)
                return false;

            if (speaker.AccessLevel >= AccessLevel.GameMaster)
                return true;

            if (agent.RosterBoundCompanion && agent.RosterTrustedCommanderSerial == speaker.Serial)
                return true;

            if (speaker is IAIGMRosterTaskAgent commander)
            {
                string commanderId = commander.RosterCharacterId;
                string agentId = agent.RosterCharacterId;

                if (AIGMRosterFactionService.IsCommander(commanderId))
                {
                    AIGMRosterFaction commanderFaction = AIGMRosterFactionService.ResolveFaction(commanderId);
                    AIGMRosterFaction agentFaction = AIGMRosterFactionService.ResolveFaction(agentId);
                    if (commanderFaction == agentFaction || AIGMRosterFactionService.AreAllied(commanderFaction, agentFaction))
                        return true;
                }
            }

            return false;
        }
    }
}
