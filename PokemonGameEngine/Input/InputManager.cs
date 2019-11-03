using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Input
{
    internal static class InputManager
    {
        private static readonly Dictionary<Key, bool> _pressed = new Dictionary<Key, bool>();
        static InputManager()
        {
            foreach (Key k in Enum.GetValues(typeof(Key)))
            {
                _pressed.Add(k, false);
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
                case Avalonia.Input.Key.Z: key = Key.A; break;
                case Avalonia.Input.Key.X: key = Key.B; break;
                case Avalonia.Input.Key.Left: key = Key.Left; break;
                case Avalonia.Input.Key.Right: key = Key.Right; break;
                case Avalonia.Input.Key.Down: key = Key.Down; break;
                case Avalonia.Input.Key.Up: key = Key.Up; break;
                case Avalonia.Input.Key.Enter: key = Key.Start; break;
                case Avalonia.Input.Key.RightShift: key = Key.Select; break;
                default: return;
            }
            _pressed[key] = down;
            e.Handled = true;
        }

        public static bool IsPressed(Key key)
        {
            return _pressed[key];
        }

        public static string GetKeys()
        {
            string s = string.Empty;
            foreach (KeyValuePair<Key, bool> kvp in _pressed)
            {
                s += kvp.Key.ToString() + ": " + kvp.Value.ToString() + Environment.NewLine;
            }
            return s;
        }
    }
}
