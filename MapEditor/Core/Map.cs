﻿using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Kermalis.EndianBinaryIO;
using Kermalis.MapEditor.Util;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Scripts;
using Kermalis.PokemonGameEngine.World;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;

namespace Kermalis.MapEditor.Core
{
    public sealed class Map
    {
        public sealed class Connection
        {
            public enum Dir : byte
            {
                South,
                North,
                West,
                East
            }
            internal Dir Direction;
            internal string Map;
            internal int Offset;

            internal Connection() { }
            internal Connection(JToken j)
            {
                Direction = j[nameof(Direction)].ReadEnumValue<Dir>();
                Map = j[nameof(Map)].Value<string>();
                Offset = j[nameof(Offset)].Value<int>();
            }

            internal void Write(JsonTextWriter w)
            {
                w.WriteStartObject();
                w.WritePropertyName(nameof(Direction));
                w.WriteEnum(Direction);
                w.WritePropertyName(nameof(Map));
                w.WriteValue(Map);
                w.WritePropertyName(nameof(Offset));
                w.WriteValue(Offset);
                w.WriteEndObject();
            }
        }
        internal sealed class Details
        {
            public MapFlags Flags;
            public MapSection Section;
            public MapWeather Weather;
            public Song Music;
            public PBEForm BurmyForm;

            public Details() { }
            public Details(JToken j)
            {
                Flags = j[nameof(Flags)].ReadFlagsEnumValue<MapFlags>();
                Section = j[nameof(Section)].ReadEnumValue<MapSection>();
                Weather = j[nameof(Weather)].ReadEnumValue<MapWeather>();
                Music = j[nameof(Music)].ReadEnumValue<Song>();
                BurmyForm = j[nameof(BurmyForm)].ReadEnumValue<PBEForm>();
            }

            public void Write(JsonTextWriter w)
            {
                w.WriteStartObject();
                w.WritePropertyName(nameof(Flags));
                w.WriteFlagsEnum(Flags);
                w.WritePropertyName(nameof(Section));
                w.WriteEnum(Section);
                w.WritePropertyName(nameof(Weather));
                w.WriteEnum(Weather);
                w.WritePropertyName(nameof(Music));
                w.WriteEnum(Music);
                w.WritePropertyName(nameof(BurmyForm));
                w.WriteValue(PBEDataUtils.GetNameOfForm(PBESpecies.Burmy, BurmyForm));
                w.WriteEndObject();
            }
        }
        internal sealed class Events
        {
            public sealed class WarpEvent
            {
                public int X;
                public int Y;
                public byte Elevation;

                public string DestMap;
                public int DestX;
                public int DestY;
                public byte DestElevation;

                public WarpEvent() { }
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

                public void Write(JsonTextWriter w)
                {
                    w.WriteStartObject();
                    w.WritePropertyName(nameof(X));
                    w.WriteValue(X);
                    w.WritePropertyName(nameof(Y));
                    w.WriteValue(Y);
                    w.WritePropertyName(nameof(Elevation));
                    w.WriteValue(Elevation);

                    w.WritePropertyName(nameof(DestMap));
                    w.WriteValue(DestMap);
                    w.WritePropertyName(nameof(DestX));
                    w.WriteValue(DestX);
                    w.WritePropertyName(nameof(DestY));
                    w.WriteValue(DestY);
                    w.WritePropertyName(nameof(DestElevation));
                    w.WriteValue(DestElevation);
                    w.WriteEndObject();
                }
            }
            public sealed class ObjEvent
            {
                public int X;
                public int Y;
                public byte Elevation;

                public ushort Id;
                public string Sprite;
                public ObjMovementType MovementType;
                public int MovementX;
                public int MovementY;
                public TrainerType TrainerType;
                public byte TrainerSight;
                public string Script;
                public Flag Flag;

                public ObjEvent()
                {
                    Script = string.Empty;
                    Flag = Flag.MAX;
                }
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

