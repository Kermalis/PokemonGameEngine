using Kermalis.PokemonGameEngine.Render;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.World.Objs
{
    internal class CameraObj : Obj
    {
        public static readonly CameraObj Camera = new CameraObj();
        public static int CameraOfsX;
        public static int CameraOfsY;
        public static Obj CameraAttachedTo = PlayerObj.Player;

        private CameraObj()
            : base(Overworld.CameraId)
        {
        }

        public static void CameraCopyMovement()
        {
            CameraObj c = Camera;
            Obj other = CameraAttachedTo;
            c.IsMovingSelf = other.IsMovingSelf;
            c.IsScriptMoving = other.IsScriptMoving;
            c.MovementTimer = other.MovementTimer;
            c.MovementSpeed = other.MovementSpeed;
            c.Pos = other.Pos;
            c.PrevPos = other.PrevPos;
            c.ProgressX = other.ProgressX;
            c.ProgressY = other.ProgressY;
            c.UpdateMap(other.Map);
        }

        protected override void UpdateMap(Map newMap)
        {
            Map curMap = Map;
            if (curMap != newMap)
            {
                curMap.UnloadObjEvents();
                newMap.LoadObjEvents();
                newMap.Objs.Add(this);
                Map = newMap;
            }
        }
        public override bool CollidesWithOthers()
        {
            return false;
        }

        public static unsafe void Render(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            // Gather variables we need to draw everything at the right coordinates
            CameraObj camera = Camera;
            Position cameraPos = camera.Pos;
            int cameraPixelX = (cameraPos.X * Overworld.Block_NumPixelsX) - (bmpWidth / 2) + (Overworld.Block_NumPixelsX / 2) + camera.ProgressX + CameraOfsX;
            int cameraPixelY = (cameraPos.Y * Overworld.Block_NumPixelsY) - (bmpHeight / 2) + (Overworld.Block_NumPixelsY / 2) + camera.ProgressY + CameraOfsY;
            Map cameraMap = camera.Map;
            List<Obj> objs = cameraMap.Objs;
            int numObjs = objs.Count;
            int xpBX = cameraPixelX % Overworld.Block_NumPixelsX;
            int ypBY = cameraPixelY % Overworld.Block_NumPixelsY;
            int startBlockX = (cameraPixelX / Overworld.Block_NumPixelsX) - (xpBX >= 0 ? 0 : 1);
            int startBlockY = (cameraPixelY / Overworld.Block_NumPixelsY) - (ypBY >= 0 ? 0 : 1);
            int numBlocksX = (bmpWidth / Overworld.Block_NumPixelsX) + (bmpWidth % Overworld.Block_NumPixelsX == 0 ? 0 : 1);
            int numBlocksY = (bmpHeight / Overworld.Block_NumPixelsY) + (bmpHeight % Overworld.Block_NumPixelsY == 0 ? 0 : 1);
            int endBlockX = startBlockX + numBlocksX + (xpBX == 0 ? 0 : 1);
            int endBlockY = startBlockY + numBlocksY + (ypBY == 0 ? 0 : 1);
            int startPixelX = xpBX >= 0 ? -xpBX : -xpBX - Overworld.Block_NumPixelsX;
            int startPixelY = ypBY >= 0 ? -ypBY : -ypBY - Overworld.Block_NumPixelsY;
            // Loop each elevation
            for (byte e = 0; e < Overworld.NumElevations; e++)
            {
                // Draw blocks
                int curPixelX = startPixelX;
                int curPixelY = startPixelY;
                for (int blockY = startBlockY; blockY < endBlockY; blockY++)
                {
                    for (int blockX = startBlockX; blockX < endBlockX; blockX++)
                    {
                        Map.Layout.Block block = cameraMap.GetBlock_CrossMap(blockX, blockY, out _);
                        if (block != null)
                        {
                            Blockset.Block b = block.BlocksetBlock;
                            void Draw(Blockset.Block.Tile[] subLayers, int tx, int ty)
                            {
                                int numSubLayers = subLayers.Length;
                                for (int t = 0; t < numSubLayers; t++)
                                {
                                    Blockset.Block.Tile tile = subLayers[t];
                                    Tileset.Tile ttile = tile.TilesetTile;
                                    RenderUtils.DrawBitmap(bmpAddress, bmpWidth, bmpHeight, tx, ty, ttile.AnimBitmap ?? ttile.Bitmap, Overworld.Tile_NumPixelsX, Overworld.Tile_NumPixelsY, xFlip: tile.XFlip, yFlip: tile.YFlip);
                                }
                            }
                            for (int by = 0; by < Overworld.Block_NumTilesY; by++)
                            {
                                Blockset.Block.Tile[][][] arrY = b.Tiles[by];
                                int ty = curPixelY + (by * Overworld.Tile_NumPixelsY);
                                for (int bx = 0; bx < Overworld.Block_NumTilesX; bx++)
                                {
                                    Draw(arrY[bx][e], curPixelX + (bx * Overworld.Tile_NumPixelsX), ty);
                                }
                            }
                        }
                        curPixelX += Overworld.Block_NumPixelsX;
                    }
                    curPixelX = startPixelX;
                    curPixelY += Overworld.Block_NumPixelsY;
                }

                // Draw VisualObjs
                // TODO: They will overlap each other regardless of y coordinate because of the order of the list
                // TODO: Objs from other maps
                for (int i = 0; i < numObjs; i++)
                {
                    Obj o = objs[i];
                    if (o.Pos.Elevation == e && o is VisualObj v)
                    {
                        v.Draw(bmpAddress, bmpWidth, bmpHeight, startBlockX, startBlockY, startPixelX, startPixelY);
                    }
                }
            }
        }
    }
}
