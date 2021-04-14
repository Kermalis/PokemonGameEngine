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

        public readonly Sprite Sprite;

        public Window(float x, float y, uint backColor)
            : this(x, y, Program.RenderWidth, Program.RenderHeight, backColor) { }
        public Window(float x, float y, float w, float h, uint backColor)
            : this(x, y, (int)(Program.RenderWidth * w), (int)(Program.RenderHeight * h), backColor) { }
        public Window(float x, float y, int pxWidth, int pxHeight, uint backColor)
        {
            _x = x;
            _y = y;
            _backColor = backColor;
            Sprite = new Sprite(pxWidth, pxHeight);
            ClearSprite();
            Game.Instance.Windows.Add(this);
        }

        public void ClearSprite()
        {
            RenderUtils.OverwriteRectangle(Sprite, _backColor);
        }
        public unsafe void ClearSprite(int x, int y, int width, int height)
        {
            void Clear(uint* bmpAddress, int bmpWidth, int bmpHeight)
            {
                RenderUtils.OverwriteRectangle(bmpAddress, bmpWidth, bmpHeight, x, y, width, height, _backColor);
            }
            Sprite.Draw(Clear);
        }

        public unsafe void Render(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            Sprite.DrawOn(bmpAddress, bmpWidth, bmpHeight, _x, _y);
        }

        public void Close()
        {
            Game.Instance.Windows.Remove(this);
        }
    }
}
