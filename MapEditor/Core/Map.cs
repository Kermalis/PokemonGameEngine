using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Kermalis.EndianBinaryIO;
using Kermalis.MapEditor.Util;
using System;
using System.Collections.Generic;
using System.IO;

namespace Kermalis.MapEditor.Core
{
    public sealed class Map
    {
        internal sealed class Connection
        {
            public enum Direction : byte
            {
                North,
                West,
                East,
                South
            }
            public Direction Dir;
            public int MapId;
            public int Offset;

            public Connection() { }
            public Connection(EndianBinaryReader r)
            {
                Dir = (Direction)r.ReadByte();
                MapId = r.ReadInt32();
                Offset = r.ReadInt32();
            }

            public void Write(EndianBinaryWriter w)
            {
                w.Write((byte)Dir);
                w.Write(MapId);
                w.Write(Offset);
            }
        }
        internal sealed class Layout : IDisposable
        {
            public sealed class Block
            {
                public readonly int X;
                public readonly int Y;

                public byte Behavior;
                public Blockset.Block BlocksetBlock;

                public Block(bool isBorderBlock, int x, int y, EndianBinaryReader r)
                {
                    X = x;
                    Y = y;
                    if (!isBorderBlock)
                    {
                        Behavior = r.ReadByte();
                    }
                    BlocksetBlock = Blockset.LoadOrGet(r.ReadInt32()).Blocks[r.ReadInt32()];
                }
                public Block(int x, int y, Blockset.Block defaultBlock)
                {
                    X = x;
                    Y = y;
                    BlocksetBlock = defaultBlock;
                }

