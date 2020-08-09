using Kermalis.EndianBinaryIO;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.World;
using Newtonsoft.Json.Linq;
using Nuke.Common.IO;
using System;
using System.Collections.Generic;
using System.IO;

internal static class WorldBuilderHelper
{
    public static TEnum EnumValue<TEnum>(this JToken j) where TEnum : struct, Enum
    {
        Type type = typeof(TEnum);
        // If it has the [Flags] attribute, read a series of bools
        if (type.IsDefined(typeof(FlagsAttribute), false))
        {
            ulong value = 0;
            foreach (TEnum flag in Enum.GetValues(type))
            {
                ulong ulFlag = Convert.ToUInt64(flag);
                if (ulFlag != 0uL && j[flag.ToString()].Value<bool>())
                {
                    value |= ulFlag;
                }
            }
            return (TEnum)Enum.ToObject(typeof(TEnum), value);
        }
        else
        {
            return Enum.Parse<TEnum>(j.Value<string>());
        }
    }
}

public sealed partial class Build
{
    private sealed class EncounterTable
    {
        private sealed class Encounter
        {
            private readonly byte Chance;
            private readonly byte MinLevel;
            private readonly byte MaxLevel;
            private readonly PBESpecies Species;
            private readonly PBEForm Form;

            public Encounter(JToken j)
            {
                Chance = j[nameof(Chance)].Value<byte>();
                MinLevel = j[nameof(MinLevel)].Value<byte>();
                MaxLevel = j[nameof(MaxLevel)].Value<byte>();
                Species = j[nameof(Species)].EnumValue<PBESpecies>();
                Form = (PBEForm)j[nameof(Form)].Value<byte>(); // Do not use "EnumValue" because strings are bad for forms
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

        private readonly string _name;

        private readonly byte ChanceOfPhenomenon;
        private readonly Encounter[] Encounters;

        public EncounterTable(string name)
        {
            var json = JObject.Parse(File.ReadAllText(EncounterTablePath / (name + ".json")));
            ChanceOfPhenomenon = json[nameof(ChanceOfPhenomenon)].Value<byte>();
            var encs = (JArray)json[nameof(Encounters)];
            int numEncounters = encs.Count;
            Encounters = new Encounter[numEncounters];
            for (int i = 0; i < numEncounters; i++)
            {
                Encounters[i] = new Encounter(encs[i]);
            }

            _name = name;
        }

        public const string EncounterTableExtension = ".pgeenctbl";
        public static readonly AbsolutePath EncounterTablePath = AssetPath / "Encounter";
        public static IdList Ids { get; } = new IdList(EncounterTablePath / "EncounterTableIds.txt");

        public void Save()
        {
            using (var w = new EndianBinaryWriter(File.Create(EncounterTablePath / (_name + EncounterTableExtension))))
            {
                w.Write(ChanceOfPhenomenon);
                byte count = (byte)Encounters.Length;
                w.Write(count);
                for (int i = 0; i < count; i++)
                {
                    Encounters[i].Write(w);
                }
            }
        }
    }
    private sealed class Layout
    {
        public static readonly AbsolutePath LayoutPath = AssetPath / "Layout";
        public static IdList Ids { get; } = new IdList(LayoutPath / "LayoutIds.txt");
    }
    private sealed class EncounterGroups
    {
        private sealed class EncounterGroup
        {
            private readonly EncounterType Type;
            private readonly string Table;

            public EncounterGroup(JToken j)
            {
                Type = j[nameof(Type)].EnumValue<EncounterType>();
                Table = j[nameof(Table)].Value<string>();
            }

            public void Write(EndianBinaryWriter w)
            {
                w.Write(Type);
                w.Write(EncounterTable.Ids[Table]);
            }
        }

        private readonly EncounterGroup[] Groups;

        public EncounterGroups(JToken j)
        {
            var arr = (JArray)j;
            int count = arr.Count;
            Groups = new EncounterGroup[count];
            for (int i = 0; i < count; i++)
            {
                Groups[i] = new EncounterGroup(arr[i]);
            }
        }

        public void Write(EndianBinaryWriter w)
        {
            byte count = (byte)Groups.Length;
            w.Write(count);
            for (int i = 0; i < count; i++)
            {
                Groups[i].Write(w);
            }
        }
    }
    private sealed class Connection
    {
        private enum Dir : byte
        {
            South,
            North,
            West,
            East
        }
        private readonly Dir Direction;
        private readonly string Map;
        private readonly int Offset;

        public Connection(JToken j)
        {
            Direction = j[nameof(Direction)].EnumValue<Dir>();
            Map = j[nameof(Map)].Value<string>();
            Offset = j[nameof(Offset)].Value<int>();
        }

