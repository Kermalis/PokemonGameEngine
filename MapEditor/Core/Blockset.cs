using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Kermalis.EndianBinaryIO;
using Kermalis.MapEditor.Util;
using Kermalis.PokemonGameEngine.World;
using System;
using System.Collections.Generic;
using System.IO;

namespace Kermalis.MapEditor.Core
{
    public sealed class Blockset : IDisposable
    {
        internal sealed class Block
        {
            public sealed class Tile
            {
                public bool XFlip;
                public bool YFlip;
                public Tileset.Tile TilesetTile;

                public Tile() { }
                public Tile(EndianBinaryReader r)
                {
                    XFlip = r.ReadBoolean();
                    YFlip = r.ReadBoolean();
                    TilesetTile = Tileset.LoadOrGet(r.ReadInt32()).Tiles[r.ReadInt32()];
                }

                public void CopyTo(Tile other)
                {
                    other.XFlip = XFlip;
                    other.YFlip = YFlip;
                    other.TilesetTile = TilesetTile;
                }

                public unsafe void Draw(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y)
                {
                    RenderUtils.DrawBitmap(bmpAddress, bmpWidth, bmpHeight, x, y, TilesetTile.Bitmap, Overworld.Tile_NumPixelsX, Overworld.Tile_NumPixelsY, xFlip: XFlip, yFlip: YFlip);
                }

                public bool Equals(Tile other)
                {
                    return other != null && XFlip == other.XFlip && YFlip == other.YFlip && TilesetTile == other.TilesetTile;
                }

                public void Write(EndianBinaryWriter w)
                {
                    w.Write(XFlip);
                    w.Write(YFlip);
                    w.Write(TilesetTile.Parent.Id);
                    w.Write(TilesetTile.Id);
                }
            }

            public Blockset Parent;
            public int Id;

            public BlocksetBlockBehavior Behavior;
            public readonly List<Tile>[][][] Tiles;

            public Block(Blockset parent, int id, EndianBinaryReader r)
            {
                Behavior = r.ReadEnum<BlocksetBlockBehavior>();
                List<Tile>[] Read()
                {
                    var eLayers = new List<Tile>[Overworld.NumElevations];
                    for (byte e = 0; e < Overworld.NumElevations; e++)
                    {
                        byte count = r.ReadByte();
                        var subLayers = new List<Tile>(count);
                        for (int i = 0; i < count; i++)
                        {
                            subLayers.Add(new Tile(r));
                        }
                        eLayers[e] = subLayers;
                    }
                    return eLayers;
                }
                Tiles = new List<Tile>[Overworld.Block_NumTilesY][][];
                for (int y = 0; y < Overworld.Block_NumTilesY; y++)
                {
                    var arrY = new List<Tile>[Overworld.Block_NumTilesX][];
                    for (int x = 0; x < Overworld.Block_NumTilesX; x++)
                    {
                        arrY[x] = Read();
                    }
                    Tiles[y] = arrY;
                }
                Parent = parent;
                Id = id;
            }
            public Block(Blockset parent, int id)
            {
                Parent = parent;
                Id = id;
                List<Tile>[] Create()
                {
                    var d = new List<Tile>[Overworld.NumElevations];
                    for (byte e = 0; e < Overworld.NumElevations; e++)
                    {
                        d[e] = new List<Tile>();
                    }
                    return d;
                }
                Tiles = new List<Tile>[Overworld.Block_NumTilesY][][];
                for (int y = 0; y < Overworld.Block_NumTilesY; y++)
                {
                    var arrY = new List<Tile>[Overworld.Block_NumTilesX][];
                    for (int x = 0; x < Overworld.Block_NumTilesX; x++)
                    {
                        arrY[x] = Create();
                    }
                    Tiles[y] = arrY;
                }
            }

            public unsafe void Draw(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y)
            {
                for (byte e = 0; e < Overworld.NumElevations; e++)
                {
                    Draw(bmpAddress, bmpWidth, bmpHeight, x, y, e);
                }
            }
            public unsafe void Draw(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y, byte e)
            {
                void Draw(List<Tile> subLayers, int tx, int ty)
                {
                    for (int t = 0; t < subLayers.Count; t++)
                    {
                        subLayers[t].Draw(bmpAddress, bmpWidth, bmpHeight, tx, ty);
                    }
                }
                for (int ly = 0; ly < Overworld.Block_NumTilesY; ly++)
                {
                    List<Tile>[][] arrY = Tiles[ly];
                    int py = ly * Overworld.Tile_NumPixelsY;
                    for (int lx = 0; lx < Overworld.Block_NumTilesX; lx++)
                    {
                        Draw(arrY[lx][e], x + (lx * Overworld.Tile_NumPixelsX), y + py);
                    }
                }
            }

