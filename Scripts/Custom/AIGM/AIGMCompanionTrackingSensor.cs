using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Spells;
using Server.Spells.Necromancy;

namespace Server.Custom.AIGM
{
    public sealed class AIGMTrackingSelectionResult
    {
        public AIGMCompanionTrackingMode Mode { get; set; }
        public Mobile SelectedTarget { get; set; }
        public string AcceptedSummary { get; set; }
        public string RejectedSummary { get; set; }
        public string SelectedName { get; set; }
        public string SelectedDirection { get; set; }
        public string SelectedDistanceText { get; set; }
        public string SelectedTileText { get; set; }
        public string SelectionReason { get; set; }
        public int ScanRange { get; set; }
        public string CategorySummary { get; set; }
        public string TargetCategory { get; set; }
        public bool PursuitAllowed { get; set; }
        public bool AttackAllowed { get; set; }

        public AIGMTrackingSelectionResult()
        {
            AcceptedSummary = "accepted=none";
            RejectedSummary = "none";
            SelectedName = String.Empty;
            SelectedDirection = String.Empty;
            SelectedDistanceText = String.Empty;
            SelectedTileText = String.Empty;
            SelectionReason = "no_valid_targets";
            CategorySummary = String.Empty;
            TargetCategory = String.Empty;
            PursuitAllowed = false;
            AttackAllowed = false;
        }
    }

    public static class AIGMCompanionTrackingSensor
    {
        public static AIGMTrackingSelectionResult SelectNearest(BaseHire companion, AIGMCompanionTrackingMode mode, int range)
        {
            if (mode == AIGMCompanionTrackingMode.All)
                return SelectAllCategories(companion, range);

            AIGMTrackingSelectionResult result = new AIGMTrackingSelectionResult();
            result.Mode = mode;

            if (companion == null || companion.Deleted || !companion.Alive || companion.Map == null)
            {
                result.SelectionReason = "invalid_companion";
                return result;
            }

            if (range <= 0)
                range = 10 + (int)(companion.Skills[SkillName.Tracking].Value / 10.0);

            result.ScanRange = range;

            List<Mobile> accepted = new List<Mobile>();
            List<string> rejected = new List<string>();

            IPooledEnumerable eable = companion.GetMobilesInRange(range);
            foreach (Mobile mob in eable)
            {
                if (mob == null || mob == companion || mob.Deleted || mob.Map != companion.Map)
                    continue;

                string rejectReason;
                if (!MatchesMode(companion, mob, mode, out rejectReason))
                {
                    if (rejected.Count < 16)
                        rejected.Add(DescribeCandidate(mob) + "=" + rejectReason);
                    continue;
                }

                if (!CanTrack(companion, mob, out rejectReason))
                {
                    if (rejected.Count < 16)
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
                for (int i = 0; i < accepted.Count && i < 16; i++)
                    acceptedText.Add(DescribeCandidate(accepted[i]) + "@" + (int)Math.Round(companion.GetDistanceToSqrt(accepted[i])));
                result.AcceptedSummary = "accepted=" + String.Join(", ", acceptedText.ToArray());

                Mobile first = accepted[0];
                result.SelectedTarget = first;
                result.SelectedName = DescribeCandidate(first);
                result.SelectedDirection = DescribeDirection(companion, first);
                result.SelectedDistanceText = ((int)Math.Round(companion.GetDistanceToSqrt(first))) + " tiles";
                result.SelectedTileText = String.Format("{0},{1},{2}", first.X, first.Y, first.Z);
                result.SelectionReason = "target_available";
                result.TargetCategory = DescribeModeLabel(mode);
            }

            if (rejected.Count > 0)
                result.RejectedSummary = String.Join(", ", rejected.ToArray());

            return result;
        }

        public static AIGMTrackingSelectionResult SelectClosestHostileMonster(BaseHire companion, int range)
        {
            return SelectNearest(companion, AIGMCompanionTrackingMode.Monsters, range);
        }

        public static Mobile FindClosestTrackableMonster(BaseHire companion, int range, out string acceptedSummary, out string rejectedSummary)
        {
            AIGMTrackingSelectionResult result = SelectClosestHostileMonster(companion, range);
            acceptedSummary = result.AcceptedSummary;
            rejectedSummary = result.RejectedSummary;
            return result.SelectedTarget;
        }

        private static AIGMTrackingSelectionResult SelectAllCategories(BaseHire companion, int range)
        {
            AIGMTrackingSelectionResult result = new AIGMTrackingSelectionResult();
            result.Mode = AIGMCompanionTrackingMode.All;

            if (companion == null || companion.Deleted || !companion.Alive || companion.Map == null)
            {
                result.SelectionReason = "invalid_companion";
                return result;
            }

            AIGMTrackingSelectionResult animals = SelectNearest(companion, AIGMCompanionTrackingMode.Animals, range);
            AIGMTrackingSelectionResult monsters = SelectNearest(companion, AIGMCompanionTrackingMode.Monsters, range);
            AIGMTrackingSelectionResult npcs = SelectNearest(companion, AIGMCompanionTrackingMode.NPCs, range);
            AIGMTrackingSelectionResult humanNpcs = SelectNearest(companion, AIGMCompanionTrackingMode.HumanNPCs, range);
            AIGMTrackingSelectionResult players = SelectNearest(companion, AIGMCompanionTrackingMode.Players, range);

            result.ScanRange = animals.ScanRange > 0 ? animals.ScanRange : monsters.ScanRange > 0 ? monsters.ScanRange : npcs.ScanRange > 0 ? npcs.ScanRange : humanNpcs.ScanRange > 0 ? humanNpcs.ScanRange : players.ScanRange;
            result.CategorySummary = String.Format("Animals={0}; Monsters={1}; NPCs={2}; HumanNPCs={3}; Players={4}",
                SummarizeNearest(animals),
                SummarizeNearest(monsters),
                SummarizeNearest(npcs),
                SummarizeNearest(humanNpcs),
                SummarizeNearest(players));

            result.AcceptedSummary = result.CategorySummary;
            result.RejectedSummary = JoinRejections(animals.RejectedSummary, monsters.RejectedSummary, npcs.RejectedSummary, humanNpcs.RejectedSummary, players.RejectedSummary);

            Mobile selected = SelectNearestOverall(companion, animals.SelectedTarget, monsters.SelectedTarget, npcs.SelectedTarget, humanNpcs.SelectedTarget, players.SelectedTarget);
            if (selected != null)
            {
                result.SelectedTarget = selected;
                result.SelectedName = DescribeCandidate(selected);
                result.SelectedDirection = DescribeDirection(companion, selected);
                result.SelectedDistanceText = ((int)Math.Round(companion.GetDistanceToSqrt(selected))) + " tiles";
                result.SelectedTileText = String.Format("{0},{1},{2}", selected.X, selected.Y, selected.Z);
                result.SelectionReason = "target_available";
                result.TargetCategory = DescribeCategoryForMobile(companion, selected);
            }
            else
            {
                result.SelectionReason = "no_valid_targets";
            }

            return result;
        }

        private static Mobile SelectNearestOverall(BaseHire companion, params Mobile[] mobiles)
        {
            Mobile best = null;
            double bestDistance = Double.MaxValue;

            if (companion == null || mobiles == null)
                return null;

            for (int i = 0; i < mobiles.Length; i++)
            {
                Mobile mob = mobiles[i];
                if (mob == null || mob.Deleted || mob.Map != companion.Map)
                    continue;

                double distance = companion.GetDistanceToSqrt(mob);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = mob;
                }
            }

            return best;
        }

