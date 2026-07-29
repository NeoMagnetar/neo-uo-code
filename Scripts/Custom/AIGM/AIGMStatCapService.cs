using Server.Mobiles;
using Server.Commands;

namespace Server.Custom.AIGM
{
    public static class AIGMStatCapService
    {
        private static readonly int TotalStatCap = Config.Get("PlayerCaps.TotalStatCap", 550);
        private static readonly int StrCap = Config.Get("PlayerCaps.StrCap", 200);
        private static readonly int DexCap = Config.Get("PlayerCaps.DexCap", 150);
        private static readonly int IntCap = Config.Get("PlayerCaps.IntCap", 200);
        private static readonly int StrMaxCap = Config.Get("PlayerCaps.StrMaxCap", 200);
        private static readonly int DexMaxCap = Config.Get("PlayerCaps.DexMaxCap", 150);
        private static readonly int IntMaxCap = Config.Get("PlayerCaps.IntMaxCap", 200);
        private static readonly int IndividualSkillCapConfig = Config.Get("PlayerCaps.SkillCap", 10000);
        private static readonly int TotalSkillCap = Config.Get("PlayerCaps.TotalSkillCap", 580000);
        private static readonly double IndividualSkillCap = IndividualSkillCapConfig / 10.0;

        public static void Initialize()
        {
            EventSink.ServerStarted += ApplyToExistingMobiles;
            EventSink.MobileCreated += OnMobileCreated;
            CommandSystem.Register("AIGMCapProof", AccessLevel.GameMaster, OnCapProof);
        }

        private static void ApplyToExistingMobiles()
        {
            int changed = 0;
            int eligible = 0;

            foreach (Mobile mobile in World.Mobiles.Values)
            {
                if (!IsEligible(mobile))
                    continue;

                eligible++;

                if (ApplyIfEligible(mobile))
                    changed++;

                LogCapState("CAP_SERVICE_EXISTING_MOBILE", mobile);
            }

            AIGMExecutionLog.Write("CAP_SERVICE_EXISTING_SUMMARY eligible={0} changed={1} statCap={2} str={3} dex={4} int={5} strMax={6} dexMax={7} intMax={8} skillCapEach={9:F1} totalSkillCap={10}", eligible, changed, TotalStatCap, StrCap, DexCap, IntCap, StrMaxCap, DexMaxCap, IntMaxCap, IndividualSkillCap, TotalSkillCap);
        }

        private static void OnMobileCreated(MobileCreatedEventArgs e)
        {
            if (e == null)
                return;

            if (ApplyIfEligible(e.Mobile))
                LogCapState("CAP_SERVICE_MOBILE_CREATED", e.Mobile);
        }

        private static bool ApplyIfEligible(Mobile mobile)
        {
            if (!IsEligible(mobile))
                return false;

            bool changed = false;

            changed |= SetIfNeeded(mobile, "StatCap", mobile.StatCap, TotalStatCap);
            changed |= SetIfNeeded(mobile, "StrCap", mobile.StrCap, StrCap);
            changed |= SetIfNeeded(mobile, "DexCap", mobile.DexCap, DexCap);
            changed |= SetIfNeeded(mobile, "IntCap", mobile.IntCap, IntCap);
            changed |= SetIfNeeded(mobile, "StrMaxCap", mobile.StrMaxCap, StrMaxCap);
            changed |= SetIfNeeded(mobile, "DexMaxCap", mobile.DexMaxCap, DexMaxCap);
            changed |= SetIfNeeded(mobile, "IntMaxCap", mobile.IntMaxCap, IntMaxCap);
            changed |= ApplySkillCaps(mobile);

            return changed;
        }

        private static bool IsEligible(Mobile mobile)
        {
            if (mobile == null || mobile.Deleted)
                return false;

            return mobile is PlayerMobile || mobile is IAIGMCompanionActor;
        }

        private static bool SetIfNeeded(Mobile mobile, string name, int current, int target)
        {
            if (current == target)
                return false;

            switch (name)
            {
                case "StatCap":
                    mobile.StatCap = target;
                    break;
                case "StrCap":
                    mobile.StrCap = target;
                    break;
                case "DexCap":
                    mobile.DexCap = target;
                    break;
                case "IntCap":
                    mobile.IntCap = target;
                    break;
                case "StrMaxCap":
                    mobile.StrMaxCap = target;
                    break;
                case "DexMaxCap":
                    mobile.DexMaxCap = target;
                    break;
                case "IntMaxCap":
                    mobile.IntMaxCap = target;
                    break;
            }

            return true;
        }

        private static bool ApplySkillCaps(Mobile mobile)
        {
            bool changed = false;

            if (mobile.Skills.Cap != TotalSkillCap)
            {
                mobile.Skills.Cap = TotalSkillCap;
                changed = true;
            }

            for (int i = 0; i < mobile.Skills.Length; i++)
            {
                Skill skill = mobile.Skills[i];

                if (skill == null)
                    continue;

                if (skill.Cap != IndividualSkillCap)
                {
                    skill.Cap = IndividualSkillCap;
                    changed = true;
                }
            }

            return changed;
        }

        private static void OnCapProof(CommandEventArgs e)
        {
            int reported = 0;

            foreach (Mobile mobile in World.Mobiles.Values)
            {
                if (!IsEligible(mobile))
                    continue;

                ApplyIfEligible(mobile);
                LogCapState("CAP_SERVICE_PROOF_MOBILE", mobile);
                reported++;
            }

            AIGMExecutionLog.Write("CAP_SERVICE_PROOF_SUMMARY reported={0} statCap={1} str={2} dex={3} int={4} strMax={5} dexMax={6} intMax={7} skillCapEach={8:F1} totalSkillCap={9}", reported, TotalStatCap, StrCap, DexCap, IntCap, StrMaxCap, DexMaxCap, IntMaxCap, IndividualSkillCap, TotalSkillCap);

            if (e != null && e.Mobile != null)
                e.Mobile.SendMessage("AIGM cap proof logged for {0} eligible mobiles.", reported);
        }

        private static void LogCapState(string label, Mobile mobile)
        {
            int skillCount = 0;
            int matchingSkillCaps = 0;
            double minSkillCap = double.MaxValue;
            double maxSkillCap = double.MinValue;

            for (int i = 0; i < mobile.Skills.Length; i++)
            {
                Skill skill = mobile.Skills[i];

                if (skill == null)
                    continue;

                skillCount++;

                if (skill.Cap == IndividualSkillCap)
                    matchingSkillCaps++;

                if (skill.Cap < minSkillCap)
                    minSkillCap = skill.Cap;

                if (skill.Cap > maxSkillCap)
                    maxSkillCap = skill.Cap;
            }

            if (skillCount == 0)
            {
                minSkillCap = 0.0;
                maxSkillCap = 0.0;
            }

            AIGMExecutionLog.Write("{0} type={1} name=\"{2}\" serial={3} statCap={4} strCap={5} dexCap={6} intCap={7} strMax={8} dexMax={9} intMax={10} skillsTotal={11} skillsCap={12} skillCount={13} matchingSkillCaps={14} minSkillCap={15:F1} maxSkillCap={16:F1}",
                label,
                mobile.GetType().Name,
                mobile.Name,
                mobile.Serial.Value,
                mobile.StatCap,
                mobile.StrCap,
                mobile.DexCap,
                mobile.IntCap,
                mobile.StrMaxCap,
                mobile.DexMaxCap,
                mobile.IntMaxCap,
                mobile.Skills.Total,
                mobile.Skills.Cap,
                skillCount,
                matchingSkillCaps,
                minSkillCap,
                maxSkillCap);
        }
    }
}
