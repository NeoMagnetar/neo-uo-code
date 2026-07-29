using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Server.Custom.AIGM;
using Server.Custom.AIGM.Tasks;
using Server.Mobiles;

namespace Server.Custom.AIGM.UMG
{
    [DataContract]
    public sealed class AIGMUMGSituationEntity
    {
        [DataMember(Order = 0)] public string Name { get; set; }
        [DataMember(Order = 1)] public string Serial { get; set; }
        [DataMember(Order = 2)] public string Map { get; set; }
        [DataMember(Order = 3)] public string Location { get; set; }
        [DataMember(Order = 4)] public int DistanceTiles { get; set; }
        [DataMember(Order = 5)] public int Hits { get; set; }
        [DataMember(Order = 6)] public int HitsMax { get; set; }
        [DataMember(Order = 7)] public int Mana { get; set; }
        [DataMember(Order = 8)] public int ManaMax { get; set; }
        [DataMember(Order = 9)] public bool Alive { get; set; }
        [DataMember(Order = 10)] public bool IsHostile { get; set; }
        [DataMember(Order = 11)] public bool IsMage { get; set; }
        [DataMember(Order = 12)] public bool IsProtectee { get; set; }
        [DataMember(Order = 13)] public List<string> Capabilities { get; set; }

        public AIGMUMGSituationEntity()
        {
            Name = String.Empty;
            Serial = String.Empty;
            Map = String.Empty;
            Location = String.Empty;
            Alive = true;
            Capabilities = new List<string>();
        }

        public double HealthPct
        {
            get { return HitsMax > 0 ? (double)Hits * 100.0 / HitsMax : 0.0; }
        }
    }

    [DataContract]
    public sealed class AIGMUMGSituationSnapshot
    {
        [DataMember(Order = 0)] public string SnapshotId { get; set; }
        [DataMember(Order = 1)] public AIGMUMGSituationSnapshotSource Source { get; set; }
        [DataMember(Order = 2)] public DateTime CapturedUtc { get; set; }
        [DataMember(Order = 3)] public string ActorSerial { get; set; }
        [DataMember(Order = 4)] public string ActorKey { get; set; }
        [DataMember(Order = 5)] public string Map { get; set; }
        [DataMember(Order = 6)] public string Location { get; set; }
        [DataMember(Order = 7)] public string OperationalState { get; set; }
        [DataMember(Order = 8)] public int CurrentCommandEpoch { get; set; }
        [DataMember(Order = 9)] public string CurrentMovementOwner { get; set; }
        [DataMember(Order = 10)] public string CurrentMission { get; set; }
        [DataMember(Order = 11)] public int ActorHits { get; set; }
        [DataMember(Order = 12)] public int ActorHitsMax { get; set; }
        [DataMember(Order = 13)] public int ActorMana { get; set; }
        [DataMember(Order = 14)] public int ActorManaMax { get; set; }
        [DataMember(Order = 15)] public int ActorStamina { get; set; }
        [DataMember(Order = 16)] public int ActorStaminaMax { get; set; }
        [DataMember(Order = 17)] public List<AIGMUMGSituationEntity> NearbyHostiles { get; set; }
        [DataMember(Order = 18)] public List<AIGMUMGSituationEntity> NearbyAllies { get; set; }
        [DataMember(Order = 19)] public string CurrentProtectee { get; set; }
        [DataMember(Order = 20)] public string CommanderSerial { get; set; }
        [DataMember(Order = 21)] public string ExplicitCommanderOrder { get; set; }
        [DataMember(Order = 22)] public string CurrentTarget { get; set; }
        [DataMember(Order = 23)] public List<string> TargetCapabilities { get; set; }
        [DataMember(Order = 24)] public List<string> ActiveCooldowns { get; set; }
        [DataMember(Order = 25)] public string InventoryCapabilitySummary { get; set; }
        [DataMember(Order = 26)] public string SpellCapabilitySummary { get; set; }
        [DataMember(Order = 27)] public string WeaponCapabilitySummary { get; set; }
        [DataMember(Order = 28)] public string CorrelationId { get; set; }
        [DataMember(Order = 29)] public string ScenarioId { get; set; }
        [DataMember(Order = 30)] public int BandageCount { get; set; }
        [DataMember(Order = 31)] public int PotionCount { get; set; }
        [DataMember(Order = 32)] public int ReagentCount { get; set; }
        [DataMember(Order = 33)] public int AmmunitionCount { get; set; }
        [DataMember(Order = 34)] public int SpellbookCount { get; set; }
        [DataMember(Order = 35)] public List<string> RemovedCapabilities { get; set; }

