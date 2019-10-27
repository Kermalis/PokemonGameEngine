using Kermalis.EndianBinaryIO;
using Kermalis.PokemonGameEngine.Util;
using System.IO;

namespace Kermalis.PokemonGameEngine.Overworld
{
    internal sealed class Map
    {
        public sealed class Block
        {
            public readonly byte Behavior;
            public readonly Blockset.Block BlocksetBlock;

            public Block(EndianBinaryReader r)
            {
                Behavior = r.ReadByte();
                BlocksetBlock = Blockset.LoadOrGet(r.ReadInt32()).Blocks[r.ReadInt32()];
            }
        }

        private readonly int _width;
        private readonly int _height;

        private readonly Block[][] _blocks;

        public Map(string name)
        {
            using (var r = new EndianBinaryReader(Utils.GetResourceStream("Map." + name + ".pgemap")))
            {
                _width = r.ReadInt32();
                if (_width <= 0)
                {
                    throw new InvalidDataException();
                }
                _height = r.ReadInt32();
                if (_height <= 0)
                {
                    throw new InvalidDataException();
                }
                _blocks = new Block[_height][];
                for (int y = 0; y < _height; y++)
                {
                    var arrY = new Block[_width];
                    for (int x = 0; x < _width; x++)
                    {
                        arrY[x] = new Block(r);
                    }
                    _blocks[y] = arrY;
                }
            }
        }

        public unsafe void Draw(uint* bmpAddress, int bmpWidth, int bmpHeight, int x, int y)
        {
            int xp16 = x % 16;
            int yp16 = y % 16;
            int startBlockX = (x / 16) - (xp16 >= 0 ? 0 : 1);
            int startBlockY = (y / 16) - (yp16 >= 0 ? 0 : 1);
            int numBlocksX = (bmpWidth / 16) + (bmpWidth % 16 == 0 ? 0 : 1);
            int numBlocksY = (bmpHeight / 16) + (bmpHeight % 16 == 0 ? 0 : 1);
            int endBlockX = startBlockX + numBlocksX + (xp16 == 0 ? 0 : 1);
            int endBlockY = startBlockY + numBlocksY + (yp16 == 0 ? 0 : 1);
            int startX = xp16 >= 0 ? -xp16 : -xp16 - 16;
            int curX = startX;
            int curY = yp16 >= 0 ? -yp16 : -yp16 - 16;
            for (int blockY = startBlockY; blockY < endBlockY; blockY++)
            {
                for (int blockX = startBlockX; blockX < endBlockX; blockX++)
                {
                    if (blockY >= 0 && blockY < _height && blockX >= 0 && blockX < _width)
                    {
                        Blockset.Block b = _blocks[blockY][blockX].BlocksetBlock;
                        byte z = 0;
                        while (true)
                        {
                            void Draw(Blockset.Block.Tile[] subLayers, int tx, int ty)
                            {
                                for (int t = 0; t < subLayers.Length; t++)
                                {
                                    Blockset.Block.Tile tile = subLayers[t];
                                    RenderUtil.Draw(bmpAddress, bmpWidth, bmpHeight, tx, ty, tile.TilesetTile.Colors, tile.XFlip, tile.YFlip);
                                }
                            }
                            Draw(b.TopLeft[z], curX, curY);
                            Draw(b.TopRight[z], curX + 8, curY);
                            Draw(b.BottomLeft[z], curX, curY + 8);
                            Draw(b.BottomRight[z], curX + 8, curY + 8);
                            if (z == byte.MaxValue)
                            {
                                break;
                            }
                            z++;
                        }
                    }
                    curX += 16;
                }
                curX = startX;
                curY += 16;
            }
        }
    }
}
