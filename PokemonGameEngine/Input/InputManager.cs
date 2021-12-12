using SDL2;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Input
{
    internal static class InputManager
    {
        private sealed class KeyDownData
        {
            // Updated in real time
            public bool StickPressed;
            public bool NonStickPressed;
            // Updated every frame
            public bool IsNew; // True if this key was not active the previous frame but now is
            //public ulong PressTime; // The amount of frames this key has been active
            public bool IsActive; // True if the key is active
        }

        private static readonly Dictionary<Key, KeyDownData> _pressed = new();
        static InputManager()
        {
            foreach (Key k in Enum.GetValues<Key>())
            {
                _pressed.Add(k, new KeyDownData());
            }
        }

        // Updating the real time presses
        public static void OnAxis(SDL.SDL_ControllerAxisEvent caxis)
        {
            const ushort Deadzone = ushort.MaxValue / 4;
            void Do(Key less, Key more)
            {
                KeyDownData pLess = _pressed[less];
                KeyDownData pMore = _pressed[more];
                short val = caxis.axisValue;
                if (val < -Deadzone)
                {
                    pLess.StickPressed = true;
                    pMore.StickPressed = false;
                }
                else if (val > Deadzone)
                {
                    pLess.StickPressed = false;
                    pMore.StickPressed = true;
                }
                else
                {
                    pLess.StickPressed = false;
                    pMore.StickPressed = false;
                }
            }
            switch ((SDL.SDL_GameControllerAxis)caxis.axis)
            {
                case SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX: Do(Key.Left, Key.Right); break;
                case SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY: Do(Key.Up, Key.Down); break;
                default: break;
            }
        }
        public static void OnButtonDown(SDL.SDL_GameControllerButton button, bool down)
        {
            Key key;
            switch (button)
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

            KeyDownData p = _pressed[key];
            p.NonStickPressed = down;
        }
        public static void OnKeyDown(SDL.SDL_Keycode sym, bool down)
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
                default: return;
            }

            KeyDownData p = _pressed[key];
            p.NonStickPressed = down;
        }

        public static void Update()
        {
            foreach (KeyValuePair<Key, KeyDownData> kvp in _pressed)
            {
                KeyDownData p = kvp.Value;
                bool active = p.NonStickPressed || p.StickPressed;
                // Was active last frame
                if (p.IsActive)
                {
                    p.IsNew = false;
                    if (active)
                    {
                        //p.PressTime++;
                    }
                    else
                    {
                        p.IsActive = false;
                        //p.PressTime = 0;
                    }
                }
                // Not active last frame
                else
                {
                    if (active)
                    {
                        p.IsNew = true;
                        p.IsActive = true;
                        //p.PressTime = 0;
                    }
                }
            }
        }

        public static bool IsPressed(Key key)
        {
            KeyDownData p = _pressed[key];
            return p.IsActive && p.IsNew;
        }
        public static bool IsDown(Key key/*, uint downTime = 0*/)
        {
            KeyDownData p = _pressed[key];
            return p.IsActive/* && p.PressTime >= downTime*/;
        }

#if DEBUG
        public static string Debug_GetKeys()
        {
            string s = string.Empty;
            foreach (KeyValuePair<Key, KeyDownData> kvp in _pressed)
            {
                KeyDownData p = kvp.Value;
                s += string.Format("{0,-15}{1}{2}", kvp.Key, /*p.IsActive ? p.PressTime : */null, Environment.NewLine);
            }
            return s;
        }
#endif
    }
}
