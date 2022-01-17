using SDL2;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Input
{
    internal static class Keyboard
    {
        private static readonly Dictionary<SDL.SDL_Keycode, Key> _keyBinds = new();
        private static readonly Dictionary<Key, PressData> _keys = new();

        static Keyboard()
        {
            _keyBinds = GetDefaultKeyBinds();
            _keys = PressData.CreateDict(Enum.GetValues<Key>());
        }

        private static Dictionary<SDL.SDL_Keycode, Key> GetDefaultKeyBinds()
        {
            return new Dictionary<SDL.SDL_Keycode, Key>(13)
            {
                { SDL.SDL_Keycode.SDLK_q, Key.L },
                { SDL.SDL_Keycode.SDLK_w, Key.R },
                { SDL.SDL_Keycode.SDLK_LEFT, Key.Left },
                { SDL.SDL_Keycode.SDLK_RIGHT, Key.Right },
                { SDL.SDL_Keycode.SDLK_UP, Key.Up },
                { SDL.SDL_Keycode.SDLK_DOWN, Key.Down },
                { SDL.SDL_Keycode.SDLK_RETURN, Key.Start },
                { SDL.SDL_Keycode.SDLK_RSHIFT, Key.Select },
                { SDL.SDL_Keycode.SDLK_a, Key.X },
                { SDL.SDL_Keycode.SDLK_s, Key.Y },
                { SDL.SDL_Keycode.SDLK_z, Key.B },
                { SDL.SDL_Keycode.SDLK_x, Key.A },
                { SDL.SDL_Keycode.SDLK_F12, Key.Screenshot }
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

        public static void OnKeyChanged(SDL.SDL_Keycode sym, bool down)
        {
            if (_keyBinds.TryGetValue(sym, out Key key))
            {
                _keys[key].OnChanged(down);
            }
        }
    }
}
