#if DEBUG_OVERWORLD
using Kermalis.PokemonGameEngine.Debug;
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
    internal sealed partial class MapRenderer
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
            public Vec2I PositionOnScreen;
            public DebugBlockStatus Status;

            public GUIString MapNameStr;
            public GUIString MapBlockPosStr;
            public GUIString MapBlockBehaviorStr;
        }

        private const int DEBUG_FBO_SCALE = 5;

        private readonly FrameBuffer2DColor _debugFrameBuffer;

        private bool _debugEnabled = true;
        private bool _debugBlockGridEnabled = true;
        private bool _debugBlockStatusEnabled = true;
        private bool _debugBlockTextEnabled = true;
        private Vec2I _debugNumVisibleBlocks;
        private readonly DebugVisibleBlock[][] _debugVisibleBlocks;

        public void Debug_CheckToggleInput()
        {
            if (InputManager.JustPressed(Key.R))
            {
                Debug_Toggle();
            }
        }
        public void Debug_Toggle()
        {
            _debugEnabled = !_debugEnabled;
            Log.WriteLineWithTime("Debug Map Renderer - Renderer toggled: " + _debugEnabled);
        }
        public void Debug_ToggleBlockGrid()
        {
            _debugBlockGridEnabled = !_debugBlockGridEnabled;
            Log.WriteLineWithTime("Debug Map Renderer - Block grid toggled: " + _debugBlockGridEnabled);
        }
        public void Debug_ToggleBlockStatus()
        {
            _debugBlockStatusEnabled = !_debugBlockStatusEnabled;
            Log.WriteLineWithTime("Debug Map Renderer - Block statuses toggled: " + _debugBlockStatusEnabled);
        }
        public void Debug_ToggleBlockText()
        {
            _debugBlockTextEnabled = !_debugBlockTextEnabled;
            Log.WriteLineWithTime("Debug Map Renderer - Block texts toggled: " + _debugBlockTextEnabled);
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
        private static void Debug_UpdateBlockString(ref GUIString gs, string str)
        {
            if (gs?.Text != str)
            {
                gs?.Delete();
                gs = new GUIString(str, Font.Default, FontColors.DefaultDebug);
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
                    ref DebugVisibleBlock vb = ref vbY[xy.X - visibleBlocks.TopLeft.X];

                    vb.PositionOnScreen = pixel;

                    if (_debugBlockStatusEnabled || _debugBlockTextEnabled)
                    {
                        MapLayout.Block block = camMap.GetBlock_CrossMap(xy, out Vec2I newXY, out Map map);
                        if (_debugBlockTextEnabled)
                        {
                            Debug_UpdateBlockString(ref vb.MapNameStr, map.Name);
                            Debug_UpdateBlockString(ref vb.MapBlockPosStr, newXY.ToString());
                            Debug_UpdateBlockString(ref vb.MapBlockBehaviorStr, block.BlocksetBlock.Behavior.ToString());
                        }
                        if (_debugBlockStatusEnabled)
                        {
                            bool xCorner = xy.X == visibleBlocks.TopLeft.X || xy.X == visibleBlocks.BottomRight.X;
                            if (xCorner && yCorner)
                            {
                                vb.Status = DebugBlockStatus.ScreenCorner;
                            }
                            else if (IsBorderBlock(xy - visibleBlocks.TopLeft, _debugNumVisibleBlocks.X)) // null border blocks are caught here too
                            {
                                vb.Status = DebugBlockStatus.BorderBlock;
                            }
                            else if (!block.Passage.HasFlag(LayoutBlockPassage.AllowOccupancy))
                            {
                                vb.Status = DebugBlockStatus.CannotOccupy;
                            }
                            else
                            {
                                vb.Status = DebugBlockStatus.None;
                            }
                        }
                    }

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

            ref DebugBlockStatus s = ref _debugVisibleBlocks[pos.Y][pos.X].Status;
            if (s is DebugBlockStatus.None or DebugBlockStatus.CannotOccupy)
            {
                s = status;
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

        public void Debug_RenderToScreen()
        {
            if (!_debugEnabled)
            {
                return;
            }
            if (!_debugBlockGridEnabled && !_debugBlockStatusEnabled && !_debugBlockTextEnabled)
            {
                return;
            }

            _debugFrameBuffer.Use();
            GL gl = Display.OpenGL;
            gl.ClearColor(Colors.Transparent);
            gl.Clear(ClearBufferMask.ColorBufferBit);

            Vec2I xy;
            for (xy.Y = 0; xy.Y < _debugNumVisibleBlocks.Y; xy.Y++)
            {
                DebugVisibleBlock[] vbY = _debugVisibleBlocks[xy.Y];
                for (xy.X = 0; xy.X < _debugNumVisibleBlocks.X; xy.X++)
                {
                    ref DebugVisibleBlock vb = ref vbY[xy.X];
                    var posRect = Rect.FromSize(vb.PositionOnScreen * DEBUG_FBO_SCALE, Overworld.Block_NumPixels * DEBUG_FBO_SCALE);

                    if (_debugBlockStatusEnabled && vb.Status != DebugBlockStatus.None)
                    {
                        GUIRenderer.Rect(Debug_GetBlockStatusColor(vb.Status), posRect);
                    }
                    if (_debugBlockGridEnabled)
                    {
                        GUIRenderer.Rect(Colors.Black4, posRect, lineThickness: 1);
                    }
                    if (_debugBlockTextEnabled)
                    {
                        vb.MapNameStr.Render(posRect.TopLeft.Plus(1, 1));
                        vb.MapBlockPosStr.Render(posRect.TopLeft.Plus(1, 17));
                        vb.MapBlockBehaviorStr.Render(posRect.TopLeft.Plus(1, 33));
                    }
                }
            }

            _debugFrameBuffer.RenderToScreen();
        }
    }
}
#endif