                public void Write(bool isBorderBlock, EndianBinaryWriter w)
                {
                    if (!isBorderBlock)
                    {
                        w.Write(Behavior);
                    }
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
                using (var r = new EndianBinaryReader(File.OpenRead(Path.Combine(_layoutPath, name + _layoutExtension))))
                {
                    Block[][] Create(bool borderBlocks, int w, int h)
                    {
                        var arr = new Block[h][];
                        for (int y = 0; y < h; y++)
                        {
                            var arrY = new Block[w];
                            for (int x = 0; x < w; x++)
                            {
                                arrY[x] = new Block(borderBlocks, x, y, r);
                            }
                            arr[y] = arrY;
                        }
                        return arr;
                    }
                    Blocks = Create(false, Width = r.ReadInt32(), Height = r.ReadInt32());
                    BorderBlocks = Create(true, BorderWidth = r.ReadByte(), BorderHeight = r.ReadByte());
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
                Dispose(false);
            }

            private const string _layoutExtension = ".pgelayout";
            private static readonly string _layoutPath = Path.Combine(Program.AssetPath, "Map", "Layout");
            public static IdList Ids { get; } = new IdList(Path.Combine(_layoutPath, "LayoutIds.txt"));
            private static readonly Dictionary<int, WeakReference<Layout>> _loadedLayouts = new Dictionary<int, WeakReference<Layout>>();
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
                if (name == null)
                {
                    throw new ArgumentOutOfRangeException(nameof(id));
                }
                return LoadOrGet(name, id);
            }
            private static Layout LoadOrGet(string name, int id)
            {
                Layout l;
                if (!_loadedLayouts.ContainsKey(id))
                {
                    l = new Layout(name, id);
                    _loadedLayouts.Add(id, new WeakReference<Layout>(l));
                    return l;
                }
                if (_loadedLayouts[id].TryGetTarget(out l))
                {
                    return l;
                }
                l = new Layout(name, id);
                _loadedLayouts[id].SetTarget(l);
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
                                if (b != null)
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
                int bmpWidth = (borderBlocks ? BorderWidth : Width) * 16;
                int bmpHeight = (borderBlocks ? BorderHeight : Height) * 16;
                bool createNew;
                if (bmp == null)
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
                    bmp = new WriteableBitmap(new PixelSize(bmpWidth, bmpHeight), new Vector(96, 96), PixelFormat.Bgra8888);
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
            public static readonly List<Block> DrawList = new List<Block>(); // Save allocations
            public unsafe void Draw(bool borderBlocks)
            {
                List<Block> list = DrawList;
                int count = list.Count;
                if (count > 0)
                {
                    WriteableBitmap bmp = borderBlocks ? BorderBlocksBitmap : BlocksBitmap;
                    using (ILockedFramebuffer l = bmp.Lock())
                    {
                        uint* bmpAddress = (uint*)l.Address.ToPointer();
                        int bmpWidth = (borderBlocks ? BorderWidth : Width) * 16;
                        int bmpHeight = (borderBlocks ? BorderHeight : Height) * 16;
                        for (int i = 0; i < count; i++)
                        {
                            Block b = list[i];
                            int x = b.X * 16;
                            int y = b.Y * 16;
                            RenderUtil.Fill(bmpAddress, bmpWidth, bmpHeight, x, y, 16, 16, 0xFF000000);
                            b.BlocksetBlock.Draw(bmpAddress, bmpWidth, bmpHeight, x, y);
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
                    uint* bmpAddress = (uint*)l.Address.ToPointer();
                    int width = borderBlocks ? BorderWidth : Width;
                    int height = borderBlocks ? BorderHeight : Height;
                    int bmpWidth = width * 16;
                    int bmpHeight = height * 16;
                    RenderUtil.Fill(bmpAddress, bmpWidth, bmpHeight, 0, 0, bmpWidth, bmpHeight, 0xFF000000);
                    Block[][] arr = borderBlocks ? BorderBlocks : Blocks;
                    for (int y = 0; y < height; y++)
                    {
                        Block[] arrY = arr[y];
                        for (int x = 0; x < width; x++)
                        {
                            arrY[x].BlocksetBlock.Draw(bmpAddress, bmpWidth, bmpHeight, x * 16, y * 16);
                        }
                    }
                }
                OnDrew?.Invoke(this, borderBlocks, wasResized);
            }

            public void Save()
            {
                using (var w = new EndianBinaryWriter(File.Create(Path.Combine(_layoutPath, Name + _layoutExtension))))
                {
                    w.Write(Width);
                    w.Write(Height);
                    for (int y = 0; y < Height; y++)
                    {
                        for (int x = 0; x < Width; x++)
                        {
                            Blocks[y][x].Write(false, w);
                        }
                    }
                    w.Write(BorderWidth);
                    w.Write(BorderHeight);
                    for (int y = 0; y < BorderHeight; y++)
                    {
                        for (int x = 0; x < BorderWidth; x++)
                        {
                            BorderBlocks[y][x].Write(true, w);
                        }
                    }
                }
            }

            public void Dispose()
            {
                Dispose(true);
            }
            private void Dispose(bool disposing)
            {
                if (disposing)
                {
                    GC.SuppressFinalize(this);
                }
                BlocksBitmap.Dispose();
                BorderBlocksBitmap.Dispose();
            }
        }

        internal readonly string Name;
        internal readonly int Id;

        internal readonly Layout MapLayout;
        internal readonly List<Connection> Connections;

        private Map(string name, int id)
        {
            using (var r = new EndianBinaryReader(File.OpenRead(Path.Combine(_mapPath, name + _mapExtension))))
            {
                MapLayout = Layout.LoadOrGet(r.ReadInt32());
                int numConnections = r.ReadByte();
                Connections = new List<Connection>(numConnections);
                for (int i = 0; i < numConnections; i++)
                {
                    Connections.Add(new Connection(r));
                }
            }
            Name = name;
            Id = id;
        }
        internal Map(string name, Layout layout)
        {
            Id = Ids.Add(name);
            _loadedMaps.Add(Id, new WeakReference<Map>(this));
            MapLayout = layout;
            Connections = new List<Connection>();
            Name = name;
            Save();
            Ids.Save();
        }

        private const string _mapExtension = ".pgemap";
        private static readonly string _mapPath = Path.Combine(Program.AssetPath, "Map");
        public static IdList Ids { get; } = new IdList(Path.Combine(_mapPath, "MapIds.txt"));
        private static readonly Dictionary<int, WeakReference<Map>> _loadedMaps = new Dictionary<int, WeakReference<Map>>();
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
            if (name == null)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }
            return LoadOrGet(name, id);
        }
        private static Map LoadOrGet(string name, int id)
        {
            Map m;
            if (!_loadedMaps.ContainsKey(id))
            {
                m = new Map(name, id);
                _loadedMaps.Add(id, new WeakReference<Map>(m));
                return m;
            }
            if (_loadedMaps[id].TryGetTarget(out m))
            {
                return m;
            }
            m = new Map(name, id);
            _loadedMaps[id].SetTarget(m);
            return m;
        }

        public void Save()
        {
            using (var w = new EndianBinaryWriter(File.Create(Path.Combine(_mapPath, Name + _mapExtension))))
            {
                w.Write(MapLayout.Id);
                byte numConnections = (byte)Connections.Count;
                w.Write(numConnections);
                for (int i = 0; i < numConnections; i++)
                {
                    Connections[i].Write(w);
                }
            }
        }
    }
}
