using SDL2;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Input
{
    internal static class Controller
    {
        private static IntPtr _controller;
        private static int _controllerId;

        private static readonly Dictionary<Key, PressData> _buttons = new();
        private static readonly AxisData _leftStick;
        static Controller()
        {
            Key[] buttons = Enum.GetValues<Key>();
            _buttons = PressData.CreateDict(buttons);
            _leftStick = new AxisData();
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
            if (caxis.which != _controllerId)
            {
                return;
            }

            float amount = caxis.axisValue / (float)short.MaxValue;
            if (amount < -1f)
            {
                amount = -1f; // Leftmost value would be less than -1 so clamp it
            }
            switch ((SDL.SDL_GameControllerAxis)caxis.axis)
            {
                case SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX: _leftStick.Update(true, amount); break;
                case SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY: _leftStick.Update(false, amount); break;
            }
        }
        public static void OnButtonChanged(SDL.SDL_ControllerButtonEvent cbutton, bool down)
        {
            if (cbutton.which != _controllerId)
            {
                return;
            }

            Key key;
            switch ((SDL.SDL_GameControllerButton)cbutton.button)
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

            _buttons[key].OnChanged(down);
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
