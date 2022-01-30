using Kermalis.EndianBinaryIO;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Kermalis.PokemonGameEngine.World;
using Kermalis.PokemonGameEngine.World.Maps;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Render.World
{
    internal sealed partial class Blockset
    {
        public sealed partial class Block
        {
            public readonly Blockset Parent;
            private readonly List<Tile> _animatedTiles;
            public readonly List<MapLayout> LayoutsUsing;
            public int UsedBlocksIndex = USED_BLOCK_INDEX_NONE;

            public readonly BlocksetBlockBehavior Behavior;
            /// <summary>Elevation,Y,X,Sublayers</summary>
            private readonly Tile[][][][] _tiles;

            public Block(EndianBinaryReader r, Blockset parent)
            {
                Parent = parent;

                Behavior = r.ReadEnum<BlocksetBlockBehavior>();

                _tiles = new Tile[Overworld.NumElevations][][][];
                for (byte e = 0; e < Overworld.NumElevations; e++)
                {
                    _tiles[e] = LoadElevation(r, parent._usedTilesets, ref _animatedTiles);
                }

                LayoutsUsing = new List<MapLayout>();
            }
            private static Tile[][][] LoadElevation(EndianBinaryReader r, List<Tileset> usedTilesets, ref List<Tile> animTiles)
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
                                subLayers[i] = LoadTile(r, usedTilesets, ref animTiles);
                            }
                        }
                        arrY[x] = subLayers;
                    }
                    arrE[y] = arrY;
                }
                return arrE;
            }
            private static Tile LoadTile(EndianBinaryReader r, List<Tileset> usedTilesets, ref List<Tile> animTiles)
            {
                // Load data
                bool xFlip = r.ReadBoolean();
                bool yFlip = r.ReadBoolean();
                int tilesetId = r.ReadInt32();
                int tileId = r.ReadInt32();

                // Load tileset if it's not loaded already by this blockset
                Tileset tileset = usedTilesets.Find(t => t.Id == tilesetId);
                if (tileset is null)
                {
                    if (usedTilesets.Count >= GLTextureUtils.MAX_ACTIVE_TEXTURES)
                    {
                        throw new InvalidOperationException("This Blockset uses too many Tilesets. The limit is " + GLTextureUtils.MAX_ACTIVE_TEXTURES);
                    }
                    tileset = Tileset.LoadOrGet(tilesetId);
                    usedTilesets.Add(tileset);
                }

                Tileset.Tile TilesetTile = tileset.Tiles[tileId];
                if (TilesetTile is Tileset.AnimatedTile)
                {
                    var anim = new AnimatedTile(xFlip, yFlip, TilesetTile);
                    if (animTiles is null)
                    {
                        animTiles = new List<Tile>();
                    }
                    animTiles.Add(anim);
                    return anim;
                }
                return new Tile(xFlip, yFlip, TilesetTile);
            }

            public void AddReference(MapLayout layout)
            {
                if (LayoutsUsing.Count == 0)
                {
                    int firstEmpty = _usedBlocks.IndexOf(null);
                    if (firstEmpty == -1)
                    {
                        UsedBlocksIndex = _usedBlocks.Count;
                        _usedBlocks.Insert(UsedBlocksIndex, this);
                    }
                    else
                    {
                        UsedBlocksIndex = firstEmpty;
                        _usedBlocks[firstEmpty] = this;
                    }
                }
                LayoutsUsing.Add(layout);
                if (_animatedTiles is not null)
                {
                    Parent._animatedBlocks.Add(this);
                }
            }
            public void DeductReference(MapLayout layout)
            {
                LayoutsUsing.Remove(layout);
                if (_animatedTiles is not null)
                {
                    Parent._animatedBlocks.Remove(this);
                }
                if (LayoutsUsing.Count == 0)
                {
                    _usedBlocks[UsedBlocksIndex] = null;
                    UsedBlocksIndex = USED_BLOCK_INDEX_NONE;
                }
            }

            public bool IsAnimDirty()
            {
                for (int i = 0; i < _animatedTiles.Count; i++)
                {
                    if (((Tileset.AnimatedTile)_animatedTiles[i].TilesetTile).IsDirty)
                    {
                        return true;
                    }
                }
                return false;
            }
            public unsafe void Draw(TileVertexBuilder builder, byte elevation)
            {
                GL gl = Display.OpenGL;
                gl.Clear(ClearBufferMask.ColorBufferBit);

                Tile[][][] tilesE = _tiles[elevation];
                // Build block mesh
                var tilePixel = new Vec2I(0, 0);
                Vec2I tile;
                for (tile.Y = 0; tile.Y < Overworld.Block_NumTilesY; tile.Y++)
                {
                    Tile[][] tilesY = tilesE[tile.Y];

                    for (tile.X = 0; tile.X < Overworld.Block_NumTilesX; tile.X++)
                    {
                        Tile[] tilesX = tilesY[tile.X]; // Sublayers

                        for (int s = 0; s < tilesX.Length; s++)
                        {
                            Tile t = tilesX[s];

                            // Create vertices
                            var rect = Rect.FromSize(tilePixel, Overworld.Tile_NumPixels);
                            int tilesetId = Parent._usedTilesets.IndexOf(t.TilesetTile.Tileset);
                            builder.Add(rect, tilesetId, t.GetUV());

                            // Create vao and draw it, then delete it
                            builder.Finish(gl, out uint indexCount, out uint vao, out uint vbo, out uint ebo);
                            builder.Clear();

                            gl.DrawElements(PrimitiveType.Triangles, indexCount, DrawElementsType.UnsignedInt, null);

                            gl.DeleteVertexArray(vao);
                            gl.DeleteBuffer(vbo);
                            gl.DeleteBuffer(ebo);
                        }

                        tilePixel.X += Overworld.Tile_NumPixelsX;
                    }

                    tilePixel.X = 0;
                    tilePixel.Y += Overworld.Tile_NumPixelsY;
                }
            }
        }
    }
}
