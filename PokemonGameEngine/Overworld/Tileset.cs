using Kermalis.PokemonGameEngine.Util;

namespace Kermalis.PokemonGameEngine.Overworld
{
    internal sealed class Tileset
    {
        private readonly uint[][][] _tiles;

        public Tileset(string resource)
        {
            _tiles = RenderUtil.LoadSpriteSheet(resource, 8, 8);
        }

        public unsafe void DrawBlock(uint* bmpAddress, int bmpWidth, int bmpHeight, Block block, int x, int y)
        {
            for (byte z = 0; z < byte.MaxValue; z++)
            {
                void Draw(Block.Tile[] layers, int px, int py)
                {
                    for (int t = 0; t < layers.Length; t++)
                    {
                        Block.Tile tile = layers[t];
                        if (tile.ZLayer == z)
                        {
                            RenderUtil.Draw(bmpAddress, bmpWidth, bmpHeight, px, py, _tiles[tile.TileNum], tile.XFlip, tile.YFlip);
                        }
                    }
                }
                Draw(block.TopLeft, x, y);
                Draw(block.TopRight, x + 8, y);
                Draw(block.BottomLeft, x, y + 8);
                Draw(block.BottomRight, x + 8, y + 8);
            }
        }
    }
}
