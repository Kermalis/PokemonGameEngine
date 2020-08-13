using Kermalis.EndianBinaryIO;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Util;
using Kermalis.PokemonGameEngine.World.Objs;
using System;
using System.Collections.Generic;
using System.IO;

namespace Kermalis.PokemonGameEngine.World
{
    internal sealed class Map
    {
        public sealed class Connection
        {
            public enum Direction : byte
            {
                South,
                North,
                West,
                East
            }
            public readonly Direction Dir;
            public readonly int MapId;
            public readonly int Offset;

            public Connection(EndianBinaryReader r)
            {
                Dir = r.ReadEnum<Direction>();
                MapId = r.ReadInt32();
                Offset = r.ReadInt32();
            }
        }
        internal sealed class Details
        {
            public readonly MapFlags Flags;
            public readonly MapSection Section;
            public readonly MapWeather Weather;
            public readonly Song Music;

            public Details(EndianBinaryReader r)
            {
                Flags = r.ReadEnum<MapFlags>();
                Section = r.ReadEnum<MapSection>();
                Weather = r.ReadEnum<MapWeather>();
                Music = r.ReadEnum<Song>();
            }
        }
        public sealed class Events
        {
            public sealed class WarpEvent : IWarp
            {
                public int X { get; }
                public int Y { get; }
                public byte Elevation { get; }

                public int DestMapId { get; }
                public int DestX { get; }
                public int DestY { get; }
                public byte DestElevation { get; }

                public WarpEvent(EndianBinaryReader r)
                {
                    X = r.ReadInt32();
                    Y = r.ReadInt32();
                    Elevation = r.ReadByte();

                    DestMapId = r.ReadInt32();
                    DestX = r.ReadInt32();
                    DestY = r.ReadInt32();
                    DestElevation = r.ReadByte();
                }
            }
            public sealed class ObjEvent
            {
                public readonly int X;
                public readonly int Y;
                public readonly byte Elevation;

                public readonly ushort Id;
                public readonly string Sprite;
                public readonly ObjMovementType MovementType;
                public readonly int MovementX;
                public readonly int MovementY;
                public readonly TrainerType TrainerType;
                public readonly byte TrainerSight;
                public readonly string Script;
                public readonly Flag Flag;

                public ObjEvent(EndianBinaryReader r)
                {
                    X = r.ReadInt32();
                    Y = r.ReadInt32();
                    Elevation = r.ReadByte();

                    Id = r.ReadUInt16();
                    Sprite = r.ReadStringNullTerminated();
                    MovementType = r.ReadEnum<ObjMovementType>();
                    MovementX = r.ReadInt32();
                    MovementY = r.ReadInt32();
                    TrainerType = r.ReadEnum<TrainerType>();
                    TrainerSight = r.ReadByte();
                    Script = r.ReadStringNullTerminated();
                    Flag = r.ReadEnum<Flag>();
                }
            }

            public readonly WarpEvent[] Warps;
            public readonly ObjEvent[] Objs;

            public Events(EndianBinaryReader r)
            {
                ushort count = r.ReadUInt16();
                Warps = new WarpEvent[count];
                for (int i = 0; i < count; i++)
                {
                    Warps[i] = new WarpEvent(r);
                }
                count = r.ReadUInt16();
                Objs = new ObjEvent[count];
                for (int i = 0; i < count; i++)
                {
                    Objs[i] = new ObjEvent(r);
                }
            }
        }
        public sealed class Layout
        {
            public sealed class Block
            {
                public readonly Layout Parent;
                public readonly int X;
                public readonly int Y;

                public readonly byte Elevations;
                public readonly LayoutBlockPassage Passage;
                public readonly Blockset.Block BlocksetBlock;

                public Block(Layout parent, int x, int y, EndianBinaryReader r)
                {
                    Parent = parent;
                    X = x;
                    Y = y;

                    Elevations = r.ReadByte();
                    Passage = r.ReadEnum<LayoutBlockPassage>();
                    BlocksetBlock = Blockset.LoadOrGet(r.ReadInt32()).Blocks[r.ReadInt32()];
                }
            }

            public readonly int BlocksWidth;
            public readonly int BlocksHeight;
            public readonly Block[][] Blocks;
            public readonly byte BorderWidth;
            public readonly byte BorderHeight;
            public readonly Block[][] BorderBlocks;

