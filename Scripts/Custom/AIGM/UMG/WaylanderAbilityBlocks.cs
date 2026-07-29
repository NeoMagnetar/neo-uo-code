using System.Collections.Generic;

namespace Server.Custom.AIGM.UMG
{
    public static class WaylanderAbilityBlocks
    {
        public static List<UMGCompanionAbilityBlock> BuildAbilityBlocks()
        {
            return new List<UMGCompanionAbilityBlock>
            {
                new UMGCompanionAbilityBlock("DARDALION_SUPPORT_CURE", "dardalion", "Cure", "party member poisoned", "Cure poison before ordinary healing.", "self, owner, Danyal, Dakeyras, other ally", Tags("mana and local spell sequence required", "no spam", "support only"), "protective, calm, spiritually urgent", Tags("spell", "support", "cure")),
                new UMGCompanionAbilityBlock("DARDALION_GREATER_HEAL", "dardalion", "GreaterHeal", "ally below 50 percent health", "Use strongest safe healing spell when the target can be healed.", "owner, self, most injured companion", Tags("do not heal poisoned targets", "respect mana and cooldown", "support only"), "steady triage, no panic", Tags("spell", "support", "heal")),
                new UMGCompanionAbilityBlock("DARDALION_BLESS_PARTY", "dardalion", "Bless", "combat begins and ally lacks stat support", "Bless key party member if safe and not already blessed.", "owner, Dakeyras, Danyal, self", Tags("no repeated buff spam", "do not cast offensive magic"), "quiet invocation, restrained resolve", Tags("spell", "support", "buff")),
                new UMGCompanionAbilityBlock("DAKEYRAS_MAGIC_RESTRAINT", "dakeyras", "Restraint", "offensive spell option exists", "Avoid flashy magic by default; prioritize tactical survival over becoming a generic mage.", "self", Tags("no offensive default", "no caster profile unless explicitly configured"), "terse tactical restraint", Tags("spell", "restraint")),
                new UMGCompanionAbilityBlock("DANYAL_PRACTICAL_SUPPORT", "danyal", "PracticalSupport", "wounded or poisoned party and Dardalion unable", "Use practical support magic only if trained, supplied, and explicitly profiled.", "owner, Dakeyras, Dardalion, self", Tags("not forced by default", "support only"), "direct and useful", Tags("spell", "support", "fallback"))
            };
        }

        private static string[] Tags(params string[] values)
        {
            return values;
        }
    }
}
