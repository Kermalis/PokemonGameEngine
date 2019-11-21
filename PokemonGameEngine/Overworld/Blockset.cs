using Kermalis.EndianBinaryIO;
using Kermalis.PokemonGameEngine.Util;
using System;
using System.Collections.Generic;
using System.IO;

namespace Kermalis.PokemonGameEngine.Overworld
{
    internal sealed class Blockset
    {
        public sealed class Block
        {
            public sealed class Tile
            {
                public readonly bool XFlip;
                public readonly bool YFlip;
                public readonly Tileset.Tile TilesetTile;

                public Tile(EndianBinaryReader r)
                {
                    XFlip = r.ReadBoolean();
                    YFlip = r.ReadBoolean();
                    TilesetTile = Tileset.LoadOrGet(r.ReadInt32()).Tiles[r.ReadInt32()];
                }
            }

            public readonly Blockset Parent;
            public readonly Dictionary<byte, Tile[]> TopLeft;
            public readonly Dictionary<byte, Tile[]> TopRight;
            public readonly Dictionary<byte, Tile[]> BottomLeft;
            public readonly Dictionary<byte, Tile[]> BottomRight;
            public readonly ushort Behavior;

            public Block(Blockset parent, EndianBinaryReader r)
            {
                Behavior = r.ReadUInt16();
                Dictionary<byte, Tile[]> Read()
                {
                    var eLayers = new Dictionary<byte, Tile[]>(byte.MaxValue + 1);
                    byte e = 0;
                    while (true)
                    {
                        byte count = r.ReadByte();
                        Tile[] subLayers;
                        if (count == 0)
                        {
                            subLayers = Array.Empty<Tile>();
                        }
                        else
                        {
                            subLayers = new Tile[count];
                            for (int i = 0; i < count; i++)
                            {
                                subLayers[i] = new Tile(r);
                            }
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
            }
        }

        public readonly Block[] Blocks;

        private Blockset(string name)
        {
            using (var r = new EndianBinaryReader(Utils.GetResourceStream(_blocksetPath + name + _blocksetExtension)))
            {
                ushort count = r.ReadUInt16();
                if (count == 0)
                {
                    throw new InvalidDataException();
                }
                Blocks = new Block[count];
                for (int i = 0; i < count; i++)
                {
                    Blocks[i] = new Block(this, r);
                }
            }
        }

        private const string _blocksetExtension = ".pgeblockset";
        private const string _blocksetPath = "Blockset.";
        private static readonly IdList _ids = new IdList(_blocksetPath + "BlocksetIds.txt");
        private static readonly Dictionary<int, WeakReference<Blockset>> _loadedBlocksets = new Dictionary<int, WeakReference<Blockset>>();
        public static Blockset LoadOrGet(int id)
        {
            string name = _ids[id];
            if (name == null)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }
            Blockset b;
            if (!_loadedBlocksets.ContainsKey(id))
            {
                b = new Blockset(name);
                _loadedBlocksets.Add(id, new WeakReference<Blockset>(b));
                return b;
            }
            if (_loadedBlocksets[id].TryGetTarget(out b))
            {
                return b;
            }
            b = new Blockset(name);
            _loadedBlocksets[id].SetTarget(b);
            return b;
        }
    }
}
