using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public sealed class AIGMCompanionAutoLootState
    {
        public bool Enabled;
        public AIGMCompanionLootProfile Profile = AIGMCompanionLootProfile.Default;
        public DateTime LastProcessUtc = DateTime.MinValue;
    }

    public static class AIGMCompanionAutoLootService
    {
        private const int AutoLootRange = 4;
        private static readonly TimeSpan CorpseDelay = TimeSpan.FromSeconds(0.8);
        private static readonly TimeSpan CompanionCooldown = TimeSpan.FromSeconds(3.0);
        private static readonly Dictionary<Serial, AIGMCompanionAutoLootState> States = new Dictionary<Serial, AIGMCompanionAutoLootState>();
        private static readonly Dictionary<Serial, DateTime> ProcessedCorpses = new Dictionary<Serial, DateTime>();

        public static void Initialize()
        {
            EventSink.OnKilledBy += OnKilledBy;
            Console.WriteLine("AIGM_AUTOLOOT_INIT subscribed=true default=off");
        }

        public static string SetAutoLoot(Mobile companion, bool enabled, AIGMCompanionLootProfile profile)
        {
            if (companion == null || companion.Deleted)
                return "No companion selected.";

            AIGMCompanionAutoLootState state = GetOrCreateState(companion);
            state.Enabled = enabled;
            if (profile != null)
                state.Profile = profile;

            Console.WriteLine("AIGM_AUTOLOOT_MODE companion={0} enabled={1} profile={2}", companion.Name, state.Enabled, state.Profile.Name);

            if (!enabled)
                return "Auto loot is off.";

            return BuildCharacterEnableLine(companion, state.Profile);
        }

        public static string SetAutoLootProfile(Mobile companion, AIGMCompanionLootProfile profile)
        {
            if (companion == null || companion.Deleted)
                return "No companion selected.";

            AIGMCompanionAutoLootState state = GetOrCreateState(companion);
            state.Profile = profile ?? AIGMCompanionLootProfile.Default;
            Console.WriteLine("AIGM_AUTOLOOT_PROFILE companion={0} enabled={1} profile={2}", companion.Name, state.Enabled, state.Profile.Name);
            return String.Format("Auto loot profile: {0}.", state.Profile.Name);
        }

        public static string GetAutoLootStatus(Mobile companion)
        {
            if (companion == null || companion.Deleted)
                return "Auto loot is off.";

            AIGMCompanionAutoLootState state = GetState(companion);
            bool enabled = state != null && state.Enabled;
            string profile = state != null && state.Profile != null ? state.Profile.Name : AIGMCompanionLootProfile.Default.Name;
            Console.WriteLine("AIGM_AUTOLOOT_STATUS companion={0} enabled={1} profile={2}", companion.Name, enabled, profile);
            return String.Format("Auto loot {0}; profile {1}.", enabled ? "on" : "off", profile);
        }

        public static bool IsEnabled(Mobile companion)
        {
            AIGMCompanionAutoLootState state = GetState(companion);
            return state != null && state.Enabled;
        }

        public static bool TryProcessCorpse(Mobile companion, Mobile owner, Corpse corpse, string trigger, out AIGMCompanionLootResult result)
        {
            result = null;
            if (companion == null || companion.Deleted || corpse == null || corpse.Deleted)
                return false;

            AIGMCompanionAutoLootState state = GetState(companion);
            if (state == null || !state.Enabled)
            {
                Console.WriteLine("AIGM_AUTOLOOT_SKIP companion={0} corpse={1} reason=disabled", companion.Name, corpse.Serial);
                return false;
            }

            DateTime now = DateTime.UtcNow;
            if (ProcessedCorpses.ContainsKey(corpse.Serial))
            {
                Console.WriteLine("AIGM_AUTOLOOT_SKIP companion={0} corpse={1} reason=already_processed", companion.Name, corpse.Serial);
                return false;
            }

            if (now - state.LastProcessUtc < CompanionCooldown)
            {
                Console.WriteLine("AIGM_AUTOLOOT_SKIP companion={0} corpse={1} reason=cooldown", companion.Name, corpse.Serial);
                return false;
            }

            string reason;
            if (!AIGMCompanionLootService.CanLootCorpse(companion, corpse, owner, out reason))
            {
                ProcessedCorpses[corpse.Serial] = now;
                Console.WriteLine("AIGM_AUTOLOOT_SKIP_CORPSE companion={0} corpse={1} trigger={2} reason={3}", companion.Name, corpse.Serial, trigger, reason);
                return false;
            }

            state.LastProcessUtc = now;
            ProcessedCorpses[corpse.Serial] = now;
            Console.WriteLine("AIGM_AUTOLOOT_TRIGGER companion={0} corpse={1} profile={2} trigger={3}", companion.Name, corpse.Serial, state.Profile.Name, trigger);
            result = AIGMCompanionLootService.StartLootCorpse(companion, owner, corpse, state.Profile);
            AIGMCompanionInventoryPolicy.RunBurdenManagement(companion, false);
            Console.WriteLine("AIGM_AUTOLOOT_DONE companion={0} corpse={1} items={2} gold={3} reason={4}", companion.Name, corpse.Serial, result.ItemsMoved, result.GoldMoved, result.SummaryReason);
            return result.ItemsMoved > 0;
        }

        public static int GetProcessedCorpseCount()
        {
            return ProcessedCorpses.Count;
        }

        private static void OnKilledBy(OnKilledByEventArgs e)
        {
            if (e == null || e.Killed == null || e.Killed.Deleted)
                return;

            Timer.DelayCall(CorpseDelay, delegate
            {
                ProcessKilled(e.Killed, e.KilledBy);
            });
        }

        private static void ProcessKilled(Mobile killed, Mobile killedBy)
        {
            if (killed == null || killed.Deleted)
                return;

            Corpse corpse = killed.Corpse as Corpse;
            if (corpse == null || corpse.Deleted)
                return;

            Mobile companion = FindBestEnabledCompanion(corpse, killedBy);
            if (companion == null)
                return;

            Mobile owner = ResolveOwner(companion) ?? killedBy;
            AIGMCompanionLootResult ignored;
            TryProcessCorpse(companion, owner, corpse, "on_killed_by", out ignored);
        }

        private static Mobile FindBestEnabledCompanion(Corpse corpse, Mobile killedBy)
        {
            if (corpse == null || corpse.Map == null)
                return null;

            Mobile direct = killedBy;
            if (IsEligibleEnabledCompanion(direct, corpse))
                return direct;

            Mobile owner = ResolveOwner(killedBy);
            Mobile best = null;
            double bestDistance = Double.MaxValue;

            foreach (Mobile mobile in World.Mobiles.Values)
            {
                if (!IsEligibleEnabledCompanion(mobile, corpse))
                    continue;

                if (owner != null && ResolveOwner(mobile) != owner && mobile != killedBy)
                    continue;

                double distance = mobile.GetDistanceToSqrt(corpse.Location);
                if (distance < bestDistance)
                {
                    best = mobile;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private static bool IsEligibleEnabledCompanion(Mobile mobile, Corpse corpse)
        {
            if (mobile == null || mobile.Deleted || !mobile.Alive || corpse == null || corpse.Map == null || mobile.Map != corpse.Map)
                return false;

            if (!(mobile is BaseHire) || !(mobile is IAIGMCompanionActor))
                return false;

            if (!IsEnabled(mobile))
                return false;

            return mobile.InRange(corpse.Location, AutoLootRange);
        }

        private static Mobile ResolveOwner(Mobile mobile)
        {
            BaseHire hire = mobile as BaseHire;
            if (hire != null)
                return hire.GetOwner();

            BaseCreature creature = mobile as BaseCreature;
            if (creature != null)
                return creature.GetMaster();

            return null;
        }

        private static AIGMCompanionAutoLootState GetState(Mobile companion)
        {
            if (companion == null)
                return null;

            AIGMCompanionAutoLootState state;
            States.TryGetValue(companion.Serial, out state);
            return state;
        }

        private static AIGMCompanionAutoLootState GetOrCreateState(Mobile companion)
        {
            AIGMCompanionAutoLootState state = GetState(companion);
            if (state == null)
            {
                state = new AIGMCompanionAutoLootState();
                States[companion.Serial] = state;
            }

            return state;
        }

        private static string BuildCharacterEnableLine(Mobile companion, AIGMCompanionLootProfile profile)
        {
            string id = GetCompanionId(companion);
            if (String.Equals(id, "danyal", StringComparison.Ordinal))
                return "I will gather what we can use.";

            if (String.Equals(id, "dardalion", StringComparison.Ordinal))
                return "We keep what serves the living.";

            return "I will take only what is useful.";
        }

        private static string GetCompanionId(Mobile companion)
        {
            IAIGMCompanionActor actor = companion as IAIGMCompanionActor;
            if (actor != null && !String.IsNullOrWhiteSpace(actor.CompanionId))
                return actor.CompanionId.ToLowerInvariant();

            return companion != null && companion.Name != null ? companion.Name.ToLowerInvariant() : String.Empty;
        }
    }
}
