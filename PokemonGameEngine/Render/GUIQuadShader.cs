using Kermalis.PokemonGameEngine.Render.OpenGL;
using Silk.NET.OpenGL;

namespace Kermalis.PokemonGameEngine.Render
{
    internal sealed class GUIQuadShader : Shader2D
    {
        private readonly int _lColor;

        public GUIQuadShader(GL gl)
            : base(gl, "Shaders.gui_quad_vert.glsl", "Shaders.gui_quad_frag.glsl")
        {
            _lColor = GetUniformLocation(gl, "color");
        }

        public void SetColor(GL gl, in ColorF c)
        {
            gl.Uniform4(_lColor, c.R, c.G, c.B, c.A);
        }
    }
}