        public AIGMUMGSituationSnapshot()
        {
            SnapshotId = Guid.NewGuid().ToString("N");
            Source = AIGMUMGSituationSnapshotSource.CurrentWorld;
            CapturedUtc = DateTime.UtcNow;
            ActorSerial = String.Empty;
            ActorKey = String.Empty;
            Map = String.Empty;
            Location = String.Empty;
            OperationalState = String.Empty;
            CurrentMovementOwner = "none";
            CurrentMission = String.Empty;
            NearbyHostiles = new List<AIGMUMGSituationEntity>();
            NearbyAllies = new List<AIGMUMGSituationEntity>();
            CurrentProtectee = String.Empty;
            CommanderSerial = String.Empty;
            ExplicitCommanderOrder = String.Empty;
            CurrentTarget = String.Empty;
            TargetCapabilities = new List<string>();
            ActiveCooldowns = new List<string>();
            InventoryCapabilitySummary = String.Empty;
            SpellCapabilitySummary = String.Empty;
            WeaponCapabilitySummary = String.Empty;
            CorrelationId = String.Empty;
            ScenarioId = String.Empty;
            RemovedCapabilities = new List<string>();
        }

        public double ActorHealthPct
        {
            get { return ActorHitsMax > 0 ? (double)ActorHits * 100.0 / ActorHitsMax : 0.0; }
        }

        public double ActorManaPct
        {
            get { return ActorManaMax > 0 ? (double)ActorMana * 100.0 / ActorManaMax : 0.0; }
        }

        public static AIGMUMGSituationSnapshot CaptureCurrentWorld(Mobile actor, string correlationId)
        {
            AIGMUMGSituationSnapshot snapshot = CaptureActorCore(actor, AIGMUMGSituationSnapshotSource.CurrentWorld, DateTime.UtcNow, correlationId);
            CaptureNearbyMobiles(actor, snapshot);
            return snapshot;
        }