                public void Write(JsonTextWriter w)
                {
                    w.WriteStartObject();
                    w.WritePropertyName(nameof(X));
                    w.WriteValue(X);
                    w.WritePropertyName(nameof(Y));
                    w.WriteValue(Y);
                    w.WritePropertyName(nameof(Elevation));
                    w.WriteValue(Elevation);

                    w.WritePropertyName(nameof(Id));
                    w.WriteValue(Id);
                    w.WritePropertyName(nameof(Sprite));
                    w.WriteValue(Sprite);
                    w.WritePropertyName(nameof(MovementType));
                    w.WriteEnum(MovementType);
                    w.WritePropertyName(nameof(MovementX));
                    w.WriteValue(MovementX);
                    w.WritePropertyName(nameof(MovementY));
                    w.WriteValue(MovementY);
                    w.WritePropertyName(nameof(TrainerType));
                    w.WriteEnum(TrainerType);
                    w.WritePropertyName(nameof(TrainerSight));
                    w.WriteValue(TrainerSight);
                    w.WritePropertyName(nameof(Script));
                    w.WriteValue(Script);
                    w.WritePropertyName(nameof(Flag));
                    w.WriteEnum(Flag);
                    w.WriteEndObject();
                }
            }
            public sealed class ScriptEvent
            {
                public int X;
                public int Y;
                public byte Elevation;

                public Var Var;
                public short VarValue;
                public ScriptConditional VarConditional;
                public string Script;

                public ScriptEvent()
                {
                    Script = string.Empty;
                }
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

                public void Write(JsonTextWriter w)
                {
                    w.WriteStartObject();
                    w.WritePropertyName(nameof(X));
                    w.WriteValue(X);
                    w.WritePropertyName(nameof(Y));
                    w.WriteValue(Y);
                    w.WritePropertyName(nameof(Elevation));
                    w.WriteValue(Elevation);

                    w.WritePropertyName(nameof(Var));
                    w.WriteEnum(Var);
                    w.WritePropertyName(nameof(VarValue));
                    w.WriteValue(VarValue);
                    w.WritePropertyName(nameof(VarConditional));
                    w.WriteEnum(VarConditional);
                    w.WritePropertyName(nameof(Script));
                    w.WriteValue(Script);
                    w.WriteEndObject();
                }
            }

            public readonly List<WarpEvent> Warps;
            public readonly List<ObjEvent> Objs;
            public readonly List<ScriptEvent> ScriptTiles;

            public Events()
            {
                Warps = new List<WarpEvent>();
                Objs = new List<ObjEvent>();
                ScriptTiles = new List<ScriptEvent>();
            }
            public Events(JToken j)
            {
                var arr = (JArray)j[nameof(Warps)];
                int count = arr.Count;
                Warps = new List<WarpEvent>(count);
                for (int i = 0; i < count; i++)
                {
                    Warps.Add(new WarpEvent(arr[i]));
                }
                arr = (JArray)j[nameof(Objs)];
                count = arr.Count;
                Objs = new List<ObjEvent>(count);
                for (int i = 0; i < count; i++)
                {
                    Objs.Add(new ObjEvent(arr[i]));
                }
                arr = (JArray)j[nameof(ScriptTiles)];
                count = arr.Count;
                ScriptTiles = new List<ScriptEvent>(count);
                for (int i = 0; i < count; i++)
                {
                    ScriptTiles.Add(new ScriptEvent(arr[i]));
                }
            }

            public void Write(JsonTextWriter w)
            {
                w.WriteStartObject();
                w.WritePropertyName(nameof(Warps));
                w.WriteStartArray();
                foreach (WarpEvent e in Warps)
                {
                    e.Write(w);
                }
                w.WriteEndArray();
                w.WritePropertyName(nameof(Objs));
                w.WriteStartArray();
                foreach (ObjEvent e in Objs)
                {
                    e.Write(w);
                }
                w.WriteEndArray();
                w.WritePropertyName(nameof(ScriptTiles));
                w.WriteStartArray();
                foreach (ScriptEvent e in ScriptTiles)
                {
                    e.Write(w);
                }
                w.WriteEndArray();
                w.WriteEndObject();
            }
        }
        internal sealed class Layout : IDisposable
        {
            public sealed class Block
            {
                public readonly int X;
                public readonly int Y;

                public byte Elevations;
                public LayoutBlockPassage Passage;
                public Blockset.Block BlocksetBlock;

                public Block(int x, int y, EndianBinaryReader r)
                {
                    X = x;
                    Y = y;
                    Elevations = r.ReadByte();
                    Passage = r.ReadEnum<LayoutBlockPassage>();
                    BlocksetBlock = Blockset.LoadOrGet(r.ReadInt32()).Blocks[r.ReadInt32()];
                }
                public Block(int x, int y, Blockset.Block defaultBlock)
                {
                    X = x;
                    Y = y;
                    Elevations = 1 << 0; // Include elevation 0
                    BlocksetBlock = defaultBlock;
                }

                public void Write(EndianBinaryWriter w)
                {
                    w.Write(Elevations);
                    w.Write(Passage);
                    w.Write(BlocksetBlock.Parent.Id);
                    w.Write(BlocksetBlock.Id);
                }
            }

