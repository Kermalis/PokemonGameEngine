﻿using Silk.NET.OpenGL;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.Shaders.Battle
{
    internal sealed class BattleSpriteShader : GLShader
    {
        private const string VERTEX_SHADER_PATH = @"Battle\BattleSprite.vert.glsl";
        private const string FRAGMENT_SHADER_PATH = @"Battle\BattleSprite.frag.glsl";

        private readonly int _lTransformViewProjection;

        private readonly int _lOutputShadow;
        private readonly int _lOpacity;

        private readonly int _lMaskColor;
        private readonly int _lMaskColorAmt;
        private readonly int _lPixelateAmt;

        public BattleSpriteShader(GL gl)
            : base(gl, VERTEX_SHADER_PATH, FRAGMENT_SHADER_PATH)
        {
            _lTransformViewProjection = GetUniformLocation(gl, "transformViewProjection");

            _lOutputShadow = GetUniformLocation(gl, "outputShadow");
            _lOpacity = GetUniformLocation(gl, "opacity");

            _lMaskColor = GetUniformLocation(gl, "maskColor");
            _lMaskColorAmt = GetUniformLocation(gl, "maskColorAmt");
            _lPixelateAmt = GetUniformLocation(gl, "pixelateAmt");

            // Set texture unit now
            Use(gl);
            gl.Uniform1(GetUniformLocation(gl, "imgTexture"), 0);
        }

        public void SetMatrix(GL gl, in Matrix4x4 transformViewProjection)
        {
            Matrix4(gl, _lTransformViewProjection, transformViewProjection);
        }
        public void SetOutputShadow(GL gl, bool b)
        {
            gl.Uniform1(_lOutputShadow, b ? 1 : 0);
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
