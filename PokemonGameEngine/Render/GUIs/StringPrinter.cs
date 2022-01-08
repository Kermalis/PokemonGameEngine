using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Input;
using System.Collections.Generic;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.GUIs
{
    internal sealed class StringPrinter
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

        private static readonly List<StringPrinter> _allStringPrinters = new();

        private readonly Window _window;

        private readonly GUIString _str;
        private int _index;
        private float _charTimer;

        private StringPrinterResult _result;
        private bool _pressedDone;
        public bool IsDone => _result == StringPrinterResult.Ended && _pressedDone;
        public bool IsEnded => _result == StringPrinterResult.Ended;

        public StringPrinter(Window w, string str, Font font, Vector4[] strColors, Vec2I pos, int scale = 1)
        {
            _window = w;
            _str = new GUIString(Game.Instance.StringBuffers.ApplyBuffers(str), font, strColors, pos: pos, allVisible: false, scale: scale);
            w.Clear();
            _allStringPrinters.Add(this);
        }
        public static StringPrinter CreateStandardMessageBox(Window w, string str, Font font, Vector4[] strColors, Vec2I totalSize, int scale = 1)
        {
            return new StringPrinter(w, str, font, strColors, Vec2I.FromRelative(0.05f, 0.01f, totalSize), scale: scale);
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
                        _window.Clear();
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
                _window.FrameBuffer.Use();
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

        public static void UpdateAll()
        {
            foreach (StringPrinter s in _allStringPrinters.ToArray())
            {
                s.Update();
            }
        }

        public void Delete()
        {
            _str.Delete();
            _allStringPrinters.Remove(this);
        }
    }
}
