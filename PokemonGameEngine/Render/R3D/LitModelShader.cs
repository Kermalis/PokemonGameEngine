using Silk.NET.OpenGL;

namespace Kermalis.PokemonGameEngine.Render.R3D
{
    internal sealed class LitModelShader : ModelShader
    {
        public const int MAX_LIGHTS = 4;

        private readonly int _lCameraPos;
        private readonly int _lNumLights;
        private readonly int[] _lLightPos;
        private readonly int[] _lLightColor;
        private readonly int[] _lLightAttenuation;
        private readonly int _lShineDamper;
        private readonly int _lReflectivity;

        public LitModelShader(GL gl)
            : base(gl, "Shaders\\model_lit_vert.glsl", "Shaders\\model_lit_frag.glsl")
        {
            _lCameraPos = GetUniformLocation(gl, "cameraPos");
            _lNumLights = GetUniformLocation(gl, "numLights");
            _lShineDamper = GetUniformLocation(gl, "shineDamper");
            _lReflectivity = GetUniformLocation(gl, "reflectivity");
            _lLightPos = new int[MAX_LIGHTS];
            _lLightColor = new int[MAX_LIGHTS];
            _lLightAttenuation = new int[MAX_LIGHTS];
            for (int i = 0; i < MAX_LIGHTS; i++)
            {
                _lLightPos[i] = GetUniformLocation(gl, "lightPos[" + i + ']');
                _lLightColor[i] = GetUniformLocation(gl, "lightColor[" + i + ']');
                _lLightAttenuation[i] = GetUniformLocation(gl, "lightAttenuation[" + i + ']');
            }
        }

        public override void SetCamera(GL gl, Camera c)
        {
            base.SetCamera(gl, c); // Projection and view
            gl.Uniform3(_lCameraPos, ref c.PR.Position); // Position
        }
        public void SetShineDamper(GL gl, float s)
        {
            gl.Uniform1(_lShineDamper, s);
        }
        public void SetReflectivity(GL gl, float r)
        {
            gl.Uniform1(_lReflectivity, r);
        }
        public void SetLights(GL gl, PointLight[] lights)
        {
            gl.Uniform1(_lNumLights, (uint)lights.Length);
            for (int i = 0; i < lights.Length; i++)
            {
                PointLight l = lights[i];
                gl.Uniform3(_lLightPos[i], ref l.Pos);
                gl.Uniform3(_lLightColor[i], ref l.Color);
                gl.Uniform3(_lLightAttenuation[i], ref l.Attenuation);
            }
        }
    }
}
