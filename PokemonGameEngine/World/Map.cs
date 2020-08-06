﻿using Kermalis.EndianBinaryIO;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Util;
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

            public readonly WarpEvent[] Warps;

            public Events(EndianBinaryReader r)
            {
                ushort count = r.ReadUInt16();
                Warps = new WarpEvent[count];
                for (int i = 0; i < count; i++)
                {
                    Warps[i] = new WarpEvent(r);
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

                public readonly byte Elevation;
                public readonly LayoutBlockPassage Passage;
                public readonly Blockset.Block BlocksetBlock;

                public Block(Layout parent, int x, int y, EndianBinaryReader r)
                {
                    Parent = parent;
                    X = x;
                    Y = y;

                    Elevation = r.ReadByte();
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
            private static readonly Dictionary<int, WeakReference<Layout>> loadedLayouts = new Dictionary<int, WeakReference<Layout>>();
            public static Layout LoadOrGet(int id)
            {
                string name = _ids[id];
                if (name == null)
                {
                    throw new ArgumentOutOfRangeException(nameof(id));
                }
                Layout l;
                if (!loadedLayouts.ContainsKey(id))
                {
                    l = new Layout(name);
                    loadedLayouts.Add(id, new WeakReference<Layout>(l));
                    return l;
                }
                if (loadedLayouts[id].TryGetTarget(out l))
                {
                    return l;
                }
                l = new Layout(name);
                loadedLayouts[id].SetTarget(l);
                return l;
            }
        }

        public readonly Layout MapLayout;
        public readonly Details MapDetails;
        public readonly Events MapEvents;
        public readonly EncounterGroups Encounters;
        public readonly Connection[] Connections;

        public readonly List<Obj> Objs = new List<Obj>();

        private Map(string name)
        {
            using (var r = new EndianBinaryReader(Utils.GetResourceStream(MapPath + name + MapExtension)))
            {
                MapLayout = Layout.LoadOrGet(r.ReadInt32());
                MapDetails = new Details(r);
                MapEvents = new Events(r);
                Encounters = new EncounterGroups(r);
                int numConnections = r.ReadByte();
                Connections = new Connection[numConnections];
                for (int i = 0; i < numConnections; i++)
                {
                    Connections[i] = new Connection(r);
                }
            }
        }

        private const string MapExtension = ".pgemap";
        private const string MapPath = "Map.";
        private static readonly IdList _ids = new IdList(MapPath + "MapIds.txt");
        private static readonly Dictionary<int, WeakReference<Map>> loadedMaps = new Dictionary<int, WeakReference<Map>>();
        public static Map LoadOrGet(int id)
        {
            string name = _ids[id];
            if (name is null)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }
            Map m;
            if (!loadedMaps.ContainsKey(id))
            {
                m = new Map(name);
                loadedMaps.Add(id, new WeakReference<Map>(m));
                return m;
            }
            if (loadedMaps[id].TryGetTarget(out m))
            {
                return m;
            }
            m = new Map(name);
            loadedMaps[id].SetTarget(m);
            return m;
        }

        public Layout.Block GetBlock(int x, int y, out Map map)
        {
            Layout ml = MapLayout;
            bool north = y < 0;
            bool south = y >= ml.BlocksHeight;
            bool west = x < 0;
            bool east = x >= ml.BlocksWidth;
            if (!north && !south && !west && !east)
            {
                map = this;
                return ml.Blocks[y][x];
            }
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
                                return m.GetBlock(x - c.Offset, y - ml.BlocksHeight, out map);
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
                                return m.GetBlock(x - c.Offset, l.BlocksHeight + y, out map);
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
                                return m.GetBlock(l.BlocksWidth + x, y - c.Offset, out map);
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
                                return m.GetBlock(x - ml.BlocksWidth, y - c.Offset, out map);
                            }
                        }
                        break;
                    }
                }
            }
            // Border blocks should count as the calling map
            map = this;
            // No border should render pure black
            if (ml.BorderWidth == 0 || ml.BorderHeight == 0)
            {
                return null;
            }
            // Has a border
            x %= ml.BorderWidth;
            if (west)
            {
                x *= -1;
            }
            y %= ml.BorderHeight;
            if (north)
            {
                y *= -1;
            }
            return ml.BorderBlocks[y][x];
        }
        public Layout.Block GetBlock(int x, int y)
        {
            return GetBlock(x, y, out _);
        }

        public static unsafe void Draw(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            Obj camera = Obj.Camera;
            Obj.Position cameraPos = camera.Pos;
            int cameraX = (cameraPos.X * Overworld.Block_NumPixelsX) - (bmpWidth / 2) + (Overworld.Block_NumPixelsX / 2) + camera.ProgressX + Obj.CameraOfsX;
            int cameraY = (cameraPos.Y * Overworld.Block_NumPixelsY) - (bmpHeight / 2) + (Overworld.Block_NumPixelsY / 2) + camera.ProgressY + Obj.CameraOfsY;
            Map cameraMap = camera.Map;
            int xpBX = cameraX % Overworld.Block_NumPixelsX;
            int ypBY = cameraY % Overworld.Block_NumPixelsY;
            int startBlockX = (cameraX / Overworld.Block_NumPixelsX) - (xpBX >= 0 ? 0 : 1);
            int startBlockY = (cameraY / Overworld.Block_NumPixelsY) - (ypBY >= 0 ? 0 : 1);
            int numBlocksX = (bmpWidth / Overworld.Block_NumPixelsX) + (bmpWidth % Overworld.Block_NumPixelsX == 0 ? 0 : 1);
            int numBlocksY = (bmpHeight / Overworld.Block_NumPixelsY) + (bmpHeight % Overworld.Block_NumPixelsY == 0 ? 0 : 1);
            int endBlockX = startBlockX + numBlocksX + (xpBX == 0 ? 0 : 1);
            int endBlockY = startBlockY + numBlocksY + (ypBY == 0 ? 0 : 1);
            int startX = xpBX >= 0 ? -xpBX : -xpBX - Overworld.Block_NumPixelsX;
            int startY = ypBY >= 0 ? -ypBY : -ypBY - Overworld.Block_NumPixelsY;
            byte e = 0;
            while (true)
            {
                int curX = startX;
                int curY = startY;
                for (int blockY = startBlockY; blockY < endBlockY; blockY++)
                {
                    for (int blockX = startBlockX; blockX < endBlockX; blockX++)
                    {
                        Layout.Block block = cameraMap.GetBlock(blockX, blockY, out _);
                        if (block != null)
                        {
                            Blockset.Block b = block.BlocksetBlock;
                            void Draw(Blockset.Block.Tile[] subLayers, int tx, int ty)
                            {
                                int numSubLayers = subLayers.Length;
                                for (int t = 0; t < numSubLayers; t++)
                                {
                                    Blockset.Block.Tile tile = subLayers[t];
                                    RenderUtils.DrawBitmap(bmpAddress, bmpWidth, bmpHeight, tx, ty, tile.TilesetTile.Bitmap, Overworld.Tile_NumPixelsX, Overworld.Tile_NumPixelsY, xFlip: tile.XFlip, yFlip: tile.YFlip);
                                }
                            }
                            for (int ly = 0; ly < Overworld.Block_NumTilesY; ly++)
                            {
                                Dictionary<byte, Blockset.Block.Tile[]>[] arrY = b.Tiles[ly];
                                int py = ly * Overworld.Tile_NumPixelsY;
                                for (int lx = 0; lx < Overworld.Block_NumTilesX; lx++)
                                {
                                    Draw(arrY[lx][e], curX + (lx * Overworld.Tile_NumPixelsX), curY + py);
                                }
                            }
                        }
                        curX += Overworld.Block_NumPixelsX;
                    }
                    curX = startX;
                    curY += Overworld.Block_NumPixelsY;
                }
                // TODO: They will overlap each other regardless of y coordinate because of the order of the list
                // TODO: Objs from other maps
                List<Obj> objs = cameraMap.Objs;
                int numObjs = objs.Count;
                for (int i = 0; i < numObjs; i++)
                {
                    Obj c = objs[i];
                    if (c == camera)
                    {
                        continue;
                    }
                    Obj.Position cPos = c.Pos;
                    if (cPos.Elevation != e)
                    {
                        continue;
                    }
                    int objX = ((cPos.X - startBlockX) * Overworld.Block_NumPixelsX) + c.ProgressX + startX;
                    int objY = ((cPos.Y - startBlockY) * Overworld.Block_NumPixelsY) + c.ProgressY + startY;
                    int objW = c.SpriteWidth;
                    int objH = c.SpriteHeight;
                    objX -= (objW - Overworld.Block_NumPixelsX) / 2;
                    objY -= objH - Overworld.Block_NumPixelsY;
                    if (objX < bmpWidth && objX + objW > 0 && objY < bmpHeight && objY + objH > 0)
                    {
                        c.Draw(bmpAddress, bmpWidth, bmpHeight, objX, objY);
                    }
                }
                if (e == byte.MaxValue)
                {
                    break;
                }
                e++;
            }
        }
    }
}