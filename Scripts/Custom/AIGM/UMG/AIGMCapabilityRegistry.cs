using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Server.Custom.AIGM.Characters.Waylander;
using Server.Custom.AIGM.Tasks;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.AIGM.UMG
{
    [DataContract]
    public sealed class AIGMCapabilitySkillValue
    {
        [DataMember(Order = 0)] public string Skill { get; set; }
        [DataMember(Order = 1)] public double Value { get; set; }

        public AIGMCapabilitySkillValue()
        {
            Skill = String.Empty;
        }

        public AIGMCapabilitySkillValue(string skill, double value)
        {
            Skill = skill ?? String.Empty;
            Value = value;
        }
    }

    [DataContract]
    public sealed class AIGMCapabilitySnapshot
    {
        [DataMember(Order = 0)] public string ActorName { get; set; }
        [DataMember(Order = 1)] public string ActorId { get; set; }
        [DataMember(Order = 2)] public string Serial { get; set; }
        [DataMember(Order = 3)] public string MobileType { get; set; }
        [DataMember(Order = 4)] public int Hits { get; set; }
        [DataMember(Order = 5)] public int HitsMax { get; set; }
        [DataMember(Order = 6)] public int Mana { get; set; }
        [DataMember(Order = 7)] public int ManaMax { get; set; }
        [DataMember(Order = 8)] public int Stamina { get; set; }
        [DataMember(Order = 9)] public int StaminaMax { get; set; }
        [DataMember(Order = 10)] public double HealthPct { get; set; }
        [DataMember(Order = 11)] public double ManaPct { get; set; }
        [DataMember(Order = 12)] public double StaminaPct { get; set; }
        [DataMember(Order = 13)] public bool Hidden { get; set; }
        [DataMember(Order = 14)] public bool Poisoned { get; set; }
        [DataMember(Order = 15)] public bool Mounted { get; set; }
        [DataMember(Order = 16)] public string OperationalMode { get; set; }
        [DataMember(Order = 17)] public string CurrentTask { get; set; }
        [DataMember(Order = 18)] public string ActiveWeapon { get; set; }
        [DataMember(Order = 19)] public string ActiveWeaponSkill { get; set; }
        [DataMember(Order = 20)] public string ActiveWeaponAnimation { get; set; }
        [DataMember(Order = 21)] public int ActiveWeaponHitSound { get; set; }
        [DataMember(Order = 22)] public int ActiveWeaponMissSound { get; set; }
        [DataMember(Order = 23)] public int BandageCount { get; set; }
        [DataMember(Order = 24)] public int PotionCount { get; set; }
        [DataMember(Order = 25)] public int ReagentCount { get; set; }
        [DataMember(Order = 26)] public int AmmunitionCount { get; set; }
        [DataMember(Order = 27)] public int SpellbookCount { get; set; }
        [DataMember(Order = 28)] public List<AIGMCapabilitySkillValue> Skills { get; set; }
        [DataMember(Order = 29)] public List<string> Equipment { get; set; }
        [DataMember(Order = 30)] public List<AIGMUMGCapabilityKind> Capabilities { get; set; }
        [DataMember(Order = 31)] public List<string> CapabilityEvidence { get; set; }
        [DataMember(Order = 32)] public List<string> KnownDefectsOrUnsupportedServices { get; set; }
        [DataMember(Order = 33)] public string ActiveWeaponSource { get; set; }
        [DataMember(Order = 34)] public string ImplementationItem { get; set; }
        [DataMember(Order = 35)] public string AuthoritativeCombatSkill { get; set; }
        [DataMember(Order = 36)] public string FallbackCombatSkill { get; set; }
        [DataMember(Order = 37)] public bool WrestlingFallback { get; set; }
        [DataMember(Order = 38)] public string NativeCombatStatus { get; set; }
        [DataMember(Order = 39)] public string TrackingStatus { get; set; }
        [DataMember(Order = 40)] public string WaypointTravelStatus { get; set; }
        [DataMember(Order = 41)] public string AutonomousTaskStatus { get; set; }
        [DataMember(Order = 42)] public string OffensiveSpellcastingStatus { get; set; }

        public AIGMCapabilitySnapshot()
        {
            ActorName = String.Empty;
            ActorId = String.Empty;
            Serial = String.Empty;
            MobileType = String.Empty;
            OperationalMode = String.Empty;
            CurrentTask = String.Empty;
            ActiveWeapon = String.Empty;
            ActiveWeaponSkill = String.Empty;
            ActiveWeaponAnimation = String.Empty;
            ActiveWeaponSource = "Unarmed";
            ImplementationItem = "None";
            AuthoritativeCombatSkill = "Wrestling";
            FallbackCombatSkill = "None";
            NativeCombatStatus = "Unsupported";
            TrackingStatus = "Unsupported";
            WaypointTravelStatus = "Unsupported";
            AutonomousTaskStatus = "Unsupported";
            OffensiveSpellcastingStatus = "Unsupported";
            Skills = new List<AIGMCapabilitySkillValue>();
            Equipment = new List<string>();
            Capabilities = new List<AIGMUMGCapabilityKind>();
            CapabilityEvidence = new List<string>();
            KnownDefectsOrUnsupportedServices = new List<string>();
        }

        public bool Has(AIGMUMGCapabilityKind kind)
        {
            return Capabilities.Contains(kind);
        }

        public double GetSkill(string skillName)
        {
            if (String.IsNullOrWhiteSpace(skillName))
                return 0.0;

            for (int i = 0; i < Skills.Count; i++)
            {
                if (String.Equals(Skills[i].Skill, skillName, StringComparison.OrdinalIgnoreCase))
                    return Skills[i].Value;
            }

            return 0.0;
        }
    }

    public static class AIGMCapabilityRegistry
    {
        public static AIGMCapabilitySnapshot CreateSnapshot(Mobile mobile)
        {
            AIGMCapabilitySnapshot snapshot = new AIGMCapabilitySnapshot();
            if (mobile == null || mobile.Deleted)
            {
                snapshot.KnownDefectsOrUnsupportedServices.Add("mobile_invalid");
                return snapshot;
            }

            snapshot.ActorName = mobile.Name ?? mobile.GetType().Name;
            snapshot.ActorId = ResolveActorId(mobile);
            snapshot.Serial = String.Format("0x{0:X8}", mobile.Serial.Value);
            snapshot.MobileType = mobile.GetType().FullName;
            snapshot.Hits = mobile.Hits;
            snapshot.HitsMax = Math.Max(1, mobile.HitsMax);
            snapshot.Mana = mobile.Mana;
            snapshot.ManaMax = Math.Max(1, mobile.ManaMax);
            snapshot.Stamina = mobile.Stam;
            snapshot.StaminaMax = Math.Max(1, mobile.StamMax);
            snapshot.HealthPct = Math.Round((double)snapshot.Hits * 100.0 / snapshot.HitsMax, 2);
            snapshot.ManaPct = Math.Round((double)snapshot.Mana * 100.0 / snapshot.ManaMax, 2);
            snapshot.StaminaPct = Math.Round((double)snapshot.Stamina * 100.0 / snapshot.StaminaMax, 2);
            snapshot.Hidden = mobile.Hidden;
            snapshot.Poisoned = mobile.Poisoned;
            snapshot.Mounted = mobile.Mounted;
            snapshot.OperationalMode = AIGMOperationalControlService.DescribeState(mobile);
            snapshot.CurrentTask = BuildTaskSummary(mobile);

            AddSkills(snapshot, mobile);
            AddEquipment(snapshot, mobile);
            AddResources(snapshot, mobile);
            AddWeapon(snapshot, mobile);
            AddCapabilities(snapshot, mobile);
            AddUnsupportedNotes(snapshot, mobile);
            return snapshot;
        }

        public static AIGMCapabilityValidationResult Validate(AIGMCapabilitySnapshot snapshot, IEnumerable<AIGMCapabilityRequirement> requirements)
        {
            AIGMCapabilityValidationResult result = new AIGMCapabilityValidationResult();
            if (snapshot == null)
            {
                result.IsValid = false;
                result.Failed.Add("capability_snapshot_missing");
                return result;
            }

            if (requirements == null)
                return result;

            foreach (AIGMCapabilityRequirement requirement in requirements)
            {
                if (requirement == null)
                    continue;

                string label = requirement.Kind.ToString();
                if (!String.IsNullOrWhiteSpace(requirement.Reason))
                    label += "(" + requirement.Reason + ")";

                if (!snapshot.Has(requirement.Kind))
                {
                    result.IsValid = false;
                    result.Failed.Add(label + ":missing");
                    continue;
                }

                if (!String.IsNullOrWhiteSpace(requirement.SkillName) && requirement.MinimumSkillValue > 0.0)
                {
                    double actual = snapshot.GetSkill(requirement.SkillName);
                    if (actual < requirement.MinimumSkillValue)
                    {
                        result.IsValid = false;
                        result.Failed.Add(String.Format("{0}:{1:0.0}<{2:0.0}", requirement.SkillName, actual, requirement.MinimumSkillValue));
                        continue;
                    }
                }

                if (!String.IsNullOrWhiteSpace(requirement.ItemTypeName) && !HasEquipmentOrResource(snapshot, requirement.ItemTypeName))
                {
                    result.IsValid = false;
                    result.Failed.Add(requirement.ItemTypeName + ":missing");
                    continue;
                }

                if (!String.IsNullOrWhiteSpace(requirement.SpellName)
                    && !String.Equals(requirement.SpellName, "heal", StringComparison.OrdinalIgnoreCase)
                    && !String.Equals(requirement.SpellName, "cure", StringComparison.OrdinalIgnoreCase)
                    && !String.Equals(requirement.SpellName, "greater heal", StringComparison.OrdinalIgnoreCase)
                    && !String.Equals(requirement.SpellName, "bless", StringComparison.OrdinalIgnoreCase))
                {
                    result.IsValid = false;
                    result.Failed.Add(requirement.SpellName + ":adapter_not_supported");
                    continue;
                }

                result.Passed.Add(label);
            }

            AIGMUMGLog.Write(
                "capability_validation",
                null,
                AIGMUMGLog.Fields(
                    "actor", snapshot.ActorName,
                    "actorId", snapshot.ActorId,
                    "valid", result.IsValid.ToString(),
                    "failed", String.Join("|", result.Failed.ToArray())));

            return result;
        }

        public static string BuildCompactSummary(AIGMCapabilitySnapshot snapshot)
        {
            if (snapshot == null)
                return "capability snapshot unavailable";

            return String.Format(
                "{0} {1}: hp={2:0}% mana={3:0}% stam={4:0}% weaponSource={5} weapon={6}/{7} nativeCombat={8} bandages={9} potions={10} reagents={11} ammo={12} caps={13}",
                snapshot.ActorName,
                snapshot.Serial,
                snapshot.HealthPct,
                snapshot.ManaPct,
                snapshot.StaminaPct,
                String.IsNullOrWhiteSpace(snapshot.ActiveWeaponSource) ? "unknown" : snapshot.ActiveWeaponSource,
                String.IsNullOrWhiteSpace(snapshot.ActiveWeapon) ? "none" : snapshot.ActiveWeapon,
                String.IsNullOrWhiteSpace(snapshot.ActiveWeaponSkill) ? "none" : snapshot.ActiveWeaponSkill,
                snapshot.NativeCombatStatus,
                snapshot.BandageCount,
                snapshot.PotionCount,
                snapshot.ReagentCount,
                snapshot.AmmunitionCount,
                snapshot.Capabilities.Count);
        }

        private static void AddSkills(AIGMCapabilitySnapshot snapshot, Mobile mobile)
        {
            AddSkill(snapshot, mobile, SkillName.Swords);
            AddSkill(snapshot, mobile, SkillName.Fencing);
            AddSkill(snapshot, mobile, SkillName.Macing);
            AddSkill(snapshot, mobile, SkillName.Archery);
            AddSkill(snapshot, mobile, SkillName.Wrestling);
            AddSkill(snapshot, mobile, SkillName.Tactics);
            AddSkill(snapshot, mobile, SkillName.Anatomy);
            AddSkill(snapshot, mobile, SkillName.Healing);
            AddSkill(snapshot, mobile, SkillName.Magery);
            AddSkill(snapshot, mobile, SkillName.EvalInt);
            AddSkill(snapshot, mobile, SkillName.MagicResist);
            AddSkill(snapshot, mobile, SkillName.Parry);
            AddSkill(snapshot, mobile, SkillName.Tracking);
            AddSkill(snapshot, mobile, SkillName.DetectHidden);
            AddSkill(snapshot, mobile, SkillName.Hiding);
            AddSkill(snapshot, mobile, SkillName.Stealth);
        }

        private static void AddSkill(AIGMCapabilitySnapshot snapshot, Mobile mobile, SkillName skillName)
        {
            snapshot.Skills.Add(new AIGMCapabilitySkillValue(skillName.ToString(), mobile.Skills[skillName].Value));
        }

        private static void AddEquipment(AIGMCapabilitySnapshot snapshot, Mobile mobile)
        {
            for (int i = 0; i < mobile.Items.Count; i++)
            {
                Item item = mobile.Items[i];
                if (item == null || item.Deleted || item.Layer == Layer.Backpack)
                    continue;

                snapshot.Equipment.Add(DescribeItem(item));
            }
        }

        private static void AddResources(AIGMCapabilitySnapshot snapshot, Mobile mobile)
        {
            Container pack = mobile.Backpack;
            if (pack == null || pack.Deleted)
                return;

            snapshot.BandageCount = CountItems<Bandage>(pack);
            snapshot.PotionCount = CountItems<BasePotion>(pack);
            snapshot.ReagentCount = CountItems<BaseReagent>(pack);
            snapshot.AmmunitionCount = CountItems<Arrow>(pack) + CountItems<Bolt>(pack);
            snapshot.SpellbookCount = CountItems<Spellbook>(pack);
        }

        private static void AddWeapon(AIGMCapabilitySnapshot snapshot, Mobile mobile)
        {
            BaseCreature creature = mobile as BaseCreature;
            IWeapon weapon = creature != null ? creature.Weapon : null;
            BaseWeapon baseWeapon = weapon as BaseWeapon;
            Item weaponItem = weapon as Item;

            if (baseWeapon != null)
            {
                snapshot.ActiveWeapon = DescribeItem(baseWeapon);
                snapshot.ActiveWeaponSource = IsNaturalWeaponProxy(baseWeapon) ? "NaturalWeaponProxy" : "EquippedWeapon";
                snapshot.ImplementationItem = snapshot.ActiveWeapon;
                snapshot.ActiveWeaponSkill = baseWeapon.Skill.ToString();
                snapshot.AuthoritativeCombatSkill = snapshot.ActiveWeaponSkill;
                snapshot.FallbackCombatSkill = "None";
                snapshot.WrestlingFallback = false;
                snapshot.ActiveWeaponAnimation = baseWeapon.Animation.ToString();
                snapshot.ActiveWeaponHitSound = baseWeapon.HitSound;
                snapshot.ActiveWeaponMissSound = baseWeapon.MissSound;
                return;
            }

            if (weaponItem != null)
            {
                snapshot.ActiveWeapon = DescribeItem(weaponItem);
                snapshot.ActiveWeaponSource = IsNaturalWeaponProxy(weaponItem) ? "NaturalWeaponProxy" : "EquippedWeapon";
                snapshot.ImplementationItem = snapshot.ActiveWeapon;
                snapshot.AuthoritativeCombatSkill = "Unknown";
            }
            else if (weapon != null)
            {
                snapshot.ActiveWeapon = weapon.GetType().Name;
                snapshot.ActiveWeaponSource = "TemporaryWeapon";
                snapshot.ImplementationItem = snapshot.ActiveWeapon;
                snapshot.AuthoritativeCombatSkill = "Unknown";
            }
            else
            {
                snapshot.ActiveWeapon = "None";
                snapshot.ActiveWeaponSource = "Unarmed";
                snapshot.ImplementationItem = "None";
                snapshot.AuthoritativeCombatSkill = "Wrestling";
                snapshot.FallbackCombatSkill = "None";
                snapshot.WrestlingFallback = true;
            }
        }

        private static void AddCapabilities(AIGMCapabilitySnapshot snapshot, Mobile mobile)
        {
            BaseCreature creature = mobile as BaseCreature;
            BaseWeapon weapon = creature != null ? creature.Weapon as BaseWeapon : null;
            string weaponName = snapshot.ActiveWeapon ?? String.Empty;

            AddIf(snapshot, AIGMUMGCapabilityKind.CanUseSword, weapon != null && weapon.Skill == SkillName.Swords, "active weapon uses Swordsmanship");
            AddIf(snapshot, AIGMUMGCapabilityKind.CanUseAxe, weaponName.IndexOf("Axe", StringComparison.OrdinalIgnoreCase) >= 0 || weaponName.IndexOf("Snaga", StringComparison.OrdinalIgnoreCase) >= 0, "active or named weapon is axe-class");
            AddIf(snapshot, AIGMUMGCapabilityKind.CanUseBow, weapon is BaseRanged || weaponName.IndexOf("Bow", StringComparison.OrdinalIgnoreCase) >= 0 || weaponName.IndexOf("Crossbow", StringComparison.OrdinalIgnoreCase) >= 0, "active weapon is ranged");
            bool unarmedCombat = weapon == null;
            AddIf(snapshot, AIGMUMGCapabilityKind.CanWrestle, unarmedCombat && snapshot.GetSkill("Wrestling") >= 20.0, "AvailableNow: unarmed combat route uses Wrestling as authoritative skill");
            AddIf(snapshot, AIGMUMGCapabilityKind.CanHeal, snapshot.GetSkill("Healing") >= 30.0 && snapshot.GetSkill("Anatomy") >= 20.0, "Healing and Anatomy meet support threshold");
            AddIf(snapshot, AIGMUMGCapabilityKind.CanBandage, snapshot.BandageCount > 0 && snapshot.GetSkill("Healing") >= 30.0, "bandages present and Healing trained");
            AddIf(snapshot, AIGMUMGCapabilityKind.CanCure, snapshot.BandageCount > 0 || snapshot.PotionCount > 0 || CanCastSupportSpell(snapshot, mobile, "cure"), "bandage, potion, or support spell cure path exists");
            AddIf(snapshot, AIGMUMGCapabilityKind.CanCastDefensiveSpell, CanCastSupportSpell(snapshot, mobile, "heal"), "support spell service, Magery, mana, and spellbook validate");
            AddIf(snapshot, AIGMUMGCapabilityKind.CanUsePotion, snapshot.PotionCount > 0, "potion items present");
            AddIf(snapshot, AIGMUMGCapabilityKind.CanTrack, snapshot.GetSkill("Tracking") >= 20.0 || mobile is IAIGMRosterTaskAgent, "Tracking skill or roster hunt service available");
            AddIf(snapshot, AIGMUMGCapabilityKind.CanDetectHidden, snapshot.GetSkill("DetectHidden") >= 20.0, "Detect Hidden skill trained");
            AddIf(snapshot, AIGMUMGCapabilityKind.CanHide, snapshot.GetSkill("Hiding") >= 20.0, "Hiding skill trained");
            AddIf(snapshot, AIGMUMGCapabilityKind.CanStealth, snapshot.GetSkill("Stealth") >= 20.0, "Stealth skill trained");
            AddIf(snapshot, AIGMUMGCapabilityKind.CanLoot, mobile.Backpack != null, "backpack and loot service are available");
            AddIf(snapshot, AIGMUMGCapabilityKind.CanGuard, creature != null, "BaseCreature guard/native combat services available");
            AddIf(snapshot, AIGMUMGCapabilityKind.CanFollow, creature != null, "BaseCreature movement services available");
            AddIf(snapshot, AIGMUMGCapabilityKind.CanTravel, mobile is IAIGMRosterTaskAgent || mobile is BaseHire, "roster task or legacy travel service available");
            AddIf(snapshot, AIGMUMGCapabilityKind.CanUseWaypoints, mobile is IAIGMRosterTaskAgent || mobile is BaseHire, "existing navigation/waypoint services available");
            AddIf(snapshot, AIGMUMGCapabilityKind.CanReceiveSquadOrders, mobile is IAIGMRosterTaskAgent, "roster task agent interface present");
            AddIf(snapshot, AIGMUMGCapabilityKind.CanReceiveRemoteTask, mobile is IAIGMRosterTaskAgent, "roster command service targetable");
            AddIf(snapshot, AIGMUMGCapabilityKind.CanBind, IsBindable(mobile), "Waylander roster binding policy permits binding");
            AddIf(snapshot, AIGMUMGCapabilityKind.CanOperateAutonomously, mobile is IAIGMRosterTaskAgent, "roster autonomous task state exists");

            snapshot.NativeCombatStatus = weapon != null || unarmedCombat ? "AvailableNow" : "Unsupported";
            snapshot.TrackingStatus = snapshot.Has(AIGMUMGCapabilityKind.CanTrack) ? "AvailableNow" : "Unsupported";
            snapshot.WaypointTravelStatus = snapshot.Has(AIGMUMGCapabilityKind.CanUseWaypoints) ? "Degraded" : "Unsupported";
            snapshot.AutonomousTaskStatus = snapshot.Has(AIGMUMGCapabilityKind.CanOperateAutonomously) ? "Registered" : "Unsupported";
            snapshot.OffensiveSpellcastingStatus = snapshot.Has(AIGMUMGCapabilityKind.CanCastOffensiveSpell) ? "AvailableNow" : "Unsupported";

            snapshot.CapabilityEvidence.Add("NativeCombatStatus: " + snapshot.NativeCombatStatus);
            snapshot.CapabilityEvidence.Add("TrackingStatus: " + snapshot.TrackingStatus);
            snapshot.CapabilityEvidence.Add("WaypointTravelStatus: " + snapshot.WaypointTravelStatus + " (long-distance route remains unproven/degraded)");
            snapshot.CapabilityEvidence.Add("AutonomousTaskStatus: " + snapshot.AutonomousTaskStatus);
            snapshot.CapabilityEvidence.Add("OffensiveSpellcastingStatus: " + snapshot.OffensiveSpellcastingStatus);
        }

        private static bool IsNaturalWeaponProxy(Item item)
        {
            if (item == null)
                return false;

            string name = (item.Name ?? String.Empty) + " " + item.GetType().Name;
            return name.IndexOf("claw", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("natural", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool CanCastSupportSpell(AIGMCapabilitySnapshot snapshot, Mobile mobile, string spellName)
        {
            if (mobile == null || snapshot.SpellbookCount <= 0 || snapshot.GetSkill("Magery") < AIGMCompanionSpellPolicy.MinimumSupportMagery)
                return false;

            string reason;
            return AIGMCompanionSpellService.CanCastSpell(mobile, spellName, out reason);
        }

        private static void AddUnsupportedNotes(AIGMCapabilitySnapshot snapshot, Mobile mobile)
        {
            if (!snapshot.Has(AIGMUMGCapabilityKind.CanCastOffensiveSpell))
                snapshot.KnownDefectsOrUnsupportedServices.Add("offensive_spell_adapter_not_registered_phase64c");

            if (snapshot.GetSkill("Magery") > 0.0 && snapshot.SpellbookCount <= 0)
                snapshot.KnownDefectsOrUnsupportedServices.Add("magery_present_but_no_spellbook_detected");

            if (!(mobile is IAIGMRosterTaskAgent))
                snapshot.KnownDefectsOrUnsupportedServices.Add("not_roster_task_agent");
        }

        private static void AddIf(AIGMCapabilitySnapshot snapshot, AIGMUMGCapabilityKind kind, bool condition, string evidence)
        {
            if (!condition || snapshot.Capabilities.Contains(kind))
                return;

            snapshot.Capabilities.Add(kind);
            snapshot.CapabilityEvidence.Add(kind + ": " + evidence);
        }

        private static bool IsBindable(Mobile mobile)
        {
            IAIGMRosterTaskAgent agent = mobile as IAIGMRosterTaskAgent;
            if (agent == null)
                return false;

            WaylanderCharacterDefinition definition = agent.RosterDefinition;
            if (definition == null)
                return true;

            return definition.ControlPolicy == WaylanderRosterControlPolicy.BindableCompanion
                || definition.ControlPolicy == WaylanderRosterControlPolicy.ScenarioBindable;
        }

        private static string ResolveActorId(Mobile mobile)
        {
            IAIGMRosterTaskAgent roster = mobile as IAIGMRosterTaskAgent;
            if (roster != null && !String.IsNullOrWhiteSpace(roster.RosterCharacterId))
                return roster.RosterCharacterId;

            IAIGMCompanionActor companion = mobile as IAIGMCompanionActor;
            if (companion != null && !String.IsNullOrWhiteSpace(companion.CompanionId))
                return companion.CompanionId;

            return mobile != null ? (mobile.Name ?? mobile.GetType().Name).Trim().ToLowerInvariant().Replace(" ", "_") : String.Empty;
        }

        private static string BuildTaskSummary(Mobile mobile)
        {
            IAIGMRosterTaskAgent roster = mobile as IAIGMRosterTaskAgent;
            if (roster != null && roster.RosterTaskState != null)
                return roster.RosterTaskState.Mode + ":" + roster.RosterTaskState.TaskStatus;

            return "none";
        }

        private static bool HasEquipmentOrResource(AIGMCapabilitySnapshot snapshot, string itemTypeName)
        {
            string needle = itemTypeName ?? String.Empty;
            if (String.IsNullOrWhiteSpace(needle))
                return true;

            if (String.Equals(needle, "Bandage", StringComparison.OrdinalIgnoreCase))
                return snapshot.BandageCount > 0;
            if (String.Equals(needle, "BasePotion", StringComparison.OrdinalIgnoreCase))
                return snapshot.PotionCount > 0;
            if (String.Equals(needle, "BaseReagent", StringComparison.OrdinalIgnoreCase))
                return snapshot.ReagentCount > 0;
            if (String.Equals(needle, "Arrow", StringComparison.OrdinalIgnoreCase) || String.Equals(needle, "Bolt", StringComparison.OrdinalIgnoreCase))
                return snapshot.AmmunitionCount > 0;
            if (String.Equals(needle, "Spellbook", StringComparison.OrdinalIgnoreCase))
                return snapshot.SpellbookCount > 0;

            for (int i = 0; i < snapshot.Equipment.Count; i++)
            {
                if (snapshot.Equipment[i].IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return snapshot.ActiveWeapon.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static int CountItems<T>(Container container) where T : Item
        {
            if (container == null || container.Deleted)
                return 0;

            int count = 0;
            CountItemsRecursive<T>(container, ref count);
            return count;
        }

        private static void CountItemsRecursive<T>(Container container, ref int count) where T : Item
        {
            for (int i = 0; i < container.Items.Count; i++)
            {
                Item item = container.Items[i];
                if (item == null || item.Deleted)
                    continue;

                if (item is T)
                    count += Math.Max(1, item.Amount);

                Container child = item as Container;
                if (child != null)
                    CountItemsRecursive<T>(child, ref count);
            }
        }

        private static string DescribeItem(Item item)
        {
            if (item == null)
                return "none";

            string name = String.IsNullOrWhiteSpace(item.Name) ? item.GetType().Name : item.Name;
            return String.Format("{0}({1}) layer={2}", name, item.GetType().Name, item.Layer);
        }
    }
}
