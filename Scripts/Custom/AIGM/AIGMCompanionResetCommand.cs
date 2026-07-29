using System;
using System.Collections.Generic;

using Server;
using Server.Commands;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionResetCommand
    {
        public static void Initialize()
        {
            CommandSystem.Register("AIGMCompanionReset", AIGMSettings.RequiredAccess, OnCommand);
        }

        private static void OnCommand(CommandEventArgs e)
        {
            Mobile from = e != null ? e.Mobile : null;
            if (from == null || from.Deleted)
                return;

            List<BaseHire> companions = GetKnownOwnedCompanions(from);
            if (companions.Count == 0)
            {
                from.SendMessage("No owned AIGM companions were found to reset.");
                return;
            }

            List<string> resetNames = new List<string>();
            List<string> warnings = new List<string>();

            for (int i = 0; i < companions.Count; i++)
            {
                BaseHire companion = companions[i];
                if (companion == null || companion.Deleted)
                    continue;

                ResetCompanion(companion, from, warnings);
                resetNames.Add(SafeName(companion));
            }

            from.SendMessage("AIGM companion reset complete: {0}.", String.Join(", ", resetNames.ToArray()));

            for (int i = 0; i < warnings.Count; i++)
                from.SendMessage(warnings[i]);
        }

        private static List<BaseHire> GetKnownOwnedCompanions(Mobile owner)
        {
            List<BaseHire> result = AIGMCompanionControlStopService.GetOwnedCompanions(owner, 0);
            HashSet<int> seen = new HashSet<int>();

            for (int i = 0; i < result.Count; i++)
                seen.Add(result[i].Serial.Value);

            foreach (Mobile mobile in World.Mobiles.Values)
            {
                BaseHire hire = mobile as BaseHire;
                IAIGMCompanionActor actor = mobile as IAIGMCompanionActor;
                if (hire == null || actor == null || hire.Deleted || hire.GetOwner() != owner)
                    continue;

                if (seen.Contains(hire.Serial.Value))
                    continue;

                seen.Add(hire.Serial.Value);
                result.Add(hire);
            }

            result.Sort(CompareCompanionOrder);
            return result;
        }

        private static void ResetCompanion(BaseHire companion, Mobile owner, List<string> warnings)
        {
            string ignored;

            AIGMNativeNavigationService.Stop(companion, "emergency_reset", out ignored);
            AIGMSmartMovementService.Stop(companion, "emergency_reset", out ignored);
            AIGMCompanionLocateService.StopLocate(companion, "emergency_reset");

            AIGMCompanionControlStateService.RestoreFollowOwner(companion, owner, "emergency_reset");

            if (companion.Map == null)
                warnings.Add(String.Format("AIGM companion reset warning: {0} has no map and could not be validated for local follow.", SafeName(companion)));
            else if (owner.Map == null || companion.Map != owner.Map)
                warnings.Add(String.Format("AIGM companion reset warning: {0} is on {1} while {2} is on {3}.", SafeName(companion), SafeMap(companion.Map), SafeName(owner), SafeMap(owner.Map)));
            else if (!companion.Alive)
                warnings.Add(String.Format("AIGM companion reset warning: {0} is not alive.", SafeName(companion)));

            AIGMExecutionLog.Write(
                "AIGM_COMPANION_RESET companion={0} order={1} cantWalk={2} frozen={3} controlTarget={4} home={5} rangeHome={6} ownerDist={7}",
                Describe(companion),
                companion.ControlOrder,
                companion.CantWalk,
                companion.Frozen,
                Describe(owner),
                FormatPoint(companion.Home),
                companion.RangeHome,
                GetOwnerDistance(companion, owner));
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

        private static int GetOwnerDistance(BaseHire companion, Mobile owner)
        {
            if (companion == null || owner == null || companion.Map == null || owner.Map == null || companion.Map != owner.Map)
                return -1;

            int dx = companion.X - owner.X;
            int dy = companion.Y - owner.Y;
            return (int)Math.Round(Math.Sqrt((dx * dx) + (dy * dy)));
        }

        private static string Describe(Mobile mobile)
        {
            if (mobile == null)
                return "none";

            return String.Format("{0}[0x{1:X8}]", SafeName(mobile), mobile.Serial.Value);
        }

        private static string SafeName(Mobile mobile)
        {
            return mobile == null ? "unknown" : (mobile.Name ?? mobile.GetType().Name);
        }

        private static string SafeMap(Map map)
        {
            return map == null ? "null" : map.Name;
        }

        private static string FormatPoint(Point3D point)
        {
            return String.Format("{0},{1},{2}", point.X, point.Y, point.Z);
        }
    }
}
