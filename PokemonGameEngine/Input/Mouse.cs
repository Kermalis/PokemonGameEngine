using Kermalis.PokemonGameEngine.Render;
using Silk.NET.SDL;
using System;
using System.Collections.Generic;

namespace Kermalis.PokemonGameEngine.Input
{
    internal static class Mouse
    {
        private static readonly Dictionary<MouseButton, PressData> _buttons;

        static Mouse()
        {
            _buttons = PressData.CreateDict(Enum.GetValues<MouseButton>());
        }

        public static Vec2I Cursor;

        public static void Prepare()
        {
            PressData.PrepareMany(_buttons.Values);
        }

        public static bool IsDown(MouseButton b)
        {
            return _buttons[b].IsPressed;
        }
        public static bool JustPressed(MouseButton b)
        {
            return _buttons[b].IsNew;
        }
        public static bool JustReleased(MouseButton b)
        {
            return _buttons[b].WasReleased;
        }

        public static void SetMouseCursorShown(bool shown)
        {
            if (Display.SDL.ShowCursor(shown ? 1 : 0) < 0) // SDL_ENABLE : SDL_DISABLE
            {
                Display.Print_SDL_Error("Could not set mouse cursor state!");
            }
        }

        public static void OnButtonDown(byte button, bool down)
        {
            InputManager.SetCursorMode(true);
            MouseButton b;
            switch (button)
            {
                case 1: b = MouseButton.Left; break; // SDL_BUTTON_LEFT
                case 2: b = MouseButton.Middle; break; // SDL_BUTTON_MIDDLE
                case 3: b = MouseButton.Right; break; // SDL_BUTTON_RIGHT
                case 4: b = MouseButton.X1; break; // SDL_BUTTON_X1
                case 5: b = MouseButton.X2; break; // SDL_BUTTON_X2
                default: return;
            }
            _buttons[b].OnChanged(down);
        }
        public static void OnMove(in MouseMotionEvent e)
        {
            InputManager.SetCursorMode(true);
            Cursor.X = e.X;
            Cursor.Y = e.Y;
        }
    }
}
