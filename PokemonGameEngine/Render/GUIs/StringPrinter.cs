using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Input;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.GUIs
{
    internal sealed class StringPrinter : IConnectedListObject<StringPrinter>
    {
        private enum StringPrinterResult : byte
        {
            EnoughChars,
            FormFeed,
            VerticalTab,
            Ended
        }

        // Chars per second
        private const float PRINT_SPEED_SLOW = 10f;
        //private const float PRINT_SPEED_NORMAL = 40f;
        private const float PRINT_SPEED_FAST = 70f;

        public static readonly ConnectedList<StringPrinter> AllStringPrinters = new();

        private readonly Window _window;

        private readonly GUIString _str;
        private int _index;
        private float _charTimer;

        private StringPrinterResult _result;
        private bool _pressedDone;
        public bool IsDone => _result == StringPrinterResult.Ended && _pressedDone;
        public bool IsEnded => _result == StringPrinterResult.Ended;

        public StringPrinter Prev { get; set; }
        public StringPrinter Next { get; set; }

        public StringPrinter(Window w, string str, Font font, Vector4[] strColors, Vec2I pos, int scale = 1)
        {
            _window = w;
            w.ClearInner(); // Required if we're reusing the window
            _str = new GUIString(Game.Instance.StringBuffers.ApplyBuffers(str), font, strColors, pos: pos, allVisible: false, scale: scale);
            AllStringPrinters.Add(this);
        }

        public void Update()
        {
            bool IsDown()
            {
                return InputManager.IsDown(Key.A) || InputManager.IsDown(Key.B);
            }
            bool JustPressed()
            {
                return InputManager.JustPressed(Key.A) || InputManager.JustPressed(Key.B);
            }
            switch (_result)
            {
                case StringPrinterResult.EnoughChars:
                {
                    float speed = IsDown() ? PRINT_SPEED_FAST : PRINT_SPEED_SLOW;
                    AdvanceAndDrawString(speed);
                    break;
                }
                case StringPrinterResult.FormFeed:
                {
                    if (JustPressed())
                    {
                        _window.ClearInner();
                        _str.VisibleStart = _str.NumVisible;
                        _str.NumVisible = 0;
                        _result = StringPrinterResult.EnoughChars;
                    }
                    break;
                }
                case StringPrinterResult.VerticalTab:
                {
                    if (JustPressed())
                    {
                        _result = StringPrinterResult.EnoughChars;
                    }
                    break;
                }
                case StringPrinterResult.Ended:
                {
                    if (JustPressed())
                    {
                        _pressedDone = true;
                        return;
                    }
                    break;
                }
            }
        }

        private void AdvanceAndDrawString(float speed)
        {
            _charTimer += Display.DeltaTime * speed;
            int count = (int)_charTimer;
            _charTimer %= 1f;
            if (count >= 1)
            {
                _window.UseInner();
                _result = DrawNext(count);
            }
        }
        private StringPrinterResult DrawNext(int count)
        {
            int i = 0;
            var cursor = new Vec2I(0, 0);
            while (i < count && _index < _str.Text.Length)
            {
                Glyph g = _str.Font.GetGlyph(_str.Text, ref _index, ref cursor, out string readStr);
                if (readStr == "\f")
                {
                    return StringPrinterResult.FormFeed;
                }
                if (readStr == "\v")
                {
                    return StringPrinterResult.VerticalTab;
                }
                if (g is not null)
                {
                    _str.NumVisible++;
                    _str.Render();
                    i++;
                }
            }
            return _index >= _str.Text.Length ? StringPrinterResult.Ended : StringPrinterResult.EnoughChars;
        }

        public void Dispose()
        {
            _str.Delete();
            AllStringPrinters.Remove(this, dispose: false);
        }
    }
}
