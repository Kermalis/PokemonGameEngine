using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Render.Fonts;
using Kermalis.PokemonGameEngine.Render.OpenGL;
using Silk.NET.OpenGL;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.GUI
{
    internal enum StringPrinterResult : byte
    {
        EnoughChars,
        FormFeed,
        VerticalTab,
        Ended
    }

    internal sealed class StringPrinter
    {
        private static readonly List<StringPrinter> _allStringPrinters = new();

        private readonly Window _window;

        private readonly GUIString _str;
        private int _index;

        private StringPrinterResult _result;
        private bool _pressedDone;
        public bool IsDone => _result == StringPrinterResult.Ended && _pressedDone;
        public bool IsEnded => _result == StringPrinterResult.Ended;

        public StringPrinter(Window w, string str, Font font, ColorF[] strColors, Pos2D pos, int scale = 1)
        {
            _window = w;
            GL gl = Game.OpenGL;
            _window.Image.PushFrameBuffer(gl);
            _str = new GUIString(Engine.Instance.StringBuffers.ApplyBuffers(str), font, strColors, pos: pos, allVisible: false, scale: scale);
            _window.ClearImagePushed(gl);
            GLHelper.PopFrameBuffer(gl);
            _allStringPrinters.Add(this);
        }
        public static StringPrinter CreateStandardMessageBox(Window w, string str, Font font, ColorF[] strColors, int scale = 1)
        {
            return new StringPrinter(w, str, font, strColors, Pos2D.FromRelative(0.05f, 0.01f), scale: scale);
        }

        public void Delete()
        {
            GL gl = Game.OpenGL;
            _str.Delete(gl);
            _allStringPrinters.Remove(this);
        }

        public void LogicTick()
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
                        _window.ClearImage();
                        _str.VisibleStart = _str.NumVisible;
                        _str.NumVisible = 0;
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

        private void AdvanceAndDrawString(int count)
        {
            GL gl = Game.OpenGL;
            _window.Image.PushFrameBuffer(gl);
            _result = DrawNext(gl, count);
            GLHelper.PopFrameBuffer(gl);
        }
        private StringPrinterResult DrawNext(GL gl, int count)
        {
            int i = 0;
            uint nx = 0, ny = 0;
            while (i < count && _index < _str.Text.Length)
            {
                Glyph glyph = _str.Font.GetGlyph(_str.Text, ref _index, ref nx, ref ny, out string readStr);
                if (readStr == "\f")
                {
                    return StringPrinterResult.FormFeed;
                }
                if (readStr == "\v")
                {
                    return StringPrinterResult.VerticalTab;
                }
                if (glyph is not null)
                {
                    _str.NumVisible++;
                    _str.Render(gl);
                    i++;
                }
            }
            return _index >= _str.Text.Length ? StringPrinterResult.Ended : StringPrinterResult.EnoughChars;
        }

        public static void ProcessAll()
        {
            foreach (StringPrinter s in _allStringPrinters.ToArray())
            {
                s.LogicTick();
            }
        }
    }
}