            public readonly string Name;
            public readonly int Id;

            public WriteableBitmap BlocksBitmap;
            public WriteableBitmap BorderBlocksBitmap;
            public delegate void LayoutDrewBitmapEventHandler(Layout layout, bool drewBorderBlocks, bool wasResized);
            public event LayoutDrewBitmapEventHandler OnDrew;

            public int Width;
            public int Height;
            public Block[][] Blocks;
            public byte BorderWidth;
            public byte BorderHeight;
            public Block[][] BorderBlocks;

            private Layout(string name, int id)
            {
                using (var r = new EndianBinaryReader(File.OpenRead(Path.Combine(LayoutPath, name + LayoutExtension))))
                {
                    Block[][] Create(int w, int h)
                    {
                        var arr = new Block[h][];
                        for (int y = 0; y < h; y++)
                        {
                            var arrY = new Block[w];
                            for (int x = 0; x < w; x++)
                            {
                                arrY[x] = new Block(x, y, r);
                            }
                            arr[y] = arrY;
                        }
                        return arr;
                    }
                    Blocks = Create(Width = r.ReadInt32(), Height = r.ReadInt32());
                    BorderBlocks = Create(BorderWidth = r.ReadByte(), BorderHeight = r.ReadByte());
                }
                Name = name;
                Id = id;
                UpdateBitmapSize(false);
                UpdateBitmapSize(true);
            }
            public Layout(string name, int width, int height, byte borderWidth, byte borderHeight, Blockset.Block defaultBlock)
            {
                Id = Ids.Add(name);
                _loadedLayouts.Add(Id, new WeakReference<Layout>(this));
                Block[][] Create(int w, int h)
                {
                    var arr = new Block[h][];
                    for (int y = 0; y < h; y++)
                    {
                        var arrY = new Block[w];
                        for (int x = 0; x < w; x++)
                        {
                            arrY[x] = new Block(x, y, defaultBlock);
                        }
                        arr[y] = arrY;
                    }
                    return arr;
                }
                Blocks = Create(Width = width, Height = height);
                BorderBlocks = Create(BorderWidth = borderWidth, BorderHeight = borderHeight);
                Name = name;
                Save();
                Ids.Save();
                UpdateBitmapSize(false);
                UpdateBitmapSize(true);
            }
            ~Layout()
            {
                DisposeBitmaps();
            }

            private const string LayoutExtension = ".pgelayout";
            private static readonly string LayoutPath = Path.Combine(Program.AssetPath, "Layout");
            public static IdList Ids { get; } = new IdList(Path.Combine(LayoutPath, "LayoutIds.txt"));
            private static readonly Dictionary<int, WeakReference<Layout>> _loadedLayouts = new();
            public static Layout LoadOrGet(string name)
            {
                int id = Ids[name];
                if (id == -1)
                {
                    throw new ArgumentOutOfRangeException(nameof(name));
                }
                return LoadOrGet(name, id);
            }
            public static Layout LoadOrGet(int id)
            {
                string name = Ids[id];
                if (name is null)
                {
                    throw new ArgumentOutOfRangeException(nameof(id));
                }
                return LoadOrGet(name, id);
            }
            private static Layout LoadOrGet(string name, int id)
            {
                Layout l;
                if (!_loadedLayouts.TryGetValue(id, out WeakReference<Layout> w))
                {
                    l = new Layout(name, id);
                    _loadedLayouts.Add(id, new WeakReference<Layout>(l));
                }
                else if (!w.TryGetTarget(out l))
                {
                    l = new Layout(name, id);
                    w.SetTarget(l);
                }
                return l;
            }

