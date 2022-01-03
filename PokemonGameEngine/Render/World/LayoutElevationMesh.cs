using Kermalis.PokemonGameEngine.Render.OpenGL;
using Kermalis.PokemonGameEngine.World;
using Kermalis.PokemonGameEngine.World.Maps;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Render.World
{
    internal sealed class LayoutElevationMesh
    {
        private readonly uint _vao;
        private readonly uint _vbo;
        private readonly uint _ebo;
        private readonly uint _indexCount;

        public LayoutElevationMesh(MapLayout lay, byte elevation, List<Tileset> usedTilesets)
        {
            GL gl = Display.OpenGL;
            var builder = new TileVertexBuilder();
            Pos2D block;
            for (block.Y = 0; block.Y < lay.BlocksHeight; block.Y++)
            {
                Pos2D blockPixel;
                blockPixel.Y = block.Y * Overworld.Block_NumPixelsY;
                MapLayout.Block[] blocksY = lay.Blocks[block.Y];
                for (block.X = 0; block.X < lay.BlocksWidth; block.X++)
                {
                    blockPixel.X = block.X * Overworld.Block_NumPixelsX;
                    Blockset.Block b = blocksY[block.X].BlocksetBlock;
                    AddBlock(b, blockPixel, elevation, usedTilesets, builder);
                }
            }
            // Done
            builder.Finish(gl, out _indexCount, out _vao, out _vbo, out _ebo);
        }
        private static void AddBlock(Blockset.Block b, Pos2D blockPixel, byte elevation, List<Tileset> usedTilesets, TileVertexBuilder builder)
        {
            Blockset.Block.Tile[][][] tilesE = b.Tiles[elevation];
            Pos2D tile;
            for (tile.Y = 0; tile.Y < Overworld.Block_NumTilesY; tile.Y++)
            {
                Pos2D tilePixel;
                tilePixel.Y = blockPixel.Y + (tile.Y * Overworld.Tile_NumPixelsY);
                Blockset.Block.Tile[][] tilesY = tilesE[tile.Y];
                for (tile.X = 0; tile.X < Overworld.Block_NumTilesX; tile.X++)
                {
                    tilePixel.X = blockPixel.X + (tile.X * Overworld.Tile_NumPixelsX);
                    Blockset.Block.Tile[] tilesX = tilesY[tile.X]; // Sublayers
                    for (int s = 0; s < tilesX.Length; s++)
                    {
                        // Add each tile to the builder
                        Blockset.Block.Tile t = tilesX[s];
                        // Add tileset to the list if it doesn't exist
                        Tileset tileset = t.TilesetTile.Tileset;
                        int tilesetIndex = usedTilesets.IndexOf(tileset);
                        if (tilesetIndex == -1)
                        {
                            tilesetIndex = usedTilesets.Count;
                            if (tilesetIndex >= GLTextureUtils.MAX_ACTIVE_TEXTURES)
                            {
                                throw new InvalidOperationException("This map layout uses too many tilesets. The limit is " + GLTextureUtils.MAX_ACTIVE_TEXTURES);
                            }
                            usedTilesets.Insert(tilesetIndex, tileset);
                        }
                        // Create vertices
                        var rect = new Rect2D(tilePixel, new Size2D(Overworld.Tile_NumPixelsX, Overworld.Tile_NumPixelsY));
                        t.UpdateAtlasPos();
                        builder.Add(rect, tilesetIndex, t.AtlasPos);
                    }
                }
            }
        }

        public unsafe void Render(GL gl)
        {
            gl.BindVertexArray(_vao);
            gl.DrawElements(PrimitiveType.Triangles, _indexCount, DrawElementsType.UnsignedInt, null);
        }

        public void Delete()
        {
            GL gl = Display.OpenGL;
            gl.DeleteVertexArray(_vao);
            gl.DeleteBuffer(_vbo);
            gl.DeleteBuffer(_ebo);
        }
    }
}
