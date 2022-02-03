using System.Numerics;
#if DEBUG
using System;
using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Render.GUIs;
#endif

namespace Kermalis.PokemonGameEngine.Render.R3D
{
    internal struct PositionRotation
    {
        public static PositionRotation Default { get; } = new(Vector3.Zero, Rotation.Default);

        public Vector3 Position;
        public Rotation Rotation;

        public PositionRotation(in Vector3 pos, in Rotation rot)
        {
            Position = pos;
            Rotation = rot;
        }

        public static PositionRotation Slerp(in PositionRotation from, in PositionRotation to, float progress)
        {
            PositionRotation pr;
            pr.Position = Vector3.Lerp(from.Position, to.Position, progress);
            pr.Rotation = new Rotation(Quaternion.Slerp(from.Rotation.Value, to.Rotation.Value, progress));
            return pr;
        }

        #region Movement

        public void MoveForward(float value)
        {
            Position += Vector3.Transform(new Vector3(0, 0, -1), Rotation.Value) * value;
        }
        public void MoveForwardZ(float value)
        {
            Position.Z -= value;
        }
        public void MoveBackward(float value)
        {
            Position += Vector3.Transform(new Vector3(0, 0, +1), Rotation.Value) * value;
        }
        public void MoveBackwardZ(float value)
        {
            Position.Z += value;
        }
        public void MoveLeft(float value)
        {
            Position += Vector3.Transform(new Vector3(-1, 0, 0), Rotation.Value) * value;
        }
        public void MoveLeftX(float value)
        {
            Position.X -= value;
        }
        public void MoveRight(float value)
        {
            Position += Vector3.Transform(new Vector3(+1, 0, 0), Rotation.Value) * value;
        }
        public void MoveRightX(float value)
        {
            Position.X += value;
        }
        public void MoveUpY(float value)
        {
            Position.Y += value;
        }
        public void MoveDownY(float value)
        {
            Position.Y -= value;
        }

        #endregion

#if DEBUG
        public void Debug_Move(float moveSpeed)
        {
            // Reset roll pitch and yaw
            if (InputManager.JustPressed(Key.Y))
            {
                Rotation = Rotation.Default;
                return;
            }
            // Reset position
            if (InputManager.JustPressed(Key.B))
            {
                Position = default;
                return;
            }
            // Roll, Pitch, Yaw
            if (InputManager.IsDown(Key.R))
            {
                moveSpeed *= 5f;
                // Pitch
                if (InputManager.IsDown(Key.Up))
                {
                    float pitch = Rotation.Pitch + (Display.DeltaTime * moveSpeed);
                    if (pitch > 89f)
                    {
                        pitch = 89f;
                    }
                    Rotation = new Rotation(Rotation.Yaw, pitch, Rotation.Roll);
                }
                else if (InputManager.IsDown(Key.Down))
                {
                    float pitch = Rotation.Pitch - (Display.DeltaTime * moveSpeed);
                    if (pitch < -89f)
                    {
                        pitch = -89f;
                    }
                    Rotation = new Rotation(Rotation.Yaw, pitch, Rotation.Roll);
                }
                if (InputManager.IsDown(Key.X))
                {
                    // Roll
                    if (InputManager.IsDown(Key.Left))
                    {
                        float roll = Rotation.Roll - (Display.DeltaTime * moveSpeed);
                        while (roll < 0f)
                        {
                            roll += 360f;
                        }
                        Rotation = new Rotation(Rotation.Yaw, Rotation.Pitch, roll);
                    }
                    else if (InputManager.IsDown(Key.Right))
                    {
                        float roll = Rotation.Roll + (Display.DeltaTime * moveSpeed);
                        while (roll >= 360f)
                        {
                            roll -= 360f;
                        }
                        Rotation = new Rotation(Rotation.Yaw, Rotation.Pitch, roll);
                    }
                }
                else
                {
                    // Yaw
                    if (InputManager.IsDown(Key.Left))
                    {
                        float yaw = Rotation.Yaw - (Display.DeltaTime * moveSpeed);
                        while (yaw < 0f)
                        {
                            yaw += 360f;
                        }
                        Rotation = new Rotation(yaw, Rotation.Pitch, Rotation.Roll);
                    }
                    else if (InputManager.IsDown(Key.Right))
                    {
                        float yaw = Rotation.Yaw + (Display.DeltaTime * moveSpeed);
                        while (yaw >= 360f)
                        {
                            yaw -= 360f;
                        }
                        Rotation = new Rotation(yaw, Rotation.Pitch, Rotation.Roll);
                    }
                }
                return;
            }
            // Move along axis
            if (InputManager.IsDown(Key.L))
            {
                if (InputManager.IsDown(Key.Up))
                {
                    if (InputManager.IsDown(Key.X))
                    {
                        MoveUpY(Display.DeltaTime * moveSpeed);
                    }
                    else
                    {
                        MoveForwardZ(Display.DeltaTime * moveSpeed);
                    }
                }
                else if (InputManager.IsDown(Key.Down))
                {
                    if (InputManager.IsDown(Key.X))
                    {
                        MoveDownY(Display.DeltaTime * moveSpeed);
                    }
                    else
                    {
                        MoveBackwardZ(Display.DeltaTime * moveSpeed);
                    }
                }
                if (InputManager.IsDown(Key.Left))
                {
                    MoveLeftX(Display.DeltaTime * moveSpeed);
                }
                else if (InputManager.IsDown(Key.Right))
                {
                    MoveRightX(Display.DeltaTime * moveSpeed);
                }
                return;
            }
            // Move along our camera angle
            {
                if (InputManager.IsDown(Key.Up))
                {
                    MoveForward(Display.DeltaTime * moveSpeed);
                }
                else if (InputManager.IsDown(Key.Down))
                {
                    MoveBackward(Display.DeltaTime * moveSpeed);
                }
                if (InputManager.IsDown(Key.Left))
                {
                    MoveLeft(Display.DeltaTime * moveSpeed);
                }
                else if (InputManager.IsDown(Key.Right))
                {
                    MoveRight(Display.DeltaTime * moveSpeed);
                }
                return;
            }
        }

        public override string ToString()
        {
            return string.Format("X: {0}\nY: {1}\nZ: {2}\n{3}",
                MathF.Round(Position.X, 2), MathF.Round(Position.Y, 2), MathF.Round(Position.Z, 2),
                Rotation);
        }
        public void Debug_RenderPosition()
        {
            GUIString.CreateAndRenderOneTimeString(ToString(), Font.Default, FontColors.DefaultRed_O, new Vec2I(0, 0));
        }
#endif
    }
}
