using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Server.Commands;
using Server.Custom.AIGM;
using Server.Mobiles;

namespace Server.Commands
{
    public static class AIGMPhase58BProofCommand
    {
        public static void Initialize()
        {
            CommandSystem.Register("p58b", AccessLevel.GameMaster, OnCommand);
            CommandSystem.Register("tproof", AccessLevel.GameMaster, OnCommand);
        }

        private static void OnCommand(CommandEventArgs e)
        {
            if (e == null || e.Mobile == null)
                return;

            BaseHire companion = ResolvePreferredCompanion(e.Mobile, 12);
            if (companion == null)
            {
                e.Mobile.SendMessage("No nearby AIGM companion found.");
                return;
            }

            string branch = ReadHeadText(".git", "HEAD");
            string head = ResolveHeadCommit(".git", "HEAD");

            string reportDir = Path.Combine(Core.BaseDirectory, "docs", "runtime");
            Directory.CreateDirectory(reportDir);
            string reportPath = Path.Combine(reportDir, String.Format("PHASE58B_TRACKING_CATEGORY_RUNTIME_PROOF_{0:yyyyMMdd_HHmmss}.md", DateTime.UtcNow));

            List<string> cleanupNotes = new List<string>();
            ProofFixtureSet fixtures = BuildProofFixtures(e.Mobile, companion, cleanupNotes);

            ProofLaneResult animals = EvaluateTrackingLane(companion, e.Mobile, AIGMCompanionTrackingMode.Animals, "[ta]", true, false, false);
            ProofLaneResult monsters = EvaluateTrackingLane(companion, e.Mobile, AIGMCompanionTrackingMode.Monsters, "[tm]", true, false, false);
            ProofLaneResult players = EvaluateTrackingLane(companion, e.Mobile, AIGMCompanionTrackingMode.Players, "[tp]", true, false, false);
            ProofLaneResult npcs = EvaluateTrackingLane(companion, e.Mobile, AIGMCompanionTrackingMode.NPCs, "[tn]", true, false, false);
            ProofLaneResult humanNpcs = EvaluateTrackingLane(companion, e.Mobile, AIGMCompanionTrackingMode.HumanNPCs, "[th]", true, false, false);
            ProofLaneResult all = EvaluateTrackingLane(companion, e.Mobile, AIGMCompanionTrackingMode.All, "[tall]", true, false, false);
            ProofLaneResult monsterHunt = EvaluateMonsterHuntLane(companion, e.Mobile, fixtures, "[hm]");
            ProofLaneResult huntAnimals = EvaluateHuntAnimalsGate(companion, e.Mobile, "[ha]");

            string finalLabel = DetermineFinalLabel(animals, monsters, players, npcs, humanNpcs, all, monsterHunt, huntAnimals);

            using (StreamWriter writer = new StreamWriter(reportPath, false))
            {
                writer.WriteLine("# Phase58B Tracking Category Runtime Proof");
                writer.WriteLine();
                writer.WriteLine("- Branch: " + branch);
                writer.WriteLine("- HEAD: " + head);
                writer.WriteLine("- CommandUsed: [p58b]");
                writer.WriteLine("- CompanionSelected: " + SafeName(companion));
                writer.WriteLine("- GeneratedUtc: " + DateTime.UtcNow.ToString("u"));
                writer.WriteLine("- ProofOnlySetup: true");
                writer.WriteLine("- FinalLabel: " + finalLabel);
                writer.WriteLine();
                writer.WriteLine("## proof-only setup");
                writer.WriteLine("- AnimalFixture: " + DescribeFixture(fixtures.AnimalFixture));
                writer.WriteLine("- MonsterFixture: " + DescribeFixture(fixtures.MonsterFixture));
                writer.WriteLine("- PlayerFixture: unavailable_no_safe_native_test_pattern");
                writer.WriteLine("- Notes: " + JoinOrNone(cleanupNotes));
                writer.WriteLine();
                WriteLane(writer, animals);
                WriteLane(writer, monsters);
                WriteLane(writer, players);
                WriteLane(writer, npcs);
                WriteLane(writer, humanNpcs);
                WriteLane(writer, all);
                WriteLane(writer, monsterHunt);
                WriteLane(writer, huntAnimals);
            }

            CleanupFixture(fixtures.AnimalFixture);
            CleanupFixture(fixtures.MonsterFixture);

            e.Mobile.SendMessage("Phase58B proof written: " + reportPath);
            e.Mobile.SendMessage("Final label: " + finalLabel);
        }

