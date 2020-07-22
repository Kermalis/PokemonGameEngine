using Kermalis.EndianBinaryIO;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Overworld;
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
            public Encounter(EndianBinaryReader r)
            {
                Chance = r.ReadByte();
                MinLevel = r.ReadByte();
                MaxLevel = r.ReadByte();
                Species = r.ReadEnum<PBESpecies>();
                Form = r.ReadEnum<PBEForm>();
            }

            public void Write(EndianBinaryWriter w)
            {
                w.Write(Chance);
                w.Write(MinLevel);
                w.Write(MaxLevel);
                w.Write(Species);
                w.Write(Form);
            }
        }

        internal readonly string Name;
        internal readonly int Id;
        internal byte ChanceOfPhenomenon;
        internal readonly List<Encounter> Encounters;

        private EncounterTable(string name, int id)
        {
            using (var r = new EndianBinaryReader(File.OpenRead(Path.Combine(_encounterTablePath, name + _encounterTableExtension))))
            {
                ChanceOfPhenomenon = r.ReadByte();
                byte count = r.ReadByte();
                Encounters = new List<Encounter>(count);
                for (int i = 0; i < count; i++)
                {
                    Encounters.Add(new Encounter(r));
                }
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

        private const string _encounterTableExtension = ".pgeenctbl";
        private static readonly string _encounterTablePath = Path.Combine(Program.AssetPath, "Encounter");
        public static IdList Ids { get; } = new IdList(Path.Combine(_encounterTablePath, "EncounterTableIds.txt"));
        private static readonly Dictionary<int, WeakReference<EncounterTable>> _loadedEncounterTables = new Dictionary<int, WeakReference<EncounterTable>>();
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
            EncounterTable w;
            if (!_loadedEncounterTables.ContainsKey(id))
            {
                w = new EncounterTable(name, id);
                _loadedEncounterTables.Add(id, new WeakReference<EncounterTable>(w));
                return w;
            }
            if (_loadedEncounterTables[id].TryGetTarget(out w))
            {
                return w;
            }
            w = new EncounterTable(name, id);
            _loadedEncounterTables[id].SetTarget(w);
            return w;
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
            using (var w = new EndianBinaryWriter(File.Create(Path.Combine(_encounterTablePath, Name + _encounterTableExtension))))
            {
                w.Write(ChanceOfPhenomenon);
                byte count = (byte)Encounters.Count;
                w.Write(count);
                for (int i = 0; i < count; i++)
                {
                    Encounters[i].Write(w);
                }
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
            public EncounterGroup(EndianBinaryReader r)
            {
                Type = r.ReadEnum<EncounterType>();
                Table = EncounterTable.LoadOrGet(r.ReadInt32());
            }

            public void Write(EndianBinaryWriter w)
            {
                w.Write(Type);
                w.Write(Table.Id);
            }
        }

        public List<EncounterGroup> Groups;

        public EncounterGroups()
        {
            Groups = new List<EncounterGroup>();
        }
        public EncounterGroups(EndianBinaryReader r)
        {
            byte count = r.ReadByte();
            Groups = new List<EncounterGroup>(count);
            for (int i = 0; i < count; i++)
            {
                Groups.Add(new EncounterGroup(r));
            }
        }

        public void Write(EndianBinaryWriter w)
        {
            byte count = (byte)Groups.Count;
            w.Write(count);
            for (int i = 0; i < count; i++)
            {
                Groups[i].Write(w);
            }
        }
    }
}