            private Layout(string name)
            {
                using (var r = new EndianBinaryReader(Utils.GetResourceStream(LayoutPath + name + LayoutExtension)))
                {
                    BlocksWidth = r.ReadInt32();
                    if (BlocksWidth <= 0)
                    {
                        throw new InvalidDataException();
                    }
                    BlocksHeight = r.ReadInt32();
                    if (BlocksHeight <= 0)
                    {
                        throw new InvalidDataException();
                    }
                    Blocks = new Block[BlocksHeight][];
                    for (int y = 0; y < BlocksHeight; y++)
                    {
                        var arrY = new Block[BlocksWidth];
                        for (int x = 0; x < BlocksWidth; x++)
                        {
                            arrY[x] = new Block(this, x, y, r);
                        }
                        Blocks[y] = arrY;
                    }
                    BorderWidth = r.ReadByte();
                    BorderHeight = r.ReadByte();
                    if (BorderWidth == 0 || BorderHeight == 0)
                    {
                        BorderBlocks = Array.Empty<Block[]>();
                    }
                    else
                    {
                        BorderBlocks = new Block[BorderHeight][];
                        for (int y = 0; y < BorderHeight; y++)
                        {
                            var arrY = new Block[BorderWidth];
                            for (int x = 0; x < BorderWidth; x++)
                            {
                                arrY[x] = new Block(this, -1, -1, r);
                            }
                            BorderBlocks[y] = arrY;
                        }
                    }
                }
            }

            private const string LayoutExtension = ".pgelayout";
            private const string LayoutPath = "Layout.";
            private static readonly IdList _ids = new IdList(LayoutPath + "LayoutIds.txt");
            private static readonly Dictionary<int, WeakReference<Layout>> _loadedLayouts = new Dictionary<int, WeakReference<Layout>>();
            public static Layout LoadOrGet(int id)
            {
                string name = _ids[id];
                if (name == null)
                {
                    throw new ArgumentOutOfRangeException(nameof(id));
                }
                Layout l;
                if (!_loadedLayouts.TryGetValue(id, out WeakReference<Layout> w))
                {
                    l = new Layout(name);
                    _loadedLayouts.Add(id, new WeakReference<Layout>(l));
                }
                else if (!w.TryGetTarget(out l))
                {
                    l = new Layout(name);
                    w.SetTarget(l);
                }
                return l;
            }
        }

        public readonly Layout MapLayout;
        public readonly Details MapDetails;
        public readonly Connection[] Connections;
        public readonly EncounterGroups Encounters;
        public readonly Events MapEvents;

        public readonly List<Obj> Objs = new List<Obj>();

        private Map(string name)
        {
            using (var r = new EndianBinaryReader(Utils.GetResourceStream(MapPath + name + MapExtension)))
            {
                MapLayout = Layout.LoadOrGet(r.ReadInt32());
                MapDetails = new Details(r);
                int numConnections = r.ReadByte();
                Connections = new Connection[numConnections];
                for (int i = 0; i < numConnections; i++)
                {
                    Connections[i] = new Connection(r);
                }
                Encounters = new EncounterGroups(r);
                MapEvents = new Events(r);
            }
        }

        private const string MapExtension = ".pgemap";
        private const string MapPath = "Map.";
        private static readonly IdList _ids = new IdList(MapPath + "MapIds.txt");
        private static readonly Dictionary<int, WeakReference<Map>> _loadedMaps = new Dictionary<int, WeakReference<Map>>();
        public static Map LoadOrGet(int id)
        {
            string name = _ids[id];
            if (name is null)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }
            Map m;
            if (!_loadedMaps.TryGetValue(id, out WeakReference<Map> w))
            {
                m = new Map(name);
                _loadedMaps.Add(id, new WeakReference<Map>(m));
            }
            else if (!w.TryGetTarget(out m))
            {
                m = new Map(name);
                w.SetTarget(m);
            }
            return m;
        }