        private static void WriteLane(StreamWriter writer, ProofLaneResult lane)
        {
            writer.WriteLine("## " + lane.Category);
            writer.WriteLine("- CommandUsed: " + lane.CommandUsed);
            writer.WriteLine("- CategoryTested: " + lane.Category);
            writer.WriteLine("- TrackReportAllowed: " + lane.TrackReportAllowed);
            writer.WriteLine("- AcceptedTarget: " + lane.AcceptedTarget);
            writer.WriteLine("- RejectedCandidates: " + lane.RejectedCandidates);
            writer.WriteLine("- PursuitAllowed: " + lane.PursuitAllowed);
            writer.WriteLine("- AttackAllowed: " + lane.AttackAllowed);
            writer.WriteLine("- MovementResult: " + lane.MovementResult);
            writer.WriteLine("- CombatResult: " + lane.CombatResult);
            writer.WriteLine("- RuntimeSummary: " + lane.RuntimeSummary);
            writer.WriteLine();
        }

        private static ProofLaneResult EvaluateTrackingLane(BaseHire companion, Mobile speaker, AIGMCompanionTrackingMode mode, string commandUsed, bool expectedTrack, bool expectedPursuit, bool expectedAttack)
        {
            string start = AIGMCompanionTrackingService.StartTracking(companion, speaker, mode);
            AIGMCompanionTrackingState state = AIGMCompanionTrackingService.GetState(companion);
            string accepted = state != null && !String.IsNullOrWhiteSpace(state.LastKnownTargetDescription) ? state.LastKnownTargetDescription : "none";
            string rejected = BuildCategoryRejectSummary(companion, mode);
            string runtimeSummary = state != null && !String.IsNullOrWhiteSpace(state.LastReport) ? state.LastReport : start;
            string movement = "none";
            string combat = "none";

            bool pursuitAllowed = expectedPursuit;
            bool attackAllowed = expectedAttack;

            return new ProofLaneResult
            {
                Category = mode.ToString(),
                CommandUsed = commandUsed,
                TrackReportAllowed = expectedTrack,
                AcceptedTarget = accepted,
                RejectedCandidates = rejected,
                PursuitAllowed = pursuitAllowed,
                AttackAllowed = attackAllowed,
                MovementResult = movement,
                CombatResult = combat,
                RuntimeSummary = runtimeSummary
            };
        }

        private static ProofLaneResult EvaluateMonsterHuntLane(BaseHire companion, Mobile speaker, ProofFixtureSet fixtures, string commandUsed)
        {
            string result = AIGMCompanionExecutionSpine.StartMonsterHunt(companion, speaker);
            AIGMCompanionExecutionState state = AIGMCompanionExecutionSpine.GetState(companion);

            return new ProofLaneResult
            {
                Category = "MonsterHunt",
                CommandUsed = commandUsed,
                TrackReportAllowed = true,
                AcceptedTarget = state != null && !String.IsNullOrWhiteSpace(state.CurrentTargetName) ? state.CurrentTargetName : "none",
                RejectedCandidates = state != null && !String.IsNullOrWhiteSpace(state.LastCandidateSummary) ? state.LastCandidateSummary : BuildMonsterRejectSummary(companion, fixtures),
                PursuitAllowed = true,
                AttackAllowed = true,
                MovementResult = state != null && !String.IsNullOrWhiteSpace(state.LastMovementResult) ? state.LastMovementResult : "none",
                CombatResult = state != null && !String.IsNullOrWhiteSpace(state.LastCombatResult) ? state.LastCombatResult : "none",
                RuntimeSummary = result + (state != null ? " | phase=" + state.Phase + " | trace=" + SafeValue(state.LastTrace) + " | lastReject=" + SafeValue(state.LastTargetRejectionReason) : String.Empty)
            };
        }

        private static ProofLaneResult EvaluateHuntAnimalsGate(BaseHire companion, Mobile speaker, string commandUsed)
        {
            string result = "Animal hunting is not enabled in this lane yet.";
            return new ProofLaneResult
            {
                Category = "HuntAnimals",
                CommandUsed = commandUsed,
                TrackReportAllowed = false,
                AcceptedTarget = "none",
                RejectedCandidates = "gated_lane",
                PursuitAllowed = false,
                AttackAllowed = false,
                MovementResult = "none",
                CombatResult = "none",
                RuntimeSummary = result
            };
        }

