using Silk.NET.OpenGL;

namespace Kermalis.PokemonGameEngine.Render.Shaders
{
    internal class Shader2D : GLShader
    {
        public static Size2D ViewportSize;

        private readonly int _lScreenSize;

        public Shader2D(GL gl, string vertexAsset, string fragmentAsset)
            : base(gl, vertexAsset, fragmentAsset)
        {
            _lScreenSize = GetUniformLocation(gl, "screenSize");
        }

        public void UpdateViewport(GL gl)
        {
            gl.Uniform2(_lScreenSize, ViewportSize.Width, ViewportSize.Height);
        }
    }
}
