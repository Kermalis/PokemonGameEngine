using SDL2;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Input
{
    internal static class Controller
    {
        private static readonly Dictionary<SDL.SDL_GameControllerButton, Key> _keyBinds = new();
        private static readonly Dictionary<Key, PressData> _buttons = new();
        private static readonly AxisData _leftStick;

        private static IntPtr _controller;
        private static int _controllerId;

        static Controller()
        {
            _keyBinds = GetDefaultKeyBinds();
            _buttons = PressData.CreateDict(Enum.GetValues<Key>());
            _leftStick = new AxisData();
        }

        private static Dictionary<SDL.SDL_GameControllerButton, Key> GetDefaultKeyBinds()
        {
            return new Dictionary<SDL.SDL_GameControllerButton, Key>(12) // No screenshot bind by default
            {
                { SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSHOULDER, Key.L },
                { SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSHOULDER, Key.R },
                { SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_LEFT, Key.Left },
                { SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_RIGHT, Key.Right },
                { SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_UP, Key.Up },
                { SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_DOWN, Key.Down },
                { SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_START, Key.Start },
                { SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_BACK, Key.Select },
                { SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_Y, Key.X }, // Switch from XBOX layout to Nintendo layout
                { SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_X, Key.Y },
                { SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_A, Key.B },
                { SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_B, Key.A }
            };
        }

        public static void Prepare()
        {
            PressData.PrepareMany(_buttons.Values);
            _leftStick.Prepare();
        }

        public static bool IsDown(Key k)
        {
            PressData a = _leftStick.GetDPADSimPressData(k);
            if (a is not null && a.IsPressed)
            {
                return true;
            }
            return _buttons[k].IsPressed;
        }
        public static bool JustPressed(Key k)
        {
            PressData a = _leftStick.GetDPADSimPressData(k);
            if (a is not null && a.IsNew)
            {
                return true;
            }
            return _buttons[k].IsNew;
        }
        public static bool JustReleased(Key k)
        {
            PressData a = _leftStick.GetDPADSimPressData(k);
            if (a is not null && a.WasReleased)
            {
                return true;
            }
            return _buttons[k].WasReleased;
        }

        public static void OnControllerAdded()
        {
            if (_controller != IntPtr.Zero)
            {
                return; // Already have a controller
            }
            TryAttachController();
        }
        public static void OnControllerRemoved(int id)
        {
            if (id != _controllerId)
            {
                return; // Our controller is still attached
            }
            SDL.SDL_GameControllerClose(_controller);
            TryAttachController();
        }
        public static void OnAxisChanged(SDL.SDL_ControllerAxisEvent caxis)
        {
            if (caxis.which == _controllerId)
            {
                AxisData ad;
                bool isX;
                switch ((SDL.SDL_GameControllerAxis)caxis.axis)
                {
                    case SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX: ad = _leftStick; isX = true; break;
                    case SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY: ad = _leftStick; isX = false; break;
                    default: return;
                }

                float amount = caxis.axisValue / (float)short.MaxValue;
                if (amount < -1f)
                {
                    amount = -1f; // Leftmost value would be less than -1 so clamp it
                }
                ad.Update(isX, amount);
            }
        }
        public static void OnButtonChanged(SDL.SDL_ControllerButtonEvent cbutton, bool down)
        {
            if (cbutton.which == _controllerId
                && _keyBinds.TryGetValue((SDL.SDL_GameControllerButton)cbutton.button, out Key key))
            {
                _buttons[key].OnChanged(down);
            }
        }

        public static void TryAttachController()
        {
            int num = SDL.SDL_NumJoysticks();
            for (int i = 0; i < num; i++)
            {
                if (SDL.SDL_IsGameController(i) == SDL.SDL_bool.SDL_TRUE)
                {
                    _controller = SDL.SDL_GameControllerOpen(i);
                    if (_controller != IntPtr.Zero)
                    {
                        _controllerId = SDL.SDL_JoystickInstanceID(SDL.SDL_GameControllerGetJoystick(_controller));
                        return;
                    }
                }
            }
            // None found
            _controller = IntPtr.Zero;
            _controllerId = -1;
        }

        public static void Quit()
        {
            SDL.SDL_GameControllerClose(_controller);
        }
    }
}
