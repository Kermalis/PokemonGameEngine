using Kermalis.EndianBinaryIO;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Core;
using System;
using System.Collections.Generic;
using System.IO;

namespace Kermalis.PokemonGameEngine.World.Data
{
    internal sealed class EncounterTable
    {
        public sealed class Encounter
        {
            public byte Chance;
            public byte MinLevel;
            public byte MaxLevel;
            public PBESpecies Species;
            public PBEForm Form;

            public Encounter(EndianBinaryReader r)
            {
                Chance = r.ReadByte();
                MinLevel = r.ReadByte();
                MaxLevel = r.ReadByte();
                Species = r.ReadEnum<PBESpecies>();
                Form = r.ReadEnum<PBEForm>();
            }
        }

        private const string ENCOUNTER_TABLE_PATH = @"Encounter\";

        private static readonly IdList _ids = new(ENCOUNTER_TABLE_PATH + "EncounterTableIds.txt");
        private static readonly Dictionary<int, WeakReference<EncounterTable>> _loadedEncounterTables = new();

        public byte ChanceOfPhenomenon;
        public readonly Encounter[] Encounters;

        private EncounterTable(string name)
        {
            using (var r = new EndianBinaryReader(File.OpenRead(AssetLoader.GetPath(ENCOUNTER_TABLE_PATH + name + ".bin"))))
            {
                ChanceOfPhenomenon = r.ReadByte();
                byte count = r.ReadByte();
                Encounters = new Encounter[count];
                for (int i = 0; i < count; i++)
                {
                    Encounters[i] = new Encounter(r);
                }
            }
        }

        public ushort GetCombinedChance()
        {
            ushort sum = 0;
            for (int i = 0; i < Encounters.Length; i++)
            {
                sum += Encounters[i].Chance;
            }
            return sum;
        }

        public static EncounterTable LoadOrGet(int id)
        {
            string name = _ids[id];
            if (name is null)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }
            EncounterTable e;
            if (!_loadedEncounterTables.TryGetValue(id, out WeakReference<EncounterTable> w))
            {
                e = new EncounterTable(name);
                _loadedEncounterTables.Add(id, new WeakReference<EncounterTable>(e));
            }
            else if (!w.TryGetTarget(out e))
            {
                e = new EncounterTable(name);
                w.SetTarget(e);
            }
            return e;
        }
    }

    internal sealed class EncounterGroups
    {
        public sealed class EncounterGroup
        {
            public EncounterType Type;
            public EncounterTable Table;

            public EncounterGroup(EndianBinaryReader r)
            {
                Type = r.ReadEnum<EncounterType>();
                Table = EncounterTable.LoadOrGet(r.ReadInt32());
            }
        }

        public readonly EncounterGroup[] Groups;

        public EncounterGroups(EndianBinaryReader r)
        {
            byte count = r.ReadByte();
            Groups = new EncounterGroup[count];
            for (int i = 0; i < count; i++)
            {
                Groups[i] = new EncounterGroup(r);
            }
        }

        public EncounterTable GetEncounterTable(EncounterType t)
        {
            foreach (EncounterGroup grp in Groups)
            {
                if (grp.Type == t)
                {
                    return grp.Table;
                }
            }
            return null;
        }
    }
}
