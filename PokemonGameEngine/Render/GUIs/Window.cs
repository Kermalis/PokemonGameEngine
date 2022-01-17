using Kermalis.PokemonGameEngine.Render.OpenGL;
using Silk.NET.OpenGL;
using System.Collections.Generic;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.GUIs
{
    internal sealed class Window
    {
        private static readonly List<Window> _allWindows = new();

        private readonly Vec2I _pos;
        private readonly Vector4 _backColor;

        public bool IsInvisible;
        public readonly FrameBuffer2DColor FrameBuffer;

        public Window(Vec2I pos, Vec2I size, in Vector4 backColor)
        {
            _pos = pos;
            _backColor = backColor;
            FrameBuffer = new FrameBuffer2DColor(size);
            Clear();
            _allWindows.Add(this);
        }
        public static Window CreateStandardMessageBox(in Vector4 backColor, Vec2I totalSize)
        {
            return new Window(Vec2I.FromRelative(0f, 0.79f, totalSize), Vec2I.FromRelative(1f, 0.17f, totalSize), backColor);
        }

        /// <summary>Uses <see cref="FrameBuffer"/> and clears it to the back color</summary>
        public void Clear()
        {
            GL gl = Display.OpenGL;
            FrameBuffer.Use(gl);
            gl.ClearColor(_backColor);
            gl.Clear(ClearBufferMask.ColorBufferBit);
        }

        public void Render()
        {
            if (IsInvisible)
            {
                return;
            }
            FrameBuffer.RenderColorTexture(_pos);
        }
        public static void RenderAll()
        {
            foreach (Window w in _allWindows)
            {
                w.Render();
            }
        }

        public void Close()
        {
            FrameBuffer.Delete();
            _allWindows.Remove(this);
        }
    }
}
