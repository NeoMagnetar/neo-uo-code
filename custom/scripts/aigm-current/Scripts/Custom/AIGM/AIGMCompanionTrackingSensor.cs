using System;
using System.Collections.Generic;
using Server.Mobiles;
using Server.SkillHandlers;
using Server.Spells;
using Server.Spells.Necromancy;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionTrackingSensor
    {
        public static AIGMCompanionTrackingSweep Sweep(BaseHire companion, AIGMTrackingCategory category)
        {
            if (companion == null || companion.Deleted || companion.Map == null)
                return null;

            int range = 10 + (int)(companion.Skills[SkillName.Tracking].Value / 10.0);
            AIGMCompanionTrackingSweep sweep = new AIGMCompanionTrackingSweep();
            sweep.CompanionSerial = companion.Serial.Value;
            sweep.CompanionName = companion.Name ?? companion.GetType().Name;
            sweep.Category = category;
            sweep.Range = range;
            sweep.TimestampUtc = DateTime.UtcNow;

            List<Mobile> matches = new List<Mobile>();
            IPooledEnumerable eable = companion.GetMobilesInRange(range);
            foreach (Mobile mob in eable)
            {
                if (mob == null || mob == companion || mob.Deleted || mob.Map != companion.Map)
                    continue;

                if (!MatchesCategory(mob, category))
                    continue;

                if (!CanTrack(companion, mob))
                    continue;

                matches.Add(mob);
            }
            eable.Free();

            matches.Sort(delegate(Mobile x, Mobile y)
            {
                return companion.GetDistanceToSqrt(x).CompareTo(companion.GetDistanceToSqrt(y));
            });

            for (int i = 0; i < matches.Count; i++)
            {
                Mobile mob = matches[i];
                int distance = (int)Math.Round(companion.GetDistanceToSqrt(mob));
                AIGMCompanionTrackingEntry entry = new AIGMCompanionTrackingEntry();
                entry.TargetSerial = mob.Serial.Value;
                entry.Name = mob.Name ?? mob.GetType().Name;
                entry.TypeName = mob.GetType().Name;
                entry.Category = category;
                entry.Distance = distance;
                entry.DirectionApprox = companion.GetDirectionTo(mob).ToString();
                entry.MapName = mob.Map != null ? mob.Map.Name : String.Empty;
                entry.X = mob.X;
                entry.Y = mob.Y;
                entry.Z = mob.Z;
                entry.HiddenKnown = mob.Hidden;
                entry.IsAlive = mob.Alive;
                entry.ThreatHint = AIGMCompanionThreatClassifier.Classify(companion, mob, category, distance);
                entry.TimestampUtc = DateTime.UtcNow;
                sweep.Entries.Add(entry);
            }

            return sweep;
        }

        public static List<AIGMCompanionTrackingSweep> SweepAll(BaseHire companion)
        {
            List<AIGMCompanionTrackingSweep> sweeps = new List<AIGMCompanionTrackingSweep>();
            sweeps.Add(Sweep(companion, AIGMTrackingCategory.Animals));
            sweeps.Add(Sweep(companion, AIGMTrackingCategory.Monsters));
            sweeps.Add(Sweep(companion, AIGMTrackingCategory.HumanNPCs));
            sweeps.Add(Sweep(companion, AIGMTrackingCategory.Players));
            return sweeps;
        }

        private static bool MatchesCategory(Mobile mob, AIGMTrackingCategory category)
        {
            switch (category)
            {
                case AIGMTrackingCategory.Animals:
                    return !mob.Player && mob.Body.IsAnimal;
                case AIGMTrackingCategory.Monsters:
                    return !mob.Player && mob.Body.IsMonster;
                case AIGMTrackingCategory.HumanNPCs:
                    return !mob.Player && mob.Body.IsHuman;
                case AIGMTrackingCategory.Players:
                    return mob.Player;
                default:
                    return false;
            }
        }

        private static bool CanTrack(Mobile tracker, Mobile target)
        {
            if (tracker == null || target == null)
                return false;

            if (target == tracker)
                return false;

            if (Core.AOS && !target.Alive)
                return false;

            if (target.Hidden && !target.Player && tracker.AccessLevel <= target.AccessLevel)
                return false;

            return CheckDifficulty(tracker, target);
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

            int chance;
            if (divisor > 0)
                chance = 50 * (tracking * 2 + detectHidden) / divisor;
            else
                chance = 100;

            return chance > Utility.Random(100);
        }
    }
}
