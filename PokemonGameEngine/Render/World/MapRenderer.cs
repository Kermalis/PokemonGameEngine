using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.World;
using Kermalis.PokemonGameEngine.World.Maps;
using Kermalis.PokemonGameEngine.World.Objs;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Render.World
{
    internal static class MapRenderer
    {
        /// <summary>This list keeps references to all visible maps, so that they are not constantly unloaded/reloaded while parsing the map connections</summary>
        public static List<Map> VisibleMaps;

        public static void GetVisibleBlocksVariables(CameraObj cam, Size2D dstSize,
            out Map cameraMap, // The map the camera is currently on
            out int startBlockX, out int startBlockY, // The first visible blocks offset from the current map
            out int endBlockX, out int endBlockY, // The last visible blocks offset from the current map
            out int startBlockPixelX, out int startBlockPixelY) // Pixel coords of the blocks to start rendering from
        {
            cameraMap = cam.Map;
            WorldPos cameraPos = cam.Pos;
            int cameraPixelX = (cameraPos.X * Overworld.Block_NumPixelsX) - ((int)dstSize.Width / 2) + (Overworld.Block_NumPixelsX / 2) + cam.ProgressX + CameraObj.CameraOfsX;
            int cameraPixelY = (cameraPos.Y * Overworld.Block_NumPixelsY) - ((int)dstSize.Height / 2) + (Overworld.Block_NumPixelsY / 2) + cam.ProgressY + CameraObj.CameraOfsY;
            int xpBX = cameraPixelX % Overworld.Block_NumPixelsX;
            int ypBY = cameraPixelY % Overworld.Block_NumPixelsY;
            startBlockX = (cameraPixelX / Overworld.Block_NumPixelsX) - (xpBX >= 0 ? 0 : 1);
            startBlockY = (cameraPixelY / Overworld.Block_NumPixelsY) - (ypBY >= 0 ? 0 : 1);
            int numBlocksX = ((int)dstSize.Width / Overworld.Block_NumPixelsX) + ((int)dstSize.Width % Overworld.Block_NumPixelsX == 0 ? 0 : 1);
            int numBlocksY = ((int)dstSize.Height / Overworld.Block_NumPixelsY) + ((int)dstSize.Height % Overworld.Block_NumPixelsY == 0 ? 0 : 1);
            endBlockX = startBlockX + numBlocksX + (xpBX == 0 ? 0 : 1);
            endBlockY = startBlockY + numBlocksY + (ypBY == 0 ? 0 : 1);
            startBlockPixelX = xpBX >= 0 ? -xpBX : -xpBX - Overworld.Block_NumPixelsX;
            startBlockPixelY = ypBY >= 0 ? -ypBY : -ypBY - Overworld.Block_NumPixelsY;
        }

        private static void RenderBlocks(byte elevation, Map cameraMap, int startBlockX, int startBlockY, int endBlockX, int endBlockY, int startBlockPixelX, int startBlockPixelY)
        {
            int curPixelX = startBlockPixelX;
            int curPixelY = startBlockPixelY;
            for (int blockY = startBlockY; blockY < endBlockY; blockY++)
            {
                for (int blockX = startBlockX; blockX < endBlockX; blockX++)
                {
                    MapLayout.Block block = cameraMap.GetBlock_CrossMap(blockX, blockY, out _, out _, out _);
                    if (block is not null) // No border would show pure black
                    {
                        block.BlocksetBlock.Render(elevation, curPixelX, curPixelY);
                    }
                    curPixelX += Overworld.Block_NumPixelsX;
                }
                curPixelX = startBlockPixelX;
                curPixelY += Overworld.Block_NumPixelsY;
            }
        }
        private static void RenderObjs(Size2D dstSize,
            List<Obj> objs, byte elevation, Map cameraMap, int startBlockX, int startBlockY, int endBlockX, int endBlockY, int startBlockPixelX, int startBlockPixelY)
        {
            // Extra tolerance for wide/tall VisualObj
            const int ToleranceW = 2;
            const int ToleranceH = 2;
            startBlockX -= ToleranceW;
            endBlockX += ToleranceW;
            startBlockPixelX -= ToleranceW * Overworld.Block_NumPixelsX;
            startBlockY -= ToleranceH;
            endBlockY += ToleranceH;
            startBlockPixelY -= ToleranceH * Overworld.Block_NumPixelsY;

            int curPixelX = startBlockPixelX;
            int curPixelY = startBlockPixelY;
            for (int blockY = startBlockY; blockY < endBlockY; blockY++)
            {
                for (int blockX = startBlockX; blockX < endBlockX; blockX++)
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
                        if (p.Elevation == elevation && p.X == outX && p.Y == outY)
                        {
                            v.Draw(dstSize, blockX - startBlockX, blockY - startBlockY, startBlockPixelX, startBlockPixelY);
                        }
                    }
                    curPixelX += Overworld.Block_NumPixelsX;
                }
                curPixelX = startBlockPixelX;
                curPixelY += Overworld.Block_NumPixelsY;
            }
        }
        public static void Render(Size2D dstSize)
        {
            GetVisibleBlocksVariables(CameraObj.Camera, dstSize,
                out Map cameraMap, out int startBlockX, out int startBlockY, out int endBlockX, out int endBlockY, out int startBlockPixelX, out int startBlockPixelY);

            List<Obj> objs = Obj.LoadedObjs;
            // Loop each elevation
            for (byte e = 0; e < Overworld.NumElevations; e++)
            {
                // Draw blocks
                RenderBlocks(e, cameraMap, startBlockX, startBlockY, endBlockX, endBlockY, startBlockPixelX, startBlockPixelY);
                // Draw VisualObjs
                RenderObjs(dstSize, objs, e, cameraMap, startBlockX, startBlockY, endBlockX, endBlockY, startBlockPixelX, startBlockPixelY);
            }
        }

        public static void UpdateVisibleMaps()
        {
            GetVisibleBlocksVariables(CameraObj.Camera, Game.RenderSize,
                out Map cameraMap, out int startBlockX, out int startBlockY, out int endBlockX, out int endBlockY, out int _, out int _);

            List<Map> oldList = VisibleMaps;
            var newList = new List<Map>();

            for (int blockY = startBlockY; blockY < endBlockY; blockY++)
            {
                for (int blockX = startBlockX; blockX < endBlockX; blockX++)
                {
                    cameraMap.GetBlock_CrossMap(blockX, blockY, out _, out _, out Map map);
                    if (!newList.Contains(map))
                    {
                        newList.Add(map);
                        if (oldList is null || !oldList.Contains(map))
                        {
                            map.OnMapNowVisible();
                        }
                    }
                }
            }

            if (oldList is not null)
            {
                foreach (Map m in newList)
                {
                    oldList.Remove(m);
                }
                foreach (Map m in oldList)
                {
                    m.OnMapNoLongerVisible();
                }
            }
            VisibleMaps = newList;
        }
    }
}
