using Silk.NET.OpenGL;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.Shaders
{
    internal sealed class BattleSpriteShadowShader : GLShader
    {
        private const string VERTEX_SHADER_PATH = @"BattleSpriteShadow.vert.glsl";
        private const string FRAGMENT_SHADER_PATH = @"BattleSpriteShadow.frag.glsl";

        private readonly int _lTransformViewProjection;

        private readonly int _lSpriteTexture;
        private readonly int _lSpriteOpacity;
        private readonly int _lPixelateAmt;

        public BattleSpriteShadowShader(GL gl)
            : base(gl, VERTEX_SHADER_PATH, FRAGMENT_SHADER_PATH)
        {
            _lTransformViewProjection = GetUniformLocation(gl, "transformViewProjection");

            _lSpriteTexture = GetUniformLocation(gl, "spriteTexture");
            _lSpriteOpacity = GetUniformLocation(gl, "spriteOpacity");
            _lPixelateAmt = GetUniformLocation(gl, "pixelateAmt");

            Use(gl);
            gl.Uniform1(_lSpriteTexture, 0); // Set texture unit now
        }

        public void SetMatrix(GL gl, in Matrix4x4 transformViewProjection)
        {
            Matrix4(gl, _lTransformViewProjection, transformViewProjection);
        }
        public void SetSpriteOpacity(GL gl, float v)
        {
            gl.Uniform1(_lSpriteOpacity, v);
        }
        public void SetPixelateAmt(GL gl, float v)
        {
            gl.Uniform1(_lPixelateAmt, v);
        }
    }
}
