using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionTurnCoordinator
    {
        private sealed class TurnDecision
        {
            public Serial VisibleCompanionSerial;
            public DateTime ChosenUtc;
        }

        private static readonly ConcurrentDictionary<string, TurnDecision> Decisions = new ConcurrentDictionary<string, TurnDecision>();
        private static readonly TimeSpan DecisionTtl = TimeSpan.FromSeconds(4.0);

        public static bool ShouldCompanionTakeVisibleTurn(IAIGMCompanionActor companion, Mobile speaker, string rawSpeech)
        {
            if (companion == null || companion.Shell == null || companion.Shell.Deleted || speaker == null || speaker.Deleted)
                return false;

            Prune();

            string key = BuildTurnKey(speaker, companion.Shell.Map != null ? companion.Shell.Map.Name : String.Empty, rawSpeech);
            TurnDecision decision = Decisions.GetOrAdd(key, _ => BuildDecision(companion, speaker, rawSpeech));
            return decision.VisibleCompanionSerial == companion.Shell.Serial;
        }

        private static TurnDecision BuildDecision(IAIGMCompanionActor companion, Mobile speaker, string rawSpeech)
        {
            List<IAIGMCompanionActor> linked = GetLinked(companion, speaker);
            if (linked.Count == 0)
            {
                return new TurnDecision
                {
                    VisibleCompanionSerial = companion.Shell.Serial,
                    ChosenUtc = DateTime.UtcNow
                };
            }

            linked.Sort((a, b) => String.Compare(a.CompanionId, b.CompanionId, StringComparison.OrdinalIgnoreCase));

            List<string> addressedIds = AIGMCompanionCommandBoundary.GetAddressedCompanionIds(rawSpeech);
            if (addressedIds != null && addressedIds.Count == 1)
            {
                foreach (IAIGMCompanionActor actor in linked)
                {
                    if (String.Equals(actor.CompanionId, addressedIds[0], StringComparison.OrdinalIgnoreCase))
                    {
                        return new TurnDecision
                        {
                            VisibleCompanionSerial = actor.Shell.Serial,
                            ChosenUtc = DateTime.UtcNow
                        };
                    }
                }
            }

            IAIGMCompanionActor chosen = ChooseStableDefault(linked, speaker);
            return new TurnDecision
            {
                VisibleCompanionSerial = chosen != null && chosen.Shell != null ? chosen.Shell.Serial : companion.Shell.Serial,
                ChosenUtc = DateTime.UtcNow
            };
        }

        private static IAIGMCompanionActor ChooseStableDefault(List<IAIGMCompanionActor> linked, Mobile speaker)
        {
            if (linked == null || linked.Count == 0)
                return null;

            foreach (string preferred in new[] { "dakeyras", "danyal", "dardalion" })
            {
                foreach (IAIGMCompanionActor actor in linked)
                {
                    if (String.Equals(actor.CompanionId, preferred, StringComparison.OrdinalIgnoreCase))
                        return actor;
                }
            }

            return linked[0];
        }

        private static List<IAIGMCompanionActor> GetLinked(IAIGMCompanionActor companion, Mobile speaker)
        {
            BaseHire self = companion != null ? companion.Shell as BaseHire : null;
            Mobile owner = self != null ? self.GetOwner() : null;
            if (owner == null || owner != speaker)
                return new List<IAIGMCompanionActor> { companion };

            return AIGMCompanionSpeechBus.GetLinkedCompanionsIncludingSource(companion, owner);
        }

        private static string BuildTurnKey(Mobile speaker, string mapName, string rawSpeech)
        {
            return String.Format("{0}:{1}:{2}", speaker.Serial.Value, mapName ?? String.Empty, (rawSpeech ?? String.Empty).Trim().ToLowerInvariant());
        }

        private static void Prune()
        {
            DateTime cutoff = DateTime.UtcNow - DecisionTtl;
            foreach (KeyValuePair<string, TurnDecision> pair in Decisions)
            {
                if (pair.Value != null && pair.Value.ChosenUtc >= cutoff)
                    continue;

                Decisions.TryRemove(pair.Key, out _);
            }
        }
    }
}
