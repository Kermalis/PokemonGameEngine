#if DEBUG
using System;
using Kermalis.PokemonGameEngine.Input;
using Kermalis.PokemonGameEngine.Render.Fonts;
using Silk.NET.OpenGL;
#endif
using Kermalis.PokemonGameEngine.Core;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.R3D
{
    internal sealed class PositionRotation
    {
        public Vector3 Position;
        public Quaternion Rotation;

        private float _rollDegrees;
        private float _rollRadians;
        private float _pitchDegrees;
        private float _pitchRadians;
        private float _yawDegrees;
        private float _yawRadians;

        public PositionRotation()
        {
            Rotation = Quaternion.Identity;
        }
        public PositionRotation(PositionRotation other)
        {
            Position = other.Position;
            Rotation = other.Rotation;
            _rollDegrees = other._rollDegrees;
            _rollRadians = other._rollRadians;
            _pitchDegrees = other._pitchDegrees;
            _pitchRadians = other._pitchRadians;
            _yawDegrees = other._yawDegrees;
            _yawRadians = other._yawRadians;
        }
        public PositionRotation(Vector3 pos, Quaternion rot)
        {
            Position = pos;
            SetRotation(rot);
        }
        public PositionRotation(Vector3 pos, float rollDegrees, float pitchDegrees, float yawDegrees)
        {
            Position = pos;
            SetRotation(rollDegrees, pitchDegrees, yawDegrees);
        }

        public void ResetRotation()
        {
            _rollDegrees = 0;
            _rollRadians = 0;
            _pitchDegrees = 0;
            _pitchRadians = 0;
            _yawDegrees = 0;
            _yawRadians = 0;
            Rotation = Quaternion.Identity;
        }
        public void SetRotation(Quaternion rot)
        {
            Rotation = rot;
            _rollRadians = -rot.GetRollRadiansF();
            _rollDegrees = Utils.RadiansToDegreesF(_rollRadians);
            _pitchRadians = -rot.GetPitchRadiansF();
            _pitchDegrees = Utils.RadiansToDegreesF(_pitchRadians);
            _yawRadians = -rot.GetYawRadiansF();
            _yawDegrees = Utils.RadiansToDegreesF(_yawRadians);
        }
        public void SetRotation(float rollDegrees, float pitchDegrees, float yawDegrees)
        {
            _rollDegrees = rollDegrees;
            _rollRadians = Utils.DegreesToRadiansF(rollDegrees);
            _pitchDegrees = pitchDegrees;
            _pitchRadians = Utils.DegreesToRadiansF(pitchDegrees);
            _yawDegrees = yawDegrees;
            _yawRadians = Utils.DegreesToRadiansF(yawDegrees);
            Rotation = CreateRotation(_yawRadians, _pitchRadians, _rollRadians);
        }
        public void UpdateRollDegrees(float degrees)
        {
            _rollDegrees = degrees;
            _rollRadians = Utils.DegreesToRadiansF(degrees);
            UpdateRotation();
        }
        public void UpdatePitchDegrees(float degrees)
        {
            _pitchDegrees = degrees;
            _pitchRadians = Utils.DegreesToRadiansF(degrees);
            UpdateRotation();
        }
        public void UpdateYawDegrees(float degrees)
        {
            _yawDegrees = degrees;
            _yawRadians = Utils.DegreesToRadiansF(degrees);
            UpdateRotation();
        }

        public static Quaternion CreateRotation(float yawRadians, float pitchRadians, float rollRadians)
        {
            return Quaternion.CreateFromYawPitchRoll(-yawRadians, -pitchRadians, -rollRadians);
        }
        private void UpdateRotation()
        {
            Rotation = Quaternion.CreateFromYawPitchRoll(-_yawRadians, -_pitchRadians, -_rollRadians);
        }

        public void LerpPosition(in Vector3 from, in Vector3 to, float progress)
        {
            Position = Vector3.Lerp(from, to, progress);
        }
        public void SlerpRotation(in Quaternion from, in Quaternion to, float progress)
        {
            SetRotation(Quaternion.Slerp(from, to, progress));
        }
        public void Slerp(PositionRotation from, PositionRotation to, float progress)
        {
            LerpPosition(from.Position, to.Position, progress);
            SlerpRotation(from.Rotation, to.Rotation, progress);
        }

        #region Movement

        public void MoveForward(float value)
        {
            Position += Vector3.Transform(new Vector3(0, 0, -1), Rotation) * value;
        }
        public void MoveForwardZ(float value)
        {
            Position.Z += value;
        }
        public void MoveBackward(float value)
        {
            Position += Vector3.Transform(new Vector3(0, 0, 1), Rotation) * value;
        }
        public void MoveBackwardZ(float value)
        {
            Position.Z -= value;
        }
        public void MoveLeft(float value)
        {
            Position += Vector3.Transform(new Vector3(-1, 0, 0), Rotation) * value;
        }
        public void MoveLeftX(float value)
        {
            Position.X += value;
        }
        public void MoveRightX(float value)
        {
            Position.X -= value;
        }
        public void MoveRight(float value)
        {
            Position += Vector3.Transform(new Vector3(1, 0, 0), Rotation) * value;
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
            if (InputManager.IsPressed(Key.Y))
            {
                ResetRotation();
                return;
            }
            // Reset position
            if (InputManager.IsPressed(Key.B))
            {
                Position = default;
                return;
            }
            // Roll, Pitch, Yaw
            if (InputManager.IsDown(Key.R))
            {
                // Pitch
                if (InputManager.IsDown(Key.Up))
                {
                    if (_pitchDegrees < 89)
                    {
                        UpdatePitchDegrees(_pitchDegrees + 1);
                    }
                }
                else if (InputManager.IsDown(Key.Down))
                {
                    if (_pitchDegrees > -89)
                    {
                        UpdatePitchDegrees(_pitchDegrees - 1);
                    }
                }
                if (InputManager.IsDown(Key.X))
                {
                    // Roll
                    if (InputManager.IsDown(Key.Left))
                    {
                        UpdateRollDegrees(_rollDegrees == 0 ? 359 : _rollDegrees - 1);
                    }
                    else if (InputManager.IsDown(Key.Right))
                    {
                        UpdateRollDegrees(_rollDegrees == 359 ? 0 : _rollDegrees + 1);
                    }
                }
                else
                {
                    // Yaw
                    if (InputManager.IsDown(Key.Left))
                    {
                        UpdateYawDegrees(_yawDegrees == 0 ? 359 : _yawDegrees - 1);
                    }
                    else if (InputManager.IsDown(Key.Right))
                    {
                        UpdateYawDegrees(_yawDegrees == 359 ? 0 : _yawDegrees + 1);
                    }
                }
                return;
            }
            // Move along axis
            if (InputManager.IsDown(Key.L))
            {
                const float xMove = 0.1f;
                const float yMove = 0.1f;
                const float zMove = 0.1f;

                if (InputManager.IsDown(Key.Up))
                {
                    if (InputManager.IsDown(Key.X))
                    {
                        MoveUpY(yMove * moveSpeed);
                    }
                    else
                    {
                        MoveForwardZ(zMove * moveSpeed);
                    }
                }
                else if (InputManager.IsDown(Key.Down))
                {
                    if (InputManager.IsDown(Key.X))
                    {
                        MoveDownY(yMove * moveSpeed);
                    }
                    else
                    {
                        MoveBackwardZ(zMove * moveSpeed);
                    }
                }
                if (InputManager.IsDown(Key.Left))
                {
                    MoveLeftX(xMove * moveSpeed);
                }
                else if (InputManager.IsDown(Key.Right))
                {
                    MoveRightX(xMove * moveSpeed);
                }
                return;
            }
            // Move along our camera angle
            {
                const float forwardMove = 0.1f;
                if (InputManager.IsDown(Key.Up))
                {
                    MoveForward(forwardMove * moveSpeed);
                }
                else if (InputManager.IsDown(Key.Down))
                {
                    MoveBackward(forwardMove * moveSpeed);
                }
                if (InputManager.IsDown(Key.Left))
                {
                    MoveLeft(forwardMove * moveSpeed);
                }
                else if (InputManager.IsDown(Key.Right))
                {
                    MoveRight(forwardMove * moveSpeed);
                }
                return;
            }
        }

        public override string ToString()
        {
            return string.Format("X: {0}\nY: {1}\nZ: {2}\nRoll: {3}\nPitch: {4}\nYaw: {5}",
                MathF.Round(Position.X, 2), MathF.Round(Position.Y, 2), MathF.Round(Position.Z, 2),
                _rollDegrees, _pitchDegrees, _yawDegrees);
        }
        public void Debug_RenderPosition(GL gl)
        {
            GUIString.CreateAndRenderOneTimeString(gl, ToString(), Font.Default, FontColors.DefaultRed_O, new Pos2D(0, 0));
        }
#endif
    }
}
