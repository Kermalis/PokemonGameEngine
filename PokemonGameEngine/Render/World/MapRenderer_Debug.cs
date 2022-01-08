#if DEBUG_OVERWORLD
using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Render.GUIs;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Kermalis.PokemonGameEngine.World;
using Kermalis.PokemonGameEngine.World.Maps;
using Kermalis.PokemonGameEngine.World.Objs;
using Silk.NET.OpenGL;
using System;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.World
{
    internal sealed partial class MapRenderer // TODO: Render instances of the rects
    {
        private enum DebugBlockStatus : byte
        {
            None,
            ScreenCorner,
            Occupied,
            WasOccupied,
            CannotOccupy,
            BorderBlock
        }
        private struct DebugVisibleBlock
        {
            /// <summary>Can be <see langword="null"/>, as in no border</summary>
            public MapLayout.Block Block;
            public Vec2I PositionOnScreen;
            public Map Map;
            public Vec2I MapPos;
            public DebugBlockStatus Status;
        }

        private const int DEBUG_FBO_SCALE = 4;

        private readonly FrameBuffer2DColor _debugFrameBuffer;

        private bool _debugEnabled = true;
        private Vec2I _debugNumVisibleBlocks;
        private readonly DebugVisibleBlock[][] _debugVisibleBlocks;

        public void Debug_CheckToggle()
        {
            if (InputManager.JustPressed(Key.Select))
            {
                _debugEnabled = !_debugEnabled;
            }
        }

        private static Vector4 Debug_GetBlockStatusColor(DebugBlockStatus status)
        {
            const float ALPHA = 0.8f;
            switch (status)
            {
                case DebugBlockStatus.ScreenCorner: return new Vector4(0.2f, 0.5f, 1f, ALPHA);
                case DebugBlockStatus.Occupied: return new Vector4(0.7f, 1f, 1f, ALPHA);
                case DebugBlockStatus.WasOccupied: return new Vector4(1f, 1f, 0.2f, ALPHA);
                case DebugBlockStatus.CannotOccupy: return new Vector4(1f, 0.1f, 0.1f, ALPHA);
                case DebugBlockStatus.BorderBlock: return new Vector4(1f, 0.5f, 0.1f, ALPHA);
                default: throw new ArgumentOutOfRangeException(nameof(status));
            }
        }

        private void Debug_UpdateVisibleBlocks(Map camMap, in Rect visibleBlocks, Vec2I startBlockPixel)
        {
            _debugNumVisibleBlocks = visibleBlocks.GetSize();

            Vec2I pixel = startBlockPixel;
            Vec2I xy;
            for (xy.Y = visibleBlocks.TopLeft.Y; xy.Y <= visibleBlocks.BottomRight.Y; xy.Y++)
            {
                bool yCorner = xy.Y == visibleBlocks.TopLeft.Y || xy.Y == visibleBlocks.BottomRight.Y;
                DebugVisibleBlock[] vbY = _debugVisibleBlocks[xy.Y - visibleBlocks.TopLeft.Y];
                for (xy.X = visibleBlocks.TopLeft.X; xy.X <= visibleBlocks.BottomRight.X; xy.X++)
                {
                    DebugVisibleBlock vb;
                    vb.Block = camMap.GetBlock_CrossMap(xy, out Vec2I newXY, out Map map);
                    vb.PositionOnScreen = pixel;
                    vb.Map = map;
                    vb.MapPos = newXY;

                    bool xCorner = xy.X == visibleBlocks.TopLeft.X || xy.X == visibleBlocks.BottomRight.X;
                    if (xCorner && yCorner)
                    {
                        vb.Status = DebugBlockStatus.ScreenCorner;
                    }
                    else if (IsBorderBlock(xy - visibleBlocks.TopLeft, _debugNumVisibleBlocks.X))
                    {
                        vb.Status = DebugBlockStatus.BorderBlock;
                    }
                    else if (!vb.Block.Passage.HasFlag(LayoutBlockPassage.AllowOccupancy))
                    {
                        vb.Status = DebugBlockStatus.CannotOccupy;
                    }
                    else
                    {
                        vb.Status = DebugBlockStatus.None;
                    }

                    vbY[xy.X - visibleBlocks.TopLeft.X] = vb;

                    pixel.X += Overworld.Block_NumPixelsX;
                }
                pixel.X = startBlockPixel.X;
                pixel.Y += Overworld.Block_NumPixelsY;
            }
        }

        private void Debug_WriteObjStatus(DebugBlockStatus status, Vec2I pos)
        {
            if (pos.X < 0 || pos.Y < 0 || pos.X >= _debugNumVisibleBlocks.X || pos.Y >= _debugNumVisibleBlocks.Y)
            {
                return;
            }

            ref DebugVisibleBlock vb = ref _debugVisibleBlocks[pos.Y][pos.X];
            if (vb.Status is DebugBlockStatus.None or DebugBlockStatus.CannotOccupy)
            {
                vb.Status = status;
            }
        }
        private void Debug_AddVisualObj(VisualObj v, Vec2I pos)
        {
            Debug_WriteObjStatus(DebugBlockStatus.Occupied, pos);

            // If MovingFromPos is different then we should add that
            Vec2I cur = v.Pos.XY;
            Vec2I from = v.MovingFromPos.XY;
            if (cur != from)
            {
                Debug_WriteObjStatus(DebugBlockStatus.WasOccupied, pos - (cur - from));
            }
        }

        private void Debug_RenderBlocks()
        {
            Vec2I xy;
            for (xy.Y = 0; xy.Y < _debugNumVisibleBlocks.Y; xy.Y++)
            {
                DebugVisibleBlock[] vbY = _debugVisibleBlocks[xy.Y];
                for (xy.X = 0; xy.X < _debugNumVisibleBlocks.X; xy.X++)
                {
                    ref DebugVisibleBlock vb = ref vbY[xy.X];

                    var posRect = Rect.FromSize(vb.PositionOnScreen * DEBUG_FBO_SCALE, Overworld.Block_NumPixels * DEBUG_FBO_SCALE);
                    if (vb.Status != DebugBlockStatus.None)
                    {
                        GUIRenderer.Instance.FillRectangle(Debug_GetBlockStatusColor(vb.Status), posRect);
                    }
                    GUIRenderer.Instance.DrawRectangle(Colors.Black4, posRect);

                    Font f = Font.DefaultSmall;
                    Vector4[] fc = FontColors.DefaultBlack_I;
                    GUIString.CreateAndRenderOneTimeString(vb.Map.Name, f, fc, posRect.TopLeft.Plus(1, 1));
                    GUIString.CreateAndRenderOneTimeString(vb.MapPos.ToString(), f, fc, posRect.TopLeft.Plus(1, 9));
                }
            }
        }
        public void Debug_RenderToScreen()
        {
            if (!_debugEnabled)
            {
                return;
            }

            _debugFrameBuffer.Use();
            GL gl = Display.OpenGL;
            gl.ClearColor(Colors.Transparent);
            gl.Clear(ClearBufferMask.ColorBufferBit);

            Debug_RenderBlocks();

            _debugFrameBuffer.RenderToScreen();

            // Clear data
            for (int y = 0; y < _debugNumVisibleBlocks.Y; y++)
            {
                Array.Clear(_debugVisibleBlocks[y]);
            }
        }
    }
}
#endif