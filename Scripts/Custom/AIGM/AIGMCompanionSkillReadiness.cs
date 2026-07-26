using System;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionSkillReadiness
    {
        public static string BuildTrackingReadiness(BaseHire companion)
        {
            if (companion == null)
                return "I have no clear read on my tracking readiness.";

            double tracking = GetSkillValue(companion, SkillName.Tracking);
            double detectHidden = GetSkillValue(companion, SkillName.DetectHidden);
            double forensics = GetOptionalSkillValue(companion, "Forensics");
            double cartography = GetOptionalSkillValue(companion, "Cartography");

            string trackingTier = BuildTier(tracking);
            string detectTier = BuildTier(detectHidden);

            if (tracking < 30.0)
                return String.Format("My Tracking is {0} at {1:0.0}, so I can watch the area, but I would not trust myself to follow a trail yet.", trackingTier, tracking);

            string extra = detectHidden > 0.0 ? String.Format(" Detect Hidden is {0} at {1:0.0}.", detectTier, detectHidden) : String.Empty;
            if (forensics > 0.0)
                extra += String.Format(" Forensics reads as {0} at {1:0.0}.", BuildTier(forensics), forensics);
            else if (cartography > 0.0)
                extra += String.Format(" Cartography reads as {0} at {1:0.0}.", BuildTier(cartography), cartography);

            return String.Format("My Tracking is {0} at {1:0.0}, so I can read signs with {2}.{3}", trackingTier, tracking, ConfidenceWord(tracking), extra).Trim();
        }

        public static string BuildHealingReadiness(BaseHire companion)
        {
            if (companion == null)
                return "I have no clear read on my healing readiness.";

            double healing = GetSkillValue(companion, SkillName.Healing);
            double anatomy = GetSkillValue(companion, SkillName.Anatomy);
            double magery = GetSkillValue(companion, SkillName.Magery);
            double meditation = GetSkillValue(companion, SkillName.Meditation);
            double evalInt = GetSkillValue(companion, SkillName.EvalInt);

            return String.Format(
                "My Healing is {0} at {1:0.0}, Anatomy is {2} at {3:0.0}, and Magery is {4} at {5:0.0}. Meditation stands at {6:0.0}.{7}",
                BuildTier(healing), healing,
                BuildTier(anatomy), anatomy,
                BuildTier(magery), magery,
                meditation,
                evalInt > 0.0 ? String.Format(" Eval Int is {0} at {1:0.0}.", BuildTier(evalInt), evalInt) : String.Empty);
        }

        public static string BuildSupportReadiness(BaseHire companion)
        {
            if (companion == null)
                return "I have no clear read on my support readiness.";

            double healing = GetSkillValue(companion, SkillName.Healing);
            double anatomy = GetSkillValue(companion, SkillName.Anatomy);
            double magery = GetSkillValue(companion, SkillName.Magery);
            double meditation = GetSkillValue(companion, SkillName.Meditation);

            return String.Format("My support readiness is grounded in Healing {0} ({1:0.0}), Anatomy {2} ({3:0.0}), Magery {4} ({5:0.0}), and Meditation {6} ({7:0.0}).",
                BuildTier(healing), healing,
                BuildTier(anatomy), anatomy,
                BuildTier(magery), magery,
                BuildTier(meditation), meditation);
        }

        public static string BuildCombatReadiness(BaseHire companion)
        {
            if (companion == null)
                return "I have no clear read on my combat readiness.";

            double tactics = GetSkillValue(companion, SkillName.Tactics);
            double anatomy = GetSkillValue(companion, SkillName.Anatomy);
            double parry = GetSkillValue(companion, SkillName.Parry);
            double resist = GetSkillValue(companion, SkillName.MagicResist);
            double archery = GetSkillValue(companion, SkillName.Archery);
            double swords = GetSkillValue(companion, SkillName.Swords);
            double macing = GetSkillValue(companion, SkillName.Macing);
            double fencing = GetSkillValue(companion, SkillName.Fencing);
            double wrestling = GetSkillValue(companion, SkillName.Wrestling);
            double magery = GetSkillValue(companion, SkillName.Magery);

            string mainWeapon = DescribePrimaryCombatSkill(archery, swords, macing, fencing, wrestling);
            return String.Format("My Tactics are {0} at {1:0.0}, Parry is {2} at {3:0.0}, Resist is {4} at {5:0.0}, and my leading combat arm is {6}. Anatomy stands at {7:0.0}, Magery at {8:0.0}.",
                BuildTier(tactics), tactics,
                BuildTier(parry), parry,
                BuildTier(resist), resist,
                mainWeapon,
                anatomy,
                magery);
        }

        public static string BuildTravelReadiness(BaseHire companion)
        {
            if (companion == null)
                return "I have no clear read on route readiness.";

            double tracking = GetSkillValue(companion, SkillName.Tracking);
            double cartography = GetOptionalSkillValue(companion, "Cartography");
            string cartographyText = cartography > 0.0
                ? String.Format(" Cartography is {0} at {1:0.0}.", BuildTier(cartography), cartography)
                : String.Empty;

            return String.Format("My route sense rests on Tracking {0} at {1:0.0}.{2}", BuildTier(tracking), tracking, cartographyText).Trim();
        }

        public static double GetSkillValue(Mobile m, SkillName skill)
        {
            if (m == null || m.Skills == null)
                return 0.0;

            Skill s = m.Skills[skill];
            return s != null ? s.Value : 0.0;
        }

        public static string BuildTier(double skillValue)
        {
            if (skillValue < 30.0)
                return "untrained";
            if (skillValue < 50.0)
                return "basic";
            if (skillValue < 70.0)
                return "capable";
            if (skillValue < 90.0)
                return "strong";
            if (skillValue < 100.0)
                return "expert";
            return "master";
        }

        private static double GetOptionalSkillValue(Mobile m, string skillName)
        {
            if (m == null || m.Skills == null || String.IsNullOrWhiteSpace(skillName))
                return 0.0;

            SkillName parsed;
            if (!Enum.TryParse(skillName, true, out parsed))
                return 0.0;

            return GetSkillValue(m, parsed);
        }

        private static string ConfidenceWord(double skill)
        {
            if (skill >= 100.0)
                return "confidence";
            if (skill >= 70.0)
                return "good confidence";
            if (skill >= 50.0)
                return "some confidence";
            return "caution";
        }

        private static string DescribePrimaryCombatSkill(double archery, double swords, double macing, double fencing, double wrestling)
        {
            double best = archery;
            string label = String.Format("Archery {0} ({1:0.0})", BuildTier(archery), archery);

            if (swords > best)
            {
                best = swords;
                label = String.Format("Swordsmanship {0} ({1:0.0})", BuildTier(swords), swords);
            }

            if (macing > best)
            {
                best = macing;
                label = String.Format("Macing {0} ({1:0.0})", BuildTier(macing), macing);
            }

            if (fencing > best)
            {
                best = fencing;
                label = String.Format("Fencing {0} ({1:0.0})", BuildTier(fencing), fencing);
            }

            if (wrestling > best)
                label = String.Format("Wrestling {0} ({1:0.0})", BuildTier(wrestling), wrestling);

            return label;
        }
    }
}
