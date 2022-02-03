using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System.Numerics;

namespace Kermalis.PokemonGameEngine.Render.Shaders.GUIs
{
    internal sealed class GUIRectShader : Shader2D
    {
        private const string VERTEX_SHADER_PATH = @"GUIs\GUI_Rect.vert.glsl";
        private const string FRAGMENT_SHADER_PATH = @"GUIs\GUI_Rect.frag.glsl";

        public static GUIRectShader Instance { get; private set; } = null!; // Set in RenderManager

        private readonly int _lPos;
        private readonly int _lSize;
        private readonly int _lCornerRadii;
        private readonly int _lLineThickness;
        private readonly int _lOpacity;

        private readonly int _lUseTexture;
        private readonly int _lColor;
        private readonly int _lLineColor;
        private readonly int _lUVStart;
        private readonly int _lUVEnd;

        public GUIRectShader(GL gl)
            : base(gl, VERTEX_SHADER_PATH, FRAGMENT_SHADER_PATH)
        {
            Instance = this;

            _lPos = GetUniformLocation(gl, "u_pos");
            _lSize = GetUniformLocation(gl, "u_size");
            _lCornerRadii = GetUniformLocation(gl, "u_cornerRadii");
            _lLineThickness = GetUniformLocation(gl, "u_lineThickness");
            _lOpacity = GetUniformLocation(gl, "u_opacity");

            _lUseTexture = GetUniformLocation(gl, "u_useTexture");
            _lColor = GetUniformLocation(gl, "u_color");
            _lLineColor = GetUniformLocation(gl, "u_lineColor");
            _lUVStart = GetUniformLocation(gl, "u_uvStart");
            _lUVEnd = GetUniformLocation(gl, "u_uvEnd");

            // Set texture unit now
            Use(gl);
            gl.Uniform1(GetUniformLocation(gl, "u_texture"), 0);
        }

        public void SetRect(GL gl, in Rect r)
        {
            gl.Uniform2(_lPos, r.TopLeft.X, r.TopLeft.Y);
            Vec2I size = r.GetSize();
            gl.Uniform2(_lSize, size.X, size.Y);
        }
        public void SetCornerRadii(GL gl, in Vector4D<int> v)
        {
            gl.Uniform4(_lCornerRadii, v.X, v.Y, v.Z, v.W);
        }
        public void SetLineThickness(GL gl, int i)
        {
            gl.Uniform1(_lLineThickness, i);
        }
        public void SetOpacity(GL gl, float f)
        {
            gl.Uniform1(_lOpacity, f);
        }

        public void SetUseTexture(GL gl, bool b)
        {
            gl.Uniform1(_lUseTexture, b ? 1 : 0);
        }
        public void SetColor(GL gl, in Vector4 c)
        {
            Colors.PutInShader(gl, _lColor, c);
        }
        public void SetLineColor(GL gl, in Vector4 c)
        {
            Colors.PutInShader(gl, _lLineColor, c);
        }
        public void SetUV(GL gl, in UV uv)
        {
            gl.Uniform2(_lUVStart, uv.Start);
            gl.Uniform2(_lUVEnd, uv.End);
        }
    }
}
