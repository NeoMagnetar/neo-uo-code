using System;
using System.Collections.Generic;
using Server.Mobiles;
using Server.Spells;
using Server.Spells.Necromancy;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionTrackingSensor
    {
        public static Mobile FindClosestTrackableMonster(BaseHire companion, int range, out string acceptedSummary, out string rejectedSummary)
        {
            acceptedSummary = "accepted=none";
            rejectedSummary = "none";

            if (companion == null || companion.Deleted || !companion.Alive || companion.Map == null)
                return null;

            if (range <= 0)
                range = 10 + (int)(companion.Skills[SkillName.Tracking].Value / 10.0);

            List<Mobile> accepted = new List<Mobile>();
            List<string> rejected = new List<string>();

            IPooledEnumerable eable = companion.GetMobilesInRange(range);
            foreach (Mobile mob in eable)
            {
                if (mob == null || mob == companion || mob.Deleted || mob.Map != companion.Map)
                    continue;

                string rejectReason;
                if (!AIGMCompanionTargetValidator.IsHostileMonsterCandidate(companion, mob, out rejectReason))
                {
                    if (rejected.Count < 12)
                        rejected.Add(DescribeCandidate(mob) + "=" + rejectReason);
                    continue;
                }

                if (!CanTrack(companion, mob, out rejectReason))
                {
                    if (rejected.Count < 12)
                        rejected.Add(DescribeCandidate(mob) + "=" + rejectReason);
                    continue;
                }

                accepted.Add(mob);
            }
            eable.Free();

            accepted.Sort(delegate(Mobile x, Mobile y)
            {
                return companion.GetDistanceToSqrt(x).CompareTo(companion.GetDistanceToSqrt(y));
            });

            if (accepted.Count > 0)
            {
                List<string> acceptedText = new List<string>();
                for (int i = 0; i < accepted.Count && i < 12; i++)
                    acceptedText.Add(DescribeCandidate(accepted[i]) + "@" + (int)Math.Round(companion.GetDistanceToSqrt(accepted[i])));
                acceptedSummary = "accepted=" + String.Join(", ", acceptedText.ToArray());
            }

            if (rejected.Count > 0)
                rejectedSummary = String.Join(", ", rejected.ToArray());

            return accepted.Count > 0 ? accepted[0] : null;
        }

        private static bool CanTrack(Mobile tracker, Mobile target, out string rejectReason)
        {
            rejectReason = String.Empty;

            if (tracker == null || target == null)
            {
                rejectReason = "invalid_target";
                return false;
            }

            if (target == tracker)
            {
                rejectReason = "self_target";
                return false;
            }

            if (Core.AOS && !target.Alive)
            {
                rejectReason = "dead_target";
                return false;
            }

            if (target.Hidden && !target.Player && tracker.AccessLevel <= target.AccessLevel)
            {
                rejectReason = "hidden_target";
                return false;
            }

            if (!CheckDifficulty(tracker, target))
            {
                rejectReason = "tracking_difficulty_failed";
                return false;
            }

            return true;
        }

        private static bool CheckDifficulty(Mobile from, Mobile target)
        {
            if (!Core.AOS || !target.Player)
                return true;

            int tracking = from.Skills[SkillName.Tracking].Fixed;
            int detectHidden = from.Skills[SkillName.DetectHidden].Fixed;

            if (Core.ML && target.Race == Race.Elf)
                tracking /= 2;

            int hiding = target.Skills[SkillName.Hiding].Fixed;
            int stealth = target.Skills[SkillName.Stealth].Fixed;
            int divisor = hiding + stealth;

            if (TransformationSpellHelper.UnderTransformation(target, typeof(HorrificBeastSpell)))
                divisor -= 200;
            else if (TransformationSpellHelper.UnderTransformation(target, typeof(VampiricEmbraceSpell)) && divisor < 500)
                divisor = 500;
            else if (TransformationSpellHelper.UnderTransformation(target, typeof(WraithFormSpell)) && divisor <= 2000)
                divisor += 200;

            int chance = divisor > 0 ? 50 * (tracking * 2 + detectHidden) / divisor : 100;
            return chance > Utility.Random(100);
        }

        private static string DescribeCandidate(Mobile mobile)
        {
            if (mobile == null)
                return "null";

            return String.IsNullOrWhiteSpace(mobile.Name) ? mobile.GetType().Name : mobile.Name.Replace(",", String.Empty);
        }
    }
}