            public void Paste(bool borderBlocks, Blockset.Block[][] blocks, int destX, int destY)
            {
                Block[][] outArr = borderBlocks ? BorderBlocks : Blocks;
                int width = borderBlocks ? BorderWidth : Width;
                int height = borderBlocks ? BorderHeight : Height;
                List<Block> list = DrawList;
                for (int y = 0; y < blocks.Length; y++)
                {
                    int dy = y + destY;
                    if (dy >= 0 && dy < height)
                    {
                        Blockset.Block[] inArrY = blocks[y];
                        Block[] outArrY = outArr[dy];
                        for (int x = 0; x < inArrY.Length; x++)
                        {
                            int dx = x + destX;
                            if (dx >= 0 && dx < width)
                            {
                                Blockset.Block b = inArrY[x];
                                if (b is not null)
                                {
                                    Block outB = outArrY[dx];
                                    if (outB.BlocksetBlock != b)
                                    {
                                        outB.BlocksetBlock = b;
                                        list.Add(outB);
                                    }
                                }
                            }
                        }
                    }
                }
                Draw(borderBlocks);
            }
            public void Fill(bool borderBlocks, Blockset.Block oldBlock, Blockset.Block newBlock, int destX, int destY)
            {
                Block[][] outArr = borderBlocks ? BorderBlocks : Blocks;
                int width = borderBlocks ? BorderWidth : Width;
                int height = borderBlocks ? BorderHeight : Height;
                void Fill(int x, int y)
                {
                    if (x >= 0 && x < width && y >= 0 && y < height)
                    {
                        Block b = outArr[y][x];
                        if (b.BlocksetBlock == oldBlock)
                        {
                            b.BlocksetBlock = newBlock;
                            DrawList.Add(b);
                            Fill(x, y + 1);
                            Fill(x, y - 1);
                            Fill(x + 1, y);
                            Fill(x - 1, y);
                        }
                    }
                }
                Fill(destX, destY);
                Draw(borderBlocks);
            }

            private void UpdateBitmapSize(bool borderBlocks)
            {
                WriteableBitmap bmp = borderBlocks ? BorderBlocksBitmap : BlocksBitmap;
                int bmpWidth = (borderBlocks ? BorderWidth : Width) * Overworld.Block_NumPixelsX;
                int bmpHeight = (borderBlocks ? BorderHeight : Height) * Overworld.Block_NumPixelsY;
                bool createNew;
                if (bmp is null)
                {
                    createNew = true;
                }
                else
                {
                    PixelSize ps = bmp.PixelSize;
                    createNew = ps.Width != bmpWidth || ps.Height != bmpHeight;
                }
                if (createNew)
                {
                    bmp?.Dispose();
                    bmp = new WriteableBitmap(new PixelSize(bmpWidth, bmpHeight), new Vector(96, 96), PixelFormat.Rgba8888, AlphaFormat.Premul);
                    if (borderBlocks)
                    {
                        BorderBlocksBitmap = bmp;
                    }
                    else
                    {
                        BlocksBitmap = bmp;
                    }
                    DrawAll(borderBlocks, true);
                }
            }
            public static readonly List<Block> DrawList = new(); // Save allocations
            public unsafe void Draw(bool borderBlocks)
            {
                List<Block> list = DrawList;
                int count = list.Count;
                if (count > 0)
                {
                    WriteableBitmap bmp = borderBlocks ? BorderBlocksBitmap : BlocksBitmap;
                    using (ILockedFramebuffer l = bmp.Lock())
                    {
                        uint* dst = (uint*)l.Address.ToPointer();
                        int dstW = (borderBlocks ? BorderWidth : Width) * Overworld.Block_NumPixelsX;
                        int dstH = (borderBlocks ? BorderHeight : Height) * Overworld.Block_NumPixelsY;
                        for (int i = 0; i < count; i++)
                        {
                            Block b = list[i];
                            int x = b.X * Overworld.Block_NumPixelsX;
                            int y = b.Y * Overworld.Block_NumPixelsY;
                            Renderer.FillRectangle(dst, dstW, dstH, x, y, Overworld.Block_NumPixelsX, Overworld.Block_NumPixelsY, Renderer.Color(0, 0, 0, 255));
                            b.BlocksetBlock.Draw(dst, dstW, dstH, x, y);
                        }
                    }
                    list.Clear();
                    OnDrew?.Invoke(this, borderBlocks, false);
                }
            }
            public unsafe void DrawAll(bool borderBlocks, bool wasResized)
            {
                WriteableBitmap bmp = borderBlocks ? BorderBlocksBitmap : BlocksBitmap;
                using (ILockedFramebuffer l = bmp.Lock())
                {
                    uint* dst = (uint*)l.Address.ToPointer();
                    int width = borderBlocks ? BorderWidth : Width;
                    int height = borderBlocks ? BorderHeight : Height;
                    int dstW = width * Overworld.Block_NumPixelsX;
                    int dstH = height * Overworld.Block_NumPixelsY;
                    Renderer.FillRectangle(dst, dstW, dstH, 0, 0, dstW, dstH, Renderer.Color(0, 0, 0, 255));
                    Block[][] arr = borderBlocks ? BorderBlocks : Blocks;
                    for (int y = 0; y < height; y++)
                    {
                        Block[] arrY = arr[y];
                        for (int x = 0; x < width; x++)
                        {
                            arrY[x].BlocksetBlock.Draw(dst, dstW, dstH, x * Overworld.Block_NumPixelsX, y * Overworld.Block_NumPixelsY);
                        }
                    }
                }
                OnDrew?.Invoke(this, borderBlocks, wasResized);
            }

