using Kermalis.PokemonGameEngine.Render.OpenGL;
using Silk.NET.OpenGL;

namespace Kermalis.PokemonGameEngine.Render.R3D
{
    internal struct LitShaderUniforms
    {
        private readonly int _lNumLights;
        private readonly int[] _lLightPos;
        private readonly int[] _lLightColor;
        private readonly int[] _lLightAttenuation;

        public LitShaderUniforms(GL gl, GLShader shader, int max)
        {
            _lNumLights = shader.GetUniformLocation(gl, "numLights");
            _lLightPos = new int[max];
            _lLightColor = new int[max];
            _lLightAttenuation = new int[max];
            for (int i = 0; i < max; i++)
            {
                _lLightPos[i] = shader.GetUniformLocation(gl, "lightPos[" + i + ']');
                _lLightColor[i] = shader.GetUniformLocation(gl, "lightColor[" + i + ']');
                _lLightAttenuation[i] = shader.GetUniformLocation(gl, "lightAttenuation[" + i + ']');
            }
        }

        public void SetLights(GL gl, PointLight[] lights)
        {
            gl.Uniform1(_lNumLights, (uint)(lights.Length));
            for (int i = 0; i < lights.Length; i++)
            {
                PointLight l = lights[i];
                gl.Uniform3(_lLightPos[i], ref l.Pos);
                Colors.PutInShader(gl, _lLightColor[i], l.Color);
                gl.Uniform3(_lLightAttenuation[i], ref l.Attenuation);
            }
        }
    }
}
