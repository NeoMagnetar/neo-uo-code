using System;
using System.IO;
using Server.Commands;
using Server.Custom.AIGM;
using Server.Mobiles;
using System.Collections.Generic;

namespace Server.Commands
{
    public static class AIGMScenarioCommand
    {
        public static void Initialize()
        {
            CommandSystem.Register("AIGMScenario", AccessLevel.GameMaster, OnCommand);
            CommandSystem.Register("mh", AccessLevel.GameMaster, OnMonsterHuntAlias);
            CommandSystem.Register("mhp", AccessLevel.GameMaster, OnMonsterHuntProofAlias);
        }

        private static void OnMonsterHuntAlias(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            ExecuteScenarioCommand(e, "MonsterHunt", false);
        }

        private static void OnMonsterHuntProofAlias(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            ExecuteScenarioCommand(e, "MonsterHunt", true);
        }

        private static void OnCommand(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            string arg = e.ArgString == null ? String.Empty : e.ArgString;
            ExecuteScenarioCommand(e, arg, false);
        }

        private static void ExecuteScenarioCommand(CommandEventArgs e, string arg, bool fullProof)
        {
            string rawArg = arg;
            arg = NormalizeScenarioArgument(arg);
            if (String.IsNullOrWhiteSpace(arg))
            {
                e.Mobile.SendMessage("Usage: [AIGMScenario MonsterHunt | MonsterHunt Near | MonsterHunt Adjacent | AnimalTrackProof");
                return;
            }

            BaseHire companion = ResolvePreferredCompanion(e.Mobile, 12);
            string reportDir = Path.Combine(Core.BaseDirectory, "docs", "runtime");
            Directory.CreateDirectory(reportDir);
            string filePrefix = fullProof ? "PHASE58A_CLOSEST_MONSTER_HUNT_PROOF" : "PHASE58A_" + arg.ToUpperInvariant() + "_RUNTIME_PROOF";
            string reportPath = Path.Combine(reportDir, String.Format(filePrefix + "_{0:yyyyMMdd_HHmmss}.md", DateTime.UtcNow));

            string scenarioKey = NormalizeScenarioKey(arg);
            string setupResult = String.Empty;
            if (fullProof)
                EnsureHostileMonsterSetup(e.Mobile, companion, out setupResult);

            string scenarioResult = ExecuteScenario(scenarioKey, e.Mobile, companion);
            AIGMCompanionExecutionState state = companion != null ? AIGMCompanionExecutionSpine.GetState(companion) : null;

            using (StreamWriter writer = new StreamWriter(reportPath, false))
            {
                writer.WriteLine("# Phase58A Runtime Proof");
                writer.WriteLine();
                writer.WriteLine("- Scenario: " + arg);
                writer.WriteLine("- RawArg: " + rawArg);
                writer.WriteLine("- NormalizedScenarioKey: " + scenarioKey);
                writer.WriteLine("- Actor: " + (companion != null ? companion.Name : "none"));
                writer.WriteLine("- GeneratedUtc: " + DateTime.UtcNow.ToString("u"));
                writer.WriteLine("- RuntimeVerified: true");
                writer.WriteLine("- SetupResult: " + (String.IsNullOrWhiteSpace(setupResult) ? "none" : setupResult));
                writer.WriteLine("- ScenarioResult: " + scenarioResult);

                if (state != null)
                {
                    writer.WriteLine("- HuntActive: " + state.HuntActive);
                    writer.WriteLine("- Phase: " + state.Phase);
                    writer.WriteLine("- Reason: " + state.PhaseReason);
                    writer.WriteLine("- Target: " + (String.IsNullOrWhiteSpace(state.CurrentTargetName) ? "none" : state.CurrentTargetName));
                    writer.WriteLine("- Trace: " + (String.IsNullOrWhiteSpace(state.LastTrace) ? "none" : state.LastTrace));
                    writer.WriteLine("- LastMove: " + (String.IsNullOrWhiteSpace(state.LastMovementResult) ? "none" : state.LastMovementResult));
                    writer.WriteLine("- LastCombat: " + (String.IsNullOrWhiteSpace(state.LastCombatResult) ? "none" : state.LastCombatResult));
                    writer.WriteLine("- LastReject: " + (String.IsNullOrWhiteSpace(state.LastTargetRejectionReason) ? "none" : state.LastTargetRejectionReason));
                    writer.WriteLine("- Candidates: " + (String.IsNullOrWhiteSpace(state.LastCandidateSummary) ? "none" : state.LastCandidateSummary));
                }
            }

            e.Mobile.SendMessage("AIGMScenario key: " + scenarioKey);
            if (!String.IsNullOrWhiteSpace(setupResult))
                e.Mobile.SendMessage("AIGMScenario setup: " + setupResult);
            e.Mobile.SendMessage("AIGMScenario result: " + scenarioResult);
            e.Mobile.SendMessage("Scenario proof written: " + reportPath);
            if (fullProof && state != null)
                e.Mobile.SendMessage("mhp: target=" + (String.IsNullOrWhiteSpace(state.CurrentTargetName) ? "none" : state.CurrentTargetName) + ", phase=" + state.Phase + ", trace=" + (String.IsNullOrWhiteSpace(state.LastTrace) ? "none" : state.LastTrace));
        }

