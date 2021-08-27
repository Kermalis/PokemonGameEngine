using Silk.NET.OpenGL;

namespace Kermalis.PokemonGameEngine.Render.OpenGL
{
    internal class Shader2D : GLShader
    {
        private readonly int _lResolution;

        public Shader2D(GL gl, string vertexAsset, string fragmentAsset)
            : base(gl, vertexAsset, fragmentAsset)
        {
            _lResolution = GetUniformLocation(gl, "resolution");
        }

        public void SetResolution(GL gl)
        {
            gl.Uniform2(_lResolution, GLHelper.CurrentWidth, GLHelper.CurrentHeight);
        }
    }
}
