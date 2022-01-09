using Kermalis.PokemonGameEngine.Render.OpenGL;
using Kermalis.PokemonGameEngine.Render.Shaders;
using Kermalis.PokemonGameEngine.Render.Shaders.World;
using Kermalis.PokemonGameEngine.World;
using Kermalis.PokemonGameEngine.World.Maps;
using Kermalis.PokemonGameEngine.World.Objs;
using Silk.NET.OpenGL;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Render.World
{
    // LIMITATION: No more than "GLTextureUtils.MAX_ACTIVE_TEXTURES" tilesets per blockset (that's more than you'll ever use at once anyway)
    // LIMITATION: Cannot create an infinite hallway of the same map connecting to itself
    internal sealed partial class MapRenderer
    {
        // Extra blocks allowed for wide/tall VisualObj
        private static readonly Vec2I _objTolerance = new(2, 2);

        private readonly Vec2I _screenSize;
        private readonly Vec2I _maxVisibleBlocks;

        private readonly List<Map> _curVisibleMaps;
        private readonly List<Map> _prevVisibleMaps;
        private readonly BitArray _nonBorderCoords;

        private readonly MapLayoutShader _layoutShader;
        private readonly MapLayoutBlockMesh _layoutMesh;
        private readonly FrameBuffer2DColor[] _layoutFrameBuffers;
        private readonly FrameBuffer2DColor[] _objFrameBuffers;
        private readonly InstancedData[] _blockData;

        public MapRenderer(Vec2I screenSize)
        {
            _screenSize = screenSize;
            _maxVisibleBlocks = GetMaximumVisibleBlocks();

#if DEBUG_OVERWORLD
            _debugFrameBuffer = new FrameBuffer2DColor(screenSize * DEBUG_FBO_SCALE);
            _debugVisibleBlocks = new DebugVisibleBlock[_maxVisibleBlocks.Y][];
            for (int i = 0; i < _maxVisibleBlocks.Y; i++)
            {
                _debugVisibleBlocks[i] = new DebugVisibleBlock[_maxVisibleBlocks.X];
            }
#endif

            _curVisibleMaps = new List<Map>()
            {
                PlayerObj.Instance.Map // Put player's map in right away since it's loaded already
            };
            _prevVisibleMaps = new List<Map>();
            _nonBorderCoords = new BitArray(_maxVisibleBlocks.GetArea());

            GL gl = Display.OpenGL;
            _layoutShader = new MapLayoutShader(gl); // shader is in use
            _layoutShader.UpdateViewport(gl, screenSize);
            _layoutFrameBuffers = new FrameBuffer2DColor[Overworld.NumElevations];
            _objFrameBuffers = new FrameBuffer2DColor[Overworld.NumElevations];
            _blockData = new InstancedData[Overworld.NumElevations];

            _layoutMesh = new MapLayoutBlockMesh(gl); // Need VAO bound for instanced attributes
            int maxVisible = _maxVisibleBlocks.GetArea();
            for (int i = 0; i < Overworld.NumElevations; i++)
            {
                _layoutFrameBuffers[i] = new FrameBuffer2DColor(screenSize);
                _objFrameBuffers[i] = new FrameBuffer2DColor(screenSize);
                _blockData[i] = VBOData_InstancedLayoutBlock.CreateInstancedData(maxVisible);
            }
        }

        private Vec2I GetMaximumVisibleBlocks()
        {
            Vec2I ret = (_screenSize / Overworld.Block_NumPixels).Plus(1, 1);
            Vec2I mod = _screenSize % Overworld.Block_NumPixels;
            if (mod.X != 0)
            {
                ret.X++;
            }
            if (mod.Y != 0)
            {
                ret.Y++;
            }
            return ret;
        }
        private void InitCameraRect(CameraObj cam,
            out Rect visibleBlocks, // The top left and bottom right visible blocks (relative to the camera's map)
            out Vec2I startBlockPixel) // Screen coords of the top left block
        {
            Vec2I camXY = cam.Pos.XY;

            // Negated amount of pixels to move the current map away from the top left of the screen
            // Example: move the map 8 pixels right, camPixel.X is -8
            Vec2I camPixel = (camXY * Overworld.Block_NumPixels) - (_screenSize / 2) + (Overworld.Block_NumPixels / 2) + cam.VisualProgress + cam.CamVisualOfs;
            // Value to check if we are exactly at the start of a block
            Vec2I camPixelMod = camPixel % Overworld.Block_NumPixels;

            // Calculate where the starting block is relative to the map
            // If the camPixelMod values are negative, we want to start rendering the block one extra left/up
            // If they're positive, we are still rendering the same block as if they were 0
            Vec2I startBlock = camPixel / Overworld.Block_NumPixels;
            if (camPixelMod.X < 0)
            {
                startBlock.X--;
            }
            if (camPixelMod.Y < 0)
            {
                startBlock.Y--;
            }
            // Calculate where the top left block is on the screen
            startBlockPixel = -camPixelMod;
            if (camPixelMod.X < 0)
            {
                startBlockPixel.X -= Overworld.Block_NumPixelsX;
            }
            if (camPixelMod.Y < 0)
            {
                startBlockPixel.Y -= Overworld.Block_NumPixelsY;
            }

            // Calculate amount of blocks currently on the screen
            Vec2I screenPixels = _screenSize - startBlockPixel;
            Vec2I numBlocks = screenPixels / Overworld.Block_NumPixels;
            if (screenPixels.X % Overworld.Block_NumPixelsX != 0)
            {
                numBlocks.X++;
            }
            if (screenPixels.Y % Overworld.Block_NumPixelsY != 0)
            {
                numBlocks.Y++;
            }

            // Done
            visibleBlocks = Rect.FromSize(startBlock, numBlocks);

            // Set up border block data for this frame
            InitBorderBlocks(numBlocks.GetArea());
        }

        private void InitBorderBlocks(int num)
        {
            for (int i = 0; i < num; i++)
            {
                _nonBorderCoords[i] = true;
            }
        }
        private void SetNonBorderBlock(Vec2I pos, int numBlocksX)
        {
            _nonBorderCoords[pos.X + (pos.Y * numBlocksX)] = false;
        }
        private bool IsBorderBlock(Vec2I pos, int numBlocksX)
        {
            return _nonBorderCoords[pos.X + (pos.Y * numBlocksX)];
        }

        public void Render(FrameBuffer targetFrameBuffer)
        {
            // Do animation tick
            Tileset.UpdateAnimations();
            Blockset.UpdateAnimations();
            Tileset.FinishUpdateAnimations();

            // Init data
            GL gl = Display.OpenGL;
            gl.ClearColor(Colors.Transparent);
            for (int i = 0; i < Overworld.NumElevations; i++)
            {
                _objFrameBuffers[i].Use();
                gl.Clear(ClearBufferMask.ColorBufferBit);
                _blockData[i].Prepare();
            }

            InitCameraRect(CameraObj.Instance, out Rect visibleBlocks, out Vec2I startBlockPixel);

            RenderLayouts(gl, CameraObj.Instance.Map, visibleBlocks, startBlockPixel);
            RenderObjs(Obj.LoadedObjs.ToArray(), visibleBlocks, startBlockPixel);

            // Finish render by rendering each layer to the target
            targetFrameBuffer.Use();
            EntireScreenTextureShader.Instance.Use(gl);

            gl.Enable(EnableCap.Blend); // Blend is disabled after rendering objs so re-enable it (can remove this when adding the obj shader)
            gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            gl.ActiveTexture(TextureUnit.Texture0);
            for (int i = 0; i < Overworld.NumElevations; i++)
            {
                gl.BindTexture(TextureTarget.Texture2D, _layoutFrameBuffers[i].ColorTexture);
                RectMesh.Instance.Render();
                gl.BindTexture(TextureTarget.Texture2D, _objFrameBuffers[i].ColorTexture);
                RectMesh.Instance.Render();
            }

            gl.Disable(EnableCap.Blend);
        }

        private void RenderLayouts(GL gl, Map curMap, in Rect visibleBlocks, Vec2I startBlockPixel)
        {
            // Set up visible map lists
            _prevVisibleMaps.AddRange(_curVisibleMaps);
            _curVisibleMaps.Clear();

            // Check map visibility. Remove invisible maps before adding visible ones
            Vec2I curMapPos = (-visibleBlocks.TopLeft * Overworld.Block_NumPixels) + startBlockPixel;
            foreach (Map m in _prevVisibleMaps)
            {
                if (!IsMapVisible(m, curMapPos))
                {
                    m.OnMapNoLongerVisible();
                }
            }
            AddVisibleMap(curMapPos, visibleBlocks, startBlockPixel, curMap); // Mark visible maps
            _prevVisibleMaps.Clear();

            // Add border blocks if they exist
            Vec2I borderSize = curMap.Layout.BorderSize;
            if (borderSize.X > 0 && borderSize.Y > 0)
            {
                AddBorderBlocks(curMap.Layout, visibleBlocks, startBlockPixel);
            }

            // Render the blocks
            gl.Disable(EnableCap.Blend);
            gl.ActiveTexture(TextureUnit.Texture0);
            _layoutShader.Use(gl);
            for (byte e = 0; e < Overworld.NumElevations; e++)
            {
                _layoutFrameBuffers[e].Use();
                gl.Clear(ClearBufferMask.ColorBufferBit);
                gl.BindTexture(TextureTarget.Texture3D, Blockset.UsedBlocksTextures[e].ColorTexture);
                _layoutMesh.Render(_blockData[e].InstanceCount);
            }

#if DEBUG_OVERWORLD
            if (_debugEnabled)
            {
                Debug_UpdateVisibleBlocks(curMap, visibleBlocks, startBlockPixel);
            }
#endif
        }
        private bool IsMapVisible(Map map, Vec2I curMapPos)
        {
            var mapPixelRect = Rect.FromSize((map.BlockOffsetFromCurrentMap * Overworld.Block_NumPixels) + curMapPos,
                map.Size * Overworld.Block_NumPixels);
            return mapPixelRect.Intersects(_screenSize);
        }
        private void AddVisibleMap(Vec2I curMapPos, in Rect visibleBlocks, Vec2I startBlockPixel, Map map)
        {
            _curVisibleMaps.Add(map);
            if (!_prevVisibleMaps.Contains(map))
            {
                map.OnMapNowVisible();
            }
            AddLayoutBlocks(map, curMapPos, visibleBlocks, startBlockPixel);

            // Check connected maps to see if they're visible, and check their connected maps
            Map.Connection[] connections = map.Connections;
            for (int i = 0; i < connections.Length; i++)
            {
                Map conMap = connections[i].Map;
                if (!_curVisibleMaps.Contains(conMap) && IsMapVisible(conMap, curMapPos))
                {
                    AddVisibleMap(curMapPos, visibleBlocks, startBlockPixel, conMap);
                }
            }
        }
        private void AddLayoutBlocks(Map map, Vec2I curMapPos, in Rect visibleBlocks, Vec2I startBlockPixel)
        {
            // Set all blocks that are visible
            var mapPixelRect = Rect.FromSize((map.BlockOffsetFromCurrentMap * Overworld.Block_NumPixels) + curMapPos,
                map.Size * Overworld.Block_NumPixels);

            int numBlocksX = visibleBlocks.GetWidth();
            Vec2I xy;
            for (xy.Y = visibleBlocks.TopLeft.Y; xy.Y <= visibleBlocks.BottomRight.Y; xy.Y++)
            {
                for (xy.X = visibleBlocks.TopLeft.X; xy.X <= visibleBlocks.BottomRight.X; xy.X++)
                {
                    if (!IsBorderBlock(xy - visibleBlocks.TopLeft, numBlocksX))
                    {
                        continue; // Already set this one
                    }

                    var xyPixelRect = Rect.FromSize(((xy - visibleBlocks.TopLeft) * Overworld.Block_NumPixels) + startBlockPixel,
                        Overworld.Block_NumPixels);
                    if (!xyPixelRect.Intersects(mapPixelRect))
                    {
                        continue; // Not visible
                    }

                    SetNonBorderBlock(xy - visibleBlocks.TopLeft, numBlocksX);
                    // Add instanced data
                    Vec2I blockXY = xy - map.BlockOffsetFromCurrentMap;
                    int blockUsedIndex = map.Layout.Blocks[blockXY.Y][blockXY.X].BlocksetBlock.UsedBlocksIndex;
                    for (byte e = 0; e < Overworld.NumElevations; e++)
                    {
                        VBOData_InstancedLayoutBlock.AddInstance(_blockData[e], xyPixelRect.TopLeft, blockUsedIndex);
                    }
                }
            }
        }
        private void AddBorderBlocks(MapLayout curMapLayout, in Rect visibleBlocks, Vec2I startBlockPixel)
        {
            int numBlocksX = visibleBlocks.GetWidth();
            Vec2I xy;
            for (xy.Y = visibleBlocks.TopLeft.Y; xy.Y <= visibleBlocks.BottomRight.Y; xy.Y++)
            {
                for (xy.X = visibleBlocks.TopLeft.X; xy.X <= visibleBlocks.BottomRight.X; xy.X++)
                {
                    if (!IsBorderBlock(xy - visibleBlocks.TopLeft, numBlocksX))
                    {
                        continue; // Already set this one
                    }

                    Vec2I borderIndex = curMapLayout.GetBorderBlockIndex(xy);
                    int blockUsedIndex = curMapLayout.BorderBlocks[borderIndex.Y][borderIndex.X].BlocksetBlock.UsedBlocksIndex;
                    Vec2I translation = ((xy - visibleBlocks.TopLeft) * Overworld.Block_NumPixels) + startBlockPixel;
                    for (byte e = 0; e < Overworld.NumElevations; e++)
                    {
                        VBOData_InstancedLayoutBlock.AddInstance(_blockData[e], translation, blockUsedIndex);
                    }
                }
            }
        }

        // TODO: Obj shader
        // TODO: Allocating array every frame = bad, plus I have to sort them every frame by y coordinate
        // Hopefully we can solve that with depth testing with their shader. Just render them with the depth being their coordinate
        private void RenderObjs(Obj[] objs, in Rect visibleBlocks, Vec2I startBlockPixel)
        {
            var toleratedBlocks = Rect.FromCorners(visibleBlocks.TopLeft - _objTolerance, visibleBlocks.BottomRight + _objTolerance);
            startBlockPixel -= _objTolerance * Overworld.Block_NumPixels;

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

                Vec2I xyOnCurMap = v.Pos.XY + v.Map.BlockOffsetFromCurrentMap;
                if (!toleratedBlocks.Contains(xyOnCurMap))
                {
                    continue; // Make sure it's within the tolerance rect
                }

                Vec2I posOnScreen = ((xyOnCurMap - toleratedBlocks.TopLeft) * Overworld.Block_NumPixels) + startBlockPixel + v.VisualProgress;
                // Draw
                _objFrameBuffers[v.Pos.Elevation].Use();
                v.Draw(_screenSize, posOnScreen);

                // Add to debug data
#if DEBUG_OVERWORLD
                if (_debugEnabled)
                {
                    Debug_AddVisualObj(v, xyOnCurMap - visibleBlocks.TopLeft);
                }
#endif
            }
        }
    }
}
