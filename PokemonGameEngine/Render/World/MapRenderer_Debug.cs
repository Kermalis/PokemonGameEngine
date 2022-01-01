#if DEBUG_OVERWORLD
using Kermalis.PokemonGameEngine.Render.Fonts;
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
        private enum DebugBlockStatus
        {
            None,
            Occupied,
            WasOccupied
        }

        private const int DEBUG_FBO_SCALE = 4;

        private readonly FrameBuffer _debugFrameBuffer;

        private static Vector4 Debug_GetBlockStatusColor(DebugBlockStatus status)
        {
            switch (status)
            {
                case DebugBlockStatus.Occupied: return new Vector4(1f, 0f, 0.5f, 0.8f);
                case DebugBlockStatus.WasOccupied: return new Vector4(1f, 0f, 0.2f, 0.8f);
                default: throw new ArgumentOutOfRangeException(nameof(status));
            }
        }
        private DebugBlockStatus Debug_IsBlockOccupied(Map map, in Pos2D mapXY)
        {
            for (int i = 0; i < _visibleObjs.Count; i++)
            {
                VisualObj o = _visibleObjs[i].Obj;
                if (o.Map == map)
                {
                    if (o.Pos.XY.Equals(mapXY))
                    {
                        return DebugBlockStatus.Occupied;
                    }
                    if (o.MovingFromPos.XY.Equals(mapXY))
                    {
                        return DebugBlockStatus.WasOccupied;
                    }
                }
            }
            return DebugBlockStatus.None;
        }

        public void Debug_RenderBlocks()
        {
            FrameBuffer c = FrameBuffer.Current;
            _debugFrameBuffer.Use();
            GL gl = Display.OpenGL;
            gl.ClearColor(Colors.Transparent);
            gl.Clear(ClearBufferMask.ColorBufferBit);

            for (int y = 0; y < _numVisibleBlocksY; y++)
            {
                VisibleBlock[] vbY = _visibleBlocks[y];
                for (int x = 0; x < _numVisibleBlocksX; x++)
                {
                    ref VisibleBlock vb = ref vbY[x];

                    var posRect = new Rect2D(vb.PositionOnScreen * DEBUG_FBO_SCALE, new Size2D(Overworld.Block_NumPixelsX * DEBUG_FBO_SCALE, Overworld.Block_NumPixelsY * DEBUG_FBO_SCALE));
                    DebugBlockStatus status = Debug_IsBlockOccupied(vb.Map, vb.MapPos);
                    if (status != DebugBlockStatus.None)
                    {
                        GUIRenderer.Instance.FillRectangle(Debug_GetBlockStatusColor(status), posRect);
                    }
                    GUIRenderer.Instance.DrawRectangle(Colors.Black4, posRect);

                    Font f = Font.DefaultSmall;
                    Vector4[] fc = FontColors.DefaultBlack_I;
                    GUIString.CreateAndRenderOneTimeString(vb.Map.Name, f, fc, posRect.TopLeft.Move(1, 1));
                    GUIString.CreateAndRenderOneTimeString(vb.MapPos.ToString(), f, fc, posRect.TopLeft.Move(1, 9));
                }
            }

            _debugFrameBuffer.RenderToScreen();
            c.Use();
        }
    }
}
#endif