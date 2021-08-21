using Kermalis.EndianBinaryIO;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.World;
using System;
using System.Collections.Generic;
using System.IO;

namespace Kermalis.PokemonGameEngine.Render.World
{
    internal sealed class Blockset
    {
        public sealed class Block
        {
            public sealed class Tile
            {
                private readonly bool _xFlip;
                private readonly bool _yFlip;
                private readonly Tileset.Tile _tilesetTile;

                public Tile(EndianBinaryReader r)
                {
                    _xFlip = r.ReadBoolean();
                    _yFlip = r.ReadBoolean();
                    _tilesetTile = Tileset.LoadOrGet(r.ReadInt32()).Tiles[r.ReadInt32()];
                }

                public void Render(Pos2D pos)
                {
                    uint tex = _tilesetTile.AnimBitmap;
                    if (tex == 0)
                    {
                        tex = _tilesetTile.Bitmap;
                    }
                    GUIRenderer.Instance.RenderTexture(tex, new Rect2D(pos, new Size2D(Overworld.Tile_NumPixelsX, Overworld.Tile_NumPixelsY)), xFlip: _xFlip, yFlip: _yFlip);
                }
            }

            /// <summary>Unused, but needed to keep the Blockset reference alive (since Blocksets aren't stored anywhere, while Blocks are stored by the MapLayout blocks)</summary>
            public readonly Blockset Parent;
            public readonly BlocksetBlockBehavior Behavior;
            private readonly Tile[][][][] _tiles; // Elevation,Y,X,Sublayers

            public Block(Blockset parent, EndianBinaryReader r)
            {
                Behavior = r.ReadEnum<BlocksetBlockBehavior>();
                Tile[] Read()
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
                    return subLayers;
                }
                _tiles = new Tile[Overworld.NumElevations][][][];
                for (byte e = 0; e < Overworld.NumElevations; e++)
                {
                    var arrE = new Tile[Overworld.Block_NumTilesY][][];
                    for (int y = 0; y < Overworld.Block_NumTilesY; y++)
                    {
                        var arrY = new Tile[Overworld.Block_NumTilesX][];
                        for (int x = 0; x < Overworld.Block_NumTilesX; x++)
                        {
                            arrY[x] = Read();
                        }
                        arrE[y] = arrY;
                    }
                    _tiles[e] = arrE;
                }
                Parent = parent;
            }

            public void Render(byte elevation, int x, int y)
            {
                Tile[][][] arrE = _tiles[elevation];
                for (int by = 0; by < Overworld.Block_NumTilesY; by++)
                {
                    Tile[][] arrY = arrE[by];
                    int ty = y + (by * Overworld.Tile_NumPixelsY);
                    for (int bx = 0; bx < Overworld.Block_NumTilesX; bx++)
                    {
                        Tile[] subLayers = arrY[bx];
                        int tx = x + (bx * Overworld.Tile_NumPixelsX);
                        for (int t = 0; t < subLayers.Length; t++)
                        {
                            subLayers[t].Render(new Pos2D(tx, ty));
                        }
                    }
                }
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
        private static readonly IdList _ids = new(BlocksetPath + "BlocksetIds.txt");
        private static readonly Dictionary<int, WeakReference<Blockset>> _loadedBlocksets = new();
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
