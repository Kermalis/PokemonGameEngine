using Kermalis.PokemonGameEngine.Render;
using Silk.NET.SDL;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Input
{
    internal static unsafe class Controller
    {
        private static readonly Dictionary<GameControllerButton, Key> _keyBinds = new();
        private static readonly Dictionary<Key, PressData> _buttons = new();
        private static readonly AxisData _leftStick;

        private static GameController* _controller;
        private static int _controllerId;

        static Controller()
        {
            _keyBinds = GetDefaultKeyBinds();
            _buttons = PressData.CreateDict(Enum.GetValues<Key>());
            _leftStick = new AxisData();
        }

        private static Dictionary<GameControllerButton, Key> GetDefaultKeyBinds()
        {
            return new Dictionary<GameControllerButton, Key>(12) // No screenshot bind by default
            {
                { GameControllerButton.ControllerButtonLeftshoulder, Key.L },
                { GameControllerButton.ControllerButtonRightshoulder, Key.R },
                { GameControllerButton.ControllerButtonDpadLeft, Key.Left },
                { GameControllerButton.ControllerButtonDpadRight, Key.Right },
                { GameControllerButton.ControllerButtonDpadUp, Key.Up },
                { GameControllerButton.ControllerButtonDpadDown, Key.Down },
                { GameControllerButton.ControllerButtonStart, Key.Start },
                { GameControllerButton.ControllerButtonBack, Key.Select },
                { GameControllerButton.ControllerButtonY, Key.X }, // Switch from XBOX layout to Nintendo layout
                { GameControllerButton.ControllerButtonX, Key.Y },
                { GameControllerButton.ControllerButtonA, Key.B },
                { GameControllerButton.ControllerButtonB, Key.A }
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
            if (_controller is not null)
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
            Display.SDL.GameControllerClose(_controller);
            TryAttachController();
        }
        public static void OnAxisChanged(in ControllerAxisEvent caxis)
        {
            if (caxis.Which != _controllerId)
            {
                return;
            }

            // Cursor state is only set if axis state changes
            AxisData ad;
            bool isX;
            switch ((GameControllerAxis)caxis.Axis)
            {
                case GameControllerAxis.ControllerAxisLeftx: ad = _leftStick; isX = true; break;
                case GameControllerAxis.ControllerAxisLefty: ad = _leftStick; isX = false; break;
                default: return;
            }

            float amount = caxis.Value / (float)short.MaxValue;
            if (amount < -1f)
            {
                amount = -1f; // Leftmost value would be less than -1 so clamp it
            }
            ad.Update(isX, amount);
        }
        public static void OnButtonChanged(in ControllerButtonEvent cbutton, bool down)
        {
            if (cbutton.Which != _controllerId)
            {
                return;
            }

            InputManager.SetCursorMode(false);
            if (_keyBinds.TryGetValue((GameControllerButton)cbutton.Button, out Key key))
            {
                _buttons[key].OnChanged(down);
            }
        }

        public static void TryAttachController()
        {
            Sdl SDL = Display.SDL;
            int num = SDL.NumJoysticks();
            for (int i = 0; i < num; i++)
            {
                if (SDL.IsGameController(i) == SdlBool.True)
                {
                    _controller = SDL.GameControllerOpen(i);
                    if (_controller is not null)
                    {
                        _controllerId = SDL.JoystickInstanceID(SDL.GameControllerGetJoystick(_controller));
                        return;
                    }
                }
            }
            // None found
            _controller = null;
            _controllerId = -1;
        }

        public static void Quit()
        {
            Display.SDL.GameControllerClose(_controller);
        }
    }
}
