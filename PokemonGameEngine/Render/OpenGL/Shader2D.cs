using Silk.NET.OpenGL;

namespace Kermalis.PokemonGameEngine.Render.OpenGL
{
    internal class Shader2D : GLShader
    {
        private readonly int _lResolution;

        public Shader2D(GL gl, string vertexResource, string fragmentResource)
            : base(gl, vertexResource, fragmentResource)
        {
            _lResolution = GetUniformLocation(gl, "resolution");
        }

        public void SetResolution(GL gl)
        {
            gl.Uniform2(_lResolution, GLHelper.CurrentWidth, GLHelper.CurrentHeight);
        }
    }
}
