using Silk.NET.OpenGL;

namespace Kermalis.PokemonGameEngine.Render.OpenGL
{
    internal class Shader2D : GLShader
    {
        private readonly int _lScreenSize;

        public Shader2D(GL gl, string vertexAsset, string fragmentAsset)
            : base(gl, vertexAsset, fragmentAsset)
        {
            _lScreenSize = GetUniformLocation(gl, "screenSize");
        }

        public void SetResolution(GL gl)
        {
            Size2D curSize = FrameBuffer.Current.Size;
            gl.Uniform2(_lScreenSize, curSize.Width, curSize.Height);
        }
    }
}
