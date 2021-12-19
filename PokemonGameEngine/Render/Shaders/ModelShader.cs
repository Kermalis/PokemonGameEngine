using Kermalis.PokemonGameEngine.Render.R3D;
using Silk.NET.OpenGL;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.Shaders
{
    internal sealed class ModelShader : GLShader
    {
        private const string VERTEX_SHADER_PATH = @"Model.vert.glsl";
        private const string FRAGMENT_SHADER_PATH = @"Model.frag.glsl";

        public const int MAX_LIGHTS = 4;

        private readonly int _lProjectionView;
        private readonly int _lTransform;

        private readonly int _lCameraPos;
        private readonly int _lShineDamper;
        private readonly int _lSpecularReflectivity;

        private readonly LitShaderUniforms _lightUniforms;

        public ModelShader(GL gl)
            : base(gl, VERTEX_SHADER_PATH, FRAGMENT_SHADER_PATH)
        {
            _lProjectionView = GetUniformLocation(gl, "projectionView");
            _lTransform = GetUniformLocation(gl, "transform");

            _lCameraPos = GetUniformLocation(gl, "cameraPos");
            _lShineDamper = GetUniformLocation(gl, "shineDamper");
            _lSpecularReflectivity = GetUniformLocation(gl, "specularReflectivity");

            _lightUniforms = new LitShaderUniforms(gl, this, MAX_LIGHTS);
        }

        public bool SetDiffuseTextureUnit(GL gl, int i)
        {
            int loc = GetUniformLocation(gl, "texture_diffuse" + (i + 1), throwIfNotExists: false);
            if (loc != -1)
            {
                gl.Uniform1(loc, i);
                return true;
            }
            return false;
        }
        public void SetCamera(GL gl, Camera cam)
        {
            Matrix4(gl, _lProjectionView, cam.CreateViewMatrix() * cam.Projection);
            gl.Uniform3(_lCameraPos, ref cam.PR.Position);
        }
        public void SetTransform(GL gl, in Matrix4x4 m)
        {
            Matrix4(gl, _lTransform, m);
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