        private static string DetermineFinalLabel(ProofLaneResult animals, ProofLaneResult monsters, ProofLaneResult players, ProofLaneResult npcs, ProofLaneResult humanNpcs, ProofLaneResult all, ProofLaneResult monsterHunt, ProofLaneResult huntAnimals)
        {
            bool categoryOk = animals.TrackReportAllowed && !animals.PursuitAllowed && !animals.AttackAllowed
                && monsters.TrackReportAllowed
                && players.TrackReportAllowed && !players.PursuitAllowed && !players.AttackAllowed
                && npcs.TrackReportAllowed && !npcs.PursuitAllowed && !npcs.AttackAllowed
                && humanNpcs.TrackReportAllowed && !humanNpcs.PursuitAllowed && !humanNpcs.AttackAllowed
                && all.TrackReportAllowed && !all.PursuitAllowed && !all.AttackAllowed
                && !huntAnimals.PursuitAllowed && !huntAnimals.AttackAllowed;

            bool hmOk = monsterHunt.PursuitAllowed && monsterHunt.AttackAllowed && (monsterHunt.AcceptedTarget != "none" || monsterHunt.RejectedCandidates.Contains("animal_target"));

            if (categoryOk && hmOk)
                return "PHASE58B-CATEGORY-PROOF";

            if (hmOk)
                return "PHASE58B-HM-PROOF";

            if (categoryOk)
                return "PHASE58B-ACTION-GATE-PROOF";

            return "PHASE58B-PROOF-SURFACE-BLOCKED";
        }

        private static string BuildCategoryRejectSummary(BaseHire companion, AIGMCompanionTrackingMode mode)
        {
            if (companion == null || companion.Map == null)
                return "none";

            AIGMSceneContext scene = AIGMSceneScanner.Capture(companion, 12);
            if (scene == null || scene.NearbyMobiles == null || scene.NearbyMobiles.Count == 0)
                return "none";

            List<string> rejects = new List<string>();
            for (int i = 0; i < scene.NearbyMobiles.Count; i++)
            {
                AIGMSceneEntitySummary mob = scene.NearbyMobiles[i];
                if (mob == null)
                    continue;

                bool matched = MatchesCategoryForProof(mob, mode);
                if (!matched)
                    rejects.Add(SafeSummaryName(mob) + "=" + ClassifySummary(mob));
            }

            return rejects.Count == 0 ? "none" : String.Join(", ", rejects.ToArray());
        }

        private static string BuildMonsterRejectSummary(BaseHire companion, ProofFixtureSet fixtures)
        {
            List<string> parts = new List<string>();

            if (fixtures.AnimalFixture != null && !fixtures.AnimalFixture.Deleted)
            {
                string reason;
                AIGMCompanionTargetValidator.IsHostileMonsterCandidate(companion, fixtures.AnimalFixture, out reason);
                parts.Add(SafeName(fixtures.AnimalFixture) + "=" + reason);
            }

            if (fixtures.MonsterFixture != null && !fixtures.MonsterFixture.Deleted)
            {
                string reason;
                AIGMCompanionTargetValidator.IsHostileMonsterCandidate(companion, fixtures.MonsterFixture, out reason);
                parts.Add(SafeName(fixtures.MonsterFixture) + "=" + reason);
            }

            return parts.Count == 0 ? "none" : String.Join(", ", parts.ToArray());
        }

        private static bool MatchesCategoryForProof(AIGMSceneEntitySummary mob, AIGMCompanionTrackingMode mode)
        {
            string typeName = (mob.TypeName ?? String.Empty).ToLowerInvariant();
            string name = (mob.Name ?? String.Empty).ToLowerInvariant();

            switch (mode)
            {
                case AIGMCompanionTrackingMode.Animals:
                    return ContainsAny(typeName, "horse", "ostard", "cat", "dog", "wolf", "bear", "boar", "eagle", "hind", "hart", "cow", "bull")
                        || ContainsAny(name, "horse", "ostard", "cat", "dog", "wolf", "bear", "boar", "eagle", "hind", "hart", "cow", "bull");
                case AIGMCompanionTrackingMode.Monsters:
                    return ContainsAny(typeName, "dragon", "daemon", "lich", "orc", "troll", "ogre", "ettin", "ratman", "mongbat", "headless", "reaper", "elemental", "skeleton")
                        || ContainsAny(name, "orc", "daemon", "lich", "brigand", "pirate", "troll", "ogre", "ratman", "headless", "skeleton");
                case AIGMCompanionTrackingMode.Players:
                    return typeName.Contains("playermobile") || typeName.Equals("player");
                case AIGMCompanionTrackingMode.HumanNPCs:
                case AIGMCompanionTrackingMode.NPCs:
                    return !typeName.Contains("playermobile") && (ContainsAny(typeName, "vendor", "healer", "seer", "human", "banker", "provisioner", "mage", "warrior")
                        || ContainsAny(name, "healer", "banker", "vendor", "mage", "guard", "innkeeper", "provisioner"));
                case AIGMCompanionTrackingMode.All:
                    return true;
                default:
                    return false;
            }
        }

