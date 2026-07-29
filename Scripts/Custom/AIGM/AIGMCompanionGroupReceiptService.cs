using System;
using System.Collections.Generic;

using Server;
using Server.Custom.AIGM.Tasks;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionGroupReceiptService
    {
        private static readonly Dictionary<int, RecentReceipt> RecentReceipts = new Dictionary<int, RecentReceipt>();
        private static readonly TimeSpan DuplicateWindow = TimeSpan.FromSeconds(1.0);

        public static bool TryHandle(BaseHire listener, Mobile speaker, string rawSpeech, out string response)
        {
            response = null;

            if (listener == null || speaker == null || String.IsNullOrWhiteSpace(rawSpeech))
                return false;

            string normalized = Normalize(rawSpeech);
            string payload;
            if (!TryStripGroupPrefix(normalized, out payload))
                return false;

            GroupAction action;
            if (!TryResolveAction(payload, out action))
                return false;

            if (!IsPreferredCompanion(listener, speaker))
            {
                if (!HasPreferredCompanion(speaker))
                    return false;

                response = String.Empty;
                return true;
            }

            string duplicateKey = speaker.Serial.Value + "|" + normalized;
            if (IsDuplicate(duplicateKey))
            {
                response = String.Empty;
                return true;
            }

            Remember(duplicateKey);

            List<BaseHire> companions = FindOwnedCompanionsIncludingRange(speaker, 30);
            if (companions.Count == 0)
            {
                response = "Command receipt: no owned AIGM companions found.";
                return true;
            }

            List<string> lines = new List<string>();
            lines.Add(action == GroupAction.Follow ? "Follow owner:" : "Stay:");

            for (int i = 0; i < companions.Count; i++)
            {
                BaseHire companion = companions[i];
                string result;
                if (companion.Deleted || !companion.Alive)
                {
                    result = "dead/deleted";
                }
                else if (companion.Map != speaker.Map)
                {
                    result = "wrong map";
                }
                else if (!companion.InRange(speaker, 30))
                {
                    result = "out of range";
                }
                else if (companion.GetOwner() != speaker && speaker.AccessLevel < AccessLevel.GameMaster)
                {
                    result = "not owned";
                }
                else if (action == GroupAction.Follow)
                {
                    AIGMCompanionControlStateService.RestoreFollowOwner(companion, speaker, "group_follow_receipt");
                    result = AIGMOperationalControlService.GetMode(companion) == AIGMOperationalMode.Active ? "started" : "command rejected";
                }
                else
                {
                    AIGMOperationalControlService.ClearAllOperationalState(companion, AIGMOperationalStopMode.CancelMission, speaker, "group_stay_receipt");
                    result = "stopped";
                }

                lines.Add(String.Format("{0}: {1}", SafeName(companion), result));
            }

            response = String.Join(" | ", lines.ToArray());
            return true;
        }

        private static bool TryResolveAction(string payload, out GroupAction action)
        {
            action = GroupAction.None;
            if (payload == "follow" || payload == "follow me" || payload == "come" || payload == "come here" || payload == "come to me")
            {
                action = GroupAction.Follow;
                return true;
            }

            if (payload == "stay" || payload == "stay here" || payload == "wait")
            {
                action = GroupAction.Stay;
                return true;
            }

            return false;
        }

        private static bool TryStripGroupPrefix(string speech, out string payload)
        {
            payload = speech;
            string[] prefixes =
            {
                "companions ",
                "all companions ",
                "everyone ",
                "everybody ",
                "all of you ",
                "you all ",
                "you three ",
                "three of you ",
                "party "
            };

            for (int i = 0; i < prefixes.Length; i++)
            {
                if (speech.StartsWith(prefixes[i], StringComparison.Ordinal))
                {
                    payload = speech.Substring(prefixes[i].Length).Trim();
                    return true;
                }
            }

            return false;
        }

        private static List<BaseHire> FindOwnedCompanionsIncludingRange(Mobile owner, int commandRange)
        {
            List<BaseHire> result = new List<BaseHire>();
            if (owner == null)
                return result;

            foreach (Mobile mobile in World.Mobiles.Values)
            {
                BaseHire hire = mobile as BaseHire;
                IAIGMCompanionActor actor = mobile as IAIGMCompanionActor;
                if (hire == null || actor == null)
                    continue;

                if (hire.GetOwner() != owner && owner.AccessLevel < AccessLevel.GameMaster)
                    continue;

                result.Add(hire);
            }

            result.Sort(CompareCompanionOrder);
            return result;
        }

        private static bool IsPreferredCompanion(BaseHire listener, Mobile speaker)
        {
            if (listener == null || speaker == null)
                return false;

            List<BaseHire> companions = AIGMCompanionControlStopService.GetOwnedCompanions(speaker, 30);
            if (companions.Count == 0)
                return false;

            companions.Sort(CompareCompanionOrder);
            return companions[0] == listener;
        }

        private static bool HasPreferredCompanion(Mobile speaker)
        {
            return AIGMCompanionControlStopService.GetOwnedCompanions(speaker, 30).Count > 0;
        }

        private static int CompareCompanionOrder(BaseHire left, BaseHire right)
        {
            return CompanionRank(left).CompareTo(CompanionRank(right));
        }

        private static int CompanionRank(BaseHire hire)
        {
            IAIGMCompanionActor actor = hire as IAIGMCompanionActor;
            string id = actor != null ? actor.CompanionId : null;
            if (String.Equals(id, "dakeyras", StringComparison.OrdinalIgnoreCase))
                return 0;
            if (String.Equals(id, "danyal", StringComparison.OrdinalIgnoreCase))
                return 1;
            if (String.Equals(id, "dardalion", StringComparison.OrdinalIgnoreCase))
                return 2;
            return 10;
        }

        private static bool IsDuplicate(string key)
        {
            RecentReceipt recent;
            return RecentReceipts.TryGetValue(0, out recent)
                && recent != null
                && recent.Key == key
                && (DateTime.UtcNow - recent.Utc) < DuplicateWindow;
        }

        private static void Remember(string key)
        {
            RecentReceipts[0] = new RecentReceipt { Key = key, Utc = DateTime.UtcNow };
        }

        private static string Normalize(string text)
        {
            string value = text == null ? String.Empty : text.Trim().ToLowerInvariant();
            value = value.Replace(",", " ").Replace(".", " ").Replace("!", " ").Replace("?", " ").Replace(";", " ").Replace(":", " ");
            while (value.Contains("  "))
                value = value.Replace("  ", " ");
            return value.Trim();
        }

        private static string SafeName(Mobile mobile)
        {
            return mobile == null ? "unknown" : (mobile.Name ?? mobile.GetType().Name);
        }

        private enum GroupAction
        {
            None,
            Follow,
            Stay
        }

        private sealed class RecentReceipt
        {
            public string Key;
            public DateTime Utc;
        }
    }
}
