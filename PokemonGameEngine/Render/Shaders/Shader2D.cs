using Silk.NET.OpenGL;

namespace Kermalis.PokemonGameEngine.Render.Shaders
{
    internal abstract class Shader2D : GLShader
    {
        private readonly int _lViewportSize;

        public Shader2D(GL gl, string vertexAsset, string fragmentAsset)
            : base(gl, vertexAsset, fragmentAsset)
        {
            _lViewportSize = GetUniformLocation(gl, "viewportSize");
        }

        public void UpdateViewport(GL gl, Vec2I size)
        {
            gl.Uniform2(_lViewportSize, size.X, size.Y);
        }
    }
}
