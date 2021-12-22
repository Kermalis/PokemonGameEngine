using Kermalis.PokemonGameEngine.Core;
using Kermalis.PokemonGameEngine.Render;
using Kermalis.PokemonGameEngine.Render.Images;
using Kermalis.PokemonGameEngine.Render.R3D;
using Kermalis.PokemonGameEngine.Render.Shaders;
using Silk.NET.OpenGL;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.GUI.Battle
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
        }

        public void Render(GL gl, BattleSpriteMesh mesh, BattleSpriteShader shader, in Matrix4x4 projection, in Matrix4x4 view)
        {
            gl.BindTexture(TextureTarget.Texture2D, AnimImage.Texture);

            Size2D imgSize = AnimImage.Size;
            Matrix4x4 transform = Matrix4x4.CreateScale(imgSize.Width * Scale.X, imgSize.Height * Scale.Y, 1f)
                * Matrix4x4.CreateRotationZ(Rotation2D * Utils.DegToRad);
            var translation = Matrix4x4.CreateTranslation(Pos);
            // Remove x/y rotation when they get multiplied together so the sprite always faces the camera
            translation.M11 = view.M11;
            translation.M12 = view.M21;
            translation.M13 = view.M31;
            translation.M21 = view.M12;
            translation.M22 = view.M22;
            translation.M23 = view.M32;
            translation.M31 = view.M13;
            translation.M32 = view.M23;
            translation.M33 = view.M33;
            transform *= translation;

            shader.SetMatrices(gl, projection, transform * view);
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
    }
}