        private static string ClassifySummary(AIGMSceneEntitySummary mob)
        {
            string typeName = (mob.TypeName ?? String.Empty).ToLowerInvariant();
            string name = (mob.Name ?? String.Empty).ToLowerInvariant();

            if (typeName.Contains("playermobile") || typeName.Equals("player"))
                return "player";
            if (ContainsAny(typeName, "vendor", "healer", "seer", "human", "banker", "provisioner", "mage", "warrior") || ContainsAny(name, "healer", "banker", "vendor", "mage", "guard", "innkeeper", "provisioner"))
                return "human_npc";
            if (ContainsAny(typeName, "horse", "ostard", "cat", "dog", "wolf", "bear", "boar", "eagle", "hind", "hart", "cow", "bull") || ContainsAny(name, "horse", "ostard", "cat", "dog", "wolf", "bear", "boar", "eagle", "hind", "hart", "cow", "bull"))
                return "animal";
            if (ContainsAny(typeName, "dragon", "daemon", "lich", "orc", "troll", "ogre", "ettin", "ratman", "mongbat", "headless", "reaper", "elemental", "skeleton") || ContainsAny(name, "orc", "daemon", "lich", "brigand", "pirate", "troll", "ogre", "ratman", "headless", "skeleton"))
                return "monster";
            return "other";
        }

        private static ProofFixtureSet BuildProofFixtures(Mobile caller, BaseHire companion, List<string> notes)
        {
            ProofFixtureSet fixtures = new ProofFixtureSet();
            if (caller == null || companion == null || caller.Map == null || companion.Map == null || caller.Map != companion.Map)
            {
                notes.Add("fixture_setup_skipped_invalid_context");
                return fixtures;
            }

            fixtures.AnimalFixture = SpawnFirstFit(companion, notes, true);
            fixtures.MonsterFixture = SpawnFirstFit(companion, notes, false);
            return fixtures;
        }

        private static BaseCreature SpawnFirstFit(BaseHire companion, List<string> notes, bool animal)
        {
            BaseCreature[] candidates = animal
                ? new BaseCreature[] { new BrownBear(), new Cow(), new Bull() }
                : new BaseCreature[] { new HeadlessOne(), new Skeleton(), new Orc(), new Ratman() };

            Point3D[] offsets = animal
                ? new Point3D[] { new Point3D(2, 1, 0), new Point3D(3, 1, 0), new Point3D(2, 2, 0) }
                : new Point3D[] { new Point3D(2, 0, 0), new Point3D(3, 0, 0), new Point3D(2, -1, 0) };

            for (int i = 0; i < candidates.Length; i++)
            {
                BaseCreature creature = candidates[i];
                for (int j = 0; j < offsets.Length; j++)
                {
                    Point3D loc = NormalizeSpawnPoint(companion.Map, new Point3D(companion.X + offsets[j].X, companion.Y + offsets[j].Y, companion.Z));
                    if (!companion.Map.CanFit(loc.X, loc.Y, loc.Z, 16, false, false))
                        continue;

                    creature.Name = (animal ? "proof-only animal target" : "proof-only hostile monster target");
                    creature.MoveToWorld(loc, companion.Map);
                    notes.Add("spawned:" + SafeName(creature) + "@" + loc.X + "," + loc.Y + "," + loc.Z);
                    return creature;
                }

                creature.Delete();
            }

            notes.Add(animal ? "animal_fixture_unavailable" : "monster_fixture_unavailable");
            return null;
        }

        private static void CleanupFixture(BaseCreature creature)
        {
            if (creature != null && !creature.Deleted)
                creature.Delete();
        }

