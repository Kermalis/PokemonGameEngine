using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.UI;

namespace Kermalis.PokemonGameEngine.GUI
{
    internal sealed class MessageBox
    {
        public string Text { get; private set; }
        public readonly Font Font;
        public readonly uint[] FontColors;

        public float X;
        public float XOffset;
        public float Y;
        public float YOffset;
        public float Width;
        public float Height;

        public bool IsDone => _result == StringPrinterResult.Ended && _pressedDone;
        public bool IsClosed { get; private set; }

        private readonly Sprite _sprite;
        private StringPrinter _printer;
        private StringPrinterResult _result;
        private bool _pressedDone;

        public MessageBox()
        {
            Font = Font.Default;
            FontColors = Font.DefaultWhite;

            X = 0.00f;
            XOffset = 0.05f;
            Y = 0.79f;
            YOffset = 0.01f;
            Width = 1.00f;
            Height = 0.16f;

            _sprite = new Sprite((int)(Program.RenderWidth * Width), (int)(Program.RenderHeight * Height));
            IsClosed = true;
        }

        public unsafe void SetText(string text)
        {
            text = Game.Instance.StringBuffers.ApplyBuffers(text);
            Text = text;
            _printer = new StringPrinter(text, (int)(Program.RenderWidth * XOffset), (int)(Program.RenderHeight * YOffset), Font, FontColors);
            _result = StringPrinterResult.EnoughChars;
            _pressedDone = false;
            _sprite.Draw(DrawBackground);
        }
        public void Open()
        {
            if (!IsClosed)
            {
                return;
            }
            IsClosed = false;
            Game.Instance.MessageBoxes.Add(this);
        }

        private unsafe void DrawBackground(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            RenderUtils.OverwriteRectangle(bmpAddress, bmpWidth, bmpHeight, RenderUtils.Color(49, 49, 49, 128));
        }
        private unsafe void AdvanceAndDrawString(int speed)
        {
            unsafe void DrawString(uint* bmpAddress, int bmpWidth, int bmpHeight)
            {
                _result = _printer.DrawNext(bmpAddress, bmpWidth, bmpHeight, speed);
            }
            _sprite.Draw(DrawString);
        }

        public unsafe void LogicTick()
        {
            bool IsDown()
            {
                return InputManager.IsDown(Key.A) || InputManager.IsDown(Key.B);
            }
            bool IsPressed()
            {
                return InputManager.IsPressed(Key.A) || InputManager.IsPressed(Key.B);
            }
            switch (_result)
            {
                case StringPrinterResult.EnoughChars:
                {
                    int speed = IsDown() ? 3 : 1;
                    AdvanceAndDrawString(speed);
                    break;
                }
                case StringPrinterResult.FormFeed:
                {
                    if (IsPressed())
                    {
                        _sprite.Draw(DrawBackground);
                        _result = StringPrinterResult.EnoughChars;
                    }
                    break;
                }
                case StringPrinterResult.VerticalTab:
                {
                    if (IsPressed())
                    {
                        _result = StringPrinterResult.EnoughChars;
                    }
                    break;
                }
                case StringPrinterResult.Ended:
                {
                    if (IsPressed())
                    {
                        _pressedDone = true;
                        return;
                    }
                    break;
                }
            }
        }

        public unsafe void Render(uint* bmpAddress, int bmpWidth, int bmpHeight)
        {
            _sprite.DrawOn(bmpAddress, bmpWidth, bmpHeight, X, Y);
        }

        public void Close()
        {
            if (IsClosed)
            {
                return;
            }
            IsClosed = true;
            Game.Instance.MessageBoxes.Remove(this);
        }
    }
}
