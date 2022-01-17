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
        private readonly int _lCornerRadius;
        private readonly int _lLineThickness;
        private readonly int _lOpacity;

        private readonly int _lUseTexture;
        private readonly int _lColor;
        private readonly int _lUVStart;
        private readonly int _lUVEnd;

        public GUIRectShader(GL gl)
            : base(gl, VERTEX_SHADER_PATH, FRAGMENT_SHADER_PATH)
        {
            Instance = this;

            _lPos = GetUniformLocation(gl, "pos");
            _lSize = GetUniformLocation(gl, "size");
            _lCornerRadius = GetUniformLocation(gl, "cornerRadius");
            _lLineThickness = GetUniformLocation(gl, "lineThickness");
            _lOpacity = GetUniformLocation(gl, "opacity");

            _lUseTexture = GetUniformLocation(gl, "useTexture");
            _lColor = GetUniformLocation(gl, "color");
            _lUVStart = GetUniformLocation(gl, "uvStart");
            _lUVEnd = GetUniformLocation(gl, "uvEnd");

            // Set texture unit now
            Use(gl);
            gl.Uniform1(GetUniformLocation(gl, "guiTexture"), 0);
        }

        public void SetRect(GL gl, in Rect r)
        {
            gl.Uniform2(_lPos, r.TopLeft.X, r.TopLeft.Y);
            Vec2I size = r.GetSize();
            gl.Uniform2(_lSize, size.X, size.Y);
        }
        public void SetCornerRadius(GL gl, int i)
        {
            gl.Uniform1(_lCornerRadius, i);
        }
        public void SetLineThickness(GL gl, int i)
        {
            gl.Uniform1(_lLineThickness, i);
        }
        public void SetOpacity(GL gl, float f)
        {
            gl.Uniform1(_lOpacity, f);
        }

        public void SetColor(GL gl, in Vector4 c)
        {
            gl.Uniform1(_lUseTexture, 0);
            Colors.PutInShader(gl, _lColor, c);
        }
        public void SetUV(GL gl, in UV uv)
        {
            gl.Uniform1(_lUseTexture, 1);
            gl.Uniform2(_lUVStart, uv.Start);
            gl.Uniform2(_lUVEnd, uv.End);
        }
    }
}