            public void Write(EndianBinaryWriter w)
            {
                w.Write(Behavior);
                void Write(List<Tile>[] eLayers)
                {
                    for (byte e = 0; e < Overworld.NumElevations; e++)
                    {
                        List<Tile> subLayers = eLayers[e];
                        byte count = (byte)subLayers.Count;
                        w.Write(count);
                        for (int i = 0; i < count; i++)
                        {
                            subLayers[i].Write(w);
                        }
                    }
                }
                for (int y = 0; y < Overworld.Block_NumTilesY; y++)
                {
                    List<Tile>[][] arrY = Tiles[y];
                    for (int x = 0; x < Overworld.Block_NumTilesX; x++)
                    {
                        Write(arrY[x]);
                    }
                }
            }
        }

        internal delegate void BlocksetEventHandler(Blockset blockset, Block block);
        internal event BlocksetEventHandler OnAdded;
        internal event BlocksetEventHandler OnChanged;
        internal event BlocksetEventHandler OnRemoved;

        internal readonly string Name;
        internal readonly int Id;

        internal const int BitmapNumBlocksX = 8;
        internal WriteableBitmap Bitmap;
        internal event EventHandler<EventArgs> OnDrew;

        internal readonly List<Block> Blocks;

        private Blockset(string name, int id)
        {
            using (var r = new EndianBinaryReader(File.OpenRead(Path.Combine(BlocksetPath, name + BlocksetExtension))))
            {
                ushort count = r.ReadUInt16();
                Blocks = new List<Block>(count);
                for (int i = 0; i < count; i++)
                {
                    Blocks.Add(new Block(this, i, r));
                }
            }
            Name = name;
            Id = id;
            UpdateBitmapSize();
            DrawAll();
        }
        internal Blockset(string name)
        {
            Id = Ids.Add(name);
            _loadedBlocksets.Add(Id, new WeakReference<Blockset>(this));
            Blocks = new List<Block>() { new Block(this, 0) };
            Name = name;
            Save();
            Ids.Save();
            UpdateBitmapSize();
            DrawAll();
        }
        ~Blockset()
        {
            Dispose(false);
        }

        private const string BlocksetExtension = ".pgeblockset";
        private static readonly string BlocksetPath = Path.Combine(Program.AssetPath, "Blockset");
        public static IdList Ids { get; } = new IdList(Path.Combine(BlocksetPath, "BlocksetIds.txt"));
        private static readonly Dictionary<int, WeakReference<Blockset>> _loadedBlocksets = new Dictionary<int, WeakReference<Blockset>>();
        internal static Blockset LoadOrGet(string name)
        {
            int id = Ids[name];
            if (id == -1)
            {
                throw new ArgumentOutOfRangeException(nameof(name));
            }
            return LoadOrGet(name, id);
        }
        internal static Blockset LoadOrGet(int id)
        {
            string name = Ids[id];
            if (name is null)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }
            return LoadOrGet(name, id);
        }
        private static Blockset LoadOrGet(string name, int id)
        {
            Blockset b;
            if (!_loadedBlocksets.TryGetValue(id, out WeakReference<Blockset> w))
            {
                b = new Blockset(name, id);
                _loadedBlocksets.Add(id, new WeakReference<Blockset>(b));
            }
            else if (!w.TryGetTarget(out b))
            {
                b = new Blockset(name, id);
                w.SetTarget(b);
            }
            return b;
        }

        internal void Add()
        {
            var block = new Block(this, Blocks.Count);
            Blocks.Add(block);
            OnAdded?.Invoke(this, block);
            if (UpdateBitmapSize())
            {
                DrawAll();
            }
            else
            {
                DrawOne(block);
            }
        }
        internal static void Clear(Block block)
        {
            for (int y = 0; y < Overworld.Block_NumTilesY; y++)
            {
                List<Block.Tile>[][] arrY = block.Tiles[y];
                for (int x = 0; x < Overworld.Block_NumTilesX; x++)
                {
                    List<Block.Tile>[] arrX = arrY[x];
                    for (byte e = 0; e < Overworld.NumElevations; e++)
                    {
                        arrX[e].Clear();
                    }
                }
            }
            Blockset blockset = block.Parent;
            blockset.OnChanged?.Invoke(blockset, block);
            blockset.DrawOne(block);
        }
        internal static void Remove(Block block)
        {
            Blockset blockset = block.Parent;
            blockset.Blocks.Remove(block);
            block.Parent = null;
            for (int i = block.Id; i < blockset.Blocks.Count; i++)
            {
                blockset.Blocks[i].Id--;
            }
            blockset.OnRemoved?.Invoke(blockset, block);
            if (blockset.UpdateBitmapSize())
            {
                blockset.DrawAll();
            }
            else
            {
                blockset.DrawFrom(block.Id);
            }
        }
        internal void FireChanged(Block block)
        {
            OnChanged?.Invoke(this, block);
            DrawOne(block);
        }

