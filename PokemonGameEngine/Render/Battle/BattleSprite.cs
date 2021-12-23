using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Render.Images;
using Kermalis.PokemonGameEngine.Render.Shaders;
using Silk.NET.OpenGL;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.Battle
{
    internal sealed class BattleSprite
    {
        private const float MASK_COLOR_SPEED = 0.1f; // Takes 10 seconds to complete the loop
        public const float MASK_COLOR_AMPLITUDE = 0.65f; // 65% of the MaskColor

        public readonly Vector2 Scale; // Only accurate for the default Display.RenderSize

        public AnimatedImage AnimImage;
        public Vector3 Pos;
        /// <summary>Represents rotation in degrees around the z-axis for the image (positive rotates counter-clockwise)</summary>
        public float Rotation2D;

        public bool IsVisible;
        public float Opacity;
        public float PixelateAmt;
        public Vector3? MaskColor;
        public bool AnimateMaskColor;
        public float MaskColorAmt;

        private Matrix4x4 _scaleCache;
        private Matrix4x4 _transformCache;

        public BattleSprite(Vector2 scale, in Vector3 startPos, bool isVisible)
        {
            Scale = scale;
            Pos = startPos;
            Opacity = 1f;
            IsVisible = isVisible;
        }

        public void UpdateImage(AnimatedImage img)
        {
            AnimImage?.DeductReference();
            AnimImage = img;
            Size2D imgSize = img.Size;
            _scaleCache = Matrix4x4.CreateScale(imgSize.Width * Scale.X, imgSize.Height * Scale.Y, 1f);
        }

        private void UpdateTransform(in Matrix4x4 camView)
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
            _transformCache = _scaleCache
                * Matrix4x4.CreateRotationZ(Rotation2D * Utils.DegToRad)
                * translation;
        }
        public void Render(GL gl, BattleSpriteMesh mesh, BattleSpriteShader shader, in Matrix4x4 projection, in Matrix4x4 camView)
        {
            gl.BindTexture(TextureTarget.Texture2D, AnimImage.Texture);
            shader.SetMatrices(gl, projection, _transformCache * camView);
            shader.SetOpacity(gl, Opacity);
            shader.SetPixelateAmt(gl, PixelateAmt);
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
            mesh.Render();
        }
        public void RenderShadow(GL gl, BattleSpriteMesh mesh, BattleSpriteShadowShader shader, in Matrix4x4 viewProjection, in Matrix4x4 camView)
        {
            UpdateTransform(camView);

            gl.BindTexture(TextureTarget.Texture2D, AnimImage.Texture);
            shader.SetMatrix(gl, _transformCache * viewProjection);
            shader.SetSpriteOpacity(gl, Opacity);
            shader.SetPixelateAmt(gl, PixelateAmt);
            mesh.Render();
        }
    }
}
