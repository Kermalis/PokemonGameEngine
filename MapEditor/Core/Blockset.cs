using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Kermalis.EndianBinaryIO;
using Kermalis.MapEditor.Util;
using Kermalis.PokemonGameEngine.Overworld;
using System;
using System.Collections.Generic;
using System.IO;

namespace Kermalis.MapEditor.Core
{
    internal sealed class Blockset : IDisposable
    {
        public sealed class Block
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
                    RenderUtil.Draw(bmpAddress, bmpWidth, bmpHeight, x, y, TilesetTile.Colors, XFlip, YFlip);
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
            public readonly Dictionary<byte, List<Tile>> TopLeft;
            public readonly Dictionary<byte, List<Tile>> TopRight;
            public readonly Dictionary<byte, List<Tile>> BottomLeft;
            public readonly Dictionary<byte, List<Tile>> BottomRight;

            public Block(Blockset parent, int id, EndianBinaryReader r)
            {
                Behavior = r.ReadEnum<BlocksetBlockBehavior>();
                Dictionary<byte, List<Tile>> Read()
                {
                    var eLayers = new Dictionary<byte, List<Tile>>(byte.MaxValue + 1);
                    byte e = 0;
                    while (true)
                    {
                        byte count = r.ReadByte();
                        var subLayers = new List<Tile>(count);
                        for (int i = 0; i < count; i++)
                        {
                            subLayers.Add(new Tile(r));
                        }
                        eLayers.Add(e, subLayers);
                        if (e == byte.MaxValue)
                        {
                            break;
                        }
                        e++;
                    }
                    return eLayers;
                }
                TopLeft = Read();
                TopRight = Read();
                BottomLeft = Read();
                BottomRight = Read();
                Parent = parent;
                Id = id;
            }
            public Block(Blockset parent, int id)
            {
                Parent = parent;
                Id = id;
                Dictionary<byte, List<Tile>> Create()
                {
                    var d = new Dictionary<byte, List<Tile>>(byte.MaxValue + 1);
                    byte e = 0;
                    while (true)
                    {
                        d.Add(e, new List<Tile>());
                        if (e == byte.MaxValue)
                        {
                            break;
                        }
                        e++;
                    }
                    return d;
                }
                TopLeft = Create();
                TopRight = Create();
                BottomLeft = Create();
                BottomRight = Create();
            }

            public unsafe void Draw(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y)
            {
                byte e = 0;
                while (true)
                {
                    Draw(bmpAddress, bmpWidth, bmpHeight, x, y, e);
                    if (e == byte.MaxValue)
                    {
                        break;
                    }
                    e++;
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
                Draw(TopLeft[e], x, y);
                Draw(TopRight[e], x + 8, y);
                Draw(BottomLeft[e], x, y + 8);
                Draw(BottomRight[e], x + 8, y + 8);
            }

            public void Write(EndianBinaryWriter w)
            {
                w.Write(Behavior);
                void Write(Dictionary<byte, List<Tile>> eLayers)
                {
                    byte e = 0;
                    while (true)
                    {
                        List<Tile> subLayers = eLayers[e];
                        byte count = (byte)subLayers.Count;
                        w.Write(count);
                        for (int i = 0; i < count; i++)
                        {
                            subLayers[i].Write(w);
                        }
                        if (e == byte.MaxValue)
                        {
                            break;
                        }
                        e++;
                    }
                }
                Write(TopLeft);
                Write(TopRight);
                Write(BottomLeft);
                Write(BottomRight);
            }
        }

        public delegate void BlocksetEventHandler(Blockset blockset, Block block);
        public event BlocksetEventHandler OnAdded;
        public event BlocksetEventHandler OnChanged;
        public event BlocksetEventHandler OnRemoved;

        public const int BitmapNumBlocksX = 8;
        public WriteableBitmap Bitmap;
        public event EventHandler<EventArgs> OnDrew;

        public readonly string Name;
        public readonly int Id;
        public List<Block> Blocks;

        private Blockset(string name, int id)
        {
            using (var r = new EndianBinaryReader(File.OpenRead(Path.Combine(_blocksetPath, name + _blocksetExtension))))
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
        public Blockset(string name)
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

        public static bool IsValidName(string name)
        {
            return !string.IsNullOrWhiteSpace(name) && !Utils.InvalidFileNameRegex.IsMatch(name) && Ids[name] == -1;
        }

        private const string _blocksetExtension = ".pgeblockset";
        private static readonly string _blocksetPath = Path.Combine(Program.AssetPath, "Blockset");
        public static IdList Ids { get; } = new IdList(Path.Combine(_blocksetPath, "BlocksetIds.txt"));
        private static readonly Dictionary<int, WeakReference<Blockset>> _loadedBlocksets = new Dictionary<int, WeakReference<Blockset>>();
        public static Blockset LoadOrGet(string name)
        {
            int id = Ids[name];
            if (id == -1)
            {
                throw new ArgumentOutOfRangeException(nameof(name));
            }
            return LoadOrGet(name, id);
        }
        public static Blockset LoadOrGet(int id)
        {
            string name = Ids[id];
            if (name == null)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }
            return LoadOrGet(name, id);
        }
        private static Blockset LoadOrGet(string name, int id)
        {
            Blockset b;
            if (!_loadedBlocksets.ContainsKey(id))
            {
                b = new Blockset(name, id);
                _loadedBlocksets.Add(id, new WeakReference<Blockset>(b));
                return b;
            }
            if (_loadedBlocksets[id].TryGetTarget(out b))
            {
                return b;
            }
            b = new Blockset(name, id);
            _loadedBlocksets[id].SetTarget(b);
            return b;
        }

