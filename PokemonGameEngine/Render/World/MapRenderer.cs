using Kermalis.PokemonGameEngine.Render.GUIs;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Kermalis.PokemonGameEngine.Render.Shaders;
using Kermalis.PokemonGameEngine.World;
using Kermalis.PokemonGameEngine.World.Maps;
using Kermalis.PokemonGameEngine.World.Objs;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Render.World
{
    // LIMITATION: No more than 12 tilesets per layout (that's more than you'll ever use at once anyway)
    // LIMITATION: Infinite hallway of the same map connecting to itself does not work
    internal sealed partial class MapRenderer
    {
        // Extra tolerance for wide/tall VisualObj
        private const int OBJ_TOLERANCE_X = 2;
        private const int OBJ_TOLERANCE_Y = 2;

        public static MapRenderer Instance { get; private set; } = null!; // Set in constructor

        private readonly List<Map> _curVisibleMaps;
        private readonly List<Map> _prevVisibleMaps;

        private readonly MapLayoutShader _layoutShader;
        private readonly FrameBuffer[] _layoutFrameBuffers;
        private readonly FrameBuffer[] _objFrameBuffers;

        public MapRenderer()
        {
            Instance = this;

#if DEBUG_OVERWORLD
            _debugFrameBuffer = FrameBuffer.CreateWithColor(OverworldGUI.RenderSize * DEBUG_FBO_SCALE);
            _debugVisibleBlocks = Array.Empty<DebugVisibleBlock[]>();
            _debugVisibleObjs = new List<DebugVisibleObj>();
#endif

            _curVisibleMaps = new List<Map>()
            {
                PlayerObj.Instance.Map // Put player's map in right away since it's loaded already
            };
            _prevVisibleMaps = new List<Map>();

            _layoutShader = new MapLayoutShader(Display.OpenGL);
            _layoutFrameBuffers = new FrameBuffer[Overworld.NumElevations];
            _objFrameBuffers = new FrameBuffer[Overworld.NumElevations];
            for (int i = 0; i < Overworld.NumElevations; i++)
            {
                _layoutFrameBuffers[i] = FrameBuffer.CreateWithColor(OverworldGUI.RenderSize);
                _objFrameBuffers[i] = FrameBuffer.CreateWithColor(OverworldGUI.RenderSize);
            }
        }

        private static void GetCameraRect(CameraObj cam, Size2D screenSize,
            out Map cameraMap, // The map the camera is currently on
            out Pos2D startBlock, // The top left visible block (relative to the camera's map)
            out Pos2D endBlock, // The bottom right visible block (relative to the camera's map)
            out Pos2D startBlockPixel) // Screen coords of the top left block
        {
            Pos2D cameraXY = cam.Pos.XY;
            cameraMap = cam.Map;

            // Negated amount of pixels to move the current map away from the top left of the screen
            // Example: move the map 8 pixels right, cameraPixelX is -8
            int cameraPixelX = (cameraXY.X * Overworld.Block_NumPixelsX) - ((int)screenSize.Width / 2) + (Overworld.Block_NumPixelsX / 2) + cam.VisualProgress.X + cam.CamVisualOfs.X;
            int cameraPixelY = (cameraXY.Y * Overworld.Block_NumPixelsY) - ((int)screenSize.Height / 2) + (Overworld.Block_NumPixelsY / 2) + cam.VisualProgress.Y + cam.CamVisualOfs.Y;
            // Value to check if we are exactly at the start of a block
            int xpBX = cameraPixelX % Overworld.Block_NumPixelsX;
            int ypBY = cameraPixelY % Overworld.Block_NumPixelsY;

            // Calculate where the starting block is relative to the map
            // If the remainders of the above values are negative, we want to start rendering the block to the left/up
            // If they're positive, we are still rendering the same block as if they were 0
            startBlock.X = (cameraPixelX / Overworld.Block_NumPixelsX) - (xpBX < 0 ? 1 : 0);
            startBlock.Y = (cameraPixelY / Overworld.Block_NumPixelsY) - (ypBY < 0 ? 1 : 0);
            // Calculate where the top left block is on the screen
            startBlockPixel.X = xpBX >= 0 ? -xpBX : -xpBX - Overworld.Block_NumPixelsX;
            startBlockPixel.Y = ypBY >= 0 ? -ypBY : -ypBY - Overworld.Block_NumPixelsY;

            // Calculate amount of blocks currently on the screen
            int xSize = (int)screenSize.Width - startBlockPixel.X;
            int ySize = (int)screenSize.Height - startBlockPixel.Y;
            int numBlocksX = (xSize / Overworld.Block_NumPixelsX) + (xSize % Overworld.Block_NumPixelsX != 0 ? 1 : 0);
            int numBlocksY = (ySize / Overworld.Block_NumPixelsY) + (ySize % Overworld.Block_NumPixelsY != 0 ? 1 : 0);
            // Calculate bottom right block
            endBlock.X = startBlock.X + numBlocksX - 1;
            endBlock.Y = startBlock.Y + numBlocksY - 1;
        }

        // TODO: BORDER BLOCKS EZZZZZZZ
        // TODO: Tile animations
        // TODO: Obj shader
        public void Render(FrameBuffer targetFrameBuffer)
        {
            Tileset.UpdateAnimations();

            GetCameraRect(CameraObj.Instance, targetFrameBuffer.Size,
                out Map cameraMap, out Pos2D startBlock, out Pos2D endBlock, out Pos2D startBlockPixel);

            GL gl = Display.OpenGL;
            gl.ClearColor(Colors.Transparent);
            for (int i = 0; i < Overworld.NumElevations; i++)
            {
                _layoutFrameBuffers[i].Use();
                gl.Clear(ClearBufferMask.ColorBufferBit);
                _objFrameBuffers[i].Use();
                gl.Clear(ClearBufferMask.ColorBufferBit);
            }

            RenderLayouts(gl, targetFrameBuffer.Size, cameraMap, startBlock, startBlockPixel);
            RenderObjs(Obj.LoadedObjs.ToArray(), CameraObj.Instance.Pos.XY, startBlock, endBlock, startBlockPixel);

#if DEBUG_OVERWORLD
            Debug_UpdateVisibleBlocks(cameraMap, startBlock, endBlock, startBlockPixel);
#endif

            gl.Enable(EnableCap.Blend);
            gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            // Finish render
            targetFrameBuffer.Use();
            EntireScreenTextureShader.Instance.Use(gl);
            gl.ActiveTexture(TextureUnit.Texture0);

            for (int i = 0; i < Overworld.NumElevations; i++)
            {
                gl.BindTexture(TextureTarget.Texture2D, _layoutFrameBuffers[i].ColorTexture.Value);
                EntireScreenMesh.Instance.Render();
                gl.BindTexture(TextureTarget.Texture2D, _objFrameBuffers[i].ColorTexture.Value);
                EntireScreenMesh.Instance.Render();
            }
            gl.Disable(EnableCap.Blend);
#if DEBUG_OVERWORLD
            _debugVisibleObjs.Clear();
#endif
        }

        private void RenderLayouts(GL gl, Size2D screenSize, Map cameraMap, Pos2D startBlock, Pos2D startBlockPixel)
        {
            gl.Enable(EnableCap.Blend);
            gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            _layoutShader.Use(gl);
            _layoutShader.UpdateViewport(gl);

            // Set up visible map lists
            _prevVisibleMaps.AddRange(_curVisibleMaps);
            _curVisibleMaps.Clear();

            var basePos = new Pos2D((-startBlock.X * Overworld.Block_NumPixelsX) + startBlockPixel.X, (-startBlock.Y * Overworld.Block_NumPixelsY) + startBlockPixel.Y);
            RenderLayoutAndVisibleConnections(screenSize, basePos, cameraMap);

            // Mark maps out of view as no longer visible if they were previously visible
            foreach (Map m in _curVisibleMaps)
            {
                _prevVisibleMaps.Remove(m);
            }
            foreach (Map m in _prevVisibleMaps)
            {
                m.OnMapNoLongerVisible();
            }
            _prevVisibleMaps.Clear();

        }
        private void RenderLayout(MapLayout layout, Pos2D translation)
        {
            GL gl = Display.OpenGL;
            _layoutShader.SetTranslation(gl, translation);
            layout.BindTilesetTextures(gl);
            for (byte e = 0; e < Overworld.NumElevations; e++)
            {
                _layoutFrameBuffers[e].Use();
                layout.RenderElevation(gl, e);
            }
        }
        private void RenderLayoutAndVisibleConnections(Size2D screenSize, Pos2D basePos, Map map)
        {
            // Mark map as visible since we're rendering it
            _curVisibleMaps.Add(map);
            if (!_prevVisibleMaps.Contains(map))
            {
                map.OnMapNowVisible();
            }
            // Render it
            RenderLayout(map.Layout, basePos + (map.BlockOffsetFromCurrentMap * Overworld.Block_NumPixels));

            // Check connected maps to see if they're visible, and repeat if they are
            MapConnection[] connections = map.Connections;
            for (int i = 0; i < connections.Length; i++)
            {
                MapConnection con = connections[i];
                var conMap = Map.LoadOrGet(con.MapId);
                if (_curVisibleMaps.Contains(conMap))
                {
                    continue; // Don't render more than once
                }

                // Visibility check
                var rect = new Rect2D(basePos + (conMap.BlockOffsetFromCurrentMap * Overworld.Block_NumPixels),
                    new Size2D((uint)conMap.Width * Overworld.Block_NumPixelsX, (uint)conMap.Height * Overworld.Block_NumPixelsY));
                if (rect.Intersects(screenSize))
                {
                    RenderLayoutAndVisibleConnections(screenSize, basePos, conMap);
                }
            }
        }

        // TODO: Allocating array every frame = bad, plus I have to sort them every frame by y coordinate
        // Hopefully we can solve that with depth testing with their shader. Just render them with the depth being their coordinate
        private void RenderObjs(Obj[] objs, Pos2D cameraXY, Pos2D startBlock, Pos2D endBlock, Pos2D startBlockPixel)
        {
            startBlock.X -= OBJ_TOLERANCE_X;
            startBlock.Y -= OBJ_TOLERANCE_Y;
            endBlock.X += OBJ_TOLERANCE_X;
            endBlock.Y += OBJ_TOLERANCE_Y;
            startBlockPixel.X -= OBJ_TOLERANCE_X * Overworld.Block_NumPixelsX;
            startBlockPixel.Y -= OBJ_TOLERANCE_Y * Overworld.Block_NumPixelsY;

            Array.Sort(objs, (o1, o2) => o1.Pos.XY.Y.CompareTo(o2.Pos.XY.Y));
            for (int i = 0; i < objs.Length; i++)
            {
                // We don't need to check MovingFromPos to prevent it from popping in/out of existence
                // The tolerance covers enough pixels so we can confidently check Pos only
                // It'd only mess up if Pos and MovingFromPos were farther apart from each other than the tolerance
                Obj o = objs[i];
                if (o is not VisualObj v)
                {
                    continue;
                }

                Pos2D currentMapXY = v.Pos.XY + v.Map.BlockOffsetFromCurrentMap;
                if (currentMapXY.X < startBlock.X || currentMapXY.Y < startBlock.Y
                    || currentMapXY.X > endBlock.X || currentMapXY.Y > endBlock.Y)
                {
                    continue; // Make sure it's within the tolerance rect
                }

                Pos2D blockPixel;
                blockPixel.X = startBlockPixel.X + ((currentMapXY.X - startBlock.X) * Overworld.Block_NumPixelsX);
                blockPixel.Y = startBlockPixel.Y + ((currentMapXY.Y - startBlock.Y) * Overworld.Block_NumPixelsY);
                Pos2D posOnScreen;
                posOnScreen.X = blockPixel.X + v.VisualProgress.X;
                posOnScreen.Y = blockPixel.Y + v.VisualProgress.Y;
                // Draw
                _objFrameBuffers[v.Pos.Elevation].Use();
                v.Draw(posOnScreen);

                // Add to debug data
#if DEBUG_OVERWORLD
                Debug_AddVisualObj(v, blockPixel);
#endif
            }
        }
    }
}
