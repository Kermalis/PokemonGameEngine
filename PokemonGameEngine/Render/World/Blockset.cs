using Kermalis.EndianBinaryIO;
using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.World;
using System;
using System.Collections.Generic;
using System.IO;
#if DEBUG_OVERWORLD
using Kermalis.PokemonGameEngine.Debug;
#endif

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
                // There are usually more blockset tiles than tileset tiles, so storing the tileset here would just waste memory
                public readonly Tileset.Tile TilesetTile;

                // Cache AtlasPos for less calculations
                public AtlasPos AtlasPos;
                private int _atlasPosId = TileAnimation.INVALID_ANIM_ID;

                public Tile(EndianBinaryReader r, List<Tileset> used)
                {
                    _xFlip = r.ReadBoolean();
                    _yFlip = r.ReadBoolean();
                    int tilesetId = r.ReadInt32();
                    int tileId = r.ReadInt32();

                    // Load tileset if it's not loaded already by this blockset
                    Tileset tileset = used.Find(t => t.Id == tilesetId);
                    if (tileset is null)
                    {
                        tileset = Tileset.LoadOrGet(tilesetId);
                        used.Add(tileset);
                    }
                    TilesetTile = tileset.Tiles[tileId];
                }

                private static AtlasPos MakeAtlasPos(Pos2D tilePixelPos, Size2D tilesetSize, bool xFlip, bool yFlip)
                {
                    return new AtlasPos(new Rect2D(tilePixelPos, new Size2D(Overworld.Tile_NumPixelsX, Overworld.Tile_NumPixelsY)), tilesetSize, xFlip: xFlip, yFlip: yFlip);
                }
                private static Pos2D GetPixelPos(int tileId, int numTilesX)
                {
                    return new Pos2D(tileId % numTilesX * Overworld.Tile_NumPixelsX, tileId / numTilesX * Overworld.Tile_NumPixelsY);
                }

                public void UpdateAtlasPos()
                {
                    int id;
                    if (TilesetTile is Tileset.AnimatedTile at && at.AnimId != TileAnimation.NO_ANIM_ID)
                    {
                        id = at.AnimId;
                    }
                    else
                    {
                        id = TilesetTile.Id;
                    }
                    Tileset tileset = TilesetTile.Tileset;
                    if (id != _atlasPosId)
                    {
                        Pos2D pp = GetPixelPos(id, tileset.NumTilesX);
                        AtlasPos = MakeAtlasPos(pp, tileset.TextureSize, _xFlip, _yFlip);
                        _atlasPosId = id;
                    }
                }
            }

            public readonly BlocksetBlockBehavior Behavior;
            /// <summary>Elevation,Y,X,Sublayers</summary>
            public readonly Tile[][][][] Tiles;

            public Block(Blockset parent, EndianBinaryReader r)
            {
                Behavior = r.ReadEnum<BlocksetBlockBehavior>();

                Tiles = new Tile[Overworld.NumElevations][][][];
                for (byte e = 0; e < Overworld.NumElevations; e++)
                {
                    var arrE = new Tile[Overworld.Block_NumTilesY][][];
                    for (int y = 0; y < Overworld.Block_NumTilesY; y++)
                    {
                        var arrY = new Tile[Overworld.Block_NumTilesX][];
                        for (int x = 0; x < Overworld.Block_NumTilesX; x++)
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
                                    subLayers[i] = new Tile(r, parent._usedTilesets);
                                }
                            }
                            arrY[x] = subLayers;
                        }
                        arrE[y] = arrY;
                    }
                    Tiles[e] = arrE;
                }
            }
        }

        public readonly Block[] Blocks;
        /// <summary>Keeps track of which tilesets are loaded so they can be unloaded later.</summary>
        private readonly List<Tileset> _usedTilesets;

        private Blockset(int id, string name)
        {
#if DEBUG_OVERWORLD
            Log.WriteLine("Loading blockset: " + name);
            Log.ModifyIndent(+1);
#endif
            using (var r = new EndianBinaryReader(AssetLoader.GetAssetStream(BlocksetPath + name + BlocksetExtension)))
            {
                ushort count = r.ReadUInt16();
                if (count == 0)
                {
                    throw new InvalidDataException();
                }

                _usedTilesets = new List<Tileset>();
                Blocks = new Block[count];
                for (int i = 0; i < count; i++)
                {
                    Blocks[i] = new Block(this, r);
                }
            }

            Id = id;
            _numReferences = 1;
            _loadedBlocksets.Add(id, this);

#if DEBUG_OVERWORLD
            Log.ModifyIndent(-1);
#endif
        }

        #region Cache

        public readonly int Id;
        private int _numReferences;

        private const string BlocksetExtension = ".pgeblockset";
        private const string BlocksetPath = "Blockset\\";
        private static readonly IdList _ids = new(BlocksetPath + "BlocksetIds.txt");
        private static readonly Dictionary<int, Blockset> _loadedBlocksets = new();
        public static Blockset LoadOrGet(int id)
        {
            if (_loadedBlocksets.TryGetValue(id, out Blockset b))
            {
                b._numReferences++;
#if DEBUG_OVERWORLD
                Log.WriteLine("Adding reference to blockset: " + _ids[b.Id] + " (new count is " + b._numReferences + ")");
#endif
            }
            else
            {
                string name = _ids[id];
                if (name is null)
                {
                    throw new ArgumentOutOfRangeException(nameof(id));
                }
                b = new Blockset(id, name);
            }
            return b;
        }

        public void DeductReference()
        {
            if (--_numReferences > 0)
            {
#if DEBUG_OVERWORLD
                Log.WriteLine("Removing reference from blockset: " + _ids[Id] + " (new count is " + _numReferences + ")");
#endif
                return;
            }

#if DEBUG_OVERWORLD
            Log.WriteLine("Unloading blockset: " + _ids[Id]);
            Log.ModifyIndent(+1);
#endif
            foreach (Tileset t in _usedTilesets)
            {
                t.DeductReference();
            }
            _loadedBlocksets.Remove(Id);
#if DEBUG_OVERWORLD
            Log.ModifyIndent(-1);
#endif
        }

        #endregion
    }
}
