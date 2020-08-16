using SDL2;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Input
{
    internal static class InputManager
    {
        private sealed class KeyDownData
        {
            public bool PressChecked;
            public ulong PressTime;
            public bool StickPressed;
            public bool NonStickPressed;
        }

        private static readonly Dictionary<Key, KeyDownData> _pressed = new Dictionary<Key, KeyDownData>();
        static InputManager()
        {
            foreach (Key k in Enum.GetValues(typeof(Key)))
            {
                _pressed.Add(k, new KeyDownData());
            }
        }

        public static void OnAxis(SDL.SDL_Event e)
        {
            const ushort Deadzone = ushort.MaxValue / 4;
            void Do(Key less, Key more)
            {
                short val = e.caxis.axisValue;
                if (val < -Deadzone)
                {
                    DoTheDown(more, false, true);
                    DoTheDown(less, true, true);
                }
                else if (val > Deadzone)
                {
                    DoTheDown(less, false, true);
                    DoTheDown(more, true, true);
                }
                else
                {
                    DoTheDown(less, false, true);
                    DoTheDown(more, false, true);
                }
            }
            switch ((SDL.SDL_GameControllerAxis)e.caxis.axis)
            {
                case SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX: Do(Key.Left, Key.Right); break;
                case SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY: Do(Key.Up, Key.Down); break;
                default: break;
            }
        }
        public static void OnButtonDown(SDL.SDL_Event e, bool down)
        {
            Key key;
            switch ((SDL.SDL_GameControllerButton)e.cbutton.button)
            {
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSHOULDER: key = Key.L; break;
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSHOULDER: key = Key.R; break;
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_Y: key = Key.X; break;
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_X: key = Key.Y; break;
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_A: key = Key.B; break;
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_B: key = Key.A; break;
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_LEFT: key = Key.Left; break;
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_RIGHT: key = Key.Right; break;
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_DOWN: key = Key.Down; break;
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_UP: key = Key.Up; break;
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_START: key = Key.Start; break;
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_BACK: key = Key.Select; break;
                default: return;
            }
            DoTheDown(key, down, false);
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
            DoTheDown(key, down, false);
        }
        private static void DoTheDown(Key key, bool down, bool stick)
        {
            KeyDownData p = _pressed[key];
            if (down)
            {
                if (stick)
                {
                    p.StickPressed = true;
                }
                else
                {
                    p.NonStickPressed = true;
                }
                p.PressTime++;
            }
            else
            {
                bool other;
                if (stick)
                {
                    p.StickPressed = false;
                    other = p.NonStickPressed;
                }
                else
                {
                    p.NonStickPressed = false;
                    other = p.StickPressed;
                }
                if (!other)
                {
                    p.PressChecked = false;
                    p.PressTime = 0;
                }
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