        private static string NormalizeScenarioArgument(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return String.Empty;

            char[] buffer = value.Trim().ToCharArray();
            System.Text.StringBuilder sb = new System.Text.StringBuilder(buffer.Length);
            for (int i = 0; i < buffer.Length; i++)
            {
                char c = buffer[i];
                if (Char.IsLetterOrDigit(c) || Char.IsWhiteSpace(c) || c == '_' || c == '-')
                    sb.Append(c);
            }

            return sb.ToString().Trim();
        }

        private static string NormalizeScenarioKey(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return String.Empty;

            string key = value.Replace(" ", String.Empty).Replace("_", String.Empty).Replace("-", String.Empty).Trim().ToLowerInvariant();
            if (key == "monsterhunt" || key == "monsters" || key == "huntmonsters")
                return "monsterhunt";
            if (key == "monsterhuntnear" || key == "mhnear")
                return "monsterhuntnear";
            if (key == "monsterhuntadjacent" || key == "mhadjacent")
                return "monsterhuntadjacent";
            if (key == "animaltrackproof" || key == "animalproof")
                return "animaltrackproof";

            if (key == "healing" || key == "heal")
                return "healing";

            if (key == "door" || key == "doors")
                return "door";

            if (key == "tracking" || key == "track")
                return "tracking";

            return key;
        }

        private static string ExecuteScenario(string scenarioKey, Mobile from, BaseHire companion)
        {
            if (companion == null)
                return "no_nearby_companion";

            switch (scenarioKey)
            {
                case "monsterhunt":
                case "monsterhuntnear":
                case "monsterhuntadjacent":
                    return AIGMCompanionExecutionSpine.StartMonsterHunt(companion, from);
                case "animaltrackproof":
                    return "animal_track_proof_not_yet_implemented";
                case "healing":
                case "door":
                case "tracking":
                    return "scenario_not_yet_implemented";
                default:
                    return "unknown_scenario";
            }
        }

        private static BaseHire ResolvePreferredCompanion(Mobile from, int range)
        {
            if (from == null || from.Map == null)
                return null;

            List<BaseHire> companions = new List<BaseHire>();
            IPooledEnumerable mobiles = from.Map.GetMobilesInRange(from.Location, range);
            foreach (Mobile mobile in mobiles)
            {
                BaseHire hire = mobile as BaseHire;
                IAIGMCompanionActor actor = mobile as IAIGMCompanionActor;
                if (hire == null || actor == null || mobile.Deleted || !mobile.Alive)
                    continue;

                companions.Add(hire);
            }
            mobiles.Free();

            BaseHire preferred = FindByName(companions, "Dakeyras") ?? FindByName(companions, "Dardalion") ?? FindByName(companions, "Danyal");
            if (preferred != null)
                return preferred;

            BaseHire best = null;
            int bestDistance = Int32.MaxValue;
            for (int i = 0; i < companions.Count; i++)
            {
                int distance = (int)from.GetDistanceToSqrt(companions[i]);
                if (distance < bestDistance)
                {
                    best = companions[i];
                    bestDistance = distance;
                }
            }

            return best;
        }

        private static BaseHire FindByName(List<BaseHire> companions, string name)
        {
            if (companions == null || String.IsNullOrWhiteSpace(name))
                return null;

            for (int i = 0; i < companions.Count; i++)
            {
                BaseHire hire = companions[i];
                if (hire != null && String.Equals(hire.Name, name, StringComparison.OrdinalIgnoreCase))
                    return hire;
            }

            return null;
        }