        private static string SummarizeNearest(AIGMTrackingSelectionResult result)
        {
            if (result == null || result.SelectedTarget == null)
                return "none";

            return result.SelectedName + "@" + ExtractDistanceNumber(result.SelectedDistanceText);
        }

        private static string ExtractDistanceNumber(string distanceText)
        {
            if (String.IsNullOrWhiteSpace(distanceText))
                return "0";

            int space = distanceText.IndexOf(' ');
            return space > 0 ? distanceText.Substring(0, space) : distanceText;
        }

        private static string JoinRejections(params string[] values)
        {
            List<string> items = new List<string>();
            if (values != null)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    string value = values[i];
                    if (!String.IsNullOrWhiteSpace(value) && !value.Equals("none", StringComparison.OrdinalIgnoreCase))
                        items.Add(value);
                }
            }

            return items.Count > 0 ? String.Join(" | ", items.ToArray()) : "none";
        }

        private static bool MatchesMode(BaseHire companion, Mobile mob, AIGMCompanionTrackingMode mode, out string rejectReason)
        {
            rejectReason = String.Empty;

            switch (mode)
            {
                case AIGMCompanionTrackingMode.Animals:
                    if (!mob.Player && mob.Body != null && mob.Body.IsAnimal)
                        return true;
                    rejectReason = DescribeClass(companion, mob);
                    return false;
                case AIGMCompanionTrackingMode.Monsters:
                    return AIGMCompanionTargetValidator.IsHostileMonsterCandidate(companion, mob, out rejectReason);
                case AIGMCompanionTrackingMode.Players:
                    if (mob.Player && !IsOwner(companion, mob))
                        return true;
                    rejectReason = IsOwner(companion, mob) ? "owner_target" : DescribeClass(companion, mob);
                    return false;
                case AIGMCompanionTrackingMode.HumanNPCs:
                    if (IsHumanNPC(companion, mob))
                        return true;
                    rejectReason = DescribeClass(companion, mob);
                    return false;
                case AIGMCompanionTrackingMode.NPCs:
                    if (IsHumanNPC(companion, mob) || IsCivilianNPC(companion, mob))
                        return true;
                    rejectReason = DescribeClass(companion, mob);
                    return false;
                case AIGMCompanionTrackingMode.Threats:
                    return AIGMCompanionTargetValidator.IsHostileMonsterCandidate(companion, mob, out rejectReason);
                case AIGMCompanionTrackingMode.General:
                case AIGMCompanionTrackingMode.All:
                    rejectReason = "use_all_summary";
                    return false;
                default:
                    rejectReason = "unsupported_mode";
                    return false;
            }
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

        private static bool IsOwner(BaseHire companion, Mobile mob)
        {
            Mobile owner = companion != null ? companion.GetOwner() : null;
            return owner != null && mob == owner;
        }

        private static bool IsHumanNPC(BaseHire companion, Mobile mob)
        {
            if (mob == null || mob.Player || IsOwner(companion, mob) || mob is BaseHire || mob is IAIGMCompanionActor)
                return false;

            if (mob.Body != null && mob.Body.IsHuman)
                return true;

            if (mob is BaseVendor || mob is BaseEscortable)
                return true;

            return false;
        }

        private static bool IsCivilianNPC(BaseHire companion, Mobile mob)
        {
            if (mob == null || mob.Player || IsOwner(companion, mob) || mob is BaseHire || mob is IAIGMCompanionActor)
                return false;

            if (mob is BaseVendor || mob is BaseEscortable)
                return true;

            if (mob.Body != null)
            {
                if (mob.Body.IsAnimal || mob.Body.IsMonster)
                    return false;

                if (mob.Body.IsHuman)
                    return true;
            }

            return mob is BaseCreature;
        }

        private static string DescribeCandidate(Mobile mobile)
        {
            if (mobile == null)
                return "null";

            return String.IsNullOrWhiteSpace(mobile.Name) ? mobile.GetType().Name : mobile.Name.Replace(",", String.Empty);
        }

        private static string DescribeDirection(Mobile from, Mobile target)
        {
            if (from == null || target == null)
                return "nearby";

            int dx = target.X - from.X;
            int dy = target.Y - from.Y;

            string vertical = String.Empty;
            string horizontal = String.Empty;

            if (dy < 0)
                vertical = "north";
            else if (dy > 0)
                vertical = "south";

            if (dx > 0)
                horizontal = "east";
            else if (dx < 0)
                horizontal = "west";

            if (String.IsNullOrWhiteSpace(vertical) && String.IsNullOrWhiteSpace(horizontal))
                return "nearby";
            if (String.IsNullOrWhiteSpace(vertical))
                return horizontal;
            if (String.IsNullOrWhiteSpace(horizontal))
                return vertical;
            return vertical + "-" + horizontal;
        }

        private static string DescribeClass(BaseHire companion, Mobile mobile)
        {
            if (mobile == null)
                return "missing_target";
            if (mobile == companion)
                return "self_target";
            if (IsOwner(companion, mobile))
                return "owner_target";
            if (mobile is IAIGMCompanionActor || mobile is BaseHire)
                return "companion_target";
            if (mobile.Player)
                return "player";
            if (mobile.Body != null && mobile.Body.IsAnimal)
                return "animal_target";
            if (mobile.Body != null && mobile.Body.IsHuman)
                return "human_npc";
            if (mobile is BaseVendor || mobile is BaseEscortable)
                return "civilian_target";
            if (mobile.Body != null && mobile.Body.IsMonster)
                return "monster";
            return "other";
        }

        private static string DescribeCategoryForMobile(BaseHire companion, Mobile mobile)
        {
            if (mobile == null)
                return "none";
            if (!mobile.Player && mobile.Body != null && mobile.Body.IsAnimal)
                return "animal";
            if (AIGMCompanionTargetValidator.IsHostileMonsterCandidate(companion, mobile, out _))
                return "monster";
            if (mobile.Player)
                return "player";
            if (IsHumanNPC(companion, mobile) || IsCivilianNPC(companion, mobile))
                return "npc";
            return "other";
        }

        private static string DescribeModeLabel(AIGMCompanionTrackingMode mode)
        {
            switch (mode)
            {
                case AIGMCompanionTrackingMode.Animals: return "animal";
                case AIGMCompanionTrackingMode.Monsters: return "monster";
                case AIGMCompanionTrackingMode.Players: return "player";
                case AIGMCompanionTrackingMode.HumanNPCs: return "human npc";
                case AIGMCompanionTrackingMode.NPCs: return "npc";
                default: return "sign";
            }
        }
    }
}