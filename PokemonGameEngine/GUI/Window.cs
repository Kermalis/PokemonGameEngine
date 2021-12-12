using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Render.Images;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Silk.NET.OpenGL;
using System.Collections.Generic;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.GUI
{
    internal sealed class Window
    {
        private static readonly List<Window> _allWindows = new();

        private readonly RelPos2D _pos;
        private readonly Vector4 _backColor;

        public bool IsInvisible;
        public readonly WriteableImage Image;

        public Window(RelPos2D pos, Size2D size, in Vector4 backColor)
        {
            _pos = pos;
            _backColor = backColor;
            Image = new WriteableImage(size);
            ClearImage();
            _allWindows.Add(this);
        }
        public static Window CreateStandardMessageBox(in Vector4 backColor)
        {
            return new Window(new RelPos2D(0f, 0.79f), Size2D.FromRelative(1f, 0.17f), backColor);
        }

        public void ClearImage()
        {
            Image.FrameBuffer.Push();
            ClearImagePushed();
            FrameBuffer.Pop();
        }
        public void ClearImagePushed()
        {
            GL gl = Display.OpenGL;
            gl.ClearColor(_backColor);
            gl.Clear(ClearBufferMask.ColorBufferBit);
        }

        public void Render()
        {
            if (IsInvisible)
            {
                return;
            }
            Image.Render(_pos.Absolute());
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
            Image.DeductReference();
            _allWindows.Remove(this);
        }
    }
}