        public void Write(EndianBinaryWriter w)
        {
            w.Write(Direction);
            w.Write(Build.Map.Ids[Map]);
            w.Write(Offset);
        }
    }
    private sealed class Details
    {
        private readonly MapFlags Flags;
        private readonly MapSection Section;
        private readonly MapWeather Weather;
        private readonly Song Music;

        public Details(JToken j)
        {
            Flags = j[nameof(Flags)].EnumValue<MapFlags>();
            Section = j[nameof(Section)].EnumValue<MapSection>();
            Weather = j[nameof(Weather)].EnumValue<MapWeather>();
            Music = j[nameof(Music)].EnumValue<Song>();
        }

        public void Write(EndianBinaryWriter w)
        {
            w.Write(Flags);
            w.Write(Section);
            w.Write(Weather);
            w.Write(Music);
        }
    }
    private sealed class Events
    {
        private sealed class WarpEvent
        {
            private readonly int X;
            private readonly int Y;
            private readonly byte Elevation;

            private readonly string DestMap;
            private readonly int DestX;
            private readonly int DestY;
            private readonly byte DestElevation;

            public WarpEvent(JToken j)
            {
                X = j[nameof(X)].Value<int>();
                Y = j[nameof(Y)].Value<int>();
                Elevation = j[nameof(Elevation)].Value<byte>();

                DestMap = j[nameof(DestMap)].Value<string>();
                DestX = j[nameof(DestX)].Value<int>();
                DestY = j[nameof(DestY)].Value<int>();
                DestElevation = j[nameof(DestElevation)].Value<byte>();
            }

            public void Write(EndianBinaryWriter w)
            {
                w.Write(X);
                w.Write(Y);
                w.Write(Elevation);

                w.Write(Map.Ids[DestMap]);
                w.Write(DestX);
                w.Write(DestY);
                w.Write(DestElevation);
            }
        }
        private sealed class ObjEvent
        {
            private readonly int X;
            private readonly int Y;
            private readonly byte Elevation;

            private readonly ushort Id;
            private readonly string Sprite;
            private readonly ObjMovementType MovementType;
            private readonly int MovementX;
            private readonly int MovementY;
            private readonly TrainerType TrainerType;
            private readonly byte TrainerSight;
            private readonly string Script;
            private readonly Flag Flag;

            public ObjEvent(JToken j)
            {
                X = j[nameof(X)].Value<int>();
                Y = j[nameof(Y)].Value<int>();
                Elevation = j[nameof(Elevation)].Value<byte>();

                Id = j[nameof(Id)].Value<ushort>();
                Sprite = j[nameof(Sprite)].Value<string>();
                MovementType = j[nameof(MovementType)].EnumValue<ObjMovementType>();
                MovementX = j[nameof(MovementX)].Value<int>();
                MovementY = j[nameof(MovementY)].Value<int>();
                TrainerType = j[nameof(TrainerType)].EnumValue<TrainerType>();
                TrainerSight = j[nameof(TrainerSight)].Value<byte>();
                Script = j[nameof(Script)].Value<string>();
                Flag = j[nameof(Flag)].EnumValue<Flag>();
            }

            public void Write(EndianBinaryWriter w)
            {
                w.Write(X);
                w.Write(Y);
                w.Write(Elevation);

                w.Write(Id);
                w.Write(Sprite, true);
                w.Write(MovementType);
                w.Write(MovementX);
                w.Write(MovementY);
                w.Write(TrainerType);
                w.Write(TrainerSight);
                w.Write(Script, true);
                w.Write(Flag);
            }
        }

        private readonly WarpEvent[] Warps;
        private readonly ObjEvent[] Objs;

        public Events(JToken j)
        {
            var arr = (JArray)j[nameof(Warps)];
            int count = arr.Count;
            Warps = new WarpEvent[count];
            for (int i = 0; i < count; i++)
            {
                Warps[i] = new WarpEvent(arr[i]);
            }
            arr = (JArray)j[nameof(Objs)];
            count = arr.Count;
            Objs = new ObjEvent[count];
            for (int i = 0; i < count; i++)
            {
                Objs[i] = new ObjEvent(arr[i]);
            }
        }

        public void Write(EndianBinaryWriter w)
        {
            ushort count = (ushort)Warps.Length;
            w.Write(count);
            for (int i = 0; i < count; i++)
            {
                Warps[i].Write(w);
            }
            count = (ushort)Objs.Length;
            w.Write(count);
            for (int i = 0; i < count; i++)
            {
                Objs[i].Write(w);
            }
        }
    }
    private sealed class Map
    {
        private readonly string _name;