        public static AIGMUMGSituationSnapshot BuildSynthetic(Mobile actor, string scenarioId, DateTime nowUtc, string correlationId)
        {
            string id = (scenarioId ?? String.Empty).Trim().ToLowerInvariant();
            AIGMUMGSituationSnapshot snapshot = CaptureActorCore(actor, AIGMUMGSituationSnapshotSource.SyntheticProof, nowUtc, correlationId);
            snapshot.ScenarioId = id;
            snapshot.NearbyHostiles.Clear();
            snapshot.NearbyAllies.Clear();
            snapshot.CurrentTarget = String.Empty;
            snapshot.ExplicitCommanderOrder = String.Empty;
            snapshot.ActiveCooldowns.Clear();

            switch (id)
            {
                case "enemy-mage":
                    AddHostile(snapshot, "Synthetic enemy mage", "0x7E510001", 6, true);
                    snapshot.CurrentTarget = "0x7E510001";
                    break;
                case "protectee-injured":
                    AddProtectee(snapshot, "Danyal", "0x00002AA5", 36, 100);
                    AddHostile(snapshot, "Synthetic threat", "0x7E510002", 5, false);
                    break;
                case "protectee-threshold-44":
                    AddProtectee(snapshot, "Danyal", "0x00002AA5", 44, 100);
                    AddHostile(snapshot, "Synthetic threat", "0x7E510012", 5, false);
                    break;
                case "protectee-threshold-48":
                    AddProtectee(snapshot, "Danyal", "0x00002AA5", 48, 100);
                    AddHostile(snapshot, "Synthetic threat", "0x7E510013", 5, false);
                    break;
                case "protectee-threshold-56":
                    AddProtectee(snapshot, "Danyal", "0x00002AA5", 56, 100);
                    AddHostile(snapshot, "Synthetic threat", "0x7E510014", 5, false);
                    break;
                case "low-mana":
                    SetManaPct(snapshot, 18.0);
                    break;
                case "mana-threshold-39":
                    SetManaPct(snapshot, 39.0);
                    break;
                case "mana-threshold-41":
                    SetManaPct(snapshot, 41.0);
                    break;
                case "mana-threshold-56":
                    SetManaPct(snapshot, 56.0);
                    break;
                case "enemy-mage-low-mana":
                    AddHostile(snapshot, "Synthetic enemy mage", "0x7E510003", 5, true);
                    snapshot.CurrentTarget = "0x7E510003";
                    SetManaPct(snapshot, 18.0);
                    break;
                case "protectee-injured-low-mana":
                    AddProtectee(snapshot, "Danyal", "0x00002AA5", 34, 100);
                    AddHostile(snapshot, "Synthetic threat", "0x7E510004", 4, false);
                    SetManaPct(snapshot, 18.0);
                    break;
                case "multi-pressure":
                    AddHostile(snapshot, "Synthetic enemy mage", "0x7E510005", 4, true);
                    AddHostile(snapshot, "Synthetic close attacker", "0x7E510006", 2, false);
                    AddProtectee(snapshot, "Danyal", "0x00002AA5", 31, 100);
                    snapshot.CurrentTarget = "0x7E510005";
                    SetManaPct(snapshot, 17.0);
                    break;
                case "commander-hold":
                    snapshot.ExplicitCommanderOrder = "hold";
                    AddHostile(snapshot, "Synthetic pressure", "0x7E510007", 5, false);
                    break;
                case "commander-stop":
                    snapshot.ExplicitCommanderOrder = "stop";
                    AddHostile(snapshot, "Synthetic pressure", "0x7E510008", 5, false);
                    break;
                case "stand-down":
                    snapshot.OperationalState = "OperationalMode=PassiveStandDown; Epoch=" + snapshot.CurrentCommandEpoch + "; LastCommand=synthetic_stand_down";
                    AddHostile(snapshot, "Synthetic pressure", "0x7E510009", 5, false);
                    break;
                case "capability-missing":
                    AddHostile(snapshot, "Synthetic enemy mage", "0x7E51000A", 5, true);
                    snapshot.CurrentTarget = "0x7E51000A";
                    snapshot.RemovedCapabilities.Add(AIGMUMGCapabilityKind.CanGuard.ToString());
                    break;
                case "target-lost":
                    snapshot.CurrentTarget = "lost";
                    break;
                case "threat-cleared":
                    snapshot.CurrentTarget = String.Empty;
                    snapshot.ActiveCooldowns.Add("threat_cleared_release_candidate");
                    break;
                case "quiet":
                default:
                    snapshot.ScenarioId = String.IsNullOrWhiteSpace(id) ? "quiet" : id;
                    break;
            }

            return snapshot;
        }

