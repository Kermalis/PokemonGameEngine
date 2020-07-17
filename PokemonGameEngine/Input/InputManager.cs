using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Input
{
    internal static class InputManager
    {
        public sealed class KeyDownData
        {
            public bool PressChecked { get; set; }
            public ulong PressTime { get; set; }
        }

        private static readonly Dictionary<Key, KeyDownData> _pressed = new Dictionary<Key, KeyDownData>();
        static InputManager()
        {
            foreach (Key k in Enum.GetValues(typeof(Key)))
            {
                _pressed.Add(k, new KeyDownData());
            }
        }

        public static void OnKeyDown(Avalonia.Input.KeyEventArgs e, bool down)
        {
            Key key;
            switch (e.Key)
            {
                case Avalonia.Input.Key.Q: key = Key.L; break;
                case Avalonia.Input.Key.W: key = Key.R; break;
                case Avalonia.Input.Key.A: key = Key.X; break;
                case Avalonia.Input.Key.S: key = Key.Y; break;
                case Avalonia.Input.Key.Z: key = Key.B; break;
                case Avalonia.Input.Key.X: key = Key.A; break;
                case Avalonia.Input.Key.Left: key = Key.Left; break;
                case Avalonia.Input.Key.Right: key = Key.Right; break;
                case Avalonia.Input.Key.Down: key = Key.Down; break;
                case Avalonia.Input.Key.Up: key = Key.Up; break;
                case Avalonia.Input.Key.Enter: key = Key.Start; break;
                case Avalonia.Input.Key.RightShift: key = Key.Select; break;
                default: return;
            }
            KeyDownData p = _pressed[key];
            if (down)
            {
                p.PressTime++;
            }
            else
            {
                p.PressChecked = false;
                p.PressTime = 0;
            }
            e.Handled = true;
        }

        public static bool IsPressed(Key key)
        {
            KeyDownData p = _pressed[key];
            bool ret = !p.PressChecked && p.PressTime != 0;
            if (ret)
            {
                p.PressChecked = true;
            }
            return ret;
        }
        public static bool IsDown(Key key)
        {
            KeyDownData p = _pressed[key];
            bool ret = p.PressTime != 0;
            if (ret)
            {
                p.PressChecked = true;
            }
            return ret;
        }

        // For debugging
        public static string GetKeys()
        {
            string s = string.Empty;
            foreach (KeyValuePair<Key, KeyDownData> kvp in _pressed)
            {
                s += kvp.Key.ToString() + ": " + kvp.Value.PressTime.ToString() + Environment.NewLine;
            }
            return s;
        }
    }
}
