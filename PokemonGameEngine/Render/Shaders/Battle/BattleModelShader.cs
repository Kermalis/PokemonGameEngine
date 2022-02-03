using Kermalis.PokemonGameEngine.Render.R3D;
using Silk.NET.OpenGL;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.Shaders.Battle
{
    internal sealed class BattleModelShader : GLShader
    {
        private const string VERTEX_SHADER_PATH = @"Battle\BattleModel.vert.glsl";
        private const string FRAGMENT_SHADER_PATH = @"Battle\BattleModel.frag.glsl";

        public const int MAX_LIGHTS = 4;

        private readonly int _lProjection;
        private readonly int _lView;
        private readonly int _lTransform;
        private readonly int _lShadowTextureConversion;

        private readonly int _lCameraPos;
        private readonly int _lShineDamper;
        private readonly int _lSpecularReflectivity;

        private readonly LitShaderUniforms _lightUniforms;

        public BattleModelShader(GL gl)
            : base(gl, VERTEX_SHADER_PATH, FRAGMENT_SHADER_PATH)
        {
            _lProjection = GetUniformLocation(gl, "u_projection");
            _lView = GetUniformLocation(gl, "u_view");
            _lTransform = GetUniformLocation(gl, "u_transform");
            _lShadowTextureConversion = GetUniformLocation(gl, "u_shadowTextureConversion");

            _lCameraPos = GetUniformLocation(gl, "u_cameraPos");
            _lShineDamper = GetUniformLocation(gl, "u_shineDamper");
            _lSpecularReflectivity = GetUniformLocation(gl, "u_specularReflectivity");

            _lightUniforms = new LitShaderUniforms(gl, this, MAX_LIGHTS);

            // Set texture units now
            Use(gl);
            gl.Uniform1(GetUniformLocation(gl, "u_shadowColorTexture"), 0);
            gl.Uniform1(GetUniformLocation(gl, "u_shadowDepthTexture"), 1);
            SetShineDamper(gl, 5f);
            SetReflectivity(gl, 0f);
        }

        public bool SetDiffuseTextureUnit(GL gl, int i, int textureUnit)
        {
            int loc = GetUniformLocation(gl, "u_diffuseTexture" + (i + 1), throwIfNotExists: false);
            if (loc != -1)
            {
                gl.Uniform1(loc, textureUnit);
                return true;
            }
            return false;
        }
        public void SetCamera(GL gl, in Matrix4x4 projection, in Matrix4x4 view, in Vector3 pos)
        {
            Matrix4(gl, _lProjection, projection);
            Matrix4(gl, _lView, view);
            gl.Uniform3(_lCameraPos, pos);
        }
        public void SetTransform(GL gl, in Matrix4x4 m)
        {
            Matrix4(gl, _lTransform, m);
        }
        public void SetShadowConversion(GL gl, in Matrix4x4 m)
        {
            Matrix4(gl, _lShadowTextureConversion, m);
        }
        public void SetShineDamper(GL gl, float v)
        {
            gl.Uniform1(_lShineDamper, v);
        }
        public void SetReflectivity(GL gl, float v)
        {
            gl.Uniform1(_lSpecularReflectivity, v);
        }
        public void SetLights(GL gl, PointLight[] lights)
        {
            _lightUniforms.SetLights(gl, lights);
        }
    }
}