        private static AIGMUMGSituationSnapshot CaptureActorCore(Mobile actor, AIGMUMGSituationSnapshotSource source, DateTime nowUtc, string correlationId)
        {
            AIGMUMGSituationSnapshot snapshot = new AIGMUMGSituationSnapshot();
            snapshot.Source = source;
            snapshot.CapturedUtc = nowUtc;
            snapshot.CorrelationId = correlationId ?? String.Empty;

            if (actor == null || actor.Deleted)
                return snapshot;

            snapshot.ActorSerial = FormatSerial(actor);
            snapshot.ActorKey = AIGMUMGOperationalLayoutService.ResolveOperationalActorKey(actor);
            snapshot.Map = actor.Map != null ? actor.Map.Name : "Internal";
            snapshot.Location = FormatLocation(actor.Location);
            snapshot.OperationalState = AIGMOperationalControlService.DescribeState(actor);
            snapshot.CurrentCommandEpoch = AIGMOperationalControlService.GetEpoch(actor);
            snapshot.ActorHits = actor.Hits;
            snapshot.ActorHitsMax = Math.Max(1, actor.HitsMax);
            snapshot.ActorMana = actor.Mana;
            snapshot.ActorManaMax = Math.Max(1, actor.ManaMax);
            snapshot.ActorStamina = actor.Stam;
            snapshot.ActorStaminaMax = Math.Max(1, actor.StamMax);

            BaseCreature creature = actor as BaseCreature;
            if (creature != null)
            {
                snapshot.CommanderSerial = creature.ControlMaster != null ? FormatSerial(creature.ControlMaster) : String.Empty;
                snapshot.ExplicitCommanderOrder = creature.ControlOrder.ToString();
                Mobile combatant = creature.Combatant as Mobile;
                if (combatant != null && !combatant.Deleted)
                    snapshot.CurrentTarget = FormatSerial(combatant);
            }

            IAIGMRosterTaskAgent taskAgent = actor as IAIGMRosterTaskAgent;
            if (taskAgent != null && taskAgent.RosterTaskState != null)
            {
                AIGMRosterTaskState state = taskAgent.RosterTaskState;
                snapshot.CurrentMission = state.Mode + ":" + state.TaskStatus;
                if (state.TrustedCommanderSerial.IsValid)
                    snapshot.CommanderSerial = String.Format("0x{0:X8}", state.TrustedCommanderSerial.Value);
            }

            BaseHire hire = actor as BaseHire;
            if (hire != null)
            {
                AIGMMovementLease lease = AIGMMovementOwnershipService.GetCurrentLease(hire);
                if (lease != null)
                    snapshot.CurrentMovementOwner = lease.OwnerType + ":" + lease.LeaseId;
            }

            AIGMCapabilitySnapshot capability = AIGMCapabilityRegistry.CreateSnapshot(actor);
            snapshot.BandageCount = capability.BandageCount;
            snapshot.PotionCount = capability.PotionCount;
            snapshot.ReagentCount = capability.ReagentCount;
            snapshot.AmmunitionCount = capability.AmmunitionCount;
            snapshot.SpellbookCount = capability.SpellbookCount;
            snapshot.InventoryCapabilitySummary = String.Format("bandages={0}; potions={1}; reagents={2}; ammunition={3}; spellbooks={4}",
                capability.BandageCount,
                capability.PotionCount,
                capability.ReagentCount,
                capability.AmmunitionCount,
                capability.SpellbookCount);
            snapshot.SpellCapabilitySummary = "defensive=" + capability.Has(AIGMUMGCapabilityKind.CanCastDefensiveSpell) + "; offensive=" + capability.Has(AIGMUMGCapabilityKind.CanCastOffensiveSpell);
            snapshot.WeaponCapabilitySummary = capability.ActiveWeapon + "/" + capability.ActiveWeaponSkill + "; nativeCombat=" + capability.NativeCombatStatus;
            return snapshot;
        }

        private static void CaptureNearbyMobiles(Mobile actor, AIGMUMGSituationSnapshot snapshot)
        {
            if (actor == null || snapshot == null || actor.Map == null || actor.Map == Server.Map.Internal)
                return;

            IPooledEnumerable eable = actor.GetMobilesInRange(12);
            try
            {
                foreach (Mobile mobile in eable)
                {
                    if (mobile == null || mobile == actor || mobile.Deleted || mobile.Map != actor.Map)
                        continue;

                    bool hostile = IsHostile(actor, mobile);
                    bool ally = IsAlly(actor, mobile);
                    if (!hostile && !ally)
                        continue;

                    AIGMUMGSituationEntity entity = FromMobile(actor, mobile, hostile, ally);
                    if (hostile)
                    {
                        if (snapshot.NearbyHostiles.Count < 16)
                            snapshot.NearbyHostiles.Add(entity);
                    }
                    else if (ally)
                    {
                        if (snapshot.NearbyAllies.Count < 16)
                            snapshot.NearbyAllies.Add(entity);
                    }
                }
            }
            finally
            {
                eable.Free();
            }

            snapshot.NearbyHostiles.Sort(delegate (AIGMUMGSituationEntity left, AIGMUMGSituationEntity right) { return left.DistanceTiles.CompareTo(right.DistanceTiles); });
            snapshot.NearbyAllies.Sort(delegate (AIGMUMGSituationEntity left, AIGMUMGSituationEntity right) { return left.DistanceTiles.CompareTo(right.DistanceTiles); });
        }

        private static AIGMUMGSituationEntity FromMobile(Mobile actor, Mobile mobile, bool hostile, bool ally)
        {
            AIGMUMGSituationEntity entity = new AIGMUMGSituationEntity();
            entity.Name = SafeName(mobile);
            entity.Serial = FormatSerial(mobile);
            entity.Map = mobile.Map != null ? mobile.Map.Name : "Internal";
            entity.Location = FormatLocation(mobile.Location);
            entity.DistanceTiles = actor != null ? (int)Math.Round(actor.GetDistanceToSqrt(mobile)) : 0;
            entity.Hits = mobile.Hits;
            entity.HitsMax = Math.Max(1, mobile.HitsMax);
            entity.Mana = mobile.Mana;
            entity.ManaMax = Math.Max(1, mobile.ManaMax);
            entity.Alive = mobile.Alive;
            entity.IsHostile = hostile;
            entity.IsProtectee = ally;
            entity.IsMage = IsMageLike(mobile);
            if (entity.IsMage)
                entity.Capabilities.Add("mage_like");
            return entity;
        }

