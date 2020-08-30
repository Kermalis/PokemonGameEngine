using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.UI;

namespace Kermalis.PokemonGameEngine.GUI
{
    internal sealed class MessageBox
    {
        public readonly string Text;
        public readonly Font Font;
        public readonly uint[] FontColors;

        public float X;
        public float XOffset;
        public float Y;
        public float YOffset;
        public float Width;
        public float Height;

        private readonly Sprite _sprite;
        private readonly StringPrinter _printer;
        private bool _done;

        public unsafe MessageBox(string text)
        {
            Text = text;

            Font = Font.Default;
            FontColors = Font.DefaultWhite;

            X = 0.00f;
            XOffset = 0.05f;
            Y = 0.79f;
            YOffset = 0.01f;
            Width = 1.00f;
            Height = 0.16f;

            _printer = new StringPrinter(text, (int)(Program.RenderWidth * XOffset), (int)(Program.RenderHeight * YOffset), Font, FontColors);
            _sprite = new Sprite((int)(Program.RenderWidth * Width), (int)(Program.RenderHeight * Height));
            _sprite.Draw(DrawBackground);
        }
        private unsafe void DrawBackground(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            RenderUtils.FillRectangle(bmpAddress, bmpWidth, bmpHeight, RenderUtils.Color(49, 49, 49, 128));
        }
        private unsafe void AdvanceAndDrawString(int speed)
        {
            unsafe void DrawString(uint* bmpAddress, int bmpWidth, int bmpHeight)
            {
                _done = _printer.DrawNext(bmpAddress, bmpWidth, bmpHeight, speed);
            }
            _sprite.Draw(DrawString);
        }

        public void LogicTick()
        {
            if (_done)
            {
                // TODO: Paragraphs
                // Close
                if (InputManager.IsPressed(Key.A) || InputManager.IsPressed(Key.B))
                {
                    Close();
                    return;
                }
            }
            else
            {
                // Advance text
                int speed = InputManager.IsDown(Key.A) || InputManager.IsDown(Key.B) ? 3 : 1;
                AdvanceAndDrawString(speed);
            }
        }

        public unsafe void Render(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            _sprite.DrawOn(bmpAddress, bmpWidth, bmpHeight, (int)(bmpWidth * X), (int)(bmpHeight * Y));
        }

        public void Close()
        {
            Game.Instance.MessageBoxes.Remove(this);
        }
    }
}
