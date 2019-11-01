using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Kermalis.EndianBinaryIO;
using Kermalis.MapEditor.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

namespace Kermalis.MapEditor.Core
{
    public sealed class Blockset : IDisposable
    {
        public sealed class Block
        {
            public sealed class Tile : INotifyPropertyChanged
            {
                private void OnPropertyChanged(string property)
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
                }
                public event PropertyChangedEventHandler PropertyChanged;

                private bool _xFlip;
                public bool XFlip
                {
                    get => _xFlip;
                    set
                    {
                        if (_xFlip != value)
                        {
                            _xFlip = value;
                            OnPropertyChanged(nameof(XFlip));
                        }
                    }
                }
                private bool _yFlip;
                public bool YFlip
                {
                    get => _yFlip;
                    set
                    {
                        if (_yFlip != value)
                        {
                            _yFlip = value;
                            OnPropertyChanged(nameof(YFlip));
                        }
                    }
                }
                private Tileset.Tile _tilesetTile;
                internal Tileset.Tile TilesetTile
                {
                    get => _tilesetTile;
                    set
                    {
                        if (_tilesetTile != value)
                        {
                            _tilesetTile = value;
                            OnPropertyChanged(nameof(TilesetTile));
                        }
                    }
                }

                internal Tile() { }
                internal Tile(EndianBinaryReader r)
                {
                    _xFlip = r.ReadBoolean();
                    _yFlip = r.ReadBoolean();
                    _tilesetTile = Tileset.LoadOrGet(r.ReadInt32()).Tiles[r.ReadInt32()];
                }

                internal void CopyTo(Tile other)
                {
                    other.XFlip = _xFlip;
                    other.YFlip = _yFlip;
                    other.TilesetTile = _tilesetTile;
                }

                internal unsafe void Draw(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y)
                {
                    RenderUtil.Draw(bmpAddress, bmpWidth, bmpHeight, x, y, _tilesetTile.Colors, _xFlip, _yFlip);
                }

                internal bool Equals(Tile other)
                {
                    return other != null && _xFlip == other._xFlip && _yFlip == other._yFlip && _tilesetTile == other._tilesetTile;
                }

                internal void Write(EndianBinaryWriter w)
                {
                    w.Write(_xFlip);
                    w.Write(_yFlip);
                    w.Write(_tilesetTile.Parent.Id);
                    w.Write(_tilesetTile.Id);
                }
            }

            internal Blockset Parent;
            internal int Id;
            internal readonly Dictionary<byte, List<Tile>> TopLeft;
            internal readonly Dictionary<byte, List<Tile>> TopRight;
            internal readonly Dictionary<byte, List<Tile>> BottomLeft;
            internal readonly Dictionary<byte, List<Tile>> BottomRight;
            internal ushort Behavior;

            internal Block(Blockset parent, int id, EndianBinaryReader r)
            {
                Behavior = r.ReadUInt16();
                Dictionary<byte, List<Tile>> Read()
                {
                    var zLayers = new Dictionary<byte, List<Tile>>(byte.MaxValue + 1);
                    byte z = 0;
                    while (true)
                    {
                        byte count = r.ReadByte();
                        var subLayers = new List<Tile>(count);
                        for (int i = 0; i < count; i++)
                        {
                            subLayers.Add(new Tile(r));
                        }
                        zLayers.Add(z, subLayers);
                        if (z == byte.MaxValue)
                        {
                            break;
                        }
                        z++;
                    }
                    return zLayers;
                }
                TopLeft = Read();
                TopRight = Read();
                BottomLeft = Read();
                BottomRight = Read();
                Parent = parent;
                Id = id;
            }
            internal Block(Blockset parent, int id)
            {
                Parent = parent;
                Id = id;
                Dictionary<byte, List<Tile>> Create()
                {
                    var d = new Dictionary<byte, List<Tile>>(byte.MaxValue + 1);
                    byte z = 0;
                    while (true)
                    {
                        d.Add(z, new List<Tile>());
                        if (z == byte.MaxValue)
                        {
                            break;
                        }
                        z++;
                    }
                    return d;
                }
                TopLeft = Create();
                TopRight = Create();
                BottomLeft = Create();
                BottomRight = Create();
            }