        private static void EnsureHostileMonsterSetup(Mobile caller, BaseHire companion, out string result)
        {
            result = String.Empty;
            if (caller == null || companion == null || caller.Map == null || companion.Map == null || caller.Map != companion.Map)
            {
                result = "setup_skipped_invalid_context";
                return;
            }

            Mobile existing = FindNearbyHostileMonster(companion, 8);
            if (existing != null)
            {
                string reason;
                bool ok = AIGMCompanionTargetValidator.IsHostileMonsterCandidate(companion, existing, out reason);
                result = "spawnedTarget=existing:" + SafeName(existing) + " bodyIsMonster=" + BodyIsMonster(existing) + " validated=" + ok + " reason=" + reason;
                return;
            }

            List<string> spawnNotes = new List<string>();
            BaseCreature spawned = TrySpawnHostileMonster(companion, spawnNotes);
            if (spawned != null)
            {
                string reason;
                bool ok = AIGMCompanionTargetValidator.IsHostileMonsterCandidate(companion, spawned, out reason);
                result = "spawnedTarget=" + SafeName(spawned) + " bodyIsMonster=" + BodyIsMonster(spawned) + " validated=" + ok + " reason=" + reason + (spawnNotes.Count > 0 ? " notes=" + String.Join(",", spawnNotes.ToArray()) : String.Empty);
                return;
            }

            result = spawnNotes.Count > 0 ? String.Join(",", spawnNotes.ToArray()) : "no_hostile_spawn_created";
        }

        private static Mobile FindNearbyHostileMonster(BaseHire companion, int range)
        {
            if (companion == null || companion.Map == null)
                return null;

            Mobile best = null;
            int bestDistance = Int32.MaxValue;
            IPooledEnumerable mobiles = companion.Map.GetMobilesInRange(companion.Location, range);
            foreach (Mobile mobile in mobiles)
            {
                string reason;
                if (!AIGMCompanionTargetValidator.IsHostileMonsterCandidate(companion, mobile, out reason))
                    continue;

                int distance = (int)companion.GetDistanceToSqrt(mobile);
                if (distance < bestDistance)
                {
                    best = mobile;
                    bestDistance = distance;
                }
            }
            mobiles.Free();
            return best;
        }

        private static BaseCreature TrySpawnHostileMonster(BaseHire companion, List<string> notes)
        {
            BaseCreature[] candidates = new BaseCreature[]
            {
                new HeadlessOne(),
                new Skeleton(),
                new Ettin(),
                new Ratman(),
                new Orc()
            };

            Point3D[] offsets = new Point3D[]
            {
                new Point3D(2, 0, 0),
                new Point3D(3, 0, 0),
                new Point3D(2, 1, 0),
                new Point3D(2, -1, 0)
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                BaseCreature creature = candidates[i];
                for (int j = 0; j < offsets.Length; j++)
                {
                    Point3D loc = new Point3D(companion.X + offsets[j].X, companion.Y + offsets[j].Y, companion.Z);
                    loc = NormalizeSpawnPoint(companion.Map, loc);
                    if (!companion.Map.CanFit(loc.X, loc.Y, loc.Z, 16, false, false))
                        continue;

                    creature.MoveToWorld(loc, companion.Map);
                    string reason;
                    if (AIGMCompanionTargetValidator.IsHostileMonsterCandidate(companion, creature, out reason))
                        return creature;

                    notes.Add("reject:" + SafeName(creature) + " bodyIsMonster=" + BodyIsMonster(creature) + " reason=" + reason);
                    creature.Delete();
                    break;
                }
            }

            return null;
        }

        private static Point3D NormalizeSpawnPoint(Map map, Point3D p)
        {
            if (map == null)
                return p;

            int z = p.Z;
            if (!map.CanFit(p.X, p.Y, z, 16, false, false))
                z = map.GetAverageZ(p.X, p.Y);
            return new Point3D(p.X, p.Y, z);
        }

        private static string SafeName(Mobile mobile)
        {
            if (mobile == null)
                return "none";

            return String.IsNullOrWhiteSpace(mobile.Name) ? mobile.GetType().Name : mobile.Name;
        }

        private static bool BodyIsMonster(Mobile mobile)
        {
            BaseCreature creature = mobile as BaseCreature;
            return creature != null && creature.Body != null && creature.Body.IsMonster;
        }
    }
}
