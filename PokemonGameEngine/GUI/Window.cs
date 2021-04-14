using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.UI;

namespace Kermalis.PokemonGameEngine.GUI
{
    internal sealed class Window
    {
        private readonly float _x;
        private readonly float _y;

        public readonly Sprite Sprite;

        public Window(float x, float y)
            : this(x, y, Program.RenderWidth, Program.RenderHeight) { }
        public Window(float x, float y, float w, float h)
            : this(x, y, (int)(Program.RenderWidth * w), (int)(Program.RenderHeight * h)) { }
        public Window(float x, float y, int pxWidth, int pxHeight)
        {
            _x = x;
            _y = y;
            Sprite = new Sprite(pxWidth, pxHeight);
            ClearSprite();
            Game.Instance.Windows.Add(this);
        }

        public void ClearSprite()
        {
            uint color = RenderUtils.Color(255, 255, 255, 255);
            RenderUtils.OverwriteRectangle(Sprite, color);
        }
        public unsafe void ClearSprite(int x, int y, int width, int height)
        {
            uint color = RenderUtils.Color(255, 255, 255, 255);
            void Clear(uint* bmpAddress, int bmpWidth, int bmpHeight)
            {
                RenderUtils.OverwriteRectangle(bmpAddress, bmpWidth, bmpHeight, x, y, width, height, color);
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