        private static bool IsHostile(Mobile actor, Mobile mobile)
        {
            if (actor == null || mobile == null || !mobile.Alive || mobile.IsStaff())
                return false;

            if (mobile.Combatant == actor)
                return true;

            BaseCreature creature = mobile as BaseCreature;
            return creature != null && creature.Combatant == actor;
        }

        private static bool IsAlly(Mobile actor, Mobile mobile)
        {
            if (actor == null || mobile == null || !mobile.Alive)
                return false;

            BaseCreature actorCreature = actor as BaseCreature;
            BaseCreature otherCreature = mobile as BaseCreature;
            if (actorCreature != null && otherCreature != null && actorCreature.ControlMaster != null && otherCreature.ControlMaster == actorCreature.ControlMaster)
                return true;

            IAIGMRosterTaskAgent actorAgent = actor as IAIGMRosterTaskAgent;
            IAIGMRosterTaskAgent otherAgent = mobile as IAIGMRosterTaskAgent;
            return actorAgent != null && otherAgent != null;
        }

        private static bool IsMageLike(Mobile mobile)
        {
            if (mobile == null)
                return false;

            if (mobile.Skills[SkillName.Magery] != null && mobile.Skills[SkillName.Magery].Value >= 40.0)
                return true;

            string typeName = mobile.GetType().Name;
            return IndexOf(typeName, "mage") >= 0
                || IndexOf(typeName, "lich") >= 0
                || IndexOf(typeName, "spell") >= 0
                || IndexOf(typeName, "wizard") >= 0;
        }

        private static void AddHostile(AIGMUMGSituationSnapshot snapshot, string name, string serial, int distance, bool mage)
        {
            AIGMUMGSituationEntity entity = new AIGMUMGSituationEntity();
            entity.Name = name;
            entity.Serial = serial;
            entity.Map = snapshot.Map;
            entity.Location = snapshot.Location;
            entity.DistanceTiles = distance;
            entity.Hits = 100;
            entity.HitsMax = 100;
            entity.Mana = mage ? 100 : 0;
            entity.ManaMax = mage ? 100 : 1;
            entity.Alive = true;
            entity.IsHostile = true;
            entity.IsMage = mage;
            if (mage)
                entity.Capabilities.Add("mage_like");
            snapshot.NearbyHostiles.Add(entity);
        }

        private static void AddProtectee(AIGMUMGSituationSnapshot snapshot, string name, string serial, int hits, int hitsMax)
        {
            AIGMUMGSituationEntity entity = new AIGMUMGSituationEntity();
            entity.Name = name;
            entity.Serial = serial;
            entity.Map = snapshot.Map;
            entity.Location = snapshot.Location;
            entity.DistanceTiles = 4;
            entity.Hits = hits;
            entity.HitsMax = Math.Max(1, hitsMax);
            entity.Alive = true;
            entity.IsProtectee = true;
            snapshot.CurrentProtectee = name + "[" + serial + "]";
            snapshot.NearbyAllies.Add(entity);
        }

        private static void SetManaPct(AIGMUMGSituationSnapshot snapshot, double pct)
        {
            snapshot.ActorManaMax = Math.Max(1, snapshot.ActorManaMax);
            snapshot.ActorMana = Math.Max(0, Math.Min(snapshot.ActorManaMax, (int)Math.Round(snapshot.ActorManaMax * pct / 100.0)));
        }

        private static string FormatSerial(Mobile mobile)
        {
            return mobile != null ? String.Format("0x{0:X8}", mobile.Serial.Value) : String.Empty;
        }

        private static string FormatLocation(Point3D point)
        {
            return String.Format("{0},{1},{2}", point.X, point.Y, point.Z);
        }

        private static string SafeName(Mobile mobile)
        {
            return mobile == null ? "none" : (String.IsNullOrWhiteSpace(mobile.Name) ? mobile.GetType().Name : mobile.Name);
        }

        private static int IndexOf(string value, string needle)
        {
            return (value ?? String.Empty).IndexOf(needle, StringComparison.OrdinalIgnoreCase);
        }
    }
}
