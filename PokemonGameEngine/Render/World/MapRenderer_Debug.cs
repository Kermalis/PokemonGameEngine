#if DEBUG_OVERWORLD
using Kermalis.PokemonGameEngine.Render.Fonts;
using Kermalis.PokemonGameEngine.Render.GUIs;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Kermalis.PokemonGameEngine.World;
using Kermalis.PokemonGameEngine.World.Maps;
using Kermalis.PokemonGameEngine.World.Objs;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.World
{
    internal sealed partial class MapRenderer
    {
        private enum DebugBlockStatus : byte
        {
            None,
            Occupied,
            WasOccupied,
            ScreenCorner
        }
        private struct DebugVisibleBlock
        {
            /// <summary>Can be <see langword="null"/>, as in no border</summary>
            public MapLayout.Block Block;
            public Pos2D PositionOnScreen;
            public Map Map;
            public Pos2D MapPos;
        }
        private struct DebugVisibleObj
        {
            public Pos2D PositionOnScreen;
            public Pos2D? PositionOnScreen_MovingFrom;
        }

        private const int DEBUG_FBO_SCALE = 4;

        private readonly FrameBuffer _debugFrameBuffer;

        private int _debugNumVisibleBlocksX;
        private int _debugNumVisibleBlocksY;
        private DebugVisibleBlock[][] _debugVisibleBlocks;
        private readonly List<DebugVisibleObj> _debugVisibleObjs;

        private static Vector4 Debug_GetBlockStatusColor(DebugBlockStatus status)
        {
            switch (status)
            {
                case DebugBlockStatus.Occupied: return new Vector4(1f, 0f, 0.5f, 0.8f);
                case DebugBlockStatus.WasOccupied: return new Vector4(1f, 0f, 0.2f, 0.8f);
                case DebugBlockStatus.ScreenCorner: return new Vector4(0.2f, 0.2f, 1f, 0.8f);
                default: throw new ArgumentOutOfRangeException(nameof(status));
            }
        }

        private void Debug_UpdateVisibleBlocksBounds(Pos2D startBlock, Pos2D endBlock)
        {
            _debugNumVisibleBlocksX = endBlock.X - startBlock.X + 1;
            _debugNumVisibleBlocksY = endBlock.Y - startBlock.Y + 1;

            if (_debugVisibleBlocks.Length < _debugNumVisibleBlocksY)
            {
                Array.Resize(ref _debugVisibleBlocks, _debugNumVisibleBlocksY);
            }
            for (int y = 0; y < _debugNumVisibleBlocksY; y++)
            {
                if (_debugVisibleBlocks[y] is null)
                {
                    _debugVisibleBlocks[y] = new DebugVisibleBlock[_debugNumVisibleBlocksX];
                }
                else if (_debugVisibleBlocks[y].Length < _debugNumVisibleBlocksX)
                {
                    Array.Resize(ref _debugVisibleBlocks[y], _debugNumVisibleBlocksX);
                }
            }
        }
        private void Debug_UpdateVisibleBlocks(Map cameraMap, Pos2D startBlock, Pos2D endBlock, Pos2D startBlockPixel)
        {
            Debug_UpdateVisibleBlocksBounds(startBlock, endBlock);

            Pos2D pixel = startBlockPixel;
            Pos2D bXY;
            for (bXY.Y = startBlock.Y; bXY.Y <= endBlock.Y; bXY.Y++)
            {
                DebugVisibleBlock[] vbY = _debugVisibleBlocks[bXY.Y - startBlock.Y];
                for (bXY.X = startBlock.X; bXY.X <= endBlock.X; bXY.X++)
                {
                    DebugVisibleBlock vb;
                    vb.Block = cameraMap.GetBlock_CrossMap(bXY, out Pos2D newXY, out Map map);
                    vb.PositionOnScreen = pixel;
                    vb.Map = map;
                    vb.MapPos = newXY;
                    vbY[bXY.X - startBlock.X] = vb;

                    pixel.X += Overworld.Block_NumPixelsX;
                }
                pixel.X = startBlockPixel.X;
                pixel.Y += Overworld.Block_NumPixelsY;
            }
        }

        private void Debug_AddVisualObj(VisualObj v, Pos2D pixelPos)
        {
            DebugVisibleObj vo;
            vo.PositionOnScreen = pixelPos;
            Pos2D cur = v.Pos.XY;
            Pos2D from = v.MovingFromPos.XY;
            if (cur.Equals(from))
            {
                vo.PositionOnScreen_MovingFrom = null;
            }
            else
            {
                vo.PositionOnScreen_MovingFrom = vo.PositionOnScreen - ((cur - from) * Overworld.Block_NumPixels);
            }
            _debugVisibleObjs.Add(vo);
        }

        private static Rect2D Debug_GetPositionRect(Pos2D pos)
        {
            return new Rect2D(pos * DEBUG_FBO_SCALE, Overworld.Block_NumPixels * DEBUG_FBO_SCALE);
        }
        private void Debug_RenderObjs()
        {
            for (int i = 0; i < _debugVisibleObjs.Count; i++)
            {
                DebugVisibleObj vo = _debugVisibleObjs[i];
                GUIRenderer.Instance.FillRectangle(Debug_GetBlockStatusColor(DebugBlockStatus.Occupied), Debug_GetPositionRect(vo.PositionOnScreen));
                // Also color MovingFromPos
                if (vo.PositionOnScreen_MovingFrom is not null)
                {
                    GUIRenderer.Instance.FillRectangle(Debug_GetBlockStatusColor(DebugBlockStatus.WasOccupied), Debug_GetPositionRect(vo.PositionOnScreen_MovingFrom.Value));
                }
            }
        }
        private void Debug_RenderBlocks()
        {
            for (int y = 0; y < _debugNumVisibleBlocksY; y++)
            {
                bool yCorner = y == 0 || y == _debugNumVisibleBlocksY - 1;
                DebugVisibleBlock[] vbY = _debugVisibleBlocks[y];
                for (int x = 0; x < _debugNumVisibleBlocksX; x++)
                {
                    bool xCorner = x == 0 || x == _debugNumVisibleBlocksX - 1;
                    ref DebugVisibleBlock vb = ref vbY[x];

                    Rect2D posRect = Debug_GetPositionRect(vb.PositionOnScreen);
                    if (xCorner && yCorner)
                    {
                        GUIRenderer.Instance.FillRectangle(Debug_GetBlockStatusColor(DebugBlockStatus.ScreenCorner), posRect);
                    }
                    GUIRenderer.Instance.DrawRectangle(Colors.Black4, posRect);

                    Font f = Font.DefaultSmall;
                    Vector4[] fc = FontColors.DefaultBlack_I;
                    GUIString.CreateAndRenderOneTimeString(vb.Map.Name, f, fc, posRect.TopLeft.Move(1, 1));
                    GUIString.CreateAndRenderOneTimeString(vb.MapPos.ToString(), f, fc, posRect.TopLeft.Move(1, 9));
                }
            }
        }
        public void Debug_Render()
        {
            FrameBuffer c = FrameBuffer.Current;

            _debugFrameBuffer.Use();
            GL gl = Display.OpenGL;
            gl.ClearColor(Colors.Transparent);
            gl.Clear(ClearBufferMask.ColorBufferBit);

            Debug_RenderObjs();
            Debug_RenderBlocks();

            _debugFrameBuffer.RenderToScreen();
            c.Use();

            // Clear data
            for (int y = 0; y < _debugNumVisibleBlocksY; y++)
            {
                Array.Clear(_debugVisibleBlocks[y]);
            }
        }
    }
}
#endif