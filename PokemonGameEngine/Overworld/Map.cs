using Kermalis.EndianBinaryIO;
using Kermalis.PokemonGameEngine.Util;
using System;
using System.Collections.Generic;
using System.IO;

namespace Kermalis.PokemonGameEngine.Overworld
{
    internal sealed class Map
    {
        private sealed class Block
        {
            public readonly byte Behavior;
            public readonly Blockset.Block BlocksetBlock;

            public Block(EndianBinaryReader r)
            {
                Behavior = r.ReadByte();
                BlocksetBlock = Blockset.LoadOrGet(r.ReadInt32()).Blocks[r.ReadInt32()];
            }
        }

        private readonly int _blocksWidth;
        private readonly int _blocksHeight;
        private readonly byte _borderWidth;
        private readonly byte _borderHeight;

        private readonly Block[][] _blocks;
        private readonly Blockset.Block[][] _borderBlocks;

        public readonly List<CharacterObj> Characters = new List<CharacterObj>();

        public Map(string name)
        {
            using (var r = new EndianBinaryReader(Utils.GetResourceStream("Map." + name + ".pgemap")))
            {
                _blocksWidth = r.ReadInt32();
                if (_blocksWidth <= 0)
                {
                    throw new InvalidDataException();
                }
                _blocksHeight = r.ReadInt32();
                if (_blocksHeight <= 0)
                {
                    throw new InvalidDataException();
                }
                _blocks = new Block[_blocksHeight][];
                for (int y = 0; y < _blocksHeight; y++)
                {
                    var arrY = new Block[_blocksWidth];
                    for (int x = 0; x < _blocksWidth; x++)
                    {
                        arrY[x] = new Block(r);
                    }
                    _blocks[y] = arrY;
                }
                _borderWidth = r.ReadByte();
                _borderHeight = r.ReadByte();
                if (_borderHeight == 0)
                {
                    _borderBlocks = Array.Empty<Blockset.Block[]>();
                }
                else
                {
                    _borderBlocks = new Blockset.Block[_borderHeight][];
                    for (int y = 0; y < _borderHeight; y++)
                    {
                        Blockset.Block[] arrY;
                        if (_borderWidth == 0)
                        {
                            arrY = Array.Empty<Blockset.Block>();
                        }
                        else
                        {
                            arrY = new Blockset.Block[_borderWidth];
                            for (int x = 0; x < _borderWidth; x++)
                            {
                                arrY[x] = Blockset.LoadOrGet(r.ReadInt32()).Blocks[r.ReadInt32()];
                            }
                        }
                        _borderBlocks[y] = arrY;
                    }
                }
            }
        }

        private Blockset.Block GetBlock(int x, int y)
        {
            bool north = y < 0;
            bool south = y >= _blocksHeight;
            bool west = x < 0;
            bool east = x >= _blocksWidth;
            if (!north && !south && !west && !east)
            {
                return _blocks[y][x].BlocksetBlock;
            }
            else
            {
                // TODO: Connections
                if (_borderWidth == 0 || _borderHeight == 0)
                {
                    return null;
                }
                else
                {
                    x %= _borderWidth;
                    if (west)
                    {
                        x *= -1;
                    }
                    y %= _borderHeight;
                    if (north)
                    {
                        y *= -1;
                    }
                    return _borderBlocks[y][x];
                }
            }
        }
        public static unsafe void Draw(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            Obj camera = Obj.Camera;
            int cameraX = (camera.X * 16) - (bmpWidth / 2) + 8;
            int cameraY = (camera.Y * 16) - (bmpHeight / 2) + 8;
            Map cameraMap = camera.Map;
            int xp16 = cameraX % 16;
            int yp16 = cameraY % 16;
            int startBlockX = (cameraX / 16) - (xp16 >= 0 ? 0 : 1);
            int startBlockY = (cameraY / 16) - (yp16 >= 0 ? 0 : 1);
            int numBlocksX = (bmpWidth / 16) + (bmpWidth % 16 == 0 ? 0 : 1);
            int numBlocksY = (bmpHeight / 16) + (bmpHeight % 16 == 0 ? 0 : 1);
            int endBlockX = startBlockX + numBlocksX + (xp16 == 0 ? 0 : 1);
            int endBlockY = startBlockY + numBlocksY + (yp16 == 0 ? 0 : 1);
            int startX = xp16 >= 0 ? -xp16 : -xp16 - 16;
            int startY = yp16 >= 0 ? -yp16 : -yp16 - 16;
            byte z = 0;
            while (true)
            {
                int curX = startX;
                int curY = startY;
                for (int blockY = startBlockY; blockY < endBlockY; blockY++)
                {
                    for (int blockX = startBlockX; blockX < endBlockX; blockX++)
                    {
                        Blockset.Block b = cameraMap.GetBlock(blockX, blockY);
                        if (b != null)
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
                        }
                        curX += 16;
                    }
                    curX = startX;
                    curY += 16;
                }
                // TODO: They will overlap each other regardless of y coordinate because of the order of the list
                // TODO: Characters from other maps
                for (int i = 0; i < cameraMap.Characters.Count; i++)
                {
                    CharacterObj c = cameraMap.Characters[i];
                    if (c.Z == z)
                    {
                        int objX = ((c.X - startBlockX) * 16) + startX;
                        int objY = ((c.Y - startBlockY) * 16) + startY;
                        int objW = c.SpriteWidth;
                        int objH = c.SpriteHeight;
                        objX -= (objW - 16) / 2;
                        objY -= objH - 16;
                        if (objX < bmpWidth && objX + objW > 0 && objY < bmpHeight && objY + objH > 0)
                        {
                            c.Draw(bmpAddress, bmpWidth, bmpHeight, objX, objY);
                        }
                    }
                }
                if (z == byte.MaxValue)
                {
                    break;
                }
                z++;
            }
        }
    }
}
