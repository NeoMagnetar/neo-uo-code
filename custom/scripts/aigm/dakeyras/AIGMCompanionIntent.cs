using Server;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionIntentKind
    {
        public const string FollowOwner = "follow_owner";
        public const string Stay = "stay";
        public const string Come = "come";
        public const string AttackTarget = "attack_target";
        public const string GuardOwner = "guard_owner";
        public const string BandageSelf = "bandage_self";
        public const string BandageOwner = "bandage_owner";
        public const string HealSelf = "heal_self";
        public const string HealOwner = "heal_owner";
        public const string StopCombat = "stop_combat";
        public const string CureSelf = "cure_self";
        public const string CureOwner = "cure_owner";
        public const string UseHealingSkill = "use_healing_skill";
        public const string UseBandages = "use_bandages";
        public const string CastHeal = "cast_heal";
        public const string CastCure = "cast_cure";
    }

    public sealed class AIGMCompanionIntent
    {
        public string Kind;
        public int TargetSerial;
        public string RawText;

        public bool HasTarget
        {
            get { return TargetSerial != 0; }
        }
    }
}