        public void GetXYMap(int x, int y, out int outX, out int outY, out Map outMap)
        {
            Layout ml = MapLayout;
            bool north = y < 0;
            bool south = y >= ml.BlocksHeight;
            bool west = x < 0;
            bool east = x >= ml.BlocksWidth;
            // If we're out of bounds, try to branch into a connection. If we don't find one, we meet at the bottom
            if (north || south || west || east)
            {
                // TODO: How should connections retain map references? Answer: Visible maps/objs list
                Connection[] connections = Connections;
                int numConnections = connections.Length;
                for (int i = 0; i < numConnections; i++)
                {
                    Connection c = connections[i];
                    switch (c.Dir)
                    {
                        case Connection.Direction.South:
                        {
                            if (south)
                            {
                                Map m = LoadOrGet(c.MapId);
                                Layout l = m.MapLayout;
                                if (x >= c.Offset && x < c.Offset + l.BlocksWidth)
                                {
                                    m.GetXYMap(x - c.Offset, y - ml.BlocksHeight, out outX, out outY, out outMap);
                                    return;
                                }
                            }
                            break;
                        }
                        case Connection.Direction.North:
                        {
                            if (north)
                            {
                                Map m = LoadOrGet(c.MapId);
                                Layout l = m.MapLayout;
                                if (x >= c.Offset && x < c.Offset + l.BlocksWidth)
                                {
                                    m.GetXYMap(x - c.Offset, l.BlocksHeight + y, out outX, out outY, out outMap);
                                    return;
                                }
                            }
                            break;
                        }
                        case Connection.Direction.West:
                        {
                            if (west)
                            {
                                Map m = LoadOrGet(c.MapId);
                                Layout l = m.MapLayout;
                                if (y >= c.Offset && y < c.Offset + l.BlocksHeight)
                                {
                                    m.GetXYMap(l.BlocksWidth + x, y - c.Offset, out outX, out outY, out outMap);
                                    return;
                                }
                            }
                            break;
                        }
                        case Connection.Direction.East:
                        {
                            if (east)
                            {
                                Map m = LoadOrGet(c.MapId);
                                Layout l = m.MapLayout;
                                if (y >= c.Offset && y < c.Offset + l.BlocksHeight)
                                {
                                    m.GetXYMap(x - ml.BlocksWidth, y - c.Offset, out outX, out outY, out outMap);
                                    return;
                                }
                            }
                            break;
                        }
                    }
                }

            }
            // If we are in bounds, return the current map
            // If we didn't find a connection, we are at the border, which counts as the current map
            outX = x;
            outY = y;
            outMap = this;
        }
        public Layout.Block GetBlock_CrossMap(int x, int y, out Map outMap)
        {
            GetXYMap(x, y, out int outX, out int outY, out outMap);
            return outMap.GetBlock_InBounds(outX, outY);
        }
        public Layout.Block GetBlock_CrossMap(int x, int y)
        {
            return GetBlock_CrossMap(x, y, out _);
        }
        public Layout.Block GetBlock_InBounds(int x, int y)
        {
            Layout ml = MapLayout;
            bool north = y < 0;
            bool south = y >= ml.BlocksHeight;
            bool west = x < 0;
            bool east = x >= ml.BlocksWidth;
            // In bounds
            if (!north && !south && !west && !east)
            {
                return ml.Blocks[y][x];
            }
            // Border blocks
            byte bw = ml.BorderWidth;
            byte bh = ml.BorderHeight;
            // No border should render pure black
            if (bw == 0 || bh == 0)
            {
                return null;
            }
            // Has a border
            x %= bw;
            if (west)
            {
                x *= -1;
            }
            y %= bh;
            if (north)
            {
                y *= -1;
            }
            return ml.BorderBlocks[y][x];
        }

        public void LoadObjEvents()
        {
            Flags flags = Game.Instance.Save.Flags;
            foreach (Events.ObjEvent oe in MapEvents.Objs)
            {
                if (!flags[oe.Flag])
                {
                    new EventObj(oe, this);
                }
            }
        }
        public void UnloadObjEvents()
        {
            foreach (Obj o in Objs)
            {
                if (o.Id != Overworld.CameraId && o != CameraObj.CameraAttachedTo)
                {
                    Obj.LoadedObjs.Remove(o);
                }
            }
            Objs.Clear();
        }

        // "exceptThisOne" is used so objs aren't checking if they collide with themselves
        // The camera is not hardcoded here because we can have some objs disable collisions, plus someone might want to get the camera from this
        public List<Obj> GetObjs_InBounds(int x, int y, byte elevation, Obj exceptThisOne)
        {
            var list = new List<Obj>();
            foreach (Obj o in Objs)
            {
                if (o != exceptThisOne)
                {
                    Obj.Position pos = o.Pos;
                    if (pos.X == x && pos.Y == y && pos.Elevation == elevation)
                    {
                        list.Add(o);
                    }
                }
            }
            return list;
        }
    }
}
