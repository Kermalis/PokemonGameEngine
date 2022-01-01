using Kermalis.EndianBinaryIO;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Scripts;
using Kermalis.PokemonGameEngine.World;
using Newtonsoft.Json.Linq;
using Nuke.Common.IO;
using System;
using System.Collections.Generic;
using System.IO;

internal static class WorldBuilderHelper
{
    public static TEnum ReadEnumValue<TEnum>(this JToken j) where TEnum : struct, Enum
    {
        return Enum.Parse<TEnum>(j.Value<string>());
    }
    public static TEnum ReadFlagsEnumValue<TEnum>(this JToken j) where TEnum : struct, Enum
    {
        ulong value = 0;
        foreach (TEnum flag in Enum.GetValues<TEnum>())
        {
            ulong ulFlag = Convert.ToUInt64(flag);
            if (ulFlag != 0uL && j[flag.ToString()].Value<bool>())
            {
                value |= ulFlag;
            }
        }
        return (TEnum)Enum.ToObject(typeof(TEnum), value);
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
                Species = j[nameof(Species)].ReadEnumValue<PBESpecies>();
                Form = j[nameof(Form)].FormValue();
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

        public static readonly AbsolutePath EncounterTablePath = AssetPath / "Encounter";
        public static IdList Ids { get; } = new IdList(EncounterTablePath / "EncounterTableIds.txt");

        public void Save()
        {
            using (var w = new EndianBinaryWriter(File.Create(EncounterTablePath / (_name + ".bin"))))
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
        public int Width;
        public int Height;

        public Layout(string name)
        {
            using (var r = new EndianBinaryReader(File.OpenRead(Path.Combine(LayoutPath, name + ".pgelayout"))))
            {
                Width = r.ReadInt32();
                Height = r.ReadInt32();
                // Don't need the rest. Only using this to embed within Map data
            }
        }

        public static readonly AbsolutePath LayoutPath = AssetPath / "Layout";
        public static IdList Ids { get; } = new IdList(LayoutPath / "LayoutIds.txt");
    }
    private sealed class Tileset
    {
        public static readonly AbsolutePath TilesetPath = AssetPath / "Tileset";
        public static IdList Ids { get; } = new IdList(TilesetPath / "TilesetIds.txt");
    }
    private sealed class EncounterGroups
    {
        private sealed class EncounterGroup
        {
            private readonly EncounterType Type;
            private readonly string Table;

            public EncounterGroup(JToken j)
            {
                Type = j[nameof(Type)].ReadEnumValue<EncounterType>();
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
            Direction = j[nameof(Direction)].ReadEnumValue<Dir>();
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
        private readonly PBEForm BurmyForm;

        public Details(JToken j)
        {
            Flags = j[nameof(Flags)].ReadFlagsEnumValue<MapFlags>();
            Section = j[nameof(Section)].ReadEnumValue<MapSection>();
            Weather = j[nameof(Weather)].ReadEnumValue<MapWeather>();
            Music = j[nameof(Music)].ReadEnumValue<Song>();
            BurmyForm = j[nameof(BurmyForm)].ReadEnumValue<PBEForm>();
        }

        public void Write(EndianBinaryWriter w)
        {
            w.Write(Flags);
            w.Write(Section);
            w.Write(Weather);
            w.Write(Music);
            w.Write(BurmyForm);
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
                MovementType = j[nameof(MovementType)].ReadEnumValue<ObjMovementType>();
                MovementX = j[nameof(MovementX)].Value<int>();
                MovementY = j[nameof(MovementY)].Value<int>();
                TrainerType = j[nameof(TrainerType)].ReadEnumValue<TrainerType>();
                TrainerSight = j[nameof(TrainerSight)].Value<byte>();
                Script = j[nameof(Script)].Value<string>();
                Flag = j[nameof(Flag)].ReadEnumValue<Flag>();
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
        private sealed class ScriptEvent
        {
            private readonly int X;
            private readonly int Y;
            private readonly byte Elevation;

            private readonly Var Var;
            private readonly short VarValue;
            private readonly ScriptConditional VarConditional;
            private readonly string Script;

            public ScriptEvent(JToken j)
            {
                X = j[nameof(X)].Value<int>();
                Y = j[nameof(Y)].Value<int>();
                Elevation = j[nameof(Elevation)].Value<byte>();

                Var = j[nameof(Var)].ReadEnumValue<Var>();
                VarValue = j[nameof(VarValue)].Value<short>();
                VarConditional = j[nameof(VarConditional)].ReadEnumValue<ScriptConditional>();
                Script = j[nameof(Script)].Value<string>();
            }

            public void Write(EndianBinaryWriter w)
            {
                w.Write(X);
                w.Write(Y);
                w.Write(Elevation);

                w.Write(Var);
                w.Write(VarValue);
                w.Write(VarConditional);
                w.Write(Script, true);
            }
        }

        private readonly WarpEvent[] Warps;
        private readonly ObjEvent[] Objs;
        private readonly ScriptEvent[] ScriptTiles;

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
            arr = (JArray)j[nameof(ScriptTiles)];
            count = arr.Count;
            ScriptTiles = new ScriptEvent[count];
            for (int i = 0; i < count; i++)
            {
                ScriptTiles[i] = new ScriptEvent(arr[i]);
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
            count = (ushort)ScriptTiles.Length;
            w.Write(count);
            for (int i = 0; i < count; i++)
            {
                ScriptTiles[i].Write(w);
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
            var cons = (JArray)json[nameof(Connections)];
            int numConnections = cons.Count;
            Connections = new Connection[numConnections];
            for (int i = 0; i < numConnections; i++)
            {
                Connections[i] = new Connection(cons[i]);
            }
            Layout = json[nameof(Layout)].Value<string>();
            Events = new Events(json[nameof(Events)]);
            Details = new Details(json[nameof(Details)]);
            Encounters = new EncounterGroups(json[nameof(Encounters)]);

            _name = name;
        }

        public static readonly AbsolutePath MapPath = AssetPath / "Map";
        public static IdList Ids { get; } = new IdList(MapPath / "MapIds.txt");

        public void Save()
        {
            const int HEADER_SIZE = sizeof(int) + sizeof(int);

            using (var fw = new EndianBinaryWriter(File.Create(MapPath / (_name + ".bin"))))
            using (var ms = new MemoryStream())
            using (var w = new EndianBinaryWriter(ms))
            {
                // Always loaded
                var l = new Layout(Layout);
                w.Write(l.Width); // Include width and height in map data so it can be used without loading the layout
                w.Write(l.Height);
                byte numConnections = (byte)Connections.Length;
                w.Write(numConnections);
                for (int i = 0; i < numConnections; i++)
                {
                    Connections[i].Write(w);
                }

                // Loaded when visible
                uint visibleMapStart = (uint)(w.BaseStream.Position + HEADER_SIZE);
                w.Write(Build.Layout.Ids[Layout]); // Write Layout ID
                Events.Write(w);

                // Loaded when the map is where the player is
                uint currentMapStart = (uint)(w.BaseStream.Position + HEADER_SIZE);
                Details.Write(w);
                Encounters.Write(w);

                // Create file header
                fw.Write(visibleMapStart);
                fw.Write(currentMapStart);
                ms.Position = 0;
                ms.CopyTo(fw.BaseStream);
            }
        }
    }

    private sealed class SpriteSheet
    {
        private readonly string Sprites;
        private readonly uint Width;
        private readonly uint Height;
        private readonly int ShadowX;
        private readonly int ShadowY;
        private readonly uint ShadowW;
        private readonly uint ShadowH;

        public SpriteSheet(JToken j)
        {
            Sprites = j[nameof(Sprites)].Value<string>();
            Width = j[nameof(Width)].Value<uint>();
            Height = j[nameof(Height)].Value<uint>();
            ShadowX = j[nameof(ShadowX)].Value<int>();
            ShadowY = j[nameof(ShadowY)].Value<int>();
            ShadowW = j[nameof(ShadowW)].Value<uint>();
            ShadowH = j[nameof(ShadowH)].Value<uint>();
        }

        private static readonly AbsolutePath SheetsPath = AssetPath / "ObjSprites";
        public static readonly AbsolutePath SheetsInputPath = SheetsPath / "ObjSprites.json";
        public static readonly AbsolutePath SheetsOutputPath = SheetsPath / "ObjSprites.bin";

        public void Write(EndianBinaryWriter w)
        {
            w.Write(Sprites, true);
            w.Write(Width);
            w.Write(Height);
            w.Write(ShadowX);
            w.Write(ShadowY);
            w.Write(ShadowW);
            w.Write(ShadowH);
        }
    }

    private sealed class TileAnimation
    {
        private sealed class Frame
        {
            private sealed class Stop
            {
                private readonly int AnimTile;
                private readonly float Time;

                public Stop(JToken j)
                {
                    AnimTile = j[nameof(AnimTile)].Value<int>();
                    Time = j[nameof(Time)].Value<float>();
                }

                public void Write(EndianBinaryWriter w)
                {
                    w.Write(AnimTile);
                    w.Write(Time);
                }
            }
            private readonly int TilesetTile;
            private readonly Stop[] Stops;

            public Frame(JToken j)
            {
                TilesetTile = j[nameof(TilesetTile)].Value<int>();
                var stops = (JArray)j[nameof(Stops)];
                int numStops = stops.Count;
                Stops = new Stop[numStops];
                for (int i = 0; i < numStops; i++)
                {
                    Stops[i] = new Stop(stops[i]);
                }
            }

            public void Write(EndianBinaryWriter w)
            {
                w.Write(TilesetTile);
                byte numStops = (byte)Stops.Length;
                w.Write(numStops);
                for (int i = 0; i < numStops; i++)
                {
                    Stops[i].Write(w);
                }
            }
        }
        public readonly string Tileset; // Not written
        private readonly float Duration;
        private readonly Frame[] Frames;

        public TileAnimation(JToken j)
        {
            Tileset = j[nameof(Tileset)].Value<string>();
            Duration = j[nameof(Duration)].Value<float>();
            var frames = (JArray)j[nameof(Frames)];
            int numFrames = frames.Count;
            Frames = new Frame[numFrames];
            for (int i = 0; i < numFrames; i++)
            {
                Frames[i] = new Frame(frames[i]);
            }
        }

        public static readonly AbsolutePath AnimationsPath = AssetPath / "Tileset" / "Animation";
        public static readonly AbsolutePath AnimationsOutputPath = AnimationsPath / "Animations.bin";

        public void Write(EndianBinaryWriter w)
        {
            w.Write(Duration);
            byte numFrames = (byte)Frames.Length;
            w.Write(numFrames);
            for (int i = 0; i < numFrames; i++)
            {
                Frames[i].Write(w);
            }
        }
    }

    private void CleanWorld()
    {
        // Clean all assets even if they're no longer in the id lists
        static void DeleteFiles(AbsolutePath path)
        {
            foreach (string file in path.GlobFiles("*.bin"))
            {
                File.Delete(file);
            }
        }
        static void DeleteFile(string file)
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }

        // Encounter tables
        DeleteFiles(EncounterTable.EncounterTablePath);
        // Obj sprites
        DeleteFile(SpriteSheet.SheetsOutputPath);
        // Maps
        DeleteFiles(Map.MapPath);
        // Tile animations
        DeleteFile(TileAnimation.AnimationsOutputPath);
    }
    private void BuildWorld()
    {
        #region Encounter tables
        foreach (string name in EncounterTable.Ids)
        {
            new EncounterTable(name).Save();
        }
        #endregion
        #region Obj sprites
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
        #endregion
        #region Maps
        foreach (string name in Map.Ids)
        {
            new Map(name).Save();
        }
        #endregion
        #region Tile animations
        using (var ms = new MemoryStream())
        using (var w = new EndianBinaryWriter(ms, encoding: EncodingType.UTF16))
        {
            var tilesetTracker = new Dictionary<int, List<long>>(Tileset.Ids.Count);
            foreach (string file in TileAnimation.AnimationsPath.GlobFiles("*.json"))
            {
                var json = JObject.Parse(File.ReadAllText(file));
                var anim = new TileAnimation(json);
                int tilesetId = Tileset.Ids[anim.Tileset];
                if (!tilesetTracker.TryGetValue(tilesetId, out List<long> list))
                {
                    list = new List<long>(1);
                    tilesetTracker.Add(tilesetId, list);
                }
                list.Add(ms.Position);
                anim.Write(w);
            }
            using (var fw = new EndianBinaryWriter(File.Create(TileAnimation.AnimationsOutputPath)))
            {
                fw.Write(tilesetTracker.Count);
                // Compute start offset of anim data
                uint dataStart = sizeof(int); // Total count needs to be accounted for in offset calc
                foreach (KeyValuePair<int, List<long>> kvp in tilesetTracker)
                {
                    List<long> list = kvp.Value;
                    dataStart += (uint)(sizeof(int) + sizeof(int) + (list.Count * sizeof(uint))); // sizeof for tilesetId, sizeof for anim count, num anims * sizeof anim pointer
                }
                // Write table
                foreach (KeyValuePair<int, List<long>> kvp in tilesetTracker)
                {
                    int tilesetId = kvp.Key;
                    List<long> list = kvp.Value;
                    fw.Write(tilesetId);
                    fw.Write(list.Count);
                    for (int i = 0; i < list.Count; i++)
                    {
                        fw.Write((uint)(list[i] + dataStart));
                    }
                }
                ms.Position = 0;
                ms.CopyTo(fw.BaseStream);
            }
        }
        #endregion
    }
}

