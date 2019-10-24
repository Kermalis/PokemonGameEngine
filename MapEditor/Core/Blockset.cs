using Kermalis.EndianBinaryIO;
using Kermalis.MapEditor.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

namespace Kermalis.MapEditor.Core
{
    public sealed class Blockset
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
            internal readonly int Id;
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
                    var d = new Dictionary<byte, List<Tile>>(byte.MaxValue + 1);
                    byte z = 0;
                    while (true)
                    {
                        byte count = r.ReadByte();
                        var list = new List<Tile>(count);
                        for (int i = 0; i < count; i++)
                        {
                            list.Add(new Tile(r));
                        }
                        d.Add(z, list);
                        if (z == byte.MaxValue)
                        {
                            break;
                        }
                        z++;
                    }
                    return d;
                }
                TopLeft = Read();
                TopRight = Read();
                BottomLeft = Read();
                BottomRight = Read();
                Parent = parent;
                Id = id;
            }
            internal Block(Blockset parent, int id, Tileset.Tile defaultTile)
            {
                Parent = parent;
                Id = id;
                Dictionary<byte, List<Tile>> Create()
                {
                    var d = new Dictionary<byte, List<Tile>>(byte.MaxValue + 1);
                    byte z = 0;
                    var l = new List<Tile>() { new Tile() { TilesetTile = defaultTile } };
                    while (true)
                    {
                        d.Add(z, l);
                        if (z == byte.MaxValue)
                        {
                            break;
                        }
                        z++;
                        l = new List<Tile>();
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
                void Draw(List<Tile> layers, int tx, int ty)
                {
                    for (int t = 0; t < layers.Count; t++)
                    {
                        layers[t].Draw(bmpAddress, bmpWidth, bmpHeight, tx, ty);
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
                void Write(Dictionary<byte, List<Tile>> dict)
                {
                    byte z = 0;
                    while (true)
                    {
                        List<Tile> list = dict[z];
                        byte count = (byte)list.Count;
                        w.Write(count);
                        for (int i = 0; i < count; i++)
                        {
                            list[i].Write(w);
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

        internal event EventHandler<bool> OnChanged;

        private static readonly IdList _ids = new IdList(Path.Combine(Program.AssetPath, "Blockset", "BlocksetIds.txt"));

        internal readonly string _name;
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
            _name = name;
            Id = id;
        }
        internal Blockset(string name, Tileset.Tile defaultTile)
        {
            Id = _ids.Add(name);
            _loadedBlocksets.Add(new WeakReference<Blockset>(this));
            Blocks = new List<Block>() { new Block(this, 0, defaultTile) };
            _name = name;
            Save();
            _ids.Save();
        }

        internal static bool IsValidName(string name)
        {
            return !string.IsNullOrWhiteSpace(name) && !Utils.InvalidFileNameRegex.IsMatch(name) && _ids[name] == -1;
        }

        private static readonly List<WeakReference<Blockset>> _loadedBlocksets = new List<WeakReference<Blockset>>();
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
            if (id >= _loadedBlocksets.Count)
            {
                b = new Blockset(name, id);
                _loadedBlocksets.Add(new WeakReference<Blockset>(b));
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

        internal void Add(Tileset.Tile defaultTile)
        {
            Blocks.Add(new Block(this, Blocks.Count, defaultTile));
            FireChanged(true);
        }
        internal static void Remove(Block block)
        {
            Blockset b = block.Parent;
            if (b != null)
            {
                b.Blocks.Remove(block);
                block.Parent = null;
                b.FireChanged(true);
            }
        }
        internal void FireChanged(bool collectionChanged)
        {
            OnChanged?.Invoke(this, collectionChanged);
        }

        internal void Save()
        {
            using (var w = new EndianBinaryWriter(File.OpenWrite(Path.Combine(Program.AssetPath, "Blockset", _name + ".pgeblockset"))))
            {
                ushort count = (ushort)Blocks.Count;
                w.Write(count);
                for (int i = 0; i < count; i++)
                {
                    Blocks[i].Write(w);
                }
            }
        }
    }
}
