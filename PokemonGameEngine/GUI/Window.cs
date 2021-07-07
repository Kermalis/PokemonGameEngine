using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.UI;

namespace Kermalis.PokemonGameEngine.GUI
{
    internal sealed class Window
    {
        private readonly float _x;
        private readonly float _y;
        private readonly uint _backColor;

        public bool IsInvisible;
        public readonly Image Image;

        public Window(float x, float y, uint backColor)
            : this(x, y, Program.RenderWidth, Program.RenderHeight, backColor) { }
        public Window(float x, float y, float w, float h, uint backColor)
            : this(x, y, (int)(Program.RenderWidth * w), (int)(Program.RenderHeight * h), backColor) { }
        public Window(float x, float y, int pxWidth, int pxHeight, uint backColor)
        {
            _x = x;
            _y = y;
            _backColor = backColor;
            Image = new Image(pxWidth, pxHeight);
            ClearImage();
            Game.Instance.Windows.Add(this);
        }

        public void ClearImage()
        {
            Renderer.OverwriteRectangle(Image, _backColor);
        }
        public unsafe void ClearImage(int x, int y, int width, int height)
        {
            void Clear(uint* dst, int dstW, int dstH)
            {
                Renderer.OverwriteRectangle(dst, dstW, dstH, x, y, width, height, _backColor);
            }
            Image.Draw(Clear);
        }

        public unsafe void Render(uint* dst, int dstW, int dstH)
        {
            if (IsInvisible)
            {
                return;
            }
            Image.DrawOn(dst, dstW, dstH, _x, _y);
        }

        public void Close()
        {
            Game.Instance.Windows.Remove(this);
        }
    }
}
