using SDL2;
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

        public static void OnKeyDown(SDL.SDL_Event e, bool down)
        {
            Key key;
            switch (e.key.keysym.sym)
            {
                case SDL.SDL_Keycode.SDLK_q: key = Key.L; break;
                case SDL.SDL_Keycode.SDLK_w: key = Key.R; break;
                case SDL.SDL_Keycode.SDLK_a: key = Key.X; break;
                case SDL.SDL_Keycode.SDLK_s: key = Key.Y; break;
                case SDL.SDL_Keycode.SDLK_z: key = Key.B; break;
                case SDL.SDL_Keycode.SDLK_x: key = Key.A; break;
                case SDL.SDL_Keycode.SDLK_LEFT: key = Key.Left; break;
                case SDL.SDL_Keycode.SDLK_RIGHT: key = Key.Right; break;
                case SDL.SDL_Keycode.SDLK_DOWN: key = Key.Down; break;
                case SDL.SDL_Keycode.SDLK_UP: key = Key.Up; break;
                case SDL.SDL_Keycode.SDLK_RETURN: key = Key.Start; break;
                case SDL.SDL_Keycode.SDLK_RSHIFT: key = Key.Select; break;
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
