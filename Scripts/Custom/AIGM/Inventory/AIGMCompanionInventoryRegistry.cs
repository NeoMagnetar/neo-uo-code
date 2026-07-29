using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Server.Custom.AIGM.Inventory
{
    public static class AIGMCompanionInventoryRegistry
    {
        private const int SchemaVersion = 1;
        private static readonly object SyncRoot = new object();
        private static bool _loaded;
        private static Dictionary<string, AIGMCompanionInventoryRegistryEntry> _entries;

        public static AIGMCompanionInventoryRegistryEntry Get(Mobile actor)
        {
            EnsureLoaded();
            if (actor == null)
                return null;

            AIGMCompanionInventoryRegistryEntry entry;
            return _entries.TryGetValue(FormatSerial(actor.Serial), out entry) ? entry : null;
        }

        public static List<AIGMCompanionInventoryRegistryEntry> GetAll()
        {
            EnsureLoaded();
            return new List<AIGMCompanionInventoryRegistryEntry>(_entries.Values);
        }

        public static void Upsert(Mobile actor, AIGMCompanionBackpack backpack, string provenance, Serial previousBackpackSerial, string validationStatus)
        {
            if (actor == null || backpack == null)
                return;

            EnsureLoaded();
            string actorSerial = FormatSerial(actor.Serial);
            AIGMCompanionInventoryRegistryEntry entry;
            if (!_entries.TryGetValue(actorSerial, out entry))
                entry = new AIGMCompanionInventoryRegistryEntry();

            entry.ActorSerial = actorSerial;
            entry.BackpackSerial = FormatSerial(backpack.Serial);
            entry.MarkerVersion = backpack.MarkerVersion;
            entry.Provenance = String.IsNullOrWhiteSpace(provenance) ? backpack.Provenance : provenance;
            entry.PreviousBackpackSerial = previousBackpackSerial.IsValid ? FormatSerial(previousBackpackSerial) : entry.PreviousBackpackSerial;
            entry.MigrationTimestampUtc = previousBackpackSerial.IsValid ? DateTime.UtcNow.ToString("o") : entry.MigrationTimestampUtc;
            entry.ValidationStatus = validationStatus ?? String.Empty;
            entry.UpdatedUtc = DateTime.UtcNow.ToString("o");

            _entries[actorSerial] = entry;
            Save();
        }

        public static void MarkDeleted(AIGMCompanionBackpack backpack, string reason)
        {
            if (backpack == null)
                return;

            EnsureLoaded();
            string backpackSerial = FormatSerial(backpack.Serial);
            foreach (AIGMCompanionInventoryRegistryEntry entry in _entries.Values)
            {
                if (!String.Equals(entry.BackpackSerial, backpackSerial, StringComparison.OrdinalIgnoreCase))
                    continue;

                entry.ValidationStatus = String.IsNullOrWhiteSpace(reason) ? "backpack_deleted" : reason;
                entry.UpdatedUtc = DateTime.UtcNow.ToString("o");
                Save();
                return;
            }
        }

        public static void Reload()
        {
            lock (SyncRoot)
            {
                _loaded = false;
                _entries = null;
            }
        }

        private static void EnsureLoaded()
        {
            if (_loaded)
                return;

            lock (SyncRoot)
            {
                if (_loaded)
                    return;

                _entries = new Dictionary<string, AIGMCompanionInventoryRegistryEntry>(StringComparer.OrdinalIgnoreCase);
                AIGMCompanionInventoryRegistryFile file = ReadFile();
                if (file != null && file.Entries != null)
                {
                    for (int i = 0; i < file.Entries.Count; i++)
                    {
                        AIGMCompanionInventoryRegistryEntry entry = file.Entries[i];
                        if (entry != null && !String.IsNullOrWhiteSpace(entry.ActorSerial))
                            _entries[entry.ActorSerial] = entry;
                    }
                }

                _loaded = true;
            }
        }

        private static AIGMCompanionInventoryRegistryFile ReadFile()
        {
            string path = GetPath();
            if (!File.Exists(path))
                return new AIGMCompanionInventoryRegistryFile();

            try
            {
                using (FileStream stream = File.OpenRead(path))
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(AIGMCompanionInventoryRegistryFile));
                    return serializer.ReadObject(stream) as AIGMCompanionInventoryRegistryFile;
                }
            }
            catch (Exception ex)
            {
                AIGMCompanionInventoryLog.Write("registry_load_error", null, AIGMCompanionInventoryLog.Fields("path", path, "error", ex.Message));
                return new AIGMCompanionInventoryRegistryFile();
            }
        }

        private static void Save()
        {
            lock (SyncRoot)
            {
                string path = GetPath();
                string dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                AIGMCompanionInventoryRegistryFile file = new AIGMCompanionInventoryRegistryFile
                {
                    SchemaVersion = SchemaVersion,
                    Entries = new List<AIGMCompanionInventoryRegistryEntry>(_entries.Values)
                };

                string temp = path + ".tmp";
                using (FileStream stream = File.Create(temp))
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(AIGMCompanionInventoryRegistryFile));
                    serializer.WriteObject(stream, file);
                }

                if (File.Exists(path))
                    File.Delete(path);

                File.Move(temp, path);
            }
        }

        private static string GetPath()
        {
            return Path.Combine(Core.BaseDirectory, "Data", "AIGM", "Inventory", "companion_backpacks_v1.json");
        }

        private static string FormatSerial(Serial serial)
        {
            return serial.IsValid ? String.Format("0x{0:X8}", serial.Value) : String.Empty;
        }
    }

    [DataContract]
    public sealed class AIGMCompanionInventoryRegistryFile
    {
        [DataMember(Order = 0)]
        public int SchemaVersion { get; set; }

        [DataMember(Order = 1)]
        public List<AIGMCompanionInventoryRegistryEntry> Entries { get; set; }

        public AIGMCompanionInventoryRegistryFile()
        {
            SchemaVersion = 1;
            Entries = new List<AIGMCompanionInventoryRegistryEntry>();
        }
    }

    [DataContract]
    public sealed class AIGMCompanionInventoryRegistryEntry
    {
        [DataMember(Order = 0)]
        public string ActorSerial { get; set; }

        [DataMember(Order = 1)]
        public string BackpackSerial { get; set; }

        [DataMember(Order = 2)]
        public int MarkerVersion { get; set; }

        [DataMember(Order = 3)]
        public string Provenance { get; set; }

        [DataMember(Order = 4)]
        public string PreviousBackpackSerial { get; set; }

        [DataMember(Order = 5)]
        public string MigrationTimestampUtc { get; set; }

        [DataMember(Order = 6)]
        public string ValidationStatus { get; set; }

        [DataMember(Order = 7)]
        public string UpdatedUtc { get; set; }
    }
}
