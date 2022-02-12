using Silk.NET.SDL;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Input
{
    internal static class Keyboard
    {
        private static readonly Dictionary<KeyCode, Key> _keyBinds = new();
        private static readonly Dictionary<Key, PressData> _keys = new();

        static Keyboard()
        {
            _keyBinds = GetDefaultKeyBinds();
            _keys = PressData.CreateDict(Enum.GetValues<Key>());
        }

        private static Dictionary<KeyCode, Key> GetDefaultKeyBinds()
        {
            return new Dictionary<KeyCode, Key>(13)
            {
                { KeyCode.KQ, Key.L },
                { KeyCode.KW, Key.R },
                { KeyCode.KLeft, Key.Left },
                { KeyCode.KRight, Key.Right },
                { KeyCode.KUp, Key.Up },
                { KeyCode.KDown, Key.Down },
                { KeyCode.KReturn, Key.Start },
                { KeyCode.KRshift, Key.Select },
                { KeyCode.KA, Key.X },
                { KeyCode.KS, Key.Y },
                { KeyCode.KZ, Key.B },
                { KeyCode.KX, Key.A },
                { KeyCode.KF12, Key.Screenshot }
            };
        }

        public static void Prepare()
        {
            PressData.PrepareMany(_keys.Values);
        }

        public static bool IsDown(Key k)
        {
            return _keys[k].IsPressed;
        }
        public static bool JustPressed(Key k)
        {
            return _keys[k].IsNew;
        }
        public static bool JustReleased(Key k)
        {
            return _keys[k].WasReleased;
        }

        public static void OnKeyChanged(KeyCode sym, bool down)
        {
            InputManager.SetCursorMode(false);
            if (_keyBinds.TryGetValue(sym, out Key key))
            {
                _keys[key].OnChanged(down);
            }
        }
    }
}