        public void Add()
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
        public static void Clear(Block block)
        {
            byte e = 0;
            while (true)
            {
                block.TopLeft[e].Clear();
                block.TopRight[e].Clear();
                block.BottomLeft[e].Clear();
                block.BottomRight[e].Clear();
                if (e == byte.MaxValue)
                {
                    break;
                }
                e++;
            }
            Blockset blockset = block.Parent;
            blockset.OnChanged?.Invoke(blockset, block);
            blockset.DrawOne(block);
        }
        public static void Remove(Block block)
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
        public void FireChanged(Block block)
        {
            OnChanged?.Invoke(this, block);
            DrawOne(block);
        }

        private int GetBitmapHeight()
        {
            int numBlocksY = (Blocks.Count / BitmapNumBlocksX) + (Blocks.Count % BitmapNumBlocksX != 0 ? 1 : 0);
            return numBlocksY * 16;
        }
        private bool UpdateBitmapSize()
        {
            int bmpHeight = GetBitmapHeight();
            if (Bitmap == null || Bitmap.PixelSize.Height != bmpHeight)
            {
                Bitmap?.Dispose();
                Bitmap = new WriteableBitmap(new PixelSize(BitmapNumBlocksX * 16, bmpHeight), new Vector(96, 96), PixelFormat.Bgra8888);
                return true;
            }
            return false;
        }
        private unsafe void DrawOne(Block block)
        {
            const int bmpWidth = BitmapNumBlocksX * 16;
            int bmpHeight = GetBitmapHeight();
            using (ILockedFramebuffer l = Bitmap.Lock())
            {
                uint* bmpAddress = (uint*)l.Address.ToPointer();
                int x = block.Id % BitmapNumBlocksX * 16;
                int y = block.Id / BitmapNumBlocksX * 16;
                RenderUtil.Fill(bmpAddress, bmpWidth, bmpHeight, x, y, 16, 16, 0xFF000000);
                block.Draw(bmpAddress, bmpWidth, bmpHeight, x, y);
            }
            OnDrew?.Invoke(this, EventArgs.Empty);
        }
        private unsafe void DrawFrom(int index)
        {
            const int bmpWidth = BitmapNumBlocksX * 16;
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
                    int bx = x * 16;
                    int by = y * 16;
                    RenderUtil.Fill(bmpAddress, bmpWidth, bmpHeight, bx, by, 16, 16, 0xFF000000);
                    Blocks[index].Draw(bmpAddress, bmpWidth, bmpHeight, bx, by);
                }
                for (; x < BitmapNumBlocksX; x++)
                {
                    int bx = x * 16;
                    int by = y * 16;
                    RenderUtil.Fill(bmpAddress, bmpWidth, bmpHeight, bx, by, 16, 16, 0xFF000000);
                    RenderUtil.DrawCrossUnchecked(bmpAddress, bmpWidth, bx, by, 16, 16, 0xFFFF0000);
                }
            }
            OnDrew?.Invoke(this, EventArgs.Empty);
        }
        private unsafe void DrawAll()
        {
            const int bmpWidth = BitmapNumBlocksX * 16;
            int bmpHeight = GetBitmapHeight();
            using (ILockedFramebuffer l = Bitmap.Lock())
            {
                uint* bmpAddress = (uint*)l.Address.ToPointer();
                RenderUtil.Fill(bmpAddress, bmpWidth, bmpHeight, 0, 0, bmpWidth, bmpHeight, 0xFF000000);
                int x = 0;
                int y = 0;
                for (int i = 0; i < Blocks.Count; i++, x++)
                {
                    if (x >= BitmapNumBlocksX)
                    {
                        x = 0;
                        y++;
                    }
                    Blocks[i].Draw(bmpAddress, bmpWidth, bmpHeight, x * 16, y * 16);
                }
                for (; x < BitmapNumBlocksX; x++)
                {
                    RenderUtil.DrawCrossUnchecked(bmpAddress, bmpWidth, x * 16, y * 16, 16, 16, 0xFFFF0000);
                }
            }
            OnDrew?.Invoke(this, EventArgs.Empty);
        }

        public void Save()
        {
            using (var w = new EndianBinaryWriter(File.OpenWrite(Path.Combine(_blocksetPath, Name + _blocksetExtension))))
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
