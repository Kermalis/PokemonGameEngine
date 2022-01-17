using Silk.NET.OpenGL;

namespace Kermalis.PokemonGameEngine.Render.Shaders.World
{
    internal sealed class VisualObjShader : Shader2D
    {
        private const string VERTEX_SHADER_PATH = @"World\VisualObj.vert.glsl";
        private const string FRAGMENT_SHADER_PATH = @"World\VisualObj.frag.glsl";

        private readonly int _lPos;
        private readonly int _lSize;

        private readonly int _lUVStart;
        private readonly int _lUVEnd;

        public VisualObjShader(GL gl)
            : base(gl, VERTEX_SHADER_PATH, FRAGMENT_SHADER_PATH)
        {
            _lPos = GetUniformLocation(gl, "pos");
            _lSize = GetUniformLocation(gl, "size");

            _lUVStart = GetUniformLocation(gl, "uvStart");
            _lUVEnd = GetUniformLocation(gl, "uvEnd");

            // Set texture unit now
            Use(gl);
            gl.Uniform1(GetUniformLocation(gl, "objTexture"), 0);
        }

        public void SetRect(GL gl, in Rect r)
        {
            gl.Uniform2(_lPos, r.TopLeft.X, r.TopLeft.Y);
            Vec2I size = r.GetSize();
            gl.Uniform2(_lSize, size.X, size.Y);
        }

        public void SetUV(GL gl, in UV uv)
        {
            gl.Uniform2(_lUVStart, uv.Start);
            gl.Uniform2(_lUVEnd, uv.End);
        }
    }
}
