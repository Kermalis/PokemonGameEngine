using Kermalis.MapEditor.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Kermalis.MapEditor.Core
{
    public sealed class Blockset
    {
        public sealed class Block
        {
            public sealed class Tile : IEquatable<Tile>, INotifyPropertyChanged
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
                public Tileset.Tile TilesetTile
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

                public void CopyTo(Tile other)
                {
                    other.XFlip = _xFlip;
                    other.YFlip = _yFlip;
                    other.TilesetTile = _tilesetTile;
                }

                public unsafe void Draw(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y)
                {
                    RenderUtil.Draw(bmpAddress, bmpWidth, bmpHeight, x, y, _tilesetTile.Colors, _xFlip, _yFlip);
                }

                public bool Equals(Tile other)
                {
                    return other != null && _xFlip == other._xFlip && _yFlip == other._yFlip && _tilesetTile == other._tilesetTile;
                }
            }

            public readonly Blockset Parent;
            public ReadOnlyDictionary<byte, List<Tile>> TopLeft;
            public ReadOnlyDictionary<byte, List<Tile>> TopRight;
            public ReadOnlyDictionary<byte, List<Tile>> BottomLeft;
            public ReadOnlyDictionary<byte, List<Tile>> BottomRight;
            public ushort Behavior;

            public Block(Blockset parent, Tileset.Tile defaultTile)
            {
                Parent = parent;
                TopLeft = Create(defaultTile);
                TopRight = Create(defaultTile);
                BottomLeft = Create(defaultTile);
                BottomRight = Create(defaultTile);
            }
            private ReadOnlyDictionary<byte, List<Tile>> Create(Tileset.Tile defaultTile)
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
                return new ReadOnlyDictionary<byte, List<Tile>>(d);
            }

            public unsafe void Draw(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y)
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
            public unsafe void DrawZ(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y, byte z)
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
        }

        public event EventHandler<bool> OnChanged;

        private readonly string _name;
        private int _numUses;
        public List<Block> Blocks;

        // TODO: Load from file
        private Blockset(string name, Tileset.Tile defaultTile)
        {
            _name = name;
            Blocks = new List<Block>() { new Block(this, defaultTile) };
        }

        private static readonly Dictionary<string, Blockset> _loadedBlocksets = new Dictionary<string, Blockset>();
        internal static Blockset LoadOrGet(string name, Tileset.Tile tempDefaultTile)
        {
            Blockset b;
            if (_loadedBlocksets.ContainsKey(name))
            {
                b = _loadedBlocksets[name];
            }
            else
            {
                b = new Blockset(name, tempDefaultTile);
                _loadedBlocksets.Add(name, b);
            }
            b._numUses++;
            return b;
        }
        internal void DeductReference()
        {
            _numUses--;
            if (_numUses <= 0)
            {
                _loadedBlocksets.Remove(_name);
            }
        }

        internal void FireChanged(bool collectionChanged)
        {
            OnChanged?.Invoke(this, collectionChanged);
        }
    }
}
