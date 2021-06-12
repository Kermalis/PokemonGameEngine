using Kermalis.PokemonGameEngine.UI;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.World.Objs
{
    internal class CameraObj : Obj
    {
        public static readonly CameraObj Camera = new();
        public static int CameraOfsX;
        public static int CameraOfsY;
        public static Obj CameraAttachedTo;

        // This list keeps references to all visible maps, so that they are not constantly unloaded/reloaded while parsing the map connections
        public static List<Map> VisibleMaps;

        private CameraObj()
            : base(Overworld.CameraId)
        {
        }
        public static void Init()
        {
            CameraAttachedTo = PlayerObj.Player;
            Camera.Pos = PlayerObj.Player.Pos;
            Camera.Map = PlayerObj.Player.Map;
            Camera.Map.Objs.Add(Camera);
            UpdateVisibleMaps();
        }

        public static void CopyMovementIfAttachedTo(Obj obj)
        {
            if (CameraAttachedTo == obj)
            {
                CameraCopyMovement();
            }
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
            UpdateVisibleMaps();
        }
        public static void SetCameraOffset(int xOffset, int yOffset)
        {
            CameraOfsX = xOffset;
            CameraOfsY = yOffset;
            UpdateVisibleMaps();
        }

        public override bool CollidesWithOthers()
        {
            return false;
        }
        protected override bool CanSurf()
        {
            return true;
        }
        protected override void OnPositionVisiblyChanged()
        {
            UpdateVisibleMaps();
        }

        private void GetVisibleBlocksVariables(int dstW, int dstH,
            out Map cameraMap, // The map the camera is currently on
            out int startBlockX, out int startBlockY, // The first visible blocks offset from the current map
            out int endBlockX, out int endBlockY, // The last visible blocks offset from the current map
            out int startBlockPixelX, out int startBlockPixelY) // Pixel coords of the blocks to start rendering from
        {
            cameraMap = Map;
            Position cameraPos = Pos;
            int cameraPixelX = (cameraPos.X * Overworld.Block_NumPixelsX) - (dstW / 2) + (Overworld.Block_NumPixelsX / 2) + ProgressX + CameraOfsX;
            int cameraPixelY = (cameraPos.Y * Overworld.Block_NumPixelsY) - (dstH / 2) + (Overworld.Block_NumPixelsY / 2) + ProgressY + CameraOfsY;
            int xpBX = cameraPixelX % Overworld.Block_NumPixelsX;
            int ypBY = cameraPixelY % Overworld.Block_NumPixelsY;
            startBlockX = (cameraPixelX / Overworld.Block_NumPixelsX) - (xpBX >= 0 ? 0 : 1);
            startBlockY = (cameraPixelY / Overworld.Block_NumPixelsY) - (ypBY >= 0 ? 0 : 1);
            int numBlocksX = (dstW / Overworld.Block_NumPixelsX) + (dstW % Overworld.Block_NumPixelsX == 0 ? 0 : 1);
            int numBlocksY = (dstH / Overworld.Block_NumPixelsY) + (dstH % Overworld.Block_NumPixelsY == 0 ? 0 : 1);
            endBlockX = startBlockX + numBlocksX + (xpBX == 0 ? 0 : 1);
            endBlockY = startBlockY + numBlocksY + (ypBY == 0 ? 0 : 1);
            startBlockPixelX = xpBX >= 0 ? -xpBX : -xpBX - Overworld.Block_NumPixelsX;
            startBlockPixelY = ypBY >= 0 ? -ypBY : -ypBY - Overworld.Block_NumPixelsY;
        }

        public static void UpdateVisibleMaps()
        {
            Camera.GetVisibleBlocksVariables(Program.RenderWidth, Program.RenderHeight,
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

            if (oldList != null)
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

        private static unsafe void RenderBlocks(uint* dst, int dstW, int dstH,
            byte elevation, Map cameraMap, int startBlockX, int startBlockY, int endBlockX, int endBlockY, int startBlockPixelX, int startBlockPixelY)
        {
            int curPixelX = startBlockPixelX;
            int curPixelY = startBlockPixelY;
            for (int blockY = startBlockY; blockY < endBlockY; blockY++)
            {
                for (int blockX = startBlockX; blockX < endBlockX; blockX++)
                {
                    Map.Layout.Block block = cameraMap.GetBlock_CrossMap(blockX, blockY, out _, out _, out _);
                    if (block != null) // No border would show pure black
                    {
                        block.BlocksetBlock.Render(dst, dstW, dstH, elevation, curPixelX, curPixelY);
                    }
                    curPixelX += Overworld.Block_NumPixelsX;
                }
                curPixelX = startBlockPixelX;
                curPixelY += Overworld.Block_NumPixelsY;
            }
        }
        private static unsafe void RenderObjs(uint* dst, int dstW, int dstH,
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
                        Position p = v.Pos;
                        if (p.Elevation == elevation && p.X == outX && p.Y == outY)
                        {
                            v.Draw(dst, dstW, dstH, blockX - startBlockX, blockY - startBlockY, startBlockPixelX, startBlockPixelY);
                        }
                    }
                    curPixelX += Overworld.Block_NumPixelsX;
                }
                curPixelX = startBlockPixelX;
                curPixelY += Overworld.Block_NumPixelsY;
            }
        }
        public static unsafe void Render(uint* dst, int dstW, int dstH)
        {
            Camera.GetVisibleBlocksVariables(dstW, dstH,
                out Map cameraMap, out int startBlockX, out int startBlockY, out int endBlockX, out int endBlockY, out int startBlockPixelX, out int startBlockPixelY);

            List<Obj> objs = LoadedObjs;
            // Loop each elevation
            for (byte e = 0; e < Overworld.NumElevations; e++)
            {
                // Draw blocks
                RenderBlocks(dst, dstW, dstH, e, cameraMap, startBlockX, startBlockY, endBlockX, endBlockY, startBlockPixelX, startBlockPixelY);
                // Draw VisualObjs
                RenderObjs(dst, dstW, dstH, objs, e, cameraMap, startBlockX, startBlockY, endBlockX, endBlockY, startBlockPixelX, startBlockPixelY);
            }
        }
    }
}
