using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.UI;

namespace Kermalis.PokemonGameEngine.GUI
{
    internal enum StringPrinterResult : byte
    {
        EnoughChars,
        FormFeed,
        VerticalTab,
        Ended,
    }

    // 1x scale only for now
    internal sealed class StringPrinter
    {
        private readonly Window _window;

        private readonly string _str;
        private readonly Font _font;
        private readonly uint[] _fontColors;
        private readonly int _startX;
        private readonly int _startY;
        private int _nextXOffset;
        private int _nextYOffset;
        private int _index;

        private StringPrinterResult _result;
        private bool _pressedDone;
        public bool IsDone => _result == StringPrinterResult.Ended && _pressedDone;

        public StringPrinter(Window w, string str, float x, float y, Font font, uint[] fontColors)
            : this(w, str, (int)(Program.RenderWidth * x), (int)(Program.RenderHeight * y), font, fontColors) { }
        public StringPrinter(Window w, string str, int x, int y, Font font, uint[] fontColors)
        {
            _window = w;
            _str = Game.Instance.StringBuffers.ApplyBuffers(str);
            _startX = x;
            _startY = y;
            _font = font;
            _fontColors = fontColors;

            _window.ClearSprite();
            Game.Instance.StringPrinters.Add(this);
        }

        public void Close()
        {
            Game.Instance.StringPrinters.Remove(this);
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
                        _window.ClearSprite();
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

        private unsafe void AdvanceAndDrawString(int speed)
        {
            unsafe void DrawString(uint* bmpAddress, int bmpWidth, int bmpHeight)
            {
                _result = DrawNext(bmpAddress, bmpWidth, bmpHeight, speed);
            }
            _window.Sprite.Draw(DrawString);
        }
        private unsafe StringPrinterResult DrawNext(uint* bmpAddress, int bmpWidth, int bmpHeight, int count)
        {
            int i = 0;
            while (i < count && _index < _str.Length)
            {
                int curX = _startX + _nextXOffset;
                int curY = _startY + _nextYOffset;
                Font.Glyph glyph = _font.GetGlyph(_str, ref _index, ref _nextXOffset, ref _nextYOffset, out string readStr);
                if (readStr == "\f")
                {
                    return StringPrinterResult.FormFeed;
                }
                if (readStr == "\v")
                {
                    return StringPrinterResult.VerticalTab;
                }
                if (glyph != null)
                {
                    _font.DrawGlyph(bmpAddress, bmpWidth, bmpHeight, curX, curY, glyph, _fontColors);
                    i++;
                }
            }
            return _index >= _str.Length ? StringPrinterResult.Ended : StringPrinterResult.EnoughChars;
        }
    }
}
