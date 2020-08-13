using Kermalis.EndianBinaryIO;
using Kermalis.PokemonGameEngine.Util;
using System;
using System.Collections.Generic;
using System.IO;

namespace Kermalis.PokemonGameEngine.World
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
            public readonly BlocksetBlockBehavior Behavior;
            public readonly Tile[][][][] Tiles; // Y,X,Elevation,Sublayers

            public Block(Blockset parent, EndianBinaryReader r)
            {
                Behavior = r.ReadEnum<BlocksetBlockBehavior>();
                Tile[][] Read()
                {
                    var eLayers = new Tile[Overworld.NumElevations][];
                    for (byte e = 0; e < Overworld.NumElevations; e++)
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
                        eLayers[e] = subLayers;
                    }
                    return eLayers;
                }
                Tiles = new Tile[Overworld.Block_NumTilesY][][][];
                for (int y = 0; y < Overworld.Block_NumTilesY; y++)
                {
                    var arrY = new Tile[Overworld.Block_NumTilesX][][];
                    for (int x = 0; x < Overworld.Block_NumTilesX; x++)
                    {
                        arrY[x] = Read();
                    }
                    Tiles[y] = arrY;
                }
                Parent = parent;
            }
        }

        public readonly Block[] Blocks;

        private Blockset(string name)
        {
            using (var r = new EndianBinaryReader(Utils.GetResourceStream(BlocksetPath + name + BlocksetExtension)))
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

        private const string BlocksetExtension = ".pgeblockset";
        private const string BlocksetPath = "Blockset.";
        private static readonly IdList _ids = new IdList(BlocksetPath + "BlocksetIds.txt");
        private static readonly Dictionary<int, WeakReference<Blockset>> _loadedBlocksets = new Dictionary<int, WeakReference<Blockset>>();
        public static Blockset LoadOrGet(int id)
        {
            string name = _ids[id];
            if (name is null)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }
            Blockset b;
            if (!_loadedBlocksets.TryGetValue(id, out WeakReference<Blockset> w))
            {
                b = new Blockset(name);
                _loadedBlocksets.Add(id, new WeakReference<Blockset>(b));
            }
            else if (!w.TryGetTarget(out b))
            {
                b = new Blockset(name);
                w.SetTarget(b);
            }
            return b;
        }
    }
}
