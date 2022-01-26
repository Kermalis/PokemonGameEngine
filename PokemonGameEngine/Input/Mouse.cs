using Kermalis.PokemonGameEngine.Render;
using SDL2;
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
            if (SDL.SDL_ShowCursor(shown ? SDL.SDL_ENABLE : SDL.SDL_DISABLE) < 0)
            {
                Display.Print_SDL_Error("Could not set mouse cursor state!");
            }
        }

        public static void OnButtonDown(uint button, bool down)
        {
            InputManager.SetCursorMode(true);
            MouseButton b;
            switch (button)
            {
                case SDL.SDL_BUTTON_LEFT: b = MouseButton.Left; break;
                case SDL.SDL_BUTTON_MIDDLE: b = MouseButton.Middle; break;
                case SDL.SDL_BUTTON_RIGHT: b = MouseButton.Right; break;
                case SDL.SDL_BUTTON_X1: b = MouseButton.X1; break;
                case SDL.SDL_BUTTON_X2: b = MouseButton.X2; break;
                default: return;
            }
            _buttons[b].OnChanged(down);
        }
        public static void OnMove(in SDL.SDL_MouseMotionEvent e)
        {
            InputManager.SetCursorMode(true);
            Cursor.X = e.x;
            Cursor.Y = e.y;
        }
    }
}
