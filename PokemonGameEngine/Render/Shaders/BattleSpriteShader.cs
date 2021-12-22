using Silk.NET.OpenGL;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.Shaders
{
    internal sealed class BattleSpriteShader : GLShader
    {
        private const string VERTEX_SHADER_PATH = @"BattleSprite.vert.glsl";
        private const string FRAGMENT_SHADER_PATH = @"BattleSprite.frag.glsl";

        private readonly int _lProjection;
        private readonly int _lTransformView;

        private readonly int _lImgTexture;
        private readonly int _lOpacity;

        private readonly int _lMaskColor;
        private readonly int _lMaskColorAmt;
        private readonly int _lPixelateAmt;

        public BattleSpriteShader(GL gl)
            : base(gl, VERTEX_SHADER_PATH, FRAGMENT_SHADER_PATH)
        {
            _lProjection = GetUniformLocation(gl, "projection");
            _lTransformView = GetUniformLocation(gl, "transformView");

            _lImgTexture = GetUniformLocation(gl, "imgTexture");
            _lOpacity = GetUniformLocation(gl, "opacity");

            _lMaskColor = GetUniformLocation(gl, "maskColor");
            _lMaskColorAmt = GetUniformLocation(gl, "maskColorAmt");
            _lPixelateAmt = GetUniformLocation(gl, "pixelateAmt");

            Use(gl);
            gl.Uniform1(_lImgTexture, 0); // Set texture unit now
        }

        public void SetMatrices(GL gl, Matrix4x4 projection, Matrix4x4 transformView)
        {
            Matrix4(gl, _lProjection, projection);
            Matrix4(gl, _lTransformView, transformView);
        }
        public void SetOpacity(GL gl, float opacity)
        {
            gl.Uniform1(_lOpacity, opacity);
        }
        public void SetMaskColor(GL gl, in Vector3 color)
        {
            gl.Uniform3(_lMaskColor, color);
        }
        public void SetMaskColorAmt(GL gl, float amt)
        {
            gl.Uniform1(_lMaskColorAmt, amt);
        }
        public void SetPixelateAmt(GL gl, float amt)
        {
            gl.Uniform1(_lPixelateAmt, amt);
        }
    }
}