            public void Save()
            {
                using (var w = new EndianBinaryWriter(File.Create(Path.Combine(LayoutPath, Name + LayoutExtension))))
                {
                    w.Write(Width);
                    w.Write(Height);
                    for (int y = 0; y < Height; y++)
                    {
                        Block[] bY = Blocks[y];
                        for (int x = 0; x < Width; x++)
                        {
                            bY[x].Write(w);
                        }
                    }
                    w.Write(BorderWidth);
                    w.Write(BorderHeight);
                    for (int y = 0; y < BorderHeight; y++)
                    {
                        Block[] bY = BorderBlocks[y];
                        for (int x = 0; x < BorderWidth; x++)
                        {
                            bY[x].Write(w);
                        }
                    }
                }
            }

            public void Dispose()
            {
                DisposeBitmaps();
                GC.SuppressFinalize(this);
            }
            private void DisposeBitmaps()
            {
                BlocksBitmap.Dispose();
                BorderBlocksBitmap.Dispose();
            }
        }

        internal readonly string Name;
        internal readonly int Id;

        internal readonly Layout MapLayout;
        internal readonly Details MapDetails;
        internal readonly List<Connection> Connections;
        internal readonly EncounterGroups Encounters;
        internal readonly Events MapEvents;

        private Map(string name, int id)
        {
            var json = JObject.Parse(File.ReadAllText(Path.Combine(MapPath, name + ".json")));
            MapLayout = Layout.LoadOrGet(json[nameof(Layout)].Value<string>());
            MapDetails = new Details(json[nameof(Details)]);
            var cons = (JArray)json[nameof(Connections)];
            int numConnections = cons.Count;
            Connections = new List<Connection>(numConnections);
            for (int i = 0; i < numConnections; i++)
            {
                Connections.Add(new Connection(cons[i]));
            }
            Encounters = new EncounterGroups(json[nameof(Encounters)]);
            MapEvents = new Events(json[nameof(Events)]);
            Name = name;
            Id = id;
        }
        internal Map(string name, Layout layout)
        {
            Id = Ids.Add(name);
            _loadedMaps.Add(Id, new WeakReference<Map>(this));
            MapLayout = layout;
            MapDetails = new Details();
            Connections = new List<Connection>();
            Encounters = new EncounterGroups();
            MapEvents = new Events();
            Name = name;
            Save();
            Ids.Save();
        }

        private static readonly string MapPath = Path.Combine(Program.AssetPath, "Map");
        public static IdList Ids { get; } = new IdList(Path.Combine(MapPath, "MapIds.txt"));
        private static readonly Dictionary<int, WeakReference<Map>> _loadedMaps = new();
        internal static Map LoadOrGet(string name)
        {
            int id = Ids[name];
            if (id == -1)
            {
                throw new ArgumentOutOfRangeException(nameof(name));
            }
            return LoadOrGet(name, id);
        }
        internal static Map LoadOrGet(int id)
        {
            string name = Ids[id];
            if (name is null)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }
            return LoadOrGet(name, id);
        }
        private static Map LoadOrGet(string name, int id)
        {
            Map m;
            if (!_loadedMaps.TryGetValue(id, out WeakReference<Map> w))
            {
                m = new Map(name, id);
                _loadedMaps.Add(id, new WeakReference<Map>(m));
            }
            else if (!w.TryGetTarget(out m))
            {
                m = new Map(name, id);
                w.SetTarget(m);
            }
            return m;
        }

        internal void Save()
        {
            using (var w = new JsonTextWriter(File.CreateText(Path.Combine(MapPath, Name + ".json"))) { Formatting = Formatting.Indented })
            {
                w.WriteStartObject();
                w.WritePropertyName(nameof(Layout));
                w.WriteValue(MapLayout.Name);
                w.WritePropertyName(nameof(Details));
                MapDetails.Write(w);
                w.WritePropertyName(nameof(Connections));
                w.WriteStartArray();
                foreach (Connection c in Connections)
                {
                    c.Write(w);
                }
                w.WriteEndArray();
                w.WritePropertyName(nameof(Encounters));
                Encounters.Write(w);
                w.WritePropertyName(nameof(Events));
                MapEvents.Write(w);
                w.WriteEndObject();
            }
        }
    }
}