        private readonly string Layout;
        private readonly Details Details;
        private readonly Connection[] Connections;
        private readonly EncounterGroups Encounters;
        private readonly Events Events;

        public Map(string name)
        {
            var json = JObject.Parse(File.ReadAllText(MapPath / (name + ".json")));
            Layout = json[nameof(Layout)].Value<string>();
            Details = new Details(json[nameof(Details)]);
            var cons = (JArray)json[nameof(Connections)];
            int numConnections = cons.Count;
            Connections = new Connection[numConnections];
            for (int i = 0; i < numConnections; i++)
            {
                Connections[i] = new Connection(cons[i]);
            }
            Encounters = new EncounterGroups(json[nameof(Encounters)]);
            Events = new Events(json[nameof(Events)]);

            _name = name;
        }

        public const string MapExtension = ".pgemap";
        public static readonly AbsolutePath MapPath = AssetPath / "Map";
        public static IdList Ids { get; } = new IdList(MapPath / "MapIds.txt");

        public void Save()
        {
            using (var w = new EndianBinaryWriter(File.Create(MapPath / (_name + MapExtension))))
            {
                w.Write(Build.Layout.Ids[Layout]);
                Details.Write(w);
                byte numConnections = (byte)Connections.Length;
                w.Write(numConnections);
                for (int i = 0; i < numConnections; i++)
                {
                    Connections[i].Write(w);
                }
                Encounters.Write(w);
                Events.Write(w);
            }
        }
    }

    private sealed class SpriteSheet
    {
        private readonly string Sprites;
        private readonly int Width;
        private readonly int Height;

        public SpriteSheet(JToken j)
        {
            Sprites = j[nameof(Sprites)].Value<string>();
            Width = j[nameof(Width)].Value<int>();
            Height = j[nameof(Height)].Value<int>();
        }

        private static readonly AbsolutePath SheetsPath = AssetPath / "ObjSprites";
        public static readonly AbsolutePath SheetsInputPath = SheetsPath / "ObjSprites.json";
        public static readonly AbsolutePath SheetsOutputPath = SheetsPath / "ObjSprites.bin";

        public void Write(EndianBinaryWriter w)
        {
            w.Write(Sprites, true);
            w.Write(Width);
            w.Write(Height);
        }
    }

    private void CleanWorld()
    {
        // Clean all assets even if they're no longer in the id lists

        // Encounter tables
        foreach (AbsolutePath file in EncounterTable.EncounterTablePath.GlobFiles("*" + EncounterTable.EncounterTableExtension))
        {
            File.Delete(file);
        }
        // Obj sprites
        string p = SpriteSheet.SheetsOutputPath;
        if (File.Exists(p))
        {
            File.Delete(p);
        }
        // Maps
        foreach (AbsolutePath file in Map.MapPath.GlobFiles("*" + Map.MapExtension))
        {
            File.Delete(file);
        }
    }
    private void BuildWorld()
    {
        // Encounter tables
        foreach (string name in EncounterTable.Ids)
        {
            new EncounterTable(name).Save();
        }
        // Obj sprites
        using (var ms = new MemoryStream())
        using (var w = new EndianBinaryWriter(ms, encoding: EncodingType.UTF16))
        {
            var json = JObject.Parse(File.ReadAllText(SpriteSheet.SheetsInputPath));
            var labels = new Dictionary<string, long>(json.Count);
            foreach (KeyValuePair<string, JToken> kvp in json)
            {
                long ofs = ms.Position;
                new SpriteSheet(kvp.Value).Write(w);
                labels.Add(kvp.Key, ofs);
            }
            using (var fw = new EndianBinaryWriter(File.Create(SpriteSheet.SheetsOutputPath), encoding: EncodingType.UTF16))
            {
                fw.Write(labels.Count);
                // Compute start offset of sheet data
                uint dataStart = sizeof(int); // Total count needs to be accounted for in offset calc
                foreach (string label in labels.Keys)
                {
                    dataStart += (uint)((label.Length * 2) + 2 + sizeof(uint)); // 2 bytes per char, 2 bytes for nullTermination, sizeof for the pointer
                }
                // Write label table
                foreach (KeyValuePair<string, long> kvp in labels)
                {
                    fw.Write(kvp.Key, true);
                    fw.Write((uint)(kvp.Value + dataStart));
                }
                ms.Position = 0;
                ms.CopyTo(fw.BaseStream);
            }
        }
        // Maps
        foreach (string name in Map.Ids)
        {
            new Map(name).Save();
        }
    }
}

