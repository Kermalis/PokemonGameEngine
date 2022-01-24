using Kermalis.PokemonGameEngine.Render.OpenGL;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.GUIs
{
    internal sealed class Window
    {
        public enum Decoration : byte
        {
            None,
            GrayRounded,
            Battle
        }

        private static readonly List<Window> _allWindows = new();

        private readonly Rect _innerRect;
        private readonly Vector4 _innerColor;
        private readonly FrameBuffer2DColor _frameBuffer;

        public Vec2I Pos;
        public bool IsInvisible;

        private Window(Vec2I pos, Vec2I innerSize, Vec2I totalSize, in Vector4 innerColor, Decoration decoration)
        {
            Pos = pos;
            _innerRect = Rect.FromSize(GetDecorationTopLeft(decoration), innerSize);
            _innerColor = innerColor;
            _frameBuffer = new FrameBuffer2DColor(totalSize);
            InitDecoration(decoration);
            ClearInner();
            _allWindows.Add(this);
        }
        public static Window CreateFromInnerSize(Vec2I pos, Vec2I innerSize, in Vector4 innerColor, Decoration decoration)
        {
            return new Window(pos, innerSize, innerSize + GetDecorationSizeAddition(decoration), innerColor, decoration);
        }
        public static Window CreateFromTotalSize(Vec2I pos, Vec2I totalSize, in Vector4 innerColor, Decoration decoration)
        {
            return new Window(pos, totalSize - GetDecorationSizeAddition(decoration), totalSize, innerColor, decoration);
        }
        public static Window CreateStandardMessageBox(in Vector4 innerColor, Vec2I availableSize)
        {
            const int OFFSET_X = 4;
            const int OFFSET_Y = 4;
            const int INNER_HEIGHT = 32;
            const Decoration D = Decoration.GrayRounded;

            Vec2I dSize = GetDecorationSizeAddition(D);
            var innerSize = new Vec2I(availableSize.X - OFFSET_X - OFFSET_X - dSize.X, INNER_HEIGHT);
            Vec2I totalSize = innerSize + dSize;
            return new Window(new Vec2I(OFFSET_X, availableSize.Y - OFFSET_Y - totalSize.Y), innerSize, totalSize, innerColor, D);
        }

        public void UseInner()
        {
            GL gl = Display.OpenGL;
            _frameBuffer.Use(gl);
            Display.Viewport(_innerRect);
        }
        /// <summary>Uses the framebuffer and clears the inside to the specified inner color</summary>
        public void ClearInner()
        {
            GL gl = Display.OpenGL;
            _frameBuffer.Use(gl);
            Display.Viewport(_innerRect);

            gl.Enable(EnableCap.ScissorTest);
            gl.Scissor(_innerRect.TopLeft.X, _innerRect.TopLeft.Y, (uint)_innerRect.GetWidth(), (uint)_innerRect.GetHeight());
            gl.ClearColor(Colors.Transparent);
            gl.Clear(ClearBufferMask.ColorBufferBit);
            gl.Disable(EnableCap.ScissorTest);

            // This handles transparency rather than ClearColor directly to the transparent color
            GUIRenderer.Rect(_innerColor, Rect.FromSize(new Vec2I(0, 0), _innerRect.GetSize()));
        }

        private static Vec2I GetDecorationSizeAddition(Decoration d)
        {
            switch (d)
            {
                case Decoration.None: return new Vec2I(0, 0);
                case Decoration.GrayRounded: return new Vec2I(10, 10);
                case Decoration.Battle: return new Vec2I(0, 10);
            }
            throw new ArgumentOutOfRangeException(nameof(d));
        }
        private static Vec2I GetDecorationTopLeft(Decoration d)
        {
            switch (d)
            {
                case Decoration.None: return new Vec2I(0, 0);
                case Decoration.GrayRounded: return new Vec2I(5, 5);
                case Decoration.Battle: return new Vec2I(0, 5);
            }
            throw new ArgumentOutOfRangeException(nameof(d));
        }
        private void InitDecoration(Decoration d)
        {
            if (d == Decoration.None)
            {
                return;
            }

            GL gl = Display.OpenGL;
            _frameBuffer.UseAndViewport(gl);
            gl.ClearColor(Colors.Transparent);
            gl.Clear(ClearBufferMask.ColorBufferBit);

            switch (d)
            {
                case Decoration.GrayRounded:
                {
                    GUIRenderer.Rect(_innerColor, Colors.V4FromRGB(80, 80, 80), Rect.FromSize(new Vec2I(0, 0), _frameBuffer.Size), 2, new(10));
                    break;
                }
                case Decoration.Battle:
                {
                    Vec2I totalSize = _frameBuffer.Size;
                    var lineSize1 = new Vec2I(totalSize.X, 1);
                    var lineSize2 = new Vec2I(totalSize.X, 2);
                    GUIRenderer.Rect(Colors.FromRGBA(0, 0, 0, 200), Rect.FromSize(new Vec2I(0, 0), lineSize1));
                    GUIRenderer.Rect(Colors.FromRGBA(30, 30, 30, 200), Rect.FromSize(new Vec2I(0, 1), lineSize1));
                    GUIRenderer.Rect(Colors.FromRGBA(60, 60, 60, 200), Rect.FromSize(new Vec2I(0, 2), lineSize1));
                    GUIRenderer.Rect(_innerColor, Rect.FromSize(new Vec2I(0, 3), lineSize2));

                    GUIRenderer.Rect(_innerColor, Rect.FromSize(new Vec2I(0, totalSize.Y - 5), lineSize2));
                    GUIRenderer.Rect(Colors.FromRGBA(60, 60, 60, 200), Rect.FromSize(new Vec2I(0, totalSize.Y - 3), lineSize1));
                    GUIRenderer.Rect(Colors.FromRGBA(30, 30, 30, 200), Rect.FromSize(new Vec2I(0, totalSize.Y - 2), lineSize1));
                    GUIRenderer.Rect(Colors.FromRGBA(0, 0, 0, 200), Rect.FromSize(new Vec2I(0, totalSize.Y - 1), lineSize1));
                    break;
                }
            }
        }

        public void Render()
        {
            if (!IsInvisible)
            {
                _frameBuffer.RenderColorTexture(Pos);
            }
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
            _frameBuffer.Delete();
            _allWindows.Remove(this);
        }
    }
}