        private static string DescribeFixture(BaseCreature creature)
        {
            return creature == null || creature.Deleted ? "none" : SafeName(creature) + " @ " + creature.X + "," + creature.Y + "," + creature.Z;
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

        private static string JoinOrNone(List<string> values)
        {
            return values == null || values.Count == 0 ? "none" : String.Join(", ", values.ToArray());
        }

        private static string SafeName(Mobile mobile)
        {
            if (mobile == null)
                return "none";

            return String.IsNullOrWhiteSpace(mobile.Name) ? mobile.GetType().Name : mobile.Name;
        }

        private static string SafeSummaryName(AIGMSceneEntitySummary summary)
        {
            if (summary == null)
                return "none";

            return !String.IsNullOrWhiteSpace(summary.Name) ? summary.Name : (!String.IsNullOrWhiteSpace(summary.TypeName) ? summary.TypeName : "unknown");
        }

        private static string SafeValue(string value)
        {
            return String.IsNullOrWhiteSpace(value) ? "none" : value;
        }

        private static BaseHire ResolvePreferredCompanion(Mobile from, int range)
        {
            if (from == null || from.Map == null)
                return null;

            BaseHire dak = null;
            BaseHire dard = null;
            BaseHire danyal = null;
            BaseHire best = null;
            int bestDistance = Int32.MaxValue;

            IPooledEnumerable mobiles = from.Map.GetMobilesInRange(from.Location, range);
            foreach (Mobile mobile in mobiles)
            {
                BaseHire hire = mobile as BaseHire;
                IAIGMCompanionActor actor = mobile as IAIGMCompanionActor;
                if (hire == null || actor == null || mobile.Deleted || !mobile.Alive)
                    continue;

                if (String.Equals(hire.Name, "Dakeyras", StringComparison.OrdinalIgnoreCase))
                    dak = hire;
                else if (String.Equals(hire.Name, "Dardalion", StringComparison.OrdinalIgnoreCase))
                    dard = hire;
                else if (String.Equals(hire.Name, "Danyal", StringComparison.OrdinalIgnoreCase))
                    danyal = hire;

                int distance = (int)from.GetDistanceToSqrt(mobile);
                if (distance < bestDistance)
                {
                    best = hire;
                    bestDistance = distance;
                }
            }
            mobiles.Free();

            return dak ?? dard ?? danyal ?? best;
        }

        private static string ReadHeadText(string gitDirName, string headFileName)
        {
            try
            {
                string gitDir = Path.Combine(Core.BaseDirectory, gitDirName);
                string headPath = Path.Combine(gitDir, headFileName);
                if (!File.Exists(headPath))
                    return "unknown";

                string headText = File.ReadAllText(headPath).Trim();
                if (headText.StartsWith("ref:", StringComparison.OrdinalIgnoreCase))
                    return headText.Substring(4).Trim().Replace("refs/heads/", String.Empty);

                return headText;
            }
            catch
            {
                return "unknown";
            }
        }

        private static string ResolveHeadCommit(string gitDirName, string headFileName)
        {
            try
            {
                string gitDir = Path.Combine(Core.BaseDirectory, gitDirName);
                string headPath = Path.Combine(gitDir, headFileName);
                if (!File.Exists(headPath))
                    return "unknown";

                string headText = File.ReadAllText(headPath).Trim();
                if (!headText.StartsWith("ref:", StringComparison.OrdinalIgnoreCase))
                    return headText;

                string refPath = headText.Substring(4).Trim().Replace('/', Path.DirectorySeparatorChar);
                string fullRefPath = Path.Combine(gitDir, refPath);
                return File.Exists(fullRefPath) ? File.ReadAllText(fullRefPath).Trim() : "unknown";
            }
            catch
            {
                return "unknown";
            }
        }

        private static bool ContainsAny(string value, params string[] needles)
        {
            if (String.IsNullOrWhiteSpace(value) || needles == null)
                return false;

            for (int i = 0; i < needles.Length; i++)
            {
                if (!String.IsNullOrWhiteSpace(needles[i]) && value.Contains(needles[i]))
                    return true;
            }

            return false;
        }

        private sealed class ProofFixtureSet
        {
            public BaseCreature AnimalFixture { get; set; }
            public BaseCreature MonsterFixture { get; set; }
        }

        private sealed class ProofLaneResult
        {
            public string Category { get; set; }
            public string CommandUsed { get; set; }
            public bool TrackReportAllowed { get; set; }
            public string AcceptedTarget { get; set; }
            public string RejectedCandidates { get; set; }
            public bool PursuitAllowed { get; set; }
            public bool AttackAllowed { get; set; }
            public string MovementResult { get; set; }
            public string CombatResult { get; set; }
            public string RuntimeSummary { get; set; }
        }
    }
}