            internal unsafe void Draw(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y)
            {
                byte z = 0;
                while (true)
                {
                    DrawZ(bmpAddress, bmpWidth, bmpHeight, x, y, z);
                    if (z == byte.MaxValue)
                    {
                        break;
                    }
                    z++;
                }
            }
            internal unsafe void DrawZ(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y, byte z)
            {
                void Draw(List<Tile> subLayers, int tx, int ty)
                {
                    for (int t = 0; t < subLayers.Count; t++)
                    {
                        subLayers[t].Draw(bmpAddress, bmpWidth, bmpHeight, tx, ty);
                    }
                }
                Draw(TopLeft[z], x, y);
                Draw(TopRight[z], x + 8, y);
                Draw(BottomLeft[z], x, y + 8);
                Draw(BottomRight[z], x + 8, y + 8);
            }

            internal void Write(EndianBinaryWriter w)
            {
                w.Write(Behavior);
                void Write(Dictionary<byte, List<Tile>> zLayers)
                {
                    byte z = 0;
                    while (true)
                    {
                        List<Tile> subLayers = zLayers[z];
                        byte count = (byte)subLayers.Count;
                        w.Write(count);
                        for (int i = 0; i < count; i++)
                        {
                            subLayers[i].Write(w);
                        }
                        if (z == byte.MaxValue)
                        {
                            break;
                        }
                        z++;
                    }
                }
                Write(TopLeft);
                Write(TopRight);
                Write(BottomLeft);
                Write(BottomRight);
            }
        }

        internal delegate void BlocksetEventHandler(Blockset blockset, Block block);
        internal event BlocksetEventHandler OnAdded;
        internal event BlocksetEventHandler OnChanged;
        internal event BlocksetEventHandler OnRemoved;

        public const int BitmapNumBlocksX = 8;
        internal WriteableBitmap Bitmap;
        internal event EventHandler<EventArgs> OnDrew;

        internal readonly string Name;
        internal readonly int Id;
        internal List<Block> Blocks;

        private Blockset(string name, int id)
        {
            using (var r = new EndianBinaryReader(File.OpenRead(Path.Combine(Program.AssetPath, "Blockset", name + ".pgeblockset"))))
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
            Id = _ids.Add(name);
            _loadedBlocksets.Add(Id, new WeakReference<Blockset>(this));
            Blocks = new List<Block>() { new Block(this, 0) };
            Name = name;
            Save();
            _ids.Save();
            UpdateBitmapSize();
            DrawAll();
        }
        ~Blockset()
        {
            Dispose(false);
        }

        internal static bool IsValidName(string name)
        {
            return !string.IsNullOrWhiteSpace(name) && !Utils.InvalidFileNameRegex.IsMatch(name) && _ids[name] == -1;
        }

        private static readonly IdList _ids = new IdList(Path.Combine(Program.AssetPath, "Blockset", "BlocksetIds.txt"));
        private static readonly Dictionary<int, WeakReference<Blockset>> _loadedBlocksets = new Dictionary<int, WeakReference<Blockset>>();
        public static Blockset LoadOrGet(string name)
        {
            int id = _ids[name];
            if (id == -1)
            {
                throw new ArgumentOutOfRangeException(nameof(name));
            }
            return LoadOrGet(name, id);
        }
        public static Blockset LoadOrGet(int id)
        {
            string name = _ids[id];
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
            byte z = 0;
            while (true)
            {
                block.TopLeft[z].Clear();
                block.TopRight[z].Clear();
                block.BottomLeft[z].Clear();
                block.BottomRight[z].Clear();
                if (z == byte.MaxValue)
                {
                    break;
                }
                z++;
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

        internal void Save()
        {
            using (var w = new EndianBinaryWriter(File.OpenWrite(Path.Combine(Program.AssetPath, "Blockset", Name + ".pgeblockset"))))
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
