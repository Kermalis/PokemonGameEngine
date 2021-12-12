using Silk.NET.OpenGL;
using System.Collections.Generic;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.OpenGL
{
    internal static partial class GLHelper
    {
        public const int MaxActiveTextures = 12;

        private static readonly Stack<uint> _fboWidths = new();
        private static readonly Stack<uint> _fboHeights = new();
        private static readonly Stack<uint> _fbos = new();
        public static uint CurrentWidth;
        public static uint CurrentHeight;
        public static Size2D CurrentSize;
        private static uint _currentFBO;

        public static void ClearColor(GL gl, in Vector3 color)
        {
            gl.ClearColor(color.X, color.Y, color.Z, 1f);
        }
        public static void ClearColor(GL gl, in Vector4 color)
        {
            gl.ClearColor(color.X, color.Y, color.Z, color.W);
        }

        public static void PushFrameBuffer(GL gl, uint fbo, uint w, uint h)
        {
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
            gl.DrawBuffer(DrawBufferMode.ColorAttachment0);
            gl.ReadBuffer(ReadBufferMode.ColorAttachment0);
            gl.Viewport(0, 0, w, h);
            _fboWidths.Push(CurrentWidth);
            _fboHeights.Push(CurrentHeight);
            _fbos.Push(_currentFBO);
            CurrentWidth = w;
            CurrentHeight = h;
            CurrentSize = new Size2D(w, h);
            _currentFBO = fbo;
        }
        public static void PopFrameBuffer(GL gl)
        {
            CurrentWidth = _fboWidths.Pop();
            CurrentHeight = _fboHeights.Pop();
            CurrentSize = new Size2D(CurrentWidth, CurrentHeight);
            _currentFBO = _fbos.Pop();
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, _currentFBO);
            gl.DrawBuffer(DrawBufferMode.ColorAttachment0);
            gl.ReadBuffer(ReadBufferMode.ColorAttachment0);
            gl.Viewport(0, 0, CurrentWidth, CurrentHeight);
        }
    }
}
