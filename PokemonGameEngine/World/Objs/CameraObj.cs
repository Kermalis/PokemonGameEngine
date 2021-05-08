using Kermalis.PokemonGameEngine.UI;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.World.Objs
{
    internal class CameraObj : Obj
    {
        public static readonly CameraObj Camera = new CameraObj();
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
        protected override void OnPositionVisiblyChanged()
        {
            UpdateVisibleMaps();
        }

        private void GetVisibleBlocksVariables(int bmpWidth, int bmpHeight,
            out Map cameraMap, // The map the camera is currently on
            out int startBlockX, out int startBlockY, // The first visible blocks offset from the current map
            out int endBlockX, out int endBlockY, // The last visible blocks offset from the current map
            out int startBlockPixelX, out int startBlockPixelY) // Pixel coords of the blocks to start rendering from
        {
            cameraMap = Map;
            Position cameraPos = Pos;
            int cameraPixelX = (cameraPos.X * Overworld.Block_NumPixelsX) - (bmpWidth / 2) + (Overworld.Block_NumPixelsX / 2) + ProgressX + CameraOfsX;
            int cameraPixelY = (cameraPos.Y * Overworld.Block_NumPixelsY) - (bmpHeight / 2) + (Overworld.Block_NumPixelsY / 2) + ProgressY + CameraOfsY;
            int xpBX = cameraPixelX % Overworld.Block_NumPixelsX;
            int ypBY = cameraPixelY % Overworld.Block_NumPixelsY;
            startBlockX = (cameraPixelX / Overworld.Block_NumPixelsX) - (xpBX >= 0 ? 0 : 1);
            startBlockY = (cameraPixelY / Overworld.Block_NumPixelsY) - (ypBY >= 0 ? 0 : 1);
            int numBlocksX = (bmpWidth / Overworld.Block_NumPixelsX) + (bmpWidth % Overworld.Block_NumPixelsX == 0 ? 0 : 1);
            int numBlocksY = (bmpHeight / Overworld.Block_NumPixelsY) + (bmpHeight % Overworld.Block_NumPixelsY == 0 ? 0 : 1);
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
                    cameraMap.GetBlock_CrossMap(blockX, blockY, out Map map);
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

        public static unsafe void Render(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            Camera.GetVisibleBlocksVariables(bmpWidth, bmpHeight,
                out Map cameraMap, out int startBlockX, out int startBlockY, out int endBlockX, out int endBlockY, out int startBlockPixelX, out int startBlockPixelY);
            List<Obj> objs = LoadedObjs;
            int numObjs = objs.Count;
            // Loop each elevation
            for (byte e = 0; e < Overworld.NumElevations; e++)
            {
                // Draw blocks
                int curPixelX = startBlockPixelX;
                int curPixelY = startBlockPixelY;
                for (int blockY = startBlockY; blockY < endBlockY; blockY++)
                {
                    for (int blockX = startBlockX; blockX < endBlockX; blockX++)
                    {
                        Map.Layout.Block block = cameraMap.GetBlock_CrossMap(blockX, blockY);
                        if (block != null)
                        {
                            block.BlocksetBlock.Render(bmpAddress, bmpWidth, bmpHeight, e, curPixelX, curPixelY);
                        }
                        curPixelX += Overworld.Block_NumPixelsX;
                    }
                    curPixelX = startBlockPixelX;
                    curPixelY += Overworld.Block_NumPixelsY;
                }

                // Draw VisualObjs
                // TODO: They will overlap each other regardless of y coordinate because of the order of the list
                // TODO: Objs from other maps (rn they are put in wrong spots)
                for (int i = 0; i < numObjs; i++)
                {
                    Obj o = objs[i];
                    if (o.Pos.Elevation == e && o is VisualObj v)
                    {
                        v.Draw(bmpAddress, bmpWidth, bmpHeight, cameraMap, startBlockX, startBlockY, startBlockPixelX, startBlockPixelY);
                    }
                }
            }
        }
    }
}
