using Kermalis.PokemonGameEngine.Render;
using Silk.NET.Maths;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Input
{
    internal static class InputManager
    {
        public static bool CursorMode;

        public static void Init()
        {
            Controller.TryAttachController();
        }

        public static void Prepare()
        {
            Keyboard.Prepare();
            Controller.Prepare();
            Mouse.Prepare();
        }

        public static void SetCursorMode(bool cursorMode)
        {
            if (CursorMode != cursorMode)
            {
                CursorMode = cursorMode;
                Mouse.SetMouseCursorShown(cursorMode);
            }
        }

        private static bool HitTest(Vec2I pointInWindow, in Rect buttonInVirtualScreen, Vector4D<int> cornerRadii)
        {
            ref Rect screenRect = ref Display.ScreenRect;
            if (!screenRect.Contains(pointInWindow))
            {
                return false;
            }
            Vec2I pointInScreen = pointInWindow - screenRect.TopLeft;
            Vector2 scale = (Vector2)Display.ScreenSize / screenRect.GetSize();
            var pointInVirtualScreen = (Vec2I)(pointInScreen * scale);
            if (!buttonInVirtualScreen.Contains(pointInVirtualScreen))
            {
                return false;
            }
            return HitTestCorner(pointInVirtualScreen, buttonInVirtualScreen, cornerRadii);
        }
        private static bool HitTestCorner(Vec2I xy, in Rect rect, Vector4D<int> cornerRadii)
        {
            Vec2I point;
            int cornerRadius;
            // Top Left
            if (xy.X < rect.TopLeft.X + cornerRadii.X
                && xy.Y < rect.TopLeft.Y + cornerRadii.X)
            {
                cornerRadius = cornerRadii.X;
                point = new Vec2I(xy.X - (rect.TopLeft.X + cornerRadius),
                    xy.Y - (rect.TopLeft.Y + cornerRadius));
            }
            // Bottom Left
            else if (xy.X < rect.TopLeft.X + cornerRadii.Y
                && xy.Y > rect.BottomRight.Y - cornerRadii.Y)
            {
                cornerRadius = cornerRadii.Y;
                point = new Vec2I(xy.X - (rect.TopLeft.X + cornerRadius),
                    xy.Y - (rect.BottomRight.Y - cornerRadius));
            }
            // Top Right
            else if (xy.X > rect.BottomRight.X - cornerRadii.Z
                && xy.Y < rect.TopLeft.Y + cornerRadii.Z)
            {
                cornerRadius = cornerRadii.Z;
                point = new Vec2I(xy.X - (rect.BottomRight.X - cornerRadius),
                    xy.Y - (rect.TopLeft.Y + cornerRadius));
            }
            // Bottom Right
            else if (xy.X > rect.BottomRight.X - cornerRadii.W
                && xy.Y > rect.BottomRight.Y - cornerRadii.W)
            {
                cornerRadius = cornerRadii.W;
                point = new Vec2I(xy.X - (rect.BottomRight.X - cornerRadius),
                    xy.Y - (rect.BottomRight.Y - cornerRadius));
            }
            // Not in a corner
            else
            {
                return true;
            }

            float dist = (point.X * point.X * cornerRadius * cornerRadius)
                + (point.Y * point.Y * cornerRadius * cornerRadius);
            return dist <= cornerRadius * cornerRadius * cornerRadius * cornerRadius;
        }

        public static bool IsDown(Key k)
        {
            return Keyboard.IsDown(k)
                || Controller.IsDown(k);
        }
        public static bool JustPressed(Key k)
        {
            return Keyboard.JustPressed(k)
                || Controller.JustPressed(k);
        }
        public static bool JustReleased(Key k)
        {
            return Keyboard.JustReleased(k)
                || Controller.JustReleased(k);
        }

        public static bool IsHovering(in Rect buttonInVirtualScreen, Vector4D<int> cornerRadii = default)
        {
            return HitTest(Mouse.Cursor, buttonInVirtualScreen, cornerRadii);
        }
        public static bool IsDown(in Rect buttonInVirtualScreen, Vector4D<int> cornerRadii = default)
        {
            return HitTest(Mouse.Cursor, buttonInVirtualScreen, cornerRadii) && Mouse.IsDown(MouseButton.Left);
        }
        public static bool JustPressed(in Rect buttonInVirtualScreen, Vector4D<int> cornerRadii = default)
        {
            return HitTest(Mouse.Cursor, buttonInVirtualScreen, cornerRadii) && Mouse.JustPressed(MouseButton.Left);
        }
        public static bool JustReleased(in Rect buttonInVirtualScreen, Vector4D<int> cornerRadii = default)
        {
            return HitTest(Mouse.Cursor, buttonInVirtualScreen, cornerRadii) && Mouse.JustReleased(MouseButton.Left);
        }

        public static void Quit()
        {
            Controller.Quit();
        }
    }
}
