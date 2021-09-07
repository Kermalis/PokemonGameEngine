using Kermalis.PokemonGameEngine.Render.OpenGL;
using Kermalis.PokemonGameEngine.World;
using Kermalis.PokemonGameEngine.World.Maps;
using Kermalis.PokemonGameEngine.World.Objs;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Render.World
{
    // TODO: Why do border blocks tank framerate so much? Even cross map blocks don't
    // TODO: Black frame when new map is now visible
    internal static class MapRenderer
    {
        private struct VisibleBlock
        {
            /// <summary>Can be null, as in no border</summary>
            public MapLayout.Block Block;
            public Pos2D PositionOnScreen;
        }
        private struct VisibleObj
        {
            public VisualObj Obj;
            public Pos2D PositionOnScreen;
        }

        /// <summary>This list keeps references to all visible maps, so that they are not constantly unloaded/reloaded while parsing the map connections</summary>
        private static List<Map> _visibleMaps;
        private static readonly List<VisibleObj> _visibleObjs = new();
        private static int _numVisibleBlocksX;
        private static int _numVisibleBlocksY;
        private static VisibleBlock[][] _visibleBlocks = Array.Empty<VisibleBlock[]>();

        private static void GetVisible(CameraObj cam,
            out Map cameraMap, // The map the camera is currently on
            out Pos2D startBlock, // The first visible blocks offset from the current map
            out Pos2D endBlock, // The last visible blocks offset from the current map
            out Pos2D startBlockPixel) // Pixel coords of the blocks to start rendering from
        {
            cameraMap = cam.Map;
            WorldPos cameraPos = cam.Pos;
            int cameraPixelX = (cameraPos.X * Overworld.Block_NumPixelsX) - ((int)GLHelper.CurrentWidth / 2) + (Overworld.Block_NumPixelsX / 2) + cam.ProgressX + CameraObj.CameraVisualOffset.X;
            int cameraPixelY = (cameraPos.Y * Overworld.Block_NumPixelsY) - ((int)GLHelper.CurrentHeight / 2) + (Overworld.Block_NumPixelsY / 2) + cam.ProgressY + CameraObj.CameraVisualOffset.Y;
            int xpBX = cameraPixelX % Overworld.Block_NumPixelsX;
            int ypBY = cameraPixelY % Overworld.Block_NumPixelsY;
            startBlock.X = (cameraPixelX / Overworld.Block_NumPixelsX) - (xpBX >= 0 ? 0 : 1);
            startBlock.Y = (cameraPixelY / Overworld.Block_NumPixelsY) - (ypBY >= 0 ? 0 : 1);
            int numBlocksX = ((int)GLHelper.CurrentWidth / Overworld.Block_NumPixelsX) + ((int)GLHelper.CurrentWidth % Overworld.Block_NumPixelsX == 0 ? 0 : 1);
            int numBlocksY = ((int)GLHelper.CurrentHeight / Overworld.Block_NumPixelsY) + ((int)GLHelper.CurrentHeight % Overworld.Block_NumPixelsY == 0 ? 0 : 1);
            endBlock.X = startBlock.X + numBlocksX + (xpBX == 0 ? 0 : 1);
            endBlock.Y = startBlock.Y + numBlocksY + (ypBY == 0 ? 0 : 1);
            startBlockPixel.X = xpBX >= 0 ? -xpBX : -xpBX - Overworld.Block_NumPixelsX;
            startBlockPixel.Y = ypBY >= 0 ? -ypBY : -ypBY - Overworld.Block_NumPixelsY;
        }

        public static void Render()
        {
            GetVisible(CameraObj.Camera, out Map cameraMap, out Pos2D startBlock, out Pos2D endBlock, out Pos2D startBlockPixel);
            UpdateVisualObjs(Obj.LoadedObjs, cameraMap, startBlock, endBlock, startBlockPixel);
            UpdateVisibleMapsAndBlocks(cameraMap, startBlock, endBlock, startBlockPixel);

            for (byte e = 0; e < Overworld.NumElevations; e++)
            {
                RenderBlocks(e);
                RenderObjs(e);
            }
        }
        private static void RenderBlocks(byte elevation)
        {
            for (int y = 0; y < _numVisibleBlocksY; y++)
            {
                VisibleBlock[] vbY = _visibleBlocks[y];
                for (int x = 0; x < _numVisibleBlocksX; x++)
                {
                    VisibleBlock vb = vbY[x];
                    if (vb.Block is not null) // No border would show pure black
                    {
                        vb.Block.BlocksetBlock.Render(elevation, vb.PositionOnScreen);
                    }
                }
            }
        }
        private static void RenderObjs(byte elevation)
        {
            for (int i = 0; i < _visibleObjs.Count; i++)
            {
                VisibleObj vo = _visibleObjs[i];
                if (vo.Obj.Pos.Elevation != elevation)
                {
                    continue;
                }
                vo.Obj.Draw(vo.PositionOnScreen);
            }
        }

        private static void UpdateVisibleMapsAndBlocks(Map cameraMap, Pos2D startBlock, Pos2D endBlock, Pos2D startBlockPixel)
        {
            List<Map> oldMaps = _visibleMaps;
            var newMaps = new List<Map>();
            UpdateVisibleBlocks(cameraMap, startBlock, endBlock, startBlockPixel, oldMaps, newMaps);

            // Would be null on the first load
            if (oldMaps is not null)
            {
                foreach (Map m in newMaps)
                {
                    oldMaps.Remove(m);
                }
                foreach (Map m in oldMaps)
                {
                    m.OnMapNoLongerVisible();
                }
            }
            _visibleMaps = newMaps;
        }

        private static void UpdateVisualObjs(List<Obj> objs, Map cameraMap, Pos2D startBlock, Pos2D endBlock, Pos2D startBlockPixel)
        {
            _visibleObjs.Clear();

            // Extra tolerance for wide/tall VisualObj
            const int ToleranceW = 2;
            const int ToleranceH = 2;
            startBlock.X -= ToleranceW;
            endBlock.X += ToleranceW;
            startBlockPixel.X -= ToleranceW * Overworld.Block_NumPixelsX;
            startBlock.Y -= ToleranceH;
            endBlock.Y += ToleranceH;
            startBlockPixel.Y -= ToleranceH * Overworld.Block_NumPixelsY;

            for (int blockY = startBlock.Y; blockY < endBlock.Y; blockY++)
            {
                for (int blockX = startBlock.X; blockX < endBlock.X; blockX++)
                {
                    cameraMap.GetXYMap(blockX, blockY, out int outX, out int outY, out Map map);
                    for (int i = 0; i < objs.Count; i++)
                    {
                        Obj o = objs[i];
                        if (o is not VisualObj v || v.Map != map)
                        {
                            continue;
                        }
                        // We don't need to check PrevPos to prevent it from popping in/out of existence
                        // The tolerance covers enough pixels where we can confidently check Pos only
                        // It'd only mess up if Pos and PrevPos were farther apart from each other than the tolerance
                        WorldPos p = v.Pos;
                        if (p.X == outX && p.Y == outY)
                        {
                            VisibleObj vo;
                            vo.Obj = v;
                            vo.PositionOnScreen.X = ((blockX - startBlock.X) * Overworld.Block_NumPixelsX) + v.ProgressX + startBlockPixel.X;
                            vo.PositionOnScreen.Y = ((blockY - startBlock.Y) * Overworld.Block_NumPixelsY) + v.ProgressY + startBlockPixel.Y;
                            _visibleObjs.Add(vo);
                        }
                    }
                }
            }
        }

        private static void UpdateVisibleBlocksBounds(Pos2D startBlock, Pos2D endBlock)
        {
            _numVisibleBlocksX = endBlock.X - startBlock.X;
            _numVisibleBlocksY = endBlock.Y - startBlock.Y;

            Array.Clear(_visibleBlocks, 0, _visibleBlocks.Length);
            if (_visibleBlocks.Length < _numVisibleBlocksY)
            {
                Array.Resize(ref _visibleBlocks, _numVisibleBlocksY);
            }
            for (int y = 0; y < _numVisibleBlocksY; y++)
            {
                VisibleBlock[] arrX = _visibleBlocks[y];
                if (arrX is null)
                {
                    _visibleBlocks[y] = new VisibleBlock[_numVisibleBlocksX];
                }
                else
                {
                    Array.Clear(arrX, 0, arrX.Length);
                    if (arrX.Length < _numVisibleBlocksX)
                    {
                        Array.Resize(ref arrX, _numVisibleBlocksX);
                    }
                }
            }
        }
        private static void UpdateVisibleBlocks(Map cameraMap, Pos2D startBlock, Pos2D endBlock, Pos2D startBlockPixel, List<Map> oldMaps, List<Map> newMaps)
        {
            UpdateVisibleBlocksBounds(startBlock, endBlock);

            Pos2D curPixel = startBlockPixel;
            for (int blockY = startBlock.Y, blockYIndex = 0; blockY < endBlock.Y; blockY++, blockYIndex++)
            {
                VisibleBlock[] vbY = _visibleBlocks[blockYIndex];
                for (int blockX = startBlock.X, blockXIndex = 0; blockX < endBlock.X; blockX++, blockXIndex++)
                {
                    VisibleBlock vb;
                    vb.Block = cameraMap.GetBlock_CrossMap(blockX, blockY, out _, out _, out Map map);
                    vb.PositionOnScreen = curPixel;
                    vbY[blockXIndex] = vb;
                    if (!newMaps.Contains(map))
                    {
                        newMaps.Add(map);
                        if (oldMaps is null || !oldMaps.Contains(map))
                        {
                            map.OnMapNowVisible();
                        }
                    }
                    curPixel.X += Overworld.Block_NumPixelsX;
                }
                curPixel.X = startBlockPixel.X;
                curPixel.Y += Overworld.Block_NumPixelsY;
            }
        }
    }
}