        internal int GetNumBlockRows()
        {
            return (Blocks.Count / BitmapNumBlocksX) + (Blocks.Count % BitmapNumBlocksX != 0 ? 1 : 0);
        }
        private int GetBitmapHeight()
        {
            return GetNumBlockRows() * Overworld.Block_NumPixelsY;
        }
        private bool UpdateBitmapSize()
        {
            int bmpHeight = GetBitmapHeight();
            if (Bitmap == null || Bitmap.PixelSize.Height != bmpHeight)
            {
                Bitmap?.Dispose();
                Bitmap = new WriteableBitmap(new PixelSize(BitmapNumBlocksX * Overworld.Block_NumPixelsX, bmpHeight), new Vector(96, 96), PixelFormat.Bgra8888);
                return true;
            }
            return false;
        }
        private unsafe void DrawOne(Block block)
        {
            const int bmpWidth = BitmapNumBlocksX * Overworld.Block_NumPixelsX;
            int bmpHeight = GetBitmapHeight();
            using (ILockedFramebuffer l = Bitmap.Lock())
            {
                uint* bmpAddress = (uint*)l.Address.ToPointer();
                int x = block.Id % BitmapNumBlocksX * Overworld.Block_NumPixelsX;
                int y = block.Id / BitmapNumBlocksX * Overworld.Block_NumPixelsY;
                RenderUtils.FillColor(bmpAddress, bmpWidth, bmpHeight, x, y, Overworld.Block_NumPixelsX, Overworld.Block_NumPixelsY, 0xFF000000);
                block.Draw(bmpAddress, bmpWidth, bmpHeight, x, y);
            }
            OnDrew?.Invoke(this, EventArgs.Empty);
        }
        private unsafe void DrawFrom(int index)
        {
            const int bmpWidth = BitmapNumBlocksX * Overworld.Block_NumPixelsX;
            int bmpHeight = GetBitmapHeight();
            using (ILockedFramebuffer l = Bitmap.Lock())
            {
                uint* bmpAddress = (uint*)l.Address.ToPointer();
                int x = index % BitmapNumBlocksX;
                int y = index / BitmapNumBlocksX;
                for (; index < Blocks.Count; index++, x++)
                {
                    if (x >= BitmapNumBlocksX)
                    {
                        x = 0;
                        y++;
                    }
                    int bx = x * Overworld.Block_NumPixelsX;
                    int by = y * Overworld.Block_NumPixelsY;
                    RenderUtils.FillColor(bmpAddress, bmpWidth, bmpHeight, bx, by, Overworld.Block_NumPixelsX, Overworld.Block_NumPixelsY, 0xFF000000);
                    Blocks[index].Draw(bmpAddress, bmpWidth, bmpHeight, bx, by);
                }
                for (; x < BitmapNumBlocksX; x++)
                {
                    int bx = x * Overworld.Block_NumPixelsX;
                    int by = y * Overworld.Block_NumPixelsY;
                    RenderUtils.FillColor(bmpAddress, bmpWidth, bmpHeight, bx, by, Overworld.Block_NumPixelsX, Overworld.Block_NumPixelsY, 0xFF000000);
                    RenderUtils.DrawCross(bmpAddress, bmpWidth, bmpHeight, bx, by, Overworld.Block_NumPixelsX, Overworld.Block_NumPixelsY, 0xFFFF0000);
                }
            }
            OnDrew?.Invoke(this, EventArgs.Empty);
        }
        private unsafe void DrawAll()
        {
            const int bmpWidth = BitmapNumBlocksX * Overworld.Block_NumPixelsX;
            int bmpHeight = GetBitmapHeight();
            using (ILockedFramebuffer l = Bitmap.Lock())
            {
                uint* bmpAddress = (uint*)l.Address.ToPointer();
                RenderUtils.FillColor(bmpAddress, bmpWidth, bmpHeight, 0, 0, bmpWidth, bmpHeight, 0xFF000000);
                int x = 0;
                int y = 0;
                for (int i = 0; i < Blocks.Count; i++, x++)
                {
                    if (x >= BitmapNumBlocksX)
                    {
                        x = 0;
                        y++;
                    }
                    Blocks[i].Draw(bmpAddress, bmpWidth, bmpHeight, x * Overworld.Block_NumPixelsX, y * Overworld.Block_NumPixelsY);
                }
                for (; x < BitmapNumBlocksX; x++)
                {
                    RenderUtils.DrawCross(bmpAddress, bmpWidth, bmpHeight, x * Overworld.Block_NumPixelsX, y * Overworld.Block_NumPixelsY, Overworld.Block_NumPixelsX, Overworld.Block_NumPixelsY, 0xFFFF0000);
                }
            }
            OnDrew?.Invoke(this, EventArgs.Empty);
        }

        internal void Save()
        {
            using (var w = new EndianBinaryWriter(File.Create(Path.Combine(BlocksetPath, Name + BlocksetExtension))))
            {
                ushort count = (ushort)Blocks.Count;
                w.Write(count);
                for (int i = 0; i < count; i++)
                {
                    Blocks[i].Write(w);
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
            Bitmap.Dispose();
        }
    }
}
