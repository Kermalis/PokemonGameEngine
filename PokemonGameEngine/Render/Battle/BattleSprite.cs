using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Render.Images;
using Kermalis.PokemonGameEngine.Render.R3D;
using Kermalis.PokemonGameEngine.Render.Shaders.Battle;
using Silk.NET.OpenGL;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.Battle
{
    internal sealed class BattleSprite
    {
        private const float MASK_COLOR_SPEED = 0.1f; // Takes 10 seconds to complete the loop
        public const float MASK_COLOR_AMPLITUDE = 0.65f; // 65% of the MaskColor

        private readonly Vector2 _baseScale;

        public AnimatedImage Image;
        public Vector3 Pos;
        /// <summary>Represents rotation in degrees around the z-axis for the image (positive rotates counter-clockwise)</summary>
        public float Rotation;
        /// <summary>Visual scale of the sprite meant to be used with animations</summary>
        public Vector2 Scale = Vector2.One;

        public bool IsVisible;
        public float Opacity;
        public float PixelateAmt;
        public Vector3? MaskColor;
        public bool AnimateMaskColor;
        public float MaskColorAmt;
        public float BlacknessAmt;

        private Matrix4x4 _scaleCache;
        private Matrix4x4 _transformCache;

        public BattleSprite(in Vector3 startPos, bool isVisible, float scale = 1f)
        {
            Pos = startPos;
            Opacity = 1f;
            IsVisible = isVisible;
            _baseScale = GetBaseScale(scale);
        }

        /// <summary>This creates the scale that will make the sprite always be its original size when in its starting position and the camera is in the default position.
        /// This will happen regardless of resolution or projection matrix (unless they change after this is calculated)</summary>
        private Vector2 GetBaseScale(float scale)
        {
            Matrix4x4 view = Camera.CreateViewMatrix(BattleGUI.DefaultCamPosition);
            Matrix4x4 transformViewProjection = CreateTranslation(view)
                * view
                * BattleGUI.Instance.Camera.Projection;

            Vector2 bottomLeft = GetAbsolutePixelForVertex(transformViewProjection, new Vector2(-0.5f, 0f)); // Rect is center x, bottom y
            Vector2 topRight = GetAbsolutePixelForVertex(transformViewProjection, new Vector2(0.5f, 1f));

            return new Vector2(scale) / (topRight - bottomLeft);
        }
        private static Vector2 GetAbsolutePixelForVertex(in Matrix4x4 transformViewProjection, Vector2 v)
        {
            Vector4 v4 = Utils.MulMatrixAndVec4(transformViewProjection, new Vector4(v.X, v.Y, 0f, 1f));
            v4 /= v4.W; // Scale back from 4d
            var v2 = new Vector2(v4.X, v4.Y);

            // Convert from GL to relative
            v2 *= 0.5f;
            v2 += new Vector2(0.5f);

            // Convert from relative to absolute
            v2 *= BattleGUI.RenderSize;
            return v2;
        }

        public void UpdateImage(AnimatedImage img)
        {
            Image?.DeductReference();
            Image = img;
            Vector2 scale = img.Size * Scale * _baseScale;
            _scaleCache = Matrix4x4.CreateScale(scale.X, scale.Y, 1f);
        }

        private Matrix4x4 CreateTranslation(in Matrix4x4 camView)
        {
            var translation = Matrix4x4.CreateTranslation(Pos);
            // Remove x/y rotation when they get multiplied together so the sprite always faces the camera
            translation.M11 = camView.M11;
            translation.M12 = camView.M21;
            translation.M13 = camView.M31;
            translation.M21 = camView.M12;
            translation.M22 = camView.M22;
            translation.M23 = camView.M32;
            translation.M31 = camView.M13;
            translation.M32 = camView.M23;
            translation.M33 = camView.M33;
            return translation;
        }
        private void UpdateTransform(in Matrix4x4 camView)
        {
            _transformCache = _scaleCache
                * Matrix4x4.CreateRotationZ(Rotation * Utils.DegToRad)
                * CreateTranslation(camView);
        }
        public void Render(GL gl, BattleSpriteShader shader, in Matrix4x4 projection, in Matrix4x4 camView)
        {
            gl.BindTexture(TextureTarget.Texture2D, Image.Texture);
            shader.SetMatrix(gl, _transformCache * camView * projection);
            shader.SetOpacity(gl, Opacity);
            shader.SetPixelateAmt(gl, PixelateAmt);
            shader.SetBlacknessAmt(gl, BlacknessAmt);
            if (MaskColor is null)
            {
                shader.SetMaskColorAmt(gl, 0f);
            }
            else
            {
                shader.SetMaskColor(gl, MaskColor.Value);
                float value;
                if (AnimateMaskColor)
                {
                    MaskColorAmt += Display.DeltaTime * MASK_COLOR_SPEED;
                    MaskColorAmt %= 1f; // Cut off >= 1, doesn't matter here
                    value = Easing.BellCurve2(MaskColorAmt) * MASK_COLOR_AMPLITUDE;
                }
                else
                {
                    value = MaskColorAmt;
                }
                shader.SetMaskColorAmt(gl, value);
            }
            RectMesh.Instance.Render(gl);
        }
        public void RenderShadow(GL gl, BattleSpriteShader shader, in Matrix4x4 viewProjection, in Matrix4x4 camView)
        {
            UpdateTransform(camView);

            gl.BindTexture(TextureTarget.Texture2D, Image.Texture);
            shader.SetMatrix(gl, _transformCache * viewProjection);
            shader.SetOpacity(gl, Opacity);
            shader.SetPixelateAmt(gl, PixelateAmt);
            RectMesh.Instance.Render(gl);
        }
    }
}
