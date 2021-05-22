using Kermalis.MapEditor.Util;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.World;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;

namespace Kermalis.MapEditor.Core
{
    public sealed class EncounterTable
    {
        internal sealed class Encounter
        {
            public byte Chance;
            public byte MinLevel;
            public byte MaxLevel;
            public PBESpecies Species;
            public PBEForm Form;

            public Encounter()
            {
                Chance = 1;
                MinLevel = 1;
                MaxLevel = 1;
                Species = PBESpecies.Bulbasaur;
                Form = 0;
            }
            public Encounter(JToken j)
            {
                Chance = j[nameof(Chance)].Value<byte>();
                MinLevel = j[nameof(MinLevel)].Value<byte>();
                MaxLevel = j[nameof(MaxLevel)].Value<byte>();
                Species = j[nameof(Species)].EnumValue<PBESpecies>();
                string strForm = j[nameof(Form)].Value<string>();
                if (strForm == null)
                {
                    Form = 0;
                }
                else
                {
                    Form = Enum.Parse<PBEForm>(strForm);
                }
            }

            public void Write(JsonTextWriter w)
            {
                w.WriteStartObject();
                w.WritePropertyName(nameof(Chance));
                w.WriteValue(Chance);
                w.WritePropertyName(nameof(MinLevel));
                w.WriteValue(MinLevel);
                w.WritePropertyName(nameof(MaxLevel));
                w.WriteValue(MaxLevel);
                w.WritePropertyName(nameof(Species));
                w.WriteEnum(Species);
                w.WritePropertyName(nameof(Form));
                w.WriteValue(PBEDataUtils.GetNameOfForm(Species, Form));
                w.WriteEndObject();
            }
        }

        internal readonly string Name;
        internal readonly int Id;

        internal byte ChanceOfPhenomenon;
        internal readonly List<Encounter> Encounters;

        private EncounterTable(string name, int id)
        {
            var json = JObject.Parse(File.ReadAllText(Path.Combine(EncounterTablePath, name + ".json")));
            ChanceOfPhenomenon = json[nameof(ChanceOfPhenomenon)].Value<byte>();
            var encs = (JArray)json[nameof(Encounters)];
            int numEncounters = encs.Count;
            Encounters = new List<Encounter>(numEncounters);
            for (int i = 0; i < numEncounters; i++)
            {
                Encounters.Add(new Encounter(encs[i]));
            }
            Name = name;
            Id = id;
        }
        internal EncounterTable(string name)
        {
            Id = Ids.Add(name);
            _loadedEncounterTables.Add(Id, new WeakReference<EncounterTable>(this));
            ChanceOfPhenomenon = 51; // 20%
            Encounters = new List<Encounter>() { new Encounter() };
            Name = name;
            Save();
            Ids.Save();
        }

        private static readonly string EncounterTablePath = Path.Combine(Program.AssetPath, "Encounter");
        public static IdList Ids { get; } = new IdList(Path.Combine(EncounterTablePath, "EncounterTableIds.txt"));
        private static readonly Dictionary<int, WeakReference<EncounterTable>> _loadedEncounterTables = new();
        internal static EncounterTable LoadOrGet(string name)
        {
            int id = Ids[name];
            if (id == -1)
            {
                throw new ArgumentOutOfRangeException(nameof(name));
            }
            return LoadOrGet(name, id);
        }
        internal static EncounterTable LoadOrGet(int id)
        {
            string name = Ids[id];
            if (name is null)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }
            return LoadOrGet(name, id);
        }
        private static EncounterTable LoadOrGet(string name, int id)
        {
            EncounterTable e;
            if (!_loadedEncounterTables.TryGetValue(id, out WeakReference<EncounterTable> w))
            {
                e = new EncounterTable(name, id);
                _loadedEncounterTables.Add(id, new WeakReference<EncounterTable>(e));
            }
            else if (!w.TryGetTarget(out e))
            {
                e = new EncounterTable(name, id);
                w.SetTarget(e);
            }
            return e;
        }

        internal float GetChanceOfPhenomenon()
        {
            return ChanceOfPhenomenon / (float)byte.MaxValue;
        }
        internal ushort GetCombinedChance()
        {
            ushort sum = 0;
            for (int i = 0; i < Encounters.Count; i++)
            {
                sum += Encounters[i].Chance;
            }
            return sum;
        }

        internal void Save()
        {
            using (var w = new JsonTextWriter(File.CreateText(Path.Combine(EncounterTablePath, Name + ".json"))) { Formatting = Formatting.Indented })
            {
                w.WriteStartObject();
                w.WritePropertyName(nameof(ChanceOfPhenomenon));
                w.WriteValue(ChanceOfPhenomenon);
                w.WritePropertyName(nameof(Encounters));
                w.WriteStartArray();
                foreach (Encounter e in Encounters)
                {
                    e.Write(w);
                }
                w.WriteEndArray();
                w.WriteEndObject();
            }
        }
    }

    internal sealed class EncounterGroups
    {
        public sealed class EncounterGroup
        {
            public EncounterType Type;
            public EncounterTable Table;

            public EncounterGroup(EncounterType t, EncounterTable tbl)
            {
                Type = t;
                Table = tbl;
            }
            public EncounterGroup(JToken j)
            {
                Type = j[nameof(Type)].EnumValue<EncounterType>();
                Table = EncounterTable.LoadOrGet(j[nameof(Table)].Value<string>());
            }

            public void Write(JsonTextWriter w)
            {
                w.WriteStartObject();
                w.WritePropertyName(nameof(Type));
                w.WriteEnum(Type);
                w.WritePropertyName(nameof(Table));
                w.WriteValue(Table.Name);
                w.WriteEndObject();
            }
        }

        public List<EncounterGroup> Groups;

        public EncounterGroups()
        {
            Groups = new List<EncounterGroup>();
        }
        public EncounterGroups(JToken j)
        {
            var arr = (JArray)j;
            int count = arr.Count;
            Groups = new List<EncounterGroup>(count);
            for (int i = 0; i < count; i++)
            {
                Groups.Add(new EncounterGroup(arr[i]));
            }
        }

        public void Write(JsonTextWriter w)
        {
            w.WriteStartArray();
            foreach (EncounterGroup g in Groups)
            {
                g.Write(w);
            }
            w.WriteEndArray();
        }
    }
}
