using Kermalis.PokemonGameEngine.Render;
using SDL2;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Input
{
    internal static class Keyboard
    {
        private static readonly Dictionary<Key, PressData> _keys = new();
        static Keyboard()
        {
            Key[] keys = Enum.GetValues<Key>();
            _keys = PressData.CreateDict(keys);
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
            Key key;
            switch (sym)
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
                // Special
                case SDL.SDL_Keycode.SDLK_F12:
                {
                    if (down)
                    {
                        Display.SaveScreenshot();
                    }
                    return;
                }
                default: return;
            }

            _keys[key].OnChanged(down);
        }
    }
}
